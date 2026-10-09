using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 중앙 아래 데이터 통계. 카운터 칸 뒤에 재화마다 획득과 보유 칸을 붙인다.
/// </summary>
public class StageDataPanel : MonoBehaviour
{
    private const string TITLE = "데이터 통계";

    [SerializeField] private TMP_Text _title;
    [SerializeField] private RectTransform _root;
    [SerializeField] private StageDataCellView _cellPrefab;

    private readonly StageViewList<StageDataCellView> _cells = new StageViewList<StageDataCellView>();
    private readonly List<(string label, string value)> _entries = new List<(string label, string value)>();
    private readonly List<string> _currencyIds = new List<string>();

    public void Bind(StageResult result)
    {
        if (_title != null)
        {
            _title.text = TITLE + " (" + StageResultFormat.Time(result.ElapsedSeconds) + ")";
        }

        _entries.Clear();
        var definitions = StageStatDefinitions.DataCells;
        for (var i = 0; i < definitions.Count; i++)
        {
            StageStatLookup.TryGet(result.Counters, definitions[i].Key, out var value);
            _entries.Add((definitions[i].Label, StageResultFormat.Value(value, definitions[i].Format)));
        }

        AddCurrencies(result);
        var cells = _cells.Ensure(_cellPrefab, _root, _entries.Count);
        for (var i = 0; i < _entries.Count && i < cells.Count; i++)
        {
            cells[i].Bind(_entries[i].label, _entries[i].value);
        }
    }

    /// <summary>
    /// 획득이나 보유가 하나라도 있는 재화만 적는다.
    /// </summary>
    private void AddCurrencies(StageResult result)
    {
        _currencyIds.Clear();
        CollectIds(result.CurrenciesGained);
        CollectIds(result.Currencies);
        for (var i = 0; i < _currencyIds.Count; i++)
        {
            var id = _currencyIds[i];
            var name = StageResultIcons.CurrencyName(id);
            _entries.Add((name + " 획득", StageStatLookup.GetAmount(result.CurrenciesGained, id).ToString()));
            _entries.Add((name + " 보유", StageStatLookup.GetAmount(result.Currencies, id).ToString()));
        }
    }

    private void CollectIds(CurrencyAmount[] amounts)
    {
        for (var i = 0; i < amounts.Length; i++)
        {
            if (amounts[i].Amount > 0 && !_currencyIds.Contains(amounts[i].CurrencyId))
            {
                _currencyIds.Add(amounts[i].CurrencyId);
            }
        }
    }
}
