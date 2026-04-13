using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 상점·인벤토리 등 동일한 패턴의 런타임 Canvas(배경 + 헤더 + SubBar) 참조 해결, 헤더 자식 순서, 버튼 리스너 연결.
/// </summary>
public static class GsiShopLikeCanvasHeaderUi
{
    public const string HeaderSettingsName = "HeaderSettingsButton";
    public const string HeaderTitleName = "HeaderTitle";
    public const string HeaderBackName = "HeaderBackButton";

    public struct CanvasRefs
    {
        public Image BackgroundImage;
        public Image HeaderStripImage;
        public Button SettingsButton;
        public TextMeshProUGUI TitleTmp;
        public Button BackButton;
        public TextMeshProUGUI GoldText;
        public TextMeshProUGUI TicketText;

        public readonly bool IsComplete =>
            BackgroundImage != null
            && HeaderStripImage != null
            && TitleTmp != null
            && BackButton != null
            && GoldText != null
            && TicketText != null;
    }

    /// <summary>
    /// <paramref name="sceneRoot"/> 아래 <paramref name="canvasChildName"/> (예: ShopCanvas)를 찾아 UI 참조를 채웁니다.
    /// </summary>
    public static bool TryResolve(Transform sceneRoot, string canvasChildName, out CanvasRefs refs)
    {
        refs = default;

        Transform canvas = sceneRoot.Find(canvasChildName);
        if (canvas == null)
        {
            return false;
        }

        Transform bg = canvas.Find("Background");
        if (bg != null)
        {
            refs.BackgroundImage = bg.GetComponent<Image>();
        }

        Transform header = canvas.Find("Header");
        if (header == null)
        {
            return false;
        }

        refs.HeaderStripImage = header.GetComponent<Image>();

        if (header.childCount >= 3
            && header.Find(HeaderSettingsName) == null
            && header.GetChild(0).name == "Button"
            && header.GetChild(1).name == "Text"
            && header.GetChild(2).name == "Button")
        {
            header.GetChild(0).name = HeaderSettingsName;
            header.GetChild(1).name = HeaderTitleName;
            header.GetChild(2).name = HeaderBackName;
        }

        Transform s = header.Find(HeaderSettingsName);
        Transform t = header.Find(HeaderTitleName);
        Transform b = header.Find(HeaderBackName);
        if (s != null)
        {
            refs.SettingsButton = s.GetComponent<Button>();
        }

        if (t != null)
        {
            refs.TitleTmp = t.GetComponent<TextMeshProUGUI>();
        }

        if (b != null)
        {
            refs.BackButton = b.GetComponent<Button>();
        }

        if (refs.SettingsButton == null && header.childCount > 0)
        {
            refs.SettingsButton = header.GetChild(0).GetComponent<Button>();
        }

        if (refs.TitleTmp == null && header.childCount > 1)
        {
            refs.TitleTmp = header.GetChild(1).GetComponent<TextMeshProUGUI>();
        }

        if (refs.BackButton == null && header.childCount > 2)
        {
            refs.BackButton = header.GetChild(2).GetComponent<Button>();
        }

        Transform sub = canvas.Find("SubBar");
        if (sub != null && sub.childCount >= 2)
        {
            refs.GoldText = sub.GetChild(0).GetComponent<TextMeshProUGUI>();
            refs.TicketText = sub.GetChild(1).GetComponent<TextMeshProUGUI>();
        }

        if (refs.TitleTmp != null)
        {
            refs.TitleTmp.raycastTarget = false;
        }

        return refs.IsComplete;
    }

    /// <summary>레거시 씬에 남아 있을 수 있는 헤더 설정 버튼을 제거합니다(ESC로 설정).</summary>
    public static void DestroyHeaderSettingsButtonIfPresent(Transform sceneRoot, string canvasChildName)
    {
        Transform t = sceneRoot.Find($"{canvasChildName}/Header/{HeaderSettingsName}");
        if (t != null)
        {
            Object.Destroy(t.gameObject);
        }
    }

    public static void EnforceHeaderChildLayoutOrder(Transform sceneRoot, string canvasChildName)
    {
        Transform header = sceneRoot.Find(canvasChildName + "/Header");
        if (header == null)
        {
            return;
        }

        Transform hs = header.Find(HeaderSettingsName);
        Transform ht = header.Find(HeaderTitleName);
        Transform hb = header.Find(HeaderBackName);
        if (ht == null || hb == null)
        {
            return;
        }

        if (hs != null)
        {
            hs.SetSiblingIndex(0);
            ht.SetSiblingIndex(1);
        }
        else
        {
            ht.SetSiblingIndex(0);
        }

        hb.SetAsLastSibling();
        ApplyHeaderStripFlexibleTitleLayout(header);
    }

    /// <summary>
    /// 헤더 가로 레이아웃에서 제목이 남는 폭을 차지하도록 해 Back 버튼이 스트립 오른쪽 끝에 붙습니다.
    /// (<see cref="HorizontalLayoutGroup.childForceExpandWidth"/> 가 꺼져 있으면 flexibleWidth 가 적용되지 않습니다.)
    /// </summary>
    public static void ApplyHeaderStripFlexibleTitleLayout(Transform header)
    {
        if (header == null)
        {
            return;
        }

        HorizontalLayoutGroup hlg = header.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.childForceExpandWidth = true;
            hlg.childControlWidth = true;
        }

        Transform hs = header.Find(HeaderSettingsName);
        if (hs != null)
        {
            LayoutElement le = hs.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = hs.gameObject.AddComponent<LayoutElement>();
            }

            le.flexibleWidth = 0f;
            le.preferredWidth = GsiUiScreenLayout.SettingsHeaderButtonWidth;
        }

        Transform ht = header.Find(HeaderTitleName);
        if (ht != null)
        {
            LayoutElement le = ht.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = ht.gameObject.AddComponent<LayoutElement>();
            }

            le.flexibleWidth = 1f;
        }

        Transform hb = header.Find(HeaderBackName);
        if (hb != null)
        {
            LayoutElement le = hb.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = hb.gameObject.AddComponent<LayoutElement>();
            }

            le.flexibleWidth = 0f;
            le.preferredWidth = GsiUiScreenLayout.BackHeaderButtonWidth;
        }
    }

    public static void WireHeaderListeners(Button settingsButton, Button backButton, UnityAction onBackClicked)
    {
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(() => GlobalSettingsOverlay.OpenSettings());
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(onBackClicked);
        }
    }

    /// <summary>
    /// 언어 전환·외관 갱신 후에도 헤더 TMP가 최초 빌드 문자열에 묶이지 않도록 현재 로케일로 다시 채웁니다.
    /// </summary>
    public static void RefreshShopHeaderLocalizedTexts(TextMeshProUGUI titleTmp, Button settingsButton, Button backButton)
    {
        ApplyLocalizedHeaderTexts(titleTmp, settingsButton, backButton,
            UiStringKeys.ShopTitle, "Shop",
            UiStringKeys.ShopBack, "Back");
    }

    public static void RefreshInventoryHeaderLocalizedTexts(TextMeshProUGUI titleTmp, Button settingsButton, Button backButton)
    {
        ApplyLocalizedHeaderTexts(titleTmp, settingsButton, backButton,
            UiStringKeys.InventoryTitle, "Inventory",
            UiStringKeys.InventoryBack, "Back");
    }

    private static void ApplyLocalizedHeaderTexts(TextMeshProUGUI titleTmp, Button settingsButton, Button backButton,
        string titleKey, string titleFallback, string backKey, string backFallback)
    {
        if (titleTmp != null)
        {
            titleTmp.text = GameLocalization.GetUiString(titleKey, titleFallback);
        }

        SetHeaderButtonLabel(settingsButton, UiStringKeys.SettingsOpen, "Settings");
        SetHeaderButtonLabel(backButton, backKey, backFallback);
    }

    private static void SetHeaderButtonLabel(Button button, string key, string englishFallback)
    {
        if (button == null)
        {
            return;
        }

        TextMeshProUGUI tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = GameLocalization.GetUiString(key, englishFallback);
        }
    }
}
