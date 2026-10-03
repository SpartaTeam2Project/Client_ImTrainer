using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 포켓몬 한 종의 그림, 타입, 크기. 몬스터와 무기가 같은 에셋을 쓴다.
/// 걷기 외 동작은 비워 둘 수 있다. 추적, 사격, 보스 동작은 필요한 칸만 쓴다.
/// 아이콘과 정보창 그림은 프레임 배열로 둔다. 재생 전에는 첫 장만 보여 준다.
/// 인스펙터는 MonsterVisualDataEditor가 그린다.
/// </summary>
[CreateAssetMenu(fileName = "MonsterVisual", menuName = "Monster/Monster Visual")]
public class MonsterVisualData : ScriptableObject
{
    public const float DEFAULT_SCALE = 2.7f;
    private const int MIN_GENERATION = 1;

    [SerializeField, Min(0.01f)] private float _scale = DEFAULT_SCALE;

    [SerializeField] private EightDirectionFrames _walk = new EightDirectionFrames();
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

    [SerializeField] private WeaponAbilityData _ability;

    [SerializeField] private MonsterType _primaryType = MonsterType.Normal;
    [SerializeField] private bool _hasSecondaryType;
    [SerializeField] private MonsterType _secondaryType = MonsterType.Normal;

    [SerializeField] private MonsterVisualData _evolution;

    [SerializeField, Min(MIN_GENERATION)] private int _generation = MIN_GENERATION;
    [SerializeField] private string _monsterName = string.Empty;
    [SerializeField, Min(0)] private int _dexNumber;
    [SerializeField] private string _unlockCondition = string.Empty;

    [SerializeField] private Sprite[] _icon = new Sprite[0];
    [SerializeField, FormerlySerializedAs("_portraitSize")] private Vector2 _iconSize;
    [SerializeField] private Sprite[] _infoAnimation = new Sprite[0];

    public float Scale => _scale > 0f ? _scale : DEFAULT_SCALE;

    public EightDirectionFrames Walk => _walk;

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

    public WeaponAbilityData WeaponAbility => _ability;

    /// <summary>
    /// 3성 세 마리를 합성하면 나오는 다음 종. 최종 진화는 비운다.
    /// </summary>
    public MonsterVisualData Evolution => _evolution;

    public MonsterType PrimaryType => _primaryType;

    public bool HasSecondaryType => _hasSecondaryType && _secondaryType != _primaryType;

    public MonsterType SecondaryType => _secondaryType;

    public int Generation => _generation < MIN_GENERATION ? MIN_GENERATION : _generation;

    public string MonsterName => _monsterName ?? string.Empty;

    public int DexNumber => _dexNumber < 0 ? 0 : _dexNumber;

    public string UnlockCondition => _unlockCondition ?? string.Empty;

    /// <summary>
    /// 스토리지 칸, 엔트리, 상점, 인벤토리에 쓰는 아이콘 애니메이션 프레임.
    /// </summary>
    public Sprite[] Icon => _icon;

    public Vector2 IconSize => _iconSize;

    /// <summary>
    /// 스토리지 정보창에 쓰는 애니메이션 프레임.
    /// </summary>
    public Sprite[] InfoAnimation => _infoAnimation;

    /// <summary>
    /// 방어 타입 목록. 서브가 없거나 메인과 같으면 길이 1이다.
    /// </summary>
    public MonsterType[] Types
    {
        get
        {
            if (!HasSecondaryType)
            {
                return new[] { _primaryType };
            }

            return new[] { _primaryType, _secondaryType };
        }
    }

    /// <summary>
    /// 프레임 중 비어 있지 않은 첫 장. 없으면 null이다.
    /// </summary>
    public static Sprite FirstFrame(Sprite[] frames)
    {
        if (frames == null)
        {
            return null;
        }

        for (var i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
            {
                return frames[i];
            }
        }

        return null;
    }

    private void OnEnable()
    {
        EnsureFrames();
    }

    private void EnsureFrames()
    {
        _walk ??= new EightDirectionFrames();
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
        _icon ??= new Sprite[0];
        _infoAnimation ??= new Sprite[0];
    }
}
