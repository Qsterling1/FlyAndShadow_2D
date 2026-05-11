using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class QualityMenuOption
{
    [Tooltip("Text shown on the quality button.")]
    [SerializeField] private string _label;

    [Tooltip("Index from Project Settings > Quality.")]
    [SerializeField] private int _qualityIndex;

    public string Label => _label;
    public int QualityIndex => _qualityIndex;
}

public class MainMenuSettingsController : MonoBehaviour
{
    private const string QualityKey = "settings.quality";
    private const string FullscreenKey = "settings.fullscreen";

    [Header("Panel")]
    [Tooltip("The full settings overlay shown from the Main Menu.")]
    [SerializeField] private GameObject _settingsOverlay;
    [Tooltip("Existing Main Menu Settings button.")]
    [SerializeField] private Button _settingsButton;
    [Tooltip("Button that closes the settings overlay.")]
    [SerializeField] private Button _backButton;
    [Tooltip("Button that restores default settings.")]
    [SerializeField] private Button _resetButton;

    [Header("Audio Controls")]
    [Tooltip("Master volume slider.")]
    [SerializeField] private Slider _masterVolumeSlider;
    [Tooltip("Music volume slider.")]
    [SerializeField] private Slider _musicVolumeSlider;
    [Tooltip("Sound effects volume slider.")]
    [SerializeField] private Slider _sfxVolumeSlider;
    [Tooltip("Music enabled toggle. On means music plays.")]
    [SerializeField] private Toggle _musicToggle;
    [Tooltip("Sound effects enabled toggle. On means sound effects play.")]
    [SerializeField] private Toggle _sfxToggle;

    [Header("Display Controls")]
    [Tooltip("Fullscreen enabled toggle.")]
    [SerializeField] private Toggle _fullscreenToggle;
    [Tooltip("Button used to cycle quality options.")]
    [SerializeField] private Button _qualityButton;
    [Tooltip("Label updated with the current quality option.")]
    [SerializeField] private TextMeshProUGUI _qualityLabel;
    [Tooltip("Quality options available from this menu.")]
    [SerializeField] private List<QualityMenuOption> _qualityOptions = new List<QualityMenuOption>();

    private int _qualityOptionIndex;

    private void Awake()
    {
        HideSettings();
        LoadDisplaySettings();
    }

    private void OnEnable()
    {
        RegisterListeners();
    }

    private void OnDisable()
    {
        UnregisterListeners();
    }

    public void ShowSettings()
    {
        SyncControls();
        SetOverlayActive(true);
    }

    public void HideSettings()
    {
        SetOverlayActive(false);
    }

    private void RegisterListeners()
    {
        if (_settingsButton != null) _settingsButton.onClick.AddListener(ShowSettings);
        if (_backButton != null) _backButton.onClick.AddListener(HideSettings);
        if (_resetButton != null) _resetButton.onClick.AddListener(ResetSettings);
        if (_qualityButton != null) _qualityButton.onClick.AddListener(CycleQuality);
        if (_masterVolumeSlider != null) _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (_musicVolumeSlider != null) _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (_sfxVolumeSlider != null) _sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        if (_musicToggle != null) _musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        if (_sfxToggle != null) _sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);
        if (_fullscreenToggle != null) _fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
    }

    private void UnregisterListeners()
    {
        if (_settingsButton != null) _settingsButton.onClick.RemoveListener(ShowSettings);
        if (_backButton != null) _backButton.onClick.RemoveListener(HideSettings);
        if (_resetButton != null) _resetButton.onClick.RemoveListener(ResetSettings);
        if (_qualityButton != null) _qualityButton.onClick.RemoveListener(CycleQuality);
        if (_masterVolumeSlider != null) _masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        if (_musicVolumeSlider != null) _musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        if (_sfxVolumeSlider != null) _sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        if (_musicToggle != null) _musicToggle.onValueChanged.RemoveListener(OnMusicToggleChanged);
        if (_sfxToggle != null) _sfxToggle.onValueChanged.RemoveListener(OnSfxToggleChanged);
        if (_fullscreenToggle != null) _fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
    }

    private void SyncControls()
    {
        AudioManager audio = AudioManager.instance;
        if (audio != null)
        {
            if (_masterVolumeSlider != null) _masterVolumeSlider.SetValueWithoutNotify(audio.MasterVolume);
            if (_musicVolumeSlider != null) _musicVolumeSlider.SetValueWithoutNotify(audio.MusicVolume);
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.SetValueWithoutNotify(audio.SfxVolume);
            if (_musicToggle != null) _musicToggle.SetIsOnWithoutNotify(!audio.MusicMuted);
            if (_sfxToggle != null) _sfxToggle.SetIsOnWithoutNotify(!audio.SfxMuted);
        }

        if (_fullscreenToggle != null)
        {
            _fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        }

        SetQualityOptionFromCurrentQuality();
        UpdateQualityLabel();
    }

    private void LoadDisplaySettings()
    {
        int savedQuality = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(Mathf.Clamp(savedQuality, 0, QualitySettings.names.Length - 1));

        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreen = fullscreen;
    }

    private void OnMasterVolumeChanged(float value)
    {
        AudioManager.instance?.SetMasterVolume(value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        AudioManager.instance?.SetMusicVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        AudioManager.instance?.SetSfxVolume(value);
    }

    private void OnMusicToggleChanged(bool isOn)
    {
        AudioManager.instance?.SetMusicMuted(!isOn);
    }

    private void OnSfxToggleChanged(bool isOn)
    {
        AudioManager.instance?.SetSfxMuted(!isOn);
    }

    private void OnFullscreenChanged(bool isOn)
    {
        Screen.fullScreen = isOn;
        PlayerPrefs.SetInt(FullscreenKey, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void CycleQuality()
    {
        if (_qualityOptions.Count == 0)
        {
            return;
        }

        _qualityOptionIndex = (_qualityOptionIndex + 1) % _qualityOptions.Count;
        ApplyQualityOption();
    }

    private void ResetSettings()
    {
        AudioManager.instance?.ResetAudioSettings();

        if (_qualityOptions.Count > 0)
        {
            _qualityOptionIndex = Mathf.Clamp(_qualityOptionIndex, 0, _qualityOptions.Count - 1);
            ApplyQualityOption();
        }

        Screen.fullScreen = true;
        PlayerPrefs.SetInt(FullscreenKey, 1);
        PlayerPrefs.Save();
        SyncControls();
    }

    private void ApplyQualityOption()
    {
        QualityMenuOption option = _qualityOptions[_qualityOptionIndex];
        int qualityIndex = Mathf.Clamp(option.QualityIndex, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt(QualityKey, qualityIndex);
        PlayerPrefs.Save();
        UpdateQualityLabel();
    }

    private void SetQualityOptionFromCurrentQuality()
    {
        int currentQuality = QualitySettings.GetQualityLevel();
        for (int i = 0; i < _qualityOptions.Count; i++)
        {
            if (_qualityOptions[i].QualityIndex == currentQuality)
            {
                _qualityOptionIndex = i;
                return;
            }
        }

        _qualityOptionIndex = 0;
    }

    private void UpdateQualityLabel()
    {
        if (_qualityLabel == null || _qualityOptions.Count == 0)
        {
            return;
        }

        _qualityOptionIndex = Mathf.Clamp(_qualityOptionIndex, 0, _qualityOptions.Count - 1);
        _qualityLabel.text = _qualityOptions[_qualityOptionIndex].Label;
    }

    private void SetOverlayActive(bool isActive)
    {
        if (_settingsOverlay != null && _settingsOverlay.activeSelf != isActive)
        {
            _settingsOverlay.SetActive(isActive);
        }
    }
}
