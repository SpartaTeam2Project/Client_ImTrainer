using UnityEngine;

/// <summary>
/// 적이 던진 투사체. 직선으로 날아가 플레이어에게 한 번 맞는다.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    private const float MOVE_SQR_EPSILON = 0.0001f;
    private const float PROJECTILE_SIZE = 0.45f;
    private const int PROJECTILE_SORTING_ORDER = 8;
    private static readonly Color PROJECTILE_COLOR = new Color(0.95f, 0.85f, 0.2f, 1f);

    private SpriteRenderer _renderer;
    private int _playerId;
    private Vector2 _direction = Vector2.right;
    private float _speed;
    private float _remainingRange;
    private float _hitRadius;
    private float _damage;

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
    /// 발사 위치와 수치를 넣고 켠다. 사거리는 맞는 판정 두께만큼 더 간다.
    /// </summary>
    public void Launch(int playerId, Vector2 position, Vector2 direction, float speed, float range, float damage, float hitRadius, float attackDistance)
    {
        _playerId = playerId;
        transform.position = position;
        var size = Mathf.Max(0.01f, attackDistance);
        transform.localScale = new Vector3(size, size, 1f);
        _direction = direction.sqrMagnitude > MOVE_SQR_EPSILON ? direction.normalized : Vector2.right;
        _speed = Mathf.Max(0f, speed);
        _hitRadius = Mathf.Max(0f, hitRadius);
        _remainingRange = Mathf.Max(0f, range) + _hitRadius;
        _damage = Mathf.Max(0f, damage);
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 이동하고, 맞거나 사거리를 다 쓰면 끈다.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (!IsActive)
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

        if (TryHit() || _remainingRange <= 0f)
        {
            gameObject.SetActive(false);
        }
    }

    #endregion

    #region Private Methods

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
