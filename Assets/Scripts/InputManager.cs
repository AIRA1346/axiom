using System;
using UnityEngine;

/// <summary>
/// Detects a single unified pointer input across mouse and touch devices.
/// This manager only reports input state changes and screen positions.
/// </summary>
public sealed class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    /// <summary>
    /// Invoked once when a unified input begins.
    /// Returns the screen position where the press or touch started.
    /// </summary>
    public event Action<Vector2> OnInputDown;

    /// <summary>
    /// Invoked while a unified input is being held or dragged.
    /// Returns the current screen position every frame during the hold.
    /// </summary>
    public event Action<Vector2> OnInputHold;

    /// <summary>
    /// Invoked once when the active unified input is released.
    /// </summary>
    public event Action OnInputUp;

    private bool _isPointerHeld;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"{gameObject.name}의 중복된 매니저 파괴됨.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (TryHandleTouchInput())
        {
            return;
        }

        HandleMouseInput();
    }

    /// <summary>
    /// Prioritizes touch on mobile and touch-enabled devices.
    /// Returns true when touch input was processed this frame.
    /// </summary>
    private bool TryHandleTouchInput()
    {
        if (Input.touchCount <= 0)
        {
            return false;
        }

        Touch touch = Input.GetTouch(0);
        Vector2 screenPosition = touch.position;

        switch (touch.phase)
        {
            case TouchPhase.Began:
                _isPointerHeld = true;
                OnInputDown?.Invoke(screenPosition);
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (_isPointerHeld)
                {
                    OnInputHold?.Invoke(screenPosition);
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (_isPointerHeld)
                {
                    _isPointerHeld = false;
                    OnInputUp?.Invoke();
                }
                break;
        }

        return true;
    }

    /// <summary>
    /// Handles a mouse left-click as the same unified input used by touch.
    /// </summary>
    private void HandleMouseInput()
    {
        Vector2 screenPosition = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"InputManager: 화면 클릭 감지됨! 좌표: {screenPosition}");
            _isPointerHeld = true;
            OnInputDown?.Invoke(screenPosition);
        }

        if (_isPointerHeld && Input.GetMouseButton(0))
        {
            OnInputHold?.Invoke(screenPosition);
        }

        if (_isPointerHeld && Input.GetMouseButtonUp(0))
        {
            _isPointerHeld = false;
            OnInputUp?.Invoke();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
