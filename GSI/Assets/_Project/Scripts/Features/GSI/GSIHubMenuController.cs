using System;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ArchE.Game;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// G.S.I 시설 로비: 연습·공식 시험 진입. ArchE 연동 빌드에서만 허브로 돌아가는 버튼을 표시합니다.
/// </summary>
[ExecuteAlways]
public sealed class GSIHubMenuController : MonoBehaviour
{
    private const string HubHeaderObjectName = "GsiHubHeader";
    private const string HubSubBarObjectName = "GsiHubSubBar";
    private const string HubHeaderSpacerName = "HubHeaderLeftSpacer";

    [SerializeField] private Button _backToArchEButton;
    [SerializeField] private TextMeshProUGUI _tokenText;
    [SerializeField] private TextMeshProUGUI _astralCoreText;
    [SerializeField] private TextMeshProUGUI _ticketText;

    private Image _lobbyPanelImage;
    private TextMeshProUGUI _facilityTitleTmp;
    private bool _appearanceSubscribed;
    private bool _localeSubscribed;

    private RectTransform _cosmicStage;
    private GsiGsiOrbitalSelector _activeSelector;

    private readonly Dictionary<TestMode, Button> _practiceButtonsByMode = new Dictionary<TestMode, Button>();
    private readonly List<(Button Button, UnityAction Action)> _practiceClickBindings = new List<(Button, UnityAction)>();

    private Button[][] _gradeButtonsByMode;
    private UnityAction[][] _gradeClickActionsByMode;

    private int _unifiedExamSelectedGrade = 9;
    private Button[] _unifiedGradeButtons;
    private UnityAction[] _unifiedGradeClickActions;

    private void Awake()
    {
        if (_backToArchEButton != null)
        {
            _backToArchEButton.onClick.AddListener(OnBackToArchEClicked);
        }
    }

    private void Start()
    {
        BuildHubRuntimeLayout();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnEconomyChanged += HandleEconomyChanged;
        }

        if (Application.isPlaying)
        {
            GsiDecoPanelController.EnsureCreated();
        }
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            return;
        }

        GameLocalization.TryInitializeSynchronouslyForEditorSceneView();
        BuildHubRuntimeLayout();
        MarkHubSceneDirtyIfNeeded();
    }
#endif

    private void BuildHubRuntimeLayout()
    {
        EnsureHubTopChrome();
        CacheHubVisualRefs();

        // 런타임 및 에디터 모두에서 반응속도 난이도 삭제 가이드가 무결하게 동작하도록 항시 레이아웃 생성
        EnsurePracticeGradeLayout();

        if (Application.isPlaying)
        {
            BuildCosmicHubStage();
        }

        ApplyHubLocalizedUiTexts();
        UpdateEconomyTexts();
        ApplyHubChrome();
        TrySubscribeAppearance();
        TrySubscribeLocaleChanged();
    }

    private void CacheHubVisualRefs()
    {
        _lobbyPanelImage = GetComponent<Image>();
        Transform layout = transform.Find("Layout");
        if (layout != null)
        {
            Transform titleTr = layout.Find("Title");
            if (titleTr == null)
            {
                Transform header = transform.Find(HubHeaderObjectName);
                if (header != null)
                {
                    titleTr = header.Find("Title");
                }
            }

            if (titleTr != null)
            {
                _facilityTitleTmp = titleTr.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    /// <summary>
    /// 상점/인벤토리와 동일한 상단 헤더 스트립 (제목 중앙, Back 우측) + 서브바 (골드/입장권) 로 쪼개고, 본문 Layout은 그 아래에 꽉 차게 시작하도록 맞추었다.
    /// </summary>
    private void EnsureHubTopChrome()
    {
        Transform layout = transform.Find("Layout");
        if (layout == null)
        {
            return;
        }

        Transform headerTf = transform.Find(HubHeaderObjectName);
        Transform subBarTf = transform.Find(HubSubBarObjectName);

        if (headerTf == null || subBarTf == null)
        {
            var headerGo = GsiUiRuntimeWidgets.CreateUiObject(HubHeaderObjectName, transform);
            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0f, GsiUiScreenLayout.HeaderStripHeight);
            var headerStrip = headerGo.AddComponent<Image>();
            headerStrip.color = GsiUiAppearance.ShopHeaderStrip(CosmeticTheme.UiAccent);
            headerStrip.raycastTarget = false;
            var headerStripLe = headerGo.AddComponent<LayoutElement>();
            headerStripLe.preferredHeight = GsiUiScreenLayout.HeaderStripHeight;
            headerStripLe.flexibleWidth = 1f;
            var headerH = headerGo.AddComponent<HorizontalLayoutGroup>();
            headerH.padding = GsiUiScreenLayout.HeaderStripPadding;
            headerH.spacing = GsiUiScreenLayout.HeaderRowSpacing;
            headerH.childAlignment = TextAnchor.MiddleLeft;
            headerH.childForceExpandHeight = true;
            headerH.childForceExpandWidth = true;

            var spacer = GsiUiRuntimeWidgets.CreateUiObject(HubHeaderSpacerName, headerGo.transform);
            var spacerLe = spacer.AddComponent<LayoutElement>();
            spacerLe.preferredWidth = GsiUiScreenLayout.SettingsHeaderButtonWidth;
            spacerLe.flexibleWidth = 0f;

            Transform titleTr = layout.Find("Title");
            if (titleTr != null)
            {
                titleTr.SetParent(headerGo.transform, false);
                var titleLe = titleTr.GetComponent<LayoutElement>();
                if (titleLe == null)
                {
                    titleLe = titleTr.gameObject.AddComponent<LayoutElement>();
                }

                titleLe.flexibleWidth = 1f;
                titleLe.preferredHeight = -1f;
            }

            if (_backToArchEButton != null)
            {
                Transform backTr = _backToArchEButton.transform;
                backTr.SetParent(headerGo.transform, false);
                LayoutElement[] backLes = backTr.GetComponents<LayoutElement>();
                for (int bi = 1; bi < backLes.Length; bi++)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        UnityEngine.Object.DestroyImmediate(backLes[bi]);
                    }
                    else
#endif
                    {
                        UnityEngine.Object.Destroy(backLes[bi]);
                    }
                }

                LayoutElement backLe = backTr.GetComponent<LayoutElement>();
                if (backLe == null)
                {
                    backLe = backTr.gameObject.AddComponent<LayoutElement>();
                }

                backLe.preferredWidth = GsiUiScreenLayout.BackHeaderButtonWidth;
                backLe.flexibleWidth = 0f;
                backLe.minHeight = -1f;
                backLe.preferredHeight = -1f;
            }

            var subBarGo = GsiUiRuntimeWidgets.CreateUiObject(HubSubBarObjectName, transform);
            var subRt = subBarGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 1f);
            subRt.anchorMax = new Vector2(1f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0f, GsiUiScreenLayout.SubBarOffsetBelowHeader);
            subRt.sizeDelta = new Vector2(0f, GsiUiScreenLayout.SubBarHeight);
            var subH = subBarGo.AddComponent<HorizontalLayoutGroup>();
            subH.padding = GsiUiScreenLayout.SubBarPadding;
            subH.spacing = GsiUiScreenLayout.SubBarItemSpacing;
            subH.childAlignment = TextAnchor.MiddleLeft;

            Transform economyRow = layout.Find("EconomyRow");
            if (economyRow != null)
            {
                economyRow.SetParent(subBarGo.transform, false);
                var erRt = economyRow.GetComponent<RectTransform>();
                erRt.anchorMin = Vector2.zero;
                erRt.anchorMax = Vector2.one;
                erRt.offsetMin = Vector2.zero;
                erRt.offsetMax = Vector2.zero;
                var erLe = economyRow.GetComponent<LayoutElement>();
                if (erLe == null)
                {
                    erLe = economyRow.gameObject.AddComponent<LayoutElement>();
                }

                erLe.flexibleWidth = 1f;
                erLe.minHeight = GsiUiScreenLayout.SubBarHeight - 8f;
                erLe.preferredHeight = GsiUiScreenLayout.SubBarHeight;
            }

            headerTf = headerGo.transform;
            subBarTf = subBarGo.transform;
        }

        ApplyHubMainLayoutTopInset(layout);

        VerticalLayoutGroup mainV = layout.GetComponent<VerticalLayoutGroup>();
        if (mainV != null)
        {
            mainV.padding.top = 24;
        }

        EnforceHubHeaderChildOrder(transform.Find(HubHeaderObjectName));
        ApplyHubHeaderFlexibleLayout(transform.Find(HubHeaderObjectName));
        ApplyHubBackButtonLabel();

        if (_backToArchEButton != null)
        {
            GsiUiRuntimeWidgets.ApplySecondaryHeaderButtonLook(_backToArchEButton);
        }

        if (headerTf != null)
        {
            headerTf.SetAsLastSibling();
        }

        if (subBarTf != null)
        {
            subBarTf.SetAsLastSibling();
        }
    }

    private static void ApplyHubMainLayoutTopInset(Transform layout)
    {
        var layoutRt = layout.GetComponent<RectTransform>();
        if (layoutRt == null)
        {
            return;
        }

        float inset = GsiUiScreenLayout.HeaderStripHeight + GsiUiScreenLayout.SubBarHeight;
        layoutRt.offsetMax = new Vector2(layoutRt.offsetMax.x, -inset);
    }

    private static void EnforceHubHeaderChildOrder(Transform headerRoot)
    {
        if (headerRoot == null)
        {
            return;
        }

        Transform spacer = headerRoot.Find(HubHeaderSpacerName);
        Transform title = headerRoot.Find("Title");
        Transform back = headerRoot.Find("BackToArchE");
        if (spacer != null)
        {
            spacer.SetSiblingIndex(0);
        }

        if (title != null)
        {
            title.SetSiblingIndex(1);
        }

        if (back != null)
        {
            back.SetAsLastSibling();
        }
    }

    private static void ApplyHubHeaderFlexibleLayout(Transform headerRoot)
    {
        if (headerRoot == null)
        {
            return;
        }

        HorizontalLayoutGroup hlg = headerRoot.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.childForceExpandWidth = true;
            hlg.childControlWidth = true;
        }

        Transform spacer = headerRoot.Find(HubHeaderSpacerName);
        if (spacer != null)
        {
            LayoutElement le = spacer.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = spacer.gameObject.AddComponent<LayoutElement>();
            }

            le.flexibleWidth = 0f;
            le.preferredWidth = GsiUiScreenLayout.SettingsHeaderButtonWidth;
        }

        Transform title = headerRoot.Find("Title");
        if (title != null)
        {
            LayoutElement le = title.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = title.gameObject.AddComponent<LayoutElement>();
            }

            le.flexibleWidth = 1f;
        }

        Transform back = headerRoot.Find("BackToArchE");
        if (back != null)
        {
            LayoutElement le = back.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = back.gameObject.AddComponent<LayoutElement>();
            }

            le.flexibleWidth = 0f;
            le.preferredWidth = GsiUiScreenLayout.BackHeaderButtonWidth;
        }
    }

    private void ApplyHubBackButtonLabel()
    {
        if (_backToArchEButton == null)
        {
            return;
        }

        TextMeshProUGUI label = _backToArchEButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = GameLocalization.GetUiString(UiStringKeys.HubBack, "Back");
        }
    }

#if UNITY_EDITOR
    private void MarkHubSceneDirtyIfNeeded()
    {
        if (Application.isPlaying)
        {
            return;
        }

        EditorUtility.SetDirty(gameObject);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    private void OnDestroy()
    {
        if (_activeSelector != null)
        {
            Destroy(_activeSelector.gameObject);
        }

        TryUnsubscribeLocaleChanged();
        TryUnsubscribeAppearance();
        if (_backToArchEButton != null)
        {
            _backToArchEButton.onClick.RemoveListener(OnBackToArchEClicked);
        }

        ClearPracticeClickBindings();

        if (_gradeButtonsByMode != null && _gradeClickActionsByMode != null)
        {
            for (int m = 0; m < _gradeButtonsByMode.Length && m < _gradeClickActionsByMode.Length; m++)
            {
                if (_gradeButtonsByMode[m] == null || _gradeClickActionsByMode[m] == null)
                {
                    continue;
                }

                for (int g = 0; g < _gradeButtonsByMode[m].Length && g < _gradeClickActionsByMode[m].Length; g++)
                {
                    if (_gradeButtonsByMode[m][g] != null && _gradeClickActionsByMode[m][g] != null)
                    {
                        _gradeButtonsByMode[m][g].onClick.RemoveListener(_gradeClickActionsByMode[m][g]);
                    }
                }
            }
        }

        if (_unifiedGradeButtons != null && _unifiedGradeClickActions != null)
        {
            for (int g = 0; g < _unifiedGradeButtons.Length && g < _unifiedGradeClickActions.Length; g++)
            {
                if (_unifiedGradeButtons[g] != null && _unifiedGradeClickActions[g] != null)
                {
                    _unifiedGradeButtons[g].onClick.RemoveListener(_unifiedGradeClickActions[g]);
                }
            }
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnEconomyChanged -= HandleEconomyChanged;
        }
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
        GsiUiAppearance.Changed += OnHubAppearanceChanged;
        _appearanceSubscribed = true;
    }

    private void TryUnsubscribeAppearance()
    {
        if (!_appearanceSubscribed)
        {
            return;
        }

        GsiUiAppearance.Changed -= OnHubAppearanceChanged;
        _appearanceSubscribed = false;
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

        GameLocalization.UiLocaleChanged += OnHubLocaleChanged;
        _localeSubscribed = true;
    }

    private void TryUnsubscribeLocaleChanged()
    {
        if (!_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged -= OnHubLocaleChanged;
        _localeSubscribed = false;
    }

    private void OnHubLocaleChanged()
    {
        ApplyHubLocalizedUiTexts();
        UpdateEconomyTexts();
        ApplyHubChrome();
    }

    private void ApplyHubLocalizedUiTexts()
    {
        ApplyHubBackButtonLabel();
        ApplyHubPracticeSectionTitleText();
        ApplyHubPracticeModeButtonLabels();
        ApplyHubPracticeGradeLegendText();
        ApplyHubUnifiedExamBlockTexts();
    }

    private void ApplyHubPracticeModeButtonLabels()
    {
        IReadOnlyList<GsiHubPracticeCatalog.Entry> entries = GsiHubPracticeCatalog.All;
        for (int i = 0; i < entries.Count; i++)
        {
            GsiHubPracticeCatalog.Entry e = entries[i];
            if (_practiceButtonsByMode.TryGetValue(e.Mode, out Button btn))
            {
                ApplyLocalizedLabelOnButton(btn, e.LocalizationKey, e.EnglishFallback);
            }
        }
    }

    private static void ApplyLocalizedLabelOnButton(Button btn, string localizationKey, string englishFallback)
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

        tmp.text = GameLocalization.GetUiString(localizationKey, englishFallback);
    }

    private void ApplyHubPracticeSectionTitleText()
    {
        Transform layout = transform.Find("Layout");
        if (layout == null)
        {
            return;
        }

        Transform practiceBlock = layout.Find("PracticeBlock");
        if (practiceBlock == null)
        {
            return;
        }

        Transform sec = practiceBlock.Find("PracticeSectionTitle");
        if (sec == null)
        {
            return;
        }

        TextMeshProUGUI tmp = sec.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = GameLocalization.GetUiString(UiStringKeys.HubSectionPractice, "Practice");
        }
    }

    private void ApplyHubPracticeGradeLegendText()
    {
        Transform layout = transform.Find("Layout");
        if (layout == null)
        {
            return;
        }

        Transform practiceBlock = layout.Find("PracticeBlock");
        if (practiceBlock == null)
        {
            return;
        }

        Transform leg = practiceBlock.Find("PracticeGradeLegend");
        if (leg == null)
        {
            return;
        }

        TextMeshProUGUI tmp = leg.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            return;
        }

        tmp.text = GameLocalization.GetUiString(UiStringKeys.HubPracticeGradeLegend,
            "Practice rank: Grade 1 = hardest, Grade 9 = entry (per mode)");
    }

    private void ApplyHubUnifiedExamBlockTexts()
    {
        Transform layout = transform.Find("Layout");
        if (layout == null)
        {
            return;
        }

        Transform block = layout.Find("UnifiedExamBlock");
        if (block == null)
        {
            return;
        }

        Transform header = block.Find("UnifiedExamHeader");
        if (header != null)
        {
            TextMeshProUGUI h = header.GetComponent<TextMeshProUGUI>();
            if (h != null)
            {
                h.text = GameLocalization.GetUiString(UiStringKeys.HubUnifiedTitle, "Unified official exam");
            }
        }

        Transform hint = block.Find("UnifiedExamGradeHint");
        if (hint != null)
        {
            TextMeshProUGUI ht = hint.GetComponent<TextMeshProUGUI>();
            if (ht != null)
            {
                ht.text = GameLocalization.GetUiString(UiStringKeys.HubUnifiedHint,
                    "Seven subjects in random order. Pass lines and final total use the rank below.");
            }
        }

        Transform startTr = block.Find("UnifiedOfficialExamStart");
        if (startTr != null)
        {
            Button startBtn = startTr.GetComponent<Button>();
            ApplyLocalizedLabelOnButton(startBtn, UiStringKeys.HubUnifiedStart, "Start unified exam (1 exam ticket)");
        }

        Transform histTr = block.Find("UnifiedExamHistoryButton");
        if (histTr != null)
        {
            ApplyLocalizedLabelOnButton(histTr.GetComponent<Button>(), UiStringKeys.HubUnifiedHistoryBtn, "Unified exam log");
        }
    }

    private void OnHubAppearanceChanged()
    {
        ApplyHubChrome();
    }

    private void ApplyHubChrome()
    {
        if (_lobbyPanelImage != null)
        {
            if (Application.isPlaying)
            {
                ProceduralSpaceBackground.ApplyToImage(_lobbyPanelImage);
            }
            else
            {
                _lobbyPanelImage.color = GsiUiAppearance.ShopScreenBackground;
            }
        }

        Transform hubHeader = transform.Find(HubHeaderObjectName);
        if (hubHeader != null)
        {
            Image hubHeaderStrip = hubHeader.GetComponent<Image>();
            if (hubHeaderStrip != null)
            {
                hubHeaderStrip.color = GsiUiAppearance.ShopHeaderStrip(CosmeticTheme.UiAccent);
            }
        }

        if (_facilityTitleTmp != null)
        {
            _facilityTitleTmp.text = GameLocalization.GetUiString(UiStringKeys.HubScreenTitle, "The Axiom");
            GsiUiScreenLayout.ApplyScreenTitleTypography(_facilityTitleTmp);
            _facilityTitleTmp.color = GsiUiAppearance.TextPrimary;
        }

        if (_tokenText != null)
        {
            _tokenText.color = GsiUiAppearance.ShopStardustText;
        }

        if (_astralCoreText != null)
        {
            _astralCoreText.color = GsiUiAppearance.ShopAstralCoreText;
        }

        if (_ticketText != null)
        {
            _ticketText.color = GsiUiAppearance.ShopTicketText;
        }

        ApplyHubBackButtonLabel();

        Transform layout = transform.Find("Layout");
        if (layout != null)
        {
            Transform leg = layout.Find("PracticeBlock/PracticeGradeLegend");
            if (leg != null)
            {
                TextMeshProUGUI legTmp = leg.GetComponent<TextMeshProUGUI>();
                if (legTmp != null)
                {
                    legTmp.color = GsiUiAppearance.TextSecondary;
                }
            }

            Transform sec = layout.Find("PracticeBlock/PracticeSectionTitle");
            if (sec != null)
            {
                TextMeshProUGUI st = sec.GetComponent<TextMeshProUGUI>();
                if (st != null)
                {
                    st.color = GsiUiAppearance.TextPrimary;
                }
            }

            Transform uTitle = layout.Find("UnifiedExamBlock/UnifiedExamHeader");
            if (uTitle != null)
            {
                TextMeshProUGUI ut = uTitle.GetComponent<TextMeshProUGUI>();
                if (ut != null)
                {
                    ut.color = GsiUiAppearance.TextPrimary;
                }
            }

            Transform uHint = layout.Find("UnifiedExamBlock/UnifiedExamGradeHint");
            if (uHint != null)
            {
                TextMeshProUGUI uh = uHint.GetComponent<TextMeshProUGUI>();
                if (uh != null)
                {
                    uh.color = GsiUiAppearance.TextSecondary;
                }
            }

            ApplyHubSecondaryButtonStyles(layout);
        }

        RefreshPracticeGradeButtonColors();
        RefreshUnifiedGradeButtonColors();
    }

    private static void ApplyHubSecondaryButtonStyles(Transform layoutRoot)
    {
        foreach (Button btn in layoutRoot.GetComponentsInChildren<Button>(true))
        {
            if (btn.name.StartsWith("GradeBtn_", System.StringComparison.Ordinal))
            {
                continue;
            }

            if (btn.targetGraphic is Image img)
            {
                img.color = GsiUiAppearance.SecondaryButton;
            }

            TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.color = GsiUiAppearance.TextPrimary;
            }
        }
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

    private void OnBackToArchEClicked()
    {
        GsiSceneNavigation.LoadArchEHub();
    }

    private static void OnPracticeModeClicked(TestMode mode)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetTestType(TestType.Practice);
        GameManager.Instance.SetTestMode(mode);
        GameManager.Instance.SetGameState(GameState.TestBriefing);
    }

    private void OnUnifiedExamGradeClicked(int grade)
    {
        _unifiedExamSelectedGrade = Mathf.Clamp(grade, 1, 9);
        RefreshUnifiedGradeButtonColors();
    }

    private void OnUnifiedOfficialExamClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (GameManager.Instance.TryStartUnifiedExam(_unifiedExamSelectedGrade))
        {
            UpdateEconomyTexts();
            return;
        }

#if UNITY_EDITOR
        Debug.Log("Not enough exam tickets.");
#endif
    }

    private void RefreshUnifiedGradeButtonColors()
    {
        if (_unifiedGradeButtons == null)
        {
            return;
        }

        for (int i = 0; i < _unifiedGradeButtons.Length; i++)
        {
            if (_unifiedGradeButtons[i] == null)
            {
                continue;
            }

            var img = _unifiedGradeButtons[i].GetComponent<Image>();
            if (img != null)
            {
                img.color = i + 1 == _unifiedExamSelectedGrade
                    ? GsiUiAppearance.GradeStepSelectedFromAccent(CosmeticTheme.UiAccent)
                    : GsiUiAppearance.GradeStepNormal;
            }
        }
    }

    private void ClearPracticeClickBindings()
    {
        for (int i = 0; i < _practiceClickBindings.Count; i++)
        {
            (Button b, UnityAction a) = _practiceClickBindings[i];
            if (b != null && a != null)
            {
                b.onClick.RemoveListener(a);
            }
        }

        _practiceClickBindings.Clear();
        _practiceButtonsByMode.Clear();
    }

    private void EnsurePracticeButtonsFromCatalog(Transform layout)
    {
        TMP_FontAsset font = TmpFontCache.LiberationSansSdf;
        IReadOnlyList<GsiHubPracticeCatalog.Entry> entries = GsiHubPracticeCatalog.All;
        for (int i = 0; i < entries.Count; i++)
        {
            GsiHubPracticeCatalog.Entry e = entries[i];
            DestroyChildIfExists(layout, e.LegacyExamTransformName);
            Transform t = FindDescendantNamed(layout, e.PracticeTransformName);
            Button btn;
            if (t != null)
            {
                btn = t.GetComponent<Button>();
            }
            else
            {
                btn = CreateLobbyButton(layout, font, e.PracticeTransformName,
                    GameLocalization.GetUiString(e.LocalizationKey, e.EnglishFallback));
            }

            if (btn == null)
            {
                continue;
            }

            _practiceButtonsByMode[e.Mode] = btn;
            TestMode modeCapture = e.Mode;
            UnityAction handler = () => OnPracticeModeClicked(modeCapture);
            btn.onClick.RemoveListener(handler);
            btn.onClick.AddListener(handler);
            _practiceClickBindings.Add((btn, handler));
        }
    }

    private static Button CreateLobbyButton(Transform parent, TMP_FontAsset font, string name, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 48f;
        le.minHeight = 44f;
        le.flexibleWidth = 1f;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 48f);

        var img = go.AddComponent<Image>();
        img.color = GsiUiAppearance.SecondaryButton;
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tRt = textGo.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            tmp.font = font;
        }

        tmp.text = label;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = GsiUiAppearance.TextPrimary;
        tmp.raycastTarget = false;

        return btn;
    }

    private static Transform FindDescendantNamed(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name == name)
            {
                return c;
            }

            Transform found = FindDescendantNamed(c, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void EnsurePracticeGradeLayout()
    {
        Transform layout = transform.Find("Layout");
        if (layout == null)
        {
            return;
        }

        ClearPracticeClickBindings();
        EnsurePracticeButtonsFromCatalog(layout);

        IReadOnlyList<GsiHubPracticeCatalog.Entry> entries = GsiHubPracticeCatalog.All;
        int n = entries.Count;
        var practiceTransforms = new Transform[n];
        for (int i = 0; i < n; i++)
        {
            practiceTransforms[i] = FindDescendantNamed(layout, entries[i].PracticeTransformName);
            if (practiceTransforms[i] == null)
            {
                return;
            }
        }

        DestroyChildIfExists(layout, "OfficialExamSectionSpacer");
        DestroyChildIfExists(layout, "OfficialExamSectionHeader");
        DestroyChildIfExists(layout, "UnifiedExamTopSpacer");
        DestroyChildIfExists(layout, "UnifiedExamBlock");

        Transform oldBlock = layout.Find("PracticeBlock");
        if (oldBlock != null)
        {
            for (int i = 0; i < n; i++)
            {
                practiceTransforms[i].SetParent(layout, true);
            }

            GsiRuntimeUiBootstrap.DestroyObjectForRuntimeUi(oldBlock.gameObject);
        }

        int insertIndex = practiceTransforms[0].GetSiblingIndex();
        for (int i = 1; i < n; i++)
        {
            insertIndex = Mathf.Min(insertIndex, practiceTransforms[i].GetSiblingIndex());
        }

        _gradeButtonsByMode = new Button[n][];
        _gradeClickActionsByMode = new UnityAction[n][];
        for (int m = 0; m < n; m++)
        {
            _gradeButtonsByMode[m] = new Button[9];
            _gradeClickActionsByMode[m] = new UnityAction[9];
        }

        TMP_FontAsset font = TmpFontCache.LiberationSansSdf;

        var blockGo = new GameObject("PracticeBlock");
        blockGo.AddComponent<RectTransform>();
        var blockV = blockGo.AddComponent<VerticalLayoutGroup>();
        blockV.spacing = 6f;
        blockV.childAlignment = TextAnchor.UpperCenter;
        blockV.childControlWidth = true;
        blockV.childControlHeight = true;
        blockV.childForceExpandWidth = true;
        blockV.childForceExpandHeight = false;
        blockV.padding = new RectOffset(12, 12, 4, 8);
        var blockLe = blockGo.AddComponent<LayoutElement>();
        blockLe.flexibleWidth = 1f;
        blockLe.flexibleHeight = 0f;
        blockGo.transform.SetParent(layout, false);
        blockGo.transform.SetSiblingIndex(insertIndex);

        AddPracticeSectionTitle(blockGo.transform, font);
        AddPracticeGradeLegend(blockGo.transform, font);

        for (int i = 0; i < n; i++)
        {
            GsiHubPracticeCatalog.Entry e = entries[i];
            BuildPracticeRow(blockGo.transform, e.RowObjectName, practiceTransforms[i], e.Mode, i, font);
        }

        RefreshPracticeGradeButtonColors();
        EnsureUnifiedExamBlock(layout);
    }

    private static void AddPracticeSectionTitle(Transform practiceBlockParent, TMP_FontAsset font)
    {
        var go = new GameObject("PracticeSectionTitle");
        go.transform.SetParent(practiceBlockParent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 24f;
        le.minHeight = 22f;
        le.flexibleWidth = 1f;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            tmp.font = font;
        }

        tmp.text = GameLocalization.GetUiString(UiStringKeys.HubSectionPractice, "Practice");
        tmp.fontSize = 15;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = GsiUiAppearance.TextPrimary;
    }

    private static void DestroyChildIfExists(Transform layout, string childName)
    {
        Transform t = layout.Find(childName);
        if (t != null)
        {
            GsiRuntimeUiBootstrap.DestroyObjectForRuntimeUi(t.gameObject);
        }
    }

    private void BuildPracticeRow(
        Transform blockParent,
        string rowName,
        Transform practiceButton,
        TestMode mode,
        int modeRowIndex,
        TMP_FontAsset font)
    {
        var rowGo = new GameObject(rowName);
        rowGo.transform.SetParent(blockParent, false);
        var rowH = rowGo.AddComponent<HorizontalLayoutGroup>();
        rowH.spacing = 10f;
        rowH.childAlignment = TextAnchor.MiddleCenter;
        rowH.childControlWidth = true;
        rowH.childControlHeight = true;
        rowH.childForceExpandWidth = false;
        rowH.childForceExpandHeight = false;
        var rowLe = rowGo.AddComponent<LayoutElement>();
        rowLe.minHeight = 46f;
        rowLe.preferredHeight = 48f;
        rowLe.flexibleWidth = 1f;

        practiceButton.SetParent(rowGo.transform, false);
        var btnLe = practiceButton.GetComponent<LayoutElement>();
        if (btnLe == null)
        {
            btnLe = practiceButton.gameObject.AddComponent<LayoutElement>();
        }

        btnLe.flexibleWidth = 1f;
        btnLe.minWidth = 200f;

        CreateGradePackForMode(rowGo.transform, font, mode, modeRowIndex);
    }

    private static void AddPracticeGradeLegend(Transform practiceBlockParent, TMP_FontAsset font)
    {
        var legGo = new GameObject("PracticeGradeLegend");
        legGo.transform.SetParent(practiceBlockParent, false);
        var legLe = legGo.AddComponent<LayoutElement>();
        legLe.preferredHeight = 22f;
        legLe.minHeight = 20f;
        legLe.flexibleWidth = 1f;
        var tmp = legGo.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            tmp.font = font;
        }

        tmp.text = GameLocalization.GetUiString(UiStringKeys.HubPracticeGradeLegend,
            "Practice rank: Grade 1 = hardest, Grade 9 = entry (per mode)");
        tmp.fontSize = 11;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = GsiUiAppearance.TextSecondary;
    }

    private void CreateGradePackForMode(Transform rowParent, TMP_FontAsset font, TestMode mode, int modeRowIndex)
    {
        var packGo = new GameObject($"GradePack_{mode}");
        packGo.transform.SetParent(rowParent, false);
        var packLe = packGo.AddComponent<LayoutElement>();
        packLe.preferredWidth = 268f;
        packLe.minWidth = 220f;
        packLe.flexibleWidth = 1f;
        packLe.minHeight = 44f;
        packLe.preferredHeight = 46f;
        var rowH = packGo.AddComponent<HorizontalLayoutGroup>();
        rowH.spacing = 4f;
        rowH.childAlignment = TextAnchor.MiddleCenter;
        rowH.childControlWidth = mode == TestMode.Reaction; // 반응속도 안내 가이드인 경우 전체 가로 맞춤
        rowH.childControlHeight = true;
        rowH.childForceExpandWidth = false;
        rowH.childForceExpandHeight = true;
        rowH.padding = new RectOffset(0, 0, 2, 2);

        if (mode == TestMode.Reaction)
        {
            var guideGo = new GameObject("ReactionGuideText");
            guideGo.transform.SetParent(packGo.transform, false);
            var guideTmp = guideGo.AddComponent<TextMeshProUGUI>();
            if (font != null) guideTmp.font = font;
            
            bool isKo = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale != null &&
                        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("ko", System.StringComparison.OrdinalIgnoreCase);
            
            guideTmp.text = isKo ? "기록별 등급 차등 지급" : "Grade based on Reaction Record";
            guideTmp.fontSize = 14;
            guideTmp.fontStyle = FontStyles.Bold;
            guideTmp.alignment = TextAlignmentOptions.Center;
            guideTmp.color = GsiUiAppearance.TextSecondary;
            guideTmp.raycastTarget = false;
            return;
        }

        for (int i = 0; i < 9; i++)
        {
            int grade = i + 1;
            _gradeButtonsByMode[modeRowIndex][i] = CreateGradeStepButton(packGo.transform, font, grade);
            _gradeClickActionsByMode[modeRowIndex][i] = () => OnPracticeGradeButtonClicked(mode, grade);
            _gradeButtonsByMode[modeRowIndex][i].onClick.AddListener(_gradeClickActionsByMode[modeRowIndex][i]);
        }
    }

    private void EnsureUnifiedExamBlock(Transform layout)
    {
        Transform practiceBlock = layout.Find("PracticeBlock");
        if (practiceBlock == null)
        {
            return;
        }

        TMP_FontAsset font = TmpFontCache.LiberationSansSdf;

        var spacerGo = new GameObject("UnifiedExamTopSpacer");
        spacerGo.transform.SetParent(layout, false);
        var spacerLe = spacerGo.AddComponent<LayoutElement>();
        const float examTopGapPx = 18f;
        spacerLe.minHeight = examTopGapPx;
        spacerLe.preferredHeight = examTopGapPx;
        spacerLe.flexibleWidth = 1f;
        spacerGo.transform.SetAsLastSibling();

        var blockGo = new GameObject("UnifiedExamBlock");
        blockGo.transform.SetParent(layout, false);
        var blockV = blockGo.AddComponent<VerticalLayoutGroup>();
        blockV.spacing = 8f;
        blockV.childAlignment = TextAnchor.UpperCenter;
        blockV.childControlWidth = true;
        blockV.childControlHeight = true;
        blockV.childForceExpandWidth = true;
        blockV.childForceExpandHeight = false;
        blockV.padding = new RectOffset(16, 16, 12, 16);
        var blockLe = blockGo.AddComponent<LayoutElement>();
        blockLe.minHeight = 200f;
        blockLe.flexibleWidth = 1f;
        blockGo.transform.SetAsLastSibling();

        var headerGo = new GameObject("UnifiedExamHeader");
        headerGo.transform.SetParent(blockGo.transform, false);
        var headerTmp = headerGo.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            headerTmp.font = font;
        }

        headerTmp.text = GameLocalization.GetUiString(UiStringKeys.HubUnifiedTitle, "Unified official exam");
        headerTmp.fontSize = 18;
        headerTmp.fontStyle = FontStyles.Bold;
        headerTmp.alignment = TextAlignmentOptions.Center;
        headerTmp.color = GsiUiAppearance.TextPrimary;
        var headerLe = headerGo.AddComponent<LayoutElement>();
        headerLe.preferredHeight = 28f;

        var hintGo = new GameObject("UnifiedExamGradeHint");
        hintGo.transform.SetParent(blockGo.transform, false);
        var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            hintTmp.font = font;
        }

        hintTmp.text = GameLocalization.GetUiString(UiStringKeys.HubUnifiedHint,
            "Seven subjects in random order. Pass lines and final total use the rank below.");
        hintTmp.fontSize = 12;
        hintTmp.alignment = TextAlignmentOptions.Center;
        hintTmp.color = GsiUiAppearance.TextSecondary;
        var hintLe = hintGo.AddComponent<LayoutElement>();
        hintLe.preferredHeight = 36f;

        var rowGo = new GameObject("UnifiedExamGradeRow");
        rowGo.transform.SetParent(blockGo.transform, false);
        var rowH = rowGo.AddComponent<HorizontalLayoutGroup>();
        rowH.spacing = 6f;
        rowH.childAlignment = TextAnchor.MiddleCenter;
        rowH.childControlWidth = false;
        rowH.childControlHeight = false;
        var rowLe = rowGo.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 48f;

        _unifiedGradeButtons = new Button[9];
        _unifiedGradeClickActions = new UnityAction[9];
        for (int i = 0; i < 9; i++)
        {
            int grade = i + 1;
            _unifiedGradeButtons[i] = CreateGradeStepButton(rowGo.transform, font, grade);
            _unifiedGradeClickActions[i] = () => OnUnifiedExamGradeClicked(grade);
            _unifiedGradeButtons[i].onClick.AddListener(_unifiedGradeClickActions[i]);
        }

        RefreshUnifiedGradeButtonColors();

        var startBtn = CreateLobbyButton(blockGo.transform, font, "UnifiedOfficialExamStart",
            GameLocalization.GetUiString(UiStringKeys.HubUnifiedStart, "Start unified exam (1 exam ticket)"));
        startBtn.onClick.RemoveListener(OnUnifiedOfficialExamClicked);
        startBtn.onClick.AddListener(OnUnifiedOfficialExamClicked);

        var historyBtn = CreateLobbyButton(blockGo.transform, font, "UnifiedExamHistoryButton",
            GameLocalization.GetUiString(UiStringKeys.HubUnifiedHistoryBtn, "Unified exam log"));
        historyBtn.onClick.RemoveAllListeners();
        historyBtn.onClick.AddListener(() => GsiUnifiedExamHistoryOverlayRoot.Toggle(transform));
    }

    private void OnPracticeGradeButtonClicked(TestMode mode, int grade)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetPracticeGrade(mode, grade);
        RefreshPracticeGradeButtonColors();
    }

    private void RefreshPracticeGradeButtonColors()
    {
        if (_gradeButtonsByMode == null || GameManager.Instance == null)
        {
            return;
        }

        TestMode[] modes = GsiHubPracticeCatalog.ModesOrdered();

        for (int m = 0; m < _gradeButtonsByMode.Length && m < modes.Length; m++)
        {
            int current = GameManager.Instance.GetPracticeGrade(modes[m]);
            if (_gradeButtonsByMode[m] == null)
            {
                continue;
            }

            for (int g = 0; g < _gradeButtonsByMode[m].Length; g++)
            {
                if (_gradeButtonsByMode[m][g] == null)
                {
                    continue;
                }

                var img = _gradeButtonsByMode[m][g].GetComponent<Image>();
                if (img != null)
                {
                    img.color = g + 1 == current
                        ? GsiUiAppearance.GradeStepSelectedFromAccent(CosmeticTheme.UiAccent)
                        : GsiUiAppearance.GradeStepNormal;
                }
            }
        }
    }

    private static Button CreateGradeStepButton(Transform parent, TMP_FontAsset font, int grade)
    {
        var go = new GameObject($"GradeBtn_{grade}");
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 26f;
        le.preferredHeight = 40f;
        le.minWidth = 24f;

        var img = go.AddComponent<Image>();
        img.color = GsiUiAppearance.GradeStepNormal;
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tRt = textGo.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            tmp.font = font;
        }

        tmp.text = grade.ToString();
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = GsiUiAppearance.TextPrimary;
        tmp.raycastTarget = false;

        return btn;
    }

    private void UpdateEconomyTexts()
    {
        if (_tokenText != null)
        {
            int stardust = EconomyManager.Instance != null ? EconomyManager.Instance.Stardust : 0;
            _tokenText.text = GameLocalization.FormatUiString(UiStringKeys.HubCurrencyStardustFmt, "Stardust: {0}", stardust);
            _tokenText.color = GsiUiAppearance.ShopStardustText;
            _tokenText.fontSize = GsiUiScreenLayout.EconomyLineFontSize;
            GsiUiRuntimeWidgets.ApplyEconomyLineTypography(_tokenText);
        }

        if (_astralCoreText != null)
        {
            int astralCores = EconomyManager.Instance != null ? EconomyManager.Instance.AstralCores : 0;
            _astralCoreText.text = GameLocalization.FormatUiString(UiStringKeys.HubCurrencyAstralCoreFmt, "Astral Cores: {0}", astralCores);
            _astralCoreText.color = GsiUiAppearance.ShopAstralCoreText;
            _astralCoreText.fontSize = GsiUiScreenLayout.EconomyLineFontSize;
            GsiUiRuntimeWidgets.ApplyEconomyLineTypography(_astralCoreText);
        }

        if (_ticketText != null)
        {
            int tickets = EconomyManager.Instance != null ? EconomyManager.Instance.ArcTickets : 0;
            _ticketText.text =
                GameLocalization.FormatUiString(UiStringKeys.HubCurrencyTicketFmt, "Exam tickets: {0}", tickets);
            _ticketText.color = GsiUiAppearance.ShopTicketText;
            _ticketText.fontSize = GsiUiScreenLayout.EconomyLineFontSize;
            GsiUiRuntimeWidgets.ApplyEconomyLineTypography(_ticketText);
        }
    }



    private void BuildCosmicHubStage()
    {
        if (gameObject.GetComponent<GsiCosmicViewportController>() == null)
        {
            gameObject.AddComponent<GsiCosmicViewportController>();
        }

        Transform layout = transform.Find("Layout");
        if (layout != null)
        {
            layout.gameObject.SetActive(false);
        }

        if (_lobbyPanelImage != null)
        {
            ProceduralSpaceBackground.ApplyToImage(_lobbyPanelImage);
        }

        var stageGo = new GameObject("GsiCosmicStage", typeof(RectTransform));
        _cosmicStage = stageGo.GetComponent<RectTransform>();
        _cosmicStage.SetParent(transform, false);
        _cosmicStage.anchorMin = Vector2.zero;
        _cosmicStage.anchorMax = Vector2.one;
        _cosmicStage.offsetMin = Vector2.zero;
        _cosmicStage.offsetMax = Vector2.zero;
        _cosmicStage.SetAsFirstSibling();

        // 백그라운드 우주 연출 레이어가 인덱스 0, 데코 컨테이너가 인덱스 1에 위치하게 렌더링 순서 보정
        _cosmicStage.SetSiblingIndex(0);
        var decoContainer = transform.Find("DecoPlacementContainer");
        if (decoContainer != null)
        {
            decoContainer.SetSiblingIndex(1);
        }

        // Ensure Orrery System exists to drive star node physics
        if (Application.isPlaying && ArchE.Game.GsiCosmicOrrerySystem.Instance == null)
        {
            gameObject.AddComponent<ArchE.Game.GsiCosmicOrrerySystem>();
        }

        SpawnWhiteHoleNode();
        SpawnPracticeStarNodes();
    }

    private void SpawnWhiteHoleNode()
    {
        if (_cosmicStage == null) return;

        var whGo = new GameObject("UnifiedExamWhiteHole", typeof(RectTransform), typeof(Image));
        var rt = whGo.GetComponent<RectTransform>();
        rt.SetParent(_cosmicStage, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(160f, 160f);

        var controller = whGo.AddComponent<GsiWhiteHoleNodeController>();
        
        // Centralize WhiteHole settings in Orrery System
        if (ArchE.Game.GsiCosmicOrrerySystem.Instance != null)
        {
            ArchE.Game.GsiCosmicOrrerySystem.Instance.SetWhiteHoleSingularity(rt, 240f, 580f);
        }

        controller.OnClickedAction = (node) => {
            SpawnOrbitalSelector(node.Rect, new Color(0.2f, 0.85f, 1f, 1f), (grade) => {
                _unifiedExamSelectedGrade = grade;
                OnUnifiedOfficialExamClicked();
            });
        };
    }

    private void SpawnPracticeStarNodes()
    {
        if (_cosmicStage == null) return;

        IReadOnlyList<GsiHubPracticeCatalog.Entry> entries = GsiHubPracticeCatalog.All;

        Vector2[] initialPositions = {
            new Vector2(-360f, 120f),
            new Vector2(-150f, 220f),
            new Vector2(150f, 220f),
            new Vector2(360f, 120f),
            new Vector2(250f, -160f),
            new Vector2(0f, -240f),
            new Vector2(-250f, -160f)
        };

        Color[] colors = {
            new Color(1.0f, 0.88f, 0.35f, 1f),
            new Color(0.85f, 0.45f, 1f, 1f),
            new Color(0.35f, 0.95f, 0.75f, 1f),
            new Color(1f, 0.4f, 0.55f, 1f),
            new Color(0.3f, 0.75f, 1f, 1f),
            new Color(1f, 0.45f, 0.25f, 1f),
            new Color(0.55f, 0.95f, 0.35f, 1f)
        };

        for (int i = 0; i < entries.Count && i < initialPositions.Length; i++)
        {
            var e = entries[i];
            var btnGo = new GameObject(e.PracticeTransformName, typeof(RectTransform), typeof(Image));
            var rt = btnGo.GetComponent<RectTransform>();
            rt.SetParent(_cosmicStage, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = initialPositions[i];

            var star = btnGo.AddComponent<GsiGsiStarNodeController>();
            star.Mode = e.Mode;
            star.StarColor = colors[i];
            star.ButtonLabelText = GameLocalization.GetUiString(e.LocalizationKey, e.EnglishFallback);
            star.InitialPosition = initialPositions[i];

            TestMode modeCapture = e.Mode;
            star.OnClickedAction = (node) => {
                SpawnOrbitalSelector(node.Rect, node.StarColor, (grade) => {
                    GameManager.Instance.SetPracticeGrade(modeCapture, grade);
                    OnPracticeModeClicked(modeCapture);
                }, modeCapture == TestMode.Reaction);
            };
        }
    }

    private void SpawnOrbitalSelector(RectTransform anchorRt, Color themeColor, Action<int> onGradeSelected, bool isReaction = false)
    {
        bool isSameAnchor = _activeSelector != null && _activeSelector.transform.parent == anchorRt;

        if (_activeSelector != null)
        {
            _activeSelector.Close();
            _activeSelector = null;
        }

        if (isSameAnchor)
        {
            GsiUiSound.PlayClick();
            return;
        }

        var go = new GameObject("GsiOrbitalSelector", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(anchorRt, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        var selector = go.AddComponent<GsiGsiOrbitalSelector>();
        selector.IsReactionMode = isReaction;
        selector.ThemeColor = themeColor;
        selector.OnGradeSelected = (grade) => {
            onGradeSelected(grade);
            selector.Close();
        };
        selector.OnClosed = () => {
            if (_activeSelector == selector)
            {
                _activeSelector = null;
            }
        };

        _activeSelector = selector;
        GsiUiSound.PlayClick();
    }
}
