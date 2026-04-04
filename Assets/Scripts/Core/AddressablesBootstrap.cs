using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// Addressables.InitializeAsync()를 한 번만 실행하고, 모든 시스템이 동일한 Task를 기다립니다.
/// Localization·아이템·Codex 등이 인트로 순서와 무관하게 안전하게 로드할 수 있습니다.
/// </summary>
public static class AddressablesBootstrap
{
    private static readonly object _gate = new object();

    private static Task _initTask;

    /// <summary>이미 완료된 경우 즉시 반환합니다. 실패 시 경고만 남기고 완료됩니다.</summary>
    public static Task EnsureInitializedAsync()
    {
        lock (_gate)
        {
            if (_initTask != null)
            {
                return _initTask;
            }

            _initTask = InitializeCoreAsync();
            return _initTask;
        }
    }

    private static async Task InitializeCoreAsync()
    {
        try
        {
            var handle = Addressables.InitializeAsync();
            await handle.Task;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AddressablesBootstrap] 초기화 실패(이후 로드는 계속 시도): {e.Message}");
        }
    }
}
