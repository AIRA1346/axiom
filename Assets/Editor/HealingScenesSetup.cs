#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 힐링 허브·낚시 씬 파일을 생성하고 Build Settings에 등록합니다.
/// Unity 메뉴: Tools → ARCHÉ → GSI → Healing → Generate Scenes & Build Settings
/// </summary>
public static class HealingScenesSetup
{
    private const string HubPath = "Assets/Scenes/HealingHubScene.unity";
    private const string FishPath = "Assets/Scenes/FishingLakeScene.unity";

    [MenuItem("Tools/ARCHÉ/GSI/Healing/Generate Scenes & Build Settings")]
    public static void Generate()
    {
        CreateSceneWithBootstrap<HealingHubBootstrap>(HubPath, "HealingHubBootstrap");
        CreateSceneWithBootstrap<FishingLakeBootstrap>(FishPath, "FishingLakeBootstrap");
        AddToBuildSettings();
        AssetDatabase.Refresh();
    }

    private static void CreateSceneWithBootstrap<T>(string path, string objectName) where T : Component
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var go = new GameObject(objectName);
        go.AddComponent<T>();
        EditorSceneManager.SaveScene(scene, path);
    }

    private static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        void Add(string p)
        {
            if (scenes.Any(s => s.path == p))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(p, true));
        }

        Add(HubPath);
        Add(FishPath);
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
