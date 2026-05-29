# Steam Release QA Matrix

Run this on a clean Windows profile before uploading a release candidate.

## Smoke Flow

- [ ] Launch from Steam client
- [ ] Intro loads first
- [ ] Skip intro works
- [ ] Lobby buttons are visible and clickable
- [ ] Settings opens with ESC
- [ ] Language switches between English and Korean
- [ ] Default/Ocean/Amber/Violet UI themes remain readable
- [ ] Quit to desktop exits in release build

## Gameplay Flow

- [ ] Enter G.S.I
- [ ] Start each practice subject
- [ ] Complete each practice subject
- [ ] Start unified official exam
- [ ] Spend one exam ticket
- [ ] Finish unified official exam
- [ ] Retry with enough tickets
- [ ] Retry with no tickets shows player-facing notice
- [ ] Result screen returns to main menu

## Economy And UI

- [ ] Gold and tickets update after rewards
- [ ] Shop purchase succeeds with enough gold
- [ ] Shop no-gold state shows player-facing notice
- [ ] Inventory equip works for owned skins
- [ ] Locked skins show clear state
- [ ] Altar of Verity lists completed unified exams

## Steam Features

- [ ] `steam_appid.txt` for local QA is `1628285`
- [ ] Steam overlay opens
- [ ] Rich Presence updates for lobby, exam, result
- [ ] Steam Cloud writes after progress
- [ ] Steam Cloud writes after changing settings/language/theme
- [ ] Steam Cloud writes after buying/equipping a skin
- [ ] Steam Cloud merges newer data on another machine/profile
- [ ] Gold, tickets, best records, language, theme, and equipped skin match after merge
- [ ] Account mismatch cloud data is ignored
- [ ] Launching EXE directly degrades gracefully

## Windows Compatibility

- [ ] 1920x1080 fullscreen
- [ ] 1920x1080 windowed
- [ ] 1366x768 windowed
- [ ] Alt+Tab out and back
- [ ] Mouse-only navigation
- [ ] Keyboard ESC settings navigation
- [ ] Gamepad Start opens settings

## Release Package

- [ ] `TheAxiom.exe` exists
- [ ] `TheAxiom_Data` exists
- [ ] `UnityPlayer.dll` exists
- [ ] Addressables/localization content included
- [ ] `steam_appid.txt` is not included
- [ ] No `.pdb`, `.mdb`, or logs included in depot unless intentionally shipping symbols

## Beta Specific

- [ ] `bundleVersion` may remain `0.x` for beta
- [ ] `Tools/Validate-SteamRelease.ps1 -BetaRelease -BuildOutput "Builds/SteamWindows"` passes
- [ ] Steam branch/password is configured if the beta is private
- [ ] Store visibility matches beta plan
- [ ] Known issues are listed in store/news/community post if needed

## Final Release Specific

- [ ] `bundleVersion` is final, for example `1.0.0`
- [ ] `Tools/Validate-SteamRelease.ps1 -StrictRelease -BuildOutput "Builds/SteamWindows"` passes
- [ ] EULA and privacy policy are final, not templates
- [ ] Store page assets are final
- [ ] Steam Cloud has been tested on two machines/profiles
