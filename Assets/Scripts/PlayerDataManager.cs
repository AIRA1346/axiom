using UnityEngine;

/// <summary>
/// Manages persistent local player data for the G.S.I project.
/// This manager only loads and saves long-term records through PlayerPrefs.
/// </summary>
public sealed class PlayerDataManager : MonoBehaviour
{
    private const string BestReactionTimeKey = "BestReactionTime";
    private const string BestAimTimeKey = "BestAimTime";
    private const float DefaultBestReactionTime = 99.99f;

    public static PlayerDataManager Instance { get; private set; }

    public float BestReactionTime { get; private set; } = DefaultBestReactionTime;
    public float BestAimTime { get; private set; } = DefaultBestReactionTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadPlayerData();
    }

    /// <summary>
    /// Saves a new best record only when the new time is faster than the current one.
    /// </summary>
    public bool CheckAndSaveBestReactionTime(float newTime)
    {
        if (newTime >= BestReactionTime)
        {
            return false;
        }

        BestReactionTime = newTime;
        PlayerPrefs.SetFloat(BestReactionTimeKey, BestReactionTime);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>
    /// Saves a new best aim record only when the new time is faster than the current one.
    /// </summary>
    public bool CheckAndSaveBestAimTime(float newTime)
    {
        if (newTime >= BestAimTime)
        {
            return false;
        }

        BestAimTime = newTime;
        PlayerPrefs.SetFloat(BestAimTimeKey, BestAimTime);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>
    /// Loads the stored best reaction time or applies the default placeholder value.
    /// </summary>
    private void LoadPlayerData()
    {
        BestReactionTime = PlayerPrefs.GetFloat(BestReactionTimeKey, DefaultBestReactionTime);
        BestAimTime = PlayerPrefs.GetFloat(BestAimTimeKey, DefaultBestReactionTime);
    }
}
