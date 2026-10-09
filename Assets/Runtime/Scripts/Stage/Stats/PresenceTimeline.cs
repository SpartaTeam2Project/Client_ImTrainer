using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 포켓몬 한 마리가 파티에 있던 구간 목록.
/// </summary>
internal sealed class PresenceTimeline
{
    private const float CLOSED = -1f;

    private readonly List<Vector2> _intervals = new List<Vector2>();
    private float _openSince = CLOSED;

    public bool IsOpen => _openSince >= 0f;

    public void Open(float time)
    {
        if (!IsOpen)
        {
            _openSince = Mathf.Max(0f, time);
        }
    }

    public void Close(float time)
    {
        if (!IsOpen)
        {
            return;
        }

        _intervals.Add(new Vector2(_openSince, Mathf.Max(_openSince, time)));
        _openSince = CLOSED;
    }

    /// <summary>
    /// 파티에 있던 전체 시간. 열린 구간은 지금까지 센다.
    /// </summary>
    public float Total(float now)
    {
        return Overlap(0f, now, now);
    }

    /// <summary>
    /// [from, to] 안에서 파티에 있던 시간.
    /// </summary>
    public float Overlap(float from, float to, float now)
    {
        var total = 0f;
        for (var i = 0; i < _intervals.Count; i++)
        {
            total += Clip(_intervals[i].x, _intervals[i].y, from, to);
        }

        if (IsOpen)
        {
            total += Clip(_openSince, now, from, to);
        }

        return total;
    }

    private static float Clip(float start, float end, float from, float to)
    {
        return Mathf.Max(0f, Mathf.Min(end, to) - Mathf.Max(start, from));
    }
}
