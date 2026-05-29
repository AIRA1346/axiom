using UnityEngine;

/// <summary>
/// 연습 모드 전용: 1급(가장 어려움) ~ 9급(가장 쉬움). 공식 시험은 이 값을 사용하지 않습니다.
/// </summary>
public static class PracticeDifficulty
{
    /// <summary>0 = 1급(어려움), 1 = 9급(쉬움)</summary>
    public static float Ease01(int grade1to9)
    {
        int g = Mathf.Clamp(grade1to9, 1, 9);
        return (g - 1) / 8f;
    }

    public static string GradeShortLabel(int grade)
    {
        int g = Mathf.Clamp(grade, 1, 9);
        if (g <= 2)
        {
            return GameLocalization.GetUiString(UiStringKeys.PracticeTierTop, "Top");
        }

        if (g <= 4)
        {
            return GameLocalization.GetUiString(UiStringKeys.PracticeTierHigh, "High");
        }

        if (g <= 6)
        {
            return GameLocalization.GetUiString(UiStringKeys.PracticeTierMid, "Mid");
        }

        if (g <= 8)
        {
            return GameLocalization.GetUiString(UiStringKeys.PracticeTierLow, "Low");
        }

        return GameLocalization.GetUiString(UiStringKeys.PracticeTierEntry, "Entry");
    }

    /// <summary>초록 신호 전 대기 시간 범위(짧을수록 긴장·실수 유발이 커짐).</summary>
    public static void GetReactionDelayRange(int grade, out float minSeconds, out float maxSeconds)
    {
        float t = Ease01(grade);
        minSeconds = Mathf.Lerp(0.85f, 2.4f, t);
        maxSeconds = Mathf.Lerp(2.4f, 6.5f, t);
        if (maxSeconds < minSeconds + 0.15f)
        {
            maxSeconds = minSeconds + 0.35f;
        }
    }

    /// <summary>목표 개수(많을수록 어려움), 빗나감 페널티, 이동 반경.</summary>
    public static void GetAimPracticeParams(int grade, out int targetsRequired, out float missPenaltySeconds, out float moveRangeX, out float moveRangeY)
    {
        float t = Ease01(grade);
        targetsRequired = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(9, 4, t)), 3, 12);
        missPenaltySeconds = Mathf.Lerp(0.85f, 0.22f, t);
        moveRangeX = Mathf.Lerp(520f, 280f, t);
        moveRangeY = Mathf.Lerp(280f, 130f, t);
    }

    /// <summary>패턴 표시 속도(짧을수록 어려움).</summary>
    public static void GetMemoryTiming(int grade, out float showCellSeconds, out float betweenShowSeconds)
    {
        float t = Ease01(grade);
        showCellSeconds = Mathf.Lerp(0.21f, 0.55f, t);
        betweenShowSeconds = Mathf.Lerp(0.055f, 0.16f, t);
    }
}
