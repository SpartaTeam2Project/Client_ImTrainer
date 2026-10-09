using TMPro;
using UnityEngine;

/// <summary>
/// 데이터 통계 칸 하나. 이름과 값.
/// </summary>
public class StageDataCellView : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;
    [SerializeField] private TMP_Text _value;

    public void Bind(string label, string value)
    {
        if (_label != null)
        {
            _label.text = label;
        }

        if (_value != null)
        {
            _value.text = value;
        }
    }
}
