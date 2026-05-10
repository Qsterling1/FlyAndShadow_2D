using UnityEngine;
using UnityEngine.UI;
using FlyShadow.EventBus;

[RequireComponent(typeof(Button))]
public class ButtonSoundPlayer : MonoBehaviour
{
    private Button _button;
    
    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(PlayButtonSound);
    }

    private void PlayButtonSound()
    {
        EventManager.Publish(new PlaySoundEvent { SoundName = "ButtonClick" });
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(PlayButtonSound);
        }
    }
}