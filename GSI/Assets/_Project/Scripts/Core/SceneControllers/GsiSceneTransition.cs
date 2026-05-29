using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Full-screen black fade with async load for single-scene transitions. Root is DontDestroyOnLoad.
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class GsiSceneTransition : MonoBehaviour
{
    /// <summary>Sort order so the fade canvas draws above other screen-space UI (within platform limits).</summary>
    private const int OverlaySortOrder = 32767;

    private static GsiSceneTransition _instance;

    [Header("Fade (sec, unscaled)")]
    [SerializeField] private float fadeOutDuration = 0.18f;
    [SerializeField] private float fadeInDuration = 0.22f;

    private CanvasGroup _group;
    private bool _busy;

    /// <summary>Loads <paramref name="sceneName"/> (must be in Build Settings) with fade.</summary>
    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[GsiSceneTransition] Empty scene name; ignoring.");
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }
#endif
        GsiSceneTransition runner = EnsureInstance();
        runner.StartCoroutine(runner.LoadRoutine(sceneName));
    }

    private static GsiSceneTransition EnsureInstance()
    {
        if (_instance == null)
        {
            var go = new GameObject("GsiSceneTransition");
            DontDestroyOnLoad(go);
            go.AddComponent<GsiSceneTransition>();
        }

        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void BuildOverlay()
    {
        if (_group != null)
        {
            return;
        }

        var canvasGo = new GameObject("FadeCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = OverlaySortOrder;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        _group = canvasGo.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;

        var imgGo = new GameObject("Dim", typeof(RectTransform));
        imgGo.transform.SetParent(canvasGo.transform, false);
        var rt = imgGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = imgGo.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = true;
    }

    private static bool IsSceneInBuildSettings(string sceneName)
    {
        int n = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < n; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            string leaf = System.IO.Path.GetFileNameWithoutExtension(path);
            if (leaf == sceneName)
            {
                return true;
            }
        }

        return false;
    }

    private void ResetOverlayAndBusy()
    {
        _busy = false;
        if (_group != null)
        {
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        if (_busy)
        {
            Debug.LogWarning("[GsiSceneTransition] Transition already in progress; ignoring: " + sceneName);
            yield break;
        }

        Scene active = SceneManager.GetActiveScene();
        if (active.IsValid() && active.name == sceneName)
        {
            yield break;
        }

        if (!IsSceneInBuildSettings(sceneName))
        {
            Debug.LogError(
                "[GsiSceneTransition] Scene is not in Build Settings or name mismatch: \"" + sceneName +
                "\". Check File → Build Settings and SceneNames.");
            yield break;
        }

        if (_group == null)
        {
            BuildOverlay();
        }

        if (_group == null)
        {
            Debug.LogError("[GsiSceneTransition] Could not build fade overlay.");
            yield break;
        }

        float fo = Mathf.Max(0.01f, fadeOutDuration);
        float fi = Mathf.Max(0.01f, fadeInDuration);

        _busy = true;
        try
        {
            _group.blocksRaycasts = true;
            _group.interactable = true;

            yield return FadeRoutine(0f, 1f, fo);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError("[GsiSceneTransition] LoadSceneAsync returned null: " + sceneName);
                yield break;
            }

            op.allowSceneActivation = false;
            yield return new WaitUntil(() => op.progress >= 0.9f);

            op.allowSceneActivation = true;
            yield return new WaitUntil(() => op.isDone);

            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();

            yield return FadeRoutine(1f, 0f, fi);
        }
        finally
        {
            ResetOverlayAndBusy();
        }
    }

    private IEnumerator FadeRoutine(float from, float to, float duration)
    {
        if (_group == null)
        {
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            _group.alpha = Mathf.Lerp(from, to, u);
            yield return null;
        }

        _group.alpha = to;
    }
}
