using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 빌드된 ItemMetadata 샤딩 산출물(카탈로그 + 샤드) 및 레거시 단일 바이너리를 검증합니다.
/// Tools → ARCHÉ → Items → Validate Item Metadata Build
/// </summary>
public static class ItemMetadataValidator
{
    private const string CatalogRelative = "Assets/StreamingAssets/ItemMetadataCatalog.bin";

    private const string ShardFolderRelative = "Assets/StreamingAssets/ItemMetadata";

    private const string LegacyRelative = "Assets/StreamingAssets/ItemMetadata.bin";

    private const uint BinaryMagic = 0x47534942; // "GSIB"

    private const uint CatalogMagic = 0x5441434D; // "MCAT"

    private const int CatalogFormatVersion = 1;

    private const int BinaryVersionExpected = 2;

    private static readonly Regex ShardFileRegex = new Regex(
        @"^ItemMetadata_shard_(\d+)\.bin$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>샤딩 빌드 직후 자동 호출 또는 메뉴에서 수동 실행.</summary>
    public static ItemMetadataValidationResult ValidateShardedBuild(string projectRoot)
    {
        var result = new ItemMetadataValidationResult();
        string catalogPath = Path.Combine(projectRoot, CatalogRelative).Replace("\\", "/");

        if (!File.Exists(catalogPath))
        {
            result.Errors.Add($"[ItemMetadataValidator] 카탈로그 파일이 없습니다: {CatalogRelative}");
            return result;
        }

        if (!TryReadCatalog(catalogPath, result, out List<CatalogEntry> catalogEntries, out int declaredCount))
        {
            return result;
        }

        var catalogIdToMain = new Dictionary<string, int>(declaredCount, StringComparer.OrdinalIgnoreCase);
        var countPerMain = new Dictionary<int, int>();

        for (int i = 0; i < catalogEntries.Count; i++)
        {
            CatalogEntry e = catalogEntries[i];
            if (string.IsNullOrWhiteSpace(e.ItemId))
            {
                result.Warnings.Add($"[ItemMetadataValidator] 카탈로그 {i + 1}번째 행: ItemId가 비어 있습니다.");
                continue;
            }

            if (catalogIdToMain.ContainsKey(e.ItemId))
            {
                result.Errors.Add(
                    $"[ItemMetadataValidator] 카탈로그에 ItemId 중복: \"{e.ItemId}\" (OrdinalIgnoreCase 기준).");
            }
            else
            {
                catalogIdToMain[e.ItemId] = e.MainCategory;
                if (!countPerMain.TryGetValue(e.MainCategory, out int c))
                {
                    c = 0;
                }

                countPerMain[e.MainCategory] = c + 1;
            }
        }

        int nonEmptyCatalogIds = catalogIdToMain.Count;
        if (declaredCount != catalogEntries.Count)
        {
            result.Errors.Add(
                $"[ItemMetadataValidator] 카탈로그 헤더 count({declaredCount})와 행 수({catalogEntries.Count}) 불일치.");
        }

        string shardRoot = Path.Combine(projectRoot, ShardFolderRelative).Replace("\\", "/");
        if (!Directory.Exists(shardRoot))
        {
            if (nonEmptyCatalogIds > 0)
            {
                result.Errors.Add($"[ItemMetadataValidator] 샤드 폴더가 없는데 카탈로그에 항목이 있습니다: {ShardFolderRelative}");
            }
            else
            {
                result.Summary = "카탈로그 0건, 샤드 없음 — 정상(빈 빌드).";
            }

            result.Success = result.Errors.Count == 0;
            return result;
        }

        string[] shardFiles = Directory.GetFiles(shardRoot, "*.bin", SearchOption.TopDirectoryOnly);
        var shardMainsFound = new HashSet<int>();
        var allShardIds = new Dictionary<string, int>(nonEmptyCatalogIds, StringComparer.OrdinalIgnoreCase);

        foreach (string shardPath in shardFiles)
        {
            string fileName = Path.GetFileName(shardPath);
            Match m = ShardFileRegex.Match(fileName);
            if (!m.Success)
            {
                result.Warnings.Add($"[ItemMetadataValidator] 무시할 수 있는 비표준 파일: {ShardFolderRelative}/{fileName}");
                continue;
            }

            int expectedMain = int.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
            shardMainsFound.Add(expectedMain);

            if (!countPerMain.TryGetValue(expectedMain, out int expectedRows) || expectedRows == 0)
            {
                result.Warnings.Add(
                    $"[ItemMetadataValidator] 샤드 파일은 있으나 카탈로그에 해당 대분류({expectedMain}) 항목이 없습니다: {fileName}");
            }

            if (!TryReadShard(shardPath, expectedMain, result, allShardIds, out int rowCount))
            {
                continue;
            }

            if (countPerMain.TryGetValue(expectedMain, out int exp) && exp != rowCount)
            {
                result.Errors.Add(
                    $"[ItemMetadataValidator] 샤드 행 수 불일치: {fileName} 실제 {rowCount}행, 카탈로그 대분류 {expectedMain} 예상 {exp}행.");
            }
        }

        foreach (var kv in countPerMain)
        {
            if (kv.Value <= 0)
            {
                continue;
            }

            string expectedFile = Path.Combine(shardRoot, $"ItemMetadata_shard_{kv.Key:D2}.bin").Replace("\\", "/");
            if (!File.Exists(expectedFile))
            {
                result.Errors.Add(
                    $"[ItemMetadataValidator] 카탈로그에 대분류 {kv.Key} 항목 {kv.Value}개가 있는데 샤드 파일이 없습니다: ItemMetadata_shard_{kv.Key:D2}.bin");
            }
        }

        foreach (var kv in catalogIdToMain)
        {
            if (!allShardIds.TryGetValue(kv.Key, out int shardMain))
            {
                result.Errors.Add($"[ItemMetadataValidator] 카탈로그에는 있으나 어떤 샤드에도 없는 ItemId: \"{kv.Key}\".");
            }
            else if (shardMain != kv.Value)
            {
                result.Errors.Add(
                    $"[ItemMetadataValidator] ItemId \"{kv.Key}\"의 대분류 불일치: 카탈로그={kv.Value}, 샤드={shardMain}.");
            }
        }

        foreach (var kv in allShardIds)
        {
            if (!catalogIdToMain.ContainsKey(kv.Key))
            {
                result.Errors.Add($"[ItemMetadataValidator] 샤드에는 있으나 카탈로그에 없는 ItemId: \"{kv.Key}\".");
            }
        }

        if (result.Errors.Count == 0 && string.IsNullOrEmpty(result.Summary))
        {
            result.Summary = $"카탈로그 고유 Id {nonEmptyCatalogIds}개, 샤드 파일 {shardMainsFound.Count}개.";
            if (result.Warnings.Count > 0)
            {
                result.Summary += $" (경고 {result.Warnings.Count}건)";
            }
        }

        result.Success = result.Errors.Count == 0;
        return result;
    }

    /// <summary>레거시 ItemMetadata.bin 단일 파일만 있는 경우.</summary>
    public static ItemMetadataValidationResult ValidateLegacyBinary(string projectRoot)
    {
        var result = new ItemMetadataValidationResult();
        string path = Path.Combine(projectRoot, LegacyRelative).Replace("\\", "/");
        if (!File.Exists(path))
        {
            result.Errors.Add($"[ItemMetadataValidator] 레거시 파일 없음: {LegacyRelative}");
            return result;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            using (var ms = new MemoryStream(bytes))
            using (var reader = new BinaryReader(ms, Encoding.UTF8))
            {
                uint first = reader.ReadUInt32();
                int version;
                int count;
                if (first == BinaryMagic)
                {
                    version = reader.ReadInt32();
                    count = reader.ReadInt32();
                }
                else
                {
                    version = 1;
                    count = (int)first;
                }

                if (version < 1 || version > BinaryVersionExpected + 2)
                {
                    result.Warnings.Add($"[ItemMetadataValidator] 레거시 바이너리 버전 {version} — 예상 범위와 다를 수 있습니다.");
                }

                var seenIds = new Dictionary<string, bool>(Math.Max(count, 16), StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < count; i++)
                {
                    ItemMetadata meta = ReadMetadataRow(reader, version, out _);
                    if (string.IsNullOrWhiteSpace(meta.ItemId))
                    {
                        result.Warnings.Add($"[ItemMetadataValidator] 레거시 바이너리 {i + 1}번째 행: ItemId 비어 있음.");
                        continue;
                    }

                    if (seenIds.ContainsKey(meta.ItemId))
                    {
                        result.Errors.Add($"[ItemMetadataValidator] 레거시 바이너리 ItemId 중복: \"{meta.ItemId}\".");
                    }
                    else
                    {
                        seenIds[meta.ItemId] = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"[ItemMetadataValidator] 레거시 바이너리 읽기 실패: {ex.Message}");
            return result;
        }

        result.Success = result.Errors.Count == 0;
        result.Summary = result.Success ? "레거시 ItemMetadata.bin 파싱 성공." : "";
        return result;
    }

    /// <summary>카탈로그가 있으면 샤딩 검증, 없으면 레거시만 시도.</summary>
    public static ItemMetadataValidationResult ValidateAuto(string projectRoot)
    {
        string catalogPath = Path.Combine(projectRoot, CatalogRelative).Replace("\\", "/");
        if (File.Exists(catalogPath))
        {
            return ValidateShardedBuild(projectRoot);
        }

        return ValidateLegacyBinary(projectRoot);
    }

    public static void Report(ItemMetadataValidationResult result, bool silent)
    {
        for (int i = 0; i < result.Errors.Count; i++)
        {
            Debug.LogError(result.Errors[i]);
        }

        for (int i = 0; i < result.Warnings.Count; i++)
        {
            Debug.LogWarning(result.Warnings[i]);
        }

        if (result.Success)
        {
            Debug.Log($"[ItemMetadataValidator] 검증 통과. {result.Summary}");
        }
        else if (!silent)
        {
            EditorUtility.DisplayDialog(
                "Item Metadata 검증 실패",
                result.Errors.Count > 0 ? string.Join("\n", result.Errors) : "알 수 없는 오류.",
                "확인");
        }
    }

    [MenuItem("Tools/ARCHÉ/Items/Validate Item Metadata Build")]
    public static void MenuValidate()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        ItemMetadataValidationResult r = ValidateAuto(projectRoot);
        Report(r, false);
    }

    private struct CatalogEntry
    {
        public string ItemId;
        public int MainCategory;
    }

    private static bool TryReadCatalog(
        string path,
        ItemMetadataValidationResult result,
        out List<CatalogEntry> entries,
        out int declaredCount)
    {
        entries = new List<CatalogEntry>();
        declaredCount = 0;
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            using (var ms = new MemoryStream(bytes))
            using (var reader = new BinaryReader(ms, Encoding.UTF8))
            {
                uint magic = reader.ReadUInt32();
                if (magic != CatalogMagic)
                {
                    result.Errors.Add($"[ItemMetadataValidator] 카탈로그 시그니처 불일치 (MCAT 아님): {path}");
                    return false;
                }

                int version = reader.ReadInt32();
                if (version != CatalogFormatVersion)
                {
                    result.Errors.Add($"[ItemMetadataValidator] 지원하지 않는 카탈로그 버전 {version} (예상 {CatalogFormatVersion}): {path}");
                    return false;
                }

                declaredCount = reader.ReadInt32();
                for (int i = 0; i < declaredCount; i++)
                {
                    string itemId = reader.ReadString();
                    int main = reader.ReadByte();
                    entries.Add(new CatalogEntry { ItemId = itemId, MainCategory = main });
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"[ItemMetadataValidator] 카탈로그 읽기 실패: {ex.Message}");
            return false;
        }

        return true;
    }

    private static bool TryReadShard(
        string shardPath,
        int expectedMain,
        ItemMetadataValidationResult result,
        Dictionary<string, int> allShardIds,
        out int rowCount)
    {
        rowCount = 0;
        try
        {
            byte[] bytes = File.ReadAllBytes(shardPath);
            using (var ms = new MemoryStream(bytes))
            using (var reader = new BinaryReader(ms, Encoding.UTF8))
            {
                uint first = reader.ReadUInt32();
                int version;
                int count;
                if (first == BinaryMagic)
                {
                    version = reader.ReadInt32();
                    count = reader.ReadInt32();
                }
                else
                {
                    version = 1;
                    count = (int)first;
                }

                if (version != BinaryVersionExpected && first == BinaryMagic)
                {
                    result.Warnings.Add($"[ItemMetadataValidator] 샤드 버전 {version} (예상 {BinaryVersionExpected}): {Path.GetFileName(shardPath)}");
                }

                var seenInShard = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < count; i++)
                {
                    ItemMetadata meta = ReadMetadataRow(reader, version, out int rawMainCategory);
                    rowCount++;
                    if (string.IsNullOrWhiteSpace(meta.ItemId))
                    {
                        result.Warnings.Add($"[ItemMetadataValidator] {Path.GetFileName(shardPath)} {i + 1}번째 행: ItemId 비어 있음.");
                        continue;
                    }

                    if (rawMainCategory != expectedMain)
                    {
                        result.Errors.Add(
                            $"[ItemMetadataValidator] {Path.GetFileName(shardPath)}: ItemId \"{meta.ItemId}\"의 MainCategory(파일 값 {rawMainCategory})가 샤드 대분류({expectedMain})와 다릅니다.");
                    }

                    if (!seenInShard.Add(meta.ItemId))
                    {
                        result.Errors.Add($"[ItemMetadataValidator] {Path.GetFileName(shardPath)}: ItemId 중복 \"{meta.ItemId}\".");
                    }

                    if (allShardIds.ContainsKey(meta.ItemId))
                    {
                        result.Errors.Add(
                            $"[ItemMetadataValidator] 여러 샤드에 동일 ItemId: \"{meta.ItemId}\" (이전 대분류 {allShardIds[meta.ItemId]}).");
                    }
                    else
                    {
                        allShardIds[meta.ItemId] = expectedMain;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"[ItemMetadataValidator] 샤드 읽기 실패 {shardPath}: {ex.Message}");
            return false;
        }

        return true;
    }

    private static ItemMetadata ReadMetadataRow(BinaryReader reader, int version, out int rawMainCategory)
    {
        string itemId = reader.ReadString();
        string itemName = reader.ReadString();
        int main = reader.ReadInt32();
        rawMainCategory = main;
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
}

/// <summary>ItemMetadata 빌드 검증 결과.</summary>
public sealed class ItemMetadataValidationResult
{
    public bool Success;

    public List<string> Errors = new List<string>();

    public List<string> Warnings = new List<string>();

    public string Summary = "";
}
