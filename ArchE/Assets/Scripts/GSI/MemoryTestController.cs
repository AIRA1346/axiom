using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 순차 공간 기억(결정적 패턴): (단계, 시드)에 따라 항상 같은 칸이 이어지며 운 요소 없음.
/// 연습은 1회 시도, 공식 시험은 3회 평균 최대 길이로 점수화합니다.
/// </summary>
public sealed class MemoryTestController : MonoBehaviour
{
    private const int GridSize = 9;
    private const int ExamTrialCount = 3;
    private const int MaxSpanPerTrial = 48;
    private const float ExamShowCellSeconds = 0.42f;
    private const float ExamBetweenShowSeconds = 0.12f;
    private const float BetweenTrialsSeconds = 0.4f;

    private float _showCellSeconds = ExamShowCellSeconds;
    private float _betweenShowSeconds = ExamBetweenShowSeconds;

    private static readonly Color DimColor = new Color(0.14f, 0.15f, 0.2f, 1f);
    private static readonly Color LitColor = new Color(0.42f, 0.58f, 0.95f, 1f);
    private static readonly Color RecallCueColor = new Color(0.32f, 0.78f, 0.52f, 1f);

    private const float RecallCueFlashSeconds = 0.14f;
    private const float RecallCueGapSeconds = 0.1f;
    private const float RecallCueAfterPauseSeconds = 0.22f;

    [SerializeField] private RectTransform _gridHost;

    /// <summary>9칸만 담는 영역(GridLayoutGroup). 안내 문구는 _gridHost에 두어 레이아웃에 끼지 않음.</summary>
    private RectTransform _cellsRoot;

    private readonly List<int> _sequence = new List<int>(48);
    private readonly List<Image> _cellImages = new List<Image>(GridSize);
    private TextMeshProUGUI _recallHintText;

    private bool _subscribedGameManager;
    private bool _sessionActive;
    private Coroutine _routine;
    private bool _acceptingRecall;
    private int? _pendingClickIndex;

    private void Awake()
    {
        EnsureGridHost();
        MigrateLegacyGridLayoutIfNeeded();
        BuildGridIfNeeded();
        SetGridVisible(false);
    }

    private void OnEnable()
    {
        SubscribeToGameManager();
        if (GameManager.Instance != null)
        {
            HandleGameStateChanged(GameManager.Instance.CurrentState);
        }
    }

    private void OnDisable()
    {
        StopSession();
        UnsubscribeFromGameManager();
        SetOtherMinigameVisuals(true);
    }

    private void SubscribeToGameManager()
    {
        if (_subscribedGameManager || GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        _subscribedGameManager = true;
    }

    private void UnsubscribeFromGameManager()
    {
        if (!_subscribedGameManager || GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        _subscribedGameManager = false;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentTestMode != TestMode.MemorySequence)
        {
            StopSession();
            SetGridVisible(false);
            ApplyNonMemoryMinigameVisuals(newState);
            return;
        }

        if (newState == GameState.TestInProgress)
        {
            StartSession();
            return;
        }

        if (newState == GameState.TestStandby || newState == GameState.MainMenu)
        {
            StopSession();
            SetGridVisible(false);
            SetOtherMinigameVisuals(true);
        }
    }

    /// <summary>
    /// 반응/에임 테스트 진행 중에는 반응 타깃을 켜지 않습니다(초록 상자가 먼저 보이는 버그 방지).
    /// </summary>
    private void ApplyNonMemoryMinigameVisuals(GameState newState)
    {
        if (newState != GameState.TestInProgress)
        {
            SetOtherMinigameVisuals(true);
            return;
        }

        if (GameManager.Instance == null)
        {
            return;
        }

        switch (GameManager.Instance.CurrentTestMode)
        {
            case TestMode.Reaction:
                SetReactionTargetVisible(false);
                SetAimAreaVisible(false);
                break;

            case TestMode.AimPrecision:
                SetReactionTargetVisible(false);
                SetAimAreaVisible(true);
                break;

            case TestMode.RhythmTiming:
                SetReactionTargetVisible(false);
                SetAimAreaVisible(false);
                break;

            case TestMode.MultipleObjectTracking:
                SetReactionTargetVisible(false);
                SetAimAreaVisible(false);
                break;

            case TestMode.BulletHell:
                SetReactionTargetVisible(false);
                SetAimAreaVisible(false);
                break;

            default:
                SetOtherMinigameVisuals(false);
                break;
        }
    }

    private static void SetReactionTargetVisible(bool visible)
    {
        Transform panels = GetTestInProgressPanel();
        if (panels == null)
        {
            return;
        }

        Transform r = panels.Find("ReactionTarget");
        if (r != null)
        {
            r.gameObject.SetActive(visible);
        }
    }

    private static void SetAimAreaVisible(bool visible)
    {
        Transform panels = GetTestInProgressPanel();
        if (panels == null)
        {
            return;
        }

        Transform a = panels.Find("AimTargetArea");
        if (a != null)
        {
            a.gameObject.SetActive(visible);
        }
    }

    private static Transform GetTestInProgressPanel()
    {
        Transform root = GameObject.Find("GSI_AutoSetup")?.transform;
        if (root == null)
        {
            return null;
        }

        return root.Find("Panels/TestInProgressPanel");
    }

    private void EnsureGridHost()
    {
        if (_gridHost != null)
        {
            return;
        }

        Transform found = transform.Find("MemoryGridHost");
        if (found != null)
        {
            _gridHost = found as RectTransform;
            return;
        }

        var go = new GameObject("MemoryGridHost");
        go.transform.SetParent(transform, false);
        _gridHost = go.AddComponent<RectTransform>();
        StretchFull(_gridHost);
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    /// <summary>
    /// 예전 구조(힌트가 GridLayoutGroup 자식)에서 칸이 밀리는 문제 수정: 칸만 MemoryGridCells로 옮깁니다.
    /// </summary>
    private void MigrateLegacyGridLayoutIfNeeded()
    {
        if (_gridHost == null)
        {
            return;
        }

        GridLayoutGroup oldGrid = _gridHost.GetComponent<GridLayoutGroup>();
        if (oldGrid == null || _gridHost.Find("MemoryGridCells") != null)
        {
            return;
        }

        var cellsGo = new GameObject("MemoryGridCells");
        cellsGo.transform.SetParent(_gridHost, false);
        _cellsRoot = cellsGo.AddComponent<RectTransform>();
        StretchFull(_cellsRoot);

        GridLayoutGroup newGrid = cellsGo.AddComponent<GridLayoutGroup>();
        newGrid.cellSize = oldGrid.cellSize;
        newGrid.spacing = oldGrid.spacing;
        newGrid.constraint = oldGrid.constraint;
        newGrid.constraintCount = oldGrid.constraintCount;
        newGrid.childAlignment = oldGrid.childAlignment;
        newGrid.padding = oldGrid.padding;

        for (int i = 0; i < GridSize; i++)
        {
            Transform cell = _gridHost.Find($"Cell_{i}");
            if (cell != null)
            {
                cell.SetParent(_cellsRoot, false);
            }
        }

        Transform hintTransform = _gridHost.Find("MemoryRecallHint");
        if (hintTransform != null)
        {
            hintTransform.SetParent(_gridHost, false);
            hintTransform.SetAsLastSibling();
            _recallHintText = hintTransform.GetComponent<TextMeshProUGUI>();
        }

        Destroy(oldGrid);

        _cellImages.Clear();
        for (int i = 0; i < GridSize; i++)
        {
            Transform cellTr = _cellsRoot.Find($"Cell_{i}");
            if (cellTr != null)
            {
                _cellImages.Add(cellTr.GetComponent<Image>());
            }
        }
    }

    private void BuildGridIfNeeded()
    {
        if (_gridHost == null || _cellImages.Count > 0)
        {
            return;
        }

        var cellsGo = new GameObject("MemoryGridCells");
        cellsGo.transform.SetParent(_gridHost, false);
        _cellsRoot = cellsGo.AddComponent<RectTransform>();
        StretchFull(_cellsRoot);

        var grid = cellsGo.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(118f, 118f);
        grid.spacing = new Vector2(14f, 14f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.padding = new RectOffset(24, 24, 88, 24);

        for (int i = 0; i < GridSize; i++)
        {
            var cell = new GameObject($"Cell_{i}");
            cell.transform.SetParent(_cellsRoot, false);
            var img = cell.AddComponent<Image>();
            img.color = DimColor;
            img.raycastTarget = true;
            var btn = cell.AddComponent<Button>();
            btn.targetGraphic = img;
            int captured = i;
            btn.onClick.AddListener(() => OnCellClicked(captured));
            _cellImages.Add(img);
        }

        EnsureRecallHintText();
    }

    private void EnsureRecallHintText()
    {
        if (_recallHintText != null || _gridHost == null)
        {
            return;
        }

        var go = new GameObject("MemoryRecallHint");
        go.transform.SetParent(_gridHost, false);
        go.transform.SetAsLastSibling();
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -4f);
        rt.sizeDelta = new Vector2(-16f, 40f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = string.Empty;
        tmp.fontSize = 19;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.82f, 0.9f, 0.96f, 1f);
        tmp.raycastTarget = false;
        TMP_FontAsset font = TmpFontCache.LiberationSansSdf;
        if (font != null)
        {
            tmp.font = font;
        }

        _recallHintText = tmp;
        go.SetActive(false);
    }

    private void SetRecallHintVisible(bool visible, string message = null)
    {
        EnsureRecallHintText();
        if (_recallHintText == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(message))
        {
            _recallHintText.text = message;
        }

        _recallHintText.gameObject.SetActive(visible);
    }

    private void OnCellClicked(int index)
    {
        if (!_acceptingRecall || !_sessionActive)
        {
            return;
        }

        _pendingClickIndex = index;
    }

    private void StartSession()
    {
        StopSession();
        SetOtherMinigameVisuals(false);
        SetGridVisible(true);
        _sessionActive = true;
        if (GameManager.Instance != null && GameManager.Instance.CurrentTestType == TestType.Practice)
        {
            PracticeDifficulty.GetMemoryTiming(
                GameManager.Instance.GetPracticeGrade(TestMode.MemorySequence),
                out _showCellSeconds,
                out _betweenShowSeconds);
        }
        else
        {
            _showCellSeconds = ExamShowCellSeconds;
            _betweenShowSeconds = ExamBetweenShowSeconds;
        }

        _routine = StartCoroutine(RunSessionRoutine());
    }

    private void StopSession()
    {
        _sessionActive = false;
        _acceptingRecall = false;
        _pendingClickIndex = null;
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        foreach (Image img in _cellImages)
        {
            if (img != null)
            {
                img.color = DimColor;
            }
        }

        SetRecallHintVisible(false);
    }

    private void SetGridVisible(bool visible)
    {
        if (_gridHost != null)
        {
            _gridHost.gameObject.SetActive(visible);
        }
    }

    private static void SetOtherMinigameVisuals(bool visible)
    {
        Transform panels = GetTestInProgressPanel();
        if (panels == null)
        {
            return;
        }

        Transform r = panels.Find("ReactionTarget");
        if (r != null)
        {
            r.gameObject.SetActive(visible);
        }

        Transform a = panels.Find("AimTargetArea");
        if (a != null)
        {
            a.gameObject.SetActive(visible);
        }
    }

    private IEnumerator RunSessionRoutine()
    {
        bool isExam = GameManager.Instance != null
            && (GameManager.Instance.CurrentTestType == TestType.OfficialExam
                || GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam);
        int baseSeed = isExam ? 0x4558414D : 0x50414374;
        int trials = isExam ? ExamTrialCount : 1;
        float sum = 0f;

        for (int t = 0; t < trials; t++)
        {
            int trialSeed = baseSeed + t * 9973;
            float trialScore = 0f;
            yield return RunSingleTrialCoroutine(trialSeed, v => trialScore = v);
            sum += trialScore;
            if (t < trials - 1)
            {
                yield return new WaitForSeconds(BetweenTrialsSeconds);
            }
        }

        float finalScore = sum / trials;
        if (GameManager.Instance != null && GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam)
        {
            GameManager.Instance.CompleteUnifiedExamSegment(new UnifiedExamSegmentPayload
            {
                Mode = TestMode.MemorySequence,
                HardFailed = false,
                Primary = finalScore
            });
            _routine = null;
            yield break;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SaveMemoryScore(finalScore, false);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.ResultScreen);
        }
    }

    private IEnumerator RunSingleTrialCoroutine(int sessionSeed, System.Action<float> onComplete)
    {
        _sequence.Clear();

        while (_sessionActive)
        {
            _sequence.Add(DeterministicCellIndex(_sequence.Count, sessionSeed));
            yield return PlayShowPhaseCoroutine();
            if (!_sessionActive)
            {
                onComplete?.Invoke(0f);
                yield break;
            }

            _acceptingRecall = true;
            bool failed = false;

            for (int i = 0; i < _sequence.Count && _sessionActive; i++)
            {
                _pendingClickIndex = null;
                while (_pendingClickIndex == null && _sessionActive)
                {
                    yield return null;
                }

                if (!_sessionActive)
                {
                    onComplete?.Invoke(0f);
                    yield break;
                }

                int clicked = _pendingClickIndex.Value;
                _pendingClickIndex = null;

                if (clicked != _sequence[i])
                {
                    failed = true;
                    break;
                }
            }

            _acceptingRecall = false;

            if (failed)
            {
                onComplete?.Invoke(Mathf.Max(0, _sequence.Count - 1));
                yield break;
            }

            if (_sequence.Count >= MaxSpanPerTrial)
            {
                onComplete?.Invoke(MaxSpanPerTrial);
                yield break;
            }

            // 이번 길이까지 완전 재현 성공 → 한 칸 더 늘려 반복
        }

        onComplete?.Invoke(0f);
    }

    private IEnumerator PlayShowPhaseCoroutine()
    {
        _acceptingRecall = false;
        SetRecallHintVisible(false);
        foreach (int cellIndex in _sequence)
        {
            if (!_sessionActive)
            {
                yield break;
            }

            if (cellIndex >= 0 && cellIndex < _cellImages.Count)
            {
                _cellImages[cellIndex].color = LitColor;
            }

            yield return new WaitForSeconds(_showCellSeconds);

            if (cellIndex >= 0 && cellIndex < _cellImages.Count)
            {
                _cellImages[cellIndex].color = DimColor;
            }

            yield return new WaitForSeconds(_betweenShowSeconds);
        }

        yield return RecallPhaseEndCueCoroutine();
        SetRecallHintVisible(true, "순서 표시 끝 · 보여준 대로 누르세요");
    }

    private IEnumerator RecallPhaseEndCueCoroutine()
    {
        if (!_sessionActive)
        {
            yield break;
        }

        for (int i = 0; i < _cellImages.Count; i++)
        {
            _cellImages[i].color = RecallCueColor;
        }

        yield return new WaitForSeconds(RecallCueFlashSeconds);

        if (!_sessionActive)
        {
            yield break;
        }

        for (int i = 0; i < _cellImages.Count; i++)
        {
            _cellImages[i].color = DimColor;
        }

        yield return new WaitForSeconds(RecallCueGapSeconds);

        if (!_sessionActive)
        {
            yield break;
        }

        for (int i = 0; i < _cellImages.Count; i++)
        {
            _cellImages[i].color = RecallCueColor;
        }

        yield return new WaitForSeconds(RecallCueFlashSeconds * 0.65f);

        if (!_sessionActive)
        {
            yield break;
        }

        for (int i = 0; i < _cellImages.Count; i++)
        {
            _cellImages[i].color = DimColor;
        }

        yield return new WaitForSeconds(RecallCueAfterPauseSeconds);
    }

    private static int DeterministicCellIndex(int step, int sessionSeed)
    {
        unchecked
        {
            uint x = (uint)(sessionSeed + step * 0x9E3779B9);
            x ^= x >> 16;
            x *= 0x7FEB352DU;
            x ^= x >> 15;
            x *= 0x846CA68BU;
            x ^= x >> 16;
            return (int)(x % 9u);
        }
    }
}
