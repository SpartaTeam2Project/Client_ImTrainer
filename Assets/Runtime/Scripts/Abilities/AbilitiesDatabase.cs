using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨업에 나올 수 있는 능력 목록과 가중치.
/// </summary>
[CreateAssetMenu(fileName = "Abilities Database", menuName = "Ability/Abilities Database")]
public class AbilitiesDatabase : ScriptableObject
{
    private const int DEFAULT_CAPACITY = 5;

    [SerializeField] private int activeAbilitiesCapacity = DEFAULT_CAPACITY;
    [SerializeField] private int passiveAbilitiesCapacity = DEFAULT_CAPACITY;

    [Header("Abilities Weight Multipliers")]
    [SerializeField] private float firstLevelsActiveAbilityWeightMultiplier = 1.2f;
    [SerializeField] private float aquiredAbilityWeightMultiplier = 1.2f;
    [SerializeField] private float lessAbilitiesOfTypeWeightMultiplier = 1.2f;
    [SerializeField] private float evolutionAbilityWeightMultiplier = 10f;
    [SerializeField] private float requiredForEvolutionWeightMultiplier = 1.2f;

    [SerializeField] private List<AbilityData> abilities = new List<AbilityData>();

    public int ActiveAbilitiesCapacity => activeAbilitiesCapacity;

    public int PassiveAbilitiesCapacity => passiveAbilitiesCapacity;

    public float FirstLevelsActiveAbilityWeightMultiplier => firstLevelsActiveAbilityWeightMultiplier;

    public float AquiredAbilityWeightMultiplier => aquiredAbilityWeightMultiplier;

    public float LessAbilitiesOfTypeWeightMultiplier => lessAbilitiesOfTypeWeightMultiplier;

    public float EvolutionAbilityWeightMultiplier => evolutionAbilityWeightMultiplier;

    public float RequiredForEvolutionWeightMultiplier => requiredForEvolutionWeightMultiplier;

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
