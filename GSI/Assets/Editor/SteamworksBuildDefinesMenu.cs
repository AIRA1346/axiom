using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// <c>STEAMWORKS_ENABLED</c> 스크립팅 심볼을 켜고 끕니다. Steamworks.NET 코드는 이 심볼이 있을 때만 활성화됩니다.
/// </summary>
public static class SteamworksBuildDefinesMenu
{
    private const string Symbol = "STEAMWORKS_ENABLED";

    [MenuItem("Tools/GSI/Steam/Enable STEAMWORKS_ENABLED (Standalone + Editor if available)")]
    public static void EnableSteamworksSymbol()
    {
        foreach (NamedBuildTarget t in EnumerateTargets())
        {
            try
            {
                AddSymbol(t);
            }
            catch (System.ArgumentException)
            {
                // 플랫폼 모듈 미설치 등
            }
        }

        Debug.Log("[Steamworks] STEAMWORKS_ENABLED 켬. 프로젝트 루트에 steam_appid.txt 를 두고 Steam 클라이언트를 실행한 뒤 플레이하세요.");
    }

    [MenuItem("Tools/GSI/Steam/Disable STEAMWORKS_ENABLED")]
    public static void DisableSteamworksSymbol()
    {
        foreach (NamedBuildTarget t in EnumerateTargets())
        {
            try
            {
                RemoveSymbol(t);
            }
            catch (System.ArgumentException)
            {
            }
        }

        Debug.Log("[Steamworks] STEAMWORKS_ENABLED 끔.");
    }

    private static void AddSymbol(NamedBuildTarget target)
    {
        string s = PlayerSettings.GetScriptingDefineSymbols(target);
        if (string.IsNullOrEmpty(s))
        {
            PlayerSettings.SetScriptingDefineSymbols(target, Symbol);
        }
        else if (!DefinesContains(s, Symbol))
        {
            PlayerSettings.SetScriptingDefineSymbols(target, s + ";" + Symbol);
        }
    }

    private static void RemoveSymbol(NamedBuildTarget target)
    {
        string s = PlayerSettings.GetScriptingDefineSymbols(target);
        if (string.IsNullOrEmpty(s))
        {
            return;
        }

        string[] parts = s.Split(';');
        var kept = new List<string>();
        foreach (string p in parts)
        {
            string trim = p.Trim();
            if (trim.Length > 0 && trim != Symbol)
            {
                kept.Add(trim);
            }
        }

        PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", kept));
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

    private static IEnumerable<NamedBuildTarget> EnumerateTargets()
    {
        yield return NamedBuildTarget.Standalone;

        PropertyInfo editorProp = typeof(NamedBuildTarget).GetProperty(
            "Editor",
            BindingFlags.Public | BindingFlags.Static);
        if (editorProp != null && editorProp.GetValue(null) is NamedBuildTarget editorTarget)
        {
            yield return editorTarget;
        }
    }
}
