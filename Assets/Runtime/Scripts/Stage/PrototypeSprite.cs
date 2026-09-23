using UnityEngine;

/// <summary>
/// 자리표시 액터가 쓰는 단색 스프라이트.
/// </summary>
public static class PrototypeSprite
{
    private static Sprite _whiteSquare;

    public static Sprite WhiteSquare
    {
        get
        {
            if (_whiteSquare != null)
            {
                return _whiteSquare;
            }

            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _whiteSquare = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return _whiteSquare;
        }
    }
}
