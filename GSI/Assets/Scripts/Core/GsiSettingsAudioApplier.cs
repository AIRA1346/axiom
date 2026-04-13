using UnityEngine;

/// <summary>
/// 부팅 시 저장된 볼륨을 적용하고, 설정 변경 시 다시 적용합니다.
/// </summary>
public sealed class GsiSettingsAudioApplier : MonoBehaviour
{
    private void Awake()
    {
        GsiUserSettings.Load();
        GsiUserSettings.ApplyToAudio();
        GsiUserSettings.SettingsChanged += OnSettingsChanged;
    }

    private void OnDestroy()
    {
        GsiUserSettings.SettingsChanged -= OnSettingsChanged;
    }

    private void OnSettingsChanged()
    {
        GsiUserSettings.ApplyToAudio();
    }
}
