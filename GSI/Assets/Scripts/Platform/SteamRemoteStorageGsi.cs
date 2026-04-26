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
        if (n <= 0 || n > MaxFileBytes)
        {
            return false;
        }

        var buffer = new byte[n];
        int read = SteamRemoteStorage.FileRead(CloudSnapshotFileName, buffer, n);
        if (read != n)
        {
            return false;
        }

        data = buffer;
        return true;
#else
        return false;
#endif
    }
}
