using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 창 왼쪽의 구매 목록. 칸을 누르면 몬스터볼로 산다.
/// </summary>
public class ShopUi : MonoBehaviour
{
    private const int GRID_COLUMNS = 3;
    private static readonly Vector2 CELL_SIZE = new Vector2(108f, 132f);

    private readonly List<InventoryItem> _slots = new List<InventoryItem>();
    private RectTransform _content;
    private InventoryItem _prefab;
    private Action _onChanged;

    /// <summary>
    /// 구매 목록 영역을 만든다. 구매가 끝나면 onChanged를 호출한다.
    /// </summary>
    public void Build(RectTransform parent, InventoryItem prefab, Action onChanged)
    {
        if (_content != null)
        {
            return;
        }

        _prefab = prefab;
        _onChanged = onChanged;
        _content = CreateScroll(parent);
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

    private RectTransform CreateScroll(RectTransform parent)
    {
        var root = InventoryUi.CreateRect("Catalog", parent);
        root.anchorMin = new Vector2(0.02f, 0.18f);
        root.anchorMax = new Vector2(0.34f, 0.84f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        var scroll = root.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        var viewport = InventoryUi.CreateRect("Viewport", root);
        InventoryUi.Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        var viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0.08f, 0.09f, 0.12f, 0.9f);
        scroll.viewport = viewport;
        var content = InventoryUi.CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);
        var layout = content.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = CELL_SIZE;
        layout.spacing = new Vector2(8f, 8f);
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = GRID_COLUMNS;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;
        return content;
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
