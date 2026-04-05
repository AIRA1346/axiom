using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// V: 1인칭 ↔ 3인칭 전환. 3인칭일 때는 <see cref="OpenWorldFollowCamera"/> + 마우스 X 로 몸(Yaw)만 회전.
/// </summary>
public sealed class OpenWorldCameraModeController : MonoBehaviour
{
    [SerializeField] private float _thirdPersonYawSensitivity = 0.18f;

    [Tooltip("3인칭 시 마우스 델타 노이즈 무시(픽셀).")]
    [SerializeField] private float _mouseDeltaDeadzonePixels = 0.12f;

    private Transform _playerRoot;

    private Camera _camera;

    private Transform _firstPersonPivot;

    private OpenWorldFirstPersonCamera _firstPerson;

    private OpenWorldFollowCamera _follow;

    private bool _thirdPerson;

    public bool IsThirdPerson => _thirdPerson;

    public void Initialize(
        Transform playerRoot,
        Camera mainCamera,
        Transform firstPersonPivot,
        OpenWorldFirstPersonCamera firstPersonScript,
        OpenWorldFollowCamera followScript)
    {
        _playerRoot = playerRoot;
        _camera = mainCamera;
        _firstPersonPivot = firstPersonPivot;
        _firstPerson = firstPersonScript;
        _follow = followScript;
        _thirdPerson = false;
        ApplyMode();
    }

    private void Update()
    {
        if (!Application.isPlaying || !enabled)
        {
            return;
        }

        if (_camera == null || _playerRoot == null)
        {
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.vKey.wasPressedThisFrame)
        {
            _thirdPerson = !_thirdPerson;
            ApplyMode();
        }

        if (_thirdPerson)
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue();
                if (delta.sqrMagnitude < _mouseDeltaDeadzonePixels * _mouseDeltaDeadzonePixels)
                {
                    delta = Vector2.zero;
                }

                if (delta.sqrMagnitude > 0.0001f)
                {
                    _playerRoot.Rotate(0f, delta.x * _thirdPersonYawSensitivity, 0f, Space.World);
                }
            }
        }
    }

    private void ApplyMode()
    {
        if (_camera == null)
        {
            return;
        }

        if (_thirdPerson)
        {
            if (_firstPerson != null)
            {
                _firstPerson.enabled = false;
            }

            _camera.transform.SetParent(null, true);
            if (_follow != null)
            {
                _follow.SetTarget(_playerRoot);
                _follow.enabled = true;
            }
        }
        else
        {
            if (_follow != null)
            {
                _follow.enabled = false;
            }

            if (_firstPersonPivot != null)
            {
                _camera.transform.SetParent(_firstPersonPivot, false);
                _camera.transform.localPosition = Vector3.zero;
                _camera.transform.localRotation = Quaternion.identity;
            }

            if (_firstPerson != null)
            {
                _firstPerson.enabled = true;
            }
        }
    }
}
