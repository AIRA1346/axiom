using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 상품 리스트용 가상화 스크롤 뷰.
/// 화면에 보이는 셀만 생성해 재사용하여 대량 아이템 대응.
/// 그리드(다열) 지원으로 페이지당 1000개 아이템을 효율적으로 표시합니다.
/// </summary>
public sealed class VirtualizedShopScrollView : MonoBehaviour
{
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _viewport;
    [SerializeField] private RectTransform _content;
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private int _cellPoolSize = 120;
    [SerializeField] private int _columns = 4;
    [SerializeField] private float _cellHeight = 64f;
    [SerializeField] private float _cellSpacing = 4f;
    [SerializeField] private RectOffset _contentPadding;

    private const float MinViewportHeight = 500f;

    private readonly List<ShopSlotData> _currentEntries = new List<ShopSlotData>();
    private readonly List<GameObject> _cellPool = new List<GameObject>();
    private int _lastFirstVisibleIndex = -1;
    private int _lastLastVisibleIndex = -1;
    private bool _initialized;

    public int EntryCount => _currentEntries.Count;
    public event Action<string, int> OnPurchaseRequested;

    public struct ShopSlotData
    {
        public string ItemId;
        public string DisplayName;
        public ItemTier Tier;
        public int Price;
        public Sprite Icon;
        public bool CanPurchase;
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
            Debug.LogError("[VirtualizedShopScrollView] Cell Prefab이 할당되지 않았습니다.");
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
        var sizeFitter = _content.GetComponent<ContentSizeFitter>();
        if (sizeFitter != null) Destroy(sizeFitter);
        if (_viewport != null)
        {
            _viewport.anchorMin = new Vector2(0, 0);
            _viewport.anchorMax = new Vector2(1, 1);
            _viewport.offsetMin = Vector2.zero;
            _viewport.offsetMax = Vector2.zero;
        }
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
            cellObj.name = $"ShopCell_{i}";
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

    /// <summary>상점 표시 데이터를 설정하고 UI를 갱신합니다.</summary>
    public void SetEntries(IReadOnlyList<ShopSlotData> entries)
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
        int cols = Mathf.Max(1, _columns);
        int rowCount = count <= 0 ? 0 : (count + cols - 1) / cols;
        float rowHeight = _cellHeight + _cellSpacing;
        float totalHeight = _contentPadding.top + rowCount * rowHeight - _cellSpacing + _contentPadding.bottom;
        float viewportH = GetEffectiveViewportHeight();
        _content.sizeDelta = new Vector2(_content.sizeDelta.x, Mathf.Max(totalHeight, viewportH));
        _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, 0);
        foreach (var cell in _cellPool) cell.SetActive(false);
        _lastFirstVisibleIndex = -1;
        _lastLastVisibleIndex = -1;
        UpdateVisibleCells();
    }

    private void OnScrollValueChanged(Vector2 _) => UpdateVisibleCells();

    private float GetEffectiveViewportHeight()
    {
        float h = _viewport != null ? _viewport.rect.height : 0f;
        if (_scrollRect != null)
        {
            var rt = _scrollRect.GetComponent<RectTransform>();
            if (rt != null && rt.rect.height > h)
                h = rt.rect.height;
        }
        return Mathf.Max(h, MinViewportHeight);
    }

    private void UpdateVisibleCells()
    {
        if (!_initialized || _viewport == null || _content == null || _cellPool.Count == 0) return;
        int count = _currentEntries.Count;
        if (count == 0) return;

        float viewportHeight = GetEffectiveViewportHeight();
        int cols = Mathf.Max(1, _columns);
        float rowHeight = _cellHeight + _cellSpacing;
        int rowCount = (count + cols - 1) / cols;
        float totalHeight = _contentPadding.top + rowCount * rowHeight - _cellSpacing + _contentPadding.bottom;
        float contentY = 0f;
        if (totalHeight > viewportHeight && _scrollRect != null)
        {
            float normY = _scrollRect.verticalNormalizedPosition;
            contentY = (1f - Mathf.Clamp01(normY)) * (totalHeight - viewportHeight);
        }
        float contentWidth = _content != null && _content.rect.width > 1f ? _content.rect.width : (_viewport != null && _viewport.rect.width > 1f ? _viewport.rect.width : 600f);
        float cellWidth = (contentWidth - _contentPadding.left - _contentPadding.right - (cols - 1) * _cellSpacing) / cols;
        if (cellWidth < 50f) cellWidth = 100f;

        int firstVisibleRow = Mathf.Clamp(Mathf.FloorToInt((contentY - _contentPadding.top) / rowHeight), 0, 99999);
        int lastVisibleRow = Mathf.Clamp(Mathf.FloorToInt((contentY + viewportHeight - _contentPadding.top) / rowHeight), 0, 99999);
        lastVisibleRow = Mathf.Min(lastVisibleRow, rowCount - 1);
        if (firstVisibleRow > lastVisibleRow)
        {
            firstVisibleRow = 0;
            lastVisibleRow = 0;
        }

        int firstVisible = firstVisibleRow * cols;
        int lastVisible = Mathf.Min(lastVisibleRow * cols + (cols - 1), count - 1);
        int visibleCount = lastVisible - firstVisible + 1;

        if (firstVisible == _lastFirstVisibleIndex && lastVisible == _lastLastVisibleIndex) return;
        _lastFirstVisibleIndex = firstVisible;
        _lastLastVisibleIndex = lastVisible;

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
                        int row = dataIndex / cols;
                        int col = dataIndex % cols;
                        float x = _contentPadding.left + col * (cellWidth + _cellSpacing) + cellWidth * 0.5f;
                        float y = _contentPadding.top + row * rowHeight;
                        rect.anchorMin = new Vector2(0, 1);
                        rect.anchorMax = new Vector2(0, 1);
                        rect.pivot = new Vector2(0.5f, 1f);
                        rect.anchoredPosition = new Vector2(x, -y);
                        rect.sizeDelta = new Vector2(cellWidth, _cellHeight);
                    }
                }
                else cellObj.SetActive(false);
            }
            else cellObj.SetActive(false);
        }
    }

    private void BindCell(GameObject cellObj, ShopSlotData data)
    {
        var texts = cellObj.GetComponentsInChildren<TextMeshProUGUI>(true);
        var images = cellObj.GetComponentsInChildren<Image>(true);
        var buyButton = cellObj.GetComponentInChildren<Button>(true);

        TextMeshProUGUI nameText = texts != null && texts.Length > 0 ? texts[0] : null;
        TextMeshProUGUI priceText = texts != null && texts.Length > 1 ? texts[1] : null;
        TextMeshProUGUI buttonText = texts != null && texts.Length > 2 ? texts[2] : null;
        if (priceText == null && texts != null && texts.Length > 1)
        {
            foreach (var t in texts)
            {
                if (t != null && t.gameObject.name == "PriceText") { priceText = t; break; }
            }
        }
        if (nameText == null && texts != null && texts.Length > 0) nameText = texts[0];
        if (priceText == null && texts != null && texts.Length > 1) priceText = texts[1];

        Image itemIconImage = null;
        if (images != null)
        {
            foreach (var img in images)
            {
                if (buyButton != null && img == buyButton.targetGraphic) continue;
                if (img.gameObject.name == "Icon") { itemIconImage = img; break; }
            }
            if (itemIconImage == null && images.Length > 0)
            {
                foreach (var img in images)
                {
                    if (buyButton == null || img != buyButton.targetGraphic) { itemIconImage = img; break; }
                }
            }
        }

        string tierStr = GetTierName(data.Tier);
        if (nameText != null)
        {
            nameText.text = $"[{tierStr}] {data.DisplayName}";
        }

        if (priceText != null)
        {
            priceText.text = $"{data.Price} 기초 골드";
        }

        if (itemIconImage != null)
        {
            itemIconImage.sprite = data.Icon;
            itemIconImage.enabled = data.Icon != null;
        }

        if (buttonText != null)
        {
            buttonText.text = "구매";
        }

        if (buyButton != null)
        {
            buyButton.interactable = data.CanPurchase && data.Price > 0;
            buyButton.onClick.RemoveAllListeners();
            var trigger = buyButton.GetComponent<ShopSlotPurchaseTrigger>();
            if (trigger == null) trigger = buyButton.gameObject.AddComponent<ShopSlotPurchaseTrigger>();
            trigger.OnPurchaseClicked -= OnTriggerPurchaseClicked;
            if (data.CanPurchase && data.Price > 0)
            {
                string itemId = data.ItemId;
                int price = data.Price;
                trigger.SetData(itemId, price);
                trigger.OnPurchaseClicked += OnTriggerPurchaseClicked;
            }
            else
            {
                trigger.SetData(null, 0);
            }
        }
    }

    private void OnTriggerPurchaseClicked(string itemId, int price)
    {
        OnPurchaseRequested?.Invoke(itemId, price);
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
