using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 중앙 파티 칸 하나. 포켓몬 아이콘과 성.
/// </summary>
public class StagePartySlotView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Image[] _stars = new Image[Item.STAR_MAX];

    public void Bind(Sprite icon, int stars)
    {
        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }

        StageStarView.Show(_stars, stars);
    }

    public void Clear()
    {
        Bind(null, 0);
    }
}
