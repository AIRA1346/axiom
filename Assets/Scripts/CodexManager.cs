using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// ARCHÉ 도서관(Codex) 허브. ADDS 색인은 CodexIndex.bin만 로드하며 메모리에 상주합니다.
/// 본문은 Resources/CodexContents/[ID].txt를 항목 선택 시에만 비동기 로드합니다.
/// </summary>
public sealed class CodexManager : MonoBehaviour
{
    /// <summary>ADDS 색인 레이아웃 버전. 레거시(대/중/소 3필드) v1과 구분합니다.</summary>
    public const int BinaryFormatVersion = 2;

    public static CodexManager Instance { get; private set; }

    private readonly List<CodexMetadata> _index = new List<CodexMetadata>();
    private readonly Dictionary<string, CodexMetadata> _idToMeta = new Dictionary<string, CodexMetadata>(StringComparer.OrdinalIgnoreCase);
    private bool _indexBuilt;

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
    }

    private void Start()
    {
        BuildIndexIfNeeded();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>색인이 비어 있으면 CodexIndex.bin에서 로드합니다.</summary>
    public void BuildIndexIfNeeded()
    {
        if (_indexBuilt)
        {
            return;
        }

        BuildIndex();
    }

    /// <summary>색인을 StreamingAssets에서 다시 읽습니다.</summary>
    public void RebuildIndex()
    {
        _indexBuilt = false;
        BuildIndex();
    }

    private void BuildIndex()
    {
        _index.Clear();
        _idToMeta.Clear();

        string binPath = Path.Combine(Application.streamingAssetsPath, IndexFileName);
        if (string.IsNullOrEmpty(binPath) || !File.Exists(binPath))
        {
            _indexBuilt = true;
            return;
        }

        if (!TryLoadFromBinary(binPath))
        {
            _indexBuilt = true;
            return;
        }

        SortIndexHierarchically();
        _indexBuilt = true;
    }

    /// <summary>CodexIndex.bin (ADDS) 로드. Header: CDXI + Version + Count.</summary>
    private bool TryLoadFromBinary(string path)
    {
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
                    return false;
                }

                int version = reader.ReadInt32();
                if (version != BinaryFormatVersion)
                {
                    return false;
                }

                int count = reader.ReadInt32();
                if (count < 0)
                {
                    return false;
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

                    _index.Add(meta);
                    _idToMeta[meta.Id] = meta;
                }
            }

            return _index.Count > 0;
        }
        catch (Exception)
        {
            return false;
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
    /// </summary>
    public IReadOnlyList<CodexMetadata> GetFlatIndexForNavigation()
    {
        BuildIndexIfNeeded();

        var flat = new List<CodexMetadata>(_index.Count * 11);
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

        return flat;
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

        BuildIndexIfNeeded();
        return _idToMeta.TryGetValue(id, out meta);
    }

    /// <summary>Resources/CodexContents에서 본문만 비동기 로드합니다.</summary>
    public async Task<string> LoadContentAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        string safeId = SanitizeIdForPath(id);
        if (string.IsNullOrEmpty(safeId))
        {
            return null;
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
