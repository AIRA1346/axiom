using System;
using UnityEngine;

/// <summary>Serializable subset of G.S.I data for Steam Cloud round-trip. Not full PlayerPrefs.</summary>
[Serializable]
public sealed class GsiSteamCloudSnapshot
{
    public int FormatVersion = 1;
    public long WrittenUnixUtc;
    public string SteamIdOwner = string.Empty;

    public float BestReactionTime;
    public float BestAimTime;
    public float BestMemorySpan;
    public float BestRhythmAccuracy;
    public float BestRhythmMeanError;
    public float BestMotAccuracy;
    public float BestBulletHellTime;
    public float BestCps;
    public float BestUnifiedExamTotal;
    public string UnifiedExamHistoryText = string.Empty;
    public string UnifiedExamHistoryJson = string.Empty;

    public int EconomyTokens;
    public int EconomyTickets;

    public int PgReaction = 9;
    public int PgAim = 9;
    public int PgMemory = 9;
    public int PgRhythm = 9;
    public int PgMot = 9;
    public int PgBullet = 9;
    public int PgCps = 9;
}
