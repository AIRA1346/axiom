using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 힐링 허브 씬: 런타임에 UI를 구성합니다(에디터에서 씬을 비워 둬도 동작).
/// </summary>
public sealed class HealingHubBootstrap : MonoBehaviour
{
    [SerializeField] private bool _rebuildEachAwake;

    private void Awake()
    {
        if (!_rebuildEachAwake && FindFirstObjectByType<Canvas>() != null)
        {
            return;
        }

        EnsureEventSystem();

        var canvasGo = new GameObject("HealingCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var root = new GameObject("Root");
        root.transform.SetParent(canvasGo.transform, false);
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(root.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.72f);
        titleRt.anchorMax = new Vector2(0.5f, 0.72f);
        titleRt.sizeDelta = new Vector2(900f, 80f);
        var title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = "힐링";
        title.fontSize = 42;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.95f, 0.93f, 0.88f, 1f);
        if (TmpFontCache.LiberationSansSdf != null)
        {
            title.font = TmpFontCache.LiberationSansSdf;
        }

        CreateButton(root.transform, "낚시 — 숲속의 호수", new Vector2(0.5f, 0.48f), HealingSceneNavigation.LoadFishingLake);
        CreateButton(root.transform, "ARCHÉ로 돌아가기", new Vector2(0.5f, 0.32f), HealingSceneNavigation.LoadMainHub);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();
    }

    private static void CreateButton(Transform parent, string label, Vector2 anchorCenter, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + label.GetHashCode());
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorCenter;
        rt.anchorMax = anchorCenter;
        rt.sizeDelta = new Vector2(420f, 64f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.28f, 0.38f, 0.95f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tr = textGo.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            tmp.font = TmpFontCache.LiberationSansSdf;
        }
    }
}
