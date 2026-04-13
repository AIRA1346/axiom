using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
#if UNITY_EDITOR
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

/// <summary>
/// Unity Localization 패키지 초기화 및 런타임 언어 전환.
/// 기본 언어: 영어(en). 한국어(ko) 등은 에디터 Locales에 등록된 코드와 맞춥니다.
/// </summary>
public static class GameLocalization
{
    /// <summary>Unity string table collection name (see Assets/Localization/UI_Strings).</summary>
    public const string UiStringsTable = "UI_Strings";

    public const string DefaultLocaleCode = "en";

    private const string PreferredLocaleKey = "RunicAtelier.PreferredLocale";

    private static bool _initialized;
    private static Task _bootTask;

    /// <summary>선택 로케일이 바뀔 때(설정·시스템 등). UI는 이 이벤트에 구독해 문자열을 다시 채웁니다.</summary>
    public static event Action UiLocaleChanged;

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
            // 씬 진입 시 PlayerPrefs와 Unity SelectedLocale이 어긋날 수 있음 — 조용히 동기화(UI 이벤트 없음).
            TrySetLocaleWithFallbacks(GetSavedLocaleCode());
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

        UiLocaleChanged?.Invoke();
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 씬 미리보기(비플레이) 시 <see cref="GetUiString"/>이 테이블 문자열을 쓰도록 Addressables·Localization을 동기 초기화합니다.
    /// 플레이 모드에서는 호출하지 마세요 — 비동기 부트와 겹칠 수 있습니다.
    /// </summary>
    public static void TryInitializeSynchronouslyForEditorSceneView()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (_initialized)
        {
            return;
        }

        try
        {
            var addrHandle = Addressables.InitializeAsync();
            addrHandle.WaitForCompletion();

            LocalizationSettings.InitializationOperation.WaitForCompletion();
            _initialized = true;

            string code = GetSavedLocaleCode();
            TrySetLocaleWithFallbacks(code);
            UiLocaleChanged?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                $"[GameLocalization] 에디터 동기 초기화 실패 — 폴백(영문) 문자열이 사용됩니다: {e.Message}");
        }
    }
#endif

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
            if (!TrySetLocaleWithFallbacks(trimmed))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[GameLocalization] Could not apply locale '{trimmed}'. Check Project Settings → Locales.");
#endif
            }

            // SelectedLocaleChanged 가 동일 로케일로 인식되어 누락되는 경우가 있어 UI 갱신을 확실히 합니다.
            UiLocaleChanged?.Invoke();
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

    /// <summary>
    /// Localized UI string. Before initialization or if the key is missing, returns <paramref name="englishFallback"/>.
    /// </summary>
    public static string GetUiString(string entryKey, string englishFallback)
    {
        if (string.IsNullOrEmpty(entryKey))
        {
            return englishFallback ?? string.Empty;
        }

        if (!IsInitialized)
        {
            return englishFallback ?? string.Empty;
        }

        try
        {
            // TableReference / TableEntryReference: use implicit conversion from string (no public 1-arg ctor).
            string result = LocalizationSettings.StringDatabase.GetLocalizedString(UiStringsTable, entryKey);

            if (string.IsNullOrEmpty(result))
            {
                return englishFallback ?? string.Empty;
            }

            if (result.StartsWith("No translation found", StringComparison.Ordinal))
            {
                return englishFallback ?? string.Empty;
            }

            return result;
        }
        catch (Exception)
        {
            return englishFallback ?? string.Empty;
        }
    }

    /// <summary>
    /// Loads a format string from the UI table (per locale) and applies <see cref="string.Format(string, object[])"/>.
    /// </summary>
    public static string FormatUiString(string entryKey, string englishFormat, params object[] args)
    {
        string fmt = GetUiString(entryKey, englishFormat);
        try
        {
            return string.Format(fmt, args);
        }
        catch (FormatException)
        {
            try
            {
                return string.Format(englishFormat, args);
            }
            catch (FormatException)
            {
                return englishFormat;
            }
        }
    }

    /// <summary>
    /// <paramref name="entryKeys"/> 순서대로 조회해, 비어 있지 않은 첫 번역을 씁니다. (동일 문구가 <c>shop.*</c>와 <c>lobby.*</c>에 중복될 때 보조용)
    /// </summary>
    public static string GetUiStringPreferKeys(string[] entryKeys, string englishFallback)
    {
        if (entryKeys == null || entryKeys.Length == 0)
        {
            return englishFallback ?? string.Empty;
        }

        foreach (string entryKey in entryKeys)
        {
            if (string.IsNullOrEmpty(entryKey))
            {
                continue;
            }

            string s = GetUiString(entryKey, "");
            if (!string.IsNullOrEmpty(s))
            {
                return s;
            }
        }

        return englishFallback ?? string.Empty;
    }

    /// <summary>
    /// <see cref="GetUiStringPreferKeys"/> 로 포맷 문자열을 고른 뒤 <see cref="string.Format(string, object[])"/> 를 적용합니다.
    /// </summary>
    public static string FormatUiStringPreferKeys(string[] entryKeys, string englishFormat, params object[] args)
    {
        string fmt = GetUiStringPreferKeys(entryKeys, englishFormat);
        try
        {
            return string.Format(fmt, args);
        }
        catch (FormatException)
        {
            try
            {
                return string.Format(englishFormat, args);
            }
            catch (FormatException)
            {
                return englishFormat;
            }
        }
    }
}
