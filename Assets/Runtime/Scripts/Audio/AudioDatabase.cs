using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이름별 효과음·음악 클립과 재생 파라미터를 보관한다.
/// </summary>
[CreateAssetMenu(fileName = "AudioDatabase", menuName = "Audio/Audio Database")]
public class AudioDatabase : ScriptableObject
{
    [SerializeField] private List<AudioEntry> _entries = new List<AudioEntry>();

    public IReadOnlyList<AudioEntry> Entries => _entries;
}

/// <summary>
/// 하나의 오디오 클립과 볼륨, 피치, 재생 간격이다.
/// </summary>
[Serializable]
public class AudioEntry
{
    [SerializeField] private string _name;
    [SerializeField] private AudioClip _clip;
    [SerializeField] private Vector2 _volume = Vector2.one;
    [SerializeField] private Vector2 _pitch = Vector2.one;
    [SerializeField, Range(0f, 1f)] private float _cooldown;

    private float _lastPlayedTime = -1f;

    public string Name => _name;

    public AudioClip Clip => _clip;

    public float Volume => UnityEngine.Random.Range(_volume.x, _volume.y);

    public float Pitch => UnityEngine.Random.Range(_pitch.x, _pitch.y);

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
