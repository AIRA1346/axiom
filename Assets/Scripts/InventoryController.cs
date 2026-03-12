using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Controls the inventory panel UI and item selling interactions.
/// This component only reads inventory data and updates inventory-related UI.
/// </summary>
public sealed class InventoryController : MonoBehaviour
{
    private sealed class InventoryDisplayEntry
    {
        public ItemInstance Instance;
        public ItemData Data;
    }

    [SerializeField] private Transform _itemContainer;
    [SerializeField] private GameObject _itemSlotPrefab;
    [SerializeField] private Button _exitButton;
    [SerializeField] private Button _filterAllBtn;
    [SerializeField] private Button _filterEquipBtn;
    [SerializeField] private Button _filterMaterialBtn;
    [SerializeField] private Button _filterCosmeticBtn;
    [SerializeField] private Button _filterConsumableBtn;
    [SerializeField] private Button _filterArtifactBtn;
    [SerializeField] private Button _filterEtcBtn;

    private ItemMainCategory _currentMainFilter = ItemMainCategory.None;
    private ItemMiddleCategory _currentMiddleFilter = ItemMiddleCategory.None;

    private UnityAction _filterAllAction;
    private UnityAction _filterEquipAction;
    private UnityAction _filterMaterialAction;
    private UnityAction _filterCosmeticAction;
    private UnityAction _filterConsumableAction;
    private UnityAction _filterArtifactAction;
    private UnityAction _filterEtcAction;

    private void Awake()
    {
        if (_exitButton != null)
        {
            _exitButton.onClick.AddListener(OnExitClicked);
        }

        if (_filterAllAction == null)
        {
            _filterAllAction = () => SetFilter(ItemMainCategory.None);
        }

        if (_filterEquipAction == null)
        {
            _filterEquipAction = () => SetFilter(ParseMainCategoryOrNone("Equipment"));
        }

        if (_filterMaterialAction == null)
        {
            _filterMaterialAction = () => SetFilter(ParseMainCategoryOrNone("Material"));
        }

        if (_filterCosmeticAction == null)
        {
            _filterCosmeticAction = () => SetFilter(ParseMainCategoryOrNone("Cosmetic"));
        }

        if (_filterConsumableAction == null)
        {
            _filterConsumableAction = () => SetFilter(ParseMainCategoryOrNone("Consumable"));
        }

        if (_filterArtifactAction == null)
        {
            _filterArtifactAction = () => SetFilter(ParseMainCategoryOrNone("Artifact"));
        }

        if (_filterEtcAction == null)
        {
            _filterEtcAction = () => SetFilter(ParseMainCategoryOrNone("Etc"));
        }

        if (_filterAllBtn != null)
        {
            _filterAllBtn.onClick.AddListener(_filterAllAction);
        }

        if (_filterEquipBtn != null)
        {
            _filterEquipBtn.onClick.AddListener(_filterEquipAction);
        }

        if (_filterMaterialBtn != null)
        {
            _filterMaterialBtn.onClick.AddListener(_filterMaterialAction);
        }

        if (_filterCosmeticBtn != null)
        {
            _filterCosmeticBtn.onClick.AddListener(_filterCosmeticAction);
        }

        if (_filterConsumableBtn != null)
        {
            _filterConsumableBtn.onClick.AddListener(_filterConsumableAction);
        }

        if (_filterArtifactBtn != null)
        {
            _filterArtifactBtn.onClick.AddListener(_filterArtifactAction);
        }

        if (_filterEtcBtn != null)
        {
            _filterEtcBtn.onClick.AddListener(_filterEtcAction);
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        RefreshInventoryUI();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        if (_exitButton != null)
        {
            _exitButton.onClick.RemoveListener(OnExitClicked);
        }

        if (_filterAllBtn != null && _filterAllAction != null)
        {
            _filterAllBtn.onClick.RemoveListener(_filterAllAction);
        }

        if (_filterEquipBtn != null && _filterEquipAction != null)
        {
            _filterEquipBtn.onClick.RemoveListener(_filterEquipAction);
        }

        if (_filterMaterialBtn != null && _filterMaterialAction != null)
        {
            _filterMaterialBtn.onClick.RemoveListener(_filterMaterialAction);
        }

        if (_filterCosmeticBtn != null && _filterCosmeticAction != null)
        {
            _filterCosmeticBtn.onClick.RemoveListener(_filterCosmeticAction);
        }

        if (_filterConsumableBtn != null && _filterConsumableAction != null)
        {
            _filterConsumableBtn.onClick.RemoveListener(_filterConsumableAction);
        }

        if (_filterArtifactBtn != null && _filterArtifactAction != null)
        {
            _filterArtifactBtn.onClick.RemoveListener(_filterArtifactAction);
        }

        if (_filterEtcBtn != null && _filterEtcAction != null)
        {
            _filterEtcBtn.onClick.RemoveListener(_filterEtcAction);
        }
    }

    /// <summary>
    /// Rebuilds the inventory item list UI from the latest stored quantities.
    /// </summary>
    private void RefreshInventoryUI()
    {
        if (_itemContainer == null
            || _itemSlotPrefab == null
            || InventoryManager.Instance == null
            || ItemDatabase.Instance == null)
        {
            return;
        }

        for (int i = _itemContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(_itemContainer.GetChild(i).gameObject);
        }

        List<InventoryDisplayEntry> validEntries = new List<InventoryDisplayEntry>();

        foreach (ItemInstance instance in InventoryManager.Instance.GetAllInstances())
        {
            if (instance == null || instance.Count < 1)
            {
                continue;
            }

            ItemData data = ItemDatabase.Instance.GetItem(instance.ItemId);

            if (data == null)
            {
                continue;
            }

            validEntries.Add(new InventoryDisplayEntry
            {
                Instance = instance,
                Data = data
            });
        }

        IEnumerable<InventoryDisplayEntry> filteredEntries = validEntries;

        if (_currentMainFilter != ItemMainCategory.None)
        {
            filteredEntries = filteredEntries.Where(entry => entry.Data.MainCategory == _currentMainFilter);
        }

        if (_currentMiddleFilter != ItemMiddleCategory.None)
        {
            filteredEntries = filteredEntries.Where(entry => entry.Data.MiddleCategory == _currentMiddleFilter);
        }

        List<InventoryDisplayEntry> sortedInstances = filteredEntries
            .OrderBy(entry => entry.Data.MainCategory)
            .ThenByDescending(entry => entry.Data.Tier)
            .ThenByDescending(entry => entry.Instance.EnhanceLevel)
            .ThenBy(entry => entry.Data.ItemName)
            .ToList();

        foreach (InventoryDisplayEntry entry in sortedInstances)
        {
            ItemInstance instance = entry.Instance;
            ItemData data = entry.Data;
            string itemId = instance.ItemId;
            string instanceId = instance.InstanceId;
            int count = instance.Count;
            int enhanceLevel = instance.EnhanceLevel;

            GameObject itemSlotObject = Instantiate(_itemSlotPrefab, _itemContainer);
            TextMeshProUGUI[] texts = itemSlotObject.GetComponentsInChildren<TextMeshProUGUI>(true);
            Image[] images = itemSlotObject.GetComponentsInChildren<Image>(true);
            Button sellButton = itemSlotObject.GetComponentInChildren<Button>(true);

            TextMeshProUGUI itemText = texts.Length > 0 ? texts[0] : null;
            TextMeshProUGUI buttonText = texts.Length > 1 ? texts[1] : null;
            Image itemIconImage = null;

            foreach (Image image in images)
            {
                if (sellButton != null && image == sellButton.targetGraphic)
                {
                    continue;
                }

                itemIconImage = image;
                break;
            }

            if (itemText != null)
            {
                itemText.text = $"[{GetTierName(data.Tier)}] {data.ItemName} {(enhanceLevel > 0 ? $"+{enhanceLevel} " : string.Empty)}x{count}";
            }

            if (itemIconImage != null && data.ItemIcon != null)
            {
                itemIconImage.sprite = data.ItemIcon;
            }

            bool isEquipped = EquipmentManager.Instance != null && EquipmentManager.Instance.IsEquipped(instanceId);

            if (buttonText != null)
            {
                if (isEquipped)
                {
                    buttonText.text = "[ 장착 중 ]";
                }
                else
                {
                    buttonText.text = data.SalePrice > 0
                        ? $"{data.SalePrice} 토큰에 판매"
                        : "판매 불가";
                }
            }

            if (sellButton != null)
            {
                if (isEquipped)
                {
                    sellButton.interactable = false;
                }
                else
                {
                    sellButton.interactable = data.SalePrice > 0;
                }

                sellButton.onClick.RemoveAllListeners();
                sellButton.onClick.AddListener(() =>
                {
                    if (data.SalePrice <= 0 || EconomyManager.Instance == null)
                    {
                        return;
                    }

                    if (InventoryManager.Instance.RemoveItemByInstance(instanceId, 1))
                    {
                        EconomyManager.Instance.AddTokens(data.SalePrice);
                        RefreshInventoryUI();
                    }
                });
            }
        }
    }

    private void SetFilter(ItemMainCategory filter)
    {
        _currentMainFilter = filter;
        _currentMiddleFilter = ItemMiddleCategory.None;
        RefreshInventoryUI();
    }

    private void SetMiddleFilter(ItemMiddleCategory filter)
    {
        _currentMiddleFilter = filter;
        RefreshInventoryUI();
    }

    private ItemMainCategory ParseMainCategoryOrNone(string categoryName)
    {
        return Enum.TryParse(categoryName, true, out ItemMainCategory parsedCategory)
            ? parsedCategory
            : ItemMainCategory.None;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.Inventory)
        {
            RefreshInventoryUI();
        }
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

    /// <summary>
    /// Returns the player to the main menu state from the inventory panel.
    /// </summary>
    private void OnExitClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetGameState(GameState.MainMenu);
    }
}
