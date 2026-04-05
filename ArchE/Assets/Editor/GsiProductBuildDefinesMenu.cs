using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// G.S.I 단독 빌드용 <c>GSI_PRODUCT</c> 스크립팅 심볼을 플랫폼별로 켜고 끕니다.
/// 켜면 <see cref="ItemDatabase"/>가 아이템 메타를 로드하지 않습니다.
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

    [MenuItem("Tools/ARCHÉ/Build Profile/GSI 단독 빌드 심볼 켜기 (GSI_PRODUCT)")]
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

    [MenuItem("Tools/ARCHÉ/Build Profile/GSI 단독 빌드 심볼 끄기 (ARCHÉ 전체)")]
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

        Debug.Log("[GSI] GSI_PRODUCT 심볼을 껐습니다. 아이템 DB·P0 검증 경로는 ARCHÉ 전제로 동작합니다.");
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
