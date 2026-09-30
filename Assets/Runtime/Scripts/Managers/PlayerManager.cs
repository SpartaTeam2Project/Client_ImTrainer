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
/// flow: 플레이어 생성 => 증강 초기화 => 이동/피해/경험치 전달량에 증강 배율 적용 => Player의 기존 처리.
/// </summary>
public class PlayerManager : BaseManager
{
    private const int LOCAL_PLAYER_ID_VALUE = 1;
    private const int STARTING_LEVEL = 1;
    private const float ACTOR_SIZE = 0.9f;
    private const int ACTOR_SORTING_ORDER = 10;

    [Header("Player Settings")]
    [SerializeField] private Player _playerPrefab;

    private Player _player;
    private UpgradeManager _upgradeManager;

    public int LocalPlayerId => LOCAL_PLAYER_ID_VALUE;

    public Transform PlayerTransform => _player == null ? null : _player.transform;

    public Vector2 LookDirection => _player == null ? Vector2.right : _player.LookDirection;

    // Player의 기준 Stat은 유지하고, 외부에는 실제 이동에 사용하는 속도를 돌려준다.
    public float Speed => _player == null ? 0f : _player.moveSpeed * (_upgradeManager != null ? _upgradeManager.MoveSpeedMultiplier : 1f);

    public float MagnetRadius => _player == null ? 0f : _player.magnetRadius;

    public float CurrentHealth => _player == null ? 0f : _player.CurrentHealth;

    public float MaxHealth => _player == null ? 0f : _player.maxHealth;

    public bool IsAlive => _player != null && _player.IsAlive;

    public int Level => _player == null ? STARTING_LEVEL : _player.Level;

    public float CurrentXp => _player == null ? 0f : _player.CurrentXp;

    public float RequiredXp => _player == null ? 0f : _player.RequiredXp;

    public event Action<int> OnPlayerDied;

    public event Action<int, int> OnLevelUp;

    //증강 적용을 위한 필드 추가 - 0930
    private float _moveSpeedMultiplier = 1f;
    private float _receivedDamageMultiplier = 1f;
    private float _experienceMultiplier = 1f;

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

        // flow: 이동 입력 => 증강 배율 => Player의 기존 이동/경계 검사.
        var movementMultiplier = _upgradeManager != null ? _upgradeManager.MoveSpeedMultiplier : 1f;
        _player.Move(inputManager.MovementValue * movementMultiplier);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 로컬 플레이어를 만들고 식별자를 돌려준다.
    /// </summary>
    public int SpawnLocal()
    {
        Despawn();

        _player = CreatePlayer();
        _player.Bind(this);
        _player.Initialize();

        // 프로토타입은 PlayerManager가 소유한다. Prefab/Managers 등록 변경 없이 붙이고,
        // 플레이어를 새로 생성할 때마다 보유 증강과 선택지를 함께 초기화한다.
        if (_upgradeManager == null)
        {
            _upgradeManager = GetComponent<UpgradeManager>();
            if (_upgradeManager == null)
            {
                _upgradeManager = gameObject.AddComponent<UpgradeManager>();
            }
        }

        _upgradeManager.BeginStage(this);
        return LocalPlayerId;
    }

    /// <summary>
    /// 플레이어 식별자에 경험치를 넘긴다. 오른 레벨은 여기서 알린다.
    /// </summary>
    public void AddExperience(int playerId, float amount)
    {
        if (playerId != LocalPlayerId || _player == null || amount <= 0f)
        {
            return;
        }

        var levelBefore = _player.Level;
        // 원래 획득량에 증강 배율을 한 번만 적용한다. 레벨업 계산/알림은 기존 흐름을 쓴다.
        var gainedXp = amount * (_upgradeManager != null ? _upgradeManager.ExperienceMultiplier : 1f);
        _player.AddExperience(gainedXp);

        PublishExperience(playerId);
        for (var level = levelBefore + 1; level <= _player.Level; level++)
        {
            NotifyLevelUp(playerId, level);
        }
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

        // Enemy/Projectile이 넘긴 피해량만 보정한다. Player의 체력/사망 처리는 그대로 사용한다.
        var appliedDamage = amount * (_upgradeManager != null ? _upgradeManager.ReceivedDamageMultiplier : 1f);
        var healthBefore = _player.CurrentHealth;
        _player.TakeDamage(appliedDamage);
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
        if (_upgradeManager != null)
        {
            _upgradeManager.EndStage();
        }
    }

    #endregion

    #region Private Methods

    private void NotifyLevelUp(int playerId, int level)
    {
        // LevelUp 사실만 알린다.
        // 어떤 Upgrade Sequence를 실행할지는 UpgradeManager가 결정한다.
        OnLevelUp?.Invoke(playerId, level);
        if (Managers.Instance != null && Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            eventManager.Publish(new PlayerLeveledUp(playerId, level));
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
        if (_upgradeManager != null)
        {
            _upgradeManager.EndStage();
        }

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
