using System.Collections.Generic;

/// <summary>
/// G.S.I 허브 로비에 노출되는 연습(미니게임) 목록. 새 과목 추가 시 여기 한 곳만 늘리면
/// 버튼 배치·급수 행·라벨 키가 함께 반영됩니다. (통합 시험 포함 순서는 <see cref="GameManager"/> 와 맞출 것.)
/// </summary>
public static class GsiHubPracticeCatalog
{
    public readonly struct Entry
    {
        public TestMode Mode { get; }
        /// <summary>씬 또는 런타임 생성 버튼의 이름 (<c>PracticeReaction</c> 등).</summary>
        public string PracticeTransformName { get; }
        /// <summary>예전 단독 시험 버튼 제거용 (<c>ExamReaction</c> 등).</summary>
        public string LegacyExamTransformName { get; }
        /// <summary>행 GameObject 이름 (<c>PracticeRow_Reaction</c> 등).</summary>
        public string RowObjectName { get; }
        public string LocalizationKey { get; }
        public string EnglishFallback { get; }

        public Entry(
            TestMode mode,
            string practiceTransformName,
            string legacyExamTransformName,
            string rowObjectName,
            string localizationKey,
            string englishFallback)
        {
            Mode = mode;
            PracticeTransformName = practiceTransformName;
            LegacyExamTransformName = legacyExamTransformName;
            RowObjectName = rowObjectName;
            LocalizationKey = localizationKey;
            EnglishFallback = englishFallback;
        }
    }

    private static readonly Entry[] EntriesOrdered =
    {
        new Entry(
            TestMode.Reaction,
            "PracticeReaction",
            "ExamReaction",
            "PracticeRow_Reaction",
            UiStringKeys.HubPracticeReaction,
            "Practice: Reaction speed"),
        new Entry(
            TestMode.AimPrecision,
            "PracticeAim",
            "ExamAim",
            "PracticeRow_Aim",
            UiStringKeys.HubPracticeAim,
            "Practice: Aim"),
        new Entry(
            TestMode.MemorySequence,
            "PracticeMemory",
            "ExamMemory",
            "PracticeRow_Memory",
            UiStringKeys.HubPracticeMemory,
            "Practice: Memory"),
        new Entry(
            TestMode.RhythmTiming,
            "PracticeRhythm",
            "ExamRhythm",
            "PracticeRow_Rhythm",
            UiStringKeys.HubPracticeRhythm,
            "Practice: Rhythm"),
        new Entry(
            TestMode.MultipleObjectTracking,
            "PracticeMot",
            "ExamMot",
            "PracticeRow_Mot",
            UiStringKeys.HubPracticeMot,
            "Practice: Multiple object tracking"),
        new Entry(
            TestMode.BulletHell,
            "PracticeBulletHell",
            "ExamBulletHell",
            "PracticeRow_BulletHell",
            UiStringKeys.HubPracticeBulletHell,
            "Practice: Bullet Hell"),
    };

    public static IReadOnlyList<Entry> All => EntriesOrdered;

    public static int Count => EntriesOrdered.Length;

    public static TestMode[] ModesOrdered()
    {
        var a = new TestMode[EntriesOrdered.Length];
        for (int i = 0; i < EntriesOrdered.Length; i++)
        {
            a[i] = EntriesOrdered[i].Mode;
        }

        return a;
    }
}
