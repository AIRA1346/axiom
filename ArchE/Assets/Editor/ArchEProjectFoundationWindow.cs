using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// 대규모 데이터·Addressables·StreamingAssets 기준을 한 화면에서 확인합니다.
/// Tools → ARCHÉ → Project → 개발 기반 점검
/// </summary>
public sealed class ArchEProjectFoundationWindow : EditorWindow
{
    private Vector2 _scroll;

    [MenuItem("Tools/ARCHÉ/Project/개발 기반 점검")]
    public static void Open()
    {
        GetWindow<ArchEProjectFoundationWindow>(false, "ARCHÉ 개발 기반", true);
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.LabelField("프로젝트", EditorStyles.boldLabel);
        string unityVer = ReadUnityEditorVersion();
        EditorGUILayout.HelpBox(
            string.IsNullOrEmpty(unityVer) ? "ProjectVersion.txt 를 읽지 못했습니다." : $"Unity 에디터 버전: {unityVer}",
            MessageType.None);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("StreamingAssets (런타임 데이터)", EditorStyles.boldLabel);
        DrawFileRow("ItemMetadataCatalog.bin", Path.Combine(Application.dataPath, "StreamingAssets", "ItemMetadataCatalog.bin"));
        DrawFileRow("CodexIndex.bin", Path.Combine(Application.dataPath, "StreamingAssets", "CodexIndex.bin"));
        DrawShardFolderRow();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Addressables", EditorStyles.boldLabel);
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            EditorGUILayout.HelpBox("AddressableAssetSettings 가 없습니다. Window → Asset Management → Addressables 로 초기화하세요.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("AddressableAssetSettings 로드됨. 아이템 주소 = ItemId 규칙을 유지하세요.", MessageType.Info);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("권장 작업 순서 (데이터 변경 후)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "시트 임포트 → (필요 시) Item Metadata 빌드 → P0 검증 → 배포 전 Addressables Build Player Content\n" +
            "루트 .cursorrules: 명명, 120fps, UI 가상화, Resources.LoadAll 금지 등",
            MessageType.None);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("바로가기", EditorStyles.boldLabel);

        if (GUILayout.Button("릴리스 허브 (P0 · Addressables 빌드)"))
        {
            ArchEReleaseHubWindow.Open();
        }

        if (GUILayout.Button("P0 검증만 (빌드 없음)"))
        {
            ArchEP0ValidationGate.RunP0ValidateOnly(false);
        }

        if (GUILayout.Button("Addressables Groups"))
        {
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
        }

        if (GUILayout.Button("Profiler"))
        {
            EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler");
        }

        EditorGUILayout.EndScrollView();
    }

    private static string ReadUnityEditorVersion()
    {
        try
        {
            string path = Path.Combine(Application.dataPath, "..", "ProjectSettings", "ProjectVersion.txt");
            path = Path.GetFullPath(path);
            if (!File.Exists(path))
            {
                return "";
            }

            foreach (string line in File.ReadLines(path))
            {
                if (line.StartsWith("m_EditorVersion:"))
                {
                    return line.Substring("m_EditorVersion:".Length).Trim();
                }
            }
        }
        catch (IOException)
        {
            // 무시
        }

        return "";
    }

    private static void DrawFileRow(string label, string fullPath)
    {
        bool ok = File.Exists(fullPath);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(180));
        EditorGUILayout.LabelField(ok ? "있음" : "없음", ok ? EditorStyles.boldLabel : EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        if (!ok)
        {
            EditorGUILayout.HelpBox($"필요 시 Tools → ARCHÉ 메뉴에서 메타/Codex 빌드·임포트를 실행하세요.\n{fullPath}", MessageType.Warning);
        }
    }

    private static void DrawShardFolderRow()
    {
        string streaming = Path.Combine(Application.dataPath, "StreamingAssets");
        string catalogPath = Path.Combine(streaming, "ItemMetadataCatalog.bin");
        string legacyPath = Path.Combine(streaming, "ItemMetadata.bin");
        string dir = Path.Combine(streaming, "ItemMetadata");

        // 카탈로그 없이 레거시 단일 바이너리만 쓰는 개발 모드
        if (File.Exists(legacyPath) && !File.Exists(catalogPath))
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ItemMetadata 샤드 (*.bin)", GUILayout.Width(180));
            EditorGUILayout.LabelField("미사용 (레거시)", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("StreamingAssets/ItemMetadata.bin 단일 파일 모드입니다.", MessageType.Info);
            return;
        }

        bool ok = Directory.Exists(dir) && Directory.GetFiles(dir, "ItemMetadata_shard_*.bin").Length > 0;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ItemMetadata 샤드 (*.bin)", GUILayout.Width(180));
        EditorGUILayout.LabelField(ok ? "1개 이상" : "없음", ok ? EditorStyles.boldLabel : EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        if (!ok)
        {
            EditorGUILayout.HelpBox("샤딩 메타를 쓰는 경우 Build Item Metadata Database 로 생성합니다.", MessageType.Warning);
        }
    }
}
