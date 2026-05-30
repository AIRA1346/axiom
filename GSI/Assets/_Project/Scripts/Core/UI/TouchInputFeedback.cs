using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <see cref="InputManager"/> 의 포인터 다운 위치에 짧은 시각 피드백(확장·페이드)을 표시합니다.
/// 레이캐스트를 막지 않도록 <see cref="CanvasGroup.blocksRaycasts"/> 를 끕니다.
/// </summary>
[DefaultExecutionOrder(50)]
public sealed class TouchInputFeedback : MonoBehaviour
{
    private const float RippleDuration = 0.22f;
    private const float StartScale = 0.38f;
    private const float EndScale = 1.22f;
    private const float BaseDiameterPx = 56f;

    private static Sprite _discSprite;

    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private RectTransform _canvasRect;
    private RectTransform _rippleRect;
    private Image _rippleImage;
    private Coroutine _rippleRoutine;

    private void Awake()
    {
        EnsureUi();
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnInputDown += HandleInputDown;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnInputDown -= HandleInputDown;
        }
    }

    private void OnDestroy()
    {
        if (_rippleRoutine != null)
        {
            StopCoroutine(_rippleRoutine);
            _rippleRoutine = null;
        }

        if (_canvas != null)
        {
            Destroy(_canvas.gameObject);
            _canvas = null;
        }
    }

    private void EnsureUi()
    {
        if (_canvas != null)
        {
            return;
        }

        var canvasGo = new GameObject("GsiTouchInputFeedbackCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = 32000;

        canvasGo.AddComponent<GraphicRaycaster>();
        _canvasGroup = canvasGo.AddComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = GsiUiScreenLayout.CanvasReferenceResolution;
        scaler.matchWidthOrHeight = GsiUiScreenLayout.CanvasMatchWidthOrHeight;

        _canvasRect = canvasGo.GetComponent<RectTransform>();
        GsiUiRuntimeWidgets.StretchFull(_canvasRect);

        var ripGo = new GameObject("Ripple", typeof(RectTransform));
        ripGo.transform.SetParent(canvasGo.transform, false);
        _rippleRect = ripGo.GetComponent<RectTransform>();
        _rippleRect.anchorMin = _rippleRect.anchorMax = new Vector2(0.5f, 0.5f);
        _rippleRect.pivot = new Vector2(0.5f, 0.5f);
        _rippleRect.sizeDelta = new Vector2(BaseDiameterPx, BaseDiameterPx);

        _rippleImage = ripGo.AddComponent<Image>();
        _rippleImage.sprite = GetOrCreateDiscSprite();
        _rippleImage.type = Image.Type.Simple;
        _rippleImage.raycastTarget = false;
        _rippleImage.color = RippleColor(0.78f);

        ripGo.SetActive(false);
    }

    private static Sprite GetOrCreateDiscSprite()
    {
        if (_discSprite != null)
        {
            return _discSprite;
        }

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "GsiTouchFeedbackDisc";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float r = size * 0.42f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float edge = Mathf.Clamp01((r - d) / 3.5f + 0.08f);
                float a = Mathf.Clamp01(edge);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply(false, true);
        _discSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
        _discSprite.name = "GsiTouchFeedbackDiscSprite";
        return _discSprite;
    }

    private static Color RippleColor(float alpha)
    {
        bool isLight = PlayerCosmetics.EquippedSkinId == PlayerCosmetics.SkinDefaultLightId;
        if (!isLight)
        {
            return new Color(0.48f, 0.76f, 1f, alpha);
        }

        return new Color(0.18f, 0.42f, 0.92f, alpha * 0.85f);
    }

    private void HandleInputDown(Vector2 screenPosition)
    {
        if (_canvas == null || _rippleRect == null || _rippleImage == null)
        {
            return;
        }

        if (_rippleRoutine != null)
        {
            StopCoroutine(_rippleRoutine);
        }

        _rippleRoutine = StartCoroutine(PlayRipple(screenPosition));
    }

    private IEnumerator PlayRipple(Vector2 screenPosition)
    {
        RectTransform canvasRt = _canvasRect;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt,
                screenPosition,
                null,
                out Vector2 local))
        {
            yield break;
        }

        _rippleRect.gameObject.SetActive(true);
        _rippleRect.anchoredPosition = local;
        _rippleRect.localScale = Vector3.one * StartScale;

        Color c0 = RippleColor(0.78f);
        Color c1 = RippleColor(0f);
        float t = 0f;

        while (t < RippleDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / RippleDuration);
            float ease = 1f - Mathf.Pow(1f - u, 2.6f);
            _rippleRect.localScale = Vector3.one * Mathf.Lerp(StartScale, EndScale, ease);
            _rippleImage.color = Color.Lerp(c0, c1, u);
            yield return null;
        }

        _rippleRect.gameObject.SetActive(false);
        _rippleRoutine = null;
    }
}
