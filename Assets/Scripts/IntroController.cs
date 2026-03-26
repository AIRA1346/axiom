using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 공식 시험 시설 컨셉 인트로. 무채색 UI, 시스템 접속 연출.
/// ItemDatabase 로딩 완료 시까지 대기 후 ACCESS GRANTED → 메인 씬 전환.
/// 120 FPS 준수: 코루틴 기반, Update 최소화.
/// </summary>
public sealed class IntroController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private TextMeshProUGUI _logoText;
    [SerializeField] private Image _fadeOverlay;

    [Header("Timing")]
    [SerializeField] private float _typewriterInterval = 0.03f;
    [SerializeField] private float _phaseDisplayDuration = 0.8f;
    [SerializeField] private float _accessGrantedHold = 1.5f;
    [SerializeField] private float _fadeOutDuration = 1f;

    [Header("Scene")]
    [SerializeField] private string _mainSceneName = "SampleScene";
    [SerializeField] private float _maxWaitSeconds = 15f;

    private void Start()
    {
        if (_fadeOverlay != null)
        {
            _fadeOverlay.color = new Color(0, 0, 0, 0);
            _fadeOverlay.raycastTarget = false;
        }

        if (_logoText != null && string.IsNullOrEmpty(_logoText.text))
        {
            _logoText.text = "G.S.I";
        }

        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        yield return null;

        yield return StartCoroutine(TypewriterEffect("CONNECTING TO SYSTEM..."));
        yield return new WaitForSecondsRealtime(_phaseDisplayDuration);

        yield return StartCoroutine(TypewriterEffect("LOADING DATABASE..."));
        float waitStart = Time.realtimeSinceStartup;
        while ((ItemDatabase.Instance == null || !ItemDatabase.Instance.IsInitialLoadComplete) &&
               (Time.realtimeSinceStartup - waitStart) < _maxWaitSeconds)
        {
            yield return null;
        }
        if (ItemDatabase.Instance == null || !ItemDatabase.Instance.IsInitialLoadComplete)
        {
            Debug.LogWarning("[IntroController] ItemDatabase 로딩 타임아웃. 메인 씬으로 진행합니다.");
        }
        yield return new WaitForSecondsRealtime(_phaseDisplayDuration * 0.5f);

        int itemCount = 0;
        var metaEnum = ItemDatabase.Instance?.GetAllMetadata();
        if (metaEnum != null)
        {
            foreach (var _ in metaEnum) itemCount++;
        }
        if (itemCount <= 0) itemCount = 100000;

        yield return StartCoroutine(TypewriterEffect($"SYNCING {itemCount:N0} ITEMS..."));
        yield return new WaitForSecondsRealtime(_phaseDisplayDuration);

        yield return StartCoroutine(TypewriterEffect("ACCESS GRANTED"));
        yield return new WaitForSecondsRealtime(_accessGrantedHold);

        yield return StartCoroutine(FadeOut());

        if (string.IsNullOrEmpty(_mainSceneName))
        {
            _mainSceneName = "SampleScene";
        }

        int targetIndex = -1;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            if (path.EndsWith(_mainSceneName + ".unity", System.StringComparison.OrdinalIgnoreCase) ||
                path.Contains("/" + _mainSceneName + ".unity"))
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex >= 0)
        {
            SceneManager.LoadScene(targetIndex);
        }
        else
        {
            SceneManager.LoadScene(_mainSceneName);
        }
    }

    private IEnumerator TypewriterEffect(string fullText)
    {
        if (_statusText == null || string.IsNullOrEmpty(fullText))
        {
            yield break;
        }

        _statusText.text = "";
        float nextCharTime = Time.realtimeSinceStartup;

        for (int i = 0; i < fullText.Length; i++)
        {
            while (Time.realtimeSinceStartup < nextCharTime)
            {
                yield return null;
            }
            nextCharTime = Time.realtimeSinceStartup + _typewriterInterval;

            _statusText.text = fullText.Substring(0, i + 1);
        }
    }

    private IEnumerator FadeOut()
    {
        if (_fadeOverlay == null)
        {
            yield break;
        }

        _fadeOverlay.raycastTarget = true;
        float elapsed = 0f;

        while (elapsed < _fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _fadeOutDuration);
            _fadeOverlay.color = new Color(0, 0, 0, t);
            yield return null;
        }

        _fadeOverlay.color = new Color(0, 0, 0, 1);
    }
}
