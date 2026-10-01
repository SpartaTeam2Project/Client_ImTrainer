using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 능력 동작의 공통 레벨 적용.
/// </summary>
public abstract class AbilityBehavior<T, K> : MonoBehaviour, IAbilityBehavior where T : GenericAbilityData<K> where K : AbilityLevel
{
    public T Data { get; private set; }

    public AbilityData AbilityData => Data;

    public AbilityType AbilityType => Data != null ? Data.AbilityType : default;

    public K AbilityLevel { get; private set; }

    public int LevelId { get; private set; }

    protected int PlayerId { get; private set; }

    /// <summary>
    /// 데이터를 연결하고 시작 레벨을 적용한다.
    /// </summary>
    public virtual void Init(int playerId, AbilityData data, int level)
    {
        PlayerId = playerId;
        Data = data as T;
        ApplyLevel(level);
    }

    /// <summary>
    /// 레벨 수치를 다시 읽고 적용한다.
    /// </summary>
    public void ApplyLevel(int level)
    {
        SetAbilityLevel(level);
    }

    /// <summary>
    /// 동작 오브젝트를 없앤다.
    /// </summary>
    public virtual void Clear()
    {
        // 플레이 종료 때는 매니저 정리보다 먼저 파괴돼 있을 수 있다.
        if (this == null)
        {
            return;
        }

        Destroy(gameObject);
    }

    protected virtual void SetAbilityLevel(int levelId)
    {
        LevelId = levelId;
        AbilityLevel = Data != null ? Data.GetLevel(levelId) : null;
    }

    /// <summary>
    /// 이 능력의 플레이어. 없으면 false.
    /// </summary>
    protected bool TryGetPlayer(out Player player)
    {
        player = null;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return false;
        }

        return playerManager.TryGetPlayer(PlayerId, out player);
    }

    /// <summary>
    /// 일시정지 시간을 제외하고 기다린다. 취소되면 true.
    /// </summary>
    protected async UniTask<bool> WaitCombatSecondsAsync(float seconds, CancellationToken token)
    {
        var elapsed = 0f;
        while (elapsed < seconds)
        {
            if (token.IsCancellationRequested)
            {
                return true;
            }

            if (!AbilityManager.IsCombatPaused())
            {
                elapsed += Time.deltaTime;
            }

            var frameCanceled = await UniTask.Yield(token).SuppressCancellationThrow();
            if (frameCanceled)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 이번 판 포켓몬의 공격 타입. 없으면 노말.
    /// </summary>
    protected MonsterType ResolveAttackType()
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<AccountManager>(out var account))
        {
            var monster = account.ResolveRunMonster();
            if (monster != null)
            {
                return monster.PrimaryType;
            }
        }

        return MonsterType.Normal;
    }
}
