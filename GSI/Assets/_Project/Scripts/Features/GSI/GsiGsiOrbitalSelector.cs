using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 미니게임 별 노드 또는 블랙홀 노드 주변에 1~9단계 난이도를 원형 궤도로 전개해주는 Premium 궤도 선택기.
/// </summary>
public sealed class GsiGsiOrbitalSelector : MonoBehaviour
{
    public float OrbitRadius = 145f;
    public float SphereSize = 26f;
    public Color ThemeColor = Color.cyan;

    public Action<int> OnGradeSelected;
    public Action OnClosed;

    public bool IsReactionMode = false;
    private RectTransform _reactionPill;
    private CanvasGroup _reactionPillCg;

    private readonly List<RectTransform> _spheres = new List<RectTransform>();
    private readonly List<CanvasGroup> _sphereCanvasGroups = new List<CanvasGroup>();
    private GameObject _orbitRingGo;
    private Image _orbitRingImg;
    private TextMeshProUGUI _feedbackText;
    private CanvasGroup _feedbackCg;

    private Coroutine _animationRoutine;
    private bool _isOpen = false;

    private void Start()
    {
        if (IsReactionMode)
        {
            // Create a single beautiful pill button
            var pillGo = new GameObject("ReactionGuidePill", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            _reactionPill = pillGo.GetComponent<RectTransform>();
            _reactionPill.SetParent(transform, false);
            _reactionPill.anchorMin = new Vector2(0.5f, 0.5f);
            _reactionPill.anchorMax = new Vector2(0.5f, 0.5f);
            _reactionPill.pivot = new Vector2(0.5f, 0.5f);
            _reactionPill.anchoredPosition = Vector2.zero; // Starts at zero for animation
            _reactionPill.sizeDelta = new Vector2(260f, 48f);

            var pillImg = pillGo.GetComponent<Image>();
            pillImg.color = Color.clear;
            pillImg.raycastTarget = true;

            // Create Text inside the pill
            var guideTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var guideTextRt = guideTextGo.GetComponent<RectTransform>();
            guideTextRt.SetParent(_reactionPill, false);
            guideTextRt.anchorMin = Vector2.zero;
            guideTextRt.anchorMax = Vector2.one;
            guideTextRt.offsetMin = new Vector2(10f, 0f);
            guideTextRt.offsetMax = new Vector2(-10f, 0f);
            
            var textTmp = guideTextGo.GetComponent<TextMeshProUGUI>();
            textTmp.alignment = TextAlignmentOptions.Center;
            textTmp.fontSize = 15f;
            textTmp.fontStyle = FontStyles.Bold;
            textTmp.color = ThemeColor;

            bool isKo = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale != null &&
                        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("ko", System.StringComparison.OrdinalIgnoreCase);
            textTmp.text = isKo ? "기록별 등급 차등 지급" : "Grade based on Record";
            textTmp.raycastTarget = false;

            if (TmpFontCache.LiberationSansSdf != null)
            {
                textTmp.font = TmpFontCache.LiberationSansSdf;
            }

            _reactionPillCg = pillGo.GetComponent<CanvasGroup>();
            _reactionPillCg.alpha = 0f;

            // Setup EventTrigger for hovering and clicking
            var trigger = pillGo.AddComponent<EventTrigger>();

            // PointerEnter
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((data) => {
                _reactionPill.localScale = new Vector3(1.05f, 1.05f, 1.05f);
                textTmp.color = Color.white;
            });
            trigger.triggers.Add(enterEntry);

            // PointerExit
            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((data) => {
                _reactionPill.localScale = Vector3.one;
                textTmp.color = ThemeColor;
            });
            trigger.triggers.Add(exitEntry);

            // PointerClick
            var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clickEntry.callback.AddListener((data) => {
                OnGradeSelected?.Invoke(5); // Invoke with grade 5 (fixed)
            });
            trigger.triggers.Add(clickEntry);

            Open();
            return;
        }

        // 1. 얇고 세련된 반투명 궤도 링(Glow Ring) 가이드 생성 (네모 배경 제거를 위해 생성하지 않음)
        _orbitRingGo = null;
        _orbitRingImg = null;

        // 2. 피드백 가이드 텍스트 (호버한 단계 정보 브랜딩 툴팁)
        var textGo = new GameObject("OrbitalFeedbackText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.SetParent(transform, false);
        textRt.anchorMin = new Vector2(0.5f, 0.5f);
        textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.anchoredPosition = new Vector2(0f, -OrbitRadius - 38f); // 궤도 링 하단 바깥쪽 밀착
        textRt.sizeDelta = new Vector2(350f, 32f);

        _feedbackText = textGo.GetComponent<TextMeshProUGUI>();
        _feedbackText.alignment = TextAlignmentOptions.Center;
        _feedbackText.fontSize = 15f;
        _feedbackText.characterSpacing = 0.8f;
        _feedbackText.fontStyle = FontStyles.Bold;
        _feedbackText.color = new Color(ThemeColor.r * 1.2f, ThemeColor.g * 1.2f, ThemeColor.b * 1.2f, 0.95f);
        _feedbackText.text = "";

        _feedbackCg = textGo.GetComponent<CanvasGroup>();
        _feedbackCg.alpha = 0f;

        // 3. 9개 난이도 구체 버튼들 생성
        for (int grade = 1; grade <= 9; grade++)
        {
            int currentGrade = grade;
            var sphereGo = new GameObject($"GradeSphere_{grade}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var sphereRt = sphereGo.GetComponent<RectTransform>();
            sphereRt.SetParent(transform, false);
            sphereRt.anchorMin = new Vector2(0.5f, 0.5f);
            sphereRt.anchorMax = new Vector2(0.5f, 0.5f);
            sphereRt.pivot = new Vector2(0.5f, 0.5f);
            sphereRt.anchoredPosition = Vector2.zero; // 애니메이션을 위해 처음엔 (0,0)
            sphereRt.sizeDelta = new Vector2(SphereSize, SphereSize);

            // 구체 배경 네온 링 (투명도를 높여 궤도와 아련하게만 겹치도록 설정)
            var sphereImg = sphereGo.GetComponent<Image>();
            sphereImg.sprite = null;
            sphereImg.color = new Color(ThemeColor.r, ThemeColor.g, ThemeColor.b, 0.08f); // 🌌 투박한 회색/유색 배경 제거, 얇은 아우라만 유지
            sphereImg.raycastTarget = true;

            // 구체 내부 미세 코어 (유지하되, 불투명한 꽉 찬 흰색 면 대신 은은한 은하수 반투명 라이트 설정)
            var coreGo = new GameObject("Core", typeof(RectTransform), typeof(Image));
            var coreRt = coreGo.GetComponent<RectTransform>();
            coreRt.SetParent(sphereRt, false);
            coreRt.anchorMin = new Vector2(0.5f, 0.5f);
            coreRt.anchorMax = new Vector2(0.5f, 0.5f);
            coreRt.pivot = new Vector2(0.5f, 0.5f);
            coreRt.anchoredPosition = Vector2.zero;
            coreRt.sizeDelta = new Vector2(SphereSize * 0.7f, SphereSize * 0.7f); // 텍스트를 담을 수 있게 약간 확장
            var coreImg = coreGo.GetComponent<Image>();
            coreImg.sprite = null;
            coreImg.color = new Color(1f, 1f, 1f, 0.08f); // 🌌 투박한 백색 면을 아련한 반투명 네온 빛으로 교체
            coreImg.raycastTarget = false;

            // 숫자 텍스트 표시
            var numGo = new GameObject("NumText", typeof(RectTransform), typeof(TextMeshProUGUI));
            var numRt = numGo.GetComponent<RectTransform>();
            numRt.SetParent(sphereRt, false);
            numRt.anchorMin = new Vector2(0.5f, 0.5f);
            numRt.anchorMax = new Vector2(0.5f, 0.5f);
            numRt.pivot = new Vector2(0.5f, 0.5f);
            numRt.anchoredPosition = Vector2.zero;
            numRt.sizeDelta = new Vector2(40f, 40f); // 🌌 폰트 증가에 대응하여 텍스트 상자 크기 확장
            var numTmp = numGo.GetComponent<TextMeshProUGUI>();
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.fontSize = 18f; // 🌌 숫자의 폰트 크기를 11f -> 18f로 대폭 증폭
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.color = ThemeColor; // 🌌 평소에는 아름다운 네온 테마 컬러로 숫자 렌더링
            numTmp.text = grade.ToString();
            numTmp.raycastTarget = false;

            // CanvasGroup 설정
            var cg = sphereGo.GetComponent<CanvasGroup>();
            cg.alpha = 0f;

            // 이벤트 트리거 부착 (IPointerEnter, IPointerExit, IPointerClick 등)
            var trigger = sphereGo.AddComponent<EventTrigger>();

            // PointerEnter
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((data) => {
                sphereRt.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                sphereImg.color = new Color(ThemeColor.r, ThemeColor.g, ThemeColor.b, 0.18f); // 호버 시 얇은 빛 강화
                coreImg.color = new Color(1f, 1f, 1f, 0.15f);
                numTmp.color = Color.white; // 🌌 호버 시 백색 네온 라이트로 피드백
                
                string gradeDesc = currentGrade == 1 ? "Grade 1 (Hardest)" : currentGrade == 9 ? "Grade 9 (Entry)" : $"Grade {currentGrade}";
                _feedbackText.text = gradeDesc;
                _feedbackCg.alpha = 1f;
            });
            trigger.triggers.Add(enterEntry);

            // PointerExit
            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((data) => {
                sphereRt.localScale = Vector3.one;
                sphereImg.color = new Color(ThemeColor.r, ThemeColor.g, ThemeColor.b, 0.08f); // 원래 아우라 복구
                coreImg.color = new Color(1f, 1f, 1f, 0.08f);
                numTmp.color = ThemeColor; // 🌌 퇴장 시 은은한 기본 네온 컬러 복구
                _feedbackCg.alpha = 0f;
            });
            trigger.triggers.Add(exitEntry);

            // PointerClick
            var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clickEntry.callback.AddListener((data) => {
                OnGradeSelected?.Invoke(currentGrade);
            });
            trigger.triggers.Add(clickEntry);

            _spheres.Add(sphereRt);
            _sphereCanvasGroups.Add(cg);
        }

        // 4. 전개 애니메이션 즉시 수행
        Open();
    }

    private void OnDisable()
    {
        // 씬 전환 등으로 로비 패널이 비활성화되는 즉시 자가 소멸하여 재진입 시 난이도 궤도 잔상이 남는 버그를 차단합니다.
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (OnClosed != null)
        {
            OnClosed.Invoke();
            OnClosed = null;
        }
    }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        if (_animationRoutine != null) StopCoroutine(_animationRoutine);
        _animationRoutine = StartCoroutine(CoAnimateExpand());
    }

    public void Close(Action onDone = null)
    {
        if (!_isOpen) return;
        _isOpen = false;
        if (_animationRoutine != null) StopCoroutine(_animationRoutine);
        _animationRoutine = StartCoroutine(CoAnimateCollapse(() => {
            onDone?.Invoke();
            if (OnClosed != null)
            {
                OnClosed.Invoke();
                OnClosed = null;
            }
            Destroy(gameObject);
        }));
    }

    private IEnumerator CoAnimateExpand()
    {
        float duration = 0.28f;
        float elapsed = 0f;

        if (IsReactionMode)
        {
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float tElastic = SinProgress(t);

                float currentY = 70f * tElastic;
                _reactionPill.anchoredPosition = new Vector2(0f, currentY);
                _reactionPillCg.alpha = Mathf.Lerp(0f, 1f, t * 1.5f);

                yield return null;
            }

            _reactionPill.anchoredPosition = new Vector2(0f, 70f);
            _reactionPillCg.alpha = 1f;
            yield break;
        }

        // 원형 링 페이드인
        if (_orbitRingImg != null)
        {
            _orbitRingImg.color = new Color(ThemeColor.r, ThemeColor.g, ThemeColor.b, 0f);
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // 탄성 있는 복귀 곡선 (Elastic Out)
            float tElastic = SinProgress(t);

            if (_orbitRingImg != null)
            {
                _orbitRingImg.color = new Color(ThemeColor.r, ThemeColor.g, ThemeColor.b, Mathf.Lerp(0f, 0.08f, t));
            }

            // 9개 구체 배치 진행
            for (int i = 0; i < _spheres.Count; i++)
            {
                // 각도 계산 (360도 / 9개 = 40도 간격, 12시 방향부터 순서대로 시계방향 배치)
                // 12시 방향 = 90도 (라디안 상에선 PI / 2)
                float angleDegrees = 90f - (i * 40f);
                float angleRad = angleDegrees * Mathf.Deg2Rad;

                float currentRadius = OrbitRadius * tElastic;
                float x = Mathf.Cos(angleRad) * currentRadius;
                float y = Mathf.Sin(angleRad) * currentRadius;

                _spheres[i].anchoredPosition = new Vector2(x, y);
                _sphereCanvasGroups[i].alpha = Mathf.Lerp(0f, 1f, t * 1.5f);
            }

            yield return null;
        }

        // 최종 안착
        for (int i = 0; i < _spheres.Count; i++)
        {
            float angleDegrees = 90f - (i * 40f);
            float angleRad = angleDegrees * Mathf.Deg2Rad;
            float x = Mathf.Cos(angleRad) * OrbitRadius;
            float y = Mathf.Sin(angleRad) * OrbitRadius;

            _spheres[i].anchoredPosition = new Vector2(x, y);
            _sphereCanvasGroups[i].alpha = 1f;
        }
    }

    private IEnumerator CoAnimateCollapse(Action onDone)
    {
        float duration = 0.2f;
        float elapsed = 0f;

        if (IsReactionMode)
        {
            Vector2 sPos = _reactionPill.anchoredPosition;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float tCurve = t * t;

                _reactionPill.anchoredPosition = Vector2.Lerp(sPos, Vector2.zero, tCurve);
                _reactionPillCg.alpha = Mathf.Lerp(1f, 0f, t);

                yield return null;
            }
            onDone?.Invoke();
            yield break;
        }

        Vector2[] startPos = new Vector2[_spheres.Count];
        for (int i = 0; i < _spheres.Count; i++)
        {
            startPos[i] = _spheres[i].anchoredPosition;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float tCurve = t * t; // 점점 빨라지는 감폭 곡선

            if (_orbitRingImg != null)
            {
                _orbitRingImg.color = new Color(ThemeColor.r, ThemeColor.g, ThemeColor.b, Mathf.Lerp(0.08f, 0f, t));
            }

            for (int i = 0; i < _spheres.Count; i++)
            {
                _spheres[i].anchoredPosition = Vector2.Lerp(startPos[i], Vector2.zero, tCurve);
                _sphereCanvasGroups[i].alpha = Mathf.Lerp(1f, 0f, t);
            }

            _feedbackCg.alpha = Mathf.Lerp(_feedbackCg.alpha, 0f, t);

            yield return null;
        }

        onDone?.Invoke();
    }

    private static float SinProgress(float t)
    {
        // 탄성 튀어오름 커브 모사
        return Mathf.Sin(t * Mathf.PI * 0.5f) * 1.08f - Mathf.Sin(t * Mathf.PI * 1.5f) * 0.08f;
    }
}
