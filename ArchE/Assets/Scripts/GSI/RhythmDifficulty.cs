using UnityEngine;

/// <summary>
/// 연습 급수(1=최상)에 따라 레인 수·BPM·판정창·노트 낙하 시간을 결정합니다.
/// </summary>
public static class RhythmDifficulty
{
    public static int GetLaneCount(int grade1to9)
    {
        int g = Mathf.Clamp(grade1to9, 1, 9);
        if (g <= 3)
        {
            return 4;
        }

        if (g <= 6)
        {
            return 3;
        }

        return 2;
    }

    public static void GetTimingParams(int grade1to9, out float bpm, out float goodWindowMs, out float travelSeconds)
    {
        float t = PracticeDifficulty.Ease01(grade1to9);
        bpm = Mathf.Lerp(128f, 84f, t);
        goodWindowMs = Mathf.Lerp(48f, 105f, t);
        travelSeconds = Mathf.Lerp(1.55f, 2.15f, t);
    }

    public static int GetPracticeNoteCount()
    {
        return 20;
    }

    public static int GetExamNoteCountPerRound()
    {
        return 16;
    }

    public static int GetExamRounds()
    {
        return 3;
    }
}
