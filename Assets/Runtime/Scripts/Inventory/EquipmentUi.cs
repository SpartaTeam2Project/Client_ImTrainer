using System;
using UnityEngine;

/// <summary>
/// 인게임 장착 칸 여섯 개. 칸을 누르면 선택한다.
/// </summary>
public class EquipmentUi : MonoBehaviour
{
    [SerializeField] private InventoryItem[] _slots = new InventoryItem[WeaponSlots.MAX_COUNT];

    private Action<int> _onSelect;

    private void Awake()
    {
        for (var i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
            {
                Debug.LogError("장착 칸이 비어 있습니다: " + i);
                continue;
            }

            var slotIndex = i;
            _slots[i].Button.onClick.AddListener(() => _onSelect?.Invoke(slotIndex));
        }
    }

    /// <summary>
    /// 장착 목록을 다시 그린다.
    /// </summary>
    public void Refresh(int playerId, int selectedSlot, Action<int> onSelect)
    {
        _onSelect = onSelect;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            return;
        }

        var equipment = itemManager.GetEquipment(playerId);
        for (var i = 0; i < _slots.Length; i++)
        {
            var slot = _slots[i];
            if (slot == null)
            {
                continue;
            }

            slot.HidePrice();
            var selected = i == selectedSlot;
            if (equipment == null || i >= equipment.Stacks.Count || equipment.Stacks[i].Empty || equipment.Stacks[i].Item == null)
            {
                slot.ShowEmpty();
                slot.SetSelected(selected);
                continue;
            }

            var item = equipment.Stacks[i].Item;
            var visual = itemManager.GetVisual(item.uid);
            var portrait = visual != null ? MonsterVisualData.FirstFrame(visual.Icon) : null;
            slot.ShowPokemon(portrait, item.name, item.upgradeLevel, equipment.Stacks[i].Number, false, selected);
        }
    }
}
