using UnityEngine;

/// <summary>
/// 직선으로 날아가 사거리 안의 적에게 한 번 맞는다.
/// </summary>
public class WeaponProjectile : MonoBehaviour
{
    private const float MOVE_SQR_EPSILON = 0.0001f;
    private const float PROJECTILE_SIZE = 0.3f;
    private const int PROJECTILE_SORTING_ORDER = 12;
    private static readonly Color PROJECTILE_COLOR = new Color(1f, 0.45f, 0.12f, 1f);

    private SpriteRenderer _renderer;
    private Vector2 _direction = Vector2.right;
    private float _speed;
    private float _remainingRange;
    private float _hitRadius;
    private float _damage;

    public bool IsActive => gameObject.activeSelf;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null && _renderer.sprite == null)
        {
            _renderer.sprite = PrototypeSprite.WhiteSquare;
        }
    }

    /// <summary>
    /// 자리표시 투사체를 만들어 꺼 둔다.
    /// </summary>
    public static WeaponProjectile Create(Transform parent)
    {
        var actorObject = new GameObject("WeaponProjectile");
        actorObject.transform.SetParent(parent, false);
        actorObject.transform.localScale = new Vector3(PROJECTILE_SIZE, PROJECTILE_SIZE, 1f);

        var renderer = actorObject.AddComponent<SpriteRenderer>();
        renderer.sprite = PrototypeSprite.WhiteSquare;
        renderer.color = PROJECTILE_COLOR;
        renderer.sortingOrder = PROJECTILE_SORTING_ORDER;

        var projectile = actorObject.AddComponent<WeaponProjectile>();
        actorObject.SetActive(false);
        return projectile;
    }

    /// <summary>
    /// 총알 그림을 넣는다. 비어 있으면 기존 그림을 유지한다.
    /// </summary>
    public void ApplySprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        if (_renderer == null)
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        if (_renderer == null)
        {
            return;
        }

        _renderer.sprite = sprite;
        _renderer.color = Color.white;
    }

    /// <summary>
    /// 발사 위치와 수치를 넣고 켠다.
    /// </summary>
    public void Launch(Vector2 position, Vector2 direction, float speed, float range, float hitRadius, float damage)
    {
        transform.position = position;
        _direction = direction.sqrMagnitude > MOVE_SQR_EPSILON ? direction.normalized : Vector2.right;
        _speed = Mathf.Max(0f, speed);
        _remainingRange = Mathf.Max(0f, range);
        _hitRadius = Mathf.Max(0f, hitRadius);
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

        if (TryHit())
        {
            gameObject.SetActive(false);
            return;
        }

        if (_remainingRange <= 0f)
        {
            gameObject.SetActive(false);
        }
    }

    private bool TryHit()
    {
        if (_damage <= 0f || Managers.Instance == null || !Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager))
        {
            return false;
        }

        if (!enemyManager.TryGetClosest(transform.position, _hitRadius, out var enemy))
        {
            return false;
        }

        enemy.ApplyDamage(_damage);
        return true;
    }
}
