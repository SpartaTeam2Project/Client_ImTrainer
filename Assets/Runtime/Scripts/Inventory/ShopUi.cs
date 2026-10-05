using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인벤토리 창 왼쪽의 구매 목록. 칸을 누르면 몬스터볼로 산다.
/// </summary>
public class ShopUi : MonoBehaviour
{
    [SerializeField] private RectTransform _content;
    [SerializeField] private InventoryItem _prefab;

    private readonly List<InventoryItem> _slots = new List<InventoryItem>();
    private Action _onChanged;

    /// <summary>
    /// 구매가 끝나면 onChanged를 호출한다.
    /// </summary>
    public void Bind(Action onChanged)
    {
        _onChanged = onChanged;
        if (_prefab == null)
        {
            Debug.LogError("상점 칸 프리팹이 없습니다.");
        }
    }

    /// <summary>
    /// 종 목록과 가격을 다시 그린다.
    /// </summary>
    public void Refresh(int playerId)
    {
        if (_content == null || Managers.Instance == null || !Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            return;
        }

        EnsureSlots(itemManager, playerId);
        var icon = MonsterBallIcon();
        var slotIndex = 0;
        for (var i = 0; i < itemManager.Items.Count; i++)
        {
            var item = itemManager.Items[i];
            if (item == null || slotIndex >= _slots.Count)
            {
                continue;
            }

            var visual = itemManager.GetVisual(item.uid);
            var portrait = visual != null ? MonsterVisualData.FirstFrame(visual.Icon) : null;
            var slot = _slots[slotIndex];
            slot.ShowPokemon(portrait, item.name, Item.STAR_MIN, 1, false, false);
            slot.ShowPrice(icon, item.price);
            slotIndex++;
        }
    }

    private void EnsureSlots(ItemManager itemManager, int playerId)
    {
        if (_prefab == null || _content == null)
        {
            return;
        }

        var count = 0;
        for (var i = 0; i < itemManager.Items.Count; i++)
        {
            if (itemManager.Items[i] != null)
            {
                count++;
            }
        }

        if (_slots.Count == count)
        {
            return;
        }

        ClearSlots();
        for (var i = 0; i < itemManager.Items.Count; i++)
        {
            var item = itemManager.Items[i];
            if (item == null)
            {
                continue;
            }

            var uid = item.uid;
            var slot = Instantiate(_prefab, _content);
            slot.Button.onClick.AddListener(() => Purchase(playerId, uid));
            _slots.Add(slot);
        }
    }

    private void Purchase(int playerId, int uid)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            return;
        }

        itemManager.TryPurchase(playerId, uid);
        _onChanged?.Invoke();
    }

    private static Sprite MonsterBallIcon()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies))
        {
            return null;
        }

        return currencies.GetIcon(CurrenciesManager.MONSTER_BALL_ID);
    }

    private void ClearSlots()
    {
        for (var i = 0; i < _slots.Count; i++)
        {
            if (_slots[i] != null)
            {
                Destroy(_slots[i].gameObject);
            }
        }

        _slots.Clear();
    }
}
