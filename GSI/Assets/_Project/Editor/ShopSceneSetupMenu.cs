#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 상점 씬 생성 및 빌드 설정 등록. 메뉴: GSI → Setup → Create Shop Scene And Add To Build
/// </summary>
public static class ShopSceneSetupMenu
{
    private const string ShopScenePath = "Assets/Scenes/ShopScene.unity";

    [MenuItem("GSI/Setup/Create Shop Scene And Add To Build")]
    public static void CreateShopSceneAndAddToBuild()
    {
        if (File.Exists(ShopScenePath))
        {
            if (!EditorUtility.DisplayDialog(
                    "Shop Scene",
                    "ShopScene.unity already exists. Overwrite?",
                    "Overwrite",
                    "Cancel"))
            {
                AddToBuildIfMissing();
                return;
            }
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Directional Light")
            {
                Object.DestroyImmediate(root);
            }
        }

        var shopRoot = new GameObject("ShopRoot");
        shopRoot.AddComponent<ShopSceneController>();

        EditorSceneManager.SaveScene(scene, ShopScenePath);
        AssetDatabase.Refresh();

        AddToBuildIfMissing();

        EditorUtility.DisplayDialog(
            "GSI",
            "Saved: " + ShopScenePath + "\n(Registered in Build Settings if missing.)",
            "OK");
    }

    private static void AddToBuildIfMissing()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == ShopScenePath))
        {
            return;
        }

        int insertIndex = -1;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path.Replace("\\", "/").Contains("LobbyScene"))
            {
                insertIndex = i + 1;
                break;
            }
        }

        var entry = new EditorBuildSettingsScene(ShopScenePath, true);
        if (insertIndex >= 0 && insertIndex <= scenes.Count)
        {
            scenes.Insert(insertIndex, entry);
        }
        else
        {
            scenes.Add(entry);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
