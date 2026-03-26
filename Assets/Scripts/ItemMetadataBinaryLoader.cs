using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// StreamingAssets/ItemMetadata.bin 에서 메타데이터를 비동기 로드합니다.
/// BinaryReader + Task.Run으로 메인 스레드 블로킹 없이 10만 개 로드.
/// Dictionary 용량 미리 할당으로 메모리 재할당 부하 감소.
/// </summary>
public sealed class ItemMetadataBinaryLoader
{
    private const string FileName = "ItemMetadata.bin";
    private const int EstimatedCapacity = 100000;

    private Dictionary<string, ItemMetadata> _byItemId;
    private List<ItemMetadata> _list;
    private bool _loaded;
    private bool _loading;
    private Task _loadTask;

    public bool IsLoaded => _loaded;
    public bool IsLoading => _loading;
    public int Count => _list?.Count ?? 0;

    /// <summary>
    /// 비동기 로드 시작. 완료 시 IsLoaded == true.
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
            }
            finally
            {
                _loading = false;
                _loaded = _byItemId != null;
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
        }
        finally
        {
            _loading = false;
            _loaded = _byItemId != null;
        }
    }

    private void LoadInternal()
    {
        string binPath = Path.Combine(Application.streamingAssetsPath, FileName);
        if (File.Exists(binPath))
        {
            LoadFromBinary(binPath);
            return;
        }

        string txtPath = Path.Combine(Application.streamingAssetsPath, "ItemMetadata.txt");
        if (File.Exists(txtPath))
        {
            LoadFromTextFallback(txtPath);
            return;
        }

        _byItemId = new Dictionary<string, ItemMetadata>(EstimatedCapacity, StringComparer.OrdinalIgnoreCase);
        _list = new List<ItemMetadata>(EstimatedCapacity);
    }

    private const uint BinaryMagic = 0x47534942; // "GSIB" - GSI Binary 시그니처

    private void LoadFromBinary(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);

        _byItemId = new Dictionary<string, ItemMetadata>(EstimatedCapacity, StringComparer.OrdinalIgnoreCase);
        _list = new List<ItemMetadata>(EstimatedCapacity);

        using (var ms = new MemoryStream(bytes))
        using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8))
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

                var meta = new ItemMetadata
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

                if (!string.IsNullOrWhiteSpace(meta.ItemId))
                {
                    _byItemId[meta.ItemId] = meta;
                    _list.Add(meta);
                }
            }
        }

        Debug.Log($"[ItemMetadataBinaryLoader] {_list.Count}개 메타데이터 로드 완료 (바이너리)");
    }

    private void LoadFromTextFallback(string path)
    {
        _byItemId = new Dictionary<string, ItemMetadata>(EstimatedCapacity, StringComparer.OrdinalIgnoreCase);
        _list = new List<ItemMetadata>(EstimatedCapacity);

        string[] lines = File.ReadAllLines(path);
        for (int i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            if (TryParseTextLine(lines[i], out ItemMetadata meta) && !string.IsNullOrWhiteSpace(meta.ItemId))
            {
                _byItemId[meta.ItemId] = meta;
                _list.Add(meta);
            }
        }

        Debug.Log($"[ItemMetadataBinaryLoader] {_list.Count}개 메타데이터 로드 완료 (텍스트 폴백)");
    }

    private static bool TryParseTextLine(string line, out ItemMetadata meta)
    {
        meta = default;
        string[] parts = line.Split('\t');
        if (parts.Length < 7) return false;

        if (!int.TryParse(parts[3], out int main)) main = 0;
        if (!int.TryParse(parts[4], out int middle)) middle = 0;
        if (!int.TryParse(parts[5], out int sub)) sub = 0;
        if (!int.TryParse(parts[6], out int tier)) tier = 0;

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
        if (!_loaded || _byItemId == null)
        {
            return null;
        }
        return _byItemId.TryGetValue(itemId, out var m) ? m : (ItemMetadata?)null;
    }

    public IEnumerable<ItemMetadata> GetAllMetadata()
    {
        if (!_loaded || _list == null)
        {
            return Array.Empty<ItemMetadata>();
        }
        return _list;
    }

    public void SearchByItemName(string query, IReadOnlyList<string> ids, List<string> outResults)
    {
        outResults.Clear();
        if (!_loaded || _byItemId == null) return;

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
        if (ids != null)
        {
            foreach (var id in ids)
            {
                if (_byItemId.TryGetValue(id, out var meta) &&
                    (meta.ItemName ?? "").ToLowerInvariant().Contains(lowerQuery))
                {
                    outResults.Add(id);
                }
            }
        }
        else
        {
            foreach (var kv in _byItemId)
            {
                if ((kv.Value.ItemName ?? "").ToLowerInvariant().Contains(lowerQuery))
                {
                    outResults.Add(kv.Key);
                }
            }
        }
    }
}
