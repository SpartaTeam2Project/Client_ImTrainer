using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 매니저 생명주기와 게임 상태를 관리한다.
/// </summary>
public class Managers : MonoBehaviour
{
    private static Managers _instance;

    public static Managers Instance => _instance;

    [Header("매니저 참조")]
    [SerializeField] private EventManager _eventManager;
    [SerializeField] private AudioManager _audioManager;
    [SerializeField] private InputManager _inputManager;
    [SerializeField] private PlayerManager _playerManager;
    [SerializeField] private CameraManager _cameraManager;
    [SerializeField] private StageFieldManager _stageFieldManager;
    [SerializeField] private EnemyManager _enemyManager;
    [SerializeField] private SceneLoadManager _sceneLoadManager;

    private readonly Dictionary<System.Type, BaseManager> _managers = new Dictionary<System.Type, BaseManager>();

    public GameState CurrentState { get; private set; } = GameState.Boot;

    public bool IsSimulationRunning => CurrentState == GameState.Playing;

    public event System.Action<GameState, GameState> OnGameStateChanged;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        RegisterManager(_eventManager);
        RegisterManager(_audioManager);
        RegisterManager(_inputManager);
        RegisterManager(_playerManager);
        RegisterManager(_cameraManager);
        RegisterManager(_stageFieldManager);
        RegisterManager(_enemyManager);
        RegisterManager(_sceneLoadManager);
    }

    private void Start()
    {
        if (_instance != this)
        {
            return;
        }

        InitializeManagersAsync().Forget();
    }

    private void OnDestroy()
    {
        if (_instance != this)
        {
            return;
        }

        foreach (var manager in _managers.Values)
        {
            manager.Cleanup();
        }

        _managers.Clear();
        _instance = null;
    }

    /// <summary>
    /// 게임 상태를 바꾸고 구독자에게 알린다.
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        var previousState = CurrentState;
        CurrentState = newState;
        OnGameStateChanged?.Invoke(previousState, newState);
        if (TryGetManager<EventManager>(out var eventManager))
        {
            eventManager.Publish(new GameStateChanged(previousState, newState));
        }
    }

    /// <summary>
    /// 등록된 매니저를 가져온다. 없으면 null.
    /// </summary>
    public T GetManager<T>() where T : BaseManager
    {
        if (TryGetManager<T>(out var manager))
        {
            return manager;
        }

        Debug.LogError($"매니저를 찾을 수 없음: {typeof(T).Name}");
        return null;
    }

    /// <summary>
    /// 등록된 매니저가 있으면 true.
    /// </summary>
    public bool TryGetManager<T>(out T manager) where T : BaseManager
    {
        if (_managers.TryGetValue(typeof(T), out var baseManager))
        {
            manager = baseManager as T;
            return manager != null;
        }

        manager = null;
        return false;
    }

    private void RegisterManager(BaseManager manager)
    {
        if (manager == null)
        {
            Debug.LogError("매니저 참조가 비어 있습니다. Managers 인스펙터를 확인하세요.");
            return;
        }

        var type = manager.GetType();
        if (_managers.ContainsKey(type))
        {
            Debug.LogError($"중복된 매니저 등록 시도: {type.Name}");
            return;
        }

        _managers[type] = manager;
    }

    /// <summary>
    /// 이벤트, 오디오, 입력, 플레이어, 카메라, 필드, 적, 씬 순서로 초기화한다.
    /// </summary>
    private async UniTaskVoid InitializeManagersAsync()
    {
        if (TryGetManager<EventManager>(out var eventManager))
        {
            await eventManager.InitializeAsync();
        }

        if (TryGetManager<AudioManager>(out var audioManager))
        {
            await audioManager.InitializeAsync();
        }

        if (TryGetManager<InputManager>(out var inputManager))
        {
            await inputManager.InitializeAsync();
        }

        if (TryGetManager<PlayerManager>(out var playerManager))
        {
            await playerManager.InitializeAsync();
        }

        if (TryGetManager<CameraManager>(out var cameraManager))
        {
            await cameraManager.InitializeAsync();
        }

        if (TryGetManager<StageFieldManager>(out var stageFieldManager))
        {
            await stageFieldManager.InitializeAsync();
        }

        if (TryGetManager<EnemyManager>(out var enemyManager))
        {
            await enemyManager.InitializeAsync();
        }

        if (TryGetManager<SceneLoadManager>(out var sceneLoadManager))
        {
            await sceneLoadManager.InitializeAsync();
        }

        ChangeState(GameState.Menu);
    }
}
