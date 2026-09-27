using UnityEngine;

/// <summary>
/// 경험치 구슬 프리팹에 붙여 획득음을 고른다.
/// </summary>
public class ExperienceSound : MonoBehaviour
{
    private const float DEFAULT_VOLUME = 1f;
    private const float DEFAULT_PITCH = 1f;

    [SerializeField] private AudioClip _clip;
    [SerializeField, Range(0f, 1f)] private float _volume = DEFAULT_VOLUME;
    [SerializeField] private float _pitch = DEFAULT_PITCH;

    /// <summary>
    /// 클립이 있으면 효과음으로 한 번 재생한다.
    /// </summary>
    public void Play()
    {
        if (_clip == null || Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(_clip, _volume, Mathf.Max(0.01f, _pitch));
    }
}
