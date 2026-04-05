using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 속한 섹터가 바뀔 때마다, 주변 N×N 격자를 "로드/언로드"하는 스텁입니다.
/// 실제 Addressables 연동 전에 호출 순서·범위를 검증합니다.
/// </summary>
public sealed class OpenWorldSectorStreamingStub : MonoBehaviour
{
    /// <summary>섹터 스텁 루트가 생성된 직후. Addressables 인스턴스 부모로 사용.</summary>
    public event Action<Vector2Int, GameObject> OnSectorStubLoaded;

    /// <summary>언로드 직전(자식 파괴 전). Addressables Release 등에 사용.</summary>
    public event Action<Vector2Int> OnSectorStubUnloaded;

    [Tooltip("중심 섹터 기준 한 축으로 ±halfExtent → (2*halfExtent+1)² 개 로드. 2 = 5×5.")]
    [SerializeField] private int _halfExtent = 2;

    [SerializeField] private bool _logToConsole = true;

    private readonly HashSet<Vector2Int> _loadedSectors = new();

    private readonly Dictionary<Vector2Int, GameObject> _sectorRoots = new();

    private Vector2Int _lastSector = new(int.MinValue, int.MinValue);

    private Transform _bucket;

    private void Awake()
    {
        GameObject root = GameObject.Find("WorldSectorStreamingRoot");
        if (root == null)
        {
            root = new GameObject("WorldSectorStreamingRoot");
        }

        _bucket = root.transform;
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Vector2Int cur = WorldSectorGrid.WorldPositionToSectorXZ(transform.position);
        if (cur == _lastSector)
        {
            return;
        }

        _lastSector = cur;
        Reconcile(cur);
    }

    private void Reconcile(Vector2Int centerSector)
    {
        var desired = new HashSet<Vector2Int>();
        for (int x = centerSector.x - _halfExtent; x <= centerSector.x + _halfExtent; x++)
        {
            for (int z = centerSector.y - _halfExtent; z <= centerSector.y + _halfExtent; z++)
            {
                desired.Add(new Vector2Int(x, z));
            }
        }

        var toUnload = new List<Vector2Int>();
        foreach (Vector2Int s in _loadedSectors)
        {
            if (!desired.Contains(s))
            {
                toUnload.Add(s);
            }
        }

        foreach (Vector2Int s in toUnload)
        {
            UnloadSectorStub(s);
        }

        foreach (Vector2Int s in desired)
        {
            if (!_loadedSectors.Contains(s))
            {
                LoadSectorStub(s);
            }
        }
    }

    private void LoadSectorStub(Vector2Int sector)
    {
        _loadedSectors.Add(sector);

        var go = new GameObject($"SectorStub_{sector.x}_{sector.y}");
        go.transform.SetParent(_bucket, false);
        Vector3 c = WorldSectorGrid.SectorCenterXZ(sector);
        go.transform.position = new Vector3(c.x, transform.position.y, c.z);

        _sectorRoots[sector] = go;

        if (_logToConsole)
        {
            Debug.Log($"[SectorStreamingStub] Load sector ({sector.x}, {sector.y})");
        }

        OnSectorStubLoaded?.Invoke(sector, go);
    }

    private void UnloadSectorStub(Vector2Int sector)
    {
        OnSectorStubUnloaded?.Invoke(sector);

        _loadedSectors.Remove(sector);

        if (_sectorRoots.TryGetValue(sector, out GameObject go))
        {
            _sectorRoots.Remove(sector);
            if (go != null)
            {
                Destroy(go);
            }
        }

        if (_logToConsole)
        {
            Debug.Log($"[SectorStreamingStub] Unload sector ({sector.x}, {sector.y})");
        }
    }
}
