using UnityEngine;

/// <summary>
/// G.S.I 시설(GSIScene) 진입 및 로비 복귀(ArchE 연동 빌드는 별도 프로젝트에서 SampleScene 사용).
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

        GsiSceneTransition.LoadScene(SceneNames.GSI);
    }

    /// <summary>로비 등에서 호출: 상점 전용 씬으로 이동합니다.</summary>
    public static void LoadShop()
    {
        GsiSceneTransition.LoadScene(SceneNames.Shop);
    }

    /// <summary>보유 골드·응시권·스킨 확인·장착 전용 인벤토리 씬으로 이동합니다.</summary>
    public static void LoadInventory()
    {
        GsiSceneTransition.LoadScene(SceneNames.Inventory);
    }

    /// <summary>상점에서 로비로 돌아갑니다.</summary>
    public static void LoadLobby()
    {
        GsiSceneTransition.LoadScene(SceneNames.Lobby);
    }

    /// <summary>G.S.I 시설에서 ArchE 로비(메인 메뉴 씬)로 돌아갑니다. GSI 단독 빌드에서도 LobbyScene으로 이동합니다.</summary>
    public static void LoadArchEHub()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LeaveGsiUnifiedExamContext();
            GameManager.Instance.SetGameState(GameState.MainMenu);
        }

        GsiSceneTransition.LoadScene(SceneNames.Lobby);
    }
}
