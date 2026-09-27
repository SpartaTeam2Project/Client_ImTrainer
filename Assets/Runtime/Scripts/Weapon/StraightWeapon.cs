using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시계 칸에 붙어 가장 가까운 적에게 직선 투사체를 쏜다.
/// </summary>
public class StraightWeapon : Weapon
{
    private const float LOOK_AXIS_EPSILON = 0.01f;
    private const float AIM_SQR_EPSILON = 0.0001f;
    private const float VOLLEY_SPREAD_DEGREES = 12f;
    private const float PLACEHOLDER_SIZE = 0.65f;
    private const int SORTING_ORDER = 11;
    private const float DEFAULT_FRAMES_PER_SECOND = 8f;
    private static readonly Color PLACEHOLDER_COLOR = new Color(0.35f, 0.75f, 0.95f, 1f);

    [Header("Idle")]
    [SerializeField] private Sprite[] _idleFrames;
    [SerializeField] private float _framesPerSecond = DEFAULT_FRAMES_PER_SECOND;

    [Header("Projectile")]
    [SerializeField] private WeaponProjectile _projectilePrefab;

    [Header("Stats")]
    [SerializeField] private WeaponStats _stats = new WeaponStats();

    private readonly List<WeaponProjectile> _projectiles = new List<WeaponProjectile>();

    private AbilityManager _owner;
    private SpriteRenderer _renderer;
    private Transform _projectileRoot;
    private Vector2 _offset = Vector2.right;
    private int _playerId;
    private float _cooldownTimer;
    private bool _facingRight;
    private int _frameIndex;
    private float _frameTimer;

    #region Unity Methods

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null)
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sortingOrder = SORTING_ORDER;
        }

        if (HasIdleFrames())
        {
            ShowIdleFrame();
            return;
        }

        EnsurePlaceholder();
    }

    private void OnDestroy()
    {
        ClearProjectiles();
        if (_owner == null)
        {
            return;
        }

        _owner.NotifyWeaponDestroyed(this);
        _owner = null;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 소유 매니저를 연결한다.
    /// </summary>
    public override void Bind(AbilityManager owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// 시계 칸에 두고 레벨 수치는 프리팹에 있는 값을 쓴다.
    /// </summary>
    public override void Initialize(int playerId, WeaponSlot slot)
    {
        _playerId = playerId;
        if (_stats == null)
        {
            _stats = new WeaponStats();
        }

        _offset = WeaponSlots.GetDirection(slot) * _stats.FollowDistance;
        _cooldownTimer = 0f;
        _facingRight = false;
        _frameIndex = 0;
        _frameTimer = 0f;
        if (HasIdleFrames())
        {
            ShowIdleFrame();
        }
        else if (_renderer != null)
        {
            _renderer.flipX = false;
        }

        EnsureProjectileRoot();
    }

    /// <summary>
    /// 플레이어를 따라가고, 쿨다운마다 사거리 안의 적에게 쏜다.
    /// </summary>
    public override void Tick(float deltaTime)
    {
        if (!TryGetPlayer(out var playerManager))
        {
            return;
        }

        var playerTransform = playerManager.PlayerTransform;
        transform.position = (Vector2)playerTransform.position + _offset;
        ApplyFacing(playerManager.LookDirection);
        AdvanceIdle(deltaTime);
        TryAttack(deltaTime);
        TickProjectiles(deltaTime);
    }

    #endregion

    #region Private Methods

    private bool TryGetPlayer(out PlayerManager playerManager)
    {
        playerManager = null;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager(out playerManager))
        {
            return false;
        }

        if (playerManager.LocalPlayerId != _playerId || !playerManager.IsAlive || playerManager.PlayerTransform == null)
        {
            return false;
        }

        return true;
    }

    private void AdvanceIdle(float deltaTime)
    {
        if (!HasIdleFrames() || _idleFrames.Length <= 1 || _framesPerSecond <= 0f)
        {
            return;
        }

        _frameTimer += deltaTime;
        var frameDuration = 1f / _framesPerSecond;
        var changed = false;
        while (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            _frameIndex = (_frameIndex + 1) % _idleFrames.Length;
            changed = true;
        }

        if (changed)
        {
            ShowIdleFrame();
        }
    }

    private void ShowIdleFrame()
    {
        if (_renderer == null || !HasIdleFrames())
        {
            return;
        }

        if (_frameIndex < 0 || _frameIndex >= _idleFrames.Length)
        {
            _frameIndex = 0;
        }

        var sprite = _idleFrames[_frameIndex];
        if (sprite == null)
        {
            return;
        }

        _renderer.sprite = sprite;
        _renderer.color = Color.white;
        _renderer.flipX = _facingRight;
    }

    private bool HasIdleFrames()
    {
        if (_idleFrames == null || _idleFrames.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < _idleFrames.Length; i++)
        {
            if (_idleFrames[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsurePlaceholder()
    {
        if (_renderer == null || _renderer.sprite != null)
        {
            return;
        }

        _renderer.sprite = PrototypeSprite.WhiteSquare;
        _renderer.color = PLACEHOLDER_COLOR;
        transform.localScale = new Vector3(PLACEHOLDER_SIZE, PLACEHOLDER_SIZE, 1f);
    }

    /// <summary>
    /// 기본 그림이 왼쪽을 보므로, 오른쪽을 볼 때만 뒤집는다.
    /// </summary>
    private void ApplyFacing(Vector2 lookDirection)
    {
        if (lookDirection.x > LOOK_AXIS_EPSILON)
        {
            _facingRight = true;
        }
        else if (lookDirection.x < -LOOK_AXIS_EPSILON)
        {
            _facingRight = false;
        }

        if (_renderer != null)
        {
            _renderer.flipX = _facingRight;
        }
    }

    private void TryAttack(float deltaTime)
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= deltaTime;
            if (_cooldownTimer > 0f)
            {
                return;
            }
        }

        if (Managers.Instance == null || !Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager))
        {
            return;
        }

        var origin = (Vector2)transform.position;
        if (!enemyManager.TryGetClosest(origin, _stats.AttackRange, out var enemy))
        {
            return;
        }

        var aim = (Vector2)enemy.transform.position - origin;
        if (aim.sqrMagnitude <= AIM_SQR_EPSILON)
        {
            aim = Vector2.right;
        }
        else
        {
            aim.Normalize();
        }

        LaunchVolley(origin, aim);
        _cooldownTimer = _stats.Cooldown;
    }

    private void LaunchVolley(Vector2 origin, Vector2 direction)
    {
        var count = _stats.ProjectileCount;
        if (count <= 1)
        {
            LaunchOne(origin, direction);
            return;
        }

        var start = -VOLLEY_SPREAD_DEGREES * (count - 1) * 0.5f;
        for (var i = 0; i < count; i++)
        {
            LaunchOne(origin, Rotate(direction, start + VOLLEY_SPREAD_DEGREES * i));
        }
    }

    private void LaunchOne(Vector2 origin, Vector2 direction)
    {
        var projectile = GetProjectile();
        projectile.Launch(origin, direction, _stats.ProjectileSpeed, _stats.AttackRange, _stats.HitRadius, _stats.Damage);
    }

    private WeaponProjectile GetProjectile()
    {
        for (var i = 0; i < _projectiles.Count; i++)
        {
            var projectile = _projectiles[i];
            if (projectile != null && !projectile.IsActive)
            {
                return projectile;
            }
        }

        EnsureProjectileRoot();
        var created = CreateProjectile();
        _projectiles.Add(created);
        return created;
    }

    private WeaponProjectile CreateProjectile()
    {
        if (_projectilePrefab == null)
        {
            return WeaponProjectile.Create(_projectileRoot);
        }

        var projectile = Instantiate(_projectilePrefab, _projectileRoot);
        projectile.gameObject.SetActive(false);
        return projectile;
    }

    private void TickProjectiles(float deltaTime)
    {
        for (var i = 0; i < _projectiles.Count; i++)
        {
            var projectile = _projectiles[i];
            if (projectile != null && projectile.IsActive)
            {
                projectile.Tick(deltaTime);
            }
        }
    }

    private void EnsureProjectileRoot()
    {
        if (_projectileRoot != null)
        {
            return;
        }

        var rootObject = new GameObject("WeaponProjectiles");
        _projectileRoot = rootObject.transform;
    }

    private void ClearProjectiles()
    {
        if (_projectileRoot != null)
        {
            Destroy(_projectileRoot.gameObject);
            _projectileRoot = null;
        }

        _projectiles.Clear();
    }

    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        var radians = degrees * Mathf.Deg2Rad;
        var sin = Mathf.Sin(radians);
        var cos = Mathf.Cos(radians);
        return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
    }

    #endregion
}
