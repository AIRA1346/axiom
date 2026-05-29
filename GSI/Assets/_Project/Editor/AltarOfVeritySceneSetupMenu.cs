using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// <see cref="SceneNames.AltarOfVerityAssetPath"/> 씬이 Project에 없거나 빈 씬만 열려 있을 때,
/// 에디터에서 한 번에 생성·저장·빌드 등록·열기 할 수 있게 합니다.
/// </summary>
public static class AltarOfVeritySceneSetupMenu
{
    private static string ScenePath => SceneNames.AltarOfVerityAssetPath;

    [MenuItem("Tools/GSI/Scenes/Save Altar Of Verity Scene to Assets/Scenes", false, 100)]
    public static void CreateSaveAndOpen()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        bool exists = File.Exists(ScenePath);
        if (exists)
        {
            if (!EditorUtility.DisplayDialog(
                    "AltarOfVerityScene 덮어쓰기",
                    $"다음 파일을 덮어씁니다:\n{ScenePath}\n\n" +
                    "계속하면 기본 카메라·조명 + VerityAltarRoot(컨트롤러)만 있는 씬으로 다시 저장됩니다.",
                    "덮어쓰기",
                    "취소"))
            {
                return;
            }
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var rootGo = new GameObject("VerityAltarRoot");
        rootGo.AddComponent<AltarOfVeritySceneController>();

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            EditorUtility.DisplayDialog("저장 실패", $"씬을 저장할 수 없습니다.\n{ScenePath}", "확인");
            return;
        }

        AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        RegisterOrSyncAltarInBuildSettings();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(ScenePath);

        EditorUtility.DisplayDialog(
            "완료",
            $"저장 및 열기 완료:\n{ScenePath}\n\n" +
            "Project 창의 Assets/Scenes에서도 확인할 수 있습니다.\n" +
            "Unity Hub에서 연 프로젝트 루트가 이 GSI 폴더인지 확인하세요.",
            "확인");
    }

    [MenuItem("Tools/GSI/Scenes/Sync Altar scene — Build Settings GUID", false, 102)]
    public static void SyncBuildSettingsGuidOnly()
    {
        if (!File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("파일 없음", ScenePath, "확인");
            return;
        }

        AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        RegisterOrSyncAltarInBuildSettings();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", "EditorBuildSettings의 Altar 씬 항목을 메타 GUID에 맞췄습니다.", "확인");
    }

    [MenuItem("Tools/GSI/Scenes/Open Altar Of Verity Scene (disk)", false, 101)]
    public static void OpenFromDisk()
    {
        if (!File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog(
                "파일 없음",
                $"다음 경로에 씬 파일이 없습니다:\n{ScenePath}\n\n" +
                "먼저 \"Save Altar Of Verity Scene to Assets/Scenes\" 메뉴를 실행하세요.",
                "확인");
            return;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    /// <summary>
    /// 빌드 목록에 없으면 추가하고, 있으면 <see cref="EditorBuildSettingsScene"/> 를 다시 만들어
    /// 깨진 guid(Deleted)를 메타와 일치시킵니다.
    /// </summary>
    private static void RegisterOrSyncAltarInBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        int idx = scenes.FindIndex(s => s.path == ScenePath);
        if (idx >= 0)
        {
            bool wasEnabled = scenes[idx].enabled;
            scenes[idx] = new EditorBuildSettingsScene(ScenePath, wasEnabled);
        }
        else
        {
            int gsi = scenes.FindIndex(s => s.path != null && s.path.Contains("GSIScene"));
            var entry = new EditorBuildSettingsScene(ScenePath, true);
            if (gsi >= 0)
            {
                scenes.Insert(gsi, entry);
            }
            else
            {
                scenes.Add(entry);
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
