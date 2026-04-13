using System;
using UnityEngine;

/// <summary>
/// 친구와 **비동기**로 점수를 맞춰 비교하기 위한 최소 데이터 계약(나중에 전송·스토어 구현 시 그대로 사용).
/// </summary>
[Serializable]
public sealed class VersusAsyncScorePayload
{
    public int PayloadVersion = 1;

    /// <summary>논리적 모드 식별자. 예: <c>gsi.unified_exam</c>, 나중에 과목별 키 추가.</summary>
    public string SurfaceId = string.Empty;

    /// <summary><see cref="UnifiedExamHistoryStorage.FormatCompletedAtNow"/> 형식 권장.</summary>
    public string CompletedAt = string.Empty;

    public string AppVersion = string.Empty;

    // --- 통합 시험 요약(다른 서피스는 필드 추가 또는 확장 JSON) ---
    public int UnifiedExamGrade;
    public float UnifiedTotal0To600;
    public bool UnifiedOverallPass;
    public string UnifiedRewardTier = string.Empty;
}

/// <summary>나중에 “같은 조건으로 치기” 동기화용으로 확장 가능한 자리 표시.</summary>
[Serializable]
public sealed class VersusAsyncChallengeStub
{
    public int PayloadVersion = 1;
    public string ChallengeId = string.Empty;
    public string SurfaceId = string.Empty;
    public long CreatedUnixUtc;
    public int RulesVersion;
}
