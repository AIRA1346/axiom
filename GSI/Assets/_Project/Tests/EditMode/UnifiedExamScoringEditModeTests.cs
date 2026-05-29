using NUnit.Framework;

/// <summary>
/// Guardrails for unified exam cutoffs (no localization / scene boot required).
/// </summary>
public sealed class UnifiedExamScoringEditModeTests
{
    [Test]
    public void MinPassScore_IsStrictest_At_Grade1()
    {
        Assert.AreEqual(76f, UnifiedExamScoring.MinPassScore(1), 0.001f);
    }

    [Test]
    public void MinPassScore_IsMostLenient_At_Grade9()
    {
        Assert.AreEqual(42f, UnifiedExamScoring.MinPassScore(9), 0.001f);
    }

    [Test]
    public void OverallPassMin_FallsBetweenExtremes()
    {
        float g1 = UnifiedExamScoring.OverallPassMinTotalScore(1);
        float g9 = UnifiedExamScoring.OverallPassMinTotalScore(9);
        Assert.AreEqual(478f, g1, 0.01f);
        Assert.AreEqual(298f, g9, 0.01f);
        Assert.Less(g9, g1);
    }

    [Test]
    public void TierLetterFromAverage_UsesBoundaries()
    {
        Assert.AreEqual("S", UnifiedExamScoring.TierLetterFromAverage(92f));
        Assert.AreEqual("A", UnifiedExamScoring.TierLetterFromAverage(82f));
        Assert.AreEqual("B", UnifiedExamScoring.TierLetterFromAverage(70f));
        Assert.AreEqual("C", UnifiedExamScoring.TierLetterFromAverage(69.9f));
    }

    [Test]
    public void Clamped_Grade_OutOfRange_StillScores()
    {
        // Ease01 clamps; scoring must not throw for user-error grades.
        float low = UnifiedExamScoring.MinPassScore(0);
        float high = UnifiedExamScoring.MinPassScore(20);
        Assert.AreEqual(UnifiedExamScoring.MinPassScore(1), low, 0.001f);
        Assert.AreEqual(UnifiedExamScoring.MinPassScore(9), high, 0.001f);
    }
}
