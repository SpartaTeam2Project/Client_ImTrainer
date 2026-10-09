using System.Globalization;
using UnityEngine;

/// <summary>
/// 결과 화면 숫자 표기와 DPS, 비율 계산.
/// </summary>
public static class StageResultFormat
{
    private const float THOUSAND = 1000f;
    private const float MILLION = 1000000f;

    public static string Value(float value, StageStatFormat format)
    {
        switch (format)
        {
            case StageStatFormat.Decimal:
                return value.ToString("0.#", CultureInfo.InvariantCulture);
            case StageStatFormat.Ratio:
                return Mathf.RoundToInt(value * 100f) + "%";
            case StageStatFormat.Percent:
                return Mathf.RoundToInt(value) + "%";
            case StageStatFormat.Abbreviated:
                return Abbreviate(value);
            default:
                return Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// 1000 미만은 정수, 그 위는 12.3K, 1.23M으로 줄인다.
    /// </summary>
    public static string Abbreviate(float value)
    {
        if (value >= MILLION)
        {
            return (value / MILLION).ToString("0.##", CultureInfo.InvariantCulture) + "M";
        }

        if (value >= THOUSAND)
        {
            return (value / THOUSAND).ToString("0.#", CultureInfo.InvariantCulture) + "K";
        }

        return Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 초를 mm:ss로 적는다.
    /// </summary>
    public static string Time(float seconds)
    {
        var total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    public static float PerSecond(float amount, float seconds)
    {
        return seconds > 0f ? amount / seconds : 0f;
    }

    public static float Ratio(float part, float whole)
    {
        return whole > 0f ? Mathf.Clamp01(part / whole) : 0f;
    }
}
