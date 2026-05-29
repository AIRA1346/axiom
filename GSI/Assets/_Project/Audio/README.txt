GSI audio asset layout (add clips here, reference from code or inspectors).

Mixers/   — Unity AudioMixer assets (master / SFX / music routing), optional follow-up
Music/    — looping BGM clips
Sfx/      — one-shots (gameplay hits, feedback)
UI/       — button / panel sounds (project-picked clips; symlink or duplicate from Packs/ as needed)
Packs/    — vendor SFX packs (CasualGameSoundsU6, FreeUIClickSounds, Leohpaz RPG Essentials Free, …)

Volume: GsiUserSettings (master / SFX / music) → GsiAudioService applies to AudioSources; AudioListener.volume = master.
