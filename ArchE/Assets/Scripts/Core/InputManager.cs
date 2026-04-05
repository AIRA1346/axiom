using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 마우스·터치를 단일 포인터 입력으로 통합합니다.
/// Player Settings가 Input System 전용이므로 레거시 UnityEngine.Input은 사용하지 않습니다.
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
        if (TryHandleTouchInput())
        {
            return;
        }

        HandleMouseInput();
    }

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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
