using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// 인벤토리 씬: 보유 골드·응시권 표시, UI 스킨 보유·장착(상점에서 구매한 스킨 적용).
/// </summary>
[ExecuteAlways]
public sealed class InventorySceneController : MonoBehaviour
{
    private TextMeshProUGUI _goldText;
    private TextMeshProUGUI _ticketText;
    private Image _bgImage;
    private Image _subBarStripImage;
    private Image _headerStrip;
    private TextMeshProUGUI _headerTitleTmp;
    private Button _headerBackButton;
    private bool _uiBuilt;
    private bool _economySubscribed;
    private bool _appearanceSubscribed;
    private bool _localeSubscribed;
    private GsiRuntimeUiRowPool _inventorySectionRowPool;
    private GsiRuntimeUiRowPool _inventorySkinRowPool;

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
        RebuildList();
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
            Debug.LogWarning("[Inventory] Localization init: " + boot.Exception.GetBaseException().Message);
        }

        BuildUi();
        _uiBuilt = true;
        CosmeticTheme.ApplyFromSave();
        ApplyInventoryChrome();
        RefreshHeader();
        RebuildList();
        TrySubscribeEconomy();
        TrySubscribeAppearance();
    }

    private void OnInventoryAppearanceChanged()
    {
        if (!_uiBuilt)
        {
            return;
        }

        ApplyInventoryChrome();
        GsiShopLikeCanvasHeaderUi.RefreshInventoryHeaderLocalizedTexts(_headerTitleTmp, null, _headerBackButton);
        RefreshHeader();
        RebuildList();
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
        GsiUiAppearance.Changed += OnInventoryAppearanceChanged;
        _appearanceSubscribed = true;
    }

    private void TryUnsubscribeAppearance()
    {
        if (!_appearanceSubscribed)
        {
            return;
        }

        GsiUiAppearance.Changed -= OnInventoryAppearanceChanged;
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
        GameLocalization.UiLocaleChanged += OnInventoryLocaleChanged;
        _localeSubscribed = true;
    }

    private void TryUnsubscribeLocaleChanged()
    {
        if (!_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged -= OnInventoryLocaleChanged;
        _localeSubscribed = false;
    }

    private void OnInventoryLocaleChanged()
    {
        OnInventoryAppearanceChanged();
    }

    private void ApplyInventoryChrome()
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

        Transform invHeader = transform.Find("InventoryCanvas/Header");
        if (invHeader != null)
        {
            GsiShopLikeCanvasHeaderUi.ApplyHeaderStripFlexibleTitleLayout(invHeader);
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
        if (transform.Find("InventoryCanvas") == null)
        {
            CreateInventoryUiContent();
        }

        if (!ResolveInventoryCanvasReferences())
        {
            Debug.LogWarning("[Inventory] InventoryCanvas UI 참조를 채우지 못했습니다.");
            return;
        }

        GsiShopLikeCanvasHeaderUi.DestroyHeaderSettingsButtonIfPresent(transform, "InventoryCanvas");
        GsiShopLikeCanvasHeaderUi.EnforceHeaderChildLayoutOrder(transform, "InventoryCanvas");
        GsiShopLikeCanvasHeaderUi.WireHeaderListeners(null, _headerBackButton, OnBackClicked);
    }

    private void CreateInventoryUiContent()
    {
        var canvasGo = new GameObject("InventoryCanvas", typeof(RectTransform));
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
            GameLocalization.GetUiString(UiStringKeys.InventoryTitle, "Inventory"),
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

        GsiShopLikeListScrollUi.BuildScrollAreaUnderCanvas(canvasGo.transform);
    }

    private bool ResolveInventoryCanvasReferences()
    {
        if (!GsiShopLikeCanvasHeaderUi.TryResolve(transform, "InventoryCanvas", out GsiShopLikeCanvasHeaderUi.CanvasRefs r))
        {
            return false;
        }

        _bgImage = r.BackgroundImage;
        _headerStrip = r.HeaderStripImage;
        _headerTitleTmp = r.TitleTmp;
        _headerBackButton = r.BackButton;
        _goldText = r.GoldText;
        _ticketText = r.TicketText;
        Transform subBar = transform.Find("InventoryCanvas/SubBar");
        if (subBar != null && subBar.TryGetComponent(out Image subStrip))
        {
            _subBarStripImage = subStrip;
        }

        return true;
    }

    private Transform FindListTransform()
    {
        return transform.Find("InventoryCanvas/ScrollView/Viewport/List");
    }

    private void RebuildList(bool scrollListToTop = true)
    {
        Transform list = FindListTransform();
        if (list == null)
        {
            return;
        }

        Transform poolSection = GsiShopLikeListScrollUi.EnsureRowPoolRoot(list,
            GsiShopLikeListScrollUi.InventorySectionRowPoolChildName);
        Transform poolSkin = GsiShopLikeListScrollUi.EnsureRowPoolRoot(list,
            GsiShopLikeListScrollUi.InventorySkinRowPoolChildName);
        if (_inventorySectionRowPool == null)
        {
            _inventorySectionRowPool = new GsiRuntimeUiRowPool(poolSection);
        }

        if (_inventorySkinRowPool == null)
        {
            _inventorySkinRowPool = new GsiRuntimeUiRowPool(poolSkin);
        }

        RecycleInventoryListRows(list);

        GameObject headerGo = _inventorySectionRowPool.PopOrCreate(list, BuildInventorySectionHeaderShell);
        PopulateInventorySectionHeader(headerGo);

        foreach (ShopCatalog.Offer offer in ShopCatalog.EnumerateSkinOffers())
        {
            GameObject skinGo = _inventorySkinRowPool.PopOrCreate(list, BuildSkinRowShell);
            PopulateSkinRow(skinGo, offer);
        }

        GsiShopLikeListScrollUi.RebuildListLayout(transform, "InventoryCanvas/ScrollView",
            list.GetComponent<RectTransform>(), scrollListToTop);
    }

    private void RecycleInventoryListRows(Transform list)
    {
        for (int i = list.childCount - 1; i >= 0; i--)
        {
            GameObject go = list.GetChild(i).gameObject;
            if (GsiShopLikeListScrollUi.IsRowPoolHolderName(go.name))
            {
                continue;
            }

            if (go.name == "SkinsHeader")
            {
                _inventorySectionRowPool.Push(go);
            }
            else
            {
                _inventorySkinRowPool.Push(go);
            }
        }
    }

    private static GameObject BuildInventorySectionHeaderShell(Transform parent)
    {
        var row = GsiUiRuntimeWidgets.CreateUiObject("SkinsHeader", parent);
        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 40f;
        rowLe.minHeight = 36f;
        rowLe.flexibleWidth = 1f;
        return row;
    }

    private void PopulateInventorySectionHeader(GameObject row)
    {
        var tmp = GsiUiRuntimeWidgets.CreateTmp(row.transform,
            GameLocalization.GetUiString(UiStringKeys.InventorySkinsHeader, "UI skins"), 20f, FontStyles.Bold);
        tmp.color = GsiUiAppearance.TextSecondary;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.characterSpacing = 0.4f;
    }

    private static GameObject BuildSkinRowShell(Transform parent)
    {
        var row = GsiUiRuntimeWidgets.CreateUiObject("SkinRow", parent);
        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = GsiUiScreenLayout.InventorySkinRowPreferredHeight;
        rowLe.flexibleWidth = 1f;
        var rowImg = row.AddComponent<Image>();
        rowImg.color = GsiUiAppearance.ShopRowBackground;
        rowImg.raycastTarget = false;
        GsiUiRuntimeWidgets.ApplyRowCardShadow(rowImg);
        var rowH = row.AddComponent<HorizontalLayoutGroup>();
        rowH.padding = GsiUiScreenLayout.InventorySkinRowPadding;
        rowH.spacing = 20f;
        rowH.childAlignment = TextAnchor.MiddleLeft;
        rowH.childForceExpandWidth = false;
        rowH.childForceExpandHeight = true;
        return row;
    }

    private void PopulateSkinRow(GameObject row, ShopCatalog.Offer offer)
    {
        if (row.TryGetComponent(out Image rowBg))
        {
            rowBg.color = GsiUiAppearance.ShopRowBackground;
        }

        bool owned = PlayerCosmetics.IsSkinOwned(offer.Id);
        string equippedId = PlayerCosmetics.EquippedSkinId;
        bool isEquipped = equippedId == offer.Id;

        var chipGo = GsiUiRuntimeWidgets.CreateUiObject("AccentChip", row.transform);
        var chipLe = chipGo.AddComponent<LayoutElement>();
        chipLe.preferredWidth = 48f;
        chipLe.preferredHeight = 48f;
        chipLe.minWidth = 48f;
        var chipRt = chipGo.GetComponent<RectTransform>();
        chipRt.sizeDelta = new Vector2(48f, 48f);
        var chipImg = chipGo.AddComponent<Image>();
        chipImg.raycastTarget = false;
        if (owned)
        {
            Color a = PlayerCosmetics.GetAccentColor(offer.Id);
            chipImg.color = Color.Lerp(GsiUiAppearance.ShopRowBackground, a, 0.82f);
        }
        else
        {
            chipImg.color = GsiUiAppearance.ChipInactive;
        }

        string label = GsiShopLikeOfferDisplayNames.GetLocalized(offer);
        var nameTmp = GsiUiRuntimeWidgets.CreateTmp(row.transform, label, GsiUiScreenLayout.ListRowTitleFontSize, FontStyles.Normal);
        nameTmp.color = GsiUiAppearance.TextPrimary;
        GsiUiRuntimeWidgets.ApplyListRowPrimaryLabel(nameTmp);
        var nameLe = nameTmp.gameObject.AddComponent<LayoutElement>();
        nameLe.flexibleWidth = 1f;
        nameLe.minWidth = 200f;

        if (!owned)
        {
            var lockTmp = GsiUiRuntimeWidgets.CreateTmp(row.transform,
                GameLocalization.GetUiString(UiStringKeys.InventoryLocked, "Locked"), 18f, FontStyles.Normal);
            lockTmp.color = GsiUiAppearance.TextSecondary;
            GsiUiRuntimeWidgets.ApplyListRowSecondaryLabel(lockTmp);
            var lockLe = lockTmp.gameObject.AddComponent<LayoutElement>();
            lockLe.preferredWidth = 88f;

            var shopBtn = GsiUiRuntimeWidgets.CreateButton(row.transform,
                GameLocalization.GetUiString(UiStringKeys.InventoryGoShop, "Shop"), OnGoShopClicked);
            var shopLe = shopBtn.gameObject.AddComponent<LayoutElement>();
            shopLe.preferredWidth = 148f;
            GsiUiRuntimeWidgets.ApplyAccentTintedActionButton(shopBtn);
            return;
        }

        var ownedTmp = GsiUiRuntimeWidgets.CreateTmp(row.transform,
            GameLocalization.GetUiString(UiStringKeys.ShopOwned, "Owned"), 18f, FontStyles.Normal);
        ownedTmp.color = GsiUiAppearance.TextSecondary;
        GsiUiRuntimeWidgets.ApplyListRowSecondaryLabel(ownedTmp);
        var ownedLe = ownedTmp.gameObject.AddComponent<LayoutElement>();
        ownedLe.preferredWidth = 72f;

        if (isEquipped)
        {
            var eqTmp = GsiUiRuntimeWidgets.CreateTmp(row.transform,
                GameLocalization.GetUiString(UiStringKeys.ShopEquipped, "Equipped"), 20f, FontStyles.Bold);
            eqTmp.color = GsiUiAppearance.TextPrimary;
            GsiUiRuntimeWidgets.ApplyListRowPrimaryLabel(eqTmp);
            var eqLe = eqTmp.gameObject.AddComponent<LayoutElement>();
            eqLe.preferredWidth = 148f;
        }
        else
        {
            string id = offer.Id;
            var equipBtn = GsiUiRuntimeWidgets.CreateButton(row.transform,
                GameLocalization.GetUiString(UiStringKeys.ShopEquip, "Equip"), () => TryEquipSkin(id));
            var equipLe = equipBtn.gameObject.AddComponent<LayoutElement>();
            equipLe.preferredWidth = 148f;
            GsiUiRuntimeWidgets.ApplyAccentTintedActionButton(equipBtn);
        }
    }

    private void TryEquipSkin(string skinId)
    {
        if (!PlayerCosmetics.TryEquip(skinId))
        {
            return;
        }

        CosmeticTheme.ApplyFromSave();
        ApplyInventoryChrome();
        RebuildList(scrollListToTop: false);
    }

    private void RefreshHeader()
    {
        GsiShopLikeEconomyBarTexts.Apply(_goldText, _ticketText);
    }

    private void OnBackClicked()
    {
        GsiSceneNavigation.LoadLobby();
    }

    private void OnGoShopClicked()
    {
        GsiSceneNavigation.LoadShop();
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 씬 뷰·게임 뷰(비플레이)에도 런타임과 같은 Canvas가 보이도록 합니다.
    /// </summary>
    private void BuildUiForEditorSceneView()
    {
        GameLocalization.TryInitializeSynchronouslyForEditorSceneView();
        GsiRuntimeUiBootstrap.EnsureEventSystemForUiScenes();
        GsiRuntimeUiBootstrap.EnsureEconomyManagerForUiScenes();
        CosmeticTheme.ApplyFromSave();

        if (transform.Find("InventoryCanvas") == null)
        {
            CreateInventoryUiContent();
        }

        if (!ResolveInventoryCanvasReferences())
        {
            return;
        }

        GsiShopLikeCanvasHeaderUi.DestroyHeaderSettingsButtonIfPresent(transform, "InventoryCanvas");
        GsiShopLikeCanvasHeaderUi.EnforceHeaderChildLayoutOrder(transform, "InventoryCanvas");
        GsiShopLikeCanvasHeaderUi.WireHeaderListeners(null, _headerBackButton, OnBackClicked);
        _uiBuilt = true;
        ApplyInventoryChrome();
        RefreshHeader();
        RebuildList();
        MarkInventorySceneDirtyIfNeeded();
    }

    private void MarkInventorySceneDirtyIfNeeded()
    {
        if (Application.isPlaying)
        {
            return;
        }

        EditorUtility.SetDirty(gameObject);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif
}
