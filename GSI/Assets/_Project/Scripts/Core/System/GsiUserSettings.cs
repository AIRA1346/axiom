using System;
using UnityEngine;
using ArchE.Game;

/// <summary>
/// 전역 사용자 설정(볼륨, 해상도, 화면 모드 등). PlayerPrefs에 저장하고, 이후 상점·기록 UI에서도 동일 키를 참조할 수 있습니다.
/// </summary>
public static class GsiUserSettings
{
    private const string MasterKey = "GSI_Settings_MasterVolume";
    private const string SfxKey = "GSI_Settings_SfxVolume";
    private const string MusicKey = "GSI_Settings_MusicVolume";
    private const string ResWidthKey = "GSI_Settings_ResolutionWidth";
    private const string ResHeightKey = "GSI_Settings_ResolutionHeight";
    private const string ScreenModeKey = "GSI_Settings_ScreenMode";

    public const float DefaultVolume = 1f;

    /// <summary>음악 슬라이더(0~1) 기본값. 최대 대비 약 20%.</summary>
    public const float DefaultMusicVolume = 0.2f;

    public static float MasterVolume { get; private set; } = DefaultVolume;
    public static float SfxVolume { get; private set; } = DefaultVolume;
    public static float MusicVolume { get; private set; } = DefaultMusicVolume;

    public static int ResolutionWidth { get; private set; }
    public static int ResolutionHeight { get; private set; }
    public static int ScreenMode { get; private set; }

    /// <summary>저장 후 다른 시스템이 반응할 때(예: UI 동기화).</summary>
    public static event Action SettingsChanged;

    static GsiUserSettings()
    {
        Load();
    }

    public static void Load()
    {
        MasterVolume = Mathf.Clamp01(GsiSaveSystem.GetFloat(MasterKey, DefaultVolume));
        SfxVolume = Mathf.Clamp01(GsiSaveSystem.GetFloat(SfxKey, DefaultVolume));
        MusicVolume = Mathf.Clamp01(GsiSaveSystem.GetFloat(MusicKey, DefaultMusicVolume));

        // 해상도 및 화면 모드 기본값 로드 (없을 경우 현재 해상도/화면 모드 사용)
        ResolutionWidth = GsiSaveSystem.GetInt(ResWidthKey, Screen.currentResolution.width > 0 ? Screen.currentResolution.width : 1920);
        ResolutionHeight = GsiSaveSystem.GetInt(ResHeightKey, Screen.currentResolution.height > 0 ? Screen.currentResolution.height : 1080);
        ScreenMode = GsiSaveSystem.GetInt(ScreenModeKey, (int)Screen.fullScreenMode);
    }

    public static void Save()
    {
        GsiSaveSystem.SetFloat(MasterKey, MasterVolume);
        GsiSaveSystem.SetFloat(SfxKey, SfxVolume);
        GsiSaveSystem.SetFloat(MusicKey, MusicVolume);
        GsiSaveSystem.SetInt(ResWidthKey, ResolutionWidth);
        GsiSaveSystem.SetInt(ResHeightKey, ResolutionHeight);
        GsiSaveSystem.SetInt(ScreenModeKey, ScreenMode);
        GsiSaveSystem.Save();
    }

    public static void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        Save();
        ApplyToAudio();
        SettingsChanged?.Invoke();
    }

    public static void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        Save();
        SettingsChanged?.Invoke();
    }

    public static void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        Save();
        SettingsChanged?.Invoke();
    }

    /// <summary>해상도 및 화면 모드를 변경하고 적용합니다.</summary>
    public static void SetDisplaySettings(int width, int height, FullScreenMode mode)
    {
        ResolutionWidth = width;
        ResolutionHeight = height;
        ScreenMode = (int)mode;
        Save();

        Screen.SetResolution(width, height, mode);
        Debug.Log($"[GsiUserSettings] Applied Display: {width}x{height} ({mode})");
        SettingsChanged?.Invoke();
    }

    /// <summary>마스터는 <see cref="AudioListener.volume"/>에 반영. SFX/음악 승수는 <see cref="GsiAudioService"/>가 별도 소스에 적용.</summary>
    public static void ApplyToAudio()
    {
        AudioListener.volume = MasterVolume;
    }
}
