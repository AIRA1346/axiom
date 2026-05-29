namespace ArchE.Platform
{
using System;
using System.Text;
using UnityEngine;
using ArchE.Game;

/// <summary>
/// Pushes a bounded snapshot to Steam Remote Storage and can merge a newer cloud copy into <see cref="GsiSaveSystem"/>.
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
    private const string SettingsMaster = "GSI_Settings_MasterVolume";
    private const string SettingsSfx = "GSI_Settings_SfxVolume";
    private const string SettingsMusic = "GSI_Settings_MusicVolume";
    private const string PreferredLocale = "RunicAtelier.PreferredLocale";
    private const string UiAppearanceMode = "GSI_UiAppearanceMode";
    private const string EquippedSkin = "GSI_Cosmetic_EquippedId";
    private const string OwnedSkinPrefix = "GSI_Cosmetic_Owned_";
    private const string SkinDefault = "skin_default";
    private const string SkinOcean = "skin_ocean";
    private const string SkinAmber = "skin_amber";
    private const string SkinViolet = "skin_violet";
    private const string PgLegacy = "GSI_PracticeGrade";

    private static string LocalBackupPath => System.IO.Path.Combine(Application.persistentDataPath, "gsi_local_backup.json");

    private static void SaveLocalBackup()
    {
        try
        {
            GsiSteamCloudSnapshot currentLocal = BuildSnapshotFromLocal();
            string json = JsonUtility.ToJson(currentLocal, true);
            System.IO.File.WriteAllText(LocalBackupPath, json, Encoding.UTF8);
            Debug.Log($"[SteamCloud] Local backup saved successfully at: {LocalBackupPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SteamCloud] Failed to write local backup: {e.Message}");
        }
    }

    private static bool TryRestoreFromBackup()
    {
        try
        {
            if (System.IO.File.Exists(LocalBackupPath))
            {
                string json = System.IO.File.ReadAllText(LocalBackupPath, Encoding.UTF8);
                GsiSteamCloudSnapshot backup = JsonUtility.FromJson<GsiSteamCloudSnapshot>(json);
                if (backup != null)
                {
                    ApplySnapshotToPlayerPrefs(backup);
                    GsiSaveSystem.Save();
                    Debug.Log("[SteamCloud] Successfully restored PlayerPrefs (via GsiSaveSystem) from local backup.");
                    return true;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SteamCloud] Failed to restore from backup: {e.Message}");
        }
        return false;
    }

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

        if (snap == null)
        {
            return;
        }

        // 포맷 버전 유연성 제공
        if (snap.FormatVersion > 1)
        {
            Debug.LogWarning($"[SteamCloud] Snapshot format version ({snap.FormatVersion}) is newer than expected. Parsing anyway, but some new properties might be ignored.");
        }
        else if (snap.FormatVersion < 1)
        {
            Debug.LogError($"[SteamCloud] Invalid format version: {snap.FormatVersion}");
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

        if (!long.TryParse(GsiSaveSystem.GetString(PrefsKeyLastMergedCloudUnix, "0"), out long lastSeen))
        {
            lastSeen = 0L;
        }

        if (snap.WrittenUnixUtc <= lastSeen)
        {
            return;
        }

        // 병합 작업 전 현재 로컬 상태 이중화 백업
        SaveLocalBackup();

        try
        {
            ApplySnapshotToPlayerPrefs(snap);
            GsiSaveSystem.SetString(PrefsKeyLastMergedCloudUnix, snap.WrittenUnixUtc.ToString());
            GsiSaveSystem.Save();
            Debug.Log("[SteamCloud] Merged newer snapshot from Steam Remote Storage.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SteamCloud] Failed to apply cloud snapshot. Attempting rollback: {ex.Message}");
            if (TryRestoreFromBackup())
            {
                Debug.LogWarning("[SteamCloud] Rollback successful. Saved data has been reverted to local backup state.");
            }
            else
            {
                Debug.LogError("[SteamCloud] Critical Error: Rollback failed. Saved data may be inconsistent.");
            }
        }
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
            GsiSaveSystem.SetString(PrefsKeyLastLocalPushUnix, now.ToString());
            GsiSaveSystem.SetString(PrefsKeyLastMergedCloudUnix, now.ToString());
            GsiSaveSystem.Save();
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
            BestReactionTime = GsiSaveSystem.GetFloat(PrefsBestReaction, 99.99f),
            BestAimTime = GsiSaveSystem.GetFloat(PrefsBestAim, 99.99f),
            BestMemorySpan = GsiSaveSystem.GetFloat(PrefsBestMem, -1f),
            BestRhythmAccuracy = GsiSaveSystem.GetFloat(PrefsBestRhythmAcc, -1f),
            BestRhythmMeanError = GsiSaveSystem.GetFloat(PrefsBestRhythmErr, 999f),
            BestMotAccuracy = GsiSaveSystem.GetFloat(PrefsBestMot, -1f),
            BestBulletHellTime = GsiSaveSystem.GetFloat(PrefsBestBh, -1f),
            BestCps = GsiSaveSystem.GetFloat(PrefsBestCps, -1f),
            BestUnifiedExamTotal = GsiSaveSystem.GetFloat(PrefsBestUnified, -1f),
            UnifiedExamHistoryText = GsiSaveSystem.GetString(PrefsUnifiedHistory, string.Empty) ?? string.Empty,
            UnifiedExamHistoryJson = GsiSaveSystem.GetString(PrefsUnifiedHistoryJson, string.Empty) ?? string.Empty,
            EconomyTokens = GsiSaveSystem.GetInt(PrefsEconTokens, 0),
            EconomyTickets = GsiSaveSystem.GetInt(PrefsEconTickets, 3),
            PgReaction = GsiSaveSystem.GetInt(PgReaction, 9),
            PgAim = GsiSaveSystem.GetInt(PgAim, 9),
            PgMemory = GsiSaveSystem.GetInt(PgMem, 9),
            PgRhythm = GsiSaveSystem.GetInt(PgRhythm, 9),
            PgMot = GsiSaveSystem.GetInt(PgMot, 9),
            PgBullet = GsiSaveSystem.GetInt(PgBh, 9),
            PgCps = GsiSaveSystem.GetInt(PgCps, 9),
            MasterVolume = GsiSaveSystem.GetFloat(SettingsMaster, 1f),
            SfxVolume = GsiSaveSystem.GetFloat(SettingsSfx, 1f),
            MusicVolume = GsiSaveSystem.GetFloat(SettingsMusic, 0.2f),
            PreferredLocaleCode = GsiSaveSystem.GetString(PreferredLocale, "en") ?? "en",
            UiAppearanceMode = GsiSaveSystem.GetInt(UiAppearanceMode, 0),
            EquippedSkinId = GsiSaveSystem.GetString(EquippedSkin, SkinDefault) ?? SkinDefault,
            UserPreferencesIncluded = 1,
            OwnsSkinOcean = GsiSaveSystem.GetInt(OwnedSkinPrefix + SkinOcean, 0),
            OwnsSkinAmber = GsiSaveSystem.GetInt(OwnedSkinPrefix + SkinAmber, 0),
            OwnsSkinViolet = GsiSaveSystem.GetInt(OwnedSkinPrefix + SkinViolet, 0)
        };
        return s;
    }

    private static void ApplySnapshotToPlayerPrefs(GsiSteamCloudSnapshot s)
    {
        GsiSaveSystem.SetFloat(PrefsBestReaction, s.BestReactionTime);
        GsiSaveSystem.SetFloat(PrefsBestAim, s.BestAimTime);
        GsiSaveSystem.SetFloat(PrefsBestMem, s.BestMemorySpan);
        GsiSaveSystem.SetFloat(PrefsBestRhythmAcc, s.BestRhythmAccuracy);
        GsiSaveSystem.SetFloat(PrefsBestRhythmErr, s.BestRhythmMeanError);
        GsiSaveSystem.SetFloat(PrefsBestMot, s.BestMotAccuracy);
        GsiSaveSystem.SetFloat(PrefsBestBh, s.BestBulletHellTime);
        GsiSaveSystem.SetFloat(PrefsBestCps, s.BestCps);
        GsiSaveSystem.SetFloat(PrefsBestUnified, s.BestUnifiedExamTotal);
        GsiSaveSystem.SetString(PrefsUnifiedHistory, s.UnifiedExamHistoryText ?? string.Empty);
        GsiSaveSystem.SetString(PrefsUnifiedHistoryJson, s.UnifiedExamHistoryJson ?? string.Empty);
        GsiSaveSystem.SetInt(PrefsEconTokens, s.EconomyTokens);
        GsiSaveSystem.SetInt(PrefsEconTickets, s.EconomyTickets);
        GsiSaveSystem.SetInt(PgReaction, Mathf.Clamp(s.PgReaction, 1, 9));
        GsiSaveSystem.SetInt(PgAim, Mathf.Clamp(s.PgAim, 1, 9));
        GsiSaveSystem.SetInt(PgMem, Mathf.Clamp(s.PgMemory, 1, 9));
        GsiSaveSystem.SetInt(PgRhythm, Mathf.Clamp(s.PgRhythm, 1, 9));
        GsiSaveSystem.SetInt(PgMot, Mathf.Clamp(s.PgMot, 1, 9));
        GsiSaveSystem.SetInt(PgBh, Mathf.Clamp(s.PgBullet, 1, 9));
        GsiSaveSystem.SetInt(PgCps, Mathf.Clamp(s.PgCps, 1, 9));
        GsiSaveSystem.SetInt(PgLegacy, Mathf.Clamp(s.PgReaction, 1, 9));
        if (s.UserPreferencesIncluded != 0)
        {
            GsiSaveSystem.SetFloat(SettingsMaster, Mathf.Clamp01(s.MasterVolume));
            GsiSaveSystem.SetFloat(SettingsSfx, Mathf.Clamp01(s.SfxVolume));
            GsiSaveSystem.SetFloat(SettingsMusic, Mathf.Clamp01(s.MusicVolume));
            GsiSaveSystem.SetString(PreferredLocale, string.IsNullOrWhiteSpace(s.PreferredLocaleCode) ? "en" : s.PreferredLocaleCode.Trim());
            GsiSaveSystem.SetInt(UiAppearanceMode, Mathf.Clamp(s.UiAppearanceMode, 0, 1));
            GsiSaveSystem.SetString(EquippedSkin, NormalizeSkinId(s.EquippedSkinId));
            GsiSaveSystem.SetInt(OwnedSkinPrefix + SkinOcean, s.OwnsSkinOcean != 0 ? 1 : 0);
            GsiSaveSystem.SetInt(OwnedSkinPrefix + SkinAmber, s.OwnsSkinAmber != 0 ? 1 : 0);
            GsiSaveSystem.SetInt(OwnedSkinPrefix + SkinViolet, s.OwnsSkinViolet != 0 ? 1 : 0);
        }
    }

    private static string NormalizeSkinId(string skinId)
    {
        switch (skinId)
        {
            case SkinOcean:
            case SkinAmber:
            case SkinViolet:
                return skinId;
            default:
                return SkinDefault;
        }
    }
}

}
