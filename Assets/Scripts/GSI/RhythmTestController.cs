using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 키보드 리듬 판정: 노트가 판정선에 닿을 때 해당 레인 키를 눌러 타이밍 오차(ms)를 측정합니다.
/// BGM 대신 짧은 비프음·UI만 사용합니다.
/// </summary>
public sealed class RhythmTestController : MonoBehaviour
{
    private const float MissPenaltyMs = 118f;

    private static readonly Key[] Keys4 = { Key.D, Key.F, Key.J, Key.K };
    private static readonly Key[] Keys3 = { Key.F, Key.G, Key.H };
    private static readonly Key[] Keys2 = { Key.D, Key.F };

    private bool _subscribed;
    private bool _pendingSessionStart;
    private Coroutine _sessionRoutine;

    private AudioSource _audio;
    private AudioClip _hitClip;
    private AudioClip _missClip;

    private Transform _playRoot;
    private Image _panelBackgroundImage;
    private bool _panelBackgroundSuppressed;
    private TextMeshProUGUI _hudText;
    private readonly List<RhythmNoteRuntime> _noteInstances = new List<RhythmNoteRuntime>();

    private float _songStartTime;
    private float _goodWindowMs;
    private float _travelSec;
    private int _laneCount;
    private bool _roundActive;
    private float _lastRoundMeanError;
    private float _lastRoundAccuracy;
    private float _lastNoteEndTime;
    private RectTransform[] _laneRects;
    private float _laneColumnHeight;

    private struct RhythmNoteData
    {
        public float Time;
        public int Lane;
    }

    private sealed class RhythmNoteRuntime
    {
        public RhythmNoteData Data;
        public bool Resolved;
        public bool Hit;
        public float AbsErrorMs;
        public RectTransform Visual;
    }

    private void Awake()
    {
        _hitClip = CreateTone(880f, 0.045f);
        _missClip = CreateTone(200f, 0.09f);
        _audio = GetComponent<AudioSource>();
        if (_audio == null)
        {
            _audio = gameObject.AddComponent<AudioSource>();
        }

        _audio.playOnAwake = false;
        _audio.volume = 0.35f;
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
        TryFlushPendingSessionStart();
        SyncWithGameStateIfNeeded();
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

    /// <summary>
    /// 패널이 비활성인 동안 OnGameStateChanged만 오고 코루틴을 못 돌린 경우 복구합니다.
    /// </summary>
    private void SyncWithGameStateIfNeeded()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (GameManager.Instance.CurrentState == GameState.TestInProgress
            && GameManager.Instance.CurrentTestMode == TestMode.RhythmTiming
            && _sessionRoutine == null
            && _playRoot == null)
        {
            _pendingSessionStart = true;
            TryFlushPendingSessionStart();
        }
    }

    private void TryFlushPendingSessionStart()
    {
        if (!_pendingSessionStart || !isActiveAndEnabled)
        {
            return;
        }

        if (GameManager.Instance == null
            || GameManager.Instance.CurrentState != GameState.TestInProgress
            || GameManager.Instance.CurrentTestMode != TestMode.RhythmTiming)
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
        if (GameManager.Instance == null || GameManager.Instance.CurrentTestMode != TestMode.RhythmTiming)
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

        _roundActive = false;
        DestroyPlayRoot();
        RestorePanelBackgroundIfNeeded();
    }

    private IEnumerator RunSessionRoutine()
    {
        bool isExam = GameManager.Instance != null
            && (GameManager.Instance.CurrentTestType == TestType.OfficialExam
                || GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam);
        int rounds = isExam ? RhythmDifficulty.GetExamRounds() : 1;
        int baseSeed = isExam ? 0x5248544D : 0x50414374;
        float sumErr = 0f;
        float sumAcc = 0f;

        for (int r = 0; r < rounds; r++)
        {
            yield return RunSingleRound(baseSeed + r * 9973, isExam);
            sumErr += _lastRoundMeanError;
            sumAcc += _lastRoundAccuracy;
        }

        float meanErr = sumErr / rounds;
        float meanAcc = sumAcc / rounds;
        bool failed = meanAcc < 22f || meanErr > 115f;

        if (GameManager.Instance != null && GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam)
        {
            GameManager.Instance.CompleteUnifiedExamSegment(new UnifiedExamSegmentPayload
            {
                Mode = TestMode.RhythmTiming,
                HardFailed = false,
                Primary = meanErr,
                Secondary = meanAcc
            });
            _sessionRoutine = null;
            yield break;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SaveRhythmResult(meanErr, meanAcc, failed);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.ResultScreen);
        }

        _sessionRoutine = null;
    }

    private IEnumerator RunSingleRound(int seed, bool isExam)
    {
        int grade = 9;
        if (GameManager.Instance != null)
        {
            grade = GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam
                ? GameManager.Instance.UnifiedExamGrade
                : GameManager.Instance.GetPracticeGrade(TestMode.RhythmTiming);
        }
        _laneCount = RhythmDifficulty.GetLaneCount(grade);
        RhythmDifficulty.GetTimingParams(grade, out float bpm, out _goodWindowMs, out _travelSec);
        int noteCount = isExam ? RhythmDifficulty.GetExamNoteCountPerRound() : RhythmDifficulty.GetPracticeNoteCount();

        var chart = new List<RhythmNoteData>();
        BuildChart(seed, noteCount, bpm, _laneCount, chart);

        _lastNoteEndTime = 0f;
        for (int i = 0; i < chart.Count; i++)
        {
            if (chart[i].Time > _lastNoteEndTime)
            {
                _lastNoteEndTime = chart[i].Time;
            }
        }

        BuildPlayArea(chart);

        if (_hudText != null)
        {
            _hudText.text = "준비…";
        }

        yield return new WaitForSecondsRealtime(1.6f);

        if (_hudText != null)
        {
            _hudText.text = "시작!";
        }

        yield return new WaitForSecondsRealtime(0.35f);

        _songStartTime = Time.time;
        _roundActive = true;

        float endTime = _lastNoteEndTime + _goodWindowMs * 0.001f + 0.85f;
        while (Time.time - _songStartTime < endTime)
        {
            TickRound();
            yield return null;
        }

        _roundActive = false;
        FinalizeMissedNotes();
        ComputeRoundStats(chart);
        DestroyPlayRoot();
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

    private static void BuildChart(int seed, int noteCount, float bpm, int lanes, List<RhythmNoteData> outList)
    {
        outList.Clear();
        var rng = new System.Random(seed);
        float spb = 60f / bpm;
        float t = Mathf.Max(0.65f, spb * 0.45f);

        for (int i = 0; i < noteCount; i++)
        {
            outList.Add(new RhythmNoteData { Time = t, Lane = rng.Next(0, lanes) });
            float step = spb * (0.28f + (float)rng.NextDouble() * 0.92f);
            t += step;
        }
    }

    private void BuildPlayArea(List<RhythmNoteData> chart)
    {
        DestroyPlayRoot();

        HideOtherTestTargetsOnPanel();
        SuppressPanelBackgroundForRhythm();

        // RectTransform은 SetParent 전에 붙이는 것이 UI 런타임 생성 권장 패턴입니다.
        var rootGo = new GameObject("RhythmPlayRoot");
        var rootRt = rootGo.AddComponent<RectTransform>();
        _playRoot = rootGo.transform;
        _playRoot.SetParent(transform, false);
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = new Vector2(24f, 72f);
        rootRt.offsetMax = new Vector2(-24f, -24f);
        rootRt.localScale = Vector3.one;

        // 동일 패널의 루트 Image가 자식 UI 위에 그려지는 경우가 있어 별도 캔버스로 위로 올립니다.
        var overlayCanvas = rootGo.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 100;

        // 배경은 루트 Image 대신 플레이 루트 안에서만 그립니다(패널 Image는 세션 중 끔).
        var bgGo = new GameObject("RhythmBackdrop");
        bgGo.transform.SetParent(_playRoot, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.09f, 0.12f, 0.97f);
        bgImg.raycastTarget = false;
        bgGo.transform.SetAsFirstSibling();

        var hudGo = new GameObject("RhythmHud");
        hudGo.transform.SetParent(_playRoot, false);
        var hudRt = hudGo.AddComponent<RectTransform>();
        hudRt.anchorMin = new Vector2(0f, 1f);
        hudRt.anchorMax = new Vector2(1f, 1f);
        hudRt.pivot = new Vector2(0.5f, 1f);
        hudRt.anchoredPosition = Vector2.zero;
        hudRt.sizeDelta = new Vector2(0f, 36f);
        _hudText = hudGo.AddComponent<TextMeshProUGUI>();
        _hudText.fontSize = 18;
        _hudText.alignment = TextAlignmentOptions.Center;
        _hudText.color = new Color(0.85f, 0.88f, 0.92f, 1f);
        TMP_FontAsset font = TmpFontCache.LiberationSansSdf;
        if (font != null)
        {
            _hudText.font = font;
        }

        var hintGo = new GameObject("KeyHint");
        hintGo.transform.SetParent(_playRoot, false);
        var hintRt = hintGo.AddComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0f, 0f);
        hintRt.anchorMax = new Vector2(1f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.anchoredPosition = new Vector2(0f, 8f);
        hintRt.sizeDelta = new Vector2(0f, 28f);
        var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
        hintTmp.fontSize = 14;
        hintTmp.alignment = TextAlignmentOptions.Center;
        hintTmp.color = new Color(0.55f, 0.58f, 0.62f, 1f);
        if (font != null)
        {
            hintTmp.font = font;
        }

        hintTmp.text = KeyHintForLanes(_laneCount);

        var lanesGo = new GameObject("Lanes");
        lanesGo.transform.SetParent(_playRoot, false);
        var lanesRt = lanesGo.AddComponent<RectTransform>();
        lanesRt.anchorMin = Vector2.zero;
        lanesRt.anchorMax = Vector2.one;
        lanesRt.offsetMin = new Vector2(0f, 40f);
        lanesRt.offsetMax = new Vector2(0f, -44f);
        var h = lanesGo.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 6f;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;

        _laneRects = new RectTransform[_laneCount];
        for (int i = 0; i < _laneCount; i++)
        {
            var col = new GameObject($"Lane_{i}");
            col.transform.SetParent(lanesGo.transform, false);
            var colRt = col.AddComponent<RectTransform>();
            var le = col.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.minWidth = 40f;
            var img = col.AddComponent<Image>();
            img.color = new Color(0.12f, 0.13f, 0.17f, 0.92f);
            img.raycastTarget = false;
            _laneRects[i] = colRt;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(lanesRt);
        if (_laneRects.Length > 0 && _laneRects[0] != null)
        {
            _laneColumnHeight = Mathf.Max(120f, _laneRects[0].rect.height);
        }
        else
        {
            _laneColumnHeight = 280f;
        }

        var judgeGo = new GameObject("JudgeLine");
        judgeGo.transform.SetParent(_playRoot, false);
        var judgeRt = judgeGo.AddComponent<RectTransform>();
        judgeRt.anchorMin = new Vector2(0f, 0.11f);
        judgeRt.anchorMax = new Vector2(1f, 0.11f);
        judgeRt.pivot = new Vector2(0.5f, 0.5f);
        judgeRt.sizeDelta = new Vector2(-32f, 4f);
        judgeRt.anchoredPosition = Vector2.zero;
        var jImg = judgeGo.AddComponent<Image>();
        jImg.color = new Color(0.45f, 0.75f, 0.95f, 0.95f);
        jImg.raycastTarget = false;
        judgeGo.transform.SetAsLastSibling();

        _playRoot.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();

        _noteInstances.Clear();
        for (int n = 0; n < chart.Count; n++)
        {
            RhythmNoteData d = chart[n];
            var run = new RhythmNoteRuntime { Data = d };
            RectTransform lane = _laneRects[d.Lane];
            var noteGo = new GameObject($"Note_{n}");
            noteGo.transform.SetParent(lane, false);
            var nRt = noteGo.AddComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0.1f, 1f);
            nRt.anchorMax = new Vector2(0.9f, 1f);
            nRt.pivot = new Vector2(0.5f, 1f);
            nRt.sizeDelta = new Vector2(0f, 14f);
            nRt.anchoredPosition = Vector2.zero;
            var nImg = noteGo.AddComponent<Image>();
            nImg.color = new Color(0.35f, 0.82f, 0.72f, 0.95f);
            nImg.raycastTarget = false;
            run.Visual = nRt;
            noteGo.SetActive(false);
            _noteInstances.Add(run);
        }
    }

    private static string KeyHintForLanes(int lanes)
    {
        switch (lanes)
        {
            case 4:
                return "키: D · F · J · K";
            case 3:
                return "키: F · G · H";
            case 2:
                return "키: D · F";
            default:
                return string.Empty;
        }
    }

    private void TickRound()
    {
        float songTime = Time.time - _songStartTime;
        Keyboard kb = Keyboard.current;
        float windowSec = _goodWindowMs * 0.001f;

        for (int i = 0; i < _noteInstances.Count; i++)
        {
            RhythmNoteRuntime r = _noteInstances[i];
            if (r.Resolved || r.Visual == null)
            {
                continue;
            }

            float appear = r.Data.Time - _travelSec;
            if (songTime < appear)
            {
                continue;
            }

            r.Visual.gameObject.SetActive(true);
            float prog = Mathf.Clamp01((songTime - appear) / _travelSec);
            float dist = _laneColumnHeight * 0.88f;
            r.Visual.anchoredPosition = new Vector2(0f, -prog * dist);
        }

        if (kb != null)
        {
            for (int lane = 0; lane < _laneCount; lane++)
            {
                Key k = GetKeyForLane(lane, _laneCount);
                if (!kb[k].wasPressedThisFrame)
                {
                    continue;
                }

                RhythmNoteRuntime best = null;
                float bestAbs = float.MaxValue;

                for (int i = 0; i < _noteInstances.Count; i++)
                {
                    RhythmNoteRuntime r = _noteInstances[i];
                    if (r.Resolved || r.Data.Lane != lane)
                    {
                        continue;
                    }

                    float ms = Mathf.Abs(songTime - r.Data.Time) * 1000f;
                    if (ms <= _goodWindowMs && ms < bestAbs)
                    {
                        bestAbs = ms;
                        best = r;
                    }
                }

                if (best != null)
                {
                    best.Resolved = true;
                    best.Hit = true;
                    best.AbsErrorMs = bestAbs;
                    PlayHitSound();
                    int laneIdx = best.Data.Lane;
                    if (best.Visual != null)
                    {
                        best.Visual.gameObject.SetActive(false);
                    }

                    GetHitJudgmentStyle(bestAbs, out string label, out Color fxColor);
                    SpawnJudgmentPopup(laneIdx, label, fxColor, new Color(1f, 1f, 1f, 0.55f));
                }
            }
        }

        for (int i = 0; i < _noteInstances.Count; i++)
        {
            RhythmNoteRuntime r = _noteInstances[i];
            if (r.Resolved)
            {
                continue;
            }

            if (songTime > r.Data.Time + windowSec + 0.008f)
            {
                r.Resolved = true;
                r.Hit = false;
                r.AbsErrorMs = MissPenaltyMs;
                PlayMissSound();
                int laneIdx = r.Data.Lane;
                if (r.Visual != null)
                {
                    r.Visual.gameObject.SetActive(false);
                }

                SpawnJudgmentPopup(laneIdx, "미스!", new Color(1f, 0.38f, 0.38f, 1f), new Color(1f, 0.2f, 0.2f, 0.5f));
            }
        }

        if (_hudText != null)
        {
            int hits = 0;
            int done = 0;
            for (int i = 0; i < _noteInstances.Count; i++)
            {
                if (_noteInstances[i].Resolved)
                {
                    done++;
                    if (_noteInstances[i].Hit)
                    {
                        hits++;
                    }
                }
            }

            _hudText.text = $"오차(ms) 기록 중…  성공 {hits}/{done}";
        }
    }

    private void FinalizeMissedNotes()
    {
        for (int i = 0; i < _noteInstances.Count; i++)
        {
            RhythmNoteRuntime r = _noteInstances[i];
            if (r.Resolved)
            {
                continue;
            }

            r.Resolved = true;
            r.Hit = false;
            r.AbsErrorMs = MissPenaltyMs;
            if (r.Visual != null)
            {
                r.Visual.gameObject.SetActive(false);
            }
        }
    }

    private void ComputeRoundStats(List<RhythmNoteData> chart)
    {
        float sum = 0f;
        int total = chart.Count;
        if (total <= 0)
        {
            _lastRoundMeanError = MissPenaltyMs;
            _lastRoundAccuracy = 0f;
            return;
        }

        int hits = 0;
        for (int i = 0; i < _noteInstances.Count; i++)
        {
            RhythmNoteRuntime r = _noteInstances[i];
            sum += r.AbsErrorMs;
            if (r.Hit)
            {
                hits++;
            }
        }

        _lastRoundMeanError = sum / total;
        _lastRoundAccuracy = hits / (float)total * 100f;
    }

    private static Key GetKeyForLane(int lane, int laneCount)
    {
        switch (laneCount)
        {
            case 4:
                return Keys4[Mathf.Clamp(lane, 0, 3)];
            case 3:
                return Keys3[Mathf.Clamp(lane, 0, 2)];
            default:
                return Keys2[Mathf.Clamp(lane, 0, 1)];
        }
    }

    private void PlayHitSound()
    {
        if (_audio != null && _hitClip != null)
        {
            _audio.PlayOneShot(_hitClip);
        }
    }

    private void PlayMissSound()
    {
        if (_audio != null && _missClip != null)
        {
            _audio.PlayOneShot(_missClip);
        }
    }

    /// <summary>판정창 대비 오차 비율로 등급 문구·색을 정합니다.</summary>
    private void GetHitJudgmentStyle(float absErrorMs, out string label, out Color color)
    {
        float denom = Mathf.Max(1f, _goodWindowMs);
        float ratio = absErrorMs / denom;
        if (ratio <= 0.22f)
        {
            label = "퍼펙트!";
            color = new Color(1f, 0.92f, 0.35f, 1f);
        }
        else if (ratio <= 0.5f)
        {
            label = "그레이트!";
            color = new Color(0.45f, 0.85f, 1f, 1f);
        }
        else
        {
            label = "굿!";
            color = new Color(0.45f, 0.95f, 0.55f, 1f);
        }
    }

    private void SpawnJudgmentPopup(int laneIndex, string text, Color color, Color laneFlashTint)
    {
        if (_laneRects == null || laneIndex < 0 || laneIndex >= _laneRects.Length)
        {
            return;
        }

        RectTransform laneRt = _laneRects[laneIndex];
        var go = new GameObject("JudgmentFx");
        go.transform.SetParent(laneRt, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(168f, 44f);
        float y = -_laneColumnHeight * 0.88f;
        rt.anchoredPosition = new Vector2(0f, y);
        rt.localScale = new Vector3(0.62f, 0.62f, 1f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 23;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        TMP_FontAsset font = TmpFontCache.LiberationSansSdf;
        if (font != null)
        {
            tmp.font = font;
        }

        go.transform.SetAsLastSibling();
        SpawnLaneHitFlash(laneRt, laneFlashTint);
        StartCoroutine(JudgmentFxRoutine(rt, tmp));
    }

    /// <summary>판정선 위 짧은 가로 플래시.</summary>
    private void SpawnLaneHitFlash(RectTransform laneRt, Color tint)
    {
        var flashGo = new GameObject("LaneHitFlash");
        flashGo.transform.SetParent(laneRt, false);
        var fr = flashGo.AddComponent<RectTransform>();
        fr.anchorMin = new Vector2(0.08f, 1f);
        fr.anchorMax = new Vector2(0.92f, 1f);
        fr.pivot = new Vector2(0.5f, 0.5f);
        fr.sizeDelta = new Vector2(0f, 10f);
        float h = laneRt.rect.height > 2f ? laneRt.rect.height : _laneColumnHeight;
        fr.anchoredPosition = new Vector2(0f, -h * 0.88f);
        var img = flashGo.AddComponent<Image>();
        img.color = tint;
        img.raycastTarget = false;
        flashGo.transform.SetAsLastSibling();
        StartCoroutine(LaneFlashRoutine(fr, img, tint.a));
    }

    private IEnumerator LaneFlashRoutine(RectTransform rt, Image img, float startAlpha)
    {
        float d = 0.14f;
        float t = 0f;
        Color c = img.color;
        while (t < d && rt != null && img != null)
        {
            t += Time.unscaledDeltaTime;
            float u = t / d;
            c.a = startAlpha * (1f - u);
            img.color = c;
            float s = 1f + 0.35f * u;
            rt.localScale = new Vector3(s, 1f, 1f);
            yield return null;
        }

        if (rt != null && rt.gameObject != null)
        {
            Destroy(rt.gameObject);
        }
    }

    private IEnumerator JudgmentFxRoutine(RectTransform rt, TextMeshProUGUI tmp)
    {
        float duration = 0.52f;
        float rise = 38f;
        Vector2 start = rt.anchoredPosition;
        float t = 0f;
        while (t < duration && rt != null && tmp != null)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            rt.anchoredPosition = start + new Vector2(0f, rise * u);
            Color c = tmp.color;
            c.a = 1f - u;
            tmp.color = c;
            float s = Mathf.Lerp(0.62f, 1.08f, Mathf.Sin(u * Mathf.PI * 0.5f));
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        if (rt != null && rt.gameObject != null)
        {
            Destroy(rt.gameObject);
        }
    }

    private void DestroyPlayRoot()
    {
        _noteInstances.Clear();
        _laneRects = null;
        if (_playRoot != null)
        {
            Destroy(_playRoot.gameObject);
            _playRoot = null;
        }

        _hudText = null;
    }

    private void SuppressPanelBackgroundForRhythm()
    {
        if (_panelBackgroundSuppressed)
        {
            return;
        }

        _panelBackgroundImage ??= GetComponent<Image>();
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

    private static AudioClip CreateTone(float frequencyHz, float durationSec)
    {
        int sampleRate = 44100;
        int samples = Mathf.Max(1, (int)(sampleRate * durationSec));
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float env = 1f - (i / (float)samples);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequencyHz * t) * 0.22f * env;
        }

        AudioClip clip = AudioClip.Create("RhythmBeep", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
