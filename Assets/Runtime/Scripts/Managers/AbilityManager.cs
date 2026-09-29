using System.Collections.Generic;
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
    [Tooltip("시작 무기가 재생할 그림. Monster0001 같은 에셋을 넣는다.")]
    [SerializeField] private MonsterVisualData _startingWeaponVisual;

    [Header("Level Up")]
    [SerializeField] private AudioClip _levelUpFanfare;

    private readonly Queue<int> _levelUpQueue = new Queue<int>();
    private readonly Weapon[] _weapons = new Weapon[WeaponSlots.MAX_COUNT];

    private int _playerId;
    private bool _waitingForChoice;

    public int PendingLevelUpLevel { get; private set; }

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
        if (Managers.Instance == null || !Managers.Instance.IsSimulationRunning)
        {
            return;
        }

        for (var i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] == null)
            {
                continue;
            }

            _weapons[i].Tick(Time.deltaTime);
        }
    }

    public override void Cleanup()
    {
        ClearLevelUpQueue();
        ClearWeapon();
        base.Cleanup();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 3시 칸에 시작 무기를 붙인다. 프로필에 스타터가 있으면 그 그림을 쓴다.
    /// </summary>
    public void BeginStage(int playerId)
    {
        _playerId = playerId;
        ClearLevelUpQueue();
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

        PlaceWeapon(STARTING_SLOT, _startingWeaponPrefab, ResolveStartingVisual());
    }

    /// <summary>
    /// 비어 있는 시계 칸에 무기를 붙인다. 여섯 칸이 차 있으면 false.
    /// </summary>
    public bool TryAddWeapon(Weapon prefab)
    {
        if (prefab == null || _playerId == 0)
        {
            return false;
        }

        for (var i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] != null)
            {
                continue;
            }

            PlaceWeapon((WeaponSlot)i, prefab, null);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 판이 끝나면 무기와 투사체를 치운다.
    /// </summary>
    public void EndStage()
    {
        ClearLevelUpQueue();
        ClearWeapon();
    }

    /// <summary>
    /// 레벨업 증강 선택 진입점. 선택지가 생기면 여기서 게임을 멈추고, 고르면 CompleteLevelUpOffer로 이어 간다.
    /// </summary>
    public void OfferLevelUp(int playerId, int level)
    {
        if (playerId != _playerId || level <= 0)
        {
            return;
        }

        _levelUpQueue.Enqueue(level);
        PresentLevelUpQueue();
    }

    /// <summary>
    /// 증강 선택이 끝났을 때 남은 레벨업을 이어서 연다.
    /// </summary>
    public void CompleteLevelUpOffer()
    {
        _waitingForChoice = false;
        PresentLevelUpQueue();
    }

    /// <summary>
    /// 고른 시계 칸 무기에, 고른 수치만 한 칸 올린다.
    /// </summary>
    public bool SelectWeaponUpgrade(WeaponSlot slot, WeaponStatKind stat)
    {
        var index = (int)slot;
        if (index < 0 || index >= _weapons.Length || _weapons[index] == null)
        {
            return false;
        }

        return _weapons[index].TryApplyUpgrade(stat);
    }

    /// <summary>
    /// 무기가 씬과 함께 사라지면 참조를 비운다.
    /// </summary>
    public void NotifyWeaponDestroyed(Weapon weapon)
    {
        for (var i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] != weapon)
            {
                continue;
            }

            _weapons[i] = null;
            return;
        }
    }

    #endregion

    #region Private Methods

    private void PresentLevelUpQueue()
    {
        if (_waitingForChoice)
        {
            return;
        }

        while (_levelUpQueue.Count > 0)
        {
            PendingLevelUpLevel = _levelUpQueue.Dequeue();
            PlayLevelUpSound();
        }
    }

    private void PlayLevelUpSound()
    {
        if (_levelUpFanfare == null || Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(_levelUpFanfare);
    }

    private void ClearLevelUpQueue()
    {
        _levelUpQueue.Clear();
        _waitingForChoice = false;
        PendingLevelUpLevel = 0;
    }

    private MonsterVisualData ResolveStartingVisual()
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<AccountManager>(out var accountManager))
        {
            var starter = accountManager.ResolveStarterVisual();
            if (starter != null)
            {
                return starter;
            }
        }

        return _startingWeaponVisual;
    }

    private void PlaceWeapon(WeaponSlot slot, Weapon prefab, MonsterVisualData visual)
    {
        var index = (int)slot;
        if (index < 0 || index >= _weapons.Length || _weapons[index] != null)
        {
            return;
        }

        var weapon = CreateWeapon(prefab);
        if (weapon == null)
        {
            return;
        }

        _weapons[index] = weapon;
        weapon.Bind(this);
        weapon.ApplyVisual(visual);
        weapon.Initialize(_playerId, slot);
        weapon.Tick(0f);
    }

    private Weapon CreateWeapon(Weapon prefab)
    {
        if (prefab != null)
        {
            var weapon = Instantiate(prefab);
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
        for (var i = 0; i < _weapons.Length; i++)
        {
            var weapon = _weapons[i];
            _weapons[i] = null;
            if (weapon != null)
            {
                Destroy(weapon.gameObject);
            }
        }
    }

    #endregion
}
