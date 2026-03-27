using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ADDS(ARCHÉ Decimal Deepening System) 도서관 빌드.
/// Assets/CodexCSVs/ CSV를 읽어 Clean Build 후 CodexIndex.bin과 Resources/CodexContents/*.txt를 생성합니다.
/// </summary>
public static class CodexDataBuilder
{
    private const string BinaryMagic = "CDXI";
    private const int BinaryVersion = 2;
    private const string CsvInputFolder = "Assets/CodexCSVs";
    private const string IndexOutputPath = "Assets/StreamingAssets/CodexIndex.bin";
    private const string ContentOutputFolder = "Assets/Resources/CodexContents";
    private const string EditorPrefsCsvPath = "CodexDataBuilder_LastCsvPath";
    private const int MaxFilenameLength = 200;
    private const int ProgressUpdateInterval = 2000;
    private static readonly char[] InvalidFilenameChars = Path.GetInvalidFileNameChars();

    /// <summary>사용자 취소 등으로 처리 중단.</summary>
    private const int ProgressCancelled = -1;

    /// <summary>Id 형식 또는 Id↔Lv1/Lv2 불일치로 빌드 즉시 중단.</summary>
    private const int IdValidationAborted = -2;

    /// <summary>CSV 필수 컬럼. Id는 KNO-Lv1-Lv2-6자리이며 Lv1·Lv2 열과 반드시 일치해야 합니다.</summary>
    private static readonly string[] RequiredColumns =
    {
        "Id", "Title", "Lv1", "Lv2", "Lv3", "Lv4", "Lv5", "Lv6", "Lv7", "Lv8", "Lv9", "Summary", "Content"
    };

    /// <summary>StreamingAssets에 CodexIndex.bin(v2)을 씁니다. count=0도 유효한 산출물입니다.</summary>
    private static void WriteCodexIndexToStreamingAssets(string projectRoot, List<CodexMetadata> entries)
    {
        if (entries == null)
        {
            entries = new List<CodexMetadata>();
        }

        string fullBinPath = Path.Combine(projectRoot, IndexOutputPath).Replace("\\", "/");
        string binDir = Path.GetDirectoryName(fullBinPath);
        if (!string.IsNullOrEmpty(binDir))
        {
            Directory.CreateDirectory(binDir);
        }

        WriteBinaryIndex(fullBinPath, entries);
    }

    /// <summary>검증 실패 시 본문 폴더를 비우고 빈 색인만 남겨 런타임과 동기화합니다.</summary>
    private static void ResetCodexOutputsToEmpty(string projectRoot, string contentDir)
    {
        CleanContentFolder(contentDir);
        WriteCodexIndexToStreamingAssets(projectRoot, new List<CodexMetadata>());
    }

    public static void BuildAllFromFolder()
    {
        BuildAllInternal();
    }

    public static void BuildFromCsv()
    {
        string lastPath = EditorPrefs.GetString(EditorPrefsCsvPath, "");
        string csvPath = EditorUtility.OpenFilePanel("CSV 파일 선택", Path.GetDirectoryName(lastPath) ?? "", "csv");

        if (string.IsNullOrEmpty(csvPath))
        {
            return;
        }

        EditorPrefs.SetString(EditorPrefsCsvPath, csvPath);
        BuildFromSingleFile(csvPath);
    }

    public static void BuildFromPath(string csvPath)
    {
        if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
        {
            Debug.LogError($"[CodexDataBuilder] CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        BuildFromSingleFile(csvPath);
    }

    /// <summary>구글 시트에서 내려받은 CSV 텍스트들로 Clean Build합니다.</summary>
    public static void BuildFromDownloadedSheets(List<(string csvText, string largeCat, string sourceName)> downloadedSheets)
    {
        if (downloadedSheets == null || downloadedSheets.Count == 0)
        {
            Debug.LogWarning("[CodexDataBuilder] 빌드할 시트가 없습니다.");
            return;
        }

        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        string contentDir = Path.Combine(projectRoot, ContentOutputFolder).Replace("\\", "/");

        var metadataList = new List<CodexMetadata>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int contentWritten = 0;
        bool hasDuplicateError = false;

        try
        {
            EditorUtility.DisplayProgressBar("Codex 빌드", "기존 본문 파일 삭제 중...", 0f);
            CleanContentFolder(contentDir);

            for (int i = 0; i < downloadedSheets.Count; i++)
            {
                (string csvText, _, string sourceName) = downloadedSheets[i];
                float progress = 0.1f + (float)i / downloadedSheets.Count * 0.7f;
                EditorUtility.DisplayProgressBar("Codex 빌드", $"{sourceName} ({i + 1}/{downloadedSheets.Count})...", progress);

                int rowCount = ProcessCsvFromText(csvText, sourceName, metadataList, seenIds, contentDir, ref contentWritten, ref hasDuplicateError);
                if (rowCount == IdValidationAborted)
                {
                    EditorUtility.ClearProgressBar();
                    ResetCodexOutputsToEmpty(projectRoot, contentDir);
                    AssetDatabase.Refresh();
                    Debug.LogError("[CodexDataBuilder] Id/Lv1/Lv2 검증 실패로 빌드를 중단하고 빈 CodexIndex.bin(v2)으로 동기화했습니다.");
                    return;
                }

                if (rowCount == ProgressCancelled)
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }
            }

            if (hasDuplicateError)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError("[CodexDataBuilder] ID 중복이 감지되어 빌드를 중단했습니다. 시트를 확인하세요.");
                return;
            }

            EditorUtility.DisplayProgressBar("Codex 빌드", "바이너리 색인 쓰는 중...", 0.9f);
            WriteCodexIndexToStreamingAssets(projectRoot, metadataList);

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            Debug.Log($"[CodexDataBuilder] 완료 (구글 시트): {downloadedSheets.Count}개 시트, 색인 {metadataList.Count}건, 본문 {contentWritten}개. {IndexOutputPath} 갱신됨.");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[CodexDataBuilder] 빌드 실패: {ex.Message}");
        }
    }

    private static void BuildAllInternal()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        string csvFolderPath = Path.Combine(projectRoot, CsvInputFolder).Replace("\\", "/");
        string contentDir = Path.Combine(projectRoot, ContentOutputFolder).Replace("\\", "/");

        if (!Directory.Exists(csvFolderPath))
        {
            Directory.CreateDirectory(csvFolderPath);
        }

        string[] csvFiles = Directory.GetFiles(csvFolderPath, "*.csv", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).StartsWith("~")).ToArray();

        var metadataList = new List<CodexMetadata>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int contentWritten = 0;
        bool hasDuplicateError = false;

        try
        {
            EditorUtility.DisplayProgressBar("Codex 빌드", "기존 본문 파일 삭제 중...", 0f);
            CleanContentFolder(contentDir);

            if (csvFiles.Length == 0)
            {
                WriteCodexIndexToStreamingAssets(projectRoot, metadataList);
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
                Debug.LogWarning($"[CodexDataBuilder] CSV가 없어 빈 CodexIndex.bin(v2)만 생성했습니다. {CsvInputFolder}에 CSV를 넣고 다시 빌드하세요.");
                return;
            }

            for (int fileIdx = 0; fileIdx < csvFiles.Length; fileIdx++)
            {
                string csvPath = csvFiles[fileIdx].Replace("\\", "/");
                string fileName = Path.GetFileName(csvPath);

                float fileProgress = 0.1f + (float)fileIdx / csvFiles.Length * 0.7f;
                EditorUtility.DisplayProgressBar("Codex 빌드", $"{fileName} ({fileIdx + 1}/{csvFiles.Length})...", fileProgress);

                int rowCount = ProcessSingleCsvFile(csvPath, fileName, metadataList, seenIds, contentDir, ref contentWritten, ref hasDuplicateError);
                if (rowCount == IdValidationAborted)
                {
                    EditorUtility.ClearProgressBar();
                    ResetCodexOutputsToEmpty(projectRoot, contentDir);
                    AssetDatabase.Refresh();
                    Debug.LogError("[CodexDataBuilder] Id/Lv1/Lv2 검증 실패로 빌드를 중단하고 빈 CodexIndex.bin(v2)으로 동기화했습니다.");
                    return;
                }

                if (rowCount == ProgressCancelled)
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }
            }

            if (hasDuplicateError)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError("[CodexDataBuilder] ID 중복이 감지되어 빌드를 중단했습니다. CSV를 확인하세요.");
                return;
            }

            EditorUtility.DisplayProgressBar("Codex 빌드", "바이너리 색인 쓰는 중...", 0.9f);
            WriteCodexIndexToStreamingAssets(projectRoot, metadataList);

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            Debug.Log($"[CodexDataBuilder] 완료: {csvFiles.Length}개 CSV, 색인 {metadataList.Count}건, 본문 {contentWritten}개. {IndexOutputPath}, {ContentOutputFolder}");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[CodexDataBuilder] 빌드 실패: {ex.Message}");
        }
    }

    private static void BuildFromSingleFile(string csvPath)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        string contentDir = Path.Combine(projectRoot, ContentOutputFolder).Replace("\\", "/");
        string label = Path.GetFileName(csvPath);

        var metadataList = new List<CodexMetadata>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int contentWritten = 0;
        bool hasDuplicateError = false;

        try
        {
            EditorUtility.DisplayProgressBar("Codex 빌드", "기존 본문 파일 삭제 중...", 0f);
            CleanContentFolder(contentDir);

            int rowCount = ProcessSingleCsvFile(csvPath, label, metadataList, seenIds, contentDir, ref contentWritten, ref hasDuplicateError);
            if (rowCount == IdValidationAborted)
            {
                EditorUtility.ClearProgressBar();
                ResetCodexOutputsToEmpty(projectRoot, contentDir);
                AssetDatabase.Refresh();
                Debug.LogError("[CodexDataBuilder] Id/Lv1/Lv2 검증 실패로 빌드를 중단하고 빈 CodexIndex.bin(v2)으로 동기화했습니다.");
                return;
            }

            if (rowCount == ProgressCancelled || hasDuplicateError)
            {
                EditorUtility.ClearProgressBar();
                if (hasDuplicateError)
                {
                    Debug.LogError("[CodexDataBuilder] ID 중복이 감지되어 빌드를 중단했습니다.");
                }

                return;
            }

            EditorUtility.DisplayProgressBar("Codex 빌드", "바이너리 색인 쓰는 중...", 0.9f);
            WriteCodexIndexToStreamingAssets(projectRoot, metadataList);

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            Debug.Log($"[CodexDataBuilder] 완료: 색인 {metadataList.Count}건, 본문 {contentWritten}개. {IndexOutputPath} 갱신됨.");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[CodexDataBuilder] 빌드 실패: {ex.Message}");
        }
    }

    private static void CleanContentFolder(string contentDir)
    {
        if (!Directory.Exists(contentDir))
        {
            Directory.CreateDirectory(contentDir);
            return;
        }

        string[] txtFiles = Directory.GetFiles(contentDir, "*.txt", SearchOption.TopDirectoryOnly);
        foreach (string f in txtFiles)
        {
            try
            {
                File.Delete(f);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CodexDataBuilder] 삭제 실패: {f} - {ex.Message}");
            }
        }
    }

    private static int ProcessSingleCsvFile(string csvPath, string sourceName, List<CodexMetadata> metadataList,
        HashSet<string> seenIds, string contentDir, ref int contentWritten, ref bool hasDuplicateError)
    {
        using (var reader = new StreamReader(csvPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
        {
            return ProcessCsvFromReader(reader, sourceName, metadataList, seenIds, contentDir, ref contentWritten, ref hasDuplicateError);
        }
    }

    private static int ProcessCsvFromText(string csvText, string sourceName, List<CodexMetadata> metadataList,
        HashSet<string> seenIds, string contentDir, ref int contentWritten, ref bool hasDuplicateError)
    {
        if (string.IsNullOrEmpty(csvText))
        {
            Debug.LogError($"[CodexDataBuilder] CSV 내용이 비어 있습니다: {sourceName}");
            return ProgressCancelled;
        }

        using (var reader = new StringReader(csvText))
        {
            return ProcessCsvFromReader(reader, sourceName, metadataList, seenIds, contentDir, ref contentWritten, ref hasDuplicateError);
        }
    }

    private static int ProcessCsvFromReader(TextReader reader, string sourceName, List<CodexMetadata> metadataList,
        HashSet<string> seenIds, string contentDir, ref int contentWritten, ref bool hasDuplicateError)
    {
        int processedCount = 0;

        string[] headerRow = ParseCsvLine(reader.ReadLine());
        if (headerRow == null || headerRow.Length == 0)
        {
            Debug.LogError($"[CodexDataBuilder] CSV 헤더가 비어 있습니다: {sourceName}");
            return ProgressCancelled;
        }

        Dictionary<string, int> columnMap = BuildColumnMap(headerRow);
        if (columnMap == null)
        {
            return ProgressCancelled;
        }

        while (reader.ReadLine() is string firstLine)
        {
            if (string.IsNullOrWhiteSpace(firstLine))
            {
                continue;
            }

            processedCount++;
            if (processedCount % ProgressUpdateInterval == 0)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Codex 빌드", $"{sourceName} {processedCount}행...", 0.5f))
                {
                    return ProgressCancelled;
                }
            }

            if (!TryReadCsvRow(reader, firstLine, out string[] parts))
            {
                continue;
            }

            if (!TryParseAddsRow(parts, columnMap, sourceName, out CodexMetadata meta, out string content, out bool abortDueToInvalidId))
            {
                if (abortDueToInvalidId)
                {
                    return IdValidationAborted;
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(meta.Id))
            {
                continue;
            }

            if (!seenIds.Add(meta.Id))
            {
                Debug.LogError($"[CodexDataBuilder] ID 중복: {meta.Id} (소스: {sourceName})");
                hasDuplicateError = true;
                continue;
            }

            meta.Depth = 9;
            meta.SortOrder = metadataList.Count;
            meta.HasBody = !string.IsNullOrWhiteSpace(content);
            metadataList.Add(meta);

            if (!string.IsNullOrWhiteSpace(content))
            {
                string safeId = SanitizeFilename(meta.Id);
                if (!string.IsNullOrEmpty(safeId))
                {
                    try
                    {
                        string filePath = Path.Combine(contentDir, safeId + ".txt").Replace("\\", "/");
                        File.WriteAllText(filePath, content, Encoding.UTF8);
                        contentWritten++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[CodexDataBuilder] 본문 저장 실패 ({meta.Id}): {ex.Message}");
                    }
                }
            }
        }

        return processedCount;
    }

    private static bool TryReadCsvRow(TextReader reader, string firstLine, out string[] parts)
    {
        parts = null;
        var sb = new StringBuilder(firstLine);
        int quoteCount = 0;
        foreach (char c in firstLine)
        {
            if (c == '"')
            {
                quoteCount++;
            }
        }

        while (quoteCount % 2 != 0)
        {
            string next = reader.ReadLine();
            if (next == null)
            {
                break;
            }

            sb.Append('\n').Append(next);
            foreach (char c in next)
            {
                if (c == '"')
                {
                    quoteCount++;
                }
            }
        }

        parts = ParseCsvLine(sb.ToString());
        return parts != null && parts.Length > 0;
    }

    private static bool TryParseAddsRow(string[] parts, Dictionary<string, int> columnMap, string sourceName,
        out CodexMetadata meta, out string content, out bool abortBuildDueToInvalidId)
    {
        meta = default;
        content = null;
        abortBuildDueToInvalidId = false;

        if (parts == null || columnMap == null)
        {
            return false;
        }

        string Get(string col)
        {
            if (!columnMap.TryGetValue(col, out int idx) || idx < 0 || idx >= parts.Length)
            {
                return "";
            }

            return (parts[idx] ?? "").Trim();
        }

        string id = Get("Id");
        string title = Get("Title");
        string lv1 = Get("Lv1");
        string lv2 = Get("Lv2");
        string lv3 = Get("Lv3");
        string lv4 = Get("Lv4");
        string lv5 = Get("Lv5");
        string lv6 = Get("Lv6");
        string lv7 = Get("Lv7");
        string lv8 = Get("Lv8");
        string lv9 = Get("Lv9");
        string summary = Get("Summary");
        content = Get("Content");

        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        if (!TryValidateAddsId(id, lv1, lv2, sourceName, out string idError))
        {
            Debug.LogError($"[CodexDataBuilder] 빌드 즉시 중단: {idError} (소스: {sourceName}, Id={id})");
            abortBuildDueToInvalidId = true;
            return false;
        }

        meta = new CodexMetadata
        {
            Id = id,
            Title = title,
            Summary = summary,
            Level1Root = lv1,
            Level2Source = lv2,
            Level3Field = lv3,
            Level4Nature = lv4,
            Level5Lineage = lv5,
            Level6Role = lv6,
            Level7Rank = lv7,
            Level8Species = lv8,
            Level9Identity = lv9,
            Depth = 9,
            SortOrder = 0,
            HasBody = false
        };

        return true;
    }

    /// <summary>Id가 KNO-[Lv1]-[Lv2]-[6자리 숫자] 형식이며 CSV의 Lv1·Lv2와 일치하는지 검증합니다.</summary>
    private static bool TryValidateAddsId(string id, string lv1, string lv2, string sourceName, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            sourceName = "?";
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            error = "Id가 비어 있습니다.";
            return false;
        }

        string[] segments = id.Split('-');
        if (segments.Length != 4)
        {
            error = "Id는 KNO-Lv1-Lv2-6자리(총 4구간) 형식이어야 합니다.";
            return false;
        }

        if (!string.Equals(segments[0], "KNO", StringComparison.OrdinalIgnoreCase))
        {
            error = "Id는 KNO로 시작해야 합니다.";
            return false;
        }

        string idLv1 = segments[1]?.Trim() ?? "";
        string idLv2 = segments[2]?.Trim() ?? "";
        string serial = segments[3]?.Trim() ?? "";

        if (serial.Length != 6 || !serial.All(char.IsDigit))
        {
            error = "Id 마지막 구간은 6자리 숫자(일련번호)여야 합니다.";
            return false;
        }

        if (!string.Equals(idLv1, lv1?.Trim() ?? "", StringComparison.OrdinalIgnoreCase))
        {
            error = $"Id의 위계1({idLv1})과 열 Lv1({lv1})이 일치하지 않습니다.";
            return false;
        }

        if (!string.Equals(idLv2, lv2?.Trim() ?? "", StringComparison.OrdinalIgnoreCase))
        {
            error = $"Id의 위계2({idLv2})과 열 Lv2({lv2})이 일치하지 않습니다.";
            return false;
        }

        return true;
    }

    private static string[] ParseCsvLine(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return null;
        }

        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && c == ',')
            {
                result.Add(sb.ToString().Trim());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        result.Add(sb.ToString().Trim());
        return result.ToArray();
    }

    private static Dictionary<string, int> BuildColumnMap(string[] headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
        {
            string h = (headers[i] ?? "").Trim();
            if (!string.IsNullOrEmpty(h))
            {
                map[h] = i;
            }
        }

        foreach (string req in RequiredColumns)
        {
            if (!map.ContainsKey(req))
            {
                Debug.LogError($"[CodexDataBuilder] 필수 컬럼 누락: {req}. 필요: {string.Join(", ", RequiredColumns)}");
                return null;
            }
        }

        return map;
    }

    private static string SanitizeFilename(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return "";
        }

        var sb = new StringBuilder(id.Length);
        foreach (char c in id)
        {
            if (Array.IndexOf(InvalidFilenameChars, c) >= 0 || c == '/' || c == '\\' || c == ':' || c == '*' || c == '?' || c == '"' || c == '<' || c == '>' || c == '|')
            {
                sb.Append('_');
            }
            else
            {
                sb.Append(c);
            }
        }

        string result = sb.ToString().Trim(' ', '.', '_');
        if (result.Length > MaxFilenameLength)
        {
            result = result.Substring(0, MaxFilenameLength);
        }

        return result;
    }

    private static void WriteBinaryIndex(string fullPath, List<CodexMetadata> list)
    {
        using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 65536))
        using (var writer = new BinaryWriter(fs, Encoding.UTF8))
        {
            byte[] magic = Encoding.ASCII.GetBytes(BinaryMagic);
            writer.Write(magic);
            writer.Write(BinaryVersion);
            writer.Write(list.Count);

            foreach (CodexMetadata m in list)
            {
                writer.Write(m.Id ?? "");
                writer.Write(m.Title ?? "");
                writer.Write(m.Summary ?? "");
                writer.Write(m.Level1Root ?? "");
                writer.Write(m.Level2Source ?? "");
                writer.Write(m.Level3Field ?? "");
                writer.Write(m.Level4Nature ?? "");
                writer.Write(m.Level5Lineage ?? "");
                writer.Write(m.Level6Role ?? "");
                writer.Write(m.Level7Rank ?? "");
                writer.Write(m.Level8Species ?? "");
                writer.Write(m.Level9Identity ?? "");
                writer.Write(m.SortOrder);
                writer.Write(m.HasBody);
            }
        }
    }
}
