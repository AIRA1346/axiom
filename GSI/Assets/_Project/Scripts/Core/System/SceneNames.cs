using UnityEngine.SceneManagement;

/// <summary>
/// 빌드에 포함된 씬 이름 상수. 이름 변경 시 여기와 Build Settings를 함께 수정하세요.
/// Steam 단독(GSI) 빌드: Intro → Lobby → GSIScene.
/// GSI 프로젝트 루트 기준 씬 파일은 모두 <c>Assets/Scenes/*.unity</c> 에 둡니다.
/// </summary>
public static class SceneNames
{
    public const string Intro = "IntroScene";
    public const string Lobby = "LobbyScene";
    public const string Shop = "ShopScene";
    public const string Inventory = "InventoryScene";

    /// <summary>런타임 <see cref="UnityEngine.SceneManagement.SceneManager"/> 용 씬 이름(파일명과 동일).</summary>
    public const string AltarOfVerity = "AltarOfVerityScene";

    /// <summary>에디터 Project 창 경로. <see cref="AltarOfVerity"/> 씬은 반드시 이 경로에 둡니다.</summary>
    public const string AltarOfVerityAssetPath = "Assets/Scenes/AltarOfVerityScene.unity";

    public const string GSI = "GSIScene";

    public static bool IsActiveSceneGsi()
    {
        return SceneManager.GetActiveScene().name == GSI;
    }
}
