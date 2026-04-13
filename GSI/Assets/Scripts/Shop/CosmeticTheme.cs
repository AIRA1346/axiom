using UnityEngine;

/// <summary>
/// 장착 스킨의 UI 스와치(무채색 명도). 헤더/칩 등 구 API 호환용으로 전달되며, 실제 채색은 <see cref="GsiUiAppearance"/> 가 회색으로 고정합니다.
/// </summary>
public static class CosmeticTheme
{
    public static Color UiAccent { get; private set; } = PlayerCosmetics.GetAccentColor(PlayerCosmetics.DefaultSkinId);

    public static void ApplyFromSave()
    {
        UiAccent = PlayerCosmetics.GetAccentColor(PlayerCosmetics.EquippedSkinId);
    }
}
