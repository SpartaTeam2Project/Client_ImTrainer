using UnityEngine;

public class GameController : MonoBehaviour
{
    private const string TITLE_MUSIC_NAME = "TitleScene";
    private const string MAIN_MUSIC_NAME = "MainScene";

    /// <summary>
    /// Playing 상태 동안 쌓인 초.
    /// </summary>
    public float ElapsedSeconds { get; private set; }

    private void Start()
    {
        if (Managers.Instance == null)
        {
            return;
        }

        Managers.Instance.OnGameStateChanged += HandleGameStateChanged;
        PlayMusicForState(Managers.Instance.CurrentState);
    }

    private void OnDestroy()
    {
        if (Managers.Instance == null)
        {
            return;
        }

        Managers.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void Update()
    {
        if (Managers.Instance == null || !Managers.Instance.IsSimulationRunning)
        {
            return;
        }

        ElapsedSeconds += Time.deltaTime;
    }

    /// <summary>
    /// 로딩이 끝나고 게임에 들어오면 경과 시간을 다시 센다. 메뉴와 전투 음악을 고른다.
    /// </summary>
    private void HandleGameStateChanged(GameState previousState, GameState newState)
    {
        if (previousState == GameState.Loading && newState == GameState.Playing)
        {
            ElapsedSeconds = 0f;
        }

        PlayMusicForState(newState);
    }

    private void PlayMusicForState(GameState state)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        switch (state)
        {
            case GameState.Menu:
                audioManager.PlayMusic(TITLE_MUSIC_NAME);
                break;
            case GameState.Playing:
                audioManager.PlayMusic(MAIN_MUSIC_NAME);
                break;
        }
    }
}
