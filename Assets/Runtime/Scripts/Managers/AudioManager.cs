using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 효과음 풀과 음악 한 채널을 재생하고 볼륨을 로컬에 보관한다.
/// </summary>
public class AudioManager : BaseManager
{
    private const string SOUND_VOLUME_KEY = "Audio.SoundVolume";
    private const string MUSIC_VOLUME_KEY = "Audio.MusicVolume";
    private const int INITIAL_SOUND_POOL_SIZE = 2;
    private const float DEFAULT_VOLUME = 1f;

    [SerializeField] private AudioDatabase _database;

    private readonly Dictionary<string, AudioEntry> _musicEntries = new Dictionary<string, AudioEntry>();
    private readonly Dictionary<string, AudioEntry> _soundEntries = new Dictionary<string, AudioEntry>();
    private readonly List<AudioSource> _soundPool = new List<AudioSource>();
    private readonly List<PlayingSound> _playingSounds = new List<PlayingSound>();

    private AudioListener _audioListener;
    private AudioSource _musicSource;
    private float _musicBaseVolume;
    private float _soundVolume = DEFAULT_VOLUME;
    private float _musicVolume = DEFAULT_VOLUME;

    public float SoundVolume
    {
        get => _soundVolume;
        set
        {
            _soundVolume = Mathf.Clamp01(value);
            ApplySoundVolume();
            SaveVolume(SOUND_VOLUME_KEY, _soundVolume);
        }
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = Mathf.Clamp01(value);
            ApplyMusicVolume();
            SaveVolume(MUSIC_VOLUME_KEY, _musicVolume);
        }
    }

    private void Awake()
    {
        _audioListener = GetComponent<AudioListener>();
        if (_audioListener == null)
        {
            _audioListener = gameObject.AddComponent<AudioListener>();
        }
    }

    /// <summary>
    /// 저장된 볼륨을 읽고 효과음 풀과 음악 소스를 준비한다.
    /// </summary>
    public override UniTask InitializeAsync()
    {
        LoadVolumes();
        BuildLookup();
        CreateMusicSource();
        WarmSoundPool();
        return base.InitializeAsync();
    }

    private void Update()
    {
        ReleaseFinishedSounds();
    }

    /// <summary>
    /// 재생 중인 소리를 멈추고 풀을 비운다.
    /// </summary>
    public override void Cleanup()
    {
        StopMusic();

        for (var i = 0; i < _playingSounds.Count; i++)
        {
            ReleaseSound(_playingSounds[i].Source);
        }

        _playingSounds.Clear();
        _soundPool.Clear();
        _musicEntries.Clear();
        _soundEntries.Clear();
        base.Cleanup();
    }

    /// <summary>
    /// 데이터베이스에 등록된 효과음을 재생한다.
    /// </summary>
    public AudioSource PlaySound(string name)
    {
        if (!TryGetSound(name, out var entry))
        {
            return null;
        }

        if (entry.Clip == null)
        {
            Debug.LogWarning($"효과음 클립이 비어 있습니다: {name}");
            return null;
        }

        if (!entry.TryConsumeCooldown())
        {
            return null;
        }

        return PlaySound(entry.Clip, entry.Volume, entry.Pitch);
    }

    /// <summary>
    /// 클립을 효과음으로 한 번 재생한다.
    /// </summary>
    public AudioSource PlaySound(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("효과음 클립이 비어 있습니다.");
            return null;
        }

        var source = GetSoundSource();
        source.clip = clip;
        source.loop = false;
        source.pitch = pitch;
        source.volume = volume * _soundVolume;
        source.Play();

        _playingSounds.Add(new PlayingSound(source, volume));
        return source;
    }

    /// <summary>
    /// 데이터베이스에 등록된 음악을 재생한다. 이전 음악은 멈춘다.
    /// </summary>
    public AudioSource PlayMusic(string name)
    {
        if (!TryGetMusic(name, out var entry))
        {
            return null;
        }

        if (entry.Clip == null)
        {
            Debug.LogWarning($"음악 클립이 비어 있습니다: {name}");
            return null;
        }

        if (_musicSource == null)
        {
            CreateMusicSource();
        }

        _musicBaseVolume = entry.Volume;
        _musicSource.clip = entry.Clip;
        _musicSource.pitch = entry.Pitch;
        _musicSource.volume = _musicBaseVolume * _musicVolume;
        _musicSource.loop = true;
        _musicSource.Play();
        return _musicSource;
    }

    /// <summary>
    /// 재생 위치를 유지한 채 음악을 멈춘다.
    /// </summary>
    public void PauseMusic()
    {
        if (_musicSource == null)
        {
            return;
        }

        _musicSource.Pause();
    }

    /// <summary>
    /// 멈춰 둔 음악을 같은 위치에서 다시 튼다.
    /// </summary>
    public void ResumeMusic()
    {
        if (_musicSource == null)
        {
            return;
        }

        _musicSource.UnPause();
    }

    /// <summary>
    /// 재생 중인 음악을 멈춘다.
    /// </summary>
    public void StopMusic()
    {
        if (_musicSource == null)
        {
            return;
        }

        _musicSource.Stop();
        _musicSource.clip = null;
        _musicBaseVolume = 0f;
    }

    private void LoadVolumes()
    {
        _soundVolume = PlayerPrefs.GetFloat(SOUND_VOLUME_KEY, DEFAULT_VOLUME);
        _musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, DEFAULT_VOLUME);
        _soundVolume = Mathf.Clamp01(_soundVolume);
        _musicVolume = Mathf.Clamp01(_musicVolume);
    }

    private void BuildLookup()
    {
        _musicEntries.Clear();
        _soundEntries.Clear();

        if (_database == null)
        {
            Debug.LogError("AudioDatabase가 할당되지 않았습니다.");
            return;
        }

        AddEntries(_database.Music, _musicEntries, "BGM");
        AddEntries(_database.Sounds, _soundEntries, "효과음");
    }

    private static void AddEntries(IReadOnlyList<AudioEntry> source, Dictionary<string, AudioEntry> target, string category)
    {
        for (var i = 0; i < source.Count; i++)
        {
            var entry = source[i];
            if (entry == null || string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            if (target.ContainsKey(entry.Name))
            {
                Debug.LogError($"같은 이름의 {category}이 이미 있습니다: {entry.Name}");
                continue;
            }

            entry.ResetCooldown();
            target.Add(entry.Name, entry);
        }
    }

    private void WarmSoundPool()
    {
        for (var i = _soundPool.Count; i < INITIAL_SOUND_POOL_SIZE; i++)
        {
            _soundPool.Add(CreateSoundSource());
        }
    }

    private void CreateMusicSource()
    {
        if (_musicSource != null)
        {
            return;
        }

        var sourceObject = new GameObject("Music Source");
        sourceObject.transform.SetParent(transform, false);
        _musicSource = sourceObject.AddComponent<AudioSource>();
        _musicSource.playOnAwake = false;
        _musicSource.loop = true;
        _musicSource.spatialBlend = 0f;
    }

    private AudioSource CreateSoundSource()
    {
        var sourceObject = new GameObject("Sound Source");
        sourceObject.transform.SetParent(transform, false);
        var source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        sourceObject.SetActive(false);
        return source;
    }

    private AudioSource GetSoundSource()
    {
        for (var i = 0; i < _soundPool.Count; i++)
        {
            var source = _soundPool[i];
            if (!source.gameObject.activeSelf)
            {
                source.gameObject.SetActive(true);
                return source;
            }
        }

        var created = CreateSoundSource();
        created.gameObject.SetActive(true);
        _soundPool.Add(created);
        return created;
    }

    private void ReleaseFinishedSounds()
    {
        for (var i = _playingSounds.Count - 1; i >= 0; i--)
        {
            var playing = _playingSounds[i];
            if (playing.Source.isPlaying)
            {
                continue;
            }

            ReleaseSound(playing.Source);
            _playingSounds.RemoveAt(i);
        }
    }

    private void ReleaseSound(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
    }

    private void ApplySoundVolume()
    {
        for (var i = 0; i < _playingSounds.Count; i++)
        {
            var playing = _playingSounds[i];
            playing.Source.volume = playing.BaseVolume * _soundVolume;
        }
    }

    private void ApplyMusicVolume()
    {
        if (_musicSource == null)
        {
            return;
        }

        _musicSource.volume = _musicBaseVolume * _musicVolume;
    }

    private bool TryGetMusic(string name, out AudioEntry entry)
    {
        if (string.IsNullOrEmpty(name) || !_musicEntries.TryGetValue(name, out entry))
        {
            Debug.LogWarning($"음악을 찾을 수 없습니다: {name}");
            entry = null;
            return false;
        }

        return true;
    }

    private bool TryGetSound(string name, out AudioEntry entry)
    {
        if (string.IsNullOrEmpty(name) || !_soundEntries.TryGetValue(name, out entry))
        {
            Debug.LogWarning($"효과음을 찾을 수 없습니다: {name}");
            entry = null;
            return false;
        }

        return true;
    }

    private static void SaveVolume(string key, float volume)
    {
        PlayerPrefs.SetFloat(key, volume);
        PlayerPrefs.Save();
    }

    private sealed class PlayingSound
    {
        public PlayingSound(AudioSource source, float baseVolume)
        {
            Source = source;
            BaseVolume = baseVolume;
        }

        public AudioSource Source { get; }

        public float BaseVolume { get; }
    }
}
