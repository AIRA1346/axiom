using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArchE.Game;

/// <summary>
/// 배경에 배치된 개별 데코 아이템의 관성 유영, 탄성 충돌, 경계면 반사 및 마우스 호버 반응 툴팁/스케일 연출을 제어하는 물리 컴포넌트
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class GsiPlacedDeco : MonoBehaviour, ICosmicKineticObject
{
    public string ItemId { get; private set; }
    public Vector2 NormalizedPos { get; set; }

    private bool _isLocked = false;
    public bool IsLocked
    {
        get { return _isLocked; }
        set
        {
            _isLocked = value;
            RefreshLockedVisualState();
        }
    }

    private RectTransform _rectTransform;
    private Canvas _parentCanvas;
    private CanvasGroup _selfCanvasGroup;
    private bool _isDragging = false;
    private Vector2 _velocity;
    private Vector2 _lastDragFramePos;

    public DecoBehavior Behavior { get; private set; }

    // 애니메이션 및 마우스 호버 피드백 캐시
    private Transform _visualRoot;
    private Transform _glowLayer;
    private Transform _spikeV;
    private Transform _spikeH;
    
    public Transform visualRoot => _visualRoot;
    public Transform glowLayer => _glowLayer;
    public Transform spikeV => _spikeV;
    public Transform spikeH => _spikeH;

    private float _randomPhaseOffset;

    private CanvasGroup _labelCanvasGroup;
    private TextMeshProUGUI _labelTmp;
    private float _currentScale = 1f;

    // ─── ICosmicKineticObject 인터페이스 구현부 ───────────────────
    public RectTransform rectTransform => _rectTransform;
    public Vector2 velocity { get { return _velocity; } set { _velocity = value; } }
    public float collisionRadius => ItemId == "deco_yellow_star" ? 2.8f : ((ItemId == "deco_purple_crystal" || ItemId == "deco_neon_ring") ? 14f / 3f : 14f); // 배치 아이템 충돌 반경 (노란색 별 1/5, 퍼플 크리스탈/네온 링 1/3 축소)
    public bool isDragging => _isDragging;

    public void Initialize(string itemId, Vector2 normalizedPos, Canvas canvas)
    {
        ItemId = itemId;
        NormalizedPos = normalizedPos;
        _parentCanvas = canvas;
        _rectTransform = GetComponent<RectTransform>();
        _randomPhaseOffset = UnityEngine.Random.Range(0f, 100f);

        _selfCanvasGroup = GetComponent<CanvasGroup>();
        if (_selfCanvasGroup == null) _selfCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 초기 자율 표류를 위한 느린 속도 인가 (기본값)
        _velocity = new Vector2(UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(-40f, 40f));

        // 1. 히트박스 크기 지정 (uGUI 레이캐스트 대신 RectTransform의 수동 경계 검사를 수행하므로 Image 컴포넌트 불필요)
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

        // 4. 툴팁 가이드 라벨 생성
        BuildTooltipLabel();

        // 5. 행동 전략 장착
        Behavior = DecoBehaviorFactory.Create(ItemId);
        Behavior.Initialize(this);
    }

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        
        if (_selfCanvasGroup == null)
        {
            _selfCanvasGroup = GetComponent<CanvasGroup>();
            if (_selfCanvasGroup == null) _selfCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Z-position 및 스케일 강제 리셋
        _rectTransform.localPosition = new Vector3(_rectTransform.localPosition.x, _rectTransform.localPosition.y, 0f);
        _rectTransform.localScale = Vector3.one;

        // Orrery System에 등록
        if (GsiCosmicOrrerySystem.Instance != null)
        {
            GsiCosmicOrrerySystem.Instance.RegisterStarNode(this);
        }

        // 초기 비주얼 락 상태 동기화
        RefreshLockedVisualState();
    }

    public void RefreshLockedVisualState()
    {
        if (_selfCanvasGroup != null)
        {
            _selfCanvasGroup.alpha = IsLocked ? 0.6f : 1.0f;
        }

        if (_labelTmp != null)
        {
            if (PlayerDecorations.TryGetItemDef(ItemId, out var def))
            {
                bool isKo = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale != null &&
                            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("ko", System.StringComparison.OrdinalIgnoreCase);
                
                string rarityName = PlayerDecorations.GetRarityName(def.Rarity, isKo);
                string displayName = GameLocalization.GetUiString(def.DisplayNameKey, def.EnglishName);
                
                if (IsLocked)
                {
                    string lockLabel = isKo ? "고정됨" : "LOCKED";
                    _labelTmp.text = $"[{rarityName}] {displayName} <color=#EF4444>[{lockLabel}]</color>";
                }
                else
                {
                    _labelTmp.text = $"[{rarityName}] {displayName}";
                }
            }
            else
            {
                _labelTmp.text = IsLocked ? $"{ItemId} <color=#EF4444>[LOCK]</color>" : ItemId;
            }
        }
    }

    private void BuildTooltipLabel()
    {
        var labelGo = new GameObject("TooltipLabel", typeof(RectTransform), typeof(CanvasGroup));
        labelGo.transform.SetParent(transform, false);

        var rt = labelGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 32f); // 데코 바로 위에 플로팅 노출
        rt.sizeDelta = new Vector2(200f, 30f);

        _labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        if (TmpFontCache.LiberationSansSdf != null)
        {
            _labelTmp.font = TmpFontCache.LiberationSansSdf;
        }

        // 아이템 정보 로드 및 로컬라이징 텍스트 적용
        if (PlayerDecorations.TryGetItemDef(ItemId, out var def))
        {
            bool isKo = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale != null &&
                        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("ko", System.StringComparison.OrdinalIgnoreCase);
            
            string rarityName = PlayerDecorations.GetRarityName(def.Rarity, isKo);
            string displayName = GameLocalization.GetUiString(def.DisplayNameKey, def.EnglishName);
            _labelTmp.text = $"[{rarityName}] {displayName}";
        }
        else
        {
            _labelTmp.text = ItemId;
        }

        _labelTmp.fontSize = 11f;
        _labelTmp.fontStyle = FontStyles.Bold;
        _labelTmp.alignment = TextAlignmentOptions.Center;
        _labelTmp.color = Color.white;
        _labelTmp.raycastTarget = false;

        _labelCanvasGroup = labelGo.GetComponent<CanvasGroup>();
        _labelCanvasGroup.alpha = 0f;
        _labelCanvasGroup.interactable = false;
        _labelCanvasGroup.blocksRaycasts = false;
    }

    private void BuildProceduralVisuals(PlayerDecorations.DecoItemDef def)
    {
        Color color = def.DefaultColor;

        if (def.ProceduralShape == "star")
        {
            float scaleMultiplier = ItemId == "deco_yellow_star" ? 0.2f : 1f;

            var glow = CreateLayer("AuraGlow", 15f * scaleMultiplier, 15f * scaleMultiplier, 45f, new Color(color.r, color.g, color.b, 0.35f));
            _glowLayer = glow.transform;

            var spV = CreateLayer("SpikeV", 1.8f * scaleMultiplier, 28f * scaleMultiplier, 0f, new Color(color.r, color.g, color.b, 0.95f));
            _spikeV = spV.transform;

            var spH = CreateLayer("SpikeH", 28f * scaleMultiplier, 1.8f * scaleMultiplier, 0f, new Color(color.r, color.g, color.b, 0.95f));
            _spikeH = spH.transform;

            CreateLayer("CoreDiamond", 7f * scaleMultiplier, 7f * scaleMultiplier, 45f, new Color(1f, 1f, 0.96f, 0.98f));
        }
        else if (def.ProceduralShape == "crystal")
        {
            float scaleMultiplier = ItemId == "deco_purple_crystal" ? (1f / 3f) : 1f;

            var glow = CreateLayer("AuraGlow", 13f * scaleMultiplier, 13f * scaleMultiplier, 45f, new Color(color.r, color.g, color.b, 0.3f));
            _glowLayer = glow.transform;

            CreateLayer("CrystalOuter", 12f * scaleMultiplier, 20f * scaleMultiplier, 45f, new Color(color.r, color.g, color.b, 0.85f));
            CreateLayer("CrystalCore", 6f * scaleMultiplier, 10f * scaleMultiplier, 45f, new Color(1f, 1f, 1f, 0.95f));
        }
        else if (def.ProceduralShape == "ring")
        {
            float scaleMultiplier = ItemId == "deco_neon_ring" ? (1f / 3f) : 1f;

            var ringOuter = CreateLayer("RingOuter", 22f * scaleMultiplier, 22f * scaleMultiplier, 0f, new Color(color.r, color.g, color.b, 0.85f));
            _glowLayer = ringOuter.transform;
            
            CreateLayer("RingHole", 16f * scaleMultiplier, 16f * scaleMultiplier, 0f, new Color(0.04f, 0.04f, 0.06f, 1f));
            CreateLayer("CoreDot", 5f * scaleMultiplier, 5f * scaleMultiplier, 0f, Color.white);
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

        // 1. 마우스 호버(오버) 수동 감지 (uGUI Sibling 차단 우회)
        bool isHovered = false;
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            Vector2 mousePos = mouse.position.ReadValue();
            Camera cam = _parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? _parentCanvas.worldCamera : null;
            
            // 드래그 중인 상태도 강제로 호버 피드백 활성 상태로 유지
            isHovered = _isDragging || RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, mousePos, cam);
        }

        // 2. 툴팁 가시성 부드러운 페이드인/아웃
        float targetAlpha = isHovered ? 1f : 0f;
        if (_labelCanvasGroup != null)
        {
            _labelCanvasGroup.alpha = Mathf.MoveTowards(_labelCanvasGroup.alpha, targetAlpha, 6f * Time.unscaledDeltaTime);
        }

        // 3. 스케일 및 찌그러짐(스쿼시) 반응 보간
        float targetScale = 1.0f;
        if (_isDragging)
        {
            targetScale = 0.85f; // 드래그 조작 시 찌그러지는 연출
        }
        else if (isHovered)
        {
            targetScale = IsLocked ? 1.0f : 1.35f; // 고정 시에는 호버 확대 스케일을 1.0으로 제한
        }

        _currentScale = Mathf.Lerp(_currentScale, targetScale, 16f * Time.unscaledDeltaTime);
        if (_visualRoot != null)
        {
            _visualRoot.localScale = new Vector3(_currentScale, _currentScale, 1f);
        }

        // Z-position 및 스케일 강제 설정 (3D 카메라 깊이 평면 탈출 및 스케일 왜곡 차단)
        if (_rectTransform != null)
        {
            _rectTransform.localPosition = new Vector3(_rectTransform.localPosition.x, _rectTransform.localPosition.y, 0f);
            _rectTransform.localScale = Vector3.one;
        }

        // 4. 아이템별 고유 연출 애니메이션 (행동 전략에 위임)
        if (Behavior != null)
        {
            Behavior.UpdateVisual(time, isHovered, _isDragging);
        }

        // 5. Kinetic drag velocity update (마우스 던지기 속도 계산 - 별 노드와 동일 스펙)
        if (_isDragging)
        {
            Vector2 currentPos = _rectTransform.anchoredPosition;
            Vector2 positionDelta = currentPos - _lastDragFramePos;
            Vector2 frameVelocity = positionDelta / Mathf.Max(Time.unscaledDeltaTime, 0.001f);

            // 마우스 지터 필터링을 위한 보간
            _velocity = Vector2.Lerp(_velocity, frameVelocity, 0.22f);
            _lastDragFramePos = currentPos;
        }
    }

    // ─── ICosmicKineticObject 물리 연산 처리부 ───────────────────────

    public void UpdatePhysicsTick(float deltaTime)
    {
        if (Behavior != null)
        {
            Behavior.UpdatePhysics(deltaTime);
        }
    }

    public void ResolveCollisionWith(ICosmicKineticObject other)
    {
        if (Behavior != null)
        {
            Behavior.ResolveCollision(other);
        }
    }

    public void HandleScreenBoundaries()
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
            _lastDragFramePos = _rectTransform.anchoredPosition;
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
