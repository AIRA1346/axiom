using UnityEngine;

/// <summary>
/// Stores the latest temporary score data for the current G.S.I session.
/// This manager only keeps reaction test result state and exposes simple access methods.
/// </summary>
public sealed class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public float LastReactionTime { get; private set; }
    public bool IsFailed { get; private set; }
    public bool IsNewRecord { get; private set; }
    public int LastEarnedTokens { get; private set; }

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

    /// <summary>
    /// Saves the latest successful test time and clears any failure flag.
    /// </summary>
    public void SaveTestTime(float time)
    {
        LastReactionTime = time;
        IsFailed = false;

        if (PlayerDataManager.Instance != null && GameManager.Instance != null)
        {
            if (GameManager.Instance.CurrentTestType == TestType.OfficialExam)
            {
                switch (GameManager.Instance.CurrentTestMode)
                {
                    case TestMode.Reaction:
                        IsNewRecord = PlayerDataManager.Instance.CheckAndSaveBestReactionTime(time);
                        break;

                    case TestMode.AimPrecision:
                        IsNewRecord = PlayerDataManager.Instance.CheckAndSaveBestAimTime(time);
                        break;
                }
            }
            else
            {
                IsNewRecord = false;
            }
        }
        else
        {
            IsNewRecord = false;
        }

        if (EconomyManager.Instance != null)
        {
            int baseReward = EconomyManager.Instance.RewardTokensForTier(GetTier());

            if (GameManager.Instance != null && GameManager.Instance.CurrentTestType == TestType.OfficialExam)
            {
                EconomyManager.Instance.AddTokens(baseReward);
                LastEarnedTokens = baseReward * 2;
            }
            else
            {
                LastEarnedTokens = baseReward;
            }
        }
        else
        {
            LastEarnedTokens = 0;
        }
    }

    /// <summary>
    /// Marks the latest result as a failed attempt.
    /// </summary>
    public void MarkFailed()
    {
        LastReactionTime = 0f;
        IsFailed = true;
        IsNewRecord = false;
        LastEarnedTokens = 0;
    }

    /// <summary>
    /// Clears the stored result state before a new test begins.
    /// </summary>
    public void ResetScore()
    {
        LastReactionTime = 0f;
        IsFailed = false;
        IsNewRecord = false;
        LastEarnedTokens = 0;
    }

    /// <summary>
    /// Returns the official G.S.I tier based on the latest successful reaction time.
    /// </summary>
    public string GetTier()
    {
        if (IsFailed || GameManager.Instance == null)
        {
            return "F";
        }

        switch (GameManager.Instance.CurrentTestMode)
        {
            case TestMode.Reaction:
                if (LastReactionTime < 0.15f)
                {
                    return "S";
                }

                if (LastReactionTime < 0.2f)
                {
                    return "A";
                }

                if (LastReactionTime < 0.25f)
                {
                    return "B";
                }

                if (LastReactionTime < 0.3f)
                {
                    return "C";
                }

                return "F";

            case TestMode.AimPrecision:
                if (LastReactionTime < 1.5f)
                {
                    return "S";
                }

                if (LastReactionTime < 2.0f)
                {
                    return "A";
                }

                if (LastReactionTime < 2.5f)
                {
                    return "B";
                }

                if (LastReactionTime < 3.0f)
                {
                    return "C";
                }

                return "F";

            default:
                return "F";
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
