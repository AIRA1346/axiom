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

    private void OnEnable()
    {
        ResolveButtonsIfNeeded();
        BindButtonListeners();

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
        UnbindButtonListeners();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    /// <summary>
    /// 씬 직렬화가 비어 있거나 비활성 패널 때문에 참조가 끊긴 경우에도 버튼을 찾습니다.
    /// </summary>
    private void ResolveButtonsIfNeeded()
    {
        if (_retryButton != null && _mainMenuButton != null)
        {
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
            GameManager.Instance.SetGameState(GameState.TestStandby);
            return;
        }

        if (GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam)
        {
            if (GameManager.Instance.TryRetryUnifiedExamWithLastGrade())
            {
                return;
            }

#if UNITY_EDITOR
            Debug.Log("응시권 부족으로 통합 시험 재응시 불가.");
#endif
            GameManager.Instance.SetGameState(GameState.MainMenu);
            return;
        }

        if (EconomyManager.Instance != null && EconomyManager.Instance.UseTicket())
        {
            GameManager.Instance.SetGameState(GameState.TestStandby);
            return;
        }

#if UNITY_EDITOR
        Debug.Log("티켓 부족으로 재도전 불가!");
#endif
        GameManager.Instance.SetGameState(GameState.MainMenu);
    }

    /// <summary>
    /// G.S.I 씬에서는 ARCHÉ 허브가 아니라 G.S.I 로비(GameState.MainMenu)로만 복귀합니다.
    /// SampleScene(허브)에서는 메인 메뉴 패널로 동일하게 복귀합니다.
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
                    _resultTitleText.text = "통합 공식 시험 · 결과";
                    break;

                case TestType.Practice:
                    switch (GameManager.Instance.CurrentTestMode)
                    {
                        case TestMode.Reaction:
                            _resultTitleText.text = "REACTION PRACTICE";
                            break;

                        case TestMode.AimPrecision:
                            _resultTitleText.text = "AIM PRACTICE";
                            break;

                        case TestMode.MemorySequence:
                            _resultTitleText.text = "MEMORY PRACTICE";
                            break;

                        case TestMode.RhythmTiming:
                            _resultTitleText.text = "RHYTHM PRACTICE";
                            break;

                        case TestMode.MultipleObjectTracking:
                            _resultTitleText.text = "MOT PRACTICE";
                            break;

                        case TestMode.BulletHell:
                            _resultTitleText.text = "BULLET HELL PRACTICE";
                            break;
                    }
                    break;

                case TestType.OfficialExam:
                    switch (GameManager.Instance.CurrentTestMode)
                    {
                        case TestMode.Reaction:
                            _resultTitleText.text = "OFFICIAL REACTION EXAM";
                            break;

                        case TestMode.AimPrecision:
                            _resultTitleText.text = "OFFICIAL AIM EXAM";
                            break;

                        case TestMode.MemorySequence:
                            _resultTitleText.text = "OFFICIAL MEMORY EXAM";
                            break;

                        case TestMode.RhythmTiming:
                            _resultTitleText.text = "OFFICIAL RHYTHM EXAM";
                            break;

                        case TestMode.MultipleObjectTracking:
                            _resultTitleText.text = "OFFICIAL MOT EXAM";
                            break;

                        case TestMode.BulletHell:
                            _resultTitleText.text = "OFFICIAL BULLET HELL EXAM";
                            break;
                    }
                    break;
            }
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam
            && ScoreManager.Instance != null)
        {
            float minTotal = UnifiedExamScoring.OverallPassMinTotalScore(GameManager.Instance.UnifiedExamGrade);
            var sb = new StringBuilder();
            sb.AppendLine(
                $"{GameManager.Instance.UnifiedExamGrade}급 시험 · 총점 {ScoreManager.Instance.LastUnifiedExamTotalScore:F1} / 600");
            sb.AppendLine(
                $"최종 기준 {minTotal:F0}점 이상 · {(ScoreManager.Instance.LastUnifiedExamOverallPass ? "최종 합격" : "최종 불합격")}");
            sb.AppendLine("— 과목별 —");
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
                    ? $"REWARD: +{ScoreManager.Instance.LastEarnedTokens} 기초 골드 (티어 {ScoreManager.Instance.LastUnifiedExamRewardTier})"
                    : "REWARD: 0 기초 골드 (최종 불합격)";
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
                _resultText.text = "FAILED\nFALSE START";
            }
            else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.Reaction)
            {
                float t = ScoreManager.Instance.LastReactionTime;
                int g = GameManager.Instance.GetPracticeGrade(TestMode.Reaction);
                float limit = ReactionDifficulty.GetPassMaxSeconds(g);
                _resultText.text =
                    $"FAILED\n반응 {t * 1000f:F0} ms (합격 기준 {limit * 1000f:F0} ms 이하)";
            }
            else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.AimPrecision)
            {
                float t = ScoreManager.Instance.LastReactionTime;
                int g = GameManager.Instance.GetPracticeGrade(TestMode.AimPrecision);
                float limit = AimDifficulty.GetPassMaxTotalSeconds(g);
                _resultText.text = $"FAILED\n에임 {t:F2}초 (합격 기준 {limit:F1}초 이하)";
            }
            else
            {
                _resultText.text = "FAILED";
            }

            if (_rewardText != null)
            {
                _rewardText.text = "REWARD: 0 기초 골드";
            }
        }
        else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.MemorySequence)
        {
            string tier = ScoreManager.Instance.GetTier();
            bool exam = GameManager.Instance.CurrentTestType == TestType.OfficialExam;
            string line1 = exam
                ? $"3회 평균 순서 길이: {ScoreManager.Instance.LastMemoryScore:F2}"
                : $"최대 순서 길이: {ScoreManager.Instance.LastMemoryScore:F2}";
            _resultText.text = $"{line1}\nTIER: {tier}";

            if (_rewardText != null)
            {
                _rewardText.text = $"REWARD: +{ScoreManager.Instance.LastEarnedTokens} 기초 골드";
            }
        }
        else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.RhythmTiming)
        {
            string tier = ScoreManager.Instance.GetTier();
            float err = ScoreManager.Instance.LastRhythmMeanErrorMs;
            float acc = ScoreManager.Instance.LastRhythmAccuracyPercent;
            _resultText.text = $"MEAN ERROR: {err:F1} MS\nACCURACY: {acc:F1} %\nTIER: {tier}";

            if (_rewardText != null)
            {
                _rewardText.text = $"REWARD: +{ScoreManager.Instance.LastEarnedTokens} 기초 골드";
            }
        }
        else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.MultipleObjectTracking)
        {
            string tier = ScoreManager.Instance.GetTier();
            float acc = ScoreManager.Instance.LastMotAccuracyPercent;
            bool exam = GameManager.Instance.CurrentTestType == TestType.OfficialExam;
            string line1 = exam
                ? $"3회 평균 정확도: {acc:F1} %"
                : $"종합 정확도: {acc:F1} %";
            _resultText.text = $"{line1}\nTIER: {tier}";

            if (_rewardText != null)
            {
                _rewardText.text = $"REWARD: +{ScoreManager.Instance.LastEarnedTokens} 기초 골드";
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
                    ? $"3판 생존 시간 합: {sec:F1} 초 (합격 기준 {BulletHellDifficulty.Grade1ExamPassSumSeconds:F0}초)"
                    : $"생존 시간: {sec:F1} 초 (1급 · 최대 생존)";
            }
            else
            {
                line1 = exam
                    ? $"3판 생존 시간 합: {sec:F1} 초"
                    : $"생존 시간: {sec:F1} 초";
            }

            _resultText.text = $"{line1}\nTIER: {tier}";

            if (_rewardText != null)
            {
                _rewardText.text = $"REWARD: +{ScoreManager.Instance.LastEarnedTokens} 기초 골드";
            }
        }
        else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.AimPrecision)
        {
            float aimTime = ScoreManager.Instance.LastReactionTime;
            string tier = ScoreManager.Instance.GetTier();
            int g = GameManager.Instance.GetPracticeGrade(TestMode.AimPrecision);
            float limit = AimDifficulty.GetPassMaxTotalSeconds(g);
            float side = AimDifficulty.GetTargetSideLengthPixels(g);
            _resultText.text =
                $"총 시간: {aimTime:F2}초\n{g}급 합격 기준: {limit:F1}초 이하 · 합격\n타겟 크기: 약 {side:F0}px\n보상 티어: {tier}";

            if (_rewardText != null)
            {
                _rewardText.text = $"REWARD: +{ScoreManager.Instance.LastEarnedTokens} 기초 골드";
            }
        }
        else if (GameManager.Instance != null && GameManager.Instance.CurrentTestMode == TestMode.Reaction)
        {
            float reactionTime = ScoreManager.Instance.LastReactionTime;
            string tier = ScoreManager.Instance.GetTier();
            int g = GameManager.Instance.GetPracticeGrade(TestMode.Reaction);
            float limit = ReactionDifficulty.GetPassMaxSeconds(g);
            _resultText.text =
                $"반응: {reactionTime * 1000f:F0} ms ({reactionTime:F3}초)\n{g}급 합격 기준: {limit * 1000f:F0} ms 이하 · 합격\n보상 티어: {tier}";

            if (_rewardText != null)
            {
                _rewardText.text = $"REWARD: +{ScoreManager.Instance.LastEarnedTokens} 기초 골드";
            }
        }
        else
        {
            float t = ScoreManager.Instance.LastReactionTime;
            string tier = ScoreManager.Instance.GetTier();
            _resultText.text = $"TIME: {t:F3} SEC\nTIER: {tier}";

            if (_rewardText != null)
            {
                _rewardText.text = $"REWARD: +{ScoreManager.Instance.LastEarnedTokens} 기초 골드";
            }
        }

        if (_newRecordIndicator != null)
        {
            _newRecordIndicator.SetActive(ScoreManager.Instance.IsNewRecord);
        }
    }
}
