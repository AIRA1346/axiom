using UnityEngine;

/// <summary>
/// G.S.I 씬 진입 시 로비 상태로 맞춥니다.
/// </summary>
public sealed class GSISceneBootstrap : MonoBehaviour
{
    /// <summary>GSIUIManager.Start보다 먼저 상태를 맞추기 위해 Awake에서 처리합니다.</summary>
    private void Awake()
    {
        // Inactive scene GameManagers never run Awake, so Instance stays null and Ensure() would
        // spawn a duplicate host. Wake them first so the scene singleton wins without a throwaway.
        TryActivateSceneGameManagersIfInstanceMissing();
        GsiCoreServices.Ensure();
        // Lobby hub (Lobby/Shop/Inventory/Altar) does not stop BGM on scene unload; cut music when entering the exam facility.
        GsiAudio.StopMusic();
        GsiGameplayWorldCameraHooks.Initialize();

        GlobalSettingsOverlay.EnsureCreated();

        EnsureMemoryTestController();
        EnsureMotTestController();
        EnsureBulletHellTestController();
        EnsureCpsTestController();

        if (GameManager.Instance == null)
        {
            GameManager orphan = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            if (orphan != null)
            {
                Debug.LogWarning(
                    "[GSISceneBootstrap] GameManager is present but Instance is null. Check Awake/destroy order and duplicate GameManagers.");
            }
            else
            {
                Debug.LogWarning(
                    "[GSISceneBootstrap] No GameManager after GsiCoreServices.Ensure() — check Intro path and GsiCoreServices.Ensure() calls.");
            }

            return;
        }

        GameManager.Instance.SetGameState(GameState.MainMenu);
    }

    private static void TryActivateSceneGameManagersIfInstanceMissing()
    {
        if (GameManager.Instance != null)
        {
            return;
        }

        GameManager[] found = Object.FindObjectsByType<GameManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] == null)
            {
                continue;
            }

            if (!found[i].gameObject.activeInHierarchy)
            {
                found[i].gameObject.SetActive(true);
            }
        }
    }

    private void EnsureMemoryTestController()
    {
        Transform progress = transform.Find("Panels/TestInProgressPanel");
        if (progress == null)
        {
            return;
        }

        if (progress.GetComponent<MemoryTestController>() == null)
        {
            progress.gameObject.AddComponent<MemoryTestController>();
        }
    }

    private void EnsureMotTestController()
    {
        Transform progress = transform.Find("Panels/TestInProgressPanel");
        if (progress == null)
        {
            return;
        }

        // MemoryTestController와 동일: 런타임에 부착. Assembly.GetType("MotTestController")는 Unity에서 null이 되는 경우가 많아 사용하지 않음
        if (progress.GetComponent<MotTestController>() == null)
        {
            progress.gameObject.AddComponent<MotTestController>();
        }
    }

    private void EnsureBulletHellTestController()
    {
        Transform progress = transform.Find("Panels/TestInProgressPanel");
        if (progress == null)
        {
            return;
        }

        if (progress.GetComponent<BulletHellTestController>() == null)
        {
            progress.gameObject.AddComponent<BulletHellTestController>();
        }
    }

    private void EnsureCpsTestController()
    {
        Transform progress = transform.Find("Panels/TestInProgressPanel");
        if (progress == null)
        {
            return;
        }

        if (progress.GetComponent<CpsTestController>() == null)
        {
            progress.gameObject.AddComponent<CpsTestController>();
        }
    }
}
