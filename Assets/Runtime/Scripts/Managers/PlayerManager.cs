using Cysharp.Threading.Tasks;
using System;
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
    private const float ACTOR_SIZE = 0.9f;
    private const int ACTOR_SORTING_ORDER = 10;

    [Header("Player Settings")]
    [SerializeField] private Player _playerPrefab;

    private Player _player;
    private bool _movementLocked;
    private Vector2 _scriptedMove;


    public int LocalPlayerId => LOCAL_PLAYER_ID_VALUE;

    public Transform PlayerTransform => _player == null ? null : _player.transform;

    /// <summary>
    /// 서 있는 채로 플레이어가 바라보는 방향을 바꾼다.
    /// </summary>
    public void SetLookDirection(Vector2 direction)
    {
        if (_player == null)
        {
            return;
        }

        _player.Movement.Face(direction);
    }

    /// <summary>
    /// 연출 동안 이동 입력을 무시한다.
    /// </summary>
    public void SetMovementLocked(bool locked)
    {
        _movementLocked = locked;
        _scriptedMove = Vector2.zero;
        if (locked && _player != null)
        {
            _player.Movement.Move(Vector2.zero);
        }
    }

    /// <summary>
    /// 이동이 잠긴 동안 연출이 플레이어를 걷게 한다. 영벡터면 그 자리에 선다.
    /// </summary>
    public void SetScriptedMove(Vector2 movement)
    {
        _scriptedMove = movement;
    }

    /// <summary>
    /// 식별자가 로컬 플레이어와 같으면 그 참조를 돌려준다.
    /// </summary>
    public bool TryGetPlayer(int playerId, out Player player)
    {
        if (playerId == LocalPlayerId && _player != null)
        {
            player = _player;
            return true;
        }

        player = null;
        return false;
    }

    public Vector2 LookDirection => _player == null ? Vector2.right : _player.Movement.LookDirection;

    /// <summary>
    /// 스폰된 플레이어의 시작 칸에 포켓몬을 붙인다.
    /// </summary>
    public void EquipStarting(int playerId)
    {
        if (!TryGetPlayer(playerId, out var player))
        {
            return;
        }

        player.Weapons.EquipStarting(playerId);
    }

    /// <summary>
    /// 장착한 포켓몬을 치운다.
    /// </summary>
    public void ClearEquipped()
    {
        if (_player == null)
        {
            return;
        }

        _player.Weapons.ClearWeapons();
    }

    /// <summary>
    /// 그 플레이어의 해당 칸 포켓몬이 발사 방향 걷기를 재생하게 한다.
    /// </summary>
    public void FaceShot(int playerId, WeaponSlot slot, Vector2 direction)
    {
        if (!TryGetPlayer(playerId, out var player))
        {
            return;
        }

        player.Weapons.FaceShot(slot, direction);
    }

    /// <summary>
    /// 그 플레이어의 장착 포켓몬이 성공 포즈를 재생하게 한다.
    /// </summary>
    public void PlayPose(int playerId)
    {
        if (!TryGetPlayer(playerId, out var player))
        {
            return;
        }

        player.Weapons.PlayPose();
    }


    public float Speed => _player == null ? 0f : _player.Stat.moveSpeed * _player.Stat.moveSpeedMultiplier;

    public float MagnetRadius => _player == null ? 0f : _player.Stat.magnetRadius;

    public float CurrentHealth => _player == null ? 0f : _player.Health.CurrentHealth;

    public float MaxHealth => _player == null ? 0f : _player.Health.maxHealth;

    public bool IsAlive => _player != null && _player.Health.IsAlive;

    public int Level => _player == null ? STARTING_LEVEL : _player.Experience.Level;

    public float CurrentXp => _player == null ? 0f : _player.Experience.CurrentXp;

    public float RequiredXp => _player == null ? 0f : _player.Experience.RequiredXp;

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

        if (_movementLocked)
        {
            _player.Movement.Move(_scriptedMove);
            return;
        }

        if (!Managers.Instance.TryGetManager<InputManager>(out var inputManager))
        {
            return;
        }


        _player.Movement.Move(inputManager.MovementValue);
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

        ApplySelectedPlayable();

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

        var levelBefore = _player.Experience.Level;
        _player.Experience.AddExperience(amount);
        PublishExperience(playerId);
        for (var level = levelBefore + 1; level <= _player.Experience.Level; level++)
        {
            NotifyLevelUp(playerId, level);
        }
    }

    #region PlayerStatAPI
    /// <summary>
    /// Player의 Upgrade 이동속도 배율을 증가시킨다. value=0.1f=10%만큼 배율 증가
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="value"></param>
    public void AddMoveSpeedMultiplier(int playerId,float value)
    {
        if (playerId != LocalPlayerId || _player == null) return;

        _player.Stat.AddMoveSpeedMultiplier(value);
    }

    public void AddXpMultiplier(int playerId,float value)
    {
        if(playerId!=LocalPlayerId || _player == null) return;
        _player.Stat.AddXpMultiplier(value);
    }

    public void AddReceiveDamageMultiplier(int playerId,float value)
    {
        if (playerId != LocalPlayerId || _player == null) return;
        _player.Stat.AddReceivedDamageMultiplier(value);
    }

    public void AddDamageMultiplier(int playerId,float value)
    {
        if (playerId != LocalPlayerId || _player == null) return;
        _player.Stat.AddDamageMultiplier(value);
    }
    #endregion



    #region player의 private field 조회 API
    /// <summary>
    /// player가 보유한 전역 주는 피해 배율을 반환한다.
    /// player가 없으면 1을 반환한다.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public float GetDamageMultiplier(int playerId)
    {
        if (playerId != LocalPlayerId || _player == null) return 1f;
        return _player.Stat.damageMultiplier;
    }

    #endregion

    /// <summary>
    /// 플레이어 식별자에 피해를 준다.
    /// </summary>
    public void TakeDamage(int playerId, float amount)
    {
        if (playerId != LocalPlayerId || _player == null)
        {
            return;
        }

        _player.Health.TakeDamage(amount);
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

    private void NotifyLevelUp(int playerId, int level)
    {
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

        eventManager.Publish(new PlayerExperienceChanged(playerId, _player.Experience.Level, _player.Experience.CurrentXp, _player.Experience.RequiredXp));
    }

    private void ApplySelectedPlayable()
    {
        if (_player == null || Managers.Instance == null || !Managers.Instance.TryGetManager<AccountManager>(out var account))
        {
            return;
        }

        _player.ApplyPlayable(account.ResolveSelectedPlayable());
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
