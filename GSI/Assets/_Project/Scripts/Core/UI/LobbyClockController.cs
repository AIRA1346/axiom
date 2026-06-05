using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ArchE.Game;

/// <summary>
/// 로비 상단 좌측의 시스템 시간(시계) 표시 및 타이틀 레이아웃 제어를 담당하는 컨트롤러입니다.
/// MainMenuController로부터 분리되어 단일 책임 원칙(SRP)을 준수합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class LobbyClockController : MonoBehaviour
{
    private const float LobbyTopLeftFontSize = 24f;
    private const float LobbyTopLeftTitleClockGap = 12f;
    private const float LobbyTopLeftTimeMinWidth = 200f;
    private const float LobbyTopLeftBarMaxHeight = 300f;
    private const float LobbyTopLeftWidthFraction = 0.5f;
    private const float LobbyActionRowSideInset = 28f;
    private const float LobbyEconomyStripTopInset = 20f;

    private RectTransform _rectTransform;
    private RectTransform _lobbyRoot;
    
    private TextMeshProUGUI _lobbyTopLeftTitleTmp;
    private TextMeshProUGUI _lobbyTopLeftTimeTmp;
    private long _lobbyClockSecondStamp = -1L;
    private float _lobbyTopLeftBarLastLayoutWidth = -1f;

    private bool _localeSubscribed = false;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _lobbyRoot = transform.parent as RectTransform;
    }

    private void Start()
    {
        InitializeClockBar();
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (Application.isPlaying && !_localeSubscribed)
        {
            GameLocalization.UiLocaleChanged += OnLocaleChanged;
            _localeSubscribed = true;
        }
    }

    private void UnsubscribeEvents()
    {
        if (_localeSubscribed)
        {
            GameLocalization.UiLocaleChanged -= OnLocaleChanged;
            _localeSubscribed = false;
        }
    }

    private void OnLocaleChanged()
    {
        RefreshTitleAndTime();
    }

    public void InitializeClockBar()
    {
        if (_lobbyRoot == null) return;

        // 자식 텍스트 탐색 및 없으면 생성
        Transform titleTf = transform.Find("LobbyTopLeftTitle");
        if (titleTf != null) _lobbyTopLeftTitleTmp = titleTf.GetComponent<TextMeshProUGUI>();
        else _lobbyTopLeftTitleTmp = CreateTitleText("LobbyTopLeftTitle", transform);

        Transform clockTf = transform.Find("LobbyTopLeftClock");
        if (clockTf != null) _lobbyTopLeftTimeTmp = clockTf.GetComponent<TextMeshProUGUI>();
        else _lobbyTopLeftTimeTmp = CreateClockText("LobbyTopLeftClock", transform);

        // 레이아웃 속성 설정
        var h = GetComponent<HorizontalLayoutGroup>();
        if (h == null) h = gameObject.AddComponent<HorizontalLayoutGroup>();
        
        h.childAlignment = TextAnchor.UpperLeft;
        h.spacing = LobbyTopLeftTitleClockGap;
        h.padding = new RectOffset(0, 0, 0, 0);
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;

        EnforceTitleBeforeClock(transform);
        ApplyClockBarRect(_rectTransform);

        _lobbyTopLeftBarLastLayoutWidth = -1f;
        RefreshTitleAndTime();
    }

    private void Update()
    {
        if (!Application.isPlaying || _lobbyRoot == null || _lobbyTopLeftTitleTmp == null)
        {
            return;
        }

        // 해상도 가로 변경 감지 시 크기 및 높이 재계산
        float w = _lobbyRoot.rect.width;
        if (!Mathf.Approximately(w, _lobbyTopLeftBarLastLayoutWidth))
        {
            _lobbyTopLeftBarLastLayoutWidth = w;
            ApplyClockBarRect(_rectTransform);
            RebuildBarHeights();
        }

        if (_lobbyTopLeftTimeTmp == null) return;

        DateTime now = DateTime.Now;
        long tickSecond = now.Ticks / TimeSpan.TicksPerSecond;
        if (tickSecond == _lobbyClockSecondStamp)
        {
            return;
        }

        _lobbyClockSecondStamp = tickSecond;
        _lobbyTopLeftTimeTmp.text = FormatDateTime(now);
        RebuildBarHeights();
    }

    public void RefreshTitleAndTime()
    {
        if (_lobbyTopLeftTitleTmp != null)
        {
            _lobbyTopLeftTitleTmp.text = GameLocalization.GetUiString(UiStringKeys.LobbyTopLeftTitle, "The Axiom");
            _lobbyTopLeftTitleTmp.color = GsiUiAppearance.TextPrimary;
        }

        if (_lobbyTopLeftTimeTmp != null)
        {
            _lobbyTopLeftTimeTmp.text = FormatDateTime(DateTime.Now);
            _lobbyTopLeftTimeTmp.color = GsiUiAppearance.TextSecondary;
        }

        _lobbyClockSecondStamp = -1L;
        RebuildBarHeights();
    }

    private void ApplyClockBarRect(RectTransform barRt)
    {
        if (_lobbyRoot == null || barRt == null) return;

        float panelW = _lobbyRoot.rect.width;
        float inner = LobbyActionRowSideInset;
        float barW = Mathf.Max(120f, panelW * LobbyTopLeftWidthFraction - 2f * inner);
        
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(0f, 1f);
        barRt.pivot = new Vector2(0f, 1f);
        barRt.anchoredPosition = new Vector2(inner, -LobbyEconomyStripTopInset);
        barRt.sizeDelta = new Vector2(barW, 80f);
    }

    private void RebuildBarHeights()
    {
        if (_lobbyRoot == null || _lobbyTopLeftTitleTmp == null || _lobbyTopLeftTimeTmp == null) return;

        ApplyColumnWidths(_rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
        
        _lobbyTopLeftTitleTmp.ForceMeshUpdate(true);
        _lobbyTopLeftTimeTmp.ForceMeshUpdate(true);

        float titleW = _lobbyTopLeftTitleTmp.rectTransform.rect.width;
        if (titleW < 2f) return;

        float titleH = _lobbyTopLeftTitleTmp.GetPreferredValues(_lobbyTopLeftTitleTmp.text, titleW, 0f).y;
        float timeH = _lobbyTopLeftTimeTmp.GetPreferredValues(_lobbyTopLeftTimeTmp.text, _lobbyTopLeftTimeTmp.rectTransform.rect.width, 0f).y;
        
        float rowH = Mathf.Max(LobbyTopLeftFontSize, Mathf.Max(titleH, timeH) + 4f);
        float barH = Mathf.Min(LobbyTopLeftBarMaxHeight, rowH);
        
        if (!Mathf.Approximately(_rectTransform.sizeDelta.y, barH))
        {
            var sd = _rectTransform.sizeDelta;
            _rectTransform.sizeDelta = new Vector2(sd.x, barH);
        }
    }

    private void ApplyColumnWidths(RectTransform bar)
    {
        if (bar == null || _lobbyTopLeftTitleTmp == null || _lobbyTopLeftTimeTmp == null) return;

        LayoutElement titleLe = _lobbyTopLeftTitleTmp.GetComponent<LayoutElement>() ?? _lobbyTopLeftTitleTmp.gameObject.AddComponent<LayoutElement>();
        LayoutElement timeLe = _lobbyTopLeftTimeTmp.GetComponent<LayoutElement>() ?? _lobbyTopLeftTimeTmp.gameObject.AddComponent<LayoutElement>();

        if (string.IsNullOrEmpty(_lobbyTopLeftTimeTmp.text))
        {
            _lobbyTopLeftTimeTmp.text = FormatDateTime(DateTime.Now);
        }

        timeLe.flexibleWidth = 0f;
        timeLe.minWidth = LobbyTopLeftTimeMinWidth;
        timeLe.preferredWidth = MeasureTimeColumnWidth(_lobbyTopLeftTimeTmp);

        float barW = bar.sizeDelta.x;
        float timeW = timeLe.preferredWidth;
        float maxTitleW = Mathf.Max(48f, barW - timeW - LobbyTopLeftTitleClockGap);

        _lobbyTopLeftTitleTmp.ForceMeshUpdate(true);
        float naturalW = _lobbyTopLeftTitleTmp.GetPreferredValues(_lobbyTopLeftTitleTmp.text, 1e4f, 0f).x;
        
        titleLe.minWidth = 48f;
        titleLe.flexibleWidth = 0f;
        titleLe.preferredWidth = naturalW <= maxTitleW ? Mathf.Max(48f, naturalW) : maxTitleW;
    }

    private static string FormatDateTime(DateTime now)
    {
        return now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
    }

    private static float MeasureTimeColumnWidth(TextMeshProUGUI timeTmp)
    {
        if (timeTmp == null || string.IsNullOrEmpty(timeTmp.text)) return LobbyTopLeftTimeMinWidth;
        timeTmp.ForceMeshUpdate(true);
        float w = timeTmp.GetPreferredValues(timeTmp.text, 0f, 0f).x;
        return Mathf.Max(LobbyTopLeftTimeMinWidth, w + 4f);
    }

    private static TextMeshProUGUI CreateTitleText(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        ApplySharedTypography(tmp);
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        
        var le = go.AddComponent<LayoutElement>();
        le.minWidth = 48f;
        le.preferredWidth = 48f;
        le.flexibleWidth = 0f;
        le.minHeight = LobbyTopLeftFontSize;

        return tmp;
    }

    private static TextMeshProUGUI CreateClockText(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        ApplySharedTypography(tmp);
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        var le = go.AddComponent<LayoutElement>();
        le.minWidth = LobbyTopLeftTimeMinWidth;
        le.preferredWidth = LobbyTopLeftTimeMinWidth;
        le.flexibleWidth = 0f;
        le.minHeight = LobbyTopLeftFontSize;

        return tmp;
    }

    private static void ApplySharedTypography(TextMeshProUGUI tmp)
    {
        if (TmpFontCache.LiberationSansSdf != null) tmp.font = TmpFontCache.LiberationSansSdf;
        tmp.fontSize = LobbyTopLeftFontSize;
        tmp.raycastTarget = false;
        GsiUiRuntimeWidgets.ApplyEconomyLineTypography(tmp);
    }

    private static void EnforceTitleBeforeClock(Transform barTf)
    {
        if (barTf == null) return;
        Transform titleTf = barTf.Find("LobbyTopLeftTitle");
        Transform clockTf = barTf.Find("LobbyTopLeftClock");
        if (titleTf != null) titleTf.SetAsFirstSibling();
        if (clockTf != null)
        {
            if (titleTf != null) clockTf.SetSiblingIndex(titleTf.GetSiblingIndex() + 1);
            else clockTf.SetAsLastSibling();
        }
    }
}
