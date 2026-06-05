using UnityEngine;

/// <summary>
/// G.S.I ?몄뀡??理쒓렐 ?먯닔쨌蹂댁긽 ?곹깭瑜?蹂닿??⑸땲?? 諛섏쓳(?쒓컙)쨌?먯엫(?쒓컙)쨌湲곗뼲(?쒖꽌 湲몄씠)??援щ텇?⑸땲??
/// </summary>
public sealed class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public float LastReactionTime { get; private set; }
    /// <summary>湲곗뼲 怨쇱젣: ?꾩쟾 ?ы쁽??理쒕? 湲몄씠(怨듭떇 ?쒗뿕? 1???쒕룄 媛?.</summary>
    public float LastMemoryScore { get; private set; }

    /// <summary>由щ벉: ?됯퇏 ?덈? ??대컢 ?ㅼ감(ms). ??쓣?섎줉 醫뗭쓬.</summary>
    public float LastRhythmMeanErrorMs { get; private set; }

    /// <summary>由щ벉: ?먯젙 ?깃났 鍮꾩쑉(0~100).</summary>
    public float LastRhythmAccuracyPercent { get; private set; }

    /// <summary>?ㅼ쨷 異붿쟻(MOT): ?쒗뿕쨌?곗뒿 醫낇빀 ?뺥솗??%). ?믪쓣?섎줉 醫뗭쓬.</summary>
    public float LastMotAccuracyPercent { get; private set; }

    /// <summary>Bullet Hell: ?앹〈 ?쒓컙 ??珥?. ?믪쓣?섎줉 醫뗭쓬.</summary>
    public float LastBulletHellSurvivalSeconds { get; private set; }

    public bool LastBulletHellWasGrade1 { get; private set; }

    /// <summary>Average CPS (total clicks in target / 10s).</summary>
    public float LastCpsAverage { get; private set; }

    public bool IsFailed { get; private set; }
    public bool IsNewRecord { get; private set; }
    private int _lastEarnedTokens;
    public int LastEarnedTokens
    {
        get => _lastEarnedTokens;
        private set
        {
            _lastEarnedTokens = value;
            if (value == 0)
            {
                LastEarnedAstralCores = 0;
            }
        }
    }
    public int LastEarnedAstralCores { get; private set; }
    public int LastEarnedStardust => LastEarnedTokens;

    public float LastUnifiedExamTotalScore { get; private set; }
    public bool LastUnifiedExamOverallPass { get; private set; }
    public int LastUnifiedExamGrade { get; private set; }
    public string LastUnifiedExamRewardTier { get; private set; } = "F";

    /// <summary>?듯빀 ?쒗뿕 ?몄뀡???앸궃 ?쒓컖(<see cref="UnifiedExamHistoryStorage.FormatCompletedAtNow"/> ?뺤떇). 寃곌낵 ?붾㈃ ?쒖떆??</summary>
    public string LastUnifiedExamSessionTimestamp { get; private set; } = string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Unified official exam (7 subjects) final call ??run once, includes gold reward.</summary>
    public void ApplyUnifiedExamFinalResult(
        float totalScore0To700,
        bool overallPass,
        int examGrade1to9,
        string rewardTierLetter,
        bool anyRecordUpdated,
        string sessionCompletedAt)
    {
        LastReactionTime = 0f;
        LastMemoryScore = 0f;
        LastRhythmMeanErrorMs = 0f;
        LastRhythmAccuracyPercent = 0f;
        LastMotAccuracyPercent = 0f;
        LastBulletHellSurvivalSeconds = 0f;
        LastBulletHellWasGrade1 = false;
        LastCpsAverage = 0f;
        LastUnifiedExamSessionTimestamp = string.IsNullOrEmpty(sessionCompletedAt)
            ? UnifiedExamHistoryStorage.FormatCompletedAtNow()
            : sessionCompletedAt;
        LastUnifiedExamTotalScore = totalScore0To700;
        LastUnifiedExamOverallPass = overallPass;
        LastUnifiedExamGrade = examGrade1to9;
        LastUnifiedExamRewardTier = string.IsNullOrEmpty(rewardTierLetter) ? "F" : rewardTierLetter;
        IsFailed = !overallPass;
        IsNewRecord = anyRecordUpdated;
        LastEarnedTokens = 0;

        if (overallPass && EconomyManager.Instance != null)
        {
            LastEarnedTokens = EconomyManager.Instance.RewardStardustForTier(LastUnifiedExamRewardTier);
        }
    }

    public void SaveTestTime(float time)
    {
        LastReactionTime = time;
        LastMemoryScore = 0f;
        LastRhythmMeanErrorMs = 0f;
        LastRhythmAccuracyPercent = 0f;
        LastMotAccuracyPercent = 0f;
        LastBulletHellSurvivalSeconds = 0f;
        LastBulletHellWasGrade1 = false;
        LastCpsAverage = 0f;
        IsFailed = false;
        if (GameManager.Instance != null)
        {
            switch (GameManager.Instance.CurrentTestMode)
            {
                case TestMode.Reaction:
                    // 난이도 선택 삭제로 인해 무조건 9급 합격 컷인 0.320초를 합격선으로 삼음
                    IsFailed = !ReactionDifficulty.IsPass(time, 9);
                    break;
                case TestMode.AimPrecision:
                    IsFailed = !AimDifficulty.IsPass(time, GameManager.Instance.GetPracticeGrade(TestMode.AimPrecision));
                    break;
            }
        }

        ApplyRecordFlagsForTimeModes(time);
        ApplyEconomyRewards();
    }

    public void SaveMemoryScore(float spanAverage, bool failed)
    {
        LastReactionTime = 0f;
        LastMemoryScore = failed ? 0f : spanAverage;
        LastRhythmMeanErrorMs = 0f;
        LastRhythmAccuracyPercent = 0f;
        LastMotAccuracyPercent = 0f;
        LastBulletHellSurvivalSeconds = 0f;
        LastBulletHellWasGrade1 = false;
        LastCpsAverage = 0f;
        IsFailed = failed;
        IsNewRecord = false;

        if (failed)
        {
            LastEarnedTokens = 0;
            return;
        }

        if (PlayerDataManager.Instance != null && GameManager.Instance != null
            && GameManager.Instance.CurrentTestType == TestType.OfficialExam
            && GameManager.Instance.CurrentTestMode == TestMode.MemorySequence)
        {
            IsNewRecord = PlayerDataManager.Instance.CheckAndSaveBestMemorySpan(spanAverage);
        }

        ApplyEconomyRewards();
    }

    public void SaveRhythmResult(float meanAbsErrorMs, float accuracyPercent, bool failed)
    {
        LastReactionTime = 0f;
        LastMemoryScore = 0f;
        LastRhythmMeanErrorMs = meanAbsErrorMs;
        LastRhythmAccuracyPercent = accuracyPercent;
        LastMotAccuracyPercent = 0f;
        LastBulletHellSurvivalSeconds = 0f;
        LastBulletHellWasGrade1 = false;
        LastCpsAverage = 0f;
        IsFailed = failed;
        IsNewRecord = false;

        if (failed)
        {
            LastEarnedTokens = 0;
            return;
        }

        if (PlayerDataManager.Instance != null && GameManager.Instance != null
            && GameManager.Instance.CurrentTestType == TestType.OfficialExam
            && GameManager.Instance.CurrentTestMode == TestMode.RhythmTiming)
        {
            IsNewRecord = PlayerDataManager.Instance.CheckAndSaveBestRhythmScore(accuracyPercent, meanAbsErrorMs);
        }

        ApplyEconomyRewards();
    }

    public void SaveMotResult(float accuracyPercent, bool failed)
    {
        LastReactionTime = 0f;
        LastMemoryScore = 0f;
        LastRhythmMeanErrorMs = 0f;
        LastRhythmAccuracyPercent = 0f;
        LastMotAccuracyPercent = accuracyPercent;
        LastBulletHellSurvivalSeconds = 0f;
        LastBulletHellWasGrade1 = false;
        LastCpsAverage = 0f;
        IsFailed = failed;
        IsNewRecord = false;

        if (failed)
        {
            LastEarnedTokens = 0;
            return;
        }

        if (PlayerDataManager.Instance != null && GameManager.Instance != null
            && GameManager.Instance.CurrentTestType == TestType.OfficialExam
            && GameManager.Instance.CurrentTestMode == TestMode.MultipleObjectTracking)
        {
            IsNewRecord = PlayerDataManager.Instance.CheckAndSaveBestMotAccuracy(accuracyPercent);
        }

        ApplyEconomyRewards();
    }

    public void SaveBulletHellResult(float survivalSecondsSum, bool failed, bool grade1, bool isExam)
    {
        LastReactionTime = 0f;
        LastMemoryScore = 0f;
        LastRhythmMeanErrorMs = 0f;
        LastRhythmAccuracyPercent = 0f;
        LastMotAccuracyPercent = 0f;
        LastBulletHellSurvivalSeconds = survivalSecondsSum;
        LastBulletHellWasGrade1 = grade1;
        LastCpsAverage = 0f;
        IsFailed = failed;
        IsNewRecord = false;

        if (failed)
        {
            LastEarnedTokens = 0;
            return;
        }

        if (PlayerDataManager.Instance != null && GameManager.Instance != null
            && GameManager.Instance.CurrentTestType == TestType.OfficialExam
            && GameManager.Instance.CurrentTestMode == TestMode.BulletHell)
        {
            IsNewRecord = PlayerDataManager.Instance.CheckAndSaveBestBulletHellTime(survivalSecondsSum);
        }

        ApplyEconomyRewards();
    }

    public void SaveCpsResult(float averageCps, bool failed)
    {
        LastReactionTime = 0f;
        LastMemoryScore = 0f;
        LastRhythmMeanErrorMs = 0f;
        LastRhythmAccuracyPercent = 0f;
        LastMotAccuracyPercent = 0f;
        LastBulletHellSurvivalSeconds = 0f;
        LastBulletHellWasGrade1 = false;
        LastCpsAverage = averageCps;
        IsFailed = failed;
        IsNewRecord = false;

        if (failed)
        {
            LastEarnedTokens = 0;
            return;
        }

        if (PlayerDataManager.Instance != null && GameManager.Instance != null
            && GameManager.Instance.CurrentTestType == TestType.OfficialExam
            && GameManager.Instance.CurrentTestMode == TestMode.ClicksPerSecond)
        {
            IsNewRecord = PlayerDataManager.Instance.CheckAndSaveBestCps(averageCps);
        }

        ApplyEconomyRewards();
    }

    private void ApplyRecordFlagsForTimeModes(float time)
    {
        if (PlayerDataManager.Instance != null && GameManager.Instance != null)
        {
            if (GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam)
            {
                IsNewRecord = false;
                return;
            }

            if (GameManager.Instance.CurrentTestType == TestType.OfficialExam)
            {
                switch (GameManager.Instance.CurrentTestMode)
                {
                    case TestMode.Reaction:
                        IsNewRecord = PlayerDataManager.Instance.CheckAndSaveBestReactionTime(time);
                        return;

                    case TestMode.AimPrecision:
                        IsNewRecord = PlayerDataManager.Instance.CheckAndSaveBestAimTime(time);
                        return;
                }
            }
        }

        IsNewRecord = false;
    }

    private void ApplyEconomyRewards()
    {
        if (EconomyManager.Instance == null)
        {
            LastEarnedTokens = 0;
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam)
        {
            LastEarnedTokens = 0;
            return;
        }

        int baseReward = EconomyManager.Instance.RewardTokensForTier(GetTier());

        if (GameManager.Instance != null && GameManager.Instance.CurrentTestType == TestType.OfficialExam)
        {
            EconomyManager.Instance.AddTokens(baseReward);
            LastEarnedTokens = baseReward * 2;
        }
        else
        {
            LastEarnedTokens = baseReward;
        }
    }

    public void MarkFailed()
    {
        LastReactionTime = 0f;
        LastMemoryScore = 0f;
        LastRhythmMeanErrorMs = 0f;
        LastRhythmAccuracyPercent = 0f;
        LastMotAccuracyPercent = 0f;
        LastBulletHellSurvivalSeconds = 0f;
        LastBulletHellWasGrade1 = false;
        LastCpsAverage = 0f;
        IsFailed = true;
        IsNewRecord = false;
        LastEarnedTokens = 0;
    }

    public void ResetScore()
    {
        LastReactionTime = 0f;
        LastMemoryScore = 0f;
        LastRhythmMeanErrorMs = 0f;
        LastRhythmAccuracyPercent = 0f;
        LastMotAccuracyPercent = 0f;
        LastBulletHellSurvivalSeconds = 0f;
        LastBulletHellWasGrade1 = false;
        LastCpsAverage = 0f;
        IsFailed = false;
        IsNewRecord = false;
        LastEarnedTokens = 0;
        LastUnifiedExamTotalScore = 0f;
        LastUnifiedExamOverallPass = false;
        LastUnifiedExamGrade = 9;
        LastUnifiedExamRewardTier = "F";
        LastUnifiedExamSessionTimestamp = string.Empty;
    }

    public string GetTier()
    {
        if (IsFailed || GameManager.Instance == null)
        {
            return "F";
        }

        if (GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam)
        {
            return LastUnifiedExamRewardTier;
        }

        switch (GameManager.Instance.CurrentTestMode)
        {
            case TestMode.Reaction:
                // 1등급부터 9등급까지 순차적으로 컷을 판정하여 통과한 가장 높은 등급을 반환
                for (int grade = 1; grade <= 9; grade++)
                {
                    if (ReactionDifficulty.IsPass(LastReactionTime, grade))
                    {
                        return grade.ToString();
                    }
                }
                return "F";

            case TestMode.AimPrecision:
                if (GameManager.Instance != null)
                {
                    int g = GameManager.Instance.GetPracticeGrade(TestMode.AimPrecision);
                    if (!AimDifficulty.IsPass(LastReactionTime, g))
                    {
                        return "F";
                    }

                    float maxT = AimDifficulty.GetPassMaxTotalSeconds(g);
                    if (LastReactionTime <= maxT * 0.50f)
                    {
                        return "S";
                    }

                    if (LastReactionTime <= maxT * 0.65f)
                    {
                        return "A";
                    }

                    if (LastReactionTime <= maxT * 0.80f)
                    {
                        return "B";
                    }

                    return "C";
                }

                return "F";

            case TestMode.MemorySequence:
                return TierForMemorySpan(LastMemoryScore);

            case TestMode.RhythmTiming:
                return TierForRhythm(LastRhythmMeanErrorMs, LastRhythmAccuracyPercent);

            case TestMode.MultipleObjectTracking:
                return TierForMot(LastMotAccuracyPercent);

            case TestMode.BulletHell:
                return TierForBulletHell(LastBulletHellSurvivalSeconds, LastBulletHellWasGrade1);

            case TestMode.ClicksPerSecond:
                if (GameManager.Instance != null)
                {
                    int g = GameManager.Instance.GetPracticeGrade(TestMode.ClicksPerSecond);
                    if (!CpsDifficulty.IsPass(LastCpsAverage, g))
                    {
                        return "F";
                    }
                }

                return TierForCps(LastCpsAverage);

            default:
                return "F";
        }
    }

    private static string TierForBulletHell(float survivalSecondsSum, bool grade1)
    {
        bool exam = GameManager.Instance != null && GameManager.Instance.CurrentTestType == TestType.OfficialExam;

        if (grade1)
        {
            if (exam)
            {
                if (survivalSecondsSum >= 95f)
                {
                    return "S";
                }

                if (survivalSecondsSum >= 72f)
                {
                    return "A";
                }

                if (survivalSecondsSum >= 52f)
                {
                    return "B";
                }

                if (survivalSecondsSum >= BulletHellDifficulty.Grade1ExamPassSumSeconds)
                {
                    return "C";
                }

                return "F";
            }

            if (survivalSecondsSum >= 38f)
            {
                return "S";
            }

            if (survivalSecondsSum >= 28f)
            {
                return "A";
            }

            if (survivalSecondsSum >= 18f)
            {
                return "B";
            }

            return "C";
        }

        if (exam)
        {
            if (survivalSecondsSum >= 58f)
            {
                return "S";
            }

            if (survivalSecondsSum >= 44f)
            {
                return "A";
            }

            if (survivalSecondsSum >= 32f)
            {
                return "B";
            }

            if (survivalSecondsSum >= 22f)
            {
                return "C";
            }

            return "F";
        }

        if (survivalSecondsSum >= 22f)
        {
            return "S";
        }

        if (survivalSecondsSum >= 16f)
        {
            return "A";
        }

        if (survivalSecondsSum >= 11f)
        {
            return "B";
        }

        if (survivalSecondsSum >= 6f)
        {
            return "C";
        }

        return "F";
    }

    private static string TierForCps(float averageCps)
    {
        if (averageCps >= 7.2f)
        {
            return "S";
        }

        if (averageCps >= 5.2f)
        {
            return "A";
        }

        if (averageCps >= 3.2f)
        {
            return "B";
        }

        if (averageCps >= 1.0f)
        {
            return "C";
        }

        return "F";
    }

    private static string TierForMot(float accuracyPercent)
    {
        if (accuracyPercent >= 92f)
        {
            return "S";
        }

        if (accuracyPercent >= 80f)
        {
            return "A";
        }

        if (accuracyPercent >= 65f)
        {
            return "B";
        }

        if (accuracyPercent >= 48f)
        {
            return "C";
        }

        return "F";
    }

    private static string TierForRhythm(float meanErrorMs, float accuracyPercent)
    {
        if (accuracyPercent < 28f)
        {
            return "F";
        }

        if (meanErrorMs < 30f && accuracyPercent >= 92f)
        {
            return "S";
        }

        if (meanErrorMs < 42f && accuracyPercent >= 85f)
        {
            return "A";
        }

        if (meanErrorMs < 58f && accuracyPercent >= 72f)
        {
            return "B";
        }

        if (meanErrorMs < 78f && accuracyPercent >= 55f)
        {
            return "C";
        }

        return "F";
    }

    private static string TierForMemorySpan(float span)
    {
        if (span >= 12f)
        {
            return "S";
        }

        if (span >= 9f)
        {
            return "A";
        }

        if (span >= 6f)
        {
            return "B";
        }

        if (span >= 4f)
        {
            return "C";
        }

        return "F";
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
