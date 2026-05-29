using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// G.S.I 허브에서 통합 시험 응시 기록을 스크롤로 확인하는 전면 오버레이.
/// </summary>
public sealed class GsiUnifiedExamHistoryOverlayRoot : MonoBehaviour
{
    public const string RootObjectName = "GsiUnifiedExamHistoryOverlay";

    private TextMeshProUGUI _titleTmp;
    private TextMeshProUGUI _emptyTmp;
    private Transform _content;
    private ScrollRect _scroll;
    private Button _closeBtn;
    private bool _localeSubscribed;

    public static void Toggle(Transform anyUnderCanvas)
    {
        Canvas canvas = anyUnderCanvas.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        Transform existing = canvas.transform.Find(RootObjectName);
        if (existing != null)
        {
            bool next = !existing.gameObject.activeSelf;
            existing.gameObject.SetActive(next);
            if (next)
            {
                existing.SetAsLastSibling();
                var host = existing.GetComponent<GsiUnifiedExamHistoryOverlayRoot>();
                if (host != null)
                {
                    host.Refresh();
                }
            }

            return;
        }

        var go = new GameObject(RootObjectName, typeof(RectTransform), typeof(GsiUnifiedExamHistoryOverlayRoot));
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        var hostNew = go.GetComponent<GsiUnifiedExamHistoryOverlayRoot>();
        hostNew.BuildUi();
        hostNew.Refresh();
        go.SetActive(true);
        go.transform.SetAsLastSibling();
    }

    private void OnEnable()
    {
        if (_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged += OnLocaleChanged;
        _localeSubscribed = true;
    }

    private void OnDisable()
    {
        if (!_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged -= OnLocaleChanged;
        _localeSubscribed = false;
    }

    private void OnLocaleChanged()
    {
        if (gameObject.activeInHierarchy)
        {
            Refresh();
        }
    }

    private void BuildUi()
    {
        var bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.07f, 0.1f, 0.96f);
        bg.raycastTarget = true;

        var sheet = new GameObject("Sheet", typeof(RectTransform));
        sheet.transform.SetParent(transform, false);
        var sheetRt = sheet.GetComponent<RectTransform>();
        sheetRt.anchorMin = new Vector2(0.5f, 0.5f);
        sheetRt.anchorMax = new Vector2(0.5f, 0.5f);
        sheetRt.pivot = new Vector2(0.5f, 0.5f);
        sheetRt.sizeDelta = new Vector2(920f, 780f);
        sheetRt.anchoredPosition = Vector2.zero;
        var sheetImg = sheet.AddComponent<Image>();
        sheetImg.color = new Color(0.12f, 0.14f, 0.19f, 1f);
        sheetImg.raycastTarget = true;
        GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(sheetImg);

        var header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(sheet.transform, false);
        var headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = Vector2.zero;
        headerRt.sizeDelta = new Vector2(0f, 72f);
        var headerH = header.AddComponent<HorizontalLayoutGroup>();
        headerH.padding = new RectOffset(20, 20, 12, 12);
        headerH.spacing = 16f;
        headerH.childAlignment = TextAnchor.MiddleLeft;
        headerH.childForceExpandHeight = true;
        headerH.childForceExpandWidth = true;

        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(header.transform, false);
        _titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        _titleTmp.raycastTarget = false;
        _titleTmp.fontSize = 26;
        _titleTmp.fontStyle = FontStyles.Bold;
        _titleTmp.alignment = TextAlignmentOptions.Left;
        _titleTmp.color = GsiUiAppearance.TextPrimary;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            _titleTmp.font = TmpFontCache.LiberationSansSdf;
        }

        var titleLe = titleGo.AddComponent<LayoutElement>();
        titleLe.flexibleWidth = 1f;

        var closeGo = new GameObject("Close", typeof(RectTransform));
        closeGo.transform.SetParent(header.transform, false);
        var closeImg = closeGo.AddComponent<Image>();
        closeImg.color = GsiUiAppearance.SecondaryButton;
        closeImg.raycastTarget = true;
        GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(closeImg);
        var closeBtn = closeGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(() => gameObject.SetActive(false));
        _closeBtn = closeBtn;
        var closeLe = closeGo.AddComponent<LayoutElement>();
        closeLe.preferredWidth = 140f;
        closeLe.flexibleWidth = 0f;
        var closeLabelGo = new GameObject("Text", typeof(RectTransform));
        closeLabelGo.transform.SetParent(closeGo.transform, false);
        StretchFull(closeLabelGo.GetComponent<RectTransform>());
        var closeTmp = closeLabelGo.AddComponent<TextMeshProUGUI>();
        closeTmp.raycastTarget = false;
        closeTmp.fontSize = 20;
        closeTmp.alignment = TextAlignmentOptions.Center;
        closeTmp.color = GsiUiAppearance.TextPrimary;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            closeTmp.font = TmpFontCache.LiberationSansSdf;
        }

        var scrollGo = new GameObject("Scroll", typeof(RectTransform));
        scrollGo.transform.SetParent(sheet.transform, false);
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(16f, 20f);
        scrollRt.offsetMax = new Vector2(-16f, -80f);
        _scroll = scrollGo.AddComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 28f;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRt = viewport.GetComponent<RectTransform>();
        StretchFull(vpRt);
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.02f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;
        _scroll.viewport = vpRt;

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 14f;
        vlg.padding = new RectOffset(8, 8, 8, 16);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        _content = content.transform;
        _scroll.content = contentRt;

        var emptyGo = new GameObject("Empty", typeof(RectTransform));
        emptyGo.transform.SetParent(sheet.transform, false);
        var emptyRt = emptyGo.GetComponent<RectTransform>();
        emptyRt.anchorMin = new Vector2(0f, 0f);
        emptyRt.anchorMax = new Vector2(1f, 1f);
        emptyRt.offsetMin = new Vector2(24f, 24f);
        emptyRt.offsetMax = new Vector2(-24f, -88f);
        _emptyTmp = emptyGo.AddComponent<TextMeshProUGUI>();
        _emptyTmp.raycastTarget = false;
        _emptyTmp.fontSize = 22;
        _emptyTmp.alignment = TextAlignmentOptions.Center;
        _emptyTmp.color = GsiUiAppearance.TextSecondary;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            _emptyTmp.font = TmpFontCache.LiberationSansSdf;
        }

        emptyGo.SetActive(false);
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public void Refresh()
    {
        if (_titleTmp != null)
        {
            _titleTmp.text = GameLocalization.GetUiString(UiStringKeys.HistoryUnifiedTitle, "Unified exam history");
        }

        if (_closeBtn != null)
        {
            TextMeshProUGUI closeLabel = _closeBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (closeLabel != null)
            {
                closeLabel.text = GameLocalization.GetUiString(UiStringKeys.HistoryUnifiedClose, "Close");
            }
        }

        if (_emptyTmp != null)
        {
            _emptyTmp.text = GameLocalization.GetUiString(UiStringKeys.HistoryUnifiedEmpty, "No completed unified exams yet.");
        }

        if (_content == null)
        {
            return;
        }

        for (int i = _content.childCount - 1; i >= 0; i--)
        {
            GsiRuntimeUiBootstrap.DestroyObjectForRuntimeUi(_content.GetChild(i).gameObject);
        }

        if (PlayerDataManager.Instance == null)
        {
            SetEmptyVisible(true);
            return;
        }

        IReadOnlyList<UnifiedExamHistoryEntry> entries = PlayerDataManager.Instance.GetUnifiedExamHistoryNewestFirst();
        if (entries == null || entries.Count == 0)
        {
            SetEmptyVisible(true);
            return;
        }

        SetEmptyVisible(false);

        for (int e = 0; e < entries.Count; e++)
        {
            UnifiedExamHistoryEntry entry = entries[e];
            string passWord = entry.OverallPass
                ? GameLocalization.GetUiString(UiStringKeys.ResultUnifiedFinalPass, "Final pass")
                : GameLocalization.GetUiString(UiStringKeys.ResultUnifiedFinalFail, "Final fail");
            string header = GameLocalization.FormatUiString(
                UiStringKeys.HistoryUnifiedRowFmt,
                "{0} | G{1} | {2:F1}/700 | {3} | Tier {4} | Cut {5:F0}",
                entry.Timestamp,
                entry.Grade,
                entry.Total,
                passWord,
                entry.RewardTier,
                entry.Cutoff);

            var subjHeader = GameLocalization.GetUiString(UiStringKeys.ResultUnifiedSubjectsHeader, "[ Subjects ]");
            string[] segs = UnifiedExamHistoryStorage.SplitSegments(entry.SegmentsJoined);
            var body = new StringBuilder();
            body.AppendLine(header);
            body.AppendLine(subjHeader);
            foreach (string line in segs)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    body.AppendLine(line.TrimEnd());
                }
            }

            var row = new GameObject($"Entry_{e}", typeof(RectTransform));
            row.transform.SetParent(_content, false);
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.flexibleWidth = 1f;
            rowLe.minHeight = 40f;
            var tmp = row.AddComponent<TextMeshProUGUI>();
            tmp.text = body.ToString().TrimEnd();
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.color = GsiUiAppearance.TextPrimary;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            tmp.margin = new Vector4(6f, 8f, 6f, 8f);
            if (TmpFontCache.LiberationSansSdf != null)
            {
                tmp.font = TmpFontCache.LiberationSansSdf;
            }

            var fit = row.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        Canvas.ForceUpdateCanvases();
        if (_scroll != null)
        {
            _scroll.verticalNormalizedPosition = 1f;
        }
    }

    private void SetEmptyVisible(bool visible)
    {
        if (_emptyTmp != null)
        {
            _emptyTmp.gameObject.SetActive(visible);
        }

        if (_scroll != null)
        {
            _scroll.gameObject.SetActive(!visible);
        }
    }
}
