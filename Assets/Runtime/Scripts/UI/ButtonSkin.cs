using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼 그림 한 벌. Normal은 평소, Focus는 마우스를 올리거나 누를 때 그림이다.
/// </summary>
[Serializable]
public struct ButtonSkin
{
    public Sprite Normal;
    public Sprite Focus;

    /// <summary>
    /// 버튼에 지금 걸린 그림을 읽는다. Focus는 Sprite Swap의 Highlighted 그림이다.
    /// </summary>
    public static ButtonSkin Read(Button button)
    {
        return button != null && button.image != null
            ? new ButtonSkin { Normal = button.image.sprite, Focus = button.spriteState.highlightedSprite }
            : default;
    }

    /// <summary>
    /// 버튼 그림과 Sprite Swap의 Highlighted, Pressed 그림을 바꾼다. Normal이 비어 있으면 그대로 둔다.
    /// </summary>
    public void ApplyTo(Button button)
    {
        if (button == null || button.image == null || Normal == null)
        {
            return;
        }

        button.image.sprite = Normal;
        var state = button.spriteState;
        state.highlightedSprite = Focus;
        state.pressedSprite = Focus;
        button.spriteState = state;
    }
}
