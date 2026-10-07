using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

/// <summary>
/// 포켓몬 한 종의 타입, 크기, 도감 정보, 아이콘. 몬스터와 무기가 같은 에셋을 쓴다.
/// 동작 그림은 텍스처가 커서 MonsterAnimationSet으로 떼어 두고 Addressables 참조만 갖는다.
/// DB와 UI가 이 에셋을 전부 참조해도 동작 그림은 올라오지 않는다. 그림은 MonsterAnimationLoader로 받는다.
/// 아이콘과 정보창 그림은 프레임 배열로 둔다. 재생 전에는 첫 장만 보여 준다.
/// 인스펙터는 MonsterVisualDataEditor가 그린다.
/// </summary>
[CreateAssetMenu(fileName = "MonsterVisual", menuName = "Monster/Monster Visual")]
public class MonsterVisualData : ScriptableObject
{
    public const float DEFAULT_SCALE = 2.7f;
    public const int DEFAULT_SHOP_PRICE = 10;
    private const int MIN_GENERATION = 1;
    private const int MIN_SHOP_PRICE = 1;

    [SerializeField, Min(0.01f)] private float _scale = DEFAULT_SCALE;

    [SerializeField] private AssetReferenceT<MonsterAnimationSet> _animations = new AssetReferenceT<MonsterAnimationSet>(string.Empty);

    [SerializeField] private WeaponAbilityData _ability;

    [SerializeField] private MonsterType _primaryType = MonsterType.Normal;
    [SerializeField] private bool _hasSecondaryType;
    [SerializeField] private MonsterType _secondaryType = MonsterType.Normal;

    [SerializeField] private MonsterVisualData _evolution;
    [SerializeField] private MonsterVisualData _megaEvolution;
    [SerializeField] private MonsterVisualData _vmaxEvolution;

    [SerializeField, Min(MIN_GENERATION)] private int _generation = MIN_GENERATION;
    [SerializeField] private string _monsterName = string.Empty;
    [SerializeField, Min(0)] private int _dexNumber;
    [SerializeField] private string _unlockCondition = string.Empty;
    [SerializeField] private bool _startable;

    [SerializeField, Min(MIN_SHOP_PRICE)] private int _shopPrice = DEFAULT_SHOP_PRICE;
    [SerializeField] private string _shopCurrencyId = CurrenciesManager.MONSTER_BALL_ID;
    [SerializeField, Min(0)] private int _baseStatTotal;
    [SerializeField] private bool _legendary;

    [SerializeField] private Sprite[] _icon = new Sprite[0];
    [SerializeField, FormerlySerializedAs("_portraitSize")] private Vector2 _iconSize;
    [SerializeField] private Sprite[] _infoAnimation = new Sprite[0];

    public float Scale => _scale > 0f ? _scale : DEFAULT_SCALE;

    /// <summary>
    /// 동작 그림 세트의 Addressables 참조. 형태가 기본형 그림을 같이 쓰면 기본형 세트를 가리킨다.
    /// </summary>
    public AssetReferenceT<MonsterAnimationSet> Animations => _animations;

    public WeaponAbilityData WeaponAbility => _ability;

    /// <summary>
    /// 3성 세 마리를 합성하면 나오는 다음 종. 최종 진화는 비운다.
    /// </summary>
    public MonsterVisualData Evolution => _evolution;

    /// <summary>
    /// 메가진화한 종. 없으면 비운다. 정보창 표시 전용이며 획득 규칙은 아직 없다.
    /// </summary>
    public MonsterVisualData MegaEvolution => _megaEvolution;

    /// <summary>
    /// 거다이맥스한 종. 없으면 비운다. 정보창 표시 전용이며 획득 규칙은 아직 없다.
    /// </summary>
    public MonsterVisualData VMaxEvolution => _vmaxEvolution;

    public MonsterType PrimaryType => _primaryType;

    public bool HasSecondaryType => _hasSecondaryType && _secondaryType != _primaryType;

    public MonsterType SecondaryType => _secondaryType;

    public int Generation => _generation < MIN_GENERATION ? MIN_GENERATION : _generation;

    public string MonsterName => _monsterName ?? string.Empty;

    public int DexNumber => _dexNumber < 0 ? 0 : _dexNumber;

    public string UnlockCondition => _unlockCondition ?? string.Empty;

    /// <summary>
    /// 스토리지에 나와 이번 판 시작 포켓몬으로 고를 수 있으면 true. 데이터베이스에 있어도 꺼져 있으면 스토리지에 없다.
    /// </summary>
    public bool Startable => _startable;

    /// <summary>
    /// 상점 1성 구매가. 화폐는 ShopCurrencyId다. 높은 성은 ItemManager가 이 값으로 계산한다.
    /// </summary>
    public int ShopPrice => _shopPrice < MIN_SHOP_PRICE ? MIN_SHOP_PRICE : _shopPrice;

    /// <summary>
    /// 상점에서 사고팔 때 쓰는 화폐 id. 비어 있으면 몬스터볼이다.
    /// </summary>
    public string ShopCurrencyId => string.IsNullOrEmpty(_shopCurrencyId) ? CurrenciesManager.MONSTER_BALL_ID : _shopCurrencyId;

    /// <summary>
    /// 종족값 합. 상점 등급을 나눌 때 쓴다.
    /// </summary>
    public int BaseStatTotal => _baseStatTotal < 0 ? 0 : _baseStatTotal;

    /// <summary>
    /// 전설·환상 포켓몬이면 true. 상점에서 종족값과 따로 전설 등급으로 뽑는다.
    /// </summary>
    public bool Legendary => _legendary;

    /// <summary>
    /// 스토리지 칸, 엔트리, 상점, 인벤토리에 쓰는 아이콘 애니메이션 프레임.
    /// </summary>
    public Sprite[] Icon => _icon;

    public Vector2 IconSize => _iconSize;

    /// <summary>
    /// 아이콘 크기에 배율을 곱한 UI 크기. 크기가 0이면 false라서 프리팹 크기를 그대로 둔다.
    /// </summary>
    public bool TryGetIconSize(float scale, out Vector2 size)
    {
        size = _iconSize * scale;
        return size.x > 0f && size.y > 0f;
    }

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
        _icon ??= new Sprite[0];
        _infoAnimation ??= new Sprite[0];
    }
}
