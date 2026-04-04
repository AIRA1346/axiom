using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 낚시 씬 1인칭 이동(WASD·Shift 달리기). 마우스 X는 몸 회전, Y는 FishingFirstPersonLook(카메라 피벗)에서 처리.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public sealed class FishingPlayerMotor : MonoBehaviour
{
    [SerializeField] private float _walkSpeed = 3.6f;
    [SerializeField] private float _sprintSpeed = 5.8f;
    [SerializeField] private float _mouseSensitivityX = 0.18f;
    [SerializeField] private float _gravityMultiplier = 1f;

    private CharacterController _cc;
    private Vector3 _verticalVelocity;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!Application.isPlaying || !enabled)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        Keyboard kb = Keyboard.current;
        if (mouse == null || kb == null)
        {
            return;
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 delta = mouse.delta.ReadValue();
            transform.Rotate(0f, delta.x * _mouseSensitivityX, 0f, Space.World);
        }

        float h = 0f;
        float v = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
        {
            h -= 1f;
        }

        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
        {
            h += 1f;
        }

        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
        {
            v += 1f;
        }

        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
        {
            v -= 1f;
        }

        Vector3 move = transform.right * h + transform.forward * v;
        if (move.sqrMagnitude > 1.01f)
        {
            move.Normalize();
        }

        float speed = kb.leftShiftKey.isPressed ? _sprintSpeed : _walkSpeed;
        bool canMove = Cursor.lockState == CursorLockMode.Locked;
        if (canMove)
        {
            _cc.Move(move * (speed * Time.deltaTime));
        }

        if (_cc.isGrounded)
        {
            _verticalVelocity.y = -1.5f;
        }
        else
        {
            _verticalVelocity.y += Physics.gravity.y * _gravityMultiplier * Time.deltaTime;
        }

        _cc.Move(_verticalVelocity * Time.deltaTime);
    }
}
