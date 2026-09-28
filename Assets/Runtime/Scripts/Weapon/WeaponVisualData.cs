using UnityEngine;

/// <summary>
/// 무기 포켓몬 한 칸의 그림과 크기. 기획자는 번호 에셋에 8방향, 총알, 크기만 넣는다.
/// 직선 사격 등 공격 프리팹은 이 에셋을 읽어 필요한 칸만 쓴다.
/// </summary>
[CreateAssetMenu(fileName = "WeaponVisual", menuName = "Weapon/Weapon Visual")]
public class WeaponVisualData : ScriptableObject
{
    public const float DEFAULT_SCALE = 2.7f;

    [Header("Size")]
    [SerializeField, Min(0.01f)] private float _scale = DEFAULT_SCALE;

    [Header("Idle Eight Direction")]
    [SerializeField] private Sprite[] _idleDown = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _idleDownRight = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _idleRight = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _idleUpRight = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _idleUp = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _idleUpLeft = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _idleLeft = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _idleDownLeft = System.Array.Empty<Sprite>();

    [Header("Projectile")]
    [SerializeField] private Sprite _projectileSprite;

    public float Scale => _scale > 0f ? _scale : DEFAULT_SCALE;

    public Sprite[] IdleDown => _idleDown;

    public Sprite[] IdleDownRight => _idleDownRight;

    public Sprite[] IdleRight => _idleRight;

    public Sprite[] IdleUpRight => _idleUpRight;

    public Sprite[] IdleUp => _idleUp;

    public Sprite[] IdleUpLeft => _idleUpLeft;

    public Sprite[] IdleLeft => _idleLeft;

    public Sprite[] IdleDownLeft => _idleDownLeft;

    public Sprite ProjectileSprite => _projectileSprite;
}
