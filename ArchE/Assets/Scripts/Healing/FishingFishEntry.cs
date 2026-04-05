using System;

/// <summary>
/// 판타지 낚시 어종 한 건. 특정 국가·현실 지명 없이 호수 생태에 맞는 이름만 사용합니다.
/// </summary>
[Serializable]
public struct FishingFishEntry
{
    public string Id;
    public string DisplayName;
    public string Flavor;
    /// <summary>가중치 낚시 난이도(릴링 소요에 반영).</summary>
    public float FightMultiplier;

    public FishingFishEntry(string id, string displayName, string flavor, float fightMultiplier)
    {
        Id = id;
        DisplayName = displayName;
        Flavor = flavor;
        FightMultiplier = fightMultiplier;
    }
}
