using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 게임 씬에서 한 판의 시작, 일시정지, 타임라인 클리어, 사망 패배를 맡는다.
/// </summary>
public class StageController : MonoBehaviour
{
    [SerializeField] private StageData _stageData;
    [SerializeField] private PlayableDirector _director;

    private GameController _gameController;
    private bool _stageActive;
    private bool _deathSubscribed;
    private bool _missingStageLogged;
    private bool _missingTimelineLogged;
    private bool _directorHooked;
    private bool _acceptDirectorStop;
    private bool _bossTimelinePaused;
    private bool _manualDirector;
    private double _bossPausedTime;

    public StageResult LastResult { get; private set; }

    private void OnEnable()
    {
        if (Managers.Instance == null)
        {
            return;
        }

        _gameController = Managers.Instance.GetComponent<GameController>();
        if (_gameController != null)
        {
            _gameController.RegisterStage(this);
        }

        Managers.Instance.OnGameStateChanged += HandleGameStateChanged;
        RegisterFieldRoot();
    }

    private void Start()
    {
        RegisterFieldRoot();
    }

    private void OnDisable()
    {
        if (_gameController != null)
        {
            _gameController.UnregisterStage(this);
        }

        if (Managers.Instance != null)
        {
            Managers.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        Time.timeScale = 1f;
        _acceptDirectorStop = false;
        UnsubscribeDirector();
        AbandonBossTimeline();
        BossArenaPlayback.Cancel();
        _stageActive = false;
        UnsubscribeDeath();
        if (Managers.Instance != null && Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager))
        {
            enemyManager.ClearStage();
        }

        ClearFieldRoot();
    }

    private void Update()
    {
        if (Managers.Instance == null)
        {
            return;
        }

        var state = Managers.Instance.CurrentState;
        if (state == GameState.Playing && !_stageActive)
        {
            StartStage();
        }

        if (_manualDirector && _stageActive && !_bossTimelinePaused && state == GameState.Playing)
        {
            TickManualDirector();
        }

        if (state == GameState.Playing && _stageActive)
        {
            TickStage();
        }
    }

    private void LateUpdate()
    {
        if (Managers.Instance == null)
        {
            return;
        }

        TogglePause(Managers.Instance.CurrentState);
    }

    private void StartStage()
    {
        if (!Managers.Instance.TryGetManager<PlayerManager>(out var playerManager)
            || !Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager))
        {
            return;
        }

        if (_stageData == null)
        {
            if (!_missingStageLogged)
            {
                _missingStageLogged = true;
                Debug.LogError("StageController에 StageData가 없습니다.");
            }

            return;
        }

        Time.timeScale = 1f;
        LastResult = null;
        if (Managers.Instance.TryGetManager<StageFieldManager>(out var fieldManager))
        {
            fieldManager.Begin(_stageData);
        }

        var playerId = playerManager.SpawnLocal();
        if (Managers.Instance.TryGetManager<CurrenciesManager>(out var currenciesManager))
        {
            currenciesManager.ClearStage(playerId);
        }

        enemyManager.BeginStage(playerId, _stageData, transform);

        if (Managers.Instance.TryGetManager<UpgradeManager>(out var upgradeManager))
        {
            upgradeManager.BeginStage(playerManager);
        }


        if (Managers.Instance.TryGetManager<AbilityManager>(out var abilityManager))
        {
            abilityManager.BeginStage(playerId);
        }

        if (Managers.Instance.TryGetManager<ExperienceManager>(out var experienceManager))
        {
            experienceManager.BeginStage(playerId);
        }

        if (Managers.Instance.TryGetManager<DropManager>(out var dropManager))
        {
            dropManager.BeginStage(playerId);
        }
        if (!_deathSubscribed)
        {
            playerManager.OnPlayerDied += HandlePlayerDied;
            _deathSubscribed = true;
        }

        _stageActive = true;
        PlayDirector();
    }

    private void TickStage()
    {
        if (_gameController == null
            || _stageData == null
            || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager)
            || !Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager))
        {
            return;
        }

        var playerId = playerManager.LocalPlayerId;
        enemyManager.Tick(playerId);
    }

    private void PlayDirector()
    {
        if (_director == null || _stageData == null || _stageData.Timeline == null)
        {
            if (!_missingTimelineLogged)
            {
                _missingTimelineLogged = true;
                Debug.LogError("StageData에 타임라인이 없어 적을 스폰하지 않습니다.");
            }

            return;
        }

        SubscribeDirector();
        _acceptDirectorStop = false;
        _bossTimelinePaused = false;
        _manualDirector = false;
        _director.Stop();
        _director.playableAsset = _stageData.Timeline;
        _director.extrapolationMode = DirectorWrapMode.None;
        _director.timeUpdateMode = DirectorUpdateMode.GameTime;
        _director.time = 0d;
        _acceptDirectorStop = true;
        _director.Play();
    }

    private void HandleDirectorStopped(PlayableDirector director)
    {
        if (!_acceptDirectorStop || !_stageActive || director != _director)
        {
            return;
        }

        EndStage(GameState.Victory);
    }

    /// <summary>
    /// 보스전 동안 타임라인을 멈춘다. 정지는 클리어로 치지 않는다.
    /// </summary>
    public void PauseTimelineForBoss()
    {
        if (_director == null || !_stageActive)
        {
            return;
        }

        _bossPausedTime = _director.time;
        _bossTimelinePaused = true;
        _manualDirector = true;
        _director.timeUpdateMode = DirectorUpdateMode.Manual;
        _director.Pause();
        HoldDirectorTime(0d);
    }

    /// <summary>
    /// 보스를 모두 쓰러뜨린 뒤 멈춘 시각부터 타임라인을 다시 튼다.
    /// </summary>
    public void ResumeTimelineAfterBoss()
    {
        if (!_bossTimelinePaused || _director == null)
        {
            _bossTimelinePaused = false;
            return;
        }

        _bossTimelinePaused = false;
        if (!_stageActive || !_acceptDirectorStop)
        {
            return;
        }

        HoldDirectorTime(1d);
        _director.Play();
        HoldDirectorTime(1d);
    }

    /// <summary>
    /// 패배로 판이 끝날 때 보스 정지를 푼다. 클리어로 처리하지 않는다.
    /// </summary>
    public void ReleaseBossTimeline()
    {
        var wasPaused = _bossTimelinePaused;
        _bossTimelinePaused = false;
        _manualDirector = false;
        if (!wasPaused || _director == null)
        {
            return;
        }

        HoldDirectorTime(0d);
    }

    private void AbandonBossTimeline()
    {
        _bossTimelinePaused = false;
        _manualDirector = false;
    }

    /// <summary>
    /// 보스전 뒤에는 디렉터 시각을 직접 쌓는다. GameTime으로 되돌리면 멈춘 시간이 한 번에 따라붙는다.
    /// </summary>
    private void TickManualDirector()
    {
        if (_director == null)
        {
            return;
        }

        _director.time += Time.deltaTime;
        _director.Evaluate();
    }

    private void HoldDirectorTime(double speed)
    {
        if (_director == null)
        {
            return;
        }

        _director.time = _bossPausedTime;
        var graph = _director.playableGraph;
        if (!graph.IsValid())
        {
            return;
        }

        var root = graph.GetRootPlayable(0);
        if (!root.IsValid())
        {
            return;
        }

        root.SetTime(_bossPausedTime);
        root.SetSpeed(speed);
    }

    private void SubscribeDirector()
    {
        if (_director == null || _directorHooked)
        {
            return;
        }

        _director.stopped += HandleDirectorStopped;
        _directorHooked = true;
    }

    private void UnsubscribeDirector()
    {
        if (_director == null || !_directorHooked)
        {
            _directorHooked = false;
            return;
        }

        _director.stopped -= HandleDirectorStopped;
        _directorHooked = false;
    }

    private void TogglePause(GameState state)
    {
        var settingsWindow = Managers.Instance.SettingsWindow;
        if (settingsWindow != null && settingsWindow.IsOpen)
        {
            return;
        }

        if (!Managers.Instance.TryGetManager<InputManager>(out var inputManager) || !inputManager.ConsumePausePressed())
        {
            return;
        }

        if (state == GameState.Playing)
        {
            Pause();
            return;
        }

        if (state == GameState.Paused)
        {
            Resume();
        }
    }

    /// <summary>
    /// 진행 중인 판을 멈추고 일시정지 상태로 바꾼다.
    /// </summary>
    public void Pause()
    {
        if (!_stageActive || Managers.Instance == null || Managers.Instance.CurrentState != GameState.Playing)
        {
            return;
        }

        Time.timeScale = 0f;
        Managers.Instance.ChangeState(GameState.Paused);
    }

    /// <summary>
    /// 일시정지를 풀고 전투를 다시 진행한다.
    /// </summary>
    public void Resume()
    {
        if (!_stageActive || Managers.Instance == null || Managers.Instance.CurrentState != GameState.Paused)
        {
            return;
        }

        Time.timeScale = 1f;
        Managers.Instance.ChangeState(GameState.Playing);
    }

    private void HandlePlayerDied(int playerId)
    {
        if (!_stageActive || Managers.Instance == null || Managers.Instance.CurrentState != GameState.Playing)
        {
            return;
        }

        if (!Managers.Instance.TryGetManager<PlayerManager>(out var playerManager) || playerId != playerManager.LocalPlayerId)
        {
            return;
        }

        EndStage(GameState.Defeat);
    }

    private void EndStage(GameState resultState)
    {
        if (!_stageActive || _gameController == null || Managers.Instance == null)
        {
            return;
        }

        _acceptDirectorStop = false;
        _stageActive = false;
        BossArenaPlayback.Cancel();
        UnsubscribeDeath();

        if (Managers.Instance.TryGetManager<UpgradeManager>(out var upgradeManager))
        {
            upgradeManager.EndStage();
        }

        if (Managers.Instance.TryGetManager<AbilityManager>(out var abilityManager))
        {
            abilityManager.EndStage();
        }

        if (Managers.Instance.TryGetManager<ExperienceManager>(out var experienceManager))
        {
            experienceManager.EndStage();
        }

        if (Managers.Instance.TryGetManager<DropManager>(out var dropManager))
        {
            dropManager.EndStage();
        }

        var killCount = 0;
        if (Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager))
        {
            killCount = enemyManager.KillCount;
            // 결과용 KillCount를 유지하면서 남아 있는 Enemy를 Pool로 반환한다.
            enemyManager.EndStage();
        }

        var playerId = 0;
        if (Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            playerId = playerManager.LocalPlayerId;
        }

        var currencies = System.Array.Empty<CurrencyAmount>();
        if (Managers.Instance.TryGetManager<CurrenciesManager>(out var currenciesManager))
        {
            currencies = currenciesManager.FinishStage(playerId);
        }

        LastResult = new StageResult(playerId, killCount, _gameController.ElapsedSeconds, currencies);
        Time.timeScale = 0f;
        ReleaseBossTimeline();
        Managers.Instance.ChangeState(resultState);
    }

    private void HandleGameStateChanged(GameState previousState, GameState newState)
    {
        if (newState != GameState.Loading && newState != GameState.Menu)
        {
            return;
        }

        _acceptDirectorStop = false;
        UnsubscribeDirector();
        Time.timeScale = 1f;
        _stageActive = false;
        AbandonBossTimeline();
        BossArenaPlayback.Cancel();
        UnsubscribeDeath();
        if (Managers.Instance != null && Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager))
        {
            enemyManager.ClearStage();
        }

        if (Managers.Instance != null && Managers.Instance.TryGetManager<AbilityManager>(out var abilityManager))
        {
            abilityManager.EndStage();
        }

        if (Managers.Instance != null && Managers.Instance.TryGetManager<ExperienceManager>(out var experienceManager))
        {
            experienceManager.EndStage();
        }
        if (Managers.Instance != null &&Managers.Instance.TryGetManager<UpgradeManager>(out var upgradeManager))
        {
            upgradeManager.EndStage();
        }

        if (Managers.Instance != null && Managers.Instance.TryGetManager<DropManager>(out var dropManager))
        {
            dropManager.EndStage();
        }

        if (Managers.Instance != null && Managers.Instance.TryGetManager<StageFieldManager>(out var fieldManager))
        {
            fieldManager.Clear();
        }
    }

    private void UnsubscribeDeath()
    {
        if (!_deathSubscribed || Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            _deathSubscribed = false;
            return;
        }

        playerManager.OnPlayerDied -= HandlePlayerDied;
        _deathSubscribed = false;
    }

    private void RegisterFieldRoot()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<StageFieldManager>(out var fieldManager))
        {
            return;
        }

        fieldManager.SetRoot(transform);
    }

    private void ClearFieldRoot()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<StageFieldManager>(out var fieldManager))
        {
            return;
        }

        fieldManager.ClearRoot(transform);
    }
}
