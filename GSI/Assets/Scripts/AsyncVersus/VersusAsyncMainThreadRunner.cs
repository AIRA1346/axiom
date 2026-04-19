using UnityEngine;

/// <summary>
/// 런타임에 숨은 GameObject로 코루틴을 돌립니다. <see cref="UnityWebRequest"/> 전송 등 메인 스레드 전용 작업용.
/// </summary>
internal sealed class VersusAsyncMainThreadRunner : MonoBehaviour
{
    public static VersusAsyncMainThreadRunner Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (Instance != null)
        {
            return;
        }

        var go = new GameObject(nameof(VersusAsyncMainThreadRunner));
        Instance = go.AddComponent<VersusAsyncMainThreadRunner>();
        DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;
    }
}
