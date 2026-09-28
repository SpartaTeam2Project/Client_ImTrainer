using System;
using UnityEngine;

/// <summary>
/// 한 동작의 8방향 프레임. 칸이 비면 호출 쪽에서 다른 방향으로 대체한다.
/// </summary>
[Serializable]
public class EightDirectionFrames
{
    [SerializeField] private Sprite[] _down = Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _downRight = Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _right = Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _upRight = Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _up = Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _upLeft = Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _left = Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _downLeft = Array.Empty<Sprite>();

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
        _down = down ?? System.Array.Empty<Sprite>();
        _downRight = downRight ?? System.Array.Empty<Sprite>();
        _right = right ?? System.Array.Empty<Sprite>();
        _upRight = upRight ?? System.Array.Empty<Sprite>();
        _up = up ?? System.Array.Empty<Sprite>();
        _upLeft = upLeft ?? System.Array.Empty<Sprite>();
        _left = left ?? System.Array.Empty<Sprite>();
        _downLeft = downLeft ?? System.Array.Empty<Sprite>();
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
