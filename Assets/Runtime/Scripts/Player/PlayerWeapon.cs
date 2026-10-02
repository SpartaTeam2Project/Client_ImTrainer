using UnityEngine;

/// <summary>
/// 플레이어 주위 시계 칸에 붙는 포켓몬 장착을 담당한다.
/// </summary>
public class PlayerWeapon : MonoBehaviour
{
    private const WeaponSlot STARTING_SLOT = WeaponSlot.Hour3;

    [Header("Equipped")]
    [SerializeField] private Transform _equippedRoot;
    [SerializeField] private Transform[] _weaponSlots;
    [SerializeField] private Weapon _weaponPrefab;

    private Player _owner;
    private readonly Weapon[] _weapons = new Weapon[WeaponSlots.MAX_COUNT];
    private int _playerId;

    public MonsterVisualData EquippedVisual { get; private set; }

    /// <summary>
    /// 투사체가 나갈 위치. 시작 칸 포켓몬이 있으면 그 위치, 없으면 플레이어 중심.
    /// </summary>
    public Vector3 FirePosition
    {
        get
        {
            var weapon = _weapons[(int)STARTING_SLOT];
            return weapon != null ? weapon.transform.position : transform.position;
        }
    }

    #region Public Methods

    /// <summary>
    /// 무기에 넘겨줄 주인을 연결한다.
    /// </summary>
    public void Initialize(Player owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// 시작 칸에 포켓몬을 붙인다. 그림은 스토리지 엔트리에서 고른 포켓몬을 쓴다.
    /// </summary>
    public void EquipStarting(int playerId)
    {
        _playerId = playerId;
        ApplyEquippedScale();
        EquippedVisual = ResolveStartingVisual();
        Equip(STARTING_SLOT, EquippedVisual);
    }

    /// <summary>
    /// 비어 있는 시계 칸에 포켓몬을 붙인다. 여섯 칸이 차 있으면 false.
    /// </summary>
    public bool TryAddWeapon(MonsterVisualData visual)
    {
        for (var i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] != null)
            {
                continue;
            }

            return Equip((WeaponSlot)i, visual);
        }

        return false;
    }

    /// <summary>
    /// 장착한 포켓몬이 발사 방향 걷기를 한 바퀴 재생하게 한다.
    /// </summary>
    public void FaceShot(Vector2 direction)
    {
        for (var i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] == null)
            {
                continue;
            }

            _weapons[i].FaceShot(direction);
        }
    }

    /// <summary>
    /// 칸에 남은 포켓몬을 치운다.
    /// </summary>
    public void ClearWeapons()
    {
        EquippedVisual = null;
        for (var i = 0; i < _weapons.Length; i++)
        {
            ReleaseWeapon(i);
        }
    }

    /// <summary>
    /// 포켓몬이 씬과 함께 사라지면 칸 참조를 비운다.
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

    private bool Equip(WeaponSlot slot, MonsterVisualData visual)
    {
        var index = (int)slot;
        if (index < 0 || index >= _weapons.Length || _weapons[index] != null)
        {
            return false;
        }

        var weapon = CreateWeapon(index);
        if (weapon == null)
        {
            return false;
        }

        _weapons[index] = weapon;
        var followDistance = weapon.FollowDistance;
        PlaceSlot(slot, followDistance);
        weapon.transform.SetParent(SlotTransform(index), false);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
        weapon.Bind(_owner);
        weapon.ApplyVisual(visual);
        weapon.Initialize(_playerId);
        return true;
    }

    private Weapon CreateWeapon(int index)
    {
        var slot = SlotTransform(index);
        if (slot == null)
        {
            return null;
        }

        var existing = slot.GetComponentInChildren<Weapon>(true);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return existing;
        }

        if (_weaponPrefab == null)
        {
            Debug.LogWarning("장착 포켓몬 프리팹이 없습니다.");
            return null;
        }

        var weapon = Instantiate(_weaponPrefab, slot);
        weapon.gameObject.name = "Weapon";
        return weapon;
    }

    private void ReleaseWeapon(int index)
    {
        var weapon = _weapons[index];
        _weapons[index] = null;
        if (weapon == null)
        {
            var slot = SlotTransform(index);
            if (slot == null)
            {
                return;
            }

            weapon = slot.GetComponentInChildren<Weapon>(true);
        }

        if (weapon == null)
        {
            return;
        }

        weapon.Unbind();
        Destroy(weapon.gameObject);
    }

    private void PlaceSlot(WeaponSlot slot, float followDistance)
    {
        var point = SlotTransform((int)slot);
        if (point == null)
        {
            return;
        }

        var offset = WeaponSlots.GetDirection(slot) * followDistance;
        point.localPosition = new Vector3(offset.x, offset.y, 0f);
    }

    private Transform SlotTransform(int index)
    {
        if (_weaponSlots == null || index < 0 || index >= _weaponSlots.Length)
        {
            return null;
        }

        return _weaponSlots[index];
    }

    private void ApplyEquippedScale()
    {
        if (_equippedRoot == null)
        {
            return;
        }

        var scale = transform.localScale;
        _equippedRoot.localScale = new Vector3(
            InverseScale(scale.x),
            InverseScale(scale.y),
            InverseScale(scale.z));
    }

    private static float InverseScale(float scale)
    {
        if (Mathf.Approximately(scale, 0f))
        {
            return 1f;
        }

        return 1f / scale;
    }

    private MonsterVisualData ResolveStartingVisual()
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<AccountManager>(out var accountManager))
        {
            var runMonster = accountManager.ResolveRunMonster();
            if (runMonster != null)
            {
                return runMonster;
            }

            var starter = accountManager.ResolveStarterVisual();
            if (starter != null)
            {
                return starter;
            }
        }

        return null;
    }

    #endregion
}
