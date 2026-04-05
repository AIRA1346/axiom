using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 루트(Yaw) + 로컬 피치(1인칭). Input System 마우스 사용.
/// Unity-Chan 이동(레거시 Input)과 병행하려면 Active Input Handling = Both 권장.
/// </summary>
public sealed class OpenWorldFirstPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform _playerRoot;

    [SerializeField] private float _mouseSensitivityX = 0.18f;

    [SerializeField] private float _mouseSensitivityY = 0.18f;

    [SerializeField] private float _pitchMin = -88f;

    [SerializeField] private float _pitchMax = 88f;

    [SerializeField] private bool _lockCursorOnStart = true;

    [Tooltip("ESC 로 커서 잠금 토글(메뉴 클릭 등).")]
    [SerializeField] private bool _escapeTogglesCursor = true;

    [Header("드리프트 방지")]
    [Tooltip("마우스를 움직이지 않아도 delta 에 노이즈가 들어오는 경우가 있어, 이 값(픽셀) 이하면 무시합니다.")]
    [SerializeField] private float _mouseDeltaDeadzonePixels = 0.12f;

    private float _pitch;

    private void Start()
    {
        if (_lockCursorOnStart && Application.isPlaying)
        {
            LockCursor(true);
        }

        Vector3 e = transform.localEulerAngles;
        _pitch = e.x;
        if (_pitch > 180f)
        {
            _pitch -= 360f;
        }
    }

    private void Update()
    {
        if (!Application.isPlaying || !enabled)
        {
            return;
        }

        if (_escapeTogglesCursor && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            LockCursor(!locked);
        }

        Mouse mouse = Mouse.current;
        if (mouse == null || _playerRoot == null)
        {
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        if (delta.sqrMagnitude < _mouseDeltaDeadzonePixels * _mouseDeltaDeadzonePixels)
        {
            delta = Vector2.zero;
        }

        _playerRoot.Rotate(0f, delta.x * _mouseSensitivityX, 0f, Space.World);

        _pitch -= delta.y * _mouseSensitivityY;
        _pitch = Mathf.Clamp(_pitch, _pitchMin, _pitchMax);
        transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    /// <summary>런타임에 OpenWorldBootstrap 이 플레이어 루트를 연결합니다.</summary>
    public void SetPlayerRoot(Transform playerRoot)
    {
        _playerRoot = playerRoot;
    }
}
