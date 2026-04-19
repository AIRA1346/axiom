GSI audio asset layout (add clips here, reference from code or inspectors).

Mixers/   — Unity AudioMixer assets (master / SFX / music routing), optional follow-up
Music/    — looping BGM clips
Sfx/      — one-shots (gameplay hits, feedback)
UI/       — button / panel sounds (can alias ThirdParty packs under Assets/ThirdParty)

Volume: GsiUserSettings (master / SFX / music) → GsiAudioService applies to AudioSources; AudioListener.volume = master.
