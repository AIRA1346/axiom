#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OpenWorldBootstrap))]
public sealed class OpenWorldBootstrapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(6f);
        EditorGUILayout.HelpBox(
            "씬에 Terrain 이 있으면 플레이 시 자동으로 사용합니다(슬롯 비워도 됨). 이름이 OpenWorld_Terrain 이면 여러 개일 때 우선합니다. 씬에 없을 때만 런타임 절차 지형을 만듭니다. 아래는 베이크용이며 슬롯에 연결해 줍니다.",
            MessageType.Info);

        if (GUILayout.Button("씬에 지형 베이크 (씬 뷰에서 편집 가능)", GUILayout.Height(28f)))
        {
            OpenWorldTerrainBake.Bake((OpenWorldBootstrap)target);
        }

        if (GUILayout.Button("씬에 플레이어 배치 (편집 모드에서 보이게)", GUILayout.Height(28f)))
        {
            OpenWorldPlayerSceneSetup.PlacePlayerInScene((OpenWorldBootstrap)target);
        }
    }
}
#endif
