using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 상점·인벤토리 등 런타임 생성 UI에서 공통으로 쓰는 최소 위젯(레이아웃 헬퍼, TMP, 기본 버튼).
/// </summary>
public static class GsiUiRuntimeWidgets
{
    private static Sprite _defaultSlicedUiSprite;

    public static GameObject CreateUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Unity 6에서는 <c>Resources.GetBuiltinResource("UI/Skin/UISprite.psd")</c>가 실패할 수 있어,
    /// 흰색 9-slice 스프라이트를 한 번 만들어 재사용합니다.
    /// </summary>
    public static Sprite GetOrCreateDefaultSlicedUiSprite()
    {
        if (_defaultSlicedUiSprite != null)
        {
            return _defaultSlicedUiSprite;
        }

        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "GSI_DefaultUiSlicedTex";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var c = Color.white;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply(false, true);

        const float ppu = 100f;
        var border = new Vector4(10f, 10f, 10f, 10f);
        _defaultSlicedUiSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            ppu,
            0,
            SpriteMeshType.FullRect,
            border);
        _defaultSlicedUiSprite.name = "GSI_DefaultUiSliced";
        return _defaultSlicedUiSprite;
    }

    /// <summary>런타임 생성 <see cref="Image"/>에 기본 9-slice 스프라이트를 넣습니다(반투명 패널·버튼 등).</summary>
    public static void EnsureUiSlicedBackgroundSprite(Image img)
    {
        if (img == null || img.sprite != null)
        {
            return;
        }

        Sprite s = GetOrCreateDefaultSlicedUiSprite();
        if (s != null)
        {
            img.sprite = s;
            img.type = Image.Type.Sliced;
        }
    }

    public static TextMeshProUGUI CreateTmp(Transform parent, string text, float size, FontStyles style)
    {
        var go = CreateUiObject("Text", parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = GsiUiAppearance.TextPrimary;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            tmp.font = TmpFontCache.LiberationSansSdf;
        }

        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>상점·인벤 리스트의 제목/가격 등 본문 라벨 가독성(자간·말줄임).</summary>
    public static void ApplyListRowPrimaryLabel(TextMeshProUGUI tmp)
    {
        if (tmp == null)
        {
            return;
        }

        tmp.characterSpacing = 0.35f;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
    }

    /// <summary>가격·부가 상태 등 2차 라벨.</summary>
    public static void ApplyListRowSecondaryLabel(TextMeshProUGUI tmp)
    {
        if (tmp == null)
        {
            return;
        }

        tmp.characterSpacing = 0.2f;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
    }

    /// <summary>골드·응시권 등 비대화형 경제 한 줄(서브바): 자간 + 레이캐스트 끔(아래 버튼 클릭 가로채기 방지).</summary>
    public static void ApplyEconomyLineTypography(TextMeshProUGUI tmp)
    {
        if (tmp == null)
        {
            return;
        }

        tmp.characterSpacing = 0.22f;
        tmp.raycastTarget = false;
    }

    /// <summary>미니멀 UI: 카드 그림자 없음(기존 씬에 남은 Shadow 는 제거).</summary>
    public static void ApplyRowCardShadow(Graphic rowBackground)
    {
        if (rowBackground == null)
        {
            return;
        }

        Shadow sh = rowBackground.gameObject.GetComponent<Shadow>();
        if (sh != null)
        {
            UnityEngine.Object.Destroy(sh);
        }
    }

    /// <summary>미니멀 UI: 테두리(Outline) 없음.</summary>
    public static void ApplyUiControlRim(Graphic graphic)
    {
        if (graphic == null)
        {
            return;
        }

        Outline ol = graphic.gameObject.GetComponent<Outline>();
        if (ol != null)
        {
            UnityEngine.Object.Destroy(ol);
        }
    }

    /// <summary>미니멀 UI: 구분선 비사용(호환용 빈 구현).</summary>
    public static void AddHeaderBottomHairline(Transform headerTransform)
    {
    }

    /// <summary>미니멀 UI: 구분선 비사용(호환용 빈 구현).</summary>
    public static void AddTopHairline(Transform parent)
    {
    }

    public static Button CreateButton(Transform parent, string label, UnityAction onClick)
    {
        var go = CreateUiObject("Button", parent);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(148f, 48f);
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        ApplySecondaryButtonColorBlock(btn);
        btn.onClick.AddListener(onClick);

        var textGo = CreateUiObject("Label", go.transform);
        StretchFull(textGo.GetComponent<RectTransform>());
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = GsiUiAppearance.TextPrimary;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            tmp.font = TmpFontCache.LiberationSansSdf;
        }

        return btn;
    }

    private static void ApplySecondaryButtonColorBlock(Button btn)
    {
        Color baseCol = GsiUiAppearance.SecondaryButton;
        ColorBlock c = btn.colors;
        c.fadeDuration = 0.1f;
        c.colorMultiplier = 1f;
        c.normalColor = baseCol;
        c.highlightedColor = new Color(
            Mathf.Min(1f, baseCol.r * 1.1f),
            Mathf.Min(1f, baseCol.g * 1.1f),
            Mathf.Min(1f, baseCol.b * 1.1f), 1f);
        c.pressedColor = new Color(baseCol.r * 0.88f, baseCol.g * 0.88f, baseCol.b * 0.88f, 1f);
        c.selectedColor = c.highlightedColor;
        c.disabledColor = new Color(baseCol.r * 0.55f, baseCol.g * 0.55f, baseCol.b * 0.55f, 0.55f);
        btn.colors = c;
    }

    /// <summary>
    /// 상점·인벤 헤더의 Settings / Back 등 보조 버튼에 다크·라이트 외관 색을 맞춥니다.
    /// </summary>
    public static void ApplySecondaryHeaderButtonLook(Button btn)
    {
        if (btn == null)
        {
            return;
        }

        btn.transition = Selectable.Transition.ColorTint;
        ApplySecondaryButtonColorBlock(btn);

        if (btn.targetGraphic is Graphic g)
        {
            g.color = Color.white;
        }

        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.color = GsiUiAppearance.TextPrimary;
        }
    }

    /// <summary>
    /// Buy/Equip 등 스킨 액센트 색을 쓰는 주요 액션 버튼에 하이라이트·배경 틴트를 맞춥니다.
    /// </summary>
    public static void ApplyAccentTintedActionButton(Button btn)
    {
        if (btn == null)
        {
            return;
        }

        btn.transition = Selectable.Transition.ColorTint;
        Color baseCol = GsiUiAppearance.PrimaryActionButton;
        ColorBlock c = btn.colors;
        c.fadeDuration = 0.1f;
        c.colorMultiplier = 1f;
        c.normalColor = baseCol;
        c.highlightedColor = new Color(
            Mathf.Min(1f, baseCol.r * 1.12f),
            Mathf.Min(1f, baseCol.g * 1.12f),
            Mathf.Min(1f, baseCol.b * 1.12f), 1f);
        c.pressedColor = new Color(
            baseCol.r * 0.88f,
            baseCol.g * 0.88f,
            baseCol.b * 0.88f, 1f);
        c.selectedColor = c.highlightedColor;
        c.disabledColor = new Color(baseCol.r * 0.55f, baseCol.g * 0.55f, baseCol.b * 0.55f, 0.55f);
        btn.colors = c;
        if (btn.targetGraphic is Graphic g)
        {
            g.color = Color.white;
        }
    }
}
