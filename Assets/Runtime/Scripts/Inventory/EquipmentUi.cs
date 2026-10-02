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
    private const float ROW_HEIGHT = 42f;

    private readonly List<Button> _slots = new List<Button>();
    private RectTransform _root;
    private Action<int> _onSelect;

    /// <summary>
    /// 장착 칸을 그릴 영역을 만든다.
    /// </summary>
    public void Build(RectTransform parent)
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

        var title = InventoryUi.CreateText("Title", _root, "장착", 28);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -8f);
        title.rectTransform.sizeDelta = new Vector2(0f, 36f);

        for (var i = 0; i < WeaponSlots.MAX_COUNT; i++)
        {
            var slot = i;
            var button = CreateSlot(_root, i);
            button.onClick.AddListener(() => _onSelect?.Invoke(slot));
            _slots.Add(button);
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
            var label = _slots[i].GetComponentInChildren<TextMeshProUGUI>();
            var text = SlotName(i) + "  비어 있음";
            if (equipment != null && i < equipment.Stacks.Count && !equipment.Stacks[i].Empty && equipment.Stacks[i].Item != null)
            {
                var item = equipment.Stacks[i].Item;
                text = SlotName(i) + "  " + item.name + " " + item.upgradeLevel + "성";
            }

            if (label != null)
            {
                label.text = text;
            }

            var image = _slots[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = i == selectedSlot
                    ? new Color(0.25f, 0.45f, 0.3f, 1f)
                    : new Color(0.16f, 0.18f, 0.22f, 1f);
            }
        }
    }

    private Button CreateSlot(RectTransform parent, int index)
    {
        var rect = InventoryUi.CreateRect("Slot " + index, parent);
        rect.anchorMin = new Vector2(0.04f, 1f);
        rect.anchorMax = new Vector2(0.96f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, ROW_HEIGHT);
        rect.anchoredPosition = new Vector2(0f, -48f - index * (ROW_HEIGHT + 6f));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.18f, 0.22f, 1f);
        var button = rect.gameObject.AddComponent<Button>();
        var label = InventoryUi.CreateText("Label", rect, string.Empty, 20);
        InventoryUi.Stretch(label.rectTransform);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.margin = new Vector4(12f, 4f, 12f, 4f);
        return button;
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
