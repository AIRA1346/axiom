using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// Resources/ItemData 에셋의 ItemId가 Addressables 주소(런타임 LoadAssetAsync와 동일)로 등록되어 있는지 검사합니다.
/// Tools → ARCHÉ → Items → Validate Addressables Item Addresses
/// </summary>
public static class AddressablesItemAddressValidator
{
    private const string ResourceFolderPath = "Assets/Resources";

    [MenuItem("Tools/ARCHÉ/Items/Validate Addressables Item Addresses")]
    public static void ValidateMenu()
    {
        Validate(false);
    }

    /// <param name="silent">true면 대화상자 없이 로그만 (배치/CI용)</param>
    public static void Validate(bool silent)
    {
        ValidateWithResult(silent);
    }

    /// <summary>주소 누락이 없으면 true. CI/배치 게이트용.</summary>
    public static bool ValidateWithResult(bool silent)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            const string msg = "[AddressablesItemAddressValidator] AddressableAssetSettings가 없습니다. Window → Asset Management → Addressables를 한 번 열어 초기화하세요.";
            Debug.LogError(msg);
            if (!silent)
            {
                EditorUtility.DisplayDialog("Addressables", msg, "확인");
            }

            return false;
        }

        var addressSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (AddressableAssetGroup group in settings.groups)
        {
            if (group == null || group.entries == null)
            {
                continue;
            }

            foreach (AddressableAssetEntry entry in group.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.address))
                {
                    continue;
                }

                addressSet.Add(entry.address.Trim());
            }
        }

        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ResourceFolderPath });

        var missing = new List<string>();
        int scanned = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            if (!silent && i > 0 && i % 2000 == 0)
            {
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Addressables 주소 검증",
                        $"{i}/{guids.Length}...",
                        (float)i / guids.Length))
                {
                    EditorUtility.ClearProgressBar();
                    Debug.LogWarning("[AddressablesItemAddressValidator] 사용자에 의해 중단됨.");
                    return false;
                }
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            string fullPath = Path.Combine(projectRoot, assetPath).Replace("\\", "/");
            if (!TryReadItemId(fullPath, out string itemId) || string.IsNullOrWhiteSpace(itemId))
            {
                continue;
            }

            scanned++;
            if (!addressSet.Contains(itemId))
            {
                missing.Add($"{itemId}  ({assetPath})");
            }
        }

        if (!silent)
        {
            EditorUtility.ClearProgressBar();
        }

        if (missing.Count == 0)
        {
            Debug.Log($"[AddressablesItemAddressValidator] 완료: ItemData {scanned}개 — 모두 Addressables 주소로 등록됨 (비교 주소 수 {addressSet.Count}개).");
            if (!silent)
            {
                EditorUtility.DisplayDialog(
                    "Addressables 검증",
                    $"ItemData {scanned}개: 주소 누락 없음.",
                    "확인");
            }

            return true;
        }

        foreach (string line in missing)
        {
            Debug.LogWarning($"[AddressablesItemAddressValidator] 주소 미등록: {line}");
        }

        Debug.LogError($"[AddressablesItemAddressValidator] 누락 {missing.Count}건 / 검사한 ItemData {scanned}건. Tools → ARCHÉ → Google Sheet → Item Importer 로 재임포트하거나 Addressables에 수동 등록하세요.");

        if (!silent)
        {
            int show = Mathf.Min(15, missing.Count);
            var preview = string.Join("\n", missing.GetRange(0, show));
            if (missing.Count > show)
            {
                preview += $"\n… 외 {missing.Count - show}건 (콘솔 전체 로그 참고)";
            }

            EditorUtility.DisplayDialog(
                "Addressables 검증",
                $"주소가 없는 ItemId: {missing.Count}건\n\n{preview}",
                "확인");
        }

        return false;
    }

    private static bool TryReadItemId(string fullPath, out string itemId)
    {
        itemId = null;
        if (!File.Exists(fullPath))
        {
            return false;
        }

        string content = File.ReadAllText(fullPath);
        itemId = GetYamlValue(content, "ItemId");
        return !string.IsNullOrWhiteSpace(itemId);
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
}
