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

    private PlayerActor _actor;
    private StageField _stageField;
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

    /// <summary>
    /// 플레이어 매니저 초기화
    /// </summary>
    public override UniTask InitializeAsync()
    {
        return base.InitializeAsync();
    }

    /// <summary>
    /// 필드가 비활성화되면 같은 참조만 지운다.
    /// </summary>
    public void ClearStageField(StageField stageField)
    {
        if (_stageField == stageField)
        {
            _stageField = null;
        }
    }

    /// <summary>
    /// 이동 한계를 등록한다. 씬 검색 대신 필드가 자신을 알린다.
    /// </summary>
    public void SetStageField(StageField stageField)
    {
        _stageField = stageField;
    }

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

        var actorObject = new GameObject("Player");
        _actor = actorObject.AddComponent<PlayerActor>();
        _actor.Bind(this);

        var renderer = actorObject.AddComponent<SpriteRenderer>();
        renderer.sprite = PrototypeSprite.WhiteSquare;
        renderer.color = new Color(0.95f, 0.85f, 0.2f, 1f);
        renderer.sortingOrder = 10;
        actorObject.transform.localScale = new Vector3(ACTOR_SIZE, ACTOR_SIZE, 1f);
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
        IsAlive = false;
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

    private void Move(Vector2 movement)
    {
        if (movement.sqrMagnitude > 0.0001f)
        {
            _lookDirection = movement.normalized;
        }

        var delta = movement * _speed * Time.deltaTime;
        var next = (Vector2)_actor.transform.position + delta;
        if (_stageField != null)
        {
            next = _stageField.ValidatePosition(next);
        }

        _actor.transform.position = next;
        var scale = _actor.transform.localScale;
        var facing = _lookDirection.x < 0f ? -ACTOR_SIZE : ACTOR_SIZE;
        _actor.transform.localScale = new Vector3(facing, Mathf.Abs(scale.y), scale.z);
    }

    private void Despawn()
    {
        if (_actor == null)
        {
            return;
        }

        var actor = _actor;
        _actor = null;
        IsAlive = false;
        if (actor != null)
        {
            Destroy(actor.gameObject);
        }
    }
}

/// <summary>
/// 플레이어 오브젝트가 파괴될 때 매니저 참조를 끊는다.
/// </summary>
public class PlayerActor : MonoBehaviour
{
    private PlayerManager _owner;

    public void Bind(PlayerManager owner)
    {
        _owner = owner;
    }

    private void OnDestroy()
    {
        if (_owner == null)
        {
            return;
        }

        _owner.NotifyActorDestroyed(this);
        _owner = null;
    }
}
