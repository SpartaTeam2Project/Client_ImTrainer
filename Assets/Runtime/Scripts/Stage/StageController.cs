using UnityEngine;

/// <summary>
/// 게임 씬에서 한 판의 시작, 일시정지, 시간 승리, 사망 패배를 맡는다.
/// </summary>
public class StageController : MonoBehaviour
{
    [SerializeField] private CharacterStats _characterStats = new CharacterStats();
    [SerializeField] private StageData _stageData;

    private GameController _gameController;
    private bool _stageActive;
    private bool _deathSubscribed;
    private bool _missingStageLogged;

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
        _stageActive = false;
        UnsubscribeDeath();
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
        var playerId = playerManager.SpawnLocal(_characterStats);
        enemyManager.BeginStage(playerId, _stageData);
        if (!_deathSubscribed)
        {
            playerManager.OnPlayerDied += HandlePlayerDied;
            _deathSubscribed = true;
        }

        _stageActive = true;
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

        var elapsed = _gameController.ElapsedSeconds;
        enemyManager.Tick(playerManager.LocalPlayerId, elapsed);
        if (!_stageActive || !_stageData.EndsOnTime || elapsed < _stageData.ClearTimeSeconds)
        {
            return;
        }

        EndStage(GameState.Victory);
    }

    private void TogglePause(GameState state)
    {
        if (!Managers.Instance.TryGetManager<InputManager>(out var inputManager) || !inputManager.ConsumePausePressed())
        {
            return;
        }

        if (!_stageActive)
        {
            return;
        }

        if (state == GameState.Playing)
        {
            Time.timeScale = 0f;
            Managers.Instance.ChangeState(GameState.Paused);
            return;
        }

        if (state == GameState.Paused)
        {
            Time.timeScale = 1f;
            Managers.Instance.ChangeState(GameState.Playing);
        }
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

        _stageActive = false;
        UnsubscribeDeath();

        var killCount = 0;
        if (Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager))
        {
            killCount = enemyManager.KillCount;
        }

        LastResult = new StageResult(killCount, _gameController.ElapsedSeconds);
        Time.timeScale = 0f;
        Managers.Instance.ChangeState(resultState);
    }

    private void HandleGameStateChanged(GameState previousState, GameState newState)
    {
        if (newState != GameState.Loading && newState != GameState.Menu)
        {
            return;
        }

        Time.timeScale = 1f;
        _stageActive = false;
        UnsubscribeDeath();
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
}
