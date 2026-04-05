using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ItemData 에셋을 스캔하여 메타데이터 바이너리를 생성합니다.
/// <para><b>샤딩(기본)</b>: ItemMetadataCatalog.bin(ItemId→대분류) + ItemMetadata/ItemMetadata_shard_XX.bin(대분류별 본문).</para>
/// <para>레거시 단일 ItemMetadata.bin은 생성하지 않습니다(런타임은 여전히 읽을 수 있음).</para>
/// 빌드 후 ItemMetadata 검증 → Addressables ItemId 주소 검증을 같은 순서로 실행합니다.
/// Tools → ARCHÉ → Items → Build Item Metadata Database
/// </summary>
public static class ItemMetadataBuilder
{
    private const string ResourceFolderPath = "Assets/Resources";

    private const string CatalogOutputRelative = "Assets/StreamingAssets/ItemMetadataCatalog.bin";

    private const string ShardFolderRelative = "Assets/StreamingAssets/ItemMetadata";

    private const uint BinaryMagic = 0x47534942; // "GSIB"

    private const uint CatalogMagic = 0x5441434D; // "MCAT"

    private const int BinaryVersion = 2;

    private const int CatalogFormatVersion = 1;

    [MenuItem("Tools/ARCHÉ/Items/Build Item Metadata Database")]
    [MenuItem("Assets/ARCHÉ/Build Item Metadata Database")]
    public static void Build()
    {
        _ = Build(false);
    }

    /// <summary>
    /// 샤딩된 메타데이터(카탈로그 + 샤드)를 생성합니다.
    /// </summary>
    /// <param name="silent">true면 진행률 표시 없이 실행 (CI/배치 모드용)</param>
    /// <returns>메타 검증·Addressables 주소 검증까지 통과하면 true.</returns>
    public static bool Build(bool silent)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        var metadataList = new List<ItemMetadata>();

        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ResourceFolderPath });

        for (int i = 0; i < guids.Length; i++)
        {
            if (!silent && i > 0 && i % 2000 == 0)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Build Item Metadata", $"{i}/{guids.Length}...", (float)i / guids.Length))
                {
                    EditorUtility.ClearProgressBar();
                    Debug.LogWarning("[ItemMetadataBuilder] 사용자에 의해 중단됨.");
                    return false;
                }
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            string fullPath = Path.Combine(projectRoot, assetPath).Replace("\\", "/");

            if (!TryParseMetadataFromFile(fullPath, assetPath, out ItemMetadata meta))
            {
                continue;
            }

            metadataList.Add(meta);
        }

        if (!silent)
        {
            EditorUtility.DisplayProgressBar("Build Item Metadata", "샤딩 바이너리 쓰는 중...", 0.95f);
        }

        var byMain = new Dictionary<int, List<ItemMetadata>>();
        for (int i = 0; i < metadataList.Count; i++)
        {
            ItemMetadata m = metadataList[i];
            int k = (int)m.MainCategory;
            if (!byMain.TryGetValue(k, out List<ItemMetadata> list))
            {
                list = new List<ItemMetadata>();
                byMain[k] = list;
            }

            list.Add(m);
        }

        string streamingRoot = Path.Combine(projectRoot, "Assets/StreamingAssets").Replace("\\", "/");
        Directory.CreateDirectory(streamingRoot);

        string shardRoot = Path.Combine(projectRoot, ShardFolderRelative).Replace("\\", "/");
        if (Directory.Exists(shardRoot))
        {
            Directory.Delete(shardRoot, true);
        }

        Directory.CreateDirectory(shardRoot);

        string legacyPath = Path.Combine(projectRoot, "Assets/StreamingAssets/ItemMetadata.bin").Replace("\\", "/");
        if (File.Exists(legacyPath))
        {
            File.Delete(legacyPath);
        }

        string catalogPath = Path.Combine(projectRoot, CatalogOutputRelative).Replace("\\", "/");
        using (var fs = new FileStream(catalogPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new BinaryWriter(fs, System.Text.Encoding.UTF8))
        {
            writer.Write(CatalogMagic);
            writer.Write(CatalogFormatVersion);
            writer.Write(metadataList.Count);
            for (int i = 0; i < metadataList.Count; i++)
            {
                ItemMetadata m = metadataList[i];
                writer.Write(m.ItemId ?? "");
                writer.Write((byte)Mathf.Clamp((int)m.MainCategory, 0, 255));
            }
        }

        foreach (var kv in byMain)
        {
            int main = kv.Key;
            List<ItemMetadata> list = kv.Value;
            string shardPath = Path.Combine(shardRoot, $"ItemMetadata_shard_{main:D2}.bin").Replace("\\", "/");
            using (var fs = new FileStream(shardPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(fs, System.Text.Encoding.UTF8))
            {
                writer.Write(BinaryMagic);
                writer.Write(BinaryVersion);
                writer.Write(list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    ItemMetadata m = list[i];
                    writer.Write(m.ItemId ?? "");
                    writer.Write(m.ItemName ?? "");
                    writer.Write((int)m.MainCategory);
                    writer.Write((int)m.MiddleCategory);
                    writer.Write((int)m.SubCategory);
                    writer.Write((int)m.Tier);
                    writer.Write(m.ResourcePath ?? "");
                    writer.Write(m.PurchasePrice);
                    writer.Write(m.SalePrice);
                }
            }
        }

        if (!silent)
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();

        Debug.Log(
            $"[ItemMetadataBuilder] {metadataList.Count}개 메타데이터 샤딩 빌드 완료: {CatalogOutputRelative}, {ShardFolderRelative}/ItemMetadata_shard_*.bin (레거시 ItemMetadata.bin 은 제거됨)");

        ItemMetadataValidationResult validation = ItemMetadataValidator.ValidateShardedBuild(projectRoot);
        ItemMetadataValidator.Report(validation, silent);

        bool addressablesOk = AddressablesItemAddressValidator.ValidateWithResult(silent);
        return validation.Success && addressablesOk;
    }

    private static bool TryParseMetadataFromFile(string fullPath, string assetPath, out ItemMetadata meta)
    {
        meta = default;

        if (!File.Exists(fullPath))
        {
            return false;
        }

        string content = File.ReadAllText(fullPath);
        string itemId = GetYamlValue(content, "ItemId");
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        string itemName = GetYamlValue(content, "ItemName");
        if (!string.IsNullOrEmpty(itemName) && itemName.StartsWith("\"") && itemName.EndsWith("\""))
        {
            itemName = itemName.Trim('"').Replace("\\\"", "\"").Replace("\\n", "\n");
        }

        int tier = GetYamlInt(content, "Tier", 0);
        int main = GetYamlInt(content, "MainCategory", 0);
        int middle = GetYamlInt(content, "MiddleCategory", 0);
        int sub = GetYamlInt(content, "SubCategory", 0);
        int purchasePrice = GetYamlInt(content, "PurchasePrice", 0);
        int salePrice = GetYamlInt(content, "SalePrice", 0);

        string resourcePath = assetPath
            .Replace("Assets/Resources/", "")
            .Replace(".asset", "");

        meta = new ItemMetadata
        {
            ItemId = itemId,
            ItemName = itemName ?? "",
            MainCategory = (ItemMainCategory)Mathf.Clamp(main, 0, (int)ItemMainCategory.SpecialSystem),
            MiddleCategory = (ItemMiddleCategory)Mathf.Clamp(middle, 0, (int)ItemMiddleCategory.SystemOnly),
            SubCategory = (ItemSubCategory)Mathf.Clamp(sub, 0, (int)ItemSubCategory.TutorialItem),
            Tier = (ItemTier)Mathf.Clamp(tier, 0, (int)ItemTier.Tier10),
            ResourcePath = resourcePath,
            PurchasePrice = purchasePrice,
            SalePrice = salePrice
        };

        return true;
    }

    private static string GetYamlValue(string content, string key)
    {
        string pattern = key + ":\\s*([^\\n]+)";
        var match = Regex.Match(content, pattern);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        pattern = key + ":\\s*\"((?:[^\"\\\\]|\\\\\\.)*)\"";
        match = Regex.Match(content, pattern, RegexOptions.Singleline);
        if (match.Success)
        {
            return match.Groups[1].Value.Replace("\\\"", "\"").Replace("\\n", "\n");
        }

        return "";
    }

    private static int GetYamlInt(string content, string key, int defaultValue)
    {
        string val = GetYamlValue(content, key);
        return int.TryParse(val, out int result) ? result : defaultValue;
    }
}
