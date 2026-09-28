using UnityEngine;

/// <summary>
/// 포켓몬 한 종의 그림, 타입, 크기. 몬스터와 무기가 같은 에셋을 쓴다.
/// 걷기 외 동작은 비워 둘 수 있다. 추적, 사격, 보스 동작은 필요한 칸만 쓴다.
/// </summary>
[CreateAssetMenu(fileName = "MonsterVisual", menuName = "Monster/Monster Visual")]
public class MonsterVisualData : ScriptableObject, ISerializationCallbackReceiver
{
    public const float DEFAULT_SCALE = 2.7f;

    [Header("Size")]
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
    [SerializeField] private EightDirectionFrames _faint = new EightDirectionFrames();

    [SerializeField, HideInInspector] private Sprite[] _walkDown = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _walkDownRight = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _walkRight = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _walkUpRight = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _walkUp = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _walkUpLeft = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _walkLeft = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _walkDownLeft = System.Array.Empty<Sprite>();

    [Header("Projectile")]
    [SerializeField] private Sprite _projectileSprite;

    [Header("Type")]
    [SerializeField] private MonsterType _primaryType = MonsterType.Normal;
    [SerializeField] private bool _hasSecondaryType;
    [SerializeField] private MonsterType _secondaryType = MonsterType.Normal;

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

    public EightDirectionFrames Faint => _faint;

    public Sprite ProjectileSprite => _projectileSprite;

    public MonsterType PrimaryType => _primaryType;

    public bool HasSecondaryType => _hasSecondaryType && _secondaryType != _primaryType;

    public MonsterType SecondaryType => _secondaryType;

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
    /// 예전 평탄 필드에 있던 걷기 그림을 접는 칸으로 옮긴다.
    /// </summary>
    public void OnAfterDeserialize()
    {
        EnsureFrames();
        if (_walk.HasFrames())
        {
            return;
        }

        _walk.Assign(
            _walkDown,
            _walkDownRight,
            _walkRight,
            _walkUpRight,
            _walkUp,
            _walkUpLeft,
            _walkLeft,
            _walkDownLeft);
    }

    /// <summary>
    /// 직렬화 전에 추가 작업은 없다.
    /// </summary>
    public void OnBeforeSerialize()
    {
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
        _faint ??= new EightDirectionFrames();
    }
}
