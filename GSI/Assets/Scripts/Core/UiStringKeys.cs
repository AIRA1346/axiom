/// <summary>
/// Keys for the Unity Localization string table <c>UI_Strings</c> (default locale: English).
/// </summary>
public static class UiStringKeys
{
    public const string UiLobbyStart = "ui.lobby.start";
    public const string UiLobbyShop = "ui.lobby.shop";
    public const string UiLobbyInventory = "ui.lobby.inventory";

    /// <summary>메인 로비 — 시험 기록(진실의 제단) 진입.</summary>
    public const string UiLobbyAltarOfVerity = "ui.lobby.altar_of_verity";

    /// <summary>시험 기록 화면 제목(영: Altar of Verity).</summary>
    public const string VerityAltarScreenTitle = "verity.screen.title";

    public const string VerityAltarEmpty = "verity.empty";

    public const string VerityAltarSelectHint = "verity.select_hint";

    public const string VerityAltarListCaption = "verity.list_caption";

    public const string VerityAltarDetailCaption = "verity.detail_caption";

    public const string IntroBootInitializing = "intro.boot.initializing";
    public const string IntroStatusConnecting = "intro.status.connecting";
    public const string IntroStatusStandby = "intro.status.standby";
    public const string IntroStatusAccessGranted = "intro.status.access_granted";
    public const string IntroLogoTitle = "intro.logo.title";
    public const string IntroTagline = "intro.tagline";

    public const string SettingsTitle = "settings.title";
    public const string SettingsMasterVolume = "settings.master_volume";
    public const string SettingsSfx = "settings.sfx";
    public const string SettingsMusic = "settings.music";
    public const string SettingsClose = "settings.close";
    public const string SettingsLanguage = "settings.language";
    public const string SettingsOpen = "settings.open";
    public const string SettingsAppearance = "settings.appearance";
    public const string SettingsDark = "settings.dark";
    public const string SettingsLight = "settings.light";

    /// <summary>설정 패널 언어 칩: 영어 UI로 전환(라벨은 로케일별 표기).</summary>
    public const string SettingsLangEnglish = "settings.lang.english";
    /// <summary>설정 패널 언어 칩: 한국어 UI로 전환.</summary>
    public const string SettingsLangKorean = "settings.lang.korean";

    public const string HubPracticeMemory = "hub.practice.memory";
    public const string HubPracticeReaction = "hub.practice.reaction";
    public const string HubPracticeAim = "hub.practice.aim";
    public const string HubPracticeRhythm = "hub.practice.rhythm";
    public const string HubPracticeMot = "hub.practice.mot";
    public const string HubPracticeBulletHell = "hub.practice.bullethell";
    public const string HubPracticeGradeLegend = "hub.practice.grade_legend";

    /// <summary>G.S.I 허브 — 연습 블록 섹션 제목.</summary>
    public const string HubSectionPractice = "hub.section.practice";

    public const string HubUnifiedTitle = "hub.unified.title";
    public const string HubUnifiedHint = "hub.unified.hint";
    public const string HubUnifiedStart = "hub.unified.start";

    /// <summary>G.S.I 허브에서 통합 시험 응시 기록 오버레이를 엽니다.</summary>
    public const string HubUnifiedHistoryBtn = "hub.unified.history_btn";

    /// <summary>G.S.I 허브 상단 화면 제목(G.S.I = Game Skill Index).</summary>
    public const string HubScreenTitle = "hub.screen_title";

    public const string HubCurrencyGoldFmt = "hub.currency.gold_fmt";
    public const string HubCurrencyTicketFmt = "hub.currency.ticket_fmt";

    public const string LobbyCurrencyGoldFmt = "lobby.currency.gold_fmt";
    public const string LobbyCurrencyTicketFmt = "lobby.currency.ticket_fmt";

    /// <summary>메인 로비(간이 G.S.I 패널) 반응·조준 베스트 한 줄. {0} {1} = 포맷된 시간 문자열.</summary>
    public const string LobbyBestRecordsReactionAimFmt = "lobby.best_records.reaction_aim_fmt";

    /// <summary>간이 베스트 기록이 없을 때 표시(한 덩어리).</summary>
    public const string LobbyBestRecordTimeMissing = "lobby.best_record.time_missing";

    /// <summary>간이 베스트 기록 초 표기. <c>{0:F3}</c> 형식 지정자 사용.</summary>
    public const string LobbyBestRecordSecondsFmt = "lobby.best_record.seconds_fmt";

    /// <summary>메인 로비 상단 브랜딩 한 줄.</summary>
    public const string LobbyBrandingTitle = "lobby.branding_title";

    public const string GsiInterstitialTitle = "gsi.interstitial.title";
    public const string GsiInterstitialContinue = "gsi.interstitial.continue";
    public const string GsiInterstitialBodyFmt = "gsi.interstitial.body_fmt";

    public const string HistoryUnifiedTitle = "history.unified.title";
    public const string HistoryUnifiedClose = "history.unified.close";
    public const string HistoryUnifiedEmpty = "history.unified.empty";
    /// <summary>{0} 시각, {1} 급수, {2} 총점, {3} 합격/불합격 문구, {4} 티어, {5} 컷 총점.</summary>
    public const string HistoryUnifiedRowFmt = "history.unified.row_fmt";

    public const string BriefingTapAnywhere = "briefing.tap_anywhere";
    public const string BriefingTitleDefault = "briefing.title.default";
    public const string BriefingBodyDefault = "briefing.body.default";
    public const string BriefingUnifiedSegmentLineFmt = "briefing.unified.segment_line_fmt";

    public const string BriefingTitleReaction = "briefing.title.reaction";
    public const string BriefingBodyReaction = "briefing.body.reaction";
    public const string BriefingTitleAim = "briefing.title.aim";
    public const string BriefingBodyAim = "briefing.body.aim";
    public const string BriefingTitleMemory = "briefing.title.memory";
    public const string BriefingBodyMemory = "briefing.body.memory";
    public const string BriefingTitleRhythm = "briefing.title.rhythm";
    public const string BriefingBodyRhythm = "briefing.body.rhythm";
    public const string BriefingTitleMot = "briefing.title.mot";
    public const string BriefingBodyMot = "briefing.body.mot";
    public const string BriefingTitleBulletHell = "briefing.title.bullethell";
    public const string BriefingBodyBulletHell = "briefing.body.bullethell";

    public const string GsiLobbyBestRecordsFmt = "gsi.lobby.best_records_fmt";

    public const string ModeReaction = "mode.reaction";
    public const string ModeAim = "mode.aim";
    public const string ModeMemory = "mode.memory";
    public const string ModeRhythm = "mode.rhythm";
    public const string ModeMot = "mode.mot";
    public const string ModeBulletHell = "mode.bullethell";

    public const string CommonPass = "common.pass";
    public const string CommonFail = "common.fail";
    public const string CommonSegmentFailed = "common.segment_failed";

    public const string ModeShortReaction = "mode.short.reaction";
    public const string ModeShortAim = "mode.short.aim";
    public const string ModeShortMemory = "mode.short.memory";
    public const string ModeShortRhythm = "mode.short.rhythm";
    public const string ModeShortMot = "mode.short.mot";
    public const string ModeShortBulletHell = "mode.short.bullethell";

    public const string ResultTitleUnified = "result.title.unified";

    public const string ResultTitlePracticeReaction = "result.title.practice.reaction";
    public const string ResultTitlePracticeAim = "result.title.practice.aim";
    public const string ResultTitlePracticeMemory = "result.title.practice.memory";
    public const string ResultTitlePracticeRhythm = "result.title.practice.rhythm";
    public const string ResultTitlePracticeMot = "result.title.practice.mot";
    public const string ResultTitlePracticeBulletHell = "result.title.practice.bullethell";

    public const string ResultTitleExamReaction = "result.title.exam.reaction";
    public const string ResultTitleExamAim = "result.title.exam.aim";
    public const string ResultTitleExamMemory = "result.title.exam.memory";
    public const string ResultTitleExamRhythm = "result.title.exam.rhythm";
    public const string ResultTitleExamMot = "result.title.exam.mot";
    public const string ResultTitleExamBulletHell = "result.title.exam.bullethell";

    public const string ResultUnifiedLine1Fmt = "result.unified.line1_fmt";
    public const string ResultUnifiedLine2Fmt = "result.unified.line2_fmt";
    public const string ResultUnifiedFinalPass = "result.unified.final_pass";
    public const string ResultUnifiedFinalFail = "result.unified.final_fail";
    public const string ResultUnifiedSubjectsHeader = "result.unified.subjects_header";
    public const string ResultUnifiedRewardOkFmt = "result.unified.reward_ok_fmt";
    public const string ResultUnifiedRewardFail = "result.unified.reward_fail";

    /// <summary>통합 시험 결과 상단에 표시하는 종료 시각. Placeholder {0} = yyyy-MM-dd HH:mm:ss.</summary>
    public const string ResultUnifiedTakenAtFmt = "result.unified.taken_at_fmt";

    /// <summary>통합 시험 과목별 요약 한 줄(결과 화면·로그). Placeholders는 <see cref="UnifiedExamScoring.Evaluate"/> 와 동일 순서.</summary>
    public const string UnifiedSegmentSummaryHardFmt = "unified.segment.summary_hard_fmt";
    public const string UnifiedSegmentSummaryReactionFmt = "unified.segment.summary_reaction_fmt";
    public const string UnifiedSegmentSummaryAimFmt = "unified.segment.summary_aim_fmt";
    public const string UnifiedSegmentSummaryMemoryFmt = "unified.segment.summary_memory_fmt";
    public const string UnifiedSegmentSummaryRhythmFmt = "unified.segment.summary_rhythm_fmt";
    public const string UnifiedSegmentSummaryMotFmt = "unified.segment.summary_mot_fmt";
    public const string UnifiedSegmentSummaryBulletFmt = "unified.segment.summary_bullet_fmt";
    public const string UnifiedSegmentSummaryDefaultFmt = "unified.segment.summary_default_fmt";

    public const string ResultFailReactionFmt = "result.fail.reaction_fmt";
    public const string ResultFailAimFmt = "result.fail.aim_fmt";
    public const string ResultRewardZeroGold = "result.reward.zero_gold";

    public const string ResultBodyFailed = "result.body.failed";
    public const string ResultFailFalseStart = "result.fail.false_start";
    public const string ResultRewardPlusGoldFmt = "result.reward.plus_gold_fmt";
    public const string ResultTierLabelFmt = "result.tier.label_fmt";

    /// <summary>단독 공식 기억 시험 결과 한 줄(1회 시도 최대 연속 길이).</summary>
    public const string ResultMemoryExamAvgSpanFmt = "result.memory.exam_avg_span_fmt";
    public const string ResultMemoryPracticeBestSpanFmt = "result.memory.practice_best_span_fmt";
    public const string ResultRhythmStatsFmt = "result.rhythm.stats_fmt";
    public const string ResultMotExamAvgFmt = "result.mot.exam_avg_fmt";
    public const string ResultMotPracticeOverallFmt = "result.mot.practice_overall_fmt";
    public const string ResultBhExamSumG1Fmt = "result.bh.exam_sum_g1_fmt";
    public const string ResultBhPracticeG1Fmt = "result.bh.practice_g1_fmt";
    public const string ResultBhExamSumFmt = "result.bh.exam_sum_fmt";
    public const string ResultBhPracticeSurvivalFmt = "result.bh.practice_survival_fmt";
    public const string ResultAimPassBodyFmt = "result.aim.pass_body_fmt";
    public const string ResultReactionPassBodyFmt = "result.reaction.pass_body_fmt";
    public const string ResultGenericTimeTierFmt = "result.generic.time_tier_fmt";

    public const string ResultActionRetry = "result.action.retry";
    public const string ResultActionMainMenu = "result.action.main_menu";

    /// <summary>결과 화면 상단 신기록 배지 한 줄.</summary>
    public const string ResultBadgeNewRecord = "result.badge.new_record";

    public const string PracticeTierTop = "practice.tier.top";
    public const string PracticeTierHigh = "practice.tier.high";
    public const string PracticeTierMid = "practice.tier.mid";
    public const string PracticeTierLow = "practice.tier.low";
    public const string PracticeTierEntry = "practice.tier.entry";

    public const string MotHudMemorize = "mot.hud.memorize";
    public const string MotHudTracking = "mot.hud.tracking";
    public const string MotHudSelectFmt = "mot.hud.select_fmt";
    public const string MotHudStatusFmt = "mot.hud.status_fmt";
    public const string MotSubmit = "mot.submit";

    public const string RhythmHudReady = "rhythm.hud.ready";
    public const string RhythmHudGo = "rhythm.hud.go";
    public const string RhythmJudgeMiss = "rhythm.judge.miss";
    public const string RhythmHudRecordingFmt = "rhythm.hud.recording_fmt";
    public const string RhythmJudgePerfect = "rhythm.judge.perfect";
    public const string RhythmJudgeGreat = "rhythm.judge.great";
    public const string RhythmJudgeGood = "rhythm.judge.good";

    /// <summary>하단 키 안내(4레인). 물리 키 레이블은 고정.</summary>
    public const string RhythmKeysFourLanes = "rhythm.keys.four_lanes";
    public const string RhythmKeysThreeLanes = "rhythm.keys.three_lanes";
    public const string RhythmKeysTwoLanes = "rhythm.keys.two_lanes";

    public const string MemoryRecallHint = "memory.recall_hint";

    /// <summary>탄막 1급(최대 생존) 라운드 시작 한 줄. {0}=생명, {1}=무적 시간(문자열).</summary>
    public const string BhHudRoundIntroSurvivalFmt = "bh.hud.round_intro_survival_fmt";
    /// <summary>목표 생존 라운드 시작. {0}=목표 초, {1}=생명, {2}=무적 시간(문자열).</summary>
    public const string BhHudRoundIntroTargetFmt = "bh.hud.round_intro_target_fmt";
    /// <summary>진행 중 무적 표시. {0}=남은 초.</summary>
    public const string BhHudInvincibilityFmt = "bh.hud.invincibility_fmt";
    public const string BhHudLiveSurvivalFmt = "bh.hud.live_survival_fmt";
    public const string BhHudLiveTargetFmt = "bh.hud.live_target_fmt";

    public const string ShopTitle = "shop.title";
    public const string ShopBack = "shop.back";
    public const string ShopGoldFmt = "shop.gold_fmt";
    public const string ShopPriceGoldFmt = "shop.price_gold_fmt";
    public const string ShopTicketsFmt = "shop.tickets_fmt";
    public const string ShopBuy = "shop.buy";
    public const string ShopOwned = "shop.owned";
    public const string ShopEquip = "shop.equip";
    public const string ShopEquipped = "shop.equipped";
    public const string ShopNeedGold = "shop.need_gold";
    public const string ShopOfferTickets1 = "shop.offer.tickets_1";
    public const string ShopOfferTickets5 = "shop.offer.tickets_5";
    public const string ShopOfferSkinDefault = "shop.offer.skin_default";
    public const string ShopOfferSkinOcean = "shop.offer.skin_ocean";
    public const string ShopOfferSkinAmber = "shop.offer.skin_amber";
    public const string ShopOfferSkinViolet = "shop.offer.skin_violet";

    public const string InventoryTitle = "inventory.title";
    public const string InventoryBack = "inventory.back";

    /// <summary>G.S.I 허브 등에서 로비/ArchE로 돌아가는 상단 버튼(상점·인벤의 Back과 동일 라벨).</summary>
    public const string HubBack = "hub.back";
    public const string InventorySkinsHeader = "inventory.skins_header";
    public const string InventoryLocked = "inventory.locked";
    public const string InventoryGoShop = "inventory.go_shop";
}
