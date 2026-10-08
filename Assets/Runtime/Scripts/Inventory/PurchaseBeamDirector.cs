using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점에서 사면 산 포켓몬이 들어간 가방 칸을 숨겨 두고, 볼이 열리면 그 칸으로 광선을 쏜다.
/// 광선이 닿으면 칸에 빨간 실루엣을 띄운다. InventoryUi가 만들어 ShopUi에 묶는다.
/// </summary>
public sealed class PurchaseBeamDirector : IShopPurchaseEffect
{
    private const string BEAM_SOUND = "pb_beam";

    private readonly PurchaseBeam _beam;
    private readonly IReadOnlyList<InventoryItem> _bagSlots;
    private readonly InventoryFocusNavigator _navigator;

    public PurchaseBeamDirector(PurchaseBeam beam, IReadOnlyList<InventoryItem> bagSlots, InventoryFocusNavigator navigator)
    {
        _beam = beam;
        _bagSlots = bagSlots;
        _navigator = navigator;
    }

    /// <summary>
    /// 새로 생긴 칸이면 광선 연출이 끝날 때까지 아이콘과 별을 숨긴다.
    /// 이미 있는 포켓몬에 쌓였으면 그대로 두고, 광선이 닿을 때 번쩍이기만 한다.
    /// </summary>
    public void OnPurchased(int offerIndex)
    {
        if (_beam == null || !TryFindSlot(offerIndex, out var bag, out var index, out _, out _))
        {
            return;
        }

        _bagSlots[index].Summon.Begin(bag.Stacks[index].Number <= 1);
    }

    /// <summary>
    /// 산 포켓몬의 가방 칸으로 ball에서 광선을 쏜다. 칸이 스크롤 밖이면 먼저 보이게 옮긴다.
    /// </summary>
    public void OnBallOpened(int offerIndex, RectTransform ball)
    {
        if (_beam == null || ball == null || !TryFindSlot(offerIndex, out _, out var index, out var uid, out var star))
        {
            return;
        }

        if (!TryGetContext(out _, out var playerId))
        {
            return;
        }

        _navigator.ScrollTo(InventorySlotDrag.SlotKind.Bag, index);
        PlaySound(BEAM_SOUND);
        _beam.Play(ball, (RectTransform)_bagSlots[index].transform, () => Arrive(playerId, uid, star));
    }

    /// <summary>
    /// 상점 칸에서 산 포켓몬이 들어간 가방 칸을 찾는다.
    /// </summary>
    private bool TryFindSlot(int offerIndex, out InventoryHolder bag, out int index, out int uid, out int star)
    {
        bag = null;
        index = -1;
        uid = 0;
        star = 0;
        if (!TryGetContext(out var itemManager, out var playerId))
        {
            return false;
        }

        var offers = itemManager.GetShopOffers(playerId);
        var item = offers != null && offerIndex >= 0 && offerIndex < offers.Count ? itemManager.CreateOfferItem(offers[offerIndex]) : null;
        bag = itemManager.GetInventory(playerId);
        if (item == null || bag == null)
        {
            return false;
        }

        uid = item.uid;
        star = item.upgradeLevel;
        index = bag.FindIndex(uid, star);
        return index >= 0 && index < _bagSlots.Count && _bagSlots[index] != null;
    }

    /// <summary>
    /// 창이 닫히면 광선을 지우고 숨겨 둔 아이콘을 되돌린다.
    /// </summary>
    public void Stop()
    {
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

    /// <summary>
    /// 날아가는 사이 다른 구매로 가방이 다시 정렬됐을 수 있어서 칸을 다시 찾는다.
    /// </summary>
    private void Arrive(int playerId, int uid, int star)
    {
        if (!TryGetContext(out var itemManager, out _))
        {
            return;
        }

        var bag = itemManager.GetInventory(playerId);
        var index = bag != null ? bag.FindIndex(uid, star) : -1;
        if (index >= 0 && index < _bagSlots.Count && _bagSlots[index] != null)
        {
            _bagSlots[index].Summon.Play();
        }
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

    private static void PlaySound(string name)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(name);
    }
}
