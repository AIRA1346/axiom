using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// UnityWebRequest로 <see cref="VersusAsyncScorePayload"/>를 JSON POST합니다. 코루틴은 메인 스레드에서만 실행해야 합니다.
/// </summary>
public sealed class VersusAsyncHttpTransport : IVersusAsyncTransport
{
    private readonly string _url;
    private readonly string _apiKey;
    private readonly int _timeoutSec;
    private readonly bool _useBearer;

    public VersusAsyncHttpTransport(string url, string apiKey, int timeoutSec, bool useBearerAuth)
    {
        _url = url ?? string.Empty;
        _apiKey = apiKey ?? string.Empty;
        _timeoutSec = Mathf.Clamp(timeoutSec, 5, 120);
        _useBearer = useBearerAuth;
    }

    public Task<VersusAsyncPublishOutcome> PublishLocalResultAsync(
        VersusAsyncScorePayload payload,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<VersusAsyncPublishOutcome>();
        VersusAsyncMainThreadRunner runner = VersusAsyncMainThreadRunner.Instance;
        if (runner == null)
        {
            tcs.TrySetResult(
                new VersusAsyncPublishOutcome(false, "VersusAsyncMainThreadRunner not ready.", retryRecommended: true));
            return tcs.Task;
        }

        runner.StartCoroutine(RunPublishTask(tcs, payload, cancellationToken));
        return tcs.Task;
    }

    private IEnumerator RunPublishTask(
        TaskCompletionSource<VersusAsyncPublishOutcome> tcs,
        VersusAsyncScorePayload payload,
        CancellationToken cancellationToken)
    {
        VersusAsyncPublishOutcome outcome = default;
        IEnumerator e = PublishEnumerator(payload, cancellationToken, o => outcome = o);
        while (e.MoveNext())
        {
            yield return e.Current;
        }

        tcs.TrySetResult(outcome);
    }

    /// <summary>메인 스레드 코루틴에서 호출하세요. 완료 시 <paramref name="onCompleted"/>가 한 번 호출됩니다.</summary>
    public IEnumerator PublishEnumerator(
        VersusAsyncScorePayload payload,
        CancellationToken cancellationToken,
        Action<VersusAsyncPublishOutcome> onCompleted)
    {
        if (payload == null)
        {
            onCompleted?.Invoke(new VersusAsyncPublishOutcome(false, "Payload is null.", retryRecommended: false));
            yield break;
        }

        if (string.IsNullOrWhiteSpace(_url))
        {
            onCompleted?.Invoke(new VersusAsyncPublishOutcome(false, "HTTP URL is empty.", retryRecommended: false));
            yield break;
        }

        string body;
        try
        {
            body = JsonUtility.ToJson(payload);
        }
        catch (Exception e)
        {
            onCompleted?.Invoke(
                new VersusAsyncPublishOutcome(false, "Serialize failed: " + e.Message, retryRecommended: false));
            yield break;
        }

        using var req = new UnityWebRequest(_url, UnityWebRequest.kHttpVerbPOST);
        byte[] raw = Encoding.UTF8.GetBytes(body);
        req.uploadHandler = new UploadHandlerRaw(raw) { contentType = "application/json" };
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = _timeoutSec;

        if (!string.IsNullOrEmpty(_apiKey))
        {
            if (_useBearer)
            {
                req.SetRequestHeader("Authorization", "Bearer " + _apiKey);
            }
            else
            {
                req.SetRequestHeader("X-Api-Key", _apiKey);
            }
        }

        req.SetRequestHeader("Accept", "application/json");

        UnityWebRequestAsyncOperation op = req.SendWebRequest();
        while (!op.isDone)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                req.Abort();
                onCompleted?.Invoke(new VersusAsyncPublishOutcome(false, "Cancelled.", retryRecommended: true));
                yield break;
            }

            yield return null;
        }

        long code = req.responseCode;
        string err = req.error;
        string dl = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

        if (req.result == UnityWebRequest.Result.Success && code >= 200 && code <= 299)
        {
            onCompleted?.Invoke(new VersusAsyncPublishOutcome(true, string.Empty));
            yield break;
        }

        string msg = !string.IsNullOrEmpty(err) ? err : dl;
        if (string.IsNullOrEmpty(msg))
        {
            msg = "HTTP " + code;
        }

        bool retry = ShouldRetry((int)code, req.result);
        onCompleted?.Invoke(new VersusAsyncPublishOutcome(false, msg, retryRecommended: retry));
    }

    private static bool ShouldRetry(int status, UnityWebRequest.Result result)
    {
        if (result == UnityWebRequest.Result.ConnectionError ||
            result == UnityWebRequest.Result.DataProcessingError)
        {
            return true;
        }

        if (status == 0)
        {
            return true;
        }

        if (status == 408 || status == 429)
        {
            return true;
        }

        if (status >= 500)
        {
            return true;
        }

        if (status >= 400 && status < 500)
        {
            return false;
        }

        return true;
    }
}
