using UnityEngine;

/// <summary>
/// 스토리지에 늘어놓을 플레이어블 캐릭터 목록.
/// </summary>
[CreateAssetMenu(fileName = "PlayableCharacterDatabase", menuName = "Player/Playable Character Database")]
public class PlayableCharacterDatabase : ScriptableObject
{
    [SerializeField] private PlayableCharacterData[] _characters = System.Array.Empty<PlayableCharacterData>();

    public PlayableCharacterData[] Characters => _characters ?? System.Array.Empty<PlayableCharacterData>();
}
