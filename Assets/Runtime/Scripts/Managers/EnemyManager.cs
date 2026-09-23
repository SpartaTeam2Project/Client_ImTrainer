using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 시간 기준 스폰, 추적, 접촉 피해, 킬 수를 담당한다.
/// </summary>
public class EnemyManager : BaseManager
{
    private const float SPAWN_RADIUS = 8f;
    private const float CONTACT_RADIUS = 0.75f;
    private const float CONTACT_INTERVAL = 0.6f;
    private const int MAX_ALIVE = 40;
    private const float ACTOR_SIZE = 0.7f;

    private readonly List<EnemyActor> _alive = new List<EnemyActor>();
    private readonly List<WaveRuntime> _waves = new List<WaveRuntime>();

    private int _playerId;
    private float _hpMultiplier = 1f;
    private float _damageMultiplier = 1f;
    private bool _stageActive;

    public int KillCount { get; private set; }

    /// <summary>
    /// 적 매니저 초기화
    /// </summary>
    public override UniTask InitializeAsync()
    {
        return base.InitializeAsync();
    }

    /// <summary>
    /// 판을 열고 웨이브 시계를 맞춘다.
    /// </summary>
    public void BeginStage(int playerId, StageData stage)
    {
        ClearActors();
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
    /// Playing 동안 스폰과 추적, 접촉 피해를 진행한다.
    /// </summary>
    public void Tick(int playerId, float elapsedSeconds)
    {
        if (!_stageActive || playerId != _playerId)
        {
            return;
        }

        if (!TryGetPlayerPosition(out var playerPosition, out var playerManager))
        {
            return;
        }

        SpawnWaves(elapsedSeconds, playerPosition);
        UpdateActors(playerPosition, playerManager, playerId);
    }

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
        var actorObject = new GameObject("Enemy");
        actorObject.transform.position = position;
        actorObject.transform.localScale = new Vector3(ACTOR_SIZE, ACTOR_SIZE, 1f);

        var renderer = actorObject.AddComponent<SpriteRenderer>();
        renderer.sprite = PrototypeSprite.WhiteSquare;
        renderer.color = new Color(0.85f, 0.2f, 0.25f, 1f);
        renderer.sortingOrder = 5;

        var actor = actorObject.AddComponent<EnemyActor>();
        var maxHealth = Mathf.Max(1f, wave.MaxHealth * _hpMultiplier);
        var contactDamage = wave.ContactDamage * _damageMultiplier;
        actor.Initialize(runtime.WaveIndex, maxHealth, contactDamage, wave.MoveSpeed);
        runtime.AliveCount++;
        _alive.Add(actor);
    }

    private void UpdateActors(Vector2 playerPosition, PlayerManager playerManager, int playerId)
    {
        for (var i = _alive.Count - 1; i >= 0; i--)
        {
            var actor = _alive[i];
            if (actor == null)
            {
                _alive.RemoveAt(i);
                continue;
            }

            actor.MoveToward(playerPosition, Time.deltaTime);
            if (!actor.IsAlive)
            {
                continue;
            }

            if (Vector2.Distance(actor.transform.position, playerPosition) > CONTACT_RADIUS)
            {
                continue;
            }

            if (!actor.TryStampContact(Time.time, CONTACT_INTERVAL))
            {
                continue;
            }

            playerManager.TakeDamage(playerId, actor.ContactDamage);
            if (playerManager.CurrentHealth <= 0f)
            {
                return;
            }
        }
    }

    private void KillAt(int index, EnemyActor actor)
    {
        KillCount++;
        ReleaseWaveCount(actor.WaveIndex);
        _alive.RemoveAt(index);
        Destroy(actor.gameObject);
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

    private bool TryGetPlayerPosition(out Vector2 playerPosition, out PlayerManager playerManager)
    {
        playerPosition = Vector2.zero;
        playerManager = null;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out playerManager))
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
        for (var i = _alive.Count - 1; i >= 0; i--)
        {
            var actor = _alive[i];
            if (actor != null)
            {
                Destroy(actor.gameObject);
            }
        }

        _alive.Clear();
        _stageActive = false;
    }

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
