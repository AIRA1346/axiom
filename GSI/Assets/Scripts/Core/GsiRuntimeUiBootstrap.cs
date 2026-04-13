using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// 인벤토리·상점 등 런타임 UI 씬에서 공통 부트스트랩.
/// EconomyManager는 씬에 이미 있으면 추가 생성하지 않아(에디터에서 Awake 전에 Instance가 null인 경우) 중복을 막습니다.
/// </summary>
public static class GsiRuntimeUiBootstrap
{
    private const string AutoEventSystemName = "GSI_EventSystem";

    public static void DestroyObjectForRuntimeUi(GameObject go)
    {
        if (go == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Object.DestroyImmediate(go);
            return;
        }
#endif
        Object.Destroy(go);
    }

    public static void EnsureEventSystemForUiScenes()
    {
        EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (systems != null && systems.Length > 0)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && systems.Length > 1)
            {
                Debug.LogWarning(
                    $"[GSI] EventSystem이 씬에 {systems.Length}개 있습니다. UI 입력이 이상하면 중복을 제거하세요.");
            }
#endif
            return;
        }

        var es = new GameObject(AutoEventSystemName);
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    public static void EnsureEconomyManagerForUiScenes()
    {
        if (EconomyManager.Instance != null)
        {
            return;
        }

        EconomyManager[] existing = Object.FindObjectsByType<EconomyManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (existing != null && existing.Length > 0)
        {
            return;
        }

        var go = new GameObject("EconomyManager");
        go.AddComponent<EconomyManager>();
    }
}
