using System;
using UnityEngine;

/// <summary>
/// 토큰·응시권을 인벤토리 아이템으로 통일 관리합니다.
/// 기초 골드(ITM-CUR-BAS-CPR-000096-01), 기초 티켓(ITM-CUR-BAS-CPR-000086-01)을 사용합니다.
/// </summary>
public sealed class EconomyManager : MonoBehaviour
{
    /// <summary>기초 골드 (기존 토큰 대체)</summary>
    public const string GoldItemId = "ITM-CUR-BAS-CPR-000096-01";

    /// <summary>기초 티켓 (기존 응시권 대체)</summary>
    public const string TicketItemId = "ITM-CUR-BAS-CPR-000086-01";

    private const string MigratedKey = "GSIEconomyMigratedToItems";
    private const string LegacyTokensKey = "GSITokens";
    private const string LegacyExamTicketsKey = "GSIExamTickets";
    private const int DefaultTicketCount = 3;

    public static EconomyManager Instance { get; private set; }

    public event Action OnEconomyChanged;

    public int Tokens => GetGoldCount();
    public int ExamTickets => GetTicketCount();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        MigrateLegacyEconomyIfNeeded();
    }

    private int GetGoldCount()
    {
        return InventoryManager.Instance != null ? InventoryManager.Instance.GetItemCount(GoldItemId) : 0;
    }

    private int GetTicketCount()
    {
        return InventoryManager.Instance != null ? InventoryManager.Instance.GetItemCount(TicketItemId) : 0;
    }

    /// <summary>
    /// 기존 PlayerPrefs 기반 토큰/응시권을 인벤토리 아이템으로 마이그레이션합니다.
    /// </summary>
    private void MigrateLegacyEconomyIfNeeded()
    {
        if (PlayerPrefs.GetInt(MigratedKey, 0) != 0)
        {
            return;
        }

        if (InventoryManager.Instance == null)
        {
            return;
        }

        int oldTokens = PlayerPrefs.GetInt(LegacyTokensKey, 0);
        int oldTickets = PlayerPrefs.GetInt(LegacyExamTicketsKey, DefaultTicketCount);

        if (oldTokens > 0)
        {
            InventoryManager.Instance.AddItem(GoldItemId, oldTokens);
        }

        if (oldTickets > 0)
        {
            InventoryManager.Instance.AddItem(TicketItemId, oldTickets);
        }

        PlayerPrefs.DeleteKey(LegacyTokensKey);
        PlayerPrefs.DeleteKey(LegacyExamTicketsKey);
        PlayerPrefs.SetInt(MigratedKey, 1);
        PlayerPrefs.Save();

        OnEconomyChanged?.Invoke();
    }

    /// <summary>
    /// 기초 골드를 지급합니다.
    /// </summary>
    public void AddTokens(int amount)
    {
        if (amount <= 0 || InventoryManager.Instance == null)
        {
            return;
        }

        InventoryManager.Instance.AddItem(GoldItemId, amount);
        OnEconomyChanged?.Invoke();
    }

    /// <summary>
    /// 기초 골드를 차감합니다. 잔액이 부족하면 false.
    /// </summary>
    public bool SpendTokens(int amount)
    {
        if (amount <= 0 || InventoryManager.Instance == null)
        {
            return false;
        }

        if (GetGoldCount() < amount)
        {
            return false;
        }

        bool ok = InventoryManager.Instance.RemoveItem(GoldItemId, amount);
        if (ok)
        {
            OnEconomyChanged?.Invoke();
        }
        return ok;
    }

    /// <summary>
    /// 기초 티켓을 지급합니다.
    /// </summary>
    public void AddTickets(int amount)
    {
        if (amount <= 0 || InventoryManager.Instance == null)
        {
            return;
        }

        InventoryManager.Instance.AddItem(TicketItemId, amount);
        OnEconomyChanged?.Invoke();
    }

    /// <summary>
    /// 기초 티켓 1개를 사용합니다.
    /// </summary>
    public bool UseTicket()
    {
        if (InventoryManager.Instance == null)
        {
            return false;
        }

        if (GetTicketCount() <= 0)
        {
            return false;
        }

        bool ok = InventoryManager.Instance.RemoveItem(TicketItemId, 1);
        if (ok)
        {
            OnEconomyChanged?.Invoke();
        }
        return ok;
    }

    /// <summary>
    /// 공식 시험 등급에 따른 기초 골드 보상 지급.
    /// </summary>
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
