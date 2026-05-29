using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small release-safe toast for failures that should not disappear into logs.
/// </summary>
public sealed class GsiUserFacingNotice : MonoBehaviour
{
    private const string HostName = "GSI_UserFacingNotice";
    private const float VisibleSeconds = 2.6f;

    private static GsiUserFacingNotice _instance;

    private TextMeshProUGUI _label;
    private CanvasGroup _canvasGroup;
    private Coroutine _hideCoroutine;

    public static void Show(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Ensure().ShowInternal(message.Trim());
    }

    public static void ShowLocalized(string key, string englishFallback)
    {
        Show(GameLocalization.GetUiString(key, englishFallback));
    }

    private static GsiUserFacingNotice Ensure()
    {
        if (_instance != null)
        {
            return _instance;
        }

        var go = new GameObject(HostName);
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<GsiUserFacingNotice>();
        _instance.BuildUi();
        return _instance;
    }

    private void BuildUi()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        GsiUiScreenLayout.ApplyCanvasScaler(scaler);
        gameObject.AddComponent<GraphicRaycaster>();
        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        GameObject panel = GsiUiRuntimeWidgets.CreateUiObject("Panel", transform);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0f);
        panelRt.anchorMax = new Vector2(0.5f, 0f);
        panelRt.pivot = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(0f, 48f);
        panelRt.sizeDelta = new Vector2(720f, 72f);

        var bg = panel.AddComponent<Image>();
        GsiArcaneUi.ApplyPanel(bg);

        _label = GsiUiRuntimeWidgets.CreateTmp(panel.transform, string.Empty, 22f, FontStyles.Bold);
        RectTransform labelRt = _label.GetComponent<RectTransform>();
        GsiUiRuntimeWidgets.StretchFull(labelRt);
        labelRt.offsetMin = new Vector2(24f, 10f);
        labelRt.offsetMax = new Vector2(-24f, -10f);
        _label.alignment = TextAlignmentOptions.Center;
        _label.enableWordWrapping = true;
        _label.characterSpacing = 0.45f;
        _label.color = GsiUiAppearance.TextPrimary;
    }

    private void ShowInternal(string message)
    {
        if (_label == null || _canvasGroup == null)
        {
            BuildUi();
        }

        _label.text = message;
        _canvasGroup.alpha = 1f;
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
        }

        _hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(VisibleSeconds);
        _canvasGroup.alpha = 0f;
        _hideCoroutine = null;
    }
}
