using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 배경에 배치된 개별 데코 아이템의 비주얼 생성, 드래그 이동 및 고유 애니메이션 담당
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class GsiPlacedDeco : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string ItemId { get; private set; }
    public Vector2 NormalizedPos { get; set; }

    private RectTransform _rectTransform;
    private Canvas _parentCanvas;
    private bool _isDragging = false;

    // 애니메이션 제어용 프라이빗 캐시
    private Transform _visualRoot;
    private Transform _glowLayer;
    private Transform _spikeV;
    private Transform _spikeH;
    private float _startFloatY;
    private float _randomPhaseOffset;

    public void Initialize(string itemId, Vector2 normalizedPos, Canvas canvas)
    {
        ItemId = itemId;
        NormalizedPos = normalizedPos;
        _parentCanvas = canvas;
        _rectTransform = GetComponent<RectTransform>();
        _randomPhaseOffset = UnityEngine.Random.Range(0f, 100f);

        // 1. 클릭 히트박스 영역(Image) 설정
        var hitImg = GetComponent<Image>();
        if (hitImg == null) hitImg = gameObject.AddComponent<Image>();
        hitImg.sprite = null;
        hitImg.color = Color.clear; // 클릭 충돌용 투명 영역
        hitImg.raycastTarget = true;
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

        // 3. 등급 정의 로드 및 절차적 비주얼 그리기
        if (PlayerDecorations.TryGetItemDef(ItemId, out var def))
        {
            BuildProceduralVisuals(def);
        }
    }

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        _startFloatY = _rectTransform.anchoredPosition.y;
    }

    private void BuildProceduralVisuals(PlayerDecorations.DecoItemDef def)
    {
        Color color = def.DefaultColor;

        if (def.ProceduralShape == "star")
        {
            // 노란 별: 글로우 마름모 + 십자 플레어 + 중앙 흰색 핵
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
            // 보라 크리스탈: 보랏빛 글로우 마름모 + 세로형 투명 마름모 코어 + 중앙 백색 코어
            var glow = CreateLayer("AuraGlow", 13f, 13f, 45f, new Color(color.r, color.g, color.b, 0.3f));
            _glowLayer = glow.transform;

            CreateLayer("CrystalOuter", 12f, 20f, 45f, new Color(color.r, color.g, color.b, 0.85f));
            CreateLayer("CrystalCore", 6f, 10f, 45f, new Color(1f, 1f, 1f, 0.95f));
        }
        else if (def.ProceduralShape == "ring")
        {
            // 사이언 링: 사이언 원형 아웃라인 + 마킹 도트 ( hollowing 효과를 위해 3겹 구조 )
            var ringOuter = CreateLayer("RingOuter", 22f, 22f, 0f, new Color(color.r, color.g, color.b, 0.85f));
            _glowLayer = ringOuter.transform; // 회전을 줄 수 있도록 캐싱
            
            // 홀링을 위해 배경색과 유사한 다크 그레이 원을 주입하여 가짜 링 구현
            CreateLayer("RingHole", 16f, 16f, 0f, new Color(0.04f, 0.04f, 0.06f, 1f));

            // 중앙 코어
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
        if (_isDragging) return;

        float time = Time.unscaledTime + _randomPhaseOffset;

        // 아이템 아이디별 고유 특화 애니메이션 작동
        if (ItemId == "deco_yellow_star")
        {
            // 1. 별 자전
            if (_visualRoot != null)
            {
                _visualRoot.localRotation = Quaternion.Euler(0f, 0f, time * 10f);
            }
            // 2. 글로우 호흡
            if (_glowLayer != null)
            {
                float pulse = 0.8f + Mathf.PingPong(time * 0.4f, 0.3f);
                _glowLayer.localScale = new Vector3(pulse, pulse, 1f);
            }
            // 3. 플레어 스파이크 교차 깜빡임
            if (_spikeV != null && _spikeH != null)
            {
                float spPulse = 0.85f + Mathf.PingPong(time * 1.5f, 0.25f);
                _spikeV.localScale = new Vector3(1f, spPulse, 1f);
                _spikeH.localScale = new Vector3(spPulse, 1f, 1f);
            }
        }
        else if (ItemId == "deco_purple_crystal")
        {
            // 1. 크리스탈 공중 부유 (Floating)
            Vector2 pos = _rectTransform.anchoredPosition;
            float floatOffset = Mathf.Sin(time * 1.8f) * 6f; // Y축 오프셋
            
            // 드래그 중이 아닐 때만 적용
            _rectTransform.anchoredPosition = new Vector2(pos.x, _startFloatY + floatOffset);

            // 2. 보랏빛 오라 호흡
            if (_glowLayer != null)
            {
                float pulse = 0.85f + Mathf.Sin(time * 2.2f) * 0.15f;
                _glowLayer.localScale = new Vector3(pulse, pulse, 1f);
            }
        }
        else if (ItemId == "deco_neon_ring")
        {
            // 1. 서클 링 자전 (Ring Rotation)
            if (_glowLayer != null)
            {
                _glowLayer.Rotate(0f, 0f, -40f * Time.unscaledDeltaTime);
            }
        }
    }

    // ─── Drag & Drop Event System 구현 ──────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        GsiDecoPanelController.Instance?.NotifyDragBegin(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        float scaleFactor = _parentCanvas != null ? _parentCanvas.scaleFactor : 1f;
        _rectTransform.anchoredPosition += eventData.delta / scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        
        // 드래그를 멈추고 부유 시작 Y점 재조정
        _startFloatY = _rectTransform.anchoredPosition.y;

        // 매니저에 배치가 끝났음을 알려 좌표를 갱신하거나 소멸/회수시킵니다.
        GsiDecoPanelController.Instance?.NotifyDragEnd(this);
    }
}
