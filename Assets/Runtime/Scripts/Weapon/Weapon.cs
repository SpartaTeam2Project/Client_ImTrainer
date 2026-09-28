using System;
using UnityEngine;

/// <summary>
/// 시계 칸을 따라다니며 가장 가까운 적을 조준하는 무기. 발사만 하위 클래스가 맡는다.
/// </summary>
public abstract class Weapon : MonoBehaviour
{
    private const float LOOK_AXIS_EPSILON = 0.01f;
    private const float AIM_SQR_EPSILON = 0.0001f;
    private const float VOLLEY_SPREAD_DEGREES = 12f;
    private const int SORTING_ORDER = 11;
    private const float DEFAULT_FRAMES_PER_SECOND = 8f;
    private const float ATTACK_FACING_HOLD = 0.6f;
    private const float SECTOR_DEGREES = 45f;
    private const float SECTOR_HALF_DEGREES = 22.5f;
    private static readonly Color PLACEHOLDER_COLOR = new Color(0.35f, 0.75f, 0.95f, 1f);

    private enum FacingMode
    {
        FlipHorizontal = 0,
        EightDirection = 1
    }

    private enum EightWay
    {
        Right = 0,
        UpRight = 1,
        Up = 2,
        UpLeft = 3,
        Left = 4,
        DownLeft = 5,
        Down = 6,
        DownRight = 7
    }

    [Header("Idle")]
    [SerializeField] private FacingMode _facingMode = FacingMode.FlipHorizontal;
    [SerializeField] private Sprite[] _idleFrames;
    [SerializeField] private float _framesPerSecond = DEFAULT_FRAMES_PER_SECOND;

    [Header("Stats")]
    [SerializeField] private WeaponStats _stats = new WeaponStats();

    [Header("Upgrade")]
    [SerializeField] private WeaponStatUpgrade[] _statUpgrades = Array.Empty<WeaponStatUpgrade>();

    private WeaponVisualData _visual;
    private AbilityManager _owner;
    private SpriteRenderer _renderer;
    private Vector2 _offset = Vector2.right;
    private int _playerId;
    private float _cooldownTimer;
    private float _attackFacingTimer;
    private bool _isAttacking;
    private bool _facingRight;
    private EightWay _eightWay = EightWay.Right;
    private int _frameIndex;
    private float _frameTimer;

    protected WeaponStats Stats => _stats;

    protected WeaponVisualData Visual => _visual;

    protected MonsterType AttackType => Visual != null ? Visual.AttackType : MonsterType.Normal;

    #region Unity Methods

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null)
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sortingOrder = SORTING_ORDER;
        }

        if (HasFrames(CurrentFrames()))
        {
            ShowCurrentFrame();
            return;
        }

        EnsurePlaceholder();
    }

    private void OnDestroy()
    {
        ClearShots();
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
    public void Bind(AbilityManager owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// 이번 무기에 쓸 8방향 그림을 넣는다. 총알과 빔 그림은 각 프리팹이 가진다.
    /// </summary>
    public virtual void ApplyVisual(WeaponVisualData visual)
    {
        _visual = visual;
        _isAttacking = false;
        _frameIndex = 0;
        _frameTimer = 0f;
        if (visual == null)
        {
            return;
        }

        var scale = visual.Scale;
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>
    /// 시계 칸에 두고 레벨 수치는 프리팹에 있는 값을 쓴다.
    /// </summary>
    public void Initialize(int playerId, WeaponSlot slot)
    {
        _playerId = playerId;
        if (_stats == null)
        {
            _stats = new WeaponStats();
        }

        _offset = WeaponSlots.GetDirection(slot) * _stats.FollowDistance;
        _cooldownTimer = 0f;
        _attackFacingTimer = 0f;
        _isAttacking = false;
        _facingRight = false;
        _eightWay = EightWay.Right;
        _frameIndex = 0;
        _frameTimer = 0f;
        if (HasFrames(CurrentFrames()))
        {
            ShowCurrentFrame();
        }
        else if (_renderer != null)
        {
            _renderer.flipX = false;
        }

        OnInitialized();
    }

    /// <summary>
    /// 플레이어를 따라가고, 쿨다운마다 사거리 안의 가장 가까운 적을 조준해 발사한다.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (!TryGetPlayer(out var playerManager))
        {
            return;
        }

        var playerTransform = playerManager.PlayerTransform;
        transform.position = (Vector2)playerTransform.position + _offset;
        if (_attackFacingTimer > 0f)
        {
            _attackFacingTimer -= deltaTime;
        }

        if (_isAttacking)
        {
            AdvanceAttack(deltaTime);
        }
        else
        {
            if (_attackFacingTimer <= 0f)
            {
                ApplyFacing(playerManager.LookDirection);
            }

            AdvanceIdle(deltaTime);
        }

        TryAttack(deltaTime);
        TickShots(deltaTime);
    }

    /// <summary>
    /// 고른 증강만 한 칸 올린다. 그 수치 칸이 없으면 false.
    /// </summary>
    public bool TryApplyUpgrade(WeaponStatKind stat)
    {
        if (_stats == null || !TryGetStatUpgrade(stat, out var upgrade))
        {
            return false;
        }

        _stats.AddStat(stat, upgrade.Amount);
        return true;
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// 조준 방향으로 한 발을 낸다.
    /// </summary>
    protected abstract void LaunchOne(Vector2 origin, Vector2 direction);

    /// <summary>
    /// 이미 나간 발사체를 진행한다.
    /// </summary>
    protected virtual void TickShots(float deltaTime)
    {
    }

    /// <summary>
    /// 무기가 사라질 때 발사체를 치운다.
    /// </summary>
    protected virtual void ClearShots()
    {
    }

    /// <summary>
    /// 시계 칸 배치가 끝난 뒤 발사체 뿌리를 만든다.
    /// </summary>
    protected virtual void OnInitialized()
    {
    }

    #endregion

    #region Private Methods

    private bool TryGetStatUpgrade(WeaponStatKind stat, out WeaponStatUpgrade upgrade)
    {
        upgrade = null;
        if (_statUpgrades == null)
        {
            return false;
        }

        for (var i = 0; i < _statUpgrades.Length; i++)
        {
            var candidate = _statUpgrades[i];
            if (candidate == null || candidate.Stat != stat)
            {
                continue;
            }

            upgrade = candidate;
            return true;
        }

        return false;
    }

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

    /// <summary>
    /// 좌우 모드는 그림을 뒤집고, 8방향 모드는 방향에 맞는 그림을 고른다.
    /// </summary>
    private void ApplyFacing(Vector2 lookDirection)
    {
        if (UsesEightDirection)
        {
            ApplyEightDirection(lookDirection);
            return;
        }

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

    private void ApplyEightDirection(Vector2 lookDirection)
    {
        _facingRight = false;
        if (_renderer != null)
        {
            _renderer.flipX = false;
        }

        if (lookDirection.sqrMagnitude <= LOOK_AXIS_EPSILON)
        {
            return;
        }

        var next = ResolveEightWay(lookDirection);
        if (next == _eightWay)
        {
            return;
        }

        _eightWay = next;
        _frameIndex = 0;
        _frameTimer = 0f;
        ShowCurrentFrame();
    }

    private void AdvanceIdle(float deltaTime)
    {
        var frames = CurrentFrames();
        if (!HasFrames(frames) || frames.Length <= 1 || _framesPerSecond <= 0f)
        {
            return;
        }

        _frameTimer += deltaTime;
        var frameDuration = 1f / _framesPerSecond;
        var changed = false;
        while (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            _frameIndex = (_frameIndex + 1) % frames.Length;
            changed = true;
        }

        if (changed)
        {
            ShowCurrentFrame();
        }
    }

    /// <summary>
    /// 공격 장은 한 번만 재생하고, 끝나면 아이들로 돌아간다.
    /// </summary>
    private void AdvanceAttack(float deltaTime)
    {
        var frames = GetAttackFrames(_eightWay);
        if (!HasFrames(frames) || _framesPerSecond <= 0f)
        {
            EndAttack();
            return;
        }

        _frameTimer += deltaTime;
        var frameDuration = 1f / _framesPerSecond;
        while (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            _frameIndex++;
            if (_frameIndex >= frames.Length)
            {
                EndAttack();
                return;
            }
        }

        ShowCurrentFrame();
    }

    /// <summary>
    /// 조준 방향에 공격 장이 있으면 0번부터 재생한다. 방향은 공격 장 수와 상관없이 0.6초 유지한다.
    /// </summary>
    private void BeginAttackPose()
    {
        _attackFacingTimer = ATTACK_FACING_HOLD;
        var attackFrames = GetAttackFrames(_eightWay);
        if (!HasFrames(attackFrames))
        {
            _isAttacking = false;
            return;
        }

        _isAttacking = true;
        _frameIndex = 0;
        _frameTimer = 0f;
        ShowCurrentFrame();
    }

    private void EndAttack()
    {
        _isAttacking = false;
        _frameIndex = 0;
        _frameTimer = 0f;
        ShowCurrentFrame();
    }

    private void ShowCurrentFrame()
    {
        var frames = CurrentFrames();
        if (_renderer == null || !HasFrames(frames))
        {
            return;
        }

        if (_frameIndex < 0 || _frameIndex >= frames.Length)
        {
            _frameIndex = 0;
        }

        var sprite = frames[_frameIndex];
        if (sprite == null)
        {
            return;
        }

        _renderer.sprite = sprite;
        _renderer.color = Color.white;
        _renderer.flipX = !UsesEightDirection && _facingRight;
    }

    private Sprite[] CurrentFrames()
    {
        if (_isAttacking)
        {
            var attackFrames = GetAttackFrames(_eightWay);
            if (HasFrames(attackFrames))
            {
                return attackFrames;
            }
        }

        if (UsesEightDirection)
        {
            return GetEightWayFrames(_eightWay);
        }

        return _idleFrames;
    }

    private bool UsesEightDirection => _visual != null || _facingMode == FacingMode.EightDirection;

    private Sprite[] GetEightWayFrames(EightWay way)
    {
        if (_visual == null || _visual.Idle == null)
        {
            return null;
        }

        switch (way)
        {
            case EightWay.UpRight:
                return FirstFrames(_visual.Idle.UpRight, _visual.Idle.Right);
            case EightWay.Up:
                return FirstFrames(_visual.Idle.Up, _visual.Idle.Right);
            case EightWay.UpLeft:
                return FirstFrames(_visual.Idle.UpLeft, _visual.Idle.Right);
            case EightWay.Left:
                return FirstFrames(_visual.Idle.Left, _visual.Idle.Right);
            case EightWay.DownLeft:
                return FirstFrames(_visual.Idle.DownLeft, _visual.Idle.Right);
            case EightWay.Down:
                return FirstFrames(_visual.Idle.Down, _visual.Idle.Right);
            case EightWay.DownRight:
                return FirstFrames(_visual.Idle.DownRight, _visual.Idle.Right);
            default:
                return _visual.Idle.Right;
        }
    }

    /// <summary>
    /// 그 방향 공격 장만 돌려준다. 비어 있으면 다른 방향 공격으로 대체하지 않는다.
    /// </summary>
    private Sprite[] GetAttackFrames(EightWay way)
    {
        if (_visual == null || _visual.Attack == null)
        {
            return null;
        }

        switch (way)
        {
            case EightWay.UpRight:
                return _visual.Attack.UpRight;
            case EightWay.Up:
                return _visual.Attack.Up;
            case EightWay.UpLeft:
                return _visual.Attack.UpLeft;
            case EightWay.Left:
                return _visual.Attack.Left;
            case EightWay.DownLeft:
                return _visual.Attack.DownLeft;
            case EightWay.Down:
                return _visual.Attack.Down;
            case EightWay.DownRight:
                return _visual.Attack.DownRight;
            default:
                return _visual.Attack.Right;
        }
    }

    private static Sprite[] FirstFrames(Sprite[] primary, Sprite[] fallback)
    {
        if (HasFrames(primary))
        {
            return primary;
        }

        return fallback;
    }

    private static EightWay ResolveEightWay(Vector2 lookDirection)
    {
        var angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
        if (angle < 0f)
        {
            angle += 360f;
        }

        var index = Mathf.FloorToInt((angle + SECTOR_HALF_DEGREES) / SECTOR_DEGREES) % 8;
        return (EightWay)index;
    }

    private static bool HasFrames(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0)
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

    private void EnsurePlaceholder()
    {
        if (_renderer == null || _renderer.sprite != null)
        {
            return;
        }

        _renderer.sprite = PrototypeSprite.WhiteSquare;
        _renderer.color = PLACEHOLDER_COLOR;
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

        ApplyFacing(aim);
        BeginAttackPose();
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

    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        var radians = degrees * Mathf.Deg2Rad;
        var sin = Mathf.Sin(radians);
        var cos = Mathf.Cos(radians);
        return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
    }

    #endregion
}
