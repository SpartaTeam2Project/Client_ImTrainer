using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 몬스터가 넘긴 재화를 바닥에 두고, 플레이어의 자석 범위 안에서 빨려 들어가게 한다.
/// </summary>
public class DropManager : BaseManager
{
    private const float DROP_SCATTER_RADIUS = 0.2f;

    [Header("Pickup")]
    [SerializeField] private float _pickupRadius = 0.35f;
    [SerializeField] private float _attractSpeed = 12f;

    private readonly Dictionary<EntityId, List<CoinDropBehavior>> _pools = new Dictionary<EntityId, List<CoinDropBehavior>>();
    private readonly List<CoinDropBehavior> _drops = new List<CoinDropBehavior>();

    private int _playerId;
    private bool _active;
    private float _pickupRadiusSqr;

    #region Unity Methods

    /// <summary>
    /// 드롭 매니저 초기화
    /// </summary>
    public override UniTask InitializeAsync()
    {
        CacheRadii();
        return base.InitializeAsync();
    }

    private void Update()
    {
        if (!_active || Managers.Instance == null || !Managers.Instance.IsSimulationRunning)
        {
            return;
        }

        if (!TryGetPlayerPosition(out var playerPosition))
        {
            return;
        }

        TickDrops(playerPosition, ResolveMagnetRadiusSqr(), Time.deltaTime);
    }

    /// <summary>
    /// 남아 있는 드롭을 치운다.
    /// </summary>
    public override void Cleanup()
    {
        ClearDrops();
        _active = false;
        base.Cleanup();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 판을 열고 재화 드롭을 받는다.
    /// </summary>
    public void BeginStage(int playerId)
    {
        ClearDrops();
        _playerId = playerId;
        _active = true;
        CacheRadii();
    }

    /// <summary>
    /// 판이 끝나면 바닥에 남은 드롭을 치운다.
    /// </summary>
    public void EndStage()
    {
        _active = false;
        ClearDrops();
    }

    /// <summary>
    /// 적이 가진 재화 프리팹을 사망 위치에 떨어뜨린다.
    /// </summary>
    public void Drop(int playerId, Vector2 position, CoinDropBehavior dropPrefab)
    {
        if (!_active || playerId != _playerId || dropPrefab == null || dropPrefab.Amount <= 0 || string.IsNullOrEmpty(dropPrefab.CurrencyId))
        {
            return;
        }

        var drop = GetDrop(dropPrefab);
        if (drop == null)
        {
            return;
        }

        var scattered = position + Random.insideUnitCircle * DROP_SCATTER_RADIUS;
        drop.Prepare(scattered);
    }

    #endregion

    #region Private Methods

    private void TickDrops(Vector2 playerPosition, float magnetRadiusSqr, float deltaTime)
    {
        var step = _attractSpeed * deltaTime;
        for (var i = 0; i < _drops.Count; i++)
        {
            var drop = _drops[i];
            if (drop == null || !drop.gameObject.activeSelf)
            {
                continue;
            }

            if (TryCollect(drop, playerPosition))
            {
                continue;
            }

            if (!drop.IsAttracting && drop.GetDistanceSqr(playerPosition) <= magnetRadiusSqr)
            {
                drop.BeginAttract();
            }

            if (!drop.IsAttracting)
            {
                continue;
            }

            drop.MoveToward(playerPosition, step);
            TryCollect(drop, playerPosition);
        }
    }

    private bool TryCollect(CoinDropBehavior drop, Vector2 playerPosition)
    {
        if (drop.GetDistanceSqr(playerPosition) > _pickupRadiusSqr)
        {
            return false;
        }

        var amount = drop.Amount;
        var currencyId = drop.CurrencyId;
        drop.Release();
        if (Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies))
        {
            currencies.Deposit(_playerId, currencyId, amount);
        }

        return true;
    }

    private float ResolveMagnetRadiusSqr()
    {
        if (!Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return 0f;
        }

        var magnet = Mathf.Max(0f, playerManager.MagnetRadius);
        return magnet * magnet;
    }

    private void CacheRadii()
    {
        var pickup = Mathf.Max(0.05f, _pickupRadius);
        _pickupRadiusSqr = pickup * pickup;
        _attractSpeed = Mathf.Max(0.1f, _attractSpeed);
    }

    private CoinDropBehavior GetDrop(CoinDropBehavior dropPrefab)
    {
        var key = dropPrefab.GetEntityId();
        if (!_pools.TryGetValue(key, out var pool))
        {
            pool = new List<CoinDropBehavior>();
            _pools.Add(key, pool);
        }

        for (var i = 0; i < pool.Count; i++)
        {
            var drop = pool[i];
            if (drop != null && !drop.gameObject.activeSelf)
            {
                return drop;
            }
        }

        var created = Instantiate(dropPrefab, transform);
        created.gameObject.name = dropPrefab.name;
        created.gameObject.SetActive(false);
        pool.Add(created);
        _drops.Add(created);
        return created;
    }

    private void ClearDrops()
    {
        for (var i = 0; i < _drops.Count; i++)
        {
            if (_drops[i] != null)
            {
                Destroy(_drops[i].gameObject);
            }
        }

        _drops.Clear();
        _pools.Clear();
    }

    private bool TryGetPlayerPosition(out Vector2 playerPosition)
    {
        playerPosition = Vector2.zero;
        if (!Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return false;
        }

        if (playerManager.LocalPlayerId != _playerId || !playerManager.IsAlive || playerManager.PlayerTransform == null)
        {
            return false;
        }

        playerPosition = playerManager.PlayerTransform.position;
        return true;
    }

    #endregion
}
