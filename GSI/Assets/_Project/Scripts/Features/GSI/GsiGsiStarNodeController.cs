using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// GSI 우주 Orrery 성계에서 개별 미니게임을 나타내는 별 노드 컨트롤러.
/// 모든 물리·비주얼·상호작용·직렬화 로직은 <see cref="StarNodeControllerBase"/>에서 상속받으며,
/// GSI 씬 전용 기능(화이트홀 척력, 액션 델리게이트, Mode 속성)만 이곳에 정의합니다.
/// </summary>
public sealed class GsiGsiStarNodeController : StarNodeControllerBase
{
    // ─── GSI-specific properties ────────────────────────────────
    public TestMode Mode;

    // ─── GSI-specific action delegates ──────────────────────────
    public Action<Vector2> OnBeginDragAction;
    public Action<Vector2> OnDragAction;
    public Action OnEndDragAction;
    public Action<GsiGsiStarNodeController> OnClickedAction;
    public Action OnPointerDownAction;
    public Action OnPointerUpAction;

    // ─── Public accessor for RectTransform (used by OrbitalSelector) ─
    public RectTransform Rect
    {
        get
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }
            return _rectTransform;
        }
    }

    // ─── GSI-specific visual tuning ─────────────────────────────
    protected override float HoverScale => 1.35f;
    protected override float PointerDownSquash => 0.8f;
    protected override float TooltipOffsetY => -28f;
    protected override float TooltipFontSize => 18f;
    protected override float TooltipCharSpacing => 0.5f;
    protected override Color TooltipColor => StarColor;

    // ─── GSI-specific boundary margins ──────────────────────────
    protected override float MarginYMin => 84f;
    protected override float MarginYMax => 148f; // GSI 상단 헤더 스트립 영역 침범 차단

    // ─── GSI-specific state persistence key ─────────────────────
    protected override string SaveKeyPrefix => "GsiStar_";

    // ═══════════════════════════════════════════════════════════════
    // GSI-specific star visuals: 4 layers (no InnerRingGlow)
    // ═══════════════════════════════════════════════════════════════

    protected override void BuildStarVisuals()
    {
        base.BuildStarVisuals(); // Creates _starVisualRoot

        // Layer 1: Aura Outer Glow (은은한 백그라운드 오라)
        CreateStarLayer("OuterGlow", 18f, 18f, 45f, new Color(StarColor.r, StarColor.g, StarColor.b, 0.3f));

        // Layer 2: Spike Vertical (세로 나선 불꽃)
        CreateStarLayer("SpikeV", 1.5f, 26f, 0f, new Color(StarColor.r, StarColor.g, StarColor.b, 0.92f));

        // Layer 3: Spike Horizontal (가로 나선 불꽃)
        CreateStarLayer("SpikeH", 26f, 1.5f, 0f, new Color(StarColor.r, StarColor.g, StarColor.b, 0.92f));

        // Layer 4: Diamond Core (영롱한 중앙 화이트 코어)
        CreateStarLayer("CoreDiamond", 6f, 6f, 45f, new Color(1f, 1f, 1f, 0.98f));
    }

    // ═══════════════════════════════════════════════════════════════
    // GSI-specific: WhiteHole repulsion force
    // ═══════════════════════════════════════════════════════════════

    protected override void ApplyAdditionalForces()
    {
        if (ArchE.Game.GsiCosmicOrrerySystem.Instance != null)
        {
            _velocity += ArchE.Game.GsiCosmicOrrerySystem.Instance.GetWhiteHoleRepulsion(_rectTransform.anchoredPosition) * Time.unscaledDeltaTime;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // GSI-specific: Pointer event delegate forwarding
    // ═══════════════════════════════════════════════════════════════

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        OnPointerDownAction?.Invoke();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        OnPointerUpAction?.Invoke();
    }

    protected override void OnStarClicked()
    {
        OnClickedAction?.Invoke(this);
    }

    // ═══════════════════════════════════════════════════════════════
    // GSI-specific: Drag event delegate forwarding
    // ═══════════════════════════════════════════════════════════════

    public override void OnBeginDrag(PointerEventData eventData)
    {
        base.OnBeginDrag(eventData);
        OnBeginDragAction?.Invoke(eventData.position);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        base.OnDrag(eventData);
        OnDragAction?.Invoke(eventData.position);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
        OnEndDragAction?.Invoke();
    }
}
