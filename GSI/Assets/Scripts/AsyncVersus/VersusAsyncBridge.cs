using System;
using System.Collections;
using System.Collections.Generic;
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
    /// <summary>false이고 <see cref="Accepted"/>가 false면 아웃박스에 넣지 않습니다(미설정·4xx 등).</summary>
    public readonly bool RetryRecommended;

    public VersusAsyncPublishOutcome(bool accepted, string message, bool retryRecommended = true)
    {
        Accepted = accepted;
        Message = message ?? string.Empty;
        RetryRecommended = retryRecommended;
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
        return Task.FromResult(
            new VersusAsyncPublishOutcome(false, "Versus async transport not configured.", retryRecommended: false));
    }
}

/// <summary>
/// 로컬에서 점수가 확정됐을 때 한 번 거치는 **단일 진입점**. UI·백엔드는 여기만 구독/교체하면 됨.
/// </summary>
public static class VersusAsyncBridge
{
    private const string LastLocalPayloadPrefsKey = "GSI_VersusAsync_LastLocalPayloadJson";

    private static readonly Queue<PublishJob> Jobs = new Queue<PublishJob>();
    private static bool ProcessorRunning;

    public static IVersusAsyncTransport Transport { get; set; } = new VersusAsyncNullTransport();

    /// <summary>로컬 점수가 “공유 후보”로 준비됐을 때(예: 통합 시험 종료 직후).</summary>
    public static event Action<VersusAsyncScorePayload> LocalResultRecorded;

    private sealed class PublishJob
    {
        public bool FlushOnly;
        public VersusAsyncScorePayload Payload;
        public TaskCompletionSource<bool> Completion;
    }

    public static VersusAsyncScorePayload CreateUnifiedExamPayload(
        int grade,
        float total0To600,
        bool overallPass,
        string rewardTier,
        string completedAt,
        string asyncMatchId = null)
    {
        var p = new VersusAsyncScorePayload
        {
            PayloadVersion = 1,
            SurfaceId = "gsi.unified_exam",
            CompletedAt = completedAt ?? string.Empty,
            AppVersion = Application.version,
            UnifiedExamGrade = grade,
            UnifiedTotal0To600 = total0To600,
            UnifiedOverallPass = overallPass,
            UnifiedRewardTier = string.IsNullOrEmpty(rewardTier) ? "F" : rewardTier,
            AsyncMatchId = asyncMatchId ?? string.Empty
        };

        if (SteamworksService.TryGetSteamId(out ulong sid))
        {
            p.SubmitterSteamId = sid.ToString();
        }

        return p;
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
        Jobs.Enqueue(new PublishJob { FlushOnly = false, Payload = payload });
        KickProcessor();
    }

    /// <summary>
    /// <see cref="VersusAsyncBackendSettings"/>를 읽어 HTTP 전송 또는 널 전송을 선택합니다. 부트 시 한 번 호출하는 것을 권장합니다.
    /// </summary>
    public static void ApplyTransportFromSettings()
    {
        if (VersusAsyncBackendSettings.IsHttpConfigured)
        {
            string url = VersusAsyncBackendSettings.BuildEndpointUrl();
            Transport = new VersusAsyncHttpTransport(
                url,
                VersusAsyncBackendSettings.ApiKey,
                VersusAsyncBackendSettings.TimeoutSeconds,
                VersusAsyncBackendSettings.UseBearerAuth);
        }
        else
        {
            Transport = new VersusAsyncNullTransport();
        }
    }

    /// <summary>시작 시 아웃박스만 비우도록 요청합니다(작업 큐에 넣음).</summary>
    public static void RequestFlushOutboxOnBoot()
    {
        Jobs.Enqueue(new PublishJob { FlushOnly = true });
        KickProcessor();
    }

    /// <summary>아웃박스에 쌓인 페이로드만 순서대로 재전송합니다.</summary>
    public static Task FlushPendingOutboxAsync(CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<bool>();
        Jobs.Enqueue(new PublishJob { FlushOnly = true, Completion = tcs });
        KickProcessor();

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() => tcs.TrySetCanceled());
        }

        return tcs.Task;
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

    private static void KickProcessor()
    {
        if (ProcessorRunning)
        {
            return;
        }

        VersusAsyncMainThreadRunner runner = VersusAsyncMainThreadRunner.Instance;
        if (runner == null)
        {
            Debug.LogWarning("[VersusAsync] Main thread runner missing; publish deferred until next frame.");
            return;
        }

        runner.StartCoroutine(CoProcessor());
    }

    private static IEnumerator CoProcessor()
    {
        ProcessorRunning = true;
        try
        {
            while (Jobs.Count > 0)
            {
                PublishJob job = Jobs.Dequeue();
                if (Transport == null)
                {
                    job.Completion?.TrySetResult(true);
                    continue;
                }

                if (job.FlushOnly)
                {
                    yield return CoDrainOutbox();
                    job.Completion?.TrySetResult(true);
                    continue;
                }

                yield return CoDrainOutbox();
                yield return CoPublishFresh(job.Payload);
            }
        }
        finally
        {
            ProcessorRunning = false;
            if (Jobs.Count > 0)
            {
                KickProcessor();
            }
        }
    }

    private static IEnumerator CoDrainOutbox()
    {
        if (Transport is VersusAsyncNullTransport)
        {
            yield break;
        }

        while (VersusAsyncOutbox.Count > 0)
        {
            if (!VersusAsyncOutbox.TryPeekFirst(out VersusAsyncScorePayload head))
            {
                VersusAsyncOutbox.RemoveFirst();
                continue;
            }

            VersusAsyncPublishOutcome outcome = default;
            IEnumerator send = CoSendTransport(head, forOutboxHead: true, o => outcome = o);
            while (send.MoveNext())
            {
                yield return send.Current;
            }

            if (outcome.Accepted)
            {
                VersusAsyncOutbox.RemoveFirst();
                continue;
            }

            if (!outcome.RetryRecommended)
            {
                VersusAsyncOutbox.RemoveFirst();
                Debug.LogWarning("[VersusAsync] Publish rejected (no retry): " + outcome.Message);
                yield break;
            }

            yield break;
        }
    }

    private static IEnumerator CoPublishFresh(VersusAsyncScorePayload payload)
    {
        VersusAsyncPublishOutcome outcome = default;
        IEnumerator send = CoSendTransport(payload, forOutboxHead: false, o => outcome = o);
        while (send.MoveNext())
        {
            yield return send.Current;
        }

        if (outcome.Accepted)
        {
            yield break;
        }

        if (!outcome.RetryRecommended)
        {
            Debug.LogWarning("[VersusAsync] Publish rejected (no retry): " + outcome.Message);
            yield break;
        }

        VersusAsyncOutbox.Enqueue(payload);
    }

    private static IEnumerator CoSendTransport(
        VersusAsyncScorePayload payload,
        bool forOutboxHead,
        Action<VersusAsyncPublishOutcome> sink)
    {
        // CS1626: iterator 안에서는 catch가 있는 try 블록에서 yield 할 수 없음 — MoveNext만 try로 감쌈.
        Exception caught = null;

        if (Transport is VersusAsyncHttpTransport http)
        {
            IEnumerator e = http.PublishEnumerator(payload, CancellationToken.None, sink);
            while (true)
            {
                bool moveNext;
                try
                {
                    moveNext = e.MoveNext();
                }
                catch (Exception ex)
                {
                    caught = ex;
                    break;
                }

                if (!moveNext)
                {
                    break;
                }

                yield return e.Current;
            }
        }
        else
        {
            Task<VersusAsyncPublishOutcome> t = Transport.PublishLocalResultAsync(payload, CancellationToken.None);
            while (!t.IsCompleted)
            {
                yield return null;
            }

            try
            {
                sink?.Invoke(t.Result);
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        }

        if (caught != null)
        {
            Debug.LogWarning("[VersusAsync] Transport publish failed: " + caught.Message);
            if (Transport is VersusAsyncHttpTransport && !forOutboxHead)
            {
                VersusAsyncOutbox.Enqueue(payload);
            }

            sink?.Invoke(new VersusAsyncPublishOutcome(false, caught.Message, retryRecommended: true));
        }
    }
}
