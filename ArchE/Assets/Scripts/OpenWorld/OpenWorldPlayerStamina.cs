using UnityEngine;

/// <summary>
/// 스태미너: 달리기·파쿠르 소모, 정지/걷기 시 회복.
/// </summary>
public sealed class OpenWorldPlayerStamina : MonoBehaviour
{
    [SerializeField] private float _maxStamina = 100f;

    [SerializeField] private float _regenPerSecond = 20f;

    [SerializeField] private float _sprintDrainPerSecond = 24f;

    [Tooltip("이 값 미만이면 달리기 시작 불가(완전히 0까지는 아님).")]
    [SerializeField] private float _minStaminaToStartSprint = 4f;

    private float _current;

    public float Current => _current;

    public float MaxStamina => _maxStamina;

    public float Normalized => _maxStamina > 0.001f ? _current / _maxStamina : 0f;

    public bool CanStartSprint => _current >= _minStaminaToStartSprint;

    private void Awake()
    {
        _current = _maxStamina;
    }

    public void ApplySprintDrain(float deltaTime)
    {
        _current -= _sprintDrainPerSecond * deltaTime;
        _current = Mathf.Clamp(_current, 0f, _maxStamina);
    }

    public void ApplyRegen(float deltaTime)
    {
        _current = Mathf.Min(_maxStamina, _current + _regenPerSecond * deltaTime);
    }

    public bool TryConsume(float amount)
    {
        if (_current < amount)
        {
            return false;
        }

        _current -= amount;
        return true;
    }

    public void Drain(float amount)
    {
        _current = Mathf.Max(0f, _current - amount);
    }
}
