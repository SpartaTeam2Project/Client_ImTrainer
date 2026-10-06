using System;
using UnityEngine;

/// <summary>
/// 보스 트레이너가 등장하는 방식.
/// </summary>
public enum BossEntranceKind
{
    [InspectorName("걸어오기")]
    WalkIn = 0,
    [InspectorName("제자리 공개")]
    Reveal = 1
}

/// <summary>
/// 보스 트레이너 한 명. 등장 그림과 VS 초상화, 데리고 있는 몬스터를 가진다.
/// </summary>
[CreateAssetMenu(fileName = "BossTrainer", menuName = "Boss/Boss Trainer")]
public class BossTrainerData : ScriptableObject
{
    [SerializeField] private string _trainerName = string.Empty;

    [Header("Idle")]
    [SerializeField] private Sprite[] _idleDown = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _idleUp = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _idleLeft = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _idleRight = System.Array.Empty<Sprite>();

    [Header("Walk")]
    [SerializeField] private Sprite[] _walkDown = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _walkUp = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _walkLeft = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _walkRight = System.Array.Empty<Sprite>();

    [Header("Entrance")]
    [SerializeField] private BossEntranceKind _entranceKind = BossEntranceKind.WalkIn;
    [Tooltip("등장할 때 띄우는 느낌표.")]
    [SerializeField] private Sprite _exclamation;

    [Header("Versus")]
    [Tooltip("보스 VS 화면에서 보스 쪽 초상화로 바뀌는 프레임.")]
    [SerializeField] private Sprite[] _versusFrames = System.Array.Empty<Sprite>();

    [Header("Party")]
    [SerializeField] private BossPartyMember[] _party = System.Array.Empty<BossPartyMember>();

    public string TrainerName => _trainerName ?? string.Empty;

    public Sprite[] IdleDown => _idleDown;

    public Sprite[] IdleUp => _idleUp;

    public Sprite[] IdleLeft => _idleLeft;

    public Sprite[] IdleRight => _idleRight;

    public Sprite[] WalkDown => _walkDown;

    public Sprite[] WalkUp => _walkUp;

    public Sprite[] WalkLeft => _walkLeft;

    public Sprite[] WalkRight => _walkRight;

    public BossEntranceKind EntranceKind => _entranceKind;

    public Sprite Exclamation => _exclamation;

    public Sprite[] VersusFrames => _versusFrames;

    public BossPartyMember[] Party => _party ?? System.Array.Empty<BossPartyMember>();

    /// <summary>
    /// 파티에 몬스터가 한 마리라도 있으면 true.
    /// </summary>
    public bool HasMonster()
    {
        var party = Party;
        for (var i = 0; i < party.Length; i++)
        {
            if (party[i] != null && party[i].Monster != null)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// 보스 트레이너가 꺼내는 몬스터 한 마리. 체력과 속도는 타임라인 클립에서 고친다.
/// </summary>
[Serializable]
public class BossPartyMember
{
    [SerializeField] private MonsterVisualData _monster;
    [SerializeField] private BossSkillKind _skill = BossSkillKind.None;
    [Tooltip("보스 스킬 탄 그림. 비어 있으면 타임라인 클립의 탄을 쓴다.")]
    [SerializeField] private Sprite _projectileSprite;

    public MonsterVisualData Monster => _monster;

    public BossSkillKind Skill => _skill;

    public Sprite ProjectileSprite => _projectileSprite;
}
