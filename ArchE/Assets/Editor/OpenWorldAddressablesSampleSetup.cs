#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

/// <summary>
/// 오픈월드 섹터용 Addressables 샘플 프리팹을 만들고 주소 World_Sector_0_0 로 등록합니다.
/// </summary>
public static class OpenWorldAddressablesSampleSetup
{
    private const string PrefabPath = "Assets/World/Addressables/SectorSampleMarker.prefab";

    private const string GroupName = "World_OpenWorld";

    private const string Address = "World_Sector_0_0";

    [MenuItem("Tools/ARCHÉ/Open World/Register Sample Sector Addressable")]
    public static void RegisterSampleSectorAddressable()
    {
        EnsureFolders();
        CreatePrefabIfMissing();

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            EditorUtility.DisplayDialog(
                "Addressables",
                "AddressableAssetSettings가 없습니다.\nWindow → Asset Management → Addressables 를 열어 초기화하세요.",
                "확인");
            return;
        }

        AddressableAssetGroup group = settings.FindGroup(GroupName);
        if (group == null)
        {
            var schemas = settings.DefaultGroup != null
                ? new System.Collections.Generic.List<AddressableAssetGroupSchema>(settings.DefaultGroup.Schemas)
                : null;

            group = settings.CreateGroup(
                GroupName,
                false,
                false,
                false,
                schemas,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));
        }

        string guid = AssetDatabase.AssetPathToGUID(PrefabPath);
        if (string.IsNullOrEmpty(guid))
        {
            EditorUtility.DisplayDialog("Open World", $"프리팹을 찾을 수 없습니다:\n{PrefabPath}", "확인");
            return;
        }

        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
        if (entry != null)
        {
            entry.address = Address;
        }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Open World",
            $"Addressables 등록 완료.\n주소: {Address}\n그룹: {GroupName}\n에셋: {PrefabPath}",
            "확인");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/World"))
        {
            AssetDatabase.CreateFolder("Assets", "World");
        }

        if (!AssetDatabase.IsValidFolder("Assets/World/Addressables"))
        {
            AssetDatabase.CreateFolder("Assets/World", "Addressables");
        }
    }

    private static void CreatePrefabIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "SectorSampleMarker";
        cube.transform.localScale = new Vector3(4f, 12f, 4f);

        PrefabUtility.SaveAsPrefabAsset(cube, PrefabPath);
        Object.DestroyImmediate(cube);
        AssetDatabase.SaveAssets();
    }
}
#endif
