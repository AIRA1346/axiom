namespace ArchE.Platform
{
using System;
using UnityEngine;

#if STEAMWORKS_ENABLED
using Steamworks;
#endif

/// <summary>
/// Thin wrapper for Steam Cloud file API (ISteamRemoteStorage). No-op if Steam is not ready.
/// Partner site: enable Steam Cloud and set an appropriate per-user quota for your App ID.
/// </summary>
public static class SteamRemoteStorageGsi
{
    public const string CloudSnapshotFileName = "gsi_steamcloud_v1.json";
    public const int MaxFileBytes = 900_000;

    public static bool IsAvailable => SteamworksService.IsInitialized;

    public static bool FileExistsInCloud
    {
        get
        {
#if STEAMWORKS_ENABLED
            if (!IsAvailable)
            {
                return false;
            }

            return SteamRemoteStorage.FileExists(CloudSnapshotFileName);
#else
            return false;
#endif
        }
    }

    public static bool TryWriteFile(byte[] data)
    {
#if STEAMWORKS_ENABLED
        if (!IsAvailable || data == null || data.Length == 0 || data.Length > MaxFileBytes)
        {
            return false;
        }

        // 최소 무결성 검증: JSON 형식 여부 및 최소 크기 검사
        try
        {
            string rawStr = System.Text.Encoding.UTF8.GetString(data).Trim();
            if (rawStr.Length < 10 || !rawStr.StartsWith("{") || !rawStr.EndsWith("}"))
            {
                Debug.LogWarning("[SteamCloud] Write aborted: Data does not look like a valid JSON snapshot.");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SteamCloud] Write aborted: Error validating write data: {e.Message}");
            return false;
        }

        return SteamRemoteStorage.FileWrite(CloudSnapshotFileName, data, data.Length);
#else
        return false;
#endif
    }

    public static bool TryReadFile(out byte[] data)
    {
        data = null;
#if STEAMWORKS_ENABLED
        if (!IsAvailable)
        {
            return false;
        }

        if (!SteamRemoteStorage.FileExists(CloudSnapshotFileName))
        {
            return false;
        }

        int n = SteamRemoteStorage.GetFileSize(CloudSnapshotFileName);
        // 최소 10바이트 이상의 유효한 데이터여야 함
        if (n < 10 || n > MaxFileBytes)
        {
            Debug.LogWarning($"[SteamCloud] Read ignored: Invalid file size in cloud ({n} bytes).");
            return false;
        }

        var buffer = new byte[n];
        int read = SteamRemoteStorage.FileRead(CloudSnapshotFileName, buffer, n);
        if (read != n)
        {
            Debug.LogError("[SteamCloud] FileRead count mismatch.");
            return false;
        }

        // 로드된 데이터 최소 무결성(JSON 포맷) 검증
        try
        {
            string rawStr = System.Text.Encoding.UTF8.GetString(buffer).Trim();
            if (!rawStr.StartsWith("{") || !rawStr.EndsWith("}"))
            {
                Debug.LogWarning("[SteamCloud] Read aborted: Loaded data has invalid JSON format.");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SteamCloud] Read aborted: Error parsing loaded data: {e.Message}");
            return false;
        }

        data = buffer;
        return true;
#else
        return false;
#endif
    }
}

}
