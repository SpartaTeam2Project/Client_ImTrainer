using UnityEngine;

public class GameController : MonoBehaviour
{
    private const string TITLE_MUSIC_NAME = "TitleScene";
    private const string MAIN_MUSIC_NAME = "MainScene";

    [Tooltip("타이틀에서 따로 고르지 않았을 때 시작할 스테이지")]
    [SerializeField] private StageData _defaultStage;

    private bool _clockPaused;
    private StageData _selectedStage;

    /// <summary>
    /// 다음 판에 진행할 스테이지. 고른 게 없으면 기본 스테이지.
    /// 로딩 씬이 게임 씬보다 먼저 알아야 프리로드할 수 있다.
    /// </summary>
    public StageData SelectedStage => _selectedStage != null ? _selectedStage : _defaultStage;

    /// <summary>
    /// 다음 판에 진행할 스테이지를 고른다.
    /// </summary>
    public void SelectStage(StageData stage)
    {
        _selectedStage = stage;
    }

    /// <summary>
    /// Playing 상태 동안 쌓인 초.
    /// </summary>
    public float ElapsedSeconds { get; private set; }

    /// <summary>
    /// 스테이지 진행 중에 쓰던 브금을 다시 튼다.
    /// </summary>
    public void PlayStageMusic()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlayMusic(MAIN_MUSIC_NAME);
    }

    /// <summary>
    /// 연출 동안 위쪽 경과 시간을 멈춘다.
    /// </summary>
    public void SetClockPaused(bool paused)
    {
        _clockPaused = paused;
    }

    /// <summary>
    /// 지금 게임 씬에 올라와 있는 스테이지. 없으면 null.
    /// </summary>
    public StageController ActiveStage { get; private set; }

    /// <summary>
    /// 게임 씬의 스테이지가 자신을 알린다.
    /// </summary>
    public void RegisterStage(StageController stage)
    {
        ActiveStage = stage;
    }

    /// <summary>
    /// 같은 스테이지가 빠질 때만 참조를 지운다.
    /// </summary>
    public void UnregisterStage(StageController stage)
    {
        if (ActiveStage == stage)
        {
            ActiveStage = null;
        }
    }

    private void Start()
    {
        if (Managers.Instance == null)
        {
            return;
        }

        Managers.Instance.OnGameStateChanged += HandleGameStateChanged;
        PlayMusicForState(GameState.Boot, Managers.Instance.CurrentState);
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
        if (_clockPaused || Managers.Instance == null || !Managers.Instance.IsSimulationRunning)
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

        if (newState == GameState.Loading || newState == GameState.Menu)
        {
            Time.timeScale = 1f;
        }

        PlayMusicForState(previousState, newState);
    }

    private void PlayMusicForState(GameState previousState, GameState state)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        // 프리팹에 붙은 AudioSource는 timeScale을 무시하므로 리스너 단위로 멈춘다.
        AudioListener.pause = state == GameState.Paused;

        if (state == GameState.Paused)
        {
            audioManager.PauseMusic();
            return;
        }

        if (previousState == GameState.Paused && state == GameState.Playing)
        {
            audioManager.ResumeMusic();
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
            case GameState.Victory:
                // 클리어 전용 BGM이 생기면 여기서 PlayMusic으로 바꾼다.
                audioManager.StopMusic();
                break;
        }
    }
}
