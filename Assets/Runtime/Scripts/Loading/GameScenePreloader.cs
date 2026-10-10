using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 로딩 씬에서 게임 씬에 들어가기 전에 셰이더를 데우고 이번 판 그림을 미리 불러온다.
/// </summary>
public static class GameScenePreloader
{
    /// <summary>
    /// 워밍업과 에셋 프리로드를 함께 돌리고 둘 다 끝날 때까지 기다린다.
    /// </summary>
    public static UniTask PreloadAsync(StageData stage, MonsterVisualData runMonster, ShaderVariantCollection shaderVariants, CancellationToken cancellationToken)
    {
        return UniTask.WhenAll(
            ShaderWarmup.WarmUpAsync(shaderVariants, cancellationToken),
            PreloadMonstersAsync(stage, runMonster, cancellationToken));
    }

    /// <summary>
    /// 스테이지의 적과 보스, 플레이어가 데려가는 포켓몬 그림을 불러온다.
    /// </summary>
    private static UniTask PreloadMonstersAsync(StageData stage, MonsterVisualData runMonster, CancellationToken cancellationToken)
    {
        var monsters = new List<MonsterVisualData>();
        MonsterAnimationLoader.CollectStageMonsters(stage, monsters);
        if (runMonster != null && !monsters.Contains(runMonster))
        {
            monsters.Add(runMonster);
        }

        return MonsterAnimationLoader.PreloadAsync(monsters, cancellationToken);
    }
}
