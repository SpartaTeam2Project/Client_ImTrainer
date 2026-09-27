using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 플레이어 경험치 게이지가 바뀌었다.
/// </summary>
public struct PlayerExperienceChanged
{
    public int PlayerId;
    public int Level;
    public float CurrentXp;
    public float RequiredXp;

    public PlayerExperienceChanged(int playerId, int level, float currentXp, float requiredXp)
    {
        PlayerId = playerId;
        Level = level;
        CurrentXp = currentXp;
        RequiredXp = requiredXp;
    }
}

/// <summary>
/// 플레이어 레벨이 올랐다. 증강 선택은 이 값을 받아 나중에 연다.
/// </summary>
public struct PlayerLeveledUp
{
    public int PlayerId;
    public int Level;

    public PlayerLeveledUp(int playerId, int level)
    {
        PlayerId = playerId;
        Level = level;
    }
}

/// <summary>
/// 로컬 플레이어의 식별자와 스폰된 플레이어 참조를 소유한다.
/// </summary>
public class PlayerManager : BaseManager
{
    private const int LOCAL_PLAYER_ID_VALUE = 1;
    private const int STARTING_LEVEL = 1;
    private const int MAX_LEVEL_UPS_PER_GAIN = 20;
    private const float MIN_REQUIRED_XP = 1f;
    private const float ACTOR_SIZE = 0.9f;
    private const int ACTOR_SORTING_ORDER = 10;

    [Header("Experience")]
    [SerializeField] private float _baseRequiredXp = 5f;
    [SerializeField] private float _requiredGrowth = 1.25f;

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

    public int Level => _player == null ? STARTING_LEVEL : _player.Level;

    public float CurrentXp => _player == null ? 0f : _player.CurrentXp;

    public float RequiredXp => _player == null ? CalculateRequiredXp(STARTING_LEVEL) : _player.RequiredXp;

    public event Action<int> OnPlayerDied;

    public event Action<int, int> OnLevelUp;

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
        _player.SetProgress(STARTING_LEVEL, 0f, CalculateRequiredXp(STARTING_LEVEL));
        return LocalPlayerId;
    }

    /// <summary>
    /// 플레이어 식별자에 경험치를 더한다. 필요량을 채우면 레벨을 올린다.
    /// </summary>
    public void AddExperience(int playerId, float amount)
    {
        if (playerId != LocalPlayerId || _player == null || amount <= 0f)
        {
            return;
        }

        var xp = _player.CurrentXp + amount;
        var level = _player.Level;
        var required = Mathf.Max(MIN_REQUIRED_XP, _player.RequiredXp);
        var safety = 0;
        while (xp >= required && safety < MAX_LEVEL_UPS_PER_GAIN)
        {
            xp -= required;
            level++;
            required = CalculateRequiredXp(level);
            safety++;
            _player.SetProgress(level, xp, required);
            PublishExperience(playerId);
            NotifyLevelUp(playerId, level);
        }

        if (safety > 0)
        {
            return;
        }

        _player.SetProgress(level, xp, required);
        PublishExperience(playerId);
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

    private float CalculateRequiredXp(int level)
    {
        var step = Mathf.Max(STARTING_LEVEL, level);
        var growth = Mathf.Max(1f, _requiredGrowth);
        var required = Mathf.Max(MIN_REQUIRED_XP, _baseRequiredXp) * Mathf.Pow(growth, step - 1);
        return Mathf.Max(MIN_REQUIRED_XP, required);
    }

    private void NotifyLevelUp(int playerId, int level)
    {
        OnLevelUp?.Invoke(playerId, level);
        if (Managers.Instance != null && Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            eventManager.Publish(new PlayerLeveledUp(playerId, level));
        }

        if (Managers.Instance != null && Managers.Instance.TryGetManager<AbilityManager>(out var abilityManager))
        {
            abilityManager.OfferLevelUp(playerId, level);
        }
    }

    private void PublishExperience(int playerId)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<EventManager>(out var eventManager) || _player == null)
        {
            return;
        }

        eventManager.Publish(new PlayerExperienceChanged(playerId, _player.Level, _player.CurrentXp, _player.RequiredXp));
    }

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
