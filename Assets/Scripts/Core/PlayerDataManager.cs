using System.Text;
using UnityEngine;

/// <summary>
/// Manages persistent local player data for the G.S.I project.
/// This manager only loads and saves long-term records through PlayerPrefs.
/// </summary>
public sealed class PlayerDataManager : MonoBehaviour
{
    private const string BestReactionTimeKey = "BestReactionTime";
    private const string BestAimTimeKey = "BestAimTime";
    private const string BestMemorySpanKey = "BestMemorySpan";
    private const string BestRhythmAccuracyKey = "BestRhythmAccuracy";
    private const string BestRhythmMeanErrorKey = "BestRhythmMeanError";
    private const string BestMotAccuracyKey = "BestMotAccuracy";
    private const string BestBulletHellTimeKey = "BestBulletHellTime";
    private const string BestUnifiedExamTotalKey = "BestUnifiedExamTotal";
    private const string UnifiedExamHistoryKey = "UnifiedExamHistory";
    private const int UnifiedExamHistoryMaxChars = 8000;
    private const float DefaultBestReactionTime = 99.99f;
    private const float NoMemoryRecord = -1f;

    public static PlayerDataManager Instance { get; private set; }

    public float BestReactionTime { get; private set; } = DefaultBestReactionTime;
    public float BestAimTime { get; private set; } = DefaultBestReactionTime;
    /// <summary>공식 시험 기억 과목: 최고 평균 순서 길이(없으면 음수).</summary>
    public float BestMemorySpan { get; private set; } = NoMemoryRecord;

    /// <summary>공식 시험 리듬: 최고 정확도(%). 없으면 -1.</summary>
    public float BestRhythmAccuracy { get; private set; } = -1f;

    /// <summary>동일 정확도대에서 비교하는 평균 오차(ms).</summary>
    public float BestRhythmMeanError { get; private set; } = 999f;

    /// <summary>공식 시험 MOT: 최고 종합 정확도(%). 없으면 -1.</summary>
    public float BestMotAccuracy { get; private set; } = -1f;

    /// <summary>공식 시험 Bullet Hell: 최고 생존 시간 합(초). 없으면 -1.</summary>
    public float BestBulletHellTime { get; private set; } = -1f;

    /// <summary>통합 공식 시험 최고 총점(6과목 합, 최대 600). 없으면 -1.</summary>
    public float BestUnifiedExamTotal { get; private set; } = -1f;

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
    /// 기억 과목: 더 긴 평균 순서 길이가 나오면 갱신합니다.
    /// </summary>
    public bool CheckAndSaveBestMemorySpan(float newSpan)
    {
        if (newSpan <= BestMemorySpan)
        {
            return false;
        }

        BestMemorySpan = newSpan;
        PlayerPrefs.SetFloat(BestMemorySpanKey, BestMemorySpan);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>
    /// 정확도가 더 높거나, 거의 같을 때 평균 오차(ms)가 더 낮으면 갱신합니다.
    /// </summary>
    public bool CheckAndSaveBestRhythmScore(float accuracyPercent, float meanAbsErrorMs)
    {
        if (BestRhythmAccuracy < 0f)
        {
            BestRhythmAccuracy = accuracyPercent;
            BestRhythmMeanError = meanAbsErrorMs;
            PersistRhythm();
            return true;
        }

        if (accuracyPercent > BestRhythmAccuracy + 0.05f)
        {
            BestRhythmAccuracy = accuracyPercent;
            BestRhythmMeanError = meanAbsErrorMs;
            PersistRhythm();
            return true;
        }

        if (Mathf.Abs(accuracyPercent - BestRhythmAccuracy) <= 0.05f && meanAbsErrorMs < BestRhythmMeanError)
        {
            BestRhythmMeanError = meanAbsErrorMs;
            PersistRhythm();
            return true;
        }

        return false;
    }

    private void PersistRhythm()
    {
        PlayerPrefs.SetFloat(BestRhythmAccuracyKey, BestRhythmAccuracy);
        PlayerPrefs.SetFloat(BestRhythmMeanErrorKey, BestRhythmMeanError);
        PlayerPrefs.Save();
    }

    /// <summary>MOT: 더 높은 정확도(%), 동률이면 유지.</summary>
    public bool CheckAndSaveBestMotAccuracy(float accuracyPercent)
    {
        if (BestMotAccuracy < 0f)
        {
            BestMotAccuracy = accuracyPercent;
            PlayerPrefs.SetFloat(BestMotAccuracyKey, BestMotAccuracy);
            PlayerPrefs.Save();
            return true;
        }

        if (accuracyPercent > BestMotAccuracy + 0.2f)
        {
            BestMotAccuracy = accuracyPercent;
            PlayerPrefs.SetFloat(BestMotAccuracyKey, BestMotAccuracy);
            PlayerPrefs.Save();
            return true;
        }

        return false;
    }

    /// <summary>Bullet Hell: 더 긴 생존 시간 합(초)이면 갱신합니다.</summary>
    public bool CheckAndSaveBestBulletHellTime(float survivalSecondsSum)
    {
        if (BestBulletHellTime < 0f)
        {
            BestBulletHellTime = survivalSecondsSum;
            PlayerPrefs.SetFloat(BestBulletHellTimeKey, BestBulletHellTime);
            PlayerPrefs.Save();
            return true;
        }

        if (survivalSecondsSum > BestBulletHellTime + 0.5f)
        {
            BestBulletHellTime = survivalSecondsSum;
            PlayerPrefs.SetFloat(BestBulletHellTimeKey, BestBulletHellTime);
            PlayerPrefs.Save();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Loads the stored best reaction time or applies the default placeholder value.
    /// </summary>
    private void LoadPlayerData()
    {
        BestReactionTime = PlayerPrefs.GetFloat(BestReactionTimeKey, DefaultBestReactionTime);
        BestAimTime = PlayerPrefs.GetFloat(BestAimTimeKey, DefaultBestReactionTime);
        BestMemorySpan = PlayerPrefs.GetFloat(BestMemorySpanKey, NoMemoryRecord);
        BestRhythmAccuracy = PlayerPrefs.GetFloat(BestRhythmAccuracyKey, -1f);
        BestRhythmMeanError = PlayerPrefs.GetFloat(BestRhythmMeanErrorKey, 999f);
        BestMotAccuracy = PlayerPrefs.GetFloat(BestMotAccuracyKey, -1f);
        BestBulletHellTime = PlayerPrefs.GetFloat(BestBulletHellTimeKey, -1f);
        BestUnifiedExamTotal = PlayerPrefs.GetFloat(BestUnifiedExamTotalKey, -1f);
    }

    /// <summary>통합 시험 한 과목 원시 결과로 종목별 최고 기록 갱신(부정 출발 등 HardFailed는 제외).</summary>
    public bool ApplyBestFromUnifiedPayload(UnifiedExamSegmentPayload p)
    {
        if (p.HardFailed)
        {
            return false;
        }

        switch (p.Mode)
        {
            case TestMode.Reaction:
                return CheckAndSaveBestReactionTime(p.Primary);

            case TestMode.AimPrecision:
                return CheckAndSaveBestAimTime(p.Primary);

            case TestMode.MemorySequence:
                return CheckAndSaveBestMemorySpan(p.Primary);

            case TestMode.RhythmTiming:
                return CheckAndSaveBestRhythmScore(p.Secondary, p.Primary);

            case TestMode.MultipleObjectTracking:
                return CheckAndSaveBestMotAccuracy(p.Primary);

            case TestMode.BulletHell:
                return CheckAndSaveBestBulletHellTime(p.Primary);

            default:
                return false;
        }
    }

    /// <summary>통합 시험 총점(600 만점) 최고 기록 갱신.</summary>
    public bool CheckAndSaveBestUnifiedExamTotal(float totalScore)
    {
        if (BestUnifiedExamTotal < 0f || totalScore > BestUnifiedExamTotal + 0.01f)
        {
            BestUnifiedExamTotal = totalScore;
            PlayerPrefs.SetFloat(BestUnifiedExamTotalKey, BestUnifiedExamTotal);
            PlayerPrefs.Save();
            return true;
        }

        return false;
    }

    /// <summary>통합 시험 응시 이력 한 줄 추가(최근 기록이 위에 오도록 앞에 붙임).</summary>
    public void AppendUnifiedExamHistoryLine(string line)
    {
        string prev = PlayerPrefs.GetString(UnifiedExamHistoryKey, string.Empty);
        var sb = new StringBuilder();
        sb.AppendLine(line);
        sb.Append(prev);
        string merged = sb.ToString();
        if (merged.Length > UnifiedExamHistoryMaxChars)
        {
            merged = merged.Substring(0, UnifiedExamHistoryMaxChars);
        }

        PlayerPrefs.SetString(UnifiedExamHistoryKey, merged);
        PlayerPrefs.Save();
    }
}
