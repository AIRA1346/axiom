using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// G.S.I 공식 시험 흐름에서 사용되는 런타임 상태를 정의합니다.
/// </summary>
public enum TestMode
{
    Reaction,
    AimPrecision,
    MemorySequence,
    RhythmTiming,
    // 다중 추적(MOT): 핵심 표식 유지력
    MultipleObjectTracking,
    /// <summary>Bullet Hell — danmaku dodge (survival).</summary>
    BulletHell
}

public enum TestType
{
    Practice,
    OfficialExam,
    /// <summary>6과목 통합 공식 시험(순서 랜덤, 응시권 1회, 최종 보상 1회).</summary>
    UnifiedOfficialExam
}

public enum GameState
{
    MainMenu,
    TestStandby,
    TestInProgress,
    TestCompleted,
    /// <summary>통합 시험: 다음 과목 안내(계속 → 대기).</summary>
    UnifiedExamInterstitial,
    ResultScreen,
    /// <summary>시험 직전: 모드 설명 후 시작 버튼으로만 진행. (열거형 끝에 둬 기존 직렬화 정수와 충돌을 피함.)</summary>
    TestBriefing
}

/// <summary>
/// G.S.I 시험 흐름의 중앙 상태 제어자.
/// 상태 전환만 담당하며 변경 시 브로드캐스트합니다.
/// </summary>
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// Invoked whenever the current game state changes.
    /// Other systems can subscribe to react without tight coupling.
    /// </summary>
    public event Action<GameState> OnGameStateChanged;

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public TestMode CurrentTestMode { get; private set; } = TestMode.Reaction;
    public TestType CurrentTestType { get; private set; } = TestType.Practice;

    /// <summary>통합 공식 시험에서 현재 진행 블록(1~6). 통합이 아니면 0.</summary>
    public int UnifiedExamSegmentOrdinalDisplay
    {
        get
        {
            if (CurrentTestType != TestType.UnifiedOfficialExam || _unifiedOrder == null)
            {
                return 0;
            }

            return Mathf.Clamp(_unifiedNextIndex + 1, 1, 6);
        }
    }

    /// <summary>통합 공식 시험에 선택된 시험 급수(1=최상).</summary>
    public int UnifiedExamGrade { get; private set; } = 9;

    /// <summary>직전에 시작·재시도에 사용된 통합 시험 급수(로비에서 재응시 시 참고).</summary>
    public int LastUnifiedExamGradeUsed { get; private set; } = 9;

    private TestMode[] _unifiedOrder;
    private UnifiedExamSegmentRecord[] _unifiedRecords;
    private int _unifiedNextIndex;
    private bool _unifiedExamFinalized;

    public IReadOnlyList<UnifiedExamSegmentRecord> UnifiedExamRecords =>
        _unifiedRecords != null ? _unifiedRecords : Array.Empty<UnifiedExamSegmentRecord>();

    private const string PracticeGradePrefsKey = "GSI_PracticeGrade";
    private const string PracticeGradeReactionKey = "GSI_PracticeGrade_Reaction";
    private const string PracticeGradeAimKey = "GSI_PracticeGrade_Aim";
    private const string PracticeGradeMemoryKey = "GSI_PracticeGrade_Memory";
    private const string PracticeGradeRhythmKey = "GSI_PracticeGrade_Rhythm";
    private const string PracticeGradeMotKey = "GSI_PracticeGrade_MOT";
    private const string PracticeGradeBulletHellKey = "GSI_PracticeGrade_BulletHell";

    /// <summary>연습 모드 전용(레거시). 반응 연습 급수와 동일합니다.</summary>
    public int PracticeGrade => _practiceGradeReaction;

    private int _practiceGradeReaction = 9;
    private int _practiceGradeAim = 9;
    private int _practiceGradeMemory = 9;
    private int _practiceGradeRhythm = 9;
    private int _practiceGradeMot = 9;
    private int _practiceGradeBulletHell = 9;
    private bool _isInputSubscribed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{gameObject.name}의 중복된 매니저 파괴됨.");
#endif
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SteamworksService.InitializePlaceholder();
        LoadPracticeGrade();
        CosmeticTheme.ApplyFromSave();
    }

    private void LoadPracticeGrade()
    {
        int legacy = Mathf.Clamp(PlayerPrefs.GetInt(PracticeGradePrefsKey, 9), 1, 9);

        if (!PlayerPrefs.HasKey(PracticeGradeReactionKey))
        {
            _practiceGradeReaction = legacy;
            _practiceGradeAim = legacy;
            _practiceGradeMemory = legacy;
            _practiceGradeRhythm = legacy;
            _practiceGradeMot = legacy;
            _practiceGradeBulletHell = legacy;
            SavePracticeGrades();
            return;
        }

        _practiceGradeReaction = Mathf.Clamp(PlayerPrefs.GetInt(PracticeGradeReactionKey, 9), 1, 9);
        _practiceGradeAim = Mathf.Clamp(PlayerPrefs.GetInt(PracticeGradeAimKey, 9), 1, 9);
        _practiceGradeMemory = Mathf.Clamp(PlayerPrefs.GetInt(PracticeGradeMemoryKey, 9), 1, 9);
        if (!PlayerPrefs.HasKey(PracticeGradeRhythmKey))
        {
            _practiceGradeRhythm = Mathf.Clamp(PlayerPrefs.GetInt(PracticeGradePrefsKey, 9), 1, 9);
        }
        else
        {
            _practiceGradeRhythm = Mathf.Clamp(PlayerPrefs.GetInt(PracticeGradeRhythmKey, 9), 1, 9);
        }

        if (!PlayerPrefs.HasKey(PracticeGradeMotKey))
        {
            _practiceGradeMot = Mathf.Clamp(PlayerPrefs.GetInt(PracticeGradePrefsKey, 9), 1, 9);
            SavePracticeGrades();
        }
        else
        {
            _practiceGradeMot = Mathf.Clamp(PlayerPrefs.GetInt(PracticeGradeMotKey, 9), 1, 9);
        }

        if (!PlayerPrefs.HasKey(PracticeGradeBulletHellKey))
        {
            _practiceGradeBulletHell = Mathf.Clamp(PlayerPrefs.GetInt(PracticeGradePrefsKey, 9), 1, 9);
            SavePracticeGrades();
        }
        else
        {
            _practiceGradeBulletHell = Mathf.Clamp(PlayerPrefs.GetInt(PracticeGradeBulletHellKey, 9), 1, 9);
        }
    }

    /// <summary>연습 모드별 급수(1급이 가장 어렵고 9급이 가장 쉬움).</summary>
    public int GetPracticeGrade(TestMode mode)
    {
        switch (mode)
        {
            case TestMode.Reaction:
                return _practiceGradeReaction;
            case TestMode.AimPrecision:
                return _practiceGradeAim;
            case TestMode.MemorySequence:
                return _practiceGradeMemory;
            case TestMode.RhythmTiming:
                return _practiceGradeRhythm;
            case TestMode.MultipleObjectTracking:
                return _practiceGradeMot;
            case TestMode.BulletHell:
                return _practiceGradeBulletHell;
            default:
                return 9;
        }
    }

    /// <summary>연습 급수 변경(로비 UI). 보상·공식 시험에는 영향 없음.</summary>
    public void SetPracticeGrade(TestMode mode, int grade)
    {
        int g = Mathf.Clamp(grade, 1, 9);
        switch (mode)
        {
            case TestMode.Reaction:
                _practiceGradeReaction = g;
                break;
            case TestMode.AimPrecision:
                _practiceGradeAim = g;
                break;
            case TestMode.MemorySequence:
                _practiceGradeMemory = g;
                break;
            case TestMode.RhythmTiming:
                _practiceGradeRhythm = g;
                break;
            case TestMode.MultipleObjectTracking:
                _practiceGradeMot = g;
                break;
            case TestMode.BulletHell:
                _practiceGradeBulletHell = g;
                break;
            default:
                return;
        }

        SavePracticeGrades();
    }

    private void SavePracticeGrades()
    {
        PlayerPrefs.SetInt(PracticeGradeReactionKey, _practiceGradeReaction);
        PlayerPrefs.SetInt(PracticeGradeAimKey, _practiceGradeAim);
        PlayerPrefs.SetInt(PracticeGradeMemoryKey, _practiceGradeMemory);
        PlayerPrefs.SetInt(PracticeGradeRhythmKey, _practiceGradeRhythm);
        PlayerPrefs.SetInt(PracticeGradeMotKey, _practiceGradeMot);
        PlayerPrefs.SetInt(PracticeGradeBulletHellKey, _practiceGradeBulletHell);
        PlayerPrefs.SetInt(PracticeGradePrefsKey, _practiceGradeReaction);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        // Lobby 등 GSI 씬이 아닐 때도 입력·터치 피드백 싱글톤이 붙도록 보강(GSIScene 전용 Ensure에만 의존하지 않음).
        GsiCoreServices.Ensure();
        SubscribeToInputManager();
        StartCoroutine(EnsureLocalizationBootCoroutine());
    }

    private IEnumerator EnsureLocalizationBootCoroutine()
    {
        Task boot = GameLocalization.InitializeAndApplySavedLocaleAsync();
        while (!boot.IsCompleted)
        {
            yield return null;
        }

        if (boot.IsFaulted && boot.Exception != null)
        {
            Debug.LogException(boot.Exception);
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromInputManager();
    }

    private void OnDestroy()
    {
        UnsubscribeFromInputManager();

        if (Instance == this)
        {
            SteamworksService.ShutdownPlaceholder();
            Instance = null;
        }
    }

    /// <summary>
    /// Updates the current state and notifies listeners only when a real change occurs.
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (newState == GameState.MainMenu && CurrentTestType == TestType.UnifiedOfficialExam)
        {
            if (_unifiedExamFinalized)
            {
                CleanupUnifiedExamAfterResult();
            }
            else if (_unifiedOrder != null)
            {
                AbandonUnifiedExamSession();
            }
        }

        if (CurrentState == newState)
        {
            return;
        }

        CurrentState = newState;
        OnGameStateChanged?.Invoke(CurrentState);
    }

    /// <summary>응시권 1장을 소모하고 통합 공식 시험을 시작합니다. 실패 시 false.</summary>
    public bool TryStartUnifiedExam(int examGrade1to9)
    {
        if (EconomyManager.Instance == null)
        {
            return false;
        }

        if (!EconomyManager.Instance.UseTicket())
        {
            return false;
        }

        int g = Mathf.Clamp(examGrade1to9, 1, 9);
        UnifiedExamGrade = g;
        LastUnifiedExamGradeUsed = g;
        _unifiedExamFinalized = false;
        _unifiedNextIndex = 0;
        _unifiedOrder = new[]
        {
            TestMode.Reaction,
            TestMode.AimPrecision,
            TestMode.MemorySequence,
            TestMode.RhythmTiming,
            TestMode.MultipleObjectTracking,
            TestMode.BulletHell
        };
        ShuffleModes(_unifiedOrder);
        _unifiedRecords = new UnifiedExamSegmentRecord[6];
        CurrentTestType = TestType.UnifiedOfficialExam;
        CurrentTestMode = _unifiedOrder[0];
        SetGameState(GameState.TestBriefing);
        return true;
    }

    /// <summary>결과 화면에서 동일 급수로 재응시(응시권 1장).</summary>
    public bool TryRetryUnifiedExamWithLastGrade()
    {
        return TryStartUnifiedExam(LastUnifiedExamGradeUsed);
    }

    /// <summary>통합 시험 대기 화면 안내 문구(다음 과목).</summary>
    public string GetUnifiedExamInterstitialText()
    {
        if (_unifiedOrder == null || _unifiedNextIndex >= 6 || _unifiedNextIndex < 0)
        {
            return string.Empty;
        }

        TestMode next = _unifiedOrder[_unifiedNextIndex];
        return GameLocalization.FormatUiString(UiStringKeys.GsiInterstitialBodyFmt, "{0}/6, next subject: {1}",
            _unifiedNextIndex + 1, GetUnifiedModeLabel(next));
    }

    private static string GetUnifiedModeLabel(TestMode m)
    {
        switch (m)
        {
            case TestMode.Reaction:
                return GameLocalization.GetUiString(UiStringKeys.ModeReaction, "Reaction");
            case TestMode.AimPrecision:
                return GameLocalization.GetUiString(UiStringKeys.ModeAim, "Aim");
            case TestMode.MemorySequence:
                return GameLocalization.GetUiString(UiStringKeys.ModeMemory, "Memory");
            case TestMode.RhythmTiming:
                return GameLocalization.GetUiString(UiStringKeys.ModeRhythm, "Rhythm");
            case TestMode.MultipleObjectTracking:
                return GameLocalization.GetUiString(UiStringKeys.ModeMot, "Multiple object tracking (MOT)");
            case TestMode.BulletHell:
                return GameLocalization.GetUiString(UiStringKeys.ModeBulletHell, "Bullet Hell");
            default:
                return m.ToString();
        }
    }

    /// <summary>한 과목 종료 시 호출. 통합 시험이 아니면 무시합니다.</summary>
    public void CompleteUnifiedExamSegment(UnifiedExamSegmentPayload payload)
    {
        if (CurrentTestType != TestType.UnifiedOfficialExam || _unifiedOrder == null)
        {
            return;
        }

        int idx = _unifiedNextIndex;
        if (idx < 0 || idx >= 6)
        {
            return;
        }

        UnifiedExamScoring.Evaluate(UnifiedExamGrade, payload, out float score, out bool passed, out string line);
        _unifiedRecords[idx] = new UnifiedExamSegmentRecord
        {
            Mode = payload.Mode,
            Score0To100 = score,
            Passed = passed,
            HardFailed = payload.HardFailed,
            SummaryLine = line,
            Payload = payload
        };

        _unifiedNextIndex++;
        if (_unifiedNextIndex < 6)
        {
            CurrentTestMode = _unifiedOrder[_unifiedNextIndex];
            SetGameState(GameState.UnifiedExamInterstitial);
        }
        else
        {
            FinalizeUnifiedExam();
        }
    }

    private void FinalizeUnifiedExam()
    {
        float total = 0f;
        bool anySegmentBest = false;
        for (int i = 0; i < 6; i++)
        {
            if (_unifiedRecords[i] != null)
            {
                total += _unifiedRecords[i].Score0To100;
            }
        }

        float minTotal = UnifiedExamScoring.OverallPassMinTotalScore(UnifiedExamGrade);
        bool overallPass = total + 0.001f >= minTotal;
        float avg = total / 6f;
        string rewardTier = overallPass ? UnifiedExamScoring.TierLetterFromAverage(avg) : "F";

        if (PlayerDataManager.Instance != null)
        {
            for (int i = 0; i < 6; i++)
            {
                if (_unifiedRecords[i] != null)
                {
                    anySegmentBest |= PlayerDataManager.Instance.ApplyBestFromUnifiedPayload(_unifiedRecords[i].Payload);
                }
            }
        }

        bool newBestTotal = PlayerDataManager.Instance != null
            && PlayerDataManager.Instance.CheckAndSaveBestUnifiedExamTotal(total);

        string completedAt = UnifiedExamHistoryStorage.FormatCompletedAtNow();
        string logLine = BuildUnifiedExamLogLine(total, overallPass, minTotal, completedAt);
        PlayerDataManager.Instance?.AppendUnifiedExamHistoryLine(logLine);
        PlayerDataManager.Instance?.RecordUnifiedExamSession(
            UnifiedExamGrade,
            total,
            minTotal,
            overallPass,
            rewardTier,
            CollectUnifiedSegmentSummaryLines(),
            completedAt);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ApplyUnifiedExamFinalResult(
                total,
                overallPass,
                UnifiedExamGrade,
                rewardTier,
                newBestTotal || anySegmentBest,
                completedAt);
        }

        VersusAsyncBridge.NotifyLocalResultReady(
            VersusAsyncBridge.CreateUnifiedExamPayload(
                UnifiedExamGrade,
                total,
                overallPass,
                rewardTier,
                completedAt));

        _unifiedExamFinalized = true;
        SetGameState(GameState.ResultScreen);
    }

    private string BuildUnifiedExamLogLine(float total, bool overallPass, float minTotal, string completedAt)
    {
        var sb = new StringBuilder();
        sb.Append(completedAt);
        sb.Append(" | ");
        sb.Append("G");
        sb.Append(UnifiedExamGrade);
        sb.Append(" | Total ");
        sb.Append(total.ToString("F1"));
        sb.Append("/600 | cutoff ");
        sb.Append(minTotal.ToString("F0"));
        sb.Append(" | ");
        sb.Append(overallPass
            ? GameLocalization.GetUiString(UiStringKeys.ResultUnifiedFinalPass, "Final pass")
            : GameLocalization.GetUiString(UiStringKeys.ResultUnifiedFinalFail, "Final fail"));
        sb.Append(" | ");
        for (int i = 0; i < 6; i++)
        {
            if (_unifiedRecords[i] != null)
            {
                sb.Append(_unifiedRecords[i].SummaryLine);
                if (i < 5)
                {
                    sb.Append(" | ");
                }
            }
        }

        return sb.ToString();
    }

    private string[] CollectUnifiedSegmentSummaryLines()
    {
        var lines = new string[6];
        for (int i = 0; i < 6; i++)
        {
            lines[i] = _unifiedRecords[i] != null ? _unifiedRecords[i].SummaryLine : string.Empty;
        }

        return lines;
    }

    private void AbandonUnifiedExamSession()
    {
        _unifiedOrder = null;
        _unifiedRecords = null;
        _unifiedNextIndex = 0;
        _unifiedExamFinalized = false;
        CurrentTestType = TestType.Practice;
    }

    /// <summary>G.S.I 씬을 떠날 때(ARCHE 복귀 등): 미완료면 세션 폐기, 결과 표시 직후였으면 정리만 합니다.</summary>
    public void LeaveGsiUnifiedExamContext()
    {
        if (CurrentTestType != TestType.UnifiedOfficialExam)
        {
            return;
        }

        if (_unifiedExamFinalized)
        {
            CleanupUnifiedExamAfterResult();
        }
        else
        {
            AbandonUnifiedExamSession();
        }
    }

    private void CleanupUnifiedExamAfterResult()
    {
        _unifiedOrder = null;
        _unifiedRecords = null;
        _unifiedNextIndex = 0;
        _unifiedExamFinalized = false;
        CurrentTestType = TestType.Practice;
    }

    private static void ShuffleModes(TestMode[] order)
    {
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
    }

    private void OnApplicationQuit()
    {
        if (CurrentTestType == TestType.UnifiedOfficialExam && !_unifiedExamFinalized)
        {
            AbandonUnifiedExamSession();
        }
    }

    /// <summary>
    /// Selects which test controller should respond during the next test flow.
    /// </summary>
    public void SetTestMode(TestMode mode)
    {
        CurrentTestMode = mode;
    }

    /// <summary>
    /// Selects whether the upcoming run is a practice session or an official exam.
    /// </summary>
    public void SetTestType(TestType type)
    {
        CurrentTestType = type;
    }

    /// <summary>
    /// Subscribes to the unified input stream that can start the active test.
    /// </summary>
    private void SubscribeToInputManager()
    {
        if (_isInputSubscribed)
        {
            return;
        }

        if (InputManager.Instance == null)
        {
            Debug.LogError("GameManager: 치명적 오류 - InputManager 인스턴스가 씬에 없습니다! GameObject에 붙어있는지 확인하세요.");
            return;
        }

        InputManager.Instance.OnInputDown += HandleInputDown;
        _isInputSubscribed = true;
#if UNITY_EDITOR
        Debug.Log("GameManager: InputManager 이벤트 구독 완료.");
#endif
    }

    /// <summary>
    /// Cleans up the input subscription when this manager is disabled or destroyed.
    /// </summary>
    private void UnsubscribeFromInputManager()
    {
        if (!_isInputSubscribed || InputManager.Instance == null)
        {
            return;
        }

        InputManager.Instance.OnInputDown -= HandleInputDown;
        _isInputSubscribed = false;
    }

    /// <summary>
    /// Advances the official test flow only when input is valid for the current state.
    /// </summary>
    private void HandleInputDown(Vector2 screenPosition)
    {
#if UNITY_EDITOR
        Debug.Log($"GameManager: HandleInputDown 진입 성공! 현재 상태: {CurrentState}");
#endif
        switch (CurrentState)
        {
            case GameState.MainMenu:
                break;

            case GameState.TestBriefing:
                break;

            case GameState.TestStandby:
                SetGameState(GameState.TestInProgress);
#if UNITY_EDITOR
                Debug.Log("G.S.I: Test Started.");
#endif
                break;

            case GameState.UnifiedExamInterstitial:
                break;

            case GameState.TestInProgress:
            case GameState.TestCompleted:
            case GameState.ResultScreen:
                break;
        }
    }
}
