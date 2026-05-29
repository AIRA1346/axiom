using UnityEngine;

/// <summary>
/// SFX clips for UI buttons/tabs. Default asset: <c>Resources/GsiDefaultUiSounds</c>.
/// </summary>
[CreateAssetMenu(menuName = "GSI/Audio/UI Sound Settings", fileName = "GsiUiSoundSettings")]
public sealed class GsiUiSoundSettings : ScriptableObject
{
    [SerializeField] private AudioClip _primaryClick;
    [SerializeField] private AudioClip _hover;
    [SerializeField] private AudioClip _cancelOrClose;
    [SerializeField] [Range(0f, 1f)] private float _clickVolume = 0.88f;
    [SerializeField] [Range(0f, 1f)] private float _hoverVolume = 0.45f;
    [SerializeField] [Range(0f, 1f)] private float _cancelVolume = 0.75f;

    public AudioClip PrimaryClick => _primaryClick;
    public AudioClip Hover => _hover;
    public AudioClip CancelOrClose => _cancelOrClose;
    public float ClickVolume => _clickVolume;
    public float HoverVolume => _hoverVolume;
    public float CancelVolume => _cancelVolume;
}
