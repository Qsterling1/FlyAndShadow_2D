using System;
using System.Collections.Generic;
using System.Linq;
using FlyShadow.Audio.Strategies;
using FlyShadow.EventBus;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    private const string MasterVolumeKey = "settings.masterVolume";
    private const string MusicVolumeKey = "settings.musicVolume";
    private const string SfxVolumeKey = "settings.sfxVolume";
    private const string MusicMutedKey = "settings.musicMuted";
    private const string SfxMutedKey = "settings.sfxMuted";

    [Header("Music Handling")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private List<AudioCue> _gameplayMusicPlaylist;
    [SerializeField] private AudioCue _mainMenuMusicCue;
    [SerializeField] private AudioCue _gameplayMusicCue;
    [SerializeField] private AudioCue _levelCompleteMusicCue;
    [SerializeField] private AudioCue _gameOverMusicCue;

    [Header("Audio Registry")]
    [SerializeField] private AudioRegistry _registry;

    [Header("Audio Cue Library")]
    public List<AudioCue> AudioCues = new List<AudioCue>();

    [Header("Audio Source Pool")]
    [SerializeField] private int _sfxPoolSize = 10;

    private readonly List<AudioSource> _sfxSourcePool = new List<AudioSource>();
    private readonly Dictionary<AudioRegistry.Binding, int> _strategyState = new Dictionary<AudioRegistry.Binding, int>();
    private readonly List<(GameEvent gameEvent, Action handler)> _registryHandlers = new List<(GameEvent, Action)>();
    private readonly System.Random _random = new System.Random();
    private AudioCue _currentMusicCue;
    private float _masterVolume = 1f;
    private float _musicVolume = 1f;
    private float _sfxVolume = 1f;
    private bool _musicMuted;
    private bool _sfxMuted;

    public float MasterVolume => _masterVolume;
    public float MusicVolume => _musicVolume;
    public float SfxVolume => _sfxVolume;
    public bool MusicMuted => _musicMuted;
    public bool SfxMuted => _sfxMuted;

    private static readonly Dictionary<PlaybackStrategyType, IPlaybackStrategy> _playbackStrategies = new Dictionary<PlaybackStrategyType, IPlaybackStrategy>
    {
        { PlaybackStrategyType.FirstValid, new FirstValidStrategy() },
        { PlaybackStrategyType.Sequential, new SequentialStrategy() },
        { PlaybackStrategyType.WeightedRandom, new WeightedRandomStrategy() }
    };

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeAudioSourcePool();

        if (_musicSource == null)
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
        }

        LoadAudioSettings();

        RegisterRegistryBindings();
        EventManager.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventManager.Subscribe<PlaySoundEvent>(OnPlaySoundRequested);
    }

    private void OnDestroy()
    {
        UnregisterRegistryBindings();
        EventManager.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventManager.Unsubscribe<PlaySoundEvent>(OnPlaySoundRequested);

        if (instance == this)
        {
            instance = null;
        }
    }
private void Start()
{
    PlayMusic(_mainMenuMusicCue);
}

    private void InitializeAudioSourcePool()
    {
        _sfxSourcePool.Clear();

        for (int i = 0; i < _sfxPoolSize; i++)
        {
            var newSource = gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newSource.loop = false;
            _sfxSourcePool.Add(newSource);
        }

        _sfxPoolIndex = 0;
    }

    private void RegisterRegistryBindings()
    {
        _registryHandlers.Clear();
        _strategyState.Clear();

        if (_registry == null)
        {
            return;
        }

        var bindings = _registry.Bindings;
        if (bindings == null)
        {
            return;
        }

        foreach (var binding in bindings)
        {
            if (binding == null)
            {
                continue;
            }

            var trigger = binding.Trigger;
            var variations = binding.Variations;

            if (trigger == null || variations == null || variations.Length == 0)
            {
                continue;
            }

            var hasValidCue = variations.Any(v => v != null && v.Cue != null);
            if (!hasValidCue)
            {
                continue;
            }

            var localBinding = binding;
            Action handler = () => HandleBinding(localBinding);
            trigger.RegisterListener(handler);
            _registryHandlers.Add((trigger, handler));
            _strategyState[localBinding] = 0;
        }
    }

    private void UnregisterRegistryBindings()
    {
        for (int i = 0; i < _registryHandlers.Count; i++)
        {
            var (gameEvent, handler) = _registryHandlers[i];
            if (gameEvent != null && handler != null)
            {
                gameEvent.UnregisterListener(handler);
            }
        }

        _registryHandlers.Clear();
        _strategyState.Clear();
    }

    private void HandleBinding(AudioRegistry.Binding binding)
    {
        var cue = ResolveCue(binding);
        if (cue == null)
        {
            return;
        }

        if (binding.IsMusic)
        {
            PlayMusic(cue);
        }
        else
        {
            PlaySound(cue);
        }
    }

    private AudioCue ResolveCue(AudioRegistry.Binding binding)
    {
        if (binding == null)
        {
            return null;
        }

        var variations = binding.Variations;
        if (variations == null || variations.Length == 0)
        {
            return null;
        }

        if (!_playbackStrategies.TryGetValue(binding.PlaybackStrategy, out var strategy))
        {
            strategy = _playbackStrategies[PlaybackStrategyType.FirstValid];
        }

        if (!_strategyState.TryGetValue(binding, out var state))
        {
            state = 0;
        }

        var cue = strategy.SelectCue(variations, ref state, _random);
        _strategyState[binding] = state;
        return cue;
    }

    private void OnPlaySoundRequested(PlaySoundEvent payload)
    {
        AudioCue cue = payload.Cue;
        if (cue == null && !string.IsNullOrEmpty(payload.SoundName))
        {
            cue = AudioCues.FirstOrDefault(c => c != null && string.Equals(c.Name, payload.SoundName, StringComparison.OrdinalIgnoreCase));
        }

        if (cue != null)
        {
            PlaySound(cue, payload.IgnoreSfxMute);
        }
    }

    private void OnGameStateChanged(GameStateChangedEvent payload)
    {
        switch (payload.State)
        {
            case GameState.MainMenu:
                PlayMusic(_mainMenuMusicCue);
                break;
          case GameState.Playing:
    if (_gameplayMusicCue != null)
    {
        PlayMusic(_gameplayMusicCue);
    }
    else
    {
        PlayRandomGameplayMusic();
    }
    break;

            case GameState.LevelComplete:
                PlayMusic(_levelCompleteMusicCue);
                break;
            case GameState.GameOver:
                PlayMusic(_gameOverMusicCue);
                break;
        }
    }

    public void PlaySound(AudioCue cue)
    {
        PlaySound(cue, false);
    }

    public void PlaySound(AudioCue cue, bool ignoreSfxMute)
    {
        if (cue == null || cue.Clip == null || _sfxSourcePool.Count == 0)
        {
            return;
        }

        var source = _sfxSourcePool[_sfxPoolIndex];
        _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxSourcePool.Count;

        if (source.isPlaying)
        {
            source.Stop();
        }

        float channelVolume = ignoreSfxMute ? _masterVolume * _sfxVolume : GetEffectiveSfxVolume();
        ConfigureSource(source, cue, channelVolume);

        if (cue.Loop)
        {
            source.Play();
        }
        else
        {
            source.PlayOneShot(source.clip);
        }
    }

    public void PlayMusic(AudioCue musicCue)
    {
        if (musicCue == null || musicCue.Clip == null)
        {
            return;
        }

        _currentMusicCue = musicCue;
        ConfigureSource(_musicSource, musicCue, GetEffectiveMusicVolume());
        _musicSource.loop = musicCue.Loop;
        _musicSource.Play();
    }

    public void PlayRandomGameplayMusic()
    {
        if (_gameplayMusicPlaylist == null || _gameplayMusicPlaylist.Count == 0)
        {
            return;
        }

        var randomTrack = _gameplayMusicPlaylist[UnityEngine.Random.Range(0, _gameplayMusicPlaylist.Count)];
        PlayMusic(randomTrack);
    }

    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MasterVolumeKey, _masterVolume);
        ApplyAudioSettings();
    }

    public void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, _musicVolume);
        ApplyAudioSettings();
    }

    public void SetSfxVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxVolumeKey, _sfxVolume);
        ApplyAudioSettings();
    }

    public void SetMusicMuted(bool muted)
    {
        _musicMuted = muted;
        PlayerPrefs.SetInt(MusicMutedKey, _musicMuted ? 1 : 0);
        ApplyAudioSettings();
    }

    public void SetSfxMuted(bool muted)
    {
        _sfxMuted = muted;
        PlayerPrefs.SetInt(SfxMutedKey, _sfxMuted ? 1 : 0);
        ApplyAudioSettings();
    }

    public void ResetAudioSettings()
    {
        _masterVolume = 1f;
        _musicVolume = 1f;
        _sfxVolume = 1f;
        _musicMuted = false;
        _sfxMuted = false;

        PlayerPrefs.SetFloat(MasterVolumeKey, _masterVolume);
        PlayerPrefs.SetFloat(MusicVolumeKey, _musicVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, _sfxVolume);
        PlayerPrefs.SetInt(MusicMutedKey, 0);
        PlayerPrefs.SetInt(SfxMutedKey, 0);
        ApplyAudioSettings();
    }

    private void LoadAudioSettings()
    {
        _masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        _musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        _sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        _musicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
        _sfxMuted = PlayerPrefs.GetInt(SfxMutedKey, 0) == 1;
        ApplyAudioSettings();
    }

    private void ApplyAudioSettings()
    {
        if (_musicSource != null && _currentMusicCue != null)
        {
            _musicSource.volume = _currentMusicCue.Volume * GetEffectiveMusicVolume();
        }

        float sfxVolume = GetEffectiveSfxVolume();
        foreach (var source in _sfxSourcePool)
        {
            if (source != null)
            {
                source.volume = sfxVolume;
            }
        }

        PlayerPrefs.Save();
    }

    private float GetEffectiveMusicVolume()
    {
        return _musicMuted ? 0f : _masterVolume * _musicVolume;
    }

    private float GetEffectiveSfxVolume()
    {
        return _sfxMuted ? 0f : _masterVolume * _sfxVolume;
    }

    private static void ConfigureSource(AudioSource source, AudioCue cue, float channelVolume)
    {
        if (source == null || cue == null)
        {
            return;
        }

        source.clip = cue.Clip;
        source.volume = cue.Volume * channelVolume;
        source.pitch = cue.Pitch;
        source.loop = cue.Loop;
    }

    private int _sfxPoolIndex;

    private sealed class FirstValidStrategy : IPlaybackStrategy
    {
        public AudioCue SelectCue(AudioVariation[] variations, ref int state, System.Random random)
        {
            if (variations == null)
            {
                return null;
            }

            for (int i = 0; i < variations.Length; i++)
            {
                var variation = variations[i];
                if (variation?.Cue != null)
                {
                    return variation.Cue;
                }
            }

            return null;
        }
    }

    private sealed class SequentialStrategy : IPlaybackStrategy
    {
        public AudioCue SelectCue(AudioVariation[] variations, ref int state, System.Random random)
        {
            if (variations == null || variations.Length == 0)
            {
                return null;
            }

            var length = variations.Length;
            if (length == 0)
            {
                return null;
            }

            state = Mathf.Clamp(state, 0, length - 1);

            for (int i = 0; i < length; i++)
            {
                var index = (state + i) % length;
                var variation = variations[index];
                if (variation?.Cue == null)
                {
                    continue;
                }

                state = (index + 1) % length;
                return variation.Cue;
            }

            return null;
        }
    }

    private sealed class WeightedRandomStrategy : IPlaybackStrategy
    {
        public AudioCue SelectCue(AudioVariation[] variations, ref int state, System.Random random)
        {
            if (variations == null || variations.Length == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            var cumulative = new List<(float threshold, AudioCue cue)>();

            for (int i = 0; i < variations.Length; i++)
            {
                var variation = variations[i];
                if (variation?.Cue == null)
                {
                    continue;
                }

                var weight = Mathf.Max(0f, variation.Weight);
                if (weight <= 0f)
                {
                    continue;
                }

                totalWeight += weight;
                cumulative.Add((totalWeight, variation.Cue));
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            var generator = random ?? new System.Random();
            float sample = (float)generator.NextDouble() * totalWeight;

            for (int i = 0; i < cumulative.Count; i++)
            {
                if (sample <= cumulative[i].threshold)
                {
                    return cumulative[i].cue;
                }
            }

            return cumulative.Count > 0 ? cumulative[cumulative.Count - 1].cue : null;
        }
    }
}
