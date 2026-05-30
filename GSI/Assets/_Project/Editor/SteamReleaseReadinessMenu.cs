using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Steam release guardrails that catch common depot/build mistakes before packaging.
/// </summary>
public sealed class SteamReleaseReadinessMenu : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    private const string MenuPath = "Tools/GSI/Steam/Run Steam Release Readiness Check";
    private const string BetaMenuPath = "Tools/GSI/Steam/Run Steam Beta Readiness Check";
    private const string BypassMenuPath = "Tools/GSI/Steam/Bypass Readiness Check for Local Builds";
    private const string SteamworksSymbol = "STEAMWORKS_ENABLED";
    private const string SpacewarAppId = "480";
    private const string BypassPrefsKey = "GSI_BypassSteamReleaseCheck";
    private static bool _allowBetaVersionForNextBuild;

    public int callbackOrder => 100;

    public static void AllowBetaVersionForNextBuild()
    {
        _allowBetaVersionForNextBuild = true;
    }

    [MenuItem(MenuPath)]
    public static void RunMenuCheck()
    {
        bool ok = RunChecks(BuildTarget.StandaloneWindows64, null, strictForBuild: false, betaRelease: false);
        if (ok)
        {
            Debug.Log("[SteamRelease] Readiness check passed. Finish Partner/depot/legal checks before release.");
        }
    }

    [MenuItem(BetaMenuPath)]
    public static void RunBetaMenuCheck()
    {
        bool ok = RunChecks(BuildTarget.StandaloneWindows64, null, strictForBuild: false, betaRelease: true);
        if (ok)
        {
            Debug.Log("[SteamRelease] Beta readiness check passed. Pre-1.0 bundleVersion is allowed for beta.");
        }
    }

    [MenuItem(BypassMenuPath)]
    public static void ToggleBypassCheck()
    {
        bool current = EditorPrefs.GetBool(BypassPrefsKey, false);
        EditorPrefs.SetBool(BypassPrefsKey, !current);
        Debug.Log($"[SteamRelease] Bypass Steam Release Readiness Check is now: {!current}");
    }

    [MenuItem(BypassMenuPath, true)]
    public static bool ToggleBypassCheckValidate()
    {
        Menu.SetChecked(BypassMenuPath, EditorPrefs.GetBool(BypassPrefsKey, false));
        return true;
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        if (EditorPrefs.GetBool(BypassPrefsKey, false))
        {
            Debug.LogWarning("[SteamRelease] Steam Release Readiness Check bypassed via user settings ('Tools > GSI > Steam > Bypass Readiness Check for Local Builds').");
            return;
        }

        if (report == null || !IsStandaloneWindows(report.summary.platform))
        {
            return;
        }

        bool betaRelease = _allowBetaVersionForNextBuild;
        _allowBetaVersionForNextBuild = false;
        if (!RunChecks(report.summary.platform, report.summary.outputPath, strictForBuild: true, betaRelease: betaRelease))
        {
            throw new BuildFailedException("[SteamRelease] Steam readiness check failed. See Console errors.");
        }
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (EditorPrefs.GetBool(BypassPrefsKey, false))
        {
            return;
        }

        if (report == null || !IsStandaloneWindows(report.summary.platform))
        {
            return;
        }

        string outputDir = ResolveOutputDirectory(report.summary.outputPath);
        if (string.IsNullOrEmpty(outputDir))
        {
            return;
        }

        string appIdInOutput = Path.Combine(outputDir, "steam_appid.txt");
        if (File.Exists(appIdInOutput))
        {
            Debug.LogError("[SteamRelease] steam_appid.txt exists in the build output. Remove it from the Steam depot.");
        }
    }

    private static bool RunChecks(BuildTarget target, string outputPath, bool strictForBuild, bool betaRelease)
    {
        bool ok = true;
        ok &= CheckIntroSceneFirst();
        ok &= CheckStandaloneSettings();
        ok &= CheckSteamSymbol();
        ok &= CheckReleaseDocs();
        ok &= CheckAddressablesData();
        ok &= CheckLocalSteamAppId(strictForBuild);
        ok &= CheckBuildOutput(outputPath);

        if (PlayerSettings.bundleVersion.StartsWith("0.", StringComparison.Ordinal))
        {
            string msg = $"[SteamRelease] bundleVersion is '{PlayerSettings.bundleVersion}'. Set the final Steam release version before depot upload.";
            if (strictForBuild && !betaRelease)
            {
                Debug.LogError(msg);
                ok = false;
            }
            else if (betaRelease)
            {
                Debug.Log("[SteamRelease] Beta release allows pre-1.0 bundleVersion.");
            }
            else
            {
                Debug.LogWarning(msg);
            }
        }

        return ok;
    }

    private static bool CheckIntroSceneFirst()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes == null || scenes.Length == 0)
        {
            Debug.LogError("[SteamRelease] No scenes are configured in Build Settings.");
            return false;
        }

        string firstPath = scenes[0].path ?? string.Empty;
        bool ok = firstPath.EndsWith("/IntroScene.unity", StringComparison.OrdinalIgnoreCase);
        if (!ok)
        {
            Debug.LogError("[SteamRelease] IntroScene must be the first scene in Build Settings.");
        }

        return ok;
    }

    private static bool CheckStandaloneSettings()
    {
        bool ok = true;
        if (!PlayerSettings.resizableWindow)
        {
            Debug.LogError("[SteamRelease] PlayerSettings.resizableWindow should be enabled for PC.");
            ok = false;
        }

        if (!PlayerSettings.runInBackground)
        {
            Debug.LogWarning("[SteamRelease] runInBackground is disabled. Verify Alt+Tab behavior before release.");
        }

        if (PlayerSettings.defaultScreenWidth < 1280 || PlayerSettings.defaultScreenHeight < 720)
        {
            Debug.LogError("[SteamRelease] Default resolution is too small for Steam PC.");
            ok = false;
        }

        return ok;
    }

    private static bool CheckSteamSymbol()
    {
        string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
        if (DefinesContains(defines, SteamworksSymbol))
        {
            return true;
        }

        Debug.LogError("[SteamRelease] STEAMWORKS_ENABLED is missing for Standalone. Use Tools/GSI/Steam/Enable STEAMWORKS_ENABLED.");
        return false;
    }

    private static bool CheckReleaseDocs()
    {
        bool ok = true;
        ok &= RequireAsset("Assets/Docs/STEAM_RELEASE_CHECKLIST.md");
        ok &= RequireAsset("Assets/Docs/STEAM_EULA_TEMPLATE.md");
        ok &= RequireAsset("Assets/Docs/STEAM_PRIVACY_TEMPLATE.md");
        return ok;
    }

    private static bool CheckAddressablesData()
    {
        return RequireAsset("Assets/AddressableAssetsData/AddressableAssetSettings.asset");
    }

    private static bool CheckLocalSteamAppId(bool strictForBuild)
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "steam_appid.txt");
        if (!File.Exists(path))
        {
            Debug.LogWarning("[SteamRelease] Local steam_appid.txt is missing. This is OK for depot builds, but local Steam testing needs it.");
            return true;
        }

        string appId = File.ReadAllText(path).Trim();
        if (appId == SpacewarAppId)
        {
            Debug.LogWarning("[SteamRelease] Local steam_appid.txt is still 480 (Spacewar). Replace it with the real App ID for final local QA.");
        }

        return true;
    }

    private static bool CheckBuildOutput(string outputPath)
    {
        string outputDir = ResolveOutputDirectory(outputPath);
        if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir))
        {
            return true;
        }

        string appId = Path.Combine(outputDir, "steam_appid.txt");
        if (!File.Exists(appId))
        {
            return true;
        }

        Debug.LogError("[SteamRelease] Existing build output contains steam_appid.txt. Remove it before depot upload.");
        return false;
    }

    private static bool RequireAsset(string assetPath)
    {
        if (File.Exists(assetPath))
        {
            return true;
        }

        Debug.LogError("[SteamRelease] Missing required release asset: " + assetPath);
        return false;
    }

    private static bool DefinesContains(string defines, string word)
    {
        foreach (string part in (defines ?? string.Empty).Split(';'))
        {
            if (part.Trim() == word)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsStandaloneWindows(BuildTarget target)
    {
        return target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64;
    }

    private static string ResolveOutputDirectory(string outputPath)
    {
        if (string.IsNullOrEmpty(outputPath))
        {
            return null;
        }

        if (Directory.Exists(outputPath))
        {
            return outputPath;
        }

        return Path.GetDirectoryName(outputPath);
    }
}
