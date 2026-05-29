namespace ArchE.AsyncVersus
{
using System;
using UnityEngine;
using ArchE.Game;

/// <summary>
/// 비동기 대전 점수 업로드 엔드포인트 설정. 기본값이 비어 있으면 HTTP 전송을 쓰지 않습니다.
/// 런타임/디버그에서 <see cref="Configure"/>로 채우거나 PlayerPrefs를 직접 설정할 수 있습니다.
/// </summary>
public static class VersusAsyncBackendSettings
{
    public const string PrefsBaseUrl = "GSI_VersusAsync_BaseUrl";
    public const string PrefsRelativePath = "GSI_VersusAsync_RelPath";
    public const string PrefsApiKey = "GSI_VersusAsync_ApiKey";
    public const string PrefsTimeoutSec = "GSI_VersusAsync_TimeoutSec";
    public const string PrefsAuthBearer = "GSI_VersusAsync_AuthBearer";

    public const string DefaultRelativePath = "/api/v1/versus/scores";

    public static string BaseUrl => GsiSaveSystem.GetString(PrefsBaseUrl, string.Empty);

    public static string RelativePath
    {
        get
        {
            string p = GsiSaveSystem.GetString(PrefsRelativePath, string.Empty);
            return string.IsNullOrEmpty(p) ? DefaultRelativePath : p;
        }
    }

    public static string ApiKey => GsiSaveSystem.GetString(PrefsApiKey, string.Empty);

    public static int TimeoutSeconds => Mathf.Clamp(GsiSaveSystem.GetInt(PrefsTimeoutSec, 25), 5, 120);

    /// <summary>true면 <c>Authorization: Bearer …</c>, false면 <c>X-Api-Key</c>.</summary>
    public static bool UseBearerAuth => GsiSaveSystem.GetInt(PrefsAuthBearer, 1) != 0;

    public static bool IsHttpConfigured =>
        !string.IsNullOrWhiteSpace(BaseUrl);

    /// <summary>저장 없이 미리보기용(에디터 UI 등). 베이스 URL이 비어 있으면 빈 문자열.</summary>
    public static string BuildEndpointUrlFromParts(string baseUrl, string relativePathOrEmptyUsesDefault)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return string.Empty;
        }

        string b = baseUrl.Trim().TrimEnd('/');
        string p = string.IsNullOrWhiteSpace(relativePathOrEmptyUsesDefault)
            ? DefaultRelativePath
            : relativePathOrEmptyUsesDefault.Trim();

        if (!p.StartsWith("/", StringComparison.Ordinal))
        {
            p = "/" + p;
        }

        return b + p;
    }

    public static string BuildEndpointUrl()
    {
        if (!IsHttpConfigured)
        {
            return string.Empty;
        }

        return BuildEndpointUrlFromParts(BaseUrl, GsiSaveSystem.GetString(PrefsRelativePath, string.Empty));
    }

    /// <summary>에디터 창에서 모든 필드를 한 번에 PlayerPrefs에 저장합니다.</summary>
    public static void SaveAll(
        string baseUrl,
        string relativePath,
        string apiKey,
        int timeoutSeconds,
        bool useBearerAuth)
    {
        GsiSaveSystem.SetString(PrefsBaseUrl, baseUrl?.Trim() ?? string.Empty);
        GsiSaveSystem.SetString(PrefsRelativePath, relativePath?.Trim() ?? string.Empty);
        GsiSaveSystem.SetString(PrefsApiKey, apiKey ?? string.Empty);
        GsiSaveSystem.SetInt(PrefsTimeoutSec, Mathf.Clamp(timeoutSeconds, 5, 120));
        GsiSaveSystem.SetInt(PrefsAuthBearer, useBearerAuth ? 1 : 0);
        GsiSaveSystem.Save();
    }

    /// <summary>베이스 URL만 비워 HTTP 업로드를 끕니다.</summary>
    public static void DisableHttpUpload()
    {
        GsiSaveSystem.SetString(PrefsBaseUrl, string.Empty);
        GsiSaveSystem.Save();
    }

    /// <summary>Versus 업로드 관련 PlayerPrefs 키를 모두 제거합니다.</summary>
    public static void DeleteAllStoredKeys()
    {
        GsiSaveSystem.DeleteKey(PrefsBaseUrl);
        GsiSaveSystem.DeleteKey(PrefsRelativePath);
        GsiSaveSystem.DeleteKey(PrefsApiKey);
        GsiSaveSystem.DeleteKey(PrefsTimeoutSec);
        GsiSaveSystem.DeleteKey(PrefsAuthBearer);
        GsiSaveSystem.Save();
    }

    /// <summary>개발·CI용: PlayerPrefs에 쓰고 저장합니다.</summary>
    public static void Configure(
        string baseUrl,
        string relativePathOrNull = null,
        string apiKey = null,
        int? timeoutSeconds = null,
        bool useBearerAuth = true)
    {
        GsiSaveSystem.SetString(PrefsBaseUrl, baseUrl ?? string.Empty);
        if (relativePathOrNull != null)
        {
            GsiSaveSystem.SetString(PrefsRelativePath, relativePathOrNull);
        }

        if (apiKey != null)
        {
            GsiSaveSystem.SetString(PrefsApiKey, apiKey);
        }

        if (timeoutSeconds.HasValue)
        {
            GsiSaveSystem.SetInt(PrefsTimeoutSec, timeoutSeconds.Value);
        }

        GsiSaveSystem.SetInt(PrefsAuthBearer, useBearerAuth ? 1 : 0);
        GsiSaveSystem.Save();
    }
}

}
