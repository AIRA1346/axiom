using System;
using UnityEngine;

/// <summary>
/// G.S.I 공식 시험 흐름에서 사용되는 런타임 상태를 정의합니다.
/// </summary>
public enum TestMode
{
    Reaction,
    AimPrecision
}

public enum TestType
{
    Practice,
    OfficialExam
}

public enum GameState
{
    MainMenu,
    Crafting,
    Equipment,
    Inventory,
    Shop,
    Encyclopedia,
    Codex,
    TestStandby,
    TestInProgress,
    TestCompleted,
    ResultScreen
}

/// <summary>
/// G.S.I 시험 흐름의 중앙 상태 제어자.
/// 상태 전환만 담당하며 변경 시 브로드캐스트합니다.
/// </summary>
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// Invoked whenever the current game state changes.
    /// Other systems can subscribe to react without tight coupling.
    /// </summary>
    public event Action<GameState> OnGameStateChanged;

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public TestMode CurrentTestMode { get; private set; } = TestMode.Reaction;
    public TestType CurrentTestType { get; private set; } = TestType.Practice;

    private bool _isInputSubscribed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{gameObject.name}의 중복된 매니저 파괴됨.");
#endif
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SteamworksService.InitializePlaceholder();
    }

    private void Start()
    {
        SubscribeToInputManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromInputManager();
    }

    private void OnDestroy()
    {
        UnsubscribeFromInputManager();

        if (Instance == this)
        {
            SteamworksService.ShutdownPlaceholder();
            Instance = null;
        }
    }

    /// <summary>
    /// Updates the current state and notifies listeners only when a real change occurs.
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        CurrentState = newState;
        OnGameStateChanged?.Invoke(CurrentState);
    }

    /// <summary>
    /// Selects which test controller should respond during the next test flow.
    /// </summary>
    public void SetTestMode(TestMode mode)
    {
        CurrentTestMode = mode;
    }

    /// <summary>
    /// Selects whether the upcoming run is a practice session or an official exam.
    /// </summary>
    public void SetTestType(TestType type)
    {
        CurrentTestType = type;
    }

    /// <summary>
    /// Subscribes to the unified input stream that can start the active test.
    /// </summary>
    private void SubscribeToInputManager()
    {
        if (_isInputSubscribed)
        {
            return;
        }

        if (InputManager.Instance == null)
        {
            Debug.LogError("GameManager: 치명적 오류 - InputManager 인스턴스가 씬에 없습니다! GameObject에 붙어있는지 확인하세요.");
            return;
        }

        InputManager.Instance.OnInputDown += HandleInputDown;
        _isInputSubscribed = true;
#if UNITY_EDITOR
        Debug.Log("GameManager: InputManager 이벤트 구독 완료.");
#endif
    }

    /// <summary>
    /// Cleans up the input subscription when this manager is disabled or destroyed.
    /// </summary>
    private void UnsubscribeFromInputManager()
    {
        if (!_isInputSubscribed || InputManager.Instance == null)
        {
            return;
        }

        InputManager.Instance.OnInputDown -= HandleInputDown;
        _isInputSubscribed = false;
    }

    /// <summary>
    /// Advances the official test flow only when input is valid for the current state.
    /// </summary>
    private void HandleInputDown(Vector2 screenPosition)
    {
#if UNITY_EDITOR
        Debug.Log($"GameManager: HandleInputDown 진입 성공! 현재 상태: {CurrentState}");
#endif
        switch (CurrentState)
        {
            case GameState.MainMenu:
                break;

            case GameState.Crafting:
                break;

            case GameState.Equipment:
                break;

            case GameState.Inventory:
                break;

            case GameState.Shop:
                break;

            case GameState.TestStandby:
                SetGameState(GameState.TestInProgress);
#if UNITY_EDITOR
                Debug.Log("G.S.I: Test Started.");
#endif
                break;

            case GameState.TestInProgress:
            case GameState.TestCompleted:
            case GameState.ResultScreen:
                break;
        }
    }
}
