using System;
using System.Text;
using UnityEngine;

/// <summary>
/// Pushes a bounded snapshot to Steam Remote Storage and can merge a newer cloud copy into <see cref="PlayerPrefs"/>.
/// Does not stop local tampering: use with partner-side rules + (for fair async PvP) a trusted backend.
/// </summary>
public static class GsiSteamCloudSync
{
    public const string PrefsKeyLastMergedCloudUnix = "GSI_SteamCloud_LastMergedWrittenUnixV2";
    public const string PrefsKeyLastLocalPushUnix = "GSI_SteamCloud_LastLocalPushUnixV2";

    private const string PrefsUnifiedHistory = "UnifiedExamHistory";
    private const string PrefsUnifiedHistoryJson = "GSI_UnifiedExamHistoryJson_v1";
    private const string PrefsBestReaction = "BestReactionTime";
    private const string PrefsBestAim = "BestAimTime";
    private const string PrefsBestMem = "BestMemorySpan";
    private const string PrefsBestRhythmAcc = "BestRhythmAccuracy";
    private const string PrefsBestRhythmErr = "BestRhythmMeanError";
    private const string PrefsBestMot = "BestMotAccuracy";
    private const string PrefsBestBh = "BestBulletHellTime";
    private const string PrefsBestCps = "BestCpsAverage";
    private const string PrefsBestUnified = "BestUnifiedExamTotal";
    private const string PrefsEconTokens = "GSI_Tokens_v2";
    private const string PrefsEconTickets = "GSI_Tickets_v2";
    private const string PgReaction = "GSI_PracticeGrade_Reaction";
    private const string PgAim = "GSI_PracticeGrade_Aim";
    private const string PgMem = "GSI_PracticeGrade_Memory";
    private const string PgRhythm = "GSI_PracticeGrade_Rhythm";
    private const string PgMot = "GSI_PracticeGrade_MOT";
    private const string PgBh = "GSI_PracticeGrade_BulletHell";
    private const string PgCps = "GSI_PracticeGrade_Cps";
    private const string PgLegacy = "GSI_PracticeGrade";

    public static void TryApplyCloudIfNewer()
    {
        if (!SteamRemoteStorageGsi.IsAvailable)
        {
            return;
        }

        if (!SteamRemoteStorageGsi.FileExistsInCloud)
        {
            return;
        }

        if (!SteamRemoteStorageGsi.TryReadFile(out byte[] raw) || raw == null || raw.Length == 0)
        {
            return;
        }

        string text = Encoding.UTF8.GetString(raw);
        GsiSteamCloudSnapshot snap;
        try
        {
            snap = JsonUtility.FromJson<GsiSteamCloudSnapshot>(text);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SteamCloud] Snapshot parse failed: " + e.Message);
            return;
        }

        if (snap == null || snap.FormatVersion != 1)
        {
            return;
        }

        if (SteamworksService.TryGetSteamId(out ulong sid) && snap.SteamIdOwner.Length > 0)
        {
            if (ulong.TryParse(snap.SteamIdOwner, out ulong owner) && owner != 0UL && owner != sid)
            {
                Debug.LogWarning("[SteamCloud] Ignoring cloud save: Steam account mismatch.");
                return;
            }
        }

        if (!long.TryParse(PlayerPrefs.GetString(PrefsKeyLastMergedCloudUnix, "0"), out long lastSeen))
        {
            lastSeen = 0L;
        }

        if (snap.WrittenUnixUtc <= lastSeen)
        {
            return;
        }

        ApplySnapshotToPlayerPrefs(snap);
        PlayerPrefs.SetString(PrefsKeyLastMergedCloudUnix, snap.WrittenUnixUtc.ToString());
        PlayerPrefs.Save();
        Debug.Log("[SteamCloud] Merged newer snapshot from Steam Remote Storage.");
    }

    public static void TryPushLocalToCloud()
    {
        if (!SteamRemoteStorageGsi.IsAvailable)
        {
            return;
        }

        GsiSteamCloudSnapshot snap = BuildSnapshotFromLocal();
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        snap.WrittenUnixUtc = now;
        if (SteamworksService.TryGetSteamId(out ulong sid))
        {
            snap.SteamIdOwner = sid.ToString();
        }

        string json = JsonUtility.ToJson(snap);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        if (bytes.Length > SteamRemoteStorageGsi.MaxFileBytes)
        {
            Debug.LogWarning("[SteamCloud] Snapshot too large; not uploading.");
            return;
        }

        if (SteamRemoteStorageGsi.TryWriteFile(bytes))
        {
            PlayerPrefs.SetString(PrefsKeyLastLocalPushUnix, now.ToString());
            PlayerPrefs.SetString(PrefsKeyLastMergedCloudUnix, now.ToString());
            PlayerPrefs.Save();
        }
        else
        {
            Debug.LogWarning("[SteamCloud] FileWrite failed (quota, offline, or API error).");
        }
    }

    public static GsiSteamCloudSnapshot BuildSnapshotFromLocal()
    {
        var s = new GsiSteamCloudSnapshot
        {
            BestReactionTime = PlayerPrefs.GetFloat(PrefsBestReaction, 99.99f),
            BestAimTime = PlayerPrefs.GetFloat(PrefsBestAim, 99.99f),
            BestMemorySpan = PlayerPrefs.GetFloat(PrefsBestMem, -1f),
            BestRhythmAccuracy = PlayerPrefs.GetFloat(PrefsBestRhythmAcc, -1f),
            BestRhythmMeanError = PlayerPrefs.GetFloat(PrefsBestRhythmErr, 999f),
            BestMotAccuracy = PlayerPrefs.GetFloat(PrefsBestMot, -1f),
            BestBulletHellTime = PlayerPrefs.GetFloat(PrefsBestBh, -1f),
            BestCps = PlayerPrefs.GetFloat(PrefsBestCps, -1f),
            BestUnifiedExamTotal = PlayerPrefs.GetFloat(PrefsBestUnified, -1f),
            UnifiedExamHistoryText = PlayerPrefs.GetString(PrefsUnifiedHistory, string.Empty) ?? string.Empty,
            UnifiedExamHistoryJson = PlayerPrefs.GetString(PrefsUnifiedHistoryJson, string.Empty) ?? string.Empty,
            EconomyTokens = PlayerPrefs.GetInt(PrefsEconTokens, 0),
            EconomyTickets = PlayerPrefs.GetInt(PrefsEconTickets, 3),
            PgReaction = PlayerPrefs.GetInt(PgReaction, 9),
            PgAim = PlayerPrefs.GetInt(PgAim, 9),
            PgMemory = PlayerPrefs.GetInt(PgMem, 9),
            PgRhythm = PlayerPrefs.GetInt(PgRhythm, 9),
            PgMot = PlayerPrefs.GetInt(PgMot, 9),
            PgBullet = PlayerPrefs.GetInt(PgBh, 9),
            PgCps = PlayerPrefs.GetInt(PgCps, 9)
        };
        return s;
    }

    private static void ApplySnapshotToPlayerPrefs(GsiSteamCloudSnapshot s)
    {
        PlayerPrefs.SetFloat(PrefsBestReaction, s.BestReactionTime);
        PlayerPrefs.SetFloat(PrefsBestAim, s.BestAimTime);
        PlayerPrefs.SetFloat(PrefsBestMem, s.BestMemorySpan);
        PlayerPrefs.SetFloat(PrefsBestRhythmAcc, s.BestRhythmAccuracy);
        PlayerPrefs.SetFloat(PrefsBestRhythmErr, s.BestRhythmMeanError);
        PlayerPrefs.SetFloat(PrefsBestMot, s.BestMotAccuracy);
        PlayerPrefs.SetFloat(PrefsBestBh, s.BestBulletHellTime);
        PlayerPrefs.SetFloat(PrefsBestCps, s.BestCps);
        PlayerPrefs.SetFloat(PrefsBestUnified, s.BestUnifiedExamTotal);
        PlayerPrefs.SetString(PrefsUnifiedHistory, s.UnifiedExamHistoryText ?? string.Empty);
        PlayerPrefs.SetString(PrefsUnifiedHistoryJson, s.UnifiedExamHistoryJson ?? string.Empty);
        PlayerPrefs.SetInt(PrefsEconTokens, s.EconomyTokens);
        PlayerPrefs.SetInt(PrefsEconTickets, s.EconomyTickets);
        PlayerPrefs.SetInt(PgReaction, Mathf.Clamp(s.PgReaction, 1, 9));
        PlayerPrefs.SetInt(PgAim, Mathf.Clamp(s.PgAim, 1, 9));
        PlayerPrefs.SetInt(PgMem, Mathf.Clamp(s.PgMemory, 1, 9));
        PlayerPrefs.SetInt(PgRhythm, Mathf.Clamp(s.PgRhythm, 1, 9));
        PlayerPrefs.SetInt(PgMot, Mathf.Clamp(s.PgMot, 1, 9));
        PlayerPrefs.SetInt(PgBh, Mathf.Clamp(s.PgBullet, 1, 9));
        PlayerPrefs.SetInt(PgCps, Mathf.Clamp(s.PgCps, 1, 9));
        PlayerPrefs.SetInt(PgLegacy, Mathf.Clamp(s.PgReaction, 1, 9));
    }
}
