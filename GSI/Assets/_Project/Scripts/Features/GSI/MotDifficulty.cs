using UnityEngine;

/// <summary>
/// 다중 추적(MOT): 연습 급수에 따라 추적 대상 수·방해 원 수·이동 속도·추적 시간이 변합니다.
/// 1급이 가장 어렵고 9급이 가장 쉽습니다. 원은 항상 플레이 영역 안에 머뭅니다.
/// </summary>
public static class MotDifficulty
{
    /// <summary>연습/시험 공통: 급수로 파라미터 산출.</summary>
    public static void GetTrackingParams(int grade1to9, out int targetCount, out int distractorCount, out float speed, out float motionDurationSec, out float cueHoldSec)
    {
        float t = PracticeDifficulty.Ease01(grade1to9);
        targetCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(4, 2, t)), 2, 5);
        distractorCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(10, 3, t)), 2, 14);
        speed = Mathf.Lerp(125f, 42f, t);
        motionDurationSec = Mathf.Lerp(5.4f, 2.3f, t);
        cueHoldSec = Mathf.Lerp(2.4f, 1.5f, t);
    }

    public static int GetExamTrialCount()
    {
        return 3;
    }
}
