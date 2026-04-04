using UnityEngine;

/// <summary>
/// 힐링 낚시 누적(로컬 저장). 시험·경제와 분리합니다.
/// </summary>
public static class HealingFishingProgress
{
    private const string TotalCaughtKey = "HealingFishing_TotalCaught";
    private const string VisitsKey = "HealingFishing_LakeVisits";

    public static int TotalCaught => PlayerPrefs.GetInt(TotalCaughtKey, 0);

    public static int LakeVisits => PlayerPrefs.GetInt(VisitsKey, 0);

    public static void RegisterCatch()
    {
        PlayerPrefs.SetInt(TotalCaughtKey, TotalCaught + 1);
        PlayerPrefs.Save();
    }

    public static void RegisterLakeVisit()
    {
        PlayerPrefs.SetInt(VisitsKey, LakeVisits + 1);
        PlayerPrefs.Save();
    }
}
