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
    private TextMeshProUGUI _closeTmp;
    private TextMeshProUGUI _titleTmp;
    private TextMeshProUGUI _langLabelTmp;
    private Image _dimImage;
    private Image _panelImage;
    private Image _closeButtonImage;
    private Image _quitButtonImage;
    private TextMeshProUGUI _quitTmp;
    private readonly System.Collections.Generic.List<TextMeshProUGUI> _sliderLabelTmps = new();

    private struct ResolutionInfo
    {
        public int width;
        public int height;
    }

    private readonly System.Collections.Generic.List<ResolutionInfo> _resolutionsList = new();
    private int _currentResolutionIndex = 0;

    private struct LanguageInfo
    {
        public string code;
        public string displayName;
    }

    private readonly System.Collections.Generic.List<LanguageInfo> _languagesList = new()
    {
        new LanguageInfo { code = "en", displayName = "English" },
        new LanguageInfo { code = "ko-KR", displayName = "한국어" }
    };
    private int _currentLanguageIndex = 0;

    private readonly FullScreenMode[] _screenModes = new[]
    {
        FullScreenMode.FullScreenWindow,
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.Windowed
    };
    private int _currentScreenModeIndex = 0;

    private TextMeshProUGUI _resolutionLabelTmp;
    private TextMeshProUGUI _resolutionValueTmp;
    private TextMeshProUGUI _screenModeLabelTmp;
    private TextMeshProUGUI _screenModeValueTmp;
    private TextMeshProUGUI _langValueTmp;
    private readonly System.Collections.Generic.List<Button> _selectorButtons = new();

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
        SyncDisplaySettingsFromModel();
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
        SyncDisplaySettingsFromModel();
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

    private void QuitToDesktop()
    {
        Close();
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return;
        }

        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
        boxRt.sizeDelta = new Vector2(680f, 780f);
        boxRt.anchoredPosition = Vector2.zero;
        var boxImg = box.AddComponent<Image>();
        _panelImage = boxImg;
        GsiArcaneUi.ApplyPanel(boxImg);
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



        _selectorButtons.Clear();
        InitResolutions();
        InitScreenMode();
        InitLanguages();

        _langLabelTmp = AddSelectorRow(box.transform, "", () => CycleLanguage(-1), () => CycleLanguage(1), out _langValueTmp);
        _resolutionLabelTmp = AddSelectorRow(box.transform, "", () => CycleResolution(-1), () => CycleResolution(1), out _resolutionValueTmp);
        _screenModeLabelTmp = AddSelectorRow(box.transform, "", () => CycleScreenMode(-1), () => CycleScreenMode(1), out _screenModeValueTmp);

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

        var quitGo = new GameObject("QuitButton", typeof(RectTransform));
        quitGo.transform.SetParent(box.transform, false);
        var quitBtn = quitGo.AddComponent<Button>();
        var quitImg = quitGo.AddComponent<Image>();
        _quitButtonImage = quitImg;
        quitImg.color = GsiUiAppearance.SecondaryButton;
        quitBtn.targetGraphic = quitImg;
        quitBtn.onClick.AddListener(QuitToDesktop);
        GsiArcaneUi.ApplyButton(quitBtn, primary: false);
        var quitBtnLe = quitGo.AddComponent<LayoutElement>();
        quitBtnLe.minHeight = 44f;
        quitBtnLe.preferredHeight = 44f;

        var quitLabel = new GameObject("Text", typeof(RectTransform));
        quitLabel.transform.SetParent(quitGo.transform, false);
        StretchFull(quitLabel.GetComponent<RectTransform>());
        _quitTmp = quitLabel.AddComponent<TextMeshProUGUI>();
        _quitTmp.fontSize = 22f;
        _quitTmp.alignment = TextAlignmentOptions.Center;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            _quitTmp.font = TmpFontCache.LiberationSansSdf;
        }

        var closeGo = new GameObject("CloseButton", typeof(RectTransform));
        closeGo.transform.SetParent(box.transform, false);
        var closeBtn = closeGo.AddComponent<Button>();
        var closeImg = closeGo.AddComponent<Image>();
        _closeButtonImage = closeImg;
        closeImg.color = GsiUiAppearance.SecondaryButton;
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(Close);
        GsiArcaneUi.ApplyButton(closeBtn, primary: true);
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

    private void RefreshAppearanceChrome()
    {
        if (_dimImage != null)
        {
            _dimImage.color = GsiUiAppearance.OverlayScrim;
        }

        if (_panelImage != null)
        {
            GsiArcaneUi.ApplyPanel(_panelImage);
        }

        if (_closeButtonImage != null)
        {
            GsiArcaneUi.ApplyButton(_closeButtonImage.GetComponent<Button>(), primary: true);
        }

        if (_quitButtonImage != null)
        {
            GsiArcaneUi.ApplyButton(_quitButtonImage.GetComponent<Button>(), primary: false);
        }

        if (_titleTmp != null)
        {
            _titleTmp.color = GsiUiAppearance.TextPrimary;
        }

        if (_langLabelTmp != null)
        {
            _langLabelTmp.color = GsiUiAppearance.TextSecondary;
        }

        if (_closeTmp != null)
        {
            _closeTmp.color = GsiUiAppearance.TextPrimary;
        }

        if (_quitTmp != null)
        {
            _quitTmp.color = GsiUiAppearance.TextPrimary;
        }

        foreach (TextMeshProUGUI lab in _sliderLabelTmps)
        {
            if (lab != null)
            {
                lab.color = GsiUiAppearance.TextPrimary;
            }
        }

        if (_resolutionValueTmp != null)
        {
            _resolutionValueTmp.color = GsiUiAppearance.TextPrimary;
        }

        if (_screenModeValueTmp != null)
        {
            _screenModeValueTmp.color = GsiUiAppearance.TextPrimary;
        }

        if (_langValueTmp != null)
        {
            _langValueTmp.color = GsiUiAppearance.TextPrimary;
        }

        if (_resolutionLabelTmp != null)
        {
            _resolutionLabelTmp.color = GsiUiAppearance.TextPrimary;
        }

        if (_screenModeLabelTmp != null)
        {
            _screenModeLabelTmp.color = GsiUiAppearance.TextPrimary;
        }

        foreach (var btn in _selectorButtons)
        {
            RefreshChipLabels(btn);
        }

        UpdateLanguageSelectorText();
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
        InitLanguages();
        RefreshLocalizedTexts();
        UpdateLanguageSelectorText();
        RefreshAppearanceChrome();
    }

    private void RefreshLocalizedTexts()
    {
        if (_titleTmp != null)
        {
            _titleTmp.text = GameLocalization.GetUiString(UiStringKeys.SettingsTitle, "Settings");
        }

        if (_langLabelTmp != null)
        {
            _langLabelTmp.text = GameLocalization.GetUiString(UiStringKeys.SettingsLanguage, "Language");
        }

        if (_closeTmp != null)
        {
            _closeTmp.text = GameLocalization.GetUiString(UiStringKeys.SettingsClose, "Close (ESC)");
        }

        if (_quitTmp != null)
        {
            _quitTmp.text = GameLocalization.GetUiString(UiStringKeys.SettingsQuitToDesktop, "Quit to desktop");
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

        if (_resolutionLabelTmp != null)
        {
            _resolutionLabelTmp.text = GameLocalization.GetUiString(UiStringKeys.SettingsResolution, "Resolution");
        }
        if (_screenModeLabelTmp != null)
        {
            _screenModeLabelTmp.text = GameLocalization.GetUiString(UiStringKeys.SettingsScreenMode, "Screen Mode");
        }
        UpdateLanguageSelectorText();
        UpdateDisplaySelectorTexts();
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
            if (g is Image img)
            {
                GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(img);
            }
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
        GsiArcaneUi.ApplyButton(btn, primary: false);
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.minHeight = 40f;

        var tGo = new GameObject("Label", typeof(RectTransform));
        tGo.transform.SetParent(go.transform, false);
        StretchFull(tGo.GetComponent<RectTransform>());
        var tmp = tGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 19f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.characterSpacing = 0.6f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = GsiUiAppearance.TextPrimary;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            tmp.font = TmpFontCache.LiberationSansSdf;
        }

        return btn;
    }

    private TextMeshProUGUI AddSelectorRow(Transform parent, string label, UnityAction onLeft, UnityAction onRight, out TextMeshProUGUI valueTmp)
    {
        var row = new GameObject("SelectorRow_" + label, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.minHeight = 44f;

        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 12f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;

        // Label
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

        // Selector container
        var selGo = new GameObject("Selector", typeof(RectTransform));
        selGo.transform.SetParent(row.transform, false);
        var selLe = selGo.AddComponent<LayoutElement>();
        selLe.flexibleWidth = 3f;
        selLe.minHeight = 36f;
        
        var selH = selGo.AddComponent<HorizontalLayoutGroup>();
        selH.spacing = 8f;
        selH.childAlignment = TextAnchor.MiddleCenter;
        selH.childForceExpandWidth = false;
        selH.childForceExpandHeight = true;

        // Left button
        var leftBtn = AddSelectorButton(selGo.transform, "<", onLeft);
        _selectorButtons.Add(leftBtn);
        
        // Value Text
        var valGo = new GameObject("ValueText", typeof(RectTransform));
        valGo.transform.SetParent(selGo.transform, false);
        var valTmpLocal = valGo.AddComponent<TextMeshProUGUI>();
        valTmpLocal.text = "";
        if (TmpFontCache.LiberationSansSdf != null)
        {
            valTmpLocal.font = TmpFontCache.LiberationSansSdf;
        }
        valTmpLocal.fontSize = 18f;
        valTmpLocal.alignment = TextAlignmentOptions.Center;
        valTmpLocal.color = GsiUiAppearance.TextPrimary;
        var valLe = valGo.AddComponent<LayoutElement>();
        valLe.preferredWidth = 200f;
        valLe.flexibleWidth = 1f;

        // Right button
        var rightBtn = AddSelectorButton(selGo.transform, ">", onRight);
        _selectorButtons.Add(rightBtn);

        valueTmp = valTmpLocal;
        return lab;
    }

    private Button AddSelectorButton(Transform parent, string arrow, UnityAction onClick)
    {
        var go = new GameObject("ArrowBtn_" + arrow, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = GsiUiAppearance.ChipInactive;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        GsiArcaneUi.ApplyButton(btn, primary: false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 36f;
        le.minHeight = 36f;

        var tGo = new GameObject("Text", typeof(RectTransform));
        tGo.transform.SetParent(go.transform, false);
        StretchFull(tGo.GetComponent<RectTransform>());
        var tmp = tGo.AddComponent<TextMeshProUGUI>();
        tmp.text = arrow;
        tmp.fontSize = 18f;
        tmp.fontStyle = FontStyles.Bold;
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

    private void InitResolutions()
    {
        _resolutionsList.Clear();
        var seen = new System.Collections.Generic.HashSet<string>();
        
        Resolution[] systemResolutions = Screen.resolutions;
        if (systemResolutions == null || systemResolutions.Length == 0)
        {
            _resolutionsList.Add(new ResolutionInfo { width = 1920, height = 1080 });
            _resolutionsList.Add(new ResolutionInfo { width = 1600, height = 900 });
            _resolutionsList.Add(new ResolutionInfo { width = 1366, height = 768 });
            _resolutionsList.Add(new ResolutionInfo { width = 1280, height = 720 });
        }
        else
        {
            System.Array.Sort(systemResolutions, (a, b) => (b.width * b.height).CompareTo(a.width * a.height));
            foreach (var res in systemResolutions)
            {
                string key = $"{res.width}x{res.height}";
                if (!seen.Contains(key))
                {
                    seen.Add(key);
                    _resolutionsList.Add(new ResolutionInfo { width = res.width, height = res.height });
                }
            }
        }

        int curW = GsiUserSettings.ResolutionWidth;
        int curH = GsiUserSettings.ResolutionHeight;

        _currentResolutionIndex = 0;
        float minDiff = float.MaxValue;
        for (int i = 0; i < _resolutionsList.Count; i++)
        {
            var res = _resolutionsList[i];
            if (res.width == curW && res.height == curH)
            {
                _currentResolutionIndex = i;
                break;
            }
            float diff = Mathf.Abs(res.width - curW) + Mathf.Abs(res.height - curH);
            if (diff < minDiff)
            {
                minDiff = diff;
                _currentResolutionIndex = i;
            }
        }
    }

    private void InitScreenMode()
    {
        int curMode = GsiUserSettings.ScreenMode;
        _currentScreenModeIndex = 0;
        for (int i = 0; i < _screenModes.Length; i++)
        {
            if ((int)_screenModes[i] == curMode)
            {
                _currentScreenModeIndex = i;
                break;
            }
        }
    }

    private void CycleResolution(int direction)
    {
        if (_resolutionsList.Count == 0) return;
        _currentResolutionIndex = (_currentResolutionIndex + direction + _resolutionsList.Count) % _resolutionsList.Count;
        ApplyDisplaySettings();
    }

    private void CycleScreenMode(int direction)
    {
        _currentScreenModeIndex = (_currentScreenModeIndex + direction + _screenModes.Length) % _screenModes.Length;
        ApplyDisplaySettings();
    }

    private void ApplyDisplaySettings()
    {
        if (_currentResolutionIndex >= 0 && _currentResolutionIndex < _resolutionsList.Count)
        {
            var res = _resolutionsList[_currentResolutionIndex];
            var mode = _screenModes[_currentScreenModeIndex];
            GsiUserSettings.SetDisplaySettings(res.width, res.height, mode);
            UpdateDisplaySelectorTexts();
        }
    }

    private void UpdateDisplaySelectorTexts()
    {
        if (_resolutionValueTmp != null && _currentResolutionIndex >= 0 && _currentResolutionIndex < _resolutionsList.Count)
        {
            var res = _resolutionsList[_currentResolutionIndex];
            _resolutionValueTmp.text = $"{res.width} x {res.height}";
        }

        if (_screenModeValueTmp != null && _currentScreenModeIndex >= 0 && _currentScreenModeIndex < _screenModes.Length)
        {
            var mode = _screenModes[_currentScreenModeIndex];
            string modeStr = GetLocalizedScreenModeName(mode);
            _screenModeValueTmp.text = modeStr;
        }
    }

    private string GetLocalizedScreenModeName(FullScreenMode mode)
    {
        switch (mode)
        {
            case FullScreenMode.ExclusiveFullScreen:
                return GameLocalization.GetUiString(UiStringKeys.SettingsModeFullscreen, "Fullscreen");
            case FullScreenMode.FullScreenWindow:
                return GameLocalization.GetUiString(UiStringKeys.SettingsModeBorderless, "Borderless");
            case FullScreenMode.Windowed:
                return GameLocalization.GetUiString(UiStringKeys.SettingsModeWindowed, "Windowed");
            default:
                return mode.ToString();
        }
    }

    private void InitLanguages()
    {
        string curCode = GameLocalization.GetSavedLocaleCode();
        _currentLanguageIndex = 0;
        for (int i = 0; i < _languagesList.Count; i++)
        {
            if (_languagesList[i].code.Equals(curCode, System.StringComparison.OrdinalIgnoreCase))
            {
                _currentLanguageIndex = i;
                break;
            }
        }
    }

    private void CycleLanguage(int direction)
    {
        if (_languagesList.Count == 0) return;
        _currentLanguageIndex = (_currentLanguageIndex + direction + _languagesList.Count) % _languagesList.Count;
        ApplyLanguageSettings();
    }

    private void ApplyLanguageSettings()
    {
        if (_currentLanguageIndex >= 0 && _currentLanguageIndex < _languagesList.Count)
        {
            var lang = _languagesList[_currentLanguageIndex];
            ApplyLanguage(lang.code);
        }
    }

    private void UpdateLanguageSelectorText()
    {
        if (_langValueTmp != null && _currentLanguageIndex >= 0 && _currentLanguageIndex < _languagesList.Count)
        {
            _langValueTmp.text = _languagesList[_currentLanguageIndex].displayName;
        }
    }

    private void SyncDisplaySettingsFromModel()
    {
        InitResolutions();
        InitScreenMode();
        InitLanguages();
        UpdateDisplaySelectorTexts();
        UpdateLanguageSelectorText();
    }
}
