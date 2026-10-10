using UnityEngine;

/// <summary>
/// 후딘 둘레에 있다가 한 방향으로 날아가는 숟가락.
/// </summary>
public class SpoonShot : MonoBehaviour
{
    private const float HIT_RADIUS = 0.4f;
    private const float VISUAL_SIZE = 0.7f;
    private const float FRAME_RATE = 12f;
    private const int SORTING_ORDER = 12;

    private SpriteRenderer _renderer;
    private Sprite[] _frames = System.Array.Empty<Sprite>();
    private int _frameIndex;
    private float _frameTimer;
    private float _frameRate = FRAME_RATE;
    private int _frameStep = 1;
    private bool _flying;
    private Vector2 _direction = Vector2.right;
    private float _speed;
    private float _lifetime;
    private float _age;
    private int _playerId;
    private float _damage;

    /// <summary>
    /// 아직 날아가지 않는 숟가락을 만든다.
    /// </summary>
    public static SpoonShot Create(Vector2 position, Sprite[] frames)
    {
        var spoonObject = new GameObject("Spoon");
        spoonObject.transform.position = position;
        var spoon = spoonObject.AddComponent<SpoonShot>();
        spoon._frames = frames ?? System.Array.Empty<Sprite>();
        spoon._renderer = spoonObject.AddComponent<SpriteRenderer>();
        spoon._renderer.sortingOrder = SORTING_ORDER;
        spoon.ApplyFrame();
        spoon.ApplyScale();
        return spoon;
    }

    /// <summary>
    /// 숟가락마다 다른 장에서, 다른 빠르기와 방향으로 돌게 한다.
    /// </summary>
    public void SetSpinPhase(int index, int count)
    {
        if (_frames.Length == 0)
        {
            return;
        }

        var safeCount = Mathf.Max(1, count);
        _frameIndex = (index * _frames.Length) / safeCount;
        _frameStep = index % 2 == 0 ? 1 : -1;
        var spread = safeCount <= 1 ? 0f : index / (float)(safeCount - 1);
        _frameRate = FRAME_RATE * Mathf.Lerp(0.65f, 1.4f, spread);
        ApplyFrame();
    }

    /// <summary>
    /// 차지 중 원 위의 위치로 옮긴다.
    /// </summary>
    public void Place(Vector2 position)
    {
        if (_flying)
        {
            return;
        }

        transform.position = position;
    }

    /// <summary>
    /// 이 방향으로 날리기 시작한다. 수명은 이때부터다.
    /// </summary>
    public void Launch(Vector2 direction, float speed, float lifetime, int playerId, float damage)
    {
        _flying = true;
        _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        _speed = Mathf.Max(0.01f, speed);
        _lifetime = Mathf.Max(0.05f, lifetime);
        _age = 0f;
        _playerId = playerId;
        _damage = damage;
    }

    private void Update()
    {
        AdvanceFrame();
        if (!_flying)
        {
            return;
        }

        _age += Time.deltaTime;
        if (_age >= _lifetime)
        {
            Destroy(gameObject);
            return;
        }

        var next = (Vector2)transform.position + _direction * _speed * Time.deltaTime;
        if (Managers.Instance != null
            && Managers.Instance.TryGetManager<StageFieldManager>(out var fieldManager)
            && fieldManager.TryBounce(next, _direction, out var bouncedPosition, out var bouncedDirection))
        {
            transform.position = bouncedPosition;
            _direction = bouncedDirection;
        }
        else
        {
            transform.position = next;
        }

        TryHit();
    }

    private void TryHit()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return;
        }

        if (!playerManager.IsAlive || playerManager.PlayerTransform == null)
        {
            return;
        }

        var offset = (Vector2)playerManager.PlayerTransform.position - (Vector2)transform.position;
        if (offset.sqrMagnitude > HIT_RADIUS * HIT_RADIUS)
        {
            return;
        }

        playerManager.TakeDamage(_playerId, _damage);
        Destroy(gameObject);
    }

    private void AdvanceFrame()
    {
        if (_frames.Length <= 1)
        {
            ApplyFrame();
            return;
        }

        _frameTimer += Time.deltaTime;
        var frameDuration = 1f / Mathf.Max(0.01f, _frameRate);
        while (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            _frameIndex = (_frameIndex + _frameStep + _frames.Length) % _frames.Length;
        }

        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (_renderer == null || _frames.Length == 0)
        {
            return;
        }

        var index = Mathf.Clamp(_frameIndex, 0, _frames.Length - 1);
        if (_frames[index] != null)
        {
            _renderer.sprite = _frames[index];
        }
    }

    private void ApplyScale()
    {
        var sprite = _renderer != null ? _renderer.sprite : null;
        var size = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
        var scale = size > 0f ? VISUAL_SIZE / size : VISUAL_SIZE;
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
