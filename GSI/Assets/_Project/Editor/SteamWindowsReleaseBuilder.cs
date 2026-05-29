using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// One-click Windows release build path for Steam. Keeps Addressables and depot checks in the same flow.
/// </summary>
public static class SteamWindowsReleaseBuilder
{
    private const string BetaMenuPath = "Tools/GSI/Steam/Build Windows Steam Beta";
    private const string FinalMenuPath = "Tools/GSI/Steam/Build Windows Steam Final Release";
    private const string OutputDirectory = "Builds/SteamWindows";
    private const string ExeName = "TheAxiom.exe";

    [MenuItem(BetaMenuPath)]
    public static void BuildWindowsSteamBeta()
    {
        SteamReleaseReadinessMenu.AllowBetaVersionForNextBuild();
        BuildWindowsSteamReleaseCore("beta");
    }

    [MenuItem(FinalMenuPath)]
    public static void BuildWindowsSteamFinalRelease()
    {
        BuildWindowsSteamReleaseCore("final");
    }

    private static void BuildWindowsSteamReleaseCore(string label)
    {
        SteamPcPlayerSettingsHelper.ApplySteamPcDefaults();
        AddressableAssetSettings.BuildPlayerContent();

        string outputDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputDirectory));
        Directory.CreateDirectory(outputDir);
        string outputPath = Path.Combine(outputDir, ExeName);

        var options = new BuildPlayerOptions
        {
            scenes = GetEnabledScenePaths(),
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[SteamRelease] Windows Steam {label} build failed: {report.summary.result}");
            return;
        }

        RemoveSteamAppIdFromBuildOutput(outputDir);
        Debug.Log($"[SteamRelease] Windows Steam {label} build complete: {outputPath}");
    }

    private static string[] GetEnabledScenePaths()
    {
        var scenes = EditorBuildSettings.scenes;
        var enabled = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (scene.enabled)
            {
                enabled.Add(scene.path);
            }
        }

        return enabled.ToArray();
    }

    private static void RemoveSteamAppIdFromBuildOutput(string outputDir)
    {
        string appIdPath = Path.Combine(outputDir, "steam_appid.txt");
        if (File.Exists(appIdPath))
        {
            File.Delete(appIdPath);
            Debug.LogWarning("[SteamRelease] Removed steam_appid.txt from build output. Steam depots should not include it.");
        }
    }
}
