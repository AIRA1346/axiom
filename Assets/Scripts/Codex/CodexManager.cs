using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// ARCHÉ 도서관(Codex) 허브. ADDS 색인(CodexIndex.bin)은 Awake에서 비동기로 읽고 파싱하며 메모리에 상주합니다.
/// 본문은 Addressables(주소=Id) 우선, 없으면 Resources/CodexContents/[safeId]로 비동기 로드합니다.
/// </summary>
public sealed class CodexManager : MonoBehaviour
{
    /// <summary>ADDS 색인 레이아웃 버전. 레거시(대/중/소 3필드) v1과 구분합니다.</summary>
    public const int BinaryFormatVersion = 2;

    public static CodexManager Instance { get; private set; }

    private readonly List<CodexMetadata> _index = new List<CodexMetadata>();
    private readonly Dictionary<string, CodexMetadata> _idToMeta = new Dictionary<string, CodexMetadata>(StringComparer.OrdinalIgnoreCase);
    private bool _indexBuilt;

    /// <summary>색인 비동기 로드 완료 여부. 완료 전에는 목차·조회가 비어 있을 수 있습니다.</summary>
    public bool IsIndexReady => _indexBuilt;

    private Task _indexLoadTask;

    /// <summary>GetFlatIndexForNavigation() 결과 캐시. 색인이 바뀔 때만 재생성합니다.</summary>
    private List<CodexMetadata> _flatNavCache;

    private bool _flatNavDirty = true;

    private const string ContentPathPrefix = "CodexContents/";
    private const string IndexFileName = "CodexIndex.bin";
    private static readonly byte[] IndexMagic = Encoding.ASCII.GetBytes("CDXI");

    public IReadOnlyList<CodexMetadata> Index => _index;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _indexLoadTask = LoadIndexFromDiskAsync();
    }

    /// <summary>CodexIndex.bin 로드가 끝날 때까지 대기합니다. UI는 OnEnable 등에서 먼저 await 하세요.</summary>
    public Task WaitForIndexAsync()
    {
        return _indexLoadTask ?? Task.CompletedTask;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>레거시 호환. 색인은 Awake에서 비동기 로드됩니다. <see cref="WaitForIndexAsync"/>를 사용하세요.</summary>
    public void BuildIndexIfNeeded()
    {
    }

    /// <summary>색인을 StreamingAssets에서 다시 읽습니다.</summary>
    public void RebuildIndex()
    {
        _indexBuilt = false;
        _flatNavDirty = true;
        _index.Clear();
        _idToMeta.Clear();
        _indexLoadTask = LoadIndexFromDiskAsync();
    }

    /// <summary>파일 읽기·바이너리 파싱은 백그라운드 스레드, 컬렉션 반영만 메인 스레드.</summary>
    private async Task LoadIndexFromDiskAsync()
    {
        _flatNavDirty = true;
        string binPath = Path.Combine(Application.streamingAssetsPath, IndexFileName);
        try
        {
            if (string.IsNullOrEmpty(binPath) || !File.Exists(binPath))
            {
                return;
            }

            (bool ok, List<CodexMetadata> parsed) = await Task.Run(() => ParseBinaryToList(binPath)).ConfigureAwait(true);

            if (!ok || parsed == null)
            {
                return;
            }

            _index.Clear();
            _idToMeta.Clear();
            for (int i = 0; i < parsed.Count; i++)
            {
                CodexMetadata m = parsed[i];
                _index.Add(m);
                _idToMeta[m.Id] = m;
            }

            SortIndexHierarchically();
            _flatNavDirty = true;
        }
        finally
        {
            _indexBuilt = true;
        }
    }

    /// <summary>CodexIndex.bin (ADDS) 파싱. Header: CDXI + Version + Count. 워커 스레드에서 호출 가능.</summary>
    private static (bool ok, List<CodexMetadata> list) ParseBinaryToList(string path)
    {
        var list = new List<CodexMetadata>();
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            using (var ms = new MemoryStream(bytes))
            using (var reader = new BinaryReader(ms, Encoding.UTF8))
            {
                byte[] magic = reader.ReadBytes(4);
                if (magic == null || magic.Length < 4 ||
                    magic[0] != IndexMagic[0] || magic[1] != IndexMagic[1] ||
                    magic[2] != IndexMagic[2] || magic[3] != IndexMagic[3])
                {
                    return (false, list);
                }

                int version = reader.ReadInt32();
                if (version != BinaryFormatVersion)
                {
                    return (false, list);
                }

                int count = reader.ReadInt32();
                if (count < 0)
                {
                    return (false, list);
                }

                for (int i = 0; i < count; i++)
                {
                    var meta = new CodexMetadata
                    {
                        Id = reader.ReadString(),
                        Title = reader.ReadString(),
                        Summary = reader.ReadString(),
                        Level1Root = reader.ReadString(),
                        Level2Source = reader.ReadString(),
                        Level3Field = reader.ReadString(),
                        Level4Nature = reader.ReadString(),
                        Level5Lineage = reader.ReadString(),
                        Level6Role = reader.ReadString(),
                        Level7Rank = reader.ReadString(),
                        Level8Species = reader.ReadString(),
                        Level9Identity = reader.ReadString(),
                        SortOrder = reader.ReadInt32(),
                        HasBody = reader.ReadBoolean(),
                        Depth = 9
                    };

                    if (string.IsNullOrWhiteSpace(meta.Id))
                    {
                        continue;
                    }

                    list.Add(meta);
                }
            }

            return (list.Count > 0, list);
        }
        catch (Exception)
        {
            return (false, list);
        }
    }

    /// <summary>Lv.1~Lv.9 순으로 정렬한 뒤 SortOrder로 안정 정렬합니다.</summary>
    private void SortIndexHierarchically()
    {
        _index.Sort(CompareLeafOrder);
    }

    private static int CompareLeafOrder(CodexMetadata a, CodexMetadata b)
    {
        int cmp = string.Compare(a.Level1Root ?? "", b.Level1Root ?? "", StringComparison.OrdinalIgnoreCase);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = string.Compare(a.Level2Source ?? "", b.Level2Source ?? "", StringComparison.OrdinalIgnoreCase);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = string.Compare(a.Level3Field ?? "", b.Level3Field ?? "", StringComparison.OrdinalIgnoreCase);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = string.Compare(a.Level4Nature ?? "", b.Level4Nature ?? "", StringComparison.OrdinalIgnoreCase);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = string.Compare(a.Level5Lineage ?? "", b.Level5Lineage ?? "", StringComparison.OrdinalIgnoreCase);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = string.Compare(a.Level6Role ?? "", b.Level6Role ?? "", StringComparison.OrdinalIgnoreCase);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = string.Compare(a.Level7Rank ?? "", b.Level7Rank ?? "", StringComparison.OrdinalIgnoreCase);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = string.Compare(a.Level8Species ?? "", b.Level8Species ?? "", StringComparison.OrdinalIgnoreCase);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = string.Compare(a.Level9Identity ?? "", b.Level9Identity ?? "", StringComparison.OrdinalIgnoreCase);
        if (cmp != 0)
        {
            return cmp;
        }

        return a.SortOrder.CompareTo(b.SortOrder);
    }

    /// <summary>
    /// ADDS 플랫 트리 목차. Depth 0~8은 구간 헤더, 9는 리프(본문 항목).
    /// Virtualized Scroll View에서 접기/펼치기 및 들여쓰기에 사용합니다.
    /// 색인이 같으면 내부 리스트를 재사용합니다(패널 OnEnable 반복 시 할당 감소).
    /// </summary>
    public IReadOnlyList<CodexMetadata> GetFlatIndexForNavigation()
    {
        if (!_indexBuilt)
        {
            return Array.Empty<CodexMetadata>();
        }

        if (_flatNavDirty)
        {
            RebuildFlatNavigationCache();
        }

        return _flatNavCache;
    }

    private void RebuildFlatNavigationCache()
    {
        int cap = Math.Max(16, _index.Count * 11);
        if (_flatNavCache == null)
        {
            _flatNavCache = new List<CodexMetadata>(cap);
        }
        else
        {
            _flatNavCache.Clear();
            if (_flatNavCache.Capacity < cap)
            {
                _flatNavCache.Capacity = cap;
            }
        }

        List<CodexMetadata> flat = _flatNavCache;
        var lastSeg = new string[9];
        for (int i = 0; i < lastSeg.Length; i++)
        {
            lastSeg[i] = string.Empty;
        }

        int navOrder = 0;

        foreach (CodexMetadata leaf in _index)
        {
            for (int d = 0; d < 9; d++)
            {
                string seg = (GetLevelValue(leaf, d) ?? "").Trim();
                string prev = lastSeg[d] ?? string.Empty;
                if (string.Equals(seg, prev, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                for (int j = d + 1; j < 9; j++)
                {
                    lastSeg[j] = string.Empty;
                }

                lastSeg[d] = seg;
                if (!string.IsNullOrWhiteSpace(seg))
                {
                    flat.Add(CreateHeaderRow(leaf, d, seg, navOrder++));
                }
            }

            flat.Add(new CodexMetadata
            {
                Id = leaf.Id,
                Title = leaf.Title,
                Summary = leaf.Summary,
                Level1Root = leaf.Level1Root,
                Level2Source = leaf.Level2Source,
                Level3Field = leaf.Level3Field,
                Level4Nature = leaf.Level4Nature,
                Level5Lineage = leaf.Level5Lineage,
                Level6Role = leaf.Level6Role,
                Level7Rank = leaf.Level7Rank,
                Level8Species = leaf.Level8Species,
                Level9Identity = leaf.Level9Identity,
                Depth = 9,
                SortOrder = leaf.SortOrder,
                HasBody = leaf.HasBody
            });
        }

        _flatNavDirty = false;
    }

    private static CodexMetadata CreateHeaderRow(CodexMetadata leaf, int depth, string segment, int order)
    {
        return new CodexMetadata
        {
            Id = "",
            Title = string.IsNullOrEmpty(segment) ? $"[Lv.{depth + 1}]" : segment,
            Summary = "",
            Level1Root = leaf.Level1Root,
            Level2Source = leaf.Level2Source,
            Level3Field = leaf.Level3Field,
            Level4Nature = leaf.Level4Nature,
            Level5Lineage = leaf.Level5Lineage,
            Level6Role = leaf.Level6Role,
            Level7Rank = leaf.Level7Rank,
            Level8Species = leaf.Level8Species,
            Level9Identity = leaf.Level9Identity,
            Depth = depth,
            SortOrder = order,
            HasBody = false
        };
    }

    /// <summary>0~8 인덱스에 해당하는 ADDS 레벨 문자열을 반환합니다.</summary>
    public static string GetLevelValue(CodexMetadata meta, int zeroBasedLevelIndex)
    {
        if (zeroBasedLevelIndex < 0 || zeroBasedLevelIndex > 8)
        {
            return "";
        }

        switch (zeroBasedLevelIndex)
        {
            case 0: return meta.Level1Root ?? "";
            case 1: return meta.Level2Source ?? "";
            case 2: return meta.Level3Field ?? "";
            case 3: return meta.Level4Nature ?? "";
            case 4: return meta.Level5Lineage ?? "";
            case 5: return meta.Level6Role ?? "";
            case 6: return meta.Level7Rank ?? "";
            case 7: return meta.Level8Species ?? "";
            default: return meta.Level9Identity ?? "";
        }
    }

    /// <summary>Lv.1부터 (lastInclusiveIndex+1)번째 레벨까지 경로 키 (접기/가시성 판별).</summary>
    public static string BuildPathKeyThroughDepth(CodexMetadata meta, int lastInclusiveZeroBased)
    {
        if (lastInclusiveZeroBased < 0 || lastInclusiveZeroBased > 8)
        {
            return "";
        }

        var sb = new StringBuilder(128);
        for (int i = 0; i <= lastInclusiveZeroBased; i++)
        {
            string part = GetLevelValue(meta, i) ?? "";
            if (i > 0)
            {
                sb.Append('|');
            }

            sb.Append(part);
        }

        return sb.ToString();
    }

    /// <summary>카테고리 행(Depth 0~8)의 접기 키. 리프는 빈 문자열.</summary>
    public static string GetCategoryKey(CodexMetadata meta)
    {
        if (meta.Depth < 0 || meta.Depth > 8)
        {
            return "";
        }

        return BuildPathKeyThroughDepth(meta, meta.Depth);
    }

    /// <summary>Id로 리프 메타데이터를 조회합니다.</summary>
    public bool TryGetMetadata(string id, out CodexMetadata meta)
    {
        meta = default;
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        if (!_indexBuilt)
        {
            return false;
        }

        return _idToMeta.TryGetValue(id, out meta);
    }

    /// <summary>본문만 비동기 로드. Addressables에 주소가 있을 때만 로드하고, 없으면 Resources로 폴백합니다.</summary>
    public async Task<string> LoadContentAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        await AddressablesBootstrap.EnsureInitializedAsync();

        string trimmed = id.Trim();
        string safeId = SanitizeIdForPath(trimmed);
        if (string.IsNullOrEmpty(safeId))
        {
            return null;
        }

        // 주소 미등록 시 LoadAssetAsync가 InvalidKeyException을 던지며 콘솔에 남는 것을 막기 위해,
        // 먼저 위치 조회로 존재 여부만 확인합니다.
        var locHandle = Addressables.LoadResourceLocationsAsync(trimmed);
        try
        {
            await locHandle.Task;
            bool hasAddress = locHandle.Status == AsyncOperationStatus.Succeeded
                && locHandle.Result != null
                && locHandle.Result.Count > 0;

            if (hasAddress)
            {
                AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(trimmed);
                try
                {
                    await handle.Task;
                    if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                    {
                        return handle.Result.text;
                    }
                }
                finally
                {
                    Addressables.Release(handle);
                }
            }
        }
        finally
        {
            Addressables.Release(locHandle);
        }

        string resourcePath = ContentPathPrefix + safeId;
        var request = Resources.LoadAsync<TextAsset>(resourcePath);

        while (request != null && !request.isDone)
        {
            await Task.Yield();
        }

        if (request == null || request.asset == null)
        {
            return null;
        }

        var textAsset = request.asset as TextAsset;
        return textAsset != null ? textAsset.text : null;
    }

    private static string SanitizeIdForPath(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return "";
        }

        char[] invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(id.Length);
        foreach (char c in id)
        {
            if (Array.IndexOf(invalid, c) >= 0 || c == '/' || c == '\\' || c == ':' || c == '*' || c == '?' || c == '"' || c == '<' || c == '>' || c == '|')
            {
                sb.Append('_');
            }
            else
            {
                sb.Append(c);
            }
        }

        string result = sb.ToString().Trim(' ', '.', '_');
        return result.Length > 200 ? result.Substring(0, 200) : result;
    }
}
