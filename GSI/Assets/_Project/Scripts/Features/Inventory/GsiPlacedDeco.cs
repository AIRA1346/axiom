using System;
using UnityEngine;
using UnityEngine.UI;
using ArchE.Game;

/// <summary>
/// 배경에 배치된 개별 데코 아이템의 관성 유영, 탄성 충돌, 경계면 반사 및 비주얼 제어를 담당하는 물리 천체 컴포넌트
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class GsiPlacedDeco : MonoBehaviour, ICosmicKineticObject
{
    public string ItemId { get; private set; }
    public Vector2 NormalizedPos { get; set; }

    private RectTransform _rectTransform;
    private Canvas _parentCanvas;
    private bool _isDragging = false;
    private Vector2 _velocity;

    // 애니메이션 제어용 프라이빗 캐시
    private Transform _visualRoot;
    private Transform _glowLayer;
    private Transform _spikeV;
    private Transform _spikeH;
    private float _randomPhaseOffset;

    // ─── ICosmicKineticObject 인터페이스 구현부 ───────────────────
    public RectTransform rectTransform => _rectTransform;
    public Vector2 velocity { get { return _velocity; } set { _velocity = value; } }
    public float collisionRadius => 14f; // 배치 아이템 충돌 반경
    public bool isDragging => _isDragging;

    public void Initialize(string itemId, Vector2 normalizedPos, Canvas canvas)
    {
        ItemId = itemId;
        NormalizedPos = normalizedPos;
        _parentCanvas = canvas;
        _rectTransform = GetComponent<RectTransform>();
        _randomPhaseOffset = UnityEngine.Random.Range(0f, 100f);

        // 1. 히트박스 영역 비활성화 (Bypass 터치를 타므로 uGUI 레이캐스트는 차단)
        var hitImg = GetComponent<Image>();
        if (hitImg == null) hitImg = gameObject.AddComponent<Image>();
        hitImg.sprite = null;
        hitImg.color = Color.clear;
        hitImg.raycastTarget = false;
        _rectTransform.sizeDelta = new Vector2(40f, 40f);

        // 2. 비주얼 루트 콘테이너 생성
        var visualGo = new GameObject("VisualRoot", typeof(RectTransform));
        _visualRoot = visualGo.transform;
        _visualRoot.SetParent(transform, false);
        var visualRt = visualGo.GetComponent<RectTransform>();
        visualRt.anchorMin = new Vector2(0.5f, 0.5f);
        visualRt.anchorMax = new Vector2(0.5f, 0.5f);
        visualRt.pivot = new Vector2(0.5f, 0.5f);
        visualRt.anchoredPosition = Vector2.zero;
        visualRt.sizeDelta = new Vector2(24f, 24f);

        // Z-position 및 스케일 초기화
        _rectTransform.localPosition = new Vector3(_rectTransform.localPosition.x, _rectTransform.localPosition.y, 0f);
        _rectTransform.localScale = Vector3.one;

        // 3. 등급 정의 로드 및 비주얼 그리기
        if (PlayerDecorations.TryGetItemDef(ItemId, out var def))
        {
            BuildProceduralVisuals(def);
        }
    }

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        
        // Orrery System에 등록
        if (GsiCosmicOrrerySystem.Instance != null)
        {
            GsiCosmicOrrerySystem.Instance.RegisterStarNode(this);
        }

        // 초기 자율 표류를 위한 느린 속도 인가
        _velocity = new Vector2(UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(-40f, 40f));
    }

    private void BuildProceduralVisuals(PlayerDecorations.DecoItemDef def)
    {
        Color color = def.DefaultColor;

        if (def.ProceduralShape == "star")
        {
            var glow = CreateLayer("AuraGlow", 15f, 15f, 45f, new Color(color.r, color.g, color.b, 0.35f));
            _glowLayer = glow.transform;

            var spV = CreateLayer("SpikeV", 1.8f, 28f, 0f, new Color(color.r, color.g, color.b, 0.95f));
            _spikeV = spV.transform;

            var spH = CreateLayer("SpikeH", 28f, 1.8f, 0f, new Color(color.r, color.g, color.b, 0.95f));
            _spikeH = spH.transform;

            CreateLayer("CoreDiamond", 7f, 7f, 45f, new Color(1f, 1f, 0.96f, 0.98f));
        }
        else if (def.ProceduralShape == "crystal")
        {
            var glow = CreateLayer("AuraGlow", 13f, 13f, 45f, new Color(color.r, color.g, color.b, 0.3f));
            _glowLayer = glow.transform;

            CreateLayer("CrystalOuter", 12f, 20f, 45f, new Color(color.r, color.g, color.b, 0.85f));
            CreateLayer("CrystalCore", 6f, 10f, 45f, new Color(1f, 1f, 1f, 0.95f));
        }
        else if (def.ProceduralShape == "ring")
        {
            var ringOuter = CreateLayer("RingOuter", 22f, 22f, 0f, new Color(color.r, color.g, color.b, 0.85f));
            _glowLayer = ringOuter.transform;
            
            CreateLayer("RingHole", 16f, 16f, 0f, new Color(0.04f, 0.04f, 0.06f, 1f));
            CreateLayer("CoreDot", 5f, 5f, 0f, Color.white);
        }
    }

    private GameObject CreateLayer(string name, float w, float h, float rotZ, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(_visualRoot, false);
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
        return go;
    }

    private void Update()
    {
        float time = Time.unscaledTime + _randomPhaseOffset;

        // 아이템별 고유 로컬 연출 애니메이션 (물리 이동 루프와 간섭되지 않도록 비주얼 루트에만 한정 적용)
        if (ItemId == "deco_yellow_star")
        {
            if (_visualRoot != null)
            {
                _visualRoot.localRotation = Quaternion.Euler(0f, 0f, time * 10f);
            }
            if (_glowLayer != null)
            {
                float pulse = 0.8f + Mathf.PingPong(time * 0.4f, 0.3f);
                _glowLayer.localScale = new Vector3(pulse, pulse, 1f);
            }
            if (_spikeV != null && _spikeH != null)
            {
                float spPulse = 0.85f + Mathf.PingPong(time * 1.5f, 0.25f);
                _spikeV.localScale = new Vector3(1f, spPulse, 1f);
                _spikeH.localScale = new Vector3(spPulse, 1f, 1f);
            }
        }
        else if (ItemId == "deco_purple_crystal")
        {
            // 부유 물리 운동을 자식 비주얼 루트에 국한하여 물리 충돌 궤적 왜곡 방지
            if (_visualRoot != null)
            {
                float floatOffset = Mathf.Sin(time * 1.8f) * 6f;
                _visualRoot.localPosition = new Vector3(0f, floatOffset, 0f);
            }
            if (_glowLayer != null)
            {
                float pulse = 0.85f + Mathf.Sin(time * 2.2f) * 0.15f;
                _glowLayer.localScale = new Vector3(pulse, pulse, 1f);
            }
        }
        else if (ItemId == "deco_neon_ring")
        {
            if (_glowLayer != null)
            {
                _glowLayer.Rotate(0f, 0f, -40f * Time.unscaledDeltaTime);
            }
        }
    }

    // ─── ICosmicKineticObject 물리 연산 처리부 ───────────────────────

    public void UpdatePhysicsTick(float deltaTime)
    {
        if (_isDragging) return;

        float currentSpeed = _velocity.magnitude;
        float maxVelocity = 1100f; // 데코 전용 속도 제약
        float minDriftSpeed = 20f;
        float friction = 0.35f;

        if (currentSpeed > maxVelocity)
        {
            _velocity = _velocity.normalized * maxVelocity;
            currentSpeed = maxVelocity;
        }

        // 마찰력 적용
        if (currentSpeed > minDriftSpeed)
        {
            float newSpeed = currentSpeed * Mathf.Exp(-friction * deltaTime);
            newSpeed = Mathf.Max(newSpeed, minDriftSpeed);
            _velocity = _velocity.normalized * newSpeed;
        }

        // 좌표 갱신
        _rectTransform.anchoredPosition += _velocity * deltaTime;

        // 경계면 충돌 반사
        HandleScreenBoundaries();
    }

    public void ResolveCollisionWith(ICosmicKineticObject other)
    {
        if (other == null || other == this) return;

        RectTransform otherRt = other.rectTransform;
        if (otherRt == null || _rectTransform == null) return;

        float minDistance = collisionRadius + other.collisionRadius;
        Vector2 diff = otherRt.anchoredPosition - _rectTransform.anchoredPosition;
        float distance = diff.magnitude;

        if (distance < 0.01f)
        {
            _rectTransform.anchoredPosition += new Vector2(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-5f, 5f));
            return;
        }

        if (distance < minDistance)
        {
            Vector2 normal = diff / distance;
            float overlap = minDistance - distance;

            // 1. 밀어내기 (겹침 강제 분리)
            float pushSelf = _isDragging ? 0f : (other.isDragging ? 1f : 0.5f);
            float pushOther = other.isDragging ? 0f : (_isDragging ? 1f : 0.5f);

            _rectTransform.anchoredPosition -= normal * overlap * pushSelf;
            otherRt.anchoredPosition += normal * overlap * pushOther;

            // 2. 탄성 튕김 속도 전달
            Vector2 rv = other.velocity - _velocity;
            float velAlongNormal = Vector2.Dot(rv, normal);

            if (velAlongNormal < 0f)
            {
                float restitution = 0.95f;
                float impulseScalar = -(1f + restitution) * velAlongNormal / 2f;
                Vector2 impulse = normal * impulseScalar;

                if (!_isDragging)
                {
                    _velocity -= impulse;
                }
                if (!other.isDragging)
                {
                    other.velocity += impulse;
                }
            }
        }
    }

    private void HandleScreenBoundaries()
    {
        if (transform.parent == null) return;

        var parentRt = (RectTransform)transform.parent;
        Rect parentRect = parentRt.rect;

        // 최초 1프레임 부모 Canvas 앵커 정렬 지연(크기 미정) 시 물리 이탈 방지
        if (parentRect.width < 100f || parentRect.height < 100f) return;

        float marginX = 40f;
        float marginY_min = 180f + 16f; // 하단 패널 높이만큼 충돌 반사 가로막 지정
        float marginY_max = 64f;

        float minX = parentRect.xMin + marginX;
        float maxX = parentRect.xMax - marginX;
        float minY = parentRect.yMin + marginY_min;
        float maxY = parentRect.yMax - marginY_max;

        Vector2 pos = _rectTransform.anchoredPosition;
        float bounceFactor = 0.9f;
        bool bounced = false;

        if (pos.x < minX)
        {
            pos.x = minX;
            _velocity.x = -_velocity.x * bounceFactor;
            bounced = true;
        }
        else if (pos.x > maxX)
        {
            pos.x = maxX;
            _velocity.x = -_velocity.x * bounceFactor;
            bounced = true;
        }

        if (pos.y < minY)
        {
            pos.y = minY;
            _velocity.y = -_velocity.y * bounceFactor;
            bounced = true;
        }
        else if (pos.y > maxY)
        {
            pos.y = maxY;
            _velocity.y = -_velocity.y * bounceFactor;
            bounced = true;
        }

        if (bounced)
        {
            _rectTransform.anchoredPosition = pos;
            _velocity += new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f));
        }
    }

    public void SetDragging(bool dragging)
    {
        _isDragging = dragging;
        if (dragging)
        {
            _velocity = Vector2.zero;
        }
    }

    private void OnDestroy()
    {
        if (GsiCosmicOrrerySystem.Instance != null)
        {
            GsiCosmicOrrerySystem.Instance.UnregisterStarNode(this);
        }
    }
}
