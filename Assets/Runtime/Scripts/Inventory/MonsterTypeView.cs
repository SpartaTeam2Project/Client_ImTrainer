using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 포켓몬 타입을 이미지 두 칸으로 그린다. 상점 칸과 정보 창이 같이 쓴다.
/// </summary>
public class MonsterTypeView : MonoBehaviour
{
    [SerializeField] private MonsterTypeDatabase _database;
    [SerializeField] private Image _primary;
    [SerializeField] private Image _secondary;

    /// <summary>
    /// 1타입을 켜고, 2타입이 있으면 2타입도 켠다. 정의가 없으면 둘 다 끈다.
    /// </summary>
    public void Show(MonsterVisualData visual)
    {
        if (visual == null)
        {
            Clear();
            return;
        }

        SetIcon(_primary, visual.PrimaryType, true);
        SetIcon(_secondary, visual.SecondaryType, visual.HasSecondaryType);
    }

    /// <summary>
    /// 두 칸을 모두 끈다.
    /// </summary>
    public void Clear()
    {
        SetIcon(_primary, MonsterType.Normal, false);
        SetIcon(_secondary, MonsterType.Normal, false);
    }

    private void SetIcon(Image image, MonsterType type, bool visible)
    {
        if (image == null)
        {
            return;
        }

        var icon = visible && _database != null ? _database.GetIcon(type) : null;
        image.sprite = icon;
        image.preserveAspect = true;
        image.gameObject.SetActive(icon != null);
    }
}
