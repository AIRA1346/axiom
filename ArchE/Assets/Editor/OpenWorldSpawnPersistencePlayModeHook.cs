#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 에디터에서 Play 종료 시 Player_UnityChan 위치를 저장합니다.
/// </summary>
[InitializeOnLoad]
public static class OpenWorldSpawnPersistencePlayModeHook
{
    static OpenWorldSpawnPersistencePlayModeHook()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode)
        {
            return;
        }

        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go == null || go.name != "Player_UnityChan")
            {
                continue;
            }

            if (!go.scene.IsValid() || go.scene.name != "OpenWorldScene")
            {
                continue;
            }

            OpenWorldSpawnPersistence.Save(go.transform.position, go.transform.rotation);
            return;
        }
    }
}
#endif
