using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ArchE.Game;

/// <summary>
/// Measures a five-target aim precision test for the G.S.I flow.
/// This controller only handles target placement, hit judgment, and total-time measurement.
/// Includes enhanced visual feedback and animations for a polished feel.
/// </summary>
public sealed class AimTestController : MonoBehaviour, IMiniGameController
{
    public string GameId => "AimPrecision";
    private int _assignedGrade = 9;

    public event System.Action<float, bool> OnGameFinished;

    public void InitializeGame(int grade)
    {
        _assignedGrade = grade;
    }

    public void StartGame()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }
        StartAimTest();
    }

    public void EndGame()
    {
        StopAimTest();
    }

    [Header("UI References")]
    [SerializeField] private RectTransform _aimTargetRect;

    [Header("Audio")]
    [SerializeField] private AudioClip _hitSfx;
    [SerializeField] private AudioClip _missSfx;

    [Header("Visual Feedback")]
    [SerializeField] private AnimationCurve _spawnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float _animationDuration = 0.15f;

    private int _targetsRequired = 5;
    private float _missPenaltySeconds = 0.5f;
    private float _moveRangeX = 400f;
    private float _moveRangeY = 200f;

    private bool _isGameManagerSubscribed;
    private bool _isInputManagerSubscribed;
    private bool _isTestActive;
    private int _currentHitCount;
    private float _startTime;

    private Coroutine _animationRoutine;
    private Vector3 _originalScale = Vector3.one;

    private void Awake()
    {
        if (_aimTargetRect != null)
        {
            _originalScale = _aimTargetRect.localScale;
        }
        SetTargetActive(false);
    }

    /// <summary>
    /// 비활성 패널에 붙은 경우 TestInProgress 전환 이벤트를 놓치지 않도록 OnEnable에서 동기화합니다.
    /// </summary>
    private void OnEnable()
    {
        SubscribeToManagers();
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.CurrentTestType == TestType.Practice)
            {
                _assignedGrade = GameManager.Instance.GetPracticeGrade(TestMode.AimPrecision);
            }
            else if (GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam)
            {
                _assignedGrade = GameManager.Instance.UnifiedExamGrade;
            }
            else if (GameManager.Instance.CurrentTestType == TestType.OfficialExam)
            {
                _assignedGrade = GameManager.Instance.GetPracticeGrade(TestMode.AimPrecision);
            }
            HandleGameStateChanged(GameManager.Instance.CurrentState);
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromManagers();
        StopAimTest();
    }

    private void OnDestroy()
    {
        UnsubscribeFromManagers();
        StopAimTest();
    }

    /// <summary>
    /// Subscribes to the shared game flow and input stream.
    /// </summary>
    private void SubscribeToManagers()
    {
        if (!_isGameManagerSubscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            _isGameManagerSubscribed = true;
        }

        if (!_isInputManagerSubscribed && InputManager.Instance != null)
        {
            InputManager.Instance.OnInputDown += HandleInputDown;
            _isInputManagerSubscribed = true;
        }
    }

    /// <summary>
    /// Removes event subscriptions when this component is disabled or destroyed.
    /// </summary>
    private void UnsubscribeFromManagers()
    {
        if (_isGameManagerSubscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            _isGameManagerSubscribed = false;
        }

        if (_isInputManagerSubscribed && InputManager.Instance != null)
        {
            InputManager.Instance.OnInputDown -= HandleInputDown;
            _isInputManagerSubscribed = false;
        }
    }

    /// <summary>
    /// Starts or clears the aim test when the official state changes.
    /// </summary>
    private void HandleGameStateChanged(GameState newState)
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentTestMode != TestMode.AimPrecision)
        {
            StopAimTest();
            return;
        }

        if (newState == GameState.TestInProgress)
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScore();
            }

            StartAimTest();
            return;
        }

        StopAimTest();
    }

    /// <summary>
    /// Judges hits and misses against the active aim target.
    /// </summary>
    private void HandleInputDown(Vector2 screenPosition)
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentTestMode != TestMode.AimPrecision)
        {
            return;
        }

        if (GameManager.Instance.CurrentState != GameState.TestInProgress || !_isTestActive || _aimTargetRect == null)
        {
            return;
        }

        bool isHit = RectTransformUtility.RectangleContainsScreenPoint(_aimTargetRect, screenPosition, null);

        if (!isHit)
        {
            HandleMiss();
            return;
        }

        HandleHit();
    }

    private void HandleHit()
    {
        _currentHitCount++;
        GsiAudio.PlaySfx(_hitSfx);

        if (_currentHitCount >= _targetsRequired)
        {
            float totalTime = Time.time - _startTime;

            if (GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam)
            {
                StopAimTest();
                GameManager.Instance.CompleteUnifiedExamSegment(new UnifiedExamSegmentPayload
                {
                    Mode = TestMode.AimPrecision,
                    HardFailed = false,
                    Primary = totalTime
                });
                OnGameFinished?.Invoke(totalTime, true);
                return;
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.SaveTestTime(totalTime);
            }

            StopAimTest();
            GameManager.Instance.SetGameState(GameState.ResultScreen);
            OnGameFinished?.Invoke(totalTime, true);
            return;
        }

        MoveTargetToRandomPosition();
        StartSpawnAnimation();
    }

    private void HandleMiss()
    {
        _startTime -= _missPenaltySeconds;
        GsiAudio.PlaySfx(_missSfx);
        // TODO: 시각적 미스 효과 추가 (예: 화면 빨간색 플래시)
    }

    /// <summary>
    /// Initializes a new aim test sequence and shows the first target.
    /// </summary>
    private void StartAimTest()
    {
        StopAimTest();

        PracticeDifficulty.GetAimPracticeParams(
            _assignedGrade,
            out _targetsRequired,
            out _missPenaltySeconds,
            out _moveRangeX,
            out _moveRangeY);

        AimDifficulty.ApplyTargetSize(_aimTargetRect, _assignedGrade);

        _currentHitCount = 0;
        _startTime = Time.time;
        _isTestActive = true;

        ApplyEquippedTargetColor();
        MoveTargetToRandomPosition();
        SetTargetActive(true);
        StartSpawnAnimation();
    }

    /// <summary>
    /// Clears the active aim test state and hides the target.
    /// </summary>
    private void StopAimTest()
    {
        _isTestActive = false;
        _currentHitCount = 0;
        if (_animationRoutine != null)
        {
            StopCoroutine(_animationRoutine);
            _animationRoutine = null;
        }
        SetTargetActive(false);
    }

    /// <summary>
    /// Moves the target to a new randomized anchored position inside the test area.
    /// </summary>
    private void MoveTargetToRandomPosition()
    {
        if (_aimTargetRect == null)
        {
            return;
        }

        _aimTargetRect.anchoredPosition = new Vector2(
            Random.Range(-_moveRangeX, _moveRangeX),
            Random.Range(-_moveRangeY, _moveRangeY));
    }

    private void StartSpawnAnimation()
    {
        if (_animationRoutine != null)
        {
            StopCoroutine(_animationRoutine);
        }
        _animationRoutine = StartCoroutine(SpawnAnimationRoutine());
    }

    private IEnumerator SpawnAnimationRoutine()
    {
        float elapsed = 0f;
        while (elapsed < _animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _animationDuration;
            float scale = _spawnCurve.Evaluate(t);
            _aimTargetRect.localScale = _originalScale * scale;
            yield return null;
        }
        _aimTargetRect.localScale = _originalScale;
        _animationRoutine = null;
    }

    /// <summary>
    /// Applies the active state to the aim target when assigned.
    /// </summary>
    private void SetTargetActive(bool isActive)
    {
        if (_aimTargetRect == null)
        {
            return;
        }

        _aimTargetRect.gameObject.SetActive(isActive);
    }

    /// <summary>
    /// Applies the equipped aim-target skin color before the target becomes visible.
    /// </summary>
    private void ApplyEquippedTargetColor()
    {
        if (_aimTargetRect == null)
        {
            return;
        }

        Image target_image = _aimTargetRect.GetComponent<Image>();

        if (target_image == null)
        {
            return;
        }

        const string prefsKey = "GSI_EquippedAimTarget";
        string equipped_aim_target = PlayerPrefs.GetString(prefsKey, "Aim_Default");

        switch (equipped_aim_target)
        {
            case "Aim_T10":
                target_image.color = Color.black;
                break;

            case "Aim_T9":
                target_image.color = Color.red;
                break;

            case "Aim_T8":
                target_image.color = new Color(0.54f, 0.27f, 0.07f);
                break;

            case "Aim_T7":
                target_image.color = new Color(0.5f, 0f, 0.5f);
                break;

            case "Aim_T6":
                target_image.color = Color.blue;
                break;

            case "Aim_T5":
                target_image.color = Color.cyan;
                break;

            case "Aim_T4":
                target_image.color = Color.green;
                break;

            case "Aim_T3":
                target_image.color = new Color(1f, 0.5f, 0f);
                break;

            case "Aim_T2":
                target_image.color = Color.yellow;
                break;

            case "Aim_T1":
            case "Aim_Default":
            default:
                target_image.color = Color.white;
                break;
        }
    }
}
