using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 획득한 능력을 들고, 레벨업 카드를 고르게 한다.
/// </summary>
public class WeaponAbilityManager : BaseManager
{
    private const int MAX_OFFERS = 3;
    private const int FIRST_LEVELS = 10;
    private const int MISSING_LEVEL = -1;
    private const int DEFAULT_SLOT_CAPACITY = 5;

    [Header("Abilities")]
    [SerializeField] private WeaponAbilitiesDatabase _abilitiesDatabase;
    [SerializeField] private WeaponAbilitiesWindowBehavior _abilitiesWindow;

    [Header("Level Up")]
    [SerializeField] private AudioClip _levelUpFanfare;

    private readonly List<EquippedAbilityLevel> _equippedLevels = new List<EquippedAbilityLevel>();
    private readonly Dictionary<StarLevelKey, int> _appliedStarLevels = new Dictionary<StarLevelKey, int>();
    private readonly List<int> _evolutionSlots = new List<int>();
    private readonly List<int> _evolutionLevels = new List<int>();
    private readonly List<WeaponAbilityType> _removedAbilityTypes = new List<WeaponAbilityType>();
    private readonly Queue<int> _levelUpQueue = new Queue<int>();
    private readonly Dictionary<int, WeaponAbilityLoadout> _loadouts = new Dictionary<int, WeaponAbilityLoadout>();
    private readonly List<WeaponAbilityData> _currentOffers = new List<WeaponAbilityData>();

    private int _playerId;
    private bool _waitingForChoice;
    private bool _weaponsPaused;
    private bool _stageRunning;

    public int PendingLevelUpLevel { get; private set; }

    public IReadOnlyList<WeaponAbilityData> CurrentOffers => _currentOffers;

    #region Unity Methods

    /// <summary>
    /// 능력 매니저 초기화
    /// </summary>
    public override UniTask InitializeAsync()
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            eventManager.Subscribe<EquipmentChanged>(HandleEquipmentChanged);
        }

        return base.InitializeAsync();
    }

    public override void Cleanup()
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            eventManager.Unsubscribe<EquipmentChanged>(HandleEquipmentChanged);
        }

        ClearLevelUpQueue();
        ClearAbilities();
        base.Cleanup();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 고른 포켓몬의 공격 능력을 성에 맞는 레벨로 넣는다.
    /// </summary>
    public void BeginStage(int playerId)
    {
        _playerId = playerId;
        _appliedStarLevels.Clear();
        ClearLevelUpQueue();
        ClearAbilities(playerId);
        var playerManager = GetManager<PlayerManager>();
        if (playerManager == null || playerManager.PlayerTransform == null)
        {
            Debug.LogError("플레이어가 없어 능력을 붙이지 않습니다.");
            return;
        }

        GrantStartingWeaponAbility(playerId);
        ApplyEquippedAbilityLevels(playerId);
        _stageRunning = true;
    }

    /// <summary>
    /// 판이 끝나면 능력을 치운다.
    /// </summary>
    public void EndStage()
    {
        _stageRunning = false;
        _appliedStarLevels.Clear();
        ClearLevelUpQueue();
        ClearAbilities(_playerId);
    }

    /// <summary>
    /// 레벨업 카드 진입. 대기열에 넣고 선택창을 연다.
    /// </summary>
    public void OfferLevelUp(int playerId, int level)
    {
        if (playerId != _playerId || level <= 0)
        {
            return;
        }

        EnqueueLevelUp(level);
        PresentWeaponAbilityOffer();
    }

    /// <summary>
    /// 능력 프리팹을 만들고 레벨을 적용한다. 진화면 요구 능력을 치운다.
    /// 점 발사 진화는 빠지는 칸마다 하나씩 만든다.
    /// </summary>
    public void AddWeaponAbility(int playerId, WeaponAbilityData abilityData, int level = 0, int slot = EquippedAbilityLevel.UNSLOTTED)
    {
        if (playerId != _playerId || abilityData == null || abilityData.Prefab == null)
        {
            return;
        }

        if (abilityData.IsEvolution && ItemManager.IsSlotOriginAbility(abilityData))
        {
            AddSlotOriginEvolution(playerId, abilityData, level);
            return;
        }

        if (slot != EquippedAbilityLevel.UNSLOTTED
            && GetAcquiredAtSlot(playerId, abilityData.WeaponAbilityType, slot) != null)
        {
            return;
        }

        RemoveEvolvedAbilities(playerId, abilityData);
        CreateAbility(playerId, abilityData, level, slot);
    }

    /// <summary>
    /// 고른 카드를 적용한다. 엔드게임은 레벨을 올리지 않는다.
    /// 같은 타입의 칸 인스턴스는 만렙이 아닌 것만 한 단계씩 올린다.
    /// </summary>
    public void ApplyOffer(int playerId, WeaponAbilityData abilityData)
    {
        if (playerId != _playerId || abilityData == null)
        {
            return;
        }

        if (abilityData.IsEndgameAbility || !IsWeaponAbilityAquired(playerId, abilityData.WeaponAbilityType))
        {
            AddWeaponAbility(playerId, abilityData, 0);
            return;
        }

        if (IsAbilityMaxed(playerId, abilityData))
        {
            return;
        }

        RaiseAcquiredLevels(playerId, abilityData);
    }

    /// <summary>
    /// 가진 능력의 가장 높은 레벨. 없으면 -1.
    /// </summary>
    public int GetWeaponAbilityLevel(int playerId, WeaponAbilityType abilityType)
    {
        if (!_loadouts.TryGetValue(playerId, out var loadout))
        {
            return MISSING_LEVEL;
        }

        var max = MISSING_LEVEL;
        for (var i = 0; i < loadout.Acquired.Count; i++)
        {
            var ability = loadout.Acquired[i];
            if (ability == null || ability.WeaponAbilityType != abilityType)
            {
                continue;
            }

            if (ability.LevelId > max)
            {
                max = ability.LevelId;
            }
        }

        if (max != MISSING_LEVEL)
        {
            return max;
        }

        if (loadout.Levels.TryGetValue(abilityType, out var level))
        {
            return level;
        }

        return MISSING_LEVEL;
    }

    /// <summary>
    /// 이미 가진 능력이면 true.
    /// </summary>
    public bool IsWeaponAbilityAquired(int playerId, WeaponAbilityType abilityType)
    {
        return GetAquiredWeaponAbility(playerId, abilityType) != null;
    }

    /// <summary>
    /// 진화 하나당 액티브 하나와 패시브 하나를 전제로, 짝이 되는 요구 타입을 찾는다.
    /// </summary>
    public bool HasEvolution(WeaponAbilityType abilityType, out WeaponAbilityType otherRequiredWeaponAbilityType)
    {
        otherRequiredWeaponAbilityType = abilityType;
        if (_abilitiesDatabase == null)
        {
            return false;
        }

        for (var i = 0; i < _abilitiesDatabase.AbilitiesCount; i++)
        {
            var ability = _abilitiesDatabase.GetWeaponAbility(i);
            if (ability == null || !ability.IsEvolution || ability.EvolutionRequirements == null)
            {
                continue;
            }

            for (var j = 0; j < ability.EvolutionRequirements.Count; j++)
            {
                var requirement = ability.EvolutionRequirements[j];
                if (requirement == null || requirement.WeaponAbilityType != abilityType)
                {
                    continue;
                }

                for (var k = 0; k < ability.EvolutionRequirements.Count; k++)
                {
                    if (k == j || ability.EvolutionRequirements[k] == null)
                    {
                        continue;
                    }

                    otherRequiredWeaponAbilityType = ability.EvolutionRequirements[k].WeaponAbilityType;
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Combat Pause

    /// <summary>
    /// 연출 동안 공격 능력이 탄을 내지 않으면 true.
    /// </summary>
    public bool WeaponsPaused => _weaponsPaused;

    /// <summary>
    /// 연출 중이면 공격 능력을 멈춘다.
    /// </summary>
    public static bool IsCombatPaused()
    {
        return Managers.Instance != null
            && Managers.Instance.TryGetManager<WeaponAbilityManager>(out var abilityManager)
            && abilityManager.WeaponsPaused;
    }

    /// <summary>
    /// 연출 동안 공격 능력 발사를 멈춘다.
    /// </summary>
    public void SetWeaponsPaused(bool paused)
    {
        _weaponsPaused = paused;
    }

    #endregion

    #region Level Up Queue

    /// <summary>
    /// 증강 선택이 끝났을 때 남은 레벨업을 이어서 연다.
    /// </summary>
    public void CompleteLevelUpOffer()
    {
        _waitingForChoice = false;
        if (_levelUpQueue.Count == 0)
        {
            ResumeAfterWeaponAbilityChoice();
            return;
        }

        PresentWeaponAbilityOffer();
    }

    private void EnqueueLevelUp(int level)
    {
        _levelUpQueue.Enqueue(level);
    }

    private bool TryDequeueLevelUp(out int level)
    {
        if (_levelUpQueue.Count == 0)
        {
            level = 0;
            return false;
        }

        level = _levelUpQueue.Dequeue();
        PendingLevelUpLevel = level;
        return true;
    }

    private void PlayLevelUpSound()
    {
        if (_levelUpFanfare == null || Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(_levelUpFanfare);
    }

    private void ClearLevelUpQueue()
    {
        _levelUpQueue.Clear();
        _waitingForChoice = false;
        PendingLevelUpLevel = 0;
        _currentOffers.Clear();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 장착 포켓몬의 성으로 무기 능력 레벨을 맞춘다. 점 발사는 칸마다, 그 외는 가장 높은 성이다.
    /// </summary>
    public void ApplyEquippedAbilityLevels(int playerId)
    {
        if (playerId != _playerId
            || Managers.Instance == null
            || !Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            return;
        }

        itemManager.CollectEquippedAbilityLevels(playerId, _equippedLevels);
        RemoveUnequippedCatalogAbilities(playerId, itemManager);
        var loadout = GetLoadout(playerId);
        for (var i = 0; i < _equippedLevels.Count; i++)
        {
            var equipped = _equippedLevels[i];
            if (equipped.Ability == null)
            {
                continue;
            }

            var abilityType = equipped.Ability.WeaponAbilityType;
            if (loadout.Removed.Contains(abilityType))
            {
                TryRestoreSlotEvolution(playerId, equipped);
                continue;
            }

            var starKey = new StarLevelKey(abilityType, equipped.Slot);
            var acquired = equipped.Slot == EquippedAbilityLevel.UNSLOTTED
                ? GetUnslottedAbility(playerId, abilityType)
                : GetAcquiredAtSlot(playerId, abilityType, equipped.Slot);
            if (acquired == null)
            {
                AddWeaponAbility(playerId, equipped.Ability, equipped.LevelIndex, equipped.Slot);
                _appliedStarLevels[starKey] = equipped.LevelIndex;
                continue;
            }

            if (_appliedStarLevels.TryGetValue(starKey, out var appliedStar) && appliedStar == equipped.LevelIndex)
            {
                continue;
            }

            acquired.ApplyLevel(equipped.LevelIndex);
            RememberLevel(playerId, abilityType);
            _appliedStarLevels[starKey] = equipped.LevelIndex;
        }
    }

    /// <summary>
    /// 장착에서 빠진 종 목록 무기 능력만 치운다. 점 발사는 그 칸만 지운다.
    /// </summary>
    private void RemoveUnequippedCatalogAbilities(int playerId, ItemManager itemManager)
    {
        var loadout = GetLoadout(playerId);
        _removedAbilityTypes.Clear();
        for (var i = loadout.Acquired.Count - 1; i >= 0; i--)
        {
            var ability = loadout.Acquired[i];
            if (ability == null)
            {
                continue;
            }

            if (ability is ISlotOriginAbility origin)
            {
                var data = ability.WeaponAbilityData;
                if (data != null && data.IsEvolution)
                {
                    if (!SlotFeedsEvolution(origin.OriginSlot, data))
                    {
                        RemoveAcquiredAt(playerId, i);
                    }

                    continue;
                }

                if (data == null || !itemManager.IsCatalogWeaponAbility(ability.WeaponAbilityType))
                {
                    continue;
                }

                if (!HasEquippedOrigin(origin.OriginSlot, ability.WeaponAbilityType))
                {
                    RemoveAcquiredAt(playerId, i);
                }

                continue;
            }

            if (!itemManager.IsCatalogWeaponAbility(ability.WeaponAbilityType))
            {
                continue;
            }

            if (HasEquippedAbility(ability.WeaponAbilityType))
            {
                continue;
            }

            if (!_removedAbilityTypes.Contains(ability.WeaponAbilityType))
            {
                _removedAbilityTypes.Add(ability.WeaponAbilityType);
            }
        }

        for (var i = 0; i < _removedAbilityTypes.Count; i++)
        {
            RemoveAbility(playerId, _removedAbilityTypes[i]);
        }
    }

    private bool HasEquippedAbility(WeaponAbilityType abilityType)
    {
        for (var i = 0; i < _equippedLevels.Count; i++)
        {
            if (_equippedLevels[i].Ability != null && _equippedLevels[i].Ability.WeaponAbilityType == abilityType)
            {
                return true;
            }
        }

        return false;
    }

    private void RemoveAbility(int playerId, WeaponAbilityType abilityType)
    {
        var loadout = GetLoadout(playerId);
        for (var i = loadout.Acquired.Count - 1; i >= 0; i--)
        {
            var ability = loadout.Acquired[i];
            if (ability == null || ability.WeaponAbilityType != abilityType)
            {
                continue;
            }

            var slot = ability is ISlotOriginAbility origin ? (int)origin.OriginSlot : EquippedAbilityLevel.UNSLOTTED;
            _appliedStarLevels.Remove(new StarLevelKey(abilityType, slot));
            ability.Clear();
            loadout.Acquired.RemoveAt(i);
        }

        loadout.Levels.Remove(abilityType);
    }

    private void HandleEquipmentChanged(EquipmentChanged changed)
    {
        if (!_stageRunning)
        {
            return;
        }

        ApplyEquippedAbilityLevels(changed.PlayerId);
    }

    private void GrantStartingWeaponAbility(int playerId)
    {
        var visual = ResolveStartingVisual(playerId);
        if (visual == null || visual.WeaponAbility == null || visual.WeaponAbility.Prefab == null)
        {
            Debug.LogWarning("시작 포켓몬에 공격 능력이 없습니다.");
            return;
        }

        var level = 0;
        if (Managers.Instance != null && Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            level = itemManager.GetEquippedAbilityLevel(playerId, visual.WeaponAbility);
        }

        var slot = EquippedAbilityLevel.UNSLOTTED;
        if (ItemManager.IsSlotOriginAbility(visual.WeaponAbility))
        {
            slot = (int)WeaponSlot.Hour3;
            if (Managers.Instance != null
                && Managers.Instance.TryGetManager<PlayerManager>(out var playerManager)
                && playerManager.TryGetPlayer(playerId, out var player)
                && player.Weapons.TryGetEquippedSlot(out var equippedSlot))
            {
                slot = (int)equippedSlot;
            }
        }

        AddWeaponAbility(playerId, visual.WeaponAbility, level, slot);
    }

    private MonsterVisualData ResolveStartingVisual(int playerId)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return null;
        }

        if (!playerManager.TryGetPlayer(playerId, out var player))
        {
            return null;
        }

        return player.Weapons.EquippedVisual;
    }

    private void PresentWeaponAbilityOffer()
    {
        if (_waitingForChoice)
        {
            return;
        }

        while (TryDequeueLevelUp(out var level))
        {
            PlayLevelUpSound();
            _currentOffers.Clear();
            _currentOffers.AddRange(SelectOffers(level));
            if (_currentOffers.Count == 0)
            {
                continue;
            }

            _waitingForChoice = true;
            PauseForWeaponAbilityChoice();
            var window = EnsureWindow();
            window.Open(_playerId, level, CompleteLevelUpOffer);
            return;
        }

        ResumeAfterWeaponAbilityChoice();
    }

    private void PauseForWeaponAbilityChoice()
    {
        Time.timeScale = 0f;
        if (Managers.Instance != null && Managers.Instance.CurrentState == GameState.Playing)
        {
            Managers.Instance.ChangeState(GameState.LevelUp);
        }
    }

    private void ResumeAfterWeaponAbilityChoice()
    {
        if (Managers.Instance == null || Managers.Instance.CurrentState != GameState.LevelUp)
        {
            Time.timeScale = 1f;
            return;
        }

        Time.timeScale = 1f;
        Managers.Instance.ChangeState(GameState.Playing);
    }

    private WeaponAbilitiesWindowBehavior EnsureWindow()
    {
        if (_abilitiesWindow != null)
        {
            return _abilitiesWindow;
        }

        _abilitiesWindow = GetComponent<WeaponAbilitiesWindowBehavior>();
        if (_abilitiesWindow == null)
        {
            _abilitiesWindow = gameObject.AddComponent<WeaponAbilitiesWindowBehavior>();
        }

        return _abilitiesWindow;
    }

    private List<WeaponAbilityData> SelectOffers(int level)
    {
        var abilities = GetAvailableAbilities(_playerId);
        var selected = new List<WeaponAbilityData>();
        var weighted = new List<WeightedWeaponAbility>();
        var firstLevels = level < FIRST_LEVELS;
        var activeCount = GetActiveAbilitiesCount(_playerId);
        var passiveCount = GetPassiveAbilitiesCount(_playerId);
        var moreActive = activeCount > passiveCount;
        var morePassive = passiveCount > activeCount;

        for (var i = 0; i < abilities.Count; i++)
        {
            var ability = abilities[i];
            var weight = 1f;
            if (IsWeaponAbilityAquired(_playerId, ability.WeaponAbilityType) && _abilitiesDatabase != null)
            {
                weight *= _abilitiesDatabase.AquiredWeaponAbilityWeightMultiplier;
            }

            if (ability.IsActiveAbility)
            {
                if (firstLevels && _abilitiesDatabase != null)
                {
                    weight *= _abilitiesDatabase.FirstLevelsActiveWeaponAbilityWeightMultiplier;
                }

                if (morePassive && _abilitiesDatabase != null)
                {
                    weight *= _abilitiesDatabase.LessAbilitiesOfTypeWeightMultiplier;
                }

                if (ability.IsEvolution && _abilitiesDatabase != null)
                {
                    weight *= _abilitiesDatabase.EvolutionWeaponAbilityWeightMultiplier;
                }
            }
            else if (_abilitiesDatabase != null)
            {
                if (IsRequiredForAquiredEvolution(_playerId, ability.WeaponAbilityType))
                {
                    weight *= _abilitiesDatabase.RequiredForEvolutionWeightMultiplier;
                }

                if (moreActive)
                {
                    weight *= _abilitiesDatabase.LessAbilitiesOfTypeWeightMultiplier;
                }
            }

            weighted.Add(new WeightedWeaponAbility(ability, weight));
        }

        while (abilities.Count > 0 && selected.Count < MAX_OFFERS && weighted.Count > 0)
        {
            var weightSum = 0f;
            for (var i = 0; i < weighted.Count; i++)
            {
                weightSum += weighted[i].Weight;
            }

            if (weightSum <= 0f)
            {
                var share = 1f / weighted.Count;
                for (var i = 0; i < weighted.Count; i++)
                {
                    weighted[i].Weight = share;
                }
            }
            else
            {
                for (var i = 0; i < weighted.Count; i++)
                {
                    weighted[i].Weight /= weightSum;
                }
            }

            var random = Random.value;
            var progress = 0f;
            WeaponAbilityData selectedWeaponAbility = null;
            for (var i = 0; i < weighted.Count; i++)
            {
                progress += weighted[i].Weight;
                if (random <= progress)
                {
                    selectedWeaponAbility = weighted[i].Data;
                    break;
                }
            }

            if (selectedWeaponAbility == null)
            {
                var index = Random.Range(0, abilities.Count);
                selectedWeaponAbility = abilities[index];
            }

            abilities.Remove(selectedWeaponAbility);
            for (var i = 0; i < weighted.Count; i++)
            {
                if (weighted[i].Data != selectedWeaponAbility)
                {
                    continue;
                }

                weighted.RemoveAt(i);
                break;
            }

            selected.Add(selectedWeaponAbility);
        }

        return selected;
    }

    private List<WeaponAbilityData> GetAvailableAbilities(int playerId)
    {
        var result = new List<WeaponAbilityData>();
        var activeCount = GetActiveAbilitiesCount(playerId);
        var passiveCount = GetPassiveAbilitiesCount(playerId);
        if (_abilitiesDatabase == null)
        {
            IncludeOwnedUpgrades(playerId, result, activeCount, passiveCount);
            return result;
        }
        for (var i = 0; i < _abilitiesDatabase.AbilitiesCount; i++)
        {
            var abilityData = _abilitiesDatabase.GetWeaponAbility(i);
            if (!CanOffer(playerId, abilityData, activeCount, passiveCount, false))
            {
                continue;
            }

            result.Add(abilityData);
        }

        IncludeOwnedUpgrades(playerId, result, activeCount, passiveCount);

        if (result.Count > 0)
        {
            return result;
        }

        for (var i = 0; i < _abilitiesDatabase.AbilitiesCount; i++)
        {
            var abilityData = _abilitiesDatabase.GetWeaponAbility(i);
            if (abilityData != null && abilityData.IsEndgameAbility && abilityData.Prefab != null)
            {
                result.Add(abilityData);
            }
        }

        return result;
    }

    private void IncludeOwnedUpgrades(int playerId, List<WeaponAbilityData> result, int activeCount, int passiveCount)
    {
        var acquired = GetLoadout(playerId).Acquired;
        for (var i = 0; i < acquired.Count; i++)
        {
            var ability = acquired[i];
            if (ability == null || ability.WeaponAbilityData == null || result.Contains(ability.WeaponAbilityData))
            {
                continue;
            }

            if (CanOffer(playerId, ability.WeaponAbilityData, activeCount, passiveCount, false))
            {
                result.Add(ability.WeaponAbilityData);
            }
        }
    }

    private bool CanOffer(int playerId, WeaponAbilityData abilityData, int activeCount, int passiveCount, bool allowEndgame)
    {
        if (abilityData == null || abilityData.Prefab == null)
        {
            return false;
        }

        if (abilityData.IsEndgameAbility)
        {
            return allowEndgame;
        }

        if (IsAbilityMaxed(playerId, abilityData))
        {
            return false;
        }

        if (GetLoadout(playerId).Removed.Contains(abilityData.WeaponAbilityType))
        {
            return false;
        }

        var acquired = IsWeaponAbilityAquired(playerId, abilityData.WeaponAbilityType);
        if (abilityData.IsEvolution && !acquired)
        {
            return MeetsEvolutionRequirements(playerId, abilityData);
        }

        if (abilityData.IsWeaponAbility && !acquired)
        {
            return false;
        }

        var activeCapacity = _abilitiesDatabase != null ? _abilitiesDatabase.ActiveAbilitiesCapacity : DEFAULT_SLOT_CAPACITY;
        var passiveCapacity = _abilitiesDatabase != null ? _abilitiesDatabase.PassiveAbilitiesCapacity : DEFAULT_SLOT_CAPACITY;
        if (abilityData.IsActiveAbility && activeCount >= activeCapacity && !acquired)
        {
            return false;
        }

        if (!abilityData.IsActiveAbility && passiveCount >= passiveCapacity && !acquired)
        {
            return false;
        }

        return true;
    }

    private bool MeetsEvolutionRequirements(int playerId, WeaponAbilityData abilityData)
    {
        if (abilityData.EvolutionRequirements == null)
        {
            return false;
        }

        for (var i = 0; i < abilityData.EvolutionRequirements.Count; i++)
        {
            var requirement = abilityData.EvolutionRequirements[i];
            if (requirement == null)
            {
                return false;
            }

            var acquired = IsWeaponAbilityAquired(playerId, requirement.WeaponAbilityType);
            var reached = GetWeaponAbilityLevel(playerId, requirement.WeaponAbilityType) >= requirement.RequiredWeaponAbilityLevel;
            if (!acquired || !reached)
            {
                return false;
            }
        }

        return abilityData.EvolutionRequirements.Count > 0;
    }

    private void RemoveEvolvedAbilities(int playerId, WeaponAbilityData abilityData)
    {
        if (!abilityData.IsEvolution || abilityData.EvolutionRequirements == null)
        {
            return;
        }

        var loadout = GetLoadout(playerId);
        for (var i = 0; i < abilityData.EvolutionRequirements.Count; i++)
        {
            var requirement = abilityData.EvolutionRequirements[i];
            if (requirement == null || !requirement.ShouldRemoveAfterEvolution)
            {
                continue;
            }

            if (GetAquiredWeaponAbility(playerId, requirement.WeaponAbilityType) == null)
            {
                continue;
            }

            RemoveAbility(playerId, requirement.WeaponAbilityType);
            if (!loadout.Removed.Contains(requirement.WeaponAbilityType))
            {
                loadout.Removed.Add(requirement.WeaponAbilityType);
            }
        }
    }

    private int GetActiveAbilitiesCount(int playerId)
    {
        var count = 0;
        var acquired = GetLoadout(playerId).Acquired;
        for (var i = 0; i < acquired.Count; i++)
        {
            var ability = acquired[i];
            if (ability == null || ability.WeaponAbilityData == null || !ability.WeaponAbilityData.IsActiveAbility)
            {
                continue;
            }

            if (WasTypeCounted(acquired, i, ability.WeaponAbilityType))
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private int GetPassiveAbilitiesCount(int playerId)
    {
        var count = 0;
        var acquired = GetLoadout(playerId).Acquired;
        for (var i = 0; i < acquired.Count; i++)
        {
            var ability = acquired[i];
            if (ability != null && ability.WeaponAbilityData != null && !ability.WeaponAbilityData.IsActiveAbility)
            {
                count++;
            }
        }

        return count;
    }

    private bool IsRequiredForAquiredEvolution(int playerId, WeaponAbilityType abilityType)
    {
        var acquired = GetLoadout(playerId).Acquired;
        for (var i = 0; i < acquired.Count; i++)
        {
            var ability = acquired[i];
            if (ability == null || ability.WeaponAbilityData == null || !ability.WeaponAbilityData.IsActiveAbility || ability.WeaponAbilityData.IsEvolution)
            {
                continue;
            }

            var requirements = ability.WeaponAbilityData.EvolutionRequirements;
            if (requirements == null)
            {
                continue;
            }

            for (var j = 0; j < requirements.Count; j++)
            {
                if (requirements[j] != null && requirements[j].WeaponAbilityType == abilityType)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void TryRestoreSlotEvolution(int playerId, EquippedAbilityLevel equipped)
    {
        if (equipped.Ability == null)
        {
            return;
        }

        var evolution = FindRemovingEvolution(equipped.Ability.WeaponAbilityType);
        if (evolution == null || !ItemManager.IsSlotOriginAbility(evolution))
        {
            return;
        }

        if (GetAcquiredAtSlot(playerId, evolution.WeaponAbilityType, equipped.Slot) != null)
        {
            return;
        }

        CreateAbility(playerId, evolution, ClampLevel(evolution, equipped.LevelIndex), equipped.Slot);
    }

    private WeaponAbilityData FindRemovingEvolution(WeaponAbilityType requiredType)
    {
        if (_abilitiesDatabase == null)
        {
            return null;
        }

        for (var i = 0; i < _abilitiesDatabase.AbilitiesCount; i++)
        {
            var ability = _abilitiesDatabase.GetWeaponAbility(i);
            if (ability == null || !ability.IsEvolution || ability.EvolutionRequirements == null)
            {
                continue;
            }

            for (var r = 0; r < ability.EvolutionRequirements.Count; r++)
            {
                var requirement = ability.EvolutionRequirements[r];
                if (requirement != null
                    && requirement.ShouldRemoveAfterEvolution
                    && requirement.WeaponAbilityType == requiredType)
                {
                    return ability;
                }
            }
        }

        return null;
    }

    private void AddSlotOriginEvolution(int playerId, WeaponAbilityData abilityData, int level)
    {
        CollectEvolutionSlots(playerId, abilityData);
        RemoveEvolvedAbilities(playerId, abilityData);
        if (_evolutionSlots.Count == 0)
        {
            CreateAbility(playerId, abilityData, level, EquippedAbilityLevel.UNSLOTTED);
            return;
        }

        for (var i = 0; i < _evolutionSlots.Count; i++)
        {
            CreateAbility(playerId, abilityData, ClampLevel(abilityData, _evolutionLevels[i]), _evolutionSlots[i]);
        }
    }

    private void CollectEvolutionSlots(int playerId, WeaponAbilityData abilityData)
    {
        _evolutionSlots.Clear();
        _evolutionLevels.Clear();
        if (abilityData.EvolutionRequirements == null)
        {
            return;
        }

        var acquired = GetLoadout(playerId).Acquired;
        for (var i = 0; i < abilityData.EvolutionRequirements.Count; i++)
        {
            var requirement = abilityData.EvolutionRequirements[i];
            if (requirement == null || !requirement.ShouldRemoveAfterEvolution)
            {
                continue;
            }

            for (var u = 0; u < acquired.Count; u++)
            {
                var ability = acquired[u];
                if (ability == null || ability.WeaponAbilityType != requirement.WeaponAbilityType)
                {
                    continue;
                }

                if (!(ability is ISlotOriginAbility origin))
                {
                    continue;
                }

                var slot = (int)origin.OriginSlot;
                if (!_evolutionSlots.Contains(slot))
                {
                    _evolutionSlots.Add(slot);
                    _evolutionLevels.Add(ability.LevelId);
                }
            }
        }
    }

    private static int ClampLevel(WeaponAbilityData abilityData, int level)
    {
        if (abilityData == null || abilityData.LevelsCount <= 0 || level < 0)
        {
            return 0;
        }

        if (level >= abilityData.LevelsCount)
        {
            return abilityData.LevelsCount - 1;
        }

        return level;
    }

    private void CreateAbility(int playerId, WeaponAbilityData abilityData, int level, int slot)
    {
        var instance = Instantiate(abilityData.Prefab);
        var ability = instance.GetComponent<IWeaponAbilityBehavior>();
        if (ability == null)
        {
            Debug.LogError("능력 프리팹에 IWeaponAbilityBehavior가 없습니다. " + abilityData.WeaponAbilityType);
            Destroy(instance);
            return;
        }

        if (slot != EquippedAbilityLevel.UNSLOTTED && ability is ISlotOriginAbility origin)
        {
            origin.BindSlot((WeaponSlot)slot);
        }

        ability.Init(playerId, abilityData, level);
        GetLoadout(playerId).Acquired.Add(ability);
        RememberLevel(playerId, abilityData.WeaponAbilityType);
    }

    private void RaiseAcquiredLevels(int playerId, WeaponAbilityData abilityData)
    {
        var acquired = GetLoadout(playerId).Acquired;
        for (var i = 0; i < acquired.Count; i++)
        {
            var ability = acquired[i];
            if (ability == null || ability.WeaponAbilityType != abilityData.WeaponAbilityType)
            {
                continue;
            }

            var next = ability.LevelId + 1;
            if (next >= abilityData.LevelsCount)
            {
                continue;
            }

            ability.ApplyLevel(next);
        }

        RememberLevel(playerId, abilityData.WeaponAbilityType);
    }

    private void RememberLevel(int playerId, WeaponAbilityType abilityType)
    {
        var loadout = GetLoadout(playerId);
        var max = MISSING_LEVEL;
        for (var i = 0; i < loadout.Acquired.Count; i++)
        {
            var ability = loadout.Acquired[i];
            if (ability == null || ability.WeaponAbilityType != abilityType)
            {
                continue;
            }

            if (ability.LevelId > max)
            {
                max = ability.LevelId;
            }
        }

        if (max == MISSING_LEVEL)
        {
            loadout.Levels.Remove(abilityType);
            return;
        }

        loadout.Levels[abilityType] = max;
    }

    private bool IsAbilityMaxed(int playerId, WeaponAbilityData abilityData)
    {
        if (abilityData == null || abilityData.LevelsCount <= 0)
        {
            return false;
        }

        var acquired = GetLoadout(playerId).Acquired;
        var found = false;
        for (var i = 0; i < acquired.Count; i++)
        {
            var ability = acquired[i];
            if (ability == null || ability.WeaponAbilityType != abilityData.WeaponAbilityType)
            {
                continue;
            }

            found = true;
            if (ability.LevelId < abilityData.LevelsCount - 1)
            {
                return false;
            }
        }

        return found;
    }

    private void RemoveAcquiredAt(int playerId, int index)
    {
        var loadout = GetLoadout(playerId);
        if (index < 0 || index >= loadout.Acquired.Count)
        {
            return;
        }

        var ability = loadout.Acquired[index];
        if (ability == null)
        {
            loadout.Acquired.RemoveAt(index);
            return;
        }

        var type = ability.WeaponAbilityType;
        var slot = ability is ISlotOriginAbility origin ? (int)origin.OriginSlot : EquippedAbilityLevel.UNSLOTTED;
        ability.Clear();
        loadout.Acquired.RemoveAt(index);
        _appliedStarLevels.Remove(new StarLevelKey(type, slot));
        RememberLevel(playerId, type);
    }

    private IWeaponAbilityBehavior GetAcquiredAtSlot(int playerId, WeaponAbilityType abilityType, int slot)
    {
        var acquired = GetLoadout(playerId).Acquired;
        for (var i = 0; i < acquired.Count; i++)
        {
            var ability = acquired[i];
            if (ability == null || ability.WeaponAbilityType != abilityType)
            {
                continue;
            }

            if (ability is ISlotOriginAbility origin && (int)origin.OriginSlot == slot)
            {
                return ability;
            }
        }

        return null;
    }

    private IWeaponAbilityBehavior GetUnslottedAbility(int playerId, WeaponAbilityType abilityType)
    {
        var acquired = GetLoadout(playerId).Acquired;
        for (var i = 0; i < acquired.Count; i++)
        {
            var ability = acquired[i];
            if (ability == null || ability.WeaponAbilityType != abilityType)
            {
                continue;
            }

            if (ability is ISlotOriginAbility)
            {
                continue;
            }

            return ability;
        }

        return null;
    }

    private bool HasEquippedOrigin(WeaponSlot slot, WeaponAbilityType abilityType)
    {
        var slotIndex = (int)slot;
        for (var i = 0; i < _equippedLevels.Count; i++)
        {
            var equipped = _equippedLevels[i];
            if (equipped.Slot == slotIndex
                && equipped.Ability != null
                && equipped.Ability.WeaponAbilityType == abilityType)
            {
                return true;
            }
        }

        return false;
    }

    private bool SlotFeedsEvolution(WeaponSlot slot, WeaponAbilityData evolution)
    {
        if (evolution == null || evolution.EvolutionRequirements == null)
        {
            return false;
        }

        var slotIndex = (int)slot;
        for (var i = 0; i < _equippedLevels.Count; i++)
        {
            var equipped = _equippedLevels[i];
            if (equipped.Slot != slotIndex || equipped.Ability == null)
            {
                continue;
            }

            for (var r = 0; r < evolution.EvolutionRequirements.Count; r++)
            {
                var requirement = evolution.EvolutionRequirements[r];
                if (requirement != null && requirement.WeaponAbilityType == equipped.Ability.WeaponAbilityType)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool WasTypeCounted(List<IWeaponAbilityBehavior> acquired, int index, WeaponAbilityType abilityType)
    {
        for (var i = 0; i < index; i++)
        {
            var ability = acquired[i];
            if (ability != null && ability.WeaponAbilityType == abilityType)
            {
                return true;
            }
        }

        return false;
    }

    private IWeaponAbilityBehavior GetAquiredWeaponAbility(int playerId, WeaponAbilityType abilityType)
    {
        var acquired = GetLoadout(playerId).Acquired;
        for (var i = 0; i < acquired.Count; i++)
        {
            var ability = acquired[i];
            if (ability != null && ability.WeaponAbilityType == abilityType)
            {
                return ability;
            }
        }

        return null;
    }

    private void ClearAbilities(int playerId)
    {
        if (!_loadouts.TryGetValue(playerId, out var loadout))
        {
            return;
        }

        for (var i = 0; i < loadout.Acquired.Count; i++)
        {
            var ability = loadout.Acquired[i];
            if (ability != null)
            {
                ability.Clear();
            }
        }

        loadout.Acquired.Clear();
        loadout.Removed.Clear();
        loadout.Levels.Clear();
    }

    private void ClearAbilities()
    {
        foreach (var pair in _loadouts)
        {
            var loadout = pair.Value;
            for (var i = 0; i < loadout.Acquired.Count; i++)
            {
                if (loadout.Acquired[i] != null)
                {
                    loadout.Acquired[i].Clear();
                }
            }
        }

        _loadouts.Clear();
    }

    private WeaponAbilityLoadout GetLoadout(int playerId)
    {
        if (!_loadouts.TryGetValue(playerId, out var loadout))
        {
            loadout = new WeaponAbilityLoadout();
            _loadouts.Add(playerId, loadout);
        }

        return loadout;
    }

    #endregion

    private readonly struct StarLevelKey : System.IEquatable<StarLevelKey>
    {
        public StarLevelKey(WeaponAbilityType type, int slot)
        {
            Type = type;
            Slot = slot;
        }

        public WeaponAbilityType Type { get; }

        public int Slot { get; }

        public bool Equals(StarLevelKey other)
        {
            return Type == other.Type && Slot == other.Slot;
        }

        public override bool Equals(object obj)
        {
            return obj is StarLevelKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ((int)Type * 397) ^ Slot;
        }
    }

    private sealed class WeaponAbilityLoadout
    {
        public readonly List<IWeaponAbilityBehavior> Acquired = new List<IWeaponAbilityBehavior>();
        public readonly List<WeaponAbilityType> Removed = new List<WeaponAbilityType>();
        public readonly Dictionary<WeaponAbilityType, int> Levels = new Dictionary<WeaponAbilityType, int>();
    }

    private sealed class WeightedWeaponAbility
    {
        public WeightedWeaponAbility(WeaponAbilityData data, float weight)
        {
            Data = data;
            Weight = weight;
        }

        public WeaponAbilityData Data { get; }

        public float Weight { get; set; }
    }
}
