using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Measures a five-target aim precision test for the G.S.I flow.
/// This controller only handles target placement, hit judgment, and total-time measurement.
/// </summary>
public sealed class AimTestController : MonoBehaviour
{
    [SerializeField] private RectTransform _aimTargetRect;

    private const int TotalTargets = 5;

    private bool _isGameManagerSubscribed;
    private bool _isInputManagerSubscribed;
    private bool _isTestActive;
    private int _currentHitCount;
    private float _startTime;

    private void Awake()
    {
        SetTargetActive(false);
    }

    private void Start()
    {
        SubscribeToManagers();
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
            // Misses apply a 0.5-second time penalty to the final result.
            _startTime -= 0.5f;
            return;
        }

        _currentHitCount++;

        if (_currentHitCount >= TotalTargets)
        {
            float totalTime = Time.time - _startTime;

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.SaveTestTime(totalTime);
            }

            StopAimTest();
            GameManager.Instance.SetGameState(GameState.ResultScreen);
            return;
        }

        MoveTargetToRandomPosition();
    }

    /// <summary>
    /// Initializes a new aim test sequence and shows the first target.
    /// </summary>
    private void StartAimTest()
    {
        StopAimTest();

        _currentHitCount = 0;
        _startTime = Time.time;
        _isTestActive = true;

        ApplyEquippedTargetColor();
        MoveTargetToRandomPosition();
        SetTargetActive(true);
    }

    /// <summary>
    /// Clears the active aim test state and hides the target.
    /// </summary>
    private void StopAimTest()
    {
        _isTestActive = false;
        _currentHitCount = 0;
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
            Random.Range(-400f, 400f),
            Random.Range(-200f, 200f));
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

        string equipped_aim_target = InventoryManager.Instance != null
            ? InventoryManager.Instance.EquippedAimTarget
            : "Aim_Default";

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
