using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 섹터 스텁이 올라올 때 Addressables 로 프리팹을 한 번 붙여 봅니다.
/// 기본은 주소 <c>World_Sector_0_0</c> 이고, (0,0) 섹터에만 인스턴스합니다.
/// </summary>
public sealed class OpenWorldSectorAddressablesSample : MonoBehaviour
{
    [SerializeField] private bool _useAddressablesSample = true;

    [Tooltip("에디터 메뉴로 등록한 Addressables 주소 (OpenWorldSpec: World_Sector_{x}_{z}).")]
    [SerializeField] private string _sampleAddress = "World_Sector_0_0";

    [Tooltip("체크 시 (0,0) 섹터에서만 샘플 프리팹을 붙입니다. 끄면 모든 섹터에 동일 주소로 시도(주소가 여러 개 있어야 함).")]
    [SerializeField] private bool _onlyOriginSector = true;

    private OpenWorldSectorStreamingStub _stub;

    private readonly Dictionary<Vector2Int, GameObject> _instantiated = new();

    private readonly Dictionary<Vector2Int, AsyncOperationHandle<GameObject>> _handles = new();

    private void OnEnable()
    {
        _stub = GetComponent<OpenWorldSectorStreamingStub>();
        if (_stub != null)
        {
            _stub.OnSectorStubLoaded += OnStubLoaded;
            _stub.OnSectorStubUnloaded += OnStubUnloaded;
        }
    }

    private void OnDisable()
    {
        if (_stub != null)
        {
            _stub.OnSectorStubLoaded -= OnStubLoaded;
            _stub.OnSectorStubUnloaded -= OnStubUnloaded;
        }
    }

    private void OnStubLoaded(Vector2Int sector, GameObject sectorRoot)
    {
        if (!_useAddressablesSample || sectorRoot == null)
        {
            return;
        }

        if (_onlyOriginSector && (sector.x != 0 || sector.y != 0))
        {
            return;
        }

        _ = LoadSampleAsync(sector, sectorRoot);
    }

    private async Task LoadSampleAsync(Vector2Int sector, GameObject sectorRoot)
    {
        if (_instantiated.ContainsKey(sector))
        {
            return;
        }

        await AddressablesBootstrap.EnsureInitializedAsync();

        string address = _sampleAddress;
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);
        _handles[sector] = handle;
        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            Debug.LogWarning(
                $"[OpenWorldAddressables] 로드 실패: `{address}` — Tools → ARCHÉ → Open World → Register Sample Sector Addressable 실행");
            if (_handles.TryGetValue(sector, out AsyncOperationHandle<GameObject> h))
            {
                _handles.Remove(sector);
                if (h.IsValid())
                {
                    Addressables.Release(h);
                }
            }

            return;
        }

        GameObject inst = Instantiate(handle.Result, sectorRoot.transform);
        inst.name = $"Addr_{address}";
        inst.transform.localPosition = Vector3.zero;
        _instantiated[sector] = inst;
    }

    private void OnStubUnloaded(Vector2Int sector)
    {
        if (_instantiated.TryGetValue(sector, out GameObject go))
        {
            _instantiated.Remove(sector);
            if (go != null)
            {
                Destroy(go);
            }
        }

        if (_handles.TryGetValue(sector, out AsyncOperationHandle<GameObject> handle))
        {
            _handles.Remove(sector);
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }
}
