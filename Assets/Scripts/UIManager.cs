using TMPro;
using UnityEngine;

/// <summary>
/// Controls state-based panel visibility for the G.S.I user interface.
/// This component only turns UI panels on and off in response to game state changes.
/// </summary>
public sealed class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _craftingPanel;
    [SerializeField] private GameObject _equipmentPanel;
    [SerializeField] private GameObject _inventoryPanel;
    [SerializeField] private GameObject _shopPanel;
    [SerializeField] private GameObject _testStandbyPanel;
    [SerializeField] private GameObject _testInProgressPanel;
    [SerializeField] private GameObject _resultScreenPanel;
    [SerializeField] private TextMeshProUGUI _bestRecordText;

    private void Awake()
    {
        ValidatePanelReferences();
    }

    private void Start()
    {
        RefreshCurrentState();
        SubscribeToGameManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameManager();
    }

    /// <summary>
    /// Subscribes to the central state manager so UI can react to official flow changes.
    /// </summary>
    private void SubscribeToGameManager()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnGameStateChanged += UpdateUIState;
        Debug.Log("UIManager: GameManager 이벤트 구독 완료.");
    }

    /// <summary>
    /// Removes the state change subscription when this component is disabled.
    /// </summary>
    private void UnsubscribeFromGameManager()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnGameStateChanged -= UpdateUIState;
    }

    /// <summary>
    /// Reports missing panel references so inspector setup issues are immediately visible.
    /// </summary>
    private void ValidatePanelReferences()
    {
        if (_mainMenuPanel == null)
        {
            Debug.LogError("UIManager: _mainMenuPanel is not assigned.");
        }

        if (_testStandbyPanel == null)
        {
            Debug.LogError("UIManager: _testStandbyPanel is not assigned.");
        }

        if (_craftingPanel == null)
        {
            Debug.LogError("UIManager: _craftingPanel is not assigned.");
        }

        if (_equipmentPanel == null)
        {
            Debug.LogError("UIManager: _equipmentPanel is not assigned.");
        }

        if (_inventoryPanel == null)
        {
            Debug.LogError("UIManager: _inventoryPanel is not assigned.");
        }

        if (_shopPanel == null)
        {
            Debug.LogError("UIManager: _shopPanel is not assigned.");
        }

        if (_testInProgressPanel == null)
        {
            Debug.LogError("UIManager: _testInProgressPanel is not assigned.");
        }

        if (_resultScreenPanel == null)
        {
            Debug.LogError("UIManager: _resultScreenPanel is not assigned.");
        }
    }

    /// <summary>
    /// Applies the current game state immediately so the correct panel is visible from the start.
    /// </summary>
    private void RefreshCurrentState()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("UIManager: GameManager instance was not found during initial UI refresh.");
            SetAllPanels(false);
            return;
        }

        UpdateUIState(GameManager.Instance.CurrentState);
    }

    /// <summary>
    /// Activates only the panel that matches the current state and disables the rest.
    /// </summary>
    private void UpdateUIState(GameState gameState)
    {
        Debug.Log($"UIManager: UI 상태 전환 시도 -> {gameState}");
        SetAllPanels(false);

        switch (gameState)
        {
            case GameState.MainMenu:
                SetPanelActive(_mainMenuPanel, true);
                UpdateBestRecordText();
                break;

            case GameState.Crafting:
                SetPanelActive(_craftingPanel, true);
                break;

            case GameState.Equipment:
                SetPanelActive(_equipmentPanel, true);
                break;

            case GameState.Inventory:
                SetPanelActive(_inventoryPanel, true);
                break;

            case GameState.Shop:
                SetPanelActive(_shopPanel, true);
                break;

            case GameState.TestStandby:
                SetPanelActive(_testStandbyPanel, true);
                break;

            case GameState.TestInProgress:
                SetPanelActive(_testInProgressPanel, true);
                break;

            case GameState.TestCompleted:
            case GameState.ResultScreen:
                SetPanelActive(_resultScreenPanel, true);
                break;
        }
    }

    /// <summary>
    /// Updates the main lobby best-record label using the latest persistent player data.
    /// </summary>
    private void UpdateBestRecordText()
    {
        if (_bestRecordText == null)
        {
            return;
        }

        float bestReactionTime = PlayerDataManager.Instance != null
            ? PlayerDataManager.Instance.BestReactionTime
            : 99.99f;
        float bestAimTime = PlayerDataManager.Instance != null
            ? PlayerDataManager.Instance.BestAimTime
            : 99.99f;

        string reactionText = bestReactionTime >= 99.99f
            ? "-- SEC"
            : $"{bestReactionTime:F3} SEC";
        string aimText = bestAimTime >= 99.99f
            ? "-- SEC"
            : $"{bestAimTime:F3} SEC";

        _bestRecordText.text = $"BEST REACTION: {reactionText}\nBEST AIM: {aimText}";
    }

    /// <summary>
    /// Applies the same active state to all managed panels.
    /// </summary>
    private void SetAllPanels(bool isActive)
    {
        SetPanelActive(_mainMenuPanel, isActive);
        SetPanelActive(_craftingPanel, isActive);
        SetPanelActive(_equipmentPanel, isActive);
        SetPanelActive(_inventoryPanel, isActive);
        SetPanelActive(_shopPanel, isActive);
        SetPanelActive(_testStandbyPanel, isActive);
        SetPanelActive(_testInProgressPanel, isActive);
        SetPanelActive(_resultScreenPanel, isActive);
    }

    /// <summary>
    /// Safely updates a panel reference only when it has been assigned in the inspector.
    /// </summary>
    private void SetPanelActive(GameObject targetPanel, bool isActive)
    {
        if (targetPanel == null)
        {
            return;
        }

        targetPanel.SetActive(isActive);
    }
}
