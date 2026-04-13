using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// 전역 설정 오버레이(DontDestroyOnLoad). 모든 씬에서 동일한 볼륨·언어 UI를 쓰며,
/// 키보드 ESC 또는 게임패드 Start로 닫기/열기(인트로 씬에서는 열지 않음 — 스킵과 충돌 방지).
/// 에디터에서는 비플레이 시에도 UI 계층이 생성되어 씬 뷰에서 레이아웃을 확인할 수 있습니다.
/// </summary>
[ExecuteAlways]
public sealed class GlobalSettingsOverlay : MonoBehaviour
{
    public static GlobalSettingsOverlay Instance { get; private set; }

    private GameObject _root;
    private Slider _masterSlider;
    private Slider _sfxSlider;
    private Slider _musicSlider;
    private Button _langEnButton;
    private Button _langKoButton;
    private TextMeshProUGUI _closeTmp;
    private TextMeshProUGUI _titleTmp;
    private TextMeshProUGUI _langLabelTmp;
    private TextMeshProUGUI _appearanceLabelTmp;
    private Button _appearanceDarkButton;
    private Button _appearanceLightButton;
    private Image _dimImage;
    private Image _panelImage;
    private Image _closeButtonImage;
    private readonly System.Collections.Generic.List<TextMeshProUGUI> _sliderLabelTmps = new();

    private bool _built;
    private bool _openRequested;
    private bool _localeSubscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureCreated();
    }

    public static void EnsureCreated()
    {
        if (Instance != null)
        {
            return;
        }

        var go = new GameObject("GlobalSettingsOverlay");
        go.AddComponent<GlobalSettingsOverlay>();
    }

    public static void OpenSettings()
    {
        EnsureCreated();
        Instance.RequestOpen();
    }

    public static void CloseSettings()
    {
        if (Instance != null)
        {
            Instance.Close();
        }
    }

    public static void ToggleSettings()
    {
        EnsureCreated();
        Instance.Toggle();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(gameObject);
                return;
            }
#endif
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            TryBuildUiForEditorSceneView();
        }
#endif
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return;
        }
#endif
        GsiUserSettings.SettingsChanged += OnSettingsChangedFromOutside;
        GsiUiAppearance.Changed += OnUiAppearanceChanged;
        TrySubscribeLocaleChanged();
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return;
        }
#endif
        TryUnsubscribeLocaleChanged();
        GsiUserSettings.SettingsChanged -= OnSettingsChangedFromOutside;
        GsiUiAppearance.Changed -= OnUiAppearanceChanged;
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

        GameLocalization.UiLocaleChanged += OnOverlayLocaleChanged;
        _localeSubscribed = true;
    }

    private void TryUnsubscribeLocaleChanged()
    {
        if (!_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged -= OnOverlayLocaleChanged;
        _localeSubscribed = false;
    }

    private void OnOverlayLocaleChanged()
    {
        if (!_built)
        {
            return;
        }

        RefreshLocalizedTexts();
        RefreshAppearanceChrome();
    }

    private void OnUiAppearanceChanged()
    {
        if (_built)
        {
            RefreshAppearanceChrome();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnSettingsChangedFromOutside()
    {
        SyncSlidersFromModel();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return;
        }
#endif
        if (!WasMenuTogglePressedThisFrame())
        {
            return;
        }

        if (_root != null && _root.activeSelf)
        {
            Close();
            return;
        }

        if (SceneManager.GetActiveScene().name == SceneNames.Intro)
        {
            return;
        }

        RequestOpen();
    }

    /// <summary>ESC(키보드) 또는 Start(게임패드). 키보드가 없는 환경에서도 패드만으로 설정을 열 수 있게 합니다.</summary>
    private static bool WasMenuTogglePressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return true;
        }

        Gamepad pad = Gamepad.current;
        return pad != null && pad.startButton.wasPressedThisFrame;
    }

    private void RequestOpen()
    {
        if (_root != null && _root.activeSelf)
        {
            return;
        }

        if (_openRequested)
        {
            return;
        }

        _openRequested = true;
        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        Task boot = GameLocalization.InitializeAndApplySavedLocaleAsync();
        while (!boot.IsCompleted)
        {
            yield return null;
        }

        if (!_built)
        {
            BuildUi();
            _built = true;
        }

        SyncSlidersFromModel();
        RefreshLocalizedTexts();
        RefreshAppearanceChrome();
        _root.SetActive(true);
        _root.transform.SetAsLastSibling();
        _openRequested = false;
    }

    private void Toggle()
    {
        if (_root != null && _root.activeSelf)
        {
            Close();
        }
        else
        {
            RequestOpen();
        }
    }

    private void Close()
    {
        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    private void BuildUi()
    {
        DestroyStaleSettingsCanvasIfRefsLost();

        if (_root != null)
        {
            return;
        }

        var canvasGo = new GameObject("SettingsCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvasRt = canvasGo.GetComponent<RectTransform>();
        canvasRt.anchorMin = Vector2.zero;
        canvasRt.anchorMax = Vector2.one;
        canvasRt.offsetMin = Vector2.zero;
        canvasRt.offsetMax = Vector2.zero;

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        _root = canvasGo;
        _root.SetActive(false);

        var dim = new GameObject("Dim", typeof(RectTransform));
        dim.transform.SetParent(canvasGo.transform, false);
        var dimRt = dim.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        var dimImg = dim.AddComponent<Image>();
        _dimImage = dimImg;
        dimImg.color = GsiUiAppearance.OverlayScrim;
        dimImg.raycastTarget = true;
        var dimBtn = dim.AddComponent<Button>();
        dimBtn.targetGraphic = dimImg;
        dimBtn.onClick.AddListener(Close);

        var box = new GameObject("Panel", typeof(RectTransform));
        box.transform.SetParent(canvasGo.transform, false);
        var boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(600f, 560f);
        boxRt.anchoredPosition = Vector2.zero;
        var boxImg = box.AddComponent<Image>();
        _panelImage = boxImg;
        boxImg.color = GsiUiAppearance.Panel;
        boxImg.raycastTarget = true;

        var v = box.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(32, 32, 26, 28);
        v.spacing = 18f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;

        _titleTmp = AddCenterTitle(box.transform, "");

        _appearanceLabelTmp = AddSectionLabel(box.transform, "");
        var appearanceRow = new GameObject("AppearanceRow", typeof(RectTransform));
        appearanceRow.transform.SetParent(box.transform, false);
        var appearanceH = appearanceRow.AddComponent<HorizontalLayoutGroup>();
        appearanceH.spacing = 12f;
        appearanceH.childAlignment = TextAnchor.MiddleCenter;
        appearanceH.childForceExpandWidth = true;
        appearanceH.childForceExpandHeight = true;
        var appearanceLe = appearanceRow.AddComponent<LayoutElement>();
        appearanceLe.minHeight = 44f;

        _appearanceDarkButton = AddChipButton(appearanceRow.transform,
            GameLocalization.GetUiString(UiStringKeys.SettingsDark, "Dark"),
            () => ApplyAppearanceMode(GsiUiAppearanceMode.Dark));
        _appearanceLightButton = AddChipButton(appearanceRow.transform,
            GameLocalization.GetUiString(UiStringKeys.SettingsLight, "Light"),
            () => ApplyAppearanceMode(GsiUiAppearanceMode.Light));

        _langLabelTmp = AddSectionLabel(box.transform, "");
        var langRow = new GameObject("LanguageRow", typeof(RectTransform));
        langRow.transform.SetParent(box.transform, false);
        var langH = langRow.AddComponent<HorizontalLayoutGroup>();
        langH.spacing = 12f;
        langH.childAlignment = TextAnchor.MiddleCenter;
        langH.childForceExpandWidth = true;
        langH.childForceExpandHeight = true;
        var langLe = langRow.AddComponent<LayoutElement>();
        langLe.minHeight = 44f;

        _langEnButton = AddChipButton(langRow.transform,
            GameLocalization.GetUiString(UiStringKeys.SettingsLangEnglish, "English"),
            () => ApplyLanguage("en"));
        _langKoButton = AddChipButton(langRow.transform,
            GameLocalization.GetUiString(UiStringKeys.SettingsLangKorean, "Korean"),
            () => ApplyLanguage("ko-KR"));

        _sliderLabelTmps.Clear();
        _masterSlider = AddSliderRow(box.transform, "", v =>
        {
            GsiUserSettings.SetMasterVolume(v);
        });
        _sfxSlider = AddSliderRow(box.transform, "", v =>
        {
            GsiUserSettings.SetSfxVolume(v);
        });
        _musicSlider = AddSliderRow(box.transform, "", v =>
        {
            GsiUserSettings.SetMusicVolume(v);
        });

        var closeGo = new GameObject("CloseButton", typeof(RectTransform));
        closeGo.transform.SetParent(box.transform, false);
        var closeBtn = closeGo.AddComponent<Button>();
        var closeImg = closeGo.AddComponent<Image>();
        _closeButtonImage = closeImg;
        closeImg.color = GsiUiAppearance.SecondaryButton;
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(Close);
        var closeBtnLe = closeGo.AddComponent<LayoutElement>();
        closeBtnLe.minHeight = 44f;
        closeBtnLe.preferredHeight = 44f;

        var closeLabel = new GameObject("Text", typeof(RectTransform));
        closeLabel.transform.SetParent(closeGo.transform, false);
        StretchFull(closeLabel.GetComponent<RectTransform>());
        _closeTmp = closeLabel.AddComponent<TextMeshProUGUI>();
        _closeTmp.fontSize = 22f;
        _closeTmp.alignment = TextAlignmentOptions.Center;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            _closeTmp.font = TmpFontCache.LiberationSansSdf;
        }

        RefreshLocalizedTexts();
        SyncSlidersFromModel();
        RefreshAppearanceChrome();
    }

    /// <summary>
    /// 씬 저장·도메인 리로드 후 자식 SettingsCanvas만 남고 필드가 비었을 때 중복 생성을 막기 위해 제거합니다.
    /// </summary>
    private void DestroyStaleSettingsCanvasIfRefsLost()
    {
        if (_root != null)
        {
            return;
        }

        Transform stale = transform.Find("SettingsCanvas");
        if (stale == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(stale.gameObject);
            return;
        }
#endif
        Destroy(stale.gameObject);
    }

#if UNITY_EDITOR
    /// <summary>
    /// 플레이하지 않을 때 SettingsCanvas를 생성해 에디터 씬/게임 뷰에 계층이 보이게 합니다(기본은 비활성).
    /// </summary>
    private void TryBuildUiForEditorSceneView()
    {
        if (_built || _root != null)
        {
            return;
        }

        GameLocalization.TryInitializeSynchronouslyForEditorSceneView();
        BuildUi();
        _built = true;
        EditorUtility.SetDirty(gameObject);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    private static void ApplyAppearanceMode(GsiUiAppearanceMode mode)
    {
        GsiUiAppearance.SetMode(mode);
    }

    private void RefreshAppearanceChrome()
    {
        if (_dimImage != null)
        {
            _dimImage.color = GsiUiAppearance.OverlayScrim;
        }

        if (_panelImage != null)
        {
            _panelImage.color = GsiUiAppearance.Panel;
        }

        if (_closeButtonImage != null)
        {
            _closeButtonImage.color = GsiUiAppearance.SecondaryButton;
        }

        if (_titleTmp != null)
        {
            _titleTmp.color = GsiUiAppearance.TextPrimary;
        }

        if (_appearanceLabelTmp != null)
        {
            _appearanceLabelTmp.color = GsiUiAppearance.TextSecondary;
        }

        if (_langLabelTmp != null)
        {
            _langLabelTmp.color = GsiUiAppearance.TextSecondary;
        }

        if (_closeTmp != null)
        {
            _closeTmp.color = GsiUiAppearance.TextPrimary;
        }

        foreach (TextMeshProUGUI lab in _sliderLabelTmps)
        {
            if (lab != null)
            {
                lab.color = GsiUiAppearance.TextPrimary;
            }
        }

        RefreshChipLabels(_langEnButton);
        RefreshChipLabels(_langKoButton);
        RefreshChipLabels(_appearanceDarkButton);
        RefreshChipLabels(_appearanceLightButton);
        UpdateLanguageButtonHighlight();
        UpdateAppearanceButtonHighlight();
    }

    private static void RefreshChipLabels(Button btn)
    {
        if (btn == null)
        {
            return;
        }

        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.color = GsiUiAppearance.TextPrimary;
        }
    }

    private void ApplyLanguage(string code)
    {
        StartCoroutine(ApplyLanguageRoutine(code));
    }

    private IEnumerator ApplyLanguageRoutine(string code)
    {
        // 최초 부트에서만 초기화를 기다립니다. 이미 초기화된 상태에서 Initialize를 다시 호출하면
        // 저장 언어를 한 번 더 맞추느라 불필요한 동기화가 생길 수 있습니다.
        if (!GameLocalization.IsInitialized)
        {
            Task t = GameLocalization.InitializeAndApplySavedLocaleAsync();
            while (!t.IsCompleted)
            {
                yield return null;
            }
        }

        GameLocalization.SavePreferredLocale(code);
        RefreshLocalizedTexts();
        UpdateLanguageButtonHighlight();
        RefreshAppearanceChrome();
    }

    private void UpdateAppearanceButtonHighlight()
    {
        bool dark = GsiUiAppearance.Mode == GsiUiAppearanceMode.Dark;
        SetChipSelected(_appearanceDarkButton, dark);
        SetChipSelected(_appearanceLightButton, !dark);
    }

    private void RefreshLocalizedTexts()
    {
        if (_titleTmp != null)
        {
            _titleTmp.text = GameLocalization.GetUiString(UiStringKeys.SettingsTitle, "Settings");
        }

        if (_appearanceLabelTmp != null)
        {
            _appearanceLabelTmp.text = GameLocalization.GetUiString(UiStringKeys.SettingsAppearance, "Appearance");
        }

        if (_langLabelTmp != null)
        {
            _langLabelTmp.text = GameLocalization.GetUiString(UiStringKeys.SettingsLanguage, "Language");
        }

        if (_closeTmp != null)
        {
            _closeTmp.text = GameLocalization.GetUiString(UiStringKeys.SettingsClose, "Close (ESC)");
        }

        string[] keys =
        {
            UiStringKeys.SettingsMasterVolume,
            UiStringKeys.SettingsSfx,
            UiStringKeys.SettingsMusic
        };

        string[] fallbacks = { "Master volume", "Sound effects", "Music" };
        for (int i = 0; i < _sliderLabelTmps.Count && i < keys.Length; i++)
        {
            if (_sliderLabelTmps[i] != null)
            {
                _sliderLabelTmps[i].text = GameLocalization.GetUiString(keys[i], fallbacks[i]);
            }
        }

        ApplyChipButtonLocalizedText(_appearanceDarkButton, UiStringKeys.SettingsDark, "Dark");
        ApplyChipButtonLocalizedText(_appearanceLightButton, UiStringKeys.SettingsLight, "Light");
        ApplyChipButtonLocalizedText(_langEnButton, UiStringKeys.SettingsLangEnglish, "English");
        ApplyChipButtonLocalizedText(_langKoButton, UiStringKeys.SettingsLangKorean, "Korean");
    }

    private static void ApplyChipButtonLocalizedText(Button btn, string localizationKey, string englishFallback)
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

    private void UpdateLanguageButtonHighlight()
    {
        string code = GameLocalization.GetSavedLocaleCode();
        bool en = code.StartsWith("en", System.StringComparison.OrdinalIgnoreCase);
        bool ko = code.StartsWith("ko", System.StringComparison.OrdinalIgnoreCase);
        SetChipSelected(_langEnButton, en);
        SetChipSelected(_langKoButton, ko);
    }

    private static void SetChipSelected(Button btn, bool selected)
    {
        if (btn == null)
        {
            return;
        }

        var g = btn.targetGraphic as Graphic;
        if (g != null)
        {
            g.color = selected
                ? GsiUiAppearance.ChipSelectedFromAccent(CosmeticTheme.UiAccent)
                : GsiUiAppearance.ChipInactive;
        }
    }

    private TextMeshProUGUI AddCenterTitle(Transform parent, string text)
    {
        var go = new GameObject("Title", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 32f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = GsiUiScreenLayout.ScreenTitleCharacterSpacing * 0.85f;
        tmp.color = GsiUiAppearance.TextPrimary;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            tmp.font = TmpFontCache.LiberationSansSdf;
        }

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 40f;
        return tmp;
    }

    private TextMeshProUGUI AddSectionLabel(Transform parent, string text)
    {
        var go = new GameObject("LangSection", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 15f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = 0.9f;
        tmp.color = GsiUiAppearance.TextSecondary;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            tmp.font = TmpFontCache.LiberationSansSdf;
        }

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 22f;
        return tmp;
    }

    private Button AddChipButton(Transform parent, string label, UnityAction onClick)
    {
        var go = new GameObject("Chip_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = GsiUiAppearance.ChipInactive;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.minHeight = 40f;

        var tGo = new GameObject("Label", typeof(RectTransform));
        tGo.transform.SetParent(go.transform, false);
        StretchFull(tGo.GetComponent<RectTransform>());
        var tmp = tGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 19f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = GsiUiAppearance.TextPrimary;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            tmp.font = TmpFontCache.LiberationSansSdf;
        }

        return btn;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private Slider AddSliderRow(Transform parent, string label, UnityAction<float> onChanged)
    {
        DefaultControls.Resources resources = BuildUiResources();

        var row = new GameObject("Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.minHeight = 56f;

        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 12f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;

        var labGo = new GameObject("Label", typeof(RectTransform));
        labGo.transform.SetParent(row.transform, false);
        var lab = labGo.AddComponent<TextMeshProUGUI>();
        lab.text = label;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            lab.font = TmpFontCache.LiberationSansSdf;
        }

        lab.fontSize = 18f;
        lab.enableWordWrapping = true;
        lab.alignment = TextAlignmentOptions.Left;
        lab.color = GsiUiAppearance.TextPrimary;
        var labLe = labGo.AddComponent<LayoutElement>();
        labLe.preferredWidth = 180f;
        labLe.flexibleWidth = 1f;
        _sliderLabelTmps.Add(lab);

        GameObject sliderRoot = DefaultControls.CreateSlider(resources);
        sliderRoot.transform.SetParent(row.transform, false);
        sliderRoot.name = "Slider";
        var slider = sliderRoot.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.onValueChanged.AddListener(onChanged);

        var sLe = sliderRoot.AddComponent<LayoutElement>();
        sLe.flexibleWidth = 3f;
        sLe.minHeight = 28f;

        return slider;
    }

    private static DefaultControls.Resources BuildUiResources()
    {
        Texture2D t = Texture2D.whiteTexture;
        Sprite s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        return new DefaultControls.Resources
        {
            standard = s,
            background = s,
            knob = s
        };
    }

    private void SyncSlidersFromModel()
    {
        if (_masterSlider != null)
        {
            _masterSlider.SetValueWithoutNotify(GsiUserSettings.MasterVolume);
        }

        if (_sfxSlider != null)
        {
            _sfxSlider.SetValueWithoutNotify(GsiUserSettings.SfxVolume);
        }

        if (_musicSlider != null)
        {
            _musicSlider.SetValueWithoutNotify(GsiUserSettings.MusicVolume);
        }
    }
}
