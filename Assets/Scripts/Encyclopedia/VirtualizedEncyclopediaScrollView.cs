using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 10만 개 아이템 대응 가상화 스크롤 뷰.
/// 화면에 보이는 20~30개 셀만 생성해 재사용하고, 스크롤 시 데이터를 실시간으로 갈아끼웁니다.
/// </summary>
public sealed class VirtualizedEncyclopediaScrollView : MonoBehaviour
{
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _viewport;
    [SerializeField] private RectTransform _content;
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private int _cellPoolSize = 28;
    [SerializeField] private float _cellHeight = 80f;
    [SerializeField] private float _cellSpacing = 4f;
    [SerializeField] private RectOffset _contentPadding;

    private readonly List<string> _currentItemIds = new List<string>();
    private readonly List<EncyclopediaItemCell> _cellPool = new List<EncyclopediaItemCell>();
    private float _totalContentHeight;
    private int _lastFirstVisibleIndex = -1;
    private int _lastLastVisibleIndex = -1;
    private bool _initialized;
    private EncyclopediaCategoryPath _currentPath;

    public int ItemCount => _currentItemIds.Count;
    public event Action<int, string> OnCellClicked;

    private void Awake()
    {
        if (_contentPadding.left == 0 && _contentPadding.right == 0 && _contentPadding.top == 0 && _contentPadding.bottom == 0)
        {
            _contentPadding = new RectOffset(8, 8, 8, 8);
        }

        if (_scrollRect == null)
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        if (_viewport == null && _scrollRect != null)
        {
            _viewport = _scrollRect.viewport;
        }

        if (_content == null && _scrollRect != null)
        {
            _content = _scrollRect.content;
        }

        if (_cellPrefab == null)
        {
            Debug.LogError("[VirtualizedEncyclopediaScrollView] Cell Prefab이 할당되지 않았습니다.");
            return;
        }

        SetupScrollRect();
        CreateCellPool();
        _initialized = true;
    }

    private void OnEnable()
    {
        if (_scrollRect != null)
        {
            _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }
    }

    private void OnDisable()
    {
        if (_scrollRect != null)
        {
            _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        }
    }

    private void SetupScrollRect()
    {
        if (_scrollRect == null || _content == null)
        {
            return;
        }

        _scrollRect.vertical = true;
        _scrollRect.horizontal = false;
        _scrollRect.content = _content;

        var layout = _content.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            Destroy(layout);
        }

        _content.anchorMin = new Vector2(0, 1);
        _content.anchorMax = new Vector2(1, 1);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = new Vector2(0, 0);
    }

    private void CreateCellPool()
    {
        _cellPool.Clear();

        for (int i = 0; i < _cellPoolSize; i++)
        {
            GameObject cellObj = Instantiate(_cellPrefab, _content);
            cellObj.name = $"EncyclopediaCell_{i}";

            var rect = cellObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(0, _cellHeight);
            }

            var cell = cellObj.GetComponent<EncyclopediaItemCell>();
            if (cell == null)
            {
                cell = cellObj.AddComponent<EncyclopediaItemCell>();
            }

            var btn = cellObj.GetComponent<Button>();
            if (btn != null)
            {
                var capturedCell = cell;
                btn.onClick.AddListener(() =>
                {
                    if (capturedCell != null && capturedCell.gameObject.activeSelf)
                    {
                        OnCellClicked?.Invoke(capturedCell.DataIndex, capturedCell.ItemId);
                    }
                });
            }

            cellObj.SetActive(false);
            _cellPool.Add(cell);
        }
    }

    /// <summary>
    /// 특정 카테고리 경로의 아이템 목록으로 도감 뷰를 갱신합니다.
    /// CategoryIndex를 통해 즉시 ItemId 리스트를 가져옵니다.
    /// </summary>
    public void SetCategoryPath(EncyclopediaCategoryPath path)
    {
        _currentPath = path;

        var manager = EncyclopediaManager.Instance;
        var db = ItemDatabase.Instance;

        if (manager == null || db == null)
        {
            _currentItemIds.Clear();
            RefreshContent();
            return;
        }

        IReadOnlyList<string> ids = manager.GetItemIdsForPath(path);
        SetItemIds(ids);
    }

    /// <summary>
    /// 필터/검색 결과 등 외부에서 계산된 ItemId 리스트를 직접 설정합니다.
    /// </summary>
    public void SetItemIds(IReadOnlyList<string> ids)
    {
        _currentItemIds.Clear();

        if (ids != null)
        {
            foreach (string id in ids)
            {
                _currentItemIds.Add(id);
            }
        }

        RefreshContent();
    }

    /// <summary>
    /// 대분류만 지정한 경로로 설정.
    /// </summary>
    public void SetCategoryPath(ItemMainCategory main)
    {
        SetCategoryPath(new EncyclopediaCategoryPath { Main = main, Middle = null, Sub = null, Tier = null });
    }

    /// <summary>
    /// 대분류+중분류 경로로 설정.
    /// </summary>
    public void SetCategoryPath(ItemMainCategory main, ItemMiddleCategory middle)
    {
        SetCategoryPath(new EncyclopediaCategoryPath { Main = main, Middle = middle, Sub = null, Tier = null });
    }

    /// <summary>
    /// 대분류+중분류+소분류 경로로 설정.
    /// </summary>
    public void SetCategoryPath(ItemMainCategory main, ItemMiddleCategory middle, ItemSubCategory sub)
    {
        SetCategoryPath(new EncyclopediaCategoryPath { Main = main, Middle = middle, Sub = sub, Tier = null });
    }

    /// <summary>
    /// 전체 경로로 설정.
    /// </summary>
    public void SetCategoryPath(ItemMainCategory main, ItemMiddleCategory middle, ItemSubCategory sub, ItemTier tier)
    {
        SetCategoryPath(new EncyclopediaCategoryPath { Main = main, Middle = middle, Sub = sub, Tier = tier });
    }

    private void RefreshContent()
    {
        if (!_initialized || _content == null)
        {
            return;
        }

        int count = _currentItemIds.Count;
        _totalContentHeight = _contentPadding.top + count * (_cellHeight + _cellSpacing) - _cellSpacing + _contentPadding.bottom;
        _content.sizeDelta = new Vector2(_content.sizeDelta.x, Mathf.Max(_totalContentHeight, _viewport.rect.height));
        _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, 0);

        foreach (var cell in _cellPool)
        {
            cell.gameObject.SetActive(false);
        }

        _lastFirstVisibleIndex = -1;
        _lastLastVisibleIndex = -1;
        UpdateVisibleCells();
    }

    private void OnScrollValueChanged(Vector2 _)
    {
        UpdateVisibleCells();
    }

    /// <summary>
    /// 스크롤 위치 변경에 따른 가시 셀 갱신. 가상화 뷰 특성상 매 프레임 체크 필요.
    /// </summary>
    private void LateUpdate()
    {
        UpdateVisibleCells();
    }

    private void UpdateVisibleCells()
    {
        if (!_initialized || _viewport == null || _content == null || _cellPool.Count == 0)
        {
            return;
        }

        int count = _currentItemIds.Count;
        if (count == 0)
        {
            return;
        }

        float viewportHeight = _viewport.rect.height;
        float contentY = Mathf.Max(0, _content.anchoredPosition.y);

        float visibleTop = contentY;
        float visibleBottom = contentY + viewportHeight;

        float itemHeight = _cellHeight + _cellSpacing;
        int firstVisible = Mathf.Clamp(Mathf.FloorToInt((visibleTop - _contentPadding.top) / itemHeight), 0, count - 1);
        int lastVisible = Mathf.Clamp(Mathf.FloorToInt((visibleBottom - _contentPadding.top) / itemHeight), 0, count - 1);

        if (firstVisible == _lastFirstVisibleIndex && lastVisible == _lastLastVisibleIndex)
        {
            return;
        }

        _lastFirstVisibleIndex = firstVisible;
        _lastLastVisibleIndex = lastVisible;

        int visibleCount = lastVisible - firstVisible + 1;
        int poolCount = Mathf.Min(visibleCount, _cellPool.Count);

        var manager = EncyclopediaManager.Instance;
        var db = ItemDatabase.Instance;

        for (int i = 0; i < _cellPool.Count; i++)
        {
            var cell = _cellPool[i];

            if (i < poolCount)
            {
                int dataIndex = firstVisible + i;
                if (dataIndex < _currentItemIds.Count)
                {
                    string itemId = _currentItemIds[dataIndex];
                    ItemMetadata? meta = db?.GetMetadata(itemId);
                    ItemData data = db?.GetItem(itemId);
                    bool isUnlocked = manager?.IsUnlocked(itemId) ?? false;

                    cell.gameObject.SetActive(true);
                    cell.Bind(itemId, meta, data, isUnlocked, dataIndex);

                    var rect = cell.RectTransform;
                    if (rect != null)
                    {
                        float y = _contentPadding.top + dataIndex * itemHeight;
                        rect.anchoredPosition = new Vector2(_contentPadding.left * 0.5f, -y);
                        rect.sizeDelta = new Vector2(0, _cellHeight);
                    }
                }
                else
                {
                    cell.gameObject.SetActive(false);
                }
            }
            else
            {
                cell.gameObject.SetActive(false);
            }
        }
    }
}
