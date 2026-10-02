using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 능력 하나. 레벨 수치와 동작 프리팹을 가진다.
/// </summary>
public abstract class WeaponAbilityData : ScriptableObject
{
    [SerializeField] protected WeaponAbilityType type;
    [SerializeField] private string title;
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject prefab;
    [SerializeField] protected bool isActiveAbility;
    [SerializeField] protected bool isWeaponAbility;
    [SerializeField] protected bool isEndgameAbility;
    [SerializeField] private bool isEvolution;
    [SerializeField] private List<EvolutionRequirement> evolutionRequirements = new List<EvolutionRequirement>();

    public WeaponAbilityType WeaponAbilityType => type;

    public string Title => title ?? string.Empty;

    public string Description => description ?? string.Empty;

    public Sprite Icon => icon;

    public GameObject Prefab => prefab;

    public bool IsActiveAbility => isActiveAbility;

    public bool IsWeaponAbility => isWeaponAbility;

    public bool IsEndgameAbility => isEndgameAbility;

    public bool IsEvolution => isEvolution;

    public List<EvolutionRequirement> EvolutionRequirements => evolutionRequirements;

    public abstract WeaponAbilityLevel[] Levels { get; }

    public int LevelsCount => Levels == null ? 0 : Levels.Length;

    /// <summary>
    /// 저장 레벨에 해당하는 수치. 범위를 벗어나면 null.
    /// </summary>
    public WeaponAbilityLevel GetLevel(int index)
    {
        if (Levels == null || index < 0 || index >= Levels.Length)
        {
            return null;
        }

        return Levels[index];
    }
}

/// <summary>
/// 레벨 한 칸의 수치.
/// </summary>
[System.Serializable]
public abstract class WeaponAbilityLevel
{
}

/// <summary>
/// 진화에 필요한 능력과 레벨.
/// </summary>
[System.Serializable]
public class EvolutionRequirement
{
    [SerializeField] private WeaponAbilityType abilityType;
    [SerializeField, Min(0)] private int requiredWeaponAbilityLevel;
    [SerializeField] private bool shouldRemoveAfterEvolution;

    public WeaponAbilityType WeaponAbilityType => abilityType;

    public int RequiredWeaponAbilityLevel => requiredWeaponAbilityLevel;

    public bool ShouldRemoveAfterEvolution => shouldRemoveAfterEvolution;
}
