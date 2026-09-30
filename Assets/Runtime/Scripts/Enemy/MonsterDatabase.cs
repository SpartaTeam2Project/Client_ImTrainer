using UnityEngine;

/// <summary>
/// 스토리지에 늘어놓을 포켓몬 목록. 폴더의 전체 종이 아니라 여기 넣은 종만 보여 준다.
/// </summary>
[CreateAssetMenu(fileName = "MonsterDatabase", menuName = "Monster/Monster Database")]
public class MonsterDatabase : ScriptableObject
{
    [SerializeField] private MonsterVisualData[] _monsters = System.Array.Empty<MonsterVisualData>();

    public MonsterVisualData[] Monsters => _monsters ?? System.Array.Empty<MonsterVisualData>();
}
