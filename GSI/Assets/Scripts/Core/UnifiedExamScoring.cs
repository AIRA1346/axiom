using UnityEngine;

/// <summary>
/// Unified official exam: per-grade scores (0–100) and pass/fail per subject.
/// </summary>
public static class UnifiedExamScoring
{
    /// <summary>Sum of seven subject scores (max 700) must reach this for a final pass (depends on exam grade).</summary>
    public static float OverallPassMinTotalScore(int examGrade1to9)
    {
        float t = PracticeDifficulty.Ease01(examGrade1to9);
        const float legacy6Max = 600f;
        const float current7Max = 700f;
        float minFor6 = Mathf.Lerp(478f, 298f, t);
        return minFor6 * (current7Max / legacy6Max);
    }

    /// <summary>Reward tier letter from average subject score (0–100).</summary>
    public static string TierLetterFromAverage(float avg0To100)
    {
        if (avg0To100 >= 92f)
        {
            return "S";
        }

        if (avg0To100 >= 82f)
        {
            return "A";
        }

        if (avg0To100 >= 70f)
        {
            return "B";
        }

        return "C";
    }

    /// <summary>Minimum score to pass a subject for this exam grade.</summary>
    public static float MinPassScore(int examGrade1to9)
    {
        float t = PracticeDifficulty.Ease01(examGrade1to9);
        return Mathf.Lerp(76f, 42f, t);
    }

    public static void Evaluate(int examGrade1to9, UnifiedExamSegmentPayload p, out float score0To100, out bool passed,
        out string summaryLine)
    {
        float minPass = MinPassScore(examGrade1to9);
        string pass = GameLocalization.GetUiString(UiStringKeys.CommonPass, "Pass");
        string fail = GameLocalization.GetUiString(UiStringKeys.CommonFail, "Fail");
        string segFail = GameLocalization.GetUiString(UiStringKeys.CommonSegmentFailed, "Failed");

        if (p.HardFailed)
        {
            score0To100 = 0f;
            passed = false;
            summaryLine = GameLocalization.FormatUiString(UiStringKeys.UnifiedSegmentSummaryHardFmt,
                "{0} | 0 pts | {1}",
                ShortMode(p.Mode), segFail);
            return;
        }

        switch (p.Mode)
        {
            case TestMode.Reaction:
                score0To100 = ScoreReactionTime(p.Primary);
                {
                    float limitSec = ReactionDifficulty.GetPassMaxSeconds(examGrade1to9);
                    passed = p.Primary <= limitSec + 1e-6f;
                    int limitMs = ReactionDifficulty.GetPassMaxMs(examGrade1to9);
                    summaryLine = GameLocalization.FormatUiString(UiStringKeys.UnifiedSegmentSummaryReactionFmt,
                        "{0} | {1:F0} pts | {2:F0} ms (limit {3} ms) | {4}",
                        ShortMode(p.Mode), score0To100, p.Primary * 1000f, limitMs, passed ? pass : fail);
                }

                return;

            case TestMode.AimPrecision:
                score0To100 = ScoreAimTime(p.Primary, examGrade1to9);
                {
                    float limitSec = AimDifficulty.GetPassMaxTotalSeconds(examGrade1to9);
                    passed = AimDifficulty.IsPass(p.Primary, examGrade1to9);
                    summaryLine = GameLocalization.FormatUiString(UiStringKeys.UnifiedSegmentSummaryAimFmt,
                        "{0} | {1:F0} pts | {2:F2} s (limit {3:F1} s) | {4}",
                        ShortMode(p.Mode), score0To100, p.Primary, limitSec, passed ? pass : fail);
                }

                return;

            case TestMode.MemorySequence:
                score0To100 = ScoreMemorySpan(p.Primary);
                passed = score0To100 >= minPass;
                summaryLine = GameLocalization.FormatUiString(UiStringKeys.UnifiedSegmentSummaryMemoryFmt,
                    "{0} | {1:F0} pts | span {2:F2} | {3}",
                    ShortMode(p.Mode), score0To100, p.Primary, passed ? pass : fail);
                return;

            case TestMode.RhythmTiming:
                score0To100 = ScoreRhythm(p.Secondary, p.Primary);
                passed = score0To100 >= minPass;
                summaryLine = GameLocalization.FormatUiString(UiStringKeys.UnifiedSegmentSummaryRhythmFmt,
                    "{0} | {1:F0} pts | acc {2:F1}% | {3}",
                    ShortMode(p.Mode), score0To100, p.Secondary, passed ? pass : fail);
                return;

            case TestMode.MultipleObjectTracking:
                score0To100 = Mathf.Clamp(p.Primary, 0f, 100f);
                passed = score0To100 >= minPass;
                summaryLine = GameLocalization.FormatUiString(UiStringKeys.UnifiedSegmentSummaryMotFmt,
                    "{0} | {1:F0} pts | acc {2:F1}% | {3}",
                    ShortMode(p.Mode), score0To100, p.Primary, passed ? pass : fail);
                return;

            case TestMode.BulletHell:
                score0To100 = ScoreBulletHell(p.Primary, p.BoolFlag, examGrade1to9);
                passed = score0To100 >= minPass;
                summaryLine = GameLocalization.FormatUiString(UiStringKeys.UnifiedSegmentSummaryBulletFmt,
                    "{0} | {1:F0} pts | survive {2:F1} s | {3}",
                    ShortMode(p.Mode), score0To100, p.Primary, passed ? pass : fail);
                return;

            case TestMode.ClicksPerSecond:
                score0To100 = ScoreCps(p.Primary, examGrade1to9);
                passed = CpsDifficulty.IsPass(p.Primary, examGrade1to9);
                summaryLine = GameLocalization.FormatUiString(UiStringKeys.UnifiedSegmentSummaryCpsFmt,
                    "{0} | {1:F0} pts | {2:F2} /s (need {3:F2}) | {4}",
                    ShortMode(p.Mode), score0To100, p.Primary, CpsDifficulty.GetMinPassCps(examGrade1to9),
                    passed ? pass : fail);
                return;

            default:
                score0To100 = 0f;
                passed = false;
                summaryLine = GameLocalization.FormatUiString(UiStringKeys.UnifiedSegmentSummaryDefaultFmt,
                    "{0} | 0 pts",
                    p.Mode.ToString());
                return;
        }
    }

    private static float ScoreReactionTime(float timeSec)
    {
        const float best = 0.14f;
        const float worst = 0.48f;
        if (timeSec <= best)
        {
            return 100f;
        }

        if (timeSec >= worst)
        {
            return 0f;
        }

        return Mathf.Clamp01((worst - timeSec) / (worst - best)) * 100f;
    }

    private static float ScoreAimTime(float totalSec, int examGrade1to9)
    {
        float passMax = AimDifficulty.GetPassMaxTotalSeconds(examGrade1to9);
        float best = Mathf.Max(0.6f, passMax * 0.36f);
        float worst = Mathf.Max(passMax * 1.12f, best + 1f);
        if (totalSec <= best)
        {
            return 100f;
        }

        if (totalSec >= worst)
        {
            return 0f;
        }

        return Mathf.Clamp01((worst - totalSec) / (worst - best)) * 100f;
    }

    private static float ScoreMemorySpan(float avgSpan)
    {
        const float worst = 3f;
        const float best = 14f;
        if (avgSpan >= best)
        {
            return 100f;
        }

        if (avgSpan <= worst)
        {
            return 0f;
        }

        return Mathf.Clamp01((avgSpan - worst) / (best - worst)) * 100f;
    }

    private static float ScoreRhythm(float accuracyPercent, float meanErrorMs)
    {
        float acc = Mathf.Clamp(accuracyPercent, 0f, 100f);
        float errPenalty = Mathf.Clamp01(meanErrorMs / 120f) * 25f;
        return Mathf.Clamp(acc * 0.85f + (100f - errPenalty) * 0.15f, 0f, 100f);
    }

    private static float ScoreCps(float averageCps, int examGrade1to9)
    {
        float need = CpsDifficulty.GetMinPassCps(examGrade1to9);
        float best = need + 5.5f;
        if (averageCps >= best)
        {
            return 100f;
        }

        if (averageCps < need)
        {
            return 0f;
        }

        return Mathf.Clamp01((averageCps - need) / Mathf.Max(0.001f, best - need)) * 100f;
    }

    private static float ScoreBulletHell(float survivalSumSec, bool grade1SurvivalMode, int examGrade)
    {
        BulletHellDifficulty.GetParams(examGrade, out bool isG1, out float targetSec, out _, out _, out _, out _);

        if (grade1SurvivalMode && isG1)
        {
            float refSec = Mathf.Max(1f, BulletHellDifficulty.Grade1ExamPassSumSeconds);
            return Mathf.Clamp01(survivalSumSec / refSec) * 100f;
        }

        float need = Mathf.Max(1f, targetSec) * BulletHellDifficulty.ExamTrialCount;
        return Mathf.Clamp01(survivalSumSec / need) * 100f;
    }

    private static string ShortMode(TestMode m)
    {
        switch (m)
        {
            case TestMode.Reaction:
                return GameLocalization.GetUiString(UiStringKeys.ModeShortReaction, "Reaction");
            case TestMode.AimPrecision:
                return GameLocalization.GetUiString(UiStringKeys.ModeShortAim, "Aim");
            case TestMode.MemorySequence:
                return GameLocalization.GetUiString(UiStringKeys.ModeShortMemory, "Memory");
            case TestMode.RhythmTiming:
                return GameLocalization.GetUiString(UiStringKeys.ModeShortRhythm, "Rhythm");
            case TestMode.MultipleObjectTracking:
                return GameLocalization.GetUiString(UiStringKeys.ModeShortMot, "MOT");
            case TestMode.BulletHell:
                return GameLocalization.GetUiString(UiStringKeys.ModeShortBulletHell, "Bullet Hell");
            case TestMode.ClicksPerSecond:
                return GameLocalization.GetUiString(UiStringKeys.ModeShortCps, "CPS");
            default:
                return m.ToString();
        }
    }
}
