# Steam Store Page Checklist

Use this before requesting Steam review.

## Required Assets

- [ ] Header capsule
- [ ] Small capsule
- [ ] Main capsule
- [ ] Vertical capsule
- [ ] Page background
- [ ] Library capsule
- [ ] Library hero
- [ ] Logo
- [ ] At least 5 current gameplay screenshots
- [ ] Trailer or gameplay video

## Copy

- [ ] Short description in English
- [ ] Short description in Korean
- [ ] Long description in English
- [ ] Long description in Korean
- [ ] Feature bullets
- [ ] Supported languages match the in-game localization
- [ ] System requirements for Windows
- [ ] Support contact or support URL

## Steamworks Settings

- [ ] App ID is final: `1628285`
- [ ] Demo App ID is noted if a demo is planned: `1628286`
- [ ] Windows depot ID is final
- [ ] Launch option points to `TheAxiom.exe`
- [ ] Steam Cloud is enabled
- [ ] Steam Cloud quota covers `gsi_steamcloud_v1.json`
- [ ] Achievements/leaderboards are either configured or intentionally omitted
- [ ] Store tags match gameplay
- [ ] Release date and visibility are configured
- [ ] Pricing package is configured or hidden intentionally
- [ ] Community hub visibility is configured
- [ ] Support URL/email matches `STEAM_SUPPORT_RUNBOOK.md`

## Review Build

- [ ] Beta build was created with `Tools/GSI/Steam/Build Windows Steam Beta`
- [ ] Beta validation passes: `Tools/Validate-SteamRelease.ps1 -BetaRelease -BuildOutput "Builds/SteamWindows"`
- [ ] Final release validation passes: `Tools/Validate-SteamRelease.ps1 -StrictRelease -BuildOutput "Builds/SteamWindows"`
- [ ] Depot candidate contains no `steam_appid.txt`
- [ ] Steam client launch from depot works
- [ ] Overlay opens
- [ ] Cloud sync works across two machines or clean Windows profiles
