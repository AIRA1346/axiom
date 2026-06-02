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
/// ?곸젏 ??UI(?고????앹꽦). ?묒떆沅?踰덈뱾怨?UI ?ㅽ궓??怨⑤뱶濡?援щℓ?섍퀬 ?ㅽ궓???μ갑?⑸땲??
/// </summary>
[ExecuteAlways]
public sealed class ShopSceneController : MonoBehaviour
{
    private TextMeshProUGUI _stardustText;
    private TextMeshProUGUI _astralCoreText;
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

        if (_stardustText != null)
        {
            _stardustText.fontSize = GsiUiScreenLayout.EconomyLineFontSize;
            _stardustText.color = GsiUiAppearance.ShopStardustText;
        }

        if (_astralCoreText != null)
        {
            _astralCoreText.fontSize = GsiUiScreenLayout.EconomyLineFontSize;
            _astralCoreText.color = GsiUiAppearance.ShopAstralCoreText;
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
            Debug.LogWarning("[Shop] ShopCanvas UI 李몄“瑜?梨꾩슦吏 紐삵뻽?듬땲??");
            return;
        }

        GsiShopLikeCanvasHeaderUi.DestroyHeaderSettingsButtonIfPresent(transform, "ShopCanvas");
        GsiShopLikeCanvasHeaderUi.EnforceHeaderChildLayoutOrder(transform, "ShopCanvas");
        GsiShopLikeCanvasHeaderUi.WireHeaderListeners(null, _headerBackButton, OnBackClicked);
    }

    /// <summary>
    /// ?ъ뿉 ??λ맂 ShopCanvas媛 ?덉뼱???몄텧?⑸땲?? ?ㅻ뜑???쒕ぉ + Back(ESC濡??ㅼ젙).
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

        _stardustText = GsiUiRuntimeWidgets.CreateTmp(subBar.transform, "", GsiUiScreenLayout.EconomyLineFontSize, FontStyles.Normal);
        _astralCoreText = GsiUiRuntimeWidgets.CreateTmp(subBar.transform, "", GsiUiScreenLayout.EconomyLineFontSize, FontStyles.Normal);
        _ticketText = GsiUiRuntimeWidgets.CreateTmp(subBar.transform, "", GsiUiScreenLayout.EconomyLineFontSize, FontStyles.Normal);
        _stardustText.color = GsiUiAppearance.ShopStardustText;
        _astralCoreText.color = GsiUiAppearance.ShopAstralCoreText;
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
        _stardustText = r.StardustText;
        _astralCoreText = r.AstralCoreText;
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

        int stardust = EconomyManager.Instance != null ? EconomyManager.Instance.Stardust : 0;
        int astralCores = EconomyManager.Instance != null ? EconomyManager.Instance.AstralCores : 0;

        if (offer.Kind == ShopCatalog.OfferKind.ExamTickets)
        {
            var priceTmp = GsiUiRuntimeWidgets.CreateTmp(row.transform,
                GameLocalization.FormatUiString(UiStringKeys.ShopPriceStardustFmt, "{0} stardust", offer.PriceStardust), 20f, FontStyles.Normal);
            priceTmp.color = GsiUiAppearance.TextSecondary;
            GsiUiRuntimeWidgets.ApplyListRowSecondaryLabel(priceTmp);
            var priceLe = priceTmp.gameObject.AddComponent<LayoutElement>();
            priceLe.preferredWidth = 140f;

            bool canBuy = stardust >= offer.PriceStardust;
            var buyBtn = GsiUiRuntimeWidgets.CreateButton(row.transform,
                GameLocalization.GetUiString(UiStringKeys.ShopBuy, "Buy"), () => TryBuyTickets(offer));
            buyBtn.interactable = canBuy;
            var buyLe = buyBtn.gameObject.AddComponent<LayoutElement>();
            buyLe.preferredWidth = 148f;
            GsiUiRuntimeWidgets.ApplyAccentTintedActionButton(buyBtn);
        }
        else if (offer.Kind == ShopCatalog.OfferKind.DecoItem)
        {
            Color rarityColor = Color.white;
            string rarityName = "일반";
            if (PlayerDecorations.TryGetItemDef(offer.Id, out var def))
            {
                rarityColor = PlayerDecorations.GetRarityColor(def.Rarity);
                bool isKo = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale != null &&
                             UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("ko", System.StringComparison.OrdinalIgnoreCase);
                rarityName = PlayerDecorations.GetRarityName(def.Rarity, isKo);
            }

            var rarityTmp = GsiUiRuntimeWidgets.CreateTmp(row.transform, $"[{rarityName}]", 20f, FontStyles.Bold);
            rarityTmp.color = rarityColor;
            GsiUiRuntimeWidgets.ApplyListRowSecondaryLabel(rarityTmp);
            var rarityLe = rarityTmp.gameObject.AddComponent<LayoutElement>();
            rarityLe.preferredWidth = 100f;

            var priceTmp = GsiUiRuntimeWidgets.CreateTmp(row.transform,
                GameLocalization.FormatUiString(UiStringKeys.ShopPriceStardustFmt, "{0} stardust", offer.PriceStardust), 20f, FontStyles.Normal);
            priceTmp.color = GsiUiAppearance.TextSecondary;
            GsiUiRuntimeWidgets.ApplyListRowSecondaryLabel(priceTmp);
            var priceLe = priceTmp.gameObject.AddComponent<LayoutElement>();
            priceLe.preferredWidth = 140f;

            int ownedCount = PlayerDecorations.GetTotalOwned(offer.Id);
            bool isKoMsg = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale != null &&
                           UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("ko", System.StringComparison.OrdinalIgnoreCase);
            string ownedText = isKoMsg ? $"보유: {ownedCount}개" : $"Owned: {ownedCount}";
            var ownedTmp = GsiUiRuntimeWidgets.CreateTmp(row.transform, ownedText, 20f, FontStyles.Normal);
            ownedTmp.color = GsiUiAppearance.TextSecondary;
            GsiUiRuntimeWidgets.ApplyListRowSecondaryLabel(ownedTmp);
            var ownedLe = ownedTmp.gameObject.AddComponent<LayoutElement>();
            ownedLe.preferredWidth = 110f;

            bool canBuy = stardust >= offer.PriceStardust;
            var buyBtn = GsiUiRuntimeWidgets.CreateButton(row.transform,
                GameLocalization.GetUiString(UiStringKeys.ShopBuy, "Buy"), () => TryBuyDecoItem(offer));
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
                if (offer.PriceAstralCore > 0)
                {
                    var priceTmp = GsiUiRuntimeWidgets.CreateTmp(row.transform,
                        GameLocalization.FormatUiString(UiStringKeys.ShopPriceAstralCoreFmt, "{0} cores", offer.PriceAstralCore),
                        20f, FontStyles.Normal);
                    priceTmp.color = GsiUiAppearance.ShopAstralCoreText;
                    GsiUiRuntimeWidgets.ApplyListRowSecondaryLabel(priceTmp);
                    var priceLe = priceTmp.gameObject.AddComponent<LayoutElement>();
                    priceLe.preferredWidth = 140f;

                    bool canBuy = astralCores >= offer.PriceAstralCore;
                    var buyBtn = GsiUiRuntimeWidgets.CreateButton(row.transform,
                        GameLocalization.GetUiString(UiStringKeys.ShopBuy, "Buy"), () => TryBuySkin(offer));
                    buyBtn.interactable = canBuy;
                    var buyLe = buyBtn.gameObject.AddComponent<LayoutElement>();
                    buyLe.preferredWidth = 148f;
                    GsiUiRuntimeWidgets.ApplyAccentTintedActionButton(buyBtn);
                }
                else
                {
                    var priceTmp = GsiUiRuntimeWidgets.CreateTmp(row.transform,
                        offer.PriceStardust > 0
                            ? GameLocalization.FormatUiString(UiStringKeys.ShopPriceStardustFmt, "{0} stardust", offer.PriceStardust)
                            : GameLocalization.GetUiString(UiStringKeys.ShopOwned, "Owned"),
                        20f, FontStyles.Normal);
                    priceTmp.color = GsiUiAppearance.TextSecondary;
                    GsiUiRuntimeWidgets.ApplyListRowSecondaryLabel(priceTmp);
                    var priceLe = priceTmp.gameObject.AddComponent<LayoutElement>();
                    priceLe.preferredWidth = 140f;

                    if (offer.PriceStardust > 0)
                    {
                        bool canBuy = stardust >= offer.PriceStardust;
                        var buyBtn = GsiUiRuntimeWidgets.CreateButton(row.transform,
                            GameLocalization.GetUiString(UiStringKeys.ShopBuy, "Buy"), () => TryBuySkin(offer));
                        buyBtn.interactable = canBuy;
                        var buyLe = buyBtn.gameObject.AddComponent<LayoutElement>();
                        buyLe.preferredWidth = 148f;
                        GsiUiRuntimeWidgets.ApplyAccentTintedActionButton(buyBtn);
                    }
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

    private void TryBuyDecoItem(ShopCatalog.Offer offer)
    {
        if (EconomyManager.Instance == null || offer.Kind != ShopCatalog.OfferKind.DecoItem)
        {
            return;
        }

        if (!EconomyManager.Instance.SpendStardust(offer.PriceStardust))
        {
            return;
        }

        int current = PlayerDecorations.GetTotalOwned(offer.Id);
        PlayerDecorations.SetTotalOwned(offer.Id, current + 1);

        GsiUiSound.PlayClick();
        RefreshHeader();
        RebuildRows(scrollListToTop: false);
    }

    private void TryBuyTickets(ShopCatalog.Offer offer)
    {
        if (EconomyManager.Instance == null || offer.Kind != ShopCatalog.OfferKind.ExamTickets)
        {
            return;
        }

        if (!EconomyManager.Instance.SpendTokens(offer.PriceStardust))
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

        if (offer.PriceAstralCore > 0)
        {
            if (!EconomyManager.Instance.SpendAstralCores(offer.PriceAstralCore))
            {
                return;
            }
        }
        else if (offer.PriceStardust > 0)
        {
            if (!EconomyManager.Instance.SpendStardust(offer.PriceStardust))
            {
                return;
            }
        }
        else
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
        GsiShopLikeEconomyBarTexts.Apply(_stardustText, _astralCoreText, _ticketText);
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
