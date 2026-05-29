using UnityEngine;

/// <summary>
/// SFX / BGM용 <see cref="AudioSource"/>를 두고 <see cref="GsiUserSettings"/> 볼륨을 반영합니다.
/// 마스터는 <see cref="AudioListener.volume"/> (<see cref="GsiUserSettings.ApplyToAudio"/>), SFX·음악은 각 소스 볼륨 승수로 적용됩니다.
/// </summary>
public sealed class GsiAudioService : MonoBehaviour
{
    public static GsiAudioService Instance { get; private set; }

    [SerializeField] private AudioSource _sfx;
    [SerializeField] private AudioSource _music;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        EnsureSources();
        RefreshVolumes();
        GsiUserSettings.SettingsChanged += OnSettingsChanged;
    }

    private void OnDestroy()
    {
        GsiUserSettings.SettingsChanged -= OnSettingsChanged;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnSettingsChanged()
    {
        RefreshVolumes();
    }

    private void EnsureSources()
    {
        if (_sfx == null)
        {
            var go = new GameObject("SfxSource");
            go.transform.SetParent(transform, false);
            _sfx = go.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.loop = false;
            _sfx.spatialBlend = 0f;
        }

        if (_music == null)
        {
            var go = new GameObject("MusicSource");
            go.transform.SetParent(transform, false);
            _music = go.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.spatialBlend = 0f;
        }
    }

    private void RefreshVolumes()
    {
        GsiUserSettings.Load();
        if (_sfx != null)
        {
            _sfx.volume = Mathf.Clamp01(GsiUserSettings.SfxVolume);
        }

        if (_music != null)
        {
            _music.volume = Mathf.Clamp01(GsiUserSettings.MusicVolume);
        }
    }

    /// <summary>효과음(겹침 허용).</summary>
    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (_sfx == null || clip == null)
        {
            return;
        }

        volumeScale = Mathf.Clamp01(volumeScale);
        _sfx.PlayOneShot(clip, volumeScale);
    }

    /// <summary>배경음. 이미 재생 중이면 클립만 바꿔 재생합니다.</summary>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (_music == null || clip == null)
        {
            return;
        }

        _music.loop = loop;
        if (_music.clip != clip || !_music.isPlaying)
        {
            _music.clip = clip;
            _music.Play();
        }
    }

    public void StopMusic()
    {
        if (_music == null)
        {
            return;
        }

        _music.Stop();
        _music.clip = null;
    }

    public bool IsMusicPlaying => _music != null && _music.isPlaying;
}

/// <summary>
/// <see cref="GsiAudioService"/> 진입점. 씬에 서비스가 없으면 호출이 무시됩니다(준비 전까지 안전).
/// </summary>
public static class GsiAudio
{
    public static void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || GsiAudioService.Instance == null)
        {
            return;
        }

        GsiAudioService.Instance.PlaySfx(clip, volumeScale);
    }

    public static void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || GsiAudioService.Instance == null)
        {
            return;
        }

        GsiAudioService.Instance.PlayMusic(clip, loop);
    }

    public static void StopMusic()
    {
        if (GsiAudioService.Instance == null)
        {
            return;
        }

        GsiAudioService.Instance.StopMusic();
    }
}
