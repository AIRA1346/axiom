using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Controls a lobby button transformed into a floating, draggable star with kinetic physics,
/// boundary bouncing, hover micro-animations, and a fading tooltip label.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class GsiLobbyStarNodeController : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Visual Config")]
    public Color StarColor = Color.white;
    public string ButtonLabelText = "Button";
    public Vector2 InitialPosition = Vector2.zero;

    [Header("Physics Config")]
    public float Friction = 0.45f;
    public float BounceFactor = 0.92f;
    public float MaxVelocity = 1800f;
    
    private RectTransform _rectTransform;
    private RectTransform _starVisualRoot;
    private CanvasGroup _labelCanvasGroup;
    private TextMeshProUGUI _labelTmp;

    private Vector2 _velocity;
    private bool _isDragging = false;
    private bool _isHovered = false;
    private float _currentHoverT = 0f;
    private Coroutine _hoverRoutine;

    // Track drag movement to separate drags from pure clicks
    private Vector2 _dragStartPos;
    private float _totalDragDist;

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();

        // 1. Force the rect size to make standard interaction area comfortable (120x120)
        _rectTransform.sizeDelta = new Vector2(120f, 120f);
        _rectTransform.anchoredPosition = InitialPosition;

        // 2. Clear original background image, but keep it active as click target
        if (TryGetComponent(out Image img))
        {
            img.sprite = null;
            img.color = Color.clear;
            img.raycastTarget = true;
        }

        // 3. Clear or hijack original text component & rule lines
        var rulesTf = transform.Find("LobbyLabelRules");
        if (rulesTf != null)
        {
            Destroy(rulesTf.gameObject);
        }

        var originalText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (originalText != null && originalText.gameObject != gameObject)
        {
            Destroy(originalText.gameObject);
        }

        // 4. Build tooltip label dynamically underneath
        BuildTooltipLabel();

        // 5. Build premium procedural star layers
        BuildStarVisuals();

        // 6. Give a small random initial drift velocity
        _velocity = new Vector2(Random.Range(-90f, 90f), Random.Range(-90f, 90f));
    }

    private void BuildTooltipLabel()
    {
        var labelGo = new GameObject("TooltipLabel", typeof(RectTransform), typeof(CanvasGroup));
        labelGo.transform.SetParent(transform, false);

        var rt = labelGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -46f); // Tooltip placed underneath
        rt.sizeDelta = new Vector2(280f, 36f);

        _labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        if (TmpFontCache.LiberationSansSdf != null)
        {
            _labelTmp.font = TmpFontCache.LiberationSansSdf;
        }
        _labelTmp.text = ButtonLabelText;
        _labelTmp.fontSize = 20f;
        _labelTmp.characterSpacing = 0.35f;
        _labelTmp.fontStyle = FontStyles.Bold;
        _labelTmp.alignment = TextAlignmentOptions.Center;
        _labelTmp.color = Color.white;
        _labelTmp.raycastTarget = false;

        _labelCanvasGroup = labelGo.GetComponent<CanvasGroup>();
        _labelCanvasGroup.alpha = 0f;
        _labelCanvasGroup.interactable = false;
        _labelCanvasGroup.blocksRaycasts = false;
    }

    private void BuildStarVisuals()
    {
        var visualGo = new GameObject("StarVisualRoot", typeof(RectTransform));
        visualGo.transform.SetParent(transform, false);
        _starVisualRoot = visualGo.GetComponent<RectTransform>();
        _starVisualRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _starVisualRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _starVisualRoot.pivot = new Vector2(0.5f, 0.5f);
        _starVisualRoot.anchoredPosition = Vector2.zero;
        _starVisualRoot.sizeDelta = new Vector2(32f, 32f);

        // Layer 1: Aura Outer Glow (Vibrant background bloom)
        CreateStarLayer("AuraGlow", 40f, 40f, 45f, new Color(StarColor.r, StarColor.g, StarColor.b, 0.35f));

        // Layer 2: Secondary soft ring glow
        CreateStarLayer("InnerRingGlow", 24f, 24f, 0f, new Color(StarColor.r, StarColor.g, StarColor.b, 0.15f));

        // Layer 3: Vertical Flare Spike
        CreateStarLayer("SpikeV", 3.5f, 52f, 0f, new Color(StarColor.r, StarColor.g, StarColor.b, 0.95f));

        // Layer 4: Horizontal Flare Spike
        CreateStarLayer("SpikeH", 52f, 3.5f, 0f, new Color(StarColor.r, StarColor.g, StarColor.b, 0.95f));

        // Layer 5: Sparkling Diamond Core (Always bright white/light center)
        CreateStarLayer("CoreDiamond", 13f, 13f, 45f, new Color(1f, 1.0f, 0.96f, 0.98f));
    }

    private void CreateStarLayer(string layerName, float w, float h, float rotZ, Color color)
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
        img.sprite = null; // Procedural vector-like rendering using solid square graphic
        img.color = color;
        img.raycastTarget = false;
    }

    private void Update()
    {
        // 1. Slow, elegant rotation for active star
        if (_starVisualRoot != null)
        {
            if (_isHovered)
            {
                float rotSpeed = 48f; // Faster spin on hover
                _starVisualRoot.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * rotSpeed);
            }
            else
            {
                float driftSpeed = 12f; // Slow background drift rotation
                _starVisualRoot.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * driftSpeed);
            }
        }

        // 2. Kinetic physics update loop
        if (!_isDragging)
        {
            // Apply speed clamp
            if (_velocity.magnitude > MaxVelocity)
            {
                _velocity = _velocity.normalized * MaxVelocity;
            }

            // Apply friction
            _velocity *= Mathf.Exp(-Friction * Time.unscaledDeltaTime);

            // Update position
            _rectTransform.anchoredPosition += _velocity * Time.unscaledDeltaTime;

            // Bounce off boundaries
            HandleScreenBoundaries();
        }

        // 3. Resolve star-to-star collisions!
        HandleStarCollisions();
    }

    private void HandleStarCollisions()
    {
        if (transform.parent == null)
        {
            return;
        }

        float collisionRadius = 46f;
        float minDistance = collisionRadius * 2f;

        // Retrieve all active star nodes in the same canvas container
        var otherStars = transform.parent.GetComponentsInChildren<GsiLobbyStarNodeController>();
        foreach (var other in otherStars)
        {
            if (other == this)
            {
                continue;
            }

            Vector2 diff = other.GetComponent<RectTransform>().anchoredPosition - _rectTransform.anchoredPosition;
            float distance = diff.magnitude;

            // Avoid division by zero if they are exactly on top of each other
            if (distance < 0.01f)
            {
                _rectTransform.anchoredPosition += new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
                continue;
            }

            if (distance < minDistance)
            {
                Vector2 normal = diff / distance;
                float overlap = minDistance - distance;

                // 1. Resolve overlap (push apart based on dragging state)
                float pushSelf = _isDragging ? 0f : (other._isDragging ? 1f : 0.5f);
                float pushOther = other._isDragging ? 0f : (_isDragging ? 1f : 0.5f);

                _rectTransform.anchoredPosition -= normal * overlap * pushSelf;
                other.GetComponent<RectTransform>().anchoredPosition += normal * overlap * pushOther;

                // 2. Resolve elastic impulse bounce
                Vector2 rv = other._velocity - _velocity;
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
                    if (!other._isDragging)
                    {
                        other._velocity += impulse;
                    }
                }
            }
        }
    }

    private void HandleScreenBoundaries()
    {
        if (transform.parent == null)
        {
            return;
        }

        var parentRt = (RectTransform)transform.parent;
        Rect parentRect = parentRt.rect;

        // Add margins to prevent stars from going off-screen (accounting for spikes & tooltip size)
        float marginX = 64f;
        float marginY_min = 80f; // More padding at bottom for tooltip
        float marginY_max = 64f;

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

    #region Pointer Event Handlers

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

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_starVisualRoot != null)
        {
            _starVisualRoot.localScale = new Vector3(0.85f, 0.85f, 0.85f); // Squash on click down
        }
        _dragStartPos = eventData.position;
        _totalDragDist = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_starVisualRoot != null)
        {
            _starVisualRoot.localScale = _isHovered ? new Vector3(1.4f, 1.4f, 1.4f) : Vector3.one;
        }

        // Only invoke click if the user didn't drag it significantly
        if (_totalDragDist < 12f)
        {
            if (TryGetComponent(out Button btn))
            {
                btn.onClick.Invoke();
            }
        }
    }

    private IEnumerator CoAnimateHover(float target)
    {
        float startT = _currentHoverT;
        float duration = 0.18f;
        float elapsed = 0f;

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
                float targetScale = Mathf.Lerp(1.0f, 1.4f, _currentHoverT);
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
            float finalScale = Mathf.Lerp(1.0f, 1.4f, target);
            _starVisualRoot.localScale = new Vector3(finalScale, finalScale, finalScale);
        }
    }

    #endregion

    #region Drag Event Handlers

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _velocity = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _totalDragDist += eventData.delta.magnitude;

        Canvas canvas = GetComponentInParent<Canvas>();
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;

        // Move position based on scaled pointer movement
        Vector2 delta = eventData.delta / scaleFactor;
        _rectTransform.anchoredPosition += delta;

        // Calculate dynamic velocity smoothing to make throwing/flicking feel natural
        Vector2 frameVelocity = delta / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
        _velocity = Vector2.Lerp(_velocity, frameVelocity, 0.35f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
    }

    #endregion
}
