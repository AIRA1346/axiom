using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Bullet Hell: 탄막 회피. 연습 1회, 공식 시험 3판 합산. 1급만 최대 생존 시간, 그 외는 목표 시간 생존.
/// </summary>
public sealed class BulletHellTestController : MonoBehaviour
{
    private const float SpawnMargin = 8f;

    private bool _subscribed;
    private bool _pendingSessionStart;
    private Coroutine _sessionRoutine;

    private Transform _playRoot;
    private Image _panelBackgroundImage;
    private bool _panelBackgroundSuppressed;
    private TextMeshProUGUI _hudText;
    private RectTransform _fieldRect;
    private RectTransform _playerRect;
    private Image _playerImage;
    private Transform _bulletsLayer;
    private Transform _bulletVisualPoolRoot;
    private readonly Stack<RectTransform> _bulletVisualPool = new Stack<RectTransform>(360);
    private Canvas _rootCanvas;

    private static Sprite _whiteSprite;

    private readonly List<BulletRuntime> _bullets = new List<BulletRuntime>(320);
    private bool _inputSubscribed;
    private Vector2 _pointerScreen;
    private bool _hasPointer;

    private bool _roundActive;
    private float _invulnTimer;
    private int _lives;
    private float _elapsedRound;
    private float _targetSeconds;
    private bool _grade1SurvivalMode;
    private float _bulletSpeed;
    private float _bulletsPerSecond;
    private float _densityMul;
    private float _spawnDebt;
    private System.Random _rng;

    private sealed class BulletRuntime
    {
        public Vector2 Pos;
        public Vector2 Vel;
        public RectTransform Visual;
    }

    private static Sprite GetWhiteSprite()
    {
        if (_whiteSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        return _whiteSprite;
    }

    private void Awake()
    {
        TrySubscribeGameManager();
        var poolGo = new GameObject("BulletVisualPool");
        poolGo.transform.SetParent(transform, false);
        _bulletVisualPoolRoot = poolGo.transform;
    }

    private void Start()
    {
        TrySubscribeGameManager();
        TryFlushPendingSessionStart();
    }

    private void OnEnable()
    {
        TrySubscribeGameManager();
        if (GameManager.Instance != null)
        {
            HandleGameStateChanged(GameManager.Instance.CurrentState);
        }

        TryFlushPendingSessionStart();
    }

    private void OnDisable()
    {
        StopSession();
        DestroyPlayRoot();
        UnsubscribeInput();
        _pendingSessionStart = false;
    }

    private void OnDestroy()
    {
        UnsubscribeGameManager();
    }

    private void Update()
    {
        if (!_roundActive || _fieldRect == null || _playerRect == null)
        {
            return;
        }

        float dt = Time.deltaTime;
        if (_invulnTimer > 0f)
        {
            _invulnTimer -= dt;
        }

        UpdatePlayerMovement(dt);
        UpdateBullets(dt);
        TrySpawnBullets(dt);
        CheckCollisions();
        CheckRoundEndConditions();
        UpdateHud();
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

        if (GameManager.Instance == null
            || GameManager.Instance.CurrentState != GameState.TestInProgress
            || GameManager.Instance.CurrentTestMode != TestMode.BulletHell)
        {
            _pendingSessionStart = false;
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
        if (GameManager.Instance == null || GameManager.Instance.CurrentTestMode != TestMode.BulletHell)
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

        _roundActive = false;
        UnsubscribeInput();
        DestroyPlayRoot();
        RestorePanelBackgroundIfNeeded();
    }

    private IEnumerator RunSessionRoutine()
    {
        bool isExam = GameManager.Instance != null
            && (GameManager.Instance.CurrentTestType == TestType.OfficialExam
                || GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam);
        bool isUnified = GameManager.Instance != null
            && GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam;
        int trials = isExam ? BulletHellDifficulty.ExamTrialCount : 1;
        int grade = GameManager.Instance != null
            ? (isUnified
                ? GameManager.Instance.UnifiedExamGrade
                : GameManager.Instance.GetPracticeGrade(TestMode.BulletHell))
            : 9;

        float sumSeconds = 0f;
        bool anyRoundFailed = false;
        bool grade1 = grade == 1;

        for (int t = 0; t < trials; t++)
        {
            yield return StartCoroutine(RunSingleRound(grade, t * 7919 + 0x42554c4c));
            sumSeconds += _lastRoundSeconds;

            if (!isUnified && !grade1 && !_lastRoundSuccess)
            {
                anyRoundFailed = true;
                break;
            }
        }

        bool failed;
        if (grade1 && isExam)
        {
            failed = sumSeconds < BulletHellDifficulty.Grade1ExamPassSumSeconds;
        }
        else if (grade1 && !isExam)
        {
            failed = false;
        }
        else
        {
            failed = anyRoundFailed;
        }

        if (isUnified && GameManager.Instance != null)
        {
            GameManager.Instance.CompleteUnifiedExamSegment(new UnifiedExamSegmentPayload
            {
                Mode = TestMode.BulletHell,
                HardFailed = false,
                Primary = sumSeconds,
                BoolFlag = grade1
            });
            _sessionRoutine = null;
            yield break;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SaveBulletHellResult(sumSeconds, failed, grade1, isExam);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.ResultScreen);
        }

        _sessionRoutine = null;
    }

    private float _lastRoundSeconds;
    private bool _lastRoundSuccess;

    private int _hudCachedElapsedTenth = int.MinValue;
    private int _hudCachedLives = int.MinValue;
    private int _hudCachedInvulnTenth = int.MinValue;

    private IEnumerator RunSingleRound(int grade, int seed)
    {
        BulletHellDifficulty.GetParams(grade, out _grade1SurvivalMode, out _targetSeconds, out _lives,
            out _bulletSpeed, out _bulletsPerSecond, out _densityMul);

        _rng = new System.Random(seed);
        BuildPlayArea();
        SubscribeInput();

        _bullets.Clear();
        _spawnDebt = 0f;
        _elapsedRound = 0f;
        _invulnTimer = 0f;
        _roundActive = true;
        _hasPointer = false;
        _hudCachedElapsedTenth = int.MinValue;
        _hudCachedLives = int.MinValue;
        _hudCachedInvulnTenth = int.MinValue;

        if (_hudText != null)
        {
            if (_grade1SurvivalMode)
            {
                _hudText.text = $"1급 · 최대 생존 · 생명 {_lives} · 무적 {BulletHellDifficulty.InvincibilitySeconds:0.#}초";
            }
            else
            {
                _hudText.text = $"목표 {_targetSeconds:0.#}초 생존 · 생명 {_lives} · 무적 {BulletHellDifficulty.InvincibilitySeconds:0.#}초";
            }
        }

        while (_roundActive)
        {
            yield return null;
        }

        UnsubscribeInput();
        _lastRoundSeconds = _elapsedRound;
    }

    private void CheckRoundEndConditions()
    {
        if (!_grade1SurvivalMode)
        {
            if (_lives <= 0)
            {
                EndRound(false);
                return;
            }

            if (_elapsedRound >= _targetSeconds && _lives > 0)
            {
                EndRound(true);
            }
        }
        else
        {
            if (_lives <= 0)
            {
                EndRound(true);
            }
        }
    }

    private void EndRound(bool success)
    {
        _roundActive = false;
        _lastRoundSuccess = success;
    }

    private void UpdateHud()
    {
        if (_hudText == null)
        {
            return;
        }

        int elapsedTenth = Mathf.RoundToInt(_elapsedRound * 10f);
        int invTenth = _invulnTimer > 0f ? Mathf.RoundToInt(_invulnTimer * 10f) : -1;

        if (elapsedTenth == _hudCachedElapsedTenth
            && _lives == _hudCachedLives
            && invTenth == _hudCachedInvulnTenth)
        {
            return;
        }

        _hudCachedElapsedTenth = elapsedTenth;
        _hudCachedLives = _lives;
        _hudCachedInvulnTenth = invTenth;

        string inv = _invulnTimer > 0f ? $"무적 {_invulnTimer:0.0}s" : " ";
        if (_grade1SurvivalMode)
        {
            _hudText.text = $"시간 {_elapsedRound:0.#}s · 생명 {_lives} · {inv}";
        }
        else
        {
            _hudText.text = $"시간 {_elapsedRound:0.#}s / 목표 {_targetSeconds:0.#}s · 생명 {_lives} · {inv}";
        }
    }

    private void SubscribeInput()
    {
        if (_inputSubscribed || InputManager.Instance == null)
        {
            return;
        }

        InputManager.Instance.OnInputDown += OnPointer;
        InputManager.Instance.OnInputHold += OnPointer;
        _inputSubscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!_inputSubscribed || InputManager.Instance == null)
        {
            return;
        }

        InputManager.Instance.OnInputDown -= OnPointer;
        InputManager.Instance.OnInputHold -= OnPointer;
        _inputSubscribed = false;
        _hasPointer = false;
    }

    private void OnPointer(Vector2 screenPos)
    {
        _pointerScreen = screenPos;
        _hasPointer = true;
    }

    private static Vector2 ReadMoveKeyboardDir()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
        {
            return Vector2.zero;
        }

        float x = 0f;
        float y = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
        {
            x -= 1f;
        }

        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
        {
            x += 1f;
        }

        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
        {
            y -= 1f;
        }

        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
        {
            y += 1f;
        }

        var v = new Vector2(x, y);
        return v.sqrMagnitude < 0.0001f ? Vector2.zero : v.normalized;
    }

    private void UpdatePlayerMovement(float dt)
    {
        if (_fieldRect == null)
        {
            return;
        }

        float step = BulletHellDifficulty.PlayerMoveSpeed * dt;
        Vector2 p = _playerRect.anchoredPosition;
        Vector2 keyboardDir = ReadMoveKeyboardDir();

        if (keyboardDir.sqrMagnitude > 0.0001f)
        {
            p += keyboardDir * step;
        }
        else
        {
            Vector2 targetLocal = p;
            if (_hasPointer && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _fieldRect, _pointerScreen, _rootCanvas != null ? _rootCanvas.worldCamera : null, out Vector2 local))
            {
                targetLocal = local;
            }

            Vector2 delta = targetLocal - p;
            float mag = delta.magnitude;
            if (mag > 0.001f)
            {
                if (step >= mag)
                {
                    p = targetLocal;
                }
                else
                {
                    p += delta.normalized * step;
                }
            }
        }

        Rect r = _fieldRect.rect;
        float pr = BulletHellDifficulty.PlayerRadius;
        float hx = Mathf.Max(0f, r.width * 0.5f - pr - 2f);
        float hy = Mathf.Max(0f, r.height * 0.5f - pr - 2f);
        p.x = Mathf.Clamp(p.x, -hx, hx);
        p.y = Mathf.Clamp(p.y, -hy, hy);
        _playerRect.anchoredPosition = p;

        if (_playerImage != null)
        {
            Color c = _playerImage.color;
            c.a = _invulnTimer > 0f ? 0.45f : 1f;
            _playerImage.color = c;
        }
    }

    private void UpdateBullets(float dt)
    {
        Rect fr = _fieldRect.rect;
        float margin = BulletHellDifficulty.BulletRadius + SpawnMargin;
        float hx = fr.width * 0.5f + margin;
        float hy = fr.height * 0.5f + margin;

        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            BulletRuntime b = _bullets[i];
            b.Pos += b.Vel * dt;
            if (b.Visual != null)
            {
                b.Visual.anchoredPosition = b.Pos;
            }

            if (Mathf.Abs(b.Pos.x) > hx || Mathf.Abs(b.Pos.y) > hy)
            {
                ReleaseBulletVisual(b.Visual);
                _bullets.RemoveAt(i);
                continue;
            }
        }

        _elapsedRound += dt;
    }

    private void TrySpawnBullets(float dt)
    {
        if (_fieldRect == null)
        {
            return;
        }

        float rate = _bulletsPerSecond * _densityMul;
        _spawnDebt += rate * dt;
        Rect fr = _fieldRect.rect;
        float hx = fr.width * 0.5f;
        float hy = fr.height * 0.5f;

        while (_spawnDebt >= 1f && _bullets.Count < 360)
        {
            _spawnDebt -= 1f;
            Vector2 pos = RandomEdgePosition(hx, hy);
            Vector2 dir = RandomInwardDirection(pos, hx, hy);
            RectTransform vis = CreateBulletImage(pos);
            _bullets.Add(new BulletRuntime
            {
                Pos = pos,
                Vel = dir * _bulletSpeed,
                Visual = vis
            });
        }
    }

    private Vector2 RandomEdgePosition(float hx, float hy)
    {
        int edge = _rng.Next(0, 4);
        float u = (float)(_rng.NextDouble() * 2 - 1);
        switch (edge)
        {
            case 0:
                return new Vector2(u * hx, hy);
            case 1:
                return new Vector2(u * hx, -hy);
            case 2:
                return new Vector2(hx, u * hy);
            default:
                return new Vector2(-hx, u * hy);
        }
    }

    private Vector2 RandomInwardDirection(Vector2 pos, float hx, float hy)
    {
        float tx = (float)((_rng.NextDouble() * 2 - 1) * hx * 0.65f);
        float ty = (float)((_rng.NextDouble() * 2 - 1) * hy * 0.65f);
        Vector2 to = new Vector2(tx, ty) - pos;
        if (to.sqrMagnitude < 0.0001f)
        {
            to = new Vector2(-pos.x, -pos.y);
        }

        float j = (float)((_rng.NextDouble() - 0.5) * 0.55f);
        float nx = -to.y;
        float ny = to.x;
        Vector2 perp = new Vector2(nx, ny).normalized * j * to.magnitude;
        Vector2 d = to + perp;
        return d.normalized;
    }

    private void CheckCollisions()
    {
        if (_invulnTimer > 0f || _playerRect == null)
        {
            return;
        }

        Vector2 pp = _playerRect.anchoredPosition;
        float pr = BulletHellDifficulty.PlayerRadius;
        float br = BulletHellDifficulty.BulletRadius;
        float rs = pr + br;

        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            BulletRuntime b = _bullets[i];
            if (Vector2.Distance(pp, b.Pos) < rs)
            {
                Vector2 hitPos = Vector2.Lerp(pp, b.Pos, 0.5f);
                SpawnHitEffect(hitPos);
                ReleaseBulletVisual(b.Visual);

                _bullets.RemoveAt(i);
                _lives--;
                _invulnTimer = BulletHellDifficulty.InvincibilitySeconds;
                break;
            }
        }
    }

    private void ReleaseBulletVisual(RectTransform rt)
    {
        if (rt == null || _bulletVisualPoolRoot == null)
        {
            return;
        }

        rt.gameObject.SetActive(false);
        rt.SetParent(_bulletVisualPoolRoot, false);
        _bulletVisualPool.Push(rt);
    }

    private RectTransform CreateBulletImage(Vector2 localPos)
    {
        RectTransform rt;
        if (_bulletVisualPool.Count > 0)
        {
            rt = _bulletVisualPool.Pop();
            rt.gameObject.SetActive(true);
            rt.SetParent(_bulletsLayer, false);
        }
        else
        {
            var go = new GameObject("Bullet");
            go.transform.SetParent(_bulletsLayer, false);
            rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.sprite = GetWhiteSprite();
            img.color = new Color(1f, 0.38f, 0.22f, 1f);
            img.raycastTarget = false;
        }

        float d = BulletHellDifficulty.BulletRadius * 2f;
        rt.sizeDelta = new Vector2(d, d);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = localPos;
        return rt;
    }

    private void SpawnHitEffect(Vector2 fieldLocalPos)
    {
        if (_fieldRect == null)
        {
            return;
        }

        var go = new GameObject("HitFx");
        go.transform.SetParent(_fieldRect, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(36f, 36f);
        rt.anchoredPosition = fieldLocalPos;
        var img = go.AddComponent<Image>();
        img.sprite = GetWhiteSprite();
        img.color = new Color(1f, 0.92f, 0.35f, 0.95f);
        img.raycastTarget = false;
        rt.SetAsLastSibling();
        StartCoroutine(AnimateHitRing(rt, img));
    }

    private IEnumerator AnimateHitRing(RectTransform rt, Image img)
    {
        Vector2 start = rt.sizeDelta;
        Color c0 = img.color;
        float dur = 0.32f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            rt.sizeDelta = Vector2.Lerp(start, start * 3.2f, k);
            Color c = c0;
            c.a = Mathf.Lerp(c0.a, 0f, k * k);
            img.color = c;
            yield return null;
        }

        if (rt != null)
        {
            Destroy(rt.gameObject);
        }
    }

    private void BuildPlayArea()
    {
        DestroyPlayRoot();
        HideOtherTestTargetsOnPanel();
        SuppressPanelBackgroundForBulletHell();

        var rootGo = new GameObject("BulletHellPlayRoot");
        var rootRt = rootGo.AddComponent<RectTransform>();
        _playRoot = rootGo.transform;
        _playRoot.SetParent(transform, false);
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = new Vector2(16f, 88f);
        rootRt.offsetMax = new Vector2(-16f, -16f);
        rootRt.localScale = Vector3.one;

        var canvas = rootGo.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        _rootCanvas = canvas;
        rootGo.AddComponent<GraphicRaycaster>();

        var bgGo = new GameObject("BhBackdrop");
        bgGo.transform.SetParent(_playRoot, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.07f, 0.1f, 0.98f);
        bgImg.raycastTarget = false;

        var hudGo = new GameObject("BhHud");
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

        var fieldGo = new GameObject("BhField");
        fieldGo.transform.SetParent(_playRoot, false);
        _fieldRect = fieldGo.AddComponent<RectTransform>();
        _fieldRect.anchorMin = new Vector2(0.06f, 0.1f);
        _fieldRect.anchorMax = new Vector2(0.94f, 0.88f);
        _fieldRect.offsetMin = Vector2.zero;
        _fieldRect.offsetMax = Vector2.zero;
        var fieldBg = fieldGo.AddComponent<Image>();
        fieldBg.sprite = GetWhiteSprite();
        fieldBg.color = new Color(0.1f, 0.11f, 0.15f, 0.92f);
        fieldBg.raycastTarget = false;

        var bulletsLayerGo = new GameObject("BulletsLayer");
        bulletsLayerGo.transform.SetParent(_fieldRect, false);
        var blRt = bulletsLayerGo.AddComponent<RectTransform>();
        blRt.anchorMin = Vector2.zero;
        blRt.anchorMax = Vector2.one;
        blRt.offsetMin = Vector2.zero;
        blRt.offsetMax = Vector2.zero;
        _bulletsLayer = bulletsLayerGo.transform;

        var playerGo = new GameObject("Player");
        playerGo.transform.SetParent(_fieldRect, false);
        _playerRect = playerGo.AddComponent<RectTransform>();
        float d = BulletHellDifficulty.PlayerRadius * 2f;
        _playerRect.sizeDelta = new Vector2(d, d);
        _playerRect.anchorMin = _playerRect.anchorMax = new Vector2(0.5f, 0.5f);
        _playerRect.pivot = new Vector2(0.5f, 0.5f);
        _playerRect.anchoredPosition = Vector2.zero;
        _playerImage = playerGo.AddComponent<Image>();
        _playerImage.sprite = GetWhiteSprite();
        _playerImage.color = new Color(0.35f, 0.85f, 0.45f, 1f);
        _playerImage.raycastTarget = false;

        _playRoot.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRt);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_fieldRect);
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

    private void SuppressPanelBackgroundForBulletHell()
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
            _panelBackgroundSuppressed = true;
        }
    }

    private void RestorePanelBackgroundIfNeeded()
    {
        if (!_panelBackgroundSuppressed || _panelBackgroundImage == null)
        {
            return;
        }

        _panelBackgroundImage.enabled = true;
        _panelBackgroundSuppressed = false;
    }

    private void DestroyPlayRoot()
    {
        _roundActive = false;
        for (int i = 0; i < _bullets.Count; i++)
        {
            ReleaseBulletVisual(_bullets[i].Visual);
        }

        _bullets.Clear();
        _fieldRect = null;
        _playerRect = null;
        _playerImage = null;
        _bulletsLayer = null;
        _rootCanvas = null;
        _hudText = null;
        if (_playRoot != null)
        {
            Destroy(_playRoot.gameObject);
            _playRoot = null;
        }
    }
}
