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
    private RectTransform _decoContainer; // 배치된 데코들이 들어갈 레이어
    private RectTransform _panelRt;       // 슬라이드업 드로어 패널
    private RectTransform _tabIndicatorRt;// 닫혔을 때 노출되는 TAB 안내 가이드
    private RectTransform _contentRt;      // ScrollRect의 콘텐츠

    private bool _isOpen = false;
    private Coroutine _slideRoutine;
    private string _sceneKey;

    private readonly List<GsiPlacedDeco> _placedDecos = new List<GsiPlacedDeco>();
    private readonly List<DecoCardUI> _cards = new List<DecoCardUI>();

    // 드래그 중인 임시 프리뷰 오브젝트
    private GameObject _dragPreviewGo;
    private string _dragPreviewId;

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

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("GsiDecoPanelManager");
        go.transform.SetParent(canvas.transform, false);
        Instance = go.AddComponent<GsiDecoPanelController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _canvas = GetComponentInParent<Canvas>();
        _sceneKey = SceneManager.GetActiveScene().name == SceneNames.GSI ? "GSI" : "Lobby";

        BuildUi();
        LoadAndSpawnAllPlacedItems();
    }

    private void Start()
    {
        // 씬 시작 시 Tab 가이드는 켜고 패널은 숨김 상태로 대기
        if (_panelRt != null) _panelRt.anchoredPosition = new Vector2(0f, -PanelHeight - 20f);
        if (_tabIndicatorRt != null) _tabIndicatorRt.gameObject.SetActive(true);
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
            _tabIndicatorRt.gameObject.SetActive(false);
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

        if (!open)
        {
            _tabIndicatorRt.gameObject.SetActive(true);
        }
    }

    // ─── UI 생성 로직 ──────────────────────────────────────────────────

    private void BuildUi()
    {
        TMP_FontAsset font = TmpFontCache.LiberationSansSdf;

        // 1. 배치 영역 컨테이너 생성 (배경 이미지 위, UI 아래 정렬)
        var containerGo = new GameObject("DecoPlacementContainer", typeof(RectTransform));
        _decoContainer = containerGo.GetComponent<RectTransform>();
        _decoContainer.SetParent(_canvas.transform, false);
        GsiUiRuntimeWidgets.StretchFull(_decoContainer);
        _decoContainer.SetSiblingIndex(1); // 0번째가 배경, 1번째가 데코, 그 위에 노드와 허브 UI

        // 2. TAB 안내 가이드 텍스트 (화면 최하단 중앙)
        var tabGo = new GameObject("TabGuideText", typeof(RectTransform), typeof(TextMeshProUGUI));
        _tabIndicatorRt = tabGo.GetComponent<RectTransform>();
        _tabIndicatorRt.SetParent(_canvas.transform, false);
        _tabIndicatorRt.anchorMin = new Vector2(0.5f, 0f);
        _tabIndicatorRt.anchorMax = new Vector2(0.5f, 0f);
        _tabIndicatorRt.pivot = new Vector2(0.5f, 0f);
        _tabIndicatorRt.anchoredPosition = new Vector2(0f, 16f);
        _tabIndicatorRt.sizeDelta = new Vector2(400f, 30f);

        var tabTmp = tabGo.GetComponent<TextMeshProUGUI>();
        if (font != null) tabTmp.font = font;
        tabTmp.text = GameLocalization.GetUiString("deco.tab_hint", "Press TAB to decorate background");
        tabTmp.fontSize = 15f;
        tabTmp.alignment = TextAlignmentOptions.Center;
        tabTmp.fontStyle = FontStyles.Bold;
        tabTmp.color = new Color(0.9f, 0.9f, 0.95f, 0.7f);
        tabTmp.raycastTarget = false;

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
        scrollRt.anchorMin = new Vector2(0f, 0.5f);
        scrollRt.anchorMax = new Vector2(1f, 0.5f);
        scrollRt.pivot = new Vector2(0f, 0.5f);
        scrollRt.offsetMin = new Vector2(150f, 16f);
        scrollRt.offsetMax = new Vector2(-16f, -16f);

        var scrollImg = scrollGo.GetComponent<Image>();
        scrollImg.sprite = null;
        scrollImg.color = Color.clear;
        scrollImg.raycastTarget = true;

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.vertical = false;
        scroll.horizontal = true;

        // Viewport
        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        var viewportRt = viewportGo.GetComponent<RectTransform>();
        viewportRt.SetParent(scrollRt, false);
        GsiUiRuntimeWidgets.StretchFull(viewportRt);
        viewportGo.GetComponent<Mask>().showMaskGraphic = false;
        var viewportImg = viewportGo.GetComponent<Image>();
        viewportImg.sprite = null;
        viewportImg.color = Color.clear;
        viewportImg.raycastTarget = true;
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

            // 5. 카드 드래그 앤 드롭 트리거 바인딩
            var trigger = cardGo.AddComponent<EventTrigger>();
            
            var beginEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginEntry.callback.AddListener((data) => OnCardBeginDrag((PointerEventData)data, def.Id));
            trigger.triggers.Add(beginEntry);

            var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener((data) => OnCardDrag((PointerEventData)data));
            trigger.triggers.Add(dragEntry);

            var endEntry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            endEntry.callback.AddListener((data) => OnCardEndDrag((PointerEventData)data));
            trigger.triggers.Add(endEntry);
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

    private void OnCardBeginDrag(PointerEventData eventData, string itemId)
    {
        if (PlayerDecorations.GetAvailableCount(itemId) <= 0)
        {
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

    private void OnCardDrag(PointerEventData eventData)
    {
        if (_dragPreviewGo != null)
        {
            UpdateDragPreviewPos(eventData.position);
        }
    }

    private void OnCardEndDrag(PointerEventData eventData)
    {
        if (_dragPreviewGo == null) return;

        Destroy(_dragPreviewGo);
        _dragPreviewGo = null;

        // 드롭된 마우스 포인터의 위치가 하단 드로어 패널 내부인지 외부인지 체크
        bool dropInPanel = eventData.position.y / _canvas.scaleFactor <= PanelHeight;

        if (!dropInPanel)
        {
            // 컨테이너 크기 기준 정규화 좌표 구함
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_decoContainer, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                Vector2 parentSize = _decoContainer.rect.size;
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
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_canvas.transform, screenPos, _canvas.worldCamera, out localPoint))
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

    public void NotifyDragEnd(GsiPlacedDeco deco)
    {
        var cg = deco.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        // 드롭 좌표가 하단 패널 내부인지 체크 (패널 회수)
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, deco.transform.position);
        bool dropInPanel = screenPos.y / _canvas.scaleFactor <= PanelHeight;

        if (dropInPanel)
        {
            // 인스턴스 파괴 및 목록 제거
            _placedDecos.Remove(deco);
            Destroy(deco.gameObject);
            GsiUiSound.PlayClick(); // 회수 틱 사운드
        }
        else
        {
            // 위치 업데이트 및 좌표 재정규화
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_decoContainer, screenPos, _canvas.worldCamera, out localPoint))
            {
                Vector2 parentSize = _decoContainer.rect.size;
                if (parentSize.x > 0 && parentSize.y > 0)
                {
                    float nx = localPoint.x / parentSize.x + 0.5f;
                    float ny = localPoint.y / parentSize.y + 0.5f;
                    deco.NormalizedPos = new Vector2(nx, ny);
                }
            }
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
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
