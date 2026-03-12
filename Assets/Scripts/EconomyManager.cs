using System;
using UnityEngine;

/// <summary>
/// Manages the local economy state for tokens and official exam tickets.
/// This manager only loads, stores, and updates persistent economy values.
/// </summary>
public sealed class EconomyManager : MonoBehaviour
{
    private const string TokensKey = "GSITokens";
    private const string ExamTicketsKey = "GSIExamTickets";
    private const int DefaultTokens = 0;
    private const int DefaultExamTickets = 3;

    public static EconomyManager Instance { get; private set; }

    public event Action OnEconomyChanged;

    public int Tokens { get; private set; }
    public int ExamTickets { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadEconomyData();
    }

    /// <summary>
    /// Adds tokens and saves the updated balance.
    /// </summary>
    public void AddTokens(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Tokens += amount;
        Debug.Log($"EconomyManager: 토큰 {amount} 지급됨. 현재 총 토큰: {Tokens}");
        SaveEconomyData();
    }

    /// <summary>
    /// Spends tokens when the balance is sufficient.
    /// </summary>
    public bool SpendTokens(int amount)
    {
        if (amount <= 0 || Tokens < amount)
        {
            return false;
        }

        Tokens -= amount;
        SaveEconomyData();
        return true;
    }

    /// <summary>
    /// Adds exam tickets and saves the updated count.
    /// </summary>
    public void AddTickets(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        ExamTickets += amount;
        SaveEconomyData();
    }

    /// <summary>
    /// Uses one exam ticket when available.
    /// </summary>
    public bool UseTicket()
    {
        if (ExamTickets <= 0)
        {
            return false;
        }

        ExamTickets--;
        SaveEconomyData();
        return true;
    }

    /// <summary>
    /// Rewards tokens based on the official tier result and returns the granted amount.
    /// </summary>
    public int RewardTokensForTier(string tier)
    {
        int rewardAmount;

        switch (tier)
        {
            case "S":
                rewardAmount = 50;
                break;

            case "A":
                rewardAmount = 30;
                break;

            case "B":
                rewardAmount = 20;
                break;

            case "C":
                rewardAmount = 10;
                break;

            case "F":
            default:
                rewardAmount = 0;
                break;
        }

        AddTokens(rewardAmount);
        return rewardAmount;
    }

    /// <summary>
    /// Loads the persistent token and ticket values.
    /// </summary>
    private void LoadEconomyData()
    {
        Tokens = PlayerPrefs.GetInt(TokensKey, DefaultTokens);
        ExamTickets = PlayerPrefs.GetInt(ExamTicketsKey, DefaultExamTickets);
    }

    /// <summary>
    /// Saves the current token and ticket values immediately.
    /// </summary>
    private void SaveEconomyData()
    {
        PlayerPrefs.SetInt(TokensKey, Tokens);
        PlayerPrefs.SetInt(ExamTicketsKey, ExamTickets);
        PlayerPrefs.Save();
        OnEconomyChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
