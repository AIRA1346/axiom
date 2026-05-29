using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Lobby scene star node controller. Inherits all physics, visuals, and persistence from
/// <see cref="StarNodeControllerBase"/>. Only scene-specific overrides are defined here.
/// </summary>
public sealed class GsiLobbyStarNodeController : StarNodeControllerBase
{
    // ─── Lobby-specific visual tuning ────────────────────────────
    protected override float HoverScale => 1.4f;
    protected override float PointerDownSquash => 0.85f;
    protected override float TooltipOffsetY => -46f;
    protected override float TooltipFontSize => 20f;
    protected override float TooltipCharSpacing => 0.35f;
    protected override Color TooltipColor => Color.white;

    // ─── Lobby-specific boundary margins ─────────────────────────
    protected override float MarginYMin => 80f;
    protected override float MarginYMax => 64f;

    // ─── Lobby-specific state persistence key ────────────────────
    protected override string SaveKeyPrefix => "LobbyStar_";

    // ═══════════════════════════════════════════════════════════════
    // Lobby-specific Start cleanup: destroy legacy decoration lines
    // ═══════════════════════════════════════════════════════════════

    protected override void OnStartCleanup()
    {
        var rulesTf = transform.Find("LobbyLabelRules");
        if (rulesTf != null)
        {
            Destroy(rulesTf.gameObject);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Lobby-specific star visuals: 5 layers (includes InnerRingGlow)
    // ═══════════════════════════════════════════════════════════════

    protected override void BuildStarVisuals()
    {
        base.BuildStarVisuals(); // Creates _starVisualRoot

        // Layer 1: Aura Outer Glow (Vibrant background bloom)
        CreateStarLayer("AuraGlow", 40f, 40f, 45f, new Color(StarColor.r, StarColor.g, StarColor.b, 0.35f));

        // Layer 2: Secondary soft ring glow (Lobby exclusive)
        CreateStarLayer("InnerRingGlow", 24f, 24f, 0f, new Color(StarColor.r, StarColor.g, StarColor.b, 0.15f));

        // Layer 3: Vertical Flare Spike
        CreateStarLayer("SpikeV", 3.5f, 52f, 0f, new Color(StarColor.r, StarColor.g, StarColor.b, 0.95f));

        // Layer 4: Horizontal Flare Spike
        CreateStarLayer("SpikeH", 52f, 3.5f, 0f, new Color(StarColor.r, StarColor.g, StarColor.b, 0.95f));

        // Layer 5: Sparkling Diamond Core (Always bright white/light center)
        CreateStarLayer("CoreDiamond", 13f, 13f, 45f, new Color(1f, 1.0f, 0.96f, 0.98f));
    }

    // ═══════════════════════════════════════════════════════════════
    // Lobby-specific click behavior: invoke Button.onClick
    // ═══════════════════════════════════════════════════════════════

    protected override void OnStarClicked()
    {
        if (TryGetComponent(out Button btn))
        {
            btn.onClick.Invoke();
        }
    }
}
