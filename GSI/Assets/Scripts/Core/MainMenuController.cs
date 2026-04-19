using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// 로비 씬 메인 메뉴: G.S.I 시설 입장 및 경제 텍스트 표시. 다크/라이트 외관 반영.
/// </summary>
[ExecuteAlways]
public sealed class MainMenuController : MonoBehaviour
{
    private const string LobbyActionRowName = "LobbyActionRow";
    private const string LobbyLabelRulesRootName = "LobbyLabelRules";
    private const string LobbyLabelRuleTopName = "LobbyLabelRuleTop";
    private const string LobbyLabelRuleBottomName = "LobbyLabelRuleBottom";
    private const string LobbyBackdropObjectName = "LobbyBackdrop";
    private const string LobbyHeaderBarLegacyName = "LobbyHeaderBar";
    private const string LobbyEconomyStripName = "LobbyEconomyStrip";
    private static readonly Color LobbyActionButtonBackgroundClear = new Color(1f, 1f, 1f, 0f);

    [Header("G.S.I")]
    [Tooltip("할당 시 G.S.I 시설 씬으로 이동합니다.")]
    [SerializeField] private Button _enterGsiFacilityButton;

    [Tooltip("할당 시 상점 씬(응시권·스킨)으로 이동합니다.")]
    [SerializeField] private Button _openShopButton;

    [Tooltip("할당 시 인벤토리 씬(골드·응시권·스킨)으로 이동합니다.")]
    [SerializeField] private Button _openInventoryButton;

    [Tooltip("할당 시 통합 시험 기록 씬(Altar of Verity)으로 이동합니다.")]
    [SerializeField] private Button _openAltarOfVerityButton;

    [SerializeField] private Button _practiceButton;
    [SerializeField] private Button _aimPracticeButton;
    [SerializeField] private Button _reactionExamButton;
    [SerializeField] private Button _aimExamButton;

    [SerializeField] private TextMeshProUGUI _tokenText;
    [SerializeField] private TextMeshProUGUI _ticketText;

    [Header("Audio")]
    [Tooltip("Lobby BGM; uses GsiAudio music channel (GsiUserSettings music volume).")]
    [SerializeField] private AudioClip _lobbyBackgroundMusic;

    private RectTransform _lobbyRoot;
    private Image _panelBackground;
    private Image _heroImage;
    private bool _lobbyShellBuilt;
    private bool _appearanceSubscribed;
    private bool _localeSubscribed;

    private void Awake()
    {
        _lobbyRoot = transform.parent as RectTransform;
        WireOptionalHierarchyButtons();
        CacheLobbyVisualRefs();

        if (_practiceButton != null)
        {
            _practiceButton.onClick.AddListener(OnPracticeClicked);
        }

        if (_aimPracticeButton != null)
        {
            _aimPracticeButton.onClick.AddListener(OnAimPracticeClicked);
        }

        if (_reactionExamButton != null)
        {
            _reactionExamButton.onClick.AddListener(OnReactionExamClicked);
        }

        if (_aimExamButton != null)
        {
            _aimExamButton.onClick.AddListener(OnAimExamClicked);
        }

        if (_enterGsiFacilityButton != null)
        {
            _enterGsiFacilityButton.onClick.AddListener(OnEnterGsiFacilityClicked);
        }

        if (_openShopButton != null)
        {
            _openShopButton.onClick.AddListener(OnOpenShopClicked);
        }

        GlobalSettingsOverlay.EnsureCreated();
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (_lobbyRoot == null)
        {
            _lobbyRoot = transform.parent as RectTransform;
            CacheLobbyVisualRefs();
        }

        GameLocalization.TryInitializeSynchronouslyForEditorSceneView();
        EnsureLobbyShell();
        WireOptionalHierarchyButtons();
        ApplyLobbyChrome();
        UpdateEconomyTexts();
        MarkLobbySceneDirtyIfNeeded();
    }
#endif

    private void WireOptionalHierarchyButtons()
    {
        if (_lobbyRoot == null)
        {
            return;
        }

        if (_enterGsiFacilityButton == null)
        {
            Transform t = _lobbyRoot.Find("GSIButton");
            if (t != null)
            {
                _enterGsiFacilityButton = t.GetComponent<Button>();
            }
        }

        if (_openShopButton == null)
        {
            Transform t = _lobbyRoot.Find("ShopButton");
            if (t != null)
            {
                _openShopButton = t.GetComponent<Button>();
            }
        }

        if (_openInventoryButton == null)
        {
            Transform t = _lobbyRoot.Find("InventoryButton");
            if (t != null)
            {
                _openInventoryButton = t.GetComponent<Button>();
            }
        }

        if (_openAltarOfVerityButton == null)
        {
            Transform t = _lobbyRoot.Find($"{LobbyActionRowName}/AltarOfVerityButton");
            if (t == null)
            {
                t = _lobbyRoot.Find("AltarOfVerityButton");
            }

            if (t != null)
            {
                _openAltarOfVerityButton = t.GetComponent<Button>();
            }
        }
    }

    private void CacheLobbyVisualRefs()
    {
        if (_lobbyRoot == null)
        {
            return;
        }

        _panelBackground = _lobbyRoot.GetComponent<Image>();
        Transform hero = _lobbyRoot.Find("Image");
        if (hero != null)
        {
            _heroImage = hero.GetComponent<Image>();
        }
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            Cursor.visible = true;
        }

        EnsureLobbyShell();
        WireOptionalHierarchyButtons();
        ApplyLobbyChrome();
        TrySubscribeAppearance();

        if (_enterGsiFacilityButton != null)
        {
            HideLegacyGsiEntryButtons();
        }

        if (_openInventoryButton != null)
        {
            _openInventoryButton.onClick.AddListener(OnOpenInventoryClicked);
        }

        if (_openAltarOfVerityButton != null)
        {
            _openAltarOfVerityButton.onClick.AddListener(OnOpenAltarOfVerityClicked);
        }

        UpdateEconomyTexts();
        TrySubscribeLocaleChanged();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnEconomyChanged += HandleEconomyChanged;
        }

        TryPlayLobbyBgm();
    }

    private void OnDestroy()
    {
        TryUnsubscribeLocaleChanged();
        TryUnsubscribeAppearance();

        if (_practiceButton != null)
        {
            _practiceButton.onClick.RemoveListener(OnPracticeClicked);
        }

        if (_aimPracticeButton != null)
        {
            _aimPracticeButton.onClick.RemoveListener(OnAimPracticeClicked);
        }

        if (_reactionExamButton != null)
        {
            _reactionExamButton.onClick.RemoveListener(OnReactionExamClicked);
        }

        if (_aimExamButton != null)
        {
            _aimExamButton.onClick.RemoveListener(OnAimExamClicked);
        }

        if (_enterGsiFacilityButton != null)
        {
            _enterGsiFacilityButton.onClick.RemoveListener(OnEnterGsiFacilityClicked);
        }

        if (_openShopButton != null)
        {
            _openShopButton.onClick.RemoveListener(OnOpenShopClicked);
        }

        if (_openInventoryButton != null)
        {
            _openInventoryButton.onClick.RemoveListener(OnOpenInventoryClicked);
        }

        if (_openAltarOfVerityButton != null)
        {
            _openAltarOfVerityButton.onClick.RemoveListener(OnOpenAltarOfVerityClicked);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnEconomyChanged -= HandleEconomyChanged;
        }

        TryStopLobbyBgm();
    }

    private void TryPlayLobbyBgm()
    {
        if (!Application.isPlaying || _lobbyBackgroundMusic == null)
        {
            return;
        }

        GsiAudio.PlayMusic(_lobbyBackgroundMusic, loop: true);
    }

    private static void TryStopLobbyBgm()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        GsiAudio.StopMusic();
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
        GsiUiAppearance.Changed += OnLobbyAppearanceChanged;
        _appearanceSubscribed = true;
    }

    private void TryUnsubscribeAppearance()
    {
        if (!_appearanceSubscribed)
        {
            return;
        }

        GsiUiAppearance.Changed -= OnLobbyAppearanceChanged;
        _appearanceSubscribed = false;
    }

    private void OnLobbyAppearanceChanged()
    {
        ApplyLobbyChrome();
    }

    private void TrySubscribeLocaleChanged()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return;
        }
#endif
        if (_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged += OnLobbyLocaleChanged;
        _localeSubscribed = true;
    }

    private void TryUnsubscribeLocaleChanged()
    {
        if (!_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged -= OnLobbyLocaleChanged;
        _localeSubscribed = false;
    }

    private void OnLobbyLocaleChanged()
    {
        ApplyLobbyChrome();
    }

    private void EnsureLobbyShell()
    {
        if (_lobbyShellBuilt || _lobbyRoot == null)
        {
            return;
        }

        RemoveLegacyLobbyHeaderAndSettings();
        BuildLobbyActionRow();
        EnsureLobbyEconomyStrip();

        _lobbyShellBuilt = true;
    }

    private void RemoveLegacyLobbyHeaderAndSettings()
    {
        if (_lobbyRoot == null)
        {
            return;
        }

        Transform legacyHeader = _lobbyRoot.Find(LobbyHeaderBarLegacyName);
        if (legacyHeader != null)
        {
            if (_tokenText != null && _tokenText.transform.IsChildOf(legacyHeader))
            {
                _tokenText.transform.SetParent(_lobbyRoot, false);
            }

            if (_ticketText != null && _ticketText.transform.IsChildOf(legacyHeader))
            {
                _ticketText.transform.SetParent(_lobbyRoot, false);
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(legacyHeader.gameObject);
            }
            else
#endif
            {
                Destroy(legacyHeader.gameObject);
            }
        }

        Transform looseSettings = _lobbyRoot.Find("SettingsButton");
        if (looseSettings != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(looseSettings.gameObject);
            }
            else
#endif
            {
                Destroy(looseSettings.gameObject);
            }
        }
    }

    private void EnsureLobbyEconomyStrip()
    {
        if (_lobbyRoot == null || (_tokenText == null && _ticketText == null))
        {
            return;
        }

        Transform stripTf = _lobbyRoot.Find(LobbyEconomyStripName);
        RectTransform stripRt;
        if (stripTf == null)
        {
            var stripGo = new GameObject(LobbyEconomyStripName);
            stripRt = stripGo.AddComponent<RectTransform>();
            stripRt.SetParent(_lobbyRoot, false);
            stripRt.anchorMin = new Vector2(0.5f, 0f);
            stripRt.anchorMax = new Vector2(0.5f, 0f);
            stripRt.pivot = new Vector2(0.5f, 0f);
            stripRt.sizeDelta = new Vector2(1000f, 48f);
            stripRt.anchoredPosition = new Vector2(0f, 36f + 118f + 18f);

            var hor = stripGo.AddComponent<HorizontalLayoutGroup>();
            hor.spacing = 36f;
            hor.padding = new RectOffset(20, 20, 6, 6);
            hor.childAlignment = TextAnchor.MiddleCenter;
            hor.childControlWidth = true;
            hor.childControlHeight = true;
            hor.childForceExpandWidth = false;
            hor.childForceExpandHeight = true;
        }
        else
        {
            stripRt = stripTf.GetComponent<RectTransform>();
        }

        if (_tokenText != null)
        {
            _tokenText.transform.SetParent(stripRt, false);
            _tokenText.alignment = TextAlignmentOptions.Midline;
            LayoutElement tLe = _tokenText.gameObject.GetComponent<LayoutElement>();
            if (tLe == null)
            {
                tLe = _tokenText.gameObject.AddComponent<LayoutElement>();
            }

            tLe.preferredWidth = 420f;
            tLe.flexibleWidth = 1f;
        }

        if (_ticketText != null)
        {
            _ticketText.transform.SetParent(stripRt, false);
            _ticketText.alignment = TextAlignmentOptions.Midline;
            LayoutElement tLe = _ticketText.gameObject.GetComponent<LayoutElement>();
            if (tLe == null)
            {
                tLe = _ticketText.gameObject.AddComponent<LayoutElement>();
            }

            tLe.preferredWidth = 420f;
            tLe.flexibleWidth = 1f;
        }

        EnsureEconomyStripBackground(stripRt);
        stripRt.SetAsLastSibling();
    }

    private static void EnsureEconomyStripBackground(RectTransform stripRt)
    {
        if (stripRt == null)
        {
            return;
        }

        Transform stripRoot = stripRt;
        Transform bgTf = stripRoot.Find("StripBg");
        if (bgTf == null)
        {
            var bgGo = new GameObject("StripBg");
            RectTransform bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.SetParent(stripRoot, false);
            GsiUiRuntimeWidgets.StretchFull(bgRt);
            var img = bgGo.AddComponent<Image>();
            img.color = GsiUiAppearance.LobbyEconomyStripGlass;
            img.raycastTarget = false;
            GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(img);
            bgRt.SetAsFirstSibling();
        }
        else if (bgTf.TryGetComponent(out Image existing))
        {
            existing.color = GsiUiAppearance.LobbyEconomyStripGlass;
            GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(existing);
        }
    }

    private void BuildLobbyActionRow()
    {
        if (_lobbyRoot == null || _lobbyRoot.Find(LobbyActionRowName) != null)
        {
            return;
        }

        Transform gsi = _lobbyRoot.Find("GSIButton");
        Transform shop = _lobbyRoot.Find("ShopButton");
        if (gsi == null || shop == null)
        {
            return;
        }

        var rowGo = new GameObject(LobbyActionRowName);
        RectTransform rowRt = rowGo.AddComponent<RectTransform>();
        rowRt.SetParent(_lobbyRoot, false);
        rowRt.anchorMin = new Vector2(0.5f, 0f);
        rowRt.anchorMax = new Vector2(0.5f, 0f);
        rowRt.pivot = new Vector2(0.5f, 0f);
        rowRt.sizeDelta = new Vector2(1040f, 118f);
        rowRt.anchoredPosition = new Vector2(0f, 40f);

        var hor = rowGo.AddComponent<HorizontalLayoutGroup>();
        hor.spacing = 22f;
        hor.padding = new RectOffset(16, 16, 0, 0);
        hor.childAlignment = TextAnchor.MiddleCenter;
        hor.childControlWidth = true;
        hor.childControlHeight = true;
        hor.childForceExpandWidth = true;
        hor.childForceExpandHeight = true;

        gsi.SetParent(rowRt, false);
        shop.SetParent(rowRt, false);

        Transform inv = _lobbyRoot.Find("InventoryButton");
        if (inv == null)
        {
            Button invBtn = CreateLobbyActionButton(rowRt, "InventoryButton",
                GameLocalization.GetUiString(UiStringKeys.UiLobbyInventory, "Inventory"));
            _openInventoryButton = invBtn;
        }
        else
        {
            inv.SetParent(rowRt, false);
            _openInventoryButton = inv.GetComponent<Button>();
        }

        rowRt.sizeDelta = new Vector2(1320f, 118f);

        Transform altar = _lobbyRoot.Find("AltarOfVerityButton");
        if (altar == null)
        {
            Button altarBtn = CreateLobbyActionButton(rowRt, "AltarOfVerityButton",
                GameLocalization.GetUiString(UiStringKeys.UiLobbyAltarOfVerity, "Altar of Verity"));
            _openAltarOfVerityButton = altarBtn;
        }
        else
        {
            altar.SetParent(rowRt, false);
            _openAltarOfVerityButton = altar.GetComponent<Button>();
        }

        LayoutElement gsiLe = gsi.gameObject.GetComponent<LayoutElement>();
        if (gsiLe == null)
        {
            gsiLe = gsi.gameObject.AddComponent<LayoutElement>();
        }

        gsiLe.minHeight = 104f;
        gsiLe.preferredHeight = 108f;

        LayoutElement shopLe = shop.gameObject.GetComponent<LayoutElement>();
        if (shopLe == null)
        {
            shopLe = shop.gameObject.AddComponent<LayoutElement>();
        }

        shopLe.minHeight = 104f;
        shopLe.preferredHeight = 108f;

        Transform invTf = rowRt.Find("InventoryButton");
        if (invTf != null)
        {
            LayoutElement invLe = invTf.gameObject.GetComponent<LayoutElement>();
            if (invLe == null)
            {
                invLe = invTf.gameObject.AddComponent<LayoutElement>();
            }

            invLe.minHeight = 104f;
            invLe.preferredHeight = 108f;
        }

        Transform altarTf = rowRt.Find("AltarOfVerityButton");
        if (altarTf != null)
        {
            LayoutElement altarLe = altarTf.gameObject.GetComponent<LayoutElement>();
            if (altarLe == null)
            {
                altarLe = altarTf.gameObject.AddComponent<LayoutElement>();
            }

            altarLe.minHeight = 104f;
            altarLe.preferredHeight = 108f;
        }

        rowRt.SetAsLastSibling();
    }

    private void ApplyLobbyChrome()
    {
        ApplyLobbyLocalizedTexts();

        EnsureLobbyBackdrop();

        if (_panelBackground != null)
        {
            _panelBackground.color = GsiUiAppearance.ShopScreenBackground;
            _panelBackground.raycastTarget = false;
        }

        if (_heroImage != null)
        {
            _heroImage.enabled = false;
            _heroImage.raycastTarget = false;
        }

        ApplyLobbyPrimaryButton(_enterGsiFacilityButton);
        ApplyLobbySecondaryButton(_openShopButton);
        ApplyLobbySecondaryButton(_openInventoryButton);
        ApplyLobbySecondaryButton(_openAltarOfVerityButton);
        LobbyButtonLabelHoverBoost.EnsureOn(_enterGsiFacilityButton);
        LobbyButtonLabelHoverBoost.EnsureOn(_openShopButton);
        LobbyButtonLabelHoverBoost.EnsureOn(_openInventoryButton);
        LobbyButtonLabelHoverBoost.EnsureOn(_openAltarOfVerityButton);
        UpdateEconomyTexts();
    }

    /// <summary>Removes legacy LobbyBackdrop RawImage child; root panel Image follows <see cref="GsiUiAppearance"/>.</summary>
    private void EnsureLobbyBackdrop()
    {
        if (_lobbyRoot == null)
        {
            return;
        }

        Transform backdropTf = _lobbyRoot.Find(LobbyBackdropObjectName);
        if (backdropTf == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(backdropTf.gameObject);
            return;
        }
#endif
        Destroy(backdropTf.gameObject);
    }


    private void ApplyLobbyLocalizedTexts()
    {
        ApplyLobbyButtonLabel(_enterGsiFacilityButton, UiStringKeys.UiLobbyStart, "Start");
        ApplyLobbyButtonLabel(_openShopButton, UiStringKeys.UiLobbyShop, "Shop");
        ApplyLobbyButtonLabel(_openInventoryButton, UiStringKeys.UiLobbyInventory, "Inventory");
        ApplyLobbyButtonLabel(_openAltarOfVerityButton, UiStringKeys.UiLobbyAltarOfVerity, "Altar of Verity");
    }

    private static void ApplyLobbyButtonLabel(Button btn, string localizationKey, string englishFallback)
    {
        if (btn == null)
        {
            return;
        }

        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null)
        {
            return;
        }

        tmp.text = GameLocalization.GetUiString(localizationKey, englishFallback);
    }

    private static void ApplyLobbyPrimaryButton(Button btn)
    {
        if (btn == null)
        {
            return;
        }

        if (btn.targetGraphic is Image img)
        {
            GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(img);
            img.color = LobbyActionButtonBackgroundClear;
            img.raycastTarget = true;
        }

        btn.transition = Selectable.Transition.None;

        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.color = LobbyPrimaryActionLabelColor();
            tmp.fontSize = 24f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.characterSpacing = 0.4f;
            tmp.alignment = TextAlignmentOptions.Midline;
        }

        EnsureLobbyActionLabelRuleLines(btn, LobbyPrimaryActionRuleLineColor());
        ApplyLobbyButtonColorTint(btn);
    }

    private static Color LobbyPrimaryActionLabelColor()
    {
        if (CosmeticSkinPalettes.TryGetActive(out _))
        {
            return GsiUiAppearance.TextPrimary;
        }

        return GsiUiAppearance.Mode == GsiUiAppearanceMode.Dark
            ? Color.white
            : GsiUiAppearance.TextPrimary;
    }

    private static Color LobbyPrimaryActionRuleLineColor()
    {
        if (CosmeticSkinPalettes.TryGetActive(out _))
        {
            Color c = GsiUiAppearance.TextPrimary;
            c.a = Mathf.Clamp01(c.a * 0.45f);
            return c;
        }

        if (GsiUiAppearance.Mode == GsiUiAppearanceMode.Dark)
        {
            return new Color(1f, 1f, 1f, 0.42f);
        }

        Color c2 = GsiUiAppearance.TextPrimary;
        c2.a = Mathf.Clamp01(c2.a * 0.45f);
        return c2;
    }

    private static void ApplyLobbySecondaryButton(Button btn)
    {
        if (btn == null)
        {
            return;
        }

        if (btn.targetGraphic is Image img)
        {
            GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(img);
            img.color = LobbyActionButtonBackgroundClear;
            img.raycastTarget = true;
        }

        btn.transition = Selectable.Transition.None;

        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.color = GsiUiAppearance.TextPrimary;
            tmp.fontSize = 23f;
            tmp.fontStyle = FontStyles.Normal;
            tmp.characterSpacing = 0.25f;
            tmp.alignment = TextAlignmentOptions.Midline;
        }

        Color line = GsiUiAppearance.TextPrimary;
        line.a = Mathf.Clamp01(line.a * 0.48f);
        EnsureLobbyActionLabelRuleLines(btn, line);
        ApplyLobbyButtonColorTint(btn);
    }

    /// <summary>로비 액션 라벨 위·아래 얇은 선(다크/라이트에 맞춰 색은 호출부에서 지정).</summary>
    private static void EnsureLobbyActionLabelRuleLines(Button btn, Color lineColor)
    {
        if (btn == null)
        {
            return;
        }

        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null)
        {
            return;
        }

        Transform rulesTf = btn.transform.Find(LobbyLabelRulesRootName);
        RectTransform rulesRt;
        if (rulesTf == null)
        {
            var rulesGo = new GameObject(LobbyLabelRulesRootName, typeof(RectTransform));
            rulesRt = rulesGo.GetComponent<RectTransform>();
            rulesRt.SetParent(btn.transform, false);
            GsiUiRuntimeWidgets.StretchFull(rulesRt);
            rulesRt.SetAsLastSibling();

            var passThrough = rulesGo.AddComponent<CanvasGroup>();
            passThrough.interactable = false;
            passThrough.blocksRaycasts = false;
            passThrough.alpha = 1f;

            var vlg = rulesGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(28, 28, 12, 12);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            CreateLobbyLabelRuleImage(rulesRt, LobbyLabelRuleTopName);
            tmp.transform.SetParent(rulesRt, false);
            CreateLobbyLabelRuleImage(rulesRt, LobbyLabelRuleBottomName);

            LayoutElement textLe = tmp.gameObject.GetComponent<LayoutElement>();
            if (textLe == null)
            {
                textLe = tmp.gameObject.AddComponent<LayoutElement>();
            }

            textLe.flexibleHeight = 1f;
            textLe.minHeight = 26f;
            textLe.flexibleWidth = 1f;
        }
        else
        {
            rulesRt = (RectTransform)rulesTf;
        }

        Transform topTf = rulesRt.Find(LobbyLabelRuleTopName);
        if (topTf != null && topTf.TryGetComponent(out Image topImg))
        {
            topImg.color = lineColor;
            topImg.raycastTarget = false;
            GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(topImg);
        }

        Transform bottomTf = rulesRt.Find(LobbyLabelRuleBottomName);
        if (bottomTf != null && bottomTf.TryGetComponent(out Image bottomImg))
        {
            bottomImg.color = lineColor;
            bottomImg.raycastTarget = false;
            GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(bottomImg);
        }
    }

    private static void CreateLobbyLabelRuleImage(RectTransform parent, string objectName)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, 2f);

        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(img);
        img.color = Color.white;

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 2f;
        le.minHeight = 2f;
        le.flexibleWidth = 1f;
    }

    private static void ApplyLobbyButtonColorTint(Button btn)
    {
        if (btn == null)
        {
            return;
        }

        ColorBlock colors = btn.colors;
        colors.fadeDuration = 0.1f;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 1f);
        btn.colors = colors;
    }

    private void OnEnterGsiFacilityClicked()
    {
        GsiSceneNavigation.LoadGsiFacility();
    }

    private void OnOpenShopClicked()
    {
        GsiSceneNavigation.LoadShop();
    }

    private void OnOpenInventoryClicked()
    {
        GsiSceneNavigation.LoadInventory();
    }

    private void OnOpenAltarOfVerityClicked()
    {
        GsiSceneNavigation.LoadAltarOfVerity();
    }

    private void HideLegacyGsiEntryButtons()
    {
        void Hide(Button b)
        {
            if (b != null)
            {
                b.gameObject.SetActive(false);
            }
        }

        Hide(_practiceButton);
        Hide(_aimPracticeButton);
        Hide(_reactionExamButton);
        Hide(_aimExamButton);
    }

    private void OnPracticeClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetTestType(TestType.Practice);
        GameManager.Instance.SetTestMode(TestMode.Reaction);
        GameManager.Instance.SetGameState(GameState.TestBriefing);
    }

    private void OnAimPracticeClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetTestType(TestType.Practice);
        GameManager.Instance.SetTestMode(TestMode.AimPrecision);
        GameManager.Instance.SetGameState(GameState.TestBriefing);
    }

    private void OnReactionExamClicked()
    {
        GsiSceneNavigation.LoadGsiFacility();
    }

    private void OnAimExamClicked()
    {
        GsiSceneNavigation.LoadGsiFacility();
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.MainMenu)
        {
            UpdateEconomyTexts();
        }
    }

    private void HandleEconomyChanged()
    {
        UpdateEconomyTexts();
    }

    private void UpdateEconomyTexts()
    {
        if (_tokenText != null)
        {
            int gold = EconomyManager.Instance != null ? EconomyManager.Instance.Tokens : 0;
            _tokenText.text = GameLocalization.FormatUiString(UiStringKeys.LobbyCurrencyGoldFmt, "Gold: {0}", gold);
            _tokenText.color = GsiUiAppearance.ShopGoldText;
            _tokenText.fontSize = 24f;
            GsiUiRuntimeWidgets.ApplyEconomyLineTypography(_tokenText);
        }

        if (_ticketText != null)
        {
            int tickets = EconomyManager.Instance != null ? EconomyManager.Instance.ExamTickets : 0;
            _ticketText.text =
                GameLocalization.FormatUiString(UiStringKeys.LobbyCurrencyTicketFmt, "Exam tickets: {0}", tickets);
            _ticketText.color = GsiUiAppearance.ShopTicketText;
            _ticketText.fontSize = 24f;
            GsiUiRuntimeWidgets.ApplyEconomyLineTypography(_ticketText);
        }
    }

    private Button CreateLobbyActionButton(RectTransform rowParent, string objectName, string label)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(rowParent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 104f;
        le.preferredHeight = 108f;
        le.flexibleWidth = 1f;

        var img = go.AddComponent<Image>();
        GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(img);
        img.color = LobbyActionButtonBackgroundClear;
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.None;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tRt = textGo.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (TmpFontCache.LiberationSansSdf != null)
        {
            tmp.font = TmpFontCache.LiberationSansSdf;
        }

        tmp.text = label;
        tmp.fontSize = 23f;
        tmp.fontStyle = FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = GsiUiAppearance.TextPrimary;
        tmp.characterSpacing = 0.25f;
        tmp.raycastTarget = false;

        ApplyLobbyButtonColorTint(btn);
        return btn;
    }

#if UNITY_EDITOR
    private void MarkLobbySceneDirtyIfNeeded()
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
