using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 피해 통계 한 줄. 아이콘, 성, 피해, DPS, 파티에 있던 비율 바.
/// </summary>
public class StageDamageRowView : MonoBehaviour
{
    private const float DIMMED_ALPHA = 0.5f;

    [SerializeField] private CanvasGroup _group;
    [SerializeField] private Image _icon;
    [SerializeField] private Image[] _stars = new Image[Item.STAR_MAX];
    [SerializeField] private TMP_Text _damage;
    [SerializeField] private TMP_Text _dps;
    [SerializeField] private TMP_Text _percent;
    [SerializeField] private Image _presenceFill;

    /// <summary>
    /// dimmed면 판이 끝날 때 파티에 없던 포켓몬이라 흐리게 그린다.
    /// </summary>
    public void Bind(Sprite icon, int stars, float damage, float dps, float presence, bool dimmed)
    {
        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }

        StageStarView.Show(_stars, stars);
        if (_damage != null)
        {
            _damage.text = StageResultFormat.Abbreviate(damage);
        }

        if (_dps != null)
        {
            _dps.text = StageResultFormat.Abbreviate(dps) + "/s";
        }

        if (_percent != null)
        {
            _percent.text = Mathf.RoundToInt(presence * 100f) + "%";
        }

        if (_presenceFill != null)
        {
            _presenceFill.fillAmount = presence;
        }

        if (_group != null)
        {
            _group.alpha = dimmed ? DIMMED_ALPHA : 1f;
        }
    }
}
