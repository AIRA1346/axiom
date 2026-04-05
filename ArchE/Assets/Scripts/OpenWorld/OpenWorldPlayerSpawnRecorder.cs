using UnityEngine;

/// <summary>
/// 빌드 실행 시 플레이 종료 직전 위치를 <see cref="OpenWorldSpawnPersistence"/>에 남깁니다.
/// </summary>
public sealed class OpenWorldPlayerSpawnRecorder : MonoBehaviour
{
    private void OnApplicationQuit()
    {
        OpenWorldSpawnPersistence.Save(transform.position, transform.rotation);
    }

    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            OpenWorldSpawnPersistence.Save(transform.position, transform.rotation);
        }
    }
}
