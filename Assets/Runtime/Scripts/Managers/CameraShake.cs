using UnityEngine;

/// <summary>
/// 남은 시간에 비례해 줄어드는 카메라 흔들림 오프셋을 만든다.
/// </summary>
public class CameraShake
{
    private float _strength;
    private float _duration;
    private float _remaining;

    /// <summary>
    /// 흔들림을 시작한다. 이미 흔들리는 중이면 더 센 쪽을 남긴다.
    /// </summary>
    public void Start(float strength, float duration)
    {
        if (strength <= 0f || duration <= 0f)
        {
            return;
        }

        var current = _remaining > 0f ? _strength * (_remaining / _duration) : 0f;
        if (current > strength)
        {
            return;
        }

        _strength = strength;
        _duration = duration;
        _remaining = duration;
    }

    /// <summary>
    /// 이번 프레임의 오프셋. 흔들림이 끝났으면 0이다.
    /// </summary>
    public Vector2 Sample(float deltaTime)
    {
        if (_remaining <= 0f)
        {
            return Vector2.zero;
        }

        _remaining = Mathf.Max(0f, _remaining - deltaTime);
        return Random.insideUnitCircle * (_strength * (_remaining / _duration));
    }

    /// <summary>
    /// 흔들림을 바로 멈춘다.
    /// </summary>
    public void Clear()
    {
        _remaining = 0f;
    }
}
