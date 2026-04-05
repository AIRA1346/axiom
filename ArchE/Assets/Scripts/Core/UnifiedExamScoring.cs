using UnityEngine;

/// <summary>
/// 통합 공식 시험: 시험 급수(1=최상)에 따른 점수(0~100) 및 과목 합격 여부.
/// </summary>
public static class UnifiedExamScoring
{
    /// <summary>6과목 점수 합(최대 600)이 이 값 이상이면 최종 합격(시험 급수별).</summary>
    public static float OverallPassMinTotalScore(int examGrade1to9)
    {
        float t = PracticeDifficulty.Ease01(examGrade1to9);
        return Mathf.Lerp(478f, 298f, t);
    }

    /// <summary>최종 합격 시 보상 티어용: 6과목 평균 점수(0~100)로 S~C.</summary>
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

    /// <summary>과목 합격에 필요한 최소 점수(시험 급수별).</summary>
    public static float MinPassScore(int examGrade1to9)
    {
        float t = PracticeDifficulty.Ease01(examGrade1to9);
        return Mathf.Lerp(76f, 42f, t);
    }

    public static void Evaluate(int examGrade1to9, UnifiedExamSegmentPayload p, out float score0To100, out bool passed, out string summaryLine)
    {
        float minPass = MinPassScore(examGrade1to9);

        if (p.HardFailed)
        {
            score0To100 = 0f;
            passed = false;
            summaryLine = $"{GetModeShortName(p.Mode)} · 0점 · 실패";
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
                    summaryLine =
                        $"{GetModeShortName(p.Mode)} · {score0To100:F0}점 · 반응 {p.Primary * 1000f:F0} ms (기준 {limitMs} ms 이하) · {(passed ? "합격" : "불합격")}";
                }

                return;

            case TestMode.AimPrecision:
                score0To100 = ScoreAimTime(p.Primary, examGrade1to9);
                {
                    float limitSec = AimDifficulty.GetPassMaxTotalSeconds(examGrade1to9);
                    passed = AimDifficulty.IsPass(p.Primary, examGrade1to9);
                    summaryLine =
                        $"{GetModeShortName(p.Mode)} · {score0To100:F0}점 · {p.Primary:F2}s (기준 {limitSec:F1}s 이하) · {(passed ? "합격" : "불합격")}";
                }

                return;

            case TestMode.MemorySequence:
                score0To100 = ScoreMemorySpan(p.Primary);
                passed = score0To100 >= minPass;
                summaryLine = $"{GetModeShortName(p.Mode)} · {score0To100:F0}점 · 평균 {p.Primary:F2} · {(passed ? "합격" : "불합격")}";
                return;

            case TestMode.RhythmTiming:
                score0To100 = ScoreRhythm(p.Secondary, p.Primary);
                passed = score0To100 >= minPass;
                summaryLine = $"{GetModeShortName(p.Mode)} · {score0To100:F0}점 · 정확도 {p.Secondary:F1}% · {(passed ? "합격" : "불합격")}";
                return;

            case TestMode.MultipleObjectTracking:
                score0To100 = Mathf.Clamp(p.Primary, 0f, 100f);
                passed = score0To100 >= minPass;
                summaryLine = $"{GetModeShortName(p.Mode)} · {score0To100:F0}점 · 정확도 {p.Primary:F1}% · {(passed ? "합격" : "불합격")}";
                return;

            case TestMode.BulletHell:
                score0To100 = ScoreBulletHell(p.Primary, p.BoolFlag, examGrade1to9);
                passed = score0To100 >= minPass;
                summaryLine = $"{GetModeShortName(p.Mode)} · {score0To100:F0}점 · 생존 {p.Primary:F1}s · {(passed ? "합격" : "불합격")}";
                return;

            default:
                score0To100 = 0f;
                passed = false;
                summaryLine = $"{p.Mode} · 0점";
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

    /// <summary>시험 급수별 합격 허용 시간에 맞춘 0~100점(총 클리어 시간, 짧을수록 고점).</summary>
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

    private static string GetModeShortName(TestMode m)
    {
        switch (m)
        {
            case TestMode.Reaction:
                return "반응";
            case TestMode.AimPrecision:
                return "에임";
            case TestMode.MemorySequence:
                return "기억";
            case TestMode.RhythmTiming:
                return "리듬";
            case TestMode.MultipleObjectTracking:
                return "MOT";
            case TestMode.BulletHell:
                return "탄막";
            default:
                return m.ToString();
        }
    }
}
