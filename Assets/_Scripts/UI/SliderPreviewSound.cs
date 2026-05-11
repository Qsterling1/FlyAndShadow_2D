using FlyShadow.EventBus;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderPreviewSound : MonoBehaviour
{
    [Header("Audio Feedback")]
    [Tooltip("Sound cue name published while this slider moves.")]
    [SerializeField] private string _soundName = "ButtonClick";
    [Tooltip("Allows this UI sound to play even when SFX is toggled off.")]
    [SerializeField] private bool _ignoreSfxMute;
    [Tooltip("Smallest time gap between preview sounds while dragging.")]
    [SerializeField] private float _minimumPreviewInterval = 0.12f;
    [Tooltip("Smallest value change needed before another preview sound can play.")]
    [SerializeField] private float _minimumValueDelta = 0.05f;
    [Tooltip("Allows sound during setup changes before Start has run.")]
    [SerializeField] private bool _playBeforeStart;

    private Slider _slider;
    private bool _hasStarted;
    private float _lastPreviewTime = -999f;
    private float _lastPreviewValue;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        _lastPreviewValue = _slider != null ? _slider.value : 0f;
    }

    private void OnEnable()
    {
        if (_slider != null)
        {
            _slider.onValueChanged.AddListener(PlayPreviewSound);
        }
    }

    private void Start()
    {
        _hasStarted = true;
    }

    private void OnDisable()
    {
        if (_slider != null)
        {
            _slider.onValueChanged.RemoveListener(PlayPreviewSound);
        }
    }

    private void PlayPreviewSound(float value)
    {
        if (!_hasStarted && !_playBeforeStart)
        {
            return;
        }

        if (Time.unscaledTime - _lastPreviewTime < _minimumPreviewInterval)
        {
            return;
        }

        if (Mathf.Abs(value - _lastPreviewValue) < _minimumValueDelta)
        {
            return;
        }

        _lastPreviewTime = Time.unscaledTime;
        _lastPreviewValue = value;
        EventManager.Publish(new PlaySoundEvent { SoundName = _soundName, IgnoreSfxMute = _ignoreSfxMute });
    }
}
