namespace ArchE.Platform
{
using UnityEngine;

#if STEAMWORKS_ENABLED
using Steamworks;
#endif

/// <summary>
/// Steamworks.NET 래퍼. <c>STEAMWORKS_ENABLED</c>가 없으면 항상 비활성(no-op).
/// 에디터/스탠드얼론에서 테스트하려면 Tools → GSI → Steam 에서 심볼을 켜고,
/// 프로젝트 루트(GSI 폴더, Assets 옆)에 <c>steam_appid.txt</c>를 두세요(예시: <c>steam_appid.txt.example</c>).
/// 스토어 빌드에서는 Valve 권장대로 <c>AppId_t.Invalid</c> 대신 실제 App ID를 쓰고 depot에서 steam_appid.txt 를 제거하세요.
/// </summary>
public static class SteamworksService
{
    /// <summary>SteamAPI_Init 성공 여부.</summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>업적 해금을 시도합니다.</summary>
    public static void UnlockAchievement(string achievementId)
    {
#if STEAMWORKS_ENABLED
        if (!IsInitialized) return;

        bool success = SteamUserStats.SetAchievement(achievementId);
        if (success)
        {
            SteamUserStats.StoreStats();
            Debug.Log($"[Steamworks] Achievement unlocked: {achievementId}");
        }
        else
        {
            Debug.LogWarning($"[Steamworks] Failed to unlock achievement: {achievementId}");
        }
#else
        Debug.Log($"[Steamworks Placeholder] Achievement {achievementId} would be unlocked.");
#endif
    }

    /// <summary>리더보드 점수 갱신을 시도합니다. (비동기 처리)</summary>
    public static void UpdateLeaderboardScore(string leaderboardName, int score)
    {
#if STEAMWORKS_ENABLED
        if (!IsInitialized) return;

        InitCallResults();
        m_pendingScore = score;

        // 오름차순(시간 기록이 짧을수록 고순위인 경우)으로 리더보드를 생성하거나 찾습니다.
        SteamAPICall_t handle = SteamUserStats.FindOrCreateLeaderboard(
            leaderboardName, 
            ELeaderboardSortMethod.k_ELeaderboardSortMethodAscending, 
            ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeSeconds
        );

        if (m_findLeaderboardCallResult != null)
        {
            m_findLeaderboardCallResult.Set(handle);
            Debug.Log($"[Steamworks] Leaderboard '{leaderboardName}' search initiated for score: {score}");
        }
#else
        Debug.Log($"[Steamworks Placeholder] Leaderboard {leaderboardName} would be updated with score: {score}");
#endif
    }

#if STEAMWORKS_ENABLED
    private static CallResult<LeaderboardFindResult_t> m_findLeaderboardCallResult;
    private static CallResult<LeaderboardScoreUploaded_t> m_uploadScoreCallResult;
    private static int m_pendingScore;

    private static void InitCallResults()
    {
        if (m_findLeaderboardCallResult == null)
        {
            m_findLeaderboardCallResult = CallResult<LeaderboardFindResult_t>.Create(OnLeaderboardFindResult);
        }
        if (m_uploadScoreCallResult == null)
        {
            m_uploadScoreCallResult = CallResult<LeaderboardScoreUploaded_t>.Create(OnLeaderboardScoreUploaded);
        }
    }

    private static void OnLeaderboardFindResult(LeaderboardFindResult_t pCallback, bool bIOFailure)
    {
        if (bIOFailure || pCallback.m_bLeaderboardFound == 0)
        {
            Debug.LogWarning("[Steamworks] Failed to find or create leaderboard.");
            return;
        }

        Debug.Log($"[Steamworks] Leaderboard found. Uploading score: {m_pendingScore}");
        SteamAPICall_t handle = SteamUserStats.UploadLeaderboardScore(
            pCallback.m_hSteamLeaderboard,
            ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
            m_pendingScore,
            null,
            0
        );

        if (m_uploadScoreCallResult != null)
        {
            m_uploadScoreCallResult.Set(handle);
        }
    }

    private static void OnLeaderboardScoreUploaded(LeaderboardScoreUploaded_t pCallback, bool bIOFailure)
    {
        if (bIOFailure || pCallback.m_bSuccess == 0)
        {
            Debug.LogWarning("[Steamworks] Failed to upload leaderboard score.");
            return;
        }

        Debug.Log($"[Steamworks] Score uploaded successfully! Score: {pCallback.m_nScore}, Changed: {pCallback.m_bScoreChanged != 0}, Rank: {pCallback.m_nGlobalRankNew}");
    }
#endif

    /// <summary>스팀 오버레이 활성화 여부 확인.</summary>
    public static bool IsOverlayActive()
    {
#if STEAMWORKS_ENABLED
        return IsInitialized && SteamUtils.IsSteamInBigPictureMode(); // 혹은 적절한 오버레이 체크
#else
        return false;
#endif
    }

#if STEAMWORKS_ENABLED
    private static GameObject _callbackHost;
#endif

    /// <summary>게임 시작 시 한 번 호출.</summary>
    public static void InitializePlaceholder()
    {
#if STEAMWORKS_ENABLED
        if (!ShouldTryInitSteam())
        {
            IsInitialized = false;
            return;
        }

        try
        {
            if (!Packsize.Test())
            {
                Debug.LogError("[Steamworks] Packsize.Test failed (native steam_api mismatch?).");
                IsInitialized = false;
                return;
            }

            if (!DllCheck.Test())
            {
                Debug.LogError("[Steamworks] DllCheck.Test failed (wrong steam_api binaries?).");
                IsInitialized = false;
                return;
            }

            // AppId_t.Invalid: 에디터/개발 시 프로젝트 루트의 steam_appid.txt 를 네이티브에서 읽습니다.
            if (SteamAPI.RestartAppIfNecessary(AppId_t.Invalid))
            {
                QuitForSteamRestart();
                return;
            }

            IsInitialized = SteamAPI.Init();
            if (!IsInitialized)
            {
                Debug.LogError(
                    "[Steamworks] SteamAPI.Init failed. Is Steam running? Is steam_appid.txt correct?");
                return;
            }

            EnsureCallbackHost();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            IsInitialized = false;
        }
#else
        IsInitialized = false;
#endif
    }

    /// <summary>게임 종료 시 정리.</summary>
    public static void ShutdownPlaceholder()
    {
#if STEAMWORKS_ENABLED
        if (IsInitialized)
        {
            SteamAPI.Shutdown();
        }

        if (_callbackHost != null)
        {
            Object.Destroy(_callbackHost);
            _callbackHost = null;
        }
#endif
        IsInitialized = false;
    }

#if STEAMWORKS_ENABLED
    /// <summary>로그인된 스팀 사용자 ID(64비트). 초기화 전이면 false.</summary>
    public static bool TryGetSteamId(out ulong steamId)
    {
        steamId = 0;
        if (!IsInitialized)
        {
            return false;
        }

        steamId = SteamUser.GetSteamID().m_SteamID;
        return steamId != 0;
    }

    /// <summary>현재 사용자 표시 이름(스팀 페르소나).</summary>
    public static bool TryGetPersonaName(out string name)
    {
        name = null;
        if (!IsInitialized)
        {
            return false;
        }

        name = SteamFriends.GetPersonaName();
        return !string.IsNullOrEmpty(name);
    }
#else
    public static bool TryGetSteamId(out ulong steamId)
    {
        steamId = 0;
        return false;
    }

    public static bool TryGetPersonaName(out string name)
    {
        name = null;
        return false;
    }
#endif

#if STEAMWORKS_ENABLED
    private static bool ShouldTryInitSteam()
    {
#if UNITY_EDITOR
        return true;
#elif UNITY_WEBGL || UNITY_ANDROID || UNITY_IOS || UNITY_TVOS
        return false;
#else
        return true;
#endif
    }

    private static void QuitForSteamRestart()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void EnsureCallbackHost()
    {
        if (_callbackHost != null)
        {
            return;
        }

        _callbackHost = new GameObject("SteamworksCallbacks");
        Object.DontDestroyOnLoad(_callbackHost);
        _callbackHost.hideFlags = HideFlags.HideAndDontSave;
        _callbackHost.AddComponent<SteamworksCallbacksBehaviour>();
    }

    private sealed class SteamworksCallbacksBehaviour : MonoBehaviour
    {
        private void Update()
        {
            if (SteamworksService.IsInitialized)
            {
                SteamAPI.RunCallbacks();
            }
        }
    }
#endif

    /// <summary>G.S.I에서 사용하는 Steam 업적(Achievement) ID 상수 정의</summary>
    public static class Achievements
    {
        public const string FirstExam = "ACH_FIRST_EXAM";
        public const string AllPracticeS = "ACH_ALL_PRACTICE_S_GRADE";
        public const string GoldenTicket = "ACH_GOLDEN_TICKET";
        public const string WealthyAgent = "ACH_WEALTHY_AGENT";
    }

    /// <summary>G.S.I에서 사용하는 Steam 리더보드(Leaderboard) 이름 상수 정의</summary>
    public static class Leaderboards
    {
        public const string UnifiedExamScore = "LB_UNIFIED_EXAM_SCORE";
    }
}

}
