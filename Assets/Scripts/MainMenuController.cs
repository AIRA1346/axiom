using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the main lobby buttons and economy text presentation.
/// This component only routes menu input and displays current token and ticket values.
/// </summary>
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button _practiceButton;
    [SerializeField] private Button _aimPracticeButton;
    [SerializeField] private Button _reactionExamButton;
    [SerializeField] private Button _aimExamButton;
    [SerializeField] private Button _craftingButton;
    [SerializeField] private Button _equipmentButton;
    [SerializeField] private Button _inventoryButton;
    [SerializeField] private Button _shopButton;
    [SerializeField] private TextMeshProUGUI _tokenText;
    [SerializeField] private TextMeshProUGUI _ticketText;

    private void Awake()
    {
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

        if (_craftingButton != null)
        {
            _craftingButton.onClick.AddListener(OnCraftingClicked);
        }

        if (_equipmentButton != null)
        {
            _equipmentButton.onClick.AddListener(OnEquipmentClicked);
        }

        if (_inventoryButton != null)
        {
            _inventoryButton.onClick.AddListener(OnInventoryClicked);
        }

        if (_shopButton != null)
        {
            _shopButton.onClick.AddListener(OnShopClicked);
        }
    }

    private void Start()
    {
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

        if (_craftingButton != null)
        {
            _craftingButton.onClick.RemoveListener(OnCraftingClicked);
        }

        if (_equipmentButton != null)
        {
            _equipmentButton.onClick.RemoveListener(OnEquipmentClicked);
        }

        if (_inventoryButton != null)
        {
            _inventoryButton.onClick.RemoveListener(OnInventoryClicked);
        }

        if (_shopButton != null)
        {
            _shopButton.onClick.RemoveListener(OnShopClicked);
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

    /// <summary>
    /// Starts the current practice-mode test flow.
    /// </summary>
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

    /// <summary>
    /// Starts the aim-precision practice flow.
    /// </summary>
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

    private void OnReactionExamClicked()
    {
        if (EconomyManager.Instance == null || GameManager.Instance == null)
        {
            return;
        }

        if (EconomyManager.Instance.UseTicket())
        {
            UpdateEconomyTexts();
            GameManager.Instance.SetTestType(TestType.OfficialExam);
            GameManager.Instance.SetTestMode(TestMode.Reaction);
            GameManager.Instance.SetGameState(GameState.TestStandby);
            Debug.Log("공식 시험 진입!");
            return;
        }

        Debug.Log("응시권 부족!");
    }

    private void OnAimExamClicked()
    {
        if (EconomyManager.Instance == null || GameManager.Instance == null)
        {
            return;
        }

        if (EconomyManager.Instance.UseTicket())
        {
            UpdateEconomyTexts();
            GameManager.Instance.SetTestType(TestType.OfficialExam);
            GameManager.Instance.SetTestMode(TestMode.AimPrecision);
            GameManager.Instance.SetGameState(GameState.TestStandby);
            Debug.Log("공식 시험 진입!");
            return;
        }

        Debug.Log("응시권 부족!");
    }

    private void OnShopClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetGameState(GameState.Shop);
    }

    private void OnCraftingClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetGameState(GameState.Crafting);
    }

    private void OnEquipmentClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetGameState(GameState.Equipment);
    }

    private void OnInventoryClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetGameState(GameState.Inventory);
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

    /// <summary>
    /// Refreshes the main menu economy labels from the current persistent data.
    /// </summary>
    private void UpdateEconomyTexts()
    {
        if (_tokenText != null)
        {
            int tokens = EconomyManager.Instance != null ? EconomyManager.Instance.Tokens : 0;
            _tokenText.text = $"TOKENS: {tokens}";
        }

        if (_ticketText != null)
        {
            int tickets = EconomyManager.Instance != null ? EconomyManager.Instance.ExamTickets : 0;
            _ticketText.text = $"TICKETS: {tickets}";
        }
    }
}
