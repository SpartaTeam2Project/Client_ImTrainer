using System;
using UnityEngine;

/// <summary>
/// 웨이브 클립이 EnemyManager에 스폰을 맡길 때 쓰는 입구.
/// </summary>
public static class MonsterWavePlayback
{
    /// <summary>
    /// 로컬 플레이어 식별자로 웨이브를 낸다. 낸 마리 수를 반환한다.
    /// </summary>
    public static int Spawn(MonsterWaveProfile profile, int count, bool circularSpawn, Action<Enemy> onDied)
    {
        if (profile == null || count <= 0 || Managers.Instance == null)
        {
            return 0;
        }

        if (!Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager)
            || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return 0;
        }

        return enemyManager.SpawnWave(playerManager.LocalPlayerId, profile, count, circularSpawn, onDied);
    }

    /// <summary>
    /// 로컬 플레이어 주변에 원을 한 번 두른다. 낸 마리 수를 반환한다.
    /// </summary>
    public static int SpawnRing(MonsterWaveProfile profile, int count, float radius, float approachSpeed)
    {
        if (profile == null || count <= 0 || Managers.Instance == null)
        {
            return 0;
        }

        if (!Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager)
            || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return 0;
        }

        return enemyManager.SpawnRing(playerManager.LocalPlayerId, profile, count, radius, approachSpeed);
    }

    /// <summary>
    /// 카메라 밖에 묶음을 두고 플레이어 쪽으로 직진시킨다. 낸 마리 수를 반환한다.
    /// </summary>
    public static int SpawnRush(MonsterWaveProfile profile, int count, float clusterRadius, float rushSpeed)
    {
        if (profile == null || count <= 0 || Managers.Instance == null)
        {
            return 0;
        }

        if (!Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager)
            || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return 0;
        }

        return enemyManager.SpawnRush(playerManager.LocalPlayerId, profile, count, clusterRadius, rushSpeed);
    }
}
