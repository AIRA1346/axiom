using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 로비 중앙 아트보드. 배경은 <see cref="FarLayer"/> → <see cref="MidLayer"/> → <see cref="NearLayer"/> 순으로 쌓고,
/// 그 위에 <see cref="UiScrimLayer"/>(UI용 반투명 면) → 글로우·비네트, 마지막으로 <see cref="DecorRoot"/>에 브랜딩을 둡니다.
/// </summary>
public sealed class LobbyCenterStageScaffold : MonoBehaviour
{
    public RectTransform BaseLayer;
    public RectTransform FarLayer;
    public RectTransform MidLayer;
    public RectTransform NearLayer;
    public RectTransform UiScrimLayer;
    public RectTransform GlowLayer;
    public RectTransform VignetteLayer;
    public RectTransform DecorRoot;

    public Image BaseImage;
    public Image FarImage;
    public Image MidImage;
    public Image NearImage;
    public Image UiScrimImage;
    public Image GlowImage;
    public Image VignetteImage;
}
