using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 보스 클립이 울타리를 세우고, 고정 보스 다음 후보 한 마리를 이어서 낸다.
/// </summary>
public static class BossArenaPlayback
{
    private const int MAX_CANDIDATES = 5;
    private const float RIGHT_SPAWN_INSET = 1.5f;
    private const float APPROACH_DELAY_SECONDS = 1f;

    private static int _generation;
    private static int _fightToken;
    private static int _bossesLeft;
    private static RectBossFence _fence;
    private static BossSpawnEntry _fixedBoss;
    private static BossSpawnEntry[] _candidates;
    private static BossFenceSpec _fenceSpec;
    private static MonsterWaveProfile _nextBoss;

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
        _nextBoss = null;
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
    /// 연출이 끝난 뒤 울타리를 세우고 클립에 넣은 보스를 낸다.
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
        _nextBoss = null;
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
        if (fixedBoss == null || fixedBoss.Monster == null)
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

        _nextBoss = PickNextBoss(candidates);
        _bossesLeft = 0;
        if (!TrySpawnBoss(enemyManager, playerId, fixedBoss.CreateProfile(), true))
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

        if (_nextBoss != null
            && TryGetEnemyManager(out var enemyManager)
            && TryGetPlayerId(out var playerId)
            && TrySpawnBoss(enemyManager, playerId, _nextBoss))
        {
            _nextBoss = null;
            return;
        }

        _nextBoss = null;
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
        _nextBoss = null;
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

    private static MonsterWaveProfile PickNextBoss(BossSpawnEntry[] candidates)
    {
        if (candidates == null)
        {
            return null;
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

        if (pool.Count == 0)
        {
            return null;
        }

        return pool[Random.Range(0, pool.Count)].CreateProfile();
    }

    private static bool TrySpawnBoss(EnemyManager enemyManager, int playerId, MonsterWaveProfile profile, bool holdApproach = false)
    {
        var enemy = enemyManager.SpawnBoss(playerId, profile, ResolveSpawnPosition(), OnBossDied);
        if (enemy == null)
        {
            return false;
        }

        if (holdApproach)
        {
            enemy.HoldApproach(APPROACH_DELAY_SECONDS);
        }

        _bossesLeft++;
        return true;
    }

    private static Vector2 ResolveSpawnPosition()
    {
        if (_fence == null)
        {
            return Vector2.zero;
        }

        return _fence.RightInnerPosition(RIGHT_SPAWN_INSET);
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
