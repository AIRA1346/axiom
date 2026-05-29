using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 로비 액션 버튼 등 마우스 호버 시 라벨(TMP)의 폰트 크기를 부드럽게 키우며,
/// 동시에 TextMeshPro 셰이더의 언더레이(Underlay) 속성을 조작하여 
/// 화려하게 은은한 네온 광채(Underlay Glow)가 부드럽게 확장/부스팅되도록 연출하는 반응형 텍스트 부스터입니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyButtonLabelHoverBoost : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Font Size Settings")]
    [SerializeField] private float hoverFontSizeMultiplier = 1.08f;
    [SerializeField] private float lerpSpeed = 12f;

    [Header("TextMesh Pro Underlay Glow")]
    [SerializeField] private float hoverUnderlayDilateBoost = 0.25f;
    [SerializeField] private float hoverUnderlayAlphaMultiplier = 2.0f;

    private TextMeshProUGUI _label;
    private float _restingFontSize = -1f;

    private Material _labelMaterial;
    private bool _hasUnderlay;
    private Color _restingUnderlayColor;
    private float _restingUnderlayDilate;
    private float _restingUnderlaySoftness;

    private Coroutine _boostRoutine;
    private bool _isHovered;

    private void Awake()
    {
        _label = GetComponentInChildren<TextMeshProUGUI>(true);
        InitializeUnderlaySettings();
    }

    private void OnEnable()
    {
        CaptureRestingSize();
        ResetVisuals();
    }

    private void OnDisable()
    {
        ResetVisuals();
    }

    private void InitializeUnderlaySettings()
    {
        if (_label == null)
        {
            return;
        }

        // fontMaterial에 접근하면 이 텍스트 컴포넌트만을 위한 인스턴스 머티리얼이 생성됩니다.
        _labelMaterial = _label.fontMaterial;
        if (_labelMaterial != null)
        {
            _hasUnderlay = _labelMaterial.HasProperty(ShaderUtilities.ID_UnderlayColor);
            if (_hasUnderlay)
            {
                _restingUnderlayColor = _labelMaterial.GetColor(ShaderUtilities.ID_UnderlayColor);
                _restingUnderlayDilate = _labelMaterial.GetFloat(ShaderUtilities.ID_UnderlayDilate);
                _restingUnderlaySoftness = _labelMaterial.GetFloat(ShaderUtilities.ID_UnderlaySoftness);
            }
        }
    }

    /// <summary>외부에서 글자 크기를 다시 세팅한 뒤 호출해 기준 크기를 맞춥니다.</summary>
    public void CaptureRestingSize()
    {
        if (_label == null)
        {
            _label = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (_label != null)
        {
            _restingFontSize = _label.fontSize;
        }

        if (_labelMaterial == null && _label != null)
        {
            InitializeUnderlaySettings();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        TriggerAnimation();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        TriggerAnimation();
    }

    private void TriggerAnimation()
    {
        if (_boostRoutine != null)
        {
            StopCoroutine(_boostRoutine);
        }
        _boostRoutine = StartCoroutine(CoAnimateBoost());
    }

    private IEnumerator CoAnimateBoost()
    {
        if (_label == null || _restingFontSize <= 0f)
        {
            _boostRoutine = null;
            yield break;
        }

        float targetScale = _isHovered ? Mathf.Max(1.01f, hoverFontSizeMultiplier) : 1f;
        float targetFontSize = _restingFontSize * targetScale;

        Color targetUnderlayColor = _restingUnderlayColor;
        float targetUnderlayDilate = _restingUnderlayDilate;

        if (_isHovered && _hasUnderlay)
        {
            targetUnderlayColor.a = Mathf.Clamp01(_restingUnderlayColor.a * hoverUnderlayAlphaMultiplier);
            targetUnderlayDilate = Mathf.Clamp(_restingUnderlayDilate + hoverUnderlayDilateBoost, -1f, 1f);
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * lerpSpeed;
            _label.fontSize = Mathf.Lerp(_label.fontSize, targetFontSize, t);

            if (_hasUnderlay && _labelMaterial != null)
            {
                Color curCol = _labelMaterial.GetColor(ShaderUtilities.ID_UnderlayColor);
                float curDil = _labelMaterial.GetFloat(ShaderUtilities.ID_UnderlayDilate);

                _labelMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, Color.Lerp(curCol, targetUnderlayColor, t));
                _labelMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, Mathf.Lerp(curDil, targetUnderlayDilate, t));
            }

            yield return null;
        }

        // 최종 상태 보장
        _label.fontSize = targetFontSize;
        if (_hasUnderlay && _labelMaterial != null)
        {
            _labelMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, targetUnderlayColor);
            _labelMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, targetUnderlayDilate);
        }

        _boostRoutine = null;
    }

    private void ResetVisuals()
    {
        _isHovered = false;
        if (_boostRoutine != null)
        {
            StopCoroutine(_boostRoutine);
            _boostRoutine = null;
        }

        if (_label != null && _restingFontSize > 0f)
        {
            _label.fontSize = _restingFontSize;
        }

        if (_hasUnderlay && _labelMaterial != null)
        {
            _labelMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, _restingUnderlayColor);
            _labelMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, _restingUnderlayDilate);
        }
    }

    public static void EnsureOn(Button button)
    {
        if (button == null)
        {
            return;
        }

        // 4대 별 노드 버튼에는 호버 부스트 컴포넌트가 부착되지 않도록 보호하여 오작동 및 텍스트 숨김 문제를 예방합니다.
        if (button.name == "GSIButton" || 
            button.name == "ShopButton" || 
            button.name == "InventoryButton" || 
            button.name == "AltarOfVerityButton" ||
            button.GetComponent<StarNodeControllerBase>() != null)
        {
            return;
        }

        var boost = button.GetComponent<LobbyButtonLabelHoverBoost>();
        if (boost == null)
        {
            boost = button.gameObject.AddComponent<LobbyButtonLabelHoverBoost>();
        }

        boost.CaptureRestingSize();
    }
}

