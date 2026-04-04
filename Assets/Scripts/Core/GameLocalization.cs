using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Unity Localization 패키지 초기화 및 런타임 언어 전환.
/// 기본 언어: 영어(en). 한국어(ko) 등은 에디터 Locales에 등록된 코드와 맞춥니다.
/// </summary>
public static class GameLocalization
{
    public const string DefaultLocaleCode = "en";

    private const string PreferredLocaleKey = "RunicAtelier.PreferredLocale";

    private static bool _initialized;
    private static Task _bootTask;

    /// <summary>LocalizationSettings 초기화가 끝났는지(실패 시 false일 수 있음).</summary>
    public static bool IsInitialized => _initialized;

    public static async Task InitializeAsync()
    {
        await AddressablesBootstrap.EnsureInitializedAsync();
        await LocalizationSettings.InitializationOperation.Task;
    }

    /// <summary>
    /// 부팅 시 한 번: 패키지 초기화 후 저장된 언어 적용, 없으면 영어.
    /// 중복 호출은 같은 Task를 기다립니다.
    /// </summary>
    public static Task InitializeAndApplySavedLocaleAsync()
    {
        if (_initialized)
        {
            return Task.CompletedTask;
        }

        if (_bootTask != null)
        {
            return _bootTask;
        }

        _bootTask = InitializeAndApplySavedLocaleCoreAsync();
        return _bootTask;
    }

    private static async Task InitializeAndApplySavedLocaleCoreAsync()
    {
        await AddressablesBootstrap.EnsureInitializedAsync();

        try
        {
            await LocalizationSettings.InitializationOperation.Task;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameLocalization] Initialization failed: {e.Message}");
            _bootTask = null;
            return;
        }

        _initialized = true;

        string code = GetSavedLocaleCode();
        if (!TrySetLocaleWithFallbacks(code))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[GameLocalization] Could not apply locale '{code}'. Check Project Settings → Locales.");
#endif
        }
    }

    /// <summary>PlayerPrefs에 저장된 코드(없으면 기본 en).</summary>
    public static string GetSavedLocaleCode()
    {
        string code = PlayerPrefs.GetString(PreferredLocaleKey, DefaultLocaleCode);
        return string.IsNullOrWhiteSpace(code) ? DefaultLocaleCode : code.Trim();
    }

    /// <summary>
    /// 설정 UI에서 호출: 저장 후 즉시 적용(이미 초기화된 경우).
    /// </summary>
    public static void SavePreferredLocale(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return;
        }

        string trimmed = languageCode.Trim();
        PlayerPrefs.SetString(PreferredLocaleKey, trimmed);
        PlayerPrefs.Save();

        if (_initialized)
        {
            TrySetLocaleWithFallbacks(trimmed);
        }
    }

    /// <summary>
    /// 예: "ko", "en". 에디터에 등록된 Locale 코드와 일치해야 합니다.
    /// </summary>
    public static bool TrySetLocale(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
        {
            return false;
        }

        if (LocalizationSettings.AvailableLocales == null)
        {
            return false;
        }

        var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(languageCode));
        if (locale == null)
        {
            return false;
        }

        LocalizationSettings.SelectedLocale = locale;
        return true;
    }

    /// <summary>
    /// 초기화 완료 후 언어를 설정합니다.
    /// </summary>
    public static async Task<bool> TrySetLocaleAsync(string languageCode)
    {
        await InitializeAsync();
        return TrySetLocale(languageCode);
    }

    private static bool TrySetLocaleWithFallbacks(string primary)
    {
        if (TrySetLocale(primary))
        {
            return true;
        }

        if (string.Equals(primary, "en", StringComparison.OrdinalIgnoreCase) &&
            TrySetLocale("en-US"))
        {
            return true;
        }

        if (string.Equals(primary, "ko", StringComparison.OrdinalIgnoreCase) &&
            TrySetLocale("ko-KR"))
        {
            return true;
        }

        if (TrySetLocale(DefaultLocaleCode))
        {
            return true;
        }

        return TrySetLocale("en-US") || TrySetLocale("ko-KR");
    }
}
