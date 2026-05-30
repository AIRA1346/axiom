using System;
using UnityEngine;
using ArchE.Game;

/// <summary>
/// G.S.I 공식 3중 화폐 경제 시스템 매니저 (Stardust, Astral Core, Arc Ticket).
/// 구버전 마이그레이션 로직을 완전히 제거하고 견고하고 단순한 저장을 보장합니다.
/// </summary>
public sealed class EconomyManager : MonoBehaviour
{
    private const string StardustKey = "GSI_Stardust";
    private const string AstralCoresKey = "GSI_AstralCores";
    private const string ArcTicketsKey = "GSI_ArcTickets";
    private const int DefaultTicketCount = 3;

    public static EconomyManager Instance { get; private set; }

    public event Action OnEconomyChanged;

    // 신규 3중 화폐 프로퍼티
    public int Stardust => GsiSaveSystem.GetInt(StardustKey, 0);
    public int AstralCores => GsiSaveSystem.GetInt(AstralCoresKey, 0);
    public int ArcTickets => GsiSaveSystem.GetInt(ArcTicketsKey, DefaultTicketCount);

    // 구버전 호환용 프로퍼티 (컴파일 무결성 및 호환성 보장)
    [Obsolete("Use Stardust instead")]
    public int Tokens => Stardust;
    [Obsolete("Use ArcTickets instead")]
    public int ExamTickets => ArcTickets;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // [주인님 테스트용 치트]: 최초 1회 10만 별가루 무상 지급
        if (GsiSaveSystem.GetInt("GSI_TestStardustGiven_v2", 0) == 0)
        {
            AddStardust(100000);
            GsiSaveSystem.SetInt("GSI_TestStardustGiven_v2", 1);
            GsiSaveSystem.Save();
        }
    }

    // 1. 별가루 (Stardust) 관련 메서드
    public void AddStardust(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        GsiSaveSystem.SetInt(StardustKey, Stardust + amount);
        GsiSaveSystem.Save();
        OnEconomyChanged?.Invoke();
    }

    public bool SpendStardust(int amount)
    {
        if (amount <= 0 || Stardust < amount)
        {
            return false;
        }

        GsiSaveSystem.SetInt(StardustKey, Stardust - amount);
        GsiSaveSystem.Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    // 2. 아스트랄 코어 (Astral Core) 관련 메서드
    public void AddAstralCores(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        GsiSaveSystem.SetInt(AstralCoresKey, AstralCores + amount);
        GsiSaveSystem.Save();
        OnEconomyChanged?.Invoke();
    }

    public bool SpendAstralCores(int amount)
    {
        if (amount <= 0 || AstralCores < amount)
        {
            return false;
        }

        GsiSaveSystem.SetInt(AstralCoresKey, AstralCores - amount);
        GsiSaveSystem.Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    // 3. 아크 응시권 (Arc Ticket) 관련 메서드
    public void AddArcTickets(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        GsiSaveSystem.SetInt(ArcTicketsKey, ArcTickets + amount);
        GsiSaveSystem.Save();
        OnEconomyChanged?.Invoke();
    }

    public bool UseArcTicket()
    {
        if (ArcTickets <= 0)
        {
            return false;
        }

        GsiSaveSystem.SetInt(ArcTicketsKey, ArcTickets - 1);
        GsiSaveSystem.Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    // 4. 구버전 및 호환성 대응 별칭 메서드들
    [Obsolete("Use AddStardust instead")]
    public void AddTokens(int amount) => AddStardust(amount);

    [Obsolete("Use SpendStardust instead")]
    public bool SpendTokens(int amount) => SpendStardust(amount);

    [Obsolete("Use AddArcTickets instead")]
    public void AddTickets(int amount) => AddArcTickets(amount);

    [Obsolete("Use UseArcTicket instead")]
    public bool UseTicket() => UseArcTicket();

    // 5. 등급별 보상 지급 메서드 개편
    public int RewardStardustForTier(string tier)
    {
        int stardustReward;
        switch (tier)
        {
            case "S": stardustReward = 50; break;
            case "A": stardustReward = 30; break;
            case "B": stardustReward = 20; break;
            case "C": stardustReward = 10; break;
            case "F":
            default: stardustReward = 0; break;
        }

        if (stardustReward > 0)
        {
            AddStardust(stardustReward);
        }

        // [요청 반영]: S랭크 공식 시험 패스 시 프리미엄 아스트랄 코어 지급 부분은 완전히 제거되었습니다.

        return stardustReward;
    }

    [Obsolete("Use RewardStardustForTier instead")]
    public int RewardTokensForTier(string tier)
    {
        return RewardStardustForTier(tier);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
