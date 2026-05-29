using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dark academy / arcane visual layer shared by every runtime screen.
/// Generates its own parchment-rune textures so the theme works without external art files.
/// </summary>
public static class GsiArcaneUi
{
    public const string CanvasBackdropName = "ArcaneBackdrop";

    private static Sprite _backgroundSprite;
    private static Sprite _lobbyFarSprite;
    private static Sprite _lobbyMidSprite;
    private static Sprite _lobbyNearSprite;

    public static Sprite BackgroundSprite
    {
        get
        {
            if (_backgroundSprite == null)
            {
                _backgroundSprite = CreateArcaneSprite(1024, 576, 0);
            }

            return _backgroundSprite;
        }
    }

    public static Sprite LobbyFarSprite
    {
        get
        {
            if (_lobbyFarSprite == null)
            {
                _lobbyFarSprite = CreateArcaneSprite(1024, 576, 1);
            }

            return _lobbyFarSprite;
        }
    }

    public static Sprite LobbyMidSprite
    {
        get
        {
            if (_lobbyMidSprite == null)
            {
                _lobbyMidSprite = CreateArcaneSprite(1024, 576, 2);
            }

            return _lobbyMidSprite;
        }
    }

    public static Sprite LobbyNearSprite
    {
        get
        {
            if (_lobbyNearSprite == null)
            {
                _lobbyNearSprite = CreateArcaneSprite(1024, 576, 3);
            }

            return _lobbyNearSprite;
        }
    }

    public static void ApplyFullscreenBackground(Image image, bool raycastTarget)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = BackgroundSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = raycastTarget;
    }

    public static Image EnsureCanvasBackdrop(Transform canvasRoot)
    {
        if (canvasRoot == null)
        {
            return null;
        }

        Transform existing = canvasRoot.Find(CanvasBackdropName);
        Image image;
        if (existing == null)
        {
            GameObject go = GsiUiRuntimeWidgets.CreateUiObject(CanvasBackdropName, canvasRoot);
            GsiUiRuntimeWidgets.StretchFull(go.GetComponent<RectTransform>());
            go.transform.SetAsFirstSibling();
            image = go.AddComponent<Image>();
        }
        else
        {
            image = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
            existing.SetAsFirstSibling();
        }

        ApplyFullscreenBackground(image, false);
        return image;
    }

    public static void ApplyPanel(Image image)
    {
        if (image == null)
        {
            return;
        }

        image.color = GsiUiAppearance.Panel;
        GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(image);
        AddOrUpdateOutline(image.gameObject, GsiUiAppearance.PanelOuterRim, new Vector2(1.3f, -1.3f));
        AddOrUpdateShadow(image.gameObject, GsiUiAppearance.CardDropShadow, new Vector2(0f, -10f));
    }

    public static void ApplyHeader(Image image)
    {
        if (image == null)
        {
            return;
        }

        image.color = GsiUiAppearance.ShopHeaderStrip(CosmeticTheme.UiAccent);
        GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(image);
        AddOrUpdateOutline(image.gameObject, GsiUiAppearance.UiHairline, new Vector2(0f, -1f));
    }

    public static void ApplyRow(Image image)
    {
        if (image == null)
        {
            return;
        }

        image.color = GsiUiAppearance.ShopRowBackground;
        GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(image);
        AddOrUpdateOutline(image.gameObject, GsiUiAppearance.UiControlRim, new Vector2(1f, -1f));
        AddOrUpdateShadow(image.gameObject, GsiUiAppearance.CardDropShadow, new Vector2(0f, -5f));
    }

    public static void ApplyButton(Button button, bool primary)
    {
        if (button == null)
        {
            return;
        }

        button.transition = Selectable.Transition.ColorTint;
        Color baseCol = primary ? GsiUiAppearance.PrimaryActionButton : GsiUiAppearance.SecondaryButton;
        Color accent = primary ? GsiUiAppearance.ShopGoldText : GsiUiAppearance.TextSecondary;
        ColorBlock c = button.colors;
        c.fadeDuration = 0.12f;
        c.colorMultiplier = 1f;
        c.normalColor = baseCol;
        c.highlightedColor = Color.Lerp(baseCol, accent, 0.28f);
        c.pressedColor = Color.Lerp(baseCol, Color.black, 0.22f);
        c.selectedColor = c.highlightedColor;
        c.disabledColor = new Color(baseCol.r * 0.55f, baseCol.g * 0.55f, baseCol.b * 0.55f, 0.55f);
        button.colors = c;

        if (button.targetGraphic is Image img)
        {
            img.color = baseCol;
            GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(img);
            Color rim = primary ? GsiUiAppearance.ShopGoldText : GsiUiAppearance.UiControlRim;
            rim.a = primary ? 0.62f : Mathf.Max(rim.a, 0.28f);
            AddOrUpdateOutline(img.gameObject, rim, new Vector2(1f, -1f));
        }
    }

    private static void AddOrUpdateOutline(GameObject go, Color color, Vector2 distance)
    {
        Outline outline = go.GetComponent<Outline>() ?? go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private static void AddOrUpdateShadow(GameObject go, Color color, Vector2 distance)
    {
        Shadow shadow = go.GetComponent<Shadow>() ?? go.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static Sprite CreateArcaneSprite(int width, int height, int variant)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.name = variant == 0 ? "GSI_ArcaneBackground" : $"GSI_ArcaneLayer_{variant}";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color top = variant == 1 ? new Color(0.02f, 0.015f, 0.026f, 1f) : new Color(0.035f, 0.024f, 0.036f, 1f);
        Color bottom = variant == 3 ? new Color(0.11f, 0.065f, 0.032f, 1f) : new Color(0.07f, 0.044f, 0.03f, 1f);

        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                float vignette = Mathf.Clamp01(1.15f - Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.48f)) * 1.55f);
                float grain = Mathf.Repeat(Mathf.Sin((x * 12.9898f + y * 78.233f + variant * 37.719f)) * 43758.5453f, 1f);
                Color c = Color.Lerp(bottom, top, v);
                c = Color.Lerp(Color.black, c, 0.55f + vignette * 0.45f);
                c += new Color(grain, grain * 0.82f, grain * 0.55f, 0f) * 0.018f;

                if (variant > 0)
                {
                    c.a = variant == 1 ? 0.58f : (variant == 2 ? 0.38f : 0.34f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }
}


