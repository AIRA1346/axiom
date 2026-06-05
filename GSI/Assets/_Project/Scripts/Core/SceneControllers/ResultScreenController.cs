using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles result screen button clicks and requests state changes.
/// This component only bridges UI button input to the central game flow.
/// </summary>
public sealed class ResultScreenController : MonoBehaviour
{
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _mainMenuButton;
    [SerializeField] private TextMeshProUGUI _resultTitleText;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private TextMeshProUGUI _rewardText;
    [SerializeField] private GameObject _newRecordIndicator;

    private bool _localeSubscribed;

    private void OnEnable()
    {
        ResolveButtonsIfNeeded();
        BindButtonListeners();
        TrySubscribeLocaleChanged();
        ApplyResultActionButtonLabels();
        ApplyNewRecordBadgeText();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.ResultScreen)
        {
            UpdateResultText();
        }
    }

    private void OnDisable()
    {
        TryUnsubscribeLocaleChanged();
        UnbindButtonListeners();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void TrySubscribeLocaleChanged()
    {
        if (_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged += OnResultLocaleChanged;
        _localeSubscribed = true;
    }

    private void TryUnsubscribeLocaleChanged()
    {
        if (!_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged -= OnResultLocaleChanged;
        _localeSubscribed = false;
    }

    private void OnResultLocaleChanged()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.ResultScreen)
        {
            UpdateResultText();
        }
        else
        {
            ApplyResultActionButtonLabels();
            ApplyNewRecordBadgeText();
        }
    }

    private void ApplyNewRecordBadgeText()
    {
        if (_newRecordIndicator == null)
        {
            return;
        }

        TextMeshProUGUI badgeTmp = _newRecordIndicator.GetComponent<TextMeshProUGUI>();
        if (badgeTmp == null)
        {
            badgeTmp = _newRecordIndicator.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (badgeTmp == null)
        {
            return;
        }

        badgeTmp.text = GameLocalization.GetUiString(UiStringKeys.ResultBadgeNewRecord, "NEW RECORD");
    }

    private void ApplyResultActionButtonLabels()
    {
        ApplyLocalizedLabelOnButton(_retryButton, UiStringKeys.ResultActionRetry, "Retry");
        ApplyLocalizedLabelOnButton(_mainMenuButton, UiStringKeys.ResultActionMainMenu, "Main menu");
    }

    private static void ApplyLocalizedLabelOnButton(Button btn, string localizationKey, string englishFallback)
    {
        if (btn == null)
        {
            return;
        }

        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null)
        {
            return;
        }

        tmp.text = GameLocalization.GetUiString(localizationKey, englishFallback);
    }

    /// <summary>
    /// Resolves buttons when references are missing (inactive panels / broken serialization).
    /// </summary>
    private void ResolveButtonsIfNeeded()
    {
        if (_retryButton != null && _mainMenuButton != null)
        {
            EnsureButtonGraphicRaycasts(_retryButton);
            EnsureButtonGraphicRaycasts(_mainMenuButton);
            return;
        }

        foreach (var btn in GetComponentsInChildren<Button>(true))
        {
            if (_retryButton == null && btn.name == "Retry")
            {
                _retryButton = btn;
            }
            else if (_mainMenuButton == null && btn.name == "MainMenu")
            {
                _mainMenuButton = btn;
            }
        }

        EnsureButtonGraphicRaycasts(_retryButton);
        EnsureButtonGraphicRaycasts(_mainMenuButton);
    }

    /// <summary>
    /// ?ㅽ봽?쇱씠?멸? ?녿뒗 Image???쇰? ?섍꼍?먯꽌 踰꾪듉 ?덉씠罹먯뒪?멸? ?ㅽ뙣?????덉뼱 湲곕낯 UI ?ㅽ봽?쇱씠?몃? ?ｌ뒿?덈떎.
    /// </summary>
    private static void EnsureButtonGraphicRaycasts(Button btn)
    {
        if (btn == null)
        {
            return;
        }

        if (btn.targetGraphic is not Image img)
        {
            return;
        }

        img.raycastTarget = true;
        GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(img);
    }

    private void BindButtonListeners()
    {
        if (_retryButton != null)
        {
            _retryButton.onClick.RemoveListener(OnRetryClicked);
            _retryButton.onClick.AddListener(OnRetryClicked);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    private void UnbindButtonListeners()
    {
        if (_retryButton != null)
        {
            _retryButton.onClick.RemoveListener(OnRetryClicked);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        }
    }

    /// <summary>
    /// Requests an immediate return to the test standby state.
    /// </summary>
    private void OnRetryClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (GameManager.Instance.CurrentTestType == TestType.Practice)
        {
            GameManager.Instance.SetGameState(GameState.TestBriefing);
            return;
        }

        if (GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam)
        {
            if (GameManager.Instance.TryRetryUnifiedExamWithLastGrade())
            {
                return;
            }

#if UNITY_EDITOR
            Debug.Log("Not enough exam tickets to retry unified exam.");
#endif
            GameManager.Instance.SetGameState(GameState.MainMenu);
            return;
        }

        if (EconomyManager.Instance != null && EconomyManager.Instance.UseTicket())
        {
            GameManager.Instance.SetGameState(GameState.TestBriefing);
            return;
        }

#if UNITY_EDITOR
        Debug.Log("Not enough tickets to retry.");
#endif
        GameManager.Instance.SetGameState(GameState.MainMenu);
    }

    /// <summary>
    /// G.S.I ?ъ뿉?쒕뒗 G.S.I 濡쒕퉬(GameState.MainMenu)濡쒕쭔 蹂듦??⑸땲??
    /// (?ㅻⅨ ???덈툕?먯꽌???숈씪?섍쾶 硫붿씤 硫붾돱 ?곹깭濡??뚯븘媛묐땲??)
    /// </summary>
    private void OnMainMenuClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetGameState(GameState.MainMenu);
    }

    /// <summary>
    /// Updates the result UI only when the official flow enters the result state.
    /// </summary>
    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.ResultScreen)
        {
            UpdateResultText();
        }
    }

    private static string GetLocalizedPracticeResultTitle(TestMode mode)
    {
        switch (mode)
        {
            case TestMode.Reaction:
                return GameLocalization.GetUiString(UiStringKeys.ResultTitlePracticeReaction, "REACTION PRACTICE");
            case TestMode.AimPrecision:
                return GameLocalization.GetUiString(UiStringKeys.ResultTitlePracticeAim, "AIM PRACTICE");
            case TestMode.MemorySequence:
                return GameLocalization.GetUiString(UiStringKeys.ResultTitlePracticeMemory, "MEMORY PRACTICE");
            case TestMode.RhythmTiming:
                return GameLocalization.GetUiString(UiStringKeys.ResultTitlePracticeRhythm, "RHYTHM PRACTICE");
            case TestMode.MultipleObjectTracking:
                return GameLocalization.GetUiString(UiStringKeys.ResultTitlePracticeMot, "MOT PRACTICE");
            case TestMode.BulletHell:
                return GameLocalization.GetUiString(UiStringKeys.ResultTitlePracticeBulletHell, "BULLET HELL PRACTICE");
            case TestMode.ClicksPerSecond:
                return GameLocalization.GetUiString(UiStringKeys.ResultTitlePracticeCps, "CPS PRACTICE");
            default:
                return string.Empty;
        }
    }

    private static string GetLocalizedOfficialExamResultTitle(TestMode mode)
    {
        switch (mode)
        {
            case TestMode.Reaction:
                return GameLocalization.GetUiString(UiStringKeys.ResultTitleExamReaction, "OFFICIAL REACTION EXAM");
            case TestMode.AimPrecision:
                return GameLocalization.GetUiString(UiStringKeys.ResultTitleExamAim, "OFFICIAL AIM EXAM");
            case TestMode.MemorySequence:
                return GameLocalization.GetUiString(UiStringKeys.ResultTitleExamMemory, "OFFICIAL MEMORY EXAM");
            case TestMode.RhythmTiming:
                return GameLocalization.GetUiString(UiStringKeys.ResultTitleExamRhythm, "OFFICIAL RHYTHM EXAM");
            case TestMode.MultipleObjectTracking:
                return GameLocalization.GetUiString(UiStringKeys.ResultTitleExamMot, "OFFICIAL MOT EXAM");
            case TestMode.BulletHell:
                return GameLocalization.GetUiString(UiStringKeys.ResultTitleExamBulletHell, "OFFICIAL BULLET HELL EXAM");
            case TestMode.ClicksPerSecond:
                return GameLocalization.GetUiString(UiStringKeys.ResultTitleExamCps, "OFFICIAL CPS EXAM");
            default:
                return string.Empty;
        }
    }

    /// <summary>
    /// Updates the official result display using the latest stored score data.
    /// </summary>
    private void UpdateResultText()
    {
        if (_resultText == null || ScoreManager.Instance == null)
        {
            return;
        }

        if (_resultTitleText != null && GameManager.Instance != null)
        {
            switch (GameManager.Instance.CurrentTestType)
            {
                case TestType.UnifiedOfficialExam:
                    _resultTitleText.text =
                        GameLocalization.GetUiString(UiStringKeys.ResultTitleUnified, "Unified exam results");
                    break;

                case TestType.Practice:
                    _resultTitleText.text = GetLocalizedPracticeResultTitle(GameManager.Instance.CurrentTestMode);
                    break;

                case TestType.OfficialExam:
                    _resultTitleText.text = GetLocalizedOfficialExamResultTitle(GameManager.Instance.CurrentTestMode);
                    break;
            }
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam
            && ScoreManager.Instance != null)
        {
            float minTotal = UnifiedExamScoring.OverallPassMinTotalScore(GameManager.Instance.UnifiedExamGrade);
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(ScoreManager.Instance.LastUnifiedExamSessionTimestamp))
            {
                sb.AppendLine(GameLocalization.FormatUiString(UiStringKeys.ResultUnifiedTakenAtFmt,
                    "Completed: {0}", ScoreManager.Instance.LastUnifiedExamSessionTimestamp));
            }

            sb.AppendLine(GameLocalization.FormatUiString(UiStringKeys.ResultUnifiedLine1Fmt,
                "Grade {0} exam, total {1:F1} / 700",
                GameManager.Instance.UnifiedExamGrade, ScoreManager.Instance.LastUnifiedExamTotalScore));
            string finalWord = ScoreManager.Instance.LastUnifiedExamOverallPass
                ? GameLocalization.GetUiString(UiStringKeys.ResultUnifiedFinalPass, "Final pass")
                : GameLocalization.GetUiString(UiStringKeys.ResultUnifiedFinalFail, "Final fail");
            sb.AppendLine(GameLocalization.FormatUiString(UiStringKeys.ResultUnifiedLine2Fmt,
                "Need {0:F0}+ total, {1}", minTotal, finalWord));
            sb.AppendLine(GameLocalization.GetUiString(UiStringKeys.ResultUnifiedSubjectsHeader, "[ Subjects ]"));
            var records = GameManager.Instance.UnifiedExamRecords;
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i] != null)
                {
                    sb.AppendLine(records[i].SummaryLine);
                }
            }

            _resultText.text = sb.ToString().TrimEnd();

            if (_rewardText != null)
            {
                _rewardText.text = ScoreManager.Instance.LastUnifiedExamOverallPass
                    ? GameLocalization.FormatUiString(UiStringKeys.ResultUnifiedRewardOkFmt,
                        "REWARD: +{0} stardust (tier {1})", ScoreManager.Instance.LastEarnedTokens,
                        ScoreManager.Instance.LastUnifiedExamRewardTier)
                    : GameLocalization.GetUiString(UiStringKeys.ResultUnifiedRewardFail,
                        "REWARD: 0 stardust (final fail)");
            }

            if (_newRecordIndicator != null)
            {
                _newRecordIndicator.SetActive(ScoreManager.Instance.IsNewRecord);
            }

            return;
        }

        if (ScoreManager.Instance.IsFailed)
        {
            bool falseStart = GameManager.Instance != null
                && GameManager.Instance.CurrentTestMode == TestMode.Reaction
                && ScoreManager.Instance.LastReactionTime <= 0f;
            if (falseStart)
            {
                _resultText.text = GameLocalization.GetUiString(UiStringKeys.ResultFailFalseStart, "FAILED\nFALSE START");
            }
            else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.Reaction)
            {
                float t = ScoreManager.Instance.LastReactionTime;
                float limit = ReactionDifficulty.GetPassMaxSeconds(9); // 9등급 컷
                
                bool isKo = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale != null &&
                            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("ko", System.StringComparison.OrdinalIgnoreCase);
                
                if (isKo)
                {
                    _resultText.text = $"불합격\n반응 속도: {t * 1000f:F0} ms (9등급 합격선 {limit * 1000f:F0} ms 이하)";
                }
                else
                {
                    _resultText.text = $"FAILED\nReaction {t * 1000f:F0} ms (pass {limit * 1000f:F0} ms or lower)";
                }
            }
            else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.AimPrecision)
            {
                float t = ScoreManager.Instance.LastReactionTime;
                int g = GameManager.Instance.GetPracticeGrade(TestMode.AimPrecision);
                float limit = AimDifficulty.GetPassMaxTotalSeconds(g);
                _resultText.text = GameLocalization.FormatUiString(UiStringKeys.ResultFailAimFmt,
                    "FAILED\nAim {0:F2}s (pass {1:F1}s or lower)", t, limit);
            }
            else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.ClicksPerSecond)
            {
                float c = ScoreManager.Instance.LastCpsAverage;
                int g = GameManager.Instance.GetPracticeGrade(TestMode.ClicksPerSecond);
                float need = CpsDifficulty.GetMinPassCps(g);
                _resultText.text = GameLocalization.FormatUiString(UiStringKeys.ResultFailCpsFmt,
                    "FAILED\nAverage {0:F2} /s (pass {1:F2} /s or higher)", c, need);
            }
            else
            {
                _resultText.text = GameLocalization.GetUiString(UiStringKeys.ResultBodyFailed, "FAILED");
            }

            if (_rewardText != null)
            {
                _rewardText.text = GameLocalization.GetUiString(UiStringKeys.ResultRewardZeroStardust, "REWARD: 0 stardust");
            }
        }
        else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.MemorySequence)
        {
            string tier = ScoreManager.Instance.GetTier();
            bool exam = GameManager.Instance.CurrentTestType == TestType.OfficialExam;
            float span = ScoreManager.Instance.LastMemoryScore;
            string line1 = exam
                ? GameLocalization.FormatUiString(UiStringKeys.ResultMemoryExamAvgSpanFmt, "Max span: {0:F2}", span)
                : GameLocalization.FormatUiString(UiStringKeys.ResultMemoryPracticeBestSpanFmt, "Best span: {0:F2}", span);
            string tierLine = GameLocalization.FormatUiString(UiStringKeys.ResultTierLabelFmt, "TIER: {0}", tier);
            _resultText.text = line1 + "\n" + tierLine;

            if (_rewardText != null)
            {
                _rewardText.text = GameLocalization.FormatUiString(UiStringKeys.ResultRewardPlusStardustFmt, "REWARD: +{0} stardust",
                    ScoreManager.Instance.LastEarnedTokens);
            }
        }
        else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.RhythmTiming)
        {
            string tier = ScoreManager.Instance.GetTier();
            float err = ScoreManager.Instance.LastRhythmMeanErrorMs;
            float acc = ScoreManager.Instance.LastRhythmAccuracyPercent;
            _resultText.text = GameLocalization.FormatUiString(UiStringKeys.ResultRhythmStatsFmt,
                "MEAN ERROR: {0:F1} MS\nACCURACY: {1:F1} %\nTIER: {2}", err, acc, tier);

            if (_rewardText != null)
            {
                _rewardText.text = GameLocalization.FormatUiString(UiStringKeys.ResultRewardPlusStardustFmt, "REWARD: +{0} stardust",
                    ScoreManager.Instance.LastEarnedTokens);
            }
        }
        else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.MultipleObjectTracking)
        {
            string tier = ScoreManager.Instance.GetTier();
            float acc = ScoreManager.Instance.LastMotAccuracyPercent;
            bool exam = GameManager.Instance.CurrentTestType == TestType.OfficialExam;
            string line1 = exam
                ? GameLocalization.FormatUiString(UiStringKeys.ResultMotExamAvgFmt, "3-run avg accuracy: {0:F1} %", acc)
                : GameLocalization.FormatUiString(UiStringKeys.ResultMotPracticeOverallFmt, "Overall accuracy: {0:F1} %", acc);
            string tierLine = GameLocalization.FormatUiString(UiStringKeys.ResultTierLabelFmt, "TIER: {0}", tier);
            _resultText.text = line1 + "\n" + tierLine;

            if (_rewardText != null)
            {
                _rewardText.text = GameLocalization.FormatUiString(UiStringKeys.ResultRewardPlusStardustFmt, "REWARD: +{0} stardust",
                    ScoreManager.Instance.LastEarnedTokens);
            }
        }
        else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.BulletHell)
        {
            string tier = ScoreManager.Instance.GetTier();
            float sec = ScoreManager.Instance.LastBulletHellSurvivalSeconds;
            bool exam = GameManager.Instance.CurrentTestType == TestType.OfficialExam;
            bool g1 = ScoreManager.Instance.LastBulletHellWasGrade1;
            string line1;
            if (g1)
            {
                line1 = exam
                    ? GameLocalization.FormatUiString(UiStringKeys.ResultBhExamSumG1Fmt,
                        "3-round survival sum: {0:F1}s (pass {1:F0}s)", sec, BulletHellDifficulty.Grade1ExamPassSumSeconds)
                    : GameLocalization.FormatUiString(UiStringKeys.ResultBhPracticeG1Fmt,
                        "Survival: {0:F1}s (Grade 1, max survival)", sec);
            }
            else
            {
                line1 = exam
                    ? GameLocalization.FormatUiString(UiStringKeys.ResultBhExamSumFmt, "3-round survival sum: {0:F1}s", sec)
                    : GameLocalization.FormatUiString(UiStringKeys.ResultBhPracticeSurvivalFmt, "Survival: {0:F1}s", sec);
            }

            string tierLineBh = GameLocalization.FormatUiString(UiStringKeys.ResultTierLabelFmt, "TIER: {0}", tier);
            _resultText.text = line1 + "\n" + tierLineBh;

            if (_rewardText != null)
            {
                _rewardText.text = GameLocalization.FormatUiString(UiStringKeys.ResultRewardPlusStardustFmt, "REWARD: +{0} stardust",
                    ScoreManager.Instance.LastEarnedTokens);
            }
        }
        else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.ClicksPerSecond)
        {
            string tier = ScoreManager.Instance.GetTier();
            float cps = ScoreManager.Instance.LastCpsAverage;
            int g = GameManager.Instance.GetPracticeGrade(TestMode.ClicksPerSecond);
            float need = CpsDifficulty.GetMinPassCps(g);
            _resultText.text = GameLocalization.FormatUiString(UiStringKeys.ResultCpsBodyFmt,
                "Average: {0:F2} clicks/s (10s total)\nGrade {1} pass: {2:F2} /s or higher\nTIER: {3}",
                cps, g, need, tier);

            if (_rewardText != null)
            {
                _rewardText.text = GameLocalization.FormatUiString(UiStringKeys.ResultRewardPlusStardustFmt, "REWARD: +{0} stardust",
                    ScoreManager.Instance.LastEarnedTokens);
            }
        }
        else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.AimPrecision)
        {
            float aimTime = ScoreManager.Instance.LastReactionTime;
            string tier = ScoreManager.Instance.GetTier();
            int g = GameManager.Instance.GetPracticeGrade(TestMode.AimPrecision);
            float limit = AimDifficulty.GetPassMaxTotalSeconds(g);
            float side = AimDifficulty.GetTargetSideLengthPixels(g);
            _resultText.text = GameLocalization.FormatUiString(UiStringKeys.ResultAimPassBodyFmt,
                "Total time: {0:F2}s\nGrade {1} pass: {2:F1}s or lower, pass\nTarget ~{3:F0}px\nReward tier: {4}",
                aimTime, g, limit, side, tier);

            if (_rewardText != null)
            {
                _rewardText.text = GameLocalization.FormatUiString(UiStringKeys.ResultRewardPlusStardustFmt, "REWARD: +{0} stardust",
                    ScoreManager.Instance.LastEarnedTokens);
            }
        }
        else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.Reaction)
        {
            float reactionTime = ScoreManager.Instance.LastReactionTime;
            string tier = ScoreManager.Instance.GetTier();
            
            bool isKo = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale != null &&
                        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("ko", System.StringComparison.OrdinalIgnoreCase);
            
            if (int.TryParse(tier, out int achievedGrade))
            {
                float limit = ReactionDifficulty.GetPassMaxSeconds(achievedGrade);
                if (isKo)
                {
                    _resultText.text = $"반응 속도: {reactionTime * 1000f:F0} ms ({reactionTime:F3}초)\n달성 등급: {achievedGrade}등급 (합격선 {limit * 1000f:F0} ms 이하)\n결과: {achievedGrade}등급 합격";
                }
                else
                {
                    _resultText.text = $"Reaction: {reactionTime * 1000f:F0} ms ({reactionTime:F3}s)\nAchieved: Grade {achievedGrade} (limit {limit * 1000f:F0} ms or lower)\nResult: Grade {achievedGrade} Pass";
                }
            }
            else
            {
                if (isKo)
                {
                    _resultText.text = $"반응 속도: {reactionTime * 1000f:F0} ms ({reactionTime:F3}초)\n합격선 초과 (9등급 합격선 {ReactionDifficulty.GetPassMaxMs(9)} ms 초과)\n결과: 불합격";
                }
                else
                {
                    _resultText.text = $"Reaction: {reactionTime * 1000f:F0} ms ({reactionTime:F3}s)\nLimit exceeded (limit {ReactionDifficulty.GetPassMaxMs(9)} ms)\nResult: Failed";
                }
            }

            if (_rewardText != null)
            {
                _rewardText.text = GameLocalization.FormatUiString(UiStringKeys.ResultRewardPlusStardustFmt, "REWARD: +{0} stardust",
                    ScoreManager.Instance.LastEarnedTokens);
            }
        }
        else
        {
            float t = ScoreManager.Instance.LastReactionTime;
            string tier = ScoreManager.Instance.GetTier();
            _resultText.text = GameLocalization.FormatUiString(UiStringKeys.ResultGenericTimeTierFmt,
                "TIME: {0:F3} SEC\nTIER: {1}", t, tier);

            if (_rewardText != null)
            {
                _rewardText.text = GameLocalization.FormatUiString(UiStringKeys.ResultRewardPlusStardustFmt, "REWARD: +{0} stardust",
                    ScoreManager.Instance.LastEarnedTokens);
            }
        }

        if (_newRecordIndicator != null)
        {
            _newRecordIndicator.SetActive(ScoreManager.Instance.IsNewRecord);
        }

        ApplyResultActionButtonLabels();
        ApplyNewRecordBadgeText();
    }
}
