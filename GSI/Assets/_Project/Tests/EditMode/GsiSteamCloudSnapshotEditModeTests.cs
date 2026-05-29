using ArchE.Platform;
using NUnit.Framework;
using UnityEngine;
using ArchE.Game;

public sealed class GsiSteamCloudSnapshotEditModeTests
{
    [SetUp]
    public void ClearPrefs()
    {
        GsiSaveSystem.DeleteAll();
    }

    [TearDown]
    public void CleanupPrefs()
    {
        GsiSaveSystem.DeleteAll();
    }

    [Test]
    public void BuildSnapshot_IncludesReleaseCriticalUserPreferences()
    {
        GsiSaveSystem.SetFloat("GSI_Settings_MasterVolume", 0.42f);
        GsiSaveSystem.SetFloat("GSI_Settings_SfxVolume", 0.63f);
        GsiSaveSystem.SetFloat("GSI_Settings_MusicVolume", 0.21f);
        GsiSaveSystem.SetString("RunicAtelier.PreferredLocale", "ko-KR");
        GsiSaveSystem.SetInt("GSI_UiAppearanceMode", 1);
        GsiSaveSystem.SetString("GSI_Cosmetic_EquippedId", "skin_violet");
        GsiSaveSystem.SetInt("GSI_Cosmetic_Owned_skin_ocean", 1);
        GsiSaveSystem.SetInt("GSI_Cosmetic_Owned_skin_amber", 0);
        GsiSaveSystem.SetInt("GSI_Cosmetic_Owned_skin_violet", 1);

        GsiSteamCloudSnapshot snap = GsiSteamCloudSync.BuildSnapshotFromLocal();

        Assert.AreEqual(0.42f, snap.MasterVolume, 0.001f);
        Assert.AreEqual(0.63f, snap.SfxVolume, 0.001f);
        Assert.AreEqual(0.21f, snap.MusicVolume, 0.001f);
        Assert.AreEqual("ko-KR", snap.PreferredLocaleCode);
        Assert.AreEqual(1, snap.UiAppearanceMode);
        Assert.AreEqual("skin_violet", snap.EquippedSkinId);
        Assert.AreEqual(1, snap.UserPreferencesIncluded);
        Assert.AreEqual(1, snap.OwnsSkinOcean);
        Assert.AreEqual(0, snap.OwnsSkinAmber);
        Assert.AreEqual(1, snap.OwnsSkinViolet);
    }

    [Test]
    public void BuildSnapshot_IncludesCoreProgressAndEconomy()
    {
        GsiSaveSystem.SetFloat("BestReactionTime", 0.18f);
        GsiSaveSystem.SetFloat("BestAimTime", 1.25f);
        GsiSaveSystem.SetFloat("BestMemorySpan", 9f);
        GsiSaveSystem.SetFloat("BestUnifiedExamTotal", 512f);
        GsiSaveSystem.SetString("UnifiedExamHistory", "history-line");
        GsiSaveSystem.SetString("GSI_UnifiedExamHistoryJson_v1", "{\"Items\":[]}");
        GsiSaveSystem.SetInt("GSI_Tokens_v2", 1234);
        GsiSaveSystem.SetInt("GSI_Tickets_v2", 7);
        GsiSaveSystem.SetInt("GSI_PracticeGrade_Reaction", 2);
        GsiSaveSystem.SetInt("GSI_PracticeGrade_Cps", 4);

        GsiSteamCloudSnapshot snap = GsiSteamCloudSync.BuildSnapshotFromLocal();

        Assert.AreEqual(0.18f, snap.BestReactionTime, 0.001f);
        Assert.AreEqual(1.25f, snap.BestAimTime, 0.001f);
        Assert.AreEqual(9f, snap.BestMemorySpan, 0.001f);
        Assert.AreEqual(512f, snap.BestUnifiedExamTotal, 0.001f);
        Assert.AreEqual("history-line", snap.UnifiedExamHistoryText);
        Assert.AreEqual("{\"Items\":[]}", snap.UnifiedExamHistoryJson);
        Assert.AreEqual(1234, snap.EconomyTokens);
        Assert.AreEqual(7, snap.EconomyTickets);
        Assert.AreEqual(2, snap.PgReaction);
        Assert.AreEqual(4, snap.PgCps);
    }

    [Test]
    public void NoticeLocalizationKeys_AreStable()
    {
        Assert.AreEqual("notice.not_enough_stardust", UiStringKeys.NoticeNotEnoughStardust);
        Assert.AreEqual("notice.not_enough_tickets", UiStringKeys.NoticeNotEnoughTickets);
        Assert.AreEqual("notice.service_unavailable", UiStringKeys.NoticeServiceUnavailable);
        Assert.AreEqual("notice.skin_locked", UiStringKeys.NoticeSkinLocked);
    }
}

