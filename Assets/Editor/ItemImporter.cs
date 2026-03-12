using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    private const string SheetUrlKey = "GSI.ItemImporter.SheetUrl";
    private const string CategorySheetUrlKey = "GSI.ItemImporter.CategorySheetUrl";
    private const string DefaultSheetUrl = "https://docs.google.com/spreadsheets/d/1103KBIdVGv1VL5BWnzw7_P5ztvqOe3Dlo5hLGLQ8VTc/export?format=csv&gid=742072563";
    private const string DefaultCategorySheetUrl = "";
    private const string ItemDefinitionsPath = "Assets/Scripts/ItemDefinitions.cs";
    private const int ProgressUpdateInterval = 100;
    private const int MinimumImportedItemCountForDeletion = 10;

    private string _sheetUrl;
    private string _categorySheetUrl;
    private bool _isImporting;
    private string _lastStatusMessage = "대기 중";

    [MenuItem("Tools/Item Importer Settings")]
    private static void OpenWindow()
    {
        ItemImporter window = GetWindow<ItemImporter>("Item Importer Settings");
        window.minSize = new Vector2(520f, 150f);
        window.Show();
    }

    private void OnEnable()
    {
        _sheetUrl = EditorPrefs.GetString(SheetUrlKey, DefaultSheetUrl);
        _categorySheetUrl = EditorPrefs.GetString(CategorySheetUrlKey, DefaultCategorySheetUrl);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Item Importer Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(_isImporting);
        EditorGUILayout.LabelField("Google Sheet CSV URL");

        string updatedUrl = EditorGUILayout.TextField(_sheetUrl ?? string.Empty);

        if (updatedUrl != _sheetUrl)
        {
            _sheetUrl = updatedUrl;
            EditorPrefs.SetString(SheetUrlKey, _sheetUrl);
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
        if (_isImporting)
        {
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
        if (string.IsNullOrWhiteSpace(_sheetUrl))
        {
            _lastStatusMessage = "Google Sheet CSV URL이 비어 있습니다.";
            Debug.LogError("ItemImporter: Google Sheet CSV URL을 먼저 입력해 주세요.");
            return;
        }

        EnsureFolderExists(ResourceFolderPath);
        EditorUtility.DisplayProgressBar("Item Importer", "Downloading CSV data...", 0.1f);

        using UnityWebRequest request = UnityWebRequest.Get(_sheetUrl);
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();

        while (!operation.isDone)
        {
            await Task.Yield();
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            _lastStatusMessage = $"CSV 다운로드 실패: {request.error}";
            Debug.LogError($"ItemImporter: CSV 다운로드 실패 - {request.error}");
            return;
        }

        string csvText = request.downloadHandler.text;
        List<List<string>> rows = ParseCsv(csvText);

        if (rows.Count < 2)
        {
            _lastStatusMessage = "가져올 데이터가 없습니다.";
            Debug.LogWarning("ItemImporter: CSV에 가져올 데이터가 없습니다.");
            return;
        }

        Dictionary<string, int> headerMap = BuildHeaderMap(rows[0]);

        if (!headerMap.ContainsKey("ItemId"))
        {
            _lastStatusMessage = "CSV Header에 ItemId 열이 없습니다.";
            Debug.LogError("ItemImporter: CSV Header에 ItemId 열이 없습니다.");
            return;
        }

        if (!ValidateItemRows(rows, headerMap))
        {
            _lastStatusMessage = "유효성 검사 실패. 시트를 수정한 뒤 다시 시도해 주세요.";
            Debug.LogError("ItemImporter: 유효성 검사에 실패하여 임포트를 중단합니다. 시트를 수정한 뒤 다시 시도해 주세요.");
            return;
        }

        int importedCount = 0;
        HashSet<string> importedItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> existingAssetPathsByItemId = BuildExistingItemAssetPathMap();
        AddressableAssetSettings addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
        Dictionary<string, AddressableAssetGroup> addressableGroupCache = BuildAddressableGroupCache(addressableSettings);

        AssetDatabase.StartAssetEditing();

        try
        {
            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                List<string> row = rows[rowIndex];
                string itemId = GetCell(row, headerMap, "ItemId");

                if (string.IsNullOrWhiteSpace(itemId))
                {
                    continue;
                }

                importedItemIds.Add(itemId);

                if (rowIndex == 1 || rowIndex % ProgressUpdateInterval == 0 || rowIndex == rows.Count - 1)
                {
                    float progress = Mathf.Lerp(0.15f, 0.95f, rowIndex / (float)Mathf.Max(1, rows.Count - 1));
                    EditorUtility.DisplayProgressBar("Item Importer", $"Importing {itemId}...", progress);
                }

                ItemImportPathInfo pathInfo = BuildItemAssetPathInfo(row, headerMap, itemId);
                EnsureFolderExists(pathInfo.FolderPath);

                ItemData itemData = LoadOrCreateItemAsset(itemId, pathInfo.AssetPath, existingAssetPathsByItemId);

                if (itemData == null)
                {
                    Debug.LogError($"ItemImporter: ItemData 생성 또는 로드에 실패했습니다 - {itemId}");
                    continue;
                }

                ApplyRowToItemData(itemData, row, headerMap);
                RegisterAddressableEntry(addressableSettings, addressableGroupCache, itemData, pathInfo.AssetPath);
                EditorUtility.SetDirty(itemData);
                existingAssetPathsByItemId[itemId] = pathInfo.AssetPath;
                importedCount++;
            }

            CleanupDeletedItems(importedItemIds);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        if (addressableSettings != null)
        {
            EditorUtility.SetDirty(addressableSettings);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _lastStatusMessage = $"{importedCount}개 아이템 생성/업데이트 완료";
        Debug.Log($"ItemImporter: {importedCount}개 아이템을 생성/업데이트했습니다.");
    }

    private static bool ValidateItemRows(List<List<string>> rows, Dictionary<string, int> headerMap)
    {
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

            ValidateCategoryCombination(row, headerMap, lineNumber, itemId, ref hasError);
        }

        return !hasError;
    }

    private static void ValidateCategoryCombination(
        List<string> row,
        Dictionary<string, int> headerMap,
        int lineNumber,
        string itemId,
        ref bool hasError)
    {
        string mainRaw = GetCell(row, headerMap, "MainCategory");
        string middleRaw = GetCell(row, headerMap, "MiddleCategory");
        string subRaw = GetCell(row, headerMap, "SubCategory");

        if (!TryResolveGeneratedEnumName("ItemMainCategory", mainRaw, "None", out string mainCategory))
        {
            Debug.LogWarning("ItemImporter Validation: 생성된 카테고리 Enum 또는 매핑을 찾을 수 없어 MainCategory 검증을 건너뜁니다.");
            return;
        }

        if (!TryResolveGeneratedEnumName("ItemMiddleCategory", middleRaw, "None", out string middleCategory))
        {
            Debug.LogWarning("ItemImporter Validation: 생성된 카테고리 Enum 또는 매핑을 찾을 수 없어 MiddleCategory 검증을 건너뜁니다.");
            return;
        }

        if (!TryResolveGeneratedEnumName("ItemSubCategory", subRaw, "None", out string subCategory))
        {
            Debug.LogWarning("ItemImporter Validation: 생성된 카테고리 Enum 또는 매핑을 찾을 수 없어 SubCategory 검증을 건너뜁니다.");
            return;
        }

        if (!TryGetCategoryMapValues("MainToMiddleMap", mainCategory, out List<string> allowedMiddles))
        {
            Debug.LogWarning("ItemImporter Validation: MainToMiddleMap을 찾지 못해 카테고리 검증을 건너뜁니다.");
            return;
        }

        if (!allowedMiddles.Contains(middleCategory))
        {
            Debug.LogError($"ItemImporter Validation: {lineNumber}번째 줄의 아이템(ItemId: {itemId})은 MainCategory '{mainCategory}'에 MiddleCategory '{middleCategory}'를 사용할 수 없습니다.");
            hasError = true;
            return;
        }

        if (!TryGetCategoryMapValues("MiddleToSubMap", middleCategory, out List<string> allowedSubs))
        {
            Debug.LogWarning("ItemImporter Validation: MiddleToSubMap을 찾지 못해 카테고리 검증을 건너뜁니다.");
            return;
        }

        if (!allowedSubs.Contains(subCategory))
        {
            Debug.LogError($"ItemImporter Validation: {lineNumber}번째 줄의 아이템(ItemId: {itemId})은 MiddleCategory '{middleCategory}'에 SubCategory '{subCategory}'를 사용할 수 없습니다.");
            hasError = true;
        }
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

        EditorUtility.DisplayProgressBar("Category Sync", "Downloading category CSV data...", 0.1f);

        using UnityWebRequest request = UnityWebRequest.Get(_categorySheetUrl);
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();

        while (!operation.isDone)
        {
            await Task.Yield();
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            _lastStatusMessage = $"카테고리 CSV 다운로드 실패: {request.error}";
            Debug.LogError($"ItemImporter: 카테고리 CSV 다운로드 실패 - {request.error}");
            return;
        }

        List<List<string>> rows = ParseCsv(request.downloadHandler.text);

        if (rows.Count < 2)
        {
            _lastStatusMessage = "카테고리 데이터가 없습니다.";
            Debug.LogWarning("ItemImporter: Categories 시트에 가져올 데이터가 없습니다.");
            return;
        }

        Dictionary<string, int> headerMap = BuildHeaderMap(rows[0]);

        if (!headerMap.ContainsKey("MainCategory")
            || !headerMap.ContainsKey("MiddleCategory")
            || !headerMap.ContainsKey("SubCategory"))
        {
            _lastStatusMessage = "Categories Header가 올바르지 않습니다.";
            Debug.LogError("ItemImporter: Categories 시트는 MainCategory, MiddleCategory, SubCategory 헤더를 포함해야 합니다.");
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

    private static void CleanupDeletedItems(HashSet<string> importedItemIds)
    {
        if (importedItemIds == null || importedItemIds.Count == 0)
        {
            Debug.LogWarning("ItemImporter: 시트 데이터가 비어 있어 삭제 동기화는 건너뜁니다.");
            return;
        }

        if (importedItemIds.Count < MinimumImportedItemCountForDeletion)
        {
            Debug.LogWarning(
                $"ItemImporter: 가져온 ItemId 수가 {importedItemIds.Count}개로 너무 적어 삭제 동기화를 건너뜁니다. "
                + $"대량 삭제 방지 기준: {MinimumImportedItemCountForDeletion}개 이상");
            return;
        }

        AddressableAssetSettings addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ResourceFolderPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            string itemId = itemData != null && !string.IsNullOrWhiteSpace(itemData.ItemId)
                ? itemData.ItemId
                : fileName;

            if (string.IsNullOrWhiteSpace(itemId) || importedItemIds.Contains(itemId))
            {
                continue;
            }

            if (AssetDatabase.DeleteAsset(assetPath))
            {
                RemoveAddressableEntry(addressableSettings, guid, itemId);
                Debug.Log($"ItemImporter: 시트에 없는 아이템을 삭제했습니다 - {itemId}");
            }
            else
            {
                Debug.LogWarning($"ItemImporter: 아이템 삭제에 실패했습니다 - {itemId}");
            }
        }
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

    private static ItemImportPathInfo BuildItemAssetPathInfo(List<string> row, Dictionary<string, int> headerMap, string itemId)
    {
        string mainFolder = SanitizePathSegment(SanitizeEnumMemberName(GetCell(row, headerMap, "MainCategory")));
        string middleFolder = SanitizePathSegment(SanitizeEnumMemberName(GetCell(row, headerMap, "MiddleCategory")));
        string subFolder = SanitizePathSegment(SanitizeEnumMemberName(GetCell(row, headerMap, "SubCategory")));
        string folderPath = $"{ResourceFolderPath}/{mainFolder}/{middleFolder}/{subFolder}";

        return new ItemImportPathInfo
        {
            FolderPath = folderPath,
            AssetPath = $"{folderPath}/{itemId}.asset"
        };
    }

    private static ItemData LoadOrCreateItemAsset(string itemId, string targetAssetPath, Dictionary<string, string> existingAssetPathsByItemId)
    {
        ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(targetAssetPath);

        if (itemData != null)
        {
            return itemData;
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
                    return itemData;
                }
            }
        }

        itemData = CreateInstance<ItemData>();
        itemData.name = itemId;
        AssetDatabase.CreateAsset(itemData, targetAssetPath);
        return itemData;
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

            if (!Enum.TryParse(statName, true, out StatType statType))
            {
                Debug.LogWarning($"ItemImporter: 알 수 없는 Stat 헤더를 건너뜁니다 - {headerEntry.Key}");
                continue;
            }

            itemData.StatModifiers.Add(new StatModifier
            {
                Stat = statType,
                Value = statValue
            });
        }
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

        Sprite loadedIcon = LoadItemIcon(itemData.ItemId);

        if (loadedIcon != null)
        {
            itemData.ItemIcon = loadedIcon;
            return;
        }

        Debug.LogWarning($"ItemImporter: 아이콘 이미지를 찾지 못했습니다 - {itemData.ItemId}");
    }

    private static Sprite LoadItemIcon(string itemId)
    {
        string[] extensions = { ".png", ".PNG", ".jpg", ".JPG", ".jpeg", ".JPEG" };
        string existingAssetPath = null;
        bool foundExistingFileWithoutSprite = false;

        foreach (string extension in extensions)
        {
            string path = $"{IconFolderPath}/{itemId}{extension}";
            Debug.Log($"[Icon Search] 시도 중인 경로: {path}");

            Sprite sprite = TryLoadSpriteAssetAtPath(path);

            if (sprite != null)
            {
                Debug.Log($"[Icon Search] Sprite 찾기 성공: {path}");
                return sprite;
            }

            UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);

            if (mainAsset != null)
            {
                if (existingAssetPath == null)
                {
                    existingAssetPath = path;
                }

                Sprite recoveredSprite = TryConvertToSpriteAndReload(path);

                if (recoveredSprite != null)
                {
                    Debug.Log($"[Icon Search] Sprite 자동 복구 성공: {path}");
                    return recoveredSprite;
                }

                foundExistingFileWithoutSprite = true;
            }
        }

        if (foundExistingFileWithoutSprite && !string.IsNullOrWhiteSpace(existingAssetPath))
        {
            Debug.LogWarning($"ItemImporter: 파일은 존재하나 그 내부에 Sprite 개체가 하나도 없습니다. 경로: {existingAssetPath}");
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

    private static void EnsureFolderExists(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string normalizedPath = folderPath.Replace('\\', '/');
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

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, segments[i]);
            }

            currentPath = nextPath;
        }
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
}
