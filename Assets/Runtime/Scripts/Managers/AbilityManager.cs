using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 무기 프리팹을 붙여 따라다니게 하고, 판이 끝나면 치운다.
/// </summary>
public class AbilityManager : BaseManager
{
    private const WeaponSlot STARTING_SLOT = WeaponSlot.Hour3;
    private const float FALLBACK_SIZE = 0.65f;
    private const int FALLBACK_SORTING_ORDER = 11;

    [Header("Weapon")]
    [SerializeField] private Weapon _startingWeaponPrefab;

    private Weapon _weapon;

    #region Unity Methods

    /// <summary>
    /// 무기 매니저 초기화
    /// </summary>
    public override UniTask InitializeAsync()
    {
        return base.InitializeAsync();
    }

    private void LateUpdate()
    {
        if (_weapon == null || Managers.Instance == null || !Managers.Instance.IsSimulationRunning)
        {
            return;
        }

        _weapon.Tick(Time.deltaTime);
    }

    public override void Cleanup()
    {
        ClearWeapon();
        base.Cleanup();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 시작 무기 선택 없이 3시 칸에 시작 무기 프리팹을 붙인다.
    /// </summary>
    public void BeginStage(int playerId)
    {
        ClearWeapon();
        var playerManager = GetManager<PlayerManager>();
        if (playerManager == null)
        {
            return;
        }

        if (playerManager.PlayerTransform == null)
        {
            Debug.LogError("플레이어가 없어 무기를 붙이지 않습니다.");
            return;
        }

        _weapon = CreateWeapon();
        _weapon.Bind(this);
        _weapon.Initialize(playerId, STARTING_SLOT);
        _weapon.Tick(0f);
    }

    /// <summary>
    /// 판이 끝나면 무기와 투사체를 치운다.
    /// </summary>
    public void EndStage()
    {
        ClearWeapon();
    }

    /// <summary>
    /// 무기가 씬과 함께 사라지면 참조를 비운다.
    /// </summary>
    public void NotifyWeaponDestroyed(Weapon weapon)
    {
        if (_weapon != weapon)
        {
            return;
        }

        _weapon = null;
    }

    #endregion

    #region Private Methods

    private Weapon CreateWeapon()
    {
        if (_startingWeaponPrefab != null)
        {
            var weapon = Instantiate(_startingWeaponPrefab);
            weapon.gameObject.name = "Weapon";
            return weapon;
        }

        Debug.LogWarning("시작 무기 프리팹이 없어 자리표시 무기를 만듭니다.");
        var actorObject = new GameObject("Weapon");
        actorObject.transform.localScale = new Vector3(FALLBACK_SIZE, FALLBACK_SIZE, 1f);
        var renderer = actorObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = FALLBACK_SORTING_ORDER;
        return actorObject.AddComponent<StraightWeapon>();
    }

    private void ClearWeapon()
    {
        if (_weapon == null)
        {
            return;
        }

        var weapon = _weapon;
        _weapon = null;
        if (weapon != null)
        {
            Destroy(weapon.gameObject);
        }
    }

    #endregion
}
