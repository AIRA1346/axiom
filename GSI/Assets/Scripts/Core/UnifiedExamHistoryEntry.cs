using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// 통합 시험 1회 응시 기록(로컬 저장용). <see cref="UnityEngine.JsonUtility"/> 직렬화에 맞춘 필드명입니다.
/// </summary>
[Serializable]
public sealed class UnifiedExamHistoryEntry
{
    public string Timestamp;
    public int Grade;
    public float Total;
    public float Cutoff;
    public bool OverallPass;
    public string RewardTier;
    /// <summary>과목별 요약 한 줄을 <see cref="UnifiedExamHistoryStorage.SegmentSeparator"/> 로 이은 문자열.</summary>
    public string SegmentsJoined;
}

/// <summary>JsonUtility 용 래퍼(배열 루트 직렬화 불가).</summary>
[Serializable]
public sealed class UnifiedExamHistoryEnvelope
{
    public UnifiedExamHistoryEntry[] Items = Array.Empty<UnifiedExamHistoryEntry>();
}

/// <summary>과목 요약 문자열 결합·분리(요약에 개행이 있어도 안전하도록 단위 구분자 사용).</summary>
public static class UnifiedExamHistoryStorage
{
    public const char SegmentSeparator = '\u001F';

    /// <summary>시험 종료 시각(연·월·일·시·분·초). 로케일과 무관하게 고정 형식으로 저장합니다.</summary>
    public static string FormatCompletedAtNow()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public static string JoinSegments(string[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            return string.Empty;
        }

        return string.Join(SegmentSeparator.ToString(), lines);
    }

    public static string[] SplitSegments(string joined)
    {
        if (string.IsNullOrEmpty(joined))
        {
            return Array.Empty<string>();
        }

        return joined.Split(SegmentSeparator);
    }
}
