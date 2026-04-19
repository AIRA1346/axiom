using UnityEngine;

/// <summary>
/// Ocean / Amber / Violet UI 스킨: <see cref="GsiUiAppearanceMode"/>와 무관하게 고정 색으로 전체 UI를 채웁니다.
/// 기본(<c>skin_default</c>)은 이 팔레트를 쓰지 않고 다크·라이트만 적용됩니다.
/// </summary>
public static class CosmeticSkinPalettes
{
    private static string _cacheSkinId = "";
    private static bool _cacheValid;
    private static bool _cacheActive;
    private static Palette _cachePalette;

    public readonly struct Palette
    {
        public readonly Color OverlayScrim;
        public readonly Color Panel;
        public readonly Color TextPrimary;
        public readonly Color TextSecondary;
        public readonly Color ChipInactive;
        public readonly Color SecondaryButton;
        public readonly Color PrimaryActionButton;
        public readonly Color ShopScreenBackground;
        public readonly Color ShopRowBackground;
        public readonly Color ShopGoldText;
        public readonly Color ShopTicketText;
        public readonly Color SubBarStripBackground;
        public readonly Color UiHairline;
        public readonly Color UiControlRim;
        public readonly Color PanelOuterRim;
        public readonly Color CardDropShadow;
        public readonly Color ShopHeaderStrip;
        public readonly Color LobbyHeroMultiply;
        public readonly Color LobbyHudGlass;
        public readonly Color LobbyEconomyStripGlass;
        public readonly Color LobbyMainActionFill;
        public readonly Color LobbySideActionFill;
        public readonly Color GradeStepNormal;
        public readonly Color GradeStepSelected;
        public readonly Color ChipSelected;

        public Palette(
            Color overlayScrim,
            Color panel,
            Color textPrimary,
            Color textSecondary,
            Color chipInactive,
            Color secondaryButton,
            Color primaryActionButton,
            Color shopScreenBackground,
            Color shopRowBackground,
            Color shopGoldText,
            Color shopTicketText,
            Color subBarStripBackground,
            Color uiHairline,
            Color uiControlRim,
            Color panelOuterRim,
            Color cardDropShadow,
            Color shopHeaderStrip,
            Color lobbyHeroMultiply,
            Color lobbyHudGlass,
            Color lobbyEconomyStripGlass,
            Color lobbyMainActionFill,
            Color lobbySideActionFill,
            Color gradeStepNormal,
            Color gradeStepSelected,
            Color chipSelected)
        {
            OverlayScrim = overlayScrim;
            Panel = panel;
            TextPrimary = textPrimary;
            TextSecondary = textSecondary;
            ChipInactive = chipInactive;
            SecondaryButton = secondaryButton;
            PrimaryActionButton = primaryActionButton;
            ShopScreenBackground = shopScreenBackground;
            ShopRowBackground = shopRowBackground;
            ShopGoldText = shopGoldText;
            ShopTicketText = shopTicketText;
            SubBarStripBackground = subBarStripBackground;
            UiHairline = uiHairline;
            UiControlRim = uiControlRim;
            PanelOuterRim = panelOuterRim;
            CardDropShadow = cardDropShadow;
            ShopHeaderStrip = shopHeaderStrip;
            LobbyHeroMultiply = lobbyHeroMultiply;
            LobbyHudGlass = lobbyHudGlass;
            LobbyEconomyStripGlass = lobbyEconomyStripGlass;
            LobbyMainActionFill = lobbyMainActionFill;
            LobbySideActionFill = lobbySideActionFill;
            GradeStepNormal = gradeStepNormal;
            GradeStepSelected = gradeStepSelected;
            ChipSelected = chipSelected;
        }
    }

    public static void InvalidateCache()
    {
        _cacheValid = false;
    }

    public static bool TryGetActive(out Palette palette)
    {
        string id = PlayerCosmetics.EquippedSkinId;
        if (_cacheValid && id == _cacheSkinId)
        {
            palette = _cachePalette;
            return _cacheActive;
        }

        _cacheSkinId = id;
        _cacheValid = true;
        if (TryResolve(id, out _cachePalette))
        {
            _cacheActive = true;
            palette = _cachePalette;
            return true;
        }

        _cacheActive = false;
        palette = default;
        return false;
    }

    public static bool IsFixedPaletteEquipped()
    {
        return TryGetActive(out _);
    }

    private static bool TryResolve(string skinId, out Palette p)
    {
        switch (skinId)
        {
            case PlayerCosmetics.SkinOceanId:
                p = BuildOcean();
                return true;
            case PlayerCosmetics.SkinAmberId:
                p = BuildAmber();
                return true;
            case PlayerCosmetics.SkinVioletId:
                p = BuildViolet();
                return true;
            default:
                p = default;
                return false;
        }
    }

    private static Palette BuildOcean()
    {
        Color bg = new Color(0.034f, 0.072f, 0.09f, 1f);
        Color row = new Color(0.042f, 0.085f, 0.105f, 1f);
        Color head = new Color(0.038f, 0.078f, 0.1f, 1f);
        Color text = new Color(0.9f, 0.96f, 0.99f, 1f);
        Color sub = new Color(0.48f, 0.72f, 0.8f, 1f);
        Color gold = new Color(0.95f, 0.86f, 0.55f, 1f);
        Color ticket = new Color(0.52f, 0.88f, 0.95f, 1f);
        Color gradeN = new Color(0.07f, 0.14f, 0.18f, 1f);
        Color gradeS = new Color(0.22f, 0.62f, 0.74f, 1f);
        Color cta = new Color(0.12f, 0.55f, 0.68f, 0.58f);
        Color side = new Color(1f, 1f, 1f, 0.14f);
        return new Palette(
            overlayScrim: new Color(0f, 0f, 0f, 0.45f),
            panel: new Color(0.04f, 0.08f, 0.11f, 0.82f),
            textPrimary: text,
            textSecondary: sub,
            chipInactive: new Color(1f, 1f, 1f, 0.06f),
            secondaryButton: new Color(1f, 1f, 1f, 0.09f),
            primaryActionButton: new Color(0.2f, 0.78f, 0.9f, 0.22f),
            shopScreenBackground: bg,
            shopRowBackground: row,
            shopGoldText: gold,
            shopTicketText: ticket,
            subBarStripBackground: head,
            uiHairline: new Color(1f, 1f, 1f, 0.05f),
            uiControlRim: new Color(1f, 1f, 1f, 0f),
            panelOuterRim: new Color(1f, 1f, 1f, 0f),
            cardDropShadow: new Color(0f, 0f, 0f, 0f),
            shopHeaderStrip: head,
            lobbyHeroMultiply: new Color(0.45f, 0.65f, 0.72f, 0.82f),
            lobbyHudGlass: new Color(0.05f, 0.1f, 0.14f, 0.76f),
            lobbyEconomyStripGlass: new Color(1f, 1f, 1f, 0.1f),
            lobbyMainActionFill: cta,
            lobbySideActionFill: side,
            gradeStepNormal: gradeN,
            gradeStepSelected: gradeS,
            chipSelected: new Color(1f, 1f, 1f, 0.16f));
    }

    private static Palette BuildAmber()
    {
        Color bg = new Color(0.1f, 0.06f, 0.035f, 1f);
        Color row = new Color(0.12f, 0.075f, 0.045f, 1f);
        Color head = new Color(0.11f, 0.065f, 0.038f, 1f);
        Color text = new Color(0.98f, 0.94f, 0.88f, 1f);
        Color sub = new Color(0.78f, 0.58f, 0.4f, 1f);
        Color gold = new Color(1f, 0.82f, 0.42f, 1f);
        Color ticket = new Color(0.95f, 0.7f, 0.52f, 1f);
        Color gradeN = new Color(0.16f, 0.1f, 0.06f, 1f);
        Color gradeS = new Color(0.78f, 0.48f, 0.18f, 1f);
        Color cta = new Color(0.85f, 0.45f, 0.12f, 0.55f);
        Color side = new Color(1f, 1f, 1f, 0.12f);
        return new Palette(
            overlayScrim: new Color(0f, 0f, 0f, 0.48f),
            panel: new Color(0.12f, 0.07f, 0.04f, 0.84f),
            textPrimary: text,
            textSecondary: sub,
            chipInactive: new Color(1f, 1f, 1f, 0.055f),
            secondaryButton: new Color(1f, 1f, 1f, 0.085f),
            primaryActionButton: new Color(1f, 0.62f, 0.22f, 0.24f),
            shopScreenBackground: bg,
            shopRowBackground: row,
            shopGoldText: gold,
            shopTicketText: ticket,
            subBarStripBackground: head,
            uiHairline: new Color(1f, 1f, 1f, 0.045f),
            uiControlRim: new Color(1f, 1f, 1f, 0f),
            panelOuterRim: new Color(1f, 1f, 1f, 0f),
            cardDropShadow: new Color(0f, 0f, 0f, 0f),
            shopHeaderStrip: head,
            lobbyHeroMultiply: new Color(0.85f, 0.62f, 0.45f, 0.78f),
            lobbyHudGlass: new Color(0.14f, 0.08f, 0.05f, 0.75f),
            lobbyEconomyStripGlass: new Color(1f, 1f, 1f, 0.09f),
            lobbyMainActionFill: cta,
            lobbySideActionFill: side,
            gradeStepNormal: gradeN,
            gradeStepSelected: gradeS,
            chipSelected: new Color(1f, 1f, 1f, 0.14f));
    }

    private static Palette BuildViolet()
    {
        Color bg = new Color(0.065f, 0.038f, 0.11f, 1f);
        Color row = new Color(0.078f, 0.045f, 0.125f, 1f);
        Color head = new Color(0.07f, 0.04f, 0.115f, 1f);
        Color text = new Color(0.95f, 0.92f, 0.99f, 1f);
        Color sub = new Color(0.68f, 0.58f, 0.86f, 1f);
        Color gold = new Color(0.98f, 0.82f, 0.55f, 1f);
        Color ticket = new Color(0.78f, 0.68f, 1f, 1f);
        Color gradeN = new Color(0.1f, 0.06f, 0.16f, 1f);
        Color gradeS = new Color(0.62f, 0.38f, 0.92f, 1f);
        Color cta = new Color(0.48f, 0.22f, 0.82f, 0.52f);
        Color side = new Color(1f, 1f, 1f, 0.11f);
        return new Palette(
            overlayScrim: new Color(0f, 0f, 0f, 0.46f),
            panel: new Color(0.08f, 0.05f, 0.12f, 0.83f),
            textPrimary: text,
            textSecondary: sub,
            chipInactive: new Color(1f, 1f, 1f, 0.055f),
            secondaryButton: new Color(1f, 1f, 1f, 0.085f),
            primaryActionButton: new Color(0.72f, 0.45f, 1f, 0.22f),
            shopScreenBackground: bg,
            shopRowBackground: row,
            shopGoldText: gold,
            shopTicketText: ticket,
            subBarStripBackground: head,
            uiHairline: new Color(1f, 1f, 1f, 0.045f),
            uiControlRim: new Color(1f, 1f, 1f, 0f),
            panelOuterRim: new Color(1f, 1f, 1f, 0f),
            cardDropShadow: new Color(0f, 0f, 0f, 0f),
            shopHeaderStrip: head,
            lobbyHeroMultiply: new Color(0.72f, 0.58f, 0.88f, 0.8f),
            lobbyHudGlass: new Color(0.1f, 0.06f, 0.16f, 0.76f),
            lobbyEconomyStripGlass: new Color(1f, 1f, 1f, 0.09f),
            lobbyMainActionFill: cta,
            lobbySideActionFill: side,
            gradeStepNormal: gradeN,
            gradeStepSelected: gradeS,
            chipSelected: new Color(1f, 1f, 1f, 0.15f));
    }
}
