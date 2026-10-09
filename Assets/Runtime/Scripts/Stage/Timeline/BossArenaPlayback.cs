using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 보스 클립이 울타리를 세우고, 파티에서 고른 두 마리를 같이 낸다.
/// </summary>
public static class BossArenaPlayback
{
    private const int PAIR_COUNT = 2;
    private const float RIGHT_SPAWN_INSET = 1.5f;
    private const float PAIR_VERTICAL_OFFSET = 2.5f;
    private const float APPROACH_DELAY_SECONDS = 1f;

    private static int _generation;
    private static int _fightToken;
    private static int _bossesLeft;
    private static RectBossFence _fence;
    private static BossSpawnEntry _fixedBoss;
    private static BossSpawnEntry[] _candidates;
    private static BossFenceSpec _fenceSpec;

    /// <summary>
    /// 지금 연출이나 보스전이 이 토큰이면 true.
    /// </summary>
    public static bool IsCurrent(int token)
    {
        return token == _generation;
    }

    /// <summary>
    /// 일반 스폰을 막고 타임라인을 멈춘 뒤, 트레이너 등장만 연다.
    /// </summary>
    public static void Begin(
        BossSpawnEntry fixedBoss,
        BossSpawnEntry[] candidates,
        int spawnCount,
        BossFenceSpec fence,
        BossTrainerEntranceCast entrance)
    {
        var token = ++_generation;
        _fightToken = token;
        _bossesLeft = 0;
        _fixedBoss = fixedBoss;
        _candidates = candidates;
        _fenceSpec = fence;
        if (!TryGetEnemyManager(out var enemyManager))
        {
            return;
        }

        enemyManager.SetBossFightActive(true);
        enemyManager.DismissAlive();
        if (TryGetStage(out var stage))
        {
            stage.PauseTimelineForBoss();
        }

        SetClockPaused(true);
        BossTrainerEntrance.PlayAsync(token, entrance).Forget();
    }

    /// <summary>
    /// 연출이 끝난 뒤 울타리를 세우고 고른 보스들을 같이 낸다.
    /// </summary>
    public static void StartFight(int token)
    {
        if (!IsCurrent(token))
        {
            return;
        }

        OpenArenaAsync(token, _fixedBoss, _candidates, _fenceSpec).Forget();
    }

    /// <summary>
    /// 울타리와 보스전 잠금을 치운다. 타임라인은 재생하지 않는다.
    /// </summary>
    public static void Cancel()
    {
        _generation++;
        _bossesLeft = 0;
        BossTrainerEntrance.Stop();
        SetClockPaused(false);
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
        BossFenceSpec fence)
    {
        await UniTask.NextFrame();
        if (token != _generation || !TryGetEnemyManager(out var enemyManager))
        {
            return;
        }

        enemyManager.DismissAlive();
        var pool = CollectPool(fixedBoss, candidates);
        if (pool.Count == 0)
        {
            Debug.LogError("보스 클립에 몬스터가 없어 보스전을 건너뜁니다.");
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

        var picked = PickBosses(pool);
        _bossesLeft = 0;
        var spawned = 0;
        for (var i = 0; i < picked.Count; i++)
        {
            var offset = ResolveVerticalOffset(i, picked.Count);
            if (TrySpawnBoss(enemyManager, playerId, picked[i].CreateProfile(), offset))
            {
                spawned++;
            }
        }

        if (spawned == 0)
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

        PublishBossDefeated();
        _bossesLeft--;
        if (_bossesLeft > 0)
        {
            return;
        }

        if (TryGetStage(out var stage))
        {
            stage.MarkBossCleared();
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
        SetClockPaused(false);
        PlayStageMusic();
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

    private static List<BossSpawnEntry> CollectPool(BossSpawnEntry fixedBoss, BossSpawnEntry[] candidates)
    {
        var pool = new List<BossSpawnEntry>();
        if (fixedBoss != null && fixedBoss.Monster != null)
        {
            pool.Add(fixedBoss);
        }

        if (candidates == null)
        {
            return pool;
        }

        for (var i = 0; i < candidates.Length; i++)
        {
            var candidate = candidates[i];
            if (candidate != null && candidate.Monster != null)
            {
                pool.Add(candidate);
            }
        }

        return pool;
    }

    /// <summary>
    /// 풀에서 서로 다른 보스를 최대 두 마리 고른다. 한 마리뿐이면 그 마리만 반환한다.
    /// </summary>
    private static List<BossSpawnEntry> PickBosses(List<BossSpawnEntry> pool)
    {
        var count = Mathf.Min(PAIR_COUNT, pool.Count);
        for (var i = 0; i < count; i++)
        {
            var swap = Random.Range(i, pool.Count);
            var chosen = pool[i];
            pool[i] = pool[swap];
            pool[swap] = chosen;
        }

        return pool.GetRange(0, count);
    }

    private static float ResolveVerticalOffset(int index, int count)
    {
        if (count <= 1)
        {
            return 0f;
        }

        return index == 0 ? PAIR_VERTICAL_OFFSET : -PAIR_VERTICAL_OFFSET;
    }

    private static bool TrySpawnBoss(EnemyManager enemyManager, int playerId, MonsterWaveProfile profile, float verticalOffset)
    {
        var enemy = enemyManager.SpawnBoss(playerId, profile, ResolveSpawnPosition(verticalOffset), OnBossDied);
        if (enemy == null)
        {
            return false;
        }

        enemy.HoldApproach(APPROACH_DELAY_SECONDS);
        _bossesLeft++;
        return true;
    }

    private static Vector2 ResolveSpawnPosition(float verticalOffset)
    {
        if (_fence == null)
        {
            return Vector2.zero;
        }

        var position = _fence.RightInnerPosition(RIGHT_SPAWN_INSET);
        position.y += verticalOffset;
        return _fence.ClampPosition(position);
    }

    private static void PublishBossDefeated()
    {
        if (TryGetPlayerId(out var playerId) && Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            eventManager.Publish(new BossDefeated(playerId));
        }
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

    private static void SetClockPaused(bool paused)
    {
        if (Managers.Instance == null)
        {
            return;
        }

        var gameController = Managers.Instance.GetComponent<GameController>();
        if (gameController != null)
        {
            gameController.SetClockPaused(paused);
        }
    }

    private static void PlayStageMusic()
    {
        if (Managers.Instance == null)
        {
            return;
        }

        var gameController = Managers.Instance.GetComponent<GameController>();
        if (gameController != null)
        {
            gameController.PlayStageMusic();
        }
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
