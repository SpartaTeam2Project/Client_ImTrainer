using UnityEngine;

/// <summary>
/// 레벨 배열을 가진 능력 데이터.
/// </summary>
public class GenericAbilityData<T> : AbilityData where T : AbilityLevel
{
    [SerializeField] private T[] levels;

    public override AbilityLevel[] Levels => levels;

    /// <summary>
    /// 해당 레벨의 구체 수치.
    /// </summary>
    public new T GetLevel(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length)
        {
            return null;
        }

        return levels[index];
    }
}
