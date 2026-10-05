using System;
using UnityEngine;

/// <summary>
/// 타입마다 화면에 그릴 이미지. 스프라이트 이름은 MonsterType 이름과 같게 둔다.
/// </summary>
[CreateAssetMenu(fileName = "MonsterTypeDatabase", menuName = "Monster/Monster Type Database")]
public class MonsterTypeDatabase : ScriptableObject
{
    [Serializable]
    private class TypeIcon
    {
        public MonsterType Type;
        public Sprite Icon;
    }

    [SerializeField] private TypeIcon[] _icons = Array.Empty<TypeIcon>();

    /// <summary>
    /// 타입 이미지. 없으면 null.
    /// </summary>
    public Sprite GetIcon(MonsterType type)
    {
        if (_icons == null)
        {
            return null;
        }

        for (var i = 0; i < _icons.Length; i++)
        {
            var entry = _icons[i];
            if (entry != null && entry.Type == type)
            {
                return entry.Icon;
            }
        }

        return null;
    }
}
