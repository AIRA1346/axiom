using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using ArchE.Game;

/// <summary>
/// 화면 하단 전체에 걸쳐 슬라이드되는 데코(배경 꾸미기) 시스템 통합 제어판
/// </summary>
public sealed class GsiDecoPanelController : MonoBehaviour
{
    public static GsiDecoPanelController Instance { get; private set; }

    private const float PanelHeight = 180f;
    private const string SaveKeyPrefix = "GSI_Deco_Placed_";

    private Canvas _canvas;
    private Canvas _targetCanvas;        // 데코 배치용 메인 캔버스
    private RectTransform _decoContainer; // 배치된 데코들이 들어갈 레이어
    private RectTransform _panelRt;       // 슬라이드업 드로어 패널
    private RectTransform _contentRt;      // ScrollRect의 콘텐츠

    private bool _isOpen = false;
    private Coroutine _slideRoutine;
    private string _sceneKey;

    private readonly List<GsiPlacedDeco> _placedDecos = new List<GsiPlacedDeco>();
    private readonly List<DecoCardUI> _cards = new List<DecoCardUI>();

    // 드래그 중인 임시 프리뷰 오브젝트
    private GameObject _dragPreviewGo;
    private string _dragPreviewId;

    // 수동 드래그 조작용 변수 (uGUI 터치 가로막힘 우회)
    private GsiPlacedDeco _manuallyDraggedDeco;
    private Vector2 _lastMousePos;

    private class DecoCardUI
    {
        public string ItemId;
        public TextMeshProUGUI CountTmp;
        public Image CardBg;
    }

    /// <summary>
    /// 로비와 GSI 씬 로드 시 싱글톤 생성 연동
    /// </summary>
    public static void EnsureCreated()
    {
        if (Instance != null) return;

        Canvas targetCanvas = null;
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        // 1. GraphicRaycaster가 존재하고 활성화된 메인 UI 캔버스를 우선 탐색
        for (int i = 0; i < canvases.Length; i++)
        {
            var c = canvases[i];
            if (c.isActiveAndEnabled && c.GetComponent<GraphicRaycaster>() != null)
            {
                if (c.name.Contains("Transition") || c.name.Contains("Intro"))
                    continue;

                targetCanvas = c;
                break;
            }
        }

        // 2. 적절한 캔버스를 찾지 못했다면 활성화된 첫 번째 캔버스를 선택
        if (targetCanvas == null)
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].isActiveAndEnabled)
                {
                    targetCanvas = canvases[i];
                    break;
                }
            }
        }

        if (targetCanvas == null && canvases.Length > 0)
        {
            targetCanvas = canvases[0];
        }

        if (targetCanvas == null)
        {
            Debug.LogError("[GsiDecoPanelController] EnsureCreated failed: No Canvas found in the scene.");
            return;
        }

        if (targetCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            targetCanvas.gameObject.AddComponent<GraphicRaycaster>();
            Debug.LogWarning($"[GsiDecoPanelController] Added missing GraphicRaycaster to Canvas: {targetCanvas.name}");
        }

        Debug.Log($"[GsiDecoPanelController] EnsureCreated: Parent canvas selected -> {targetCanvas.name} (sortingOrder={targetCanvas.sortingOrder})");

        var go = new GameObject("GsiDecoPanelManager");
        var controller = go.AddComponent<GsiDecoPanelController>();
        controller.InitCanvas(targetCanvas);
        Instance = controller;
    }

    /// <summary>
    /// 자체 독립 Canvas 및 데코 배치 대상 Canvas 설정
    /// </summary>
    public void InitCanvas(Canvas targetCanvas)
    {
        _targetCanvas = targetCanvas;
        
        _canvas = gameObject.GetComponent<Canvas>();
        if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 90; // 일반 게임 UI들보다 위에 배치하여 완벽한 터치 우선권 획득
        
        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // EnsureCreated를 통하지 않고 에디터상 수동 배치 등으로 깨질 수 있으므로 대비책 마련
        if (_canvas == null)
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                if (canvases.Length > 0)
                {
                    InitCanvas(canvases[0]);
                }
            }
        }

        _sceneKey = SceneManager.GetActiveScene().name == SceneNames.GSI ? "GSI" : "Lobby";

        BuildUi();
        LoadAndSpawnAllPlacedItems();
    }

    private void Start()
    {
        // 씬 시작 시 패널은 숨김 상태로 대기
        if (_panelRt != null) _panelRt.anchoredPosition = new Vector2(0f, -PanelHeight - 20f);

        // InputManager 전역 마우스/터치 입력 바인딩
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnInputDown += HandleGlobalInputDown;
            InputManager.Instance.OnInputHold += HandleGlobalInputHold;
            InputManager.Instance.OnInputUp += HandleGlobalInputUp;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TogglePanel();
        }
    }

    private void TogglePanel()
    {
        _isOpen = !_isOpen;
        if (_slideRoutine != null) StopCoroutine(_slideRoutine);
        _slideRoutine = StartCoroutine(CoSlidePanel(_isOpen));
        GsiUiSound.PlayClick();
    }

    private System.Collections.IEnumerator CoSlidePanel(bool open)
    {
        float duration = 0.22f;
        float elapsed = 0f;

        Vector2 startPos = _panelRt.anchoredPosition;
        Vector2 targetPos = open ? new Vector2(0f, 0f) : new Vector2(0f, -PanelHeight - 20f);

        // 닫히기 시작할 때 텍스트 가이드 작동
        if (open)
        {
            RefreshAllCards();
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float curve = t * t * (3f - 2f * t); // SmoothStep
            _panelRt.anchoredPosition = Vector2.Lerp(startPos, targetPos, curve);
            yield return null;
        }

        _panelRt.anchoredPosition = targetPos;
    }

    // ─── UI 생성 로직 ──────────────────────────────────────────────────

    private void BuildUi()
    {
        TMP_FontAsset font = TmpFontCache.LiberationSansSdf;

        // 1. 배치 영역 컨테이너 생성 (배경 이미지 위, UI 아래 정렬)
        var containerGo = new GameObject("DecoPlacementContainer", typeof(RectTransform));
        _decoContainer = containerGo.GetComponent<RectTransform>();
        
        // _targetCanvas가 Null일 경우 안전하게 씬 메인 캔버스 탐색
        if (_targetCanvas == null)
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].isActiveAndEnabled && canvases[i] != _canvas)
                {
                    _targetCanvas = canvases[i];
                    break;
                }
            }
        }

        if (_targetCanvas != null)
        {
            _decoContainer.SetParent(_targetCanvas.transform, false);
            
            // 캔버스 내 메인 배경(Bg/Background/Space) 직계 자식의 Sibling Index 자동 추적
            int bgIndex = -1;
            int childCount = _targetCanvas.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = _targetCanvas.transform.GetChild(i);
                if (child == _decoContainer.transform) continue;

                string lowerName = child.name.ToLower();
                if (lowerName.Contains("bg") || lowerName.Contains("background") || lowerName.Contains("back") || lowerName.Contains("space"))
                {
                    bgIndex = i;
                }
            }

            if (bgIndex >= 0)
            {
                // 배경의 바로 앞 레이어로 주입 (배경보다 뒤로 숨지 않도록 보장)
                _decoContainer.SetSiblingIndex(bgIndex + 1);
                Debug.Log($"[GsiDecoPanelController] _decoContainer sibling set to {bgIndex + 1} (above bg child: {_targetCanvas.transform.GetChild(bgIndex).name})");
            }
            else
            {
                _decoContainer.SetSiblingIndex(1); // fallback
            }
        }
        else
        {
            _decoContainer.SetParent(_canvas.transform, false);
            _decoContainer.SetSiblingIndex(0);
        }

        // Z-position 및 스케일 꼬임 전면 리셋
        _decoContainer.localPosition = new Vector3(_decoContainer.localPosition.x, _decoContainer.localPosition.y, 0f);
        _decoContainer.localScale = Vector3.one;

        GsiUiRuntimeWidgets.StretchFull(_decoContainer);

        // 3. 메인 데코 슬라이딩 패널 (하단 전체 꽉 채움)
        var panelGo = new GameObject("GsiDecoPanel", typeof(RectTransform), typeof(Image));
        _panelRt = panelGo.GetComponent<RectTransform>();
        _panelRt.SetParent(_canvas.transform, false);
        _panelRt.anchorMin = new Vector2(0f, 0f);
        _panelRt.anchorMax = new Vector2(1f, 0f);
        _panelRt.pivot = new Vector2(0.5f, 0f);
        _panelRt.sizeDelta = new Vector2(0f, PanelHeight);

        var panelImg = panelGo.GetComponent<Image>();
        panelImg.sprite = null;
        panelImg.color = new Color(0.04f, 0.04f, 0.06f, 0.92f); // 글래스모피즘 어두운 면
        panelImg.raycastTarget = true; // 패널 뒤로 클릭이 넘어가지 않음

        // 패널 상단 데코 선
        var topBorder = new GameObject("TopBorder", typeof(RectTransform), typeof(Image));
        var borderRt = topBorder.GetComponent<RectTransform>();
        borderRt.SetParent(_panelRt, false);
        borderRt.anchorMin = new Vector2(0f, 1f);
        borderRt.anchorMax = new Vector2(1f, 1f);
        borderRt.pivot = new Vector2(0.5f, 1f);
        borderRt.anchoredPosition = Vector2.zero;
        borderRt.sizeDelta = new Vector2(0f, 2.5f);
        var borderImg = topBorder.GetComponent<Image>();
        borderImg.sprite = null;
        borderImg.color = CosmeticTheme.UiAccent;

        // 4. 패널 레이아웃 나눔: 왼쪽 (필터 공간) / 오른쪽 (아이템 스크롤 뷰)
        var filterPlaceholderGo = new GameObject("FilterPlaceholder", typeof(RectTransform));
        var filterRt = filterPlaceholderGo.GetComponent<RectTransform>();
        filterRt.SetParent(_panelRt, false);
        filterRt.anchorMin = new Vector2(0f, 0.5f);
        filterRt.anchorMax = new Vector2(0f, 0.5f);
        filterRt.pivot = new Vector2(0f, 0.5f);
        filterRt.anchoredPosition = new Vector2(16f, 0f);
        filterRt.sizeDelta = new Vector2(120f, PanelHeight - 32f);

        // 미래 확장성용 텍스트 가이드
        var filterTmp = GsiUiRuntimeWidgets.CreateTmp(filterRt, GameLocalization.GetUiString("deco.all_items", "ALL"), 16f, FontStyles.Bold);
        filterTmp.color = CosmeticTheme.UiAccent;
        GsiUiRuntimeWidgets.StretchFull(filterTmp.rectTransform);
        filterTmp.alignment = TextAlignmentOptions.Center;

        // 오른쪽 스크롤 뷰 생성
        var scrollGo = new GameObject("ItemScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.SetParent(_panelRt, false);
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.pivot = new Vector2(0f, 0.5f);
        scrollRt.offsetMin = new Vector2(150f, 16f);
        scrollRt.offsetMax = new Vector2(-16f, -16f);

        var scrollImg = scrollGo.GetComponent<Image>();
        scrollImg.sprite = null;
        scrollImg.color = Color.clear;
        scrollImg.raycastTarget = false; // 부모 ScrollRect가 하위 카드들의 레이캐스트를 뺏지 못하도록 비활성화

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.vertical = false;
        scroll.horizontal = true;

        // Viewport (Unity 6 LTS 내 RectMask2D의 레이캐스트 정렬 오판을 회피하기 위해 표준 Mask 사용)
        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var viewportRt = viewportGo.GetComponent<RectTransform>();
        viewportRt.SetParent(scrollRt, false);
        GsiUiRuntimeWidgets.StretchFull(viewportRt);
        
        var viewportImg = viewportGo.GetComponent<Image>();
        viewportImg.sprite = null;
        viewportImg.color = new Color(1f, 1f, 1f, 0.005f); // 투명에 가까운 픽셀 설정 (Mask를 위해 그래픽 활성화)
        viewportImg.raycastTarget = false; // 마스크 뷰포트 자체가 터치를 뺏지 않도록 차단
        
        var mask = viewportGo.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        scroll.viewport = viewportRt;

        // Content
        var contentGo = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        _contentRt = contentGo.GetComponent<RectTransform>();
        _contentRt.SetParent(viewportRt, false);
        _contentRt.anchorMin = new Vector2(0f, 0.5f);
        _contentRt.anchorMax = new Vector2(0f, 0.5f);
        _contentRt.pivot = new Vector2(0f, 0.5f);
        _contentRt.sizeDelta = new Vector2(0f, PanelHeight - 40f);

        var hlg = contentGo.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        var csf = contentGo.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = _contentRt;

        // 5. 등록부에 있는 아이템 카드 렌더링
        BuildItemCards(font);
    }

    private void BuildItemCards(TMP_FontAsset font)
    {
        _cards.Clear();
        foreach (var def in PlayerDecorations.All)
        {
            var cardGo = new GameObject($"Card_{def.Id}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            var cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.SetParent(_contentRt, false);
            cardRt.localPosition = new Vector3(cardRt.localPosition.x, cardRt.localPosition.y, 0f);
            cardRt.localScale = Vector3.one;

            var le = cardGo.GetComponent<LayoutElement>();
            le.preferredWidth = 110f;
            le.preferredHeight = PanelHeight - 50f;

            var cardImg = cardGo.GetComponent<Image>();
            cardImg.sprite = null;
            cardImg.color = new Color(0.12f, 0.12f, 0.16f, 0.75f);
            cardImg.raycastTarget = true;
            
            // 일반 등급 테두리 선 (F3F4F6)
            GsiUiRuntimeWidgets.ApplyRowCardShadow(cardImg);
            var outlineGo = new GameObject("BorderOutline", typeof(RectTransform), typeof(Image));
            var outlineRt = outlineGo.GetComponent<RectTransform>();
            outlineRt.SetParent(cardRt, false);
            GsiUiRuntimeWidgets.StretchFull(outlineRt);
            var outImg = outlineGo.GetComponent<Image>();
            outImg.sprite = null;
            outImg.color = PlayerDecorations.GetRarityColor(def.Rarity);
            outImg.raycastTarget = false;
            var outLe = outlineGo.AddComponent<Outline>();
            outLe.effectColor = PlayerDecorations.GetRarityColor(def.Rarity);
            outLe.effectDistance = new Vector2(1f, 1f);
            outImg.color = Color.clear;

            // 1. 카드 상단 제목
            var nameGo = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.SetParent(cardRt, false);
            nameRt.anchorMin = new Vector2(0.5f, 1f);
            nameRt.anchorMax = new Vector2(0.5f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0f, -8f);
            nameRt.sizeDelta = new Vector2(100f, 20f);
            var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
            if (font != null) nameTmp.font = font;
            nameTmp.text = GameLocalization.GetUiString(def.DisplayNameKey, def.EnglishName);
            nameTmp.fontSize = 11f;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = Color.white;
            nameTmp.raycastTarget = false;

            // 2. 카드 중앙 등급 배지
            var badgeGo = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            var badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.SetParent(cardRt, false);
            badgeRt.anchorMin = new Vector2(0.5f, 1f);
            badgeRt.anchorMax = new Vector2(0.5f, 1f);
            badgeRt.pivot = new Vector2(0.5f, 1f);
            badgeRt.anchoredPosition = new Vector2(0f, -28f);
            badgeRt.sizeDelta = new Vector2(100f, 18f);
            var badgeTmp = badgeGo.GetComponent<TextMeshProUGUI>();
            if (font != null) badgeTmp.font = font;
            bool isKo = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale != null &&
                        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("ko", System.StringComparison.OrdinalIgnoreCase);
            badgeTmp.text = $"[{PlayerDecorations.GetRarityName(def.Rarity, isKo)}]";
            badgeTmp.fontSize = 10f;
            badgeTmp.fontStyle = FontStyles.Bold;
            badgeTmp.alignment = TextAlignmentOptions.Center;
            badgeTmp.color = PlayerDecorations.GetRarityColor(def.Rarity);
            badgeTmp.raycastTarget = false;

            // 3. 카드 내부 아이콘 이미지 영역
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.SetParent(cardRt, false);
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(0f, -4f);
            iconRt.sizeDelta = new Vector2(28f, 28f);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = null;
            iconImg.color = def.DefaultColor;
            iconImg.raycastTarget = false;

            // 4. 카드 하단 잔여 수량 표시
            var countGo = new GameObject("CountText", typeof(RectTransform), typeof(TextMeshProUGUI));
            var countRt = countGo.GetComponent<RectTransform>();
            countRt.SetParent(cardRt, false);
            countRt.anchorMin = new Vector2(0.5f, 0f);
            countRt.anchorMax = new Vector2(0.5f, 0f);
            countRt.pivot = new Vector2(0.5f, 0f);
            countRt.anchoredPosition = new Vector2(0f, 8f);
            countRt.sizeDelta = new Vector2(100f, 20f);
            var countTmp = countGo.GetComponent<TextMeshProUGUI>();
            if (font != null) countTmp.font = font;
            countTmp.text = "0 / 0";
            countTmp.fontSize = 11f;
            countTmp.alignment = TextAlignmentOptions.Center;
            countTmp.color = GsiUiAppearance.TextSecondary;
            countTmp.raycastTarget = false;

            var cardUi = new DecoCardUI
            {
                ItemId = def.Id,
                CountTmp = countTmp,
                CardBg = cardImg
            };
            _cards.Add(cardUi);

            // 5. 카드 드래그 앤 드롭 컴포넌트 바인딩
            var cardComponent = cardGo.AddComponent<GsiDecoCard>();
            cardComponent.ItemId = def.Id;
            cardComponent.PanelController = this;
        }
        RefreshAllCards();
    }

    private void RefreshAllCards()
    {
        for (int i = 0; i < _cards.Count; i++)
        {
            var card = _cards[i];
            int owned = PlayerDecorations.GetTotalOwned(card.ItemId);
            int available = PlayerDecorations.GetAvailableCount(card.ItemId);
            card.CountTmp.text = $"{available} / {owned}";

            // 소진 시 어둡게 딤드(Dimmed) 처리
            card.CardBg.color = available > 0
                ? new Color(0.12f, 0.12f, 0.16f, 0.75f)
                : new Color(0.08f, 0.08f, 0.10f, 0.35f);
        }
    }

    // ─── 세이브 데이터 기반 데코 일괄 스폰 ────────────────────────────────────

    private void LoadAndSpawnAllPlacedItems()
    {
        // 기존 배치물 클리어
        for (int i = _placedDecos.Count - 1; i >= 0; i--)
        {
            if (_placedDecos[i] != null) Destroy(_placedDecos[i].gameObject);
        }
        _placedDecos.Clear();

        var list = PlayerDecorations.LoadPlacedDecos(_sceneKey);
        Vector2 containerSize = _decoContainer.rect.size;
        
        // 만약 컨테이너 사이즈가 아직 가로세로 잡히지 않았다면 (최초 프레임 앵커링 지연) 전체화면 크기 디폴트로 잡음
        if (containerSize.x < 100f) containerSize = new Vector2(Screen.width, Screen.height);

        for (int i = 0; i < list.Count; i++)
        {
            var pData = list[i];
            SpawnPlacedDeco(pData.ItemId, pData.NormalizedPos, containerSize);
        }
    }

    private void SpawnPlacedDeco(string itemId, Vector2 normalizedPos, Vector2 containerSize)
    {
        var go = new GameObject($"PlacedDeco_{itemId}", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(_decoContainer, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        // 비율에서 월드 좌표계 복원
        float px = (normalizedPos.x - 0.5f) * containerSize.x;
        float py = (normalizedPos.y - 0.5f) * containerSize.y;
        rt.anchoredPosition = new Vector2(px, py);

        var deco = go.AddComponent<GsiPlacedDeco>();
        deco.Initialize(itemId, normalizedPos, _canvas);
        _placedDecos.Add(deco);
    }

    // ─── 인벤토리 카드 드래그 제어 (신규 아이템 생성) ──────────────────────────

    public void OnCardBeginDrag(PointerEventData eventData, string itemId)
    {
        int total = PlayerDecorations.GetTotalOwned(itemId);
        int available = PlayerDecorations.GetAvailableCount(itemId);
        Debug.Log($"[GsiDecoPanelController] OnCardBeginDrag: ItemId={itemId}, TotalOwned={total}, Available={available}");

        if (available <= 0)
        {
            Debug.LogWarning($"[GsiDecoPanelController] Cancel drag: No available count for {itemId}");
            eventData.pointerDrag = null; // 드래그 취소
            return;
        }

        _dragPreviewId = itemId;
        
        // 마우스 포인터 따라다니는 임시 이미지 생성
        _dragPreviewGo = new GameObject("DragPreview", typeof(RectTransform), typeof(Image));
        var rt = _dragPreviewGo.GetComponent<RectTransform>();
        rt.SetParent(_canvas.transform, false);
        rt.sizeDelta = new Vector2(28f, 28f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);
        rt.localScale = Vector3.one;

        var img = _dragPreviewGo.GetComponent<Image>();
        img.sprite = null;
        if (PlayerDecorations.TryGetItemDef(itemId, out var def))
        {
            img.color = new Color(def.DefaultColor.r, def.DefaultColor.g, def.DefaultColor.b, 0.6f);
        }
        img.raycastTarget = false;

        UpdateDragPreviewPos(eventData.position);
        GsiUiSound.PlayClick();
    }

    public void OnCardDrag(PointerEventData eventData)
    {
        Debug.Log($"[GsiDecoPanelController] OnCardDrag: position={eventData.position}");
        if (_dragPreviewGo != null)
        {
            UpdateDragPreviewPos(eventData.position);
        }
    }

    public void OnCardEndDrag(PointerEventData eventData)
    {
        Debug.Log($"[GsiDecoPanelController] OnCardEndDrag: position={eventData.position}");
        if (_dragPreviewGo == null) return;

        Destroy(_dragPreviewGo);
        _dragPreviewGo = null;

        // 드롭된 마우스 포인터의 위치가 하단 드로어 패널 내부인지 외부인지 체크
        bool dropInPanel = RectTransformUtility.RectangleContainsScreenPoint(_panelRt, eventData.position, eventData.pressEventCamera);
        Debug.Log($"[GsiDecoPanelController] dropInPanel={dropInPanel}");

        if (!dropInPanel)
        {
            // 컨테이너 크기 기준 정규화 좌표 구함
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_decoContainer, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                Vector2 parentSize = _decoContainer.rect.size;
                Debug.Log($"[GsiDecoPanelController] localPoint={localPoint}, parentSize={parentSize}");
                if (parentSize.x > 0 && parentSize.y > 0)
                {
                    float nx = localPoint.x / parentSize.x + 0.5f;
                    float ny = localPoint.y / parentSize.y + 0.5f;
                    var norm = new Vector2(nx, ny);

                    // 영구 데코 인스턴스 소환
                    SpawnPlacedDeco(_dragPreviewId, norm, parentSize);

                    // 세이브
                    SaveCurrentPlacementData();

                    // 수량 갱신
                    RefreshAllCards();
                }
            }
        }
        _dragPreviewId = null;
    }

    private void UpdateDragPreviewPos(Vector2 screenPos)
    {
        Vector2 localPoint;
        Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_canvas.transform, screenPos, cam, out localPoint))
        {
            _dragPreviewGo.GetComponent<RectTransform>().anchoredPosition = localPoint;
        }
    }

    // ─── 배치물 드래그 알림 수신 (위치 이동 및 회수) ───────────────────────────

    public void NotifyDragBegin(GsiPlacedDeco deco)
    {
        // 끄는 동안 시각적 반투명 피드백
        var cg = deco.GetComponent<CanvasGroup>();
        if (cg == null) cg = deco.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0.6f;
    }

    // ─── 직접 터치 바이패스 조작 감지 (uGUI Sibling 가로막힘 우회) ───────────

    private void HandleGlobalInputDown(Vector2 screenPosition)
    {
        if (_manuallyDraggedDeco != null) return;

        // 1. 하단 서랍 패널 내부 클릭 시 무시 (패널 안은 독립 캔버스에서 uGUI 카드가 터치를 직접 처리)
        bool clickInPanel = RectTransformUtility.RectangleContainsScreenPoint(_panelRt, screenPosition, _canvas.worldCamera);
        if (clickInPanel) return;

        // 2. 배치된 데코들 중 터치 위치에 충돌하는 것이 있는지 역순(최상단에 렌더링된 요소 우선) 검사
        Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        for (int i = _placedDecos.Count - 1; i >= 0; i--)
        {
            var deco = _placedDecos[i];
            if (deco == null) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(deco.rectTransform, screenPosition, cam))
            {
                // 충돌 감지 -> 수동 드래그 상태로 전환
                _manuallyDraggedDeco = deco;
                _manuallyDraggedDeco.SetDragging(true);
                _lastMousePos = screenPosition;
                
                // 반투명 비주얼 피드백 적용
                NotifyDragBegin(_manuallyDraggedDeco);
                break;
            }
        }
    }

    private void HandleGlobalInputHold(Vector2 screenPosition)
    {
        if (_manuallyDraggedDeco == null) return;

        // 3. 드래그 이동 갱신
        Vector2 delta = screenPosition - _lastMousePos;
        float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
        _manuallyDraggedDeco.rectTransform.anchoredPosition += delta / scaleFactor;
        _lastMousePos = screenPosition;
    }

    private void HandleGlobalInputUp()
    {
        if (_manuallyDraggedDeco == null) return;

        var deco = _manuallyDraggedDeco;
        _manuallyDraggedDeco = null;
        
        // 4. 드래그 종료 처리
        deco.SetDragging(false);
        var cg = deco.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, deco.transform.position);

        // 드롭 좌표가 하단 패널 내부인지 체크 (패널 회수)
        bool dropInPanel = RectTransformUtility.RectangleContainsScreenPoint(_panelRt, screenPos, cam);

        if (dropInPanel)
        {
            _placedDecos.Remove(deco);
            Destroy(deco.gameObject);
            GsiUiSound.PlayClick(); // 회수 틱 사운드
        }
        else
        {
            // 위치 업데이트 및 좌표 재정규화
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_decoContainer, screenPos, cam, out localPoint))
            {
                Vector2 parentSize = _decoContainer.rect.size;
                if (parentSize.x > 0 && parentSize.y > 0)
                {
                    float nx = localPoint.x / parentSize.x + 0.5f;
                    float ny = localPoint.y / parentSize.y + 0.5f;
                    deco.NormalizedPos = new Vector2(nx, ny);
                }
            }
            
            // 마우스를 놓는 순간 약간의 관성 관유 속도를 튕겨주듯 인가
            deco.velocity = new Vector2(UnityEngine.Random.Range(-50f, 50f), UnityEngine.Random.Range(-50f, 50f));
        }

        // 저장 상태 동기화 및 패널 수량 갱신
        SaveCurrentPlacementData();
        RefreshAllCards();
    }

    // ─── 배치 영구 저장 데이터 쓰기 ──────────────────────────────────────────

    private void SaveCurrentPlacementData()
    {
        var dataList = new List<PlayerDecorations.PlacedDecoData>();
        for (int i = 0; i < _placedDecos.Count; i++)
        {
            var deco = _placedDecos[i];
            if (deco != null)
            {
                dataList.Add(new PlayerDecorations.PlacedDecoData(deco.ItemId, deco.NormalizedPos));
            }
        }
        PlayerDecorations.SavePlacedDecos(_sceneKey, dataList);
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnInputDown -= HandleGlobalInputDown;
            InputManager.Instance.OnInputHold -= HandleGlobalInputHold;
            InputManager.Instance.OnInputUp -= HandleGlobalInputUp;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}

/// <summary>
/// 인벤토리 데코 카드의 드래그 앤 드롭 입력을 직접 수신하여 EventSystem의 ScrollRect 간섭을 차단하고 
/// 드래그 타겟 지정을 강제하는 UI 드래그 바인딩 컴포넌트
/// </summary>
public sealed class GsiDecoCard : MonoBehaviour, 
    IPointerDownHandler, IPointerUpHandler, 
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public string ItemId;
    public GsiDecoPanelController PanelController;
    private ScrollRect _parentScroll;

    private void Awake()
    {
        _parentScroll = GetComponentInParent<ScrollRect>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"[GsiDecoCard] OnPointerEnter: ItemId={ItemId}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"[GsiDecoCard] OnPointerExit: ItemId={ItemId}");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[GsiDecoCard] OnPointerClick: ItemId={ItemId}");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // EventSystem에 이 오브젝트가 클릭되었음을 알려 드래그 주도권을 선점하도록 만듦
        Debug.Log($"[GsiDecoCard] OnPointerDown: ItemId={ItemId}");
        if (_parentScroll != null)
        {
            _parentScroll.enabled = false; // 스크롤 일시 정지 (드래그 탈취 방지)
            Debug.Log("[GsiDecoCard] Temporarily disabled parent ScrollRect to prevent drag hijacking.");
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"[GsiDecoCard] OnPointerUp: ItemId={ItemId}");
        if (_parentScroll != null)
        {
            _parentScroll.enabled = true; // 스크롤 원래대로 복구
            Debug.Log("[GsiDecoCard] Restored parent ScrollRect.");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[GsiDecoCard] OnBeginDrag: ItemId={ItemId}");
        if (PanelController != null)
        {
            PanelController.OnCardBeginDrag(eventData, ItemId);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (PanelController != null)
        {
            PanelController.OnCardDrag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"[GsiDecoCard] OnEndDrag: ItemId={ItemId}");
        if (PanelController != null)
        {
            PanelController.OnCardEndDrag(eventData);
        }
        if (_parentScroll != null)
        {
            _parentScroll.enabled = true; // 안전 장치로 복구
        }
    }
}
