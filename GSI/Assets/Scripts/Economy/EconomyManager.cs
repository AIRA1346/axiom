using System;
using UnityEngine;

/// <summary>
/// G.S.I 전용: 토큰·응시권을 PlayerPrefs 로만 관리합니다 (아이템·인벤토리 없음).
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

    public int Tokens => PlayerPrefs.GetInt(TokensKey, 0);
    public int ExamTickets => PlayerPrefs.GetInt(TicketsKey, DefaultTicketCount);

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
        if (PlayerPrefs.HasKey(TokensKey) || PlayerPrefs.HasKey(TicketsKey))
        {
            return;
        }

        int t = PlayerPrefs.GetInt(LegacyTokensKey, 0);
        int k = PlayerPrefs.GetInt(LegacyExamTicketsKey, DefaultTicketCount);
        PlayerPrefs.SetInt(TokensKey, t);
        PlayerPrefs.SetInt(TicketsKey, k);
        PlayerPrefs.Save();
    }

    public void AddTokens(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        PlayerPrefs.SetInt(TokensKey, Tokens + amount);
        PlayerPrefs.Save();
        OnEconomyChanged?.Invoke();
    }

    public bool SpendTokens(int amount)
    {
        if (amount <= 0 || Tokens < amount)
        {
            return false;
        }

        PlayerPrefs.SetInt(TokensKey, Tokens - amount);
        PlayerPrefs.Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    public void AddTickets(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        PlayerPrefs.SetInt(TicketsKey, ExamTickets + amount);
        PlayerPrefs.Save();
        OnEconomyChanged?.Invoke();
    }

    public bool UseTicket()
    {
        if (ExamTickets <= 0)
        {
            return false;
        }

        PlayerPrefs.SetInt(TicketsKey, ExamTickets - 1);
        PlayerPrefs.Save();
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
