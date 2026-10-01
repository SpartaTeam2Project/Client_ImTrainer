using UnityEngine;

/// <summary>
/// 월드 좌표에 피해 숫자를 띄우라는 요청.
/// </summary>
public struct DamageTextRequested
{
    public Vector2 WorldPosition;
    public string Text;

    public DamageTextRequested(Vector2 worldPosition, string text)
    {
        WorldPosition = worldPosition;
        Text = text;
    }
}
