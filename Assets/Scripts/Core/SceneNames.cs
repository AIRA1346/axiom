using UnityEngine.SceneManagement;

/// <summary>
/// 빌드에 포함된 씬 이름 상수. 이름 변경 시 여기와 Build Settings를 함께 수정하세요.
/// </summary>
public static class SceneNames
{
    public const string Intro = "IntroScene";
    public const string MainHub = "SampleScene";
    public const string GSI = "GSIScene";
    public const string HealingHub = "HealingHubScene";
    public const string FishingLake = "FishingLakeScene";

    public static bool IsActiveSceneGsi()
    {
        return SceneManager.GetActiveScene().name == GSI;
    }
}
