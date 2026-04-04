using UnityEngine;

/// <summary>
/// Bullet Hell (탄막 회피): 급수에 따라 탄 속도·발사·밀도·목표 생존 시간·생명 수가 변합니다.
/// 1급만 목표 시간 없이 &quot;최대 생존&quot;, 공식 시험 합격은 3판 합산 시간 기준입니다.
/// </summary>
public static class BulletHellDifficulty
{
    public const float InvincibilitySeconds = 1f;
    public const float PlayerMoveSpeed = 380f;
    public const float PlayerRadius = 14f;
    public const float BulletRadius = 6f;
    public const int ExamTrialCount = 3;

    /// <summary>공식 시험 1급: 3판 생존 시간 합이 이 값 이상이면 합격.</summary>
    public const float Grade1ExamPassSumSeconds = 34f;

    /// <summary>
    /// grade 1: 목표 시간 없음(끝까지 생존 시간만 기록). grades 2~9: 목표 생존 시간(초).
    /// </summary>
    public static void GetParams(int grade1to9, out bool isGrade1SurvivalScoring, out float targetSurvivalSeconds,
        out int lives, out float bulletSpeed, out float bulletsPerSecond, out float densityMultiplier)
    {
        int g = Mathf.Clamp(grade1to9, 1, 9);
        float t = PracticeDifficulty.Ease01(g);
        isGrade1SurvivalScoring = g == 1;

        lives = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1, 5, t)), 1, 5);

        bulletSpeed = Mathf.Lerp(300f, 125f, t);
        bulletsPerSecond = Mathf.Lerp(11f, 2.4f, t);
        densityMultiplier = Mathf.Lerp(1.25f, 0.38f, t);

        if (isGrade1SurvivalScoring)
        {
            targetSurvivalSeconds = 0f;
        }
        else
        {
            targetSurvivalSeconds = Mathf.Lerp(14f, 4.5f, t);
        }
    }
}
