using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 10만 개 아이템 대응: 가벼운 ItemMetadata를 비동기 바이너리 로드하고,
/// 실제 ItemData(아이콘 등)는 UI 셀 Bind 시 LoadAsync로 비동기 로드합니다.
/// 우선순위: (1) StreamingAssets/ItemMetadataCatalog.bin + ItemMetadata/샤드(지연 로드) (2) 레거시 ItemMetadata.bin (3) 개발용 ItemManifest.
/// ItemData 비동기 로드: Addressables(주소=ItemId, ItemImporter 등록) 우선, 실패 시 Resources.LoadAsync 폴백.
/// Resources.LoadAll 은 사용하지 않습니다.
/// </summary>
/// <remarks>
/// <c>GSI_PRODUCT</c> 심볼이 켜진 G.S.I 단독 빌드에서는 로더를 만들지 않고 즉시 <see cref="IsInitialLoadComplete"/>만 true로 둡니다.
/// </remarks>
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

    /// <summary>메타데이터 색인 행 수(샤딩 카탈로그 또는 레거시 전체). 인트로 등에서 전체 순회 없이 개수만 쓸 때 사용.</summary>
    public int MetadataEntryCount
    {
        get
        {
            if (_metadataLoader != null && _metadataLoader.IsLoaded && _metadataLoader.Count > 0)
            {
                return _metadataLoader.Count;
            }

            return _itemCache.Count;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
#if GSI_PRODUCT
        // G.S.I 단독: StreamingAssets 아이템 DB·Addressables 사전 로드 없음
        IsInitialLoadComplete = true;
#else
        _metadataLoader = new ItemMetadataBinaryLoader();
#endif
    }

    private async void Start()
    {
#if GSI_PRODUCT
        return;
#else
        await AddressablesBootstrap.EnsureInitializedAsync();
        await _metadataLoader.LoadAsync();

        if (!UseMetadataOnly)
        {
            TryPopulateFromItemManifest();
        }

        IsInitialLoadComplete = true;
#endif
    }

    /// <summary>
    /// ItemMetadata.bin이 없을 때만: Resources/ItemManifest 를 한 번 로드해 캐시를 채웁니다.
    /// 에디터 메뉴 Tools → ARCHÉ → Items → Build Item Manifest 로 생성합니다.
    /// </summary>
    private void TryPopulateFromItemManifest()
    {
        var manifest = Resources.Load<ItemManifest>("ItemManifest");
        if (manifest == null || manifest.Items == null || manifest.Items.Count == 0)
        {
            Debug.LogError(
                "[ItemDatabase] 아이템 색인을 불러올 수 없습니다.\n" +
                "• 본 실행: StreamingAssets에 ItemMetadataCatalog.bin 및 ItemMetadata/ItemMetadata_shard_*.bin(또는 레거시 ItemMetadata.bin)이 있어야 합니다. 메뉴 「Tools → ARCHÉ → Items → Build Item Metadata Database」를 실행하세요.\n" +
                "• 개발(바이너리 없이): 「Tools → ARCHÉ → Items → Build Item Manifest」로 ItemManifest.asset 을 만든 뒤 Assets/Resources 에 두세요.");
            return;
        }

        foreach (ItemData item in manifest.Items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
            {
                continue;
            }

            _itemCache[item.ItemId] = item;
            _cacheOrder.Add(item.ItemId);
        }

#if UNITY_EDITOR
        Debug.Log($"[ItemDatabase] ItemMetadata.bin 없음 → ItemManifest에서 {manifest.Items.Count}개 ItemData 참조 로드(개발용).");
#endif
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
        LoadItemDataWithAddressablesFallback(itemId, path, onLoaded);
    }

    /// <summary>
    /// Addressables 주소(ItemId)로 ItemData 로드 후, 카탈로그에 없거나 실패하면 Resources 경로로 폴백합니다.
    /// </summary>
    private void LoadItemDataWithAddressablesFallback(string itemId, string resourcesPath, Action<ItemData> onLoaded)
    {
        AsyncOperationHandle<ItemData> handle = Addressables.LoadAssetAsync<ItemData>(itemId);
        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
            {
                AddToCache(itemId, op.Result);
                Addressables.Release(handle);
                onLoaded(op.Result);
                return;
            }

            Addressables.Release(handle);

            var resOp = Resources.LoadAsync<ItemData>(resourcesPath);
            resOp.completed += _ =>
            {
                var data = resOp.asset as ItemData;
                if (data != null)
                {
                    AddToCache(itemId, data);
                }

                onLoaded(data);
            };
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

        string trimmed = query.Trim();

        if (ids != null)
        {
            foreach (var id in ids)
            {
                var data = GetItem(id);
                if (data != null && ItemNameSearch.ContainsIgnoreCase(data.ItemName, trimmed))
                {
                    outResults.Add(id);
                }
            }
        }
        else
        {
            foreach (var kv in _itemCache)
            {
                if (kv.Value != null && ItemNameSearch.ContainsIgnoreCase(kv.Value.ItemName, trimmed))
                {
                    outResults.Add(kv.Key);
                }
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
