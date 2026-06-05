using UnityEngine;

/// <summary>
/// 배치된 데코레이션 아이템의 자율 행동(물리, 충돌, 시각 연출, 타 개체 영향력 등)을 정의하는 추상 전략 클래스
/// </summary>
public abstract class DecoBehavior
{
    protected GsiPlacedDeco _deco;

    public virtual void Initialize(GsiPlacedDeco deco)
    {
        _deco = deco;
    }

    /// <summary>
    /// 물리 연산 틱 처리 (속도 한계, 마찰, 경계면 반사 등)
    /// </summary>
    public abstract void UpdatePhysics(float deltaTime);

    /// <summary>
    /// 타 물리 개체와의 충돌 해결 및 탄성 튕김 연산
    /// </summary>
    public abstract void ResolveCollision(ICosmicKineticObject other);

    /// <summary>
    /// 매 프레임 시각적 애니메이션 및 특수 효과 업데이트
    /// </summary>
    public virtual void UpdateVisual(float unscaledTime, bool isHovered, bool isDragging) { }

    /// <summary>
    /// 필드 영역 데코가 다른 씬 내 천체에 시공간적 물리 영향을 가할 때 계산하는 루프
    /// </summary>
    public virtual void ApplyInfluence(ICosmicKineticObject target, float deltaTime) { }

    // ─── 공용 물리 및 충돌 유틸리티 ────────────────────────────────────────────────

    protected void DefaultUpdatePhysics(float deltaTime)
    {
        if (_deco == null || _deco.isDragging || _deco.IsLocked) return;

        float currentSpeed = _deco.velocity.magnitude;
        float maxVelocity = 1100f; // 데코 전용 속도 제약
        float minDriftSpeed = 20f;
        float friction = 0.35f;

        // 1. 최대 속도 초과 시 제한 적용
        if (currentSpeed > maxVelocity)
        {
            _deco.velocity = _deco.velocity.normalized * maxVelocity;
            currentSpeed = maxVelocity;
        }

        // 2. 마찰력에 의한 감속 처리
        if (currentSpeed > minDriftSpeed)
        {
            float newSpeed = currentSpeed * Mathf.Exp(-friction * deltaTime);
            newSpeed = Mathf.Max(newSpeed, minDriftSpeed);
            _deco.velocity = _deco.velocity.normalized * newSpeed;
        }

        // 좌표 갱신
        _deco.rectTransform.anchoredPosition += _deco.velocity * deltaTime;

        // 경계면 충돌 반사
        _deco.HandleScreenBoundaries();
    }

    protected void DefaultResolveCollision(ICosmicKineticObject other)
    {
        if (_deco == null || other == null || other == _deco || _deco.IsLocked) return;
        if (other is GsiPlacedDeco otherDeco && otherDeco.IsLocked) return;

        RectTransform otherRt = other.rectTransform;
        if (otherRt == null || _deco.rectTransform == null) return;

        float minDistance = _deco.collisionRadius + other.collisionRadius;
        Vector2 diff = otherRt.anchoredPosition - _deco.rectTransform.anchoredPosition;
        float distance = diff.magnitude;

        if (distance < 0.01f)
        {
            _deco.rectTransform.anchoredPosition += new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            return;
        }

        if (distance < minDistance)
        {
            Vector2 normal = diff / distance;
            float overlap = minDistance - distance;

            // 1. 밀어내기 (겹침 강제 분리)
            float pushSelf = _deco.isDragging ? 0f : (other.isDragging ? 1f : 0.5f);
            float pushOther = other.isDragging ? 0f : (_deco.isDragging ? 1f : 0.5f);

            _deco.rectTransform.anchoredPosition -= normal * overlap * pushSelf;
            otherRt.anchoredPosition += normal * overlap * pushOther;

            // 2. 탄성 튕김 속도 전달
            Vector2 rv = other.velocity - _deco.velocity;
            float velAlongNormal = Vector2.Dot(rv, normal);

            if (velAlongNormal < 0f)
            {
                float restitution = 0.95f;
                float impulseScalar = -(1f + restitution) * velAlongNormal / 2f;
                Vector2 impulse = normal * impulseScalar;

                if (!_deco.isDragging)
                {
                    _deco.velocity -= impulse;
                }
                if (!other.isDragging)
                {
                    other.velocity += impulse;
                }
            }
        }
    }
}

/// <summary>
/// 기본형 자율 유영 및 충돌 데코레이션 행동 클래스 (Yellow Star, Purple Crystal, Neon Ring 등 기존 3종에 장착)
/// </summary>
public sealed class DefaultDecoBehavior : DecoBehavior
{
    public override void UpdatePhysics(float deltaTime)
    {
        DefaultUpdatePhysics(deltaTime);
    }

    public override void ResolveCollision(ICosmicKineticObject other)
    {
        DefaultResolveCollision(other);
    }

    public override void UpdateVisual(float unscaledTime, bool isHovered, bool isDragging)
    {
        if (_deco == null) return;
        float time = unscaledTime;

        // 아이템별 고유 연출 애니메이션 (호버 시 속도 가속 연동, 고정 시에도 애니메이션은 지속 작동)
        if (_deco.ItemId == "deco_yellow_star")
        {
            if (_deco.visualRoot != null)
            {
                float rotSpeed = isHovered ? 45f : 10f;
                _deco.visualRoot.localRotation = Quaternion.Euler(0f, 0f, time * rotSpeed);
            }
            if (_deco.glowLayer != null)
            {
                float pulse = 0.8f + Mathf.PingPong(time * 0.4f, 0.3f);
                _deco.glowLayer.localScale = new Vector3(pulse, pulse, 1f);
            }
            if (_deco.spikeV != null && _deco.spikeH != null)
            {
                float spPulse = 0.85f + Mathf.PingPong(time * 1.5f, 0.25f);
                _deco.spikeV.localScale = new Vector3(1f, spPulse, 1f);
                _deco.spikeH.localScale = new Vector3(spPulse, 1f, 1f);
            }
        }
        else if (_deco.ItemId == "deco_purple_crystal")
        {
            if (_deco.visualRoot != null)
            {
                float floatOffset = Mathf.Sin(time * 1.8f) * 6f;
                if (isHovered && !isDragging) floatOffset *= 1.4f;
                _deco.visualRoot.localPosition = new Vector3(0f, floatOffset, 0f);
            }
            if (_deco.glowLayer != null)
            {
                float pulse = 0.85f + Mathf.Sin(time * 2.2f) * 0.15f;
                _deco.glowLayer.localScale = new Vector3(pulse, pulse, 1f);
            }
        }
        else if (_deco.ItemId == "deco_neon_ring")
        {
            if (_deco.glowLayer != null)
            {
                float rotSpeed = isHovered ? -120f : -40f;
                _deco.glowLayer.Rotate(0f, 0f, rotSpeed * Time.unscaledDeltaTime);
            }
        }
    }
}
