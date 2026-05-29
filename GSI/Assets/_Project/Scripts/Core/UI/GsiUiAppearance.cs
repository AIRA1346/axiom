using System;
using UnityEngine;
using ArchE.Game;

/// <summary>
/// UI 다크/라이트 모드. 디자인 방향: **크롬 최소화** - 배경·헤더·행이 거의 구분되지 않도록 톤을 낮추고,
/// 글자·버튼만 도드라지게 연출하는 느낌(네오모피즘/미니멀)에 가깝게 맞춥니다.
/// Ocean/Amber/Violet 장착 시에는 <see cref="CosmeticSkinPalettes"/>가 모드를 대체합니다.
/// (각 API는 <c>accent</c> 컬러를 매개변수로 받지만 기본 스킨에서는 배합만 바꿉니다)
/// </summary>
public enum GsiUiAppearanceMode
{
    Dark = 0,
    Light = 1
}

public static class GsiUiAppearance
{
    private const string PrefsKey = "GSI_UiAppearanceMode";

    public static GsiUiAppearanceMode Mode { get; private set; } = GsiUiAppearanceMode.Dark;

    public static event Action Changed;

    static GsiUiAppearance()
    {
        Load();
    }

    public static void Load()
    {
        Mode = (GsiUiAppearanceMode)Mathf.Clamp(GsiSaveSystem.GetInt(PrefsKey, 0), 0, 1);
    }

    public static void SetMode(GsiUiAppearanceMode mode)
    {
        if (Mode == mode)
        {
            return;
        }

        Mode = mode;
        GsiSaveSystem.SetInt(PrefsKey, (int)mode);
        GsiSaveSystem.Save();
        Changed?.Invoke();
    }

    /// <summary>스킨 장착 상태에서 파레트만 바뀔 때 UI를 강제 갱신하기 위해 호출합니다.</summary>
    public static void RaiseChanged()
    {
        Changed?.Invoke();
    }

    private static bool TryPalette(out CosmeticSkinPalettes.Palette p)
    {
        return CosmeticSkinPalettes.TryGetActive(out p);
    }

    public static Color OverlayScrim =>
        TryPalette(out var pal)
            ? pal.OverlayScrim
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(0f, 0f, 0f, 0.38f)
                : new Color(0f, 0f, 0f, 0.32f);

    /// <summary>설정 등 모달 본문 - 흐릿하지만 불투명한 글래스(프레임 최소화).</summary>
    public static Color Panel =>
        TryPalette(out var pal)
            ? pal.Panel
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(0.04f, 0.042f, 0.05f, 0.78f)
                : new Color(0.97f, 0.97f, 0.98f, 0.92f);

    public static Color TextPrimary =>
        TryPalette(out var pal)
            ? pal.TextPrimary
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(0.88f, 0.89f, 0.91f, 1f)
                : new Color(0.12f, 0.12f, 0.13f, 1f);

    public static Color TextSecondary =>
        TryPalette(out var pal)
            ? pal.TextSecondary
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(0.48f, 0.5f, 0.54f, 1f)
                : new Color(0.44f, 0.45f, 0.47f, 1f);

    public static Color ChipInactive =>
        TryPalette(out var pal)
            ? pal.ChipInactive
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(1f, 1f, 1f, 0.05f)
                : new Color(0f, 0f, 0f, 0.06f);

    /// <summary>보조 버튼 테두리 느낌 - 매우 낮은 불투명도.</summary>
    public static Color SecondaryButton =>
        TryPalette(out var pal)
            ? pal.SecondaryButton
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(1f, 1f, 1f, 0.07f)
                : new Color(0f, 0f, 0f, 0.07f);

    /// <summary>Buy/Equip 등 주요 액션 - 살짝 밝게 보이도록.</summary>
    public static Color PrimaryActionButton =>
        TryPalette(out var pal)
            ? pal.PrimaryActionButton
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(1f, 1f, 1f, 0.14f)
                : new Color(0f, 0f, 0f, 0.12f);

    /// <summary>상점·인벤 캔버스 배경(거의 불투명).</summary>
    public static Color ShopScreenBackground =>
        TryPalette(out var pal)
            ? pal.ShopScreenBackground
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(0.024f, 0.026f, 0.032f, 1f)
                : new Color(0.93f, 0.932f, 0.94f, 1f);

    /// <summary>
    /// 3D 필드(메인 카메라) 테두리 - 디바이스 및 스킨별 UI 배경(<see cref="ShopScreenBackground"/>)과 일치시킴.
    /// </summary>
    public static Color GameplayWorldBackground => ShopScreenBackground;

    /// <summary>리스트 아이템 배경 - 거의 투명(구분은 약간의 명도 차이로만).</summary>
    public static Color ShopRowBackground =>
        TryPalette(out var pal)
            ? pal.ShopRowBackground
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(0.026f, 0.028f, 0.034f, 1f)
                : new Color(0.96f, 0.962f, 0.97f, 1f);

    public static Color ShopGoldText =>
        TryPalette(out var pal)
            ? pal.ShopGoldText
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(0.72f, 0.74f, 0.78f, 1f)
                : new Color(0.32f, 0.32f, 0.33f, 1f);

    public static Color ShopTicketText =>
        TryPalette(out var pal)
            ? pal.ShopTicketText
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(0.52f, 0.54f, 0.58f, 1f)
                : new Color(0.38f, 0.38f, 0.4f, 1f);

    /// <summary>서브 바 스트립 배경(구분용이지만 거의 안 보임).</summary>
    public static Color SubBarStripBackground =>
        TryPalette(out var pal)
            ? pal.SubBarStripBackground
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(0.024f, 0.026f, 0.032f, 1f)
                : new Color(0.93f, 0.932f, 0.94f, 1f);

    /// <summary>구분선용 - 거의 투명.</summary>
    public static Color UiHairline =>
        TryPalette(out var pal)
            ? pal.UiHairline
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(1f, 1f, 1f, 0.02f)
                : new Color(0f, 0f, 0f, 0.03f);

    /// <summary>컨트롤 외곽선(미세하게).</summary>
    public static Color UiControlRim =>
        TryPalette(out var pal)
            ? pal.UiControlRim
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(1f, 1f, 1f, 0f)
                : new Color(0f, 0f, 0f, 0f);

    /// <summary>패널 외곽선 - 미세하게.</summary>
    public static Color PanelOuterRim =>
        TryPalette(out var pal)
            ? pal.PanelOuterRim
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(1f, 1f, 1f, 0f)
                : new Color(0f, 0f, 0f, 0f);

    /// <summary>카드 그림자 - 미니멀 모드에서는 거의 사용 안 함.</summary>
    public static Color CardDropShadow =>
        TryPalette(out var pal)
            ? pal.CardDropShadow
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(0f, 0f, 0f, 0f)
                : new Color(0f, 0f, 0f, 0f);

    /// <summary>상단 헤더 스트립 - 화면 배경과 동일(액센트 적용은 기본 스킨에서 무시).</summary>
    public static Color ShopHeaderStrip(Color _)
    {
        if (TryPalette(out var pal))
        {
            return pal.ShopHeaderStrip;
        }

        return Mode == GsiUiAppearanceMode.Dark
            ? new Color(0.024f, 0.026f, 0.032f, 1f)
            : new Color(0.93f, 0.932f, 0.94f, 1f);
    }

    public static Color LobbyHeroMultiply =>
        TryPalette(out var pal)
            ? pal.LobbyHeroMultiply
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(0.55f, 0.55f, 0.58f, 0.85f)
                : new Color(0.92f, 0.92f, 0.93f, 0.9f);

    /// <summary>로비 베스트 기록 상단 HUD 글래스 패널.</summary>
    public static Color LobbyHudGlass =>
        TryPalette(out var pal)
            ? pal.LobbyHudGlass
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(0.05f, 0.055f, 0.08f, 0.72f)
                : new Color(1f, 1f, 1f, 0.78f);

    /// <summary>로비 상단 골드·티켓 등 배경.</summary>
    public static Color LobbyEconomyStripGlass =>
        TryPalette(out var pal)
            ? pal.LobbyEconomyStripGlass
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(1f, 1f, 1f, 0.09f)
                : new Color(0f, 0f, 0f, 0.06f);

    /// <summary>로비 메인 CTA(Start) 버튼 글래스.</summary>
    public static Color LobbyMainActionFill =>
        TryPalette(out var pal)
            ? pal.LobbyMainActionFill
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(0.14f, 0.42f, 0.52f, 0.62f)
                : new Color(0.2f, 0.5f, 0.58f, 0.45f);

    /// <summary>로비 보조 버튼(Shop / Inventory).</summary>
    public static Color LobbySideActionFill =>
        TryPalette(out var pal)
            ? pal.LobbySideActionFill
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(1f, 1f, 1f, 0.13f)
                : new Color(0f, 0f, 0f, 0.1f);

    /// <summary>로비 주요 CTA(액센트 적용은 기본 스킨에서 무시).</summary>
    public static Color LobbyPrimaryCta(Color _)
    {
        return LobbyMainActionFill;
    }

    public static Color GradeStepNormal =>
        TryPalette(out var pal)
            ? pal.GradeStepNormal
            : Mode == GsiUiAppearanceMode.Dark
                ? new Color(0.1f, 0.1f, 0.1f, 1f)
                : new Color(0.84f, 0.84f, 0.85f, 1f);

    /// <summary>등급 칩 선택(액센트 적용은 기본 스킨에서 무시).</summary>
    public static Color GradeStepSelectedFromAccent(Color _)
    {
        if (TryPalette(out var pal))
        {
            return pal.GradeStepSelected;
        }

        return Mode == GsiUiAppearanceMode.Dark
            ? new Color(0.26f, 0.26f, 0.27f, 1f)
            : new Color(0.58f, 0.58f, 0.6f, 1f);
    }

    /// <summary>일반 칩 선택 배경(액센트 적용은 기본 스킨에서 무시).</summary>
    public static Color ChipSelectedFromAccent(Color _)
    {
        if (TryPalette(out var pal))
        {
            return pal.ChipSelected;
        }

        return Mode == GsiUiAppearanceMode.Dark
            ? new Color(1f, 1f, 1f, 0.12f)
            : new Color(0f, 0f, 0f, 0.1f);
    }
}
