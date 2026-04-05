using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// CodexIndex.bin의 HasBody 항목에 대해 Resources/CodexContents/[safeId].txt 존재 여부를 검사합니다.
/// Tools → ARCHÉ → Codex → Validate Codex Content Files
/// </summary>
public static class CodexContentFileValidator
{
    private const string IndexRelative = "Assets/StreamingAssets/CodexIndex.bin";

    private const string ResourcesCodexFolderRelative = "Assets/Resources/CodexContents";

    private static readonly byte[] IndexMagic = Encoding.ASCII.GetBytes("CDXI");

    private const int BinaryFormatVersion = 2;

    [MenuItem("Tools/ARCHÉ/Codex/Validate Codex Content Files")]
    public static void ValidateMenu()
    {
        Validate(false);
    }

    public static void Validate(bool silent)
    {
        ValidateWithResult(silent);
    }

    /// <summary>본문 .txt 누락이 없으면 true. CI/배치 게이트용.</summary>
    public static bool ValidateWithResult(bool silent)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        string indexPath = Path.Combine(projectRoot, IndexRelative).Replace("\\", "/");

        if (!File.Exists(indexPath))
        {
            string msg = $"[CodexContentFileValidator] 색인 파일 없음: {IndexRelative}";
            Debug.LogError(msg);
            if (!silent)
            {
                EditorUtility.DisplayDialog("Codex 본문 검증", msg, "확인");
            }

            return false;
        }

        if (!TryParseIndex(indexPath, out List<CodexMetadata> list) || list == null)
        {
            string msg = "[CodexContentFileValidator] CodexIndex.bin 파싱 실패 (시그니처/버전 확인).";
            Debug.LogError(msg);
            if (!silent)
            {
                EditorUtility.DisplayDialog("Codex 본문 검증", msg, "확인");
            }

            return false;
        }

        var missing = new List<string>();
        int bodyRows = 0;

        for (int i = 0; i < list.Count; i++)
        {
            CodexMetadata meta = list[i];
            if (!meta.HasBody || string.IsNullOrWhiteSpace(meta.Id))
            {
                continue;
            }

            bodyRows++;
            string safeId = SanitizeIdForPath(meta.Id.Trim());
            if (string.IsNullOrEmpty(safeId))
            {
                missing.Add($"{meta.Id} (safeId 비어 있음)");
                continue;
            }

            string txtPath = Path.Combine(projectRoot, ResourcesCodexFolderRelative, safeId + ".txt").Replace("\\", "/");
            if (!File.Exists(txtPath))
            {
                missing.Add($"{meta.Id}  →  {ResourcesCodexFolderRelative}/{safeId}.txt 없음");
            }
        }

        if (missing.Count == 0)
        {
            Debug.Log($"[CodexContentFileValidator] 완료: 본문 행 {bodyRows}개 — Resources .txt 모두 존재.");
            if (!silent)
            {
                EditorUtility.DisplayDialog("Codex 본문 검증", $"본문(HasBody) {bodyRows}개: 누락 파일 없음.", "확인");
            }

            return true;
        }

        foreach (string line in missing)
        {
            Debug.LogWarning($"[CodexContentFileValidator] {line}");
        }

        Debug.LogError($"[CodexContentFileValidator] 누락 {missing.Count}건 / 본문 행 {bodyRows}건. CodexDataBuilder 또는 시트 임포트 후 txt를 생성하세요.");

        if (!silent)
        {
            int show = Mathf.Min(12, missing.Count);
            var preview = string.Join("\n", missing.GetRange(0, show));
            if (missing.Count > show)
            {
                preview += $"\n… 외 {missing.Count - show}건";
            }

            EditorUtility.DisplayDialog(
                "Codex 본문 검증",
                $"Resources에 .txt 없음: {missing.Count}건\n\n{preview}",
                "확인");
        }

        return false;
    }

    private static bool TryParseIndex(string path, out List<CodexMetadata> list)
    {
        list = new List<CodexMetadata>();
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

                    list.Add(meta);
                }
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>CodexManager.LoadContentAsync와 동일 규칙.</summary>
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
