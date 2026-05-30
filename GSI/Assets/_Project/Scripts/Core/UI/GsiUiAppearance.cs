using System;
using UnityEngine;
using ArchE.Game;

/// <summary>
/// GsiUiAppearance: 파편화되었던 라이트/다크 모드 시스템을 완전히 제거하고,
/// 모든 UI 요소의 색상을 인벤토리의 스킨 팔레트(<see cref="CosmeticSkinPalettes"/>) 단일 파이프라인에서 프록시 조회하도록 리팩토링했습니다.
/// 이로써 개별 분기가 소멸하고 오직 단일 테마 바인딩으로 통합 동작합니다.
/// </summary>
public static class GsiUiAppearance
{
    public static event Action Changed;

    /// <summary>테마/스킨 장착 상태가 바뀔 때 UI 강제 리빌드를 트리거합니다.</summary>
    public static void RaiseChanged()
    {
        Changed?.Invoke();
    }

    private static bool TryPalette(out CosmeticSkinPalettes.Palette p)
    {
        return CosmeticSkinPalettes.TryGetActive(out p);
    }

    // 모든 시각 스타일 프로퍼티들의 스킨 팔레트 포워더 (껍데기 호환성 100% 유지)
    public static Color OverlayScrim =>
        TryPalette(out var pal) ? pal.OverlayScrim : new Color(0f, 0f, 0f, 0.38f);

    public static Color Panel =>
        TryPalette(out var pal) ? pal.Panel : new Color(0.04f, 0.042f, 0.05f, 0.78f);

    public static Color TextPrimary =>
        TryPalette(out var pal) ? pal.TextPrimary : new Color(0.88f, 0.89f, 0.91f, 1f);

    public static Color TextSecondary =>
        TryPalette(out var pal) ? pal.TextSecondary : new Color(0.48f, 0.5f, 0.54f, 1f);

    public static Color ChipInactive =>
        TryPalette(out var pal) ? pal.ChipInactive : new Color(1f, 1f, 1f, 0.05f);

    public static Color SecondaryButton =>
        TryPalette(out var pal) ? pal.SecondaryButton : new Color(1f, 1f, 1f, 0.07f);

    public static Color PrimaryActionButton =>
        TryPalette(out var pal) ? pal.PrimaryActionButton : new Color(1f, 1f, 1f, 0.14f);

    public static Color ShopScreenBackground =>
        TryPalette(out var pal) ? pal.ShopScreenBackground : new Color(0.024f, 0.026f, 0.032f, 1f);

    public static Color GameplayWorldBackground => ShopScreenBackground;

    public static Color ShopRowBackground =>
        TryPalette(out var pal) ? pal.ShopRowBackground : new Color(0.026f, 0.028f, 0.034f, 1f);

    public static Color ShopStardustText =>
        TryPalette(out var pal) ? pal.ShopStardustText : new Color(0.72f, 0.74f, 0.78f, 1f);

    public static Color ShopAstralCoreText =>
        TryPalette(out var pal) ? pal.ShopAstralCoreText : new Color(0.4f, 0.85f, 1f, 1f);

    public static Color ShopTicketText =>
        TryPalette(out var pal) ? pal.ShopTicketText : new Color(0.52f, 0.54f, 0.58f, 1f);

    public static Color SubBarStripBackground =>
        TryPalette(out var pal) ? pal.SubBarStripBackground : new Color(0.024f, 0.026f, 0.032f, 1f);

    public static Color UiHairline =>
        TryPalette(out var pal) ? pal.UiHairline : new Color(1f, 1f, 1f, 0.02f);

    public static Color UiControlRim =>
        TryPalette(out var pal) ? pal.UiControlRim : new Color(1f, 1f, 1f, 0f);

    public static Color PanelOuterRim =>
        TryPalette(out var pal) ? pal.PanelOuterRim : new Color(1f, 1f, 1f, 0f);

    public static Color CardDropShadow =>
        TryPalette(out var pal) ? pal.CardDropShadow : new Color(0f, 0f, 0f, 0f);

    public static Color ShopHeaderStrip(Color _)
    {
        return TryPalette(out var pal) ? pal.ShopHeaderStrip : new Color(0.024f, 0.026f, 0.032f, 1f);
    }

    public static Color LobbyHeroMultiply =>
        TryPalette(out var pal) ? pal.LobbyHeroMultiply : new Color(0.55f, 0.55f, 0.58f, 0.85f);

    public static Color LobbyHudGlass =>
        TryPalette(out var pal) ? pal.LobbyHudGlass : new Color(0.05f, 0.055f, 0.08f, 0.72f);

    public static Color LobbyEconomyStripGlass =>
        TryPalette(out var pal) ? pal.LobbyEconomyStripGlass : new Color(1f, 1f, 1f, 0.09f);

    public static Color LobbyMainActionFill =>
        TryPalette(out var pal) ? pal.LobbyMainActionFill : new Color(0.14f, 0.42f, 0.52f, 0.62f);

    public static Color LobbySideActionFill =>
        TryPalette(out var pal) ? pal.LobbySideActionFill : new Color(1f, 1f, 1f, 0.13f);

    public static Color LobbyPrimaryCta(Color _)
    {
        return LobbyMainActionFill;
    }

    public static Color GradeStepNormal =>
        TryPalette(out var pal) ? pal.GradeStepNormal : new Color(0.1f, 0.1f, 0.1f, 1f);

    public static Color GradeStepSelectedFromAccent(Color _)
    {
        return TryPalette(out var pal) ? pal.GradeStepSelected : new Color(0.26f, 0.26f, 0.27f, 1f);
    }

    public static Color ChipSelectedFromAccent(Color _)
    {
        return TryPalette(out var pal) ? pal.ChipSelected : new Color(1f, 1f, 1f, 0.12f);
    }
}