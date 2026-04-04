using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 프로젝트 내 모든 CraftingRecipe를 찾아 RecipeManifest에 등록합니다.
/// 레시피 에셋을 추가·삭제한 뒤 한 번 실행하세요.
/// </summary>
public static class RecipeManifestBuilder
{
    private const string ManifestResourcePath = "Assets/Resources/RecipeManifest.asset";

    [MenuItem("Tools/ARCHÉ/Items/Build Recipe Manifest")]
    public static void BuildManifest()
    {
        string[] guids = AssetDatabase.FindAssets("t:CraftingRecipe");
        var list = new List<CraftingRecipe>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(path);
            if (recipe != null)
            {
                list.Add(recipe);
            }
        }

        list.Sort((a, b) => string.CompareOrdinal(a != null ? a.RecipeId : "", b != null ? b.RecipeId : ""));

        RecipeManifest manifest = AssetDatabase.LoadAssetAtPath<RecipeManifest>(ManifestResourcePath);
        if (manifest == null)
        {
            manifest = ScriptableObject.CreateInstance<RecipeManifest>();
            EnsureResourcesFolder();
            AssetDatabase.CreateAsset(manifest, ManifestResourcePath);
        }

        var so = new SerializedObject(manifest);
        SerializedProperty prop = so.FindProperty("_recipes");
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

        Debug.Log($"[RecipeManifest] 등록 완료: {list.Count}개 → {ManifestResourcePath}");
    }

    private static void EnsureResourcesFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
    }
}
