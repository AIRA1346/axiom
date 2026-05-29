using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임플레이 씬의 메인 카메라를 Unity 기본 스카이박스 대신,
/// <see cref="GsiUiAppearance"/>·코스메틱 스킨과 맞는 단색 클리어로 둡니다.
/// </summary>
public static class GsiGameplayWorldCameraHooks
{
    private static bool _registered;

    public static void Initialize()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        GsiUiAppearance.Changed += OnAppearanceChanged;
        ApplyToSceneIfMatched(SceneManager.GetActiveScene());
    }

    public static void Shutdown()
    {
        if (!_registered)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        GsiUiAppearance.Changed -= OnAppearanceChanged;
        _registered = false;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode _)
    {
        ApplyToSceneIfMatched(scene);
    }

    private static void OnAppearanceChanged()
    {
        ApplyToSceneIfMatched(SceneManager.GetActiveScene());
    }

    private static bool ShouldApplyForSceneName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return false;
        }

        return sceneName == SceneNames.GSI || sceneName == SceneNames.AltarOfVerity;
    }

    private static void ApplyToSceneIfMatched(Scene scene)
    {
        if (!scene.IsValid() || !ShouldApplyForSceneName(scene.name))
        {
            return;
        }

        Color clear = GsiUiAppearance.GameplayWorldBackground;
        Camera[] cameras = Object.FindObjectsByType<Camera>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null || cam.gameObject.scene != scene)
            {
                continue;
            }

            if (!cam.CompareTag("MainCamera"))
            {
                continue;
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = clear;
        }
    }
}
