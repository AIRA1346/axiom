using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the main lobby buttons and economy text presentation.
/// This component only routes menu input and displays current token and ticket values.
/// <see cref="_enterGsiFacilityButton"/>이 있으면 G.S.I는 전용 씬으로만 들어가고, 기존 4개 시험 버튼은 숨깁니다.
/// </summary>
public sealed class MainMenuController : MonoBehaviour
{
    [Header("Healing")]
    [Tooltip("할당 시 힐링 허브 씬(낚시 등)으로 이동합니다.")]
    [SerializeField] private Button _healingGameButton;

    [Header("ARCHÉ Open World")]
    [Tooltip("할당 시 오픈월드 프로토타입 씬(Unity-Chan)으로 이동합니다. 빌드에 OpenWorldScene 필요.")]
    [SerializeField] private Button _archEOpenWorldButton;

    [Header("G.S.I")]
    [Tooltip("할당 시 G.S.I 시설 씬으로 이동하며, 아래 4개 시험 버튼은 비활성화됩니다.")]
    [SerializeField] private Button _enterGsiFacilityButton;

    [SerializeField] private Button _practiceButton;
    [SerializeField] private Button _aimPracticeButton;
    [SerializeField] private Button _reactionExamButton;
    [SerializeField] private Button _aimExamButton;
    [SerializeField] private Button _craftingButton;
    [SerializeField] private Button _equipmentButton;
    [SerializeField] private Button _inventoryButton;
    [SerializeField] private Button _shopButton;
    [SerializeField] private Button _encyclopediaButton;
    [SerializeField] private Button _codexButton;
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

        if (_encyclopediaButton != null)
        {
            _encyclopediaButton.onClick.AddListener(OnEncyclopediaClicked);
        }

        if (_codexButton != null)
        {
            _codexButton.onClick.AddListener(OnCodexClicked);
        }

        if (_enterGsiFacilityButton != null)
        {
            _enterGsiFacilityButton.onClick.AddListener(OnEnterGsiFacilityClicked);
        }

        if (_healingGameButton != null)
        {
            _healingGameButton.onClick.AddListener(OnHealingGameClicked);
        }

        if (_archEOpenWorldButton != null)
        {
            _archEOpenWorldButton.onClick.AddListener(OnArchEOpenWorldClicked);
        }
    }

    private void Start()
    {
        if (_enterGsiFacilityButton != null)
        {
            HideLegacyGsiEntryButtons();
        }

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

        if (_encyclopediaButton != null)
        {
            _encyclopediaButton.onClick.RemoveListener(OnEncyclopediaClicked);
        }

        if (_codexButton != null)
        {
            _codexButton.onClick.RemoveListener(OnCodexClicked);
        }

        if (_enterGsiFacilityButton != null)
        {
            _enterGsiFacilityButton.onClick.RemoveListener(OnEnterGsiFacilityClicked);
        }

        if (_healingGameButton != null)
        {
            _healingGameButton.onClick.RemoveListener(OnHealingGameClicked);
        }

        if (_archEOpenWorldButton != null)
        {
            _archEOpenWorldButton.onClick.RemoveListener(OnArchEOpenWorldClicked);
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

    private void OnEnterGsiFacilityClicked()
    {
        GsiSceneNavigation.LoadGsiFacility();
    }

    private void OnHealingGameClicked()
    {
        HealingSceneNavigation.LoadHealingHub();
    }

    private void OnArchEOpenWorldClicked()
    {
        OpenWorldSceneNavigation.LoadOpenWorld();
    }

    private void HideLegacyGsiEntryButtons()
    {
        void Hide(Button b)
        {
            if (b != null)
            {
                b.gameObject.SetActive(false);
            }
        }

        Hide(_practiceButton);
        Hide(_aimPracticeButton);
        Hide(_reactionExamButton);
        Hide(_aimExamButton);
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

    /// <summary>레거시 버튼: 과목별 공식 시험 대신 G.S.I 시설(통합 공식 시험)으로 이동합니다.</summary>
    private void OnReactionExamClicked()
    {
        GsiSceneNavigation.LoadGsiFacility();
    }

    /// <summary>레거시 버튼: 과목별 공식 시험 대신 G.S.I 시설(통합 공식 시험)으로 이동합니다.</summary>
    private void OnAimExamClicked()
    {
        GsiSceneNavigation.LoadGsiFacility();
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

    private void OnEncyclopediaClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetGameState(GameState.Encyclopedia);
    }

    private void OnCodexClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetGameState(GameState.Codex);
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
            int gold = EconomyManager.Instance != null ? EconomyManager.Instance.Tokens : 0;
            _tokenText.text = $"기초 골드: {gold}";
        }

        if (_ticketText != null)
        {
            int tickets = EconomyManager.Instance != null ? EconomyManager.Instance.ExamTickets : 0;
            _ticketText.text = $"기초 티켓: {tickets}";
        }
    }
}
