using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// G.S.I 씬 진입 시 로비 상태로 맞추고, 하이브리드 미니게임 생명주기(씬 분리 및 동적 스폰)를 관리합니다.
/// </summary>
public sealed class GSISceneBootstrap : MonoBehaviour
{
    private GameObject _activeGameInstance;
    private string _activeLoadedSceneName;
    private bool _isSubscribed;

    /// <summary>GSIUIManager.Start보다 먼저 상태를 맞추기 위해 Awake에서 처리합니다.</summary>
    private void Awake()
    {
        TryActivateSceneGameManagersIfInstanceMissing();
        GsiCoreServices.Ensure();
        // Lobby hub (Lobby/Shop/Inventory/Altar) does not stop BGM on scene unload; cut music when entering the exam facility.
        GsiAudio.StopMusic();
        GsiGameplayWorldCameraHooks.Initialize();

        GlobalSettingsOverlay.EnsureCreated();

        // 씬 시작 시 전체 미니게임을 한꺼번에 스폰하는 구조를 비활성화하여 메모리를 100% 최적화합니다.
        // EnsureActiveMiniGameControllers() 호출 제거.

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

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            _isSubscribed = true;
        }
    }

    private void OnDestroy()
    {
        if (_isSubscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        // 혹시 비정상적으로 씬이 꺼지더라도 동적 인스턴스 및 추가 씬을 청소합니다.
        CleanupActiveGame();
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.TestInProgress)
        {
            LaunchActiveMiniGame();
        }
        else
        {
            CleanupActiveGame();
        }
    }

    private void LaunchActiveMiniGame()
    {
        CleanupActiveGame();

        if (GameManager.Instance == null) return;

        TestMode mode = GameManager.Instance.CurrentTestMode;
        
        // 1. 레지스트리에서 현재 미니게임 식별자 탐색
        var descriptor = ArchE.Game.MiniGameRegistry.Instance != null
            ? ArchE.Game.MiniGameRegistry.Instance.FindGameByLegacyMode(mode)
            : null;

        Transform progressPanel = transform.Find("Panels/TestInProgressPanel");
        if (progressPanel == null)
        {
            Debug.LogError("[GSISceneBootstrap] Panels/TestInProgressPanel 을 씬에서 찾을 수 없습니다.");
            return;
        }

        // 2. 하이브리드 로드: 분리 씬 세팅이 되어 있는 경우
        if (descriptor != null && !string.IsNullOrEmpty(descriptor.SceneName))
        {
            Debug.Log($"[GSISceneBootstrap] 하이브리드 아키텍처: 미니게임 씬 '{descriptor.SceneName}'을 Additive 로드합니다.");
            _activeLoadedSceneName = descriptor.SceneName;
            SceneManager.LoadSceneAsync(descriptor.SceneName, LoadSceneMode.Additive);
            return;
        }

        // 3. 폴백(Fallback) 구조: 분리 씬이 없거나 지정되지 않은 경우 동적 스폰(Instantiate)
        if (descriptor != null)
        {
            if (descriptor.ControllerPrefab != null)
            {
                Debug.Log($"[GSISceneBootstrap] 동적 폴백: 프리팹 '{descriptor.ControllerPrefab.name}'을 실시간 인스턴스화합니다.");
                _activeGameInstance = Instantiate(descriptor.ControllerPrefab, progressPanel);
                _activeGameInstance.name = descriptor.ControllerPrefab.name;
            }
            else if (!string.IsNullOrEmpty(descriptor.ControllerAssemblyQualifiedName))
            {
                Debug.Log($"[GSISceneBootstrap] 동적 폴백: 컴포넌트 '{descriptor.ControllerAssemblyQualifiedName}'을 추가합니다.");
                System.Type t = System.Type.GetType(descriptor.ControllerAssemblyQualifiedName);
                if (t != null)
                {
                    _activeGameInstance = new GameObject(descriptor.GameId + "_Controller");
                    _activeGameInstance.transform.SetParent(progressPanel, false);
                    _activeGameInstance.AddComponent(t);
                }
            }
        }
        else
        {
            // 4. 레거시 완전 하드코딩 폴백 (레지스트리 예외 대비)
            string legacyTypeName = GetLegacyControllerTypeName(mode);
            Debug.Log($"[GSISceneBootstrap] 레거시 하드코딩 폴백: {legacyTypeName} 동적 주입.");
            System.Type t = System.Type.GetType(legacyTypeName);
            if (t != null)
            {
                _activeGameInstance = new GameObject(mode + "_LegacyController");
                _activeGameInstance.transform.SetParent(progressPanel, false);
                _activeGameInstance.AddComponent(t);
            }
        }
    }

    private void CleanupActiveGame()
    {
        // A. 동적 생성된 게임 오브젝트 파괴 및 즉시 가비지 컬렉팅
        if (_activeGameInstance != null)
        {
            Debug.Log($"[GSISceneBootstrap] 미니게임 종료: 동적 인스턴스 '{_activeGameInstance.name}'을 파괴 및 소멸시킵니다.");
            Destroy(_activeGameInstance);
            _activeGameInstance = null;
        }

        // B. Additive 로드되어 있던 서브 미니게임 씬 언로드
        if (!string.IsNullOrEmpty(_activeLoadedSceneName))
        {
            Debug.Log($"[GSISceneBootstrap] 미니게임 종료: Additive 씬 '{_activeLoadedSceneName}'을 언로드 및 리소스를 반환합니다.");
            SceneManager.UnloadSceneAsync(_activeLoadedSceneName);
            _activeLoadedSceneName = null;
        }
    }

    private static string GetLegacyControllerTypeName(TestMode mode)
    {
        switch (mode)
        {
            case TestMode.Reaction:
                return "ReactionTestController, ArchE.Game";
            case TestMode.AimPrecision:
                return "AimTestController, ArchE.Game";
            case TestMode.MemorySequence:
                return "MemoryTestController, ArchE.Game";
            case TestMode.RhythmTiming:
                return "RhythmTestController, ArchE.Game";
            case TestMode.MultipleObjectTracking:
                return "MotTestController, ArchE.Game";
            case TestMode.BulletHell:
                return "BulletHellTestController, ArchE.Game";
            case TestMode.ClicksPerSecond:
                return "CpsTestController, ArchE.Game";
            default:
                return "ReactionTestController, ArchE.Game";
        }
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
}
