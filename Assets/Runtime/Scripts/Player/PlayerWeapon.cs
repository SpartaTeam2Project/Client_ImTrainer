using UnityEngine;

/// <summary>
/// 플레이어 주위 시계 칸에 붙는 포켓몬 장착을 담당한다.
/// </summary>
public class PlayerWeapon : MonoBehaviour
{
    private const WeaponSlot STARTING_SLOT = WeaponSlot.Hour3;
    private const float RESULT_SPREAD_SECONDS = 0.28f;

    [Header("Equipped")]
    [SerializeField] private Transform _equippedRoot;
    [SerializeField] private Transform[] _weaponSlots;
    [SerializeField] private Weapon _weaponPrefab;

    private Player _owner;
    private readonly Weapon[] _weapons = new Weapon[WeaponSlots.MAX_COUNT];
    private readonly MonsterVisualData[] _slotVisuals = new MonsterVisualData[WeaponSlots.MAX_COUNT];
    private int _playerId;
    private bool _equipmentSubscribed;

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

    /// <summary>
    /// 그 칸 포켓몬의 발사 위치. 칸이 비어 있으면 플레이어 중심을 넣고 false.
    /// </summary>
    public bool TryGetFirePosition(WeaponSlot slot, out Vector3 position)
    {
        var index = (int)slot;
        if (index >= 0 && index < _weapons.Length && _weapons[index] != null)
        {
            position = _weapons[index].transform.position;
            return true;
        }

        position = transform.position;
        return false;
    }

    #region Unity Methods

    private void OnEnable()
    {
        SubscribeEquipment();
    }

    private void OnDisable()
    {
        UnsubscribeEquipment();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 무기에 넘겨줄 주인을 연결한다.
    /// </summary>
    public void Initialize(Player owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// 시작 칸에 포켓몬을 붙인다. 그림은 이번 판 장착 목록을 따른다.
    /// </summary>
    public void EquipStarting(int playerId)
    {
        _playerId = playerId;
        ApplyEquippedScale();
        SubscribeEquipment();
        SyncFromInventory();
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
    /// 그 칸 포켓몬만 발사 방향 걷기를 재생하게 한다.
    /// </summary>
    public void FaceShot(WeaponSlot slot, Vector2 direction)
    {
        var index = (int)slot;
        if (index < 0 || index >= _weapons.Length || _weapons[index] == null)
        {
            return;
        }

        _weapons[index].FaceShot(direction);
    }

    /// <summary>
    /// 시작 그림이 붙어 있는 시계 칸. 3시가 비어 있으면 앞에서부터 찬 칸.
    /// </summary>
    public bool TryGetEquippedSlot(out WeaponSlot slot)
    {
        if (_slotVisuals[(int)STARTING_SLOT] != null)
        {
            slot = STARTING_SLOT;
            return true;
        }

        for (var i = 0; i < _slotVisuals.Length; i++)
        {
            if (_slotVisuals[i] == null)
            {
                continue;
            }

            slot = (WeaponSlot)i;
            return true;
        }

        slot = STARTING_SLOT;
        return false;
    }

    /// <summary>
    /// 장착한 포켓몬이 짧은 구간 안에서 각자 기절 그림을 재생하게 한다.
    /// </summary>
    public void PlayFaint()
    {
        PlayResult(true);
    }

    /// <summary>
    /// 장착한 포켓몬이 짧은 구간 안에서 각자 성공 포즈를 재생하게 한다.
    /// </summary>
    public void PlayPose()
    {
        PlayResult(false);
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
            _slotVisuals[i] = null;
            RefreshEquippedVisual();
            return;
        }
    }

    #endregion

    #region Private Methods

    private void PlayResult(bool faint)
    {
        var equipped = CountEquipped();
        for (var i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] == null)
            {
                continue;
            }

            // 한 마리는 바로 재생한다. 여러 마리는 칸 순서 없이 같은 구간 안에서만 어긋난다.
            var delay = equipped <= 1 ? 0f : Random.Range(0f, RESULT_SPREAD_SECONDS);
            if (faint)
            {
                _weapons[i].PlayFaint(delay);
            }
            else
            {
                _weapons[i].PlayPose(delay);
            }
        }
    }

    private int CountEquipped()
    {
        var count = 0;
        for (var i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] != null)
            {
                count++;
            }
        }

        return count;
    }

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
        weapon.Initialize(_playerId, slot);
        _slotVisuals[index] = visual;
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
        _slotVisuals[index] = null;
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
        // Destroy는 프레임 끝에 일어나므로, 같은 프레임 Equip의 CreateWeapon이 이 오브젝트를 다시 쓰지 않게 칸에서 뗀다.
        weapon.gameObject.SetActive(false);
        weapon.transform.SetParent(null, false);
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

    private void SyncFromInventory()
    {
        if (Managers.Instance != null
            && Managers.Instance.TryGetManager<ItemManager>(out var itemManager)
            && itemManager.GetEquipment(_playerId) != null)
        {
            var equipment = itemManager.GetEquipment(_playerId);
            for (var i = 0; i < _weapons.Length; i++)
            {
                ReleaseWeapon(i);
                if (i >= equipment.Stacks.Count || equipment.Stacks[i].Empty || equipment.Stacks[i].Item == null)
                {
                    continue;
                }

                var visual = itemManager.GetVisual(equipment.Stacks[i].Item.uid);
                if (visual != null)
                {
                    Equip((WeaponSlot)i, visual);
                }
            }

            RefreshEquippedVisual();
            return;
        }

        EquippedVisual = ResolveStartingVisual();
        Equip(STARTING_SLOT, EquippedVisual);
    }

    private void HandleEquipmentChanged(EquipmentChanged changed)
    {
        if (changed.PlayerId != _playerId || changed.Slot < 0 || changed.Slot >= _weapons.Length)
        {
            return;
        }

        ReleaseWeapon(changed.Slot);
        if (changed.Uid >= 0 && Managers.Instance != null && Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            var visual = itemManager.GetVisual(changed.Uid);
            if (visual != null)
            {
                Equip((WeaponSlot)changed.Slot, visual);
            }
        }

        RefreshEquippedVisual();
    }

    private void RefreshEquippedVisual()
    {
        var starting = _slotVisuals[(int)STARTING_SLOT];
        if (starting != null)
        {
            EquippedVisual = starting;
            return;
        }

        for (var i = 0; i < _slotVisuals.Length; i++)
        {
            if (_slotVisuals[i] != null)
            {
                EquippedVisual = _slotVisuals[i];
                return;
            }
        }

        EquippedVisual = null;
    }

    private void SubscribeEquipment()
    {
        if (_equipmentSubscribed || Managers.Instance == null || !Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            return;
        }

        eventManager.Subscribe<EquipmentChanged>(HandleEquipmentChanged);
        _equipmentSubscribed = true;
    }

    private void UnsubscribeEquipment()
    {
        if (!_equipmentSubscribed || Managers.Instance == null || !Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            _equipmentSubscribed = false;
            return;
        }

        eventManager.Unsubscribe<EquipmentChanged>(HandleEquipmentChanged);
        _equipmentSubscribed = false;
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
