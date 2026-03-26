using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 패널을 제어합니다. 도감처럼 대/중/소분류·티어 필터로 PurchasePrice&gt;0인 모든 아이템을 표시합니다.
/// </summary>
public sealed class ShopController : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private ShopConfig _shopConfig;

    [Header("고정 상품 (응시권)")]
    [SerializeField] private GameObject _ticketSlotRoot;
    [SerializeField] private Button _ticketBuyButton;
    [SerializeField] private TextMeshProUGUI _ticketPriceText;

    [Header("카테고리 (도감과 동일 - TMP_Dropdown)")]
    [SerializeField] private TMP_Dropdown _mainCategoryDropdown;
    [SerializeField] private TMP_Dropdown _middleCategoryDropdown;
    [SerializeField] private TMP_Dropdown _subCategoryDropdown;
    [SerializeField] private TMP_Dropdown _tierDropdown;

    [Header("상품 목록")]
    [SerializeField] private VirtualizedShopScrollView _virtualizedScrollView;

    [Header("페이지네이션")]
    [SerializeField] private Button _prevPageButton;
    [SerializeField] private Button _nextPageButton;
    [SerializeField] private TextMeshProUGUI _pageText;
    [SerializeField] private int _pageSize = 1000;

    [Header("공통")]
    [SerializeField] private TextMeshProUGUI _shopTokenText;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private Button _exitButton;

    private ItemMainCategory _currentMain = ItemMainCategory.None;
    private ItemMiddleCategory _currentMiddle = ItemMiddleCategory.None;
    private ItemSubCategory _currentSub = ItemSubCategory.None;
    private ItemTier? _currentTier;
    private int _currentPage;
    private readonly List<string> _filteredIds = new List<string>();
    private readonly List<string> _pageIds = new List<string>();
    private readonly List<VirtualizedShopScrollView.ShopSlotData> _slotDataCache = new List<VirtualizedShopScrollView.ShopSlotData>();

    private void Awake()
    {
        if (_virtualizedScrollView != null)
            _virtualizedScrollView.OnPurchaseRequested += HandlePurchaseRequested;
        if (_ticketBuyButton != null)
            _ticketBuyButton.onClick.AddListener(OnTicketBuyClicked);
        if (_exitButton != null)
            _exitButton.onClick.AddListener(OnExitClicked);
        if (_mainCategoryDropdown != null)
            _mainCategoryDropdown.onValueChanged.AddListener(OnMainCategoryChanged);
        if (_middleCategoryDropdown != null)
            _middleCategoryDropdown.onValueChanged.AddListener(OnMiddleCategoryChanged);
        if (_subCategoryDropdown != null)
            _subCategoryDropdown.onValueChanged.AddListener(OnSubCategoryChanged);
        if (_tierDropdown != null)
            _tierDropdown.onValueChanged.AddListener(OnTierChanged);
        if (_prevPageButton != null)
            _prevPageButton.onClick.AddListener(GoToPrevPage);
        if (_nextPageButton != null)
            _nextPageButton.onClick.AddListener(GoToNextPage);
    }

    private void Start()
    {
        UpdateShopTokenText();
        RefreshCategoryOptions();
        RefreshTicketSlot();
        ApplyFiltersAndRefresh();
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnEconomyChanged += HandleEconomyChanged;
    }

    private void OnEnable()
    {
        if (EncyclopediaManager.Instance != null)
            EncyclopediaManager.Instance.RebuildIndexIfEmpty();
        RefreshCategoryOptions();
        RefreshTicketSlot();
        ApplyFiltersAndRefresh();
    }

    private void OnDestroy()
    {
        if (_virtualizedScrollView != null)
            _virtualizedScrollView.OnPurchaseRequested -= HandlePurchaseRequested;
        if (_ticketBuyButton != null)
            _ticketBuyButton.onClick.RemoveListener(OnTicketBuyClicked);
        if (_exitButton != null)
            _exitButton.onClick.RemoveListener(OnExitClicked);
        if (_mainCategoryDropdown != null)
            _mainCategoryDropdown.onValueChanged.RemoveListener(OnMainCategoryChanged);
        if (_middleCategoryDropdown != null)
            _middleCategoryDropdown.onValueChanged.RemoveListener(OnMiddleCategoryChanged);
        if (_subCategoryDropdown != null)
            _subCategoryDropdown.onValueChanged.RemoveListener(OnSubCategoryChanged);
        if (_tierDropdown != null)
            _tierDropdown.onValueChanged.RemoveListener(OnTierChanged);
        if (_prevPageButton != null)
            _prevPageButton.onClick.RemoveListener(GoToPrevPage);
        if (_nextPageButton != null)
            _nextPageButton.onClick.RemoveListener(GoToNextPage);
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnEconomyChanged -= HandleEconomyChanged;
    }

    private void RefreshCategoryOptions()
    {
        RefreshMainCategoryOptions();
        RefreshMiddleCategoryOptions();
        RefreshSubCategoryOptions();
        RefreshTierCategoryOptions();
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

    private void RefreshTierCategoryOptions()
    {
        if (_tierDropdown == null) return;
        _tierDropdown.ClearOptions();
        var options = new List<string> { "전체" };
        options.AddRange(System.Enum.GetNames(typeof(ItemTier)));
        _tierDropdown.AddOptions(options);
        _tierDropdown.SetValueWithoutNotify(_currentTier.HasValue ? (int)_currentTier.Value + 1 : 0);
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

    private void OnTierChanged(int index)
    {
        _currentTier = index <= 0 ? (ItemTier?)null : (ItemTier?)System.Math.Min(index - 1, (int)ItemTier.Tier10);
        ApplyFiltersAndRefresh();
    }

    private void ApplyFiltersAndRefresh()
    {
        var manager = EncyclopediaManager.Instance;
        var db = ItemDatabase.Instance;
        if (manager == null || db == null)
        {
            if (_virtualizedScrollView != null)
                _virtualizedScrollView.SetEntries(new List<VirtualizedShopScrollView.ShopSlotData>());
            RefreshPagination();
            return;
        }

        var path = new EncyclopediaCategoryPath
        {
            Main = _currentMain,
            Middle = _currentMiddle != ItemMiddleCategory.None ? _currentMiddle : (ItemMiddleCategory?)null,
            Sub = _currentSub != ItemSubCategory.None ? _currentSub : (ItemSubCategory?)null,
            Tier = _currentTier
        };

        IReadOnlyList<string> baseIds = manager.GetItemIdsForPath(path);

        _filteredIds.Clear();
        if (baseIds != null)
        {
            foreach (string id in baseIds)
            {
                int price = GetItemPurchasePrice(id);
                if (price > 0)
                    _filteredIds.Add(id);
            }
        }

        _currentPage = 0;
        ApplyPageSlice();
        RefreshPagination();
    }

    private void ApplyPageSlice()
    {
        _pageIds.Clear();
        int total = _filteredIds.Count;
        int pageCount = GetTotalPageCount();
        if (pageCount <= 0)
        {
            ApplyEntriesToScrollView(new List<VirtualizedShopScrollView.ShopSlotData>());
            return;
        }
        int start = _currentPage * _pageSize;
        int end = Mathf.Min(start + _pageSize, total);
        for (int i = start; i < end; i++)
            _pageIds.Add(_filteredIds[i]);
        BuildSlotDataAndApply();
    }

    private void BuildSlotDataAndApply()
    {
        _slotDataCache.Clear();
        var db = ItemDatabase.Instance;
        if (db == null || _pageIds == null)
        {
            ApplyEntriesToScrollView(_slotDataCache);
            return;
        }

        int currentGold = EconomyManager.Instance != null ? EconomyManager.Instance.Tokens : 0;

        foreach (string itemId in _pageIds)
        {
            var meta = db.GetMetadata(itemId);
            if (!meta.HasValue) continue;

            int price = GetItemPurchasePrice(itemId);
            if (price <= 0) continue;

            _slotDataCache.Add(new VirtualizedShopScrollView.ShopSlotData
            {
                ItemId = itemId,
                DisplayName = meta.Value.ItemName ?? itemId,
                Tier = meta.Value.Tier,
                Price = price,
                Icon = null,
                CanPurchase = currentGold >= price
            });
        }

        ApplyEntriesToScrollView(_slotDataCache);

        for (int i = 0; i < _slotDataCache.Count; i++)
        {
            string itemId = _slotDataCache[i].ItemId;
            db.LoadItemDataAsync(itemId, data =>
            {
                if (data == null || data.ItemIcon == null) return;
                for (int j = 0; j < _slotDataCache.Count; j++)
                {
                    if (string.Equals(_slotDataCache[j].ItemId, itemId, System.StringComparison.OrdinalIgnoreCase))
                    {
                        var sd = _slotDataCache[j];
                        sd.Icon = data.ItemIcon;
                        _slotDataCache[j] = sd;
                        _virtualizedScrollView?.SetEntries(_slotDataCache);
                        break;
                    }
                }
            });
        }
    }

    private void ApplyEntriesToScrollView(List<VirtualizedShopScrollView.ShopSlotData> entries)
    {
        if (_virtualizedScrollView != null)
            _virtualizedScrollView.SetEntries(entries);
    }

    private int GetTotalPageCount()
    {
        int total = _filteredIds.Count;
        if (total <= 0) return 0;
        return (total + _pageSize - 1) / _pageSize;
    }

    private void RefreshPagination()
    {
        int totalPages = GetTotalPageCount();
        int current = _currentPage + 1;
        if (_prevPageButton != null)
            _prevPageButton.interactable = _currentPage > 0;
        if (_nextPageButton != null)
            _nextPageButton.interactable = totalPages > 1 && _currentPage < totalPages - 1;
        if (_pageText != null)
            _pageText.text = totalPages <= 0 ? "1 / 1" : $"{current} / {totalPages}";
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
        if (_currentPage >= GetTotalPageCount() - 1) return;
        _currentPage++;
        ApplyPageSlice();
        RefreshPagination();
    }

    private void RefreshTicketSlot()
    {
        if (_shopConfig == null) { SetTicketSlotActive(false); return; }
        string ticketId = string.IsNullOrWhiteSpace(_shopConfig.TicketItemId) ? EconomyManager.TicketItemId : _shopConfig.TicketItemId;
        int price = GetItemPurchasePrice(ticketId);
        if (price <= 0) { SetTicketSlotActive(false); return; }

        SetTicketSlotActive(true);
        if (_ticketPriceText != null)
            _ticketPriceText.text = $"응시권 x1 — {price} 기초 골드";
        if (_ticketBuyButton != null)
            _ticketBuyButton.interactable = (EconomyManager.Instance?.Tokens ?? 0) >= price;
    }

    private static void SetTicketSlotActive(GameObject root, bool active)
    {
        if (root != null) root.SetActive(active);
    }

    private void SetTicketSlotActive(bool active)
    {
        SetTicketSlotActive(_ticketSlotRoot, active);
    }

    private static int GetItemPurchasePrice(string itemId)
    {
        var db = ItemDatabase.Instance;
        if (db == null || string.IsNullOrWhiteSpace(itemId)) return 0;
        var meta = db.GetMetadata(itemId);
        if (meta.HasValue && meta.Value.PurchasePrice > 0) return meta.Value.PurchasePrice;
        var data = db.GetItem(itemId);
        return data != null ? data.PurchasePrice : 0;
    }

    private void HandlePurchaseRequested(string itemId, int price)
    {
        if (EconomyManager.Instance == null || InventoryManager.Instance == null)
        {
            ShowResult("구매할 수 없습니다.");
            return;
        }
        if (!EconomyManager.Instance.SpendTokens(price))
        {
            ShowResult("기초 골드가 부족합니다.");
            return;
        }
        InventoryManager.Instance.AddItem(itemId, 1);
        var meta = ItemDatabase.Instance?.GetMetadata(itemId);
        ShowResult($"구매 완료: {(meta.HasValue ? meta.Value.ItemName : itemId)}");
        RefreshTicketSlot();
        ApplyFiltersAndRefresh();
    }

    private void OnTicketBuyClicked()
    {
        if (_shopConfig == null) return;
        string ticketId = string.IsNullOrWhiteSpace(_shopConfig.TicketItemId) ? EconomyManager.TicketItemId : _shopConfig.TicketItemId;
        int price = GetItemPurchasePrice(ticketId);
        if (price <= 0) return;
        HandlePurchaseRequested(ticketId, price);
    }

    private void ShowResult(string message)
    {
        if (_resultText != null) _resultText.text = message;
    }

    private void HandleEconomyChanged()
    {
        UpdateShopTokenText();
        RefreshTicketSlot();
        ApplyFiltersAndRefresh();
    }

    private void UpdateShopTokenText()
    {
        if (_shopTokenText == null) return;
        _shopTokenText.text = $"기초 골드: {EconomyManager.Instance?.Tokens ?? 0}";
    }

    private void OnExitClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetGameState(GameState.MainMenu);
    }
}
