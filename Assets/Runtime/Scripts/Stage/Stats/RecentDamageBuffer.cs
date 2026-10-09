using UnityEngine;

/// <summary>
/// 마지막 60초 피해를 초 단위 칸으로 모은다. 칸은 돌려 쓰고, 오래된 초가 들어 있으면 비우고 쓴다.
/// </summary>
internal sealed class RecentDamageBuffer
{
    public const int WINDOW_SECONDS = 60;

    private readonly float[] _damage = new float[WINDOW_SECONDS];
    private readonly int[] _seconds = new int[WINDOW_SECONDS];

    public RecentDamageBuffer()
    {
        for (var i = 0; i < _seconds.Length; i++)
        {
            _seconds[i] = -1;
        }
    }

    /// <summary>
    /// 최근 창이 시작하는 시각. 지금 초를 포함해 60칸을 덮는다.
    /// </summary>
    public static float WindowStart(float now)
    {
        return Mathf.Max(0f, Mathf.FloorToInt(now) - (WINDOW_SECONDS - 1));
    }

    public void Add(float time, float amount)
    {
        var second = Mathf.Max(0, Mathf.FloorToInt(time));
        var index = second % WINDOW_SECONDS;
        if (_seconds[index] != second)
        {
            _seconds[index] = second;
            _damage[index] = 0f;
        }

        _damage[index] += amount;
    }

    /// <summary>
    /// 지금 기준 마지막 60칸의 합.
    /// </summary>
    public float Sum(float now)
    {
        var current = Mathf.FloorToInt(now);
        var oldest = current - (WINDOW_SECONDS - 1);
        var total = 0f;
        for (var i = 0; i < WINDOW_SECONDS; i++)
        {
            if (_seconds[i] >= oldest && _seconds[i] <= current)
            {
                total += _damage[i];
            }
        }

        return total;
    }
}
