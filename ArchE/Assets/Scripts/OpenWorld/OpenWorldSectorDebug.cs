using UnityEngine;

/// <summary>
/// 현재 위치의 섹터 인덱스를 화면에 표시하고, Scene 뷰에서 섹터 타일 윤곽을 그립니다.
/// </summary>
public sealed class OpenWorldSectorDebug : MonoBehaviour
{
    [SerializeField] private bool _showScreenHud = true;

    [SerializeField] private bool _drawSectorGizmo = true;

    [SerializeField] private Color _gizmoColor = new Color(1f, 0.85f, 0.2f, 0.9f);

    private GUIStyle _hudStyle;

    private void OnGUI()
    {
        if (!_showScreenHud || !Application.isPlaying)
        {
            return;
        }

        if (_hudStyle == null)
        {
            _hudStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
            };
            _hudStyle.normal.textColor = Color.white;
        }

        Vector2Int sec = WorldSectorGrid.WorldPositionToSectorXZ(transform.position);
        Vector3 p = transform.position;
        string text =
            $"Sector (XZ): ({sec.x}, {sec.y})\n" +
            $"World: ({p.x:F1}, {p.y:F1}, {p.z:F1})\n" +
            $"Tile: {WorldSectorGrid.SectorSizeMeters:F0} m";

        const float pad = 12f;
        GUI.Label(new Rect(pad, pad, 520f, 90f), text, _hudStyle);
    }

    private void OnDrawGizmos()
    {
        if (!_drawSectorGizmo || !Application.isPlaying)
        {
            return;
        }

        Vector2Int sec = WorldSectorGrid.WorldPositionToSectorXZ(transform.position);
        WorldSectorGrid.GetSectorBoundsXZ(sec, out Vector3 min, out Vector3 max);
        float y = transform.position.y + 0.05f;

        Vector3 a = new Vector3(min.x, y, min.z);
        Vector3 b = new Vector3(max.x, y, min.z);
        Vector3 c = new Vector3(max.x, y, max.z);
        Vector3 d = new Vector3(min.x, y, max.z);

        Gizmos.color = _gizmoColor;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }
}
