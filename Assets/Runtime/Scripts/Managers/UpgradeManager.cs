using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;


public enum UpgradeTarget
{
    Player,
    Weapon,
    Companion
}

public enum UpgradeEffectType
{
    MoveSpeed,
    ReceivedDamage,
    Experience,

    // 이후 추가
    Damage,
    Health,
    ProjectileCount,
    ProjectileSpeed,
    ProjectileSize
}


#region data classes
/// <summary>
/// 한 판의 증강 Level, 선택 weight, 효과 배율을 보관하는 프로토타입 Manager.
/// input: Numpad 1/2/3 => 현재 선택지 선택 => Level 증가 => 배율 재계산 => 다음 선택지 출력.
/// output: PlayerManager가 이동/피해/경험치를 전달할 때 읽는 세 배율과 Console 관찰 로그.
/// </summary>
[DisallowMultipleComponent]
public sealed class UpgradeManager : BaseManager
{
    private const int CHOICE_COUNT = 3;

    /// <summary>
    /// 증강 하나의 정의. 효과는 비율이며 0.2는 +20%, -0.2는 -20%를 의미한다.
    /// 세 효과를 한 정의에 함께 넣을 수 있고, 보유 Level은 정의와 분리해 Manager가 보관한다.
    /// </summary>
    [Serializable]
    public sealed class UpgradeDefinition
    {
        public string Id;
        public string Name;
        [Min(1)] public int MaxLevel;
        [Min(0f)] public float Weight;
        public List<UpgradeEffectData> Effects;

        public UpgradeDefinition(string id, string name, int maxLevel, float weight,
            List<UpgradeEffectData> effects)
        {
            Id = id;
            Name = name;
            MaxLevel = maxLevel;
            Weight = weight;
            Effects = effects;
        }
    }
    [Serializable]
    public sealed class UpgradeEffectData
    {
        // 효과 적용 대상
        // 당장은 Player만 사용하고, 이후 Weapon 등을 추가한다.
        public UpgradeTarget Target;

        // 어떤 효과인지 구분
        // MoveSpeed, ReceivedDamage, Experience 등
        public UpgradeEffectType Type;

        // Level 1당 적용되는 값
        // 0.2 = +20%, -0.2 = -20%
        public float Value;

        public UpgradeEffectData(UpgradeTarget target, UpgradeEffectType type, float value)
        {
            Target = target;
            Type = type;
            Value = value;
        }
    }
    [Header("Upgrade Data")]
    [SerializeField] private TextAsset _upgradeJson;

    private UpgradeDefinition[] _definitions;

    /// <summary>
    /// JSON 데이터를 읽어 Runtime에서 사용할 UpgradeDefinition 배열로 변환한다.
    /// JSON의 Target / Type 문자열은 enum으로 검증 후 변환한다.
    /// </summary>
    private bool LoadDefinitions()
    {
        if (_upgradeJson == null)
        {
            Debug.LogError("[Upgrade] JSON TextAsset이 연결되지 않았습니다.", this);
            return false;
        }

        var root = JsonUtility.FromJson<UpgradeJsonRoot>(_upgradeJson.text);

        if (root == null || root.upgrades == null)
        {
            Debug.LogError("[Upgrade] JSON 데이터를 읽지 못했습니다.", this);
            return false;
        }

        var definitions = new List<UpgradeDefinition>();

        foreach (var jsonUpgrade in root.upgrades)
        {
            if (jsonUpgrade == null)
            {
                continue;
            }

            var effects = new List<UpgradeEffectData>();

            if (jsonUpgrade.effects != null)
            {
                foreach (var jsonEffect in jsonUpgrade.effects)
                {
                    if (jsonEffect == null)
                    {
                        continue;
                    }

                    // JSON의 "Player" 등을 Runtime enum으로 변환
                    if (!Enum.TryParse(
                            jsonEffect.target,
                            true,
                            out UpgradeTarget target))
                    {
                        Debug.LogError(
                            $"[Upgrade] 알 수 없는 Target: {jsonEffect.target}",
                            this
                        );

                        return false;
                    }

                    // JSON의 "Experience" 등을 Runtime enum으로 변환
                    if (!Enum.TryParse(
                            jsonEffect.type,
                            true,
                            out UpgradeEffectType type))
                    {
                        Debug.LogError(
                            $"[Upgrade] 알 수 없는 Effect Type: {jsonEffect.type}",
                            this
                        );

                        return false;
                    }

                    effects.Add(
                        new UpgradeEffectData(
                            target,
                            type,
                            jsonEffect.value
                        )
                    );
                }
            }

            definitions.Add(
                new UpgradeDefinition(
                    jsonUpgrade.id,
                    jsonUpgrade.name,
                    jsonUpgrade.maxLevel,
                    jsonUpgrade.weight,
                    effects
                )
            );
        }

        _definitions = definitions.ToArray();

        return true;
    }

    /// <summary>
    /// upgrade_master.json의 최상위 데이터.
    /// JsonUtility는 최상위 배열을 직접 읽지 않으므로 wrapper가 필요하다.
    /// </summary>
    [Serializable]
    private sealed class UpgradeJsonRoot
    {
        public List<UpgradeJsonData> upgrades;
    }

    /// <summary>
    /// JSON에서 읽어오는 Upgrade 원본 데이터.
    /// Runtime에서 사용하는 UpgradeDefinition과 분리한다.
    /// </summary>
    [Serializable]
    private sealed class UpgradeJsonData
    {
        public string id;
        public string name;
        public int maxLevel;
        public float weight;
        public List<UpgradeEffectJsonData> effects;
    }

    /// <summary>
    /// JSON에서는 Target/Type을 사람이 읽기 쉬운 문자열로 저장한다.
    /// 로딩할 때 Runtime enum으로 변환한다.
    /// </summary>
    [Serializable]
    private sealed class UpgradeEffectJsonData
    {
        public string target;
        public string type;
        public float value;
    }
    #endregion

    [Header("프로토타입")]
    [SerializeField] private bool _logAppliedValues = true;

    private readonly Dictionary<string, int> _levels = new Dictionary<string, int>();
    private readonly List<UpgradeDefinition> _choices = new List<UpgradeDefinition>(CHOICE_COUNT);
    private readonly List<UpgradeDefinition> _candidates = new List<UpgradeDefinition>();
    private PlayerManager _playerManager;
    private bool _active;
    private int _offerNumber;

    public float MoveSpeedMultiplier { get; private set; } = 1f;
    public float ReceivedDamageMultiplier { get; private set; } = 1f;
    public float ExperienceMultiplier { get; private set; } = 1f;
    public IReadOnlyList<UpgradeDefinition> CurrentChoices => _choices;

    private void Update()
    {
        if (!CanSelect())
        {
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // 위쪽 숫자 키가 아닌 Numpad만 읽는다. 한 프레임에 여러 키를 눌러도 한 번만 선택한다.
        // 키 번호는 증강 종류가 아니라, 방금 Console에 출력된 선택지 순서다.
        if (keyboard.numpad1Key.wasPressedThisFrame) TrySelectChoice(0);
        else if (keyboard.numpad2Key.wasPressedThisFrame) TrySelectChoice(1);
        else if (keyboard.numpad3Key.wasPressedThisFrame) TrySelectChoice(2);
    }

    /// <summary>
    /// Stage 시작 시 Upgrade Runtime 상태를 초기화하고,
    /// 등록된 UpgradeDefinition / Effect 데이터가 정상인지 검증한다.
    /// </summary>
    public void BeginStage(PlayerManager playerManager)
    {
        EndStage();
        if (!LoadDefinitions())
        {
            return;
        }
        if (playerManager == null || _definitions == null)
        {
            Debug.LogError("[증강] PlayerManager 또는 증강 정의가 없습니다.", this);
            return;
        }

        foreach (var definition in _definitions)
        {
            // Upgrade 자체의 기본 데이터 검증
            if (definition == null ||
                string.IsNullOrWhiteSpace(definition.Id) ||
                _levels.ContainsKey(definition.Id) ||
                definition.MaxLevel < 1 ||
                !float.IsFinite(definition.Weight) ||
                definition.Weight < 0f ||
                definition.Effects == null)
            {
                Debug.LogError(
                    "[증강] Id 중복/누락, MaxLevel, Weight 또는 Effects 설정을 확인하세요.",
                    this
                );

                EndStage();
                return;
            }

            // Upgrade 안에 들어있는 Effect 데이터 검증
            foreach (var effect in definition.Effects)
            {
                if (effect == null ||
                    !float.IsFinite(effect.Value))
                {
                    Debug.LogError(
                        $"[증강] {definition.Id}의 Effect 설정을 확인하세요.",
                        this
                    );

                    EndStage();
                    return;
                }
            }

            // 새 판에서는 모든 Upgrade Level을 0으로 시작한다.
            _levels.Add(definition.Id, 0);
        }

        _playerManager = playerManager;
        _active = false;

        // Player LevelUp을 UpgradeManager가 전달받는다.
        _playerManager.OnLevelUp += HandlePlayerLevelUp;
    }

    /// <summary>
    /// 보유 Level/선택지/플레이어 참조를 비우고 효과 배율을 모두 1로 되돌린다.
    /// </summary>
    public void EndStage()
    {
        _active = false;

        if (_playerManager != null)
        {
            _playerManager.OnLevelUp -= HandlePlayerLevelUp;
        }

        _playerManager = null;

        _levels.Clear();
        _choices.Clear();
        _candidates.Clear();
        _offerNumber = 0;
        MoveSpeedMultiplier = 1f;
        ReceivedDamageMultiplier = 1f;
        ExperienceMultiplier = 1f;
    }

    public void UpgradeSequence()
    {
        _active = true;
        PrintChoices();
    }

    /// <summary>증강 Id의 현재 Level을 반환한다. 보유하지 않은 Id는 0.</summary>
    public int GetLevel(string id)
    {
        return id != null && _levels.TryGetValue(id, out var level) ? level : 0;
    }

    /// <summary>
    /// 실제 추첨에 쓰는 weight를 반환한다. 최대레벨/없는 Id는 0.
    /// 정의의 Weight는 보존하므로 다음 판에는 원래 weight로 다시 시작한다.
    /// </summary>
    public float GetWeight(string id)
    {
        if (_definitions != null)
        {
            foreach (var definition in _definitions)
            {
                if (definition != null && definition.Id == id)
                {
                    return GetLevel(id) >= definition.MaxLevel ? 0f : definition.Weight;
                }
            }
        }

        return 0f;
    }

    /// <summary>
    /// 현재 선택지 index를 받아 해당 Upgrade Level을 1 증가시키고,
    /// 모든 보유 Upgrade의 Effects를 기준으로 최종 배율을 다시 계산한다.
    /// </summary>
    public bool TrySelectChoice(int choiceIndex)
    {
        if (!CanSelect() ||
            choiceIndex < 0 ||
            choiceIndex >= _choices.Count)
        {
            return false;
        }

        var chosen = _choices[choiceIndex];

        // 이미 최대 Level에 도달했거나 Weight가 0이면 선택하지 않는다.
        if (GetWeight(chosen.Id) <= 0f)
        {
            return false;
        }

        var previousLevel = GetLevel(chosen.Id);

        // 선택한 Upgrade Level 증가
        _levels[chosen.Id] = previousLevel + 1;

        /*
         * 기존 배율에 계속 값을 더하거나 곱하지 않는다.
         *
         * 현재 보유하고 있는 모든 Upgrade Level을 기준으로
         * 처음부터 다시 계산한다.
         *
         * 따라서 Upgrade 선택 순서에 영향을 받지 않는다.
         */
        var speedBonus = 0f;
        var damageChange = 0f;
        var xpBonus = 0f;

        foreach (var definition in _definitions)
        {
            var level = GetLevel(definition.Id);

            // 보유하지 않은 Upgrade라면 계산할 필요 없음
            if (level <= 0)
            {
                continue;
            }

            foreach (var effect in definition.Effects)
            {
                // 현재 구현에서는 Player Effect만 처리한다.
                // Weapon Effect는 이후 AbilityManager 연결 시 추가한다.
                if (effect.Target != UpgradeTarget.Player)
                {
                    continue;
                }

                // effect.Value는 Level 1당 적용량
                var totalValue = effect.Value * level;

                switch (effect.Type)
                {
                    case UpgradeEffectType.MoveSpeed:
                        speedBonus += totalValue;
                        break;

                    case UpgradeEffectType.ReceivedDamage:
                        damageChange += totalValue;
                        break;

                    case UpgradeEffectType.Experience:
                        xpBonus += totalValue;
                        break;

                    default:
                        Debug.LogWarning(
                            $"[증강] 처리되지 않은 Player Effect Type: {effect.Type}",
                            this
                        );
                        break;
                }
            }
        }

        // 기본값 1에 모든 증강 효과를 합산한다.
        MoveSpeedMultiplier =
            Mathf.Max(0f, 1f + speedBonus);

        ReceivedDamageMultiplier =
            Mathf.Max(0f, 1f + damageChange);

        ExperienceMultiplier =
            Mathf.Max(0f, 1f + xpBonus);

        Debug.Log(
            $"[증강 선택] {chosen.Name}: " +
            $"Lv.{previousLevel} => Lv.{GetLevel(chosen.Id)}/{chosen.MaxLevel}, " +
            $"다음 추첨 weight={GetWeight(chosen.Id):0.###}\n" +
            $"[증강 합계] " +
            $"이동 x{MoveSpeedMultiplier:0.###} " +
            $"(실제 속도 {_playerManager.Speed:0.###}), " +
            $"받는 피해 x{ReceivedDamageMultiplier:0.###}, " +
            $"획득 경험치 x{ExperienceMultiplier:0.###}",
            this
        );

        // Upgrade Sequence 종료
        _active = false;
        Time.timeScale = 1f;

        return true;
    }


    private bool CanSelect()
    {
        return _active && _playerManager != null && _playerManager.IsAlive &&
            Managers.Instance != null && Managers.Instance.IsSimulationRunning;
    }

    /// <summary>
    /// weight > 0인 후보에서 최대 3개를 중복 없이 추첨한다.
    /// 한 개를 뽑을 때마다 후보에서 제거한다. 후보가 3개 이하이면 낮은 weight도 모두 포함된다.
    /// </summary>
    private void RefreshChoices()
    {
        _choices.Clear();
        _candidates.Clear();
        foreach (var definition in _definitions)
        {
            if (GetWeight(definition.Id) > 0f)
            {
                _candidates.Add(definition);
            }
        }

        while (_choices.Count < CHOICE_COUNT && _candidates.Count > 0)
        {
            double totalWeight = 0;
            foreach (var candidate in _candidates) totalWeight += GetWeight(candidate.Id);
            var roll = UnityEngine.Random.value * totalWeight;
            var selectedIndex = _candidates.Count - 1;
            for (var i = 0; i < _candidates.Count; i++)
            {
                roll -= GetWeight(_candidates[i].Id);
                if (roll < 0)
                {
                    selectedIndex = i;
                    break;
                }
            }

            // Random.value는 1도 반환할 수 있다. 그 경계에서는 마지막 유효 후보를 사용한다.
            _choices.Add(_candidates[selectedIndex]);
            _candidates.RemoveAt(selectedIndex);
        }
    }


    //코드 정상 진입시 Time.timeScale=0f; 됩니다. 주의.
    /// <summary>
    /// 현재 선택 가능한 Upgrade를 추첨하고,
    /// 각 Upgrade의 다음 Level과 Effect 정보를 Console에 출력한다.
    /// 정상 진입하면 게임을 일시정지한다.
    /// </summary>
    private void PrintChoices()
    {
        RefreshChoices();

        _offerNumber++;

        var text = new StringBuilder(
            $"[증강 선택지 #{_offerNumber}]\n"
        );

        if (_choices.Count == 0)
        {
            text.Append(
                "선택 가능한 증강이 없습니다. " +
                "모든 최대레벨/weight 0 항목은 제외되었습니다."
            );

            Debug.Log(text.ToString(), this);
            return;
        }

        // 선택 중 게임 진행 정지
        Time.timeScale = 0f;

        for (var i = 0; i < _choices.Count; i++)
        {
            var choice = _choices[i];

            text.Append(
                $"Numpad {i + 1}: {choice.Name} | " +
                $"Lv.{GetLevel(choice.Id)} => " +
                $"{GetLevel(choice.Id) + 1}/{choice.MaxLevel}"
            );

            // Upgrade 하나가 여러 Effect를 가질 수 있으므로 모두 출력
            foreach (var effect in choice.Effects)
            {
                switch (effect.Type)
                {
                    case UpgradeEffectType.MoveSpeed:
                        text.Append(
                            $" | 이동 " +
                            $"{effect.Value * 100f:+0.#;-0.#;0}%"
                        );
                        break;

                    case UpgradeEffectType.ReceivedDamage:
                        text.Append(
                            $" | 받는 피해 " +
                            $"{effect.Value * 100f:+0.#;-0.#;0}%"
                        );
                        break;

                    case UpgradeEffectType.Experience:
                        text.Append(
                            $" | 경험치 " +
                            $"{effect.Value * 100f:+0.#;-0.#;0}%"
                        );
                        break;

                    default:
                        text.Append(
                            $" | {effect.Type} " +
                            $"{effect.Value:+0.###;-0.###;0}"
                        );
                        break;
                }
            }

            text.AppendLine(
                $" | weight={GetWeight(choice.Id):0.###}"
            );
        }

        Debug.Log(text.ToString(), this);
    }


    private bool ShouldOfferAugment(int level)
    {
        // 현재 테스트 조건.
        // 실제 증강 획득 Level 규칙이 정해지면 여기만 변경한다.
        // 예를들어 3레벨마다 증강을 획득한다면
        // return level % 3 == 0;

        //현재는 test용으로 매 레벨업시마다 증강을 획득한다.
        return true;
    }
    private void HandlePlayerLevelUp(int playerId, int level)
    {
        if (_playerManager == null ||
            playerId != _playerManager.LocalPlayerId)
        {
            return;
        }

        if (!ShouldOfferAugment(level))
        {
            return;
        }

        UpgradeSequence();
    }
}
