using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 재생 시 최소값과 최대값 사이에서 하나를 고르는 범위다.
/// </summary>
[Serializable]
public struct AudioFloatRange
{
    [SerializeField] private float _min;
    [SerializeField] private float _max;

    public AudioFloatRange(float min, float max)
    {
        _min = min;
        _max = max;
    }

    public float Min => _min;

    public float Max => _max;

    public float Pick()
    {
        return UnityEngine.Random.Range(_min, _max);
    }
}

/// <summary>
/// BGM과 효과음을 이름별로 나눠 보관한다.
/// </summary>
[CreateAssetMenu(fileName = "AudioDatabase", menuName = "Audio/Audio Database")]
public class AudioDatabase : ScriptableObject
{
    [Header("BGM")]
    [FormerlySerializedAs("_entries")]
    [SerializeField] private List<AudioEntry> _music = new List<AudioEntry>();

    [Header("효과음")]
    [SerializeField] private List<AudioEntry> _sounds = new List<AudioEntry>();

    public IReadOnlyList<AudioEntry> Music => _music;

    public IReadOnlyList<AudioEntry> Sounds => _sounds;
}

/// <summary>
/// 하나의 오디오 클립과 볼륨, 피치, 재생 간격이다.
/// </summary>
[Serializable]
public class AudioEntry
{
    [SerializeField] private string _name;
    [SerializeField] private AudioClip _clip;
    [SerializeField] private AudioFloatRange _volume = new AudioFloatRange(1f, 1f);
    [SerializeField] private AudioFloatRange _pitch = new AudioFloatRange(1f, 1f);
    [SerializeField, Range(0f, 1f)] private float _cooldown;

    private float _lastPlayedTime = -1f;

    public string Name => _name;

    public AudioClip Clip => _clip;

    public float Volume => _volume.Pick();

    public float Pitch => _pitch.Pick();

    /// <summary>
    /// 재생 간격을 초기화한다.
    /// </summary>
    public void ResetCooldown()
    {
        _lastPlayedTime = -1f;
    }

    /// <summary>
    /// 최소 간격이 지났으면 재생 시각을 기록하고 true를 반환한다.
    /// </summary>
    public bool TryConsumeCooldown()
    {
        if (_cooldown > 0f && Time.unscaledTime <= _lastPlayedTime + _cooldown)
        {
            return false;
        }

        _lastPlayedTime = Time.unscaledTime;
        return true;
    }
}
