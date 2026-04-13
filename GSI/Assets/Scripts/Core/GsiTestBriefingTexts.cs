using UnityEngine;

/// <summary>
/// 시험 시작 전 브리핑 화면의 제목·본문(로컬라이즈 키 + 영어 폴백).
/// </summary>
public static class GsiTestBriefingTexts
{
    public static void GetBriefingTitleAndBody(out string title, out string body)
    {
        if (GameManager.Instance == null)
        {
            title = string.Empty;
            body = string.Empty;
            return;
        }

        GetBriefingTitleAndBody(GameManager.Instance.CurrentTestMode, out title, out body);

        if (GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam)
        {
            int seg = GameManager.Instance.UnifiedExamSegmentOrdinalDisplay;
            if (seg >= 1 && seg <= 6)
            {
                string line = GameLocalization.FormatUiString(UiStringKeys.BriefingUnifiedSegmentLineFmt,
                    "Unified exam — subject {0}/6", seg);
                body = body + "\n\n" + line;
            }
        }
    }

    public static void GetBriefingTitleAndBody(TestMode mode, out string title, out string body)
    {
        switch (mode)
        {
            case TestMode.Reaction:
                title = GameLocalization.GetUiString(UiStringKeys.BriefingTitleReaction, "Reaction");
                body = GameLocalization.GetUiString(UiStringKeys.BriefingBodyReaction,
                    "Wait for the stimulus. When it appears, respond as quickly as possible. False starts may count against you.");
                break;
            case TestMode.AimPrecision:
                title = GameLocalization.GetUiString(UiStringKeys.BriefingTitleAim, "Aim precision");
                body = GameLocalization.GetUiString(UiStringKeys.BriefingBodyAim,
                    "Targets will appear in the field. Tap each target as accurately and quickly as the task allows.");
                break;
            case TestMode.MemorySequence:
                title = GameLocalization.GetUiString(UiStringKeys.BriefingTitleMemory, "Memory sequence");
                body = GameLocalization.GetUiString(UiStringKeys.BriefingBodyMemory,
                    "Memorize the pattern or sequence shown, then reproduce it within the time limit.");
                break;
            case TestMode.RhythmTiming:
                title = GameLocalization.GetUiString(UiStringKeys.BriefingTitleRhythm, "Rhythm timing");
                body = GameLocalization.GetUiString(UiStringKeys.BriefingBodyRhythm,
                    "Follow the rhythm cues. Your timing error determines the outcome.");
                break;
            case TestMode.MultipleObjectTracking:
                title = GameLocalization.GetUiString(UiStringKeys.BriefingTitleMot, "Multiple object tracking (MOT)");
                body = GameLocalization.GetUiString(UiStringKeys.BriefingBodyMot,
                    "Track the marked objects as they move. Identify them when prompted.");
                break;
            case TestMode.BulletHell:
                title = GameLocalization.GetUiString(UiStringKeys.BriefingTitleBulletHell, "Bullet Hell");
                body = GameLocalization.GetUiString(UiStringKeys.BriefingBodyBulletHell,
                    "Avoid projectiles and survive as long as possible. Movement is constrained to the play area.");
                break;
            default:
                title = GameLocalization.GetUiString(UiStringKeys.BriefingTitleDefault, "Test");
                body = GameLocalization.GetUiString(UiStringKeys.BriefingBodyDefault,
                    "Tap anywhere to begin.");
                break;
        }
    }
}
