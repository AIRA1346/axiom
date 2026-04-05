using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// C: 앉기(웅크리기), Z: 엎드리기(우선). 캡슐·1인칭 시점 높이 조정. 이동 속도 배율 제공.
/// </summary>
public sealed class OpenWorldPlayerStance : MonoBehaviour
{
    public enum StanceKind
    {
        Stand = 0,
        Crouch = 1,
        Prone = 2,
    }

    [SerializeField] private float _stanceBlendSpeed = 14f;

    [SerializeField] private float _crouchHeightMultiplier = 0.52f;

    [SerializeField] private float _proneMinHeight = 0.42f;

    [SerializeField] private float _eyeHeightStand = 1.55f;

    [SerializeField] private float _eyeHeightCrouchMultiplier = 0.58f;

    [SerializeField] private float _eyeHeightProne = 0.28f;

    [Tooltip("1인칭 피벗이 캐릭터 루트에만 있을 때 카메라를 얼굴 쪽(+Z)으로 당깁니다. Head 본 부착 시에는 무시됩니다.")]
    [SerializeField] private float _rootPivotForwardZ = 0.16f;

    private CapsuleCollider _col;

    private Transform _fpPivot;

    private bool _pivotOnHead;

    private Vector3 _pivotStandLocal;

    private float _heightStand;

    private Vector3 _centerStand;

    private float _radius;

    private float _heightTarget;

    private Vector3 _centerTarget;

    private float _eyeTarget;

    public StanceKind CurrentTarget { get; private set; } = StanceKind.Stand;

    /// <summary>이동 속도에 곱합니다. (달리기 불가 시 모터에서 별도 처리)</summary>
    public float MoveSpeedMultiplier { get; private set; } = 1f;

    public bool CanSprint => CurrentTarget == StanceKind.Stand;

    public bool CanJump => CurrentTarget != StanceKind.Prone;

    private void Awake()
    {
        _col = GetComponent<CapsuleCollider>();
        if (_col != null)
        {
            _heightStand = _col.height;
            _centerStand = _col.center;
            _radius = _col.radius;
        }
    }

    private void FixedUpdate()
    {
        if (!Application.isPlaying || !enabled)
        {
            return;
        }

        Keyboard kb = Keyboard.current;
        if (kb == null || _col == null)
        {
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            CurrentTarget = StanceKind.Stand;
        }
        else if (kb.zKey.isPressed)
        {
            CurrentTarget = StanceKind.Prone;
        }
        else if (kb.cKey.isPressed)
        {
            CurrentTarget = StanceKind.Crouch;
        }
        else
        {
            CurrentTarget = StanceKind.Stand;
        }

        switch (CurrentTarget)
        {
            case StanceKind.Prone:
                _heightTarget = Mathf.Max(_proneMinHeight, _radius * 2.05f);
                _centerTarget = new Vector3(
                    _centerStand.x,
                    _centerStand.y - (_heightStand - _heightTarget) * 0.5f,
                    _centerStand.z);
                _eyeTarget = _eyeHeightProne;
                MoveSpeedMultiplier = 0.28f;
                break;
            case StanceKind.Crouch:
                _heightTarget = _heightStand * _crouchHeightMultiplier;
                _centerTarget = new Vector3(
                    _centerStand.x,
                    _centerStand.y - (_heightStand - _heightTarget) * 0.5f,
                    _centerStand.z);
                _eyeTarget = _eyeHeightStand * _eyeHeightCrouchMultiplier;
                MoveSpeedMultiplier = 0.55f;
                break;
            default:
                _heightTarget = _heightStand;
                _centerTarget = _centerStand;
                _eyeTarget = _eyeHeightStand;
                MoveSpeedMultiplier = 1f;
                break;
        }

        float t = Mathf.Clamp01(_stanceBlendSpeed * Time.fixedDeltaTime);
        _col.height = Mathf.Lerp(_col.height, _heightTarget, t);
        _col.center = Vector3.Lerp(_col.center, _centerTarget, t);

        if (_fpPivot != null)
        {
            Vector3 target;
            if (_pivotOnHead)
            {
                float eyeScale = _eyeTarget / Mathf.Max(0.01f, _eyeHeightStand);
                eyeScale = Mathf.Clamp(eyeScale, 0.18f, 1.1f);
                float zScale = Mathf.Lerp(0.72f, 1f, eyeScale);
                target = new Vector3(
                    _pivotStandLocal.x,
                    _pivotStandLocal.y * eyeScale,
                    _pivotStandLocal.z * zScale);
            }
            else
            {
                target = new Vector3(0f, _eyeTarget, _rootPivotForwardZ);
            }

            _fpPivot.localPosition = Vector3.Lerp(_fpPivot.localPosition, target, t);
        }
    }

    public void BindFirstPersonPivot(Transform pivot)
    {
        _fpPivot = pivot;
        if (_fpPivot == null)
        {
            return;
        }

        _pivotOnHead = _fpPivot.parent != transform;
        _pivotStandLocal = _fpPivot.localPosition;

        if (!_pivotOnHead)
        {
            Vector3 lp = _fpPivot.localPosition;
            lp.y = _eyeHeightStand;
            lp.z = _rootPivotForwardZ;
            _fpPivot.localPosition = lp;
            _pivotStandLocal = _fpPivot.localPosition;
        }
    }

    public void ConfigureEyeHeights(float standEyeHeight)
    {
        _eyeHeightStand = standEyeHeight;
    }
}
