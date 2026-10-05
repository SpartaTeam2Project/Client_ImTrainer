using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 능력 동작의 공통 레벨 적용.
/// </summary>
public abstract class WeaponAbilityBehavior<T, K> : MonoBehaviour, IWeaponAbilityBehavior where T : GenericWeaponAbilityData<K> where K : WeaponAbilityLevel
{
    public T Data { get; private set; }

    public WeaponAbilityData WeaponAbilityData => Data;

    public WeaponAbilityType WeaponAbilityType => Data != null ? Data.WeaponAbilityType : default;

    public K WeaponAbilityLevel { get; private set; }

    public int LevelId { get; private set; }

    protected int PlayerId { get; private set; }

    /// <summary>
    /// 데이터를 연결하고 시작 레벨을 적용한다.
    /// </summary>
    public virtual void Init(int playerId, WeaponAbilityData data, int level)
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
        SetWeaponAbilityLevel(level);
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

    protected virtual void SetWeaponAbilityLevel(int levelId)
    {
        LevelId = levelId;
        WeaponAbilityLevel = Data != null ? Data.GetLevel(levelId) : null;
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

            if (!WeaponAbilityManager.IsCombatPaused())
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
    /// 이 공격 능력의 상성 타입. 능력 SO의 속성을 쓰고, 공격이 아니면 노말.
    /// </summary>
    protected MonsterType ResolveAttackType()
    {
        if (Data != null && Data.TryGetElementType(out var attackType))
        {
            return attackType;
        }

        return MonsterType.Normal;
    }
}
