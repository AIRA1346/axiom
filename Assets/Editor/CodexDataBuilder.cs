using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/CodexCSVs/ 폴더 내 모든 CSV를 읽어 CodexIndex.bin과 본문 .txt를 빌드합니다.
/// 각 CSV 파일명(예: Bio.csv)이 해당 시트의 LargeCat(대분류)로 자동 할당됩니다.
/// 10만 개 데이터 대응. Clean Build(기존 파일 삭제 후 재생성).
/// </summary>
public static class CodexDataBuilder
{
    private const string BinaryMagic = "CDXI";
    private const int BinaryVersion = 1;
    private const string CsvInputFolder = "Assets/CodexCSVs";
    private const string IndexOutputPath = "Assets/StreamingAssets/CodexIndex.bin";
    private const string ContentOutputFolder = "Assets/Resources/CodexContents";
    private const string EditorPrefsCsvPath = "CodexDataBuilder_LastCsvPath";
    private const int MaxFilenameLength = 200;
    private const int ProgressUpdateInterval = 2000;
    private static readonly char[] InvalidFilenameChars = Path.GetInvalidFileNameChars();

    /// <summary>CSV 필수 컬럼. LargeCat 비어 있으면 파일명으로 자동 보정.</summary>
    private static readonly string[] RequiredColumns = { "Id", "Title", "LargeCat", "MidCat", "SmallCat", "Summary", "Content" };

    /// <summary>
    /// Assets/CodexCSVs/ 폴더 내 모든 CSV를 한 번에 빌드합니다.
    /// Tools > Codex > Build All Codex Databases 에서 호출됩니다.
    /// </summary>
    public static void BuildAllFromFolder()
    {
        BuildAllInternal();
    }

    /// <summary>
    /// 단일 CSV 파일 선택 대화상자로 빌드 (레거시 호환).
    /// </summary>
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

    /// <summary>
    /// 지정 경로의 단일 CSV에서 빌드합니다. (배치/CI용)
    /// </summary>
    public static void BuildFromPath(string csvPath)
    {
        if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
        {
            Debug.LogError($"[CodexDataBuilder] CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        BuildFromSingleFile(csvPath);
    }

    /// <summary>
    /// CodexImporter에서 다운로드한 구글 시트 CSV 텍스트들로 빌드합니다.
    /// Clean Build 후 CodexIndex.bin과 본문 .txt를 생성합니다.
    /// </summary>
    /// <param name="downloadedSheets">(csvText, largeCat, sourceName) 리스트</param>
    public static void BuildFromDownloadedSheets(List<(string csvText, string largeCat, string sourceName)> downloadedSheets)
    {
        if (downloadedSheets == null || downloadedSheets.Count == 0)
        {
            Debug.LogWarning("[CodexDataBuilder] 빌드할 시트가 없습니다.");
            return;
        }

        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        string contentDir = Path.Combine(projectRoot, ContentOutputFolder).Replace("\\", "/");
        string fullBinPath = Path.Combine(projectRoot, IndexOutputPath).Replace("\\", "/");

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
                var (csvText, largeCat, sourceName) = downloadedSheets[i];
                float progress = 0.1f + (float)i / downloadedSheets.Count * 0.7f;
                EditorUtility.DisplayProgressBar("Codex 빌드", $"{sourceName} ({i + 1}/{downloadedSheets.Count})...", progress);

                int rowCount = ProcessCsvFromText(csvText, largeCat, sourceName, metadataList, seenIds, contentDir, ref contentWritten, ref hasDuplicateError);
                if (rowCount < 0)
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
            string binDir = Path.GetDirectoryName(fullBinPath);
            if (!string.IsNullOrEmpty(binDir))
            {
                Directory.CreateDirectory(binDir);
            }
            WriteBinaryIndex(fullBinPath, metadataList);

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            Debug.Log($"[CodexDataBuilder] 완료 (구글 시트): {downloadedSheets.Count}개 시트, 색인 {metadataList.Count}건, 본문 {contentWritten}개.");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[CodexDataBuilder] 빌드 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// CodexCSVs 폴더 내 모든 CSV를 순회하며 Clean Build 수행.
    /// </summary>
    private static void BuildAllInternal()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        string csvFolderPath = Path.Combine(projectRoot, CsvInputFolder).Replace("\\", "/");
        string contentDir = Path.Combine(projectRoot, ContentOutputFolder).Replace("\\", "/");

        if (!Directory.Exists(csvFolderPath))
        {
            Directory.CreateDirectory(csvFolderPath);
            Debug.LogWarning($"[CodexDataBuilder] {CsvInputFolder} 폴더가 없어 생성했습니다. CSV 파일을 넣은 뒤 다시 빌드하세요.");
            return;
        }

        string[] csvFiles = Directory.GetFiles(csvFolderPath, "*.csv", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).StartsWith("~")).ToArray();

        if (csvFiles.Length == 0)
        {
            Debug.LogWarning($"[CodexDataBuilder] {CsvInputFolder}에 CSV 파일이 없습니다.");
            return;
        }

        var metadataList = new List<CodexMetadata>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int contentWritten = 0;
        bool hasDuplicateError = false;

        try
        {
            // 1. Clean Build: 기존 CodexContents 내 .txt 전부 삭제
            EditorUtility.DisplayProgressBar("Codex 빌드", "기존 본문 파일 삭제 중...", 0f);
            CleanContentFolder(contentDir);

            // 2. 각 CSV 파일 순회 (파일명 = LargeCat)
            for (int fileIdx = 0; fileIdx < csvFiles.Length; fileIdx++)
            {
                string csvPath = csvFiles[fileIdx].Replace("\\", "/");
                string fileName = Path.GetFileNameWithoutExtension(csvPath);
                string largeCat = string.IsNullOrWhiteSpace(fileName) ? "기타" : fileName.Trim();

                float fileProgress = 0.1f + (float)fileIdx / csvFiles.Length * 0.7f;
                EditorUtility.DisplayProgressBar("Codex 빌드", $"{fileName}.csv ({fileIdx + 1}/{csvFiles.Length})...", fileProgress);

                int rowCount = ProcessSingleCsvFile(csvPath, largeCat, metadataList, seenIds, contentDir, ref contentWritten, ref hasDuplicateError);
                if (rowCount < 0)
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

            // 3. CodexIndex.bin 새로 생성
            EditorUtility.DisplayProgressBar("Codex 빌드", "바이너리 색인 쓰는 중...", 0.9f);
            string fullBinPath = Path.Combine(projectRoot, IndexOutputPath).Replace("\\", "/");
            string binDir = Path.GetDirectoryName(fullBinPath);
            if (!string.IsNullOrEmpty(binDir))
            {
                Directory.CreateDirectory(binDir);
            }

            WriteBinaryIndex(fullBinPath, metadataList);

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            Debug.Log($"[CodexDataBuilder] 완료: {csvFiles.Length}개 시트, 색인 {metadataList.Count}건, 본문 {contentWritten}개. CodexIndex.bin, {ContentOutputFolder}");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[CodexDataBuilder] 빌드 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 단일 CSV 파일만 빌드. (기존 동작, LargeCat은 CSV 또는 '기타')
    /// </summary>
    private static void BuildFromSingleFile(string csvPath)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        string contentDir = Path.Combine(projectRoot, ContentOutputFolder).Replace("\\", "/");
        string largeCat = Path.GetFileNameWithoutExtension(csvPath);
        if (string.IsNullOrWhiteSpace(largeCat)) largeCat = "기타";

        var metadataList = new List<CodexMetadata>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int contentWritten = 0;
        bool hasDuplicateError = false;

        try
        {
            EditorUtility.DisplayProgressBar("Codex 빌드", "기존 본문 파일 삭제 중...", 0f);
            CleanContentFolder(contentDir);

            int rowCount = ProcessSingleCsvFile(csvPath, largeCat, metadataList, seenIds, contentDir, ref contentWritten, ref hasDuplicateError);
            if (rowCount < 0 || hasDuplicateError)
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            EditorUtility.DisplayProgressBar("Codex 빌드", "바이너리 색인 쓰는 중...", 0.9f);
            string fullBinPath = Path.Combine(projectRoot, IndexOutputPath).Replace("\\", "/");
            string binDir = Path.GetDirectoryName(fullBinPath);
            if (!string.IsNullOrEmpty(binDir)) Directory.CreateDirectory(binDir);
            WriteBinaryIndex(fullBinPath, metadataList);

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            Debug.Log($"[CodexDataBuilder] 완료: 색인 {metadataList.Count}건, 본문 {contentWritten}개.");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[CodexDataBuilder] 빌드 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// CodexContents 폴더 내 기존 .txt 파일을 모두 삭제합니다.
    /// </summary>
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

    /// <summary>
    /// 단일 CSV 파일을 읽어 메타데이터와 본문을 처리합니다.
    /// 반환: 처리된 행 수. 취소 시 -1.
    /// </summary>
    private static int ProcessSingleCsvFile(string csvPath, string largeCat, List<CodexMetadata> metadataList,
        HashSet<string> seenIds, string contentDir, ref int contentWritten, ref bool hasDuplicateError)
    {
        using (var reader = new StreamReader(csvPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
        {
            return ProcessCsvFromReader(reader, largeCat, Path.GetFileName(csvPath), metadataList, seenIds, contentDir, ref contentWritten, ref hasDuplicateError);
        }
    }

    /// <summary>
    /// CSV 텍스트를 파싱해 메타데이터와 본문을 처리합니다. (구글 시트 다운로드용)
    /// </summary>
    private static int ProcessCsvFromText(string csvText, string largeCat, string sourceName, List<CodexMetadata> metadataList,
        HashSet<string> seenIds, string contentDir, ref int contentWritten, ref bool hasDuplicateError)
    {
        if (string.IsNullOrEmpty(csvText))
        {
            Debug.LogError($"[CodexDataBuilder] CSV 내용이 비어 있습니다: {sourceName}");
            return -1;
        }

        using (var reader = new StringReader(csvText))
        {
            return ProcessCsvFromReader(reader, largeCat, sourceName, metadataList, seenIds, contentDir, ref contentWritten, ref hasDuplicateError);
        }
    }

    /// <summary>
    /// TextReader에서 CSV를 읽어 메타데이터와 본문을 처리합니다.
    /// </summary>
    private static int ProcessCsvFromReader(TextReader reader, string largeCat, string sourceName, List<CodexMetadata> metadataList,
        HashSet<string> seenIds, string contentDir, ref int contentWritten, ref bool hasDuplicateError)
    {
        int processedCount = 0;

        string[] headerRow = ParseCsvLine(reader.ReadLine());
        if (headerRow == null || headerRow.Length == 0)
        {
            Debug.LogError($"[CodexDataBuilder] CSV 헤더가 비어 있습니다: {sourceName}");
            return -1;
        }

        var columnMap = BuildColumnMap(headerRow);
        if (columnMap == null)
        {
            return -1;
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
                    return -1;
                }
            }

            if (!TryReadCsvRow(reader, firstLine, out string[] parts))
            {
                continue;
            }

            if (!TryParseRowFromParts(parts, columnMap, largeCat, out CodexMetadata meta, out string content))
            {
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

            meta.Depth = 3;
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

    /// <summary>따옴표로 감싼 다중 줄 필드를 포함해 CSV 한 행 전체를 읽습니다.</summary>
    private static bool TryReadCsvRow(TextReader reader, string firstLine, out string[] parts)
    {
        parts = null;
        var sb = new StringBuilder(firstLine);
        int quoteCount = 0;
        foreach (char c in firstLine)
        {
            if (c == '"') quoteCount++;
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
                if (c == '"') quoteCount++;
            }
        }

        parts = ParseCsvLine(sb.ToString());
        return parts != null && parts.Length > 0;
    }

    private static bool TryParseRowFromParts(string[] parts, Dictionary<string, int> columnMap, string largeCatOverride, out CodexMetadata meta, out string content)
    {
        meta = default;
        content = null;

        if (!columnMap.ContainsKey("Content") || !columnMap.ContainsKey("Id"))
        {
            return false;
        }

        int contentCol = columnMap["Content"];
        int idCol = columnMap["Id"];
        if (parts == null || parts.Length <= Math.Max(contentCol, idCol))
        {
            return false;
        }

        string Get(int col)
        {
            return col < parts.Length ? (parts[col] ?? "").Trim() : "";
        }

        meta.Id = Get(idCol);
        meta.Title = Get(columnMap["Title"]);
        string csvLargeCat = columnMap.ContainsKey("LargeCat") ? Get(columnMap["LargeCat"]) : "";
        meta.LargeCat = !string.IsNullOrWhiteSpace(csvLargeCat) ? csvLargeCat : largeCatOverride;
        meta.MidCat = columnMap.ContainsKey("MidCat") ? Get(columnMap["MidCat"]) : "";
        meta.SmallCat = columnMap.ContainsKey("SmallCat") ? Get(columnMap["SmallCat"]) : "";
        meta.Summary = columnMap.ContainsKey("Summary") ? Get(columnMap["Summary"]) : "";
        content = Get(contentCol);

        if (string.IsNullOrWhiteSpace(meta.LargeCat))
        {
            meta.LargeCat = "기타";
        }

        // SmallCat 비어 있으면 Id에서 추출 (KNO-대-중-소-일련번호 형식)
        if (string.IsNullOrWhiteSpace(meta.SmallCat) && !string.IsNullOrWhiteSpace(meta.Id))
        {
            var idParts = meta.Id.Split('-');
            if (idParts.Length >= 5)
            {
                meta.SmallCat = idParts[3]?.Trim() ?? "";
            }
        }

        if (string.IsNullOrWhiteSpace(meta.MidCat) && !string.IsNullOrWhiteSpace(meta.Id))
        {
            var idParts = meta.Id.Split('-');
            if (idParts.Length >= 5)
            {
                meta.MidCat = idParts[2]?.Trim() ?? "";
            }
        }

        return true;
    }

    /// <summary>CSV 한 행을 파싱. 따옴표 안의 쉼표·줄바꿈을 처리합니다.</summary>
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

    /// <summary>
    /// CSV 헤더로 컬럼 인덱스 맵 생성.
    /// LargeCat 비어 있으면 파일명(largeCatOverride)으로 보정됩니다.
    /// </summary>
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
                Debug.LogError($"[CodexDataBuilder] 필수 컬럼 누락: {req}. 필요: Id, Title, LargeCat, MidCat, SmallCat, Summary, Content");
                return null;
            }
        }

        return map;
    }

    /// <summary>파일명으로 사용할 수 없거나 위험한 문자를 제거합니다.</summary>
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

            foreach (var m in list)
            {
                writer.Write(m.Id ?? "");
                writer.Write(m.Title ?? "");
                writer.Write(m.LargeCat ?? "");
                writer.Write(m.MidCat ?? "");
                writer.Write(m.SmallCat ?? "");
                writer.Write(m.Summary ?? "");
                writer.Write(m.Depth);
                writer.Write(m.SortOrder);
                writer.Write(m.HasBody);
            }
        }
    }
}
