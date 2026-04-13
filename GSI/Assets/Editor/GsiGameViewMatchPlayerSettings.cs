#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 에디터 Game 뷰 해상도를 Player Settings(기본 1920×1080)와 맞춰,
/// 플레이 모드/스탠드얼론 빌드의 UI 스케일을 최대한 동일하게 보이게 합니다.
/// </summary>
[InitializeOnLoad]
public static class GsiGameViewMatchPlayerSettings
{
    private const string EditorPrefAutoOnPlay = "GSI.Editor.MatchGameViewToPlayerOnPlay";

    static GsiGameViewMatchPlayerSettings()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
        {
            return;
        }

        if (!EditorPrefs.GetBool(EditorPrefAutoOnPlay, true))
        {
            return;
        }

        EditorApplication.delayCall += ApplyGameViewToPlayerSettings;
    }

    [MenuItem("Tools/G.S.I/Game View/Player 설정과 동일한 해상도로 맞추기 (지금) %#g", false, 0)]
    public static void MatchGameViewToPlayerSettingsMenu()
    {
        ApplyGameViewToPlayerSettings();
    }

    [MenuItem("Tools/G.S.I/Game View/플레이 시 자동 맞춤 (켜기)", false, 10)]
    public static void EnableAutoMatchOnPlay()
    {
        EditorPrefs.SetBool(EditorPrefAutoOnPlay, true);
        Debug.Log("[GSI] 플레이 진입 시 Game 뷰를 Player 해상도에 맞춥니다.");
    }

    [MenuItem("Tools/G.S.I/Game View/플레이 시 자동 맞춤 (끄기)", false, 11)]
    public static void DisableAutoMatchOnPlay()
    {
        EditorPrefs.SetBool(EditorPrefAutoOnPlay, false);
        Debug.Log("[GSI] 플레이 시 Game 뷰 자동 맞춤을 끕니다.");
    }

    private static void ApplyGameViewToPlayerSettings()
    {
        int w = PlayerSettings.defaultScreenWidth;
        int h = PlayerSettings.defaultScreenHeight;
        if (w <= 0)
        {
            w = 1920;
        }

        if (h <= 0)
        {
            h = 1080;
        }

        try
        {
            if (!TrySetGameViewResolution(w, h))
            {
                Debug.LogWarning(
                    "[GSI] Game 뷰 해상도를 스크립트로 설정하지 못했습니다. " +
                    "Game 탭에서 해상도를 " + w + "×" + h + " 로 선택하거나, " +
                    "Tools → G.S.I → Game View → Player 설정과 동일한 해상도로 맞추기 를 실행하세요.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[GSI] Game 뷰 해상도 설정 중 예외: " + e.Message);
        }
    }

    /// <summary>Unity 버전에 따라 내부 API가 달라질 수 있어, 실패 시 false.</summary>
    private static bool TrySetGameViewResolution(int width, int height)
    {
        Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        if (gameViewType == null)
        {
            return false;
        }

        EditorWindow gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
        if (gameView == null)
        {
            return false;
        }

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        PropertyInfo sizeProp = gameViewType.GetProperty("currentGameViewSize", flags);
        if (sizeProp == null)
        {
            return false;
        }

        object sizeObj = sizeProp.GetValue(gameView);
        if (sizeObj == null)
        {
            return false;
        }

        Type sizeType = sizeObj.GetType();

        FieldInfo sizeTypeField = sizeType.GetField("sizeType", flags);
        if (sizeTypeField != null && sizeTypeField.FieldType.IsEnum)
        {
            Array enumValues = Enum.GetValues(sizeTypeField.FieldType);
            // 1 = Fixed Resolution in typical Unity GameViewSizeType layouts
            object fixedResolution = enumValues.Length > 1 ? enumValues.GetValue(1) : null;
            if (fixedResolution != null)
            {
                sizeTypeField.SetValue(sizeObj, fixedResolution);
            }
        }

        FieldInfo wField = sizeType.GetField("width", flags);
        FieldInfo hField = sizeType.GetField("height", flags);
        if (wField != null)
        {
            wField.SetValue(sizeObj, width);
        }

        if (hField != null)
        {
            hField.SetValue(sizeObj, height);
        }

        sizeProp.SetValue(gameView, sizeObj, null);

        MethodInfo updateZoom = gameViewType.GetMethod("UpdateZoomAreaAndParent", flags);
        updateZoom?.Invoke(gameView, null);

        gameView.Repaint();
        return true;
    }
}
#endif
