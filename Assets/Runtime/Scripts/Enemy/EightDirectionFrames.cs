using System;
using UnityEngine;

/// <summary>
/// 한 동작의 8방향 프레임. 칸이 비면 호출 쪽에서 다른 방향으로 대체한다.
/// </summary>
[Serializable]
public class EightDirectionFrames
{
    // Array.Empty는 공유 배열이라 인스펙터 +가 빈 칸에서 동작하지 않는다.
    [SerializeField] private Sprite[] _down = new Sprite[0];
    [SerializeField] private Sprite[] _downRight = new Sprite[0];
    [SerializeField] private Sprite[] _right = new Sprite[0];
    [SerializeField] private Sprite[] _upRight = new Sprite[0];
    [SerializeField] private Sprite[] _up = new Sprite[0];
    [SerializeField] private Sprite[] _upLeft = new Sprite[0];
    [SerializeField] private Sprite[] _left = new Sprite[0];
    [SerializeField] private Sprite[] _downLeft = new Sprite[0];

    public Sprite[] Down => _down;

    public Sprite[] DownRight => _downRight;

    public Sprite[] Right => _right;

    public Sprite[] UpRight => _upRight;

    public Sprite[] Up => _up;

    public Sprite[] UpLeft => _upLeft;

    public Sprite[] Left => _left;

    public Sprite[] DownLeft => _downLeft;

    /// <summary>
    /// 방향 칸 중 하나라도 스프라이트가 있는지 확인한다.
    /// </summary>
    public bool HasFrames()
    {
        return HasSprites(_down)
            || HasSprites(_downRight)
            || HasSprites(_right)
            || HasSprites(_upRight)
            || HasSprites(_up)
            || HasSprites(_upLeft)
            || HasSprites(_left)
            || HasSprites(_downLeft);
    }

    /// <summary>
    /// 평탄하게 저장돼 있던 방향 그림을 이 칸으로 옮긴다.
    /// </summary>
    public void Assign(
        Sprite[] down,
        Sprite[] downRight,
        Sprite[] right,
        Sprite[] upRight,
        Sprite[] up,
        Sprite[] upLeft,
        Sprite[] left,
        Sprite[] downLeft)
    {
        _down = CopySprites(down);
        _downRight = CopySprites(downRight);
        _right = CopySprites(right);
        _upRight = CopySprites(upRight);
        _up = CopySprites(up);
        _upLeft = CopySprites(upLeft);
        _left = CopySprites(left);
        _downLeft = CopySprites(downLeft);
    }

    private static Sprite[] CopySprites(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0)
        {
            return new Sprite[0];
        }

        var copy = new Sprite[frames.Length];
        Array.Copy(frames, copy, frames.Length);
        return copy;
    }

    private static bool HasSprites(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
            {
                return true;
            }
        }

        return false;
    }
}
