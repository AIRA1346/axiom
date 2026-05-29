using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Wires default click/hover SFX on UI <see cref="Button"/> instances in loaded scenes.
/// For buttons created at runtime, also call <see cref="GsiUiSound.WireButtonIfNeeded"/> or <see cref="GsiUiSound.WireButtonsUnder"/>.
/// </summary>
[DefaultExecutionOrder(-20)]
public sealed class GsiUiInteractionSound : MonoBehaviour
{
    private const string DefaultSettingsResourcePath = "GsiDefaultUiSounds";

    [SerializeField] private GsiUiSoundSettings _settings;

    private static GsiUiInteractionSound _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        ApplyConfiguredOrDefaultSettings();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(WireAfterFrame());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(WireAfterFrame());
    }

    private IEnumerator WireAfterFrame()
    {
        yield return null;
        WireAllSceneButtons();
    }

    private void ApplyConfiguredOrDefaultSettings()
    {
        GsiUiSoundSettings s = _settings;
        if (s == null)
        {
            s = Resources.Load<GsiUiSoundSettings>(DefaultSettingsResourcePath);
        }

        GsiUiSound.ApplySettings(s);
    }

    private static void WireAllSceneButtons()
    {
        Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];
            if (b == null)
            {
                continue;
            }

            GameObject go = b.gameObject;
            if (!go.scene.IsValid() || !go.scene.isLoaded)
            {
                continue;
            }

            if ((go.hideFlags & HideFlags.HideAndDontSave) != 0)
            {
                continue;
            }

            GsiUiSound.WireButtonIfNeeded(b);
        }
    }
}
