using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점에서 사면 산 포켓몬이 들어간 가방 칸을 숨겨 두고, 볼이 열리면 그 칸으로 광선을 쏜다.
/// 광선이 닿으면 칸에 빨간 실루엣을 띄운다. InventoryUi가 만들어 ShopUi에 묶는다.
/// 연달아 사면 살 때마다 가방이 다시 정렬돼 포켓몬이 다른 칸으로 옮겨진다.
/// 그래서 연출 상태는 칸이 아니라 포켓몬(uid, 성)마다 기억하고, 가방을 다시 그릴 때마다 지금 칸에 다시 건다.
/// </summary>
public sealed class PurchaseBeamDirector : IShopPurchaseEffect
{
    private const string BEAM_SOUND = "pb_beam";

    /// <summary>
    /// 구매 한 번의 연출. 광선이 닿기 전에는 칸을 숨기고, 닿은 뒤에는 실루엣을 띄운다.
    /// </summary>
    private sealed class PendingSummon
    {
        public int Uid;
        public int Star;
        // 새로 생긴 칸이면 광선이 닿을 때까지 빈 칸처럼 숨긴다. 쌓인 칸이면 닿을 때 번쩍이기만 한다.
        public bool Hide;
        public bool Arrived;
        // 지금 연출을 걸어 둔 칸. 정렬로 다른 칸이 되면 새 칸에 다시 건다.
        public InventoryItem Slot;
    }

    private readonly PurchaseBeam _beam;
    private readonly IReadOnlyList<InventoryItem> _bagSlots;
    private readonly InventoryFocusNavigator _navigator;
    private readonly List<PendingSummon> _pending = new List<PendingSummon>();
    // 구매 직후 볼이 열릴 때까지 상점 칸 번호로 그 구매를 찾는다.
    private readonly Dictionary<int, PendingSummon> _byOffer = new Dictionary<int, PendingSummon>();

    public PurchaseBeamDirector(PurchaseBeam beam, IReadOnlyList<InventoryItem> bagSlots, InventoryFocusNavigator navigator)
    {
        _beam = beam;
        _bagSlots = bagSlots;
        _navigator = navigator;
    }

    /// <summary>
    /// 새로 생긴 칸이면 광선 연출이 끝날 때까지 칸을 숨긴다. 가방 칸이 그려진 같은 프레임이라 한 번도 보이지 않는다.
    /// </summary>
    public void OnPurchased(int offerIndex)
    {
        if (_beam == null || !TryGetPurchased(offerIndex, out var uid, out var star)
            || !TryFindIndex(uid, star, out var bag, out var index))
        {
            return;
        }

        var pending = new PendingSummon { Uid = uid, Star = star, Hide = bag.Stacks[index].Number <= 1 };
        _pending.Add(pending);
        _byOffer[offerIndex] = pending;
        Apply(pending);
    }

    /// <summary>
    /// 산 포켓몬의 가방 칸으로 ball에서 광선을 쏜다. 칸이 스크롤 밖이면 먼저 보이게 옮긴다.
    /// </summary>
    public void OnBallOpened(int offerIndex, RectTransform ball)
    {
        if (!_byOffer.TryGetValue(offerIndex, out var pending))
        {
            return;
        }

        _byOffer.Remove(offerIndex);
        if (ball == null || !TryFindIndex(pending.Uid, pending.Star, out _, out var index))
        {
            Remove(pending);
            return;
        }

        _navigator.ScrollTo(InventorySlotDrag.SlotKind.Bag, index);
        UiSound.Play(BEAM_SOUND);
        _beam.Play(ball, () => FindSlotRect(pending), () => Arrive(pending));
    }

    /// <summary>
    /// 가방을 다시 그린 뒤 부른다. 정렬로 옮겨진 포켓몬의 연출을 지금 칸에 다시 건다.
    /// 다른 포켓몬이 그려진 칸의 연출은 그 칸이 다시 그려질 때 이미 끝났다.
    /// </summary>
    public void Reapply()
    {
        for (var i = _pending.Count - 1; i >= 0; i--)
        {
            if (i < _pending.Count)
            {
                Apply(_pending[i]);
            }
        }
    }

    /// <summary>
    /// 창이 닫히면 광선을 지우고 숨겨 둔 칸을 되돌린다.
    /// </summary>
    public void Stop()
    {
        _pending.Clear();
        _byOffer.Clear();
        if (_beam != null)
        {
            _beam.Stop();
        }

        for (var i = 0; i < _bagSlots.Count; i++)
        {
            if (_bagSlots[i] != null)
            {
                _bagSlots[i].StopSummon();
            }
        }
    }

    private void Arrive(PendingSummon pending)
    {
        if (!_pending.Contains(pending))
        {
            return;
        }

        pending.Arrived = true;
        pending.Slot = null;
        Apply(pending);
    }

    /// <summary>
    /// 포켓몬이 지금 있는 칸에 연출을 건다. 이미 그 칸에 걸려 있으면 그대로 둔다.
    /// 광선이 닿기 전이면 칸을 숨기고, 닿은 뒤면 실루엣을 처음부터 띄운다.
    /// </summary>
    private void Apply(PendingSummon pending)
    {
        if (!TryFindIndex(pending.Uid, pending.Star, out _, out var index))
        {
            Remove(pending);
            return;
        }

        var slot = _bagSlots[index];
        if (slot == pending.Slot)
        {
            return;
        }

        pending.Slot = slot;
        // 같은 포켓몬을 연달아 사 한 칸에 쌓였으면 먼저 산 쪽 연출을 그대로 둔다. 다시 걸면 먼저 산 쪽이 풀린다.
        if (HasEarlier(pending))
        {
            if (pending.Arrived)
            {
                Remove(pending);
            }

            return;
        }

        if (!pending.Arrived)
        {
            slot.Summon.Begin(pending.Hide);
            return;
        }

        // 실루엣 도중 다른 칸으로 옮겨졌으면 새 칸에서 처음부터 다시 띄운다.
        slot.Summon.Begin(pending.Hide);
        slot.Summon.Play(() => Remove(pending));
    }

    private bool HasEarlier(PendingSummon pending)
    {
        for (var i = 0; i < _pending.Count && _pending[i] != pending; i++)
        {
            if (_pending[i].Uid == pending.Uid && _pending[i].Star == pending.Star)
            {
                return true;
            }
        }

        return false;
    }

    private void Remove(PendingSummon pending)
    {
        _pending.Remove(pending);
    }

    private RectTransform FindSlotRect(PendingSummon pending)
    {
        return TryFindIndex(pending.Uid, pending.Star, out _, out var index) ? (RectTransform)_bagSlots[index].transform : null;
    }

    /// <summary>
    /// 상점 칸에서 산 포켓몬의 uid와 성을 얻는다.
    /// </summary>
    private static bool TryGetPurchased(int offerIndex, out int uid, out int star)
    {
        uid = 0;
        star = 0;
        if (!TryGetContext(out var itemManager, out var playerId))
        {
            return false;
        }

        var offers = itemManager.GetShopOffers(playerId);
        var item = offers != null && offerIndex >= 0 && offerIndex < offers.Count ? itemManager.CreateOfferItem(offers[offerIndex]) : null;
        if (item == null)
        {
            return false;
        }

        uid = item.uid;
        star = item.upgradeLevel;
        return true;
    }

    /// <summary>
    /// 포켓몬(uid, 성)이 지금 들어 있는 가방 칸을 찾는다.
    /// </summary>
    private bool TryFindIndex(int uid, int star, out InventoryHolder bag, out int index)
    {
        bag = null;
        index = -1;
        if (!TryGetContext(out var itemManager, out var playerId))
        {
            return false;
        }

        bag = itemManager.GetInventory(playerId);
        index = bag != null ? bag.FindIndex(uid, star) : -1;
        return index >= 0 && index < _bagSlots.Count && _bagSlots[index] != null;
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
