using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
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
    [SerializeField] private Button _practiceButton;
    [SerializeField] private Button _aimPracticeButton;
    [SerializeField] private TextMeshProUGUI _tokenText;
    [SerializeField] private TextMeshProUGUI _ticketText;

    private Image _lobbyPanelImage;
    private TextMeshProUGUI _facilityTitleTmp;
    private bool _appearanceSubscribed;
    private bool _localeSubscribed;

    private Button _memoryPracticeButton;
    private Button _rhythmPracticeButton;
    private Button _motPracticeButton;
    private Button _bulletHellPracticeButton;

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

        if (_practiceButton != null)
        {
            _practiceButton.onClick.AddListener(OnPracticeClicked);
        }

        if (_aimPracticeButton != null)
        {
            _aimPracticeButton.onClick.AddListener(OnAimPracticeClicked);
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

        EnsureMemoryLobbyButtons();
        EnsureRhythmLobbyButtons();
        EnsureMotLobbyButtons();
        EnsureBulletHellLobbyButtons();
        EnsurePracticeGradeLayout();

        UpdateEconomyTexts();
        CacheHubVisualRefs();
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
    /// 상점·인벤과 동일한 상단 헤더 스트립(제목 중앙·Back 우측) + 서브바(골드·응시권)로 옮기고, 본문 Layout은 그 아래에서 시작하도록 맞춥니다.
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
                        Object.DestroyImmediate(backLes[bi]);
                    }
                    else
#endif
                    {
                        Object.Destroy(backLes[bi]);
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
        TryUnsubscribeLocaleChanged();
        TryUnsubscribeAppearance();
        if (_backToArchEButton != null)
        {
            _backToArchEButton.onClick.RemoveListener(OnBackToArchEClicked);
        }

        if (_practiceButton != null)
        {
            _practiceButton.onClick.RemoveListener(OnPracticeClicked);
        }

        if (_aimPracticeButton != null)
        {
            _aimPracticeButton.onClick.RemoveListener(OnAimPracticeClicked);
        }

        if (_memoryPracticeButton != null)
        {
            _memoryPracticeButton.onClick.RemoveListener(OnMemoryPracticeClicked);
        }

        if (_rhythmPracticeButton != null)
        {
            _rhythmPracticeButton.onClick.RemoveListener(OnRhythmPracticeClicked);
        }

        if (_motPracticeButton != null)
        {
            _motPracticeButton.onClick.RemoveListener(OnMotPracticeClicked);
        }

        if (_bulletHellPracticeButton != null)
        {
            _bulletHellPracticeButton.onClick.RemoveListener(OnBulletHellPracticeClicked);
        }

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
        ApplyHubPracticeModeButtonLabels();
        ApplyHubPracticeGradeLegendText();
        ApplyHubUnifiedExamBlockTexts();
    }

    private void ApplyHubPracticeModeButtonLabels()
    {
        ApplyLocalizedLabelOnButton(_memoryPracticeButton, UiStringKeys.HubPracticeMemory, "Practice: Memory");
        ApplyLocalizedLabelOnButton(_rhythmPracticeButton, UiStringKeys.HubPracticeRhythm, "Practice: Rhythm");
        ApplyLocalizedLabelOnButton(_motPracticeButton, UiStringKeys.HubPracticeMot, "Practice: Multiple object tracking");
        ApplyLocalizedLabelOnButton(_bulletHellPracticeButton, UiStringKeys.HubPracticeBulletHell, "Practice: Bullet Hell");
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
                    "Six subjects in random order. Pass lines and final total use the rank below.");
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
            _lobbyPanelImage.color = GsiUiAppearance.ShopScreenBackground;
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
            _facilityTitleTmp.text = GameLocalization.GetUiString(UiStringKeys.HubScreenTitle, "Game Skill Index");
            GsiUiScreenLayout.ApplyScreenTitleTypography(_facilityTitleTmp);
            _facilityTitleTmp.color = GsiUiAppearance.TextPrimary;
        }

        if (_tokenText != null)
        {
            _tokenText.color = GsiUiAppearance.ShopGoldText;
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

    private void OnMemoryPracticeClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetTestType(TestType.Practice);
        GameManager.Instance.SetTestMode(TestMode.MemorySequence);
        GameManager.Instance.SetGameState(GameState.TestBriefing);
    }

    private void OnRhythmPracticeClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetTestType(TestType.Practice);
        GameManager.Instance.SetTestMode(TestMode.RhythmTiming);
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

    private void EnsureMemoryLobbyButtons()
    {
        Transform layout = transform.Find("Layout");
        if (layout == null)
        {
            return;
        }

        DestroyChildIfExists(layout, "ExamMemory");

        Transform existingP = layout.Find("PracticeMemory");
        if (existingP != null)
        {
            _memoryPracticeButton = existingP.GetComponent<Button>();
        }

        TMP_FontAsset font = TmpFontCache.LiberationSansSdf;
        if (_memoryPracticeButton == null)
        {
            _memoryPracticeButton = CreateLobbyButton(layout, font, "PracticeMemory",
                GameLocalization.GetUiString(UiStringKeys.HubPracticeMemory, "Practice: Memory"));
        }

        if (_memoryPracticeButton != null)
        {
            _memoryPracticeButton.onClick.RemoveListener(OnMemoryPracticeClicked);
            _memoryPracticeButton.onClick.AddListener(OnMemoryPracticeClicked);
        }
    }

    private void EnsureRhythmLobbyButtons()
    {
        Transform layout = transform.Find("Layout");
        if (layout == null)
        {
            return;
        }

        DestroyChildIfExists(layout, "ExamRhythm");

        Transform existingP = layout.Find("PracticeRhythm");
        if (existingP != null)
        {
            _rhythmPracticeButton = existingP.GetComponent<Button>();
        }

        TMP_FontAsset font = TmpFontCache.LiberationSansSdf;
        if (_rhythmPracticeButton == null)
        {
            _rhythmPracticeButton = CreateLobbyButton(layout, font, "PracticeRhythm",
                GameLocalization.GetUiString(UiStringKeys.HubPracticeRhythm, "Practice: Rhythm"));
        }

        if (_rhythmPracticeButton != null)
        {
            _rhythmPracticeButton.onClick.RemoveListener(OnRhythmPracticeClicked);
            _rhythmPracticeButton.onClick.AddListener(OnRhythmPracticeClicked);
        }
    }

    private void OnMotPracticeClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetTestType(TestType.Practice);
        GameManager.Instance.SetTestMode(TestMode.MultipleObjectTracking);
        GameManager.Instance.SetGameState(GameState.TestBriefing);
    }

    private void OnBulletHellPracticeClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetTestType(TestType.Practice);
        GameManager.Instance.SetTestMode(TestMode.BulletHell);
        GameManager.Instance.SetGameState(GameState.TestBriefing);
    }

    private void EnsureMotLobbyButtons()
    {
        Transform layout = transform.Find("Layout");
        if (layout == null)
        {
            return;
        }

        DestroyChildIfExists(layout, "ExamMot");

        Transform existingP = layout.Find("PracticeMot");
        if (existingP != null)
        {
            _motPracticeButton = existingP.GetComponent<Button>();
        }

        TMP_FontAsset font = TmpFontCache.LiberationSansSdf;
        if (_motPracticeButton == null)
        {
            _motPracticeButton = CreateLobbyButton(layout, font, "PracticeMot",
                GameLocalization.GetUiString(UiStringKeys.HubPracticeMot, "Practice: Multiple object tracking"));
        }

        if (_motPracticeButton != null)
        {
            _motPracticeButton.onClick.RemoveListener(OnMotPracticeClicked);
            _motPracticeButton.onClick.AddListener(OnMotPracticeClicked);
        }
    }

    private void EnsureBulletHellLobbyButtons()
    {
        Transform layout = transform.Find("Layout");
        if (layout == null)
        {
            return;
        }

        DestroyChildIfExists(layout, "ExamBulletHell");

        Transform existingP = layout.Find("PracticeBulletHell");
        if (existingP != null)
        {
            _bulletHellPracticeButton = existingP.GetComponent<Button>();
        }

        TMP_FontAsset font = TmpFontCache.LiberationSansSdf;
        if (_bulletHellPracticeButton == null)
        {
            _bulletHellPracticeButton = CreateLobbyButton(layout, font, "PracticeBulletHell",
                GameLocalization.GetUiString(UiStringKeys.HubPracticeBulletHell, "Practice: Bullet Hell"));
        }

        if (_bulletHellPracticeButton != null)
        {
            _bulletHellPracticeButton.onClick.RemoveListener(OnBulletHellPracticeClicked);
            _bulletHellPracticeButton.onClick.AddListener(OnBulletHellPracticeClicked);
        }
    }

    private static Button CreateLobbyButton(Transform parent, TMP_FontAsset font, string name, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 52f;
        le.minHeight = 48f;
        le.flexibleWidth = 1f;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 52f);

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
        tmp.fontSize = 22;
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

        DestroyChildIfExists(layout, "ExamReaction");
        DestroyChildIfExists(layout, "ExamAim");

        EnsureMemoryLobbyButtons();
        EnsureRhythmLobbyButtons();
        EnsureMotLobbyButtons();
        EnsureBulletHellLobbyButtons();

        Transform pr = FindDescendantNamed(layout, "PracticeReaction");
        Transform pa = FindDescendantNamed(layout, "PracticeAim");
        Transform pm = FindDescendantNamed(layout, "PracticeMemory");
        Transform py = FindDescendantNamed(layout, "PracticeRhythm");
        Transform pz = FindDescendantNamed(layout, "PracticeMot");
        Transform pb = FindDescendantNamed(layout, "PracticeBulletHell");
        if (pr == null || pa == null || pm == null || py == null || pz == null || pb == null)
        {
            return;
        }

        DestroyChildIfExists(layout, "OfficialExamSectionSpacer");
        DestroyChildIfExists(layout, "OfficialExamSectionHeader");
        DestroyChildIfExists(layout, "UnifiedExamTopSpacer");
        DestroyChildIfExists(layout, "UnifiedExamBlock");

        Transform oldBlock = layout.Find("PracticeBlock");
        if (oldBlock != null)
        {
            pr.SetParent(layout, true);
            pa.SetParent(layout, true);
            pm.SetParent(layout, true);
            py.SetParent(layout, true);
            pz.SetParent(layout, true);
            pb.SetParent(layout, true);
            GsiRuntimeUiBootstrap.DestroyObjectForRuntimeUi(oldBlock.gameObject);
        }

        int insertIndex = Mathf.Min(
            pr.GetSiblingIndex(),
            pa.GetSiblingIndex(),
            pm.GetSiblingIndex(),
            py.GetSiblingIndex(),
            pz.GetSiblingIndex(),
            pb.GetSiblingIndex());

        _gradeButtonsByMode = new Button[6][];
        _gradeClickActionsByMode = new UnityAction[6][];
        for (int m = 0; m < 6; m++)
        {
            _gradeButtonsByMode[m] = new Button[9];
            _gradeClickActionsByMode[m] = new UnityAction[9];
        }

        TMP_FontAsset font = TmpFontCache.LiberationSansSdf;

        var blockGo = new GameObject("PracticeBlock");
        blockGo.AddComponent<RectTransform>();
        var blockV = blockGo.AddComponent<VerticalLayoutGroup>();
        blockV.spacing = 8f;
        blockV.childAlignment = TextAnchor.UpperCenter;
        blockV.childControlWidth = true;
        blockV.childControlHeight = true;
        blockV.childForceExpandWidth = true;
        blockV.childForceExpandHeight = false;
        blockV.padding = new RectOffset(0, 0, 8, 8);
        var blockLe = blockGo.AddComponent<LayoutElement>();
        // minHeight 고정 시 실제 6행+범례 높이보다 작으면 연습 행이 블록 밖으로 넘쳐 통합 시험 UI와 겹침 — 자식 레이아웃이 높이를 결정하도록 둠
        blockLe.flexibleWidth = 1f;
        blockLe.flexibleHeight = 0f;
        blockGo.transform.SetParent(layout, false);
        blockGo.transform.SetSiblingIndex(insertIndex);

        AddPracticeGradeLegend(blockGo.transform, font);

        BuildPracticeRow(blockGo.transform, "PracticeRow_Reaction", pr, TestMode.Reaction, 0, font);
        BuildPracticeRow(blockGo.transform, "PracticeRow_Aim", pa, TestMode.AimPrecision, 1, font);
        BuildPracticeRow(blockGo.transform, "PracticeRow_Memory", pm, TestMode.MemorySequence, 2, font);
        BuildPracticeRow(blockGo.transform, "PracticeRow_Rhythm", py, TestMode.RhythmTiming, 3, font);
        BuildPracticeRow(blockGo.transform, "PracticeRow_Mot", pz, TestMode.MultipleObjectTracking, 4, font);
        BuildPracticeRow(blockGo.transform, "PracticeRow_BulletHell", pb, TestMode.BulletHell, 5, font);

        RefreshPracticeGradeButtonColors();
        EnsureUnifiedExamBlock(layout);
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
        rowH.spacing = 12f;
        rowH.childAlignment = TextAnchor.MiddleCenter;
        rowH.childControlWidth = true;
        rowH.childControlHeight = true;
        rowH.childForceExpandWidth = false;
        rowH.childForceExpandHeight = false;
        var rowLe = rowGo.AddComponent<LayoutElement>();
        rowLe.minHeight = 50f;
        rowLe.preferredHeight = 52f;
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
        tmp.fontSize = 13;
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
        rowH.childControlWidth = false;
        rowH.childControlHeight = true;
        rowH.childForceExpandWidth = false;
        rowH.childForceExpandHeight = true;
        rowH.padding = new RectOffset(0, 0, 2, 2);

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
        const float examTopGapPx = 28f;
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
            "Six subjects in random order. Pass lines and final total use the rank below.");
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

        TestMode[] modes =
        {
            TestMode.Reaction,
            TestMode.AimPrecision,
            TestMode.MemorySequence,
            TestMode.RhythmTiming,
            TestMode.MultipleObjectTracking,
            TestMode.BulletHell
        };

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
            int gold = EconomyManager.Instance != null ? EconomyManager.Instance.Tokens : 0;
            _tokenText.text = GameLocalization.FormatUiString(UiStringKeys.HubCurrencyGoldFmt, "Gold: {0}", gold);
            _tokenText.color = GsiUiAppearance.ShopGoldText;
            _tokenText.fontSize = GsiUiScreenLayout.EconomyLineFontSize;
            GsiUiRuntimeWidgets.ApplyEconomyLineTypography(_tokenText);
        }

        if (_ticketText != null)
        {
            int tickets = EconomyManager.Instance != null ? EconomyManager.Instance.ExamTickets : 0;
            _ticketText.text =
                GameLocalization.FormatUiString(UiStringKeys.HubCurrencyTicketFmt, "Exam tickets: {0}", tickets);
            _ticketText.color = GsiUiAppearance.ShopTicketText;
            _ticketText.fontSize = GsiUiScreenLayout.EconomyLineFontSize;
            GsiUiRuntimeWidgets.ApplyEconomyLineTypography(_ticketText);
        }
    }
}
