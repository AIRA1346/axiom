using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// StreamingAssets에서 아이템 메타데이터를 로드합니다.
/// <para><b>샤딩 모드(권장)</b>: ItemMetadataCatalog.bin(ItemId→대분류)만 먼저 RAM에 올리고,
/// GetMetadata 등으로 해당 대분류가 필요할 때 ItemMetadata/ItemMetadata_shard_XX.bin 한 덩어리만 로드합니다.</para>
/// <para><b>레거시</b>: ItemMetadata.bin 단일 파일 전체 로드(기존 프로젝트 호환).</para>
/// </summary>
public sealed class ItemMetadataBinaryLoader
{
    private const string LegacyFileName = "ItemMetadata.bin";
    private const string CatalogFileName = "ItemMetadataCatalog.bin";
    private const string ShardSubfolder = "ItemMetadata";
    private const string ShardFilePrefix = "ItemMetadata_shard_";
    private const int EstimatedCapacity = 65536;

    private const uint BinaryMagic = 0x47534942; // "GSIB"

    private const uint CatalogMagic = 0x5441434D; // "MCAT" (little-endian uint)

    private const int CatalogFormatVersion = 1;

    /// <summary>레거시: 단일 Dictionary + 리스트. 샤딩: 사용 안 함.</summary>
    private Dictionary<string, ItemMetadata> _byItemId;

    private List<ItemMetadata> _list;

    /// <summary>샤딩: ItemId → 대분류(enum 정수). 이름·경로 없이 샤드 선택만.</summary>
    private Dictionary<string, int> _catalogItemToMain;

    private int _catalogCount;

    /// <summary>샤딩: 이미 로드된 샤드(대분류 → Id→메타).</summary>
    private readonly Dictionary<int, Dictionary<string, ItemMetadata>> _shardCaches =
        new Dictionary<int, Dictionary<string, ItemMetadata>>();

    /// <summary>샤드 접근 순서(LRU). 용량 초과 시 가장 오래된 대분류 샤드만 메모리에서 제거(디스크는 그대로).</summary>
    private readonly LinkedList<int> _shardLru = new LinkedList<int>();

    private readonly Dictionary<int, LinkedListNode<int>> _shardLruNodes = new Dictionary<int, LinkedListNode<int>>();

    private bool _shardedMode;
    private bool _loaded;
    private bool _loading;
    private Task _loadTask;

    public bool IsLoaded => _loaded;
    public bool IsLoading => _loading;

    /// <summary>
    /// 샤딩 모드에서 메모리에 동시에 유지할 최대 대분류(샤드) 개수. 0 이하면 LRU 비활성(기존처럼 적재된 샤드 유지).
    /// 게임 시작 전에 변경하는 것을 권장합니다.
    /// </summary>
    public static int ShardCacheCapacity { get; set; } = 16;

    /// <summary>메타 항목 수(샤딩 시 카탈로그 행 수, 레거시 시 파싱된 행 수).</summary>
    public int Count => _shardedMode ? _catalogCount : (_list?.Count ?? 0);

    /// <summary>
    /// 비동기 로드 시작. 샤딩이면 카탈로그만 읽고 완료; 레거시면 전체 파싱.
    /// </summary>
    public Task LoadAsync()
    {
        if (_loaded || _loading)
        {
            return _loadTask ?? Task.CompletedTask;
        }

        _loading = true;
        _loadTask = Task.Run(() =>
        {
            try
            {
                LoadInternal();
                _loaded = true;
            }
            finally
            {
                _loading = false;
            }
        });

        return _loadTask;
    }

    /// <summary>
    /// 블로킹 로드 (에디터/테스트용). 평문 .txt도 시도 후 폴백.
    /// </summary>
    public void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        if (_loading)
        {
            _loadTask?.GetAwaiter().GetResult();
            return;
        }

        _loading = true;
        try
        {
            LoadInternal();
            _loaded = true;
        }
        finally
        {
            _loading = false;
        }
    }

    private void LoadInternal()
    {
        string catalogPath = Path.Combine(Application.streamingAssetsPath, CatalogFileName);
        if (File.Exists(catalogPath))
        {
            LoadCatalogOnly(catalogPath);
            _shardedMode = true;
            return;
        }

        string binPath = Path.Combine(Application.streamingAssetsPath, LegacyFileName);
        if (File.Exists(binPath))
        {
            LoadFromBinaryLegacy(binPath);
            _shardedMode = false;
            return;
        }

        string txtPath = Path.Combine(Application.streamingAssetsPath, "ItemMetadata.txt");
        if (File.Exists(txtPath))
        {
            LoadFromTextFallback(txtPath);
            _shardedMode = false;
            return;
        }

        _byItemId = new Dictionary<string, ItemMetadata>(EstimatedCapacity, StringComparer.OrdinalIgnoreCase);
        _list = new List<ItemMetadata>(EstimatedCapacity);
        _shardedMode = false;
    }

    private void LoadCatalogOnly(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        using (var ms = new MemoryStream(bytes))
        using (var reader = new BinaryReader(ms, Encoding.UTF8))
        {
            uint magic = reader.ReadUInt32();
            if (magic != CatalogMagic)
            {
                throw new InvalidDataException($"[ItemMetadataBinaryLoader] 카탈로그 시그니처 불일치: {path}");
            }

            int version = reader.ReadInt32();
            if (version != CatalogFormatVersion)
            {
                throw new InvalidDataException($"[ItemMetadataBinaryLoader] 지원하지 않는 카탈로그 버전 {version}: {path}");
            }

            int count = reader.ReadInt32();
            _catalogCount = count;
            _catalogItemToMain = new Dictionary<string, int>(Math.Max(count, 16), StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < count; i++)
            {
                string itemId = reader.ReadString();
                int main = reader.ReadByte();
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    continue;
                }

                _catalogItemToMain[itemId] = main;
            }
        }

#if UNITY_EDITOR
        Debug.Log($"[ItemMetadataBinaryLoader] 샤딩 카탈로그 로드: {_catalogCount}개 (메타 본문은 샤드 지연 로드)");
#endif
    }

    private static string GetShardPath(int mainCategory)
    {
        return Path.Combine(Application.streamingAssetsPath, ShardSubfolder, $"{ShardFilePrefix}{mainCategory:D2}.bin");
    }

    private void EnsureShardLoaded(int mainCategory)
    {
        if (_shardCaches.TryGetValue(mainCategory, out _))
        {
            TouchShardLru(mainCategory);
            return;
        }

        EvictOldestShardsIfNeeded();

        string path = GetShardPath(mainCategory);
        if (!File.Exists(path))
        {
            _shardCaches[mainCategory] = new Dictionary<string, ItemMetadata>(0, StringComparer.OrdinalIgnoreCase);
            RegisterShardInLru(mainCategory);
#if UNITY_EDITOR
            Debug.LogWarning($"[ItemMetadataBinaryLoader] 샤드 파일 없음: {path}");
#endif
            return;
        }

        var dict = new Dictionary<string, ItemMetadata>(EstimatedCapacity, StringComparer.OrdinalIgnoreCase);
        byte[] bytes = File.ReadAllBytes(path);
        using (var ms = new MemoryStream(bytes))
        using (var reader = new BinaryReader(ms, Encoding.UTF8))
        {
            uint first = reader.ReadUInt32();
            int version = 1;
            int rowCount;

            if (first == BinaryMagic)
            {
                version = reader.ReadInt32();
                rowCount = reader.ReadInt32();
            }
            else
            {
                rowCount = (int)first;
            }

            for (int i = 0; i < rowCount; i++)
            {
                ItemMetadata meta = ReadMetadataRow(reader, version);
                if (!string.IsNullOrWhiteSpace(meta.ItemId))
                {
                    dict[meta.ItemId] = meta;
                }
            }
        }

        _shardCaches[mainCategory] = dict;
        RegisterShardInLru(mainCategory);
    }

    private void TouchShardLru(int mainCategory)
    {
        if (!_shardLruNodes.TryGetValue(mainCategory, out LinkedListNode<int> node))
        {
            RegisterShardInLru(mainCategory);
            return;
        }

        _shardLru.Remove(node);
        _shardLruNodes[mainCategory] = _shardLru.AddLast(mainCategory);
    }

    private void RegisterShardInLru(int mainCategory)
    {
        if (_shardLruNodes.ContainsKey(mainCategory))
        {
            TouchShardLru(mainCategory);
            return;
        }

        _shardLruNodes[mainCategory] = _shardLru.AddLast(mainCategory);
    }

    /// <summary>캐시 상한을 넘기면 LRU 순으로 샤드 딕셔너리만 제거합니다.</summary>
    private void EvictOldestShardsIfNeeded()
    {
        int cap = ShardCacheCapacity;
        if (cap <= 0)
        {
            return;
        }

        while (_shardCaches.Count >= cap && _shardLru.First != null)
        {
            LinkedListNode<int> first = _shardLru.First;
            int victim = first.Value;
            _shardLru.Remove(first);
            _shardLruNodes.Remove(victim);
            _shardCaches.Remove(victim);
        }
    }

    private void LoadFromBinaryLegacy(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);

        _byItemId = new Dictionary<string, ItemMetadata>(EstimatedCapacity, StringComparer.OrdinalIgnoreCase);
        _list = new List<ItemMetadata>(EstimatedCapacity);

        using (var ms = new MemoryStream(bytes))
        using (var reader = new BinaryReader(ms, Encoding.UTF8))
        {
            uint first = reader.ReadUInt32();
            int version = 1;
            int count;

            if (first == BinaryMagic)
            {
                version = reader.ReadInt32();
                count = reader.ReadInt32();
            }
            else
            {
                count = (int)first;
            }

            for (int i = 0; i < count; i++)
            {
                ItemMetadata meta = ReadMetadataRow(reader, version);
                if (!string.IsNullOrWhiteSpace(meta.ItemId))
                {
                    _byItemId[meta.ItemId] = meta;
                    _list.Add(meta);
                }
            }
        }

#if UNITY_EDITOR
        Debug.Log($"[ItemMetadataBinaryLoader] 레거시 단일 바이너리 로드: {_list.Count}개");
#endif
    }

    private static ItemMetadata ReadMetadataRow(BinaryReader reader, int version)
    {
        string itemId = reader.ReadString();
        string itemName = reader.ReadString();
        int main = reader.ReadInt32();
        int middle = reader.ReadInt32();
        int sub = reader.ReadInt32();
        int tier = reader.ReadInt32();
        string resourcePath = reader.ReadString();
        int purchasePrice = 0;
        int salePrice = 0;
        if (version >= 2)
        {
            purchasePrice = reader.ReadInt32();
            salePrice = reader.ReadInt32();
        }

        return new ItemMetadata
        {
            ItemId = itemId,
            ItemName = itemName,
            MainCategory = (ItemMainCategory)Mathf.Clamp(main, 0, (int)ItemMainCategory.SpecialSystem),
            MiddleCategory = (ItemMiddleCategory)Mathf.Clamp(middle, 0, (int)ItemMiddleCategory.SystemOnly),
            SubCategory = (ItemSubCategory)Mathf.Clamp(sub, 0, (int)ItemSubCategory.TutorialItem),
            Tier = (ItemTier)Mathf.Clamp(tier, 0, (int)ItemTier.Tier10),
            ResourcePath = resourcePath,
            PurchasePrice = purchasePrice,
            SalePrice = salePrice
        };
    }

    private void LoadFromTextFallback(string path)
    {
        _byItemId = new Dictionary<string, ItemMetadata>(EstimatedCapacity, StringComparer.OrdinalIgnoreCase);
        _list = new List<ItemMetadata>(EstimatedCapacity);

        string[] lines = File.ReadAllLines(path);
        for (int i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            if (TryParseTextLine(lines[i], out ItemMetadata meta) && !string.IsNullOrWhiteSpace(meta.ItemId))
            {
                _byItemId[meta.ItemId] = meta;
                _list.Add(meta);
            }
        }

#if UNITY_EDITOR
        Debug.Log($"[ItemMetadataBinaryLoader] {_list.Count}개 메타데이터 로드 완료 (텍스트 폴백)");
#endif
    }

    private static bool TryParseTextLine(string line, out ItemMetadata meta)
    {
        meta = default;
        string[] parts = line.Split('\t');
        if (parts.Length < 7)
        {
            return false;
        }

        if (!int.TryParse(parts[3], out int main))
        {
            main = 0;
        }

        if (!int.TryParse(parts[4], out int middle))
        {
            middle = 0;
        }

        if (!int.TryParse(parts[5], out int sub))
        {
            sub = 0;
        }

        if (!int.TryParse(parts[6], out int tier))
        {
            tier = 0;
        }

        meta = new ItemMetadata
        {
            ItemId = parts[0],
            ItemName = (parts[1] ?? "").Replace("\\n", "\n").Replace("\\t", "\t"),
            ResourcePath = parts[2],
            MainCategory = (ItemMainCategory)Mathf.Clamp(main, 0, (int)ItemMainCategory.SpecialSystem),
            MiddleCategory = (ItemMiddleCategory)Mathf.Clamp(middle, 0, (int)ItemMiddleCategory.SystemOnly),
            SubCategory = (ItemSubCategory)Mathf.Clamp(sub, 0, (int)ItemSubCategory.TutorialItem),
            Tier = (ItemTier)Mathf.Clamp(tier, 0, (int)ItemTier.Tier10)
        };
        return true;
    }

    public ItemMetadata? GetMetadata(string itemId)
    {
        if (!_loaded || string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        if (_shardedMode)
        {
            if (_catalogItemToMain == null || !_catalogItemToMain.TryGetValue(itemId, out int main))
            {
                return null;
            }

            EnsureShardLoaded(main);
            if (_shardCaches.TryGetValue(main, out var dict) && dict.TryGetValue(itemId, out var m))
            {
                return m;
            }

            return null;
        }

        if (_byItemId != null && _byItemId.TryGetValue(itemId, out var meta))
        {
            return meta;
        }

        return null;
    }

    public IEnumerable<ItemMetadata> GetAllMetadata()
    {
        if (!_loaded)
        {
            return Array.Empty<ItemMetadata>();
        }

        if (_shardedMode)
        {
            return EnumerateAllShardedMetadata();
        }

        if (_list != null)
        {
            return _list;
        }

        return Array.Empty<ItemMetadata>();
    }

    /// <summary>샤딩: 디스크에서 샤드 순회(전체 메타를 RAM에 한꺼번에 올리지 않음).</summary>
    private IEnumerable<ItemMetadata> EnumerateAllShardedMetadata()
    {
        int maxMain = (int)ItemMainCategory.SpecialSystem;
        for (int main = 0; main <= maxMain; main++)
        {
            string path = GetShardPath(main);
            if (!File.Exists(path))
            {
                continue;
            }

            foreach (ItemMetadata m in ReadShardRowsFromDisk(path))
            {
                yield return m;
            }
        }
    }

    private static IEnumerable<ItemMetadata> ReadShardRowsFromDisk(string path)
    {
        using (var fs = File.OpenRead(path))
        using (var reader = new BinaryReader(fs, Encoding.UTF8))
        {
            uint first = reader.ReadUInt32();
            int version = 1;
            int count;

            if (first == BinaryMagic)
            {
                version = reader.ReadInt32();
                count = reader.ReadInt32();
            }
            else
            {
                count = (int)first;
            }

            for (int i = 0; i < count; i++)
            {
                ItemMetadata meta = ReadMetadataRow(reader, version);
                if (!string.IsNullOrWhiteSpace(meta.ItemId))
                {
                    yield return meta;
                }
            }
        }
    }

    public void SearchByItemName(string query, IReadOnlyList<string> ids, List<string> outResults)
    {
        outResults.Clear();
        if (!_loaded)
        {
            return;
        }

        if (_shardedMode)
        {
            SearchByItemNameSharded(query, ids, outResults);
            return;
        }

        if (_byItemId == null)
        {
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
                if (_byItemId.TryGetValue(id, out var meta) &&
                    ItemNameSearch.ContainsIgnoreCase(meta.ItemName, trimmed))
                {
                    outResults.Add(id);
                }
            }
        }
        else
        {
            foreach (var kv in _byItemId)
            {
                if (ItemNameSearch.ContainsIgnoreCase(kv.Value.ItemName, trimmed))
                {
                    outResults.Add(kv.Key);
                }
            }
        }
    }

    private void SearchByItemNameSharded(string query, IReadOnlyList<string> ids, List<string> outResults)
    {
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
                if (!_catalogItemToMain.TryGetValue(id, out int main))
                {
                    continue;
                }

                EnsureShardLoaded(main);
                if (_shardCaches.TryGetValue(main, out var dict) &&
                    dict.TryGetValue(id, out var meta) &&
                    ItemNameSearch.ContainsIgnoreCase(meta.ItemName, trimmed))
                {
                    outResults.Add(id);
                }
            }

            return;
        }

        // 전체 검색: 샤드 파일을 순회하며 스트리밍(전 샤드 RAM 적재 없음)
        foreach (ItemMetadata meta in EnumerateAllShardedMetadata())
        {
            if (ItemNameSearch.ContainsIgnoreCase(meta.ItemName, trimmed))
            {
                outResults.Add(meta.ItemId);
            }
        }
    }
}
