using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 로컬 플레이어의 식별자, 트랜스폼, 체력을 소유한다.
/// </summary>
public class PlayerManager : BaseManager
{
    private const int LOCAL_PLAYER_ID_VALUE = 1;
    private const float ACTOR_SIZE = 0.9f;
    private const int ACTOR_SORTING_ORDER = 10;
    private const float MOVE_SQR_EPSILON = 0.0001f;

    [Header("Player Settings")]
    [SerializeField] private PlayerActor _playerPrefab;

    private PlayerActor _actor;
    private PlayerView _view;
    private float _speed;
    private Vector2 _lookDirection = Vector2.right;

    public int LocalPlayerId => LOCAL_PLAYER_ID_VALUE;

    public Transform PlayerTransform => _actor == null ? null : _actor.transform;

    public Vector2 LookDirection => _lookDirection;

    public float Speed => _speed;

    public float CurrentHealth { get; private set; }

    public float MaxHealth { get; private set; }

    public bool IsAlive { get; private set; }

    public event Action<int> OnPlayerDied;

    #region Unity Methods

    /// <summary>
    /// 플레이어 매니저 초기화
    /// </summary>
    public override UniTask InitializeAsync()
    {
        return base.InitializeAsync();
    }

    private void Update()
    {
        if (!IsAlive || _actor == null || Managers.Instance == null || !Managers.Instance.IsSimulationRunning)
        {
            return;
        }

        if (!Managers.Instance.TryGetManager<InputManager>(out var inputManager))
        {
            return;
        }

        Move(inputManager.MovementValue);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 로컬 플레이어를 만들고 식별자를 돌려준다.
    /// </summary>
    public int SpawnLocal(CharacterStats stats)
    {
        Despawn();

        var maxHealth = stats != null ? stats.MaxHealth : 1f;
        var speed = stats != null ? stats.MoveSpeed : 0f;
        MaxHealth = Mathf.Max(1f, maxHealth);
        CurrentHealth = MaxHealth;
        _speed = Mathf.Max(0f, speed);
        _lookDirection = Vector2.right;
        IsAlive = true;

        _actor = CreateActor();
        _actor.Bind(this);
        _view = _actor.GetComponent<PlayerView>();
        if (_view != null)
        {
            _view.SetVisual(false, _lookDirection);
        }

        return LocalPlayerId;
    }

    /// <summary>
    /// 플레이어 식별자에 피해를 준다. 체력이 0이면 사망을 알린다.
    /// </summary>
    public void TakeDamage(int playerId, float amount)
    {
        if (playerId != LocalPlayerId || !IsAlive || amount <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        if (CurrentHealth > 0f)
        {
            return;
        }

        IsAlive = false;
        if (_view != null)
        {
            _view.SetVisual(false, _lookDirection);
        }

        OnPlayerDied?.Invoke(playerId);
    }

    /// <summary>
    /// 스폰된 액터가 씬과 함께 사라지면 참조를 비운다.
    /// </summary>
    public void NotifyActorDestroyed(PlayerActor actor)
    {
        if (_actor != actor)
        {
            return;
        }

        _actor = null;
        _view = null;
        IsAlive = false;
    }

    #endregion

    #region Private Methods

    private void Move(Vector2 movement)
    {
        var isMoving = movement.sqrMagnitude > MOVE_SQR_EPSILON;
        if (isMoving)
        {
            _lookDirection = movement.normalized;
        }

        var delta = movement * _speed * Time.deltaTime;
        var next = (Vector2)_actor.transform.position + delta;
        if (Managers.Instance.TryGetManager<StageFieldManager>(out var fieldManager))
        {
            next = fieldManager.ValidatePosition(next);
        }

        _actor.transform.position = next;
        if (_view != null)
        {
            _view.SetVisual(isMoving, _lookDirection);
        }
    }

    private PlayerActor CreateActor()
    {
        if (_playerPrefab != null)
        {
            var actor = Instantiate(_playerPrefab);
            actor.gameObject.name = "Player";
            return actor;
        }

        Debug.LogWarning("Player 프리팹이 없어 자리표시 액터를 만듭니다.");
        var actorObject = new GameObject("Player");
        actorObject.transform.localScale = new Vector3(ACTOR_SIZE, ACTOR_SIZE, 1f);

        var renderer = actorObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = ACTOR_SORTING_ORDER;

        var actorFallback = actorObject.AddComponent<PlayerActor>();
        actorObject.AddComponent<PlayerView>();
        return actorFallback;
    }

    private void Despawn()
    {
        if (_actor == null)
        {
            return;
        }

        var actor = _actor;
        _actor = null;
        _view = null;
        IsAlive = false;
        if (actor != null)
        {
            Destroy(actor.gameObject);
        }
    }

    #endregion
}
