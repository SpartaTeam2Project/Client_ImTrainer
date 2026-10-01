using UnityEngine;

/// <summary>
/// 방어 타입을 곱한 배율 칸. 0, 0.25, 0.5, 1, 2, 4배만 구분한다.
/// </summary>
public enum DamageTextKind
{
    Immune,
    VeryResisted,
    Resisted,
    Neutral,
    Super,
    VerySuper
}

/// <summary>
/// 월드 좌표에 피해 숫자나 무효 문장을 띄우라는 요청.
/// </summary>
public struct DamageTextRequested
{
    public Vector2 WorldPosition;
    public string Text;
    public DamageTextKind Kind;

    public DamageTextRequested(Vector2 worldPosition, string text, DamageTextKind kind)
    {
        WorldPosition = worldPosition;
        Text = text;
        Kind = kind;
    }

    /// <summary>
    /// 타입 배율을 여섯 칸 중 하나로 고른다.
    /// </summary>
    public static DamageTextKind FromMultiplier(float multiplier)
    {
        if (multiplier <= 0f)
        {
            return DamageTextKind.Immune;
        }

        if (multiplier < 0.375f)
        {
            return DamageTextKind.VeryResisted;
        }

        if (multiplier < 0.75f)
        {
            return DamageTextKind.Resisted;
        }

        if (multiplier < 1.5f)
        {
            return DamageTextKind.Neutral;
        }

        if (multiplier < 3f)
        {
            return DamageTextKind.Super;
        }

        return DamageTextKind.VerySuper;
    }
}
