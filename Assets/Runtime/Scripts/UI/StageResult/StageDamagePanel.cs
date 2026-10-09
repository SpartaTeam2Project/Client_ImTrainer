using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 오른쪽 피해 통계. 포켓몬 개체별 피해와 DPS를 많은 순으로 그린다.
/// R키로 판 전체와 최근 60초를 바꾼다. 포켓몬에 귀속되지 않은 줄은 맨 뒤다.
/// </summary>
public class StageDamagePanel : MonoBehaviour
{
    private const string WHOLE_LABEL = "판 전체  [R]";
    private const string RECENT_LABEL = "최근 1분  [R]";

    [SerializeField] private TMP_Text _modeText;
    [SerializeField] private RectTransform _root;
    [SerializeField] private StageDamageRowView _rowPrefab;
    [SerializeField] private WeaponAbilitiesDatabase _abilities;

    private readonly StageViewList<StageDamageRowView> _rows = new StageViewList<StageDamageRowView>();
    private readonly List<StageUnitDamage> _sorted = new List<StageUnitDamage>();
    private StageResult _result;
    private bool _recent;

    private void Update()
    {
        if (_result == null || Managers.Instance == null || !Managers.Instance.TryGetManager<InputManager>(out var input))
        {
            return;
        }

        if (input.ConsumeDamageWindowToggle())
        {
            _recent = !_recent;
            Redraw();
        }
    }

    public void Bind(StageResult result)
    {
        _result = result;
        _recent = false;
        Redraw();
    }

    private void Redraw()
    {
        if (_modeText != null)
        {
            _modeText.text = _recent ? RECENT_LABEL : WHOLE_LABEL;
        }

        _sorted.Clear();
        _sorted.AddRange(_result.UnitDamages);
        _sorted.Sort(Compare);
        var window = _recent ? Mathf.Min(RecentDamageBuffer.WINDOW_SECONDS, _result.ElapsedSeconds) : _result.ElapsedSeconds;
        var rows = _rows.Ensure(_rowPrefab, _root, _sorted.Count);
        for (var i = 0; i < _sorted.Count && i < rows.Count; i++)
        {
            var unit = _sorted[i];
            var damage = GetDamage(unit);
            var seconds = _recent ? unit.RecentSeconds : unit.SecondsInParty;
            rows[i].Bind(ResolveIcon(unit), unit.UpgradeLevel, damage, StageResultFormat.PerSecond(damage, seconds),
                StageResultFormat.Ratio(seconds, window), !unit.InPartyAtEnd);
        }
    }

    private int Compare(StageUnitDamage a, StageUnitDamage b)
    {
        var aUnattributed = a.MemberId == DamageSource.NO_MEMBER;
        var bUnattributed = b.MemberId == DamageSource.NO_MEMBER;
        if (aUnattributed != bUnattributed)
        {
            return aUnattributed ? 1 : -1;
        }

        return GetDamage(b).CompareTo(GetDamage(a));
    }

    private float GetDamage(StageUnitDamage unit)
    {
        return _recent ? unit.RecentDamage : unit.TotalDamage;
    }

    private Sprite ResolveIcon(StageUnitDamage unit)
    {
        return unit.MemberId != DamageSource.NO_MEMBER
            ? StageResultIcons.Monster(unit.Uid)
            : StageResultIcons.Ability(_abilities, unit.AbilityType);
    }
}
