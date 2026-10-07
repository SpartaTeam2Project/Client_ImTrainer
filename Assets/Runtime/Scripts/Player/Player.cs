using UnityEngine;

/// <summary>
/// 스폰된 플레이어 한 명의 컴포넌트를 초기화하고 소유 매니저와 잇는다.
/// </summary>
[RequireComponent(typeof(PlayerStat))]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerExperience))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerWeapon))]
public class Player : MonoBehaviour
{
    private PlayerManager _owner;

    public PlayerStat Stat { get; private set; }

    public PlayerHealth Health { get; private set; }

    public PlayerExperience Experience { get; private set; }

    public PlayerMovement Movement { get; private set; }

    public PlayerWeapon Weapons { get; private set; }

    public PlayerView View { get; private set; }

    #region Unity Methods

    private void OnDestroy()
    {
        if (_owner == null)
        {
            return;
        }

        _owner.NotifyActorDestroyed(this);
        _owner = null;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 소유 매니저를 연결한다.
    /// </summary>
    public void Bind(PlayerManager owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// 프리팹에 적힌 스탯과 경험치 곡선으로 한 판을 시작한다.
    /// </summary>
    public void Initialize()
    {
        // 대체 생성 경로는 Player 다음에 PlayerView를 붙이므로 Awake가 아니라 여기서 찾는다.
        Stat = GetComponent<PlayerStat>();
        Health = GetComponent<PlayerHealth>();
        Experience = GetComponent<PlayerExperience>();
        Movement = GetComponent<PlayerMovement>();
        Weapons = GetComponent<PlayerWeapon>();
        View = GetComponent<PlayerView>();

        Stat.Initialize();
        Health.Initialize(this, Stat);
        Experience.Initialize(Stat);
        Movement.Initialize(Stat, Health, View);
        Weapons.Initialize(this);
    }

    /// <summary>
    /// 선택한 플레이어블의 애니메이션 스프라이트로 모습을 바꾼다.
    /// </summary>
    public void ApplyPlayable(PlayableCharacterData data)
    {
        if (data == null)
        {
            return;
        }

        if (View == null)
        {
            View = GetComponent<PlayerView>();
        }

        if (View == null)
        {
            return;
        }

        View.ApplyPlayable(data);
    }

    /// <summary>
    /// 피해를 받으면 PlayerHealth가 부른다. 피격 번쩍임을 켠다.
    /// </summary>
    public void NotifyDamaged()
    {
        if (View != null)
        {
            View.PlayHitFlash();
        }
    }

    /// <summary>
    /// 체력이 0이 되면 PlayerHealth가 부른다. 사망 연출과 장착 포켓몬 기절을 켜고 소유 매니저에 알린다.
    /// </summary>
    public void NotifyDied()
    {
        if (View != null)
        {
            View.SetVisual(false, Movement.LookDirection);
            View.ShowDeathLight();
        }

        if (Weapons != null)
        {
            Weapons.PlayFaint();
        }

        if (_owner != null)
        {
            _owner.NotifyDied(this);
        }
    }

    #endregion
}
