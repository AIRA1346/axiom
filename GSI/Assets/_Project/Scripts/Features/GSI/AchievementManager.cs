using ArchE.Platform;
using UnityEngine;

/// <summary>
/// 게임 내 성적과 이벤트를 모니터링하여 스팀 업적을 해금합니다.
/// GameManager와 ScoreManager가 포함된 ArchE.Game 어셈블리에 위치해야 합니다.
/// </summary>
public sealed class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    private bool _isSubscribed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (_isSubscribed || GameManager.Instance == null) return;

        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed || GameManager.Instance == null) return;

        GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        _isSubscribed = false;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.ResultScreen)
        {
            CheckAchievements();
        }
    }

    private void CheckAchievements()
    {
        if (ScoreManager.Instance == null || GameManager.Instance == null) return;

        // 1. 첫 발걸음 (어떤 테스트든 완료)
        if (!ScoreManager.Instance.IsFailed)
        {
            SteamworksService.UnlockAchievement("ACH_FIRST_STEPS");
        }

        // 2. 에임 마스터 (에임 테스트 S 등급 달성)
        if (GameManager.Instance.CurrentTestMode == TestMode.AimPrecision && ScoreManager.Instance.GetTier() == "S")
        {
            SteamworksService.UnlockAchievement("ACH_AIM_MASTER");
        }

        // 3. 공식 인증 (통합 공식 시험 합격)
        if (GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam && ScoreManager.Instance.LastUnifiedExamOverallPass)
        {
            SteamworksService.UnlockAchievement("ACH_UNIFIED_PASS");
        }
    }
}
