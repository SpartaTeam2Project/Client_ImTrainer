using UnityEngine;

/// <summary>
/// 무기 포켓몬 한 칸의 그림과 크기. 기획자는 번호 에셋에 아이들, 공격, 추가 동작, 총알, 크기를 넣는다.
/// 추가 동작은 비워 둘 수 있다. 직선 사격 등 공격 프리팹은 필요한 칸만 쓴다.
/// </summary>
[CreateAssetMenu(fileName = "WeaponVisual", menuName = "Weapon/Weapon Visual")]
public class WeaponVisualData : ScriptableObject, ISerializationCallbackReceiver
{
    public const float DEFAULT_SCALE = 2.7f;

    [Header("Size")]
    [SerializeField, Min(0.01f)] private float _scale = DEFAULT_SCALE;

    [SerializeField] private EightDirectionFrames _idle = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _attack = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _sleep = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _hurt = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _charge = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _shoot = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _strike = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _swing = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _rotate = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _hop = new EightDirectionFrames();
    [SerializeField] private EightDirectionFrames _faint = new EightDirectionFrames();

    [SerializeField, HideInInspector] private Sprite[] _idleDown = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _idleDownRight = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _idleRight = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _idleUpRight = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _idleUp = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _idleUpLeft = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _idleLeft = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _idleDownLeft = System.Array.Empty<Sprite>();

    [SerializeField, HideInInspector] private Sprite[] _attackDown = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _attackDownRight = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _attackRight = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _attackUpRight = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _attackUp = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _attackUpLeft = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _attackLeft = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] private Sprite[] _attackDownLeft = System.Array.Empty<Sprite>();

    [Header("Projectile")]
    [SerializeField] private Sprite _projectileSprite;

    [Header("Type")]
    [SerializeField] private MonsterType _attackType = MonsterType.Normal;

    public float Scale => _scale > 0f ? _scale : DEFAULT_SCALE;

    public EightDirectionFrames Idle => _idle;

    public EightDirectionFrames Attack => _attack;

    public EightDirectionFrames Sleep => _sleep;

    public EightDirectionFrames Hurt => _hurt;

    public EightDirectionFrames Charge => _charge;

    public EightDirectionFrames Shoot => _shoot;

    public EightDirectionFrames Strike => _strike;

    public EightDirectionFrames Swing => _swing;

    public EightDirectionFrames Rotate => _rotate;

    public EightDirectionFrames Hop => _hop;

    public EightDirectionFrames Faint => _faint;

    public Sprite ProjectileSprite => _projectileSprite;

    public MonsterType AttackType => _attackType;

    /// <summary>
    /// 예전 평탄 필드에 있던 아이들·공격 그림을 접는 칸으로 옮긴다.
    /// </summary>
    public void OnAfterDeserialize()
    {
        EnsureFrames();
        CopyLegacyIfEmpty(_idle, _idleDown, _idleDownRight, _idleRight, _idleUpRight, _idleUp, _idleUpLeft, _idleLeft, _idleDownLeft);
        CopyLegacyIfEmpty(_attack, _attackDown, _attackDownRight, _attackRight, _attackUpRight, _attackUp, _attackUpLeft, _attackLeft, _attackDownLeft);
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
        _idle ??= new EightDirectionFrames();
        _attack ??= new EightDirectionFrames();
        _sleep ??= new EightDirectionFrames();
        _hurt ??= new EightDirectionFrames();
        _charge ??= new EightDirectionFrames();
        _shoot ??= new EightDirectionFrames();
        _strike ??= new EightDirectionFrames();
        _swing ??= new EightDirectionFrames();
        _rotate ??= new EightDirectionFrames();
        _hop ??= new EightDirectionFrames();
        _faint ??= new EightDirectionFrames();
    }

    private static void CopyLegacyIfEmpty(
        EightDirectionFrames frames,
        Sprite[] down,
        Sprite[] downRight,
        Sprite[] right,
        Sprite[] upRight,
        Sprite[] up,
        Sprite[] upLeft,
        Sprite[] left,
        Sprite[] downLeft)
    {
        if (frames.HasFrames())
        {
            return;
        }

        frames.Assign(down, downRight, right, upRight, up, upLeft, left, downLeft);
    }
}
