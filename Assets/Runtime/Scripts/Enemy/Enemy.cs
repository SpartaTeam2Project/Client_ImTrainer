using UnityEngine;

/// <summary>
/// 스폰된 적 한 마리의 추적, 공격, 체력을 담당한다.
/// 웨이브가 고른 공격만 쓴다. 근거리는 접촉하고, 원거리는 사거리에서 투사체를 던진다.
/// </summary>
public class Enemy : MonoBehaviour
{
    private const float MOVE_SQR_EPSILON = 0.0001f;
    private const float CONTACT_INTERVAL = 0.6f;
    private const float MIN_ATTACK_RANGE = 0.5f;
    private const float MIN_SHOT_INTERVAL = 0.05f;
    private const float SHOOT_POSE_SECONDS = 0.25f;
    private static readonly MonsterType[] DEFAULT_DEFENDER_TYPES = { MonsterType.Normal };

    [Header("Drop")]
    [SerializeField] private ExperienceGem _experienceGem;

    private EnemyManager _owner;
    private EnemyView _view;
    private int _playerId;
    private float _health;
    private float _contactDamage;
    private float _moveSpeed;
    private float _nextContactTime;
    private EnemyAttackKind _attackKind;
    private float _attackRange;
    private float _attackInterval;
    private int _id;
    private float _hitRadius;
    private float _attackDistance;
    private float _projectileSpeed;
    private Sprite _projectileSprite;
    private float _nextShotTime;
    private float _shootPoseUntil;
    private int _waveIndex;
    private bool _disableOffscreenTeleport;
    private bool _isRushing;
    private Vector2 _rushDirection;
    private float _spawnedAt;
    private MonsterType[] _defenderTypes = DEFAULT_DEFENDER_TYPES;

    public int Id => _id;

    public float Health => _health;

    public int WaveIndex => _waveIndex;

    public bool IsAlive => _health > 0f;

    public bool DisableOffscreenTeleport => _disableOffscreenTeleport;

    public bool IsRushing => _isRushing;

    public float SpawnedAt => _spawnedAt;

    public ExperienceGem ExperienceGem => _experienceGem;

    #region Unity Methods

    private void Awake()
    {
        _view = GetComponent<EnemyView>();
    }

    private void OnDestroy()
    {
        if (_owner == null)
        {
            return;
        }

        _owner.NotifyActorDestroyed(this);
        _owner = null;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 소유 매니저를 연결한다.
    /// </summary>
    public void Bind(EnemyManager owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// 스폰 직후 그림과 크기를 넣는다. 크기를 넘기지 않으면 종 크기를 쓴다.
    /// </summary>
    public void ApplyVisual(MonsterVisualData visual, float? scale = null)
    {
        if (_view == null)
        {
            _view = GetComponent<EnemyView>();
        }

        _defenderTypes = visual != null ? visual.Types : DEFAULT_DEFENDER_TYPES;
        var resolvedScale = scale ?? (visual != null ? visual.Scale : MonsterVisualData.DEFAULT_SCALE);
        transform.localScale = new Vector3(resolvedScale, resolvedScale, 1f);

        if (_view != null)
        {
            _view.ApplyVisual(visual);
        }
    }

    /// <summary>
    /// 스폰 직후 대상 플레이어, 도감 번호, 체력, 피해, 이동, 공격, 피격 반경을 넣는다.
    /// </summary>
    public void Initialize(
        int playerId,
        int waveIndex,
        float maxHealth,
        float contactDamage,
        float moveSpeed,
        EnemyAttackKind attackKind = EnemyAttackKind.Contact,
        float attackRange = 0f,
        float attackInterval = 0f,
        float projectileSpeed = 0f,
        Sprite projectileSprite = null,
        int id = 0,
        float hitRadius = 0f,
        float attackDistance = 0.45f)
    {
        _playerId = playerId;
        _waveIndex = waveIndex;
        _id = id;
        _health = maxHealth;
        _contactDamage = contactDamage;
        _moveSpeed = moveSpeed;
        _attackKind = attackKind;
        _attackRange = attackRange;
        _attackInterval = attackInterval;
        _hitRadius = Mathf.Max(0f, hitRadius);
        _attackDistance = Mathf.Max(0.01f, attackDistance);
        _projectileSpeed = projectileSpeed;
        _projectileSprite = projectileSprite;
        _nextContactTime = 0f;
        _nextShotTime = 0f;
        _shootPoseUntil = 0f;
        _disableOffscreenTeleport = false;
        _isRushing = false;
        _rushDirection = Vector2.zero;
        _spawnedAt = Time.time;

        if (_view == null)
        {
            _view = GetComponent<EnemyView>();
        }

        if (_view != null)
        {
            _view.SetVisual(false, Vector2.down);
        }
    }

    /// <summary>
    /// 화면 밖 재배치를 끈다. 돌진은 BeginRush가 켠다.
    /// </summary>
    public void SetLaneFlags(bool disableOffscreenTeleport, bool isRushing)
    {
        _disableOffscreenTeleport = disableOffscreenTeleport;
        _isRushing = isRushing;
    }

    /// <summary>
    /// 스폰 순간 플레이어 쪽으로 직진을 시작한다. 이후 방향은 바뀌지 않는다.
    /// </summary>
    public void BeginRush(Vector2 direction)
    {
        _isRushing = true;
        _disableOffscreenTeleport = true;
        _rushDirection = direction.sqrMagnitude > MOVE_SQR_EPSILON ? direction.normalized : Vector2.right;
        _spawnedAt = Time.time;
    }

    /// <summary>
    /// 공격 방식에 맞춰 이동하고 피해를 준다. 플레이어가 죽으면 false.
    /// </summary>
    public bool Tick(Vector2 playerPosition)
    {
        if (!IsAlive)
        {
            return true;
        }

        if (_isRushing)
        {
            return TickRush(playerPosition);
        }

        if (_attackKind == EnemyAttackKind.Projectile)
        {
            TickProjectile(playerPosition);
            return true;
        }

        return TickContact(playerPosition);
    }

    /// <summary>
    /// 공격 타입 상성을 곱한 뒤 체력을 깎는다. 체력이 0이면 소유 매니저에 사망을 알린다.
    /// </summary>
    public void ApplyDamage(float amount, MonsterType attackType)
    {
        if (!IsAlive)
        {
            return;
        }

        var dealt = amount * TypeChart.GetMultiplier(attackType, _defenderTypes);
        if (dealt <= 0f)
        {
            return;
        }

        _health = Mathf.Max(0f, _health - dealt);
        if (_health > 0f)
        {
            return;
        }

        if (_owner != null)
        {
            _owner.NotifyDied(this);
        }
    }

    #endregion

    #region Private Methods

    private bool TickRush(Vector2 playerPosition)
    {
        transform.position += (Vector3)(_rushDirection * _moveSpeed * Time.deltaTime);
        if (_view != null)
        {
            _view.SetVisual(true, _rushDirection);
        }

        return TryContactDamage(playerPosition);
    }

    private bool TickContact(Vector2 playerPosition)
    {
        MoveToward(playerPosition, Time.deltaTime);
        return TryContactDamage(playerPosition);
    }

    private bool TryContactDamage(Vector2 playerPosition)
    {
        if (Vector2.Distance(transform.position, playerPosition) > _hitRadius)
        {
            return true;
        }

        if (Time.time < _nextContactTime)
        {
            return true;
        }

        _nextContactTime = Time.time + CONTACT_INTERVAL;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return true;
        }

        playerManager.TakeDamage(_playerId, _contactDamage);
        return playerManager.IsAlive;
    }

    private void TickProjectile(Vector2 playerPosition)
    {
        var origin = (Vector2)transform.position;
        var toPlayer = playerPosition - origin;
        if (!IsInAttackRange(toPlayer))
        {
            MoveToward(playerPosition, Time.deltaTime);
            return;
        }

        var shooting = TryFire(origin, toPlayer) || Time.time < _shootPoseUntil;
        SetView(false, toPlayer, shooting);
    }

    private bool IsInAttackRange(Vector2 toPlayer)
    {
        var range = Mathf.Max(MIN_ATTACK_RANGE, _attackRange);
        return toPlayer.sqrMagnitude <= range * range;
    }

    private bool TryFire(Vector2 origin, Vector2 toPlayer)
    {
        if (_owner == null || Time.time < _nextShotTime || toPlayer.sqrMagnitude <= MOVE_SQR_EPSILON)
        {
            return false;
        }

        _nextShotTime = Time.time + Mathf.Max(MIN_SHOT_INTERVAL, _attackInterval);
        _shootPoseUntil = Time.time + SHOOT_POSE_SECONDS;
        _owner.LaunchProjectile(_playerId, origin, toPlayer, _projectileSpeed, _attackRange, _contactDamage, _hitRadius, _attackDistance, _projectileSprite);
        return true;
    }

    private void SetView(bool isMoving, Vector2 lookDirection, bool shooting)
    {
        if (_view == null)
        {
            return;
        }

        _view.SetVisual(isMoving, lookDirection, shooting);
    }

    private void MoveToward(Vector2 target, float deltaTime)
    {
        var current = (Vector2)transform.position;
        var next = Vector2.MoveTowards(current, target, _moveSpeed * deltaTime);
        transform.position = next;

        if (_view == null)
        {
            return;
        }

        // 프레임 이동량은 고주사율에서 너무 작아져 걷기 재생이 멈춘다. 추적 방향으로 판정한다.
        var toTarget = target - current;
        var isChasing = toTarget.sqrMagnitude > MOVE_SQR_EPSILON;
        var lookDirection = isChasing ? toTarget : next - current;
        _view.SetVisual(isChasing, lookDirection);
    }

    #endregion
}
