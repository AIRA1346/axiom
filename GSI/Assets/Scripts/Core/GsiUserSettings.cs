using System;
using UnityEngine;

/// <summary>
/// 전역 사용자 설정(볼륨 등). PlayerPrefs에 저장하고, 이후 상점·기록 UI에서도 동일 키를 참조할 수 있습니다.
/// </summary>
public static class GsiUserSettings
{
    private const string MasterKey = "GSI_Settings_MasterVolume";
    private const string SfxKey = "GSI_Settings_SfxVolume";
    private const string MusicKey = "GSI_Settings_MusicVolume";

    public const float DefaultVolume = 1f;

    public static float MasterVolume { get; private set; } = DefaultVolume;
    public static float SfxVolume { get; private set; } = DefaultVolume;
    public static float MusicVolume { get; private set; } = DefaultVolume;

    /// <summary>저장 후 다른 시스템이 반응할 때(예: UI 동기화).</summary>
    public static event Action SettingsChanged;

    static GsiUserSettings()
    {
        Load();
    }

    public static void Load()
    {
        MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, DefaultVolume));
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, DefaultVolume));
        MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, DefaultVolume));
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(MasterKey, MasterVolume);
        PlayerPrefs.SetFloat(SfxKey, SfxVolume);
        PlayerPrefs.SetFloat(MusicKey, MusicVolume);
        PlayerPrefs.Save();
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

    /// <summary>마스터는 <see cref="AudioListener.volume"/>에 반영. SFX/음악은 AudioMixer 연동 시 확장.</summary>
    public static void ApplyToAudio()
    {
        AudioListener.volume = MasterVolume;
    }
}
