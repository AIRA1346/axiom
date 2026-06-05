using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// G.S.I 전용 씬에서만 사용. 테스트 관련 패널만 전환합니다.
/// GameState.MainMenu 는 이 씬 안에서 "G.S.I 로비"를 의미합니다.
/// </summary>
public sealed class GSIUIManager : MonoBehaviour
{
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _testStandbyPanel;
    [SerializeField] private GameObject _testInProgressPanel;
    [SerializeField] private GameObject _resultScreenPanel;
    [SerializeField] private TextMeshProUGUI _bestRecordText;

    private GameObject _unifiedInterstitialPanel;
    private TextMeshProUGUI _unifiedInterstitialText;
    private TextMeshProUGUI _unifiedInterstitialTitleTmp;
    private TextMeshProUGUI _unifiedInterstitialContinueTmp;
    private Button _unifiedInterstitialContinue;

    private GsiTestBriefingUi.Refs _briefingRefs;
    private float _briefingEnterTime;
    private bool _localeSubscribed;

    private void Start()
    {
        RefreshCurrentState();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += UpdateUIState;
        }

        TrySubscribeLocaleChanged();
    }

    private void OnDestroy()
    {
        TryUnsubscribeLocaleChanged();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= UpdateUIState;
        }
    }

    private void TrySubscribeLocaleChanged()
    {
        if (_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged += OnGsiUiLocaleChanged;
        _localeSubscribed = true;
    }

    private void TryUnsubscribeLocaleChanged()
    {
        if (!_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged -= OnGsiUiLocaleChanged;
        _localeSubscribed = false;
    }

    private void OnGsiUiLocaleChanged()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameState st = GameManager.Instance.CurrentState;

        if (st == GameState.TestBriefing && _briefingRefs != null)
        {
            GsiTestBriefingUi.ApplyContent(_briefingRefs);
            GsiTestBriefingUi.RefreshChrome(_briefingRefs);
        }

        if (st == GameState.UnifiedExamInterstitial)
        {
            RefreshUnifiedInterstitialLocalizedLabels();
            UpdateUnifiedInterstitialText();
        }

        if (st == GameState.MainMenu)
        {
            UpdateBestRecordText();
        }

        if (st == GameState.TestStandby)
        {
            RefreshTestStandbyHint();
        }
    }

    /// <summary>
    /// TestStandby 패널의 Hint는 씬 직렬화 문자열일 수 있어, 표시 시·언어 변경 시 현재 로케일로 맞춥니다.
    /// </summary>
    private void RefreshTestStandbyHint()
    {
        if (_testStandbyPanel == null)
        {
            return;
        }

        Transform hint = _testStandbyPanel.transform.Find("Hint");
        if (hint == null)
        {
            return;
        }

        var tmp = hint.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            return;
        }

        tmp.text = GameLocalization.GetUiString(UiStringKeys.BriefingTapAnywhere, "Tap anywhere to begin.");
    }

    private void RefreshUnifiedInterstitialLocalizedLabels()
    {
        if (_unifiedInterstitialTitleTmp != null)
        {
            _unifiedInterstitialTitleTmp.text =
                GameLocalization.GetUiString(UiStringKeys.GsiInterstitialTitle, "Next subject");
        }

        if (_unifiedInterstitialContinueTmp != null)
        {
            _unifiedInterstitialContinueTmp.text =
                GameLocalization.GetUiString(UiStringKeys.GsiInterstitialContinue, "Continue");
        }
    }

    private void RefreshCurrentState()
    {
        if (GameManager.Instance == null)
        {
            SetAllPanels(false);
            return;
        }

        UpdateUIState(GameManager.Instance.CurrentState);
    }

    private void UpdateUIState(GameState gameState)
    {
        SetAllPanels(false);

        switch (gameState)
        {
            case GameState.MainMenu:
                SetPanelActive(_lobbyPanel, true);
                UpdateBestRecordText();
                break;

            case GameState.TestBriefing:
                _briefingEnterTime = Time.unscaledTime;
                EnsureTestBriefingUi();
                SetBriefingVisible(true);
                break;

            case GameState.TestStandby:
                SetPanelActive(_testStandbyPanel, true);
                RefreshTestStandbyHint();
                break;

            case GameState.TestInProgress:
                SetPanelActive(_testInProgressPanel, true);
                break;

            case GameState.UnifiedExamInterstitial:
                EnsureUnifiedInterstitialPanel();
                SetPanelActive(_unifiedInterstitialPanel, true);
                UpdateUnifiedInterstitialText();
                break;

            case GameState.TestCompleted:
            case GameState.ResultScreen:
                SetPanelActive(_resultScreenPanel, true);
                break;

            default:
                SetPanelActive(_lobbyPanel, true);
                break;
        }
    }

    private void UpdateUnifiedInterstitialText()
    {
        if (_unifiedInterstitialText != null && GameManager.Instance != null)
        {
            _unifiedInterstitialText.text = GameManager.Instance.GetUnifiedExamInterstitialText();
        }
    }

    private void EnsureUnifiedInterstitialPanel()
    {
        if (_unifiedInterstitialPanel != null)
        {
            return;
        }

        Transform parent = _testStandbyPanel != null ? _testStandbyPanel.transform.parent : transform;
        var go = new GameObject("UnifiedExamInterstitialPanel");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.07f, 0.1f, 0.96f);
        bg.raycastTarget = true;

        var box = new GameObject("Box");
        box.transform.SetParent(go.transform, false);
        var boxRt = box.AddComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(560f, 220f);
        boxRt.anchoredPosition = Vector2.zero;
        var boxImg = box.AddComponent<Image>();
        boxImg.color = new Color(0.14f, 0.16f, 0.22f, 1f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(box.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.55f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = new Vector2(24f, 0f);
        titleRt.offsetMax = new Vector2(-24f, -12f);
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        _unifiedInterstitialTitleTmp = titleTmp;
        titleTmp.text = GameLocalization.GetUiString(UiStringKeys.GsiInterstitialTitle, "Next subject");
        titleTmp.fontSize = 26;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;

        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(box.transform, false);
        var bodyRt = bodyGo.AddComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0f, 0.28f);
        bodyRt.anchorMax = new Vector2(1f, 0.55f);
        bodyRt.offsetMin = new Vector2(20f, 0f);
        bodyRt.offsetMax = new Vector2(-20f, 0f);
        _unifiedInterstitialText = bodyGo.AddComponent<TextMeshProUGUI>();
        _unifiedInterstitialText.fontSize = 22;
        _unifiedInterstitialText.alignment = TextAlignmentOptions.Center;
        _unifiedInterstitialText.color = new Color(0.85f, 0.88f, 0.92f, 1f);

        var btnGo = new GameObject("Continue");
        btnGo.transform.SetParent(box.transform, false);
        var btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.sizeDelta = new Vector2(200f, 48f);
        btnRt.anchoredPosition = new Vector2(0f, 28f);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.28f, 0.42f, 0.62f, 1f);
        _unifiedInterstitialContinue = btnGo.AddComponent<Button>();
        _unifiedInterstitialContinue.targetGraphic = btnImg;
        _unifiedInterstitialContinue.onClick.AddListener(OnUnifiedInterstitialContinue);

        var btnLabelGo = new GameObject("Text");
        btnLabelGo.transform.SetParent(btnGo.transform, false);
        var btnLabelRt = btnLabelGo.AddComponent<RectTransform>();
        btnLabelRt.anchorMin = Vector2.zero;
        btnLabelRt.anchorMax = Vector2.one;
        btnLabelRt.offsetMin = Vector2.zero;
        btnLabelRt.offsetMax = Vector2.zero;
        var btnTmp = btnLabelGo.AddComponent<TextMeshProUGUI>();
        _unifiedInterstitialContinueTmp = btnTmp;
        btnTmp.text = GameLocalization.GetUiString(UiStringKeys.GsiInterstitialContinue, "Continue");
        btnTmp.fontSize = 22;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.color = Color.white;

        _unifiedInterstitialPanel = go;
        _unifiedInterstitialPanel.SetActive(false);
    }

    private void OnUnifiedInterstitialContinue()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.TestBriefing);
        }
    }

    private void EnsureTestBriefingUi()
    {
        Transform parent = _testStandbyPanel != null ? _testStandbyPanel.transform.parent : transform;
        _briefingRefs = GsiTestBriefingUi.Ensure(parent, OnTestBriefingStartClicked);
        GsiTestBriefingUi.ApplyContent(_briefingRefs);
        GsiTestBriefingUi.RefreshChrome(_briefingRefs);
    }

    private void OnTestBriefingStartClicked()
    {
        if (Time.unscaledTime - _briefingEnterTime < 0.25f)
        {
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.TestInProgress);
        }
    }

    private void SetBriefingVisible(bool visible)
    {
        if (_briefingRefs != null && _briefingRefs.Root != null)
        {
            _briefingRefs.Root.SetActive(visible);
            if (visible)
            {
                GsiTestBriefingUi.ApplyContent(_briefingRefs);
                GsiTestBriefingUi.RefreshChrome(_briefingRefs);
                _briefingRefs.Root.transform.SetAsLastSibling();
            }
        }
    }

    private void UpdateBestRecordText()
    {
        if (_bestRecordText == null)
        {
            return;
        }

        float bestReaction = PlayerDataManager.Instance != null
            ? PlayerDataManager.Instance.BestReactionTime
            : 99.99f;
        float bestAim = PlayerDataManager.Instance != null
            ? PlayerDataManager.Instance.BestAimTime
            : 99.99f;
        float bestMem = PlayerDataManager.Instance != null
            ? PlayerDataManager.Instance.BestMemorySpan
            : -1f;
        float bestRhythmAcc = PlayerDataManager.Instance != null
            ? PlayerDataManager.Instance.BestRhythmAccuracy
            : -1f;
        float bestMotAcc = PlayerDataManager.Instance != null
            ? PlayerDataManager.Instance.BestMotAccuracy
            : -1f;
        float bestBh = PlayerDataManager.Instance != null
            ? PlayerDataManager.Instance.BestBulletHellTime
            : -1f;
        float bestCps = PlayerDataManager.Instance != null
            ? PlayerDataManager.Instance.BestCps
            : -1f;

        string r = bestReaction >= 99.99f ? "--" : $"{bestReaction:F3}s";
        string a = bestAim >= 99.99f ? "--" : $"{bestAim:F3}s";
        string m = bestMem < 0f ? "--" : $"{bestMem:F2}";
        string y = bestRhythmAcc < 0f ? "--" : $"{bestRhythmAcc:F1}%";
        string o = bestMotAcc < 0f ? "--" : $"{bestMotAcc:F1}%";
        string h = bestBh < 0f ? "--" : $"{bestBh:F1}s";
        string c = bestCps < 0f ? "--" : $"{bestCps:F2}/s";
        _bestRecordText.text = GameLocalization.FormatUiString(UiStringKeys.GsiLobbyBestRecordsFmt,
            "BEST  Reaction {0}   Aim {1}   Memory {2}\nBEST  Rhythm {3}   MOT {4}   Bullet Hell {5}   CPS {6}",
            r, a, m, y, o, h, c);
        _bestRecordText.fontSize = 18f;
        _bestRecordText.lineSpacing = 0f;
        _bestRecordText.raycastTarget = false;
    }

    private void SetAllPanels(bool active)
    {
        SetPanelActive(_lobbyPanel, active);
        SetPanelActive(_testStandbyPanel, active);
        SetPanelActive(_testInProgressPanel, active);
        SetPanelActive(_resultScreenPanel, active);
        if (_unifiedInterstitialPanel != null)
        {
            SetPanelActive(_unifiedInterstitialPanel, active);
        }

        if (_briefingRefs != null && _briefingRefs.Root != null)
        {
            _briefingRefs.Root.SetActive(active);
        }
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }
}
