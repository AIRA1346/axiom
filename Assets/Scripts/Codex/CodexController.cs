using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ARCHÉ 도서관(Codex) UI. ADDS 플랫 트리 목차 + 우측 본문. 무채색 공식 문서 톤.
/// </summary>
public sealed class CodexController : MonoBehaviour
{
    [Header("상단")]
    [SerializeField] private Button _exitButton;
    [SerializeField] private TextMeshProUGUI _breadcrumbText;

    [Header("좌측 목차")]
    [SerializeField] private VirtualizedCodexScrollView _indexScrollView;

    [Header("우측 본문 뷰어")]
    [SerializeField] private TextMeshProUGUI _bodyViewer;
    [SerializeField] private GameObject _loadingIndicator;
    [SerializeField] private GameObject _emptyStateRoot;

    private CodexMetadata? _currentEntry;
    private readonly List<string> _breadcrumbPath = new List<string>();
    private readonly HashSet<string> _collapsedCategoryKeys = new HashSet<string>();

    private void Awake()
    {
        if (_exitButton != null)
        {
            _exitButton.onClick.AddListener(OnExitClicked);
        }

        if (_indexScrollView != null)
        {
            _indexScrollView.OnEntryClicked += HandleEntryClicked;
        }
    }

    private void OnDestroy()
    {
        if (_indexScrollView != null)
        {
            _indexScrollView.OnEntryClicked -= HandleEntryClicked;
        }

        if (_exitButton != null)
        {
            _exitButton.onClick.RemoveListener(OnExitClicked);
        }
    }

    private async void OnEnable()
    {
        var manager = CodexManager.Instance;
        if (manager != null)
        {
            await manager.WaitForIndexAsync();
        }

        RefreshIndex();
        ShowEmptyState();
    }

    private void RefreshIndex()
    {
        if (_indexScrollView == null)
        {
            return;
        }

        var manager = CodexManager.Instance;
        if (manager == null)
        {
            _indexScrollView.SetEntries(new List<CodexMetadata>());
            return;
        }

        ApplyFilterAndRefresh();
    }

    private void ApplyFilterAndRefresh()
    {
        var manager = CodexManager.Instance;
        if (manager == null || _indexScrollView == null)
        {
            return;
        }

        IReadOnlyList<CodexMetadata> full = manager.GetFlatIndexForNavigation();
        List<CodexMetadata> filtered = FilterByExpanded(full);
        _indexScrollView.SetEntries(filtered);
    }

    private List<CodexMetadata> FilterByExpanded(IReadOnlyList<CodexMetadata> full)
    {
        var result = new List<CodexMetadata>();
        if (full == null)
        {
            return result;
        }

        foreach (CodexMetadata m in full)
        {
            if (IsVisible(m))
            {
                result.Add(m);
            }
        }

        return result;
    }

    /// <summary>접힌 조상 구간이 있으면 행을 숨깁니다.</summary>
    private bool IsVisible(CodexMetadata m)
    {
        int maxPrefixIndex = m.Depth < 9 ? m.Depth - 1 : 8;
        if (maxPrefixIndex < 0)
        {
            return true;
        }

        for (int d = 0; d <= maxPrefixIndex; d++)
        {
            string key = CodexManager.BuildPathKeyThroughDepth(m, d);
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            if (_collapsedCategoryKeys.Contains(key))
            {
                return false;
            }
        }

        return true;
    }

    private void HandleEntryClicked(CodexMetadata data)
    {
        if (data.HasBody && !string.IsNullOrWhiteSpace(data.Id))
        {
            _currentEntry = data;
            UpdateBreadcrumb(data);
            _ = LoadAndShowContentAsync(data);
            return;
        }

        string key = CodexManager.GetCategoryKey(data);
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (_collapsedCategoryKeys.Contains(key))
        {
            _collapsedCategoryKeys.Remove(key);
        }
        else
        {
            _collapsedCategoryKeys.Add(key);
        }

        ApplyFilterAndRefresh();
    }

    private async Task LoadAndShowContentAsync(CodexMetadata entry)
    {
        SetLoading(true);

        if (_bodyViewer != null)
        {
            _bodyViewer.text = "";
        }

        if (_emptyStateRoot != null)
        {
            _emptyStateRoot.SetActive(false);
        }

        string content = null;
        var manager = CodexManager.Instance;
        if (manager != null && !string.IsNullOrWhiteSpace(entry.Id))
        {
            content = await manager.LoadContentAsync(entry.Id);
        }

        SetLoading(false);

        if (_bodyViewer != null)
        {
            _bodyViewer.richText = true;
            string title = entry.Title ?? "";
            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrEmpty(content))
            {
                _bodyViewer.text = $"<color=#CCCCCC><b>{EscapeTmp(title)}</b></color>\n\n{content}";
            }
            else if (!string.IsNullOrEmpty(content))
            {
                _bodyViewer.text = content;
            }
            else if (!string.IsNullOrWhiteSpace(title))
            {
                _bodyViewer.text = $"<color=#888888>{EscapeTmp(title)}</color>\n\n<color=#888888>본문을 불러올 수 없습니다.</color>";
            }
            else
            {
                _bodyViewer.text = "<color=#888888>본문을 불러올 수 없습니다.</color>";
            }
        }

        if (string.IsNullOrEmpty(content) && _emptyStateRoot != null)
        {
            _emptyStateRoot.SetActive(true);
        }
    }

    private static string EscapeTmp(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return "";
        }

        return raw.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private void SetLoading(bool loading)
    {
        if (_loadingIndicator != null)
        {
            _loadingIndicator.SetActive(loading);
        }
    }

    private void UpdateBreadcrumb(CodexMetadata? data)
    {
        _breadcrumbPath.Clear();
        if (!data.HasValue)
        {
            if (_breadcrumbText != null)
            {
                _breadcrumbText.text = "ARCHÉ · 도서관";
            }

            return;
        }

        CodexMetadata m = data.Value;
        for (int i = 0; i < 9; i++)
        {
            string seg = CodexManager.GetLevelValue(m, i);
            if (!string.IsNullOrWhiteSpace(seg))
            {
                _breadcrumbPath.Add(seg.Trim());
            }
        }

        if (!string.IsNullOrWhiteSpace(m.Title))
        {
            _breadcrumbPath.Add(m.Title.Trim());
        }
        else if (!string.IsNullOrWhiteSpace(m.Id))
        {
            _breadcrumbPath.Add(m.Id.Trim());
        }

        if (_breadcrumbText != null)
        {
            _breadcrumbText.text = _breadcrumbPath.Count > 0
                ? string.Join(" > ", _breadcrumbPath)
                : "ARCHÉ · 도서관";
        }
    }

    private void ShowEmptyState()
    {
        _currentEntry = null;

        if (_breadcrumbText != null)
        {
            _breadcrumbText.text = "ARCHÉ · 도서관";
        }

        if (_bodyViewer != null)
        {
            _bodyViewer.text = "";
        }

        if (_emptyStateRoot != null)
        {
            _emptyStateRoot.SetActive(true);
        }

        if (_loadingIndicator != null)
        {
            _loadingIndicator.SetActive(false);
        }
    }

    private void OnExitClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.MainMenu);
        }
    }
}
