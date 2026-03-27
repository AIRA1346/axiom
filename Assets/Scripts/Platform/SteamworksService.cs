using UnityEngine;

/// <summary>
/// Steamworks API 연동 예약 자리. STEAMWORKS_ENABLED 정의 후 실제 SDK 초기화 코드를 채웁니다.
/// </summary>
public static class SteamworksService
{
    /// <summary>SteamAPI_Init 성공 여부(플레이스홀더 시 항상 false).</summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>게임 시작 시 한 번 호출. SDK 패키지 도입 전까지는 무해한 no-op.</summary>
    public static void InitializePlaceholder()
    {
#if STEAMWORKS_ENABLED
        // TODO: Steamworks.NET 또는 Facepunch.Steamworks 초기화
        // 예: if (!SteamAPI.Init()) { Debug.LogError("Steam 초기화 실패"); return; }
        // IsInitialized = true;
#else
        IsInitialized = false;
#endif
    }

    /// <summary>게임 종료 시 정리. SDK 연동 시 SteamAPI.Shutdown 등 호출.</summary>
    public static void ShutdownPlaceholder()
    {
#if STEAMWORKS_ENABLED
        // TODO: SteamAPI.Shutdown();
#endif
        IsInitialized = false;
    }
}
