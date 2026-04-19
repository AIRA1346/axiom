using UnityEngine;

/// <summary>
/// 장착 스킨의 UI 액센트 색(미리보기·구 API 호환). 기본 스킨은 무채 명도, Ocean/Amber/Violet은 브랜드 색.
/// 전역 화면 채색은 <see cref="GsiUiAppearance"/> 및 <see cref="CosmeticSkinPalettes"/>가 담당합니다.
/// </summary>
public static class CosmeticTheme
{
    public static Color UiAccent { get; private set; } = PlayerCosmetics.GetAccentColor(PlayerCosmetics.DefaultSkinId);

    public static void ApplyFromSave()
    {
        CosmeticSkinPalettes.InvalidateCache();
        UiAccent = PlayerCosmetics.GetAccentColor(PlayerCosmetics.EquippedSkinId);
        GsiUiAppearance.RaiseChanged();
    }
}
