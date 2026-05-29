using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 시험 브리핑: 화면 전체를 덮고, 아무 곳이나 클릭 시 시작. 하단에 안내 문구.
/// </summary>
public static class GsiTestBriefingUi
{
    public const string PanelName = "GsiTestBriefingPanel";

    public sealed class Refs
    {
        public GameObject Root;
        public TextMeshProUGUI TitleTmp;
        public TextMeshProUGUI BodyTmp;
        public TextMeshProUGUI HintTmp;
        public Button FullScreenTapButton;
    }

    public static Refs Ensure(Transform parent, UnityAction onStartClicked)
    {
        if (parent == null)
        {
            return null;
        }

        Transform existing = parent.Find(PanelName);
        if (existing != null && existing.Find("Box") != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
            else
#endif
            {
                Object.Destroy(existing.gameObject);
            }

            existing = null;
        }

        if (existing != null)
        {
            var refs = new Refs
            {
                Root = existing.gameObject,
                TitleTmp = existing.Find("Title")?.GetComponent<TextMeshProUGUI>(),
                BodyTmp = existing.Find("Body")?.GetComponent<TextMeshProUGUI>(),
                HintTmp = existing.Find("TapHint")?.GetComponent<TextMeshProUGUI>(),
                FullScreenTapButton = existing.GetComponent<Button>()
            };
            if (refs.FullScreenTapButton != null)
            {
                refs.FullScreenTapButton.onClick.RemoveListener(onStartClicked);
                refs.FullScreenTapButton.onClick.AddListener(onStartClicked);
            }

            ApplyHintText(refs);
            return refs;
        }

        var go = new GameObject(PanelName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        var bg = go.AddComponent<Image>();
        GsiArcaneUi.ApplyFullscreenBackground(bg, true);

        var tapBtn = go.AddComponent<Button>();
        tapBtn.targetGraphic = bg;
        tapBtn.transition = Selectable.Transition.None;
        tapBtn.onClick.AddListener(onStartClicked);

        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(go.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.66f);
        titleRt.anchorMax = new Vector2(1f, 0.92f);
        titleRt.offsetMin = new Vector2(120f, 0f);
        titleRt.offsetMax = new Vector2(-120f, -18f);
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.fontSize = 46;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.characterSpacing = 1.5f;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = GsiUiAppearance.TextPrimary;
        titleTmp.raycastTarget = false;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            titleTmp.font = TmpFontCache.LiberationSansSdf;
        }

        var bodyGo = new GameObject("Body", typeof(RectTransform));
        bodyGo.transform.SetParent(go.transform, false);
        var bodyRt = bodyGo.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0.16f, 0.27f);
        bodyRt.anchorMax = new Vector2(0.84f, 0.64f);
        bodyRt.offsetMin = new Vector2(0f, 18f);
        bodyRt.offsetMax = new Vector2(0f, 0f);
        var bodyTmp = bodyGo.AddComponent<TextMeshProUGUI>();
        bodyTmp.fontSize = 25;
        bodyTmp.alignment = TextAlignmentOptions.Top;
        bodyTmp.color = GsiUiAppearance.TextSecondary;
        bodyTmp.enableWordWrapping = true;
        bodyTmp.raycastTarget = false;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            bodyTmp.font = TmpFontCache.LiberationSansSdf;
        }

        var hintGo = new GameObject("TapHint", typeof(RectTransform));
        hintGo.transform.SetParent(go.transform, false);
        var hintRt = hintGo.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0f, 0f);
        hintRt.anchorMax = new Vector2(1f, 0.2f);
        hintRt.offsetMin = new Vector2(32f, 28f);
        hintRt.offsetMax = new Vector2(-32f, 0f);
        var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
        hintTmp.fontSize = 22;
        hintTmp.fontStyle = FontStyles.Italic;
        hintTmp.alignment = TextAlignmentOptions.Bottom;
        hintTmp.color = GsiUiAppearance.TextSecondary;
        hintTmp.raycastTarget = false;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            hintTmp.font = TmpFontCache.LiberationSansSdf;
        }

        ApplyHintText(new Refs { HintTmp = hintTmp });

        go.SetActive(false);
        return new Refs
        {
            Root = go,
            TitleTmp = titleTmp,
            BodyTmp = bodyTmp,
            HintTmp = hintTmp,
            FullScreenTapButton = tapBtn
        };
    }

    private static void ApplyHintText(Refs refs)
    {
        if (refs?.HintTmp == null)
        {
            return;
        }

        refs.HintTmp.text = GameLocalization.GetUiString(UiStringKeys.BriefingTapAnywhere,
            "Click anywhere to start");
    }

    public static void ApplyContent(Refs refs)
    {
        if (refs?.TitleTmp == null || refs.BodyTmp == null)
        {
            return;
        }

        GsiTestBriefingTexts.GetBriefingTitleAndBody(out string title, out string body);
        refs.TitleTmp.text = title;
        refs.BodyTmp.text = body;
        ApplyHintText(refs);
    }

    public static void RefreshChrome(Refs refs)
    {
        if (refs?.Root == null)
        {
            return;
        }

        Image bg = refs.Root.GetComponent<Image>();
        if (bg != null)
        {
            GsiArcaneUi.ApplyFullscreenBackground(bg, true);
        }

        if (refs.TitleTmp != null)
        {
            refs.TitleTmp.color = GsiUiAppearance.TextPrimary;
        }

        if (refs.BodyTmp != null)
        {
            refs.BodyTmp.color = GsiUiAppearance.TextSecondary;
        }

        if (refs.HintTmp != null)
        {
            refs.HintTmp.color = GsiUiAppearance.TextSecondary;
        }
    }
}
