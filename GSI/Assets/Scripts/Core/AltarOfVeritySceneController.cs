using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// 메인 로비에서 진입하는 통합 시험 응시 기록 화면(Altar of Verity / 진실의 제단).
/// <see cref="PlayerDataManager.GetUnifiedExamHistoryNewestFirst"/> JSON 기록을 목록·상세로 나누어 표시합니다.
/// </summary>
[ExecuteAlways]
public sealed class AltarOfVeritySceneController : MonoBehaviour
{
    private const string CanvasName = "VerityCanvas";

    private TextMeshProUGUI _goldText;
    private TextMeshProUGUI _ticketText;
    private Image _bgImage;
    private Image _subBarStripImage;
    private Image _headerStrip;
    private TextMeshProUGUI _headerTitleTmp;
    private Button _headerBackButton;
    private TextMeshProUGUI _listCaptionTmp;
    private TextMeshProUGUI _detailCaptionTmp;
    private TextMeshProUGUI _detailBodyTmp;
    private Transform _listContent;
    private ScrollRect _listScroll;
    private ScrollRect _detailScroll;
    private readonly List<UnifiedExamHistoryEntry> _entries = new List<UnifiedExamHistoryEntry>();
    private readonly List<(Button Btn, Image Bg)> _sessionRows = new List<(Button, Image)>();
    private int _selectedIndex = -1;
    private bool _uiBuilt;
    private bool _economySubscribed;
    private bool _appearanceSubscribed;
    private bool _localeSubscribed;

    private void Awake()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            BuildUiForEditorSceneView();
            return;
        }
#endif
        GlobalSettingsOverlay.EnsureCreated();
        GsiGameplayWorldCameraHooks.Initialize();
        GsiRuntimeUiBootstrap.EnsureEventSystemForUiScenes();
        GsiRuntimeUiBootstrap.EnsureEconomyManagerForUiScenes();
        StartCoroutine(BuildUiAfterLocalizationCoroutine());
    }

    private void OnEnable()
    {
        if (!_uiBuilt)
        {
            return;
        }

        RefreshHeader();
        RebuildSessionUi();
        TrySubscribeEconomy();
    }

    private void OnDisable()
    {
        TryUnsubscribeEconomy();
        TryUnsubscribeAppearance();
        TryUnsubscribeLocaleChanged();
    }

    private IEnumerator BuildUiAfterLocalizationCoroutine()
    {
        TrySubscribeLocaleChanged();
        Task boot = GameLocalization.InitializeAndApplySavedLocaleAsync();
        while (!boot.IsCompleted)
        {
            yield return null;
        }

        if (boot.IsFaulted && boot.Exception != null)
        {
            Debug.LogWarning("[VerityAltar] Localization init: " + boot.Exception.GetBaseException().Message);
        }

        BuildUi();
        _uiBuilt = true;
        CosmeticTheme.ApplyFromSave();
        ApplyVerityChrome();
        RefreshLocalizedChrome();
        RefreshHeader();
        RebuildSessionUi();
        TrySubscribeEconomy();
        TrySubscribeAppearance();
    }

    private void OnVerityAppearanceChanged()
    {
        if (!_uiBuilt)
        {
            return;
        }

        ApplyVerityChrome();
        GsiShopLikeCanvasHeaderUi.RefreshVerityAltarHeaderLocalizedTexts(_headerTitleTmp, null, _headerBackButton);
        RefreshHeader();
        RebuildSessionUi(scrollListToTop: false);
    }

    private void TrySubscribeAppearance()
    {
        if (_appearanceSubscribed)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return;
        }
#endif
        GsiUiAppearance.Changed += OnVerityAppearanceChanged;
        _appearanceSubscribed = true;
    }

    private void TryUnsubscribeAppearance()
    {
        if (!_appearanceSubscribed)
        {
            return;
        }

        GsiUiAppearance.Changed -= OnVerityAppearanceChanged;
        _appearanceSubscribed = false;
    }

    private void TrySubscribeLocaleChanged()
    {
        if (_localeSubscribed)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return;
        }
#endif
        GameLocalization.UiLocaleChanged += OnVerityLocaleChanged;
        _localeSubscribed = true;
    }

    private void TryUnsubscribeLocaleChanged()
    {
        if (!_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged -= OnVerityLocaleChanged;
        _localeSubscribed = false;
    }

    private void OnVerityLocaleChanged()
    {
        OnVerityAppearanceChanged();
    }

    private void ApplyVerityChrome()
    {
        if (_bgImage != null)
        {
            _bgImage.color = GsiUiAppearance.ShopScreenBackground;
        }

        if (_headerStrip != null)
        {
            _headerStrip.color = GsiUiAppearance.ShopHeaderStrip(CosmeticTheme.UiAccent);
        }

        if (_subBarStripImage != null)
        {
            _subBarStripImage.color = GsiUiAppearance.SubBarStripBackground;
        }

        if (_headerTitleTmp != null)
        {
            GsiUiScreenLayout.ApplyScreenTitleTypography(_headerTitleTmp);
            _headerTitleTmp.color = GsiUiAppearance.TextPrimary;
        }

        if (_goldText != null)
        {
            _goldText.fontSize = GsiUiScreenLayout.EconomyLineFontSize;
            _goldText.color = GsiUiAppearance.ShopGoldText;
        }

        if (_ticketText != null)
        {
            _ticketText.fontSize = GsiUiScreenLayout.EconomyLineFontSize;
            _ticketText.color = GsiUiAppearance.ShopTicketText;
        }

        GsiUiRuntimeWidgets.ApplySecondaryHeaderButtonLook(_headerBackButton);

        Transform header = transform.Find($"{CanvasName}/Header");
        if (header != null)
        {
            GsiShopLikeCanvasHeaderUi.ApplyHeaderStripFlexibleTitleLayout(header);
        }

        if (_listCaptionTmp != null)
        {
            _listCaptionTmp.color = GsiUiAppearance.TextSecondary;
        }

        if (_detailCaptionTmp != null)
        {
            _detailCaptionTmp.color = GsiUiAppearance.TextSecondary;
        }

        if (_detailBodyTmp != null)
        {
            _detailBodyTmp.color = GsiUiAppearance.TextPrimary;
        }
    }

    private void RefreshLocalizedChrome()
    {
        GsiShopLikeCanvasHeaderUi.RefreshVerityAltarHeaderLocalizedTexts(_headerTitleTmp, null, _headerBackButton);
        if (_listCaptionTmp != null)
        {
            _listCaptionTmp.text = GameLocalization.GetUiString(UiStringKeys.VerityAltarListCaption, "Completed sessions");
        }

        if (_detailCaptionTmp != null)
        {
            _detailCaptionTmp.text = GameLocalization.GetUiString(UiStringKeys.VerityAltarDetailCaption, "Detailed results");
        }
    }

    private void OnEconomyChanged()
    {
        RefreshHeader();
    }

    private void TrySubscribeEconomy()
    {
        if (_economySubscribed || EconomyManager.Instance == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return;
        }
#endif
        EconomyManager.Instance.OnEconomyChanged += OnEconomyChanged;
        _economySubscribed = true;
    }

    private void TryUnsubscribeEconomy()
    {
        if (!_economySubscribed || EconomyManager.Instance == null)
        {
            return;
        }

        EconomyManager.Instance.OnEconomyChanged -= OnEconomyChanged;
        _economySubscribed = false;
    }

    private void BuildUi()
    {
        if (transform.Find(CanvasName) == null)
        {
            CreateVerityCanvasContent();
        }

        if (!ResolveCanvasReferences())
        {
            Debug.LogWarning("[VerityAltar] VerityCanvas references incomplete.");
            return;
        }

        GsiShopLikeCanvasHeaderUi.DestroyHeaderSettingsButtonIfPresent(transform, CanvasName);
        GsiShopLikeCanvasHeaderUi.EnforceHeaderChildLayoutOrder(transform, CanvasName);
        GsiShopLikeCanvasHeaderUi.WireHeaderListeners(null, _headerBackButton, OnBackClicked);
    }

    private void CreateVerityCanvasContent()
    {
        var canvasGo = new GameObject(CanvasName, typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        GsiUiRuntimeWidgets.StretchFull(canvasGo.GetComponent<RectTransform>());

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        GsiUiScreenLayout.ApplyCanvasScaler(scaler);
        canvasGo.AddComponent<GraphicRaycaster>();

        var bg = GsiUiRuntimeWidgets.CreateUiObject("Background", canvasGo.transform);
        GsiUiRuntimeWidgets.StretchFull(bg.GetComponent<RectTransform>());
        bg.transform.SetAsFirstSibling();
        var bgImg = bg.AddComponent<Image>();
        _bgImage = bgImg;
        bgImg.color = GsiUiAppearance.ShopScreenBackground;
        bgImg.raycastTarget = true;

        var header = GsiUiRuntimeWidgets.CreateUiObject("Header", canvasGo.transform);
        var headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = Vector2.zero;
        headerRt.sizeDelta = new Vector2(0f, GsiUiScreenLayout.HeaderStripHeight);
        var headerStrip = header.AddComponent<Image>();
        _headerStrip = headerStrip;
        headerStrip.color = GsiUiAppearance.ShopHeaderStrip(CosmeticTheme.UiAccent);
        headerStrip.raycastTarget = false;
        var headerLe = header.AddComponent<LayoutElement>();
        headerLe.preferredHeight = GsiUiScreenLayout.HeaderStripHeight;
        headerLe.flexibleWidth = 1f;
        var headerH = header.AddComponent<HorizontalLayoutGroup>();
        headerH.padding = GsiUiScreenLayout.HeaderStripPadding;
        headerH.spacing = GsiUiScreenLayout.HeaderRowSpacing;
        headerH.childAlignment = TextAnchor.MiddleLeft;
        headerH.childForceExpandHeight = true;
        headerH.childForceExpandWidth = true;

        var title = GsiUiRuntimeWidgets.CreateTmp(header.transform,
            GameLocalization.GetUiString(UiStringKeys.VerityAltarScreenTitle, "Altar of Verity"),
            GsiUiScreenLayout.ScreenTitleFontSize, FontStyles.Bold);
        title.gameObject.name = GsiShopLikeCanvasHeaderUi.HeaderTitleName;
        title.raycastTarget = false;
        _headerTitleTmp = title;
        var titleLe = title.gameObject.AddComponent<LayoutElement>();
        titleLe.flexibleWidth = 1f;
        title.color = GsiUiAppearance.TextPrimary;

        var backBtn = GsiUiRuntimeWidgets.CreateButton(header.transform,
            GameLocalization.GetUiString(UiStringKeys.InventoryBack, "Back"), OnBackClicked);
        backBtn.gameObject.name = GsiShopLikeCanvasHeaderUi.HeaderBackName;
        _headerBackButton = backBtn;
        var backLe = backBtn.gameObject.AddComponent<LayoutElement>();
        backLe.preferredWidth = GsiUiScreenLayout.BackHeaderButtonWidth;
        backLe.flexibleWidth = 0f;

        var subBar = GsiUiRuntimeWidgets.CreateUiObject("SubBar", canvasGo.transform);
        var subRt = subBar.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 1f);
        subRt.anchorMax = new Vector2(1f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, GsiUiScreenLayout.SubBarOffsetBelowHeader);
        subRt.sizeDelta = new Vector2(0f, GsiUiScreenLayout.SubBarHeight);
        _subBarStripImage = subBar.AddComponent<Image>();
        _subBarStripImage.color = GsiUiAppearance.SubBarStripBackground;
        _subBarStripImage.raycastTarget = false;
        var subH = subBar.AddComponent<HorizontalLayoutGroup>();
        subH.padding = GsiUiScreenLayout.SubBarPadding;
        subH.spacing = GsiUiScreenLayout.SubBarItemSpacing;
        subH.childAlignment = TextAnchor.MiddleLeft;

        _goldText = GsiUiRuntimeWidgets.CreateTmp(subBar.transform, "", GsiUiScreenLayout.EconomyLineFontSize, FontStyles.Normal);
        _ticketText = GsiUiRuntimeWidgets.CreateTmp(subBar.transform, "", GsiUiScreenLayout.EconomyLineFontSize, FontStyles.Normal);
        _goldText.color = GsiUiAppearance.ShopGoldText;
        _ticketText.color = GsiUiAppearance.ShopTicketText;

        CreateBodySplit(canvasGo.transform);
    }

    private void CreateBodySplit(Transform canvasTransform)
    {
        var body = GsiUiRuntimeWidgets.CreateUiObject("BodySplit", canvasTransform);
        var bodyRt = body.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(GsiUiScreenLayout.ScrollHorizontalInset, GsiUiScreenLayout.ScrollBottomInset);
        bodyRt.offsetMax = new Vector2(-GsiUiScreenLayout.ScrollHorizontalInset, -GsiUiScreenLayout.ScrollContentTopMargin);

        var bodyH = body.AddComponent<HorizontalLayoutGroup>();
        bodyH.padding = new RectOffset(0, 0, 0, 0);
        bodyH.spacing = 20f;
        bodyH.childAlignment = TextAnchor.UpperCenter;
        bodyH.childControlHeight = true;
        bodyH.childControlWidth = true;
        bodyH.childForceExpandHeight = true;
        bodyH.childForceExpandWidth = true;

        var listPanel = GsiUiRuntimeWidgets.CreateUiObject("ListPanel", body.transform);
        var listPanelLe = listPanel.AddComponent<LayoutElement>();
        listPanelLe.flexibleWidth = 2f;
        listPanelLe.flexibleHeight = 1f;
        listPanelLe.minWidth = 300f;
        var listCol = listPanel.AddComponent<VerticalLayoutGroup>();
        listCol.spacing = 10f;
        listCol.childAlignment = TextAnchor.UpperCenter;
        listCol.childControlHeight = true;
        listCol.childControlWidth = true;
        listCol.childForceExpandHeight = true;
        listCol.childForceExpandWidth = true;

        var listCapGo = GsiUiRuntimeWidgets.CreateUiObject("ListCaption", listPanel.transform);
        var listCapLe = listCapGo.AddComponent<LayoutElement>();
        listCapLe.preferredHeight = 28f;
        listCapLe.flexibleHeight = 0f;
        listCapLe.flexibleWidth = 1f;
        _listCaptionTmp = listCapGo.AddComponent<TextMeshProUGUI>();
        if (TmpFontCache.LiberationSansSdf != null)
        {
            _listCaptionTmp.font = TmpFontCache.LiberationSansSdf;
        }

        _listCaptionTmp.fontSize = 18f;
        _listCaptionTmp.fontStyle = FontStyles.Bold;
        _listCaptionTmp.alignment = TextAlignmentOptions.MidlineLeft;
        _listCaptionTmp.raycastTarget = false;

        CreateVerticalScroll(listPanel.transform, "ListScroll", out _listScroll, out _listContent);

        var detailPanel = GsiUiRuntimeWidgets.CreateUiObject("DetailPanel", body.transform);
        var detailPanelLe = detailPanel.AddComponent<LayoutElement>();
        detailPanelLe.flexibleWidth = 3f;
        detailPanelLe.flexibleHeight = 1f;
        detailPanelLe.minWidth = 360f;
        var detailCol = detailPanel.AddComponent<VerticalLayoutGroup>();
        detailCol.spacing = 10f;
        detailCol.childAlignment = TextAnchor.UpperCenter;
        detailCol.childControlHeight = true;
        detailCol.childControlWidth = true;
        detailCol.childForceExpandHeight = true;
        detailCol.childForceExpandWidth = true;

        var detCapGo = GsiUiRuntimeWidgets.CreateUiObject("DetailCaption", detailPanel.transform);
        var detCapLe = detCapGo.AddComponent<LayoutElement>();
        detCapLe.preferredHeight = 28f;
        detCapLe.flexibleHeight = 0f;
        detCapLe.flexibleWidth = 1f;
        _detailCaptionTmp = detCapGo.AddComponent<TextMeshProUGUI>();
        if (TmpFontCache.LiberationSansSdf != null)
        {
            _detailCaptionTmp.font = TmpFontCache.LiberationSansSdf;
        }

        _detailCaptionTmp.fontSize = 18f;
        _detailCaptionTmp.fontStyle = FontStyles.Bold;
        _detailCaptionTmp.alignment = TextAlignmentOptions.MidlineLeft;
        _detailCaptionTmp.raycastTarget = false;

        CreateDetailScroll(detailPanel.transform, out _detailScroll, out _detailBodyTmp);
    }

    private static void CreateVerticalScroll(Transform parent, string name, out ScrollRect scroll, out Transform listContent)
    {
        var scrollGo = new GameObject(name, typeof(RectTransform));
        scrollGo.transform.SetParent(parent, false);
        var scrollLe = scrollGo.AddComponent<LayoutElement>();
        scrollLe.flexibleWidth = 1f;
        scrollLe.flexibleHeight = 1f;
        scrollLe.minHeight = 200f;

        scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 48f;
        scroll.inertia = true;

        var viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRt = viewport.GetComponent<RectTransform>();
        GsiUiRuntimeWidgets.StretchFull(vpRt);
        viewport.AddComponent<RectMask2D>();
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0f);
        vpImg.raycastTarget = true;

        var listGo = new GameObject("List", typeof(RectTransform));
        listGo.transform.SetParent(viewport.transform, false);
        var listRt = listGo.GetComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0f, 1f);
        listRt.anchorMax = new Vector2(1f, 1f);
        listRt.pivot = new Vector2(0.5f, 1f);
        listRt.anchoredPosition = Vector2.zero;
        listRt.sizeDelta = new Vector2(0f, 0f);

        var listV = listGo.AddComponent<VerticalLayoutGroup>();
        listV.spacing = GsiUiScreenLayout.ListVerticalSpacing;
        listV.childAlignment = TextAnchor.UpperCenter;
        listV.childControlWidth = true;
        listV.childControlHeight = true;
        listV.childForceExpandWidth = true;
        listV.childForceExpandHeight = false;
        listV.padding = GsiUiScreenLayout.ListPadding;

        var fitter = listGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = vpRt;
        scroll.content = listRt;
        listContent = listGo.transform;
    }

    private static void CreateDetailScroll(Transform parent, out ScrollRect scroll, out TextMeshProUGUI bodyTmp)
    {
        var scrollGo = new GameObject("DetailScroll", typeof(RectTransform));
        scrollGo.transform.SetParent(parent, false);
        var scrollLe = scrollGo.AddComponent<LayoutElement>();
        scrollLe.flexibleWidth = 1f;
        scrollLe.flexibleHeight = 1f;
        scrollLe.minHeight = 200f;

        scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 48f;

        var viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRt = viewport.GetComponent<RectTransform>();
        GsiUiRuntimeWidgets.StretchFull(vpRt);
        viewport.AddComponent<RectMask2D>();
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(0f, 0f, 0f, 0.02f);
        vpImg.raycastTarget = true;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewport.transform, false);
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);

        var pad = contentGo.AddComponent<VerticalLayoutGroup>();
        pad.padding = new RectOffset(12, 12, 8, 16);
        pad.childAlignment = TextAnchor.UpperLeft;
        pad.childControlWidth = true;
        pad.childControlHeight = true;
        pad.childForceExpandWidth = true;

        var contentCsf = contentGo.AddComponent<ContentSizeFitter>();
        contentCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var textGo = GsiUiRuntimeWidgets.CreateUiObject("Body", contentGo.transform);
        var textLe = textGo.AddComponent<LayoutElement>();
        textLe.flexibleWidth = 1f;
        bodyTmp = textGo.AddComponent<TextMeshProUGUI>();
        if (TmpFontCache.LiberationSansSdf != null)
        {
            bodyTmp.font = TmpFontCache.LiberationSansSdf;
        }

        bodyTmp.fontSize = 20f;
        bodyTmp.alignment = TextAlignmentOptions.TopLeft;
        bodyTmp.enableWordWrapping = true;
        bodyTmp.raycastTarget = false;
        bodyTmp.text = string.Empty;

        var csf = textGo.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = vpRt;
        scroll.content = contentRt;
    }

    private bool ResolveCanvasReferences()
    {
        if (!GsiShopLikeCanvasHeaderUi.TryResolve(transform, CanvasName, out GsiShopLikeCanvasHeaderUi.CanvasRefs r))
        {
            return false;
        }

        _bgImage = r.BackgroundImage;
        _headerStrip = r.HeaderStripImage;
        _headerTitleTmp = r.TitleTmp;
        _headerBackButton = r.BackButton;
        _goldText = r.GoldText;
        _ticketText = r.TicketText;
        Transform subBar = transform.Find($"{CanvasName}/SubBar");
        if (subBar != null && subBar.TryGetComponent(out Image subStrip))
        {
            _subBarStripImage = subStrip;
        }

        Transform listCap = transform.Find($"{CanvasName}/BodySplit/ListPanel/ListCaption");
        if (listCap != null && listCap.TryGetComponent(out TextMeshProUGUI lc))
        {
            _listCaptionTmp = lc;
        }

        Transform detCap = transform.Find($"{CanvasName}/BodySplit/DetailPanel/DetailCaption");
        if (detCap != null && detCap.TryGetComponent(out TextMeshProUGUI dc))
        {
            _detailCaptionTmp = dc;
        }

        Transform listRoot = transform.Find($"{CanvasName}/BodySplit/ListPanel/ListScroll/Viewport/List");
        if (listRoot != null)
        {
            _listContent = listRoot;
            Transform listScrollT = listRoot.parent.parent;
            _listScroll = listScrollT != null ? listScrollT.GetComponent<ScrollRect>() : null;
        }

        Transform bodyTf = transform.Find($"{CanvasName}/BodySplit/DetailPanel/DetailScroll/Viewport/Content/Body");
        if (bodyTf != null && bodyTf.TryGetComponent(out TextMeshProUGUI db))
        {
            _detailBodyTmp = db;
        }

        Transform detScrollT = transform.Find($"{CanvasName}/BodySplit/DetailPanel/DetailScroll");
        if (detScrollT != null)
        {
            _detailScroll = detScrollT.GetComponent<ScrollRect>();
        }

        return _listContent != null && _detailBodyTmp != null && _headerBackButton != null;
    }

    private void RebuildSessionUi(bool scrollListToTop = true)
    {
        if (_listContent == null || _detailBodyTmp == null)
        {
            return;
        }

        _sessionRows.Clear();
        for (int i = _listContent.childCount - 1; i >= 0; i--)
        {
            GsiRuntimeUiBootstrap.DestroyObjectForRuntimeUi(_listContent.GetChild(i).gameObject);
        }

        _entries.Clear();
        if (PlayerDataManager.Instance != null)
        {
            IReadOnlyList<UnifiedExamHistoryEntry> fromDisk = PlayerDataManager.Instance.GetUnifiedExamHistoryNewestFirst();
            if (fromDisk != null)
            {
                for (int i = 0; i < fromDisk.Count; i++)
                {
                    _entries.Add(fromDisk[i]);
                }
            }
        }

        _selectedIndex = -1;

        if (_entries.Count == 0)
        {
            ApplyDetailText(GameLocalization.GetUiString(UiStringKeys.VerityAltarEmpty, "No unified exam records yet."));
            RectTransform listRt = _listContent.GetComponent<RectTransform>();
            GsiShopLikeListScrollUi.RebuildListLayout(transform, $"{CanvasName}/BodySplit/ListPanel/ListScroll", listRt, scrollListToTop);
            if (_detailScroll != null)
            {
                _detailScroll.verticalNormalizedPosition = 1f;
            }

            return;
        }

        TMP_FontAsset font = TmpFontCache.LiberationSansSdf;
        for (int i = 0; i < _entries.Count; i++)
        {
            int idx = i;
            UnifiedExamHistoryEntry entry = _entries[i];
            var rowGo = new GameObject($"Session_{i}", typeof(RectTransform));
            rowGo.transform.SetParent(_listContent, false);
            var rowLe = rowGo.AddComponent<LayoutElement>();
            rowLe.minHeight = 56f;
            rowLe.preferredHeight = 60f;
            rowLe.flexibleWidth = 1f;

            var img = rowGo.AddComponent<Image>();
            GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(img);
            img.color = GsiUiAppearance.ShopRowBackground;
            img.raycastTarget = true;
            GsiUiRuntimeWidgets.ApplyRowCardShadow(img);

            var btn = rowGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => OnSessionRowClicked(idx));

            var labelGo = GsiUiRuntimeWidgets.CreateUiObject("Label", rowGo.transform);
            GsiUiRuntimeWidgets.StretchFull(labelGo.GetComponent<RectTransform>());
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                tmp.font = font;
            }

            tmp.text = FormatListSummaryLine(entry);
            tmp.fontSize = 17f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = GsiUiAppearance.TextPrimary;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.margin = new Vector4(14f, 4f, 14f, 4f);
            tmp.raycastTarget = false;

            _sessionRows.Add((btn, img));
        }

        ApplyDetailText(GameLocalization.GetUiString(UiStringKeys.VerityAltarSelectHint, "Select a session to view detailed results."));
        RectTransform listRect = _listContent.GetComponent<RectTransform>();
        GsiShopLikeListScrollUi.RebuildListLayout(transform, $"{CanvasName}/BodySplit/ListPanel/ListScroll", listRect, scrollListToTop);
        if (_detailScroll != null)
        {
            _detailScroll.verticalNormalizedPosition = 1f;
        }
    }

    private void OnSessionRowClicked(int index)
    {
        if (index < 0 || index >= _entries.Count)
        {
            return;
        }

        _selectedIndex = index;
        RefreshRowSelectionColors();
        ApplyDetailText(BuildDetailString(_entries[index]));
        if (_detailScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            _detailScroll.verticalNormalizedPosition = 1f;
        }
    }

    private void RefreshRowSelectionColors()
    {
        Color baseBg = GsiUiAppearance.ShopRowBackground;
        Color accent = CosmeticTheme.UiAccent;
        Color selected = Color.Lerp(baseBg, accent, 0.38f);
        for (int i = 0; i < _sessionRows.Count; i++)
        {
            _sessionRows[i].Bg.color = i == _selectedIndex ? selected : baseBg;
        }
    }

    private void ApplyDetailText(string text)
    {
        if (_detailBodyTmp != null)
        {
            _detailBodyTmp.text = text ?? string.Empty;
        }
    }

    private string FormatListSummaryLine(UnifiedExamHistoryEntry entry)
    {
        string passWord = entry.OverallPass
            ? GameLocalization.GetUiString(UiStringKeys.ResultUnifiedFinalPass, "Final pass")
            : GameLocalization.GetUiString(UiStringKeys.ResultUnifiedFinalFail, "Final fail");
        return GameLocalization.FormatUiString(
            UiStringKeys.HistoryUnifiedRowFmt,
            "{0} | G{1} | {2:F1}/700 | {3} | Tier {4} | Cut {5:F0}",
            entry.Timestamp,
            entry.Grade,
            entry.Total,
            passWord,
            entry.RewardTier,
            entry.Cutoff);
    }

    private static string BuildDetailString(UnifiedExamHistoryEntry entry)
    {
        string passWord = entry.OverallPass
            ? GameLocalization.GetUiString(UiStringKeys.ResultUnifiedFinalPass, "Final pass")
            : GameLocalization.GetUiString(UiStringKeys.ResultUnifiedFinalFail, "Final fail");
        string header = GameLocalization.FormatUiString(
            UiStringKeys.HistoryUnifiedRowFmt,
            "{0} | G{1} | {2:F1}/700 | {3} | Tier {4} | Cut {5:F0}",
            entry.Timestamp,
            entry.Grade,
            entry.Total,
            passWord,
            entry.RewardTier,
            entry.Cutoff);

        var subjHeader = GameLocalization.GetUiString(UiStringKeys.ResultUnifiedSubjectsHeader, "[ Subjects ]");
        string[] segs = UnifiedExamHistoryStorage.SplitSegments(entry.SegmentsJoined);
        var body = new StringBuilder();
        body.AppendLine(header);
        body.AppendLine(subjHeader);
        foreach (string line in segs)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                body.AppendLine(line.TrimEnd());
            }
        }

        return body.ToString().TrimEnd();
    }

    private void RefreshHeader()
    {
        GsiShopLikeEconomyBarTexts.Apply(_goldText, _ticketText);
    }

    private void OnBackClicked()
    {
        GsiSceneNavigation.LoadLobby();
    }

#if UNITY_EDITOR
    private void BuildUiForEditorSceneView()
    {
        GameLocalization.TryInitializeSynchronouslyForEditorSceneView();
        GsiRuntimeUiBootstrap.EnsureEventSystemForUiScenes();
        GsiRuntimeUiBootstrap.EnsureEconomyManagerForUiScenes();
        CosmeticTheme.ApplyFromSave();

        if (transform.Find(CanvasName) == null)
        {
            CreateVerityCanvasContent();
        }

        if (!ResolveCanvasReferences())
        {
            return;
        }

        GsiShopLikeCanvasHeaderUi.DestroyHeaderSettingsButtonIfPresent(transform, CanvasName);
        GsiShopLikeCanvasHeaderUi.EnforceHeaderChildLayoutOrder(transform, CanvasName);
        GsiShopLikeCanvasHeaderUi.WireHeaderListeners(null, _headerBackButton, OnBackClicked);
        _uiBuilt = true;
        ApplyVerityChrome();
        RefreshLocalizedChrome();
        RefreshHeader();
        RebuildSessionUi();
        EditorUtility.SetDirty(gameObject);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif
}
