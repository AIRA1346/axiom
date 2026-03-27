using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 10만 개 아이템 대응: 가벼운 ItemMetadata를 비동기 바이너리 로드하고,
/// 실제 ItemData(아이콘 등)는 UI 셀 Bind 시 LoadAsync로 비동기 로드합니다.
/// ItemMetadata.bin(StreamingAssets) 우선, 없으면 LoadAll 폴백.
/// </summary>
public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    private ItemMetadataBinaryLoader _metadataLoader;
    private readonly Dictionary<string, ItemData> _itemCache = new Dictionary<string, ItemData>(200, StringComparer.OrdinalIgnoreCase);
    private const int MaxCacheSize = 200;
    private readonly List<string> _cacheOrder = new List<string>(MaxCacheSize);

    public bool UseMetadataOnly => _metadataLoader != null && _metadataLoader.IsLoaded && _metadataLoader.Count > 0;
    public bool IsMetadataLoading => _metadataLoader?.IsLoading ?? false;
    public bool IsInitialLoadComplete { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _metadataLoader = new ItemMetadataBinaryLoader();
    }

    private async void Start()
    {
        await _metadataLoader.LoadAsync();

        if (!UseMetadataOnly)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[ItemDatabase] ItemMetadata.bin 없음. 기존 LoadAll 방식으로 폴백합니다.");
#endif
            LoadAllItemsFallback();
        }

        IsInitialLoadComplete = true;
    }

    private void LoadAllItemsFallback()
    {
        ItemData[] loadedItems = Resources.LoadAll<ItemData>("items");
        foreach (ItemData item in loadedItems)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.ItemId))
            {
                _itemCache[item.ItemId] = item;
                _cacheOrder.Add(item.ItemId);
            }
        }
    }

    /// <summary>
    /// 메타데이터만 있으면 즉시 반환. 없으면 null (비동기 로드 필요).
    /// </summary>
    public ItemData GetItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        if (_itemCache.TryGetValue(itemId, out ItemData cached))
        {
            return cached;
        }

        return null;
    }

    /// <summary>
    /// 가벼운 메타데이터 반환. Metadata 모드에서만 유효.
    /// </summary>
    public ItemMetadata? GetMetadata(string itemId)
    {
        if (_metadataLoader != null && UseMetadataOnly)
        {
            return _metadataLoader.GetMetadata(itemId);
        }

        var data = GetItem(itemId);
        if (data != null)
        {
            return new ItemMetadata
            {
                ItemId = data.ItemId,
                ItemName = data.ItemName,
                MainCategory = data.MainCategory,
                MiddleCategory = data.MiddleCategory,
                SubCategory = data.SubCategory,
                Tier = data.Tier,
                ResourcePath = $"items/{data.MainCategory}/{data.MiddleCategory}/{data.SubCategory}/{data.Tier}/{data.ItemId}",
                PurchasePrice = data.PurchasePrice,
                SalePrice = data.SalePrice
            };
        }

        return null;
    }

    /// <summary>
    /// UI 셀 Bind 시 호출. 아이콘·상세 데이터를 비동기 로드하고 콜백으로 반환.
    /// </summary>
    public void LoadItemDataAsync(string itemId, Action<ItemData> onLoaded)
    {
        if (string.IsNullOrWhiteSpace(itemId) || onLoaded == null)
        {
            return;
        }

        if (_itemCache.TryGetValue(itemId, out ItemData cached))
        {
            onLoaded(cached);
            return;
        }

        if (!UseMetadataOnly)
        {
            onLoaded(GetItem(itemId));
            return;
        }

        var meta = _metadataLoader.GetMetadata(itemId);
        if (!meta.HasValue)
        {
            onLoaded(null);
            return;
        }

        string path = meta.Value.GetResourcePath();
        var op = Resources.LoadAsync<ItemData>(path);

        op.completed += _ =>
        {
            var data = op.asset as ItemData;
            if (data != null)
            {
                AddToCache(itemId, data);
            }

            onLoaded(data);
        };
    }

    private void AddToCache(string itemId, ItemData data)
    {
        if (_itemCache.ContainsKey(itemId))
        {
            return;
        }

        while (_cacheOrder.Count >= MaxCacheSize && _cacheOrder.Count > 0)
        {
            string oldest = _cacheOrder[0];
            _cacheOrder.RemoveAt(0);
            _itemCache.Remove(oldest);
        }

        _itemCache[itemId] = data;
        _cacheOrder.Add(itemId);
    }

    public IEnumerable<ItemData> GetAllItems()
    {
        if (!UseMetadataOnly)
        {
            return _itemCache.Values;
        }

        return null;
    }

    public IEnumerable<ItemMetadata> GetAllMetadata()
    {
        if (_metadataLoader != null && UseMetadataOnly)
        {
            return _metadataLoader.GetAllMetadata();
        }

        return null;
    }

    /// <summary>
    /// ItemName으로 검색. ids가 null이면 전체에서 검색.
    /// </summary>
    public void SearchByItemName(string query, IReadOnlyList<string> ids, List<string> outResults)
    {
        outResults.Clear();

        if (_metadataLoader != null && UseMetadataOnly)
        {
            _metadataLoader.SearchByItemName(query, ids, outResults);
            return;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            if (ids != null)
            {
                foreach (var id in ids)
                {
                    outResults.Add(id);
                }
            }
            return;
        }

        string lowerQuery = query.Trim().ToLowerInvariant();
        var searchIn = ids;
        if (searchIn == null)
        {
            var all = new List<string>();
            foreach (var kv in _itemCache)
            {
                all.Add(kv.Key);
            }
            searchIn = all;
        }

        foreach (var id in searchIn)
        {
            var data = GetItem(id);
            if (data != null && (data.ItemName ?? "").ToLowerInvariant().Contains(lowerQuery))
            {
                outResults.Add(id);
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
