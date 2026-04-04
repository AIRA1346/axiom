using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 1인칭 수직 시야(피치만). 부모 오브젝트(플레이어)는 FishingPlayerMotor가 Yaw로 회전합니다.
/// </summary>
public sealed class FishingFirstPersonLook : MonoBehaviour
{
    [SerializeField] private float _sensitivityY = 0.18f;
    [SerializeField] private float _pitchMin = -72f;
    [SerializeField] private float _pitchMax = 72f;
    [SerializeField] private bool _lockCursorOnStart = true;

    private float _pitch;

    private void Start()
    {
        if (_lockCursorOnStart && Application.isPlaying)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        _pitch -= delta.y * _sensitivityY;
        _pitch = Mathf.Clamp(_pitch, _pitchMin, _pitchMax);
        transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }
}
