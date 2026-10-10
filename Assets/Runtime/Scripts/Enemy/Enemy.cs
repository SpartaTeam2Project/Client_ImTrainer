using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 스폰된 적 한 마리의 추적, 공격, 체력을 담당한다.
/// 웨이브가 고른 공격만 쓴다. 근거리는 접촉하고, 원거리는 사거리에서 투사체를 던진다.
/// 상승 연사 보스는 쫓으면서 몸통 피해도 주고, 쿨타임마다 제자리에서 머리 위 총알을 쏜다.
/// </summary>
public class Enemy : MonoBehaviour
{
    private const float MOVE_SQR_EPSILON = 0.0001f;
    private const float CONTACT_INTERVAL = 0.6f;
    private const float MIN_ATTACK_RANGE = 0.5f;
    private const float MIN_SHOT_INTERVAL = 0.05f;
    private const float SHOOT_POSE_SECONDS = 0.25f;
    private const int VOLLEY_COUNT = 5;
    private const float VOLLEY_RISE_HEIGHT = 2.2f;
    private const float VOLLEY_SPREAD = 0.45f;
    private const float VOLLEY_RISE_SECONDS = 0.95f;
    private const float VOLLEY_SHOT_GAP = 0.28f;
    private const float VOLLEY_RANGE = 30f;
    private const float FAN_ARC_DEGREES = 90f;
    private const float FAN_HOVER_RADIUS = 1.2f;
    private const float DEFAULT_FAN_CAST_RANGE = 4f;
    private const int DEFAULT_FAN_COUNT = 5;
    private const float DEFAULT_FAN_DISTANCE = 8f;
    private const float DEFAULT_BEAM_CAST_RANGE = 6f;
    private const float DEFAULT_BEAM_WIDTH = 0.8f;
    private const float DEFAULT_BEAM_LENGTH = 8f;
    private const float DEFAULT_BEAM_SECONDS = 2.5f;
    private const float DEFAULT_BEAM_HIT_INTERVAL = 0.25f;
    private const float DEFAULT_BEAM_TURN_SPEED = 45f;
    private const float DEFAULT_POOL_RANGE = 3f;
    private const float DEFAULT_POOL_SPREAD_SPEED = 1.5f;
    private const float DEFAULT_POOL_HITS_PER_SECOND = 2f;
    private const float DEFAULT_POOL_SLOW_PERCENT = 40f;
    private const float DEFAULT_POOL_CAST_SECONDS = 1.2f;
    private const float DEFAULT_POOL_SECONDS = 5f;
    private const int DEFAULT_SPOON_COUNT = 8;
    private const float DEFAULT_SPOON_CHARGE_SECONDS = 1.2f;
    private const float DEFAULT_SPOON_SPEED = 6f;
    private const float DEFAULT_SPOON_FLY_SECONDS = 4f;
    private const float SPOON_ORBIT_RADIUS = 1.2f;
    private const int SPOON_SHOOT_RELEASE_FRAME = 4;
    private const float BEAM_FRAME_RATE = 10f;
    private const int BEAM_SORTING_ORDER = 6;
    private const string REAR_UP_SKILL = "RearUp";
    private static readonly Color BEAM_WARNING_COLOR = new Color(1f, 0.15f, 0.12f, 0.28f);
    private static readonly Color BEAM_FILL_COLOR = new Color(1f, 0.08f, 0.08f, 0.9f);
    private static readonly Color BEAM_FALLBACK_COLOR = new Color(0.55f, 0.9f, 1f, 0.92f);
    private const float CHARGE_SECONDS = 0.9f;
    private const float CHARGE_DASH_OVERSHOOT = 1.5f;
    private const int DASH_END_FRAME_COUNT = 3;
    private const float CHARGE_DASH_SPEED = 3f;
    private const float CHARGE_MARK_ALPHA_START = 0.28f;
    private const float CHARGE_MARK_ALPHA_END = 0.92f;
    private const float CHARGE_MARK_MIN_THICKNESS = 0.35f;
    private const float CHARGE_MARK_THICKNESS = 0.15f;
    private const int CHARGE_MARK_SORTING_ORDER = 4;
    private const float SLAM_WINDUP_SECONDS = 0.5f;
    private const float SLAM_JUMP_SECONDS = 0.4f;
    private const float SLAM_JUMP_HEIGHT = 1.2f;
    private const float SLAM_MARK_ALPHA_START = 0.28f;
    private const float SLAM_MARK_ALPHA_END = 0.9f;
    private const int SLAM_MARK_SORTING_ORDER = 3;
    private const int SLAM_RING_SORTING_ORDER = 4;
    private const float SLAM_RING_ALPHA = 0.45f;
    // 부드러운 원 그림은 가장자리가 일찍 투명해진다. 이 비율이 테두리에 닿는다.
    private const float SLAM_FILL_EDGE = 0.8f;
    private const float DEFAULT_LUNGE_SPEED = 3f;
    private const int CIRCLE_ATTACK_COUNT = 3;
    private const float DEFAULT_CIRCLE_MOVE_SPEED = 3f;
    private const float DEFAULT_CIRCLE_RANGE = 2.5f;
    private const float DAMAGE_TEXT_INTERVAL = 0.2f;
    private const float DAMAGE_TEXT_MIN_VALUE = 1f;
    private const float DAMAGE_TEXT_OFFSET = 0.1f;
    private const string DAMAGE_ZERO_SOUND = "damage_zero";
    private const string DAMAGE_LOW_SOUND = "damage_low";
    private const string DAMAGE_MIDDLE_SOUND = "damage_middle";
    private const string DAMAGE_HIGH_SOUND = "damage_high";
    private static readonly MonsterType[] DEFAULT_DEFENDER_TYPES = { MonsterType.Normal };

    [Header("Drop")]
    [SerializeField] private ExperienceGem _experienceGem;
    private ExperienceGem _dropGem;
    [SerializeField] private CoinDropBehavior _pocketDollarDrop;
    [SerializeField, Range(0f, 100f)] private float _pocketDollarChance;
    [SerializeField] private CoinDropBehavior _monsterBallDrop;
    [SerializeField, Range(0f, 100f)] private float _monsterBallChance;
    [SerializeField] private Sprite _slamCircleSprite;

    private EnemyManager _owner;
    private EnemyView _view;
    private CircleCollider2D _hitCollider;
    private int _playerId;
    private float _health;
    private float _contactDamage;
    private float _skillDamage;
    private float _moveSpeed;
    private float _nextContactTime;
    private EnemyAttackKind _attackKind;
    private float _attackRange;
    private float _attackInterval;
    private float _skillCooldown;
    private int _id;
    private float _hitRadius;
    private float _attackDistance;
    private float _projectileSpeed;
    private Sprite _projectileSprite;
    private Sprite[] _chargeProjectileFrames = System.Array.Empty<Sprite>();
    private Sprite[] _flyProjectileFrames = System.Array.Empty<Sprite>();
    private float _nextShotTime;
    private float _shootPoseUntil;
    private int _waveIndex;
    private bool _disableOffscreenTeleport;
    private bool _isRushing;
    private Vector2 _rushDirection;
    private float _spawnedAt;
    private float _approachAt;
    private MonsterType[] _defenderTypes = DEFAULT_DEFENDER_TYPES;
    private BossSkillKind _skill;
    private ChargeDashPhase _chargePhase;
    private float _chargeUntil;
    private float _chargeSeconds;
    private float _dashSpeed;
    private float _dashRecoverSeconds;
    private Vector2 _dashDirection;
    private Vector2 _dashStop;
    private SpriteRenderer _chargeMark;
    private SlamPhase _slamPhase;
    private float _slamUntil;
    private float _slamChargeSeconds;
    private float _skillRange;
    private float _slamRecoverSeconds;
    private float _lungeRange;
    private float _lungeRecoverSeconds;
    private float _lungeSpeed;
    private LungePhase _lungePhase;
    private float _lungeUntil;
    private Vector2 _lungeDirection;
    private Vector2 _lungeStop;
    private bool _lungeHit;
    private bool _lungeArrived;
    private CirclePhase _circlePhase;
    private int _circleAttacks;
    private bool _circleHit;
    private float _circleUntil;
    private float _circleMoveSpeed;
    private float _circleRange;
    private float _circleGapSeconds;
    private Vector2 _circleLook;
    private Vector2 _circleCenter;
    private float _slamJumpStart;
    private Vector2 _slamLook;
    private Vector2 _slamOrigin;
    private Vector2 _slamCenter;
    private SpriteRenderer _slamMark;
    private SpriteRenderer _slamRing;
    private VolleyPhase _volleyPhase;
    private int _volleyReleased;
    private float _nextVolleyShotTime;
    private readonly EnemyProjectile[] _volley = new EnemyProjectile[VOLLEY_COUNT];
    private FanPhase _fanPhase;
    private float _fanCastRange;
    private int _fanCount;
    private float _fanDistance;
    private float _fanChargeStart;
    private Vector2 _fanAim;
    private int _fanSpawned;
    private readonly List<EnemyProjectile> _fanShots = new List<EnemyProjectile>();
    private readonly List<Vector2> _fanDirections = new List<Vector2>();
    private BeamPhase _beamPhase;
    private float _beamCastRange;
    private float _beamWidth;
    private float _beamLength;
    private float _beamSeconds;
    private float _beamHitInterval;
    private float _beamTurnSpeed;
    private float _beamNextHitTime;
    private int _beamFrameIndex;
    private float _beamFrameTimer;
    private Vector2 _beamAim;
    private int _mouthSector = int.MinValue;
    private Vector2 _mouthOffset;
    private Sprite[] _beamFrames = System.Array.Empty<Sprite>();
    private Vector2[] _beamMouthOffsets = System.Array.Empty<Vector2>();
    private PowderPhase _powderPhase;
    private float _powderCastUntil;
    private float _poolRange;
    private float _poolSpreadSpeed;
    private float _poolHitsPerSecond;
    private float _poolSlowPercent;
    private float _poolCastSeconds;
    private float _poolSeconds;
    private Sprite[] _poolBurstFrames = System.Array.Empty<Sprite>();
    private Sprite[] _poolLingerFrames = System.Array.Empty<Sprite>();
    private SpoonPhase _spoonPhase;
    private float _spoonChargeUntil;
    private int _spoonCount;
    private float _spoonChargeSeconds;
    private float _spoonSpeed;
    private float _spoonFlySeconds;
    private Sprite[] _spoonFrames = System.Array.Empty<Sprite>();
    private readonly List<SpoonShot> _spoons = new List<SpoonShot>();
    private SpriteRenderer _beamWarning;
    private SpriteRenderer _beamFill;
    private SpriteRenderer _beam;
    private float _damageTextValue;
    private float _lastTimeDamageText;
    private DamageTextKind _damageTextKind;
    private string _monsterName = string.Empty;

    private enum VolleyPhase
    {
        None = 0,
        Rising = 1,
        Firing = 2
    }

    private enum FanPhase
    {
        None = 0,
        Charging = 1,
        Attacking = 2
    }

    private enum BeamPhase
    {
        None = 0,
        Charging = 1,
        Firing = 2
    }

    private enum PowderPhase
    {
        None = 0,
        Casting = 1
    }

    private enum SpoonPhase
    {
        None = 0,
        Charging = 1,
        Releasing = 2,
        Shooting = 3
    }

    private enum SlamPhase
    {
        None = 0,
        Windup = 1,
        Filling = 2,
        Jumping = 3,
        Recovering = 4
    }

    private enum ChargeDashPhase
    {
        None = 0,
        Charging = 1,
        Dashing = 2,
        Recovering = 3
    }

    private enum LungePhase
    {
        None = 0,
        Dashing = 1,
        Recovering = 2
    }

    private enum CirclePhase
    {
        None = 0,
        Attacking = 1,
        Moving = 2
    }

    public int Id => _id;

    public float Health => _health;

    public int WaveIndex => _waveIndex;

    public bool IsAlive => _health > 0f;

    public bool DisableOffscreenTeleport => _disableOffscreenTeleport;

    public bool IsRushing => _isRushing;

    public float SpawnedAt => _spawnedAt;

    public ExperienceGem ExperienceGem => _dropGem != null ? _dropGem : _experienceGem;

    public CoinDropBehavior PocketDollarDrop => _pocketDollarDrop;

    public float PocketDollarChance => _pocketDollarChance;

    public CoinDropBehavior MonsterBallDrop => _monsterBallDrop;

    public float MonsterBallChance => _monsterBallChance;

    /// <summary>
    /// 그림에 맞춘 맞는 판정이 트랜스폼 중심에서 닿는 거리.
    /// </summary>
    public float BodyReach
    {
        get
        {
            if (_hitCollider == null)
            {
                return 0f;
            }

            var scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            return (_hitCollider.radius + _hitCollider.offset.magnitude) * scale;
        }
    }

    /// <summary>
    /// 다른 보스와 떨어질 때 쓰는 몸통 반경.
    /// </summary>
    public float SeparationRadius
    {
        get
        {
            if (_view == null || !_view.HasBodyBounds)
            {
                return 0.5f;
            }

            var extents = _view.BodyExtents;
            var scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            return Mathf.Max(extents.x, extents.y) * scale;
        }
    }

    #region Unity Methods

    private void Awake()
    {
        _view = GetComponent<EnemyView>();
        _hitCollider = GetComponent<CircleCollider2D>();
    }

    private void OnDestroy()
    {
        HideChargeMark();
        HideSlamMark();
        HideBeamVisuals();
        ClearOrbitSpoons();
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
    /// 이번 스폰의 경험치 구슬을 바꾼다. 비우면 프리팹 구슬을 쓴다.
    /// </summary>
    public void SetDropGem(ExperienceGem gem)
    {
        _dropGem = gem;
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
        _monsterName = visual != null ? visual.MonsterName : string.Empty;
        var resolvedScale = scale ?? (visual != null ? visual.Scale : MonsterVisualData.DEFAULT_SCALE);
        transform.localScale = new Vector3(resolvedScale, resolvedScale, 1f);

        if (_view != null)
        {
            _view.ApplyVisual(visual);
        }
    }

    /// <summary>
    /// hurt 칸에 죽는 그림이 있으면 true.
    /// </summary>
    public bool HasHurtSprite
    {
        get
        {
            if (_view == null)
            {
                _view = GetComponent<EnemyView>();
            }

            return _view != null && _view.HasHurtSprite;
        }
    }

    /// <summary>
    /// 지금 보는 방향의 hurt 그림을 한 번 재생한다. 칸이 비어 있으면 바로 끝난다.
    /// </summary>
    public UniTask PlayHurtAsync()
    {
        if (_view == null)
        {
            _view = GetComponent<EnemyView>();
        }

        if (_view == null)
        {
            return UniTask.CompletedTask;
        }

        return _view.PlayHurtAsync();
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
        float attackDistance = 0.45f,
        BossSkillKind skill = BossSkillKind.None,
        float skillCooldown = 0f,
        float chargeSeconds = 0f,
        float dashSpeed = 0f,
        float skillDamage = 0f,
        float slamChargeSeconds = 0f,
        float skillRange = 0f,
        float dashRecoverSeconds = 0f,
        float slamRecoverSeconds = 0f,
        float lungeRange = 0f,
        float lungeRecoverSeconds = 0f,
        float lungeSpeed = 0f,
        float circleMoveSpeed = 0f,
        float circleRange = 0f,
        float circleGapSeconds = 0f,
        float fanCastRange = 0f,
        int fanCount = 0,
        float fanDistance = 0f,
        Sprite[] chargeProjectileFrames = null,
        Sprite[] flyProjectileFrames = null,
        float beamCastRange = 0f,
        float beamWidth = 0f,
        float beamLength = 0f,
        float beamSeconds = 0f,
        float beamHitInterval = 0f,
        float beamTurnSpeed = 0f,
        Sprite[] beamFrames = null,
        Vector2[] beamMouthOffsets = null,
        float poolRange = 0f,
        float poolSpreadSpeed = 0f,
        float poolHitsPerSecond = 0f,
        float poolSlowPercent = -1f,
        float poolCastSeconds = 0f,
        float poolSeconds = 0f,
        Sprite[] poolBurstFrames = null,
        Sprite[] poolLingerFrames = null,
        int spoonCount = 0,
        float spoonChargeSeconds = 0f,
        float spoonSpeed = 0f,
        float spoonFlySeconds = 0f,
        Sprite[] spoonFrames = null)
    {
        CancelUnfiredVolley();
        CancelFanShots();
        HideChargeMark();
        HideSlamMark();
        HideBeamVisuals();
        _playerId = playerId;
        _waveIndex = waveIndex;
        _id = id;
        _health = maxHealth;
        _contactDamage = contactDamage;
        _skillDamage = skillDamage;
        _moveSpeed = moveSpeed;
        _attackKind = attackKind;
        _attackRange = attackRange;
        _attackInterval = attackInterval;
        _skillCooldown = ResolveSkillCooldown(skill, attackInterval, skillCooldown);
        _hitRadius = Mathf.Max(0f, hitRadius);
        _attackDistance = Mathf.Max(0.01f, attackDistance);
        _projectileSpeed = projectileSpeed;
        _projectileSprite = projectileSprite;
        _chargeProjectileFrames = chargeProjectileFrames;
        _flyProjectileFrames = flyProjectileFrames;
        _skill = skill;
        _volleyPhase = VolleyPhase.None;
        _volleyReleased = 0;
        _chargePhase = ChargeDashPhase.None;
        _chargeSeconds = chargeSeconds > 0f ? chargeSeconds : CHARGE_SECONDS;
        _dashSpeed = dashSpeed > 0f ? dashSpeed : moveSpeed * CHARGE_DASH_SPEED;
        _dashRecoverSeconds = Mathf.Max(0f, dashRecoverSeconds);
        _slamPhase = SlamPhase.None;
        _slamChargeSeconds = slamChargeSeconds > 0f ? slamChargeSeconds : 1.5f;
        _skillRange = skillRange > 0f ? skillRange : 2.5f;
        _slamRecoverSeconds = Mathf.Max(0f, slamRecoverSeconds);
        _lungeRange = lungeRange > 0f ? lungeRange : 2.5f;
        _lungeRecoverSeconds = Mathf.Max(0f, lungeRecoverSeconds);
        _lungeSpeed = lungeSpeed > 0f ? lungeSpeed : DEFAULT_LUNGE_SPEED;
        _lungePhase = LungePhase.None;
        _circlePhase = CirclePhase.None;
        _circleAttacks = 0;
        _circleHit = false;
        _circleMoveSpeed = circleMoveSpeed > 0f ? circleMoveSpeed : DEFAULT_CIRCLE_MOVE_SPEED;
        _circleRange = circleRange > 0f ? circleRange : DEFAULT_CIRCLE_RANGE;
        _circleGapSeconds = Mathf.Max(0f, circleGapSeconds);
        _fanPhase = FanPhase.None;
        _fanCastRange = fanCastRange > 0f ? fanCastRange : DEFAULT_FAN_CAST_RANGE;
        _fanCount = fanCount > 0 ? fanCount : DEFAULT_FAN_COUNT;
        _fanDistance = fanDistance > 0f ? fanDistance : DEFAULT_FAN_DISTANCE;
        _fanSpawned = 0;
        _beamPhase = BeamPhase.None;
        _beamCastRange = beamCastRange > 0f ? beamCastRange : DEFAULT_BEAM_CAST_RANGE;
        _beamWidth = beamWidth > 0f ? beamWidth : DEFAULT_BEAM_WIDTH;
        _beamLength = beamLength > 0f ? beamLength : DEFAULT_BEAM_LENGTH;
        _beamSeconds = beamSeconds > 0f ? beamSeconds : DEFAULT_BEAM_SECONDS;
        _beamHitInterval = beamHitInterval > 0f ? beamHitInterval : DEFAULT_BEAM_HIT_INTERVAL;
        _beamTurnSpeed = beamTurnSpeed > 0f ? beamTurnSpeed : DEFAULT_BEAM_TURN_SPEED;
        _beamFrames = beamFrames ?? System.Array.Empty<Sprite>();
        _beamMouthOffsets = beamMouthOffsets ?? System.Array.Empty<Vector2>();
        _powderPhase = PowderPhase.None;
        _poolRange = poolRange > 0f ? poolRange : DEFAULT_POOL_RANGE;
        _poolSpreadSpeed = poolSpreadSpeed > 0f ? poolSpreadSpeed : DEFAULT_POOL_SPREAD_SPEED;
        _poolHitsPerSecond = poolHitsPerSecond > 0f ? poolHitsPerSecond : DEFAULT_POOL_HITS_PER_SECOND;
        _poolSlowPercent = poolSlowPercent >= 0f ? Mathf.Clamp(poolSlowPercent, 0f, 100f) : DEFAULT_POOL_SLOW_PERCENT;
        _poolCastSeconds = poolCastSeconds > 0f ? poolCastSeconds : DEFAULT_POOL_CAST_SECONDS;
        _poolSeconds = poolSeconds > 0f ? poolSeconds : DEFAULT_POOL_SECONDS;
        _poolBurstFrames = poolBurstFrames ?? System.Array.Empty<Sprite>();
        _poolLingerFrames = poolLingerFrames ?? System.Array.Empty<Sprite>();
        ClearOrbitSpoons();
        _spoonPhase = SpoonPhase.None;
        _spoonCount = spoonCount > 0 ? spoonCount : DEFAULT_SPOON_COUNT;
        _spoonChargeSeconds = spoonChargeSeconds > 0f ? spoonChargeSeconds : DEFAULT_SPOON_CHARGE_SECONDS;
        _spoonSpeed = spoonSpeed > 0f ? spoonSpeed : DEFAULT_SPOON_SPEED;
        _spoonFlySeconds = spoonFlySeconds > 0f ? spoonFlySeconds : DEFAULT_SPOON_FLY_SECONDS;
        _spoonFrames = spoonFrames ?? System.Array.Empty<Sprite>();
        _mouthSector = int.MinValue;
        _beamAim = Vector2.right;
        _nextContactTime = 0f;
        _nextShotTime = UsesSkillCooldown(skill) ? Time.time + _skillCooldown : 0f;
        _shootPoseUntil = 0f;
        _disableOffscreenTeleport = false;
        _isRushing = false;
        _rushDirection = Vector2.zero;
        _spawnedAt = Time.time;
        _approachAt = Time.time;
        _damageTextValue = 0f;
        _lastTimeDamageText = 0f;
        _damageTextKind = DamageTextKind.Neutral;

        if (_view == null)
        {
            _view = GetComponent<EnemyView>();
        }

        if (_view != null)
        {
            _view.SetIdleMove(skill == BossSkillKind.CircleVolley);
            _view.SetNamedSkill(null);
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
    /// 이 시간 동안은 플레이어 쪽으로 걸어가지 않는다.
    /// </summary>
    public void HoldApproach(float seconds)
    {
        _approachAt = Time.time + Mathf.Max(0f, seconds);
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

        if (_skill == BossSkillKind.Slam)
        {
            return TickSlam(playerPosition);
        }

        if (_skill == BossSkillKind.Lunge || _skill == BossSkillKind.NidokingLunge)
        {
            return TickLunge(playerPosition);
        }

        if (_skill == BossSkillKind.CircleVolley)
        {
            return TickCircleVolley(playerPosition);
        }

        if (_skill == BossSkillKind.ChargeDash)
        {
            return TickChargeDash(playerPosition);
        }

        if (_skill == BossSkillKind.RisingVolley)
        {
            return TickRisingVolley(playerPosition);
        }

        if (_skill == BossSkillKind.FanVolley)
        {
            return TickFanVolley(playerPosition);
        }

        if (_skill == BossSkillKind.TrackingBeam)
        {
            return TickBeam(playerPosition);
        }

        if (_skill == BossSkillKind.SleepPowder)
        {
            return TickSleepPowder(playerPosition);
        }

        if (_skill == BossSkillKind.SpoonRing)
        {
            return TickSpoonRing(playerPosition);
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
    /// 실제로 깎인 양은 출처와 함께 EnemyDamaged로 알린다.
    /// </summary>
    public void ApplyDamage(float amount, MonsterType attackType, DamageSource source)
    {
        if (!IsAlive)
        {
            return;
        }

        var multiplier = TypeChart.GetMultiplier(attackType, _defenderTypes);
        if (multiplier <= 0f)
        {
            PlayDamageSound(DamageTextKind.Immune);
            PublishImmuneText();
            return;
        }

        var dealt = amount * multiplier;
        if (dealt <= 0f)
        {
            return;
        }

        var healthBefore = _health;
        _health = Mathf.Max(0f, _health - dealt);
        _damageTextKind = DamageTextRequested.FromMultiplier(multiplier);
        PlayDamageSound(_damageTextKind);
        PublishDamageText(dealt);
        PublishDamaged(source, healthBefore - _health, _health <= 0f);
        if (_health > 0f)
        {
            return;
        }

        HideChargeMark();
        HideSlamMark();
        HideBeamVisuals();
        CancelFanShots();
        ClearOrbitSpoons();
        if (_owner != null)
        {
            _owner.NotifyDied(this);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 효과가 없으면 zero, 별로면 low, 1배면 middle, 효과가 굉장하면 high를 튼다.
    /// </summary>
    private static void PlayDamageSound(DamageTextKind kind)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(GetDamageSoundName(kind));
    }

    private static string GetDamageSoundName(DamageTextKind kind)
    {
        switch (kind)
        {
            case DamageTextKind.Immune:
                return DAMAGE_ZERO_SOUND;
            case DamageTextKind.VeryResisted:
            case DamageTextKind.Resisted:
                return DAMAGE_LOW_SOUND;
            case DamageTextKind.Super:
            case DamageTextKind.VerySuper:
                return DAMAGE_HIGH_SOUND;
            default:
                return DAMAGE_MIDDLE_SOUND;
        }
    }

    private void PublishImmuneText()
    {
        if (Time.unscaledTime - _lastTimeDamageText <= DAMAGE_TEXT_INTERVAL)
        {
            return;
        }

        _lastTimeDamageText = Time.unscaledTime;
        var message = string.IsNullOrEmpty(_monsterName)
            ? "효과가 없는 것 같다..."
            : _monsterName + "에게는 효과가 없는 것 같다...";
        PublishDamageTextEvent(message, DamageTextKind.Immune);
    }

    private void PublishDamageText(float dealt)
    {
        _damageTextValue += dealt;
        if (Time.unscaledTime - _lastTimeDamageText <= DAMAGE_TEXT_INTERVAL || _damageTextValue < DAMAGE_TEXT_MIN_VALUE)
        {
            return;
        }

        var damageText = Mathf.RoundToInt(_damageTextValue).ToString();
        _damageTextValue = 0f;
        _lastTimeDamageText = Time.unscaledTime;
        PublishDamageTextEvent(damageText, _damageTextKind);
    }

    private static void PublishDamaged(DamageSource source, float amount, bool killed)
    {
        if (amount > 0f && Managers.Instance != null && Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            eventManager.Publish(new EnemyDamaged(source, amount, killed));
        }
    }

    private void PublishDamageTextEvent(string text, DamageTextKind kind)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            return;
        }

        var position = (Vector2)transform.position + new Vector2(
            Random.Range(-DAMAGE_TEXT_OFFSET, DAMAGE_TEXT_OFFSET),
            Random.value * DAMAGE_TEXT_OFFSET);
        eventManager.Publish(new DamageTextRequested(position, text, kind));
    }

    private bool TickRush(Vector2 playerPosition)
    {
        transform.position += (Vector3)(_rushDirection * _moveSpeed * Time.deltaTime);
        if (_view != null)
        {
            _view.SetVisual(true, _rushDirection);
        }

        return TryContactDamage(playerPosition);
    }

    private bool TickRisingVolley(Vector2 playerPosition)
    {
        if (_volleyPhase == VolleyPhase.None && Time.time >= _nextShotTime)
        {
            BeginVolley();
        }

        if (_volleyPhase != VolleyPhase.None)
        {
            var look = playerPosition - (Vector2)transform.position;
            SetAttackView(look);
            TickVolley(playerPosition);
            return TryContactDamage(playerPosition);
        }

        MoveToward(playerPosition, Time.deltaTime);
        return TryContactDamage(playerPosition);
    }

    private void BeginVolley()
    {
        if (_owner == null)
        {
            _nextShotTime = Time.time + _skillCooldown;
            return;
        }

        var origin = (Vector2)transform.position;
        var originX = -((VOLLEY_COUNT - 1) * VOLLEY_SPREAD) * 0.5f;
        var armed = 0;
        for (var i = 0; i < VOLLEY_COUNT; i++)
        {
            var swayPhase = (i + 1) * 1.37f;
            var hover = origin + new Vector2(
                originX + i * VOLLEY_SPREAD + Mathf.Sin(swayPhase) * 0.55f,
                VOLLEY_RISE_HEIGHT + Mathf.Cos(swayPhase * 0.8f) * 0.4f);
            var shot = _owner.ArmRisingProjectile(
                _playerId,
                origin,
                hover,
                VOLLEY_RISE_SECONDS,
                _attackDistance,
                _projectileSprite,
                swayPhase);
            _volley[i] = shot;
            if (shot != null)
            {
                armed++;
            }
        }

        if (armed == 0)
        {
            _nextShotTime = Time.time + _skillCooldown;
            return;
        }

        _volleyPhase = VolleyPhase.Rising;
        _volleyReleased = 0;
    }

    private void TickVolley(Vector2 playerPosition)
    {
        if (_volleyPhase == VolleyPhase.Rising)
        {
            if (!HasVolleyRisen())
            {
                return;
            }

            _volleyPhase = VolleyPhase.Firing;
            _nextVolleyShotTime = Time.time;
        }

        if (_volleyPhase != VolleyPhase.Firing || Time.time < _nextVolleyShotTime)
        {
            return;
        }

        ReleaseNextVolleyShot(playerPosition);
    }

    private bool HasVolleyRisen()
    {
        for (var i = 0; i < _volley.Length; i++)
        {
            var shot = _volley[i];
            if (shot != null && shot.IsRising)
            {
                return false;
            }
        }

        return true;
    }

    private void ReleaseNextVolleyShot(Vector2 playerPosition)
    {
        while (_volleyReleased < _volley.Length && _volley[_volleyReleased] == null)
        {
            _volleyReleased++;
        }

        if (_volleyReleased >= _volley.Length)
        {
            FinishVolley();
            return;
        }

        var shot = _volley[_volleyReleased];
        var origin = (Vector2)shot.transform.position;
        var toPlayer = playerPosition - origin;
        shot.Release(toPlayer, _projectileSpeed, VOLLEY_RANGE, _skillDamage, _hitRadius);
        _volley[_volleyReleased] = null;
        _volleyReleased++;
        if (_volleyReleased >= _volley.Length)
        {
            FinishVolley();
            return;
        }

        _nextVolleyShotTime = Time.time + VOLLEY_SHOT_GAP;
    }

    private void FinishVolley()
    {
        _volleyPhase = VolleyPhase.None;
        _nextShotTime = Time.time + _skillCooldown;
    }

    private bool TickFanVolley(Vector2 playerPosition)
    {
        if (_fanPhase == FanPhase.Charging)
        {
            TickFanCharge();
            return TryContactDamage(playerPosition);
        }

        if (_fanPhase == FanPhase.Attacking)
        {
            TickFanAttack();
            return TryContactDamage(playerPosition);
        }

        if (Time.time >= _nextShotTime && Vector2.Distance(transform.position, playerPosition) <= _fanCastRange)
        {
            BeginFanCharge(playerPosition);
            return TryContactDamage(playerPosition);
        }

        MoveToward(playerPosition, Time.deltaTime);
        return TryContactDamage(playerPosition);
    }

    private void BeginFanCharge(Vector2 playerPosition)
    {
        var toPlayer = playerPosition - (Vector2)transform.position;
        // 차지 시작 방향을 고정한다. 끝나는 순간까지 부채꼴은 그 앞을 유지한다.
        _fanAim = toPlayer.sqrMagnitude > MOVE_SQR_EPSILON ? toPlayer.normalized : Vector2.right;
        _fanPhase = FanPhase.Charging;
        _fanChargeStart = Time.time;
        _chargeUntil = Time.time + _chargeSeconds;
        _fanSpawned = 0;
        _fanShots.Clear();
        _fanDirections.Clear();
        SetChargeView(_fanAim);
    }

    private void TickFanCharge()
    {
        SetChargeView(_fanAim);
        var count = Mathf.Max(1, _fanCount);
        while (_fanSpawned < count && Time.time >= FanSpawnTime(_fanSpawned, count))
        {
            ArmFanShot(_fanSpawned, count);
            _fanSpawned++;
        }

        if (Time.time < _chargeUntil)
        {
            return;
        }

        BeginFanRelease();
    }

    private float FanSpawnTime(int index, int count)
    {
        return _fanChargeStart + _chargeSeconds * (index + 1) / (count + 1f);
    }

    private static bool HasFrames(Sprite[] frames)
    {
        return frames != null && frames.Length > 0;
    }

    private void ArmFanShot(int index, int count)
    {
        var direction = FanDirection(index, count);
        _fanDirections.Add(direction);
        if (_owner == null)
        {
            _fanShots.Add(null);
            return;
        }

        // 몸에서 떠오르지 않고, 조준 방향 앞에 바로 둔다.
        var hover = (Vector2)transform.position + direction * FAN_HOVER_RADIUS;
        var shot = HasFrames(_chargeProjectileFrames) || HasFrames(_flyProjectileFrames)
            ? _owner.ArmFanProjectile(
                _playerId,
                hover,
                hover,
                0f,
                _attackDistance,
                _chargeProjectileFrames,
                _flyProjectileFrames,
                index + 1f)
            : _owner.ArmRisingProjectile(
                _playerId,
                hover,
                hover,
                0f,
                _attackDistance,
                _projectileSprite,
                index + 1f);
        _fanShots.Add(shot);
    }

    private Vector2 FanDirection(int index, int count)
    {
        var offset = count <= 1
            ? 0f
            : Mathf.Lerp(-FAN_ARC_DEGREES * 0.5f, FAN_ARC_DEGREES * 0.5f, index / (float)(count - 1));
        var radians = offset * Mathf.Deg2Rad;
        var cos = Mathf.Cos(radians);
        var sin = Mathf.Sin(radians);
        return new Vector2(
            _fanAim.x * cos - _fanAim.y * sin,
            _fanAim.x * sin + _fanAim.y * cos);
    }

    private void BeginFanRelease()
    {
        for (var i = 0; i < _fanShots.Count; i++)
        {
            var shot = _fanShots[i];
            if (shot == null)
            {
                continue;
            }

            var direction = i < _fanDirections.Count ? _fanDirections[i] : _fanAim;
            shot.Release(direction, _projectileSpeed, _fanDistance, _skillDamage, _hitRadius);
        }

        _fanShots.Clear();
        _fanDirections.Clear();
        _fanPhase = FanPhase.Attacking;
        if (_view != null)
        {
            _view.PlayAttackLunge(_fanAim, false);
        }
    }

    private void TickFanAttack()
    {
        if (_view != null)
        {
            // 장은 SetVisual이 불릴 때만 넘어간다. 한 번만 부르면 공격 그림에서 멈춘다.
            _view.PlayAttackLunge(_fanAim, false);
            if (!_view.IsStrikeFinished)
            {
                return;
            }
        }

        _fanPhase = FanPhase.None;
        _nextShotTime = Time.time + _skillCooldown;
    }

    private void CancelFanShots()
    {
        for (var i = 0; i < _fanShots.Count; i++)
        {
            var shot = _fanShots[i];
            if (shot != null)
            {
                shot.CancelIfUnfired();
            }
        }

        _fanShots.Clear();
        _fanDirections.Clear();
        _fanPhase = FanPhase.None;
        _fanSpawned = 0;
    }

    /// <summary>
    /// 숟가락. 차지 동안 원으로 두고, 끝나면 각 방향으로 날린다.
    /// </summary>
    private bool TickSpoonRing(Vector2 playerPosition)
    {
        if (_spoonPhase == SpoonPhase.Charging)
        {
            if (_view != null)
            {
                _view.SetChargeDown();
            }

            PlaceOrbitSpoons();
            if (Time.time >= _spoonChargeUntil)
            {
                _spoonPhase = SpoonPhase.Releasing;
                if (_view != null)
                {
                    _view.PlayShootDownOnce();
                }
            }

            return TryContactDamage(playerPosition);
        }

        if (_spoonPhase == SpoonPhase.Releasing)
        {
            if (_view != null)
            {
                _view.PlayShootDownOnce();
            }

            PlaceOrbitSpoons();
            if (IsSpoonReleaseFrame())
            {
                ReleaseSpoons();
            }

            return TryContactDamage(playerPosition);
        }

        if (_spoonPhase == SpoonPhase.Shooting)
        {
            if (_view != null)
            {
                _view.PlayShootDownOnce();
            }

            if (_view == null || _view.IsShootDownFinished)
            {
                _spoonPhase = SpoonPhase.None;
            }

            return TryContactDamage(playerPosition);
        }

        if (Time.time >= _nextShotTime)
        {
            BeginSpoonCharge();
            return TryContactDamage(playerPosition);
        }

        MoveToward(playerPosition, Time.deltaTime);
        return TryContactDamage(playerPosition);
    }

    private void BeginSpoonCharge()
    {
        ClearOrbitSpoons();
        _spoonPhase = SpoonPhase.Charging;
        _spoonChargeUntil = Time.time + _spoonChargeSeconds;
        var count = Mathf.Max(1, _spoonCount);
        for (var i = 0; i < count; i++)
        {
            var direction = SpoonDirection(i, count);
            var spoon = SpoonShot.Create((Vector2)transform.position + direction * SPOON_ORBIT_RADIUS, _spoonFrames);
            spoon.SetSpinPhase(i, count);
            _spoons.Add(spoon);
        }

        if (_view != null)
        {
            _view.SetChargeDown();
        }
    }

    private void PlaceOrbitSpoons()
    {
        var count = _spoons.Count;
        for (var i = 0; i < count; i++)
        {
            var spoon = _spoons[i];
            if (spoon == null)
            {
                continue;
            }

            spoon.Place((Vector2)transform.position + SpoonDirection(i, count) * SPOON_ORBIT_RADIUS);
        }
    }

    private void ReleaseSpoons()
    {
        var count = _spoons.Count;
        for (var i = 0; i < count; i++)
        {
            var spoon = _spoons[i];
            if (spoon == null)
            {
                continue;
            }

            spoon.Launch(SpoonDirection(i, count), _spoonSpeed, _spoonFlySeconds, _playerId, _skillDamage);
        }

        _spoons.Clear();
        _spoonPhase = SpoonPhase.Shooting;
        _nextShotTime = Time.time + _skillCooldown;
        if (_view != null)
        {
            _view.PlayShootDownOnce();
        }
    }

    private bool IsSpoonReleaseFrame()
    {
        if (_view == null || _view.ShootDownFrameCount <= 0)
        {
            return true;
        }

        var releaseFrame = Mathf.Min(SPOON_SHOOT_RELEASE_FRAME, _view.ShootDownFrameCount - 1);
        return _view.ShootDownFrameIndex >= releaseFrame || _view.IsShootDownFinished;
    }

    private void ClearOrbitSpoons()
    {
        for (var i = 0; i < _spoons.Count; i++)
        {
            var spoon = _spoons[i];
            if (spoon != null)
            {
                Destroy(spoon.gameObject);
            }
        }

        _spoons.Clear();
    }

    private static Vector2 SpoonDirection(int index, int count)
    {
        var turns = count <= 0 ? 0f : index / (float)count;
        var radians = turns * Mathf.PI * 2f;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    /// <summary>
    /// 수면가루. 시전 동안만 멈추고, 장판은 깔린 자리에서 따로 퍼진다.
    /// </summary>
    private bool TickSleepPowder(Vector2 playerPosition)
    {
        if (_powderPhase == PowderPhase.Casting)
        {
            if (_view != null)
            {
                _view.SetHopDown(_poolCastSeconds);
            }

            if (Time.time >= _powderCastUntil)
            {
                _powderPhase = PowderPhase.None;
                _nextShotTime = Time.time + _skillCooldown;
            }

            return TryContactDamage(playerPosition);
        }

        if (Time.time >= _nextShotTime)
        {
            BeginSleepPowder();
            return TryContactDamage(playerPosition);
        }

        MoveToward(playerPosition, Time.deltaTime);
        return TryContactDamage(playerPosition);
    }

    private void BeginSleepPowder()
    {
        _powderPhase = PowderPhase.Casting;
        _powderCastUntil = Time.time + _poolCastSeconds;
        SleepPowderField.Create(
            transform.position,
            _playerId,
            _skillDamage,
            _poolRange,
            _poolSpreadSpeed,
            _poolHitsPerSecond,
            _poolSlowPercent,
            _poolSeconds,
            _poolBurstFrames,
            _poolLingerFrames);
        if (_view != null)
        {
            _view.SetHopDown(_poolCastSeconds);
        }
    }

    /// <summary>
    /// 추적 빔. 차지 중에는 빨간 예고만 깔고, 쏘는 동안 직선 빔으로 맞춘다.
    /// </summary>
    private bool TickBeam(Vector2 playerPosition)
    {
        if (_beamPhase == BeamPhase.Charging)
        {
            TickBeamCharge(playerPosition);
            return TryContactDamage(playerPosition);
        }

        if (_beamPhase == BeamPhase.Firing)
        {
            TickBeamFire(playerPosition);
            return TryContactDamage(playerPosition);
        }

        if (Time.time >= _nextShotTime && Vector2.Distance(transform.position, playerPosition) <= _beamCastRange)
        {
            BeginBeamCharge(playerPosition);
            return TryContactDamage(playerPosition);
        }

        MoveToward(playerPosition, Time.deltaTime);
        return TryContactDamage(playerPosition);
    }

    private void BeginBeamCharge(Vector2 playerPosition)
    {
        _beamAim = AimDirection(playerPosition);
        _beamPhase = BeamPhase.Charging;
        _chargeUntil = Time.time + _chargeSeconds;
        if (_view != null)
        {
            _view.SetNamedSkill(REAR_UP_SKILL);
        }

        _mouthSector = int.MinValue;
        SetChargeView(_beamAim);
    }

    private void TickBeamCharge(Vector2 playerPosition)
    {
        TurnBeamToward(playerPosition, Time.deltaTime);
        SetChargeView(_beamAim);
        var duration = Mathf.Max(MIN_SHOT_INTERVAL, _chargeSeconds);
        var progress = 1f - Mathf.Clamp01((_chargeUntil - Time.time) / duration);
        ShowBeamWarning(_beamAim, progress);
        if (Time.time < _chargeUntil)
        {
            return;
        }

        BeginBeamFire();
    }

    private void BeginBeamFire()
    {
        HideBeamWarning();
        _beamPhase = BeamPhase.Firing;
        _chargeUntil = Time.time + _beamSeconds;
        _beamNextHitTime = Time.time;
        _beamFrameIndex = 0;
        _beamFrameTimer = 0f;
        if (_view != null)
        {
            _view.SetNamedSkill(null);
        }

        _mouthSector = int.MinValue;
        SetShootView(_beamAim);
    }

    private void TickBeamFire(Vector2 playerPosition)
    {
        TurnBeamToward(playerPosition, Time.deltaTime);
        SetShootView(_beamAim);
        ShowBeam(_beamAim);
        TryBeamHit(playerPosition);
        if (Time.time < _chargeUntil)
        {
            return;
        }

        FinishBeam();
    }

    private void FinishBeam()
    {
        _beamPhase = BeamPhase.None;
        HideBeamVisuals();
        if (_view != null)
        {
            _view.SetNamedSkill(null);
        }

        _nextShotTime = Time.time + _skillCooldown;
    }

    private void TurnBeamToward(Vector2 playerPosition, float deltaTime)
    {
        var target = AimDirection(playerPosition);
        var current = Mathf.Atan2(_beamAim.y, _beamAim.x) * Mathf.Rad2Deg;
        var desired = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;
        var next = Mathf.MoveTowardsAngle(current, desired, _beamTurnSpeed * deltaTime) * Mathf.Deg2Rad;
        _beamAim = new Vector2(Mathf.Cos(next), Mathf.Sin(next));
    }

    private Vector2 AimDirection(Vector2 playerPosition)
    {
        var toPlayer = playerPosition - (Vector2)transform.position;
        return toPlayer.sqrMagnitude > MOVE_SQR_EPSILON ? toPlayer.normalized : Vector2.right;
    }

    private void TryBeamHit(Vector2 playerPosition)
    {
        if (Time.time < _beamNextHitTime || !IsInsideBeam(playerPosition))
        {
            return;
        }

        DealSkillDamage();
        _beamNextHitTime = Time.time + _beamHitInterval;
    }

    private bool IsInsideBeam(Vector2 playerPosition)
    {
        var offset = playerPosition - BeamOrigin(_beamAim);
        var along = Vector2.Dot(offset, _beamAim);
        var side = offset - _beamAim * along;
        return along >= 0f && along <= _beamLength && side.sqrMagnitude <= _beamWidth * _beamWidth * 0.25f;
    }

    /// <summary>
    /// 빔이 바라보는 방향의 입 좌표에서 시작하게 한다.
    /// </summary>
    private Vector2 BeamOrigin(Vector2 aim)
    {
        var sector = FacingSector(aim);
        if (sector != _mouthSector)
        {
            _mouthSector = sector;
            var scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            _mouthOffset = BossPartyMember.MouthOffset(_beamMouthOffsets, sector) * scale;
        }

        return (Vector2)transform.position + _mouthOffset;
    }

    private static int FacingSector(Vector2 aim)
    {
        var degrees = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        if (degrees < 0f)
        {
            degrees += 360f;
        }

        return Mathf.RoundToInt(degrees / 45f) % BossPartyMember.MOUTH_SECTOR_COUNT;
    }

    private void ShowBeamWarning(Vector2 aim, float progress)
    {
        var origin = BeamOrigin(aim);
        var lane = EnsureBeamBar(ref _beamWarning, "BeamWarning", BEAM_SORTING_ORDER - 1);
        var fill = EnsureBeamBar(ref _beamFill, "BeamFill", BEAM_SORTING_ORDER);
        lane.color = BEAM_WARNING_COLOR;
        fill.color = BEAM_FILL_COLOR;
        PlaceBeamBar(lane, origin, aim, _beamLength, _beamWidth);
        PlaceBeamBar(fill, origin, aim, _beamLength * Mathf.Clamp01(progress), _beamWidth);
    }

    private void ShowBeam(Vector2 aim)
    {
        var beam = EnsureBeamBar(ref _beam, "TrackingBeam", BEAM_SORTING_ORDER + 1);
        var frames = _beamFrames;
        if (frames != null && frames.Length > 0)
        {
            AdvanceBeamFrames(beam, frames, ref _beamFrameIndex, ref _beamFrameTimer);
            beam.color = Color.white;
        }
        else
        {
            beam.sprite = PrototypeSprite.WhiteSquare;
            beam.color = BEAM_FALLBACK_COLOR;
        }

        PlaceBeamBar(beam, BeamOrigin(aim), aim, _beamLength, _beamWidth);
    }

    private void AdvanceBeamFrames(SpriteRenderer beam, Sprite[] frames, ref int frameIndex, ref float frameTimer)
    {
        frameTimer += Time.deltaTime;
        var frameDuration = 1f / BEAM_FRAME_RATE;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % frames.Length;
        }

        var index = Mathf.Clamp(frameIndex, 0, frames.Length - 1);
        if (frames[index] != null)
        {
            beam.sprite = frames[index];
        }
    }

    private SpriteRenderer EnsureBeamBar(ref SpriteRenderer renderer, string objectName, int sortingOrder)
    {
        if (renderer != null)
        {
            return renderer;
        }

        var barObject = new GameObject(objectName);
        renderer = barObject.AddComponent<SpriteRenderer>();
        renderer.sprite = PrototypeSprite.WhiteSquare;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static void PlaceBeamBar(SpriteRenderer renderer, Vector2 origin, Vector2 aim, float length, float width)
    {
        var sprite = renderer.sprite;
        var size = sprite != null ? sprite.bounds.size : Vector3.one;
        var scaleX = length / Mathf.Max(0.01f, size.x);
        var scaleY = width / Mathf.Max(0.01f, size.y);
        renderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        var angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        renderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        var center = sprite != null ? (Vector2)sprite.bounds.center : Vector2.zero;
        var midpoint = origin + aim * (length * 0.5f);
        var pivotShift = (Vector2)(renderer.transform.rotation * new Vector3(center.x * scaleX, center.y * scaleY, 0f));
        renderer.transform.position = midpoint - pivotShift;
        renderer.enabled = length > 0.01f;
    }

    private void HideBeamWarning()
    {
        DestroyBar(ref _beamWarning);
        DestroyBar(ref _beamFill);
    }

    /// <summary>
    /// 예고와 빔을 끄고 시전 상태를 비운다.
    /// </summary>
    private void HideBeamVisuals()
    {
        HideBeamWarning();
        DestroyBar(ref _beam);
        _beamPhase = BeamPhase.None;
    }

    private static void DestroyBar(ref SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        Destroy(renderer.gameObject);
        renderer = null;
    }

    private void SetShootView(Vector2 lookDirection)
    {
        if (_view == null)
        {
            return;
        }

        _view.SetBeamShoot(lookDirection);
    }

    private bool TickChargeDash(Vector2 playerPosition)
    {
        if (_chargePhase == ChargeDashPhase.None)
        {
            if (Time.time < _nextShotTime)
            {
                return TickContact(playerPosition);
            }

            BeginCharge();
        }

        if (_chargePhase == ChargeDashPhase.Charging)
        {
            TickCharge(playerPosition);
        }
        else if (_chargePhase == ChargeDashPhase.Dashing)
        {
            TickDash();
        }

        if (_chargePhase == ChargeDashPhase.Recovering)
        {
            TickDashRecover();
            return TryContactDamage(playerPosition);
        }

        if (_chargePhase == ChargeDashPhase.Dashing)
        {
            return TryContactDamage(playerPosition, _skillDamage);
        }

        return TryContactDamage(playerPosition);
    }

    private void BeginCharge()
    {
        _chargePhase = ChargeDashPhase.Charging;
        _chargeUntil = Time.time + _chargeSeconds;
    }

    private void TickCharge(Vector2 playerPosition)
    {
        var look = playerPosition - (Vector2)transform.position;
        SetChargeView(look);
        UpdateChargeMark(playerPosition);
        if (Time.time < _chargeUntil)
        {
            return;
        }

        BeginDash(playerPosition);
    }

    private void BeginDash(Vector2 playerPosition)
    {
        var origin = (Vector2)transform.position;
        var toPlayer = playerPosition - origin;
        _dashDirection = toPlayer.sqrMagnitude > MOVE_SQR_EPSILON ? toPlayer.normalized : Vector2.right;
        _dashStop = origin + _dashDirection * (toPlayer.magnitude + CHARGE_DASH_OVERSHOOT);
        _chargePhase = ChargeDashPhase.Dashing;
        HideChargeMark();
        HideSlamMark();
    }

    private void TickDash()
    {
        var position = (Vector2)transform.position;
        var toStop = _dashStop - position;
        var step = _dashSpeed * Time.deltaTime;
        if (toStop.sqrMagnitude <= step * step || Vector2.Dot(toStop, _dashDirection) <= 0f)
        {
            transform.position = _dashStop;
            BeginDashRecover();
            return;
        }

        transform.position = position + _dashDirection * step;
        SetAttackView(_dashDirection);
    }

    private void BeginDashRecover()
    {
        _chargePhase = ChargeDashPhase.Recovering;
        var poseSeconds = _view != null ? _view.BeginAttackTail(_dashDirection, DASH_END_FRAME_COUNT) : 0f;
        _chargeUntil = Time.time + Mathf.Max(_dashRecoverSeconds, poseSeconds);
    }

    private void TickDashRecover()
    {
        SetAttackView(_dashDirection);
        if (Time.time < _chargeUntil)
        {
            return;
        }

        FinishChargeDash();
    }

    private void FinishChargeDash()
    {
        _chargePhase = ChargeDashPhase.None;
        _nextShotTime = Time.time + _skillCooldown;
    }

    private void SetChargeView(Vector2 lookDirection)
    {
        if (_view == null)
        {
            return;
        }

        _view.SetVisual(false, lookDirection, false, false, true);
    }

    private void UpdateChargeMark(Vector2 playerPosition)
    {
        var origin = (Vector2)transform.position;
        var toPlayer = playerPosition - origin;
        var direction = toPlayer.sqrMagnitude > MOVE_SQR_EPSILON ? toPlayer.normalized : Vector2.right;
        var length = toPlayer.magnitude + CHARGE_DASH_OVERSHOOT;
        var mark = EnsureChargeMark();
        mark.transform.position = origin;
        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        mark.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        var thickness = Mathf.Max(CHARGE_MARK_MIN_THICKNESS, Mathf.Abs(transform.lossyScale.x) * CHARGE_MARK_THICKNESS);
        mark.transform.localScale = new Vector3(length / PrototypeSprite.CHARGE_LANE_WIDTH, thickness, 1f);
        var blend = 1f - Mathf.Clamp01((_chargeUntil - Time.time) / _chargeSeconds);
        mark.color = new Color(1f, 1f, 1f, Mathf.Lerp(CHARGE_MARK_ALPHA_START, CHARGE_MARK_ALPHA_END, blend));
    }

    private SpriteRenderer EnsureChargeMark()
    {
        if (_chargeMark != null)
        {
            return _chargeMark;
        }

        var markObject = new GameObject("ChargeDashMark");
        _chargeMark = markObject.AddComponent<SpriteRenderer>();
        _chargeMark.sprite = PrototypeSprite.ChargeLane;
        _chargeMark.sortingOrder = CHARGE_MARK_SORTING_ORDER;
        return _chargeMark;
    }

    private bool TickLunge(Vector2 playerPosition)
    {
        if (_lungePhase == LungePhase.None)
        {
            if (Time.time < _nextShotTime || Vector2.Distance(transform.position, playerPosition) > _lungeRange)
            {
                return TickContact(playerPosition);
            }

            BeginLunge(playerPosition);
        }

        if (_lungePhase == LungePhase.Dashing)
        {
            TickLungeDash();
            if (!TryLungeHit(playerPosition))
            {
                return false;
            }
        }

        if (_lungePhase == LungePhase.Recovering)
        {
            TickLungeRecover();
            return TryContactDamage(playerPosition);
        }

        return true;
    }

    private void BeginLunge(Vector2 playerPosition)
    {
        var origin = (Vector2)transform.position;
        var toPlayer = playerPosition - origin;
        _lungeDirection = toPlayer.sqrMagnitude > MOVE_SQR_EPSILON ? toPlayer.normalized : Vector2.down;
        _lungeStop = playerPosition;
        _lungeHit = false;
        _lungeArrived = false;
        _lungePhase = LungePhase.Dashing;
    }

    private void TickLungeDash()
    {
        if (!_lungeArrived)
        {
            var position = (Vector2)transform.position;
            var toStop = _lungeStop - position;
            var step = _lungeSpeed * Time.deltaTime;
            if (toStop.sqrMagnitude <= step * step || Vector2.Dot(toStop, _lungeDirection) <= 0f)
            {
                transform.position = _lungeStop;
                _lungeArrived = true;
            }
            else
            {
                transform.position = position + _lungeDirection * step;
            }
        }

        PlayLungeClip(_lungeDirection, !_lungeArrived);
        if (_view == null || _view.IsStrikeFinished)
        {
            BeginLungeRecover();
        }
    }

    private void BeginLungeRecover()
    {
        _lungePhase = LungePhase.Recovering;
        _lungeUntil = Time.time + _lungeRecoverSeconds;
        HoldLungeClip(_lungeDirection);
    }

    private void TickLungeRecover()
    {
        HoldLungeClip(_lungeDirection);
        if (Time.time < _lungeUntil)
        {
            return;
        }

        _lungePhase = LungePhase.None;
        _nextShotTime = Time.time + _skillCooldown;
    }

    private bool TryLungeHit(Vector2 playerPosition)
    {
        if (_lungeHit || !IsBodyTouching(playerPosition))
        {
            return true;
        }

        _lungeHit = true;
        DealSkillDamage();
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return true;
        }

        return playerManager.IsAlive;
    }

    private void PlayLungeClip(Vector2 lookDirection, bool isMoving)
    {
        if (_view == null)
        {
            return;
        }

        if (_skill == BossSkillKind.NidokingLunge)
        {
            _view.PlayAttackLunge(lookDirection, isMoving);
            return;
        }

        _view.PlayStrike(lookDirection, isMoving);
    }

    private void HoldLungeClip(Vector2 lookDirection)
    {
        if (_view == null)
        {
            return;
        }

        if (_skill == BossSkillKind.NidokingLunge)
        {
            _view.HoldAttackLunge(lookDirection);
            return;
        }

        _view.HoldStrike(lookDirection);
    }

    private bool TickSlam(Vector2 playerPosition)
    {
        if (_slamPhase == SlamPhase.None)
        {
            if (Time.time < _nextShotTime)
            {
                return TickContact(playerPosition);
            }

            BeginSlamWindup(playerPosition);
        }

        if (_slamPhase == SlamPhase.Windup)
        {
            TickSlamWindup(playerPosition);
            return TryContactDamage(playerPosition);
        }

        if (_slamPhase == SlamPhase.Filling)
        {
            TickSlamFill();
            return TryContactDamage(playerPosition);
        }

        if (_slamPhase == SlamPhase.Recovering)
        {
            TickSlamRecover();
            return TryContactDamage(playerPosition);
        }

        TickSlamJump(playerPosition);
        return true;
    }

    private void BeginSlamWindup(Vector2 playerPosition)
    {
        _slamPhase = SlamPhase.Windup;
        _slamUntil = Time.time + SLAM_WINDUP_SECONDS;
        var look = playerPosition - (Vector2)transform.position;
        _slamLook = look.sqrMagnitude > MOVE_SQR_EPSILON ? look.normalized : Vector2.down;
    }

    private void TickSlamWindup(Vector2 playerPosition)
    {
        SetChargeView(_slamLook);
        if (Time.time < _slamUntil)
        {
            return;
        }

        _slamCenter = playerPosition;
        _slamPhase = SlamPhase.Filling;
        _slamUntil = Time.time + _slamChargeSeconds;
        UpdateSlamMark(0f);
    }

    private void TickSlamFill()
    {
        SetChargeView(_slamLook);
        var blend = 1f - Mathf.Clamp01((_slamUntil - Time.time) / _slamChargeSeconds);
        UpdateSlamMark(blend);
        if (Time.time < _slamUntil)
        {
            return;
        }

        BeginSlamJump();
    }

    private void BeginSlamJump()
    {
        _slamOrigin = transform.position;
        var look = _slamCenter - _slamOrigin;
        if (look.sqrMagnitude > MOVE_SQR_EPSILON)
        {
            _slamLook = look.normalized;
        }

        _slamJumpStart = Time.time;
        _slamPhase = SlamPhase.Jumping;
        UpdateSlamMark(1f);
    }

    private void TickSlamJump(Vector2 playerPosition)
    {
        var blend = Mathf.Clamp01((Time.time - _slamJumpStart) / SLAM_JUMP_SECONDS);
        var flat = Vector2.Lerp(_slamOrigin, _slamCenter, blend);
        var height = Mathf.Sin(blend * Mathf.PI) * SLAM_JUMP_HEIGHT;
        transform.position = flat + Vector2.up * height;
        SetAttackView(_slamLook);
        if (blend < 1f)
        {
            return;
        }

        transform.position = _slamCenter;
        BeginSlamRecover(playerPosition);
    }

    private void BeginSlamRecover(Vector2 playerPosition)
    {
        HideSlamMark();
        if (Vector2.Distance(playerPosition, _slamCenter) <= _skillRange)
        {
            DealSkillDamage();
        }

        _slamPhase = SlamPhase.Recovering;
        var poseSeconds = _view != null ? _view.BeginAttackTail(_slamLook, 1) : 0f;
        _slamUntil = Time.time + Mathf.Max(_slamRecoverSeconds, poseSeconds);
    }

    private void TickSlamRecover()
    {
        SetAttackView(_slamLook);
        if (Time.time < _slamUntil)
        {
            return;
        }

        _slamPhase = SlamPhase.None;
        _nextShotTime = Time.time + _skillCooldown;
    }

    private void DealSkillDamage()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return;
        }

        playerManager.TakeDamage(_playerId, _skillDamage);
    }

    private void UpdateSlamMark(float blend)
    {
        var mark = EnsureSlamMark();
        var ring = EnsureSlamRing();
        var amount = Mathf.Clamp01(blend);
        var fullDiameter = _skillRange * 2f;
        mark.transform.position = _slamCenter;
        ring.transform.position = _slamCenter;
        mark.transform.localScale = SlamMarkScale(mark.sprite, fullDiameter * amount / SLAM_FILL_EDGE);
        ring.transform.localScale = SlamMarkScale(ring.sprite, fullDiameter);
        var alpha = Mathf.Lerp(SLAM_MARK_ALPHA_START, SLAM_MARK_ALPHA_END, amount);
        mark.color = new Color(1f, 0.18f, 0.12f, alpha);
        ring.color = new Color(1f, 0.18f, 0.12f, SLAM_RING_ALPHA);
    }

    private SpriteRenderer EnsureSlamMark()
    {
        if (_slamMark != null)
        {
            return _slamMark;
        }

        var markObject = new GameObject("SlamCircle");
        _slamMark = markObject.AddComponent<SpriteRenderer>();
        _slamMark.sprite = _slamCircleSprite != null ? _slamCircleSprite : PrototypeSprite.SlamCircle;
        _slamMark.sortingOrder = SLAM_MARK_SORTING_ORDER;
        return _slamMark;
    }

    private SpriteRenderer EnsureSlamRing()
    {
        if (_slamRing != null)
        {
            return _slamRing;
        }

        var ringObject = new GameObject("SlamRing");
        _slamRing = ringObject.AddComponent<SpriteRenderer>();
        _slamRing.sprite = PrototypeSprite.SlamRing;
        _slamRing.sortingOrder = SLAM_RING_SORTING_ORDER;
        return _slamRing;
    }

    private static Vector3 SlamMarkScale(Sprite sprite, float diameter)
    {
        var spriteWidth = sprite != null ? sprite.bounds.size.x : 1f;
        var scale = spriteWidth > 0f ? diameter / spriteWidth : diameter;
        return new Vector3(scale, scale, 1f);
    }

    private void HideSlamMark()
    {
        if (_slamMark != null)
        {
            Destroy(_slamMark.gameObject);
            _slamMark = null;
        }

        if (_slamRing != null)
        {
            Destroy(_slamRing.gameObject);
            _slamRing = null;
        }
    }

    private void HideChargeMark()
    {
        if (_chargeMark == null)
        {
            return;
        }

        Destroy(_chargeMark.gameObject);
        _chargeMark = null;
    }

    private static bool UsesSkillCooldown(BossSkillKind skill)
    {
        return skill == BossSkillKind.RisingVolley
            || skill == BossSkillKind.ChargeDash
            || skill == BossSkillKind.Slam
            || skill == BossSkillKind.Lunge
            || skill == BossSkillKind.NidokingLunge
            || skill == BossSkillKind.CircleVolley
            || skill == BossSkillKind.FanVolley
            || skill == BossSkillKind.TrackingBeam
            || skill == BossSkillKind.SleepPowder
            || skill == BossSkillKind.SpoonRing;
    }

    private static float ResolveSkillCooldown(BossSkillKind skill, float attackInterval, float skillCooldown)
    {
        if (!UsesSkillCooldown(skill))
        {
            return 0f;
        }

        var cooldown = skillCooldown > 0f ? skillCooldown : attackInterval;
        return Mathf.Max(MIN_SHOT_INTERVAL, cooldown);
    }

    private void CancelUnfiredVolley()
    {
        for (var i = 0; i < _volley.Length; i++)
        {
            var shot = _volley[i];
            if (shot != null)
            {
                shot.CancelIfUnfired();
            }

            _volley[i] = null;
        }

        _volleyPhase = VolleyPhase.None;
    }

    private void SetAttackView(Vector2 lookDirection)
    {
        if (_view == null)
        {
            return;
        }

        _view.SetVisual(false, lookDirection, false, true);
    }

    private bool TickCircleVolley(Vector2 playerPosition)
    {
        if (_circlePhase == CirclePhase.Attacking)
        {
            return TickCircleAttack(playerPosition);
        }

        if (_circlePhase == CirclePhase.Moving)
        {
            return TickCircleMove(playerPosition);
        }

        if (Time.time >= _nextShotTime)
        {
            _circleAttacks = 0;
            BeginCircleBurrow(playerPosition);
            return true;
        }

        if (_view != null)
        {
            _view.SetIdleMove(true);
        }

        return TickContact(playerPosition);
    }

    private void BeginCircleBurrow(Vector2 playerPosition)
    {
        _circleCenter = ClampToField(playerPosition);
        _circlePhase = CirclePhase.Moving;
        _circleUntil = Time.time + _circleGapSeconds;
        _circleHit = false;
        if (_view != null)
        {
            _view.SetIdleMove(false);
        }

        ShowSkillCircle(_circleCenter, _circleRange);
    }

    private void BeginCircleAttack(Vector2 playerPosition)
    {
        var side = playerPosition.x >= _circleCenter.x ? 1f : -1f;
        _circleLook = new Vector2(side, 0f);
        _circlePhase = CirclePhase.Attacking;
        _circleHit = false;
        if (_view != null)
        {
            _view.SetIdleMove(false);
        }

        ShowSkillCircle(_circleCenter, _circleRange);
        PlayPose(_circleLook);
    }

    private bool TickCircleAttack(Vector2 playerPosition)
    {
        ShowSkillCircle(_circleCenter, _circleRange);
        PlayPose(_circleLook);
        if (!_circleHit && Vector2.Distance(_circleCenter, playerPosition) <= _circleRange)
        {
            _circleHit = true;
            DealSkillDamage();
            if (!IsPlayerAlive())
            {
                return false;
            }
        }

        if (_view != null && !_view.IsPoseFinished)
        {
            return true;
        }

        HideSlamMark();
        _circleAttacks++;
        if (_circleAttacks >= CIRCLE_ATTACK_COUNT)
        {
            _circlePhase = CirclePhase.None;
            _circleAttacks = 0;
            _nextShotTime = Time.time + _skillCooldown;
            if (_view != null)
            {
                _view.SetIdleMove(true);
            }

            return true;
        }

        BeginCircleBurrow(playerPosition);
        return true;
    }

    private bool TickCircleMove(Vector2 playerPosition)
    {
        ShowSkillCircle(_circleCenter, _circleRange);
        if (_view != null)
        {
            _view.SetIdleMove(false);
        }

        if (!HasReachedCircle())
        {
            MoveToward(_circleCenter, Time.deltaTime, _circleMoveSpeed);
        }

        if (HasReachedCircle() && Time.time >= _circleUntil)
        {
            BeginCircleAttack(playerPosition);
            return true;
        }

        return TryContactDamage(playerPosition);
    }

    private bool HasReachedCircle()
    {
        var toCenter = _circleCenter - (Vector2)transform.position;
        var step = Mathf.Max(_circleMoveSpeed * Time.deltaTime, 0.05f);
        return toCenter.sqrMagnitude <= step * step;
    }

    private Vector2 ClampToField(Vector2 position)
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<StageFieldManager>(out var fieldManager))
        {
            return fieldManager.ValidatePosition(position);
        }

        return position;
    }

    private bool IsPlayerAlive()
    {
        return Managers.Instance != null
            && Managers.Instance.TryGetManager<PlayerManager>(out var playerManager)
            && playerManager.IsAlive;
    }

    private void PlayPose(Vector2 lookDirection)
    {
        if (_view == null)
        {
            return;
        }

        _view.PlayPose(lookDirection);
    }

    private void ShowSkillCircle(Vector2 center, float radius)
    {
        var mark = EnsureSlamMark();
        var ring = EnsureSlamRing();
        var diameter = Mathf.Max(0.1f, radius) * 2f;
        mark.transform.position = center;
        ring.transform.position = center;
        mark.transform.localScale = SlamMarkScale(mark.sprite, diameter / SLAM_FILL_EDGE);
        ring.transform.localScale = SlamMarkScale(ring.sprite, diameter);
        mark.color = new Color(1f, 0.18f, 0.12f, SLAM_MARK_ALPHA_END);
        ring.color = new Color(1f, 0.18f, 0.12f, SLAM_RING_ALPHA);
    }

    private bool TickContact(Vector2 playerPosition)
    {
        MoveToward(playerPosition, Time.deltaTime);
        return TryContactDamage(playerPosition);
    }

    private bool TryContactDamage(Vector2 playerPosition)
    {
        return TryContactDamage(playerPosition, _contactDamage);
    }

    private bool TryContactDamage(Vector2 playerPosition, float damage)
    {
        if (!IsBodyTouching(playerPosition))
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

        playerManager.TakeDamage(_playerId, damage);
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

    /// <summary>
    /// 그림의 가로세로 안에 있을 때만 접촉으로 본다. 피벗에서 그린 원은 몸 아래 빈 칸까지 맞는다.
    /// </summary>
    private bool IsBodyTouching(Vector2 playerPosition)
    {
        if (_view != null && _view.HasBodyBounds)
        {
            return _view.ContainsBody(playerPosition);
        }

        return Vector2.Distance(transform.position, playerPosition) <= ContactReach;
    }

    /// <summary>
    /// 몸통이 플레이어에게 닿는 거리. 그림에 맞춘 판정에 스케일을 곱한 값이다.
    /// </summary>
    private float ContactReach
    {
        get
        {
            var body = BodyReach;
            return body > 0f ? body : _hitRadius;
        }
    }

    private void SetView(bool isMoving, Vector2 lookDirection, bool shooting)
    {
        if (_view == null)
        {
            return;
        }

        _view.SetVisual(isMoving, lookDirection, shooting);
    }

    private void MoveToward(Vector2 target, float deltaTime, float speed = -1f)
    {
        if (Time.time < _approachAt)
        {
            return;
        }

        var stepSpeed = speed >= 0f ? speed : _moveSpeed;
        var current = (Vector2)transform.position;
        var next = Vector2.MoveTowards(current, target, stepSpeed * deltaTime);
        if (Managers.Instance != null && Managers.Instance.TryGetManager<StageFieldManager>(out var fieldManager))
        {
            next = fieldManager.ValidatePosition(next);
        }

        if (_owner != null)
        {
            next = _owner.SeparateFromBosses(this, next);
        }

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
