using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 획득한 능력을 들고, 레벨업 카드를 고르게 한다.
/// </summary>
public class AbilityManager : BaseManager
{
    private const WeaponSlot STARTING_SLOT = WeaponSlot.Hour3;
    private const float FALLBACK_SIZE = 0.65f;
    private const int FALLBACK_SORTING_ORDER = 11;
    private const int MAX_OFFERS = 3;
    private const int FIRST_LEVELS = 10;
    private const int MISSING_LEVEL = -1;
    private const int DEFAULT_SLOT_CAPACITY = 5;

    [Header("Weapon")]
    [SerializeField] private Weapon _startingWeaponPrefab;
    [Tooltip("시작 무기가 재생할 그림. Monster0001 같은 에셋을 넣는다.")]
    [SerializeField] private MonsterVisualData _startingWeaponVisual;

    [Header("Abilities")]
    [SerializeField] private AbilitiesDatabase _abilitiesDatabase;
    [SerializeField] private AbilitiesWindowBehavior _abilitiesWindow;

    [Header("Level Up")]
    [SerializeField] private AudioClip _levelUpFanfare;

    private readonly Queue<int> _levelUpQueue = new Queue<int>();
    private readonly Weapon[] _weapons = new Weapon[WeaponSlots.MAX_COUNT];
    private readonly Dictionary<int, AbilityLoadout> _loadouts = new Dictionary<int, AbilityLoadout>();
    private readonly List<AbilityData> _currentOffers = new List<AbilityData>();

    private int _playerId;
    private bool _waitingForChoice;

    public int PendingLevelUpLevel { get; private set; }

    public IReadOnlyList<AbilityData> CurrentOffers => _currentOffers;

    #region Unity Methods

    /// <summary>
    /// 능력 매니저 초기화
    /// </summary>
    public override UniTask InitializeAsync()
    {
        return base.InitializeAsync();
    }

    private void LateUpdate()
    {
        TickWeapons();
    }

    public override void Cleanup()
    {
        ClearLevelUpQueue();
        ClearAbilities();
        ClearWeapon();
        base.Cleanup();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 시계 칸에 포켓몬을 장착하고, 고른 포켓몬의 공격 능력을 레벨 0으로 넣는다.
    /// </summary>
    public void BeginStage(int playerId)
    {
        _playerId = playerId;
        ClearLevelUpQueue();
        ClearAbilities(playerId);
        ClearWeapon();
        var playerManager = GetManager<PlayerManager>();
        if (playerManager == null || playerManager.PlayerTransform == null)
        {
            Debug.LogError("플레이어가 없어 능력을 붙이지 않습니다.");
            return;
        }

        PlaceWeapon(STARTING_SLOT, _startingWeaponPrefab, ResolveStartingVisual());
        GrantStartingAbility(playerId);
    }

    /// <summary>
    /// 판이 끝나면 능력과 무기를 치운다.
    /// </summary>
    public void EndStage()
    {
        ClearLevelUpQueue();
        ClearAbilities(_playerId);
        ClearWeapon();
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
        PresentAbilityOffer();
    }

    /// <summary>
    /// 능력 프리팹을 만들고 레벨을 적용한다. 진화면 요구 능력을 치운다.
    /// </summary>
    public void AddAbility(int playerId, AbilityData abilityData, int level = 0)
    {
        if (playerId != _playerId || abilityData == null || abilityData.Prefab == null)
        {
            return;
        }

        var instance = Instantiate(abilityData.Prefab);
        var ability = instance.GetComponent<IAbilityBehavior>();
        if (ability == null)
        {
            Debug.LogError("능력 프리팹에 IAbilityBehavior가 없습니다. " + abilityData.AbilityType);
            Destroy(instance);
            return;
        }

        ability.Init(playerId, abilityData, level);
        RemoveEvolvedAbilities(playerId, abilityData);
        var loadout = GetLoadout(playerId);
        loadout.Levels[abilityData.AbilityType] = level;
        loadout.Acquired.Add(ability);
    }

    /// <summary>
    /// 고른 카드를 적용한다. 엔드게임은 레벨을 올리지 않는다.
    /// </summary>
    public void ApplyOffer(int playerId, AbilityData abilityData)
    {
        if (playerId != _playerId || abilityData == null)
        {
            return;
        }

        if (abilityData.IsEndgameAbility || !IsAbilityAquired(playerId, abilityData.AbilityType))
        {
            AddAbility(playerId, abilityData, 0);
            return;
        }

        var nextLevel = GetAbilityLevel(playerId, abilityData.AbilityType) + 1;
        if (nextLevel >= abilityData.LevelsCount)
        {
            return;
        }

        var acquired = GetAquiredAbility(playerId, abilityData.AbilityType);
        if (acquired == null)
        {
            return;
        }

        acquired.ApplyLevel(nextLevel);
        GetLoadout(playerId).Levels[abilityData.AbilityType] = nextLevel;
    }

    /// <summary>
    /// 가진 능력의 저장 레벨. 없으면 -1.
    /// </summary>
    public int GetAbilityLevel(int playerId, AbilityType abilityType)
    {
        if (!_loadouts.TryGetValue(playerId, out var loadout))
        {
            return MISSING_LEVEL;
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
    public bool IsAbilityAquired(int playerId, AbilityType abilityType)
    {
        return GetAquiredAbility(playerId, abilityType) != null;
    }

    /// <summary>
    /// 진화 하나당 액티브 하나와 패시브 하나를 전제로, 짝이 되는 요구 타입을 찾는다.
    /// </summary>
    public bool HasEvolution(AbilityType abilityType, out AbilityType otherRequiredAbilityType)
    {
        otherRequiredAbilityType = abilityType;
        if (_abilitiesDatabase == null)
        {
            return false;
        }

        for (var i = 0; i < _abilitiesDatabase.AbilitiesCount; i++)
        {
            var ability = _abilitiesDatabase.GetAbility(i);
            if (ability == null || !ability.IsEvolution || ability.EvolutionRequirements == null)
            {
                continue;
            }

            for (var j = 0; j < ability.EvolutionRequirements.Count; j++)
            {
                var requirement = ability.EvolutionRequirements[j];
                if (requirement == null || requirement.AbilityType != abilityType)
                {
                    continue;
                }

                for (var k = 0; k < ability.EvolutionRequirements.Count; k++)
                {
                    if (k == j || ability.EvolutionRequirements[k] == null)
                    {
                        continue;
                    }

                    otherRequiredAbilityType = ability.EvolutionRequirements[k].AbilityType;
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Weapon Slot

    /// <summary>
    /// 비어 있는 시계 칸에 무기를 붙인다. 여섯 칸이 차 있으면 false.
    /// </summary>
    public bool TryAddWeapon(Weapon prefab)
    {
        if (prefab == null || _playerId == 0)
        {
            return false;
        }

        for (var i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] != null)
            {
                continue;
            }

            PlaceWeapon((WeaponSlot)i, prefab, null);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 무기가 씬과 함께 사라지면 참조를 비운다.
    /// </summary>
    public void NotifyWeaponDestroyed(Weapon weapon)
    {
        for (var i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] != weapon)
            {
                continue;
            }

            _weapons[i] = null;
            return;
        }
    }

    private void TickWeapons()
    {
        if (Managers.Instance == null || !Managers.Instance.IsSimulationRunning)
        {
            return;
        }

        for (var i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] == null)
            {
                continue;
            }

            _weapons[i].Tick(Time.deltaTime);
        }
    }

    private void PlaceWeapon(WeaponSlot slot, Weapon prefab, MonsterVisualData visual)
    {
        var index = (int)slot;
        if (index < 0 || index >= _weapons.Length || _weapons[index] != null)
        {
            return;
        }

        var weapon = CreateWeapon(prefab);
        if (weapon == null)
        {
            return;
        }

        _weapons[index] = weapon;
        weapon.Bind(this);
        weapon.ApplyVisual(visual);
        weapon.Initialize(_playerId, slot);
        weapon.Tick(0f);
    }

    private void ClearWeapon()
    {
        for (var i = 0; i < _weapons.Length; i++)
        {
            var weapon = _weapons[i];
            _weapons[i] = null;
            if (weapon != null)
            {
                Destroy(weapon.gameObject);
            }
        }
    }

    #endregion

    #region Weapon Visual

    /// <summary>
    /// 그 플레이어의 시계 칸 포켓몬이 발사 방향 걷기를 한 바퀴 재생하게 한다.
    /// </summary>
    public void FaceShot(int playerId, Vector2 direction)
    {
        for (var i = 0; i < _weapons.Length; i++)
        {
            var weapon = _weapons[i];
            if (weapon == null || weapon.PlayerId != playerId)
            {
                continue;
            }

            weapon.FaceShot(direction);
        }
    }

    private MonsterVisualData ResolveStartingVisual()
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<AccountManager>(out var accountManager))
        {
            var runMonster = accountManager.ResolveRunMonster();
            if (runMonster != null)
            {
                return runMonster;
            }

            var starter = accountManager.ResolveStarterVisual();
            if (starter != null)
            {
                return starter;
            }
        }

        return _startingWeaponVisual;
    }

    private Weapon CreateWeapon(Weapon prefab)
    {
        if (prefab != null)
        {
            var weapon = Instantiate(prefab);
            weapon.gameObject.name = "Weapon";
            return weapon;
        }

        Debug.LogWarning("시작 무기 프리팹이 없어 자리표시 무기를 만듭니다.");
        var actorObject = new GameObject("Weapon");
        actorObject.transform.localScale = new Vector3(FALLBACK_SIZE, FALLBACK_SIZE, 1f);
        var renderer = actorObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = FALLBACK_SORTING_ORDER;
        return actorObject.AddComponent<StraightWeapon>();
    }

    #endregion

    #region Weapon Upgrade

    /// <summary>
    /// 고른 시계 칸 무기에, 고른 수치만 한 칸 올린다.
    /// </summary>
    public bool SelectWeaponUpgrade(WeaponSlot slot, WeaponStatKind stat)
    {
        var index = (int)slot;
        if (index < 0 || index >= _weapons.Length || _weapons[index] == null)
        {
            return false;
        }

        return _weapons[index].TryApplyUpgrade(stat);
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
            ResumeAfterAbilityChoice();
            return;
        }

        PresentAbilityOffer();
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

    private void GrantStartingAbility(int playerId)
    {
        var visual = ResolveStartingVisual();
        if (visual == null || visual.Ability == null || visual.Ability.Prefab == null)
        {
            Debug.LogWarning("시작 포켓몬에 공격 능력이 없습니다.");
            return;
        }

        AddAbility(playerId, visual.Ability, 0);
    }

    private void PresentAbilityOffer()
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
            PauseForAbilityChoice();
            var window = EnsureWindow();
            window.Open(_playerId, level, CompleteLevelUpOffer);
            return;
        }

        ResumeAfterAbilityChoice();
    }

    private void PauseForAbilityChoice()
    {
        Time.timeScale = 0f;
        if (Managers.Instance != null && Managers.Instance.CurrentState == GameState.Playing)
        {
            Managers.Instance.ChangeState(GameState.LevelUp);
        }
    }

    private void ResumeAfterAbilityChoice()
    {
        if (Managers.Instance == null || Managers.Instance.CurrentState != GameState.LevelUp)
        {
            Time.timeScale = 1f;
            return;
        }

        Time.timeScale = 1f;
        Managers.Instance.ChangeState(GameState.Playing);
    }

    private AbilitiesWindowBehavior EnsureWindow()
    {
        if (_abilitiesWindow != null)
        {
            return _abilitiesWindow;
        }

        _abilitiesWindow = GetComponent<AbilitiesWindowBehavior>();
        if (_abilitiesWindow == null)
        {
            _abilitiesWindow = gameObject.AddComponent<AbilitiesWindowBehavior>();
        }

        return _abilitiesWindow;
    }

    private List<AbilityData> SelectOffers(int level)
    {
        var abilities = GetAvailableAbilities(_playerId);
        var selected = new List<AbilityData>();
        var weighted = new List<WeightedAbility>();
        var firstLevels = level < FIRST_LEVELS;
        var activeCount = GetActiveAbilitiesCount(_playerId);
        var passiveCount = GetPassiveAbilitiesCount(_playerId);
        var moreActive = activeCount > passiveCount;
        var morePassive = passiveCount > activeCount;

        for (var i = 0; i < abilities.Count; i++)
        {
            var ability = abilities[i];
            var weight = 1f;
            if (IsAbilityAquired(_playerId, ability.AbilityType) && _abilitiesDatabase != null)
            {
                weight *= _abilitiesDatabase.AquiredAbilityWeightMultiplier;
            }

            if (ability.IsActiveAbility)
            {
                if (firstLevels && _abilitiesDatabase != null)
                {
                    weight *= _abilitiesDatabase.FirstLevelsActiveAbilityWeightMultiplier;
                }

                if (morePassive && _abilitiesDatabase != null)
                {
                    weight *= _abilitiesDatabase.LessAbilitiesOfTypeWeightMultiplier;
                }

                if (ability.IsEvolution && _abilitiesDatabase != null)
                {
                    weight *= _abilitiesDatabase.EvolutionAbilityWeightMultiplier;
                }
            }
            else if (_abilitiesDatabase != null)
            {
                if (IsRequiredForAquiredEvolution(_playerId, ability.AbilityType))
                {
                    weight *= _abilitiesDatabase.RequiredForEvolutionWeightMultiplier;
                }

                if (moreActive)
                {
                    weight *= _abilitiesDatabase.LessAbilitiesOfTypeWeightMultiplier;
                }
            }

            weighted.Add(new WeightedAbility(ability, weight));
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
            AbilityData selectedAbility = null;
            for (var i = 0; i < weighted.Count; i++)
            {
                progress += weighted[i].Weight;
                if (random <= progress)
                {
                    selectedAbility = weighted[i].Data;
                    break;
                }
            }

            if (selectedAbility == null)
            {
                var index = Random.Range(0, abilities.Count);
                selectedAbility = abilities[index];
            }

            abilities.Remove(selectedAbility);
            for (var i = 0; i < weighted.Count; i++)
            {
                if (weighted[i].Data != selectedAbility)
                {
                    continue;
                }

                weighted.RemoveAt(i);
                break;
            }

            selected.Add(selectedAbility);
        }

        return selected;
    }

    private List<AbilityData> GetAvailableAbilities(int playerId)
    {
        var result = new List<AbilityData>();
        var activeCount = GetActiveAbilitiesCount(playerId);
        var passiveCount = GetPassiveAbilitiesCount(playerId);
        if (_abilitiesDatabase == null)
        {
            IncludeOwnedUpgrades(playerId, result, activeCount, passiveCount);
            return result;
        }
        for (var i = 0; i < _abilitiesDatabase.AbilitiesCount; i++)
        {
            var abilityData = _abilitiesDatabase.GetAbility(i);
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
            var abilityData = _abilitiesDatabase.GetAbility(i);
            if (abilityData != null && abilityData.IsEndgameAbility && abilityData.Prefab != null)
            {
                result.Add(abilityData);
            }
        }

        return result;
    }

    private void IncludeOwnedUpgrades(int playerId, List<AbilityData> result, int activeCount, int passiveCount)
    {
        var acquired = GetLoadout(playerId).Acquired;
        for (var i = 0; i < acquired.Count; i++)
        {
            var ability = acquired[i];
            if (ability == null || ability.AbilityData == null || result.Contains(ability.AbilityData))
            {
                continue;
            }

            if (CanOffer(playerId, ability.AbilityData, activeCount, passiveCount, false))
            {
                result.Add(ability.AbilityData);
            }
        }
    }

    private bool CanOffer(int playerId, AbilityData abilityData, int activeCount, int passiveCount, bool allowEndgame)
    {
        if (abilityData == null || abilityData.Prefab == null)
        {
            return false;
        }

        if (abilityData.IsEndgameAbility)
        {
            return allowEndgame;
        }

        if (GetAbilityLevel(playerId, abilityData.AbilityType) >= abilityData.LevelsCount - 1)
        {
            return false;
        }

        if (GetLoadout(playerId).Removed.Contains(abilityData.AbilityType))
        {
            return false;
        }

        if (abilityData.IsEvolution)
        {
            return MeetsEvolutionRequirements(playerId, abilityData);
        }

        var acquired = IsAbilityAquired(playerId, abilityData.AbilityType);
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

    private bool MeetsEvolutionRequirements(int playerId, AbilityData abilityData)
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

            var acquired = IsAbilityAquired(playerId, requirement.AbilityType);
            var reached = GetAbilityLevel(playerId, requirement.AbilityType) >= requirement.RequiredAbilityLevel;
            if (!acquired || !reached)
            {
                return false;
            }
        }

        return abilityData.EvolutionRequirements.Count > 0;
    }

    private void RemoveEvolvedAbilities(int playerId, AbilityData abilityData)
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

            var required = GetAquiredAbility(playerId, requirement.AbilityType);
            if (required == null)
            {
                continue;
            }

            required.Clear();
            loadout.Acquired.Remove(required);
            loadout.Levels.Remove(requirement.AbilityType);
            loadout.Removed.Add(requirement.AbilityType);
        }
    }

    private int GetActiveAbilitiesCount(int playerId)
    {
        var count = 0;
        var acquired = GetLoadout(playerId).Acquired;
        for (var i = 0; i < acquired.Count; i++)
        {
            var ability = acquired[i];
            if (ability != null && ability.AbilityData != null && ability.AbilityData.IsActiveAbility)
            {
                count++;
            }
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
            if (ability != null && ability.AbilityData != null && !ability.AbilityData.IsActiveAbility)
            {
                count++;
            }
        }

        return count;
    }

    private bool IsRequiredForAquiredEvolution(int playerId, AbilityType abilityType)
    {
        var acquired = GetLoadout(playerId).Acquired;
        for (var i = 0; i < acquired.Count; i++)
        {
            var ability = acquired[i];
            if (ability == null || ability.AbilityData == null || !ability.AbilityData.IsActiveAbility || ability.AbilityData.IsEvolution)
            {
                continue;
            }

            var requirements = ability.AbilityData.EvolutionRequirements;
            if (requirements == null)
            {
                continue;
            }

            for (var j = 0; j < requirements.Count; j++)
            {
                if (requirements[j] != null && requirements[j].AbilityType == abilityType)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private IAbilityBehavior GetAquiredAbility(int playerId, AbilityType abilityType)
    {
        var acquired = GetLoadout(playerId).Acquired;
        for (var i = 0; i < acquired.Count; i++)
        {
            var ability = acquired[i];
            if (ability != null && ability.AbilityType == abilityType)
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

    private AbilityLoadout GetLoadout(int playerId)
    {
        if (!_loadouts.TryGetValue(playerId, out var loadout))
        {
            loadout = new AbilityLoadout();
            _loadouts.Add(playerId, loadout);
        }

        return loadout;
    }

    #endregion

    private sealed class AbilityLoadout
    {
        public readonly List<IAbilityBehavior> Acquired = new List<IAbilityBehavior>();
        public readonly List<AbilityType> Removed = new List<AbilityType>();
        public readonly Dictionary<AbilityType, int> Levels = new Dictionary<AbilityType, int>();
    }

    private sealed class WeightedAbility
    {
        public WeightedAbility(AbilityData data, float weight)
        {
            Data = data;
            Weight = weight;
        }

        public AbilityData Data { get; }

        public float Weight { get; set; }
    }
}
