using UnityEditor;
using UnityEngine;

/// <summary>
/// Steam(PC) 배포에 맞춘 Player Settings를 한 번에 적용합니다.
/// Active Input Handling(Input System 전용)은 Project Settings에서 동기화되며, 필요 시 수동으로 확인하세요.
/// </summary>
public static class SteamPcPlayerSettingsHelper
{
    private const string MenuPath = "Tools/ARCHÉ/Apply Steam PC Player Settings";

    [MenuItem(MenuPath)]
    public static void ApplySteamPcDefaults()
    {
        PlayerSettings.runInBackground = true;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.allowFullscreenSwitch = true;
        PlayerSettings.visibleInBackground = true;

        AssetDatabase.SaveAssets();
        Debug.Log("[SteamPcPlayerSettingsHelper] Steam PC용 설정 적용: runInBackground, resizableWindow, 1920x1080 기본, Windowed, 풀스크린 전환 허용.");
    }
}
