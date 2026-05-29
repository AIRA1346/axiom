using UnityEngine;

/// <summary>
/// Clicks-per-second: practice and unified scoring use average CPS (total clicks / 10s).
/// </summary>
public static class CpsDifficulty
{
    public const float SessionDurationSeconds = 10f;

    /// <summary>Minimum average clicks per second to count as a pass for the given practice/exam rank.</summary>
    public static float GetMinPassCps(int grade1to9)
    {
        float t = PracticeDifficulty.Ease01(grade1to9);
        return Mathf.Lerp(7.2f, 1.1f, t);
    }

    public static bool IsPass(float averageCps, int grade1to9)
    {
        return averageCps + 1e-5f >= GetMinPassCps(grade1to9);
    }
}
