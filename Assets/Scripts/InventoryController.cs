using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 패널 UI 및 아이템 판매 상호작용을 제어합니다.
/// 도감과 동일하게 대/중/소분류 드롭다운 및 1000개 단위 페이지네이션을 지원합니다.
/// </summary>
public sealed class InventoryController : MonoBehaviour
{
    private sealed class InventoryDisplayEntry
    {
        public ItemInstance Instance;
        public ItemData Data;
        public ItemMetadata? Meta;
    }

    [Header("Scroll View")]
    [SerializeField] private Transform _itemContainer;
    [SerializeField] private GameObject _itemSlotPrefab;
    [SerializeField] private VirtualizedInventoryScrollView _virtualizedScrollView;

    [Header("Category Selection")]
    [SerializeField] private SimpleCategoryDropdown _mainCategoryDropdown;
    [SerializeField] private SimpleCategoryDropdown _middleCategoryDropdown;
    [SerializeField] private SimpleCategoryDropdown _subCategoryDropdown;

    [Header("Navigation")]
    [SerializeField] private Button _exitButton;

    [Header("Pagination")]
    [SerializeField] private Button _prevPageButton;
    [SerializeField] private Button _nextPageButton;
    [SerializeField] private TextMeshProUGUI _pageText;
    [SerializeField] private int _pageSize = 1000;

    private ItemMainCategory _currentMain = ItemMainCategory.None;
    private ItemMiddleCategory _currentMiddle = ItemMiddleCategory.None;
    private ItemSubCategory _currentSub = ItemSubCategory.None;
    private int _currentPage;

    private readonly List<InventoryDisplayEntry> _filteredEntries = new List<InventoryDisplayEntry>();
    private readonly List<InventoryDisplayEntry> _pageEntries = new List<InventoryDisplayEntry>();

    private void Awake()
    {
        if (_virtualizedScrollView != null)
        {
            _virtualizedScrollView.OnSellRequested += HandleSellRequested;
        }

        if (_exitButton != null)
        {
            _exitButton.onClick.AddListener(OnExitClicked);
        }

        if (_mainCategoryDropdown != null)
            _mainCategoryDropdown.onValueChanged.AddListener(OnMainCategoryChanged);
        if (_middleCategoryDropdown != null)
            _middleCategoryDropdown.onValueChanged.AddListener(OnMiddleCategoryChanged);
        if (_subCategoryDropdown != null)
            _subCategoryDropdown.onValueChanged.AddListener(OnSubCategoryChanged);

        if (_prevPageButton != null)
        {
            _prevPageButton.onClick.AddListener(GoToPrevPage);
        }

        if (_nextPageButton != null)
        {
            _nextPageButton.onClick.AddListener(GoToNextPage);
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        RefreshCategoryOptions();
        ApplyFiltersAndRefresh();
    }

    private void OnEnable()
    {
        RefreshCategoryOptions();
        ApplyFiltersAndRefresh();
    }

    private void OnDestroy()
    {
        if (_virtualizedScrollView != null)
        {
            _virtualizedScrollView.OnSellRequested -= HandleSellRequested;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        if (_exitButton != null) _exitButton.onClick.RemoveListener(OnExitClicked);
        if (_mainCategoryDropdown != null) _mainCategoryDropdown.onValueChanged?.RemoveListener(OnMainCategoryChanged);
        if (_middleCategoryDropdown != null) _middleCategoryDropdown.onValueChanged?.RemoveListener(OnMiddleCategoryChanged);
        if (_subCategoryDropdown != null) _subCategoryDropdown.onValueChanged?.RemoveListener(OnSubCategoryChanged);
        if (_prevPageButton != null) _prevPageButton.onClick.RemoveListener(GoToPrevPage);
        if (_nextPageButton != null) _nextPageButton.onClick.RemoveListener(GoToNextPage);
    }

    private void RefreshCategoryOptions()
    {
        RefreshMainCategoryOptions();
        RefreshMiddleCategoryOptions();
        RefreshSubCategoryOptions();
    }

    private void RefreshMainCategoryOptions()
    {
        if (_mainCategoryDropdown == null) return;
        _mainCategoryDropdown.ClearOptions();
        var options = new List<string> { "전체" };
        options.AddRange(System.Enum.GetNames(typeof(ItemMainCategory)));
        _mainCategoryDropdown.AddOptions(options);
        _mainCategoryDropdown.SetValueWithoutNotify(_currentMain == ItemMainCategory.None ? 0 : (int)_currentMain + 1);
    }

    private void RefreshMiddleCategoryOptions()
    {
        if (_middleCategoryDropdown == null) return;
        var validMiddles = GetValidMiddleCategories(_currentMain);
        if (!validMiddles.Contains(_currentMiddle)) _currentMiddle = ItemMiddleCategory.None;
        _middleCategoryDropdown.ClearOptions();
        var options = validMiddles.Select(m => m == ItemMiddleCategory.None ? "전체" : m.ToString()).ToList();
        _middleCategoryDropdown.AddOptions(options);
        int idx = validMiddles.IndexOf(_currentMiddle);
        _middleCategoryDropdown.SetValueWithoutNotify(idx >= 0 ? idx : 0);
    }

    private void RefreshSubCategoryOptions()
    {
        if (_subCategoryDropdown == null) return;
        var validSubs = GetValidSubCategories(_currentMiddle);
        if (!validSubs.Contains(_currentSub)) _currentSub = ItemSubCategory.None;
        _subCategoryDropdown.ClearOptions();
        var options = validSubs.Select(s => s == ItemSubCategory.None ? "전체" : s.ToString()).ToList();
        _subCategoryDropdown.AddOptions(options);
        int idx = validSubs.IndexOf(_currentSub);
        _subCategoryDropdown.SetValueWithoutNotify(idx >= 0 ? idx : 0);
    }

    private static List<ItemMiddleCategory> GetValidMiddleCategories(ItemMainCategory main)
    {
        if (CategoryDefinitionMaps.MainToMiddleMap.TryGetValue(main, out var arr))
            return new List<ItemMiddleCategory>(arr);
        return new List<ItemMiddleCategory> { ItemMiddleCategory.None };
    }

    private static List<ItemSubCategory> GetValidSubCategories(ItemMiddleCategory middle)
    {
        if (CategoryDefinitionMaps.MiddleToSubMap.TryGetValue(middle, out var arr))
            return new List<ItemSubCategory>(arr);
        return new List<ItemSubCategory> { ItemSubCategory.None };
    }

    private void OnMainCategoryChanged(int index)
    {
        _currentMain = index <= 0 ? ItemMainCategory.None : (ItemMainCategory)(index - 1);
        _currentMiddle = ItemMiddleCategory.None;
        _currentSub = ItemSubCategory.None;
        RefreshMiddleCategoryOptions();
        RefreshSubCategoryOptions();
        ApplyFiltersAndRefresh();
    }

    private void OnMiddleCategoryChanged(int index)
    {
        var validMiddles = GetValidMiddleCategories(_currentMain);
        _currentMiddle = index >= 0 && index < validMiddles.Count ? validMiddles[index] : ItemMiddleCategory.None;
        _currentSub = ItemSubCategory.None;
        RefreshSubCategoryOptions();
        ApplyFiltersAndRefresh();
    }

    private void OnSubCategoryChanged(int index)
    {
        var validSubs = GetValidSubCategories(_currentMiddle);
        _currentSub = index >= 0 && index < validSubs.Count ? validSubs[index] : ItemSubCategory.None;
        ApplyFiltersAndRefresh();
    }

    public void GoToPrevPage()
    {
        if (_currentPage <= 0) return;
        _currentPage--;
        ApplyPageSlice();
        RefreshPagination();
    }

    public void GoToNextPage()
    {
        int totalPages = GetTotalPageCount();
        if (_currentPage >= totalPages - 1) return;
        _currentPage++;
        ApplyPageSlice();
        RefreshPagination();
    }

    private int GetTotalPageCount()
    {
        int total = _filteredEntries.Count;
        if (total <= 0) return 0;
        return (total + _pageSize - 1) / _pageSize;
    }

    private void RefreshPagination()
    {
        int totalPages = GetTotalPageCount();
        int current = _currentPage + 1;
        if (_prevPageButton != null) _prevPageButton.interactable = _currentPage > 0;
        if (_nextPageButton != null) _nextPageButton.interactable = totalPages > 1 && _currentPage < totalPages - 1;
        if (_pageText != null) _pageText.text = totalPages <= 0 ? "1 / 1" : $"{current} / {totalPages}";
    }

    /// <summary>
    /// 카테고리 필터를 적용하고, 페이지네이션 후 UI를 갱신합니다.
    /// </summary>
    private void ApplyFiltersAndRefresh()
    {
        if (InventoryManager.Instance == null || ItemDatabase.Instance == null)
        {
            return;
        }

        List<InventoryDisplayEntry> validEntries = new List<InventoryDisplayEntry>();
        foreach (ItemInstance instance in InventoryManager.Instance.GetAllInstances())
        {
            if (instance == null || instance.Count < 1) continue;
            ItemData data = ItemDatabase.Instance.GetItem(instance.ItemId);
            ItemMetadata? meta = ItemDatabase.Instance?.GetMetadata(instance.ItemId);
            bool isKnownCurrency = instance.ItemId == EconomyManager.GoldItemId || instance.ItemId == EconomyManager.TicketItemId;
            if (data == null && !meta.HasValue && !isKnownCurrency) continue;
            validEntries.Add(new InventoryDisplayEntry { Instance = instance, Data = data, Meta = meta });
        }

        IEnumerable<InventoryDisplayEntry> filtered = validEntries;
        if (_currentMain != ItemMainCategory.None)
            filtered = filtered.Where(e => GetMainCategory(e) == _currentMain);
        if (_currentMiddle != ItemMiddleCategory.None)
            filtered = filtered.Where(e => GetMiddleCategory(e) == _currentMiddle);
        if (_currentSub != ItemSubCategory.None)
            filtered = filtered.Where(e => GetSubCategory(e) == _currentSub);

        _filteredEntries.Clear();
        _filteredEntries.AddRange(filtered
            .OrderBy(e => GetMainCategory(e))
            .ThenByDescending(e => GetTier(e))
            .ThenByDescending(e => e.Instance.EnhanceLevel)
            .ThenBy(e => GetItemName(e)));

        _currentPage = 0;
        ApplyPageSlice();
        RefreshPagination();
    }

    private void ApplyPageSlice()
    {
        _pageEntries.Clear();
        int total = _filteredEntries.Count;
        int pageCount = GetTotalPageCount();
        if (pageCount <= 0)
        {
            ApplyEntriesToUI(_pageEntries);
            return;
        }
        int start = _currentPage * _pageSize;
        int end = Mathf.Min(start + _pageSize, total);
        for (int i = start; i < end; i++)
        {
            _pageEntries.Add(_filteredEntries[i]);
        }
        ApplyEntriesToUI(_pageEntries);
    }

    private void ApplyEntriesToUI(List<InventoryDisplayEntry> entries)
    {
        if (_virtualizedScrollView != null)
        {
            var slotDataList = entries.Select(ConvertToSlotData).ToList();
            _virtualizedScrollView.SetEntries(slotDataList);
        }
        else if (_itemContainer != null && _itemSlotPrefab != null)
        {
            for (int i = _itemContainer.childCount - 1; i >= 0; i--)
                Destroy(_itemContainer.GetChild(i).gameObject);
            foreach (var entry in entries)
                CreateLegacySlot(entry);
        }
    }

    private void HandleSellRequested(string instanceId, int price)
    {
        if (price <= 0 || EconomyManager.Instance == null || InventoryManager.Instance == null) return;
        if (InventoryManager.Instance.RemoveItemByInstance(instanceId, 1))
        {
            EconomyManager.Instance.AddTokens(price);
            ApplyFiltersAndRefresh();
        }
    }

    private VirtualizedInventoryScrollView.InventorySlotData ConvertToSlotData(InventoryDisplayEntry entry)
    {
        var instance = entry.Instance;
        var data = entry.Data;
        var meta = entry.Meta;
        return new VirtualizedInventoryScrollView.InventorySlotData
        {
            InstanceId = instance.InstanceId,
            ItemId = instance.ItemId,
            DisplayName = GetItemName(entry),
            Tier = GetTier(entry),
            Count = instance.Count,
            EnhanceLevel = instance.EnhanceLevel,
            SalePrice = data?.SalePrice ?? meta?.SalePrice ?? 0,
            Icon = data?.ItemIcon,
            IsEquipped = EquipmentManager.Instance != null && EquipmentManager.Instance.IsEquipped(instance.InstanceId),
            IsCurrency = instance.ItemId == EconomyManager.GoldItemId || instance.ItemId == EconomyManager.TicketItemId
        };
    }

    private void CreateLegacySlot(InventoryDisplayEntry entry)
    {
        ItemInstance instance = entry.Instance;
        ItemData data = entry.Data;
        ItemMetadata? meta = entry.Meta;
        string itemId = instance.ItemId;
        string itemName = GetItemName(entry);
        ItemTier tier = GetTier(entry);
        int salePrice = data?.SalePrice ?? meta?.SalePrice ?? 0;
        Sprite icon = data?.ItemIcon;
        string instanceId = instance.InstanceId;
        int count = instance.Count;
        int enhanceLevel = instance.EnhanceLevel;

        GameObject itemSlotObject = Instantiate(_itemSlotPrefab, _itemContainer);
        TextMeshProUGUI[] texts = itemSlotObject.GetComponentsInChildren<TextMeshProUGUI>(true);
        Image[] images = itemSlotObject.GetComponentsInChildren<Image>(true);
        Button sellButton = itemSlotObject.GetComponentInChildren<Button>(true);

        TextMeshProUGUI itemText = texts != null && texts.Length > 0 ? texts[0] : null;
        TextMeshProUGUI buttonText = texts != null && texts.Length > 1 ? texts[1] : null;
        Image itemIconImage = null;
        if (images != null)
        {
            foreach (Image image in images)
            {
                if (sellButton != null && image == sellButton.targetGraphic) continue;
                itemIconImage = image;
                break;
            }
        }

        if (itemText != null)
        {
            itemText.text = $"[{GetTierName(tier)}] {itemName} {(enhanceLevel > 0 ? $"+{enhanceLevel} " : string.Empty)}x{count}";
        }
        if (itemIconImage != null) itemIconImage.sprite = icon;

        bool isEquipped = EquipmentManager.Instance != null && EquipmentManager.Instance.IsEquipped(instanceId);
        bool isCurrency = instance.ItemId == EconomyManager.GoldItemId || instance.ItemId == EconomyManager.TicketItemId;

        if (buttonText != null)
        {
            if (isEquipped) buttonText.text = "[ 장착 중 ]";
            else if (isCurrency) buttonText.text = "[ 통화 ]";
            else buttonText.text = salePrice > 0 ? $"{salePrice} 기초 골드에 판매" : "판매 불가";
        }

        if (sellButton != null)
        {
            sellButton.interactable = !isEquipped && !isCurrency && salePrice > 0;
            if (!isCurrency && salePrice > 0)
            {
                int price = salePrice;
                sellButton.onClick.AddListener(() =>
                {
                    if (price <= 0 || EconomyManager.Instance == null) return;
                    if (InventoryManager.Instance != null && InventoryManager.Instance.RemoveItemByInstance(instanceId, 1))
                    {
                        EconomyManager.Instance.AddTokens(price);
                        ApplyFiltersAndRefresh();
                    }
                });
            }
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.Inventory)
            ApplyFiltersAndRefresh();
    }

    private static ItemMainCategory GetMainCategory(InventoryDisplayEntry entry)
    {
        if (entry.Data != null) return entry.Data.MainCategory;
        return entry.Meta.HasValue ? entry.Meta.Value.MainCategory : ItemMainCategory.None;
    }

    private static ItemMiddleCategory GetMiddleCategory(InventoryDisplayEntry entry)
    {
        if (entry.Data != null) return entry.Data.MiddleCategory;
        return entry.Meta.HasValue ? entry.Meta.Value.MiddleCategory : ItemMiddleCategory.None;
    }

    private static ItemSubCategory GetSubCategory(InventoryDisplayEntry entry)
    {
        if (entry.Data != null) return entry.Data.SubCategory;
        return entry.Meta.HasValue ? entry.Meta.Value.SubCategory : ItemSubCategory.None;
    }

    private static ItemTier GetTier(InventoryDisplayEntry entry)
    {
        if (entry.Data != null) return entry.Data.Tier;
        return entry.Meta.HasValue ? entry.Meta.Value.Tier : ItemTier.Tier1;
    }

    private static string GetItemName(InventoryDisplayEntry entry)
    {
        if (entry.Data != null && !string.IsNullOrEmpty(entry.Data.ItemName)) return entry.Data.ItemName;
        if (entry.Meta.HasValue && !string.IsNullOrEmpty(entry.Meta.Value.ItemName)) return entry.Meta.Value.ItemName;
        if (entry.Instance.ItemId == EconomyManager.GoldItemId) return "기초 골드";
        if (entry.Instance.ItemId == EconomyManager.TicketItemId) return "기초 티켓";
        return entry.Instance.ItemId;
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
