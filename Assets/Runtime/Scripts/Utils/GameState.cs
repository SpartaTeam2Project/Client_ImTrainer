/// <summary>
/// 한 판의 진행 상태
/// </summary>
public enum GameState
{
    Boot,
    Menu,
    Loading,
    Playing,
    LevelUp,
    Paused,
    Victory,
    Defeat
}

/// <summary>
/// 진행 상태가 바뀌었을 때의 이전 값과 다음 값.
/// </summary>
[System.Serializable]
public struct GameStateChanged
{
    public GameState Previous;
    public GameState Next;

    public GameStateChanged(GameState previous, GameState next)
    {
        Previous = previous;
        Next = next;
    }
}
