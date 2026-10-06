using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Timeline;

/// <summary>
/// 몬스터 동작 그림(MonsterAnimationSet)을 AddressableManager로 불러온다.
/// 판 시작 전에 그 판의 적과 보스 그림을 "run" 묶음으로 미리 불러오고, 타이틀로 돌아갈 때 내린다.
/// 판 중에 처음 쓰는 종(상점 구매, 장착 변경)은 받을 때 바로 불러온다.
/// </summary>
public static class MonsterAnimationLoader
{
    public const string RUN_GROUP = "run";

    /// <summary>
    /// 종의 동작 그림. 미리 불러오지 않았으면 지금 불러온다. 참조가 비었으면 null.
    /// </summary>
    public static MonsterAnimationSet Get(MonsterVisualData visual)
    {
        if (visual == null)
        {
            return null;
        }

        var reference = visual.Animations;
#if UNITY_EDITOR
        // 플레이 전(타임라인 미리보기, 인스펙터)에는 Addressables 없이 에셋을 바로 쓴다.
        if (!Application.isPlaying)
        {
            return reference != null ? reference.editorAsset : null;
        }
#endif
        if (reference == null || !reference.RuntimeKeyIsValid() || !TryGetManager(out var manager))
        {
            return null;
        }

        var set = manager.Get<MonsterAnimationSet>(reference);
        if (set != null)
        {
            return set;
        }

        Debug.Log($"[MonsterAnimationLoader] 미리 불러오지 않은 그림을 지금 불러옴: {visual.name}");
        return manager.Load<MonsterAnimationSet>(reference, RUN_GROUP);
    }

    /// <summary>
    /// 스테이지 타임라인에 나오는 적과 보스의 그림을 미리 불러온다.
    /// </summary>
    public static UniTask PreloadStageAsync(StageData stage, CancellationToken cancellationToken)
    {
        if (stage == null || !TryGetManager(out var manager))
        {
            return UniTask.CompletedTask;
        }

        var monsters = new List<MonsterVisualData>();
        CollectStageMonsters(stage, monsters);
        var references = new List<AssetReference>();
        foreach (var monster in monsters)
        {
            if (monster != null && monster.Animations != null && monster.Animations.RuntimeKeyIsValid())
            {
                references.Add(monster.Animations);
            }
        }

        return manager.PreloadAsync<MonsterAnimationSet>(references, RUN_GROUP, cancellationToken);
    }

    /// <summary>
    /// 판에서 불러온 그림을 내린다.
    /// </summary>
    public static void ReleaseRun()
    {
        if (TryGetManager(out var manager))
        {
            manager.ReleaseGroup(RUN_GROUP);
        }
    }

    /// <summary>
    /// 스테이지 타임라인의 몬스터 웨이브와 보스 조우에 나오는 종을 모은다. 꺼 둔 트랙은 뺀다.
    /// </summary>
    public static void CollectStageMonsters(StageData stage, List<MonsterVisualData> monsters)
    {
        var timeline = stage != null ? stage.Timeline : null;
        if (timeline == null)
        {
            return;
        }

        foreach (var track in timeline.GetOutputTracks())
        {
            if (track == null || track.muted)
            {
                continue;
            }

            if (track is MonsterWaveTrack waveTrack)
            {
                AddUnique(monsters, waveTrack.Monster);
                continue;
            }

            if (track is not BossEncounterTrack)
            {
                continue;
            }

            foreach (var clip in track.GetClips())
            {
                if (clip.asset is BossEncounter encounter)
                {
                    encounter.CollectMonsters(monsters);
                }
            }
        }
    }

    private static void AddUnique(List<MonsterVisualData> monsters, MonsterVisualData monster)
    {
        if (monster != null && !monsters.Contains(monster))
        {
            monsters.Add(monster);
        }
    }

    private static bool TryGetManager(out AddressableManager manager)
    {
        manager = null;
        return Managers.Instance != null && Managers.Instance.TryGetManager(out manager);
    }
}
