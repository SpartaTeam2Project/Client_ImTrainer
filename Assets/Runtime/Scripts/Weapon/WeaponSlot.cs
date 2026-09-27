using UnityEngine;

/// <summary>
/// 플레이어 주위에 무기를 붙여 두는 시계 칸.
/// </summary>
public enum WeaponSlot
{
    Hour12 = 0,
    Hour1 = 1,
    Hour3 = 2,
    Hour5 = 3,
    Hour6 = 4,
    Hour7 = 5,
    Hour9 = 6,
    Hour11 = 7
}

/// <summary>
/// 시계 칸을 플레이어 기준 방향으로 바꾼다. 12시가 위다.
/// </summary>
public static class WeaponSlots
{
    /// <summary>
    /// 칸의 단위 방향을 돌려준다.
    /// </summary>
    public static Vector2 GetDirection(WeaponSlot slot)
    {
        switch (slot)
        {
            case WeaponSlot.Hour12:
                return Direction(0f);
            case WeaponSlot.Hour1:
                return Direction(30f);
            case WeaponSlot.Hour3:
                return Direction(90f);
            case WeaponSlot.Hour5:
                return Direction(150f);
            case WeaponSlot.Hour6:
                return Direction(180f);
            case WeaponSlot.Hour7:
                return Direction(210f);
            case WeaponSlot.Hour9:
                return Direction(270f);
            default:
                return Direction(330f);
        }
    }

    private static Vector2 Direction(float degreesClockwiseFromTwelve)
    {
        var radians = degreesClockwiseFromTwelve * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
    }
}
