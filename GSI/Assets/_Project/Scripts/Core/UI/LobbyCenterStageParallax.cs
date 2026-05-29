using UnityEngine;

/// <summary>
/// Slight opposite motion of Far / Mid / Near layers to mouse or touch for a subtle depth effect.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyCenterStageParallax : MonoBehaviour
{
    /// <summary>
    /// Extra pixels on each edge for Far/Mid/Near rects so <see cref="Update"/> nudges never expose the stage edge.
    /// </summary>
    public const float ParallaxLayerEdgeOverflow = 40f;

    [SerializeField] private RectTransform _layerFar;
    [SerializeField] private RectTransform _layerMid;
    [SerializeField] private RectTransform _layerNear;
    [SerializeField] private float _farMaxPixels = 12f;
    [SerializeField] private float _midMaxPixels = 7f;
    [SerializeField] private float _nearMaxPixels = 3.5f;
    [SerializeField] private float _followSpeed = 4.5f;

    private Vector2 _smoothedNudge;

    public void Configure(RectTransform far, RectTransform mid, RectTransform near)
    {
        _layerFar = far;
        _layerMid = mid;
        _layerNear = near;
    }

    /// <summary>Re-applies oversized stretch for layers created before overflow was added (or after layout reset).</summary>
    public static void EnsureLayersSizedForParallax(RectTransform far, RectTransform mid, RectTransform near)
    {
        ApplyOne(far);
        ApplyOne(mid);
        ApplyOne(near);
    }

    private static void ApplyOne(RectTransform layer)
    {
        if (layer == null)
        {
            return;
        }

        GsiUiRuntimeWidgets.StretchFullWithEdgeOverflow(layer, ParallaxLayerEdgeOverflow);
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (_layerFar == null && _layerMid == null && _layerNear == null)
        {
            return;
        }

        Vector2 screen = new Vector2(Screen.width, Screen.height);
        if (screen.x < 1f || screen.y < 1f)
        {
            return;
        }

        Vector2 pointer;
        if (Input.touchCount > 0)
        {
            pointer = Input.GetTouch(0).position;
        }
        else
        {
            pointer = Input.mousePosition;
        }

        Vector2 center = screen * 0.5f;
        Vector2 fromCenter = pointer - center;
        float nx = Mathf.Clamp(fromCenter.x / (screen.x * 0.5f), -1f, 1f);
        float ny = Mathf.Clamp(fromCenter.y / (screen.y * 0.5f), -1f, 1f);
        Vector2 targetNudge = new Vector2(-nx, -ny);
        _smoothedNudge = Vector2.Lerp(_smoothedNudge, targetNudge, Time.unscaledDeltaTime * _followSpeed);

        if (_layerFar != null)
        {
            _layerFar.anchoredPosition = _smoothedNudge * _farMaxPixels;
        }

        if (_layerMid != null)
        {
            _layerMid.anchoredPosition = _smoothedNudge * _midMaxPixels;
        }

        if (_layerNear != null)
        {
            _layerNear.anchoredPosition = _smoothedNudge * _nearMaxPixels;
        }
    }
}
