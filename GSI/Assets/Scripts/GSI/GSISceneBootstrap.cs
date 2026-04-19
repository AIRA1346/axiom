using UnityEngine;

/// <summary>
/// G.S.I 씬 진입 시 로비 상태로 맞춥니다.
/// </summary>
public sealed class GSISceneBootstrap : MonoBehaviour
{
    /// <summary>GSIUIManager.Start보다 먼저 상태를 맞추기 위해 Awake에서 처리합니다.</summary>
    private void Awake()
    {
        GsiCoreServices.Ensure();
        GsiGameplayWorldCameraHooks.Initialize();

        GlobalSettingsOverlay.EnsureCreated();

        EnsureMemoryTestController();
        EnsureMotTestController();
        EnsureBulletHellTestController();

        if (GameManager.Instance == null)
        {
            GameManager orphan = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            if (orphan != null)
            {
                Debug.LogWarning(
                    "[GSISceneBootstrap] GameManager 오브젝트는 있으나 싱글톤 Instance가 비어 있습니다. GsiCoreServices가 GameManager를 생성했는지 확인하세요.");
            }
            else
            {
                Debug.LogWarning(
                    "[GSISceneBootstrap] GameManager가 없습니다. Intro 직후 GSIScene이거나 GsiCoreServices.Ensure()가 호출되었는지 확인하세요.");
            }

            return;
        }

        GameManager.Instance.SetGameState(GameState.MainMenu);
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
}
