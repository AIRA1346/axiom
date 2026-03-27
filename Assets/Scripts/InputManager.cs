using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 마우스·터치를 단일 포인터 입력으로 통합합니다. 새 Input System 전용(구 Input Manager 비활성 프로젝트 대응).
/// </summary>
public sealed class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public event Action<Vector2> OnInputDown;
    public event Action<Vector2> OnInputHold;
    public event Action OnInputUp;

    private bool _isPointerHeld;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{gameObject.name}의 중복된 매니저 파괴됨.");
#endif
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (TryHandleTouchInput())
        {
            return;
        }

        HandleMouseInput();
#else
        LegacyUpdate();
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private bool TryHandleTouchInput()
    {
        Touchscreen ts = Touchscreen.current;
        if (ts == null)
        {
            return false;
        }

        var press = ts.primaryTouch.press;
        Vector2 screenPosition = ts.primaryTouch.position.ReadValue();

        if (press.wasPressedThisFrame)
        {
            _isPointerHeld = true;
            OnInputDown?.Invoke(screenPosition);
            return true;
        }

        if (_isPointerHeld && press.isPressed)
        {
            OnInputHold?.Invoke(screenPosition);
            return true;
        }

        if (_isPointerHeld && press.wasReleasedThisFrame)
        {
            _isPointerHeld = false;
            OnInputUp?.Invoke();
            return true;
        }

        return press.isPressed;
    }

    private void HandleMouseInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 screenPosition = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame)
        {
#if UNITY_EDITOR
            Debug.Log($"InputManager: 화면 클릭 감지됨! 좌표: {screenPosition}");
#endif
            _isPointerHeld = true;
            OnInputDown?.Invoke(screenPosition);
        }

        if (_isPointerHeld && mouse.leftButton.isPressed)
        {
            OnInputHold?.Invoke(screenPosition);
        }

        if (_isPointerHeld && mouse.leftButton.wasReleasedThisFrame)
        {
            _isPointerHeld = false;
            OnInputUp?.Invoke();
        }
    }
#else
    private void LegacyUpdate()
    {
        if (TryLegacyTouch())
        {
            return;
        }

        HandleLegacyMouse();
    }

    private bool TryLegacyTouch()
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

    private void HandleLegacyMouse()
    {
        Vector2 screenPosition = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
#if UNITY_EDITOR
            Debug.Log($"InputManager: 화면 클릭 감지됨! 좌표: {screenPosition}");
#endif
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
#endif

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
