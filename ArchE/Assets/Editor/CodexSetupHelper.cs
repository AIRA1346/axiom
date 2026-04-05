using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Codex 시스템 씬 설정 헬퍼.
/// Menu: Tools > ARCHÉ > Codex (구글 시트 임포트는 Tools > ARCHÉ > Google Sheet)
/// </summary>
public static class CodexSetupHelper
{
    [MenuItem("Tools/ARCHÉ/Codex/Create Cell Prefab")]
    public static void CreateCellPrefab()
    {
        CodexCellPrefabCreator.CreateCodexCellPrefab();
    }

    [MenuItem("Tools/ARCHÉ/Codex/Create Panel Prefab")]
    public static void CreatePanelPrefab()
    {
        CodexPanelCreator.CreateCodexPanelPrefab();
    }

    [MenuItem("Tools/ARCHÉ/Codex/Build Index & Content")]
    public static void BuildAllDatabases()
    {
        CodexDataBuilder.BuildAllFromFolder();
    }

    [MenuItem("Tools/ARCHÉ/Codex/Add CodexManager to Scene")]
    public static void AddCodexManagerToScene()
    {
        if (Object.FindFirstObjectByType<CodexManager>(FindObjectsInactive.Exclude) != null)
        {
            Debug.Log("[Codex] CodexManager가 이미 씬에 있습니다.");
            return;
        }

        var go = new GameObject("CodexManager");
        go.AddComponent<CodexManager>();
        Undo.RegisterCreatedObjectUndo(go, "Add CodexManager");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Codex] CodexManager가 씬에 추가되었습니다.");
    }
}
