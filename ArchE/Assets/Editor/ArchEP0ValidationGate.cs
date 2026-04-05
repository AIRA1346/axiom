using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// P0: 메타 빌드 + Codex 본문 검증을 한 번에 실행하고, batchmode에서는 종료 코드로 성공/실패를 반환합니다.
/// <para>예: Unity.exe -batchmode -quit -projectPath "..." -executeMethod ArchEP0ValidationGate.RunP0GateForCi</para>
/// Tools → ARCHÉ → Release → P0 검증 (배치/CI)
/// </summary>
public static class ArchEP0ValidationGate
{
    /// <summary>CI/로컬 스크립트에서 -executeMethod로 호출. batchmode일 때만 Exit 코드를 설정합니다.</summary>
    public static void RunP0GateForCi()
    {
        bool ok = RunP0Gate(silent: true);
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }

    /// <summary>
    /// 메타 DB를 다시 빌드하지 않고, 커밋된 StreamingAssets·Addressables·Codex 본문만 검사합니다. GitHub Actions 등 CI용.
    /// </summary>
    public static void RunP0ValidateOnlyForCi()
    {
        bool ok = RunP0ValidateOnly(silent: true);
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }

    /// <summary>메타 빌드·검증 + Codex 본문 검증. 둘 다 통과하면 true.</summary>
    public static bool RunP0Gate(bool silent)
    {
        bool metaOk = ItemMetadataBuilder.Build(silent);
        bool codexOk = CodexContentFileValidator.ValidateWithResult(silent);
        bool ok = metaOk && codexOk;
        Debug.Log($"[ArchEP0ValidationGate] P0 게이트: {(ok ? "통과" : "실패")} (메타·Addressables={metaOk}, Codex 본문={codexOk})");
        return ok;
    }

    /// <summary>
    /// ItemMetadata 빌드 없이 검증만(샤딩·레거시 산출물, Addressables 주소, Codex .txt). CI와 동일한 검사.
    /// </summary>
    public static bool RunP0ValidateOnly(bool silent)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        ItemMetadataValidationResult metaResult = ItemMetadataValidator.ValidateAuto(projectRoot);
        ItemMetadataValidator.Report(metaResult, silent);
        bool addrOk = AddressablesItemAddressValidator.ValidateWithResult(silent);
        bool codexOk = CodexContentFileValidator.ValidateWithResult(silent);
        bool ok = metaResult.Success && addrOk && codexOk;
        Debug.Log(
            $"[ArchEP0ValidationGate] P0 검증만(빌드 없음): {(ok ? "통과" : "실패")} (메타={metaResult.Success}, Addressables={addrOk}, Codex={codexOk})");
        return ok;
    }

    [MenuItem("Tools/ARCHÉ/Release/P0 검증 (silent, CI용 로그)")]
    public static void MenuRunP0Silent()
    {
        RunP0Gate(silent: true);
    }

    [MenuItem("Tools/ARCHÉ/Release/P0 검증만 (빌드 없음 · CI와 동일)")]
    public static void MenuRunP0ValidateOnly()
    {
        RunP0ValidateOnly(silent: false);
    }
}
