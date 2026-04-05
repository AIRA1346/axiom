#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class OpenWorldSpawnPersistenceMenu
{
    [MenuItem("Tools/ARCHÉ/Open World/Clear Saved Spawn Position")]
    public static void ClearSavedSpawn()
    {
        OpenWorldSpawnPersistence.Clear();
        Debug.Log("[OpenWorld] 저장된 스폰 위치를 지웠습니다. 다음 플레이는 기본 스폰(지형 중심 등)입니다.");
    }
}
#endif
