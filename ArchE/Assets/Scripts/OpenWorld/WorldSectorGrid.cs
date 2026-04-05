using UnityEngine;

/// <summary>
/// XZ 평면 기준 월드 섹터 격자. 크기는 <see cref="OpenWorldSpec.md"/> 와 맞춤.
/// </summary>
public static class WorldSectorGrid
{
    /// <summary>한 섹터의 가로·세로 길이(미터).</summary>
    public const float SectorSizeMeters = 512f;

    /// <summary>월드 위치가 속한 섹터 인덱스 (XZ). Y 는 무시.</summary>
    public static Vector2Int WorldPositionToSectorXZ(Vector3 worldPosition)
    {
        int sx = Mathf.FloorToInt(worldPosition.x / SectorSizeMeters);
        int sz = Mathf.FloorToInt(worldPosition.z / SectorSizeMeters);
        return new Vector2Int(sx, sz);
    }

    /// <summary>섹터 셀의 XZ 중심 (월드). Y 는 0.</summary>
    public static Vector3 SectorCenterXZ(Vector2Int sector)
    {
        return new Vector3(
            (sector.x + 0.5f) * SectorSizeMeters,
            0f,
            (sector.y + 0.5f) * SectorSizeMeters);
    }

    /// <summary>섹터 셀의 XZ 바닥 경계 (min/max).</summary>
    public static void GetSectorBoundsXZ(Vector2Int sector, out Vector3 min, out Vector3 max)
    {
        min = new Vector3(sector.x * SectorSizeMeters, 0f, sector.y * SectorSizeMeters);
        max = new Vector3(
            (sector.x + 1) * SectorSizeMeters,
            0f,
            (sector.y + 1) * SectorSizeMeters);
    }

    /// <summary>맨해튼 거리 (스트리밍 링 계산용).</summary>
    public static int ManhattanSectorDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
