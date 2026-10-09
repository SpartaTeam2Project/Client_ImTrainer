using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// 판 통계를 플레이어 식별자마다 모은다. 이벤트를 받아 StageStatsTracker로 넘기기만 하고,
/// 집계는 Stage/Stats 아래 장부가 맡는다. 판 결과는 StageController가 FinishStage로 받아 간다.
/// </summary>
public class StageStatsManager : BaseManager
{
    private readonly Dictionary<int, StageStatsTracker> _trackers = new Dictionary<int, StageStatsTracker>();
    private readonly List<StagePartyMember> _party = new List<StagePartyMember>(WeaponSlots.MAX_COUNT);
    private GameController _gameController;

    private float Now => _gameController != null ? _gameController.ElapsedSeconds : 0f;

    public override UniTask InitializeAsync()
    {
        _gameController = Managers.Instance != null ? Managers.Instance.GetComponent<GameController>() : null;
        if (Managers.Instance != null && Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            eventManager.Subscribe<EquipmentChanged>(HandleEquipmentChanged);
            eventManager.Subscribe<EnemyDamaged>(HandleEnemyDamaged);
            eventManager.Subscribe<PlayerDamaged>(HandlePlayerDamaged);
            eventManager.Subscribe<PlayerHealed>(HandlePlayerHealed);
            eventManager.Subscribe<ExperienceGained>(HandleExperienceGained);
            eventManager.Subscribe<BossDefeated>(HandleBossDefeated);
            eventManager.Subscribe<ItemPurchased>(HandleItemPurchased);
            eventManager.Subscribe<ItemSold>(HandleItemSold);
            eventManager.Subscribe<ItemSynthesized>(HandleItemSynthesized);
            eventManager.Subscribe<CurrencyDeposited>(HandleCurrencyDeposited);
            eventManager.Subscribe<StageCounterAdded>(HandleCounterAdded);
        }

        return base.InitializeAsync();
    }

    public override void Cleanup()
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            eventManager.Unsubscribe<EquipmentChanged>(HandleEquipmentChanged);
            eventManager.Unsubscribe<EnemyDamaged>(HandleEnemyDamaged);
            eventManager.Unsubscribe<PlayerDamaged>(HandlePlayerDamaged);
            eventManager.Unsubscribe<PlayerHealed>(HandlePlayerHealed);
            eventManager.Unsubscribe<ExperienceGained>(HandleExperienceGained);
            eventManager.Unsubscribe<BossDefeated>(HandleBossDefeated);
            eventManager.Unsubscribe<ItemPurchased>(HandleItemPurchased);
            eventManager.Unsubscribe<ItemSold>(HandleItemSold);
            eventManager.Unsubscribe<ItemSynthesized>(HandleItemSynthesized);
            eventManager.Unsubscribe<CurrencyDeposited>(HandleCurrencyDeposited);
            eventManager.Unsubscribe<StageCounterAdded>(HandleCounterAdded);
        }

        _trackers.Clear();
        base.Cleanup();
    }

    /// <summary>
    /// 새 판 통계를 연다. 시작 포켓몬 장착 이벤트를 받아야 하므로 ItemManager.BeginRun보다 먼저 부른다.
    /// </summary>
    public void BeginStage(int playerId)
    {
        _trackers[playerId] = new StageStatsTracker();
    }

    /// <summary>
    /// 판 통계를 닫고 돌려준다. 파티를 읽으므로 ItemManager.EndRun보다 먼저 부른다.
    /// </summary>
    public StageStatsReport FinishStage(int playerId, int killCount)
    {
        if (!_trackers.TryGetValue(playerId, out var tracker))
        {
            return default;
        }

        _trackers.Remove(playerId);
        var playerStats = System.Array.Empty<StageStatValue>();
        if (Managers.Instance.TryGetManager<PlayerManager>(out var playerManager)
            && playerManager.TryGetPlayer(playerId, out var player))
        {
            playerStats = StagePlayerStatReader.Read(player);
            tracker.Counters.Set(StageStatKeys.REACHED_LEVEL, player.Experience.Level);
        }

        tracker.Counters.Set(StageStatKeys.KILLS, killCount);
        _party.Clear();
        if (Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            itemManager.CopyParty(playerId, _party);
        }

        return tracker.Finish(Now, playerStats, _party.ToArray());
    }

    /// <summary>
    /// 결과 없이 판을 버린다. 일시정지에서 나가거나 씬을 떠날 때다.
    /// </summary>
    public void CancelStage(int playerId)
    {
        _trackers.Remove(playerId);
    }

    private StageStatsTracker GetTracker(int playerId)
    {
        _trackers.TryGetValue(playerId, out var tracker);
        return tracker;
    }

    private void HandleEquipmentChanged(EquipmentChanged changed)
    {
        GetTracker(changed.PlayerId)?.Units.HandleEquipmentChanged(
            Now, changed.Slot, changed.MemberId, changed.Uid, changed.UpgradeLevel, ResolveAbilityType(changed.Uid));
    }

    private void HandleEnemyDamaged(EnemyDamaged damaged)
    {
        GetTracker(damaged.PlayerId)?.Units.AddDamage(Now, damaged.MemberId, damaged.AbilityType, damaged.Amount);
    }

    private void HandlePlayerDamaged(PlayerDamaged damaged)
    {
        GetTracker(damaged.PlayerId)?.Counters.Add(StageStatKeys.DAMAGE_TAKEN, damaged.Amount);
    }

    private void HandlePlayerHealed(PlayerHealed healed)
    {
        GetTracker(healed.PlayerId)?.Counters.Add(StageStatKeys.HEALED, healed.Amount);
    }

    private void HandleExperienceGained(ExperienceGained gained)
    {
        GetTracker(gained.PlayerId)?.Counters.Add(StageStatKeys.EXP_GAINED, gained.Amount);
    }

    private void HandleBossDefeated(BossDefeated defeated)
    {
        GetTracker(defeated.PlayerId)?.Counters.Add(StageStatKeys.BOSS_KILLS, 1f);
    }

    private void HandleItemPurchased(ItemPurchased purchased)
    {
        GetTracker(purchased.PlayerId)?.Counters.Add(StageStatKeys.SHOP_PURCHASES, 1f);
    }

    private void HandleItemSold(ItemSold sold)
    {
        GetTracker(sold.PlayerId)?.Counters.Add(StageStatKeys.SELLS, sold.Count);
    }

    private void HandleItemSynthesized(ItemSynthesized synthesized)
    {
        GetTracker(synthesized.PlayerId)?.Counters.Add(StageStatKeys.SYNTHESES, 1f);
    }

    private void HandleCurrencyDeposited(CurrencyDeposited deposited)
    {
        GetTracker(deposited.PlayerId)?.CurrenciesGained.Add(deposited.CurrencyId, deposited.Amount);
    }

    private void HandleCounterAdded(StageCounterAdded added)
    {
        GetTracker(added.PlayerId)?.Counters.Add(added.Key, added.Amount);
    }

    private static WeaponAbilityType ResolveAbilityType(int uid)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            return default;
        }

        var visual = itemManager.GetVisual(uid);
        return visual != null && visual.WeaponAbility != null ? visual.WeaponAbility.WeaponAbilityType : default;
    }
}
