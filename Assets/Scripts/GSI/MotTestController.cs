using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전형적 다중 추적(MOT): 표식 → 전원 동일 외형으로 이동 → 표식만 다시 고르기.
/// </summary>
public sealed class MotTestController : MonoBehaviour
{
    private bool _subscribed;
    private bool _pendingSessionStart;
    private Coroutine _sessionRoutine;

    private Transform _playRoot;
    private Image _panelBackgroundImage;
    private bool _panelBackgroundSuppressed;
    private TextMeshProUGUI _hudText;
    private Button _submitButton;
    private RectTransform _fieldRect;

    private readonly List<MotDisk> _disks = new List<MotDisk>();
    private readonly HashSet<int> _selected = new HashSet<int>();

    private int _targetCount;
    private float _lastTrialAccuracy;
    private bool _motionActive;
    private bool _trialSubmitPending;
    private float _trialResultAccuracy;

    private sealed class MotDisk
    {
        public int Index;
        public bool IsTarget;
        public RectTransform Rect;
        public Image Fill;
        public Image SelectionRing;
        public Vector2 Velocity;
        public float Radius;
    }

    private void Awake()
    {
        TrySubscribeGameManager();
    }

    private void Start()
    {
        TrySubscribeGameManager();
        TryFlushPendingSessionStart();
    }

    private void OnEnable()
    {
        TrySubscribeGameManager();
        if (GameManager.Instance != null)
        {
            HandleGameStateChanged(GameManager.Instance.CurrentState);
        }

        TryFlushPendingSessionStart();
    }

    private void OnDisable()
    {
        StopSession();
        DestroyPlayRoot();
        _pendingSessionStart = false;
    }

    private void OnDestroy()
    {
        UnsubscribeGameManager();
    }

    private void Update()
    {
        if (!_motionActive || _fieldRect == null)
        {
            return;
        }

        Rect bounds = _fieldRect.rect;
        float halfW = bounds.width * 0.5f - 2f;
        float halfH = bounds.height * 0.5f - 2f;
        float dt = Time.deltaTime;
        for (int i = 0; i < _disks.Count; i++)
        {
            MotDisk d = _disks[i];
            if (d.Rect == null)
            {
                continue;
            }

            Vector2 p = d.Rect.anchoredPosition;
            Vector2 vel = d.Velocity;
            p += vel * dt;
            float rad = d.Radius;
            if (p.x + rad > halfW)
            {
                p.x = halfW - rad;
                vel.x = -Mathf.Abs(vel.x);
            }
            else if (p.x - rad < -halfW)
            {
                p.x = -halfW + rad;
                vel.x = Mathf.Abs(vel.x);
            }

            if (p.y + rad > halfH)
            {
                p.y = halfH - rad;
                vel.y = -Mathf.Abs(vel.y);
            }
            else if (p.y - rad < -halfH)
            {
                p.y = -halfH + rad;
                vel.y = Mathf.Abs(vel.y);
            }

            d.Velocity = vel;
            d.Rect.anchoredPosition = p;
        }
    }

    private void TrySubscribeGameManager()
    {
        if (_subscribed || GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        _subscribed = true;
    }

    private void UnsubscribeGameManager()
    {
        if (!_subscribed || GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        _subscribed = false;
    }

    private void TryFlushPendingSessionStart()
    {
        if (!_pendingSessionStart || !isActiveAndEnabled)
        {
            return;
        }

        if (GameManager.Instance == null
            || GameManager.Instance.CurrentState != GameState.TestInProgress
            || GameManager.Instance.CurrentTestMode != TestMode.MultipleObjectTracking)
        {
            _pendingSessionStart = false;
            return;
        }

        _pendingSessionStart = false;
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }

        StopSession();
        _sessionRoutine = StartCoroutine(RunSessionRoutine());
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentTestMode != TestMode.MultipleObjectTracking)
        {
            _pendingSessionStart = false;
            StopSession();
            return;
        }

        if (newState == GameState.TestInProgress)
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScore();
            }

            StopSession();

            if (!isActiveAndEnabled)
            {
                _pendingSessionStart = true;
                return;
            }

            _pendingSessionStart = false;
            _sessionRoutine = StartCoroutine(RunSessionRoutine());
            return;
        }

        _pendingSessionStart = false;
        StopSession();
    }

    private void StopSession()
    {
        if (_sessionRoutine != null)
        {
            StopCoroutine(_sessionRoutine);
            _sessionRoutine = null;
        }

        _motionActive = false;
        DestroyPlayRoot();
        RestorePanelBackgroundIfNeeded();
    }

    private IEnumerator RunSessionRoutine()
    {
        bool isExam = GameManager.Instance != null
            && (GameManager.Instance.CurrentTestType == TestType.OfficialExam
                || GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam);
        int trials = isExam ? MotDifficulty.GetExamTrialCount() : 1;
        // 세션마다 다른 시드 — 고정 상수 시드는 연습·시험 모두 매번 동일 패턴이 됨
        int sessionBase = Random.Range(int.MinValue, int.MaxValue);
        float sumAcc = 0f;

        for (int t = 0; t < trials; t++)
        {
            yield return RunSingleTrial(unchecked(sessionBase + t * 7919));
            sumAcc += _lastTrialAccuracy;
        }

        float avgAcc = sumAcc / trials;
        bool failed = avgAcc < 38f;

        if (GameManager.Instance != null && GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam)
        {
            GameManager.Instance.CompleteUnifiedExamSegment(new UnifiedExamSegmentPayload
            {
                Mode = TestMode.MultipleObjectTracking,
                HardFailed = false,
                Primary = avgAcc
            });
            _sessionRoutine = null;
            yield break;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SaveMotResult(avgAcc, failed);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.ResultScreen);
        }

        _sessionRoutine = null;
    }

    private IEnumerator RunSingleTrial(int seed)
    {
        int grade = GameManager.Instance != null
            ? (GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam
                ? GameManager.Instance.UnifiedExamGrade
                : GameManager.Instance.GetPracticeGrade(TestMode.MultipleObjectTracking))
            : 9;
        MotDifficulty.GetTrackingParams(grade, out int targets, out int distractors, out float speed, out float motionDur, out float cueHold);
        _targetCount = targets;
        int total = targets + distractors;

        BuildPlayArea(total, targets, seed, speed);

        if (_submitButton == null)
        {
            yield break;
        }

        if (_hudText != null)
        {
            _hudText.text = "표시된 대상을 기억하세요.";
        }

        yield return new WaitForSecondsRealtime(cueHold);

        for (int i = 0; i < _disks.Count; i++)
        {
            SetDiskCueVisual(_disks[i], false);
        }

        if (_hudText != null)
        {
            _hudText.text = "추적 중… (모두 같은 모양입니다)";
        }

        _motionActive = true;
        yield return new WaitForSecondsRealtime(motionDur);
        _motionActive = false;

        if (_hudText != null)
        {
            _hudText.text = $"추적했던 대상을 눌러 선택하세요 (최대 {targets}개). [제출]은 언제든 누를 수 있습니다.";
        }

        _selected.Clear();
        _trialSubmitPending = true;
        _submitButton.interactable = true;
        RefreshAllDiskSelectionVisuals();
        UpdateSelectionHudText();

        for (int i = 0; i < _disks.Count; i++)
        {
            int idx = i;
            MotDisk d = _disks[i];
            if (d.Fill != null)
            {
                d.Fill.raycastTarget = true;
            }

            Button b = d.Rect.GetComponent<Button>();
            if (b != null)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => OnDiskClicked(idx));
            }
        }

        if (_submitButton != null)
        {
            _submitButton.onClick.RemoveAllListeners();
            _submitButton.onClick.AddListener(OnSubmitClicked);
        }

        while (_trialSubmitPending)
        {
            yield return null;
        }

        _lastTrialAccuracy = _trialResultAccuracy;
        DestroyPlayRoot();
    }

    private void OnDiskClicked(int index)
    {
        if (!_trialSubmitPending || index < 0 || index >= _disks.Count)
        {
            return;
        }

        if (_selected.Contains(index))
        {
            _selected.Remove(index);
        }
        else if (_selected.Count < _targetCount)
        {
            _selected.Add(index);
        }

        RefreshAllDiskSelectionVisuals();
        UpdateSelectionHudText();
    }

    private void OnSubmitClicked()
    {
        if (!_trialSubmitPending)
        {
            return;
        }

        int hits = 0;
        for (int i = 0; i < _disks.Count; i++)
        {
            if (_selected.Contains(i) && _disks[i].IsTarget)
            {
                hits++;
            }
        }

        _trialResultAccuracy = _targetCount > 0 ? hits / (float)_targetCount * 100f : 0f;
        _trialSubmitPending = false;
    }

    private void UpdateSelectionHudText()
    {
        if (_hudText == null || !_trialSubmitPending)
        {
            return;
        }

        _hudText.text = $"선택 {_selected.Count} / {_targetCount}개  ·  [제출]로 확정 (미선택·일부 선택도 제출 가능)";
    }

    private void RefreshAllDiskSelectionVisuals()
    {
        Color baseFill = new Color(0.32f, 0.34f, 0.4f, 1f);
        Color selectedFill = new Color(0.35f, 0.82f, 1f, 1f);
        Color selectedRing = new Color(0.2f, 0.65f, 1f, 0.95f);

        for (int i = 0; i < _disks.Count; i++)
        {
            bool on = _selected.Contains(i);
            MotDisk d = _disks[i];
            if (d.Fill != null)
            {
                d.Fill.color = on ? selectedFill : baseFill;
            }

            if (d.SelectionRing != null)
            {
                d.SelectionRing.enabled = on;
                d.SelectionRing.color = selectedRing;
            }

            if (d.Rect != null)
            {
                d.Rect.localScale = on ? new Vector3(1.14f, 1.14f, 1f) : Vector3.one;
            }
        }
    }

    private void SetDiskCueVisual(MotDisk d, bool cue)
    {
        if (d.Fill == null)
        {
            return;
        }

        d.Fill.color = cue
            ? new Color(0.95f, 0.82f, 0.35f, 1f)
            : new Color(0.32f, 0.34f, 0.4f, 1f);
    }

    private void BuildPlayArea(int totalObjects, int targets, int seed, float speed)
    {
        DestroyPlayRoot();
        HideOtherTestTargetsOnPanel();
        SuppressPanelBackgroundForMot();

        var rootGo = new GameObject("MotPlayRoot");
        var rootRt = rootGo.AddComponent<RectTransform>();
        _playRoot = rootGo.transform;
        _playRoot.SetParent(transform, false);
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = new Vector2(20f, 88f);
        rootRt.offsetMax = new Vector2(-20f, -20f);
        rootRt.localScale = Vector3.one;

        var canvas = rootGo.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        rootGo.AddComponent<GraphicRaycaster>();

        var bgGo = new GameObject("MotBackdrop");
        bgGo.transform.SetParent(_playRoot, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.09f, 0.12f, 0.97f);
        bgImg.raycastTarget = false;

        var hudGo = new GameObject("MotHud");
        hudGo.transform.SetParent(_playRoot, false);
        var hudRt = hudGo.AddComponent<RectTransform>();
        hudRt.anchorMin = new Vector2(0f, 1f);
        hudRt.anchorMax = new Vector2(1f, 1f);
        hudRt.pivot = new Vector2(0.5f, 1f);
        hudRt.sizeDelta = new Vector2(0f, 40f);
        hudRt.anchoredPosition = Vector2.zero;
        _hudText = hudGo.AddComponent<TextMeshProUGUI>();
        _hudText.fontSize = 17;
        _hudText.alignment = TextAlignmentOptions.Center;
        _hudText.color = new Color(0.88f, 0.9f, 0.93f, 1f);
        _hudText.raycastTarget = false;
        TMP_FontAsset font = TmpFontCache.LiberationSansSdf;
        if (font != null)
        {
            _hudText.font = font;
        }

        var fieldGo = new GameObject("MotField");
        fieldGo.transform.SetParent(_playRoot, false);
        _fieldRect = fieldGo.AddComponent<RectTransform>();
        _fieldRect.anchorMin = new Vector2(0.06f, 0.12f);
        _fieldRect.anchorMax = new Vector2(0.94f, 0.88f);
        _fieldRect.offsetMin = Vector2.zero;
        _fieldRect.offsetMax = Vector2.zero;
        var fieldBg = fieldGo.AddComponent<Image>();
        fieldBg.color = new Color(0.11f, 0.12f, 0.16f, 0.95f);
        fieldBg.raycastTarget = false;

        var submitGo = new GameObject("Submit");
        submitGo.transform.SetParent(_playRoot, false);
        var subRt = submitGo.AddComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.35f, 0.02f);
        subRt.anchorMax = new Vector2(0.65f, 0.1f);
        subRt.offsetMin = Vector2.zero;
        subRt.offsetMax = Vector2.zero;
        var subImg = submitGo.AddComponent<Image>();
        subImg.color = new Color(0.28f, 0.42f, 0.62f, 1f);
        _submitButton = submitGo.AddComponent<Button>();
        _submitButton.targetGraphic = subImg;
        _submitButton.interactable = false;

        var subLabelGo = new GameObject("Label");
        subLabelGo.transform.SetParent(submitGo.transform, false);
        var subLabelRt = subLabelGo.AddComponent<RectTransform>();
        subLabelRt.anchorMin = Vector2.zero;
        subLabelRt.anchorMax = Vector2.one;
        subLabelRt.offsetMin = Vector2.zero;
        subLabelRt.offsetMax = Vector2.zero;
        var subTmp = subLabelGo.AddComponent<TextMeshProUGUI>();
        subTmp.text = "제출";
        subTmp.fontSize = 20;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = Color.white;
        subTmp.raycastTarget = false;
        if (font != null)
        {
            subTmp.font = font;
        }

        _playRoot.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRt);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_fieldRect);

        _disks.Clear();
        var rng = new System.Random(seed);
        float tEase = PracticeDifficulty.Ease01(GameManager.Instance != null
            ? GameManager.Instance.GetPracticeGrade(TestMode.MultipleObjectTracking)
            : 9);
        float diameter = Mathf.Lerp(30f, 44f, tEase);
        float rad = diameter * 0.5f;
        Rect field = _fieldRect.rect;
        float halfW = field.width * 0.5f - rad - 4f;
        float halfH = field.height * 0.5f - rad - 4f;

        var positions = new List<Vector2>();
        int tries = 0;
        while (positions.Count < totalObjects && tries < 8000)
        {
            tries++;
            float x = (float)(rng.NextDouble() * 2 - 1) * halfW * 0.92f;
            float y = (float)(rng.NextDouble() * 2 - 1) * halfH * 0.92f;
            var candidate = new Vector2(x, y);
            bool ok = true;
            for (int p = 0; p < positions.Count; p++)
            {
                if (Vector2.Distance(candidate, positions[p]) < (rad * 2f + 6f))
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
            {
                positions.Add(candidate);
            }
        }

        while (positions.Count < totalObjects)
        {
            positions.Add(Vector2.zero);
        }

        var targetSet = new HashSet<int>();
        while (targetSet.Count < targets)
        {
            targetSet.Add(rng.Next(0, totalObjects));
        }

        for (int i = 0; i < totalObjects; i++)
        {
            var diskGo = new GameObject($"Disk_{i}");
            diskGo.transform.SetParent(_fieldRect, false);
            var dRt = diskGo.AddComponent<RectTransform>();
            dRt.sizeDelta = new Vector2(diameter, diameter);
            dRt.anchorMin = dRt.anchorMax = new Vector2(0.5f, 0.5f);
            dRt.pivot = new Vector2(0.5f, 0.5f);
            dRt.anchoredPosition = positions[i];

            var ringGo = new GameObject("SelectionRing");
            ringGo.transform.SetParent(diskGo.transform, false);
            var ringRt = ringGo.AddComponent<RectTransform>();
            ringRt.anchorMin = Vector2.zero;
            ringRt.anchorMax = Vector2.one;
            ringRt.offsetMin = new Vector2(-5f, -5f);
            ringRt.offsetMax = new Vector2(5f, 5f);
            var ringImg = ringGo.AddComponent<Image>();
            ringImg.color = new Color(0.2f, 0.65f, 1f, 0.95f);
            ringImg.raycastTarget = false;
            ringImg.enabled = false;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(diskGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var img = fillGo.AddComponent<Image>();
            img.color = new Color(0.32f, 0.34f, 0.4f, 1f);
            img.raycastTarget = false;

            var btn = diskGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;

            bool isT = targetSet.Contains(i);
            var disk = new MotDisk
            {
                Index = i,
                IsTarget = isT,
                Rect = dRt,
                Fill = img,
                SelectionRing = ringImg,
                Radius = rad,
                Velocity = RandomUnitVector2(rng) * speed
            };
            _disks.Add(disk);
            SetDiskCueVisual(disk, isT);
            img.raycastTarget = false;
            if (disk.Velocity.sqrMagnitude < 4f)
            {
                disk.Velocity = RandomUnitVector2(rng) * speed;
            }
        }
    }

    private static Vector2 RandomUnitVector2(System.Random rng)
    {
        double a = rng.NextDouble() * System.Math.PI * 2;
        return new Vector2((float)System.Math.Cos(a), (float)System.Math.Sin(a));
    }

    private void HideOtherTestTargetsOnPanel()
    {
        Transform panel = transform;
        Transform aim = panel.Find("AimTargetArea");
        if (aim != null)
        {
            aim.gameObject.SetActive(false);
        }

        Transform reaction = panel.Find("ReactionTarget");
        if (reaction != null)
        {
            reaction.gameObject.SetActive(false);
        }
    }

    private void SuppressPanelBackgroundForMot()
    {
        if (_panelBackgroundSuppressed)
        {
            return;
        }

        if (_panelBackgroundImage == null)
        {
            _panelBackgroundImage = GetComponent<Image>();
        }

        if (_panelBackgroundImage != null)
        {
            _panelBackgroundImage.enabled = false;
            _panelBackgroundSuppressed = true;
        }
    }

    private void RestorePanelBackgroundIfNeeded()
    {
        if (!_panelBackgroundSuppressed || _panelBackgroundImage == null)
        {
            return;
        }

        _panelBackgroundImage.enabled = true;
        _panelBackgroundSuppressed = false;
    }

    private void DestroyPlayRoot()
    {
        _motionActive = false;
        _trialSubmitPending = false;
        _disks.Clear();
        _selected.Clear();
        _fieldRect = null;
        _submitButton = null;
        _hudText = null;
        if (_playRoot != null)
        {
            Destroy(_playRoot.gameObject);
            _playRoot = null;
        }
    }
}
