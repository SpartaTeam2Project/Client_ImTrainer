using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 몬스터가 넘긴 경험치 구슬을 바닥에 두고, 플레이어의 자석 범위 안에서 빨려 들어가게 한다.
/// </summary>
public class ExperienceManager : BaseManager
{
    private const float DROP_SCATTER_RADIUS = 0.2f;

    [Header("Pickup")]
    [SerializeField] private float _pickupRadius = 0.35f;
    [SerializeField] private float _attractSpeed = 12f;

    private readonly Dictionary<EntityId, List<ExperienceGem>> _pools = new Dictionary<EntityId, List<ExperienceGem>>();
    private readonly List<ExperienceGem> _gems = new List<ExperienceGem>();
    private readonly ExperienceGemGlow _glow = new ExperienceGemGlow();

    private int _playerId;
    private bool _active;
    private float _pickupRadiusSqr;

    #region Unity Methods

    /// <summary>
    /// 경험치 매니저 초기화
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

        _glow.Tick(Time.time);
        if (!TryGetPlayerPosition(out var playerPosition))
        {
            return;
        }

        TickGems(playerPosition, ResolveMagnetRadiusSqr(), Time.deltaTime);
    }

    /// <summary>
    /// 남아 있는 구슬을 치운다.
    /// </summary>
    public override void Cleanup()
    {
        ClearGems();
        _glow.Dispose();
        _active = false;
        base.Cleanup();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 판을 열고 구슬 드롭을 받는다.
    /// </summary>
    public void BeginStage(int playerId)
    {
        ClearGems();
        _playerId = playerId;
        _active = true;
        CacheRadii();
    }

    /// <summary>
    /// 판이 끝나면 바닥에 남은 구슬을 치운다.
    /// </summary>
    public void EndStage()
    {
        _active = false;
        ClearGems();
    }

    /// <summary>
    /// 적이 가진 경험치 프리팹을 사망 위치에 떨어뜨린다.
    /// </summary>
    public void Drop(int playerId, Vector2 position, ExperienceGem gemPrefab)
    {
        if (!_active || playerId != _playerId || gemPrefab == null || gemPrefab.Xp <= 0f)
        {
            return;
        }

        var gem = GetGem(gemPrefab);
        if (gem == null)
        {
            return;
        }

        var scattered = position + Random.insideUnitCircle * DROP_SCATTER_RADIUS;
        gem.Prepare(scattered);
    }

    #endregion

    #region Private Methods

    private void TickGems(Vector2 playerPosition, float magnetRadiusSqr, float deltaTime)
    {
        var step = _attractSpeed * deltaTime;
        for (var i = 0; i < _gems.Count; i++)
        {
            var gem = _gems[i];
            if (gem == null || !gem.gameObject.activeSelf)
            {
                continue;
            }

            if (TryCollect(gem, playerPosition))
            {
                continue;
            }

            if (!gem.IsAttracting && gem.GetDistanceSqr(playerPosition) <= magnetRadiusSqr)
            {
                gem.BeginAttract();
            }

            if (!gem.IsAttracting)
            {
                continue;
            }

            gem.MoveToward(playerPosition, step);
            TryCollect(gem, playerPosition);
        }
    }

    private bool TryCollect(ExperienceGem gem, Vector2 playerPosition)
    {
        if (gem.GetDistanceSqr(playerPosition) > _pickupRadiusSqr)
        {
            return false;
        }

        var xp = gem.Xp;
        gem.PlayPickupSound();
        gem.Release();
        if (Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            playerManager.AddExperience(_playerId, xp);
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

    private ExperienceGem GetGem(ExperienceGem gemPrefab)
    {
        var key = gemPrefab.GetEntityId();
        if (!_pools.TryGetValue(key, out var pool))
        {
            pool = new List<ExperienceGem>();
            _pools.Add(key, pool);
        }

        for (var i = 0; i < pool.Count; i++)
        {
            var gem = pool[i];
            if (gem != null && !gem.gameObject.activeSelf)
            {
                return gem;
            }
        }

        var created = Instantiate(gemPrefab, transform);
        created.gameObject.name = gemPrefab.name;
        created.SetSharedMaterial(_glow.GetRuntimeMaterial(gemPrefab.SourceMaterial));
        created.gameObject.SetActive(false);
        pool.Add(created);
        _gems.Add(created);
        return created;
    }

    private void ClearGems()
    {
        for (var i = 0; i < _gems.Count; i++)
        {
            if (_gems[i] != null)
            {
                Destroy(_gems[i].gameObject);
            }
        }

        _gems.Clear();
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
