using UnityEngine;

/// <summary>
/// G.S.I 공용 우주 천체 물리(관성 유영, 경계 반사, 탄성 충돌)가 적용되는 모든 2D 객체들의 공통 인터페이스 규격
/// </summary>
public interface ICosmicKineticObject
{
    RectTransform rectTransform { get; }
    Vector2 velocity { get; set; }
    float collisionRadius { get; }
    bool isDragging { get; }
    
    /// <summary>
    /// 물리 틱 업데이트 (마찰력, 속도 제한, 경계 튕김 등 처리)
    /// </summary>
    void UpdatePhysicsTick(float deltaTime);

    /// <summary>
    /// 다른 물리 객체와의 탄성 충돌 및 겹침 해결
    /// </summary>
    void ResolveCollisionWith(ICosmicKineticObject other);
}
