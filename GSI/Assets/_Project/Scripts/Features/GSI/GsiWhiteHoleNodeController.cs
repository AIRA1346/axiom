using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// GSI 우주 Orrery 성계에서 공식 통합 시험을 상징하는 눈부신 화이트홀(White Hole) 노드 컨트롤러.
/// 주변 별자리 노드들을 부드럽게 밀어내는 척력 공간을 제공하며, 중심에서 외곽으로 네온 입자들을 지속 분출합니다.
/// </summary>
public sealed class GsiWhiteHoleNodeController : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    public Color EmissionDiskColor = new Color(0.2f, 0.85f, 1f, 0.85f); // Neon Cyan Light
    public Color CoreGlowColor = new Color(1f, 1f, 1f, 0.95f);       // Bright White Light

    public Action<GsiWhiteHoleNodeController> OnClickedAction;

    private RectTransform _whiteHoleVisualRoot;
    private RectTransform _emissionDiskRt;
    private CanvasGroup _labelCanvasGroup;
    private TextMeshProUGUI _labelTmp;

    private Coroutine _hoverRoutine;
    private bool _isHovered = false;
    private float _currentHoverT = 0f;
    private RectTransform _rectTransform;

    private readonly List<WhiteHoleDust> _dustTrail = new List<WhiteHoleDust>();
    private float _dustSpawnTimer = 0f;

    private class WhiteHoleDust
    {
        public RectTransform Rect;
        public CanvasGroup Cg;
        public Vector2 Velocity;
        public float Lifetime;
        public float MaxLifetime;
    }

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

        // 1. 투명 클릭 영역 설정
        if (TryGetComponent(out Image img))
        {
            img.sprite = null;
            img.color = new Color(1f, 1f, 1f, 0.005f);
            img.raycastTarget = true;
        }

        // 2. 텍스트 라벨 툴팁화
        _labelTmp = GetComponentInChildren<TextMeshProUGUI>(true);
        if (_labelTmp != null)
        {
            _labelTmp.transform.SetParent(transform, false);
            var labelRt = _labelTmp.rectTransform;

            if (_labelTmp.TryGetComponent(out LayoutElement le))
            {
                Destroy(le);
            }

            labelRt.anchorMin = new Vector2(0.5f, 0.5f);
            labelRt.anchorMax = new Vector2(0.5f, 0.5f);
            labelRt.pivot = new Vector2(0.5f, 0.5f);
            labelRt.anchoredPosition = new Vector2(0f, -32f);
            labelRt.sizeDelta = new Vector2(300f, 36f);

            _labelTmp.alignment = TextAlignmentOptions.Center;
            _labelTmp.fontSize = 18f;
            _labelTmp.characterSpacing = 0.5f;
            _labelTmp.fontStyle = FontStyles.Bold;
            _labelTmp.color = new Color(0.7f, 0.9f, 1f, 1f); // 맑은 하늘색 툴팁
            _labelTmp.gameObject.SetActive(true);

            _labelCanvasGroup = _labelTmp.gameObject.GetComponent<CanvasGroup>();
            if (_labelCanvasGroup == null)
            {
                _labelCanvasGroup = _labelTmp.gameObject.AddComponent<CanvasGroup>();
            }
            _labelCanvasGroup.alpha = 0f;
            _labelCanvasGroup.interactable = false;
            _labelCanvasGroup.blocksRaycasts = false;
        }

        // 3. 화이트홀 비주얼 동적 생성 (눈부시게 밝은 천체 오러리 구성)
        var visualGo = new GameObject("WhiteHoleVisualRoot", typeof(RectTransform));
        _whiteHoleVisualRoot = visualGo.GetComponent<RectTransform>();
        _whiteHoleVisualRoot.SetParent(transform, false);
        _whiteHoleVisualRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _whiteHoleVisualRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _whiteHoleVisualRoot.pivot = new Vector2(0.5f, 0.5f);
        _whiteHoleVisualRoot.anchoredPosition = Vector2.zero;
        _whiteHoleVisualRoot.sizeDelta = new Vector2(20f, 20f);

        // A. 팽창형 고광도 아우라 (Intense Outward Aura Glow)
        CreateWhiteHoleLayer("OutwardAuraGlow", 24f, 24f, 0f, new Color(EmissionDiskColor.r, EmissionDiskColor.g, EmissionDiskColor.b, 0.32f));

        // B. 분출원반 고리 (Emission Disk, 회전 및 외부 팽창감 연출)
        var diskGo = CreateWhiteHoleLayer("EmissionDiskRing", 20f, 20f, -45f, new Color(EmissionDiskColor.r * 1.1f, EmissionDiskColor.g * 1.1f, EmissionDiskColor.b * 1.1f, 0.78f));
        _emissionDiskRt = diskGo.GetComponent<RectTransform>();

        // C. 고에너지 코어 아웃라인 (High energy core edge)
        CreateWhiteHoleLayer("InnerFlareRing", 13f, 13f, 45f, new Color(CoreGlowColor.r, CoreGlowColor.g, CoreGlowColor.b, 0.85f));

        // D. 화이트홀 특이점 코어 (Singularity Core, 순수한 백색 광원)
        CreateWhiteHoleLayer("SingularityCore", 8f, 8f, 0f, new Color(1f, 1f, 1f, 0.98f));
    }

    private GameObject CreateWhiteHoleLayer(string layerName, float w, float h, float rotZ, Color color)
    {
        var go = new GameObject(layerName, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(_whiteHoleVisualRoot, false);
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
        if (!Application.isPlaying) return;

        if (_whiteHoleVisualRoot != null && _emissionDiskRt != null)
        {
            // 분출원반의 나선은 시계 방향으로 소용돌이치며 분출
            float spinSpeed = _isHovered ? 90f : 55f;
            _emissionDiskRt.Rotate(0f, 0f, spinSpeed * Time.unscaledDeltaTime);

            // 미세한 초신성 맥동 (Pulsing Breathing)
            float pulse = 1.0f + Mathf.Sin(Time.unscaledTime * 3.2f) * 0.05f;
            _whiteHoleVisualRoot.localScale = new Vector3(pulse, pulse, pulse);
        }

        // 척력 입자 방출 시뮬레이션
        UpdateEmissionParticles();
    }

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
        // 누르면 에너지를 일시 응축하는 듯이 약간 작아짐
        if (_whiteHoleVisualRoot != null)
        {
            _whiteHoleVisualRoot.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 탕! 하고 초신성 폭발하듯 1.3배 이상 일시 팽창했다가 원복되는 탄성 팽창 연출
        if (_whiteHoleVisualRoot != null)
        {
            StopAllCoroutines();
            StartCoroutine(CoAnimatePopExpand());
        }

        if (!eventData.dragging)
        {
            OnClickedAction?.Invoke(this);
        }
    }

    private IEnumerator CoAnimatePopExpand()
    {
        float elapsed = 0f;
        float duration = 0.35f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // Elastic Out
            float scale = 1f + Mathf.Exp(-5f * t) * Mathf.Sin(10f * t) * 0.45f;
            if (_whiteHoleVisualRoot != null)
            {
                _whiteHoleVisualRoot.localScale = new Vector3(scale, scale, scale);
            }
            yield return null;
        }
        if (_whiteHoleVisualRoot != null) _whiteHoleVisualRoot.localScale = Vector3.one;
    }

    private IEnumerator CoAnimateHover(float target)
    {
        float startT = _currentHoverT;
        float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float curve = t * t * (3f - 2f * t);
            _currentHoverT = Mathf.Lerp(startT, target, curve);

            if (_labelCanvasGroup != null)
            {
                _labelCanvasGroup.alpha = _currentHoverT;
            }

            yield return null;
        }
        _currentHoverT = target;
        if (_labelCanvasGroup != null) _labelCanvasGroup.alpha = target;
    }

    // ==========================================
    // 🌌 화이트홀 전용 척력 입자(Emission) 방출 시스템
    // ==========================================
    private void UpdateEmissionParticles()
    {
        // 입자 스폰
        _dustSpawnTimer -= Time.unscaledDeltaTime;
        if (_dustSpawnTimer <= 0f)
        {
            _dustSpawnTimer = _isHovered ? 0.05f : 0.09f;
            SpawnEmissionParticle();
        }

        // 입자 업데이트 및 소멸
        for (int i = _dustTrail.Count - 1; i >= 0; i--)
        {
            var p = _dustTrail[i];
            if (p.Rect == null || p.Cg == null)
            {
                _dustTrail.RemoveAt(i);
                continue;
            }

            p.Lifetime += Time.unscaledDeltaTime;
            if (p.Lifetime >= p.MaxLifetime)
            {
                Destroy(p.Rect.gameObject);
                _dustTrail.RemoveAt(i);
            }
            else
            {
                // 바깥으로 속도에 맞춰 밀려남
                p.Rect.anchoredPosition += p.Velocity * Time.unscaledDeltaTime;

                float t = p.Lifetime / p.MaxLifetime;
                p.Cg.alpha = Mathf.Lerp(0.8f, 0f, t);
                p.Rect.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            }
        }
    }

    private void SpawnEmissionParticle()
    {
        if (_whiteHoleVisualRoot == null) return;

        var go = new GameObject("WhiteHoleSpark", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; // 중심에서 시작

        float size = UnityEngine.Random.Range(2.0f, 4.8f);
        rt.sizeDelta = new Vector2(size, size);

        var img = go.GetComponent<Image>();
        img.sprite = null;
        // 시안에서 백색 사이의 영롱한 척력 입자
        img.color = Color.Lerp(EmissionDiskColor, Color.white, UnityEngine.Random.value);
        img.raycastTarget = false;

        var cg = go.GetComponent<CanvasGroup>();
        cg.alpha = 0.8f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // 사방으로 분출되는 척력 속도 벡터
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float speed = UnityEngine.Random.Range(40f, 95f);
        Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

        _dustTrail.Add(new WhiteHoleDust
        {
            Rect = rt,
            Cg = cg,
            Velocity = vel,
            Lifetime = 0f,
            MaxLifetime = UnityEngine.Random.Range(0.4f, 0.75f)
        });

        // 별자리 라인보다 아래, 배경보다는 위에 그리게끔 순서 강제
        rt.SetAsFirstSibling();
    }

    private void OnDestroy()
    {
        foreach (var p in _dustTrail)
        {
            if (p != null && p.Rect != null) Destroy(p.Rect.gameObject);
        }
        _dustTrail.Clear();
    }
}
