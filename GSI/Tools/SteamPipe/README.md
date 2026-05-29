# SteamPipe Templates

These files are templates. Copy them to `.vdf` files on the build machine and replace:

- `TODO_STEAM_APP_ID`
- `TODO_WINDOWS_DEPOT_ID`

Known Steam App ID:

- The Axiom app: `1628285`
- The Axiom demo app: `1628286`

Recommended flow:

1. In Unity, run `Tools/GSI/Steam/Build Windows Steam Beta` for beta, or `Tools/GSI/Steam/Build Windows Steam Final Release` for final release.
2. Run `Tools/Validate-SteamRelease.ps1 -BetaRelease -BuildOutput "Builds/SteamWindows"` for beta, or `-StrictRelease` for final release.
3. Create local VDF files:

```powershell
.\New-SteamPipeBuildFiles.ps1 -AppId 1628285 -WindowsDepotId <WINDOWS_DEPOT_ID>
```

4. Upload with SteamCMD:

```powershell
steamcmd +login <steam_user> +run_app_build_http "app_build_the_axiom.vdf" +quit
```

Do not include `steam_appid.txt` in the depot. The depot template excludes it, and the release builder removes it from the local build output if found.
