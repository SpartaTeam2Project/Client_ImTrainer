using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 창의 합성, 장착, 해제, 판매 버튼과 휴지통 연출. 누를 때 눌리는 연출과 소리를 내고,
/// 키보드로 합성 대상을 고르는 동안에는 합성, 해제 버튼을 확정, 취소로 바꾸고 장착 버튼을 숨긴다. InventoryUi가 만든다.
/// </summary>
public sealed class InventoryActionBar
{
    private const string PRESS_SOUND = "cursor";
    private const string CONFIRM_LABEL = "확정";
    private const string CANCEL_LABEL = "취소";
    // 확정, 취소로 바뀔 때 글자를 튕겨서 눈에 띄게 한다.
    private const float LABEL_PUNCH_SCALE = 0.3f;
    private const float LABEL_PUNCH_DURATION = 0.3f;
    private const int LABEL_PUNCH_VIBRATO = 8;
    private const float LABEL_PUNCH_ELASTICITY = 0.8f;

    private readonly Button _synthesizeButton;
    private readonly Button _equipButton;
    private readonly Button _unequipButton;
    private readonly Button _sellButton;
    private readonly InventoryDropZone _trash;
    private readonly ButtonSkin _confirmSkin;
    private readonly ButtonSkin _cancelSkin;
    private readonly PressFlash _synthesizePress;
    private readonly PressFlash _equipPress;
    private readonly PressFlash _unequipPress;
    private readonly PressFlash _sellPress;
    private readonly TMP_Text _synthesizeLabel;
    private readonly TMP_Text _unequipLabel;
    private readonly string _synthesizeText;
    private readonly string _unequipText;
    private readonly ButtonSkin _synthesizeSkin;
    private readonly ButtonSkin _unequipSkin;
    private bool _picking;

    public InventoryActionBar(Button synthesize, Button equip, Button unequip, Button sell, InventoryDropZone trash,
        ButtonSkin confirmSkin, ButtonSkin cancelSkin)
    {
        _synthesizeButton = synthesize;
        _equipButton = equip;
        _unequipButton = unequip;
        _sellButton = sell;
        _trash = trash;
        _confirmSkin = confirmSkin;
        _cancelSkin = cancelSkin;
        _synthesizePress = PressFlash.ForButton(synthesize);
        _equipPress = PressFlash.ForButton(equip);
        _unequipPress = PressFlash.ForButton(unequip);
        _sellPress = PressFlash.ForButton(sell);
        _synthesizeLabel = FindLabel(synthesize, out _synthesizeText);
        _unequipLabel = FindLabel(unequip, out _unequipText);
        _synthesizeSkin = ButtonSkin.Read(synthesize);
        _unequipSkin = ButtonSkin.Read(unequip);
    }

    /// <summary>
    /// 버튼을 누르면 눌리는 연출과 소리를 낸 뒤 동작한다.
    /// </summary>
    public void Bind(Action synthesize, Action equip, Action unequip, Action sell)
    {
        AddAction(_synthesizeButton, () => Press(InventoryHotkey.Synthesize, false, synthesize));
        AddAction(_equipButton, () => Press(InventoryHotkey.Equip, false, equip));
        AddAction(_unequipButton, () => Press(InventoryHotkey.Unequip, false, unequip));
        AddAction(_sellButton, () => Press(InventoryHotkey.Sell, false, sell));
    }

    /// <summary>
    /// 단축키가 맡은 버튼을 잠깐 눌린 그림으로 보여 준다. 판매 단축키는 휴지통이 반응한다. 동작보다 먼저 부른다.
    /// </summary>
    public void PlayHotkey(InventoryHotkey hotkey)
    {
        Press(hotkey, true, null);
    }

    /// <summary>
    /// 합성 대상을 고르는 중이면 확정, 취소 글자와 그림으로 바꾸고 장착 버튼을 숨긴다. 바뀔 때만 바꾼다.
    /// </summary>
    public void SetPickMode(bool picking)
    {
        if (picking == _picking)
        {
            return;
        }

        _picking = picking;
        SetLabel(_synthesizeLabel, picking ? CONFIRM_LABEL : _synthesizeText, picking);
        SetLabel(_unequipLabel, picking ? CANCEL_LABEL : _unequipText, picking);
        // 단축키로 덮어 둔 눌린 그림이 남으면 새 그림이 늦게 보여서 바로 걷는다.
        _synthesizePress.ClearSprite();
        _unequipPress.ClearSprite();
        (picking ? _confirmSkin : _synthesizeSkin).ApplyTo(_synthesizeButton);
        (picking ? _cancelSkin : _unequipSkin).ApplyTo(_unequipButton);
        if (_equipButton != null)
        {
            _equipButton.gameObject.SetActive(!picking);
        }
    }

    /// <summary>
    /// 남은 눌림 연출을 바로 걷는다. 창이 꺼질 때 부른다.
    /// </summary>
    public void Release()
    {
        _synthesizePress.Release();
        _equipPress.Release();
        _unequipPress.Release();
        _sellPress.Release();
    }

    /// <summary>
    /// 누르면 cursor 소리를 낸다. 합성 확정은 키보드가 결과에 따라 select나 error를, 판매는 판매 쪽이 결과에 따라 소리를 낸다.
    /// </summary>
    private void Press(InventoryHotkey hotkey, bool fromHotkey, Action action)
    {
        var sound = PRESS_SOUND;
        switch (hotkey)
        {
            case InventoryHotkey.Synthesize:
                Play(_synthesizePress, fromHotkey);
                if (_picking)
                {
                    sound = null;
                }

                break;
            case InventoryHotkey.Equip:
                Play(_equipPress, fromHotkey);
                break;
            case InventoryHotkey.Unequip:
                Play(_unequipPress, fromHotkey);
                break;
            case InventoryHotkey.Sell:
                if (fromHotkey && _trash != null)
                {
                    _trash.PlayPress();
                }
                else
                {
                    Play(_sellPress, fromHotkey);
                }

                sound = null;
                break;
        }

        UiSound.Play(sound);
        action?.Invoke();
    }

    /// <summary>
    /// 마우스로 누르면 버튼이 눌린 그림을 직접 보여 주니 눌리는 연출만, 단축키면 눌린 그림도 잠깐 덮는다.
    /// </summary>
    private static void Play(PressFlash press, bool fromHotkey)
    {
        if (fromHotkey)
        {
            press.Play();
        }
        else
        {
            press.Punch();
        }
    }

    private static void AddAction(Button button, Action action)
    {
        if (button == null)
        {
            Debug.LogError("인벤토리 창 버튼이 없습니다.");
            return;
        }

        button.onClick.AddListener(() => action());
    }

    private static TMP_Text FindLabel(Button button, out string text)
    {
        var label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        text = label != null ? label.text : null;
        return label;
    }

    /// <summary>
    /// 글자가 바뀌면 바꾸고, punch면 튕기는 연출을 건다. 창이 열려 있는 동안 시간이 멈춰 있어서 시간 정지와 상관없이 돈다.
    /// </summary>
    private static void SetLabel(TMP_Text label, string text, bool punch)
    {
        if (label == null || label.text == text)
        {
            return;
        }

        label.text = text;
        if (!punch)
        {
            return;
        }

        // 연달아 바뀌어도 크기가 어긋나지 않게 이전 연출을 끝낸 크기에서 다시 건다.
        label.transform.DOKill(true);
        label.transform
            .DOPunchScale(Vector3.one * LABEL_PUNCH_SCALE, LABEL_PUNCH_DURATION, LABEL_PUNCH_VIBRATO, LABEL_PUNCH_ELASTICITY)
            .SetUpdate(true)
            .SetLink(label.gameObject);
    }
}
