using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles result screen button clicks and requests state changes.
/// This component only bridges UI button input to the central game flow.
/// </summary>
public sealed class ResultScreenController : MonoBehaviour
{
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _mainMenuButton;
    [SerializeField] private TextMeshProUGUI _resultTitleText;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private TextMeshProUGUI _rewardText;
    [SerializeField] private GameObject _newRecordIndicator;

    private void Awake()
    {
        if (_retryButton != null)
        {
            _retryButton.onClick.AddListener(OnRetryClicked);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDestroy()
    {
        if (_retryButton != null)
        {
            _retryButton.onClick.RemoveListener(OnRetryClicked);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    /// <summary>
    /// Requests an immediate return to the test standby state.
    /// </summary>
    private void OnRetryClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (GameManager.Instance.CurrentTestType == TestType.Practice)
        {
            GameManager.Instance.SetGameState(GameState.TestStandby);
            return;
        }

        if (EconomyManager.Instance != null && EconomyManager.Instance.UseTicket())
        {
            GameManager.Instance.SetGameState(GameState.TestStandby);
            return;
        }

#if UNITY_EDITOR
        Debug.Log("티켓 부족으로 재도전 불가!");
#endif
        GameManager.Instance.SetGameState(GameState.MainMenu);
    }

    /// <summary>
    /// Requests a return to the main menu state.
    /// </summary>
    private void OnMainMenuClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetGameState(GameState.MainMenu);
    }

    /// <summary>
    /// Updates the result UI only when the official flow enters the result state.
    /// </summary>
    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.ResultScreen)
        {
            UpdateResultText();
        }
    }

    /// <summary>
    /// Updates the official result display using the latest stored score data.
    /// </summary>
    private void UpdateResultText()
    {
        if (_resultText == null || ScoreManager.Instance == null)
        {
            return;
        }

        if (_resultTitleText != null && GameManager.Instance != null)
        {
            switch (GameManager.Instance.CurrentTestType)
            {
                case TestType.Practice:
                    switch (GameManager.Instance.CurrentTestMode)
                    {
                        case TestMode.Reaction:
                            _resultTitleText.text = "REACTION PRACTICE";
                            break;

                        case TestMode.AimPrecision:
                            _resultTitleText.text = "AIM PRACTICE";
                            break;
                    }
                    break;

                case TestType.OfficialExam:
                    switch (GameManager.Instance.CurrentTestMode)
                    {
                        case TestMode.Reaction:
                            _resultTitleText.text = "OFFICIAL REACTION EXAM";
                            break;

                        case TestMode.AimPrecision:
                            _resultTitleText.text = "OFFICIAL AIM EXAM";
                            break;
                    }
                    break;
            }
        }

        if (ScoreManager.Instance.IsFailed)
        {
            _resultText.text = "FAILED\nFALSE START";

            if (_rewardText != null)
            {
                _rewardText.text = "REWARD: 0 기초 골드";
            }
        }
        else
        {
            float reactionTime = ScoreManager.Instance.LastReactionTime;
            string tier = ScoreManager.Instance.GetTier();
            _resultText.text = $"TIME: {reactionTime:F3} SEC\nTIER: {tier}";

            if (_rewardText != null)
            {
                _rewardText.text = $"REWARD: +{ScoreManager.Instance.LastEarnedTokens} 기초 골드";
            }
        }

        if (_newRecordIndicator != null)
        {
            _newRecordIndicator.SetActive(ScoreManager.Instance.IsNewRecord);
        }
    }
}
