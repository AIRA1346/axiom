using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 로비 액션 버튼 등: 포인터가 올라가면 라벨(TMP) 글자 크기를 살짝 키웁니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyButtonLabelHoverBoost : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverFontSizeMultiplier = 1.12f;

    private TextMeshProUGUI _label;
    private float _restingFontSize = -1f;

    private void Awake()
    {
        _label = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void OnEnable()
    {
        CaptureRestingSize();
        RestoreVisual();
    }

    private void OnDisable()
    {
        RestoreVisual();
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
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_label == null || _restingFontSize <= 0f)
        {
            return;
        }

        float m = Mathf.Max(1.01f, hoverFontSizeMultiplier);
        _label.fontSize = _restingFontSize * m;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RestoreVisual();
    }

    private void RestoreVisual()
    {
        if (_label == null || _restingFontSize <= 0f)
        {
            return;
        }

        _label.fontSize = _restingFontSize;
    }

    public static void EnsureOn(Button button)
    {
        if (button == null)
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
