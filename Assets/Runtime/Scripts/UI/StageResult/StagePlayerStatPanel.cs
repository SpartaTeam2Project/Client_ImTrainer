using UnityEngine;

/// <summary>
/// 왼쪽 플레이어 스탯 목록. StageStatDefinitions.PlayerStats 순서로 그린다.
/// </summary>
public class StagePlayerStatPanel : MonoBehaviour
{
    [SerializeField] private RectTransform _root;
    [SerializeField] private StageStatRowView _rowPrefab;
    [SerializeField] private WeaponAbilitiesDatabase _abilities;

    private readonly StageViewList<StageStatRowView> _rows = new StageViewList<StageStatRowView>();

    public void Bind(StageResult result)
    {
        var definitions = StageStatDefinitions.PlayerStats;
        var rows = _rows.Ensure(_rowPrefab, _root, definitions.Count);
        for (var i = 0; i < definitions.Count && i < rows.Count; i++)
        {
            var definition = definitions[i];
            StageStatLookup.TryGet(result.PlayerStats, definition.Key, out var value);
            var icon = definition.HasIcon ? StageResultIcons.Ability(_abilities, definition.IconAbility) : null;
            rows[i].Bind(icon, definition.Label, StageResultFormat.Value(value, definition.Format));
        }
    }
}
