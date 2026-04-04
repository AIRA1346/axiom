using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ARCHÉ 허브(SampleScene)와 G.S.I 전용 씬(GSIScene) 사이 전환.
/// </summary>
public static class GsiSceneNavigation
{
    public static void LoadGsiFacility()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LeaveGsiUnifiedExamContext();
            GameManager.Instance.SetGameState(GameState.MainMenu);
        }

        SceneManager.LoadScene(SceneNames.GSI);
    }

    public static void LoadArchEHub()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LeaveGsiUnifiedExamContext();
            GameManager.Instance.SetGameState(GameState.MainMenu);
        }

        SceneManager.LoadScene(SceneNames.MainHub);
    }
}
