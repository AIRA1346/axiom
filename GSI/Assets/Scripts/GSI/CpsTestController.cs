using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Average CPS: 10s session, only clicks on the large hit target count (no full-screen tap capture).
/// </summary>
public sealed class CpsTestController : MonoBehaviour
{
    private const float SessionSeconds = CpsDifficulty.SessionDurationSeconds;

    private bool _subscribed;
    private bool _localeSubscribed;
    private bool _pendingSessionStart;
    private Coroutine _sessionRoutine;

    private Transform _playRoot;
    private Image _panelBackgroundImage;
    private bool _panelBackgroundSuppressed;
    private TextMeshProUGUI _hudText;
    private TextMeshProUGUI _buttonLabel;
    private Button _hitButton;
    private int _clickCount;
    private bool _sessionActive;

    private void Awake()
    {
        TrySubscribeGameManager();
    }

    private void Start()
    {
        TrySubscribeGameManager();
        TryFlushPendingSessionStart();
    }

    private void OnEnable()
    {
        TrySubscribeGameManager();
        SubscribeLocale();
        if (GameManager.Instance != null)
        {
            HandleGameStateChanged(GameManager.Instance.CurrentState);
        }

        TryFlushPendingSessionStart();
    }

    private void OnDisable()
    {
        UnsubscribeLocale();
        StopSession();
        DestroyPlayRoot();
        RestorePanelBackgroundIfNeeded();
        _pendingSessionStart = false;
    }

    private void OnDestroy()
    {
        UnsubscribeGameManager();
        UnsubscribeLocale();
    }

    private void SubscribeLocale()
    {
        if (_localeSubscribed)
        {
            return;
        }

        if (GameLocalization.IsInitialized)
        {
            OnLocaleOrUiChanged();
        }

        GameLocalization.UiLocaleChanged += OnLocaleOrUiChanged;
        _localeSubscribed = true;
    }

    private void UnsubscribeLocale()
    {
        if (!_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged -= OnLocaleOrUiChanged;
        _localeSubscribed = false;
    }

    private void OnLocaleOrUiChanged()
    {
        if (_buttonLabel != null)
        {
            _buttonLabel.text = GameLocalization.GetUiString(UiStringKeys.CpsTapButton, "Click here");
        }
    }

    private void TrySubscribeGameManager()
    {
        if (_subscribed || GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        _subscribed = true;
    }

    private void UnsubscribeGameManager()
    {
        if (!_subscribed || GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        _subscribed = false;
    }

    private void TryFlushPendingSessionStart()
    {
        if (!_pendingSessionStart || !isActiveAndEnabled)
        {
            return;
        }

        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.TestInProgress
            || GameManager.Instance.CurrentTestMode != TestMode.ClicksPerSecond)
        {
            return;
        }

        _pendingSessionStart = false;
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }

        StopSession();
        _sessionRoutine = StartCoroutine(RunSessionRoutine());
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentTestMode != TestMode.ClicksPerSecond)
        {
            _pendingSessionStart = false;
            StopSession();
            return;
        }

        if (newState == GameState.TestInProgress)
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScore();
            }

            StopSession();

            if (!isActiveAndEnabled)
            {
                _pendingSessionStart = true;
                return;
            }

            _pendingSessionStart = false;
            _sessionRoutine = StartCoroutine(RunSessionRoutine());
            return;
        }

        _pendingSessionStart = false;
        StopSession();
    }

    private void StopSession()
    {
        if (_sessionRoutine != null)
        {
            StopCoroutine(_sessionRoutine);
            _sessionRoutine = null;
        }

        _sessionActive = false;
        if (_hitButton != null)
        {
            _hitButton.onClick.RemoveListener(OnHitClicked);
        }

        DestroyPlayRoot();
        RestorePanelBackgroundIfNeeded();
    }

    private IEnumerator RunSessionRoutine()
    {
        BuildPlayArea();
        _clickCount = 0;
        _sessionActive = true;

        float elapsed = 0f;
        while (elapsed < SessionSeconds)
        {
            if (!_sessionActive)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            float remaining = Mathf.Max(0f, SessionSeconds - elapsed);
            float avgSoFar = elapsed > 0.0001f ? _clickCount / elapsed : 0f;
            UpdateHud(remaining, _clickCount, avgSoFar);
            yield return null;
        }

        _sessionActive = false;
        if (_hitButton != null)
        {
            _hitButton.onClick.RemoveListener(OnHitClicked);
        }

        float averageCps = _clickCount / SessionSeconds;
        bool isUnified = GameManager.Instance != null
            && GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam;

        if (isUnified && GameManager.Instance != null)
        {
            GameManager.Instance.CompleteUnifiedExamSegment(new UnifiedExamSegmentPayload
            {
                Mode = TestMode.ClicksPerSecond,
                HardFailed = false,
                Primary = averageCps
            });
            _sessionRoutine = null;
            yield break;
        }

        bool practiceFail = GameManager.Instance != null
            && GameManager.Instance.CurrentTestType == TestType.Practice
            && !CpsDifficulty.IsPass(
                averageCps,
                GameManager.Instance.GetPracticeGrade(TestMode.ClicksPerSecond));

        int cpsRank = GameManager.Instance != null
            ? GameManager.Instance.GetPracticeGrade(TestMode.ClicksPerSecond)
            : 9;
        bool officialFail = GameManager.Instance != null
            && GameManager.Instance.CurrentTestType == TestType.OfficialExam
            && !CpsDifficulty.IsPass(averageCps, cpsRank);

        bool failed = practiceFail || officialFail;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SaveCpsResult(averageCps, failed);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.ResultScreen);
        }

        _sessionRoutine = null;
    }

    private void OnHitClicked()
    {
        if (!_sessionActive)
        {
            return;
        }

        _clickCount++;
    }

    private void UpdateHud(float remainingSec, int clicks, float liveAvgCps)
    {
        if (_hudText == null)
        {
            return;
        }

        _hudText.text = GameLocalization.FormatUiString(UiStringKeys.CpsHudLineFmt,
            "Time {0:0.0}s · Clicks {1} · Avg {2:0.00} /s",
            remainingSec,
            clicks.ToString(CultureInfo.InvariantCulture),
            liveAvgCps);
    }

    private static Sprite GetWhiteSprite()
    {
        Texture2D tex = Texture2D.whiteTexture;
        return Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    private void BuildPlayArea()
    {
        DestroyPlayRoot();
        HideOtherTestTargetsOnPanel();
        SuppressPanelBackgroundForCps();

        var rootGo = new GameObject("CpsPlayRoot");
        var rootRt = rootGo.AddComponent<RectTransform>();
        _playRoot = rootGo.transform;
        _playRoot.SetParent(transform, false);
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = new Vector2(8f, 8f);
        rootRt.offsetMax = new Vector2(-8f, -8f);
        rootRt.localScale = Vector3.one;

        var canvas = rootGo.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        rootGo.AddComponent<GraphicRaycaster>();

        var blockGo = new GameObject("CpsClickSink");
        blockGo.transform.SetParent(_playRoot, false);
        var blockRt = blockGo.AddComponent<RectTransform>();
        blockRt.anchorMin = Vector2.zero;
        blockRt.anchorMax = Vector2.one;
        blockRt.offsetMin = Vector2.zero;
        blockRt.offsetMax = Vector2.zero;
        var blockImg = blockGo.AddComponent<Image>();
        blockImg.color = new Color(0.04f, 0.05f, 0.08f, 0.98f);
        blockImg.raycastTarget = true;

        var hudGo = new GameObject("CpsHud");
        hudGo.transform.SetParent(_playRoot, false);
        var hudRt = hudGo.AddComponent<RectTransform>();
        hudRt.anchorMin = new Vector2(0f, 1f);
        hudRt.anchorMax = new Vector2(1f, 1f);
        hudRt.pivot = new Vector2(0.5f, 1f);
        hudRt.sizeDelta = new Vector2(0f, 44f);
        hudRt.anchoredPosition = Vector2.zero;
        _hudText = hudGo.AddComponent<TextMeshProUGUI>();
        _hudText.fontSize = 17;
        _hudText.alignment = TextAlignmentOptions.Center;
        _hudText.color = new Color(0.9f, 0.92f, 0.95f, 1f);
        _hudText.raycastTarget = false;
        TMP_FontAsset font = TmpFontCache.LiberationSansSdf;
        if (font != null)
        {
            _hudText.font = font;
        }

        UpdateHud(SessionSeconds, 0, 0f);

        var btnGo = new GameObject("CpsHitTarget");
        btnGo.transform.SetParent(_playRoot, false);
        var btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.1f, 0.2f);
        btnRt.anchorMax = new Vector2(0.9f, 0.55f);
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.sprite = GetWhiteSprite();
        btnImg.color = new Color(0.2f, 0.5f, 0.9f, 0.95f);
        btnImg.raycastTarget = true;
        _hitButton = btnGo.AddComponent<Button>();
        _hitButton.targetGraphic = btnImg;
        _hitButton.onClick.AddListener(OnHitClicked);

        var labelGo = new GameObject("CpsLabel");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        _buttonLabel = labelGo.AddComponent<TextMeshProUGUI>();
        _buttonLabel.fontSize = 28;
        _buttonLabel.fontStyle = FontStyles.Bold;
        _buttonLabel.alignment = TextAlignmentOptions.Center;
        _buttonLabel.color = Color.white;
        _buttonLabel.raycastTarget = false;
        if (font != null)
        {
            _buttonLabel.font = font;
        }

        _buttonLabel.text = GameLocalization.GetUiString(UiStringKeys.CpsTapButton, "Click here");
        _playRoot.SetAsLastSibling();
    }

    private void DestroyPlayRoot()
    {
        if (_playRoot == null)
        {
            return;
        }

        Destroy(_playRoot.gameObject);
        _playRoot = null;
        _hudText = null;
        _buttonLabel = null;
        _hitButton = null;
    }

    private void HideOtherTestTargetsOnPanel()
    {
        Transform panel = transform;
        Transform aim = panel.Find("AimTargetArea");
        if (aim != null)
        {
            aim.gameObject.SetActive(false);
        }

        Transform reaction = panel.Find("ReactionTarget");
        if (reaction != null)
        {
            reaction.gameObject.SetActive(false);
        }
    }

    private void SuppressPanelBackgroundForCps()
    {
        if (_panelBackgroundSuppressed)
        {
            return;
        }

        if (_panelBackgroundImage == null)
        {
            _panelBackgroundImage = GetComponent<Image>();
        }

        if (_panelBackgroundImage != null)
        {
            _panelBackgroundImage.enabled = false;
        }

        _panelBackgroundSuppressed = true;
    }

    private void RestorePanelBackgroundIfNeeded()
    {
        if (!_panelBackgroundSuppressed)
        {
            return;
        }

        if (_panelBackgroundImage != null)
        {
            _panelBackgroundImage.enabled = true;
        }

        _panelBackgroundSuppressed = false;
    }
}
