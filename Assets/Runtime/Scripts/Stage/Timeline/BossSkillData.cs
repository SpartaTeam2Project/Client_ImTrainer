using UnityEngine;

/// <summary>
/// 보스 스킬 하나. 어떤 기술인지만 가지고, 쿨타임과 범위는 타임라인에 둔다.
/// </summary>
[CreateAssetMenu(fileName = "BossSkill", menuName = "Boss/Boss Skill")]
public class BossSkillData : ScriptableObject
{
    [SerializeField] private BossSkillKind _kind = BossSkillKind.None;

    public BossSkillKind Kind => _kind;
}
