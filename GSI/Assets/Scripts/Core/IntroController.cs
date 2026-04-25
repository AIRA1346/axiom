using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Intro sequence: localization boot, logo/status beats, then <c>TAP TO START</c> before loading the lobby.
/// </summary>
public sealed class IntroController : MonoBehaviour
{
    public const string DefaultLogoTitle = "The Axiom";

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private TextMeshProUGUI _logoText;
    [SerializeField] private Image _fadeOverlay;

    [Header("Entry")]
    [Tooltip("Skip at 0. Fades from black at start.")]
    [SerializeField] private float _entryFadeInDuration = 0.55f;

    [Header("Tagline (runtime)")]
    [SerializeField] private bool _createTaglineUnderLogo = true;
    [SerializeField] private float _taglineFadeDuration = 0.45f;

    [Header("Logo title")]
    [Tooltip("씬에 찍힌 TMP보다 우선해 첫 프레임부터 적용됩니다. 인스펙터에서 조절 가능합니다.")]
    [SerializeField] private float _logoTitleFontSize = 112f;

    [Header("Logo animation")]
    [SerializeField] private float _logoIntroDuration = 1.05f;
    [SerializeField] private float _logoStartScale = 0.88f;
    [Tooltip("Subtle scale pulse on the logo during status typewriter.")]
    [SerializeField] private bool _logoBreathingDuringStatus = true;
    [SerializeField] private float _logoBreathingAmplitude = 0.012f;
    [SerializeField] private float _logoBreathingSpeed = 2.2f;

    [Header("Timing")]
    [SerializeField] private float _typewriterInterval = 0.03f;
    [SerializeField] private float _phaseDisplayDuration = 0.8f;
    [SerializeField] private float _accessGrantedHold = 1.5f;
    [SerializeField] private float _fadeOutDuration = 1f;

    [Header("Tap to start")]
    [SerializeField] private float _tapToStartPulseAmplitude = 0.065f;
    [SerializeField] private float _tapToStartPulseSpeed = 2.6f;

    [Header("Version label (runtime)")]
    [SerializeField] private bool _createVersionLabelBottomLeft = true;
    [SerializeField] private float _versionLabelFontSize = 20f;
    [SerializeField] private Vector2 _versionLabelMargin = new Vector2(24f, 20f);

    [Header("Skip")]
    [SerializeField] private bool _allowSkipWithKeyboard = true;

    [Header("Scene")]
    [SerializeField] private string _mainSceneName = "LobbyScene";
    [SerializeField] private float _maxLocalizationBootSeconds = 30f;

    private Color _logoColorOpaque = Color.white;
    private Color _defaultStatusColor = Color.white;
    private Coroutine _logoBreathingCoroutine;
    private TextMeshProUGUI _runtimeTagline;
    private TextMeshProUGUI _runtimeVersionLabel;
    private bool _skipRequested;
    private bool _sequenceActive;
    private bool _localeSubscribed;
    private bool _showingTapToStart;
    private Coroutine _tapToStartPulseCoroutine;

    private void Awake()
    {
#if GSI_PRODUCT
        _mainSceneName = SceneNames.Lobby;
#endif
        PrepareIntroUiBeforeFirstFrame();
    }

    /// <summary>첫 렌더 전에 호출해 씬에 남은 TMP 기본 문구·로고 알파 등이 한 프레임 보이지 않게 합니다.</summary>
    private void PrepareIntroUiBeforeFirstFrame()
    {
        if (_statusText != null)
        {
            _defaultStatusColor = new Color(0.75f, 0.82f, 0.88f, 1f);
            _statusText.text = string.Empty;
            _statusText.color = _defaultStatusColor;
            _statusText.characterSpacing = 3f;
        }

        if (_fadeOverlay != null)
        {
            if (_entryFadeInDuration > 0f)
            {
                _fadeOverlay.color = new Color(0f, 0f, 0f, 1f);
                _fadeOverlay.raycastTarget = true;
            }
            else
            {
                _fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
                _fadeOverlay.raycastTarget = false;
            }
        }

        if (_logoText != null)
        {
            if (string.IsNullOrWhiteSpace(_logoText.text))
            {
                _logoText.text = GameLocalization.GetUiString(UiStringKeys.IntroLogoTitle, DefaultLogoTitle);
            }

            if (_logoTitleFontSize > 0f)
            {
                _logoText.fontSize = _logoTitleFontSize;
            }

            _logoColorOpaque = _logoText.color;
            _logoColorOpaque.a = 1f;
            _logoText.color = new Color(_logoColorOpaque.r, _logoColorOpaque.g, _logoColorOpaque.b, 0f);
            _logoText.rectTransform.localScale = Vector3.one * _logoStartScale;
        }
    }

    private void Start()
    {
        GsiUserSettings.Load();
        GsiUserSettings.ApplyToAudio();

        if (_createTaglineUnderLogo)
        {
            TryCreateRuntimeTagline();
        }

        if (_createVersionLabelBottomLeft)
        {
            TryCreateRuntimeVersionLabel();
        }

        if (_fadeOverlay != null)
        {
            _fadeOverlay.transform.SetAsLastSibling();
        }

        Cursor.visible = false;
        _skipRequested = false;
        _sequenceActive = true;
        TrySubscribeLocaleChanged();
        StartCoroutine(IntroSequence());
    }

    private void Update()
    {
        if (!_allowSkipWithKeyboard || !_sequenceActive)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame ||
            Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            _skipRequested = true;
        }
    }

    private void OnDestroy()
    {
        TryUnsubscribeLocaleChanged();

        if (_logoBreathingCoroutine != null)
        {
            StopCoroutine(_logoBreathingCoroutine);
            _logoBreathingCoroutine = null;
        }

        StopTapToStartPulse();
        Cursor.visible = true;
    }

    private void TrySubscribeLocaleChanged()
    {
        if (_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged += OnIntroLocaleChanged;
        _localeSubscribed = true;
    }

    private void TryUnsubscribeLocaleChanged()
    {
        if (!_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged -= OnIntroLocaleChanged;
        _localeSubscribed = false;
    }

    private void OnIntroLocaleChanged()
    {
        if (_logoText != null)
        {
            _logoText.text = GameLocalization.GetUiString(UiStringKeys.IntroLogoTitle, DefaultLogoTitle);
            if (_logoTitleFontSize > 0f)
            {
                _logoText.fontSize = _logoTitleFontSize;
            }
        }

        if (_runtimeTagline != null)
        {
            _runtimeTagline.text = GameLocalization.GetUiString(UiStringKeys.IntroTagline,
                "Skill metrics: assessment and records");
        }

        if (_showingTapToStart && _statusText != null)
        {
            _statusText.text = GameLocalization.GetUiString(UiStringKeys.IntroTapToStart, "- TAP TO START -");
        }
    }

    private void TryCreateRuntimeTagline()
    {
        if (_logoText == null || _logoText.canvas == null)
        {
            return;
        }

        var go = new GameObject("Tagline");
        go.transform.SetParent(_logoText.canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1520f, 76f);
        rt.anchoredPosition = new Vector2(0f, -108f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = GameLocalization.GetUiString(UiStringKeys.IntroTagline,
            "Skill metrics: assessment and records");
        tmp.fontSize = 36f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.65f, 0.72f, 0.8f, 0f);
        if (TmpFontCache.LiberationSansSdf != null)
        {
            tmp.font = TmpFontCache.LiberationSansSdf;
        }

        _runtimeTagline = tmp;
    }

    private void TryCreateRuntimeVersionLabel()
    {
        Canvas canvas = _logoText != null ? _logoText.canvas : (_statusText != null ? _statusText.canvas : null);
        if (canvas == null)
        {
            return;
        }

        var go = new GameObject("VersionLabel");
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.sizeDelta = new Vector2(560f, 40f);
        rt.anchoredPosition = _versionLabelMargin;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        string ver = string.IsNullOrEmpty(Application.version) ? "0.0.0" : Application.version;
        tmp.text = $"v{ver}";
        tmp.fontSize = _versionLabelFontSize;
        tmp.alignment = TextAlignmentOptions.BottomLeft;
        tmp.color = new Color(0.52f, 0.58f, 0.66f, 0.82f);
        tmp.raycastTarget = false;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            tmp.font = TmpFontCache.LiberationSansSdf;
        }

        _runtimeVersionLabel = tmp;
    }

    private IEnumerator IntroSequence()
    {
        yield return null;

        yield return FadeInFromEntry();

        yield return WaitForLocalizationBoot();

        if (_statusText != null)
        {
            _statusText.text = "";
            _statusText.color = _defaultStatusColor;
        }

        yield return LogoIntroAnimation();

        if (_runtimeTagline != null)
        {
            yield return FadeRuntimeTagline();
        }

        if (_logoBreathingDuringStatus && _logoText != null)
        {
            _logoBreathingCoroutine = StartCoroutine(LogoBreathingLoop());
        }

        yield return TypewriterEffect(GameLocalization.GetUiString(UiStringKeys.IntroStatusConnecting,
            "CONNECTING TO SYSTEM..."));
        yield return WaitRealtimeSkippable(_phaseDisplayDuration);

        yield return TypewriterEffect(GameLocalization.GetUiString(UiStringKeys.IntroStatusStandby,
            "ASSESSMENT SYSTEM STANDBY..."));
        yield return WaitRealtimeSkippable(_phaseDisplayDuration);

        yield return TypewriterEffect(GameLocalization.GetUiString(UiStringKeys.IntroStatusAccessGranted,
            "ACCESS GRANTED"));
        if (_statusText != null)
        {
            _statusText.color = new Color(0.45f, 0.95f, 0.58f, 1f);
        }

        yield return WaitRealtimeSkippable(_accessGrantedHold);

        if (_logoBreathingCoroutine != null)
        {
            StopCoroutine(_logoBreathingCoroutine);
            _logoBreathingCoroutine = null;
        }

        if (_logoText != null)
        {
            _logoText.rectTransform.localScale = Vector3.one;
        }

        int lobbyIdx = ResolveLobbyBuildIndex();
        AsyncOperation preload = null;
        if (lobbyIdx >= 0)
        {
            preload = SceneManager.LoadSceneAsync(lobbyIdx, LoadSceneMode.Single);
            if (preload != null)
            {
                preload.allowSceneActivation = false;
            }
        }

        Cursor.visible = true;
        _skipRequested = false;
        ApplyTapToStartOnStatusLine();
        yield return WaitForProceedInput();

        _showingTapToStart = false;
        StopTapToStartPulse();
        yield return FadeOut();

        _sequenceActive = false;

        if (preload != null)
        {
            preload.allowSceneActivation = true;
            yield return preload;
        }
        else if (lobbyIdx >= 0)
        {
            SceneManager.LoadScene(lobbyIdx);
        }
        else
        {
            SceneManager.LoadScene(_mainSceneName);
        }
    }

    /// <summary>Replaces the status line with TAP TO START while the intro layout (logo, etc.) stays visible.</summary>
    private void ApplyTapToStartOnStatusLine()
    {
        if (_statusText == null)
        {
            return;
        }

        StopTapToStartPulse();
        _showingTapToStart = true;
        _statusText.alignment = TextAlignmentOptions.Center;
        _statusText.text = GameLocalization.GetUiString(UiStringKeys.IntroTapToStart, "- TAP TO START -");
        _statusText.fontSize = Mathf.Max(_statusText.fontSize, 40f);
        // Bright green CTA (distinct from status line gray and ACCESS GRANTED).
        _statusText.color = new Color(0.38f, 0.96f, 0.52f, 1f);
        _statusText.rectTransform.localScale = Vector3.one;
        _tapToStartPulseCoroutine = StartCoroutine(TapToStartPulseRoutine());
    }

    private void StopTapToStartPulse()
    {
        if (_tapToStartPulseCoroutine != null)
        {
            StopCoroutine(_tapToStartPulseCoroutine);
            _tapToStartPulseCoroutine = null;
        }

        if (_statusText != null)
        {
            _statusText.rectTransform.localScale = Vector3.one;
        }
    }

    private IEnumerator TapToStartPulseRoutine()
    {
        if (_statusText == null)
        {
            yield break;
        }

        RectTransform rt = _statusText.rectTransform;
        float amp = Mathf.Max(0.001f, _tapToStartPulseAmplitude);
        float speed = Mathf.Max(0.1f, _tapToStartPulseSpeed);

        while (_showingTapToStart && _statusText != null)
        {
            float s = 1f + amp * Mathf.Sin(Time.unscaledTime * speed);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        if (rt != null)
        {
            rt.localScale = Vector3.one;
        }
    }

    private IEnumerator WaitForProceedInput()
    {
        while (!WasProceedInputPressedThisFrame())
        {
            yield return null;
        }
    }

    private static bool WasProceedInputPressedThisFrame()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            foreach (KeyControl key in kb.allKeys)
            {
                if (key != null && key.wasPressedThisFrame)
                {
                    return true;
                }
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse != null &&
            (mouse.leftButton.wasPressedThisFrame ||
             mouse.rightButton.wasPressedThisFrame ||
             mouse.middleButton.wasPressedThisFrame))
        {
            return true;
        }

        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            if (pad.buttonSouth.wasPressedThisFrame ||
                pad.buttonNorth.wasPressedThisFrame ||
                pad.buttonEast.wasPressedThisFrame ||
                pad.buttonWest.wasPressedThisFrame ||
                pad.startButton.wasPressedThisFrame ||
                pad.selectButton.wasPressedThisFrame)
            {
                return true;
            }
        }

        Touchscreen touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }

        return false;
    }

    private int ResolveLobbyBuildIndex()
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            if (path.EndsWith(_mainSceneName + ".unity", System.StringComparison.OrdinalIgnoreCase) ||
                path.Contains("/" + _mainSceneName + ".unity"))
            {
                return i;
            }
        }

        return -1;
    }

    private IEnumerator FadeInFromEntry()
    {
        if (_fadeOverlay == null || _entryFadeInDuration <= 0f)
        {
            yield break;
        }

        float dur = _entryFadeInDuration;
        float elapsed = 0f;
        _fadeOverlay.raycastTarget = true;

        while (elapsed < dur)
        {
            if (_skipRequested)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / dur);
            _fadeOverlay.color = new Color(0f, 0f, 0f, t);
            yield return null;
        }

        _fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        _fadeOverlay.raycastTarget = false;
    }

    private IEnumerator FadeRuntimeTagline()
    {
        if (_runtimeTagline == null)
        {
            yield break;
        }

        float dur = Mathf.Max(0.05f, _taglineFadeDuration);
        Color c = _runtimeTagline.color;
        float targetA = 0.92f;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            if (_skipRequested)
            {
                t = 1f;
            }

            c.a = t * targetA;
            _runtimeTagline.color = c;
            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        c.a = targetA;
        _runtimeTagline.color = c;
    }

    private IEnumerator WaitRealtimeSkippable(float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end)
        {
            if (_skipRequested)
            {
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator LogoIntroAnimation()
    {
        if (_logoText == null)
        {
            yield break;
        }

        RectTransform rt = _logoText.rectTransform;
        float startScale = _logoStartScale;
        float dur = Mathf.Max(0.05f, _logoIntroDuration);
        float elapsed = 0f;

        while (elapsed < dur)
        {
            if (_skipRequested)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float ease = EaseOutBack(t);
            float alpha = SmoothStep(t);
            float sc = Mathf.LerpUnclamped(startScale, 1f, ease);
            rt.localScale = new Vector3(sc, sc, 1f);
            _logoText.color = new Color(_logoColorOpaque.r, _logoColorOpaque.g, _logoColorOpaque.b, alpha);
            yield return null;
        }

        rt.localScale = Vector3.one;
        _logoText.color = _logoColorOpaque;
    }

    private IEnumerator LogoBreathingLoop()
    {
        if (_logoText == null)
        {
            yield break;
        }

        RectTransform rt = _logoText.rectTransform;
        while (true)
        {
            float w = 1f + _logoBreathingAmplitude * Mathf.Sin(Time.unscaledTime * _logoBreathingSpeed);
            rt.localScale = new Vector3(w, w, 1f);
            yield return null;
        }
    }

    private static float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private IEnumerator TypewriterEffect(string fullText)
    {
        if (_statusText == null || string.IsNullOrEmpty(fullText))
        {
            yield break;
        }

        _statusText.color = _defaultStatusColor;
        _statusText.text = "";
        float nextCharTime = Time.realtimeSinceStartup;

        for (int i = 0; i < fullText.Length; i++)
        {
            if (_skipRequested)
            {
                _statusText.text = fullText;
                yield break;
            }

            while (Time.realtimeSinceStartup < nextCharTime)
            {
                yield return null;
            }

            nextCharTime = Time.realtimeSinceStartup + _typewriterInterval;

            _statusText.text = fullText.Substring(0, i + 1);
        }
    }

    private IEnumerator FadeOut()
    {
        if (_fadeOverlay == null)
        {
            yield break;
        }

        _fadeOverlay.raycastTarget = true;
        _fadeOverlay.transform.SetAsLastSibling();

        Color logo0 = _logoText != null ? _logoText.color : Color.white;
        Color status0 = _statusText != null ? _statusText.color : Color.white;
        Color tag0 = _runtimeTagline != null ? _runtimeTagline.color : Color.white;
        Color ver0 = _runtimeVersionLabel != null ? _runtimeVersionLabel.color : Color.white;

        float elapsed = 0f;
        float dur = _fadeOutDuration;

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float inv = 1f - t;
            _fadeOverlay.color = new Color(0f, 0f, 0f, t);

            if (_logoText != null)
            {
                _logoText.color = new Color(logo0.r, logo0.g, logo0.b, logo0.a * inv);
            }

            if (_statusText != null)
            {
                _statusText.color = new Color(status0.r, status0.g, status0.b, status0.a * inv);
            }

            if (_runtimeTagline != null)
            {
                _runtimeTagline.color = new Color(tag0.r, tag0.g, tag0.b, tag0.a * inv);
            }

            if (_runtimeVersionLabel != null)
            {
                _runtimeVersionLabel.color = new Color(ver0.r, ver0.g, ver0.b, ver0.a * inv);
            }

            yield return null;
        }

        _fadeOverlay.color = new Color(0f, 0f, 0f, 1f);
        if (_logoText != null)
        {
            Color c = _logoText.color;
            c.a = 0f;
            _logoText.color = c;
        }

        if (_statusText != null)
        {
            Color c = _statusText.color;
            c.a = 0f;
            _statusText.color = c;
        }

        if (_runtimeTagline != null)
        {
            Color c = _runtimeTagline.color;
            c.a = 0f;
            _runtimeTagline.color = c;
        }

        if (_runtimeVersionLabel != null)
        {
            Color c = _runtimeVersionLabel.color;
            c.a = 0f;
            _runtimeVersionLabel.color = c;
        }
    }

    private IEnumerator WaitForLocalizationBoot()
    {
        Task boot = GameLocalization.InitializeAndApplySavedLocaleAsync();
        float bootStart = Time.realtimeSinceStartup;
        float dotPhase = 0f;
        int dotCount = 0;

        while (!boot.IsCompleted &&
               (Time.realtimeSinceStartup - bootStart) < _maxLocalizationBootSeconds)
        {
            if (_statusText != null)
            {
                dotPhase += Time.unscaledDeltaTime;
                if (dotPhase >= 0.32f)
                {
                    dotPhase = 0f;
                    dotCount = (dotCount + 1) % 4;
                }

                string dots = dotCount == 0 ? "" : new string('.', dotCount);
                string bootBase = GameLocalization.GetUiString(UiStringKeys.IntroBootInitializing, "INITIALIZING");
                _statusText.text = bootBase + dots;
            }

            yield return null;
        }

        if (!boot.IsCompleted)
        {
            Debug.LogWarning(
                "[IntroController] Localization/Addressables 부팅이 시간 초과했습니다. 인트로를 계속합니다.");
        }
        else if (boot.IsFaulted && boot.Exception != null)
        {
            Debug.LogException(boot.Exception);
        }
    }
}
