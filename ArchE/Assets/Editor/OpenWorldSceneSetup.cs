#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 오픈월드 씬 생성 + Build Settings 등록 + Unity-Chan 프리팹을 OpenWorldBootstrap 에 연결합니다.
/// Tools → ARCHÉ → Open World → Generate Open World Scene & Build Settings
/// 지형을 씬 뷰에서 보이게 하려면 같은 메뉴 그룹의 Bake Terrain Into Current Scene (또는 OpenWorldBootstrap 인스펙터 버튼)을 사용하세요.
/// </summary>
public static class OpenWorldSceneSetup
{
    private const string ScenePath = "Assets/Scenes/OpenWorldScene.unity";

    private const string UnityChanPrefabPath = "Assets/unity-chan!/Unity-chan! Model/Prefabs/unitychan.prefab";

    [MenuItem("Tools/ARCHÉ/Open World/Generate Open World Scene & Build Settings")]
    public static void Generate()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityChanPrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog(
                "Open World",
                $"프리팹을 찾을 수 없습니다:\n{UnityChanPrefabPath}\n\nUnity-Chan! Model 을 Import 했는지 확인하세요.",
                "확인");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var go = new GameObject(nameof(OpenWorldBootstrap));
        var boot = go.AddComponent<OpenWorldBootstrap>();

        SerializedObject so = new SerializedObject(boot);
        SerializedProperty prop = so.FindProperty("_playerPrefab");
        if (prop != null)
        {
            prop.objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Open World", $"저장됨:\n{ScenePath}\n\nBuild Settings 에 등록했습니다.", "확인");
    }

    private static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == ScenePath))
        {
            EditorBuildSettings.scenes = scenes.ToArray();
            return;
        }

        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
