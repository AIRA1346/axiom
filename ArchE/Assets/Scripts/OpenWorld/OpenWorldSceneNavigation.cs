using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 메인 허브(SampleScene) ↔ 오픈월드 씬 전환. 청크 스트리밍·세이브는 이후 확장.
/// </summary>
public static class OpenWorldSceneNavigation
{
    public static void LoadOpenWorld()
    {
        if (!Application.CanStreamedLevelBeLoaded(SceneNames.OpenWorld))
        {
            Debug.LogError(
                "[OpenWorld] 빌드에 OpenWorldScene 이 없습니다. Tools → ARCHÉ → Open World → Generate Open World Scene & Build Settings 를 실행하세요.");
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.MainMenu);
        }

        SceneManager.LoadScene(SceneNames.OpenWorld);
    }

    /// <summary>ARCHÉ 메인 허브(SampleScene)로 복귀합니다.</summary>
    public static void LoadArchEMainHub()
    {
        HealingSceneNavigation.LoadMainHub();
    }
}
