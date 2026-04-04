using UnityEngine;

/// <summary>
/// 통합 공식 시험 한 과목 종료 시 GameManager로 전달되는 원시 결과입니다.
/// </summary>
public struct UnifiedExamSegmentPayload
{
    public TestMode Mode;
    /// <summary>게임 로직상 즉시 실패(예: 반응 부정 출발).</summary>
    public bool HardFailed;
    public float Primary;
    public float Secondary;
    public float Tertiary;
    public bool BoolFlag;
}

/// <summary>
/// 통합 시험 6과목 중 한 줄 기록(결과 화면·저장용).
/// </summary>
public sealed class UnifiedExamSegmentRecord
{
    public TestMode Mode;
    public float Score0To100;
    public bool Passed;
    public bool HardFailed;
    public string SummaryLine;
    public UnifiedExamSegmentPayload Payload;
}
