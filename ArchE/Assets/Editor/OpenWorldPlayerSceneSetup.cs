#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 오픈월드 씬에 플레이어 프리팹을 스폰 위치에 배치해 편집 모드에서도 보이게 합니다.
/// </summary>
public static class OpenWorldPlayerSceneSetup
{
    [MenuItem("Tools/ARCHÉ/Open World/Place Player In Scene")]
    public static void PlacePlayerInSceneFromMenu()
    {
        PlacePlayerInScene(null, true);
    }

    [MenuItem("Tools/ARCHÉ/Open World/Auto-place Player On Scene Open (Toggle)")]
    public static void ToggleAutoPlaceOnOpen()
    {
        const string k = "ArchE.OpenWorldAutoPlacePlayerOnOpen";
        bool v = !EditorPrefs.GetBool(k, true);
        EditorPrefs.SetBool(k, v);
        EditorUtility.DisplayDialog(
            "Open World",
            v
                ? "OpenWorldScene 을 열 때 플레이어가 없으면 자동 배치합니다."
                : "자동 배치를 끕니다. 수동으로 Place Player In Scene 을 사용하세요.",
            "확인");
    }

    /// <param name="boot">null 이면 활성 씬 우선으로 OpenWorldBootstrap 을 찾습니다.</param>
    /// <param name="showCompletionDialog">자동 배치 시 false 로 완료 팝업을 띄우지 않습니다.</param>
    public static void PlacePlayerInScene(OpenWorldBootstrap boot, bool showCompletionDialog = true)
    {
        if (boot == null)
        {
            boot = FindOpenWorldBootstrapPreferActiveScene();
        }

        if (boot == null)
        {
            if (showCompletionDialog)
            {
                EditorUtility.DisplayDialog("Open World", "로드된 씬에 OpenWorldBootstrap 이 없습니다.", "확인");
            }

            return;
        }

        SerializedObject so = new SerializedObject(boot);
        var prefab = so.FindProperty("_playerPrefab").objectReferenceValue as GameObject;
        if (prefab == null)
        {
            if (showCompletionDialog)
            {
                EditorUtility.DisplayDialog("Open World", "OpenWorldBootstrap 에 Player Prefab 을 넣어주세요.", "확인");
            }
            else
            {
                Debug.LogWarning("[OpenWorld] 자동 배치: Player Prefab 이 비어 있어 건너뜁니다.");
            }

            return;
        }

        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go != null && go.scene == boot.gameObject.scene && go.name == "Player_UnityChan")
            {
                Undo.DestroyObjectImmediate(go);
            }
        }

        Vector3 pos = boot.GetSpawnWorldPositionPreview();

        GameObject player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (player == null)
        {
            player = Object.Instantiate(prefab, pos, Quaternion.identity);
            Undo.RegisterCreatedObjectUndo(player, "Place Player In Scene");
        }
        else
        {
            Undo.RegisterCreatedObjectUndo(player, "Place Player In Scene");
        }

        if (player.scene != boot.gameObject.scene)
        {
            SceneManager.MoveGameObjectToScene(player, boot.gameObject.scene);
        }

        player.name = "Player_UnityChan";
        player.transform.SetPositionAndRotation(pos, Quaternion.identity);

        so.FindProperty("_playerPlacedInScene").objectReferenceValue = player;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(boot.gameObject.scene);
        Selection.activeGameObject = player;

        if (showCompletionDialog)
        {
            EditorUtility.DisplayDialog(
                "Open World",
                "씬에 Player_UnityChan 을 배치하고 Player Placed In Scene 에 연결했습니다.\n변경을 유지하려면 씬을 저장하세요.",
                "확인");
        }
        else
        {
            Debug.Log("[OpenWorld] OpenWorldScene 에 Player_UnityChan 을 자동 배치했습니다. 씬을 저장하세요.");
        }
    }

    /// <summary>여러 씬이 열려 있을 때 활성 씬의 Bootstrap 을 우선합니다.</summary>
    public static OpenWorldBootstrap FindOpenWorldBootstrapPreferActiveScene()
    {
        Scene active = EditorSceneManager.GetActiveScene();
        foreach (OpenWorldBootstrap b in Object.FindObjectsByType<OpenWorldBootstrap>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (b != null && b.gameObject.scene == active)
            {
                return b;
            }
        }

        return Object.FindFirstObjectByType<OpenWorldBootstrap>();
    }
}
#endif
