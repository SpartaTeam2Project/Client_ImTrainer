using UnityEngine;

/// <summary>
/// 고를 수 있는 트레이너 한 명. 인트로 획득 대상이고 세대와 성별이 같으면 인트로에서 획득한다.
/// </summary>
[CreateAssetMenu(fileName = "PlayableCharacter", menuName = "Player/Playable Character")]
public class PlayableCharacterData : ScriptableObject
{
    private const int MIN_GENERATION = 1;

    [SerializeField] private string _characterName = string.Empty;
    [SerializeField, Min(MIN_GENERATION)] private int _generation = MIN_GENERATION;
    [SerializeField] private TrainerGender _gender = TrainerGender.Boy;
    [SerializeField] private string _unlockCondition = string.Empty;
    [SerializeField] private bool _unlockedByIntro;
    [SerializeField] private Sprite _inGameSprite;
    [SerializeField] private Sprite _portrait;
    [SerializeField] private Vector2 _portraitSize;

    [Header("Versus")]
    [Tooltip("보스 VS 화면 왼쪽에 나오는 등 사진.")]
    [SerializeField] private Sprite _versusBack;

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

    public string CharacterName => _characterName ?? string.Empty;

    public int Generation => _generation < MIN_GENERATION ? MIN_GENERATION : _generation;

    public TrainerGender Gender => _gender;

    public string UnlockCondition => _unlockCondition ?? string.Empty;

    /// <summary>
    /// 인트로에서 세대와 성별을 고르면 바로 얻는 캐릭터면 true.
    /// </summary>
    public bool UnlockedByIntro => _unlockedByIntro;

    public Sprite InGameSprite => _inGameSprite;

    public Sprite Portrait => _portrait;

    public Vector2 PortraitSize => _portraitSize;

    public Sprite VersusBack => _versusBack;

    public Sprite[] IdleDown => _idleDown;

    public Sprite[] IdleUp => _idleUp;

    public Sprite[] IdleLeft => _idleLeft;

    public Sprite[] IdleRight => _idleRight;

    public Sprite[] WalkDown => _walkDown;

    public Sprite[] WalkUp => _walkUp;

    public Sprite[] WalkLeft => _walkLeft;

    public Sprite[] WalkRight => _walkRight;
}
