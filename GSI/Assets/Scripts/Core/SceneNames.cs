using UnityEngine.SceneManagement;

/// <summary>
/// 빌드에 포함된 씬 이름 상수. 이름 변경 시 여기와 Build Settings를 함께 수정하세요.
/// Steam 단독(GSI) 빌드: Intro → Lobby → GSIScene.
/// </summary>
public static class SceneNames
{
    public const string Intro = "IntroScene";
    public const string Lobby = "LobbyScene";
    public const string Shop = "ShopScene";
    public const string Inventory = "InventoryScene";
    public const string GSI = "GSIScene";

    public static bool IsActiveSceneGsi()
    {
        return SceneManager.GetActiveScene().name == GSI;
    }
}
