using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 시간 기준 스폰, 활성 적 목록, 킬 수와 Pool 수명을 담당한다.
/// 흐름: Tick => Wave 조건 => Pool.Get => Enemy.Bind/ApplyVisual/Initialize => 기존 Enemy.Tick.
/// 사망: NotifyDied => 목록/카운트 갱신 => 상태 정리 => Pool.Return.
/// </summary>
public class EnemyManager : BaseManager
{
    private const float SPAWN_RADIUS = 8f;
    private const int MAX_ALIVE = 40;
    private const float ACTOR_SIZE = 0.7f;
    private const int ACTOR_SORTING_ORDER = 5;

    [Header("Enemy Settings")]
    [SerializeField] private Enemy _enemyPrefab;

    private readonly List<Enemy> _alive = new List<Enemy>();
    private readonly List<WaveRuntime> _waves = new List<WaveRuntime>();
    private CombatObjectPool _pool;
    private GameObject _fallbackPrefab;

    private int _playerId;
    private float _hpMultiplier = 1f;
    private float _damageMultiplier = 1f;
    private bool _stageActive;

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
    /// 판을 열고 웨이브 시계를 맞춘다. 같은 Scene의 Pool은 다음 판에도 재사용한다.
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
        _stageActive = stage != null;
        _hpMultiplier = stage != null ? Mathf.Max(0.01f, stage.EnemyHpMultiplier) : 1f;
        _damageMultiplier = stage != null ? Mathf.Max(0f, stage.EnemyDamageMultiplier) : 1f;
        _waves.Clear();

        if (stage == null || stage.Waves == null)
        {
            Debug.LogError("StageData가 없어 적을 스폰하지 않습니다.");
            return;
        }

        if (_pool == null)
        {
            _pool = new CombatObjectPool(stageRoot);
        }

        for (var i = 0; i < stage.Waves.Length; i++)
        {
            var wave = stage.Waves[i];
            if (wave == null)
            {
                continue;
            }

            _waves.Add(new WaveRuntime(i, wave));
        }
    }

    /// <summary>
    /// 판 종료 시 활성 적을 반환한다. 결과에 쓸 KillCount와 재사용할 Pool은 유지한다.
    /// </summary>
    public void EndStage()
    {
        ClearActors();
        _waves.Clear();
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
    }

    /// <summary>
    /// Playing 동안 스폰을 진행하고 살아 있는 적에게 플레이어 위치를 넘긴다.
    /// </summary>
    public void Tick(int playerId, float elapsedSeconds)
    {
        if (!_stageActive || playerId != _playerId)
        {
            return;
        }

        if (!TryGetPlayerPosition(out var playerPosition))
        {
            return;
        }

        SpawnWaves(elapsedSeconds, playerPosition);
        UpdateEnemies(playerPosition);
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
        var radiusSqr = radius * radius;
        for (var i = 0; i < _alive.Count; i++)
        {
            var candidate = _alive[i];
            if (candidate == null || !candidate.IsAlive)
            {
                continue;
            }

            var offset = (Vector2)candidate.transform.position - origin;
            var along = Vector2.Dot(offset, forward);
            if (along < 0f || along > length)
            {
                continue;
            }

            var lateral = offset - forward * along;
            if (lateral.sqrMagnitude > radiusSqr)
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
        var index = _alive.IndexOf(enemy);
        if (index < 0)
        {
            return;
        }

        ReleaseWaveCount(enemy.WaveIndex);
        _alive.RemoveAt(index);
    }

    #endregion

    #region Private Methods

    private void SpawnWaves(float elapsedSeconds, Vector2 playerPosition)
    {
        for (var i = 0; i < _waves.Count; i++)
        {
            var runtime = _waves[i];
            var wave = runtime.Spawn;
            if (elapsedSeconds < wave.StartSeconds || elapsedSeconds > wave.EndSeconds)
            {
                continue;
            }

            switch (wave.Kind)
            {
                case WaveKind.Burst:
                    SpawnBurst(runtime, playerPosition);
                    break;
                case WaveKind.Maintain:
                    SpawnMaintain(runtime, elapsedSeconds, playerPosition);
                    break;
                default:
                    SpawnContinuous(runtime, elapsedSeconds, playerPosition);
                    break;
            }
        }
    }

    private void SpawnBurst(WaveRuntime runtime, Vector2 playerPosition)
    {
        if (runtime.BurstFired)
        {
            return;
        }

        runtime.BurstFired = true;
        SpawnCircle(runtime, playerPosition, Mathf.Max(1, runtime.Spawn.Count));
    }

    private void SpawnMaintain(WaveRuntime runtime, float elapsedSeconds, Vector2 playerPosition)
    {
        if (runtime.AliveCount >= runtime.Spawn.Count || elapsedSeconds < runtime.NextSpawnElapsed)
        {
            return;
        }

        runtime.NextSpawnElapsed = elapsedSeconds + Mathf.Max(0.05f, runtime.Spawn.SpawnInterval);
        SpawnCircle(runtime, playerPosition, 1);
    }

    private void SpawnContinuous(WaveRuntime runtime, float elapsedSeconds, Vector2 playerPosition)
    {
        if (elapsedSeconds < runtime.NextSpawnElapsed)
        {
            return;
        }

        runtime.NextSpawnElapsed = elapsedSeconds + Mathf.Max(0.05f, runtime.Spawn.SpawnInterval);
        SpawnCircle(runtime, playerPosition, 1);
    }

    private void SpawnCircle(WaveRuntime runtime, Vector2 origin, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (_alive.Count >= MAX_ALIVE)
            {
                return;
            }

            var angle = (Mathf.PI * 2f * i / count) + Random.Range(-0.25f, 0.25f);
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * SPAWN_RADIUS;
            CreateEnemy(runtime, origin + offset);
        }
    }

    private void CreateEnemy(WaveRuntime runtime, Vector2 position)
    {
        var wave = runtime.Spawn;
        var enemy = CreateActor(_enemyPrefab, position);
        if (enemy == null)
        {
            return;
        }

        var maxHealth = Mathf.Max(1f, wave.MaxHealth * _hpMultiplier);
        var contactDamage = wave.ContactDamage * _damageMultiplier;
        // 재사용 여부와 관계없이 그림과 스탯을 넣은 뒤 Tick에 등록한다.
        enemy.Bind(this);
        enemy.ApplyVisual(SelectMonsterVisual(wave));
        enemy.Initialize(_playerId, runtime.WaveIndex, maxHealth, contactDamage, wave.MoveSpeed);
        runtime.AliveCount++;
        _alive.Add(enemy);
    }

    private MonsterVisualData SelectMonsterVisual(WaveSpawn wave)
    {
        if (wave.Monster != null)
        {
            return wave.Monster;
        }

        if (wave.Enemies == null || wave.Enemies.Length == 0)
        {
            return null;
        }

        var totalWeight = 0;
        for (var i = 0; i < wave.Enemies.Length; i++)
        {
            var entry = wave.Enemies[i];
            if (entry == null || entry.Visual == null || entry.Weight <= 0)
            {
                continue;
            }

            totalWeight += entry.Weight;
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        var roll = Random.Range(0, totalWeight);
        for (var i = 0; i < wave.Enemies.Length; i++)
        {
            var entry = wave.Enemies[i];
            if (entry == null || entry.Visual == null || entry.Weight <= 0)
            {
                continue;
            }

            if (roll < entry.Weight)
            {
                return entry.Visual;
            }

            roll -= entry.Weight;
        }

        return null;
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

    private void UpdateEnemies(Vector2 playerPosition)
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
                return;
            }
        }
    }

    private void KillAt(int index, Enemy enemy)
    {
        var dropPosition = enemy != null ? (Vector2)enemy.transform.position : Vector2.zero;
        KillCount++;
        ReleaseWaveCount(enemy.WaveIndex);
        _alive.RemoveAt(index);
        DropExperience(dropPosition, enemy != null ? enemy.ExperienceGem : null);
        ReturnEnemy(enemy);
    }

    private void DropExperience(Vector2 position, ExperienceGem gem)
    {
        if (gem == null || Managers.Instance == null || !Managers.Instance.TryGetManager<ExperienceManager>(out var experienceManager))
        {
            return;
        }

        experienceManager.Drop(_playerId, position, gem);
    }

    private void ReleaseWaveCount(int waveIndex)
    {
        for (var i = 0; i < _waves.Count; i++)
        {
            if (_waves[i].WaveIndex != waveIndex)
            {
                continue;
            }

            _waves[i].AliveCount = Mathf.Max(0, _waves[i].AliveCount - 1);
            return;
        }
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

    private void ClearActors()
    {
        _stageActive = false;
        for (var i = _alive.Count - 1; i >= 0; i--)
        {
            var enemy = _alive[i];
            _alive.RemoveAt(i);
            if (enemy != null)
            {
                // Stage 종료는 처치가 아니다. KillCount를 올리지 않고 Wave 수만 줄인다.
                ReleaseWaveCount(enemy.WaveIndex);
                ReturnEnemy(enemy);
            }
        }
    }

    private void ReturnEnemy(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        // 비활성 보관 중의 피해/파괴 알림이 기존 판의 상태를 바꾸지 않도록 해제한다.
        // Enemy 내부 수정 없이 기존 Initialize로 HP, WaveIndex, 접촉 시각 등을 비운다.
        enemy.Bind(null);
        enemy.ApplyVisual(null);
        enemy.Initialize(0, -1, 0f, 0f, 0f);
        _pool?.Return(enemy.gameObject);
    }

    #endregion

    private sealed class WaveRuntime
    {
        public WaveRuntime(int waveIndex, WaveSpawn spawn)
        {
            WaveIndex = waveIndex;
            Spawn = spawn;
            NextSpawnElapsed = spawn.StartSeconds;
        }

        public int WaveIndex { get; }

        public WaveSpawn Spawn { get; }

        public float NextSpawnElapsed { get; set; }

        public bool BurstFired { get; set; }

        public int AliveCount { get; set; }
    }
}
