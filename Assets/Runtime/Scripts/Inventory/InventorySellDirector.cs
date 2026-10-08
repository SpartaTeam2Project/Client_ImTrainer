using System;
using System.Collections.Generic;
using Coffee.UIEffects;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// 판매할 포켓몬 아이콘을 디졸브로 지운 뒤에 판다. 디졸브 동안 가방은 그대로 두고 그 칸은 조작할 수 없다.
/// 그사이 구매로 가방이 다시 정렬되면 포켓몬이 옮겨 간 칸에서 디졸브를 다시 건다. InventoryUi가 만든다.
/// 휴지통에 끌어다 놓았으면 놓은 자리의 끌기 그림이 디졸브되고, 칸 아이콘은 판매될 때까지 숨겨 둔다.
/// </summary>
public sealed class InventorySellDirector
{
    private sealed class PendingSale
    {
        public InventorySlotRef Target;
        // 지금 디졸브를 걸어 둔 칸. 정렬로 다른 칸이 되면 새 칸에 다시 건다.
        public InventoryItem Slot;
        // 휴지통에 놓았을 때 대신 디졸브되는 끌기 그림 복사본. 없으면 칸 아이콘이 디졸브된다.
        public Image Ghost;
        public IconDissolve GhostDissolve;
    }

    private readonly IReadOnlyList<InventoryItem> _bagSlots;
    private readonly EquipmentUi _equipmentUi;
    private readonly Action _onSold;
    private readonly UIEffect _ghostTemplate;
    private readonly List<PendingSale> _pending = new List<PendingSale>();

    /// <summary>
    /// ghostTemplate은 끌기 그림에 복사할 디졸브 설정이다. 보통 슬롯 프리팹의 디졸브 덮개를 넘긴다.
    /// </summary>
    public InventorySellDirector(IReadOnlyList<InventoryItem> bagSlots, EquipmentUi equipmentUi, UIEffect ghostTemplate, Action onSold)
    {
        _bagSlots = bagSlots;
        _equipmentUi = equipmentUi;
        _ghostTemplate = ghostTemplate;
        _onSold = onSold;
    }

    /// <summary>
    /// 판매를 디졸브로 시작한다. 이미 판매 중인 포켓몬이면 true를 돌려주고 무시한다.
    /// 디졸브로 팔 수 없으면 false를 돌려주고, 부른 쪽이 바로 판다.
    /// 마지막 장착 포켓몬처럼 판매가 실패할 칸은 디졸브 뒤에 실패하지 않게 미리 false로 돌려준다.
    /// dragGhost를 넘기면 그 자리에 복사본을 남겨 복사본을 디졸브한다. 원본 끌기 그림은 다음 끌기에 바로 쓸 수 있다.
    /// </summary>
    public bool TryBegin(InventorySlotRef target, Image dragGhost = null)
    {
        if (target.IsEmpty || !TryGetContext(out var itemManager, out var playerId))
        {
            return false;
        }

        if (Find(target) != null)
        {
            return true;
        }

        if (target.Kind == InventorySlotDrag.SlotKind.Equipment && !CanSellEquipped(itemManager, playerId))
        {
            return false;
        }

        var pending = new PendingSale { Target = target };
        CreateGhost(pending, dragGhost);
        _pending.Add(pending);
        if (!Apply(pending, itemManager, playerId))
        {
            _pending.Remove(pending);
            DestroyGhost(pending);
            return false;
        }

        if (pending.GhostDissolve != null)
        {
            pending.GhostDissolve.Play(() => Complete(pending));
        }

        return true;
    }

    /// <summary>
    /// 디졸브 중인 칸이면 true. 끌기와 선택을 막을 때 쓴다.
    /// </summary>
    public bool IsSelling(InventorySlotDrag.SlotKind kind, int index)
    {
        for (var i = 0; i < _pending.Count; i++)
        {
            var target = _pending[i].Target;
            if (target.Kind == kind && target.Index == index)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 칸을 다시 그린 뒤 부른다. 정렬로 옮겨진 포켓몬은 새 칸에서 디졸브를 처음부터 다시 걸고,
    /// 합성 재료로 쓰여 사라진 포켓몬은 판매를 그만둔다.
    /// </summary>
    public void Reapply()
    {
        if (_pending.Count == 0 || !TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        for (var i = _pending.Count - 1; i >= 0; i--)
        {
            if (i < _pending.Count && !Apply(_pending[i], itemManager, playerId))
            {
                DestroyGhost(_pending[i]);
                _pending.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 창을 닫을 때 디졸브를 기다리지 않고 남은 판매를 바로 처리한다.
    /// </summary>
    public void FinishAll()
    {
        if (_pending.Count == 0)
        {
            return;
        }

        var pending = _pending.ToArray();
        _pending.Clear();
        for (var i = 0; i < pending.Length; i++)
        {
            DestroyGhost(pending[i]);
            if (pending[i].Slot != null)
            {
                pending[i].Slot.StopDissolve();
            }

            Sell(pending[i].Target);
        }

        // 씬이 내려가며 꺼질 때는 매니저가 먼저 사라졌을 수 있어서 다시 그리지 않는다.
        if (TryGetContext(out _, out _))
        {
            _onSold?.Invoke();
        }
    }

    /// <summary>
    /// 포켓몬이 지금 있는 칸에 디졸브를 건다. 이미 그 칸에 걸려 있으면 그대로 둔다. 포켓몬이 없으면 false.
    /// </summary>
    private bool Apply(PendingSale pending, ItemManager itemManager, int playerId)
    {
        if (!TryResolve(itemManager, playerId, ref pending.Target))
        {
            if (pending.Slot != null)
            {
                pending.Slot.StopDissolve();
            }

            return false;
        }

        var slot = GetSlot(pending.Target);
        if (slot == null)
        {
            return false;
        }

        if (slot == pending.Slot)
        {
            return true;
        }

        pending.Slot = slot;
        if (pending.Ghost != null)
        {
            slot.HoldDissolve();
        }
        else
        {
            slot.PlayDissolve(() => Complete(pending));
        }

        return true;
    }

    private void Complete(PendingSale pending)
    {
        if (!_pending.Remove(pending))
        {
            return;
        }

        DestroyGhost(pending);
        // 판매 뒤 다시 그릴 때까지 숨긴 아이콘이 보이지 않게 같은 프레임에 되돌린다.
        if (pending.Slot != null)
        {
            pending.Slot.StopDissolve();
        }

        Sell(pending.Target);
        _onSold?.Invoke();
    }

    /// <summary>
    /// 끌기 그림을 같은 자리에 복사하고 디졸브 설정을 붙인다. 설정이 없거나 끌기 그림이 안 보이면 칸 아이콘을 디졸브한다.
    /// </summary>
    private void CreateGhost(PendingSale pending, Image dragGhost)
    {
        if (dragGhost == null || _ghostTemplate == null || !dragGhost.gameObject.activeInHierarchy || dragGhost.sprite == null)
        {
            return;
        }

        var ghost = Object.Instantiate(dragGhost, dragGhost.transform.parent);
        ghost.name = "SellGhost";
        ghost.transform.SetAsLastSibling();
        var effect = ghost.gameObject.AddComponent<UIEffect>();
        effect.LoadPreset(_ghostTemplate);
        pending.Ghost = ghost;
        pending.GhostDissolve = new IconDissolve(ghost, effect, ghost.gameObject);
    }

    private static void DestroyGhost(PendingSale pending)
    {
        if (pending.GhostDissolve != null)
        {
            pending.GhostDissolve.Stop();
            pending.GhostDissolve = null;
        }

        if (pending.Ghost != null)
        {
            Object.Destroy(pending.Ghost.gameObject);
            pending.Ghost = null;
        }
    }

    private void Sell(InventorySlotRef target)
    {
        if (!TryGetContext(out var itemManager, out var playerId) || !TryResolve(itemManager, playerId, ref target))
        {
            return;
        }

        if (target.Kind == InventorySlotDrag.SlotKind.Bag)
        {
            itemManager.TrySell(playerId, target.Index);
        }
        else
        {
            itemManager.TrySellEquipped(playerId, target.Index);
        }
    }

    /// <summary>
    /// 판매할 포켓몬의 지금 칸 번호를 맞춘다. 가방은 정렬돼서 종과 성으로 다시 찾고, 장착 칸은 그대로인지 확인한다.
    /// </summary>
    private static bool TryResolve(ItemManager itemManager, int playerId, ref InventorySlotRef target)
    {
        if (target.Kind == InventorySlotDrag.SlotKind.Bag)
        {
            var index = InventoryMerge.ResolveBagIndex(itemManager.GetInventory(playerId), target);
            if (index < 0)
            {
                return false;
            }

            target.Index = index;
            return true;
        }

        var equipment = itemManager.GetEquipment(playerId);
        if (equipment == null || target.Index < 0 || target.Index >= equipment.Stacks.Count)
        {
            return false;
        }

        var stack = equipment.Stacks[target.Index];
        return !stack.Empty && stack.Item != null && stack.Item.uid == target.Uid && stack.Item.upgradeLevel == target.Star;
    }

    /// <summary>
    /// 판매 중인 장착 칸까지 빼고도 최소 장착 수보다 많이 남아야 판다.
    /// </summary>
    private bool CanSellEquipped(ItemManager itemManager, int playerId)
    {
        var equipment = itemManager.GetEquipment(playerId);
        if (equipment == null)
        {
            return false;
        }

        var selling = 0;
        for (var i = 0; i < _pending.Count; i++)
        {
            if (_pending[i].Target.Kind == InventorySlotDrag.SlotKind.Equipment)
            {
                selling++;
            }
        }

        return equipment.CountFilled() - selling > ItemManager.MIN_EQUIPPED;
    }

    private InventoryItem GetSlot(InventorySlotRef target)
    {
        if (target.Kind == InventorySlotDrag.SlotKind.Bag)
        {
            return target.Index >= 0 && target.Index < _bagSlots.Count ? _bagSlots[target.Index] : null;
        }

        return _equipmentUi != null ? _equipmentUi.GetSlot(target.Index) : null;
    }

    private PendingSale Find(InventorySlotRef target)
    {
        for (var i = 0; i < _pending.Count; i++)
        {
            var pending = _pending[i].Target;
            if (pending.Kind == target.Kind && pending.Uid == target.Uid && pending.Star == target.Star
                && (target.Kind == InventorySlotDrag.SlotKind.Bag || pending.Index == target.Index))
            {
                return _pending[i];
            }
        }

        return null;
    }

    private static bool TryGetContext(out ItemManager itemManager, out int playerId)
    {
        itemManager = null;
        playerId = 0;
        if (Managers.Instance == null
            || !Managers.Instance.TryGetManager<ItemManager>(out itemManager)
            || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return false;
        }

        playerId = playerManager.LocalPlayerId;
        return true;
    }
}
