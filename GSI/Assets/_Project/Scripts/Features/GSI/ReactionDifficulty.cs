using UnityEngine;

/// <summary>
/// 반응 속도: 연습·단일 공식·통합 공식 시험 공통 — 급수별 합격 기준(이하 = 합격, 빠를수록 좋음).
/// </summary>
public static class ReactionDifficulty
{
    /// <summary>1급~9급 합격 허용 최대 반응 시간(ms). 이 값 이하이면 합격.</summary>
    private static readonly int[] PassMaxMs =
    {
        165, 170, 180, 190, 200, 220, 250, 280, 320
    };

    public static int GetPassMaxMs(int grade1to9)
    {
        int g = Mathf.Clamp(grade1to9, 1, 9);
        return PassMaxMs[g - 1];
    }

    public static float GetPassMaxSeconds(int grade1to9)
    {
        return GetPassMaxMs(grade1to9) / 1000f;
    }

    public static bool IsPass(float reactionTimeSec, int grade1to9)
    {
        return reactionTimeSec <= GetPassMaxSeconds(grade1to9) + 1e-6f;
    }
}
