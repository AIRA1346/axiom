using UnityEngine;

/// <summary>
/// Steam 단독 빌드에서 Intro가 GSIScene으로 바로 이어질 때
/// DontDestroyOnLoad 싱글톤(GameManager, Economy, 입력)을 보장합니다.
/// </summary>
public static class GsiCoreServices
{
    public static void Ensure()
    {
        if (GameManager.Instance == null)
        {
            var go = new GameObject("GSI_CoreServices");
            go.AddComponent<GameManager>();
            go.AddComponent<EconomyManager>();
            go.AddComponent<InputManager>();
            go.AddComponent<TouchInputFeedback>();
            go.AddComponent<GsiSettingsAudioApplier>();
            go.AddComponent<GsiAudioService>();
            go.AddComponent<GsiUiInteractionSound>();
            return;
        }

        GameObject host = GameManager.Instance.gameObject;

        if (EconomyManager.Instance == null)
        {
            host.AddComponent<EconomyManager>();
        }

        if (InputManager.Instance == null)
        {
            host.AddComponent<InputManager>();
        }

        if (host.GetComponent<TouchInputFeedback>() == null)
        {
            host.AddComponent<TouchInputFeedback>();
        }

        if (host.GetComponent<GsiSettingsAudioApplier>() == null)
        {
            host.AddComponent<GsiSettingsAudioApplier>();
        }

        if (host.GetComponent<GsiAudioService>() == null)
        {
            host.AddComponent<GsiAudioService>();
        }

        if (host.GetComponent<GsiUiInteractionSound>() == null)
        {
            host.AddComponent<GsiUiInteractionSound>();
        }
    }
}
