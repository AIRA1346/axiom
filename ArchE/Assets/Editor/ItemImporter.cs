using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public sealed class SheetEntry
{
    public bool IsEnabled = true;
    public string SheetUrl = "";
    public string MainCategory = "Material";
}

public sealed class ItemImporter : EditorWindow
{
    private static readonly HashSet<string> ReservedCSharpKeywords = new HashSet<string>(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum",
        "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
        "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
        "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
        "ushort", "using", "virtual", "void", "volatile", "while"
    };

    private sealed class CategorySyncResult
    {
        public string GeneratedCode;
        public int MainCategoryCount;
        public int MiddleCategoryCount;
        public int SubCategoryCount;
        public int MainToMiddleMapCount;
        public int MiddleToSubMapCount;
    }

    private sealed class ItemImportPathInfo
    {
        public string FolderPath;
        public string AssetPath;
    }

    private const string ResourceFolderPath = "Assets/Resources/Items";
    private const string IconFolderPath = "Assets/UI/Icons/Items";
    private const string DefaultIconFileName = "DefaultIcon";
    private const string SheetListKey = "GSI.ItemImporter.SheetList";
    private const string CategorySheetUrlKey = "GSI.ItemImporter.CategorySheetUrl";
    private const string DefaultCategorySheetUrl = "";
    private const string ItemDefinitionsPath = "Assets/Scripts/Items/ItemDefinitions.cs";
    private const int ProgressUpdateInterval = 100;
    private const int ProgressLogInterval = 1000;
    private const int MinimumImportedItemCountForDeletion = 10;
    private const int RequestTimeoutSeconds = 15;
    private static readonly Dictionary<string, Sprite> ItemIconCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
    private static readonly string[] DefaultMainCategories = { "Material", "Equipment", "Consumable", "Crafting", "Quest", "Etc" };

    private List<SheetEntry> _sheetEntries = new List<SheetEntry>();
    private string _categorySheetUrl;
    private bool _isImporting;
    private string _lastStatusMessage = "대기 중";
    private Vector2 _sheetListScroll;

    [MenuItem("Tools/ARCHÉ/Google Sheet/Item Importer")]
    private static void OpenWindow()
    {
        ItemImporter window = GetWindow<ItemImporter>("Item Importer Settings");
        window.minSize = new Vector2(580f, 320f);
        window.Show();
    }

    private void OnEnable()
    {
        _categorySheetUrl = EditorPrefs.GetString(CategorySheetUrlKey, DefaultCategorySheetUrl);
        LoadSheetList();
    }

    private void LoadSheetList()
    {
        string json = EditorPrefs.GetString(SheetListKey, "");
        if (string.IsNullOrEmpty(json))
        {
            _sheetEntries = new List<SheetEntry> { new SheetEntry { SheetUrl = "", MainCategory = "Material" } };
            return;
        }
        try
        {
            var wrapper = JsonUtility.FromJson<SheetListWrapper>(json);
            _sheetEntries = wrapper?.Entries ?? new List<SheetEntry> { new SheetEntry() };
            if (_sheetEntries.Count == 0) _sheetEntries.Add(new SheetEntry());
        }
        catch
        {
            _sheetEntries = new List<SheetEntry> { new SheetEntry() };
        }
    }

    private void SaveSheetList()
    {
        var wrapper = new SheetListWrapper { Entries = _sheetEntries };
        EditorPrefs.SetString(SheetListKey, JsonUtility.ToJson(wrapper));
    }

    [Serializable]
    private class SheetListWrapper { public List<SheetEntry> Entries; }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Item Importer Settings (대분류별 분할 임포트)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(_isImporting);

        EditorGUILayout.LabelField("Items 시트 목록 (☑=임포트 대상, URL + 대분류)", EditorStyles.boldLabel);
        _sheetListScroll = EditorGUILayout.BeginScrollView(_sheetListScroll, GUILayout.MaxHeight(160f));
        for (int i = 0; i < _sheetEntries.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            bool enabled = EditorGUILayout.Toggle(_sheetEntries[i].IsEnabled, GUILayout.Width(18f));
            if (enabled != _sheetEntries[i].IsEnabled)
            {
                _sheetEntries[i].IsEnabled = enabled;
                SaveSheetList();
            }
            EditorGUILayout.LabelField($"{i + 1}", GUILayout.Width(18f));
            string url = EditorGUILayout.TextField(_sheetEntries[i].SheetUrl ?? "", GUILayout.ExpandWidth(true));
            string mainCat = EditorGUILayout.TextField(_sheetEntries[i].MainCategory ?? "Material", GUILayout.Width(95f));
            if (url != _sheetEntries[i].SheetUrl || mainCat != _sheetEntries[i].MainCategory)
            {
                _sheetEntries[i].SheetUrl = url;
                _sheetEntries[i].MainCategory = string.IsNullOrWhiteSpace(mainCat) ? "Material" : mainCat.Trim();
                SaveSheetList();
            }
            if (GUILayout.Button("−", GUILayout.Width(22f)) && _sheetEntries.Count > 1)
            {
                _sheetEntries.RemoveAt(i);
                SaveSheetList();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        if (GUILayout.Button("+ 시트 추가", GUILayout.Width(100f)))
        {
            _sheetEntries.Add(new SheetEntry { MainCategory = "Material" });
            SaveSheetList();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Categories CSV URL");
        string updatedCategoryUrl = EditorGUILayout.TextField(_categorySheetUrl ?? string.Empty);
        if (updatedCategoryUrl != _categorySheetUrl)
        {
            _categorySheetUrl = updatedCategoryUrl;
            EditorPrefs.SetString(CategorySheetUrlKey, _categorySheetUrl);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Import Items", GUILayout.Height(32f)))
        {
            ImportItems();
        }
        if (GUILayout.Button("Sync Categories", GUILayout.Height(32f)))
        {
            SyncCategories();
        }

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(_lastStatusMessage, MessageType.Info);
    }

    private async void ImportItems()
    {
        await RunImportInternalAsync(null);
    }

    /// <summary>구글 시트 통합 파이프라인: 아이템 임포트 완료 후 콜백.</summary>
    public void RunImportWithCallback(Action onComplete)
    {
        _ = RunImportInternalAsync(onComplete);
    }

    private async Task RunImportInternalAsync(Action onComplete)
    {
        if (_isImporting)
        {
            onComplete?.Invoke();
            return;
        }

        _isImporting = true;
        _lastStatusMessage = "CSV 다운로드 준비 중...";
        Repaint();

        try
        {
            await RunImportAsync();
        }
        finally
        {
            _isImporting = false;
            EditorUtility.ClearProgressBar();
            Repaint();
            onComplete?.Invoke();
        }
    }

    private async void SyncCategories()
    {
        if (_isImporting)
        {
            return;
        }

        _isImporting = true;
        _lastStatusMessage = "카테고리 동기화 준비 중...";
        Repaint();

        try
        {
            await RunCategorySyncAsync();
        }
        finally
        {
            _isImporting = false;
            EditorUtility.ClearProgressBar();
            Repaint();
        }
    }

    private async Task RunImportAsync()
    {
        try
        {
            var validSheets = _sheetEntries.Where(s => s.IsEnabled && !string.IsNullOrWhiteSpace(s.SheetUrl)).ToList();
            if (validSheets.Count == 0)
            {
                _lastStatusMessage = "임포트할 시트를 체크하고 URL을 입력해 주세요.";
                Debug.LogError("ItemImporter: 체크된 시트가 없거나 URL이 비어 있습니다.");
                return;
            }

            string absoluteResourceFolderPath = GetAbsoluteProjectPath(ResourceFolderPath);
            Directory.CreateDirectory(absoluteResourceFolderPath);
            EnsureFolderExists(ResourceFolderPath);
            Debug.Log($"ItemImporter: 아이템 저장 경로 확인 - {absoluteResourceFolderPath}");

            ItemIconCache.Clear();
            _defaultIconCache = null;
            HashSet<string> ensuredFolderPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> existingAssetPathsByItemId = BuildExistingItemAssetPathMap();
            AddressableAssetSettings addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            Dictionary<string, AddressableAssetGroup> addressableGroupCache = BuildAddressableGroupCache(addressableSettings);

            int totalImported = 0;
            int totalCreated = 0;
            int totalUpdated = 0;
            int totalDeleted = 0;
            var importedIdsByMainCategory = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var importStopwatch = System.Diagnostics.Stopwatch.StartNew();

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int sheetIdx = 0; sheetIdx < validSheets.Count; sheetIdx++)
                {
                    SheetEntry entry = validSheets[sheetIdx];
                    string mainCategoryOverride = string.IsNullOrWhiteSpace(entry.MainCategory) ? "Material" : SanitizePathSegment(SanitizeEnumMemberName(entry.MainCategory.Trim()));

                    _lastStatusMessage = $"시트 {sheetIdx + 1}/{validSheets.Count} ({mainCategoryOverride}) 다운로드 중...";
                    Repaint();

                    string convertedUrl = ConvertGoogleSheetUrlToCsv(entry.SheetUrl);
                    Debug.Log($"ItemImporter: 시트 {mainCategoryOverride} - {convertedUrl}");

                    using UnityWebRequest request = UnityWebRequest.Get(convertedUrl);
                    request.timeout = RequestTimeoutSeconds;

                    if (!await WaitForWebRequestAsync(request, "Item Importer", $"Downloading {mainCategoryOverride}..."))
                    {
                        Debug.LogError($"ItemImporter: 시트 다운로드 타임아웃 또는 취소 - {mainCategoryOverride}");
                        continue;
                    }

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        _lastStatusMessage = $"CSV 다운로드 실패: {request.error}";
                        Debug.LogError($"ItemImporter: CSV 다운로드 실패 - {request.error}\nURL: {convertedUrl}\n상세: {request.downloadHandler?.text?.Substring(0, Math.Min(200, request.downloadHandler?.text?.Length ?? 0)) ?? ""}");
                        continue;
                    }

                    string csvText = StripUtf8Bom(request.downloadHandler.text);
                    List<List<string>> rows = ParseCsv(csvText);
                    if (rows.Count < 2)
                    {
                        Debug.LogWarning($"ItemImporter: 시트 {mainCategoryOverride}에 데이터가 없습니다.");
                        continue;
                    }

                    Dictionary<string, int> rawHeaderMap = BuildHeaderMap(rows[0]);
                    Dictionary<string, int> headerMap = ResolveItemsHeaderMap(rawHeaderMap);
                    if (headerMap == null)
                    {
                        string foundHeaders = string.Join(", ", rawHeaderMap.Keys);
                        Debug.LogError($"ItemImporter: 시트 {mainCategoryOverride} 헤더 오류 - ItemId, MainCategory, MiddleCategory, SubCategory 필요. 발견: [{foundHeaders}]");
                        continue;
                    }

                    if (!ValidateItemRows(rows, headerMap))
                    {
                        Debug.LogError($"ItemImporter: 시트 {mainCategoryOverride} 유효성 검사 실패.");
                        continue;
                    }

                    int endRowExclusive = rows.Count;

                    HashSet<string> sheetImportedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    int processedCount = 0;

                    for (int rowIndex = 1; rowIndex < endRowExclusive; rowIndex++)
                    {
                        List<string> row = rows[rowIndex];
                        string itemId = GetCell(row, headerMap, "ItemId");
                        if (string.IsNullOrWhiteSpace(itemId)) continue;

                        processedCount++;
                        sheetImportedIds.Add(itemId);

                        if (rowIndex == 1 || rowIndex % ProgressUpdateInterval == 0 || rowIndex == endRowExclusive - 1)
                        {
                            float p = (sheetIdx + (float)rowIndex / endRowExclusive) / validSheets.Count;
                            EditorUtility.DisplayProgressBar("Item Importer", $"[{mainCategoryOverride}] {itemId}...", 0.1f + 0.8f * p);
                        }

                        if (processedCount % ProgressLogInterval == 0)
                        {
                            Debug.Log($"ItemImporter: [{mainCategoryOverride}] {processedCount}개 처리 완료");
                        }

                        ItemImportPathInfo pathInfo = BuildItemAssetPathInfo(row, headerMap, itemId, mainCategoryOverride);
                        EnsureFolderExistsWithDisk(pathInfo.FolderPath, ensuredFolderPaths);

                        (ItemData itemData, bool wasCreated) = LoadOrCreateItemAsset(itemId, pathInfo.AssetPath, existingAssetPathsByItemId);
                        if (itemData == null)
                        {
                            Debug.LogError($"ItemImporter: ItemData 실패 - {itemId}");
                            continue;
                        }

                        if (wasCreated) totalCreated++; else totalUpdated++;

                        ApplyRowToItemData(itemData, row, headerMap);
                        RegisterAddressableEntry(addressableSettings, addressableGroupCache, itemData, pathInfo.AssetPath);
                        EditorUtility.SetDirty(itemData);
                        existingAssetPathsByItemId[itemId] = pathInfo.AssetPath;
                        totalImported++;
                    }

                    importedIdsByMainCategory[mainCategoryOverride] = sheetImportedIds;

                    if (sheetImportedIds.Count >= MinimumImportedItemCountForDeletion)
                    {
                        totalDeleted += CleanupDeletedItemsInMainCategory(mainCategoryOverride, sheetImportedIds);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            if (addressableSettings != null) EditorUtility.SetDirty(addressableSettings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (totalImported > 0)
            {
                ItemMetadataBuilder.Build();
            }

            importStopwatch.Stop();
            _lastStatusMessage = $"{totalImported}개 아이템 처리 완료 ({totalCreated} 신규, {totalUpdated} 업데이트, {totalDeleted} 삭제)";

            LogImportSummary(validSheets.Count, totalCreated, totalUpdated, totalDeleted, importStopwatch.Elapsed);
        }
        catch (Exception exception)
        {
            _lastStatusMessage = $"임포트 중 예외: {exception.Message}";
            Debug.LogError($"ItemImporter: 예외\n{exception}");
        }
    }

    private sealed class CategoryValidationCache
    {
        public HashSet<string> MainNames;
        public HashSet<string> MiddleNames;
        public HashSet<string> SubNames;
        public Dictionary<string, HashSet<string>> MainToMiddle;
        public Dictionary<string, HashSet<string>> MiddleToSub;
        public bool IsValid;
    }

    private static bool ValidateItemRows(List<List<string>> rows, Dictionary<string, int> headerMap)
    {
        CategoryValidationCache cache = BuildCategoryValidationCache();

        if (cache == null || !cache.IsValid)
        {
            Debug.LogWarning("ItemImporter Validation: 카테고리 캐시를 로드하지 못해 카테고리 검증을 건너뜁니다.");
        }

        bool hasError = false;
        Dictionary<string, int> itemIdLines = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            List<string> row = rows[rowIndex];
            int lineNumber = rowIndex + 1;
            string itemId = GetCell(row, headerMap, "ItemId");
            string itemName = GetCell(row, headerMap, "ItemName");

            if (string.IsNullOrWhiteSpace(itemId))
            {
                Debug.LogError($"ItemImporter Validation: {lineNumber}번째 줄에 ItemId가 비어 있습니다.");
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(itemName))
            {
                Debug.LogError($"ItemImporter Validation: {lineNumber}번째 줄의 아이템(ItemId: {itemId})에 ItemName이 비어 있습니다.");
                hasError = true;
            }

            if (!string.IsNullOrWhiteSpace(itemId))
            {
                if (itemIdLines.TryGetValue(itemId, out int existingLine))
                {
                    Debug.LogError($"ItemImporter Validation: {lineNumber}번째 줄의 ItemId '{itemId}'가 중복되었습니다. 최초 등장 줄: {existingLine}.");
                    hasError = true;
                }
                else
                {
                    itemIdLines[itemId] = lineNumber;
                }
            }

            if (cache != null && cache.IsValid)
            {
                ValidateCategoryCombination(row, headerMap, lineNumber, itemId, cache, ref hasError);
            }
        }

        return !hasError;
    }

    private static CategoryValidationCache BuildCategoryValidationCache()
    {
        var cache = new CategoryValidationCache
        {
            MainNames = new HashSet<string>(StringComparer.Ordinal),
            MiddleNames = new HashSet<string>(StringComparer.Ordinal),
            SubNames = new HashSet<string>(StringComparer.Ordinal),
            MainToMiddle = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal),
            MiddleToSub = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        };

        Type mainType = FindTypeByName("ItemMainCategory");
        Type middleType = FindTypeByName("ItemMiddleCategory");
        Type subType = FindTypeByName("ItemSubCategory");

        if (mainType == null || !mainType.IsEnum || middleType == null || !middleType.IsEnum || subType == null || !subType.IsEnum)
        {
            return null;
        }

        foreach (string name in Enum.GetNames(mainType))
        {
            cache.MainNames.Add(name);
        }

        foreach (string name in Enum.GetNames(middleType))
        {
            cache.MiddleNames.Add(name);
        }

        foreach (string name in Enum.GetNames(subType))
        {
            cache.SubNames.Add(name);
        }

        if (!TryBuildMainToMiddleCache(cache) || !TryBuildMiddleToSubCache(cache))
        {
            return null;
        }

        cache.IsValid = true;
        return cache;
    }

    private static bool TryBuildMainToMiddleCache(CategoryValidationCache cache)
    {
        if (!TryGetCategoryMapAllPairs("MainToMiddleMap", out var pairs))
        {
            return false;
        }

        foreach (var (mainKey, middleValues) in pairs)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (string v in middleValues)
            {
                set.Add(v);
            }

            cache.MainToMiddle[mainKey] = set;
        }

        return true;
    }

    private static bool TryBuildMiddleToSubCache(CategoryValidationCache cache)
    {
        if (!TryGetCategoryMapAllPairs("MiddleToSubMap", out var pairs))
        {
            return false;
        }

        foreach (var (middleKey, subValues) in pairs)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (string v in subValues)
            {
                set.Add(v);
            }

            cache.MiddleToSub[middleKey] = set;
        }

        return true;
    }

    private static void ValidateCategoryCombination(
        List<string> row,
        Dictionary<string, int> headerMap,
        int lineNumber,
        string itemId,
        CategoryValidationCache cache,
        ref bool hasError)
    {
        string mainRaw = GetCell(row, headerMap, "MainCategory");
        string middleRaw = GetCell(row, headerMap, "MiddleCategory");
        string subRaw = GetCell(row, headerMap, "SubCategory");

        string mainCategory = ResolveToCachedName(mainRaw, "None", cache.MainNames);
        string middleCategory = ResolveToCachedName(middleRaw, "None", cache.MiddleNames);
        string subCategory = ResolveToCachedName(subRaw, "None", cache.SubNames);

        if (mainCategory == null || middleCategory == null || subCategory == null)
        {
            return;
        }

        if (!cache.MainToMiddle.TryGetValue(mainCategory, out HashSet<string> allowedMiddles) || !allowedMiddles.Contains(middleCategory))
        {
            Debug.LogError($"ItemImporter Validation: {lineNumber}번째 줄의 아이템(ItemId: {itemId})은 MainCategory '{mainCategory}'에 MiddleCategory '{middleCategory}'를 사용할 수 없습니다.");
            hasError = true;
            return;
        }

        if (!cache.MiddleToSub.TryGetValue(middleCategory, out HashSet<string> allowedSubs) || !allowedSubs.Contains(subCategory))
        {
            Debug.LogError($"ItemImporter Validation: {lineNumber}번째 줄의 아이템(ItemId: {itemId})은 MiddleCategory '{middleCategory}'에 SubCategory '{subCategory}'를 사용할 수 없습니다.");
            hasError = true;
        }
    }

    private static string ResolveToCachedName(string rawValue, string fallback, HashSet<string> validNames)
    {
        string sanitized = string.IsNullOrWhiteSpace(rawValue)
            ? fallback
            : SanitizeEnumMemberName(rawValue.Trim());

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = fallback;
        }

        return validNames.Contains(sanitized) ? sanitized : null;
    }

    private static bool TryGetCategoryMapAllPairs(string fieldName, out List<(string Key, List<string> Values)> allPairs)
    {
        allPairs = new List<(string, List<string>)>();
        Type mapContainerType = FindTypeByName("CategoryDefinitionMaps");

        if (mapContainerType == null)
        {
            return false;
        }

        FieldInfo fieldInfo = mapContainerType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
        if (fieldInfo == null)
        {
            return false;
        }

        object fieldValue = fieldInfo.GetValue(null);
        if (fieldValue is not IEnumerable enumerable)
        {
            return false;
        }

        foreach (object entry in enumerable)
        {
            if (entry == null)
            {
                continue;
            }

            Type entryType = entry.GetType();
            PropertyInfo keyProperty = entryType.GetProperty("Key");
            PropertyInfo valueProperty = entryType.GetProperty("Value");

            if (keyProperty == null || valueProperty == null)
            {
                continue;
            }

            object keyObject = keyProperty.GetValue(entry);
            object valueObject = valueProperty.GetValue(entry);

            if (valueObject is not IEnumerable valueEnumerable)
            {
                continue;
            }

            var values = new List<string>();
            foreach (object value in valueEnumerable)
            {
                if (value != null)
                {
                    values.Add(value.ToString());
                }
            }

            allPairs.Add((keyObject?.ToString() ?? string.Empty, values));
        }

        return allPairs.Count > 0;
    }

    private static bool TryParseCategoryEnum<TEnum>(string rawValue, TEnum emptyFallback, out TEnum parsedValue)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            parsedValue = emptyFallback;
            return true;
        }

        return Enum.TryParse(rawValue.Trim(), true, out parsedValue);
    }

    private async Task RunCategorySyncAsync()
    {
        if (string.IsNullOrWhiteSpace(_categorySheetUrl))
        {
            _lastStatusMessage = "Categories CSV URL이 비어 있습니다.";
            Debug.LogError("ItemImporter: Categories CSV URL을 먼저 입력해 주세요.");
            return;
        }

        _lastStatusMessage = "카테고리 CSV 다운로드 중...";
        Repaint();

        string convertedCategoryUrl = ConvertGoogleSheetUrlToCsv(_categorySheetUrl);
        using UnityWebRequest request = UnityWebRequest.Get(convertedCategoryUrl);
        request.timeout = 30;

        if (!await WaitForWebRequestAsync(request, "Category Sync", "Downloading category CSV data..."))
        {
            return;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            _lastStatusMessage = $"카테고리 CSV 다운로드 실패: {request.error}";
            Debug.LogError($"ItemImporter: 카테고리 CSV 다운로드 실패 - {request.error}");
            return;
        }

        string csvText = StripUtf8Bom(request.downloadHandler.text);
        List<List<string>> rows = ParseCsv(csvText);

        if (rows.Count < 2)
        {
            _lastStatusMessage = "카테고리 데이터가 없습니다.";
            Debug.LogWarning("ItemImporter: Categories 시트에 가져올 데이터가 없습니다.");
            return;
        }

        Dictionary<string, int> rawHeaderMap = BuildHeaderMap(rows[0]);
        Dictionary<string, int> headerMap = ResolveCategoryHeaderMap(rawHeaderMap);

        if (headerMap == null)
        {
            string foundHeaders = string.Join(", ", rawHeaderMap.Keys);
            _lastStatusMessage = "Categories Header가 올바르지 않습니다.";
            Debug.LogError(
                "ItemImporter: Categories 시트는 MainCategory, MiddleCategory, SubCategory 헤더를 포함해야 합니다.\n"
                + $"지원 형식: 영문(MainCategory/MiddleCategory/SubCategory) 또는 한글(대분류/중분류/소분류)\n"
                + $"실제 발견된 헤더: [{foundHeaders}]");
            return;
        }

        Debug.Log("ItemImporter: 기존 ItemDefinitions.cs 초기화 시도");
        _lastStatusMessage = "기존 ItemDefinitions.cs 초기화 시도";
        Repaint();

        Debug.Log("ItemImporter: 카테고리 데이터 분석 시작");
        _lastStatusMessage = "카테고리 데이터 분석 중...";
        Repaint();

        CategorySyncResult syncResult = GenerateItemDefinitionsCode(rows, headerMap);

        if (syncResult == null
            || syncResult.MainCategoryCount < 1
            || syncResult.MiddleCategoryCount < 1
            || syncResult.SubCategoryCount < 1)
        {
            _lastStatusMessage = "None 외의 카테고리가 없어 동기화를 중단했습니다.";
            Debug.LogError("ItemImporter: None 외에 유효한 카테고리가 발견되지 않아 ItemDefinitions.cs 생성을 중단합니다.");
            return;
        }

        Debug.Log("ItemImporter: 새 ItemDefinitions.cs 생성 시작");
        _lastStatusMessage = "새 ItemDefinitions.cs 생성 중...";
        Repaint();

        string itemDefinitionsFullPath = GetAbsoluteProjectPath(ItemDefinitionsPath);

        File.WriteAllText(itemDefinitionsFullPath, GenerateFallbackItemDefinitionsCode(), Encoding.UTF8);
        AssetDatabase.ImportAsset(ItemDefinitionsPath, ImportAssetOptions.ForceUpdate);
        await WaitForEditorDelayCallAsync();

        File.WriteAllText(itemDefinitionsFullPath, syncResult.GeneratedCode, Encoding.UTF8);
        AssetDatabase.ImportAsset(ItemDefinitionsPath, ImportAssetOptions.ForceUpdate);
        await WaitForEditorDelayCallAsync();

        Debug.Log(
            "ItemImporter Category Sync Report\n"
            + $"- Main Categories: {syncResult.MainCategoryCount}\n"
            + $"- Middle Categories: {syncResult.MiddleCategoryCount}\n"
            + $"- Sub Categories: {syncResult.SubCategoryCount}\n"
            + $"- MainToMiddle Mappings: {syncResult.MainToMiddleMapCount}\n"
            + $"- MiddleToSub Mappings: {syncResult.MiddleToSubMapCount}");

        AssetDatabase.Refresh();
        await WaitForEditorDelayCallAsync();

        _lastStatusMessage = "카테고리 동기화 완료";
        Debug.Log("ItemImporter: ItemDefinitions.cs를 Categories 시트 기준으로 갱신했습니다.");
    }

    private static int CleanupDeletedItemsInMainCategory(string mainCategory, HashSet<string> importedItemIds)
    {
        if (importedItemIds == null || importedItemIds.Count == 0)
        {
            Debug.LogWarning($"ItemImporter: [{mainCategory}] 시트 데이터가 비어 있어 삭제 동기화는 건너뜁니다.");
            return 0;
        }

        if (importedItemIds.Count < MinimumImportedItemCountForDeletion)
        {
            Debug.LogWarning(
                $"ItemImporter: [{mainCategory}] 가져온 ItemId 수가 {importedItemIds.Count}개로 삭제 동기화 건너뜀 (최소 {MinimumImportedItemCountForDeletion}개)");
            return 0;
        }

        string safeMain = NormalizeFolderName(SanitizePathSegment(SanitizeEnumMemberName(mainCategory)));
        string mainCategoryFolder = $"{ResourceFolderPath}/{safeMain}";
        if (!mainCategoryFolder.StartsWith(ResourceFolderPath, StringComparison.Ordinal))
        {
            Debug.LogError($"ItemImporter: 잘못된 대분류 경로 - {mainCategory}");
            return 0;
        }

        AddressableAssetSettings addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { mainCategoryFolder });

        int deleted = 0;
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string normAssetPath = assetPath.Replace('\\', '/');
            if (!normAssetPath.StartsWith(mainCategoryFolder + "/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
            string itemId = itemData != null && !string.IsNullOrWhiteSpace(itemData.ItemId)
                ? itemData.ItemId
                : System.IO.Path.GetFileNameWithoutExtension(assetPath);

            if (string.IsNullOrWhiteSpace(itemId) || importedItemIds.Contains(itemId))
            {
                continue;
            }

            if (AssetDatabase.DeleteAsset(assetPath))
            {
                RemoveAddressableEntry(addressableSettings, guid, itemId);
                deleted++;
            }
        }

        if (deleted > 0)
        {
            Debug.Log($"ItemImporter: [{mainCategory}] 시트에 없는 아이템 {deleted}개 삭제 완료");
        }
        return deleted;
    }

    private static void LogImportSummary(int sheetsProcessed, int created, int updated, int deleted, TimeSpan elapsed)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════");
        sb.AppendLine("           Item Importer - 최종 결과 보고서");
        sb.AppendLine("═══════════════════════════════════════════════════");
        sb.AppendLine($"  처리된 시트 수       : {sheetsProcessed}");
        sb.AppendLine($"  새로 생성된 아이템   : {created}");
        sb.AppendLine($"  업데이트된 아이템   : {updated}");
        sb.AppendLine($"  삭제된 아이템       : {deleted}");
        sb.AppendLine($"  총 처리 아이템       : {created + updated}");
        sb.AppendLine($"  전체 소요 시간      : {elapsed.TotalSeconds:F1}초");
        sb.AppendLine("═══════════════════════════════════════════════════");
        Debug.Log(sb.ToString());
    }

    private static Dictionary<string, string> BuildExistingItemAssetPathMap()
    {
        Dictionary<string, string> assetPathMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ResourceFolderPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);

            if (itemData == null || string.IsNullOrWhiteSpace(itemData.ItemId) || assetPathMap.ContainsKey(itemData.ItemId))
            {
                continue;
            }

            assetPathMap[itemData.ItemId] = assetPath;
        }

        return assetPathMap;
    }

    private static ItemImportPathInfo BuildItemAssetPathInfo(List<string> row, Dictionary<string, int> headerMap, string itemId, string mainCategoryOverride)
    {
        string mainFolder = NormalizeFolderName(SanitizePathSegment(string.IsNullOrWhiteSpace(mainCategoryOverride) ? SanitizeEnumMemberName(GetCell(row, headerMap, "MainCategory")) : mainCategoryOverride));
        string middleFolder = NormalizeFolderName(SanitizePathSegment(SanitizeEnumMemberName(GetCell(row, headerMap, "MiddleCategory"))));
        string subFolder = NormalizeFolderName(SanitizePathSegment(SanitizeEnumMemberName(GetCell(row, headerMap, "SubCategory"))));
        string tierFolder = GetTierFolderName(row, headerMap);

        if (string.Equals(mainFolder, middleFolder, StringComparison.Ordinal) && mainFolder != "None")
        {
            Debug.LogWarning($"ItemImporter: 아이템 '{itemId}' - MainCategory와 MiddleCategory가 동일합니다 ({mainFolder}).");
        }

        string safeFileName = SanitizeAssetFileName(itemId);
        if (safeFileName != itemId)
        {
            Debug.LogWarning($"ItemImporter: 아이템 ID '{itemId}' -> '{safeFileName}' 저장");
        }

        string folderPath = $"{ResourceFolderPath}/{mainFolder}/{middleFolder}/{subFolder}/{tierFolder}";

        return new ItemImportPathInfo
        {
            FolderPath = folderPath,
            AssetPath = $"{folderPath}/{safeFileName}.asset"
        };
    }

    private static string GetTierFolderName(List<string> row, Dictionary<string, int> headerMap)
    {
        if (!TryGetCell(row, headerMap, "Tier", out string tierValue) || string.IsNullOrWhiteSpace(tierValue))
        {
            return "Tier1";
        }
        string tier = tierValue.Trim();
        if (Enum.TryParse<ItemTier>(tier, true, out ItemTier parsed))
        {
            return parsed.ToString();
        }
        if (int.TryParse(tier, out int tierNum) && tierNum >= 1 && tierNum <= 10)
        {
            return $"Tier{tierNum}";
        }
        string sanitized = SanitizePathSegment(SanitizeEnumMemberName(tier));
        return string.IsNullOrWhiteSpace(sanitized) ? "Tier1" : sanitized;
    }

    private static (ItemData itemData, bool wasCreated) LoadOrCreateItemAsset(string itemId, string targetAssetPath, Dictionary<string, string> existingAssetPathsByItemId)
    {
        ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(targetAssetPath);

        if (itemData != null)
        {
            return (itemData, false);
        }

        if (existingAssetPathsByItemId.TryGetValue(itemId, out string existingAssetPath)
            && !string.IsNullOrWhiteSpace(existingAssetPath)
            && !string.Equals(existingAssetPath, targetAssetPath, StringComparison.OrdinalIgnoreCase))
        {
            string moveError = AssetDatabase.MoveAsset(existingAssetPath, targetAssetPath);

            if (!string.IsNullOrWhiteSpace(moveError))
            {
                Debug.LogWarning($"ItemImporter: 에셋 이동에 실패했습니다. 기존 경로: {existingAssetPath}, 대상 경로: {targetAssetPath}, 오류: {moveError}");
            }
            else
            {
                itemData = AssetDatabase.LoadAssetAtPath<ItemData>(targetAssetPath);

                if (itemData != null)
                {
                    return (itemData, false);
                }
            }
        }

        itemData = CreateInstance<ItemData>();
        itemData.name = itemId;
        AssetDatabase.CreateAsset(itemData, targetAssetPath);
        return (itemData, true);
    }

    private static CategorySyncResult GenerateItemDefinitionsCode(List<List<string>> rows, Dictionary<string, int> headerMap)
    {
        List<string> mainCategories = new List<string> { "None" };
        List<string> middleCategories = new List<string> { "None" };
        List<string> subCategories = new List<string> { "None" };
        HashSet<string> mainCategorySet = new HashSet<string>(StringComparer.Ordinal) { "None" };
        HashSet<string> middleCategorySet = new HashSet<string>(StringComparer.Ordinal) { "None" };
        HashSet<string> subCategorySet = new HashSet<string>(StringComparer.Ordinal) { "None" };
        Dictionary<string, string> mainCategoryNameCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["None"] = "None"
        };
        Dictionary<string, string> middleCategoryNameCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["None"] = "None"
        };
        Dictionary<string, string> subCategoryNameCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["None"] = "None"
        };

        Dictionary<string, List<string>> mainToMiddleMap = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        Dictionary<string, List<string>> middleToSubMap = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        mainToMiddleMap["None"] = new List<string> { "None" };
        middleToSubMap["None"] = new List<string> { "None" };

        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            List<string> row = rows[rowIndex];
            string mainCategoryRaw = GetCell(row, headerMap, "MainCategory");
            string middleCategoryRaw = GetCell(row, headerMap, "MiddleCategory");
            string subCategoryRaw = GetCell(row, headerMap, "SubCategory");

            string mainCategory = RegisterSanitizedEnumName(mainCategoryRaw, mainCategoryNameCache, mainCategorySet, mainCategories);
            string middleCategory = RegisterSanitizedEnumName(middleCategoryRaw, middleCategoryNameCache, middleCategorySet, middleCategories);
            string subCategory = RegisterSanitizedEnumName(subCategoryRaw, subCategoryNameCache, subCategorySet, subCategories);

            if (string.IsNullOrWhiteSpace(mainCategory))
            {
                continue;
            }

            EnsureMapEntry(mainToMiddleMap, mainCategory, "None");

            if (!string.IsNullOrWhiteSpace(middleCategory) && middleCategorySet.Contains(middleCategory))
            {
                AddUniqueMapValue(mainToMiddleMap, mainCategory, middleCategory);
                EnsureMapEntry(middleToSubMap, middleCategory, "None");
            }

            if (!string.IsNullOrWhiteSpace(subCategory)
                && !string.IsNullOrWhiteSpace(middleCategory)
                && middleCategorySet.Contains(middleCategory)
                && subCategorySet.Contains(subCategory))
            {
                AddUniqueMapValue(middleToSubMap, middleCategory, subCategory);
            }
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine();
        builder.AppendLine("// 이 파일은 ItemImporter에 의해 자동 생성됩니다.");
        builder.AppendLine("// 고정 Enum은 CoreDefinitions.cs를 확인하세요.");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// Defines the high-level categories for items in the G.S.I project.");
        builder.AppendLine("/// </summary>");
        AppendEnum(builder, "ItemMainCategory", mainCategories);
        builder.AppendLine();
        AppendEnum(builder, "ItemMiddleCategory", middleCategories);
        builder.AppendLine();
        AppendEnum(builder, "ItemSubCategory", subCategories);
        builder.AppendLine();
        builder.AppendLine("public static class CategoryDefinitionMaps");
        builder.AppendLine("{");
        AppendCategoryMap(builder, "MainToMiddleMap", "ItemMainCategory", "ItemMiddleCategory", mainToMiddleMap);
        builder.AppendLine();
        AppendCategoryMap(builder, "MiddleToSubMap", "ItemMiddleCategory", "ItemSubCategory", middleToSubMap);
        builder.AppendLine("}");

        return new CategorySyncResult
        {
            GeneratedCode = builder.ToString(),
            MainCategoryCount = Mathf.Max(0, mainCategories.Count - 1),
            MiddleCategoryCount = Mathf.Max(0, middleCategories.Count - 1),
            SubCategoryCount = Mathf.Max(0, subCategories.Count - 1),
            MainToMiddleMapCount = CountMapRelationships(mainToMiddleMap),
            MiddleToSubMapCount = CountMapRelationships(middleToSubMap)
        };
    }

    private static string GenerateFallbackItemDefinitionsCode()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine();
        builder.AppendLine("// 이 파일은 ItemImporter의 fallback 안전 정의입니다.");
        builder.AppendLine("// Sync Categories 완료 후 실제 생성 코드로 덮어써집니다.");
        builder.AppendLine();
        builder.AppendLine("public enum ItemMainCategory");
        builder.AppendLine("{");
        builder.AppendLine("    None");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("public enum ItemMiddleCategory");
        builder.AppendLine("{");
        builder.AppendLine("    None");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("public enum ItemSubCategory");
        builder.AppendLine("{");
        builder.AppendLine("    None");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("public static class CategoryDefinitionMaps");
        builder.AppendLine("{");
        builder.AppendLine("    public static readonly Dictionary<ItemMainCategory, ItemMiddleCategory[]> MainToMiddleMap =");
        builder.AppendLine("        new Dictionary<ItemMainCategory, ItemMiddleCategory[]>");
        builder.AppendLine("        {");
        builder.AppendLine("            { ItemMainCategory.None, new[] { ItemMiddleCategory.None } }");
        builder.AppendLine("        };");
        builder.AppendLine();
        builder.AppendLine("    public static readonly Dictionary<ItemMiddleCategory, ItemSubCategory[]> MiddleToSubMap =");
        builder.AppendLine("        new Dictionary<ItemMiddleCategory, ItemSubCategory[]>");
        builder.AppendLine("        {");
        builder.AppendLine("            { ItemMiddleCategory.None, new[] { ItemSubCategory.None } }");
        builder.AppendLine("        };");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void RemoveAddressableEntry(AddressableAssetSettings settings, string assetGuid, string itemId)
    {
        if (settings == null || string.IsNullOrWhiteSpace(assetGuid))
        {
            return;
        }

        AddressableAssetEntry entry = settings.FindAssetEntry(assetGuid);

        if (entry == null)
        {
            return;
        }

        settings.RemoveAssetEntry(assetGuid, false);
        Debug.Log($"ItemImporter: Addressables 엔트리를 삭제했습니다 - {itemId}");
    }

    private static Dictionary<string, AddressableAssetGroup> BuildAddressableGroupCache(AddressableAssetSettings settings)
    {
        Dictionary<string, AddressableAssetGroup> groupCache = new Dictionary<string, AddressableAssetGroup>(StringComparer.Ordinal);

        if (settings == null || settings.groups == null)
        {
            return groupCache;
        }

        foreach (AddressableAssetGroup group in settings.groups)
        {
            if (group == null || string.IsNullOrWhiteSpace(group.Name) || groupCache.ContainsKey(group.Name))
            {
                continue;
            }

            groupCache[group.Name] = group;
        }

        return groupCache;
    }

    private static void RegisterAddressableEntry(
        AddressableAssetSettings settings,
        Dictionary<string, AddressableAssetGroup> groupCache,
        ItemData itemData,
        string assetPath)
    {
        if (settings == null || itemData == null || string.IsNullOrWhiteSpace(itemData.ItemId) || string.IsNullOrWhiteSpace(assetPath))
        {
            return;
        }

        string groupName = $"Items_{itemData.MainCategory}";
        AddressableAssetGroup group = GetOrCreateAddressableGroup(settings, groupCache, groupName);

        if (group == null)
        {
            Debug.LogWarning($"ItemImporter: Addressables 그룹을 준비하지 못해 등록을 건너뜁니다 - {itemData.ItemId}");
            return;
        }

        string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

        if (string.IsNullOrWhiteSpace(assetGuid))
        {
            Debug.LogWarning($"ItemImporter: Addressables GUID를 찾지 못했습니다 - {assetPath}");
            return;
        }

        AddressableAssetEntry entry = settings.CreateOrMoveEntry(assetGuid, group, false, false);

        if (entry != null)
        {
            entry.address = itemData.ItemId;
        }
    }

    private static AddressableAssetGroup GetOrCreateAddressableGroup(
        AddressableAssetSettings settings,
        Dictionary<string, AddressableAssetGroup> groupCache,
        string groupName)
    {
        if (settings == null || string.IsNullOrWhiteSpace(groupName))
        {
            return null;
        }

        if (groupCache != null && groupCache.TryGetValue(groupName, out AddressableAssetGroup cachedGroup) && cachedGroup != null)
        {
            return cachedGroup;
        }

        AddressableAssetGroup group = settings.FindGroup(groupName);

        if (group == null)
        {
            List<AddressableAssetGroupSchema> schemaTemplates = settings.DefaultGroup != null
                ? new List<AddressableAssetGroupSchema>(settings.DefaultGroup.Schemas)
                : null;

            group = settings.CreateGroup(
                groupName,
                false,
                false,
                false,
                schemaTemplates,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));
        }

        if (groupCache != null && group != null)
        {
            groupCache[groupName] = group;
        }

        return group;
    }

    private static void AppendEnum(StringBuilder builder, string enumName, List<string> values)
    {
        builder.AppendLine($"public enum {enumName}");
        builder.AppendLine("{");

        for (int i = 0; i < values.Count; i++)
        {
            string suffix = i < values.Count - 1 ? "," : string.Empty;
            builder.AppendLine($"    {values[i]}{suffix}");
        }

        builder.AppendLine("}");
    }

    private static void AppendCategoryMap(
        StringBuilder builder,
        string mapName,
        string keyEnumName,
        string valueEnumName,
        Dictionary<string, List<string>> mapData)
    {
        builder.AppendLine($"    public static readonly Dictionary<{keyEnumName}, {valueEnumName}[]> {mapName} =");
        builder.AppendLine($"        new Dictionary<{keyEnumName}, {valueEnumName}[]>");
        builder.AppendLine("        {");

        int entryIndex = 0;
        foreach (KeyValuePair<string, List<string>> pair in mapData)
        {
            string suffix = entryIndex < mapData.Count - 1 ? "," : string.Empty;
            builder.AppendLine($"            {{ {keyEnumName}.{pair.Key}, new[] {{ {FormatEnumArray(valueEnumName, pair.Value)} }} }}{suffix}");
            entryIndex++;
        }

        builder.AppendLine("        };");
    }

    private static string FormatEnumArray(string enumName, List<string> values)
    {
        List<string> formattedValues = new List<string>();

        foreach (string value in values)
        {
            formattedValues.Add($"{enumName}.{value}");
        }

        return string.Join(", ", formattedValues);
    }

    private static void EnsureMapEntry(Dictionary<string, List<string>> map, string key, string defaultValue)
    {
        if (map.ContainsKey(key))
        {
            return;
        }

        map[key] = new List<string> { defaultValue };
    }

    private static void AddUniqueMapValue(Dictionary<string, List<string>> map, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        EnsureMapEntry(map, key, "None");

        if (!map[key].Contains(value))
        {
            map[key].Add(value);
        }
    }

    private static int CountMapRelationships(Dictionary<string, List<string>> map)
    {
        int count = 0;

        foreach (KeyValuePair<string, List<string>> pair in map)
        {
            if (pair.Value == null)
            {
                continue;
            }

            foreach (string value in pair.Value)
            {
                if (string.Equals(pair.Key, "None", StringComparison.Ordinal)
                    && string.Equals(value, "None", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(value, "None", StringComparison.Ordinal))
                {
                    continue;
                }

                count++;
            }
        }

        return count;
    }

    private static string SanitizeEnumMemberName(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        string trimmedValue = rawValue.Trim();
        MatchCollection matches = Regex.Matches(trimmedValue, "[A-Za-z0-9]+");

        if (matches.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder result = new StringBuilder(trimmedValue.Length);

        foreach (Match match in matches)
        {
            string token = match.Value;

            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            if (char.IsLetter(token[0]))
            {
                result.Append(char.ToUpperInvariant(token[0]));

                if (token.Length > 1)
                {
                    result.Append(token.Substring(1));
                }
            }
            else
            {
                result.Append(token);
            }
        }

        if (result.Length == 0)
        {
            return string.Empty;
        }

        if (ReservedCSharpKeywords.Contains(result.ToString()) || (!char.IsLetter(result[0]) && result[0] != '_'))
        {
            result.Insert(0, '_');
        }

        return result.ToString();
    }

    private static string RegisterSanitizedEnumName(
        string rawValue,
        Dictionary<string, string> nameCache,
        HashSet<string> confirmedNames,
        List<string> orderedNames)
    {
        string normalizedRawValue = rawValue?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedRawValue))
        {
            return string.Empty;
        }

        if (nameCache != null && nameCache.TryGetValue(normalizedRawValue, out string cachedName))
        {
            return cachedName;
        }

        string sanitizedName = SanitizeEnumMemberName(normalizedRawValue);

        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            return string.Empty;
        }

        string uniqueName = sanitizedName;
        int suffix = 2;

        while (confirmedNames != null && confirmedNames.Contains(uniqueName))
        {
            uniqueName = $"{sanitizedName}_{suffix}";
            suffix++;
        }

        if (!string.Equals(uniqueName, sanitizedName, StringComparison.Ordinal))
        {
            Debug.LogWarning($"ItemImporter: 카테고리 이름 충돌로 '{normalizedRawValue}'가 '{uniqueName}'로 변경되었습니다.");
        }

        if (nameCache != null)
        {
            nameCache[normalizedRawValue] = uniqueName;
        }

        if (confirmedNames != null)
        {
            confirmedNames.Add(uniqueName);
        }

        if (orderedNames != null && !orderedNames.Contains(uniqueName))
        {
            orderedNames.Add(uniqueName);
        }

        return uniqueName;
    }

    private static string GetAbsoluteProjectPath(string assetRelativePath)
    {
        string projectRootPath = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        string normalizedRelativePath = assetRelativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(projectRootPath, normalizedRelativePath);
    }

    private static Task WaitForEditorDelayCallAsync()
    {
        TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();

        void HandleDelayCall()
        {
            EditorApplication.delayCall -= HandleDelayCall;
            completionSource.TrySetResult(true);
        }

        EditorApplication.delayCall += HandleDelayCall;
        return completionSource.Task;
    }

    private static Task<bool> WaitForWebRequestAsync(UnityWebRequest request, string progressTitle, string progressInfo)
    {
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        float elapsed = 0f;
        const float maxWaitSeconds = 120f;

        void Poll()
        {
            if (operation.isDone)
            {
                EditorUtility.ClearProgressBar();
                tcs.TrySetResult(true);
                return;
            }

            elapsed += 0.05f;
            if (elapsed >= maxWaitSeconds)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"ItemImporter: CSV 다운로드 타임아웃 ({maxWaitSeconds}초)");
                request.Abort();
                tcs.TrySetResult(false);
                return;
            }

            float progress = Mathf.Clamp01(0.1f + (elapsed / maxWaitSeconds) * 0.5f);
            if (EditorUtility.DisplayCancelableProgressBar(progressTitle, progressInfo, progress))
            {
                EditorUtility.ClearProgressBar();
                request.Abort();
                tcs.TrySetResult(false);
                return;
            }

            EditorApplication.delayCall += Poll;
        }

        EditorApplication.delayCall += Poll;
        return tcs.Task;
    }

    private static string ConvertGoogleSheetUrlToCsv(string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return string.Empty;
        }

        string trimmedUrl = rawUrl.Trim();

        if (!trimmedUrl.Contains("docs.google.com/spreadsheets/d/", StringComparison.OrdinalIgnoreCase))
        {
            return trimmedUrl;
        }

        Match match = Regex.Match(
            trimmedUrl,
            @"https?://docs\.google\.com/spreadsheets/d/([^/?#]+)",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            return trimmedUrl;
        }

        string sheetId = match.Groups[1].Value;
        string gid = string.Empty;

        Match gidQueryMatch = Regex.Match(trimmedUrl, @"[?&]gid=(\d+)", RegexOptions.IgnoreCase);
        Match gidFragmentMatch = Regex.Match(trimmedUrl, @"#gid=(\d+)", RegexOptions.IgnoreCase);

        if (gidQueryMatch.Success)
        {
            gid = gidQueryMatch.Groups[1].Value;
        }
        else if (gidFragmentMatch.Success)
        {
            gid = gidFragmentMatch.Groups[1].Value;
        }

        string convertedUrl = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv";

        if (!string.IsNullOrWhiteSpace(gid))
        {
            convertedUrl += $"&gid={gid}";
        }

        return convertedUrl;
    }

    private static bool TryResolveGeneratedEnumName(string enumTypeName, string rawValue, string fallbackName, out string resolvedName)
    {
        resolvedName = fallbackName;
        Type enumType = FindTypeByName(enumTypeName);

        if (enumType == null || !enumType.IsEnum)
        {
            return false;
        }

        string sanitizedName = string.IsNullOrWhiteSpace(rawValue)
            ? fallbackName
            : SanitizeEnumMemberName(rawValue);

        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            sanitizedName = fallbackName;
        }

        foreach (string enumName in Enum.GetNames(enumType))
        {
            if (string.Equals(enumName, sanitizedName, StringComparison.Ordinal))
            {
                resolvedName = enumName;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetCategoryMapValues(string fieldName, string keyName, out List<string> values)
    {
        values = new List<string>();
        Type mapContainerType = FindTypeByName("CategoryDefinitionMaps");

        if (mapContainerType == null)
        {
            return false;
        }

        FieldInfo fieldInfo = mapContainerType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);

        if (fieldInfo == null)
        {
            return false;
        }

        object fieldValue = fieldInfo.GetValue(null);

        if (fieldValue is not IEnumerable enumerable)
        {
            return false;
        }

        foreach (object entry in enumerable)
        {
            if (entry == null)
            {
                continue;
            }

            Type entryType = entry.GetType();
            PropertyInfo keyProperty = entryType.GetProperty("Key");
            PropertyInfo valueProperty = entryType.GetProperty("Value");

            if (keyProperty == null || valueProperty == null)
            {
                continue;
            }

            object keyObject = keyProperty.GetValue(entry);

            if (!string.Equals(keyObject?.ToString(), keyName, StringComparison.Ordinal))
            {
                continue;
            }

            object valueObject = valueProperty.GetValue(entry);

            if (valueObject is not IEnumerable valueEnumerable)
            {
                return false;
            }

            foreach (object value in valueEnumerable)
            {
                if (value != null)
                {
                    values.Add(value.ToString());
                }
            }

            return true;
        }

        return false;
    }

    private static Type FindTypeByName(string typeName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type directType = assembly.GetType(typeName);

            if (directType != null)
            {
                return directType;
            }

            Type[] assemblyTypes;

            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                assemblyTypes = exception.Types;
            }

            if (assemblyTypes == null)
            {
                continue;
            }

            foreach (Type assemblyType in assemblyTypes)
            {
                if (assemblyType != null && string.Equals(assemblyType.Name, typeName, StringComparison.Ordinal))
                {
                    return assemblyType;
                }
            }
        }

        return null;
    }

    private static void SetEnumPropertyByName(UnityEngine.Object targetObject, string propertyName, string rawEnumName, string fallbackName)
    {
        if (targetObject == null || string.IsNullOrWhiteSpace(propertyName))
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(targetObject);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null || property.propertyType != SerializedPropertyType.Enum)
        {
            return;
        }

        string sanitizedName = string.IsNullOrWhiteSpace(rawEnumName)
            ? fallbackName
            : SanitizeEnumMemberName(rawEnumName);

        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            sanitizedName = fallbackName;
        }

        string[] enumNames = property.enumNames;
        int enumIndex = Array.IndexOf(enumNames, sanitizedName);

        if (enumIndex < 0)
        {
            enumIndex = Array.IndexOf(enumNames, fallbackName);
        }

        if (enumIndex < 0)
        {
            return;
        }

        property.enumValueIndex = enumIndex;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplyRowToItemData(ItemData itemData, List<string> row, Dictionary<string, int> headerMap)
    {
        itemData.ItemId = GetCell(row, headerMap, "ItemId");
        TryAssignItemIcon(itemData);
        itemData.ItemName = GetCell(row, headerMap, "ItemName");
        itemData.Description = GetCell(row, headerMap, "Description");

        if (itemData.StatModifiers == null)
        {
            itemData.StatModifiers = new List<StatModifier>();
        }
        else
        {
            itemData.StatModifiers.Clear();
        }

        if (TryGetCell(row, headerMap, "MainCategory", out string mainCategoryValue))
        {
            SetEnumPropertyByName(itemData, nameof(ItemData.MainCategory), mainCategoryValue, "None");
        }

        if (TryGetCell(row, headerMap, "MiddleCategory", out string middleCategoryValue))
        {
            SetEnumPropertyByName(itemData, nameof(ItemData.MiddleCategory), middleCategoryValue, "None");
        }

        if (TryGetCell(row, headerMap, "SubCategory", out string subCategoryValue))
        {
            SetEnumPropertyByName(itemData, nameof(ItemData.SubCategory), subCategoryValue, "None");
        }

        if (TryGetCell(row, headerMap, "Tier", out string tierValue))
        {
            itemData.Tier = ParseEnumValue<ItemTier>(tierValue, itemData.Tier);
        }

        if (TryGetCell(row, headerMap, "DefaultSlot", out string defaultSlotValue))
        {
            itemData.DefaultSlot = ParseEnumValue<EquipSlot>(defaultSlotValue, EquipSlot.None);
        }
        else
        {
            itemData.DefaultSlot = EquipSlot.None;
        }

        if (TryGetCell(row, headerMap, "GripType", out string gripTypeValue))
        {
            itemData.GripType = ParseEnumValue<WeaponGrip>(gripTypeValue, WeaponGrip.None);
        }
        else
        {
            itemData.GripType = WeaponGrip.None;
        }

        if (TryGetCell(row, headerMap, "PurchasePrice", out string purchasePriceValue) && int.TryParse(purchasePriceValue, out int purchasePrice))
        {
            itemData.PurchasePrice = purchasePrice;
        }

        if (TryGetCell(row, headerMap, "SalePrice", out string salePriceValue) && int.TryParse(salePriceValue, out int salePrice))
        {
            itemData.SalePrice = salePrice;
        }

        ApplyDynamicStatModifiers(itemData, row, headerMap);
    }

    private static readonly Dictionary<string, StatType> StatNameAliases = new Dictionary<string, StatType>(StringComparer.OrdinalIgnoreCase)
    {
        ["MaxHP"] = StatType.Hp, ["Max HP"] = StatType.Hp, ["HP"] = StatType.Hp, ["Health"] = StatType.Hp, ["체력"] = StatType.Hp,
        ["MaxMP"] = StatType.Mp, ["Max MP"] = StatType.Mp, ["MP"] = StatType.Mp, ["Mana"] = StatType.Mp, ["마나"] = StatType.Mp,
        ["SP"] = StatType.Sp, ["Stamina"] = StatType.Sp, ["스태미나"] = StatType.Sp,
        ["Attack"] = StatType.PhysAtk, ["Physical Attack"] = StatType.PhysAtk, ["PhysAtk"] = StatType.PhysAtk, ["물리공격"] = StatType.PhysAtk,
        ["Magic Attack"] = StatType.MagAtk, ["MagAtk"] = StatType.MagAtk, ["마법공격"] = StatType.MagAtk,
        ["Accuracy"] = StatType.Accuracy, ["명중"] = StatType.Accuracy,
        ["Crit"] = StatType.CritRate, ["Critical"] = StatType.CritRate, ["CritRate"] = StatType.CritRate, ["치명타"] = StatType.CritRate,
        ["Defense"] = StatType.PhysDef, ["Physical Defense"] = StatType.PhysDef, ["PhysDef"] = StatType.PhysDef, ["물리방어"] = StatType.PhysDef,
        ["Magic Defense"] = StatType.MagDef, ["MagDef"] = StatType.MagDef, ["마법방어"] = StatType.MagDef,
        ["Evasion"] = StatType.Evasion, ["회피"] = StatType.Evasion,
        ["GuardRate"] = StatType.GuardRate, ["Guard"] = StatType.GuardRate, ["막기"] = StatType.GuardRate,
        ["StatusInflict"] = StatType.StatusInflict, ["상태이상부여"] = StatType.StatusInflict,
        ["StatusResist"] = StatType.StatusResist, ["상태이상저항"] = StatType.StatusResist,
        ["Speed"] = StatType.Speed, ["이동속도"] = StatType.Speed,
        ["Luck"] = StatType.Luck, ["운"] = StatType.Luck
    };

    private static void ApplyDynamicStatModifiers(ItemData itemData, List<string> row, Dictionary<string, int> headerMap)
    {
        foreach (KeyValuePair<string, int> headerEntry in headerMap)
        {
            if (!headerEntry.Key.StartsWith("Stat_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string statName = headerEntry.Key.Substring("Stat_".Length).Trim();

            if (string.IsNullOrWhiteSpace(statName))
            {
                continue;
            }

            if (!TryGetCell(row, headerMap, headerEntry.Key, out string statValueText)
                || string.IsNullOrWhiteSpace(statValueText)
                || !TryParseFloat(statValueText, out float statValue)
                || Mathf.Approximately(statValue, 0f))
            {
                continue;
            }

            if (!TryResolveStatType(statName, out StatType statType))
            {
                Debug.LogWarning($"ItemImporter: 알 수 없는 Stat 헤더를 건너뜁니다 - {headerEntry.Key} (시트값: '{statName}')");
                continue;
            }

            itemData.StatModifiers.Add(new StatModifier
            {
                Stat = statType,
                Value = statValue
            });
        }
    }

    private static bool TryResolveStatType(string statName, out StatType statType)
    {
        if (Enum.TryParse(statName.Trim(), true, out statType))
        {
            return true;
        }

        if (StatNameAliases.TryGetValue(statName.Trim(), out statType))
        {
            return true;
        }

        statType = default;
        return false;
    }

    private static bool TryParseFloat(string rawValue, out float parsedValue)
    {
        return float.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out parsedValue)
            || float.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out parsedValue);
    }

    private static void TryAssignItemIcon(ItemData itemData)
    {
        if (itemData == null || string.IsNullOrWhiteSpace(itemData.ItemId))
        {
            return;
        }

        if (ItemIconCache.TryGetValue(itemData.ItemId, out Sprite cachedIcon))
        {
            itemData.ItemIcon = cachedIcon;
            return;
        }

        Sprite loadedIcon = LoadItemIcon(itemData.ItemId);
        ItemIconCache[itemData.ItemId] = loadedIcon;

        if (loadedIcon != null)
        {
            itemData.ItemIcon = loadedIcon;
            return;
        }

        Sprite defaultIcon = LoadDefaultIcon();
        itemData.ItemIcon = defaultIcon;
        if (defaultIcon == null)
        {
            Debug.LogWarning($"ItemImporter: Warning: Icon not found at [{IconFolderPath}/{itemData.ItemId}], null 할당");
        }
    }

    private static Sprite _defaultIconCache;

    private static Sprite LoadDefaultIcon()
    {
        if (_defaultIconCache != null) return _defaultIconCache;
        string[] extensions = { ".png", ".PNG", ".jpg", ".JPG", ".jpeg", ".JPEG" };
        foreach (string ext in extensions)
        {
            string path = $"{IconFolderPath}/{DefaultIconFileName}{ext}";
            if (File.Exists(GetAbsoluteProjectPath(path)))
            {
                _defaultIconCache = TryLoadSpriteAssetAtPath(path) ?? TryConvertToSpriteAndReload(path);
                return _defaultIconCache;
            }
        }
        return null;
    }

    private static Sprite LoadItemIcon(string itemId)
    {
        string[] extensions = { ".png", ".PNG", ".jpg", ".JPG", ".jpeg", ".JPEG" };
        string firstCheckedPath = null;

        foreach (string extension in extensions)
        {
            string path = $"{IconFolderPath}/{itemId}{extension}";
            string absolutePath = GetAbsoluteProjectPath(path);

            if (!File.Exists(absolutePath))
            {
                continue;
            }

            if (firstCheckedPath == null) firstCheckedPath = path;

            Sprite sprite = TryLoadSpriteAssetAtPath(path);
            if (sprite != null) return sprite;

            Sprite recovered = TryConvertToSpriteAndReload(path);
            if (recovered != null) return recovered;
        }

        if (firstCheckedPath != null)
        {
            Debug.LogWarning($"ItemImporter: Warning: Icon not found at [{firstCheckedPath}] (파일 존재하나 Sprite 로드 실패), null/DefaultIcon 시도");
        }

        return null;
    }

    private static Sprite TryConvertToSpriteAndReload(string path)
    {
        TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

        if (textureImporter == null)
        {
            return null;
        }

        Debug.LogWarning($"ItemImporter: 파일이 존재하지만 Texture Type이 Sprite (2D and UI)가 아닙니다. 자동으로 Sprite로 변경합니다. 경로: {path}");

        textureImporter.textureType = TextureImporterType.Sprite;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        Sprite sprite = TryLoadSpriteAssetAtPath(path);

        if (sprite == null)
        {
            Debug.LogWarning($"ItemImporter: Texture Type을 Sprite로 변경했지만 여전히 Sprite 로드에 실패했습니다. 경로: {path}");
        }

        return sprite;
    }

    private static Sprite TryLoadSpriteAssetAtPath(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if (sprite != null)
        {
            return sprite;
        }

        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);

        foreach (UnityEngine.Object subAsset in subAssets)
        {
            if (subAsset is Sprite subSprite)
            {
                return subSprite;
            }
        }

        return null;
    }

    private static TEnum ParseEnumValue<TEnum>(string rawValue, TEnum fallbackValue) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return fallbackValue;
        }

        return Enum.TryParse(rawValue.Trim(), true, out TEnum parsedValue) ? parsedValue : fallbackValue;
    }

    private static string StripUtf8Bom(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (text.Length >= 1 && text[0] == '\uFEFF')
        {
            return text.Substring(1);
        }

        return text;
    }

    private static Dictionary<string, int> ResolveCategoryHeaderMap(Dictionary<string, int> rawHeaderMap)
    {
        if (rawHeaderMap == null)
        {
            return null;
        }

        string[] mainAliases = { "MainCategory", "Main Category", "대분류" };
        string[] middleAliases = { "MiddleCategory", "Middle Category", "중분류" };
        string[] subAliases = { "SubCategory", "Sub Category", "소분류" };

        int? mainIdx = TryGetIndexFromAliases(rawHeaderMap, mainAliases);
        int? middleIdx = TryGetIndexFromAliases(rawHeaderMap, middleAliases);
        int? subIdx = TryGetIndexFromAliases(rawHeaderMap, subAliases);

        if (mainIdx == null || middleIdx == null || subIdx == null)
        {
            return null;
        }

        return new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["MainCategory"] = mainIdx.Value,
            ["MiddleCategory"] = middleIdx.Value,
            ["SubCategory"] = subIdx.Value
        };
    }

    private static int? TryGetIndexFromAliases(Dictionary<string, int> headerMap, string[] aliases)
    {
        foreach (string alias in aliases)
        {
            if (headerMap.TryGetValue(alias, out int idx))
            {
                return idx;
            }
        }

        return null;
    }

    private static Dictionary<string, int> ResolveItemsHeaderMap(Dictionary<string, int> rawHeaderMap)
    {
        if (rawHeaderMap == null)
        {
            return null;
        }

        var resolved = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        string[] itemIdAliases = { "ItemId", "Item ID", "아이템ID", "ID" };
        string[] itemNameAliases = { "ItemName", "Item Name", "아이템명", "Name" };
        string[] mainAliases = { "MainCategory", "Main Category", "대분류" };
        string[] middleAliases = { "MiddleCategory", "Middle Category", "중분류" };
        string[] subAliases = { "SubCategory", "Sub Category", "소분류" };

        if (TryGetIndexFromAliases(rawHeaderMap, itemIdAliases) is not int itemIdIdx) return null;
        if (TryGetIndexFromAliases(rawHeaderMap, itemNameAliases) is not int itemNameIdx) return null;
        if (TryGetIndexFromAliases(rawHeaderMap, mainAliases) is not int mainIdx) return null;
        if (TryGetIndexFromAliases(rawHeaderMap, middleAliases) is not int middleIdx) return null;
        if (TryGetIndexFromAliases(rawHeaderMap, subAliases) is not int subIdx) return null;

        resolved["ItemId"] = itemIdIdx;
        resolved["ItemName"] = itemNameIdx;
        resolved["MainCategory"] = mainIdx;
        resolved["MiddleCategory"] = middleIdx;
        resolved["SubCategory"] = subIdx;

        string[] descAliases = { "Description", "설명" };
        string[] tierAliases = { "Tier", "티어" };
        string[] slotAliases = { "DefaultSlot", "Default Slot", "기본슬롯" };
        string[] gripAliases = { "GripType", "Grip Type", "손잡이타입" };
        string[] purchaseAliases = { "PurchasePrice", "Purchase Price", "구매가" };
        string[] saleAliases = { "SalePrice", "Sale Price", "판매가" };

        if (TryGetIndexFromAliases(rawHeaderMap, descAliases) is int descIdx) resolved["Description"] = descIdx;
        if (TryGetIndexFromAliases(rawHeaderMap, tierAliases) is int tierIdx) resolved["Tier"] = tierIdx;
        if (TryGetIndexFromAliases(rawHeaderMap, slotAliases) is int slotIdx) resolved["DefaultSlot"] = slotIdx;
        if (TryGetIndexFromAliases(rawHeaderMap, gripAliases) is int gripIdx) resolved["GripType"] = gripIdx;
        if (TryGetIndexFromAliases(rawHeaderMap, purchaseAliases) is int purchaseIdx) resolved["PurchasePrice"] = purchaseIdx;
        if (TryGetIndexFromAliases(rawHeaderMap, saleAliases) is int saleIdx) resolved["SalePrice"] = saleIdx;

        foreach (var kv in rawHeaderMap)
        {
            if (kv.Key.StartsWith("Stat_", StringComparison.OrdinalIgnoreCase) && !resolved.ContainsKey(kv.Key))
            {
                resolved[kv.Key] = kv.Value;
            }
        }

        return resolved;
    }

    private static Dictionary<string, int> BuildHeaderMap(List<string> headerRow)
    {
        Dictionary<string, int> headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headerRow.Count; i++)
        {
            string header = headerRow[i].Trim();

            if (!string.IsNullOrWhiteSpace(header) && !headerMap.ContainsKey(header))
            {
                headerMap.Add(header, i);
            }
        }

        return headerMap;
    }

    private static bool TryGetCell(List<string> row, Dictionary<string, int> headerMap, string headerName, out string value)
    {
        value = string.Empty;

        if (!headerMap.TryGetValue(headerName, out int index))
        {
            return false;
        }

        if (index < 0 || index >= row.Count)
        {
            return false;
        }

        value = row[index].Trim();
        return true;
    }

    private static string GetCell(List<string> row, Dictionary<string, int> headerMap, string headerName)
    {
        return TryGetCell(row, headerMap, headerName, out string value) ? value : string.Empty;
    }

    private static List<List<string>> ParseCsv(string csvText)
    {
        List<List<string>> rows = new List<List<string>>();
        List<string> currentRow = new List<string>();
        List<char> currentCell = new List<char>();
        bool insideQuotes = false;

        for (int i = 0; i < csvText.Length; i++)
        {
            char character = csvText[i];

            if (character == '"')
            {
                if (insideQuotes && i + 1 < csvText.Length && csvText[i + 1] == '"')
                {
                    currentCell.Add('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }

                continue;
            }

            if (character == ',' && !insideQuotes)
            {
                currentRow.Add(new string(currentCell.ToArray()));
                currentCell.Clear();
                continue;
            }

            if ((character == '\n' || character == '\r') && !insideQuotes)
            {
                if (character == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                {
                    i++;
                }

                currentRow.Add(new string(currentCell.ToArray()));
                currentCell.Clear();

                if (currentRow.Count > 1 || !string.IsNullOrWhiteSpace(currentRow[0]))
                {
                    rows.Add(new List<string>(currentRow));
                }

                currentRow.Clear();
                continue;
            }

            currentCell.Add(character);
        }

        if (currentCell.Count > 0 || currentRow.Count > 0)
        {
            currentRow.Add(new string(currentCell.ToArray()));

            if (currentRow.Count > 1 || !string.IsNullOrWhiteSpace(currentRow[0]))
            {
                rows.Add(currentRow);
            }
        }

        return rows;
    }

    private static void EnsureFolderExistsWithDisk(string folderPath, HashSet<string> ensuredCache = null)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        string normalizedPath = folderPath.Replace('\\', '/');
        if (ensuredCache != null && ensuredCache.Contains(normalizedPath))
        {
            return;
        }

        string absolutePath = GetAbsoluteProjectPath(normalizedPath);
        Directory.CreateDirectory(absolutePath);
        ensuredCache?.Add(normalizedPath);
        EnsureFolderExists(normalizedPath, ensuredCache);
    }

    private static void EnsureFolderExists(string folderPath, HashSet<string> ensuredCache = null)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        string normalizedPath = folderPath.Replace('\\', '/');
        if (ensuredCache != null && ensuredCache.Contains(normalizedPath))
        {
            return;
        }

        if (AssetDatabase.IsValidFolder(normalizedPath))
        {
            ensuredCache?.Add(normalizedPath);
            return;
        }

        string[] segments = normalizedPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments[0] != "Assets")
        {
            Debug.LogError($"ItemImporter: 잘못된 폴더 경로입니다 - {folderPath}");
            return;
        }

        string currentPath = segments[0];

        for (int i = 1; i < segments.Length; i++)
        {
            string nextPath = $"{currentPath}/{segments[i]}";

            if (ensuredCache != null && ensuredCache.Contains(nextPath))
            {
                currentPath = nextPath;
                continue;
            }

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, segments[i]);
            }

            ensuredCache?.Add(nextPath);
            currentPath = nextPath;
        }
    }

    private static string NormalizeFolderName(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment) || segment == "None")
        {
            return segment;
        }

        Match match = Regex.Match(segment, @"^(.+?)\s+\d+$");
        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
        {
            return match.Groups[1].Value.Trim();
        }

        return segment;
    }

    private static string SanitizePathSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
        {
            return "None";
        }

        StringBuilder builder = new StringBuilder(segment.Length);

        foreach (char character in segment)
        {
            if (character == '/' || character == '\\' || character == ':' || character == '*' || character == '?'
                || character == '"' || character == '<' || character == '>' || character == '|')
            {
                builder.Append('_');
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static string SanitizeAssetFileName(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return "Unknown";
        }

        string sanitized = SanitizePathSegment(itemId.Trim());

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "Unknown";
        }

        if (sanitized.Length > 200)
        {
            sanitized = sanitized.Substring(0, 200);
        }

        return sanitized;
    }
}
