using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 오픈월드 1인칭 이동: WASD, Shift 달리기, Ctrl 느린 걷기, Space 점프.
/// 코요테 타임·점프 버퍼·지면 SphereCast·공중 제어·낙하 속도 제한.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public sealed class OpenWorldPlayerMotor : MonoBehaviour
{
    [Header("속도 (m/s) — md/OpenWorldSpec.md 기준")]
    [SerializeField] private float _walkSpeed = 1.4f;

    [SerializeField] private float _sprintSpeed = 6.5f;

    [Tooltip("Ctrl 느린 이동. 스펙에 별도 수치 없음 — 필요 시 조정.")]
    [SerializeField] private float _slowWalkSpeed = 0.95f;

    [Header("가속·감속")]
    [SerializeField] private float _groundAcceleration = 88f;

    [SerializeField] private float _groundDeceleration = 115f;

    [SerializeField] private float _airAcceleration = 18f;

    [SerializeField] private float _airDeceleration = 8f;

    [Header("점프")]
    [SerializeField] private float _jumpVelocity = 5.6f;

    [Tooltip("땅을 떠난 직후에도 이 시간(초) 안이면 점프 허용.")]
    [SerializeField] private float _coyoteTime = 0.14f;

    [Tooltip("Space 를 눌렀을 때 착지까지 이 시간(초) 안이면 점프 실행.")]
    [SerializeField] private float _jumpBufferTime = 0.12f;

    [Header("지면")]
    [SerializeField] private float _groundProbeDistance = 0.22f;

    [Tooltip("지면으로만 판정할 레이어. 비어 있으면 Everything.")]
    [SerializeField] private LayerMask _groundLayers = ~0;

    [Tooltip("착지 시 아래로 살짝 밀어 지형에 붙게 함.")]
    [SerializeField] private float _groundStickVelocity = -1.2f;

    [Header("공중")]
    [SerializeField] private float _gravityMultiplier = 1.08f;

    [SerializeField] private float _maxFallSpeed = 52f;

    [Header("애니메이션 (Unity-Chan Locomotion)")]
    [SerializeField] private float _animSpeedWalk = 0.44f;

    [SerializeField] private float _animSpeedSprint = 1f;

    [SerializeField] private float _animSpeedSlow = 0.28f;

    [SerializeField] private float _animPlaybackSprint = 1.12f;

    [SerializeField] private float _animPlaybackSlow = 0.78f;

    [Range(0f, 1f)]
    [SerializeField] private float _sprintDiagonalRunBoost = 0.85f;

    private Rigidbody _rb;

    private CapsuleCollider _col;

    private Animator _anim;

    private bool _grounded;

    private float _coyoteTimer;

    private float _jumpBufferTimer;

    private bool _jumpPressedThisFrame;

    private static readonly int AnimSpeed = Animator.StringToHash("Speed");

    private static readonly int AnimDirection = Animator.StringToHash("Direction");

    private OpenWorldPlayerStance _stance;

    private OpenWorldPlayerStamina _stamina;

    private OpenWorldParkourController _parkour;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<CapsuleCollider>();
        _anim = GetComponent<Animator>();
        _stance = GetComponent<OpenWorldPlayerStance>();
        _stamina = GetComponent<OpenWorldPlayerStamina>();
        _parkour = GetComponent<OpenWorldParkourController>();

        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rb.useGravity = true;
    }

    private void OnEnable()
    {
        foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
        {
            if (mb != null && mb.GetType().Name == "UnityChanControlScriptWithRgidBody")
            {
                mb.enabled = false;
                break;
            }
        }
    }

    private void Update()
    {
        if (!Application.isPlaying || !enabled)
        {
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Keyboard kb = Keyboard.current;
        if (kb == null)
        {
            return;
        }

        if (kb.spaceKey.wasPressedThisFrame)
        {
            _jumpPressedThisFrame = true;
            _jumpBufferTimer = _jumpBufferTime;
        }
    }

    private void FixedUpdate()
    {
        if (!Application.isPlaying || !enabled)
        {
            return;
        }

        Keyboard kb = Keyboard.current;
        if (kb == null)
        {
            return;
        }

        bool cursorLocked = Cursor.lockState == CursorLockMode.Locked;

        _grounded = CheckGrounded();

        if (_grounded)
        {
            _coyoteTimer = _coyoteTime;
        }
        else
        {
            _coyoteTimer -= Time.fixedDeltaTime;
        }

        if (_jumpBufferTimer > 0f)
        {
            _jumpBufferTimer -= Time.fixedDeltaTime;
        }

        float h = 0f;
        float v = 0f;
        if (cursorLocked)
        {
            if (kb.aKey.isPressed)
            {
                h -= 1f;
            }

            if (kb.dKey.isPressed)
            {
                h += 1f;
            }

            if (kb.wKey.isPressed)
            {
                v += 1f;
            }

            if (kb.sKey.isPressed)
            {
                v -= 1f;
            }
        }

        Vector3 localInput = new Vector3(h, 0f, v);
        if (localInput.sqrMagnitude > 1.01f)
        {
            localInput.Normalize();
        }

        bool hasMoveInput = localInput.sqrMagnitude > 0.0001f;

        float speed;
        bool slow = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
        bool sprintInput = !slow && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) &&
                           (_stance == null || _stance.CanSprint);
        bool sprint = sprintInput;
        if (_stamina != null && sprint && _stamina.Current <= 0.01f)
        {
            sprint = false;
        }

        if (slow)
        {
            speed = _slowWalkSpeed;
        }
        else if (sprint)
        {
            speed = _sprintSpeed;
        }
        else
        {
            speed = _walkSpeed;
        }

        Vector3 worldFlat = transform.right * localInput.x + transform.forward * localInput.z;
        if (worldFlat.sqrMagnitude > 0.0001f)
        {
            worldFlat.Normalize();
        }

        float stanceMult = _stance != null ? _stance.MoveSpeedMultiplier : 1f;
        Vector3 targetHorizontal = worldFlat * speed * stanceMult;
        Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

        float accel;
        float decel;
        if (_grounded)
        {
            accel = _groundAcceleration;
            decel = _groundDeceleration;
        }
        else
        {
            accel = _airAcceleration;
            decel = _airDeceleration;
        }

        Vector3 newFlat;
        if (hasMoveInput && cursorLocked)
        {
            newFlat = Vector3.MoveTowards(flatVel, targetHorizontal, accel * Time.fixedDeltaTime);
        }
        else
        {
            newFlat = Vector3.MoveTowards(flatVel, Vector3.zero, decel * Time.fixedDeltaTime);
        }

        Vector3 vel = _rb.linearVelocity;
        vel.x = newFlat.x;
        vel.z = newFlat.z;

        if (_parkour != null && _parkour.TryProcessParkour(
                ref vel,
                _grounded,
                cursorLocked,
                localInput,
                _jumpPressedThisFrame))
        {
            _rb.linearVelocity = vel;
            _rb.angularVelocity = Vector3.zero;
            _jumpPressedThisFrame = false;
            _jumpBufferTimer = 0f;
            if (_anim != null)
            {
                float dt = Time.fixedDeltaTime;
                _anim.SetFloat(AnimSpeed, 0f, 0.15f, dt);
                _anim.SetFloat(AnimDirection, 0f, 0.15f, dt);
            }

            return;
        }

        bool wantJump = cursorLocked && (_jumpPressedThisFrame || _jumpBufferTimer > 0f);
        bool canJump = (_grounded || _coyoteTimer > 0f) && (_stance == null || _stance.CanJump);
        bool didJump = wantJump && canJump;

        if (didJump)
        {
            vel.y = _jumpVelocity;
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            if (_anim != null)
            {
                _anim.SetBool("Jump", true);
                StartCoroutine(ClearJumpBoolAfterDelay());
            }
        }
        else if (_grounded && vel.y <= 0.1f && vel.y > -4f)
        {
            vel.y = _groundStickVelocity;
        }

        _rb.linearVelocity = vel;

        if (!didJump && !_grounded && _gravityMultiplier > 1.0001f)
        {
            _rb.AddForce(Physics.gravity * (_gravityMultiplier - 1f), ForceMode.Acceleration);
        }

        vel = _rb.linearVelocity;
        if (vel.y < -_maxFallSpeed)
        {
            vel.y = -_maxFallSpeed;
            _rb.linearVelocity = vel;
        }

        _rb.angularVelocity = Vector3.zero;

        if (_stamina != null)
        {
            if (sprint)
            {
                _stamina.ApplySprintDrain(Time.fixedDeltaTime);
            }
            else
            {
                _stamina.ApplyRegen(Time.fixedDeltaTime);
            }
        }

        UpdateAnimator(cursorLocked, slow, sprint, localInput, hasMoveInput);

        _jumpPressedThisFrame = false;
    }

    private void UpdateAnimator(
        bool cursorLocked,
        bool slow,
        bool sprint,
        Vector3 localInput,
        bool hasMoveInput)
    {
        if (_anim == null)
        {
            return;
        }

        float animScale = slow ? _animSpeedSlow : (sprint ? _animSpeedSprint : _animSpeedWalk);
        float forward = localInput.z;
        float strafe = localInput.x;

        if (sprint && Mathf.Abs(forward) > 0.08f && Mathf.Abs(strafe) > 0.08f)
        {
            forward = Mathf.Sign(forward) *
                      Mathf.Max(Mathf.Abs(forward), Mathf.Abs(strafe) * _sprintDiagonalRunBoost);
        }

        float targetSpeed = forward * animScale;
        float targetDir = strafe * animScale;
        if (!hasMoveInput || !cursorLocked)
        {
            targetSpeed = 0f;
            targetDir = 0f;
        }

        float damp = !hasMoveInput || !cursorLocked ? 0.2f : 0.09f;
        float dt = Time.fixedDeltaTime;
        _anim.SetFloat(AnimSpeed, targetSpeed, damp, dt);
        _anim.SetFloat(AnimDirection, targetDir, damp, dt);
        _anim.speed = sprint ? _animPlaybackSprint : (slow ? _animPlaybackSlow : 1f);
    }

    private IEnumerator ClearJumpBoolAfterDelay()
    {
        yield return new WaitForSeconds(0.12f);
        if (_anim != null)
        {
            _anim.SetBool("Jump", false);
        }
    }

    private bool CheckGrounded()
    {
        Vector3 worldCenter = transform.TransformPoint(_col.center);
        float half = Mathf.Max(0.01f, _col.height * 0.5f - _col.radius);
        Vector3 bottom = worldCenter - Vector3.up * half;
        float sphereRadius = _col.radius * 0.92f;
        float castDist = _groundProbeDistance;

        if (Physics.SphereCast(
                bottom + Vector3.up * (sphereRadius * 0.35f),
                sphereRadius * 0.88f,
                Vector3.down,
                out RaycastHit hit,
                castDist + sphereRadius * 0.5f,
                _groundLayers,
                QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        Vector3 origin = bottom + Vector3.up * 0.06f;
        float rayLen = castDist + sphereRadius * 0.4f;
        return Physics.Raycast(origin, Vector3.down, rayLen, _groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void OnDrawGizmosSelected()
    {
        if (_col == null)
        {
            _col = GetComponent<CapsuleCollider>();
        }

        if (_col == null)
        {
            return;
        }

        Vector3 worldCenter = transform.TransformPoint(_col.center);
        float half = Mathf.Max(0.01f, _col.height * 0.5f - _col.radius);
        Vector3 bottom = worldCenter - Vector3.up * half;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(bottom + Vector3.up * (_col.radius * 0.35f), _col.radius * 0.88f * 0.92f);
    }
}
