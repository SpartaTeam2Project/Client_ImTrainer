using UnityEngine;

/// <summary>
/// 몬스터 한 칸의 그림. 기획자는 번호 에셋에 8방향 걷기와 총알 스프라이트만 넣는다.
/// 추적, 사격, 보스 동작은 이 에셋을 읽어 필요한 칸만 쓴다.
/// </summary>
[CreateAssetMenu(fileName = "MonsterVisual", menuName = "Monster/Monster Visual")]
public class MonsterVisualData : ScriptableObject
{
    [Header("Walk Eight Direction")]
    [SerializeField] private Sprite[] _walkDown = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _walkDownRight = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _walkRight = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _walkUpRight = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _walkUp = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _walkUpLeft = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _walkLeft = System.Array.Empty<Sprite>();
    [SerializeField] private Sprite[] _walkDownLeft = System.Array.Empty<Sprite>();

    [Header("Projectile")]
    [SerializeField] private Sprite _projectileSprite;

    public Sprite[] WalkDown => _walkDown;

    public Sprite[] WalkDownRight => _walkDownRight;

    public Sprite[] WalkRight => _walkRight;

    public Sprite[] WalkUpRight => _walkUpRight;

    public Sprite[] WalkUp => _walkUp;

    public Sprite[] WalkUpLeft => _walkUpLeft;

    public Sprite[] WalkLeft => _walkLeft;

    public Sprite[] WalkDownLeft => _walkDownLeft;

    public Sprite ProjectileSprite => _projectileSprite;
}
