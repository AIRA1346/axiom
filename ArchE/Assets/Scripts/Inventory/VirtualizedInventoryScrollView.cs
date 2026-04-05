using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 리스트 UI용 가상화 스크롤 뷰.
/// 화면에 보이는 셀만 생성해 재사용하여 10만 개 대응.
/// </summary>
public sealed class VirtualizedInventoryScrollView : MonoBehaviour
{
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _viewport;
    [SerializeField] private RectTransform _content;
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private int _cellPoolSize = 32;
    [SerializeField] private float _cellHeight = 115f;
    [SerializeField] private float _cellSpacing = 4f;
    [SerializeField] private RectOffset _contentPadding;

    private readonly List<InventorySlotData> _currentEntries = new List<InventorySlotData>();
    private readonly List<GameObject> _cellPool = new List<GameObject>();
    private float _totalContentHeight;
    private int _lastFirstVisibleIndex = -1;
    private int _lastLastVisibleIndex = -1;
    private bool _initialized;

    public int EntryCount => _currentEntries.Count;
    public event Action<string, int> OnSellRequested;

    public struct InventorySlotData
    {
        public string InstanceId;
        public string ItemId;
        public string DisplayName;
        public ItemTier Tier;
        public int Count;
        public int EnhanceLevel;
        public int SalePrice;
        public Sprite Icon;
        public bool IsEquipped;
        public bool IsCurrency;
    }

    private void Awake()
    {
        if (_contentPadding.left == 0 && _contentPadding.right == 0 && _contentPadding.top == 0 && _contentPadding.bottom == 0)
        {
            _contentPadding = new RectOffset(8, 8, 8, 8);
        }

        if (_scrollRect == null) _scrollRect = GetComponent<ScrollRect>();
        if (_viewport == null && _scrollRect != null) _viewport = _scrollRect.viewport;
        if (_content == null && _scrollRect != null) _content = _scrollRect.content;

        if (_cellPrefab == null)
        {
            Debug.LogError("[VirtualizedInventoryScrollView] Cell Prefab이 할당되지 않았습니다.");
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

    private void LateUpdate()
    {
        UpdateVisibleCells();
    }

    private void SetupScrollRect()
    {
        if (_scrollRect == null || _content == null) return;
        _scrollRect.vertical = true;
        _scrollRect.horizontal = false;
        _scrollRect.content = _content;
        var layout = _content.GetComponent<VerticalLayoutGroup>();
        if (layout != null) Destroy(layout);
        _content.anchorMin = new Vector2(0, 1);
        _content.anchorMax = new Vector2(1, 1);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.sizeDelta = new Vector2(0, 0);
        _content.anchoredPosition = Vector2.zero;
    }

    private void CreateCellPool()
    {
        _cellPool.Clear();
        for (int i = 0; i < _cellPoolSize; i++)
        {
            var cellObj = Instantiate(_cellPrefab, _content);
            cellObj.name = $"InventoryCell_{i}";
            var rect = cellObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(0, _cellHeight);
            }
            cellObj.SetActive(false);
            _cellPool.Add(cellObj);
        }
    }

    /// <summary>인벤토리 표시 데이터를 설정하고 UI를 갱신합니다.</summary>
    public void SetEntries(IReadOnlyList<InventorySlotData> entries)
    {
        _currentEntries.Clear();
        if (entries != null)
        {
            foreach (var e in entries)
            {
                _currentEntries.Add(e);
            }
        }
        RefreshContent();
    }

    private void RefreshContent()
    {
        if (!_initialized || _content == null) return;
        int count = _currentEntries.Count;
        _totalContentHeight = _contentPadding.top + count * (_cellHeight + _cellSpacing) - _cellSpacing + _contentPadding.bottom;
        _content.sizeDelta = new Vector2(_content.sizeDelta.x, Mathf.Max(_totalContentHeight, _viewport != null ? _viewport.rect.height : 400f));
        _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, 0);
        foreach (var cell in _cellPool) cell.SetActive(false);
        _lastFirstVisibleIndex = -1;
        _lastLastVisibleIndex = -1;
        UpdateVisibleCells();
    }

    private void OnScrollValueChanged(Vector2 _) => UpdateVisibleCells();

    private void UpdateVisibleCells()
    {
        if (!_initialized || _viewport == null || _content == null || _cellPool.Count == 0) return;
        int count = _currentEntries.Count;
        if (count == 0) return;

        float viewportHeight = _viewport.rect.height;
        float contentY = Mathf.Max(0f, -_content.anchoredPosition.y);
        float itemHeight = _cellHeight + _cellSpacing;
        int firstVisible = Mathf.Clamp(Mathf.FloorToInt((contentY - _contentPadding.top) / itemHeight), 0, count - 1);
        int lastVisible = Mathf.Clamp(Mathf.FloorToInt((contentY + viewportHeight - _contentPadding.top) / itemHeight), 0, count - 1);

        if (firstVisible == _lastFirstVisibleIndex && lastVisible == _lastLastVisibleIndex) return;
        _lastFirstVisibleIndex = firstVisible;
        _lastLastVisibleIndex = lastVisible;

        int visibleCount = lastVisible - firstVisible + 1;
        int poolCount = Mathf.Min(visibleCount, _cellPool.Count);

        for (int i = 0; i < _cellPool.Count; i++)
        {
            var cellObj = _cellPool[i];
            if (i < poolCount)
            {
                int dataIndex = firstVisible + i;
                if (dataIndex < _currentEntries.Count)
                {
                    BindCell(cellObj, _currentEntries[dataIndex]);
                    cellObj.SetActive(true);
                    var rect = cellObj.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        float y = _contentPadding.top + dataIndex * itemHeight;
                        rect.anchoredPosition = new Vector2(_contentPadding.left * 0.5f, -y);
                        rect.sizeDelta = new Vector2(0, _cellHeight);
                    }
                }
                else cellObj.SetActive(false);
            }
            else cellObj.SetActive(false);
        }
    }

    private void BindCell(GameObject cellObj, InventorySlotData data)
    {
        var texts = cellObj.GetComponentsInChildren<TextMeshProUGUI>(true);
        var images = cellObj.GetComponentsInChildren<Image>(true);
        var sellButton = cellObj.GetComponentInChildren<Button>(true);

        TextMeshProUGUI itemText = texts != null && texts.Length > 0 ? texts[0] : null;
        TextMeshProUGUI buttonText = texts != null && texts.Length > 1 ? texts[1] : null;
        Image itemIconImage = null;
        if (images != null)
        {
            foreach (var img in images)
            {
                if (sellButton != null && img == sellButton.targetGraphic) continue;
                itemIconImage = img;
                break;
            }
        }

        string tierStr = GetTierName(data.Tier);
        if (itemText != null)
        {
            itemText.text = $"[{tierStr}] {data.DisplayName} {(data.EnhanceLevel > 0 ? $"+{data.EnhanceLevel} " : "")}x{data.Count}";
        }

        if (itemIconImage != null)
        {
            itemIconImage.sprite = data.Icon;
        }

        if (buttonText != null)
        {
            if (data.IsEquipped) buttonText.text = "[ 장착 중 ]";
            else if (data.IsCurrency) buttonText.text = "[ 통화 ]";
            else buttonText.text = data.SalePrice > 0 ? $"{data.SalePrice} 기초 골드에 판매" : "판매 불가";
        }

        if (sellButton != null)
        {
            sellButton.interactable = !data.IsEquipped && !data.IsCurrency && data.SalePrice > 0;
            sellButton.onClick.RemoveAllListeners();
            if (!data.IsCurrency && data.SalePrice > 0)
            {
                string instanceId = data.InstanceId;
                int price = data.SalePrice;
                sellButton.onClick.AddListener(() => OnSellRequested?.Invoke(instanceId, price));
            }
        }
    }

    private static string GetTierName(ItemTier tier)
    {
        switch (tier)
        {
            case ItemTier.Tier1: return "凡";
            case ItemTier.Tier2: return "奇";
            case ItemTier.Tier3: return "珍";
            case ItemTier.Tier4: return "傑";
            case ItemTier.Tier5: return "古";
            case ItemTier.Tier6: return "遺";
            case ItemTier.Tier7: return "聖";
            case ItemTier.Tier8: return "傳";
            case ItemTier.Tier9: return "神";
            case ItemTier.Tier10: return "極";
            default: return "凡";
        }
    }
}
