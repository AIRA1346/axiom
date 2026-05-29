using TMPro;
using UnityEngine;

/// <summary>
/// 로비 씬용 패널 표시: G.S.I <see cref="GameState"/>만 처리합니다.
/// </summary>
public sealed class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _testStandbyPanel;
    [SerializeField] private GameObject _testInProgressPanel;
    [SerializeField] private GameObject _resultScreenPanel;
    [SerializeField] private TextMeshProUGUI _bestRecordText;

    private GsiTestBriefingUi.Refs _briefingRefs;
    private bool _localeSubscribed;
    private bool _appearanceSubscribed;

    private void Start()
    {
        RefreshCurrentState();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += UpdateUIState;
        }

        TrySubscribeLocaleChanged();
        TrySubscribeAppearance();
    }

    private void OnDestroy()
    {
        TryUnsubscribeLocaleChanged();
        TryUnsubscribeAppearance();

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

        GameLocalization.UiLocaleChanged += OnLobbyUiLocaleChanged;
        _localeSubscribed = true;
    }

    private void TryUnsubscribeLocaleChanged()
    {
        if (!_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged -= OnLobbyUiLocaleChanged;
        _localeSubscribed = false;
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
        GsiUiAppearance.Changed += OnLobbyUiAppearanceChanged;
        _appearanceSubscribed = true;
    }

    private void TryUnsubscribeAppearance()
    {
        if (!_appearanceSubscribed)
        {
            return;
        }

        GsiUiAppearance.Changed -= OnLobbyUiAppearanceChanged;
        _appearanceSubscribed = false;
    }

    private void OnLobbyUiAppearanceChanged()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (GameManager.Instance.CurrentState == GameState.MainMenu)
        {
            ApplyLobbyHudTextChrome();
        }

        if (GameManager.Instance.CurrentState == GameState.TestBriefing && _briefingRefs != null)
        {
            GsiTestBriefingUi.RefreshChrome(_briefingRefs);
        }
    }

    private void OnLobbyUiLocaleChanged()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (GameManager.Instance.CurrentState == GameState.TestBriefing && _briefingRefs != null)
        {
            GsiTestBriefingUi.ApplyContent(_briefingRefs);
            GsiTestBriefingUi.RefreshChrome(_briefingRefs);
        }

        if (GameManager.Instance.CurrentState == GameState.MainMenu)
        {
            UpdateBestRecordText();
            ApplyLobbyHudTextChrome();
        }

        if (GameManager.Instance.CurrentState == GameState.TestStandby)
        {
            RefreshTestStandbyHint();
        }
    }

    /// <summary>
    /// TestStandby 패널 Hint는 씬에 박힌 문자열일 수 있어, 표시 시·언어 변경 시 <c>briefing.tap_anywhere</c>로 맞춥니다.
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

    private void RefreshCurrentState()
    {
        if (GameManager.Instance == null)
        {
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
                SetPanelActive(_mainMenuPanel, true);
                UpdateBestRecordText();
                ApplyLobbyHudTextChrome();
                break;

            case GameState.TestBriefing:
                EnsureTestBriefingUi();
                SetBriefingVisible(true);
                break;

            case GameState.TestStandby:
                SetPanelActive(_testStandbyPanel, true);
                RefreshTestStandbyHint();
                break;

            case GameState.UnifiedExamInterstitial:
                SetPanelActive(_testStandbyPanel, true);
                break;

            case GameState.TestInProgress:
                SetPanelActive(_testInProgressPanel, true);
                break;

            case GameState.TestCompleted:
            case GameState.ResultScreen:
                SetPanelActive(_resultScreenPanel, true);
                break;

            default:
                break;
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

        string missing = GameLocalization.GetUiString(UiStringKeys.LobbyBestRecordTimeMissing, "-- SEC");
        string r = bestReaction >= 99.99f
            ? missing
            : GameLocalization.FormatUiString(UiStringKeys.LobbyBestRecordSecondsFmt, "{0:F3} SEC", bestReaction);
        string a = bestAim >= 99.99f
            ? missing
            : GameLocalization.FormatUiString(UiStringKeys.LobbyBestRecordSecondsFmt, "{0:F3} SEC", bestAim);
        _bestRecordText.text = GameLocalization.FormatUiString(UiStringKeys.LobbyBestRecordsReactionAimFmt,
            "BEST REACTION: {0}\nBEST AIM: {1}", r, a);
        _bestRecordText.raycastTarget = false;
        ApplyLobbyHudTextChrome();
    }

    /// <summary>메인 로비 베스트 기록 등 — 다크/라이트 전환 시 가독색 유지.</summary>
    private void ApplyLobbyHudTextChrome()
    {
        if (_bestRecordText == null)
        {
            return;
        }

        _bestRecordText.color = GsiUiAppearance.TextSecondary;
    }

    private void SetAllPanels(bool active)
    {
        SetPanelActive(_mainMenuPanel, active);
        SetPanelActive(_testStandbyPanel, active);
        SetPanelActive(_testInProgressPanel, active);
        SetPanelActive(_resultScreenPanel, active);
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
