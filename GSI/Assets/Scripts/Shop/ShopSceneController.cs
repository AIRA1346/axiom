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
/// 상점 씬 UI(런타임 생성). 응시권 번들과 UI 스킨을 골드로 구매하고 스킨을 장착합니다.
/// </summary>
[ExecuteAlways]
public sealed class ShopSceneController : MonoBehaviour
{
    private TextMeshProUGUI _goldText;
    private TextMeshProUGUI _ticketText;
    private Image _shopBgImage;
    private Image _subBarStripImage;
    private Image _shopHeaderImage;
    private TextMeshProUGUI _headerTitleTmp;
    private Button _headerBackButton;
    private bool _uiBuilt;
    private bool _economySubscribed;
    private bool _appearanceSubscribed;
    private bool _localeSubscribed;
    private GsiRuntimeUiRowPool _offerRowPool;

    private void Awake()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            BuildUiForEditorSceneView();
            return;
        }
#endif
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
        RebuildRows();
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
            Debug.LogWarning("[Shop] Localization init: " + boot.Exception.GetBaseException().Message);
        }

        BuildUi();
        _uiBuilt = true;
        ApplyShopChrome();
        RefreshHeader();
        RebuildRows();
        TrySubscribeEconomy();
        TrySubscribeAppearance();
    }

    private void OnShopAppearanceChanged()
    {
        if (!_uiBuilt)
        {
            return;
        }

        ApplyShopChrome();
        GsiShopLikeCanvasHeaderUi.RefreshShopHeaderLocalizedTexts(_headerTitleTmp, null, _headerBackButton);
        RefreshHeader();
        RebuildRows();
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
        GsiUiAppearance.Changed += OnShopAppearanceChanged;
        _appearanceSubscribed = true;
    }

    private void TryUnsubscribeAppearance()
    {
        if (!_appearanceSubscribed)
        {
            return;
        }

        GsiUiAppearance.Changed -= OnShopAppearanceChanged;
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
        GameLocalization.UiLocaleChanged += OnShopLocaleChanged;
        _localeSubscribed = true;
    }

    private void TryUnsubscribeLocaleChanged()
    {
        if (!_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged -= OnShopLocaleChanged;
        _localeSubscribed = false;
    }

    private void OnShopLocaleChanged()
    {
        OnShopAppearanceChanged();
    }

    private void ApplyShopChrome()
    {
        if (_shopBgImage != null)
        {
            _shopBgImage.color = GsiUiAppearance.ShopScreenBackground;
        }

        if (_shopHeaderImage != null)
        {
            _shopHeaderImage.color = GsiUiAppearance.ShopHeaderStrip(CosmeticTheme.UiAccent);
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

        Transform shopHeader = transform.Find("ShopCanvas/Header");
        if (shopHeader != null)
        {
            GsiShopLikeCanvasHeaderUi.ApplyHeaderStripFlexibleTitleLayout(shopHeader);
        }
    }

    private void OnEconomyChanged()
    {
        RefreshHeader();
        RebuildRows(scrollListToTop: false);
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
        if (transform.Find("ShopCanvas") == null)
        {
            CreateShopUiContent();
        }

        if (!ResolveShopCanvasReferences())
        {
            Debug.LogWarning("[Shop] ShopCanvas UI 참조를 채우지 못했습니다.");
            return;
        }

        GsiShopLikeCanvasHeaderUi.DestroyHeaderSettingsButtonIfPresent(transform, "ShopCanvas");
        GsiShopLikeCanvasHeaderUi.EnforceHeaderChildLayoutOrder(transform, "ShopCanvas");
        GsiShopLikeCanvasHeaderUi.WireHeaderListeners(null, _headerBackButton, OnBackClicked);
    }

    /// <summary>
    /// 씬에 저장된 ShopCanvas가 있어도 호출됩니다. 헤더는 제목 + Back(ESC로 설정).
    /// </summary>
    private void CreateShopUiContent()
    {
        var canvasGo = new GameObject("ShopCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        GsiUiRuntimeWidgets.StretchFull(canvasGo.GetComponent<RectTransform>());

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        GsiUiScreenLayout.ApplyCanvasScaler(scaler);
        canvasGo.AddComponent<GraphicRaycaster>();

        var bg = GsiUiRuntimeWidgets.CreateUiObject("Background", canvasGo.transform);
        var bgRt = bg.GetComponent<RectTransform>();
        GsiUiRuntimeWidgets.StretchFull(bgRt);
        bg.transform.SetAsFirstSibling();
        var bgImg = bg.AddComponent<Image>();
        _shopBgImage = bgImg;
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
        _shopHeaderImage = headerStrip;
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

        var title = GsiUiRuntimeWidgets.CreateTmp(header.transform, GameLocalization.GetUiString(UiStringKeys.ShopTitle, "Shop"),
            GsiUiScreenLayout.ScreenTitleFontSize, FontStyles.Bold);
        title.gameObject.name = GsiShopLikeCanvasHeaderUi.HeaderTitleName;
        title.raycastTarget = false;
        _headerTitleTmp = title;
        var titleLe = title.gameObject.AddComponent<LayoutElement>();
        titleLe.flexibleWidth = 1f;
        title.color = GsiUiAppearance.TextPrimary;

        var backBtn = GsiUiRuntimeWidgets.CreateButton(header.transform, GameLocalization.GetUiString(UiStringKeys.ShopBack, "Back"), OnBackClicked);
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

    private bool ResolveShopCanvasReferences()
    {
        if (!GsiShopLikeCanvasHeaderUi.TryResolve(transform, "ShopCanvas", out GsiShopLikeCanvasHeaderUi.CanvasRefs r))
        {
            return false;
        }

        _shopBgImage = r.BackgroundImage;
        _shopHeaderImage = r.HeaderStripImage;
        _headerTitleTmp = r.TitleTmp;
        _headerBackButton = r.BackButton;
        _goldText = r.GoldText;
        _ticketText = r.TicketText;
        Transform subBar = transform.Find("ShopCanvas/SubBar");
        if (subBar != null && subBar.TryGetComponent(out Image subStrip))
        {
            _subBarStripImage = subStrip;
        }

        return true;
    }

    private Transform FindListTransform()
    {
        return transform.Find("ShopCanvas/ScrollView/Viewport/List");
    }

    private void RebuildRows(bool scrollListToTop = true)
    {
        Transform list = FindListTransform();
        if (list == null)
        {
            return;
        }

        Transform poolRoot = GsiShopLikeListScrollUi.EnsureRowPoolRoot(list);
        if (_offerRowPool == null)
        {
            _offerRowPool = new GsiRuntimeUiRowPool(poolRoot);
        }

        _offerRowPool.RecycleAllListRows(list);

        foreach (ShopCatalog.Offer offer in ShopCatalog.All)
        {
            GameObject row = _offerRowPool.PopOrCreate(list, BuildOfferRowShell);
            PopulateOfferRow(row, offer);
        }

        GsiShopLikeListScrollUi.RebuildListLayout(transform, "ShopCanvas/ScrollView",
            list.GetComponent<RectTransform>(), scrollListToTop);
    }

    private static GameObject BuildOfferRowShell(Transform parent)
    {
        var row = GsiUiRuntimeWidgets.CreateUiObject("OfferRow", parent);
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0f, GsiUiScreenLayout.ShopOfferRowPreferredHeight);
        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = GsiUiScreenLayout.ShopOfferRowPreferredHeight;
        rowLe.flexibleWidth = 1f;
        var rowImg = row.AddComponent<Image>();
        rowImg.color = GsiUiAppearance.ShopRowBackground;
        rowImg.raycastTarget = false;
        GsiUiRuntimeWidgets.ApplyRowCardShadow(rowImg);
        var rowH = row.AddComponent<HorizontalLayoutGroup>();
        rowH.padding = GsiUiScreenLayout.ShopOfferRowPadding;
        rowH.spacing = 20f;
        rowH.childAlignment = TextAnchor.MiddleLeft;
        rowH.childForceExpandWidth = false;
        rowH.childForceExpandHeight = true;
        return row;
    }

    private void PopulateOfferRow(GameObject row, ShopCatalog.Offer offer)
    {
        if (row.TryGetComponent(out Image rowBg))
        {
            rowBg.color = GsiUiAppearance.ShopRowBackground;
        }

        string label = GsiShopLikeOfferDisplayNames.GetLocalized(offer);
        var nameTmp = GsiUiRuntimeWidgets.CreateTmp(row.transform, label, GsiUiScreenLayout.ListRowTitleFontSize, FontStyles.Normal);
        nameTmp.color = GsiUiAppearance.TextPrimary;
        GsiUiRuntimeWidgets.ApplyListRowPrimaryLabel(nameTmp);
        var nameLe = nameTmp.gameObject.AddComponent<LayoutElement>();
        nameLe.flexibleWidth = 1f;
        nameLe.minWidth = 280f;

        int gold = EconomyManager.Instance != null ? EconomyManager.Instance.Tokens : 0;

        if (offer.Kind == ShopCatalog.OfferKind.ExamTickets)
        {
            var priceTmp = GsiUiRuntimeWidgets.CreateTmp(row.transform,
                GameLocalization.FormatUiString(UiStringKeys.ShopPriceGoldFmt, "{0} gold", offer.PriceGold), 20f, FontStyles.Normal);
            priceTmp.color = GsiUiAppearance.TextSecondary;
            GsiUiRuntimeWidgets.ApplyListRowSecondaryLabel(priceTmp);
            var priceLe = priceTmp.gameObject.AddComponent<LayoutElement>();
            priceLe.preferredWidth = 140f;

            bool canBuy = gold >= offer.PriceGold;
            var buyBtn = GsiUiRuntimeWidgets.CreateButton(row.transform,
                GameLocalization.GetUiString(UiStringKeys.ShopBuy, "Buy"), () => TryBuyTickets(offer));
            buyBtn.interactable = canBuy;
            var buyLe = buyBtn.gameObject.AddComponent<LayoutElement>();
            buyLe.preferredWidth = 148f;
            GsiUiRuntimeWidgets.ApplyAccentTintedActionButton(buyBtn);
        }
        else
        {
            bool owned = PlayerCosmetics.IsSkinOwned(offer.Id);
            string equipped = PlayerCosmetics.EquippedSkinId;
            bool isEquipped = equipped == offer.Id;

            if (!owned)
            {
                var priceTmp = GsiUiRuntimeWidgets.CreateTmp(row.transform,
                    offer.PriceGold > 0
                        ? GameLocalization.FormatUiString(UiStringKeys.ShopPriceGoldFmt, "{0} gold", offer.PriceGold)
                        : GameLocalization.GetUiString(UiStringKeys.ShopOwned, "Owned"),
                    20f, FontStyles.Normal);
                priceTmp.color = GsiUiAppearance.TextSecondary;
                GsiUiRuntimeWidgets.ApplyListRowSecondaryLabel(priceTmp);
                var priceLe = priceTmp.gameObject.AddComponent<LayoutElement>();
                priceLe.preferredWidth = 140f;

                if (offer.PriceGold > 0)
                {
                    bool canBuy = gold >= offer.PriceGold;
                    var buyBtn = GsiUiRuntimeWidgets.CreateButton(row.transform,
                        GameLocalization.GetUiString(UiStringKeys.ShopBuy, "Buy"), () => TryBuySkin(offer));
                    buyBtn.interactable = canBuy;
                    var buyLe = buyBtn.gameObject.AddComponent<LayoutElement>();
                    buyLe.preferredWidth = 148f;
                    GsiUiRuntimeWidgets.ApplyAccentTintedActionButton(buyBtn);
                }
            }
            else
            {
                var ownedTmp = GsiUiRuntimeWidgets.CreateTmp(row.transform,
                    GameLocalization.GetUiString(UiStringKeys.ShopOwned, "Owned"), 20f, FontStyles.Normal);
                ownedTmp.color = GsiUiAppearance.TextSecondary;
                GsiUiRuntimeWidgets.ApplyListRowSecondaryLabel(ownedTmp);
                var ownedLe = ownedTmp.gameObject.AddComponent<LayoutElement>();
                ownedLe.preferredWidth = 100f;

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
                    var equipBtn = GsiUiRuntimeWidgets.CreateButton(row.transform,
                        GameLocalization.GetUiString(UiStringKeys.ShopEquip, "Equip"), () => TryEquipSkin(offer.Id));
                    var equipLe = equipBtn.gameObject.AddComponent<LayoutElement>();
                    equipLe.preferredWidth = 148f;
                    GsiUiRuntimeWidgets.ApplyAccentTintedActionButton(equipBtn);
                }
            }
        }
    }

    private void TryBuyTickets(ShopCatalog.Offer offer)
    {
        if (EconomyManager.Instance == null || offer.Kind != ShopCatalog.OfferKind.ExamTickets)
        {
            return;
        }

        if (!EconomyManager.Instance.SpendTokens(offer.PriceGold))
        {
            return;
        }

        EconomyManager.Instance.AddTickets(offer.TicketCount);
        RefreshHeader();
        RebuildRows(scrollListToTop: false);
    }

    private void TryBuySkin(ShopCatalog.Offer offer)
    {
        if (EconomyManager.Instance == null || offer.Kind != ShopCatalog.OfferKind.Skin)
        {
            return;
        }

        if (PlayerCosmetics.IsSkinOwned(offer.Id))
        {
            return;
        }

        if (offer.PriceGold <= 0)
        {
            return;
        }

        if (!EconomyManager.Instance.SpendTokens(offer.PriceGold))
        {
            return;
        }

        PlayerCosmetics.UnlockSkin(offer.Id);
        PlayerCosmetics.TryEquip(offer.Id);
        CosmeticTheme.ApplyFromSave();
        RefreshHeader();
        RebuildRows(scrollListToTop: false);
    }

    private void TryEquipSkin(string skinId)
    {
        if (!PlayerCosmetics.TryEquip(skinId))
        {
            return;
        }

        CosmeticTheme.ApplyFromSave();
        RebuildRows(scrollListToTop: false);
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

        if (transform.Find("ShopCanvas") == null)
        {
            CreateShopUiContent();
        }

        if (!ResolveShopCanvasReferences())
        {
            return;
        }

        GsiShopLikeCanvasHeaderUi.DestroyHeaderSettingsButtonIfPresent(transform, "ShopCanvas");
        GsiShopLikeCanvasHeaderUi.EnforceHeaderChildLayoutOrder(transform, "ShopCanvas");
        GsiShopLikeCanvasHeaderUi.WireHeaderListeners(null, _headerBackButton, OnBackClicked);
        _uiBuilt = true;
        ApplyShopChrome();
        RefreshHeader();
        RebuildRows();
        MarkShopSceneDirtyIfNeeded();
    }

    private void MarkShopSceneDirtyIfNeeded()
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
