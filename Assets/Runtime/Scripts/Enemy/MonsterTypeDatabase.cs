using System;
using UnityEngine;

/// <summary>
/// 타입마다 화면에 그릴 이미지와 색. 스프라이트 이름은 MonsterType 이름과 같게 둔다.
/// </summary>
[CreateAssetMenu(fileName = "MonsterTypeDatabase", menuName = "Monster/Monster Type Database")]
public class MonsterTypeDatabase : ScriptableObject
{
    [Serializable]
    private class TypeEntry
    {
        public MonsterType Type;
        public Sprite Icon;
        public Color Color = Color.white;
    }

    [SerializeField] private TypeEntry[] _entries = Array.Empty<TypeEntry>();

    /// <summary>
    /// 타입 이미지. 없으면 null.
    /// </summary>
    public Sprite GetIcon(MonsterType type)
    {
        var entry = Find(type);
        return entry != null ? entry.Icon : null;
    }

    /// <summary>
    /// 타입 색. 없으면 false.
    /// </summary>
    public bool TryGetColor(MonsterType type, out Color color)
    {
        var entry = Find(type);
        color = entry != null ? entry.Color : Color.white;
        return entry != null;
    }

    private TypeEntry Find(MonsterType type)
    {
        if (_entries == null)
        {
            return null;
        }

        for (var i = 0; i < _entries.Length; i++)
        {
            var entry = _entries[i];
            if (entry != null && entry.Type == type)
            {
                return entry;
            }
        }

        return null;
    }
}
