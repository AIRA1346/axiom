using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ItemData 에셋을 스캔하여 ItemMetadata.bin(바이너리)을 생성합니다.
/// 10만 개도 빠르게 처리. BinaryWriter로 0/1 이진 데이터 저장.
/// Tools > Build Item Metadata Database
/// </summary>
public static class ItemMetadataBuilder
{
    private const string ResourceFolderPath = "Assets/Resources";
    private const string OutputPath = "Assets/StreamingAssets/ItemMetadata.bin";

    [MenuItem("Tools/Build Item Metadata Database")]
    [MenuItem("Assets/Build Item Metadata Database")]
    [MenuItem("GSI/Build Item Metadata Database")]
    public static void Build() => Build(false);

    /// <summary>
    /// ItemMetadata.bin을 생성합니다.
    /// </summary>
    /// <param name="silent">true면 진행률 표시 없이 실행 (CI/배치 모드용)</param>
    public static void Build(bool silent)
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
                    return;
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
            EditorUtility.DisplayProgressBar("Build Item Metadata", "바이너리 쓰는 중...", 0.95f);
        }

        string fullOutputPath = Path.Combine(projectRoot, OutputPath).Replace("\\", "/");
        string dir = Path.GetDirectoryName(fullOutputPath) ?? "";
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        const uint BinaryMagic = 0x47534942; // "GSIB"
        const int BinaryVersion = 2;

        using (var fs = new FileStream(fullOutputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new BinaryWriter(fs, System.Text.Encoding.UTF8))
        {
            writer.Write(BinaryMagic);
            writer.Write(BinaryVersion);
            writer.Write(metadataList.Count);
            foreach (var m in metadataList)
            {
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

        if (!silent)
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();

        Debug.Log($"[ItemMetadataBuilder] {metadataList.Count}개 아이템 메타데이터 빌드 완료: {OutputPath}");
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
