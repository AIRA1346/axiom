using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// P0 릴리스: 프로파일링 체크리스트 + 데이터 검증·Addressables 콘텐츠 빌드 진입.
/// Tools → ARCHÉ → Release → 릴리스 허브
/// </summary>
public class ArchEReleaseHubWindow : EditorWindow
{
    private Vector2 _scroll;

    [MenuItem("Tools/ARCHÉ/Release/릴리스 허브")]
    public static void Open()
    {
        GetWindow<ArchEReleaseHubWindow>(true, "ARCHÉ 릴리스 허브", true);
    }

    [MenuItem("Tools/ARCHÉ/Release/전체 데이터 점검 (메타 + Codex 본문)")]
    public static void RunFullDataValidation()
    {
        bool ok = ArchEP0ValidationGate.RunP0Gate(silent: false);
        Debug.Log($"[ArchEReleaseHub] 전체 데이터 점검 종료 — {(ok ? "통과" : "실패 (콘솔 확인)")}.");
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.LabelField("권장 릴리스 순서", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1) Codex: CSV/시트 → CodexDataBuilder 빌드\n" +
            "2) 아이템: Build Item Metadata Database\n" +
            "3) Addressables: 아래 「Addressables 콘텐츠 빌드」\n" +
            "4) 플레이어 빌드",
            MessageType.Info);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("실측 프로파일링 (P0)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "에디터: Play Mode Script = Use Asset Database (Addressables 그룹).\n" +
            "빌드 측정: File → Build Settings → Development Build + Autoconnect Profiler 체크 후 빌드·실행.\n" +
            "Profiler: 첫 로드·도감·상점·인벤 구간에서 CPU/GPU 스파이크 확인.\n" +
            "Memory Profiler: 해당 화면 전후 스냅샷 비교(네이티브+관리 힙).",
            MessageType.None);

        if (GUILayout.Button("Profiler 창 열기"))
        {
            EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler");
        }

        if (GUILayout.Button("Memory Profiler (패키지 설치 시)"))
        {
            EditorApplication.ExecuteMenuItem("Window/Analysis/Memory Profiler");
        }

        if (GUILayout.Button("Build Settings 열기 (Development / Autoconnect Profiler)"))
        {
            EditorApplication.ExecuteMenuItem("File/Build Settings...");
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("원클릭 검증", EditorStyles.boldLabel);

        if (GUILayout.Button("① Item Metadata 빌드 + (내장) 메타·Addressables 주소 검증"))
        {
            ItemMetadataBuilder.Build(false);
        }

        if (GUILayout.Button("② Codex 본문(.txt) 검증만"))
        {
            CodexContentFileValidator.Validate(false);
        }

        if (GUILayout.Button("① + ② 연속 실행 (전체 데이터 점검)"))
        {
            RunFullDataValidation();
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Addressables (P0 점검)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "그룹: 라벨·번들 중복(같은 에셋이 여러 그룹)·Remote 분리를 주기적으로 확인.\n" +
            "에디터에서 번들을 갱신해야 런타임/빌드에 최신 주소가 반영됩니다.",
            MessageType.Warning);

        if (GUILayout.Button("Addressables Groups 창 열기"))
        {
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
        }

        if (GUILayout.Button("Addressables 콘텐츠 빌드 (Build Player Content)"))
        {
            if (!EditorUtility.DisplayDialog(
                    "Addressables",
                    "Build Player Content를 실행합니다. 프로젝트 크기에 따라 수 분 걸릴 수 있습니다. 계속할까요?",
                    "실행",
                    "취소"))
            {
                return;
            }

            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                EditorUtility.DisplayDialog("Addressables", "AddressableAssetSettings가 없습니다. Addressables 창에서 초기화하세요.", "확인");
                return;
            }

            AddressableAssetSettings.BuildPlayerContent();
            Debug.Log("[ArchEReleaseHub] Addressables BuildPlayerContent 완료.");
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("배치 / CI (P0 게이트)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "로컬 또는 CI에서 Unity를 batchmode로 띄워 검증만 할 때:\n" +
            "-executeMethod ArchEP0ValidationGate.RunP0GateForCi\n" +
            "성공 시 프로세스 종료 코드 0, 실패 시 1.",
            MessageType.Info);

        if (GUILayout.Button("P0 검증 명령줄을 클립보드에 복사"))
        {
            string projectPath = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath) ?? "";
            projectPath = projectPath.Replace("\\", "/");
            string line =
                $"Unity.exe -batchmode -nographics -quit -projectPath \"{projectPath}\" -executeMethod ArchEP0ValidationGate.RunP0GateForCi";
            EditorGUIUtility.systemCopyBuffer = line;
            Debug.Log("[ArchEReleaseHub] 클립보드에 복사됨. Unity.exe 경로는 설치 위치에 맞게 바꾸세요.");
        }

        EditorGUILayout.EndScrollView();
    }
}
