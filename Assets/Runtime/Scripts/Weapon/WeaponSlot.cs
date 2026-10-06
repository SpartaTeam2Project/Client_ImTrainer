using UnityEngine;

/// <summary>
/// 플레이어 주위에 무기를 붙여 두는 시계 칸. 최대 여섯 자리다.
/// </summary>
public enum WeaponSlot
{
    Hour1 = 0,
    Hour3 = 1,
    Hour5 = 2,
    Hour7 = 3,
    Hour9 = 4,
    Hour11 = 5
}

/// <summary>
/// 시계 칸을 플레이어 기준 방향으로 바꾼다. 각도는 12시 방향을 위로 본다.
/// </summary>
public static class WeaponSlots
{
    public const int MAX_COUNT = 6;

    /// <summary>
    /// 칸의 단위 방향을 돌려준다.
    /// </summary>
    public static Vector2 GetDirection(WeaponSlot slot)
    {
        switch (slot)
        {
            case WeaponSlot.Hour1:
                return Direction(30f);
            case WeaponSlot.Hour3:
                return Direction(90f);
            case WeaponSlot.Hour5:
                return Direction(150f);
            case WeaponSlot.Hour7:
                return Direction(210f);
            case WeaponSlot.Hour9:
                return Direction(270f);
            default:
                return Direction(330f);
        }
    }

    /// <summary>
    /// 플레이어 아래쪽(5시, 7시) 칸인지 알려준다. 이 칸은 플레이어 앞에 그린다.
    /// </summary>
    public static bool IsBelowOwner(WeaponSlot slot)
    {
        return slot == WeaponSlot.Hour5 || slot == WeaponSlot.Hour7;
    }

    private static Vector2 Direction(float degreesClockwiseFromTwelve)
    {
        var radians = degreesClockwiseFromTwelve * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
    }
}
