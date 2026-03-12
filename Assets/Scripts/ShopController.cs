using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the dedicated shop panel for ticket purchases and gacha actions.
/// This component only handles shop UI input and display updates.
/// </summary>
public sealed class ShopController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _shopTokenText;
    [SerializeField] private TextMeshProUGUI _gachaResultText;
    [SerializeField] private Button _buyTicketButton;
    [SerializeField] private Button _gachaButton;
    [SerializeField] private Button _exitButton;

    private void Awake()
    {
        if (_buyTicketButton != null)
        {
            _buyTicketButton.onClick.AddListener(OnBuyTicketClicked);
        }

        if (_gachaButton != null)
        {
            _gachaButton.onClick.AddListener(OnGachaClicked);
        }

        if (_exitButton != null)
        {
            _exitButton.onClick.AddListener(OnExitClicked);
        }
    }

    private void Start()
    {
        UpdateShopTokenText();

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnEconomyChanged += HandleEconomyChanged;
        }
    }

    private void OnDestroy()
    {
        if (_buyTicketButton != null)
        {
            _buyTicketButton.onClick.RemoveListener(OnBuyTicketClicked);
        }

        if (_gachaButton != null)
        {
            _gachaButton.onClick.RemoveListener(OnGachaClicked);
        }

        if (_exitButton != null)
        {
            _exitButton.onClick.RemoveListener(OnExitClicked);
        }

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnEconomyChanged -= HandleEconomyChanged;
        }
    }

    /// <summary>
    /// Purchases one official exam ticket for 100 tokens.
    /// </summary>
    private void OnBuyTicketClicked()
    {
        if (EconomyManager.Instance == null || _gachaResultText == null)
        {
            return;
        }

        if (EconomyManager.Instance.SpendTokens(100))
        {
            EconomyManager.Instance.AddTickets(1);
            _gachaResultText.text = "응시권 1장 구매 완료!";
            return;
        }

        _gachaResultText.text = "토큰이 부족합니다!";
    }

    /// <summary>
    /// Runs the hardcore target gacha and stores duplicate drops as stackable inventory.
    /// </summary>
    private void OnGachaClicked()
    {
        if (EconomyManager.Instance == null
            || InventoryManager.Instance == null
            || ItemDatabase.Instance == null
            || _gachaResultText == null)
        {
            return;
        }

        if (!EconomyManager.Instance.SpendTokens(500))
        {
            _gachaResultText.text = "토큰 부족!";
            return;
        }

        var allItems = ItemDatabase.Instance.GetAllItems().ToList();

        if (allItems.Count == 0)
        {
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, allItems.Count);
        ItemData randomItem = allItems[randomIndex];

        if (randomItem == null || string.IsNullOrWhiteSpace(randomItem.ItemId))
        {
            return;
        }

        InventoryManager.Instance.AddItem(randomItem.ItemId, 1);
        _gachaResultText.text = $"[{GetTierName(randomItem.Tier)}] {randomItem.ItemName} 획득!";
    }

    /// <summary>
    /// Returns the player to the main menu state.
    /// </summary>
    private void OnExitClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetGameState(GameState.MainMenu);
    }

    /// <summary>
    /// Refreshes the displayed wallet balance whenever the economy changes.
    /// </summary>
    private void HandleEconomyChanged()
    {
        UpdateShopTokenText();
    }

    /// <summary>
    /// Updates the current token balance text in the shop UI.
    /// </summary>
    private void UpdateShopTokenText()
    {
        if (_shopTokenText == null)
        {
            return;
        }

        int tokens = EconomyManager.Instance != null ? EconomyManager.Instance.Tokens : 0;
        _shopTokenText.text = $"TOKENS: {tokens}";
    }

    private string GetTierName(ItemTier tier)
    {
        switch (tier)
        {
            case ItemTier.Tier1:
                return "凡";
            case ItemTier.Tier2:
                return "奇";
            case ItemTier.Tier3:
                return "珍";
            case ItemTier.Tier4:
                return "傑";
            case ItemTier.Tier5:
                return "古";
            case ItemTier.Tier6:
                return "遺";
            case ItemTier.Tier7:
                return "聖";
            case ItemTier.Tier8:
                return "傳";
            case ItemTier.Tier9:
                return "神";
            case ItemTier.Tier10:
                return "極";
            default:
                return "凡";
        }
    }
}
