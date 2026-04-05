using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 파쿠르 프로토타입: 벽 타기(W+Space, 정면 벽), 벽 달리기(공중+Shift+전진+옆 벽), 매틀(점프 시 난간).
/// 스태미너 소모. <see cref="OpenWorldPlayerMotor"/> 가 FixedUpdate 안에서 호출합니다.
/// </summary>
public sealed class OpenWorldParkourController : MonoBehaviour
{
    [Header("레이어")]
    [SerializeField] private LayerMask _wallLayers = ~0;

    [Header("벽 달리기")]
    [SerializeField] private float _wallRunSpeed = 6.2f;

    [SerializeField] private float _wallRunUpward = 1.1f;

    [SerializeField] private float _wallRunMaxTime = 1.15f;

    [SerializeField] private float _wallRunStaminaPerSecond = 28f;

    [SerializeField] private float _wallProbeDistance = 0.42f;

    [Header("벽 타기")]
    [SerializeField] private float _wallClimbSpeed = 3.4f;

    [SerializeField] private float _wallClimbStaminaPerSecond = 32f;

    [Header("매틀")]
    [SerializeField] private float _mantleStaminaCost = 28f;

    [SerializeField] private float _mantleUpImpulse = 4.2f;

    [SerializeField] private float _mantleForwardImpulse = 1.8f;

    [SerializeField] private float _mantleMaxHeight = 1.35f;

    [SerializeField] private float _mantleMinHeight = 0.35f;

    private Rigidbody _rb;

    private CapsuleCollider _col;

    private OpenWorldPlayerStamina _stamina;

    private OpenWorldPlayerStance _stance;

    private float _wallRunTimeLeft;

    private Vector3 _wallRunTangent;

    private bool _wallRunActive;

    public bool IsParkourActive { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<CapsuleCollider>();
        _stamina = GetComponent<OpenWorldPlayerStamina>();
        _stance = GetComponent<OpenWorldPlayerStance>();
    }

    /// <summary>
    /// true 이메 모터는 일반 이동·점프를 건너뜁니다(이미 처리됨).
    /// </summary>
    public bool TryProcessParkour(
        ref Vector3 velocity,
        bool grounded,
        bool cursorLocked,
        Vector3 localWishDir,
        bool jumpPressedThisFrame)
    {
        IsParkourActive = false;

        if (!cursorLocked || _rb == null || _col == null)
        {
            return false;
        }

        if (_stance != null && _stance.CurrentTarget != OpenWorldPlayerStance.StanceKind.Stand)
        {
            _wallRunActive = false;
            return false;
        }

        float dt = Time.fixedDeltaTime;
        Vector3 chest = transform.position + Vector3.up * 0.95f;

        if (TryMantle(ref velocity, grounded, jumpPressedThisFrame, chest))
        {
            IsParkourActive = true;
            return true;
        }

        if (TryWallClimb(ref velocity, grounded, localWishDir, chest, dt))
        {
            IsParkourActive = true;
            return true;
        }

        if (TryWallRun(ref velocity, grounded, localWishDir, chest, dt))
        {
            IsParkourActive = true;
            return true;
        }

        return false;
    }

    private bool TryMantle(ref Vector3 velocity, bool grounded, bool jumpPressedThisFrame, Vector3 chest)
    {
        if (!jumpPressedThisFrame || _stamina == null)
        {
            return false;
        }

        Vector3 forward = transform.forward;
        if (!Physics.Raycast(chest, forward, out RaycastHit wallHit, 0.55f, _wallLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (wallHit.normal.y > 0.35f)
        {
            return false;
        }

        Vector3 above = wallHit.point + Vector3.up * 0.08f + forward * 0.12f;
        if (!Physics.Raycast(above, Vector3.down, out RaycastHit topHit, _mantleMaxHeight + 0.5f, _wallLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        float feetY = transform.position.y;
        float rise = topHit.point.y - feetY;
        if (rise < _mantleMinHeight || rise > _mantleMaxHeight)
        {
            return false;
        }

        if (_stamina == null || !_stamina.TryConsume(_mantleStaminaCost))
        {
            return false;
        }

        velocity.y = _mantleUpImpulse;
        Vector3 flatF = forward;
        flatF.y = 0f;
        if (flatF.sqrMagnitude > 0.01f)
        {
            velocity += flatF.normalized * _mantleForwardImpulse;
        }

        return true;
    }

    private bool TryWallClimb(
        ref Vector3 velocity,
        bool grounded,
        Vector3 localWishDir,
        Vector3 chest,
        float dt)
    {
        if (_stamina == null)
        {
            return false;
        }

        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.spaceKey.isPressed || localWishDir.z < 0.2f)
        {
            return false;
        }

        if (!Physics.Raycast(chest, transform.forward, out RaycastHit hit, _wallProbeDistance, _wallLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (hit.normal.y > 0.4f || Vector3.Angle(Vector3.up, hit.normal) < 25f)
        {
            return false;
        }

        float cost = _wallClimbStaminaPerSecond * dt;
        if (_stamina != null && !_stamina.TryConsume(cost))
        {
            return false;
        }

        velocity.y = Mathf.Max(velocity.y, _wallClimbSpeed);
        Vector3 flatVel = new Vector3(velocity.x, 0f, velocity.z);
        flatVel = Vector3.MoveTowards(flatVel, Vector3.zero, 40f * dt);
        velocity.x = flatVel.x;
        velocity.z = flatVel.z;
        return true;
    }

    private bool TryWallRun(
        ref Vector3 velocity,
        bool grounded,
        Vector3 localWishDir,
        Vector3 chest,
        float dt)
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || _stamina == null)
        {
            return false;
        }

        bool wantRun = !grounded && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) && localWishDir.z > 0.15f;
        if (!wantRun && !_wallRunActive)
        {
            return false;
        }

        Vector3 right = transform.right;
        bool hitWall = false;
        Vector3 wallN = Vector3.zero;

        if (Physics.Raycast(chest, right, out RaycastHit hr, _wallProbeDistance, _wallLayers, QueryTriggerInteraction.Ignore))
        {
            hitWall = true;
            wallN = hr.normal;
        }
        else if (Physics.Raycast(chest, -right, out RaycastHit hl, _wallProbeDistance, _wallLayers, QueryTriggerInteraction.Ignore))
        {
            hitWall = true;
            wallN = hl.normal;
        }

        if (!hitWall)
        {
            _wallRunActive = false;
            _wallRunTimeLeft = 0f;
            return false;
        }

        if (wallN.y > 0.45f)
        {
            _wallRunActive = false;
            return false;
        }

        Vector3 up = Vector3.up;
        Vector3 tangent = Vector3.Cross(wallN, up);
        if (tangent.sqrMagnitude < 0.01f)
        {
            return false;
        }

        tangent.Normalize();
        if (Vector3.Dot(tangent, transform.forward) < 0f)
        {
            tangent = -tangent;
        }

        if (!_wallRunActive)
        {
            if (!wantRun)
            {
                return false;
            }

            if (_stamina == null || !_stamina.CanStartSprint)
            {
                return false;
            }

            _wallRunActive = true;
            _wallRunTimeLeft = _wallRunMaxTime;
            _wallRunTangent = tangent;
        }
        else
        {
            _wallRunTimeLeft -= dt;
            if (_wallRunTimeLeft <= 0f || !wantRun)
            {
                _wallRunActive = false;
                return false;
            }

            tangent = _wallRunTangent;
        }

        float cost = _wallRunStaminaPerSecond * dt;
        if (_stamina != null && !_stamina.TryConsume(cost))
        {
            _wallRunActive = false;
            return false;
        }

        Vector3 along = tangent * _wallRunSpeed;
        velocity.x = along.x;
        velocity.z = along.z;
        velocity.y = Mathf.Max(velocity.y * 0.35f, 0f) + _wallRunUpward;
        return true;
    }
}
