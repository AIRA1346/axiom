using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// G.S.I 시설 로비: 연습·공식 시험 진입 및 ARCHÉ 허브로 복귀.
/// </summary>
public sealed class GSIHubMenuController : MonoBehaviour
{
    [SerializeField] private Button _backToArchEButton;
    [SerializeField] private Button _practiceButton;
    [SerializeField] private Button _aimPracticeButton;
    [SerializeField] private TextMeshProUGUI _tokenText;
    [SerializeField] private TextMeshProUGUI _ticketText;

    private Button _memoryPracticeButton;
    private Button _rhythmPracticeButton;
    private Button _motPracticeButton;
    private Button _bulletHellPracticeButton;

    private Button[][] _gradeButtonsByMode;
    private UnityAction[][] _gradeClickActionsByMode;

    private static readonly Color GradeSelectedColor = new Color(0.38f, 0.52f, 0.78f, 1f);
    private static readonly Color GradeNormalColor = new Color(0.18f, 0.2f, 0.26f, 1f);

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
        EnsureMemoryLobbyButtons();
        EnsureRhythmLobbyButtons();
        EnsureMotLobbyButtons();
        EnsureBulletHellLobbyButtons();
        EnsurePracticeGradeLayout();
        UpdateEconomyTexts();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnEconomyChanged += HandleEconomyChanged;
        }
    }

    private void OnDestroy()
    {
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
        GameManager.Instance.SetGameState(GameState.TestStandby);
    }

    private void OnAimPracticeClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetTestType(TestType.Practice);
        GameManager.Instance.SetTestMode(TestMode.AimPrecision);
        GameManager.Instance.SetGameState(GameState.TestStandby);
    }

    private void OnMemoryPracticeClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetTestType(TestType.Practice);
        GameManager.Instance.SetTestMode(TestMode.MemorySequence);
        GameManager.Instance.SetGameState(GameState.TestStandby);
    }

    private void OnRhythmPracticeClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetTestType(TestType.Practice);
        GameManager.Instance.SetTestMode(TestMode.RhythmTiming);
        GameManager.Instance.SetGameState(GameState.TestStandby);
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
        Debug.Log("응시권 부족!");
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
                img.color = i + 1 == _unifiedExamSelectedGrade ? GradeSelectedColor : GradeNormalColor;
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
            _memoryPracticeButton = CreateLobbyButton(layout, font, "PracticeMemory", "연습 · 기억");
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
            _rhythmPracticeButton = CreateLobbyButton(layout, font, "PracticeRhythm", "연습 · 리듬");
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
        GameManager.Instance.SetGameState(GameState.TestStandby);
    }

    private void OnBulletHellPracticeClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetTestType(TestType.Practice);
        GameManager.Instance.SetTestMode(TestMode.BulletHell);
        GameManager.Instance.SetGameState(GameState.TestStandby);
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
            _motPracticeButton = CreateLobbyButton(layout, font, "PracticeMot", "연습 · 다중 추적");
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
            _bulletHellPracticeButton = CreateLobbyButton(layout, font, "PracticeBulletHell", "연습 · Bullet Hell");
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
        img.color = new Color(0.22f, 0.24f, 0.32f, 1f);
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
        tmp.color = Color.white;
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
            Destroy(oldBlock.gameObject);
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
            Destroy(t.gameObject);
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

        tmp.text = "연습 급수: 1급 = 최상 · 9급 = 입문 (과목마다 선택)";
        tmp.fontSize = 13;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.62f, 0.66f, 0.74f, 1f);
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

        headerTmp.text = "통합 공식 시험";
        headerTmp.fontSize = 18;
        headerTmp.fontStyle = FontStyles.Bold;
        headerTmp.alignment = TextAlignmentOptions.Center;
        headerTmp.color = new Color(0.78f, 0.8f, 0.88f, 1f);
        var headerLe = headerGo.AddComponent<LayoutElement>();
        headerLe.preferredHeight = 28f;

        var hintGo = new GameObject("UnifiedExamGradeHint");
        hintGo.transform.SetParent(blockGo.transform, false);
        var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            hintTmp.font = font;
        }

        hintTmp.text = "6과목 · 순서 랜덤 · 아래 급수로 합격선·최종 총점 기준이 정해집니다.";
        hintTmp.fontSize = 12;
        hintTmp.alignment = TextAlignmentOptions.Center;
        hintTmp.color = new Color(0.62f, 0.66f, 0.74f, 1f);
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

        var startBtn = CreateLobbyButton(blockGo.transform, font, "UnifiedOfficialExamStart", "통합 공식 시험 시작 · 응시권 1장");
        startBtn.onClick.RemoveListener(OnUnifiedOfficialExamClicked);
        startBtn.onClick.AddListener(OnUnifiedOfficialExamClicked);
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
                    img.color = g + 1 == current ? GradeSelectedColor : GradeNormalColor;
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
        img.color = GradeNormalColor;
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
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return btn;
    }

    private void UpdateEconomyTexts()
    {
        if (_tokenText != null)
        {
            int gold = EconomyManager.Instance != null ? EconomyManager.Instance.Tokens : 0;
            _tokenText.text = $"기초 골드: {gold}";
        }

        if (_ticketText != null)
        {
            int tickets = EconomyManager.Instance != null ? EconomyManager.Instance.ExamTickets : 0;
            _ticketText.text = $"응시권: {tickets}";
        }
    }
}
