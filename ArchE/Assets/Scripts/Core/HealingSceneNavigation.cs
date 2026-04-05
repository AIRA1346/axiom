using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ARCHÉ 허브 ↔ 힐링 허브 ↔ 낚시(숲속 호수) 씬 전환.
/// GameManager는 DontDestroyOnLoad이므로 힐링 진입 시에도 유지됩니다.
/// </summary>
public static class HealingSceneNavigation
{
    public static void LoadHealingHub()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.MainMenu);
        }

        if (!Application.CanStreamedLevelBeLoaded(SceneNames.HealingHub))
        {
            Debug.LogError(
                "[Healing] 빌드에 HealingHubScene이 없습니다. Unity 메뉴 GSI → Healing → Generate Healing Scenes & Build Settings 를 실행하세요.");
            return;
        }

        SceneManager.LoadScene(SceneNames.HealingHub);
    }

    public static void LoadFishingLake()
    {
        if (!Application.CanStreamedLevelBeLoaded(SceneNames.FishingLake))
        {
            Debug.LogError(
                "[Healing] 빌드에 FishingLakeScene이 없습니다. Unity 메뉴 GSI → Healing → Generate Healing Scenes & Build Settings 를 실행하세요.");
            return;
        }

        SceneManager.LoadScene(SceneNames.FishingLake);
    }

    /// <summary>ARCHÉ 메인 허브(SampleScene)로 복귀합니다.</summary>
    public static void LoadMainHub()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.MainMenu);
        }

        SceneManager.LoadScene(SceneNames.MainHub);
    }
}
