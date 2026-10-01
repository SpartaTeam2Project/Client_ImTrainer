using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨업에 나올 수 있는 능력 목록과 가중치.
/// </summary>
[CreateAssetMenu(fileName = "Abilities Database", menuName = "Ability/Abilities Database")]
public class AbilitiesDatabase : ScriptableObject
{
    /// <summary>
    /// 슬롯 수를 따로 정하지 않았을 때의 기본값.
    /// </summary>
    private const int DEFAULT_CAPACITY = 5;

    /// <summary>
    /// 가질 수 있는 액티브 능력 최대 수. 가득 차면 새 액티브 능력은 나오지 않음.
    /// </summary>
    [SerializeField] private int activeAbilitiesCapacity = DEFAULT_CAPACITY;

    /// <summary>
    /// 가질 수 있는 패시브 능력 최대 수. 가득 차면 새 패시브 능력은 나오지 않음.
    /// </summary>
    [SerializeField] private int passiveAbilitiesCapacity = DEFAULT_CAPACITY;

    // 모든 능력은 가중치 1에서 시작하고, 조건마다 아래 배율이 곱해짐. 클수록 잘 나옴.
    /// <summary>
    /// 초반 레벨(FIRST_LEVELS 미만)에 액티브 능력에 곱하는 배율.
    /// </summary>
    [Header("Abilities Weight Multipliers")]
    [SerializeField] private float firstLevelsActiveAbilityWeightMultiplier = 1.2f;

    /// <summary>
    /// 이미 가진 능력에 곱하는 배율.
    /// </summary>
    [SerializeField] private float aquiredAbilityWeightMultiplier = 1.2f;

    /// <summary>
    /// 액티브/패시브 중 가진 개수가 더 적은 쪽에 곱하는 배율.
    /// </summary>
    [SerializeField] private float lessAbilitiesOfTypeWeightMultiplier = 1.2f;

    /// <summary>
    /// 진화 능력에 곱하는 배율.
    /// </summary>
    [SerializeField] private float evolutionAbilityWeightMultiplier = 10f;

    /// <summary>
    /// 가진 능력의 진화에 필요한 패시브에 곱하는 배율.
    /// </summary>
    [SerializeField] private float requiredForEvolutionWeightMultiplier = 1.2f;

    /// <summary>
    /// 레벨업에 나올 수 있는 전체 능력 목록.
    /// </summary>
    [SerializeField] private List<AbilityData> abilities = new List<AbilityData>();

    public int ActiveAbilitiesCapacity => activeAbilitiesCapacity;

    public int PassiveAbilitiesCapacity => passiveAbilitiesCapacity;

    public float FirstLevelsActiveAbilityWeightMultiplier => firstLevelsActiveAbilityWeightMultiplier;

    public float AquiredAbilityWeightMultiplier => aquiredAbilityWeightMultiplier;

    public float LessAbilitiesOfTypeWeightMultiplier => lessAbilitiesOfTypeWeightMultiplier;

    public float EvolutionAbilityWeightMultiplier => evolutionAbilityWeightMultiplier;

    public float RequiredForEvolutionWeightMultiplier => requiredForEvolutionWeightMultiplier;

    /// <summary>
    /// 목록의 능력 수. 목록이 없으면 0.
    /// </summary>
    public int AbilitiesCount => abilities == null ? 0 : abilities.Count;

    /// <summary>
    /// 목록 순번의 능력. 없으면 null.
    /// </summary>
    public AbilityData GetAbility(int index)
    {
        if (abilities == null || index < 0 || index >= abilities.Count)
        {
            return null;
        }

        return abilities[index];
    }

    /// <summary>
    /// 타입이 같은 능력. 없으면 null.
    /// </summary>
    public AbilityData GetAbility(AbilityType type)
    {
        if (abilities == null)
        {
            return null;
        }

        for (var i = 0; i < abilities.Count; i++)
        {
            var ability = abilities[i];
            if (ability != null && ability.AbilityType == type)
            {
                return ability;
            }
        }

        return null;
    }
}
