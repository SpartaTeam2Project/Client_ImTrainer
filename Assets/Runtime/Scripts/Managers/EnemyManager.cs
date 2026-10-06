using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 타임라인 클립이 요청한 스폰, 활성 적 목록, 킬 수와 Pool 수명을 담당한다.
/// 흐름: SpawnWave => Pool.Get => Enemy.Bind/ApplyVisual/Initialize => Enemy.Tick.
/// 발사 몬스터의 투사체는 별도 목록에서 재사용한다.
/// 사망: NotifyDied => 목록/카운트 갱신 => 상태 정리 => Pool.Return.
/// </summary>
public class EnemyManager : BaseManager
{
    private const float FALLBACK_SPAWN_RADIUS = 8f;
    private const int MAX_ALIVE = 1000;
    private const float ACTOR_SIZE = 0.7f;
    private const int ACTOR_SORTING_ORDER = 5;
    private const float DIAGONAL_DISTANCE_MULTIPLIER = 1.3f;
    private const float TELEPORT_CONE_SIZE = 0.8f;
    private const int TELEPORT_CAP = 100;
    private const float TELEPORT_ANGLE = 45f;
    private const int TELEPORT_FRAME_DIVISOR = 20;
    private const int TELEPORT_FRAME_MIN = 1;
    private const int TELEPORT_FRAME_MAX = 100;
    private const float SPAWN_OUTSIDE_PADDING = 0.5f;
    private const float SPAWN_PADDING_RANDOM = 0.2f;
    private const float CIRCULAR_DISTANCE_MULTIPLIER = 1.05f;
    private const float CIRCULAR_PADDING_RANDOM = 0.2f;
    private const int LARGE_SPAWN_AMOUNT = 100;
    private const float LARGE_SPAWN_SPREAD = 0.1f;
    private const int SPAWN_POSITION_TRIES = 10;
    private const int SPAWN_PULL_STEPS = 10;
    private const float POSITION_ACCEPT_SQR = 0.0025f;
    private const float LOOK_SQR_EPSILON = 0.0001f;
    private const float MIN_RING_RADIUS = 0.5f;
    private const float RING_VIEW_MARGIN = 0.85f;
    private const float MIN_APPROACH_SPEED = 0.05f;
    private const float MIN_RUSH_SPEED = 0.05f;
    private const float MIN_CLUSTER_RADIUS = 0.05f;
    private const float RUSH_EDGE_PADDING = 0.6f;
    private const float RUSH_HIDE_DELAY = 2f;
    private const float DIRECTION_SQR_EPSILON = 0.0001f;
    private const float GOLDEN_ANGLE = 2.3999631f;

    [Header("Enemy Settings")]
    [SerializeField] private Enemy _enemyPrefab;
    [SerializeField] private ExperienceGem _bossExperienceGem;

    private readonly List<Enemy> _alive = new List<Enemy>();
    private readonly List<EnemyProjectile> _projectiles = new List<EnemyProjectile>();
    private readonly Dictionary<Enemy, Action<Enemy>> _deathCallbacks = new Dictionary<Enemy, Action<Enemy>>();
    private readonly List<Enemy> _hurting = new List<Enemy>();
    private int _hurtEpoch;
    private CombatObjectPool _pool;
    private Transform _projectileRoot;
    private GameObject _fallbackPrefab;

    private int _playerId;
    private float _hpMultiplier = 1f;
    private float _damageMultiplier = 1f;
    private bool _stageActive;
    private bool _bossFightActive;

    public int KillCount { get; private set; }

    #region Unity Methods

    /// <summary>
    /// 적 매니저 초기화
    /// </summary>
    public override UniTask InitializeAsync()
    {
        return base.InitializeAsync();
    }

    /// <summary>
    /// Managers 정리 시에도 Pool 참조와 객체를 남기지 않는다.
    /// </summary>
    public override void Cleanup()
    {
        ClearStage();
        base.Cleanup();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 판을 열고 스폰 배율을 맞춘다. 같은 Scene의 Pool은 다음 판에도 재사용한다.
    /// stageRoot를 생략하는 기존 호출은 현재 활성 Scene에 Pool을 만든다.
    /// </summary>
    public void BeginStage(int playerId, StageData stage, Transform stageRoot = null)
    {
        ClearActors();
        if (_pool != null && (_pool.Root == null || _pool.Root.parent != stageRoot))
        {
            ClearStage();
        }

        KillCount = 0;
        _playerId = playerId;
        _bossFightActive = false;
        _stageActive = stage != null;
        _hpMultiplier = stage != null ? Mathf.Max(0.01f, stage.EnemyHpMultiplier) : 1f;
        _damageMultiplier = stage != null ? Mathf.Max(0f, stage.EnemyDamageMultiplier) : 1f;

        if (stage == null)
        {
            Debug.LogError("StageData가 없어 적을 스폰하지 않습니다.");
            return;
        }

        if (_pool == null)
        {
            _pool = new CombatObjectPool(stageRoot);
        }
    }

    /// <summary>
    /// 판 종료 시 활성 적을 반환한다. 결과에 쓸 KillCount와 재사용할 Pool은 유지한다.
    /// </summary>
    public void EndStage()
    {
        ClearActors();
    }

    /// <summary>
    /// Scene을 떠날 때 활성 적 반환 후 Pool 전체와 자리표시 원본을 정리한다.
    /// </summary>
    public void ClearStage()
    {
        EndStage();
        _pool?.DestroyAll();
        _pool = null;
        _fallbackPrefab = null;
        _projectiles.Clear();
        _projectileRoot = null;
    }

    /// <summary>
    /// Playing 동안 살아 있는 적에게 플레이어 위치를 넘기고, 화면 밖 적을 앞으로 옮긴다.
    /// </summary>
    public void Tick(int playerId)
    {
        if (!_stageActive || playerId != _playerId)
        {
            return;
        }

        if (!TryGetPlayerPosition(out var playerPosition))
        {
            return;
        }

        if (!UpdateEnemies(playerPosition))
        {
            return;
        }

        TickProjectiles(Time.deltaTime);
        HideRushedOffscreen();
        TeleportOffscreen(playerPosition);
    }

    /// <summary>
    /// 타임라인 클립이 요청한 수만큼 낸다. 실제로 낸 마리 수를 반환한다.
    /// </summary>
    public int SpawnWave(int playerId, MonsterWaveProfile profile, int count, bool circularSpawn, Action<Enemy> onDied)
    {
        if (!_stageActive || _bossFightActive || playerId != _playerId || profile == null || count <= 0)
        {
            return 0;
        }

        if (!TryGetPlayerPosition(out var playerPosition))
        {
            return 0;
        }

        var spawned = 0;
        for (var i = 0; i < count; i++)
        {
            if (_alive.Count >= MAX_ALIVE)
            {
                break;
            }

            var position = ResolveSpawnPosition(playerPosition, count, circularSpawn);
            if (CreateEnemy(profile, position, onDied) == null)
            {
                break;
            }

            spawned++;
        }

        return spawned;
    }

    /// <summary>
    /// 플레이어 주변 원 위에 균등하게 한 번 낸다. 이동 속도는 접근 속도를 쓴다.
    /// </summary>
    public int SpawnRing(int playerId, MonsterWaveProfile profile, int count, float radius, float approachSpeed)
    {
        if (!_stageActive || _bossFightActive || playerId != _playerId || profile == null || count <= 0)
        {
            return 0;
        }

        if (!TryGetPlayerPosition(out var playerPosition))
        {
            return 0;
        }

        profile.MoveSpeed = Mathf.Max(MIN_APPROACH_SPEED, approachSpeed);
        profile.DisableOffscreenTeleport = true;
        var resolvedRadius = ResolveRingRadius(radius);
        var spawned = 0;
        for (var i = 0; i < count; i++)
        {
            if (_alive.Count >= MAX_ALIVE)
            {
                break;
            }

            var angle = Mathf.PI * 2f * i / count;
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * resolvedRadius;
            var position = AcceptOrBorder(playerPosition + offset, playerPosition);
            if (CreateEnemy(profile, position, null) == null)
            {
                break;
            }

            spawned++;
        }

        return spawned;
    }

    /// <summary>
    /// 카메라 밖에 묶음을 두고, 그 순간 플레이어 방향으로 직진시킨다.
    /// </summary>
    public int SpawnRush(int playerId, MonsterWaveProfile profile, int count, float clusterRadius, float rushSpeed)
    {
        if (!_stageActive || _bossFightActive || playerId != _playerId || profile == null || count <= 0)
        {
            return 0;
        }

        if (!TryGetPlayerPosition(out var playerPosition))
        {
            return 0;
        }

        profile.MoveSpeed = Mathf.Max(MIN_RUSH_SPEED, rushSpeed);
        profile.DisableOffscreenTeleport = true;
        var center = ResolveRushCenter(playerPosition, out var direction);
        var radius = Mathf.Max(MIN_CLUSTER_RADIUS, clusterRadius);
        var spawned = 0;
        for (var i = 0; i < count; i++)
        {
            if (_alive.Count >= MAX_ALIVE)
            {
                break;
            }

            var offset = ClusterOffset(i, count, radius);
            var position = AcceptOrBorder(center + offset, playerPosition);
            var enemy = CreateEnemy(profile, position, null);
            if (enemy == null)
            {
                break;
            }

            enemy.BeginRush(direction);
            spawned++;
        }

        return spawned;
    }

    /// <summary>
    /// 보스전이 켜지면 일반 스폰과 화면 밖 재배치를 멈춘다.
    /// </summary>
    public void SetBossFightActive(bool active)
    {
        _bossFightActive = active;
    }

    /// <summary>
    /// 살아 있는 적과 투사체를 풀로 돌린다. 처치 수는 올리지 않고 판은 유지한다.
    /// </summary>
    public void DismissAlive()
    {
        DeactivateProjectiles();
        CancelHurtDeaths();
        for (var i = _alive.Count - 1; i >= 0; i--)
        {
            var enemy = _alive[i];
            _alive.RemoveAt(i);
            if (enemy != null)
            {
                ReturnEnemy(enemy);
            }
        }

        _deathCallbacks.Clear();
    }

    /// <summary>
    /// 보스전 중에도 지정 위치에 보스 한 마리를 낸다. 죽으면 콜백을 부른다.
    /// </summary>
    public Enemy SpawnBoss(int playerId, MonsterWaveProfile profile, Vector2 position, Action<Enemy> onDied)
    {
        if (!_stageActive || playerId != _playerId || profile == null || profile.Monster == null || _alive.Count >= MAX_ALIVE)
        {
            return null;
        }

        profile.DisableOffscreenTeleport = true;
        return CreateEnemy(profile, position, onDied);
    }

    /// <summary>
    /// 적 투사체를 풀에서 꺼내 플레이어 쪽으로 날린다.
    /// </summary>
    public void LaunchProjectile(int playerId, Vector2 position, Vector2 direction, float speed, float range, float damage, float hitRadius, float attackDistance, Sprite sprite)
    {
        var projectile = GetProjectile();
        if (projectile == null)
        {
            return;
        }

        projectile.ApplySprite(sprite);
        projectile.Launch(playerId, position, direction, speed, range, damage, hitRadius, attackDistance);
    }

    /// <summary>
    /// 보스 스킬용 투사체를 꺼내 머리 위까지 올린다. 맞추기 전에는 피해가 없다.
    /// </summary>
    public EnemyProjectile ArmRisingProjectile(int playerId, Vector2 origin, Vector2 hoverPoint, float riseSeconds, float attackDistance, Sprite sprite, float swayPhase)
    {
        var projectile = GetProjectile();
        if (projectile == null)
        {
            return null;
        }

        projectile.ApplySprite(sprite);
        projectile.BeginRise(playerId, origin, hoverPoint, riseSeconds, attackDistance, swayPhase);
        return projectile;
    }

    /// <summary>
    /// 적 체력이 0이 되면 목록에서 빼고 처치 수를 올린다.
    /// </summary>
    public void NotifyDied(Enemy enemy)
    {
        var index = _alive.IndexOf(enemy);
        if (index < 0)
        {
            return;
        }

        if (_deathCallbacks.ContainsKey(enemy) && enemy.HasHurtSprite)
        {
            BeginHurtDeath(index, enemy);
            return;
        }

        InvokeDeathCallback(enemy);
        KillAt(index, enemy);
    }

    /// <summary>
    /// 원점부터 사거리 안에서 가장 가까운 살아 있는 적을 찾는다.
    /// </summary>
    public bool TryGetClosest(Vector2 origin, float range, out Enemy enemy)
    {
        enemy = null;
        var closestSqr = range * range;
        var found = false;
        for (var i = 0; i < _alive.Count; i++)
        {
            var candidate = _alive[i];
            if (candidate == null || !candidate.IsAlive)
            {
                continue;
            }

            var offset = (Vector2)candidate.transform.position - origin;
            var sqr = offset.sqrMagnitude;
            if (sqr > closestSqr)
            {
                continue;
            }

            closestSqr = sqr;
            enemy = candidate;
            found = true;
        }

        return found;
    }

    /// <summary>
    /// 원점부터 반경 안에 있는 살아 있는 적을 모은다. 결과는 비운 뒤 채운다.
    /// </summary>
    public void CollectInRadius(Vector2 origin, float radius, List<Enemy> results)
    {
        if (results == null)
        {
            return;
        }

        results.Clear();
        if (radius <= 0f)
        {
            return;
        }

        for (var i = 0; i < _alive.Count; i++)
        {
            var candidate = _alive[i];
            if (candidate == null || !candidate.IsAlive)
            {
                continue;
            }

            var offset = (Vector2)candidate.transform.position - origin;
            var reach = radius + candidate.BodyReach;
            if (offset.sqrMagnitude > reach * reach)
            {
                continue;
            }

            results.Add(candidate);
        }
    }

    /// <summary>
    /// 시작점에서 방향으로 길이만큼 그은 선에서, 두께 안에 있는 살아 있는 적을 모은다. 결과는 비운 뒤 채운다.
    /// </summary>
    public void CollectAlongSegment(Vector2 origin, Vector2 direction, float length, float radius, List<Enemy> results)
    {
        const float DIRECTION_SQR_EPSILON = 0.0001f;

        if (results == null)
        {
            return;
        }

        results.Clear();
        if (length <= 0f || direction.sqrMagnitude <= DIRECTION_SQR_EPSILON)
        {
            return;
        }

        var forward = direction.normalized;
        for (var i = 0; i < _alive.Count; i++)
        {
            var candidate = _alive[i];
            if (candidate == null || !candidate.IsAlive)
            {
                continue;
            }

            var offset = (Vector2)candidate.transform.position - origin;
            var reach = candidate.BodyReach;
            var along = Vector2.Dot(offset, forward);
            if (along < -reach || along > length + reach)
            {
                continue;
            }

            var lateral = offset - forward * along;
            var lateralReach = radius + reach;
            if (lateral.sqrMagnitude > lateralReach * lateralReach)
            {
                continue;
            }

            results.Add(candidate);
        }
    }

    /// <summary>
    /// 스폰된 적이 씬과 함께 사라지면 목록에서 뺀다.
    /// </summary>
    public void NotifyActorDestroyed(Enemy enemy)
    {
        if (_hurting.Remove(enemy))
        {
            InvokeDeathCallback(enemy);
            return;
        }

        var index = _alive.IndexOf(enemy);
        if (index < 0)
        {
            return;
        }

        InvokeDeathCallback(enemy);
        _alive.RemoveAt(index);
    }

    #endregion

    #region Private Methods

    private Enemy CreateEnemy(MonsterWaveProfile profile, Vector2 position, Action<Enemy> onDied)
    {
        var enemy = CreateActor(_enemyPrefab, position);
        if (enemy == null)
        {
            return null;
        }

        var visual = profile.Monster;
        var maxHealth = Mathf.Max(1f, profile.MaxHealth * _hpMultiplier);
        var contactDamage = profile.ContactDamage * _damageMultiplier;
        enemy.Bind(this);
        enemy.ApplyVisual(visual, ResolveScale(profile));
        enemy.Initialize(
            _playerId,
            -1,
            maxHealth,
            contactDamage,
            profile.MoveSpeed,
            profile.AttackKind,
            profile.AttackRange,
            profile.AttackInterval,
            profile.ProjectileSpeed,
            ResolveProjectileSprite(profile),
            profile.Id,
            profile.HitRadius,
            profile.AttackDistance,
            profile.Skill,
            profile.SkillCooldown);
        enemy.SetLaneFlags(profile.DisableOffscreenTeleport, false);
        enemy.SetDropGem(onDied != null ? _bossExperienceGem : null);
        if (onDied != null)
        {
            _deathCallbacks[enemy] = onDied;
        }

        _alive.Add(enemy);
        return enemy;
    }

    private static float ResolveScale(MonsterWaveProfile profile)
    {
        if (profile.OverrideScale)
        {
            return profile.Scale;
        }

        var visual = profile.Monster;
        return visual != null ? visual.Scale : MonsterVisualData.DEFAULT_SCALE;
    }

    /// <summary>
    /// 보스 클립에 지정한 탄 그림을 쓴다. 없으면 기본 탄 그림을 쓴다.
    /// </summary>
    private static Sprite ResolveProjectileSprite(MonsterWaveProfile profile)
    {
        return profile.ProjectileSprite;
    }

    private float ResolveRingRadius(float radius)
    {
        var requested = Mathf.Max(MIN_RING_RADIUS, radius);
        if (!TryGetCamera(out var camera))
        {
            return requested;
        }

        var limit = Mathf.Min(camera.HalfWidth, camera.HalfHeight) * RING_VIEW_MARGIN;
        if (limit <= 0f)
        {
            return requested;
        }

        return Mathf.Min(requested, limit);
    }

    /// <summary>
    /// 반지름 안을 고르게 채워 둘레만 있는 고리가 되지 않게 한다.
    /// </summary>
    private static Vector2 ClusterOffset(int index, int count, float radius)
    {
        if (count <= 1)
        {
            return Vector2.zero;
        }

        var distance = radius * Mathf.Sqrt((index + 0.5f) / count);
        var angle = index * GOLDEN_ANGLE;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
    }

    private Vector2 ResolveRushCenter(Vector2 playerPosition, out Vector2 direction)
    {
        var outward = RandomDirection();
        if (!TryGetCamera(out var camera) || camera.HalfWidth <= 0f || camera.HalfHeight <= 0f)
        {
            var centerFallback = playerPosition + outward * FALLBACK_SPAWN_RADIUS;
            direction = playerPosition - centerFallback;
            if (direction.sqrMagnitude <= DIRECTION_SQR_EPSILON)
            {
                direction = -outward;
            }

            return centerFallback;
        }

        var edge = DistanceToCameraEdge(outward, camera);
        var center = camera.Position + outward * (edge + RUSH_EDGE_PADDING);
        direction = playerPosition - center;
        if (direction.sqrMagnitude <= DIRECTION_SQR_EPSILON)
        {
            direction = -outward;
        }

        return center;
    }

    private static float DistanceToCameraEdge(Vector2 direction, CameraManager camera)
    {
        var alongX = Mathf.Abs(direction.x) > DIRECTION_SQR_EPSILON
            ? camera.HalfWidth / Mathf.Abs(direction.x)
            : float.MaxValue;
        var alongY = Mathf.Abs(direction.y) > DIRECTION_SQR_EPSILON
            ? camera.HalfHeight / Mathf.Abs(direction.y)
            : float.MaxValue;
        return Mathf.Min(alongX, alongY);
    }

    private void HideRushedOffscreen()
    {
        if (_alive.Count == 0 || !TryGetCamera(out var camera))
        {
            return;
        }

        var now = Time.time;
        for (var i = _alive.Count - 1; i >= 0; i--)
        {
            var enemy = _alive[i];
            if (enemy == null)
            {
                _alive.RemoveAt(i);
                continue;
            }

            if (!enemy.IsRushing || !enemy.IsAlive || now - enemy.SpawnedAt < RUSH_HIDE_DELAY)
            {
                continue;
            }

            if (!IsOutsideCamera(enemy.transform.position, camera))
            {
                continue;
            }

            _alive.RemoveAt(i);
            ReturnEnemy(enemy);
        }
    }

    private static bool IsOutsideCamera(Vector2 position, CameraManager camera)
    {
        return position.x < camera.LeftBound
            || position.x > camera.RightBound
            || position.y < camera.BottomBound
            || position.y > camera.TopBound;
    }

    private Vector2 ResolveSpawnPosition(Vector2 playerPosition, int amount, bool circularSpawn)
    {
        var extra = amount > LARGE_SPAWN_AMOUNT ? Mathf.Sqrt(_alive.Count) * LARGE_SPAWN_SPREAD : 0f;
        if (!TryGetCamera(out var camera) || camera.ViewDiagonal <= 0f)
        {
            var angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            return AcceptOrBorder(playerPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * FALLBACK_SPAWN_RADIUS, playerPosition);
        }

        var diagonal = camera.ViewDiagonal;
        var lastCandidate = playerPosition;
        for (var attempt = 0; attempt < SPAWN_POSITION_TRIES; attempt++)
        {
            lastCandidate = circularSpawn
                ? playerPosition + RandomDirection() * (diagonal * CIRCULAR_DISTANCE_MULTIPLIER + UnityEngine.Random.value * CIRCULAR_PADDING_RANDOM + extra)
                : camera.GetRandomPointOutside(SPAWN_OUTSIDE_PADDING + UnityEngine.Random.value * SPAWN_PADDING_RANDOM + extra);
            if (TryAcceptPosition(lastCandidate, out var accepted))
            {
                return accepted;
            }
        }

        return AcceptOrBorder(lastCandidate, playerPosition);
    }

    private Vector2 AcceptOrBorder(Vector2 candidate, Vector2 playerPosition)
    {
        if (TryAcceptPosition(candidate, out var accepted))
        {
            return accepted;
        }

        for (var step = 1; step < SPAWN_PULL_STEPS; step++)
        {
            var pulled = Vector2.Lerp(candidate, playerPosition, 1f - step / (float)SPAWN_PULL_STEPS);
            if (TryAcceptPosition(pulled, out var pulledAccepted))
            {
                return pulledAccepted;
            }
        }

        if (Managers.Instance != null && Managers.Instance.TryGetManager<StageFieldManager>(out var fieldManager))
        {
            return fieldManager.GetRandomPositionOnBorder();
        }

        return playerPosition;
    }

    private void TeleportOffscreen(Vector2 playerPosition)
    {
        if (_bossFightActive || _alive.Count == 0 || _alive.Count > TELEPORT_CAP)
        {
            return;
        }

        if (!TryGetCamera(out var camera) || camera.ViewDiagonal <= 0f)
        {
            return;
        }

        if (!TryGetLookDirection(out var lookDirection))
        {
            return;
        }

        var diagonal = camera.ViewDiagonal * DIAGONAL_DISTANCE_MULTIPLIER;
        var thresholdSqr = diagonal * diagonal;
        var behindDot = TELEPORT_CONE_SIZE - 1f;
        var stride = Mathf.Clamp(_alive.Count / TELEPORT_FRAME_DIVISOR, TELEPORT_FRAME_MIN, TELEPORT_FRAME_MAX);
        var frame = Time.frameCount % stride;
        for (var i = frame; i < _alive.Count; i += stride)
        {
            var enemy = _alive[i];
            if (enemy == null || !enemy.IsAlive || enemy.IsRushing || enemy.DisableOffscreenTeleport)
            {
                continue;
            }

            var offset = (Vector2)enemy.transform.position - playerPosition;
            if (offset.sqrMagnitude <= thresholdSqr)
            {
                continue;
            }

            var away = offset.normalized;
            if (Vector2.Dot(away, lookDirection) >= behindDot)
            {
                continue;
            }

            var turned = (Vector2)(Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-TELEPORT_ANGLE, TELEPORT_ANGLE)) * (Vector3)lookDirection);
            var destination = playerPosition + turned * diagonal;
            if (!TryAcceptPosition(destination, out var accepted))
            {
                continue;
            }

            enemy.transform.position = accepted;
        }
    }

    private bool TryAcceptPosition(Vector2 candidate, out Vector2 accepted)
    {
        accepted = candidate;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<StageFieldManager>(out var fieldManager))
        {
            return true;
        }

        var clamped = fieldManager.ValidatePosition(candidate);
        if ((clamped - candidate).sqrMagnitude > POSITION_ACCEPT_SQR)
        {
            return false;
        }

        accepted = clamped;
        return true;
    }

    private bool TryGetCamera(out CameraManager camera)
    {
        camera = null;
        return Managers.Instance != null && Managers.Instance.TryGetManager(out camera) && camera.HasView;
    }

    private bool TryGetLookDirection(out Vector2 lookDirection)
    {
        lookDirection = Vector2.right;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return false;
        }

        var look = playerManager.LookDirection;
        if (look.sqrMagnitude <= LOOK_SQR_EPSILON)
        {
            return true;
        }

        lookDirection = look.normalized;
        return true;
    }

    private static Vector2 RandomDirection()
    {
        var direction = UnityEngine.Random.insideUnitCircle;
        if (direction.sqrMagnitude <= LOOK_SQR_EPSILON)
        {
            return Vector2.right;
        }

        return direction.normalized;
    }

    private void InvokeDeathCallback(Enemy enemy)
    {
        if (enemy == null || !_deathCallbacks.TryGetValue(enemy, out var callback))
        {
            return;
        }

        _deathCallbacks.Remove(enemy);
        callback?.Invoke(enemy);
    }

    private Enemy CreateActor(Enemy selectedPrefab, Vector2 position)
    {
        if (_pool == null || _pool.Root == null)
        {
            return null;
        }

        var prefab = selectedPrefab != null ? selectedPrefab.gameObject : _fallbackPrefab;
        if (prefab == null)
        {
            // 기존 Prefab 미지정 동작을 유지하되, 비활성 원본 하나를 Pool에서 복제한다.
            Debug.LogWarning("유효한 Enemy Prefab이 없어 런타임 Fallback Enemy를 생성합니다. StageData와 EnemyManager 설정을 확인하세요.");
            _fallbackPrefab = new GameObject("Enemy Fallback");
            _fallbackPrefab.SetActive(false);
            _fallbackPrefab.transform.SetParent(_pool.Root, false);
            _fallbackPrefab.transform.localScale = new Vector3(ACTOR_SIZE, ACTOR_SIZE, 1f);

            var renderer = _fallbackPrefab.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = ACTOR_SORTING_ORDER;
            _fallbackPrefab.AddComponent<Enemy>();
            _fallbackPrefab.AddComponent<EnemyView>();
            prefab = _fallbackPrefab;
        }

        var actorObject = _pool.Get(prefab, position, Quaternion.identity);
        if (actorObject == null)
        {
            return null;
        }

        actorObject.name = "Enemy";
        return actorObject.GetComponent<Enemy>();
    }

    private bool UpdateEnemies(Vector2 playerPosition)
    {
        for (var i = _alive.Count - 1; i >= 0; i--)
        {
            var enemy = _alive[i];
            if (enemy == null)
            {
                _alive.RemoveAt(i);
                continue;
            }

            if (!enemy.Tick(playerPosition))
            {
                return false;
            }
        }

        return true;
    }

    private void KillAt(int index, Enemy enemy)
    {
        var dropPosition = enemy != null ? (Vector2)enemy.transform.position : Vector2.zero;
        KillCount++;
        _alive.RemoveAt(index);
        DropExperience(dropPosition, enemy != null ? enemy.ExperienceGem : null);
        DropCurrencies(dropPosition, enemy);
        ReturnEnemy(enemy);
    }

    /// <summary>
    /// 보스 사망 그림을 재생한 뒤에 경험치를 떨어뜨리고, 풀로 돌린 다음 보스 콜백을 연다.
    /// </summary>
    private void BeginHurtDeath(int index, Enemy enemy)
    {
        var dropPosition = (Vector2)enemy.transform.position;
        var gem = enemy.ExperienceGem;
        KillCount++;
        _alive.RemoveAt(index);
        DropCurrencies(dropPosition, enemy);
        _hurting.Add(enemy);
        FinishHurtDeathAsync(enemy, dropPosition, gem, _hurtEpoch).Forget();
    }

    private async UniTaskVoid FinishHurtDeathAsync(Enemy enemy, Vector2 dropPosition, ExperienceGem gem, int epoch)
    {
        await enemy.PlayHurtAsync();
        if (epoch != _hurtEpoch || enemy == null || !_hurting.Remove(enemy))
        {
            return;
        }

        DropExperience(dropPosition, gem);
        InvokeDeathCallback(enemy);
        ReturnEnemy(enemy);
    }

    private void CancelHurtDeaths()
    {
        _hurtEpoch++;
        for (var i = _hurting.Count - 1; i >= 0; i--)
        {
            var enemy = _hurting[i];
            _hurting.RemoveAt(i);
            ReturnEnemy(enemy);
        }
    }

    private void DropCurrencies(Vector2 position, Enemy enemy)
    {
        if (enemy == null || Managers.Instance == null || !Managers.Instance.TryGetManager<DropManager>(out var dropManager))
        {
            return;
        }

        TryDropCurrency(dropManager, position, enemy.PocketDollarDrop, enemy.PocketDollarChance);
        TryDropCurrency(dropManager, position, enemy.MonsterBallDrop, enemy.MonsterBallChance);
    }

    private void TryDropCurrency(DropManager dropManager, Vector2 position, CoinDropBehavior prefab, float chance)
    {
        if (prefab == null || chance <= 0f)
        {
            return;
        }

        if (UnityEngine.Random.value * 100f > chance)
        {
            return;
        }

        dropManager.Drop(_playerId, position, prefab);
    }

    private void DropExperience(Vector2 position, ExperienceGem gem)
    {
        if (gem == null || Managers.Instance == null || !Managers.Instance.TryGetManager<ExperienceManager>(out var experienceManager))
        {
            return;
        }

        experienceManager.Drop(_playerId, position, gem);
    }

    private bool TryGetPlayerPosition(out Vector2 playerPosition)
    {
        playerPosition = Vector2.zero;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return false;
        }

        var playerTransform = playerManager.PlayerTransform;
        if (playerTransform == null || !playerManager.IsAlive)
        {
            return false;
        }

        playerPosition = playerTransform.position;
        return true;
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

    private EnemyProjectile GetProjectile()
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
        if (_projectileRoot == null)
        {
            return null;
        }

        var created = EnemyProjectile.Create(_projectileRoot);
        _projectiles.Add(created);
        return created;
    }

    private void EnsureProjectileRoot()
    {
        if (_projectileRoot != null || _pool == null || _pool.Root == null)
        {
            return;
        }

        var rootObject = new GameObject("EnemyProjectiles");
        rootObject.transform.SetParent(_pool.Root, false);
        _projectileRoot = rootObject.transform;
    }

    private void DeactivateProjectiles()
    {
        for (var i = 0; i < _projectiles.Count; i++)
        {
            var projectile = _projectiles[i];
            if (projectile != null)
            {
                projectile.gameObject.SetActive(false);
            }
        }
    }

    private void ClearActors()
    {
        _stageActive = false;
        DeactivateProjectiles();
        CancelHurtDeaths();
        for (var i = _alive.Count - 1; i >= 0; i--)
        {
            var enemy = _alive[i];
            _alive.RemoveAt(i);
            if (enemy != null)
            {
                // 판 종료는 처치가 아니다. KillCount를 올리지 않는다.
                ReturnEnemy(enemy);
            }
        }

        _deathCallbacks.Clear();
    }

    private void ReturnEnemy(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        // 비활성 보관 중의 피해/파괴 알림이 기존 판의 상태를 바꾸지 않도록 해제한다.
        // 사망 콜백은 이미 죽었을 때만 부르고, 판 종료에서는 호출하지 않는다.
        _deathCallbacks.Remove(enemy);
        enemy.SetDropGem(null);
        enemy.Bind(null);
        enemy.ApplyVisual(null);
        enemy.Initialize(0, -1, 0f, 0f, 0f);
        _pool?.Return(enemy.gameObject);
    }

    #endregion
}
