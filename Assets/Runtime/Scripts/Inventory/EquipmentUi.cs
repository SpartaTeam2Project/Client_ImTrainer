using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 장착 칸 여섯 개. 칸을 누르면 선택한다.
/// </summary>
public class EquipmentUi : MonoBehaviour
{
    private const float SLOT_SCALE = 0.72f;
    private const float ROW_STEP = 108f;

    private readonly List<InventoryItem> _slots = new List<InventoryItem>();
    private RectTransform _root;
    private Action<int> _onSelect;

    /// <summary>
    /// 장착 칸을 그릴 영역을 만든다.
    /// </summary>
    public void Build(RectTransform parent, InventoryItem prefab)
    {
        if (_root != null)
        {
            return;
        }

        _root = InventoryUi.CreateRect("Equipment", parent);
        _root.anchorMin = new Vector2(0.72f, 0.12f);
        _root.anchorMax = new Vector2(0.98f, 0.84f);
        _root.offsetMin = Vector2.zero;
        _root.offsetMax = Vector2.zero;
        var background = _root.gameObject.AddComponent<Image>();
        background.color = new Color(0.1f, 0.12f, 0.16f, 0.95f);
        background.raycastTarget = false;

        var title = InventoryUi.CreateText("Title", _root, "장착", 28);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -8f);
        title.rectTransform.sizeDelta = new Vector2(0f, 36f);

        if (prefab == null)
        {
            Debug.LogError("장착 칸 프리팹이 없습니다.");
            return;
        }

        for (var i = 0; i < WeaponSlots.MAX_COUNT; i++)
        {
            var slotIndex = i;
            var row = InventoryUi.CreateRect("Row " + i, _root);
            row.anchorMin = new Vector2(0.5f, 1f);
            row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(220f, ROW_STEP);
            row.anchoredPosition = new Vector2(0f, -48f - i * ROW_STEP);

            var label = InventoryUi.CreateText("Clock", row, SlotName(i), 18);
            label.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            label.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            label.rectTransform.pivot = new Vector2(0f, 0.5f);
            label.rectTransform.anchoredPosition = new Vector2(8f, 0f);
            label.rectTransform.sizeDelta = new Vector2(52f, 32f);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            var slot = Instantiate(prefab, row);
            var slotRect = slot.transform as RectTransform;
            if (slotRect != null)
            {
                slotRect.anchorMin = new Vector2(1f, 0.5f);
                slotRect.anchorMax = new Vector2(1f, 0.5f);
                slotRect.pivot = new Vector2(1f, 0.5f);
                slotRect.anchoredPosition = new Vector2(-8f, 0f);
                slotRect.localScale = new Vector3(SLOT_SCALE, SLOT_SCALE, 1f);
            }

            slot.Button.onClick.AddListener(() => _onSelect?.Invoke(slotIndex));
            _slots.Add(slot);
        }
    }

    /// <summary>
    /// 장착 목록을 다시 그린다.
    /// </summary>
    public void Refresh(int playerId, int selectedSlot, Action<int> onSelect)
    {
        _onSelect = onSelect;
        if (_root == null || Managers.Instance == null || !Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            return;
        }

        var equipment = itemManager.GetEquipment(playerId);
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
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

    private static string SlotName(int index)
    {
        switch ((WeaponSlot)index)
        {
            case WeaponSlot.Hour1:
                return "1시";
            case WeaponSlot.Hour3:
                return "3시";
            case WeaponSlot.Hour5:
                return "5시";
            case WeaponSlot.Hour7:
                return "7시";
            case WeaponSlot.Hour9:
                return "9시";
            default:
                return "11시";
        }
    }
}
