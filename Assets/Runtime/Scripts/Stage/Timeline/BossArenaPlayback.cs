using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 보스 클립이 울타리를 세우고, 고른 수만큼 보스를 낸 뒤 전부 죽으면 타임라인을 이어 간다.
/// </summary>
public static class BossArenaPlayback
{
    private const int MIN_SPAWN_COUNT = 1;
    private const int MAX_SPAWN_COUNT = 6;
    private const int MAX_CANDIDATES = 5;
    private const float SPAWN_RADIUS = 1.6f;

    private static int _generation;
    private static int _fightToken;
    private static int _bossesLeft;
    private static RectBossFence _fence;

    /// <summary>
    /// 일반 스폰을 막고 타임라인을 멈춘 뒤, 다음 프레임에 보스전을 연다.
    /// </summary>
    public static void Begin(
        BossSpawnEntry fixedBoss,
        BossSpawnEntry[] candidates,
        int spawnCount,
        BossFenceSpec fence)
    {
        var token = ++_generation;
        _fightToken = token;
        _bossesLeft = 0;
        if (!TryGetEnemyManager(out var enemyManager))
        {
            return;
        }

        enemyManager.SetBossFightActive(true);
        if (TryGetStage(out var stage))
        {
            stage.PauseTimelineForBoss();
        }

        OpenArenaAsync(token, fixedBoss, candidates, spawnCount, fence).Forget();
    }

    /// <summary>
    /// 울타리와 보스전 잠금을 치운다. 타임라인은 재생하지 않는다.
    /// </summary>
    public static void Cancel()
    {
        _generation++;
        _bossesLeft = 0;
        ClearFence();
        if (TryGetEnemyManager(out var enemyManager))
        {
            enemyManager.SetBossFightActive(false);
        }
    }

    private static async UniTaskVoid OpenArenaAsync(
        int token,
        BossSpawnEntry fixedBoss,
        BossSpawnEntry[] candidates,
        int spawnCount,
        BossFenceSpec fence)
    {
        await UniTask.NextFrame();
        if (token != _generation || !TryGetEnemyManager(out var enemyManager))
        {
            return;
        }

        enemyManager.DismissAlive();
        var roster = BuildRoster(fixedBoss, candidates, spawnCount);
        if (roster.Count == 0)
        {
            Debug.LogError("보스 클립에 고정 몬스터가 없어 보스전을 건너뜁니다.");
            Finish(token);
            return;
        }

        if (!TryGetPlayerPosition(out var center))
        {
            Debug.LogError("플레이어 위치가 없어 보스전을 건너뜁니다.");
            Finish(token);
            return;
        }

        ClearFence();
        var parent = TryGetStage(out var stage) ? stage.transform : null;
        _fence = RectBossFence.Create(center, parent, fence);
        if (TryGetField(out var fieldManager))
        {
            fieldManager.SetFence(_fence);
            fieldManager.RemovePropsInsideFence();
        }

        if (!TryGetPlayerId(out var playerId))
        {
            Debug.LogError("플레이어 식별자가 없어 보스전을 건너뜁니다.");
            Finish(token);
            return;
        }

        _bossesLeft = 0;
        for (var i = 0; i < roster.Count; i++)
        {
            var position = ResolveSpawnPosition(center, i, roster.Count);
            var enemy = enemyManager.SpawnBoss(playerId, roster[i], position, OnBossDied);
            if (enemy != null)
            {
                _bossesLeft++;
            }
        }

        if (_bossesLeft == 0)
        {
            Debug.LogError("보스를 스폰하지 못해 보스전을 건너뜁니다.");
            Finish(token);
        }
    }

    private static void OnBossDied(Enemy enemy)
    {
        if (_fightToken != _generation || _bossesLeft <= 0)
        {
            return;
        }

        _bossesLeft--;
        if (_bossesLeft > 0)
        {
            return;
        }

        Finish(_fightToken);
    }

    private static void Finish(int token)
    {
        if (token != _generation)
        {
            return;
        }

        _bossesLeft = 0;
        ClearFence();
        if (TryGetEnemyManager(out var enemyManager))
        {
            enemyManager.SetBossFightActive(false);
        }

        if (TryGetStage(out var stage))
        {
            stage.ResumeTimelineAfterBoss();
        }
    }

    private static void ClearFence()
    {
        if (_fence != null)
        {
            _fence.DestroyVisuals();
            _fence = null;
        }

        if (TryGetField(out var fieldManager))
        {
            fieldManager.ClearFence();
        }
    }

    private static List<MonsterWaveProfile> BuildRoster(BossSpawnEntry fixedBoss, BossSpawnEntry[] candidates, int spawnCount)
    {
        var roster = new List<MonsterWaveProfile>();
        if (fixedBoss == null || fixedBoss.Monster == null)
        {
            return roster;
        }

        var count = Mathf.Clamp(spawnCount, MIN_SPAWN_COUNT, MAX_SPAWN_COUNT);
        roster.Add(fixedBoss.CreateProfile());
        var extra = count - 1;
        if (extra <= 0 || candidates == null)
        {
            return roster;
        }

        var pool = new List<BossSpawnEntry>();
        var limit = Mathf.Min(MAX_CANDIDATES, candidates.Length);
        for (var i = 0; i < limit; i++)
        {
            var candidate = candidates[i];
            if (candidate != null && candidate.Monster != null)
            {
                pool.Add(candidate);
            }
        }

        for (var i = pool.Count - 1; i > 0; i--)
        {
            var swap = Random.Range(0, i + 1);
            var current = pool[i];
            pool[i] = pool[swap];
            pool[swap] = current;
        }

        var take = Mathf.Min(extra, pool.Count);
        for (var i = 0; i < take; i++)
        {
            roster.Add(pool[i].CreateProfile());
        }

        return roster;
    }

    private static Vector2 ResolveSpawnPosition(Vector2 center, int index, int count)
    {
        var angle = Mathf.PI * 0.5f + Mathf.PI * 2f * index / Mathf.Max(1, count);
        var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * SPAWN_RADIUS;
        var position = center + offset;
        return _fence != null ? _fence.ClampPosition(position) : position;
    }

    private static bool TryGetPlayerId(out int playerId)
    {
        playerId = 0;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return false;
        }

        playerId = playerManager.LocalPlayerId;
        return true;
    }

    private static bool TryGetPlayerPosition(out Vector2 position)
    {
        position = Vector2.zero;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return false;
        }

        var playerTransform = playerManager.PlayerTransform;
        if (playerTransform == null)
        {
            return false;
        }

        position = playerTransform.position;
        return true;
    }

    private static bool TryGetEnemyManager(out EnemyManager enemyManager)
    {
        enemyManager = null;
        return Managers.Instance != null && Managers.Instance.TryGetManager(out enemyManager);
    }

    private static bool TryGetField(out StageFieldManager fieldManager)
    {
        fieldManager = null;
        return Managers.Instance != null && Managers.Instance.TryGetManager(out fieldManager);
    }

    private static bool TryGetStage(out StageController stage)
    {
        stage = null;
        if (Managers.Instance == null)
        {
            return false;
        }

        var gameController = Managers.Instance.GetComponent<GameController>();
        if (gameController == null || gameController.ActiveStage == null)
        {
            return false;
        }

        stage = gameController.ActiveStage;
        return true;
    }
}
