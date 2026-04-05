#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// OpenWorldScene 을 열었을 때 Player_UnityChan 이 없으면 자동으로 배치합니다.
/// </summary>
[InitializeOnLoad]
public static class OpenWorldSceneAutoPlacePlayer
{
    private const string OpenWorldSceneSuffix = "OpenWorldScene.unity";

    private const string EditorPrefAutoPlace = "ArchE.OpenWorldAutoPlacePlayerOnOpen";

    static OpenWorldSceneAutoPlacePlayer()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (!IsOpenWorldScenePath(scene.path))
        {
            return;
        }

        if (!EditorPrefs.GetBool(EditorPrefAutoPlace, true))
        {
            return;
        }

        EditorApplication.delayCall += () => TryAutoPlace(scene);
    }

    private static bool IsOpenWorldScenePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        path = path.Replace("\\", "/");
        return path.EndsWith(OpenWorldSceneSuffix);
    }

    private static void TryAutoPlace(Scene scene)
    {
        if (!scene.isLoaded || !IsOpenWorldScenePath(scene.path))
        {
            return;
        }

        OpenWorldBootstrap boot = null;
        foreach (OpenWorldBootstrap b in Object.FindObjectsByType<OpenWorldBootstrap>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (b != null && b.gameObject.scene == scene)
            {
                boot = b;
                break;
            }
        }

        if (boot == null)
        {
            return;
        }

        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go != null && go.scene == scene && go.name == "Player_UnityChan")
            {
                return;
            }
        }

        OpenWorldPlayerSceneSetup.PlacePlayerInScene(boot, false);
    }
}
#endif
