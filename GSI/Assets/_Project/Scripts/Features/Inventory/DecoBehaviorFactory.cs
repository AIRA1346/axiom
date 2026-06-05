/// <summary>
/// 데코레이션 아이템의 ItemId에 따른 행동 전략(DecoBehavior) 인스턴스를 동적으로 생성 및 매핑해 주는 팩토리 클래스
/// </summary>
public static class DecoBehaviorFactory
{
    public static DecoBehavior Create(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return new DefaultDecoBehavior();
        }

        // [미래 기믹 아이템 확장 구현부]
        // 예:
        // if (itemId == "deco_shooting_star") return new ShootingStarBehavior();
        // if (itemId == "deco_slow_field") return new SlowFieldBehavior();

        // 기존 3종 데코레이션("deco_yellow_star", "deco_purple_crystal", "deco_neon_ring") 및 그 외는 기본 유영/충돌 전략 적용
        return new DefaultDecoBehavior();
    }
}
