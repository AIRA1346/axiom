using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비·상점·인벤토리·G.S.I 허브 등에서 공통으로 쓰는 Canvas/헤더/서브바/스크롤 수치입니다.
/// </summary>
public static class GsiUiScreenLayout
{
    public const float CanvasMatchWidthOrHeight = 0.5f;

    public static readonly Vector2 CanvasReferenceResolution = new Vector2(1920f, 1080f);

    /// <summary>상단 헤더 스트립 높이(상점/인벤/로비 헤더 바).</summary>
    public const float HeaderStripHeight = 88f;

    public static readonly RectOffset HeaderStripPadding = new RectOffset(28, 28, 14, 14);

    public const float HeaderRowSpacing = 16f;

    /// <summary>헤더 중앙 화면 제목(Shop / Inventory / 로비 브랜딩 등).</summary>
    public const float ScreenTitleFontSize = 31f;

    /// <summary>화면 제목 자간(크롬을 덜 강조).</summary>
    public const float ScreenTitleCharacterSpacing = -0.35f;

    /// <summary>골드·응시권 한 줄(헤더 내 또는 SubBar).</summary>
    public const float EconomyLineFontSize = 23f;

    public const float SubBarHeight = 48f;

    /// <summary>서브바를 헤더 바로 아래에 붙일 때 Y 앵커 오프셋.</summary>
    public const float SubBarOffsetBelowHeader = -88f;

    public static readonly RectOffset SubBarPadding = new RectOffset(40, 40, 4, 12);

    public const float SubBarItemSpacing = 48f;

    public const float SettingsHeaderButtonWidth = 140f;

    public const float BackHeaderButtonWidth = 160f;

    public const float ScrollHorizontalInset = 48f;

    public const float ScrollBottomInset = 44f;

    /// <summary>헤더 + 서브바 아래로 리스트가 시작되도록 하는 상단 inset(양수: 캔버스 위에서 아래로).</summary>
    public static float ScrollContentTopMargin => HeaderStripHeight + SubBarHeight + 20f;

    public const float ListVerticalSpacing = 10f;

    public static readonly RectOffset ListPadding = new RectOffset(8, 8, 10, 12);

    /// <summary>상점·인벤 리스트 행 제목 글자 크기.</summary>
    public const float ListRowTitleFontSize = 23f;

    /// <summary>상점 상품 행(응시권·스킨) 기본 높이.</summary>
    public const float ShopOfferRowPreferredHeight = 84f;

    /// <summary>상점 상품 행 내부 여백.</summary>
    public static readonly RectOffset ShopOfferRowPadding = new RectOffset(24, 24, 14, 14);

    /// <summary>인벤토리 스킨 행 기본 높이(상점과 시각적 정렬).</summary>
    public const float InventorySkinRowPreferredHeight = 84f;

    /// <summary>인벤토리 스킨 행 내부 여백.</summary>
    public static readonly RectOffset InventorySkinRowPadding = new RectOffset(24, 24, 14, 14);

    /// <summary>로비 헤더 바 안쪽 <c>HeaderInner</c> 여백(Shop 헤더 HLG 패딩과 시각적으로 맞춤).</summary>
    public static readonly Vector2 LobbyHeaderInnerOffsetMin = new Vector2(32f, 16f);

    public static readonly Vector2 LobbyHeaderInnerOffsetMax = new Vector2(-32f, -16f);

    public static void ApplyCanvasScaler(CanvasScaler scaler)
    {
        if (scaler == null)
        {
            return;
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = CanvasReferenceResolution;
        scaler.matchWidthOrHeight = CanvasMatchWidthOrHeight;
    }

    /// <summary>화면 제목 TMP에 공통 타이포(크기·볼드)를 맞춥니다. 색은 호출부에서 <see cref="GsiUiAppearance"/> 로 적용합니다.</summary>
    public static void ApplyScreenTitleTypography(TextMeshProUGUI tmp)
    {
        if (tmp == null)
        {
            return;
        }

        tmp.fontSize = ScreenTitleFontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.characterSpacing = ScreenTitleCharacterSpacing;
    }
}
