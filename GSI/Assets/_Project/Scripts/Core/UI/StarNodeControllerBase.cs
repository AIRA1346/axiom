using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ArchE.Game;

/* =========================================================================================================
 * 🌌 STAR NODE BASE CLASS — 공용 우주 물리 및 비주얼 엔진
 * =========================================================================================================
 * GsiLobbyStarNodeController (메인 로비) 와 GsiGsiStarNodeController (GSI 시설) 의 공통 물리·비주얼·
 * 상호작용·상태 직렬화 로직을 단일 소스(Single Source of Truth)로 통합한 추상 기반 클래스입니다.
 *
 * 씬별 의도적 차이점은 protected virtual 프로퍼티/메서드로 override하십시오.
 * 📄 물리 동기화 규격: Assets/_Project/Docs/GSI_COSMIC_PHYSICS_DESIGN_STANDARDS.md
 * ========================================================================================================= */

/// <summary>
/// Abstract base class for draggable, kinetic-physics star nodes with elastic collisions,
/// boundary bouncing, hover micro-animations, tooltip labels, and state persistence.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public abstract class StarNodeControllerBase : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    ICosmicKineticObject
{
    // ─── Visual Config ──────────────────────────────────────────────
    [Header("Visual Config")]
    public Color StarColor = Color.white;
    public string ButtonLabelText = "Button";
    public Vector2 InitialPosition = Vector2.zero;

    // ─── Physics Config (동기화 대상 — 수정 시 디자인 문서 참조) ───
    [Header("Physics Config")]
    public float Friction = 0.45f;
    public float BounceFactor = 0.92f;
    public float MaxVelocity = 1800f;
    public float MinDriftSpeed = 80f;
    public float CollisionRadius = 16f;

    // ─── Audio Config ───────────────────────────────────────────────
    [Header("Audio Config")]
    [Tooltip("Optional custom collision sound effect. If null, falls back to GsiUiSound hover/click settings.")]
    public AudioClip CollisionSfx;

    // ─── Protected state accessible to subclasses ───────────────────
    protected RectTransform _rectTransform;
    protected Transform _scaleReferenceParent;
    protected RectTransform _starVisualRoot;
    protected CanvasGroup _labelCanvasGroup;
    protected TextMeshProUGUI _labelTmp;
    protected Vector2 _velocity;
    protected bool _isDragging = false;
    protected bool _isHovered = false;

    // ─── Private state ──────────────────────────────────────────────
    private float _currentHoverT = 0f;
    private Coroutine _hoverRoutine;
    private Vector2 _dragStartPos;
    private float _totalDragDist;
    private Vector2 _lastDragFramePos;
    private float _lastCollisionSoundTime = 0f;
    private bool _draggedThisFrame = false;

    // ═══════════════════════════════════════════════════════════════
    // Scene-specific virtual properties — override in subclasses
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Default touch interaction bounding box size (Lobby & GSI default is 80x80)</summary>
    protected virtual Vector2 InteractionSize => new Vector2(80f, 80f);

    /// <summary>Default kinetic physics collision radius (Lobby & GSI default is 16f)</summary>
    protected virtual float BaseCollisionRadius => 16f;

    /// <summary>Hover animation target scale (Lobby=1.4f, GSI=1.35f)</summary>
    protected virtual float HoverScale => 1.4f;

    /// <summary>Pointer-down squash scale (Lobby=0.85f, GSI=0.8f)</summary>
    protected virtual float PointerDownSquash => 0.85f;

    /// <summary>Tooltip label Y offset from star center (Lobby=-46f, GSI=-42f)</summary>
    protected virtual float TooltipOffsetY => -46f;

    /// <summary>Tooltip font size (Lobby=20f, GSI=18f)</summary>
    protected virtual float TooltipFontSize => 20f;

    /// <summary>Tooltip character spacing (Lobby=0.35f, GSI=0.5f)</summary>
    protected virtual float TooltipCharSpacing => 0.35f;

    /// <summary>Tooltip text color (Lobby=White, GSI=StarColor)</summary>
    protected virtual Color TooltipColor => Color.white;

    /// <summary>Screen boundary bottom margin (Lobby=80f, GSI=84f)</summary>
    protected virtual float MarginYMin => 80f;

    /// <summary>Screen boundary top margin (Lobby=64f, GSI=148f for header strip)</summary>
    protected virtual float MarginYMax => 64f;

    /// <summary>PlayerPrefs save key prefix (Lobby="LobbyStar_", GSI="GsiStar_")</summary>
    protected abstract string SaveKeyPrefix { get; }

    // ═══════════════════════════════════════════════════════════════
    // Lifecycle
    // ═══════════════════════════════════════════════════════════════

    protected virtual void Start()
    {
        _rectTransform = GetComponent<RectTransform>();

        // Force the rect size to make standard interaction area comfortable
        _rectTransform.sizeDelta = InteractionSize;
        _rectTransform.anchoredPosition = InitialPosition;

        // Force collision radius to match scaled visuals
        CollisionRadius = BaseCollisionRadius;

        // Clear original background image, but keep it active as click target
        if (TryGetComponent(out Image img))
        {
            img.sprite = null;
            img.color = Color.clear;
            img.raycastTarget = true;
        }

        // Hook for subclass-specific cleanup (e.g. LobbyLabelRules destruction)
        OnStartCleanup();

        // Clear existing child text components
        var originalText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (originalText != null && originalText.gameObject != gameObject)
        {
            Destroy(originalText.gameObject);
        }

        // Build tooltip label dynamically underneath
        BuildTooltipLabel();

        // Build premium procedural star layers
        BuildStarVisuals();

        // Give a small random initial drift velocity
        _velocity = new Vector2(Random.Range(-90f, 90f), Random.Range(-90f, 90f));

        // Cache parent scale reference
        Transform curr = transform.parent;
        while (curr != null)
        {
            if (curr.name == "LobbyCenterStage" || curr.name == "GsiCosmicStage" || curr.GetComponent<GsiCosmicViewportController>() != null)
            {
                _scaleReferenceParent = curr;
                break;
            }
            curr = curr.parent;
        }

        // Load saved cosmic position & velocity state if it exists
        LoadState();

        // Register in centralized Orrery System
        if (GsiCosmicOrrerySystem.Instance != null)
        {
            GsiCosmicOrrerySystem.Instance.RegisterStarNode(this);
        }
    }

    /// <summary>Override to perform additional cleanup during Start (before tooltip/visual build).</summary>
    protected virtual void OnStartCleanup() { }

    protected virtual void Update()
    {
        // 1. Elegant rotation
        if (_starVisualRoot != null)
        {
            float rotSpeed = _isHovered ? 48f : 12f;
            _starVisualRoot.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * rotSpeed);

            // ─── Procedural Soft Twinkling Effect (은은하게 반짝이는 효과) ───
            // Slowly breathe the glows
            float glowPulse = 0.82f + Mathf.PingPong(Time.unscaledTime * 0.5f, 0.25f); // pulses between 0.82 and 1.07
            
            Transform aura = _starVisualRoot.Find("AuraGlow");
            if (aura != null) aura.localScale = new Vector3(glowPulse, glowPulse, 1f);
            
            Transform outer = _starVisualRoot.Find("OuterGlow");
            if (outer != null) outer.localScale = new Vector3(glowPulse, glowPulse, 1f);
            
            Transform inner = _starVisualRoot.Find("InnerRingGlow");
            if (inner != null) inner.localScale = new Vector3(glowPulse, glowPulse, 1f);

            // Twist and stretch the flare spikes in opposite phases
            float spikePulse = 0.88f + Mathf.PingPong(Time.unscaledTime * 1.2f, 0.22f); // faster twinkling
            
            Transform spikeV = _starVisualRoot.Find("SpikeV");
            if (spikeV != null) spikeV.localScale = new Vector3(1f, spikePulse, 1f);
            
            Transform spikeH = _starVisualRoot.Find("SpikeH");
            if (spikeH != null) spikeH.localScale = new Vector3(spikePulse, 1f, 1f);
        }

        // 2. Kinetic drag velocity update (keeps dragging smooth and responsive)
        if (_isDragging)
        {
            if (_draggedThisFrame)
            {
                _draggedThisFrame = false;
            }
            else
            {
                _velocity = Vector2.Lerp(_velocity, Vector2.zero, 0.22f);
            }
        }
    }

    /// <summary>
    /// GsiCosmicOrrerySystem에 의해 중앙 호출되는 물리 연산 틱입니다.
    /// </summary>
    public void UpdatePhysicsTick(float deltaTime)
    {
        if (_isDragging) return;

        // Apply speed clamp
        float currentSpeed = _velocity.magnitude;
        if (currentSpeed > MaxVelocity)
        {
            _velocity = _velocity.normalized * MaxVelocity;
            currentSpeed = MaxVelocity;
        }

        // Apply friction only above MinDriftSpeed
        if (currentSpeed > MinDriftSpeed)
        {
            float newSpeed = currentSpeed * Mathf.Exp(-Friction * deltaTime);
            newSpeed = Mathf.Max(newSpeed, MinDriftSpeed);
            _velocity = _velocity.normalized * newSpeed;
        }

        // Hook for subclass-specific per-frame forces (e.g. WhiteHole repulsion)
        ApplyAdditionalForces();

        // Update position
        _rectTransform.anchoredPosition += _velocity * deltaTime;

        // Bounce off boundaries
        HandleScreenBoundaries();
    }

    /// <summary>Override to apply per-frame forces (e.g. GSI WhiteHole repulsion). Called before position update.</summary>
    protected virtual void ApplyAdditionalForces() { }

    // ═══════════════════════════════════════════════════════════════
    // Star Collision System
    // ═══════════════════════════════════════════════════════════════

    // ─── ICosmicKineticObject 인터페이스 구현부 ───────────────────
    public RectTransform rectTransform => _rectTransform;
    public Vector2 velocity { get { return _velocity; } set { _velocity = value; } }
    public float collisionRadius => CollisionRadius;
    public bool isDragging => _isDragging;

    /// <summary>
    /// GsiCosmicOrrerySystem에 의해 호출되며 두 물리 객체 간의 탄성 구체 충돌을 해결합니다.
    /// </summary>
    public void ResolveCollisionWith(ICosmicKineticObject other)
    {
        if (other == null || other == this) return;

        // 충돌 대상이 고정된 데코레이션 아이템(GsiPlacedDeco)인 경우 즉시 충돌 무시 (Ghost 관통)
        if (other is GsiPlacedDeco deco && deco.IsLocked) return;

        RectTransform otherRt = other.rectTransform;
        if (otherRt == null || _rectTransform == null) return;

        float minDistance = CollisionRadius + other.collisionRadius;
        Vector2 diff = otherRt.anchoredPosition - _rectTransform.anchoredPosition;
        float distance = diff.magnitude;

        // Avoid division by zero if they are exactly on top of each other
        if (distance < 0.01f)
        {
            _rectTransform.anchoredPosition += new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            return;
        }

        if (distance < minDistance)
        {
            Vector2 normal = diff / distance;
            float overlap = minDistance - distance;

            // 1. Resolve overlap (push apart based on dragging state)
            float pushSelf = _isDragging ? 0f : (other.isDragging ? 1f : 0.5f);
            float pushOther = other.isDragging ? 0f : (_isDragging ? 1f : 0.5f);

            _rectTransform.anchoredPosition -= normal * overlap * pushSelf;
            otherRt.anchoredPosition += normal * overlap * pushOther;

            // 2. Resolve elastic impulse bounce
            Vector2 rv = other.velocity - _velocity;
            float velAlongNormal = Vector2.Dot(rv, normal);

            // Only resolve if they are moving towards each other
            if (velAlongNormal < 0f)
            {
                float restitution = 0.96f; // Elastic bounce coefficient
                float impulseScalar = -(1f + restitution) * velAlongNormal / 2f; // assumes equal mass

                Vector2 impulse = normal * impulseScalar;

                if (!_isDragging)
                {
                    _velocity -= impulse;
                }
                if (!other.isDragging)
                {
                    other.velocity += impulse;
                }

                // Play premium tactile collision sound based on relative velocity with cooldown
                float relativeSpeed = Mathf.Abs(velAlongNormal);
                if (relativeSpeed > 30f && Time.unscaledTime - _lastCollisionSoundTime > 0.15f)
                {
                    _lastCollisionSoundTime = Time.unscaledTime;
                    
                    if (other is StarNodeControllerBase otherStar)
                    {
                        otherStar._lastCollisionSoundTime = Time.unscaledTime;
                    }

                    float volumeScale = Mathf.Clamp(relativeSpeed / 500f, 0.15f, 0.75f);
                    if (CollisionSfx != null)
                    {
                        GsiAudio.PlaySfx(CollisionSfx, volumeScale);
                    }
                    else if (GsiUiSound.Settings != null)
                    {
                        // Dynamic fallback to Click (high speed) or Hover (low speed) UI sounds
                        AudioClip defaultClip = relativeSpeed > 180f ? GsiUiSound.Settings.PrimaryClick : GsiUiSound.Settings.Hover;
                        if (defaultClip != null)
                        {
                            GsiAudio.PlaySfx(defaultClip, volumeScale);
                        }
                    }
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Screen Boundary Bouncing
    // ═══════════════════════════════════════════════════════════════

    private void HandleScreenBoundaries()
    {
        if (transform.parent == null)
        {
            return;
        }

        var parentRt = (RectTransform)transform.parent;
        Rect parentRect = parentRt.rect;

        // If parent rect is zero-sized or too small (e.g. LobbyActionRow is 0x0), traverse up to find a valid ancestor
        if (parentRect.width < 10f || parentRect.height < 10f)
        {
            Transform curr = transform.parent;
            while (curr != null)
            {
                var rt = curr as RectTransform;
                if (rt != null && rt.rect.width >= 100f && rt.rect.height >= 100f)
                {
                    Vector3 ancestorLocalCenter = parentRt.InverseTransformPoint(rt.transform.position);
                    parentRect = new Rect(
                        ancestorLocalCenter.x - rt.rect.width * 0.5f,
                        ancestorLocalCenter.y - rt.rect.height * 0.5f,
                        rt.rect.width,
                        rt.rect.height
                    );
                    break;
                }
                curr = curr.parent;
            }
        }

        float marginX = 64f;
        float marginY_min = MarginYMin;
        float marginY_max = MarginYMax;

        float minX = parentRect.xMin + marginX;
        float maxX = parentRect.xMax - marginX;
        float minY = parentRect.yMin + marginY_min;
        float maxY = parentRect.yMax - marginY_max;

        Vector2 pos = _rectTransform.anchoredPosition;

        bool bouncedX = false;
        bool bouncedY = false;

        if (pos.x < minX)
        {
            pos.x = minX;
            _velocity.x = -_velocity.x * BounceFactor;
            bouncedX = true;
        }
        else if (pos.x > maxX)
        {
            pos.x = maxX;
            _velocity.x = -_velocity.x * BounceFactor;
            bouncedX = true;
        }

        if (pos.y < minY)
        {
            pos.y = minY;
            _velocity.y = -_velocity.y * BounceFactor;
            bouncedY = true;
        }
        else if (pos.y > maxY)
        {
            pos.y = maxY;
            _velocity.y = -_velocity.y * BounceFactor;
            bouncedY = true;
        }

        if (bouncedX || bouncedY)
        {
            _rectTransform.anchoredPosition = pos;

            // Add a tiny random jitter on bounce to prevent infinite straight loops
            _velocity += new Vector2(Random.Range(-15f, 15f), Random.Range(-15f, 15f));
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Tooltip & Visual Construction
    // ═══════════════════════════════════════════════════════════════

    private void BuildTooltipLabel()
    {
        var labelGo = new GameObject("TooltipLabel", typeof(RectTransform), typeof(CanvasGroup));
        labelGo.transform.SetParent(transform, false);

        var rt = labelGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, TooltipOffsetY);
        rt.sizeDelta = new Vector2(280f, 36f);

        _labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        if (TmpFontCache.LiberationSansSdf != null)
        {
            _labelTmp.font = TmpFontCache.LiberationSansSdf;
        }
        _labelTmp.text = ButtonLabelText;
        _labelTmp.fontSize = TooltipFontSize;
        _labelTmp.characterSpacing = TooltipCharSpacing;
        _labelTmp.fontStyle = FontStyles.Bold;
        _labelTmp.alignment = TextAlignmentOptions.Center;
        _labelTmp.color = TooltipColor;
        _labelTmp.raycastTarget = false;

        _labelCanvasGroup = labelGo.GetComponent<CanvasGroup>();
        _labelCanvasGroup.alpha = 0f;
        _labelCanvasGroup.interactable = false;
        _labelCanvasGroup.blocksRaycasts = false;
    }

    /// <summary>Override to customize star visual layers. Base builds the visual root container.</summary>
    protected virtual void BuildStarVisuals()
    {
        var visualGo = new GameObject("StarVisualRoot", typeof(RectTransform));
        visualGo.transform.SetParent(transform, false);
        _starVisualRoot = visualGo.GetComponent<RectTransform>();
        _starVisualRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _starVisualRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _starVisualRoot.pivot = new Vector2(0.5f, 0.5f);
        _starVisualRoot.anchoredPosition = Vector2.zero;
        _starVisualRoot.sizeDelta = new Vector2(16f, 16f);
    }

    /// <summary>Creates a single star visual layer as a child of the star visual root.</summary>
    protected void CreateStarLayer(string layerName, float w, float h, float rotZ, Color color)
    {
        var go = new GameObject(layerName, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(_starVisualRoot, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(w, h);
        rt.localRotation = Quaternion.Euler(0f, 0f, rotZ);

        var img = go.GetComponent<Image>();
        img.sprite = null;
        img.color = color;
        img.raycastTarget = false;
    }

    // ═══════════════════════════════════════════════════════════════
    // Pointer Event Handlers
    // ═══════════════════════════════════════════════════════════════

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        if (_hoverRoutine != null)
        {
            StopCoroutine(_hoverRoutine);
        }
        _hoverRoutine = StartCoroutine(CoAnimateHover(1f));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        if (_hoverRoutine != null)
        {
            StopCoroutine(_hoverRoutine);
        }
        _hoverRoutine = StartCoroutine(CoAnimateHover(0f));
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (_starVisualRoot != null)
        {
            float s = PointerDownSquash;
            _starVisualRoot.localScale = new Vector3(s, s, s);
        }
        _dragStartPos = eventData.position;
        _totalDragDist = 0f;
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        if (_starVisualRoot != null)
        {
            float hs = HoverScale;
            _starVisualRoot.localScale = _isHovered ? new Vector3(hs, hs, hs) : Vector3.one;
        }

        // Only invoke click if the user didn't drag it significantly
        if (_totalDragDist < 12f)
        {
            OnStarClicked();
        }
    }

    /// <summary>Override to define what happens when the star is clicked (not dragged).</summary>
    protected virtual void OnStarClicked() { }

    private IEnumerator CoAnimateHover(float target)
    {
        float startT = _currentHoverT;
        float duration = 0.18f;
        float elapsed = 0f;
        float hs = HoverScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float curve = t * t * (3f - 2f * t); // Smoothstep curve
            _currentHoverT = Mathf.Lerp(startT, target, curve);

            if (_labelCanvasGroup != null)
            {
                _labelCanvasGroup.alpha = _currentHoverT;
            }

            if (_starVisualRoot != null)
            {
                float targetScale = Mathf.Lerp(1.0f, hs, _currentHoverT);
                _starVisualRoot.localScale = new Vector3(targetScale, targetScale, targetScale);
            }

            yield return null;
        }

        _currentHoverT = target;
        if (_labelCanvasGroup != null)
        {
            _labelCanvasGroup.alpha = target;
        }
        if (_starVisualRoot != null)
        {
            float finalScale = Mathf.Lerp(1.0f, hs, target);
            _starVisualRoot.localScale = new Vector3(finalScale, finalScale, finalScale);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Drag Event Handlers
    // ═══════════════════════════════════════════════════════════════

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _velocity = Vector2.zero;
        _draggedThisFrame = false;
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        _totalDragDist += eventData.delta.magnitude;

        Canvas canvas = GetComponentInParent<Canvas>();
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;

        // Move position based on scaled pointer movement
        Vector2 delta = eventData.delta / scaleFactor;

        // Zoom Sensitivity Compensation
        if (_scaleReferenceParent == null || (_scaleReferenceParent.name != "LobbyCenterStage" && _scaleReferenceParent.name != "GsiCosmicStage"))
        {
            Transform curr = transform.parent;
            while (curr != null)
            {
                if (curr.name == "LobbyCenterStage" || curr.name == "GsiCosmicStage")
                {
                    _scaleReferenceParent = curr;
                    break;
                }
                curr = curr.parent;
            }
        }

        if (_scaleReferenceParent != null)
        {
            delta.x /= _scaleReferenceParent.localScale.x;
            delta.y /= _scaleReferenceParent.localScale.y;
        }

        _rectTransform.anchoredPosition += delta;

        // Calculate dynamic velocity directly from drag event
        _draggedThisFrame = true;
        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.001f);
        Vector2 frameVelocity = delta / dt;
        _velocity = Vector2.Lerp(_velocity, frameVelocity, 0.22f);
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        SaveState();
    }

    // ═══════════════════════════════════════════════════════════════
    // State Save / Load (PlayerPrefs persistence)
    // ═══════════════════════════════════════════════════════════════

    protected virtual void OnDestroy()
    {
        if (GsiCosmicOrrerySystem.Instance != null)
        {
            GsiCosmicOrrerySystem.Instance.UnregisterStarNode(this);
        }
        SaveState();
    }

    private void OnApplicationQuit()
    {
        SaveState();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveState();
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveState();
        }
    }

    public void SaveState()
    {
        if (_rectTransform == null) return;

        string nameKey = gameObject.name;
        if (string.IsNullOrEmpty(nameKey)) return;

        string prefix = SaveKeyPrefix + nameKey;
        Vector2 pos = _rectTransform.anchoredPosition;

        GsiSaveSystem.SetFloat(prefix + "_PosX", pos.x);
        GsiSaveSystem.SetFloat(prefix + "_PosY", pos.y);
        GsiSaveSystem.SetFloat(prefix + "_VelX", _velocity.x);
        GsiSaveSystem.SetFloat(prefix + "_VelY", _velocity.y);
        GsiSaveSystem.SetInt(prefix + "_HasState", 1);
        GsiSaveSystem.Save();
    }

    private void LoadState()
    {
        if (_rectTransform == null) return;

        string nameKey = gameObject.name;
        if (string.IsNullOrEmpty(nameKey)) return;

        string prefix = SaveKeyPrefix + nameKey;
        if (GsiSaveSystem.GetInt(prefix + "_HasState", 0) == 1)
        {
            float px = GsiSaveSystem.GetFloat(prefix + "_PosX", InitialPosition.x);
            float py = GsiSaveSystem.GetFloat(prefix + "_PosY", InitialPosition.y);
            float vx = GsiSaveSystem.GetFloat(prefix + "_VelX", _velocity.x);
            float vy = GsiSaveSystem.GetFloat(prefix + "_VelY", _velocity.y);

            _rectTransform.anchoredPosition = new Vector2(px, py);
            _velocity = new Vector2(vx, vy);
        }
    }
}
