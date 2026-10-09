using UnityEngine;

/// <summary>
/// UI 효과음을 AudioManager로 재생한다. 매니저가 없으면 소리 없이 넘어간다.
/// </summary>
public static class UiSound
{
    /// <summary>
    /// 재생한 소리를 돌려준다. 재생하지 못했으면 null.
    /// </summary>
    public static AudioSource Play(string name)
    {
        if (string.IsNullOrEmpty(name) || Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return null;
        }

        return audioManager.PlaySound(name);
    }

    /// <summary>
    /// 성공하면 success, 실패하면 failure를 재생한다. null이면 그쪽은 소리를 내지 않는다.
    /// </summary>
    public static void PlayResult(bool succeeded, string success, string failure)
    {
        Play(succeeded ? success : failure);
    }
}
