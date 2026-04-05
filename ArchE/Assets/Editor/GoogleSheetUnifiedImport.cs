using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 구글 시트에서 아이템·도서관을 연속 임포트한 뒤 Item Metadata Database까지 자동 빌드합니다.
/// 각 Importer 창에 저장된 URL·체크 목록을 사용합니다.
/// Tools → ARCHÉ → Google Sheet → Import All (Items → Codex → Item Metadata)
/// </summary>
public static class GoogleSheetUnifiedImport
{
    private const string MenuPath = "Tools/ARCHÉ/Google Sheet/Import All (Items → Codex → Item Metadata)";

    [MenuItem(MenuPath, priority = -50)]
    public static void RunFullPipeline()
    {
        if (!EditorUtility.DisplayDialog(
                "구글 시트 통합 임포트",
                "① Item Importer에 저장된 시트 → ② Codex Importer에 저장된 시트 순으로 임포트한 뒤\n" +
                "③ Build Item Metadata Database(샤딩·검증·Addressables 주소 검증 포함)를 실행합니다.\n\n" +
                "시간이 오래 걸릴 수 있습니다. 계속할까요?",
                "시작",
                "취소"))
        {
            return;
        }

        var itemWin = EditorWindow.GetWindow<ItemImporter>(false, null, false);
        itemWin.Show();

        itemWin.RunImportWithCallback(() =>
        {
            EditorApplication.delayCall += RunCodexThenMetadata;
        });
    }

    private static void RunCodexThenMetadata()
    {
        var codexWin = EditorWindow.GetWindow<CodexImporter>(false, null, false);
        codexWin.Show();

        codexWin.RunImportWithCallback(() =>
        {
            EditorApplication.delayCall += BuildItemMetadataStep;
        });
    }

    private static void BuildItemMetadataStep()
    {
        ItemMetadataBuilder.Build(false);
        Debug.Log(
            "[GoogleSheetUnifiedImport] 통합 임포트 및 Item Metadata 빌드까지 완료했습니다.\n" +
            "Addressables 번들 갱신은 Tools → ARCHÉ → Release → 릴리스 허브에서 필요 시 실행하세요.");
    }
}
