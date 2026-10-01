using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어를 중심으로 세우는 사각 울타리. 이동 한계와 둘레 스프라이트를 함께 가진다.
/// </summary>
public sealed class RectBossFence : IStageFence
{
    public const float WIDTH = 7f;
    public const float HEIGHT = 8f;
    public const float DEFAULT_SCALE = 1f;
    private const float MIN_SIZE = 4f;
    private const float MIN_SCALE = 0.01f;
    private const float BODY_PADDING = 0.35f;
    private const float CORE_PACK = 0.8f;
    private const int SORTING_ORDER = 3;

    private readonly List<GameObject> _links = new List<GameObject>();
    private GameObject _root;
    private float _width = WIDTH;
    private float _height = HEIGHT;
    private float _minX;
    private float _maxX;
    private float _minY;
    private float _maxY;

    public Vector2 Center { get; private set; }

    /// <summary>
    /// 중심 둘레에 울타리를 세운다.
    /// </summary>
    public static RectBossFence Create(Vector2 center, Transform parent, BossFenceSpec spec)
    {
        if (spec.Left == null || spec.Right == null || spec.Edge == null)
        {
            Debug.LogWarning("보스 울타리 스프라이트가 비어 있습니다. 클립 Fence에 왼쪽 en_6, 오른쪽 en_7, 아래와 위 en_14를 넣으세요.");
        }

        var fence = new RectBossFence();
        fence.Center = center;
        fence._width = Mathf.Max(MIN_SIZE, spec.Width);
        fence._height = Mathf.Max(MIN_SIZE, spec.Height);
        fence.Build(parent, spec);
        return fence;
    }

    /// <summary>
    /// 오른쪽 울타리에서 안쪽으로 띄운 위치. 세로는 울타리 중심이다.
    /// </summary>
    public Vector2 RightInnerPosition(float inset)
    {
        return new Vector2(_maxX - Mathf.Max(0f, inset), Center.y);
    }

    /// <summary>
    /// 좌표를 울타리 안쪽으로 되돌린다.
    /// </summary>
    public Vector2 ClampPosition(Vector2 position)
    {
        position.x = Mathf.Clamp(position.x, _minX, _maxX);
        position.y = Mathf.Clamp(position.y, _minY, _maxY);
        return position;
    }

    /// <summary>
    /// 점이 울타리 사각형 안이면 true.
    /// </summary>
    public bool Contains(Vector2 position)
    {
        var halfWidth = _width * 0.5f;
        var halfHeight = _height * 0.5f;
        return position.x >= Center.x - halfWidth
            && position.x <= Center.x + halfWidth
            && position.y >= Center.y - halfHeight
            && position.y <= Center.y + halfHeight;
    }

    /// <summary>
    /// 둘레 스프라이트를 없앤다.
    /// </summary>
    public void DestroyVisuals()
    {
        if (_root != null)
        {
            Object.Destroy(_root);
        }

        _root = null;
        _links.Clear();
    }

    private void Build(Transform parent, BossFenceSpec spec)
    {
        _root = new GameObject("BossFence");
        if (parent != null)
        {
            _root.transform.SetParent(parent, false);
        }

        var halfWidth = _width * 0.5f;
        var halfHeight = _height * 0.5f;
        var gap = Mathf.Max(0f, spec.Gap);
        var topScale = Mathf.Max(MIN_SCALE, spec.TopScale);
        var bottomScale = Mathf.Max(MIN_SCALE, spec.BottomScale);
        var leftScale = Mathf.Max(MIN_SCALE, spec.LeftScale);
        var rightScale = Mathf.Max(MIN_SCALE, spec.RightScale);
        var topHeight = SpriteExtent(spec.Edge, topScale, false);
        var bottomHeight = SpriteExtent(spec.Edge, bottomScale, false);
        var leftWidth = SpriteExtent(spec.Left, leftScale, true);
        var rightWidth = SpriteExtent(spec.Right, rightScale, true);
        SetWalkBounds(halfWidth, halfHeight, leftWidth, rightWidth, topHeight, bottomHeight);

        PlaceLine(
            spec.Edge,
            topScale,
            false,
            new Vector2(Center.x - halfWidth, Center.y + halfHeight),
            new Vector2(Center.x + halfWidth, Center.y + halfHeight),
            true,
            SpriteExtent(spec.Edge, topScale, true),
            false,
            gap);
        PlaceLine(
            spec.Edge,
            bottomScale,
            false,
            new Vector2(Center.x - halfWidth, Center.y - halfHeight),
            new Vector2(Center.x + halfWidth, Center.y - halfHeight),
            true,
            SpriteExtent(spec.Edge, bottomScale, true),
            false,
            gap);

        var verticalBottom = Center.y - halfHeight + bottomHeight * 0.5f;
        var verticalTop = Center.y + halfHeight - topHeight * 0.5f;
        PlaceLine(
            spec.Left,
            leftScale,
            false,
            new Vector2(Center.x - halfWidth, verticalBottom),
            new Vector2(Center.x - halfWidth, verticalTop),
            false,
            SideStep(spec.Left, leftScale),
            true,
            gap);
        PlaceLine(
            spec.Right,
            rightScale,
            false,
            new Vector2(Center.x + halfWidth, verticalBottom),
            new Vector2(Center.x + halfWidth, verticalTop),
            false,
            SideStep(spec.Right, rightScale),
            true,
            gap);
    }

    private void SetWalkBounds(float halfWidth, float halfHeight, float leftWidth, float rightWidth, float topHeight, float bottomHeight)
    {
        _minX = Center.x - halfWidth + leftWidth * 0.5f + BODY_PADDING;
        _maxX = Center.x + halfWidth - rightWidth * 0.5f - BODY_PADDING;
        _minY = Center.y - halfHeight + bottomHeight * 0.5f + BODY_PADDING;
        _maxY = Center.y + halfHeight - topHeight * 0.5f - BODY_PADDING;
        if (_minX > _maxX)
        {
            _minX = Center.x;
            _maxX = Center.x;
        }

        if (_minY > _maxY)
        {
            _minY = Center.y;
            _maxY = Center.y;
        }
    }

    private void PlaceLine(Sprite sprite, float scale, bool flipY, Vector2 from, Vector2 to, bool alongX, float spriteSize, bool fillSpan, float gap)
    {
        var length = alongX ? Mathf.Abs(to.x - from.x) : Mathf.Abs(to.y - from.y);
        var count = fillSpan ? FillCount(length, spriteSize, gap) : FitCount(length, spriteSize, gap);
        if (count <= 0)
        {
            return;
        }

        var used = count * spriteSize + (count - 1) * gap;
        var origin = (from + to) * 0.5f;
        var start = -used * 0.5f + spriteSize * 0.5f;
        var step = spriteSize + gap;
        for (var i = 0; i < count; i++)
        {
            var offset = start + i * step;
            var position = alongX
                ? new Vector2(origin.x + offset, origin.y)
                : new Vector2(origin.x, origin.y + offset);
            Place(sprite, position, flipY, scale);
        }
    }

    private static int FillCount(float length, float spriteSize, float gap)
    {
        if (gap > 0f)
        {
            return FitCount(length, spriteSize, gap);
        }

        if (length <= 0f || spriteSize <= 0f)
        {
            return 0;
        }

        return Mathf.Max(1, Mathf.CeilToInt(length / spriteSize - 0.001f));
    }

    private static float SideStep(Sprite sprite, float scale)
    {
        var width = SpriteExtent(sprite, scale, true);
        var core = CoreExtent(sprite, scale);
        if (core <= 0f)
        {
            return width;
        }

        return Mathf.Min(width, core * CORE_PACK);
    }

    private static float CoreExtent(Sprite sprite, float scale)
    {
        if (sprite == null || !TryMeasureCorePixels(sprite, out var corePixels))
        {
            return 0f;
        }

        var size = corePixels / sprite.pixelsPerUnit;
        return Mathf.Max(MIN_SCALE, size * Mathf.Max(MIN_SCALE, scale));
    }

    /// <summary>
    /// 잔디에 묻히는 밝은 끝을 빼고, 진한 몸통의 픽셀 길이를 구한다.
    /// </summary>
    private static bool TryMeasureCorePixels(Sprite sprite, out int corePixels)
    {
        corePixels = 0;
        var texture = sprite.texture;
        if (texture == null)
        {
            return false;
        }

        var rect = sprite.textureRect;
        var width = Mathf.RoundToInt(rect.width);
        var height = Mathf.RoundToInt(rect.height);
        if (width <= 1 || height <= 1)
        {
            return false;
        }

        var pixels = ReadSpritePixels(texture, rect, width, height);
        if (pixels == null)
        {
            return false;
        }

        var counts = new int[height];
        var lumaSum = new float[height];
        var minLuma = 1f;
        var maxLuma = 0f;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = pixels[y * width + x];
                if (pixel.a <= 16)
                {
                    continue;
                }

                var luma = (pixel.r * 0.2126f + pixel.g * 0.7152f + pixel.b * 0.0722f) / 255f;
                counts[y]++;
                lumaSum[y] += luma;
                if (luma < minLuma)
                {
                    minLuma = luma;
                }

                if (luma > maxLuma)
                {
                    maxLuma = luma;
                }
            }
        }

        if (maxLuma - minLuma < 0.05f)
        {
            corePixels = height;
            return true;
        }

        var maxCount = 0;
        for (var y = 0; y < height; y++)
        {
            if (counts[y] > maxCount)
            {
                maxCount = counts[y];
            }
        }

        if (maxCount <= 0)
        {
            return false;
        }

        var threshold = minLuma + (maxLuma - minLuma) * 0.5f;
        var first = -1;
        var last = -1;
        for (var y = 0; y < height; y++)
        {
            if (counts[y] <= 0 || counts[y] < maxCount * 0.35f || lumaSum[y] / counts[y] > threshold)
            {
                continue;
            }

            if (first < 0)
            {
                first = y;
            }

            last = y;
        }

        if (first < 0)
        {
            corePixels = height;
            return true;
        }

        corePixels = last - first + 1;
        return true;
    }

    private static Color32[] ReadSpritePixels(Texture texture, Rect rect, int width, int height)
    {
        var temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        var previous = RenderTexture.active;
        Texture2D readable = null;
        try
        {
            Graphics.Blit(texture, temporary);
            RenderTexture.active = temporary;
            readable = new Texture2D(width, height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(rect.x, rect.y, width, height), 0, 0);
            readable.Apply();
            return readable.GetPixels32();
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
            if (readable != null)
            {
                Object.Destroy(readable);
            }
        }
    }

    private static int FitCount(float length, float spriteSize, float gap)
    {
        if (length <= 0f || spriteSize <= 0f)
        {
            return 0;
        }

        if (spriteSize >= length)
        {
            return 1;
        }

        var step = spriteSize + gap;
        var count = Mathf.FloorToInt((length + gap) / step + 0.001f);
        return Mathf.Max(1, count);
    }

    private static float SpriteExtent(Sprite sprite, float scale, bool alongX)
    {
        if (sprite == null)
        {
            return 0f;
        }

        var size = alongX ? sprite.bounds.size.x : sprite.bounds.size.y;
        return Mathf.Max(MIN_SCALE, size * Mathf.Max(MIN_SCALE, scale));
    }

    private void Place(Sprite sprite, Vector2 position, bool flipY, float scale)
    {
        if (sprite == null || _root == null)
        {
            return;
        }

        var linkObject = new GameObject("BossFenceLink");
        linkObject.transform.SetParent(_root.transform, false);
        linkObject.transform.localScale = new Vector3(scale, scale, 1f);
        var center = sprite.bounds.center;
        var placed = position - new Vector2(center.x * scale, (flipY ? -center.y : center.y) * scale);
        linkObject.transform.position = new Vector3(placed.x, placed.y, 0f);
        var renderer = linkObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.flipY = flipY;
        renderer.sortingOrder = SORTING_ORDER;
        _links.Add(linkObject);
    }
}

/// <summary>
/// 보스 클립이 울타리 크기와 변마다 배율을 넘길 때 쓰는 값.
/// </summary>
public struct BossFenceSpec
{
    public float Width;
    public float Height;
    public Sprite Left;
    public Sprite Right;
    public Sprite Edge;
    public float LeftScale;
    public float RightScale;
    public float BottomScale;
    public float TopScale;
    public float Gap;
}
