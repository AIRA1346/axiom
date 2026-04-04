using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 프로젝트 내 ItemData 에셋을 모아 ItemManifest에 등록합니다. (LoadAll 대체)
/// </summary>
public static class ItemManifestBuilder
{
    private const string ManifestResourcePath = "Assets/Resources/ItemManifest.asset";

    [MenuItem("Tools/ARCHÉ/Items/Build Item Manifest")]
    public static void BuildManifest()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        var list = new List<ItemData>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null && !string.IsNullOrWhiteSpace(item.ItemId))
            {
                list.Add(item);
            }
        }

        list.Sort((a, b) => string.CompareOrdinal(a.ItemId, b.ItemId));

        ItemManifest manifest = AssetDatabase.LoadAssetAtPath<ItemManifest>(ManifestResourcePath);
        if (manifest == null)
        {
            manifest = ScriptableObject.CreateInstance<ItemManifest>();
            EnsureResourcesFolder();
            AssetDatabase.CreateAsset(manifest, ManifestResourcePath);
        }

        var so = new SerializedObject(manifest);
        SerializedProperty prop = so.FindProperty("_items");
        prop.ClearArray();
        for (int i = 0; i < list.Count; i++)
        {
            prop.InsertArrayElementAtIndex(i);
            prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manifest);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ItemManifest] 등록 완료: {list.Count}개 → {ManifestResourcePath}");
    }

    private static void EnsureResourcesFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
    }
}
