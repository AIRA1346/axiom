#if STEAMWORKS_ENABLED
using Steamworks;
#endif

/// <summary>
/// Steam Friends / overlay status line (simple English for Steam client display).
/// Lives in ArchE.Game (with GameState/TestMode) to avoid a circular asmdef with ArchE.Platform.
/// </summary>
public static class GsiSteamRichPresence
{
    /// <summary>Updates status from the active game flow. No-op if Steam is off.</summary>
    public static void ApplyFrom(
        GameState state,
        TestMode testMode,
        TestType testType,
        int unifiedSegmentDisplay)
    {
#if STEAMWORKS_ENABLED
        if (!SteamworksService.IsInitialized)
        {
            return;
        }

        string line = BuildLine(state, testMode, testType, unifiedSegmentDisplay);
        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        SteamFriends.SetRichPresence("status", line);
#endif
    }

    /// <summary>Clears rich presence before shutdown. Safe to call if Steam is not up.</summary>
    public static void Clear()
    {
#if STEAMWORKS_ENABLED
        if (!SteamworksService.IsInitialized)
        {
            return;
        }

        SteamFriends.ClearRichPresence();
#endif
    }

    private static string BuildLine(
        GameState state,
        TestMode testMode,
        TestType testType,
        int unifiedSegmentDisplay)
    {
        bool unified = testType == TestType.UnifiedOfficialExam;

        switch (state)
        {
            case GameState.MainMenu:
                return "The Axiom — menu";

            case GameState.TestBriefing:
            case GameState.TestStandby:
            {
                string mode = TestModeToShortName(testMode);
                if (unified && unifiedSegmentDisplay > 0)
                {
                    return $"The Axiom — unified exam · seg {unifiedSegmentDisplay}/7 — {mode}";
                }

                if (testType == TestType.OfficialExam)
                {
                    return $"The Axiom — official exam — {mode}";
                }

                return $"The Axiom — practice — {mode}";
            }

            case GameState.TestInProgress:
            {
                string mode = TestModeToShortName(testMode);
                if (unified && unifiedSegmentDisplay > 0)
                {
                    return $"The Axiom — in test ({unifiedSegmentDisplay}/7) — {mode}";
                }

                return testType == TestType.OfficialExam
                    ? $"The Axiom — in official test — {mode}"
                    : $"The Axiom — in practice — {mode}";
            }

            case GameState.TestCompleted:
            case GameState.UnifiedExamInterstitial:
            {
                if (unified && unifiedSegmentDisplay > 0)
                {
                    return $"The Axiom — between segments ({unifiedSegmentDisplay}/7)";
                }

                return "The Axiom — between rounds";
            }

            case GameState.ResultScreen:
                return unified
                    ? "The Axiom — unified exam result"
                    : "The Axiom — result screen";

            default:
                return "The Axiom";
        }
    }

    private static string TestModeToShortName(TestMode mode)
    {
        return mode switch
        {
            TestMode.Reaction => "Reaction",
            TestMode.AimPrecision => "Aim",
            TestMode.MemorySequence => "Memory",
            TestMode.RhythmTiming => "Rhythm",
            TestMode.MultipleObjectTracking => "MOT",
            TestMode.BulletHell => "Bullet hell",
            TestMode.ClicksPerSecond => "CPS",
            _ => "Assessment"
        };
    }
}
