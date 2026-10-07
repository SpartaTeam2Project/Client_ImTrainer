using UnityEngine;

/// <summary>
/// 자리표시 액터가 쓰는 단색 스프라이트.
/// </summary>
public static class PrototypeSprite
{
    public const float CHARGE_LANE_WIDTH = 16f;

    private static Sprite _whiteSquare;
    private static Sprite _chargeLane;
    private static Sprite _slamCircle;
    private static Sprite _slamRing;

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

    /// <summary>
    /// 돌진 예고. 시작은 옅고 끝으로 갈수록 진한 빨간 띠다.
    /// </summary>
    public static Sprite ChargeLane
    {
        get
        {
            if (_chargeLane != null)
            {
                return _chargeLane;
            }

            var width = Mathf.RoundToInt(CHARGE_LANE_WIDTH);
            var texture = new Texture2D(width, 1);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            for (var x = 0; x < width; x++)
            {
                var blend = width <= 1 ? 1f : x / (width - 1f);
                texture.SetPixel(x, 0, new Color(1f, 0.18f, 0.12f, Mathf.Lerp(0.2f, 1f, blend)));
            }

            texture.Apply();
            _chargeLane = Sprite.Create(texture, new Rect(0f, 0f, width, 1f), new Vector2(0f, 0.5f), 1f);
            return _chargeLane;
        }
    }

    /// <summary>
    /// 내리찍기 예고. 지름 1인 빨간 원이다.
    /// </summary>
    public static Sprite SlamCircle
    {
        get
        {
            if (_slamCircle != null)
            {
                return _slamCircle;
            }

            const int SIZE = 32;
            var texture = new Texture2D(SIZE, SIZE);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var center = (SIZE - 1) * 0.5f;
            var radius = SIZE * 0.5f;
            for (var y = 0; y < SIZE; y++)
            {
                for (var x = 0; x < SIZE; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var alpha = distance <= radius ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 0.18f, 0.12f, alpha));
                }
            }

            texture.Apply();
            _slamCircle = Sprite.Create(texture, new Rect(0f, 0f, SIZE, SIZE), new Vector2(0.5f, 0.5f), SIZE);
            return _slamCircle;
        }
    }

    /// <summary>
    /// 내리찍기 범위 테두리. 바깥 끝이 지름 1이다.
    /// </summary>
    public static Sprite SlamRing
    {
        get
        {
            if (_slamRing != null)
            {
                return _slamRing;
            }

            const int SIZE = 256;
            const float THICKNESS_PIXELS = 0.8f;
            const float FEATHER_PIXELS = 1.15f;
            var texture = new Texture2D(SIZE, SIZE);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var center = (SIZE - 1) * 0.5f;
            var edge = SIZE * 0.5f;
            var coreOuter = edge - FEATHER_PIXELS;
            var coreInner = coreOuter - THICKNESS_PIXELS;
            for (var y = 0; y < SIZE; y++)
            {
                for (var x = 0; x < SIZE; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var outerFade = Mathf.Clamp01((edge - distance) / FEATHER_PIXELS);
                    var innerFade = Mathf.Clamp01((distance - (coreInner - FEATHER_PIXELS)) / FEATHER_PIXELS);
                    var alpha = distance > coreOuter ? outerFade : innerFade;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            _slamRing = Sprite.Create(texture, new Rect(0f, 0f, SIZE, SIZE), new Vector2(0.5f, 0.5f), SIZE);
            return _slamRing;
        }
    }
}
