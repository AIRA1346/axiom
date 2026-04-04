using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 10만 개 아이템 대응 영구 도감(Encyclopedia) 시스템.
/// HashSet으로 해금된 ItemId를 관리하고, ItemDatabase를 통해 아이템을 조회합니다.
/// Resources.Load를 사용하지 않고 ItemDatabase(ScriptableObject 리스트) 참조를 사용합니다.
/// CategoryIndex: [Main/Middle/Sub/Tier] 경로별 ItemId 리스트를 즉시 반환합니다.
/// </summary>
public sealed class EncyclopediaManager : MonoBehaviour
{
    private const string SaveFileName = "EncyclopediaData.json";

    public static EncyclopediaManager Instance { get; private set; }

    private readonly HashSet<string> _unlockedItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _categoryIndex = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    private bool _indexBuilt;
    private string _savePath;
    private bool _isDirty;

    public IReadOnlyCollection<string> UnlockedItemIds => _unlockedItemIds;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        Load();
    }

    private void Start()
    {
        BuildCategoryIndex();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && _isDirty)
        {
            Save();
        }
    }

    private void OnApplicationQuit()
    {
        if (_isDirty)
        {
            Save();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (_isDirty)
            {
                Save();
            }
            Instance = null;
        }
    }

    /// <summary>
    /// 아이템 획득 시 호출하여 도감에 영구 등록합니다.
    /// </summary>
    /// <param name="itemId">등록할 아이템의 ItemId</param>
    /// <returns>새로 해금된 경우 true, 이미 해금된 경우 false</returns>
    public bool UnlockItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        if (_unlockedItemIds.Add(itemId))
        {
            _isDirty = true;
            Save();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 해당 ItemId가 도감에 해금되어 있는지 확인합니다.
    /// </summary>
    public bool IsUnlocked(string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId) && _unlockedItemIds.Contains(itemId);
    }

    /// <summary>
    /// 특정 [대/중/소/티어] 경로에 속한 아이템 중 해금된 개수와 전체 개수를 반환합니다.
    /// UI에서 "15/100 완료" 등의 표시에 사용합니다.
    /// </summary>
    /// <param name="path">카테고리 경로 (null 필드 = 해당 레벨에서 전체)</param>
    /// <returns>(해금된 개수, 전체 개수)</returns>
    public (int unlocked, int total) GetCategoryProgress(EncyclopediaCategoryPath path)
    {
        var db = ItemDatabase.Instance;
        if (db == null)
        {
            return (0, 0);
        }

        EnsureCategoryIndexBuilt();

        if (TryGetCategoryProgressFromIndex(path, out int unlockedFromIndex, out int totalFromIndex))
        {
            return (unlockedFromIndex, totalFromIndex);
        }

        int total = 0;
        int unlocked = 0;

        var metadataEnum = db.GetAllMetadata();
        if (metadataEnum != null)
        {
            foreach (var meta in metadataEnum)
            {
                if (string.IsNullOrWhiteSpace(meta.ItemId))
                {
                    continue;
                }

                if (!MatchesPath(meta, path))
                {
                    continue;
                }

                total++;
                if (_unlockedItemIds.Contains(meta.ItemId))
                {
                    unlocked++;
                }
            }
        }
        else
        {
            foreach (ItemData item in db.GetAllItems() ?? System.Array.Empty<ItemData>())
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    continue;
                }

                if (!MatchesPath(item, path))
                {
                    continue;
                }

                total++;
                if (_unlockedItemIds.Contains(item.ItemId))
                {
                    unlocked++;
                }
            }
        }

        return (unlocked, total);
    }

    /// <summary>
    /// CategoryIndex 버킷이 <see cref="MatchesPath"/>와 동일한 집합일 때만 O(목록 크기)로 진행률을 계산합니다.
    /// (버킷 "None"은 모든 아이템이 한 번씩 들어가며, MainCategory.None만 필터하는 진행률과 다릅니다.)
    /// </summary>
    private bool TryGetCategoryProgressFromIndex(EncyclopediaCategoryPath path, out int unlocked, out int total)
    {
        unlocked = 0;
        total = 0;

        if (!CanUseCategoryIndexForProgress(path))
        {
            return false;
        }

        string key = ToCategoryIndexKey(
            path.Main,
            path.Middle ?? ItemMiddleCategory.None,
            path.Sub ?? ItemSubCategory.None,
            path.Tier);

        if (!_categoryIndex.TryGetValue(key, out List<string> list) || list == null)
        {
            return false;
        }

        total = list.Count;
        for (int i = 0; i < list.Count; i++)
        {
            if (_unlockedItemIds.Contains(list[i]))
            {
                unlocked++;
            }
        }

        return true;
    }

    /// <summary>
    /// 인덱스 키가 MatchesPath 필터와 일치하는 경우에만 true.
    /// </summary>
    private static bool CanUseCategoryIndexForProgress(EncyclopediaCategoryPath path)
    {
        // 버킷 "None": 모든 아이템이 들어감. MainCategory.None만 세는 진행률과 다름 → 전수 스캔.
        if (path.Main == ItemMainCategory.None &&
            !path.Middle.HasValue &&
            !path.Sub.HasValue &&
            !path.Tier.HasValue)
        {
            return false;
        }

        // ByTier_* 는 티어 기준 전체인데, Main=None+Tier 경로의 MatchesPath는 MainCategory.None만 포함 → 전수 스캔.
        if (path.Main == ItemMainCategory.None && path.Tier.HasValue)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 대분류만 지정한 경로로 진행률을 조회합니다.
    /// </summary>
    public (int unlocked, int total) GetCategoryProgress(ItemMainCategory main)
    {
        return GetCategoryProgress(new EncyclopediaCategoryPath
        {
            Main = main,
            Middle = null,
            Sub = null,
            Tier = null
        });
    }

    /// <summary>
    /// 대분류+중분류 경로로 진행률을 조회합니다.
    /// </summary>
    public (int unlocked, int total) GetCategoryProgress(ItemMainCategory main, ItemMiddleCategory middle)
    {
        return GetCategoryProgress(new EncyclopediaCategoryPath
        {
            Main = main,
            Middle = middle,
            Sub = null,
            Tier = null
        });
    }

    /// <summary>
    /// 대분류+중분류+소분류 경로로 진행률을 조회합니다.
    /// </summary>
    public (int unlocked, int total) GetCategoryProgress(
        ItemMainCategory main,
        ItemMiddleCategory middle,
        ItemSubCategory sub)
    {
        return GetCategoryProgress(new EncyclopediaCategoryPath
        {
            Main = main,
            Middle = middle,
            Sub = sub,
            Tier = null
        });
    }

    /// <summary>
    /// 대분류+중분류+소분류+티어 전체 경로로 진행률을 조회합니다.
    /// </summary>
    public (int unlocked, int total) GetCategoryProgress(
        ItemMainCategory main,
        ItemMiddleCategory middle,
        ItemSubCategory sub,
        ItemTier tier)
    {
        return GetCategoryProgress(new EncyclopediaCategoryPath
        {
            Main = main,
            Middle = middle,
            Sub = sub,
            Tier = tier
        });
    }

    /// <summary>
    /// CategoryIndex: 특정 [Main/Middle/Sub/Tier] 경로에 해당하는 ItemId 리스트를 즉시 반환합니다.
    /// 도감 켤 때마다 10만 개 전수 조사 없이 O(1) 조회가 가능합니다.
    /// </summary>
    /// <param name="path">카테고리 경로</param>
    /// <returns>해당 경로의 ItemId 리스트 (읽기 전용, 없으면 빈 리스트)</returns>
    public IReadOnlyList<string> GetItemIdsForPath(EncyclopediaCategoryPath path)
    {
        EnsureCategoryIndexBuilt();

        string key;
        if (path.Main == ItemMainCategory.None && path.Tier.HasValue)
        {
            key = "ByTier_" + path.Tier.Value;
        }
        else
        {
            key = ToCategoryIndexKey(path);
        }

        return _categoryIndex.TryGetValue(key, out List<string> list) ? list : _emptyList;
    }

    /// <summary>
    /// 대분류만 지정한 경로의 ItemId 리스트.
    /// </summary>
    public IReadOnlyList<string> GetItemIdsForPath(ItemMainCategory main)
    {
        return GetItemIdsForPath(new EncyclopediaCategoryPath { Main = main, Middle = null, Sub = null, Tier = null });
    }

    /// <summary>
    /// 대분류+중분류 경로의 ItemId 리스트.
    /// </summary>
    public IReadOnlyList<string> GetItemIdsForPath(ItemMainCategory main, ItemMiddleCategory middle)
    {
        return GetItemIdsForPath(new EncyclopediaCategoryPath { Main = main, Middle = middle, Sub = null, Tier = null });
    }

    /// <summary>
    /// 대분류+중분류+소분류 경로의 ItemId 리스트.
    /// </summary>
    public IReadOnlyList<string> GetItemIdsForPath(ItemMainCategory main, ItemMiddleCategory middle, ItemSubCategory sub)
    {
        return GetItemIdsForPath(new EncyclopediaCategoryPath { Main = main, Middle = middle, Sub = sub, Tier = null });
    }

    /// <summary>
    /// 대분류+중분류+소분류+티어 전체 경로의 ItemId 리스트.
    /// </summary>
    public IReadOnlyList<string> GetItemIdsForPath(ItemMainCategory main, ItemMiddleCategory middle, ItemSubCategory sub, ItemTier tier)
    {
        return GetItemIdsForPath(new EncyclopediaCategoryPath { Main = main, Middle = middle, Sub = sub, Tier = tier });
    }

    private static readonly List<string> _emptyList = new List<string>();

    private void EnsureCategoryIndexBuilt()
    {
        if (_indexBuilt)
        {
            return;
        }

        BuildCategoryIndex();
    }

    /// <summary>
    /// 인덱스가 비어 있으면 강제로 재빌드합니다. ItemDatabase 로딩 지연 대응.
    /// </summary>
    public void RebuildIndexIfEmpty()
    {
        if (_categoryIndex.Count > 0)
        {
            return;
        }

        _indexBuilt = false;
        BuildCategoryIndex();
    }

    private void BuildCategoryIndex()
    {
        var db = ItemDatabase.Instance;
        if (db == null)
        {
            return;
        }

        _categoryIndex.Clear();

        var metadataEnum = db.GetAllMetadata();
        if (metadataEnum != null)
        {
            foreach (var meta in metadataEnum)
            {
                if (string.IsNullOrWhiteSpace(meta.ItemId))
                {
                    continue;
                }

                AddToIndex(meta.ItemId, ItemMainCategory.None, ItemMiddleCategory.None, ItemSubCategory.None, null);
                AddToIndex(meta.ItemId, meta.MainCategory, ItemMiddleCategory.None, ItemSubCategory.None, null);
                AddToIndex(meta.ItemId, meta.MainCategory, meta.MiddleCategory, ItemSubCategory.None, null);
                AddToIndex(meta.ItemId, meta.MainCategory, meta.MiddleCategory, meta.SubCategory, null);
                AddToIndex(meta.ItemId, meta.MainCategory, meta.MiddleCategory, meta.SubCategory, meta.Tier);
                AddToTierIndex(meta.ItemId, meta.Tier);
            }
        }
        else
        {
            foreach (ItemData item in db.GetAllItems() ?? System.Array.Empty<ItemData>())
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    continue;
                }

                AddToIndex(item.ItemId, ItemMainCategory.None, ItemMiddleCategory.None, ItemSubCategory.None, null);
                AddToIndex(item.ItemId, item.MainCategory, ItemMiddleCategory.None, ItemSubCategory.None, null);
                AddToIndex(item.ItemId, item.MainCategory, item.MiddleCategory, ItemSubCategory.None, null);
                AddToIndex(item.ItemId, item.MainCategory, item.MiddleCategory, item.SubCategory, null);
                AddToIndex(item.ItemId, item.MainCategory, item.MiddleCategory, item.SubCategory, item.Tier);
                AddToTierIndex(item.ItemId, item.Tier);
            }
        }

        _indexBuilt = true;
    }

    private void AddToTierIndex(string itemId, ItemTier tier)
    {
        string key = "ByTier_" + tier;
        if (!_categoryIndex.TryGetValue(key, out var list))
        {
            list = new List<string>(4096);
            _categoryIndex[key] = list;
        }
        list.Add(itemId);
    }

    private void AddToIndex(string itemId, ItemMainCategory main, ItemMiddleCategory middle, ItemSubCategory sub, ItemTier? tier)
    {
        string key = ToCategoryIndexKey(main, middle, sub, tier);

        if (!_categoryIndex.TryGetValue(key, out List<string> list))
        {
            int capacity = (main == ItemMainCategory.None && middle == ItemMiddleCategory.None && sub == ItemSubCategory.None && !tier.HasValue)
                ? 65536
                : 4096;
            list = new List<string>(capacity);
            _categoryIndex[key] = list;
        }

        list.Add(itemId);
    }

    private static string ToCategoryIndexKey(EncyclopediaCategoryPath path)
    {
        return ToCategoryIndexKey(path.Main, path.Middle ?? ItemMiddleCategory.None, path.Sub ?? ItemSubCategory.None, path.Tier);
    }

    private static string ToCategoryIndexKey(ItemMainCategory main, ItemMiddleCategory middle, ItemSubCategory sub, ItemTier? tier)
    {
        string key = main.ToString();

        if (middle != ItemMiddleCategory.None)
        {
            key += "_" + middle;

            if (sub != ItemSubCategory.None)
            {
                key += "_" + sub;

                if (tier.HasValue)
                {
                    key += "_" + tier.Value;
                }
            }
        }

        return key;
    }

    private static bool MatchesPath(ItemData item, EncyclopediaCategoryPath path)
    {
        if (item.MainCategory != path.Main)
        {
            return false;
        }

        if (path.Middle.HasValue && path.Middle.Value != ItemMiddleCategory.None)
        {
            if (item.MiddleCategory != path.Middle.Value)
            {
                return false;
            }
        }

        if (path.Sub.HasValue && path.Sub.Value != ItemSubCategory.None)
        {
            if (item.SubCategory != path.Sub.Value)
            {
                return false;
            }
        }

        if (path.Tier.HasValue)
        {
            if (item.Tier != path.Tier.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesPath(ItemMetadata meta, EncyclopediaCategoryPath path)
    {
        if (meta.MainCategory != path.Main)
        {
            return false;
        }

        if (path.Middle.HasValue && path.Middle.Value != ItemMiddleCategory.None)
        {
            if (meta.MiddleCategory != path.Middle.Value)
            {
                return false;
            }
        }

        if (path.Sub.HasValue && path.Sub.Value != ItemSubCategory.None)
        {
            if (meta.SubCategory != path.Sub.Value)
            {
                return false;
            }
        }

        if (path.Tier.HasValue)
        {
            if (meta.Tier != path.Tier.Value)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 전체 도감 수집률 (해금/전체) 및 카테고리별 수집 현황을 반환합니다.
    /// 대시보드 UI용.
    /// </summary>
    public (int totalUnlocked, int totalCount) GetOverallProgress()
    {
        EnsureCategoryIndexBuilt();

        // BuildCategoryIndex 첫 번째 버킷: key "None"에 카탈로그 전체 ItemId가 각 1회씩 들어감 → 중복 합산 없이 전체 수 집계.
        if (_categoryIndex.TryGetValue(ItemMainCategory.None.ToString(), out List<string> allIds) && allIds != null && allIds.Count > 0)
        {
            int total = allIds.Count;
            int unlocked = 0;
            for (int i = 0; i < allIds.Count; i++)
            {
                if (_unlockedItemIds.Contains(allIds[i]))
                {
                    unlocked++;
                }
            }

            return (unlocked, total);
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var list in _categoryIndex.Values)
        {
            foreach (string id in list)
            {
                seen.Add(id);
            }
        }

        int totalFallback = seen.Count;
        int unlockedFallback = 0;
        foreach (string id in seen)
        {
            if (_unlockedItemIds.Contains(id))
            {
                unlockedFallback++;
            }
        }

        return (unlockedFallback, totalFallback);
    }

    /// <summary>
    /// 대분류(MainCategory)별 수집 현황 목록.
    /// </summary>
    public List<(ItemMainCategory category, int unlocked, int total)> GetCategoryBreakdown()
    {
        EnsureCategoryIndexBuilt();
        var result = new List<(ItemMainCategory, int, int)>();

        foreach (ItemMainCategory main in System.Enum.GetValues(typeof(ItemMainCategory)))
        {
            if (main == ItemMainCategory.None)
            {
                continue;
            }

            string key = main.ToString();
            if (!_categoryIndex.TryGetValue(key, out var list) || list.Count == 0)
            {
                continue;
            }

            int u = 0;
            foreach (string id in list)
            {
                if (_unlockedItemIds.Contains(id))
                {
                    u++;
                }
            }

            result.Add((main, u, list.Count));
        }

        return result;
    }

    /// <summary>
    /// 데이터를 JSON 파일로 저장합니다.
    /// </summary>
    public void Save()
    {
        if (!_isDirty)
        {
            return;
        }

        try
        {
            var wrapper = new EncyclopediaSaveData
            {
                ItemIds = new List<string>(_unlockedItemIds)
            };

            string json = JsonUtility.ToJson(wrapper);
            File.WriteAllText(_savePath, json);
            _isDirty = false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EncyclopediaManager] Save 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// JSON 파일에서 데이터를 불러옵니다.
    /// </summary>
    public void Load()
    {
        if (!File.Exists(_savePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(_savePath);
            var wrapper = JsonUtility.FromJson<EncyclopediaSaveData>(json);

            if (wrapper?.ItemIds != null)
            {
                _unlockedItemIds.Clear();
                foreach (string id in wrapper.ItemIds)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        _unlockedItemIds.Add(id);
                    }
                }
            }

            _isDirty = false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EncyclopediaManager] Load 실패: {ex.Message}");
        }
    }

    [Serializable]
    private class EncyclopediaSaveData
    {
        public List<string> ItemIds;
    }
}

/// <summary>
/// 도감 UI 진행률 조회 시 사용하는 카테고리 경로.
/// null 필드는 "해당 레벨 전체"를 의미합니다.
/// </summary>
public struct EncyclopediaCategoryPath
{
    public ItemMainCategory Main;
    public ItemMiddleCategory? Middle;
    public ItemSubCategory? Sub;
    public ItemTier? Tier;
}
