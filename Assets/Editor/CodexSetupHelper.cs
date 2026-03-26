using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Codex 시스템 씬 설정 헬퍼.
/// Menu: Tools > Codex > Add CodexManager to Scene
/// </summary>
public static class CodexSetupHelper
{
    [MenuItem("Tools/Codex/1. Create Codex Cell Prefab")]
    public static void CreateCellPrefab()
    {
        CodexCellPrefabCreator.CreateCodexCellPrefab();
    }

    [MenuItem("Tools/Codex/2. Create Codex Panel Prefab")]
    public static void CreatePanelPrefab()
    {
        CodexPanelCreator.CreateCodexPanelPrefab();
    }

    [MenuItem("Tools/Codex/3. Build All Codex Databases")]
    public static void BuildAllDatabases()
    {
        CodexDataBuilder.BuildAllFromFolder();
    }

    [MenuItem("Tools/Codex/4. Add CodexManager to Scene")]
    public static void AddCodexManagerToScene()
    {
        if (Object.FindObjectOfType<CodexManager>() != null)
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

    [MenuItem("Tools/Codex/5. Codex Importer Settings (구글 시트)")]
    public static void OpenCodexImporter()
    {
        CodexImporter.OpenWindow();
    }
}
