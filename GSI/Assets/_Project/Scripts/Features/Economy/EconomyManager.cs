using System;
using UnityEngine;
using ArchE.Game;

/// <summary>
/// G.S.I 전용: 토큰·시험권을 GsiSaveSystem으로 관리합니다 (아이템·인벤토리 없음).
/// </summary>
public sealed class EconomyManager : MonoBehaviour
{
    private const string LegacyTokensKey = "GSITokens";
    private const string LegacyExamTicketsKey = "GSIExamTickets";
    private const string TokensKey = "GSI_Tokens_v2";
    private const string TicketsKey = "GSI_Tickets_v2";
    private const int DefaultTicketCount = 3;

    public static EconomyManager Instance { get; private set; }

    public event Action OnEconomyChanged;

    public int Tokens => GsiSaveSystem.GetInt(TokensKey, 0);
    public int ExamTickets => GsiSaveSystem.GetInt(TicketsKey, DefaultTicketCount);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        MigrateLegacyOnce();
    }

    private void MigrateLegacyOnce()
    {
        if (GsiSaveSystem.HasKey(TokensKey) || GsiSaveSystem.HasKey(TicketsKey))
        {
            return;
        }

        // GsiSaveSystem 자체 마이그레이션이 작동할 것이므로, 필요에 따라 수동 마이그레이션도 안전장치로 남김
        int t = GsiSaveSystem.GetInt(LegacyTokensKey, 0);
        int k = GsiSaveSystem.GetInt(LegacyExamTicketsKey, DefaultTicketCount);
        GsiSaveSystem.SetInt(TokensKey, t);
        GsiSaveSystem.SetInt(TicketsKey, k);
        GsiSaveSystem.Save();
    }

    public void AddTokens(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        GsiSaveSystem.SetInt(TokensKey, Tokens + amount);
        GsiSaveSystem.Save();
        OnEconomyChanged?.Invoke();
    }

    public bool SpendTokens(int amount)
    {
        if (amount <= 0 || Tokens < amount)
        {
            return false;
        }

        GsiSaveSystem.SetInt(TokensKey, Tokens - amount);
        GsiSaveSystem.Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    public void AddTickets(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        GsiSaveSystem.SetInt(TicketsKey, ExamTickets + amount);
        GsiSaveSystem.Save();
        OnEconomyChanged?.Invoke();
    }

    public bool UseTicket()
    {
        if (ExamTickets <= 0)
        {
            return false;
        }

        GsiSaveSystem.SetInt(TicketsKey, ExamTickets - 1);
        GsiSaveSystem.Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    public int RewardTokensForTier(string tier)
    {
        int rewardAmount;
        switch (tier)
        {
            case "S": rewardAmount = 50; break;
            case "A": rewardAmount = 30; break;
            case "B": rewardAmount = 20; break;
            case "C": rewardAmount = 10; break;
            case "F":
            default: rewardAmount = 0; break;
        }

        if (rewardAmount > 0)
        {
            AddTokens(rewardAmount);
        }

        return rewardAmount;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
