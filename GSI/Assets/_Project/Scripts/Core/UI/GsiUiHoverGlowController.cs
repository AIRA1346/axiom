using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI 요소 호버(Hover) 시, 버튼의 스케일 크기뿐만 아니라 
/// 외곽 테두리(Outline)의 림(Rim) 두께 및 발광 강도(Alpha)를 
/// 부드러운 Lerp 코루틴을 통해 팽창/수축시키는 초프리미엄 반응형 글로우 컨트롤러입니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class GsiUiHoverGlowController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Physical Scale Feedback")]
    [SerializeField] private float hoverScaleMultiplier = 1.04f;
    [SerializeField] private float lerpSpeed = 12f;

    [Header("Rim Glow & Outline Expansion")]
    [SerializeField] private float hoverOutlineThicknessMultiplier = 1.8f;
    [SerializeField] private float hoverGlowAlphaMultiplier = 1.6f;

    private Vector3 _restingScale;
    private Outline _outline;
    private Image _image;
    private CanvasGroup _glowLayerCg;

    private Vector2 _restingOutlineDistance;
    private Color _restingOutlineColor;
    private Color _restingImageColor;

    private Coroutine _hoverAnimationRoutine;
    private bool _isHovered;

    private void Awake()
    {
        _restingScale = transform.localScale;
        _outline = GetComponent<Outline>();
        _image = GetComponent<Image>();

        if (_outline != null)
        {
            _restingOutlineDistance = _outline.effectDistance;
            _restingOutlineColor = _outline.effectColor;
        }

        if (_image != null)
        {
            _restingImageColor = _image.color;
        }

        // 자식 요소 중 "Glow", "glow", "GlowRim", "OutlineGlow" 명칭의 글로우 레이어가 있다면 캐싱
        Transform glowTf = transform.Find("Glow") ?? transform.Find("glow") ?? transform.Find("GlowRim") ?? transform.Find("OutlineGlow");
        if (glowTf != null)
        {
            if (!glowTf.TryGetComponent(out _glowLayerCg))
            {
                _glowLayerCg = glowTf.gameObject.AddComponent<CanvasGroup>();
            }
            _glowLayerCg.alpha = 0f;
        }
    }

    private void OnEnable()
    {
        ResetVisualsToResting();
    }

    private void OnDisable()
    {
        ResetVisualsToResting();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        // 호버 시 사운드나 텍스트 하이라이트 등과 연동
        StartHoverAnimation();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        StartHoverAnimation();
    }

    private void StartHoverAnimation()
    {
        if (_hoverAnimationRoutine != null)
        {
            StopCoroutine(_hoverAnimationRoutine);
        }
        _hoverAnimationRoutine = StartCoroutine(CoAnimateHoverFeedback());
    }

    private IEnumerator CoAnimateHoverFeedback()
    {
        float targetScaleMult = _isHovered ? hoverScaleMultiplier : 1f;
        Vector3 targetScale = _restingScale * targetScaleMult;

        Vector2 targetOutlineDist = _restingOutlineDistance;
        Color targetOutlineColor = _restingOutlineColor;
        if (_isHovered)
        {
            targetOutlineDist = _restingOutlineDistance * hoverOutlineThicknessMultiplier;
            targetOutlineColor.a = Mathf.Clamp01(_restingOutlineColor.a * hoverGlowAlphaMultiplier);
        }

        Color targetImageColor = _restingImageColor;
        if (_isHovered && _image != null)
        {
            // 글래스 백그라운드의 밀도도 은은하게 높여 대비감을 증가
            targetImageColor.a = Mathf.Clamp01(_restingImageColor.a * 1.15f);
        }

        float targetGlowAlpha = _isHovered ? 1f : 0f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * lerpSpeed;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, t);

            if (_outline != null)
            {
                _outline.effectDistance = Vector2.Lerp(_outline.effectDistance, targetOutlineDist, t);
                _outline.effectColor = Color.Lerp(_outline.effectColor, targetOutlineColor, t);
            }

            if (_image != null)
            {
                _image.color = Color.Lerp(_image.color, targetImageColor, t);
            }

            if (_glowLayerCg != null)
            {
                _glowLayerCg.alpha = Mathf.Lerp(_glowLayerCg.alpha, targetGlowAlpha, t);
            }

            yield return null;
        }

        // 최종 상태 보장
        transform.localScale = targetScale;
        if (_outline != null)
        {
            _outline.effectDistance = targetOutlineDist;
            _outline.effectColor = targetOutlineColor;
        }
        if (_image != null)
        {
            _image.color = targetImageColor;
        }
        if (_glowLayerCg != null)
        {
            _glowLayerCg.alpha = targetGlowAlpha;
        }

        _hoverAnimationRoutine = null;
    }

    private void ResetVisualsToResting()
    {
        _isHovered = false;
        if (_hoverAnimationRoutine != null)
        {
            StopCoroutine(_hoverAnimationRoutine);
            _hoverAnimationRoutine = null;
        }

        transform.localScale = _restingScale;
        if (_outline != null)
        {
            _outline.effectDistance = _restingOutlineDistance;
            _outline.effectColor = _restingOutlineColor;
        }
        if (_image != null)
        {
            _image.color = _restingImageColor;
        }
        if (_glowLayerCg != null)
        {
            _glowLayerCg.alpha = 0f;
        }
    }

    /// <summary>
    /// 버튼 컴포넌트에 호버 글로우 연출 컨트롤러가 없다면 동적으로 할당하고 초기화합니다.
    /// </summary>
    public static void EnsureOn(Button button)
    {
        if (button == null)
        {
            return;
        }

        var ctrl = button.GetComponent<GsiUiHoverGlowController>();
        if (ctrl == null)
        {
            ctrl = button.gameObject.AddComponent<GsiUiHoverGlowController>();
        }
    }
}
