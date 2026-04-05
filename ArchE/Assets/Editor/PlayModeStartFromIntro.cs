#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 에디터에서 Play 시 현재 열린 씬이 아니라 인트로 씬부터 실행합니다.
/// </summary>
[InitializeOnLoad]
public static class PlayModeStartFromIntro
{
    private const string IntroSceneAssetPath = "Assets/Scenes/IntroScene.unity";

    private const string EditorPrefUseIntro = "ArchE.PlayModeStartFromIntro";

    static PlayModeStartFromIntro()
    {
        if (EditorPrefs.GetBool(EditorPrefUseIntro, true))
        {
            ApplyIntroAsPlayModeStartInternal();
        }
    }

    [MenuItem("Tools/ARCHÉ/Play Mode/Always Start From Intro Scene (On)", priority = 0)]
    public static void EnableIntroPlayModeStart()
    {
        EditorPrefs.SetBool(EditorPrefUseIntro, true);
        ApplyIntroAsPlayModeStartInternal();
        Debug.Log("[PlayModeStartFromIntro] Play 시 항상 IntroScene 에서 시작합니다.");
    }

    [MenuItem("Tools/ARCHÉ/Play Mode/Always Start From Intro Scene (Off)", priority = 1)]
    public static void DisableIntroPlayModeStart()
    {
        EditorPrefs.SetBool(EditorPrefUseIntro, false);
        EditorSceneManager.playModeStartScene = null;
        Debug.Log("[PlayModeStartFromIntro] Play 시 현재 열린 씬에서 시작합니다.");
    }

    private static void ApplyIntroAsPlayModeStartInternal()
    {
        var intro = AssetDatabase.LoadAssetAtPath<SceneAsset>(IntroSceneAssetPath);
        if (intro == null)
        {
            Debug.LogWarning(
                $"[PlayModeStartFromIntro] 인트로 씬을 찾을 수 없습니다: {IntroSceneAssetPath}\n" +
                "Tools → ARCHÉ → Setup → Create Intro Scene 로 생성하거나 경로를 확인하세요.");
            return;
        }

        EditorSceneManager.playModeStartScene = intro;
    }
}
#endif
