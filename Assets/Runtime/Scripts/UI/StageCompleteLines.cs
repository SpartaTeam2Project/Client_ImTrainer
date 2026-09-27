using System;
using UnityEngine;

/// <summary>
/// 클리어 화면에 띄울 대사 목록. 기획자가 항목을 추가한다.
/// </summary>
[CreateAssetMenu(fileName = "StageCompleteLines", menuName = "UI/Stage Complete Lines")]
public class StageCompleteLines : ScriptableObject
{
    [TextArea(1, 3)]
    [SerializeField] private string[] _lines = Array.Empty<string>();

    /// <summary>
    /// 비어 있지 않은 대사 중 하나를 고른다.
    /// </summary>
    public bool TryPick(out string line)
    {
        line = null;
        var count = CountFilled();
        if (count == 0)
        {
            return false;
        }

        var pick = UnityEngine.Random.Range(0, count);
        for (var i = 0; i < _lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(_lines[i]))
            {
                continue;
            }

            if (pick == 0)
            {
                line = _lines[i];
                return true;
            }

            pick--;
        }

        return false;
    }

    private int CountFilled()
    {
        if (_lines == null)
        {
            return 0;
        }

        var count = 0;
        for (var i = 0; i < _lines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(_lines[i]))
            {
                count++;
            }
        }

        return count;
    }
}
