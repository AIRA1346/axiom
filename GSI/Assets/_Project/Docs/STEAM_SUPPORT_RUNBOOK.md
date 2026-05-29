# Steam Support Runbook

This runbook covers the minimum player-support flow for the Windows Steam release.

## Player Log Location

Ask Windows players to attach:

```text
%USERPROFILE%\AppData\LocalLow\RunicAtelier\The Axiom\Player.log
```

If the game crashed, also ask for:

```text
%USERPROFILE%\AppData\Local\Temp\RunicAtelier\The Axiom\Crashes
```

The crash folder may not exist because Unity Cloud Diagnostics is disabled.

## First Response Checklist

- Confirm Steam client was running.
- Confirm the build branch and version shown in Steam.
- Ask whether `Steam Cloud` is enabled for the game and account.
- Ask whether the problem reproduces after restarting Steam.
- Ask for monitor resolution, fullscreen/windowed mode, input device, and language.

## Save/Cloud Issues

Important data is synchronized through `gsi_steamcloud_v1.json` in Steam Remote Storage.

The cloud snapshot includes:

- Best records and unified exam history
- Gold and exam tickets
- Practice grades per subject
- Volume settings
- Preferred language
- Default Dark/Light appearance mode
- Owned and equipped UI skins

For save problems, ask:

- Did the player use multiple PCs?
- Which PC had the newest progress?
- Was Steam offline or Cloud disabled?
- Did Steam show a Cloud conflict dialog?

Do not ask players to manually edit `PlayerPrefs` or the Steam Cloud JSON.

## Known Release Dependencies

- Steamworks must initialize through Steam, not direct EXE launch.
- The shipped depot must not contain `steam_appid.txt`.
- Steam Partner Cloud quota/path must be configured before public release.
- If asynchronous versus uses a production backend, verify the backend URL and Steam ID validation.

## Escalation Data

For bugs that cannot be reproduced, collect:

- `Player.log`
- Steam build ID / branch
- Locale (`en` or `ko-KR`)
- Resolution and fullscreen/windowed mode
- Steps from launch to issue
- Whether Steam overlay opens
- Whether Steam Cloud is enabled
