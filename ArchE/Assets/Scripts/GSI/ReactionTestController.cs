using System.Collections;
using UnityEngine;

/// <summary>
/// Measures a single reaction-time attempt for the G.S.I test flow.
/// This controller only handles target timing, input judgment, and test completion.
/// </summary>
public sealed class ReactionTestController : MonoBehaviour
{
    [SerializeField] private GameObject _targetObject;

    private Coroutine _reactionTestRoutine;
    private bool _isGameManagerSubscribed;
    private bool _isInputManagerSubscribed;
    private bool _isWaitingForTarget;
    private bool _isTargetVisible;
    private bool _ignoreInput;
    private float _spawnTime;

    private void Awake()
    {
        SetTargetActive(false);
    }

    /// <summary>
    /// 비활성 패널에 붙어 있으면 Start/이벤트 순서 때문에 TestInProgress 알림을 놓칩니다.
    /// 활성화될 때마다 구독하고 현재 상태를 맞춥니다.
    /// </summary>
    private void OnEnable()
    {
        SubscribeToManagers();
        if (GameManager.Instance != null)
        {
            HandleGameStateChanged(GameManager.Instance.CurrentState);
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromManagers();
        StopReactionTest();
    }

    private void OnDestroy()
    {
        UnsubscribeFromManagers();
        StopReactionTest();
    }

    /// <summary>
    /// Subscribes to the central state flow and unified input stream.
    /// </summary>
    private void SubscribeToManagers()
    {
        if (!_isGameManagerSubscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            _isGameManagerSubscribed = true;
        }

        if (!_isInputManagerSubscribed && InputManager.Instance != null)
        {
            InputManager.Instance.OnInputDown += HandleInputDown;
            _isInputManagerSubscribed = true;
        }
    }

    /// <summary>
    /// Cleans up event subscriptions when this component is disabled or destroyed.
    /// </summary>
    private void UnsubscribeFromManagers()
    {
        if (_isGameManagerSubscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            _isGameManagerSubscribed = false;
        }

        if (_isInputManagerSubscribed && InputManager.Instance != null)
        {
            InputManager.Instance.OnInputDown -= HandleInputDown;
            _isInputManagerSubscribed = false;
        }
    }

    /// <summary>
    /// Starts or clears the reaction test depending on the official game state.
    /// </summary>
    private void HandleGameStateChanged(GameState newState)
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentTestMode != TestMode.Reaction)
        {
            StopReactionTest();
            return;
        }

        if (newState == GameState.TestInProgress)
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScore();
            }

            StartReactionTest();
            return;
        }

        StopReactionTest();
    }

    /// <summary>
    /// Runs the randomized wait and then reveals the target for the player response.
    /// </summary>
    private IEnumerator ReactionTestRoutine()
    {
        _isWaitingForTarget = true;
        _isTargetVisible = false;
        SetTargetActive(false);

        float minDelay = 2.0f;
        float maxDelay = 5.0f;
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.CurrentTestType == TestType.Practice)
            {
                PracticeDifficulty.GetReactionDelayRange(
                    GameManager.Instance.GetPracticeGrade(TestMode.Reaction),
                    out minDelay,
                    out maxDelay);
            }
            else if (GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam)
            {
                PracticeDifficulty.GetReactionDelayRange(
                    GameManager.Instance.UnifiedExamGrade,
                    out minDelay,
                    out maxDelay);
            }
        }

        float randomDelay = Random.Range(minDelay, maxDelay);
        yield return new WaitForSeconds(randomDelay);

        _isWaitingForTarget = false;
        _isTargetVisible = true;
        _spawnTime = Time.time;
        SetTargetActive(true);
    }

    /// <summary>
    /// Ignores the input bleed that occurs immediately after the standby-to-start transition.
    /// </summary>
    private IEnumerator InputCooldownRoutine()
    {
        _ignoreInput = true;
        yield return new WaitForSeconds(0.1f);
        _ignoreInput = false;
    }

    /// <summary>
    /// Judges a press during the active reaction test.
    /// </summary>
    private void HandleInputDown(Vector2 screenPosition)
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentTestMode != TestMode.Reaction)
        {
            return;
        }

        if (GameManager.Instance.CurrentState != GameState.TestInProgress)
        {
            return;
        }

        if (_ignoreInput)
        {
            return;
        }

        if (_isWaitingForTarget)
        {
            if (GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam)
            {
#if UNITY_EDITOR
                Debug.Log("부정 출발! 통합 시험 해당 과목 0점 처리.");
#endif
                StopReactionTest();
                GameManager.Instance.CompleteUnifiedExamSegment(new UnifiedExamSegmentPayload
                {
                    Mode = TestMode.Reaction,
                    HardFailed = true,
                    Primary = 0f
                });
                return;
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.MarkFailed();
            }

#if UNITY_EDITOR
            Debug.Log("부정 출발! 테스트 실패.");
#endif
            StopReactionTest();
            GameManager.Instance.SetGameState(GameState.ResultScreen);
            return;
        }

        if (_isTargetVisible)
        {
            float reactionTime = Time.time - _spawnTime;
            if (GameManager.Instance.CurrentTestType == TestType.UnifiedOfficialExam)
            {
#if UNITY_EDITOR
                Debug.Log($"반응 속도: {reactionTime:F3}초");
#endif
                StopReactionTest();
                GameManager.Instance.CompleteUnifiedExamSegment(new UnifiedExamSegmentPayload
                {
                    Mode = TestMode.Reaction,
                    HardFailed = false,
                    Primary = reactionTime
                });
                return;
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.SaveTestTime(reactionTime);
            }

#if UNITY_EDITOR
            Debug.Log($"반응 속도: {reactionTime:F3}초");
#endif
            StopReactionTest();
            GameManager.Instance.SetGameState(GameState.ResultScreen);
        }
    }

    /// <summary>
    /// Starts a new reaction measurement attempt.
    /// </summary>
    private void StartReactionTest()
    {
        StopReactionTest();
        SetTargetActive(false);
        StartCoroutine(InputCooldownRoutine());
        _reactionTestRoutine = StartCoroutine(ReactionTestRoutine());
    }

    /// <summary>
    /// Stops the active test and clears all temporary target state.
    /// </summary>
    private void StopReactionTest()
    {
        if (_reactionTestRoutine != null)
        {
            StopCoroutine(_reactionTestRoutine);
            _reactionTestRoutine = null;
        }

        _isWaitingForTarget = false;
        _isTargetVisible = false;
        _ignoreInput = false;
        SetTargetActive(false);
    }

    /// <summary>
    /// Applies the visible state to the reaction target when assigned.
    /// </summary>
    private void SetTargetActive(bool isActive)
    {
        if (_targetObject == null)
        {
            return;
        }

        _targetObject.SetActive(isActive);
    }
}
