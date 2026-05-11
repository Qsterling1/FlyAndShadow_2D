using FlyShadow.EventBus;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleSoundPlayer : MonoBehaviour
{
    [Header("Audio Feedback")]
    [Tooltip("Sound cue name published when this toggle changes.")]
    [SerializeField] private string _soundName = "ButtonClick";
    [Tooltip("Allows this UI sound to play even when SFX is toggled off.")]
    [SerializeField] private bool _ignoreSfxMute = true;
    [Tooltip("Allows sound during setup changes before Start has run.")]
    [SerializeField] private bool _playBeforeStart;

    private Toggle _toggle;
    private bool _hasStarted;

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        if (_toggle != null)
        {
            _toggle.onValueChanged.AddListener(PlayToggleSound);
        }
    }

    private void Start()
    {
        _hasStarted = true;
    }

    private void OnDisable()
    {
        if (_toggle != null)
        {
            _toggle.onValueChanged.RemoveListener(PlayToggleSound);
        }
    }

    private void PlayToggleSound(bool isOn)
    {
        if (!_hasStarted && !_playBeforeStart)
        {
            return;
        }

        EventManager.Publish(new PlaySoundEvent { SoundName = _soundName, IgnoreSfxMute = _ignoreSfxMute });
    }
}
