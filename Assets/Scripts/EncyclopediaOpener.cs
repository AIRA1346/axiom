using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도감 열기 버튼에 붙이는 컴포넌트.
/// 클릭 시 UIManager를 통해 도감 상태로 전환합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class EncyclopediaOpener : MonoBehaviour
{
    private void Awake()
    {
        var button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OpenEncyclopedia);
        }
    }

    private void OpenEncyclopedia()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Encyclopedia);
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("[EncyclopediaOpener] GameManager를 찾을 수 없습니다.");
#endif
        }
    }
}
