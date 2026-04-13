using UnityEngine;

/// <summary>
/// 구매한 UI 스킨 보유·장착 상태. PlayerPrefs 저장.
/// </summary>
public static class PlayerCosmetics
{
    private const string EquippedKey = "GSI_Cosmetic_EquippedId";
    private const string OwnedPrefix = "GSI_Cosmetic_Owned_";

    public const string DefaultSkinId = "skin_default";

    public static string EquippedSkinId
    {
        get
        {
            string id = PlayerPrefs.GetString(EquippedKey, DefaultSkinId);
            return string.IsNullOrEmpty(id) ? DefaultSkinId : id;
        }
    }

    private static void SetEquippedSkinId(string skinId)
    {
        PlayerPrefs.SetString(EquippedKey, skinId);
        PlayerPrefs.Save();
    }

    public static bool IsSkinOwned(string skinId)
    {
        if (skinId == DefaultSkinId)
        {
            return true;
        }

        return PlayerPrefs.GetInt(OwnedPrefix + skinId, 0) != 0;
    }

    public static void UnlockSkin(string skinId)
    {
        if (skinId == DefaultSkinId)
        {
            return;
        }

        PlayerPrefs.SetInt(OwnedPrefix + skinId, 1);
        PlayerPrefs.Save();
    }

    public static bool TryEquip(string skinId)
    {
        if (!IsSkinOwned(skinId))
        {
            return false;
        }

        SetEquippedSkinId(skinId);
        return true;
    }

    /// <summary>스킨별 스와치(무채색 명도만 구분 — UI 전역은 회색 스케일).</summary>
    public static Color GetAccentColor(string skinId)
    {
        switch (skinId)
        {
            case "skin_ocean":
                return new Color(0.62f, 0.62f, 0.63f, 1f);
            case "skin_amber":
                return new Color(0.52f, 0.52f, 0.53f, 1f);
            case "skin_violet":
                return new Color(0.66f, 0.66f, 0.67f, 1f);
            default:
                return new Color(0.72f, 0.72f, 0.73f, 1f);
        }
    }
}
