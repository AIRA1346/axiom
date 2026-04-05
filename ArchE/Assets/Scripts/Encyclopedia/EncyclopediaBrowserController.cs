using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도감 UI 브라우저. VirtualizedEncyclopediaScrollView, 검색, 필터, 대시보드를 통합합니다.
/// </summary>
public sealed class EncyclopediaBrowserController : MonoBehaviour
{
    public enum UnlockFilterMode
    {
        All,
        Unlocked,
        Locked
    }

    [Header("Scroll View")]
    [SerializeField] private VirtualizedEncyclopediaScrollView _scrollView;

    [Header("Category Selection")]
    [SerializeField] private TMP_Dropdown _mainCategoryDropdown;
    [SerializeField] private TMP_Dropdown _middleCategoryDropdown;
    [SerializeField] private TMP_Dropdown _subCategoryDropdown;
    [SerializeField] private TMP_Dropdown _tierDropdown;

    [Header("Search")]
    [SerializeField] private TMP_InputField _searchInputField;
    [SerializeField] private float _searchDebounceSeconds = 0.25f;
    private Coroutine _searchDebounceCoroutine;

    [Header("Unlock Filter")]
    [SerializeField] private Toggle _filterAllToggle;
    [SerializeField] private Toggle _filterUnlockedToggle;
    [SerializeField] private Toggle _filterLockedToggle;

    [Header("Tier Only Filter")]
    [SerializeField] private Toggle _tierOnlyFilterToggle;
    [SerializeField] private TMP_Dropdown _tierOnlyDropdown;

    [Header("Progress Display")]
    [SerializeField] private TextMeshProUGUI _progressText;
    [SerializeField] private string _progressFormat = "{0}/{1}";

    [Header("Dashboard (Overall Progress)")]
    [SerializeField] private RectTransform _dashboardPanel;
    [SerializeField] private TextMeshProUGUI _overallRateText;
    [SerializeField] private string _overallRateFormat = "전체 수집률: {0:F1}%";
    [SerializeField] private Transform _categoryBarsContainer;
    [SerializeField] private GameObject _categoryBarPrefab;
    [SerializeField] private int _maxCategoryBars = 12;

    [Header("Navigation")]
    [SerializeField] private Button _closeButton;

    [Header("Pagination")]
    [SerializeField] private Button _prevPageButton;
    [SerializeField] private Button _nextPageButton;
    [SerializeField] private TextMeshProUGUI _pageText;
    [SerializeField] private int _pageSize = 1000;

    private ItemMainCategory _currentMain = ItemMainCategory.None;
    private ItemMiddleCategory _currentMiddle = ItemMiddleCategory.None;
    private ItemSubCategory _currentSub = ItemSubCategory.None;
    private ItemTier _currentTier = ItemTier.Tier1;
    private UnlockFilterMode _unlockFilter = UnlockFilterMode.All;
    private bool _tierOnlyFilter;
    private ItemTier _tierOnlyValue = ItemTier.Tier1;
    private string _searchQuery = "";
    private readonly List<string> _filteredIds = new List<string>();
    private readonly List<string> _searchTemp = new List<string>();
    private readonly List<string> _pageIds = new List<string>();
    private int _currentPage;

    private void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Close);
        }

        if (_mainCategoryDropdown != null)
        {
            _mainCategoryDropdown.onValueChanged.AddListener(OnMainCategoryChanged);
        }

        if (_middleCategoryDropdown != null)
        {
            _middleCategoryDropdown.onValueChanged.AddListener(OnMiddleCategoryChanged);
        }

        if (_subCategoryDropdown != null)
        {
            _subCategoryDropdown.onValueChanged.AddListener(OnSubCategoryChanged);
        }

        if (_tierDropdown != null)
        {
            _tierDropdown.onValueChanged.AddListener(OnTierChanged);
        }

        if (_searchInputField != null)
        {
            _searchInputField.onValueChanged.AddListener(OnSearchChanged);
        }

        if (_filterAllToggle != null)
        {
            _filterAllToggle.onValueChanged.AddListener(v => { if (v) _unlockFilter = UnlockFilterMode.All; ApplyFiltersAndRefresh(); });
        }

        if (_filterUnlockedToggle != null)
        {
            _filterUnlockedToggle.onValueChanged.AddListener(v => { if (v) _unlockFilter = UnlockFilterMode.Unlocked; ApplyFiltersAndRefresh(); });
        }

        if (_filterLockedToggle != null)
        {
            _filterLockedToggle.onValueChanged.AddListener(v => { if (v) _unlockFilter = UnlockFilterMode.Locked; ApplyFiltersAndRefresh(); });
        }

        if (_tierOnlyFilterToggle != null)
        {
            _tierOnlyFilterToggle.onValueChanged.AddListener(v => { _tierOnlyFilter = v; ApplyFiltersAndRefresh(); });
        }

        if (_tierOnlyDropdown != null)
        {
            _tierOnlyDropdown.onValueChanged.AddListener(_ => { _tierOnlyValue = (ItemTier)_tierOnlyDropdown.value; ApplyFiltersAndRefresh(); });
        }

        if (_scrollView != null)
        {
            _scrollView.OnCellClicked += OnCellClicked;
        }

        if (_prevPageButton != null)
        {
            _prevPageButton.onClick.AddListener(GoToPrevPage);
        }

        if (_nextPageButton != null)
        {
            _nextPageButton.onClick.AddListener(GoToNextPage);
        }
    }

    private void OnDestroy()
    {
        if (_scrollView != null)
        {
            _scrollView.OnCellClicked -= OnCellClicked;
        }
    }

    private void OnSearchChanged(string value)
    {
        _searchQuery = value ?? "";
        if (_searchDebounceCoroutine != null)
        {
            StopCoroutine(_searchDebounceCoroutine);
        }
        _searchDebounceCoroutine = StartCoroutine(SearchDebounceRoutine());
    }

    private System.Collections.IEnumerator SearchDebounceRoutine()
    {
        yield return new WaitForSeconds(_searchDebounceSeconds);
        _searchDebounceCoroutine = null;
        ApplyFiltersAndRefresh();
    }

    /// <summary>
    /// UIManager가 패널을 활성화할 때 호출됩니다. 카테고리/필터/대시보드를 새로고침합니다.
    /// </summary>
    private void OnEnable()
    {
        if (EncyclopediaManager.Instance != null)
        {
            EncyclopediaManager.Instance.RebuildIndexIfEmpty();
        }

        RefreshCategoryOptions();
        RefreshTierOnlyOptions();
        ApplyFiltersAndRefresh();
        RefreshDashboard();
    }

    /// <summary>
    /// 도감을 닫고 메인메뉴로 돌아갑니다.
    /// </summary>
    public void Close()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.MainMenu);
        }
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
        if (!validMiddles.Contains(_currentMiddle))
        {
            _currentMiddle = ItemMiddleCategory.None;
        }

        _middleCategoryDropdown.ClearOptions();
        var options = new List<string>();
        foreach (var m in validMiddles)
        {
            options.Add(m == ItemMiddleCategory.None ? "전체" : m.ToString());
        }
        _middleCategoryDropdown.AddOptions(options);

        int middleIndex = validMiddles.IndexOf(_currentMiddle);
        if (middleIndex < 0) middleIndex = 0;
        _middleCategoryDropdown.SetValueWithoutNotify(middleIndex);
    }

    private void RefreshSubCategoryOptions()
    {
        if (_subCategoryDropdown == null) return;

        var validSubs = GetValidSubCategories(_currentMiddle);
        if (!validSubs.Contains(_currentSub))
        {
            _currentSub = ItemSubCategory.None;
        }

        _subCategoryDropdown.ClearOptions();
        var options = new List<string>();
        foreach (var s in validSubs)
        {
            options.Add(s == ItemSubCategory.None ? "전체" : s.ToString());
        }
        _subCategoryDropdown.AddOptions(options);

        int subIndex = validSubs.IndexOf(_currentSub);
        if (subIndex < 0) subIndex = 0;
        _subCategoryDropdown.SetValueWithoutNotify(subIndex);
    }

    private void RefreshTierCategoryOptions()
    {
        if (_tierDropdown == null) return;

        _tierDropdown.ClearOptions();
        var tierOpts = new List<string> { "전체" };
        tierOpts.AddRange(System.Enum.GetNames(typeof(ItemTier)));
        _tierDropdown.AddOptions(tierOpts);
        _tierDropdown.SetValueWithoutNotify(0);
    }

    private static List<ItemMiddleCategory> GetValidMiddleCategories(ItemMainCategory main)
    {
        if (CategoryDefinitionMaps.MainToMiddleMap.TryGetValue(main, out var arr))
        {
            return new List<ItemMiddleCategory>(arr);
        }
        return new List<ItemMiddleCategory> { ItemMiddleCategory.None };
    }

    private static List<ItemSubCategory> GetValidSubCategories(ItemMiddleCategory middle)
    {
        if (CategoryDefinitionMaps.MiddleToSubMap.TryGetValue(middle, out var arr))
        {
            return new List<ItemSubCategory>(arr);
        }
        return new List<ItemSubCategory> { ItemSubCategory.None };
    }

    private void RefreshTierOnlyOptions()
    {
        if (_tierOnlyDropdown != null)
        {
            _tierOnlyDropdown.ClearOptions();
            _tierOnlyDropdown.AddOptions(new List<string>(System.Enum.GetNames(typeof(ItemTier))));
            _tierOnlyDropdown.SetValueWithoutNotify((int)_tierOnlyValue);
        }
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
        _currentTier = index <= 0 ? ItemTier.Tier1 : (ItemTier)System.Math.Min(index - 1, (int)ItemTier.Tier10);
        ApplyFiltersAndRefresh();
    }

    private void ApplyFiltersAndRefresh()
    {
        var manager = EncyclopediaManager.Instance;
        var db = ItemDatabase.Instance;

        if (manager == null || db == null)
        {
            if (_scrollView != null)
            {
                _scrollView.SetItemIds(new List<string>());
            }
            return;
        }

        EncyclopediaCategoryPath path;

        if (_tierOnlyFilter)
        {
            path = new EncyclopediaCategoryPath
            {
                Main = ItemMainCategory.None,
                Middle = null,
                Sub = null,
                Tier = _tierOnlyValue
            };
        }
        else
        {
            path = new EncyclopediaCategoryPath
            {
                Main = _currentMain,
                Middle = _currentMiddle,
                Sub = _currentSub,
                Tier = _tierDropdown != null && _tierDropdown.value > 0 ? (ItemTier?)(_tierDropdown.value - 1) : null
            };
        }

        IReadOnlyList<string> baseIds = manager.GetItemIdsForPath(path);

        _filteredIds.Clear();
        if (baseIds != null && _filteredIds.Capacity < baseIds.Count)
        {
            _filteredIds.Capacity = Mathf.Min(baseIds.Count, 102400);
        }

        if (!string.IsNullOrWhiteSpace(_searchQuery))
        {
            _searchTemp.Clear();
            db.SearchByItemName(_searchQuery.Trim(), baseIds, _searchTemp);
            foreach (string id in _searchTemp)
            {
                bool isUnlocked = manager.IsUnlocked(id);
                switch (_unlockFilter)
                {
                    case UnlockFilterMode.Unlocked:
                        if (!isUnlocked) continue;
                        break;
                    case UnlockFilterMode.Locked:
                        if (isUnlocked) continue;
                        break;
                }
                _filteredIds.Add(id);
            }
        }
        else if (baseIds != null)
        {
            foreach (string id in baseIds)
            {
                bool isUnlocked = manager.IsUnlocked(id);
                switch (_unlockFilter)
                {
                    case UnlockFilterMode.Unlocked:
                        if (!isUnlocked) continue;
                        break;
                    case UnlockFilterMode.Locked:
                        if (isUnlocked) continue;
                        break;
                }
                _filteredIds.Add(id);
            }
        }

        _currentPage = 0;
        ApplyPageSlice();

        RefreshProgress(path);
        RefreshPagination();
        RefreshDashboard();
    }

    /// <summary>
    /// 현재 페이지 구간의 ID만 스크롤뷰에 전달합니다. 페이지당 _pageSize개로 제한합니다.
    /// </summary>
    private void ApplyPageSlice()
    {
        _pageIds.Clear();
        int total = _filteredIds.Count;
        int pageCount = GetTotalPageCount();
        if (pageCount <= 0)
        {
            if (_scrollView != null)
            {
                _scrollView.SetItemIds(_pageIds);
            }
            return;
        }

        int start = _currentPage * _pageSize;
        int end = Mathf.Min(start + _pageSize, total);

        for (int i = start; i < end; i++)
        {
            _pageIds.Add(_filteredIds[i]);
        }

        if (_scrollView != null)
        {
            _scrollView.SetItemIds(_pageIds);
        }
    }

    private int GetTotalPageCount()
    {
        int total = _filteredIds.Count;
        if (total <= 0) return 0;
        return (total + _pageSize - 1) / _pageSize;
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

    /// <summary>
    /// 특정 페이지로 이동합니다. (1-based)
    /// </summary>
    public void GoToPage(int pageIndex)
    {
        int totalPages = GetTotalPageCount();
        if (totalPages <= 0) return;
        int idx = Mathf.Clamp(pageIndex - 1, 0, totalPages - 1);
        if (_currentPage == idx) return;
        _currentPage = idx;
        ApplyPageSlice();
        RefreshPagination();
    }

    private void RefreshPagination()
    {
        int totalPages = GetTotalPageCount();
        int current = _currentPage + 1;

        if (_prevPageButton != null)
        {
            _prevPageButton.interactable = _currentPage > 0;
        }

        if (_nextPageButton != null)
        {
            _nextPageButton.interactable = totalPages > 1 && _currentPage < totalPages - 1;
        }

        if (_pageText != null)
        {
            if (totalPages <= 0)
            {
                _pageText.text = "1 / 1";
            }
            else
            {
                _pageText.text = $"{current} / {totalPages}";
            }
        }
    }

    private void RefreshProgress(EncyclopediaCategoryPath path)
    {
        var manager = EncyclopediaManager.Instance;
        if (manager == null || _progressText == null)
        {
            return;
        }

        int total = _filteredIds.Count;
        int unlocked = 0;
        foreach (string id in _filteredIds)
        {
            if (manager.IsUnlocked(id))
            {
                unlocked++;
            }
        }

        _progressText.text = string.Format(_progressFormat, unlocked, total);
    }

    private void RefreshDashboard()
    {
        var manager = EncyclopediaManager.Instance;
        if (manager == null || _dashboardPanel == null)
        {
            return;
        }

        var (unlocked, total) = manager.GetOverallProgress();
        float rate = total > 0 ? (unlocked * 100f / total) : 0f;

        if (_overallRateText != null)
        {
            _overallRateText.text = string.Format(_overallRateFormat, rate);
        }

        if (_categoryBarsContainer != null && _categoryBarPrefab != null)
        {
            var breakdown = manager.GetCategoryBreakdown();

            while (_categoryBarsContainer.childCount > 0)
            {
                DestroyImmediate(_categoryBarsContainer.GetChild(0).gameObject);
            }

            int count = 0;
            foreach (var (category, u, t) in breakdown)
            {
                if (count >= _maxCategoryBars)
                {
                    break;
                }

                var go = Instantiate(_categoryBarPrefab, _categoryBarsContainer);
                go.name = "CategoryBar_" + category;

                var label = go.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    float pct = t > 0 ? (u * 100f / t) : 0;
                    label.text = $"{category}: {u}/{t} ({pct:F0}%)";
                }

                var fill = go.transform.Find("Fill")?.GetComponent<Image>();
                if (fill != null && t > 0)
                {
                    fill.fillAmount = u / (float)t;
                }

                count++;
            }
        }
    }

    private void OnCellClicked(int index, string itemId)
    {
    }
}
