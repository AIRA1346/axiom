using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// G.S.I 단독 빌드용 <c>GSI_PRODUCT</c> 스크립팅 심볼을 플랫폼별로 켜고 끕니다.
/// (레거시) Player Settings 심볼만 조정합니다. 에디터 컴파일은 <c>GSI/csc.rsp</c>를 참고하세요.
/// 에디터에서의 컴파일 심볼은 Unity 6에 Editor 전용 NamedBuildTarget/BuildTargetGroup 이 없으므로
/// <c>GSI/csc.rsp</c>의 <c>-define:GSI_PRODUCT</c>로 맞춥니다.
/// </summary>
public static class GsiProductBuildDefinesMenu
{
    private const string Symbol = "GSI_PRODUCT";

    private static readonly NamedBuildTarget[] Targets =
    {
        NamedBuildTarget.Standalone,
        NamedBuildTarget.Android,
        NamedBuildTarget.iOS,
        NamedBuildTarget.WebGL,
    };

    [MenuItem("Tools/ARCHE/Build Profile/GSI 단독 빌드 심볼 켜기 (GSI_PRODUCT)")]
    public static void EnableGsiProductSymbol()
    {
        foreach (NamedBuildTarget t in Targets)
        {
            try
            {
                string s = PlayerSettings.GetScriptingDefineSymbols(t);
                if (string.IsNullOrEmpty(s))
                {
                    PlayerSettings.SetScriptingDefineSymbols(t, Symbol);
                }
                else if (!DefinesContains(s, Symbol))
                {
                    PlayerSettings.SetScriptingDefineSymbols(t, s + ";" + Symbol);
                }
            }
            catch (System.ArgumentException)
            {
                // 해당 NamedBuildTarget 미설치 등
            }
        }

        Debug.Log("[GSI] GSI_PRODUCT 심볼을 켰습니다. 아이템 DB는 빌드에 포함하지 않아도 됩니다(런타임 로드 안 함).");
    }

    [MenuItem("Tools/ARCHE/Build Profile/GSI 단독 빌드 심볼 끄기 (ARCHE 전체)")]
    public static void DisableGsiProductSymbol()
    {
        foreach (NamedBuildTarget t in Targets)
        {
            try
            {
                string s = PlayerSettings.GetScriptingDefineSymbols(t);
                if (string.IsNullOrEmpty(s))
                {
                    continue;
                }

                string[] parts = s.Split(';');
                System.Collections.Generic.List<string> kept = new System.Collections.Generic.List<string>();
                foreach (string p in parts)
                {
                    string trim = p.Trim();
                    if (trim.Length > 0 && trim != Symbol)
                    {
                        kept.Add(trim);
                    }
                }

                PlayerSettings.SetScriptingDefineSymbols(t, string.Join(";", kept));
            }
            catch (System.ArgumentException)
            {
            }
        }

        Debug.LogWarning(
            "[GSI] Player Settings 에서 GSI_PRODUCT 를 껐습니다. GSI/csc.rsp 에 -define:GSI_PRODUCT 가 있으면 " +
            "에디터·빌드 모두 여전히 심볼이 켜진 상태입니다. 완전히 끄려면 csc.rsp 해당 줄을 제거하세요.");
    }

    private static bool DefinesContains(string defines, string word)
    {
        foreach (string p in defines.Split(';'))
        {
            if (p.Trim() == word)
            {
                return true;
            }
        }

        return false;
    }
}
