using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 비동기 대전용 **교체 가능한 전송 계층**. 기본은 아무 것도 하지 않음(게임 완성 전 플레이스홀더).
/// </summary>
public interface IVersusAsyncTransport
{
    Task<VersusAsyncPublishOutcome> PublishLocalResultAsync(VersusAsyncScorePayload payload, CancellationToken cancellationToken);
}

public readonly struct VersusAsyncPublishOutcome
{
    public readonly bool Accepted;
    public readonly string Message;

    public VersusAsyncPublishOutcome(bool accepted, string message)
    {
        Accepted = accepted;
        Message = message ?? string.Empty;
    }
}

/// <summary>
/// 전송 미구현 시 사용. 나중에 Steam / HTTP / 파일 공유 등으로 교체.
/// </summary>
public sealed class VersusAsyncNullTransport : IVersusAsyncTransport
{
    public Task<VersusAsyncPublishOutcome> PublishLocalResultAsync(
        VersusAsyncScorePayload payload,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new VersusAsyncPublishOutcome(false, "Versus async transport not configured."));
    }
}

/// <summary>
/// 로컬에서 점수가 확정됐을 때 한 번 거치는 **단일 진입점**. UI·백엔드는 여기만 구독/교체하면 됨.
/// </summary>
public static class VersusAsyncBridge
{
    private const string LastLocalPayloadPrefsKey = "GSI_VersusAsync_LastLocalPayloadJson";

    public static IVersusAsyncTransport Transport { get; set; } = new VersusAsyncNullTransport();

    /// <summary>로컬 점수가 “공유 후보”로 준비됐을 때(예: 통합 시험 종료 직후).</summary>
    public static event Action<VersusAsyncScorePayload> LocalResultRecorded;

    public static VersusAsyncScorePayload CreateUnifiedExamPayload(
        int grade,
        float total0To600,
        bool overallPass,
        string rewardTier,
        string completedAt)
    {
        return new VersusAsyncScorePayload
        {
            PayloadVersion = 1,
            SurfaceId = "gsi.unified_exam",
            CompletedAt = completedAt ?? string.Empty,
            AppVersion = Application.version,
            UnifiedExamGrade = grade,
            UnifiedTotal0To600 = total0To600,
            UnifiedOverallPass = overallPass,
            UnifiedRewardTier = string.IsNullOrEmpty(rewardTier) ? "F" : rewardTier
        };
    }

    /// <summary>
    /// 나중에 전송 구현 시 이 메서드만 호출하면 됨. 현재는 이벤트 + 로컬 디버그용 저장 + (옵션) Transport 호출.
    /// </summary>
    public static void NotifyLocalResultReady(VersusAsyncScorePayload payload)
    {
        if (payload == null)
        {
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(payload);
            PlayerPrefs.SetString(LastLocalPayloadPrefsKey, json);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[VersusAsync] Failed to cache last payload: " + e.Message);
        }

        LocalResultRecorded?.Invoke(payload);
        _ = PublishBestEffortAsync(payload);
    }

    public static bool TryLoadLastLocalPayload(out VersusAsyncScorePayload payload)
    {
        payload = null;
        string json = PlayerPrefs.GetString(LastLocalPayloadPrefsKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            return false;
        }

        try
        {
            payload = JsonUtility.FromJson<VersusAsyncScorePayload>(json);
            return payload != null;
        }
        catch
        {
            return false;
        }
    }

    private static async Task PublishBestEffortAsync(VersusAsyncScorePayload payload)
    {
        if (Transport == null)
        {
            return;
        }

        try
        {
            await Transport.PublishLocalResultAsync(payload, CancellationToken.None);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[VersusAsync] Transport publish failed: " + e.Message);
        }
    }
}
