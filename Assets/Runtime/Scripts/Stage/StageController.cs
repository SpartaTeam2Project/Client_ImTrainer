using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// 게임 씬에서 한 판의 시작, 일시정지, 타임라인 클리어, 사망 패배를 맡는다.
/// </summary>
public class StageController : MonoBehaviour
{
    [SerializeField] private StageData _stageData;
    [SerializeField] private PlayableDirector _director;

    [Header("성공 연출")]
    [Tooltip("성공 포즈 마지막 장을 보여 준 뒤 결과창을 열기까지의 시간")]
    [SerializeField] private float _poseHoldSeconds = 0.6f;
    [Tooltip("포즈 그림이 없어도 결과창을 열기 전에 기다리는 최소 시간")]
    [SerializeField] private float _minResultDelaySeconds = 0.5f;

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
    private readonly List<double> _clearedBossTimes = new List<double>();

    public StageResult LastResult { get; private set; }

    /// <summary>
    /// 판이 끝난 뒤 결과창을 열기 전에 기다릴 실제 시간(초). 성공 포즈를 보여 줄 시간이다.
    /// </summary>
    public float ResultRevealDelay { get; private set; }

    /// <summary>
    /// 이 씬이 진행할 스테이지. 판 시작 전에 몬스터 그림을 미리 불러올 때 쓴다.
    /// </summary>
    public StageData StageData => _stageData;

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
        ResultRevealDelay = 0f;
        _clearedBossTimes.Clear();
        if (Managers.Instance.TryGetManager<StageFieldManager>(out var fieldManager))
        {
            fieldManager.Begin(_stageData);
        }

        var playerId = playerManager.SpawnLocal();
        if (Managers.Instance.TryGetManager<StageStatsManager>(out var statsManager))
        {
            statsManager.BeginStage(playerId);
        }

        if (Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            itemManager.BeginRun(playerId, _stageData.ShopGenerations);
        }

        playerManager.EquipStarting(playerId);
        if (Managers.Instance.TryGetManager<CurrenciesManager>(out var currenciesManager))
        {
            currenciesManager.ClearStage(playerId);
        }

        enemyManager.BeginStage(playerId, _stageData, transform);

        if (Managers.Instance.TryGetManager<UpgradeManager>(out var upgradeManager))
        {
            upgradeManager.BeginStage(playerManager);
        }


        if (Managers.Instance.TryGetManager<WeaponAbilityManager>(out var abilityManager))
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
    /// 타임라인 현재 시각과 전체 길이를 돌려준다. 재생 전이면 시각은 0이다.
    /// </summary>
    public bool TryGetTimelineProgress(out double time, out double duration)
    {
        time = 0d;
        duration = 0d;
        var timeline = ResolveTimeline();
        if (timeline == null)
        {
            return false;
        }

        duration = timeline.duration;
        if (duration <= 0d)
        {
            return false;
        }

        if (_director != null && _director.playableAsset == timeline)
        {
            time = _director.time;
        }

        return true;
    }

    /// <summary>
    /// 보스 조우 클립이 시작하는 시각을 모은다. 진행도 바가 마커 위치를 계산할 때 쓴다.
    /// </summary>
    public void CopyBossMarkerTimes(List<double> times)
    {
        if (times == null)
        {
            return;
        }

        times.Clear();
        var timeline = ResolveTimeline();
        if (timeline == null)
        {
            return;
        }

        foreach (var track in timeline.GetOutputTracks())
        {
            if (track == null || track.muted || track is not BossEncounterTrack)
            {
                continue;
            }

            foreach (var clip in track.GetClips())
            {
                times.Add(clip.start);
            }
        }
    }

    /// <summary>
    /// 이번 판에서 클리어한 보스 조우 시각을 모은다. 진행도 바가 잡은 보스의 마커를 지울 때 쓴다.
    /// </summary>
    public void CopyClearedBossTimes(List<double> times)
    {
        if (times == null)
        {
            return;
        }

        times.Clear();
        for (var i = 0; i < _clearedBossTimes.Count; i++)
        {
            times.Add(_clearedBossTimes[i]);
        }
    }

    /// <summary>
    /// 지금 멈춰 있는 보스 조우를 클리어로 기록한다.
    /// </summary>
    public void MarkBossCleared()
    {
        _clearedBossTimes.Add(_bossPausedTime);
    }

    private TimelineAsset ResolveTimeline()
    {
        if (_director != null && _director.playableAsset is TimelineAsset playing)
        {
            return playing;
        }

        return _stageData != null ? _stageData.Timeline : null;
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

        if (Managers.Instance.TryGetManager<WeaponAbilityManager>(out var abilityManager))
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
        var report = default(StageStatsReport);
        if (Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            playerId = playerManager.LocalPlayerId;
            // 파티를 읽어야 하므로 가방을 지우는 EndRun보다 먼저 닫는다.
            if (Managers.Instance.TryGetManager<StageStatsManager>(out var statsManager))
            {
                report = statsManager.FinishStage(playerId, killCount);
            }

            if (resultState == GameState.Victory)
            {
                var poseSeconds = playerManager.PlayPose(playerId);
                ResultRevealDelay = Mathf.Max(_minResultDelaySeconds, poseSeconds + _poseHoldSeconds);
            }

            if (Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
            {
                itemManager.EndRun(playerId, true);
            }
        }

        var currencies = System.Array.Empty<CurrencyAmount>();
        if (Managers.Instance.TryGetManager<CurrenciesManager>(out var currenciesManager))
        {
            currencies = currenciesManager.FinishStage(playerId);
        }

        LastResult = StageResultBuilder.Build(playerId, _stageData, resultState == GameState.Victory, killCount,
            _gameController.ElapsedSeconds, currencies, report);
        if (Managers.Instance.TryGetManager<RankManager>(out var rankManager))
        {
            rankManager.Submit(LastResult);
        }

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

        if (Managers.Instance != null && Managers.Instance.TryGetManager<WeaponAbilityManager>(out var abilityManager))
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

        if (Managers.Instance != null && Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            if (Managers.Instance.TryGetManager<StageStatsManager>(out var statsManager))
            {
                statsManager.CancelStage(playerManager.LocalPlayerId);
            }

            if (Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
            {
                itemManager.EndRun(playerManager.LocalPlayerId);
            }

            playerManager.ClearEquipped();
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
