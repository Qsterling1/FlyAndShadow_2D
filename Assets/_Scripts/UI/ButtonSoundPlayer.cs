using UnityEngine;
using UnityEngine.UI;
using FlyShadow.EventBus;

[RequireComponent(typeof(Button))]
public class ButtonSoundPlayer : MonoBehaviour
{
    [Header("Audio Feedback")]
    [Tooltip("Sound cue name published when this button is clicked.")]
    [SerializeField] private string _soundName = "ButtonClick";
    [Tooltip("Allows this UI sound to play even when SFX is toggled off.")]
    [SerializeField] private bool _ignoreSfxMute;

    private Button _button;
    
    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(PlayButtonSound);
    }

    private void PlayButtonSound()
    {
        EventManager.Publish(new PlaySoundEvent { SoundName = _soundName, IgnoreSfxMute = _ignoreSfxMute });
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(PlayButtonSound);
        }
    }
}
