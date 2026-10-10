using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 나시 자리에 남는 수면가루. 보스와 따로 퍼지고, 끝나면 스스로 사라진다.
/// </summary>
public class SleepPowderField : MonoBehaviour
{
    private const float LINGER_FRAMES_PER_SECOND = 8f;
    private const float BURST_FADE_SECONDS = 0.5f;
    private const float SHEET_PIXELS = 96f;
    private const float SHEET_PIXELS_PER_UNIT = 100f;
    private const float MIN_FRAME_SIZE = 0.1f;
    private const int SORTING_ORDER = -2;
    private const int CIRCLE_SIZE = 32;
    private static readonly Color FALLBACK_COLOR = new Color(0.55f, 0.9f, 0.45f, 0.45f);
    private static readonly List<SleepPowderField> _active = new List<SleepPowderField>();
    private static Sprite _whiteCircle;

    private SpriteRenderer _burstRenderer;
    private SpriteRenderer _lingerRenderer;
    private int _playerId;
    private float _damage;
    private float _range;
    private float _spreadSeconds;
    private float _hitInterval;
    private float _slowPercent;
    private float _duration;
    private float _age;
    private float _radius;
    private float _nextHitTime;
    private float _lingerTimer;
    private int _lingerFrame;
    private Sprite[] _burstFrames = System.Array.Empty<Sprite>();
    private Sprite[] _lingerFrames = System.Array.Empty<Sprite>();

    /// <summary>
    /// 깔린 자리에 장판을 만든다. 보스에게 붙이지 않아 보스가 죽어도 남는다.
    /// </summary>
    public static void Create(
        Vector2 center,
        int playerId,
        float damage,
        float range,
        float spreadSpeed,
        float hitsPerSecond,
        float slowPercent,
        float duration,
        Sprite[] burstFrames,
        Sprite[] lingerFrames)
    {
        var fieldObject = new GameObject("SleepPowder");
        fieldObject.transform.position = center;
        var field = fieldObject.AddComponent<SleepPowderField>();
        field.Begin(playerId, damage, range, spreadSpeed, hitsPerSecond, slowPercent, duration, burstFrames, lingerFrames);
    }

    private void Begin(
        int playerId,
        float damage,
        float range,
        float spreadSeconds,
        float hitsPerSecond,
        float slowPercent,
        float duration,
        Sprite[] burstFrames,
        Sprite[] lingerFrames)
    {
        _playerId = playerId;
        _damage = damage;
        _range = Mathf.Max(0.1f, range);
        _spreadSeconds = Mathf.Max(0.05f, spreadSeconds);
        _hitInterval = hitsPerSecond > 0f ? 1f / hitsPerSecond : 0.5f;
        _slowPercent = Mathf.Clamp(slowPercent, 0f, 100f);
        _duration = Mathf.Max(0.05f, duration);
        _burstFrames = GrowingFrames(burstFrames);
        _lingerFrames = lingerFrames ?? System.Array.Empty<Sprite>();
        _nextHitTime = Time.time;
        _burstRenderer = AddRenderer(gameObject, SORTING_ORDER);
        var lingerObject = new GameObject("SleepPowderLinger");
        lingerObject.transform.SetParent(transform, false);
        _lingerRenderer = AddRenderer(lingerObject, SORTING_ORDER - 1);
        RefreshVisual();
    }

    private static SpriteRenderer AddRenderer(GameObject target, int sortingOrder)
    {
        var renderer = target.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void OnEnable()
    {
        if (!_active.Contains(this))
        {
            _active.Add(this);
        }
    }

    private void OnDisable()
    {
        _active.Remove(this);
        ApplySlow();
    }

    private void Update()
    {
        _age += Time.deltaTime;
        if (_age >= _duration)
        {
            Destroy(gameObject);
            return;
        }

        _radius = _range * Mathf.Clamp01(_age / _spreadSeconds);
        RefreshVisual();
        TryHit();
        ApplySlow();
    }

    private void RefreshVisual()
    {
        if (_burstRenderer == null)
        {
            return;
        }

        var spread = Mathf.Max(0.05f, _spreadSeconds);
        var progress = Mathf.Clamp01(_age / spread);
        var fadeStart = Mathf.Max(0f, spread - BURST_FADE_SECONDS);
        var burstAlpha = _age <= fadeStart ? 1f : 1f - Mathf.InverseLerp(fadeStart, spread, _age);
        ApplySheetScale(_range * 2f);

        if (HasFrames(_burstFrames) && burstAlpha > 0f)
        {
            var index = _burstFrames.Length <= 1
                ? 0
                : Mathf.Clamp(Mathf.FloorToInt(progress * _burstFrames.Length), 0, _burstFrames.Length - 1);
            if (progress >= 1f)
            {
                index = _burstFrames.Length - 1;
            }

            Show(_burstRenderer, _burstFrames[index], new Color(1f, 1f, 1f, burstAlpha));
        }
        else
        {
            _burstRenderer.enabled = false;
        }

        // 흩어지기 전에 유지 구름을 뒤에 꽉 채워 둔다. 앞에서 사라지면 그 구름이 그대로 보인다.
        if (HasFrames(_lingerFrames) && progress >= 0.65f)
        {
            AdvanceLinger();
            Show(_lingerRenderer, FrameAt(_lingerFrames, _lingerFrame), Color.white);
            return;
        }

        if (!HasFrames(_burstFrames) && !HasFrames(_lingerFrames))
        {
            Show(_burstRenderer, WhiteCircle, FALLBACK_COLOR);
            FitSprite(_burstRenderer.sprite, _radius * 2f);
            return;
        }

        if (_lingerRenderer != null)
        {
            _lingerRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 96칸 전체를 스킬 지름에 맞춘다. 잘린 장마다 키우면 퍼지는 그림이 처음부터 가득 찬다.
    /// </summary>
    private void ApplySheetScale(float diameter)
    {
        var size = SHEET_PIXELS / SHEET_PIXELS_PER_UNIT;
        var scale = size > 0f ? diameter / size : diameter;
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void FitSprite(Sprite sprite, float diameter)
    {
        var size = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
        var scale = size > 0f ? diameter / size : diameter;
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static void Show(SpriteRenderer renderer, Sprite sprite, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.enabled = sprite != null && color.a > 0f;
        renderer.sprite = sprite;
        renderer.color = color;
    }

    /// <summary>
    /// 가장 큰 구름까지 한 번만 쓴다. 그 뒤 흩어지는 장은 빈 구간처럼 보인다.
    /// </summary>
    private static Sprite[] GrowingFrames(Sprite[] frames)
    {
        var grown = new List<Sprite>();
        if (frames == null)
        {
            return System.Array.Empty<Sprite>();
        }

        var peak = -1;
        var peakArea = 0f;
        for (var i = 0; i < frames.Length; i++)
        {
            var sprite = frames[i];
            if (!IsUsableFrame(sprite))
            {
                continue;
            }

            var area = sprite.bounds.size.x * sprite.bounds.size.y;
            if (area > peakArea)
            {
                peakArea = area;
                peak = i;
            }
        }

        if (peak < 0)
        {
            return System.Array.Empty<Sprite>();
        }

        for (var i = 0; i <= peak; i++)
        {
            if (IsUsableFrame(frames[i]))
            {
                grown.Add(frames[i]);
            }
        }

        return grown.ToArray();
    }

    private static Sprite FrameAt(Sprite[] frames, int index)
    {
        if (frames == null || frames.Length == 0)
        {
            return null;
        }

        index = Mathf.Clamp(index, 0, frames.Length - 1);
        for (var i = index; i >= 0; i--)
        {
            if (IsUsableFrame(frames[i]))
            {
                return frames[i];
            }
        }

        for (var i = index + 1; i < frames.Length; i++)
        {
            if (IsUsableFrame(frames[i]))
            {
                return frames[i];
            }
        }

        return frames[index];
    }

    private static bool IsUsableFrame(Sprite sprite)
    {
        if (sprite == null)
        {
            return false;
        }

        return Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) >= MIN_FRAME_SIZE;
    }

    private void AdvanceLinger()
    {
        if (_lingerFrames.Length <= 1)
        {
            _lingerFrame = 0;
            return;
        }

        _lingerTimer += Time.deltaTime;
        var frameDuration = 1f / LINGER_FRAMES_PER_SECOND;
        while (_lingerTimer >= frameDuration)
        {
            _lingerTimer -= frameDuration;
            _lingerFrame = (_lingerFrame + 1) % _lingerFrames.Length;
        }
    }

    private void TryHit()
    {
        if (Time.time < _nextHitTime || !TryGetPlayerPosition(out var playerPosition) || !Contains(playerPosition))
        {
            return;
        }

        if (Managers.Instance != null && Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            playerManager.TakeDamage(_playerId, _damage);
        }

        _nextHitTime = Time.time + _hitInterval;
    }

    private bool Contains(Vector2 playerPosition)
    {
        return Vector2.Distance(transform.position, playerPosition) <= _radius;
    }

    private bool TryGetPlayerPosition(out Vector2 playerPosition)
    {
        playerPosition = Vector2.zero;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return false;
        }

        if (!playerManager.TryGetPlayer(_playerId, out var player) || player.Health == null || !player.Health.IsAlive)
        {
            return false;
        }

        playerPosition = player.transform.position;
        return true;
    }

    private static void ApplySlow()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return;
        }

        var playerId = playerManager.LocalPlayerId;
        var slowPercent = 0f;
        var inside = false;
        if (_active.Count > 0)
        {
            playerId = _active[0]._playerId;
        }

        if (TryGetAlivePosition(playerManager, playerId, out var playerPosition))
        {
            for (var i = 0; i < _active.Count; i++)
            {
                var field = _active[i];
                if (field == null || !field.Contains(playerPosition))
                {
                    continue;
                }

                inside = true;
                if (field._slowPercent > slowPercent)
                {
                    slowPercent = field._slowPercent;
                }
            }
        }

        var multiplier = inside ? 1f - slowPercent / 100f : 1f;
        playerManager.SetMoveSlow(playerId, multiplier);
    }

    private static bool TryGetAlivePosition(PlayerManager playerManager, int playerId, out Vector2 playerPosition)
    {
        playerPosition = Vector2.zero;
        if (!playerManager.TryGetPlayer(playerId, out var player) || player.Health == null || !player.Health.IsAlive)
        {
            return false;
        }

        playerPosition = player.transform.position;
        return true;
    }

    private static bool HasFrames(Sprite[] frames)
    {
        if (frames == null)
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

    private static Sprite WhiteCircle
    {
        get
        {
            if (_whiteCircle != null)
            {
                return _whiteCircle;
            }

            var texture = new Texture2D(CIRCLE_SIZE, CIRCLE_SIZE);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var center = (CIRCLE_SIZE - 1) * 0.5f;
            var radius = CIRCLE_SIZE * 0.5f;
            for (var y = 0; y < CIRCLE_SIZE; y++)
            {
                for (var x = 0; x < CIRCLE_SIZE; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var alpha = distance <= radius ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            _whiteCircle = Sprite.Create(texture, new Rect(0f, 0f, CIRCLE_SIZE, CIRCLE_SIZE), new Vector2(0.5f, 0.5f), CIRCLE_SIZE);
            return _whiteCircle;
        }
    }
}
