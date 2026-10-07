using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 포켓몬 한 종의 동작 그림. 시트 텍스처가 커서 MonsterVisualData와 떼어 Addressables로 필요할 때만 불러온다.
/// 직접 참조하지 말고 MonsterAnimationLoader로 받는다. 필드 이름은 SpriteCollab 애니 이름과 맞춘다.
/// </summary>
[CreateAssetMenu(fileName = "MonsterAnimation", menuName = "Monster/Monster Animation Set")]
public class MonsterAnimationSet : ScriptableObject
{
    [SerializeField] private EightDirectionFrames _walk = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _idle = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _sleep = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _hurt = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _attack = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _charge = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _shoot = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _strike = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _swing = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _rotate = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _hop = new EightDirectionFrames();

    [SerializeField] private Sprite[] _faintLeft = new Sprite[0];
    [SerializeField] private Sprite[] _faintRight = new Sprite[0];

    [SerializeField] private Sprite[] _poseLeft = new Sprite[0];
    [SerializeField] private Sprite[] _poseRight = new Sprite[0];

    [SerializeField] private List<MonsterSkillAnimation> _skills = new List<MonsterSkillAnimation>();

    public EightDirectionFrames Walk => _walk;

    public EightDirectionFrames Idle => _idle;

    public EightDirectionFrames Sleep => _sleep;

    public EightDirectionFrames Hurt => _hurt;

    public EightDirectionFrames Attack => _attack;

    public EightDirectionFrames Charge => _charge;

    public EightDirectionFrames Shoot => _shoot;

    public EightDirectionFrames Strike => _strike;

    public EightDirectionFrames Swing => _swing;

    public EightDirectionFrames Rotate => _rotate;

    public EightDirectionFrames Hop => _hop;

    /// <summary>
    /// 기절 그림. 좌우 두 방향만 있다.
    /// </summary>
    public Sprite[] FaintLeft => _faintLeft;

    public Sprite[] FaintRight => _faintRight;

    /// <summary>
    /// 성공 포즈 그림. 좌우 두 방향만 있다.
    /// </summary>
    public Sprite[] PoseLeft => _poseLeft;

    public Sprite[] PoseRight => _poseRight;

    /// <summary>
    /// 종마다 다른 특수 기술 그림 목록. 종에 없는 기술은 들어 있지 않다.
    /// </summary>
    public IReadOnlyList<MonsterSkillAnimation> Skills => _skills;

    /// <summary>
    /// 이름으로 특수 기술 그림을 찾는다. 예: 피카츄의 "Shock". 없으면 false.
    /// </summary>
    public bool TryGetSkill(string skillName, out EightDirectionFrames frames)
    {
        for (var i = 0; i < _skills.Count; i++)
        {
            if (_skills[i] != null && _skills[i].Name == skillName)
            {
                frames = _skills[i].Frames;
                return frames != null;
            }
        }

        frames = null;
        return false;
    }

    private void OnEnable()
    {
        _walk ??= new EightDirectionFrames();
        _idle ??= new EightDirectionFrames();
        _sleep ??= new EightDirectionFrames();
        _hurt ??= new EightDirectionFrames();
        _attack ??= new EightDirectionFrames();
        _charge ??= new EightDirectionFrames();
        _shoot ??= new EightDirectionFrames();
        _strike ??= new EightDirectionFrames();
        _swing ??= new EightDirectionFrames();
        _rotate ??= new EightDirectionFrames();
        _hop ??= new EightDirectionFrames();
        _faintLeft ??= new Sprite[0];
        _faintRight ??= new Sprite[0];
        _poseLeft ??= new Sprite[0];
        _poseRight ??= new Sprite[0];
        _skills ??= new List<MonsterSkillAnimation>();
    }
}
