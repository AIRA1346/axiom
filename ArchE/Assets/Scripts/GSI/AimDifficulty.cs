using UnityEngine;

/// <summary>
/// 에임 정밀도: 급수별 타겟 크기(픽셀)와 총 클리어 시간 합격선(초 이하 = 합격).
/// </summary>
public static class AimDifficulty
{
    /// <summary>1~9급 합격 허용 최대 총 시간(초). 7급은 명시 없어 6·8·9급과 동일(7초) 처리.</summary>
    private static readonly float[] PassMaxTotalSecondsByGrade =
    {
        4f, 5f, 5f, 5f, 5f, 7f, 7f, 7f, 7f
    };

    /// <summary>타겟 한 변 길이(px). 1급 ≈40 → 9급 ≈110 (Fitt's law에 맞춰 입문일수록 클릭 용이).</summary>
    public static float GetTargetSideLengthPixels(int grade1to9)
    {
        float t = PracticeDifficulty.Ease01(grade1to9);
        return Mathf.Round(Mathf.Lerp(40f, 110f, t));
    }

    /// <summary>필요 타겟을 모두 맞춘 총 시간(초)이 이 값 이하이면 합격.</summary>
    public static float GetPassMaxTotalSeconds(int grade1to9)
    {
        int g = Mathf.Clamp(grade1to9, 1, 9);
        return PassMaxTotalSecondsByGrade[g - 1];
    }

    public static bool IsPass(float totalTimeSeconds, int grade1to9)
    {
        return totalTimeSeconds <= GetPassMaxTotalSeconds(grade1to9) + 1e-4f;
    }

    /// <summary>에임 타겟 RectTransform의 크기를 급수에 맞게 설정합니다.</summary>
    public static void ApplyTargetSize(RectTransform aimTargetRect, int grade1to9)
    {
        if (aimTargetRect == null)
        {
            return;
        }

        float side = GetTargetSideLengthPixels(grade1to9);
        aimTargetRect.sizeDelta = new Vector2(side, side);
    }
}
