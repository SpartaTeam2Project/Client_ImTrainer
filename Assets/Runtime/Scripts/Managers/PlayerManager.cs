using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 로컬 플레이어의 식별자와 스폰된 플레이어 참조를 소유한다.
/// </summary>
public class PlayerManager : BaseManager
{
    private const int LOCAL_PLAYER_ID_VALUE = 1;
    private const float ACTOR_SIZE = 0.9f;
    private const int ACTOR_SORTING_ORDER = 10;

    [Header("Player Settings")]
    [SerializeField] private Player _playerPrefab;

    private Player _player;

    public int LocalPlayerId => LOCAL_PLAYER_ID_VALUE;

    public Transform PlayerTransform => _player == null ? null : _player.transform;

    public Vector2 LookDirection => _player == null ? Vector2.right : _player.LookDirection;

    public float Speed => _player == null ? 0f : _player.Speed;

    public float CurrentHealth => _player == null ? 0f : _player.CurrentHealth;

    public float MaxHealth => _player == null ? 0f : _player.MaxHealth;

    public bool IsAlive => _player != null && _player.IsAlive;

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
        if (!IsAlive || Managers.Instance == null || !Managers.Instance.IsSimulationRunning)
        {
            return;
        }

        if (!Managers.Instance.TryGetManager<InputManager>(out var inputManager))
        {
            return;
        }

        _player.Move(inputManager.MovementValue);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 로컬 플레이어를 만들고 식별자를 돌려준다.
    /// </summary>
    public int SpawnLocal(CharacterStats stats)
    {
        Despawn();

        _player = CreatePlayer();
        _player.Bind(this);
        _player.Initialize(stats);
        return LocalPlayerId;
    }

    /// <summary>
    /// 플레이어 식별자에 피해를 준다.
    /// </summary>
    public void TakeDamage(int playerId, float amount)
    {
        if (playerId != LocalPlayerId || _player == null)
        {
            return;
        }

        _player.TakeDamage(amount);
    }

    /// <summary>
    /// 플레이어 체력이 0이 되면 사망을 알린다.
    /// </summary>
    public void NotifyDied(Player player)
    {
        if (_player != player)
        {
            return;
        }

        OnPlayerDied?.Invoke(LocalPlayerId);
    }

    /// <summary>
    /// 스폰된 플레이어가 씬과 함께 사라지면 참조를 비운다.
    /// </summary>
    public void NotifyActorDestroyed(Player player)
    {
        if (_player != player)
        {
            return;
        }

        _player = null;
    }

    #endregion

    #region Private Methods

    private Player CreatePlayer()
    {
        if (_playerPrefab != null)
        {
            var player = Instantiate(_playerPrefab);
            player.gameObject.name = "Player";
            return player;
        }

        Debug.LogWarning("Player 프리팹이 없어 자리표시 액터를 만듭니다.");
        var actorObject = new GameObject("Player");
        actorObject.transform.localScale = new Vector3(ACTOR_SIZE, ACTOR_SIZE, 1f);

        var renderer = actorObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = ACTOR_SORTING_ORDER;

        var playerFallback = actorObject.AddComponent<Player>();
        actorObject.AddComponent<PlayerView>();
        return playerFallback;
    }

    private void Despawn()
    {
        if (_player == null)
        {
            return;
        }

        var player = _player;
        _player = null;
        if (player != null)
        {
            Destroy(player.gameObject);
        }
    }

    #endregion
}
