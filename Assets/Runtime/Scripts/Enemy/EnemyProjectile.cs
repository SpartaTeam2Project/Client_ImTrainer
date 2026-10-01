using UnityEngine;

/// <summary>
/// 적이 던진 투사체. 직선으로 날아가 플레이어에게 한 번 맞는다.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    private const float MOVE_SQR_EPSILON = 0.0001f;
    private const float PROJECTILE_SIZE = 0.45f;
    private const float MIN_RISE_SECONDS = 0.05f;
    private const float FLUTTER_AMPLITUDE = 0.48f;
    private const float FLUTTER_TILT = 28f;
    private const float FLY_SPIN_DEGREES = 420f;
    private const float FLY_SPIN_VARIANCE = 90f;
    private const int PROJECTILE_SORTING_ORDER = 8;
    private static readonly Color PROJECTILE_COLOR = new Color(0.95f, 0.85f, 0.2f, 1f);

    private enum Phase
    {
        Holding = 0,
        Rising = 1,
        Flying = 2
    }

    private SpriteRenderer _renderer;
    private int _playerId;
    private Vector2 _direction = Vector2.right;
    private Vector2 _riseFrom;
    private Vector2 _hoverPoint;
    private float _speed;
    private float _remainingRange;
    private float _hitRadius;
    private float _damage;
    private float _attackDistance = PROJECTILE_SIZE;
    private float _riseDuration = MIN_RISE_SECONDS;
    private float _riseElapsed;
    private float _swayPhase;
    private float _swayFrequency = 6f;
    private float _spinDegreesPerSecond;
    private Phase _phase = Phase.Holding;

    public bool IsRising => IsActive && _phase == Phase.Rising;

    public bool IsActive => gameObject.activeSelf;

    #region Unity Methods

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null && _renderer.sprite == null)
        {
            _renderer.sprite = PrototypeSprite.WhiteSquare;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 자리표시 투사체를 만들어 꺼 둔다.
    /// </summary>
    public static EnemyProjectile Create(Transform parent)
    {
        var actorObject = new GameObject("EnemyProjectile");
        actorObject.transform.SetParent(parent, false);
        actorObject.transform.localScale = new Vector3(PROJECTILE_SIZE, PROJECTILE_SIZE, 1f);

        var renderer = actorObject.AddComponent<SpriteRenderer>();
        renderer.sprite = PrototypeSprite.WhiteSquare;
        renderer.color = PROJECTILE_COLOR;
        renderer.sortingOrder = PROJECTILE_SORTING_ORDER;

        var projectile = actorObject.AddComponent<EnemyProjectile>();
        actorObject.SetActive(false);
        return projectile;
    }

    /// <summary>
    /// 총알 그림을 넣는다. 비어 있으면 자리표시 사각형으로 되돌린다.
    /// </summary>
    public void ApplySprite(Sprite sprite)
    {
        if (_renderer == null)
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        if (_renderer == null)
        {
            return;
        }

        if (sprite == null)
        {
            _renderer.sprite = PrototypeSprite.WhiteSquare;
            _renderer.color = PROJECTILE_COLOR;
            return;
        }

        _renderer.sprite = sprite;
        _renderer.color = Color.white;
    }

    /// <summary>
    /// 보스 머리 위로 올린다. 도착해서 발사되기 전에는 맞지 않는다.
    /// </summary>
    public void BeginRise(int playerId, Vector2 origin, Vector2 hoverPoint, float riseSeconds, float attackDistance, float swayPhase)
    {
        _playerId = playerId;
        _riseFrom = origin;
        _hoverPoint = hoverPoint;
        _riseDuration = Mathf.Max(MIN_RISE_SECONDS, riseSeconds);
        _riseElapsed = 0f;
        _swayPhase = swayPhase;
        _swayFrequency = 5.5f + Mathf.Repeat(swayPhase, 1f) * 3.5f;
        _spinDegreesPerSecond = 0f;
        _attackDistance = Mathf.Max(0.01f, attackDistance);
        _phase = Phase.Rising;
        transform.position = origin;
        transform.rotation = Quaternion.identity;
        ApplyWorldSize(_attackDistance);
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 대기 중인 총알을 그 자리에서 직선으로 날린다.
    /// </summary>
    public void Release(Vector2 direction, float speed, float range, float damage, float hitRadius)
    {
        if (!IsActive || _phase == Phase.Flying)
        {
            return;
        }

        // 잎마다 도는 방향과 속도를 조금씩 달리한다.
        var spinDirection = Mathf.Repeat(_swayPhase, 2f) < 1f ? 1f : -1f;
        var spin = spinDirection * (FLY_SPIN_DEGREES + Mathf.Repeat(_swayPhase, 1f) * FLY_SPIN_VARIANCE);
        Launch(_playerId, transform.position, direction, speed, range, damage, hitRadius, _attackDistance, spin);
    }

    /// <summary>
    /// 아직 발사되지 않은 총알만 끈다.
    /// </summary>
    public void CancelIfUnfired()
    {
        if (!IsActive || _phase == Phase.Flying)
        {
            return;
        }

        _phase = Phase.Holding;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 발사 위치와 수치를 넣고 켠다. 사거리는 맞는 판정 두께만큼 더 간다.
    /// </summary>
    public void Launch(int playerId, Vector2 position, Vector2 direction, float speed, float range, float damage, float hitRadius, float attackDistance, float spinDegreesPerSecond = 0f)
    {
        _playerId = playerId;
        transform.position = position;
        var size = Mathf.Max(0.01f, attackDistance);
        ApplyWorldSize(size);
        _direction = direction.sqrMagnitude > MOVE_SQR_EPSILON ? direction.normalized : Vector2.right;
        _speed = Mathf.Max(0f, speed);
        _hitRadius = Mathf.Max(0f, hitRadius);
        _remainingRange = Mathf.Max(0f, range) + _hitRadius;
        _damage = Mathf.Max(0f, damage);
        _phase = Phase.Flying;
        _spinDegreesPerSecond = spinDegreesPerSecond;
        if (Mathf.Abs(spinDegreesPerSecond) <= MOVE_SQR_EPSILON)
        {
            var angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 이동하고, 맞거나 사거리를 다 쓰면 끈다. 상승·대기 중에는 맞지 않는다.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (!IsActive)
        {
            return;
        }

        if (_phase == Phase.Rising)
        {
            TickRise(deltaTime);
            return;
        }

        if (_phase == Phase.Holding)
        {
            _riseElapsed += deltaTime;
            ApplyFlutter(_hoverPoint);
            return;
        }

        if (_phase != Phase.Flying)
        {
            return;
        }

        var step = _speed * deltaTime;
        if (step > _remainingRange)
        {
            step = _remainingRange;
        }

        transform.position += (Vector3)(_direction * step);
        _remainingRange -= step;
        if (Mathf.Abs(_spinDegreesPerSecond) > MOVE_SQR_EPSILON)
        {
            var angle = transform.eulerAngles.z + _spinDegreesPerSecond * deltaTime;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (TryHit() || _remainingRange <= 0f)
        {
            gameObject.SetActive(false);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 스프라이트 픽셀 크기와 상관없이, 긴 변이 탄환 크기와 같게 맞춘다.
    /// </summary>
    private void ApplyWorldSize(float worldSize)
    {
        var spriteSize = 1f;
        if (_renderer != null && _renderer.sprite != null)
        {
            var bounds = _renderer.sprite.bounds.size;
            spriteSize = Mathf.Max(bounds.x, bounds.y);
        }

        if (spriteSize <= MOVE_SQR_EPSILON)
        {
            spriteSize = 1f;
        }

        var scale = worldSize / spriteSize;
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void TickRise(float deltaTime)
    {
        _riseElapsed += deltaTime;
        var blend = Mathf.Clamp01(_riseElapsed / _riseDuration);
        var basePosition = Vector2.Lerp(_riseFrom, _hoverPoint, blend);
        ApplyFlutter(basePosition);
        if (blend >= 1f)
        {
            _phase = Phase.Holding;
        }
    }

    private void ApplyFlutter(Vector2 anchor)
    {
        var side = Mathf.Sin(_riseElapsed * _swayFrequency + _swayPhase);
        var lift = Mathf.Sin(_riseElapsed * _swayFrequency * 0.63f + _swayPhase * 1.4f);
        transform.position = anchor + new Vector2(side * FLUTTER_AMPLITUDE, lift * FLUTTER_AMPLITUDE * 0.35f);
        transform.rotation = Quaternion.Euler(0f, 0f, side * FLUTTER_TILT);
    }

    private bool TryHit()
    {
        if (_damage <= 0f || Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return false;
        }

        if (!playerManager.IsAlive || playerManager.PlayerTransform == null)
        {
            return false;
        }

        var offset = (Vector2)playerManager.PlayerTransform.position - (Vector2)transform.position;
        if (offset.sqrMagnitude > _hitRadius * _hitRadius)
        {
            return false;
        }

        playerManager.TakeDamage(_playerId, _damage);
        return true;
    }

    #endregion
}
