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

    public int Amount => _amount;

    public string CurrencyId => _currencyId;

    public bool IsAttracting { get; private set; }

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
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (_spriteRenderer == null)
        {
            return;
        }

        _spriteRenderer.sortingOrder = SORTING_ORDER;
    }

    #endregion
}
