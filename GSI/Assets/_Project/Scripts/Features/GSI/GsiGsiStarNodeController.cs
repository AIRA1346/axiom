using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// GSI 우주 Orrery 성계에서 개별 미니게임을 나타내는 4포인트 네온 별 노드 컨트롤러.
/// 메인 로비 별들과 100% 동일한 고급 물리 법칙(관성 드래그, 탄성 충돌, 화이트홀 척력, 햅틱 효과음)을 따릅니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class GsiGsiStarNodeController : MonoBehaviour, 
    IPointerEnterHandler, 
    IPointerExitHandler, 
    IPointerDownHandler, 
    IPointerUpHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public TestMode Mode;
    public Color StarColor = Color.white;
    public string ButtonLabelText = "Practice";
    public Vector2 InitialPosition = Vector2.zero;

    [Header("Physics Config")]
    public float Friction = 0.45f;
    public float BounceFactor = 0.92f;
    public float MaxVelocity = 1800f;
    public float MinDriftSpeed = 80f;
    public float CollisionRadius = 30f; // 중심부 코어가 알맞게 겹친 상태에서 충돌하도록 작게 설정

    [Header("Audio Config")]
    [Tooltip("연습 별 충돌 시 재생되는 효과음. 미지정 시 시스템 UI 사운드가 지능형 대체 작동합니다.")]
    public AudioClip CollisionSfx;

    public Action<Vector2> OnBeginDragAction;
    public Action<Vector2> OnDragAction;
    public Action OnEndDragAction;
    public Action<GsiGsiStarNodeController> OnClickedAction;
    public Action OnPointerDownAction;
    public Action OnPointerUpAction;

    private RectTransform _rectTransform;
    private RectTransform _starVisualRoot;
    private CanvasGroup _labelCanvasGroup;
    private TextMeshProUGUI _labelTmp;
    
    private Vector2 _velocity;
    private bool _isDragging = false;
    private bool _isHovered = false;
    private float _currentHoverT = 0f;
    private Coroutine _hoverRoutine;

    // 드래그 속도 측정 및 정지 지터 노이즈 방지 필터링
    private Vector2 _dragStartPos;
    private float _totalDragDist;
    private Vector2 _lastDragFramePos;
    private float _lastCollisionSoundTime = 0f;

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

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();

        // 1. 편안한 조작 영역 크기 강제 설정 (120x120)
        _rectTransform.sizeDelta = new Vector2(120f, 120f);
        _rectTransform.anchoredPosition = InitialPosition;

        // 2. 기존 사각형 백그라운드 이미지 완전 투명화 및 터치 영역 보장
        if (TryGetComponent(out Image img))
        {
            img.sprite = null;
            img.color = Color.clear;
            img.raycastTarget = true;
        }

        // 3. 기존 자식 텍스트 제거 후 툴팁 라벨화
        var originalText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (originalText != null && originalText.gameObject != gameObject)
        {
            Destroy(originalText.gameObject);
        }

        BuildTooltipLabel();

        // 4. 프리미엄 4포인트 네온 별 비주얼 동적 생성
        BuildStarVisuals();

        // 5. 초기 자율 영동 유영 속도 부여
        _velocity = new Vector2(UnityEngine.Random.Range(-90f, 90f), UnityEngine.Random.Range(-90f, 90f));
    }

    private void BuildTooltipLabel()
    {
        var labelGo = new GameObject("TooltipLabel", typeof(RectTransform), typeof(CanvasGroup));
        labelGo.transform.SetParent(transform, false);

        var rt = labelGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -42f); // 툴팁 위치 밀착
        rt.sizeDelta = new Vector2(280f, 36f);

        _labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        if (TmpFontCache.LiberationSansSdf != null)
        {
            _labelTmp.font = TmpFontCache.LiberationSansSdf;
        }
        _labelTmp.text = ButtonLabelText;
        _labelTmp.fontSize = 18f;
        _labelTmp.characterSpacing = 0.5f;
        _labelTmp.fontStyle = FontStyles.Bold;
        _labelTmp.alignment = TextAlignmentOptions.Center;
        _labelTmp.color = StarColor;
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

        // Layer 1: Aura Outer Glow (은은한 백그라운드 오라)
        CreateStarLayer("OuterGlow", 38f, 38f, 45f, new Color(StarColor.r, StarColor.g, StarColor.b, 0.3f));

        // Layer 2: Spike Vertical (세로 나선 불꽃)
        CreateStarLayer("SpikeV", 3f, 48f, 0f, new Color(StarColor.r, StarColor.g, StarColor.b, 0.92f));

        // Layer 3: Spike Horizontal (가로 나선 불꽃)
        CreateStarLayer("SpikeH", 48f, 3f, 0f, new Color(StarColor.r, StarColor.g, StarColor.b, 0.92f));

        // Layer 4: Diamond Core (영롱한 중앙 화이트 코어)
        CreateStarLayer("CoreDiamond", 12f, 12f, 45f, new Color(1f, 1f, 1f, 0.98f));
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
        img.sprite = null;
        img.color = color;
        img.raycastTarget = false;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        // 1. 부드러운 성체 자전 운동
        if (_starVisualRoot != null)
        {
            float rotSpeed = _isHovered ? 48f : 12f;
            _starVisualRoot.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * rotSpeed);
        }

        // 2. 물리 관성 연산 루프
        if (!_isDragging)
        {
            float currentSpeed = _velocity.magnitude;
            if (currentSpeed > MaxVelocity)
            {
                _velocity = _velocity.normalized * MaxVelocity;
                currentSpeed = MaxVelocity;
            }

            // 항성 유영 속도 유지 마찰 저항
            if (currentSpeed > MinDriftSpeed)
            {
                float newSpeed = currentSpeed * Mathf.Exp(-Friction * Time.unscaledDeltaTime);
                newSpeed = Mathf.Max(newSpeed, MinDriftSpeed);
                _velocity = _velocity.normalized * newSpeed;
            }

            // 화이트홀 중심체에 대한 척력(밀어내는 힘) 장 적용
            ApplyWhiteHoleRepulsion();

            // 위치 반영
            _rectTransform.anchoredPosition += _velocity * Time.unscaledDeltaTime;

            // 스크린 경계 충돌 처리
            HandleScreenBoundaries();
        }
        else
        {
            // 드래그 중 속도 측정
            Vector2 currentPos = _rectTransform.anchoredPosition;
            Vector2 positionDelta = currentPos - _lastDragFramePos;
            Vector2 frameVelocity = positionDelta / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
            
            _velocity = Vector2.Lerp(_velocity, frameVelocity, 0.22f);
            _lastDragFramePos = currentPos;
        }

        // 3. 스타들 간의 탄성 구체 충돌 연산
        HandleStarCollisions();
    }

    private void ApplyWhiteHoleRepulsion()
    {
        if (transform.parent == null)
        {
            return;
        }

        // 부모 산하에서 화이트홀 탐색
        var whiteHole = transform.parent.GetComponentInChildren<GsiWhiteHoleNodeController>();
        if (whiteHole != null)
        {
            Vector2 whPos = whiteHole.Rect.anchoredPosition;
            Vector2 starPos = _rectTransform.anchoredPosition;
            Vector2 diff = starPos - whPos;
            float dist = diff.magnitude;

            const float repulsionOuterRadius = 240f;
            if (dist < repulsionOuterRadius && dist > 0.1f)
            {
                // 밀어내는 척력 강도 (가까워질수록 포물선 궤적으로 증폭)
                float factor = 1f - dist / repulsionOuterRadius;
                float force = factor * factor * 580f;

                _velocity += diff.normalized * force * Time.unscaledDeltaTime;
            }
        }
    }

    private void HandleStarCollisions()
    {
        if (transform.parent == null)
        {
            return;
        }

        float minDistance = CollisionRadius * 2f;
        var otherStars = transform.parent.GetComponentsInChildren<GsiGsiStarNodeController>();
        foreach (var other in otherStars)
        {
            if (other == this)
            {
                continue;
            }

            Vector2 diff = other.Rect.anchoredPosition - _rectTransform.anchoredPosition;
            float distance = diff.magnitude;

            if (distance < 0.01f)
            {
                _rectTransform.anchoredPosition += new Vector2(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-5f, 5f));
                continue;
            }

            if (distance < minDistance)
            {
                Vector2 normal = diff / distance;
                float overlap = minDistance - distance;

                // 겹침 해결
                float pushSelf = _isDragging ? 0f : (other._isDragging ? 1f : 0.5f);
                float pushOther = other._isDragging ? 0f : (_isDragging ? 1f : 0.5f);

                _rectTransform.anchoredPosition -= normal * overlap * pushSelf;
                other.Rect.anchoredPosition += normal * overlap * pushOther;

                // 운동량 탄성 교환
                Vector2 rv = other._velocity - _velocity;
                float velAlongNormal = Vector2.Dot(rv, normal);

                if (velAlongNormal < 0f)
                {
                    float restitution = 0.96f;
                    float impulseScalar = -(1f + restitution) * velAlongNormal / 2f;
                    Vector2 impulse = normal * impulseScalar;

                    if (!_isDragging)
                    {
                        _velocity -= impulse;
                    }
                    if (!other._isDragging)
                    {
                        other._velocity += impulse;
                    }

                    // 햅틱 사운드 피드백
                    float relativeSpeed = Mathf.Abs(velAlongNormal);
                    if (relativeSpeed > 30f && Time.unscaledTime - _lastCollisionSoundTime > 0.15f)
                    {
                        _lastCollisionSoundTime = Time.unscaledTime;
                        other._lastCollisionSoundTime = Time.unscaledTime;

                        float volumeScale = Mathf.Clamp(relativeSpeed / 500f, 0.15f, 0.75f);
                        if (CollisionSfx != null)
                        {
                            GsiAudio.PlaySfx(CollisionSfx, volumeScale);
                        }
                        else if (GsiUiSound.Settings != null)
                        {
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
    }

    private void HandleScreenBoundaries()
    {
        if (transform.parent == null)
        {
            return;
        }

        var parentRt = (RectTransform)transform.parent;
        Rect parentRect = parentRt.rect;

        float marginX = 64f;
        float marginY_min = 84f; // 툴팁 위치 고려 하단 여백 확대
        float marginY_max = 148f; // GSI 상단 헤더 스트립 영역 침범 차단

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
            _velocity += new Vector2(UnityEngine.Random.Range(-15f, 15f), UnityEngine.Random.Range(-15f, 15f));
        }
    }

    #region Pointer Event Handlers

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        if (_hoverRoutine != null) StopCoroutine(_hoverRoutine);
        _hoverRoutine = StartCoroutine(CoAnimateHover(1f));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        if (_hoverRoutine != null) StopCoroutine(_hoverRoutine);
        _hoverRoutine = StartCoroutine(CoAnimateHover(0f));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_starVisualRoot != null)
        {
            _starVisualRoot.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        }
        _dragStartPos = eventData.position;
        _totalDragDist = 0f;
        OnPointerDownAction?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_starVisualRoot != null)
        {
            _starVisualRoot.localScale = _isHovered ? new Vector3(1.35f, 1.35f, 1.35f) : Vector3.one;
        }
        OnPointerUpAction?.Invoke();

        if (_totalDragDist < 12f)
        {
            OnClickedAction?.Invoke(this);
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
            float curve = t * t * (3f - 2f * t); // Smoothstep
            _currentHoverT = Mathf.Lerp(startT, target, curve);

            if (_labelCanvasGroup != null)
            {
                _labelCanvasGroup.alpha = _currentHoverT;
            }

            if (_starVisualRoot != null)
            {
                float targetScale = Mathf.Lerp(1.0f, 1.35f, _currentHoverT);
                _starVisualRoot.localScale = new Vector3(targetScale, targetScale, targetScale);
            }

            yield return null;
        }

        _currentHoverT = target;
        if (_labelCanvasGroup != null) _labelCanvasGroup.alpha = target;
        if (_starVisualRoot != null)
        {
            float finalScale = Mathf.Lerp(1.0f, 1.35f, target);
            _starVisualRoot.localScale = new Vector3(finalScale, finalScale, finalScale);
        }
    }

    #endregion

    #region Drag Event Handlers

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _velocity = Vector2.zero;
        _lastDragFramePos = _rectTransform.anchoredPosition;
        OnBeginDragAction?.Invoke(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _totalDragDist += eventData.delta.magnitude;

        Canvas canvas = GetComponentInParent<Canvas>();
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
        Vector2 delta = eventData.delta / scaleFactor;
        _rectTransform.anchoredPosition += delta;

        OnDragAction?.Invoke(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        OnEndDragAction?.Invoke();
    }

    #endregion
}
