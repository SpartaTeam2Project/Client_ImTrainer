using UnityEngine;

/// <summary>
/// 적이 떨어뜨리는 재화. 수량과 재화 아이디는 프리팹이 가진다.
/// </summary>
public class CoinDropBehavior : MonoBehaviour
{
    private const int SORTING_ORDER = 2;

    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private int _amount = 1;
    [SerializeField] private string _currencyId;

    [Header("Glow")]
    [Tooltip("펄스 글로우를 줄 렌더러. 비우면 본체 스프라이트에 준다.")]
    [SerializeField] private SpriteRenderer _glowRenderer;
    [SerializeField] private Transform _aura;

    public int Amount => _amount;

    public string CurrencyId => _currencyId;

    public bool IsAttracting { get; private set; }

    /// <summary>
    /// 프리팹에서 펄스 글로우 렌더러에 넣어 둔 머티리얼.
    /// </summary>
    public Material SourceMaterial
    {
        get
        {
            var glowRenderer = ResolveGlowRenderer();
            return glowRenderer != null ? glowRenderer.sharedMaterial : null;
        }
    }

    #region Unity Methods

    private void Awake()
    {
        ApplySprite();
    }

    private void OnEnable()
    {
        ApplySprite();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 바닥에 두고 획득 전 상태로 되돌린다.
    /// </summary>
    public void Prepare(Vector2 position)
    {
        IsAttracting = false;
        transform.position = position;
        RandomizeAura();
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            return;
        }

        ApplySprite();
    }

    /// <summary>
    /// 자석 범위에 들어왔으므로 플레이어를 계속 따라간다.
    /// </summary>
    public void BeginAttract()
    {
        IsAttracting = true;
    }

    /// <summary>
    /// 목표 지점까지 한 스텝 이동한다.
    /// </summary>
    public void MoveToward(Vector2 target, float step)
    {
        var current = (Vector2)transform.position;
        transform.position = Vector2.MoveTowards(current, target, step);
    }

    /// <summary>
    /// 목표와의 거리 제곱을 돌려준다.
    /// </summary>
    public float GetDistanceSqr(Vector2 target)
    {
        return ((Vector2)transform.position - target).sqrMagnitude;
    }

    /// <summary>
    /// 펄스 글로우 렌더러를 같은 종류 드롭끼리 공유하는 머티리얼로 바꾼다.
    /// </summary>
    public void SetSharedMaterial(Material material)
    {
        var glowRenderer = ResolveGlowRenderer();
        if (glowRenderer != null && material != null)
        {
            glowRenderer.sharedMaterial = material;
        }
    }

    /// <summary>
    /// 풀로 되돌린다.
    /// </summary>
    public void Release()
    {
        IsAttracting = false;
        gameObject.SetActive(false);
    }

    #endregion

    #region Private Methods

    private void ApplySprite()
    {
        CacheRenderer();
        if (_spriteRenderer == null)
        {
            return;
        }

        _spriteRenderer.sortingOrder = SORTING_ORDER;
    }

    private void CacheRenderer()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private SpriteRenderer ResolveGlowRenderer()
    {
        if (_glowRenderer != null)
        {
            return _glowRenderer;
        }

        CacheRenderer();
        return _spriteRenderer;
    }

    // 아우라 연기는 셰이더 시간으로 모두 같이 움직이므로, 방향을 돌려 드롭마다 다르게 보이게 한다.
    private void RandomizeAura()
    {
        if (_aura != null)
        {
            _aura.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }
    }

    #endregion
}
