using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ADDS 도서관 플랫 트리 목차용 가상화 스크롤 뷰. Depth별 들여쓰기, 10만 건 이상 대응.
/// </summary>
public sealed class VirtualizedCodexScrollView : MonoBehaviour
{
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _viewport;
    [SerializeField] private RectTransform _content;
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private int _cellPoolSize = 32;
    [SerializeField] private float _cellHeight = 48f;
    [SerializeField] private float _cellSpacing = 2f;
    [SerializeField] private float _indentPerDepth = 14f;
    [SerializeField] private RectOffset _contentPadding;

    private readonly List<CodexMetadata> _currentEntries = new List<CodexMetadata>();
    private readonly List<GameObject> _cellPool = new List<GameObject>();
    private int _lastFirstVisibleIndex = -1;
    private int _lastLastVisibleIndex = -1;
    private bool _initialized;

    public int EntryCount => _currentEntries.Count;
    public event Action<CodexMetadata> OnEntryClicked;

    private void Awake()
    {
        if (_contentPadding == null)
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
            Debug.LogError("[VirtualizedCodexScrollView] Cell Prefab이 할당되지 않았습니다.");
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
        if (_scrollRect == null || _content == null)
        {
            return;
        }

        _scrollRect.vertical = true;
        _scrollRect.horizontal = false;
        _scrollRect.content = _content;
        VerticalLayoutGroup layout = _content.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            Destroy(layout);
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
            GameObject cellObj = Instantiate(_cellPrefab, _content);
            cellObj.name = $"CodexCell_{i}";
            RectTransform rect = cellObj.GetComponent<RectTransform>();
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

    public void SetEntries(IReadOnlyList<CodexMetadata> entries)
    {
        _currentEntries.Clear();
        if (entries != null)
        {
            foreach (CodexMetadata e in entries)
            {
                _currentEntries.Add(e);
            }
        }

        RefreshContent();
    }

    private void RefreshContent()
    {
        if (!_initialized || _content == null)
        {
            return;
        }

        int count = _currentEntries.Count;
        int pTop = _contentPadding != null ? _contentPadding.top : 8;
        int pBottom = _contentPadding != null ? _contentPadding.bottom : 8;
        float totalHeight = pTop + count * (_cellHeight + _cellSpacing) - _cellSpacing + pBottom;
        float minH = _viewport != null ? _viewport.rect.height : 400f;
        _content.sizeDelta = new Vector2(_content.sizeDelta.x, Mathf.Max(totalHeight, minH));
        _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, 0);
        foreach (GameObject cell in _cellPool)
        {
            if (cell != null)
            {
                cell.SetActive(false);
            }
        }

        _lastFirstVisibleIndex = -1;
        _lastLastVisibleIndex = -1;
        UpdateVisibleCells();
    }

    private void OnScrollValueChanged(Vector2 _)
    {
        UpdateVisibleCells();
    }

    private void UpdateVisibleCells()
    {
        if (!_initialized || _viewport == null || _content == null || _cellPool.Count == 0)
        {
            return;
        }

        int count = _currentEntries.Count;
        if (count == 0)
        {
            return;
        }

        float viewportHeight = _viewport.rect.height;
        int padTop = _contentPadding != null ? _contentPadding.top : 8;
        float contentY = Mathf.Max(0f, -_content.anchoredPosition.y);
        float itemHeight = _cellHeight + _cellSpacing;
        int firstVisible = Mathf.Clamp(Mathf.FloorToInt((contentY - padTop) / itemHeight), 0, count - 1);
        int lastVisible = Mathf.Clamp(Mathf.FloorToInt((contentY + viewportHeight - padTop) / itemHeight), 0, count - 1);

        if (firstVisible == _lastFirstVisibleIndex && lastVisible == _lastLastVisibleIndex)
        {
            return;
        }

        _lastFirstVisibleIndex = firstVisible;
        _lastLastVisibleIndex = lastVisible;

        int visibleCount = lastVisible - firstVisible + 1;
        int poolCount = Mathf.Min(visibleCount, _cellPool.Count);

        for (int i = 0; i < _cellPool.Count; i++)
        {
            GameObject cellObj = _cellPool[i];
            if (cellObj == null)
            {
                continue;
            }

            if (i < poolCount)
            {
                int dataIndex = firstVisible + i;
                if (dataIndex < _currentEntries.Count)
                {
                    BindCell(cellObj, _currentEntries[dataIndex]);
                    cellObj.SetActive(true);
                    RectTransform rect = cellObj.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        float y = padTop + dataIndex * itemHeight;
                        int padL = _contentPadding != null ? _contentPadding.left : 8;
                        rect.anchoredPosition = new Vector2(padL * 0.5f, -y);
                        rect.sizeDelta = new Vector2(0, _cellHeight);
                    }
                }
                else
                {
                    cellObj.SetActive(false);
                }
            }
            else
            {
                cellObj.SetActive(false);
            }
        }
    }

    private void BindCell(GameObject cellObj, CodexMetadata data)
    {
        TextMeshProUGUI label = cellObj.GetComponentInChildren<TextMeshProUGUI>(true);
        Button btn = cellObj.GetComponent<Button>();
        CodexEntryCell selectable = cellObj.GetComponent<CodexEntryCell>();

        if (selectable == null)
        {
            selectable = cellObj.AddComponent<CodexEntryCell>();
        }

        if (selectable != null)
        {
            selectable.SetData(data);
            selectable.OnClicked -= HandleEntryClicked;
            selectable.OnClicked += HandleEntryClicked;
        }

        if (label != null)
        {
            label.richText = true;
            label.text = data.Title ?? data.Id ?? "";
            bool category = data.Depth >= 0 && data.Depth <= 8;
            label.fontStyle = category ? FontStyles.Bold : FontStyles.Normal;

            RectTransform textRt = label.rectTransform;
            if (textRt != null)
            {
                int d = Mathf.Clamp(data.Depth, 0, 9);
                float padLeft = _contentPadding != null ? _contentPadding.left : 8;
                float indent = padLeft + d * _indentPerDepth;
                textRt.anchorMin = new Vector2(0, 0);
                textRt.anchorMax = new Vector2(1, 1);
                textRt.offsetMin = new Vector2(indent, 4f);
                textRt.offsetMax = new Vector2(-8f, -4f);
            }
        }

        if (btn != null)
        {
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
        }
    }

    private void HandleEntryClicked(CodexMetadata data)
    {
        OnEntryClicked?.Invoke(data);
    }
}
