using UnityEngine;

/// <summary>
/// 보스 스킬 하나. 종류와 탄 그림을 가지고, 쿨타임과 범위는 타임라인에 둔다.
/// </summary>
[CreateAssetMenu(fileName = "BossSkill", menuName = "Boss/Boss Skill")]
public class BossSkillData : ScriptableObject
{
    [SerializeField] private BossSkillKind _kind = BossSkillKind.None;
    [Tooltip("보스 스킬 탄 그림.")]
    [SerializeField] private Sprite _projectileSprite;
    [Tooltip("차지 중 회오리. 작은 장부터 큰 장까지 한 번 재생한다.")]
    [SerializeField] private Sprite[] _chargeProjectileFrames = System.Array.Empty<Sprite>();
    [Tooltip("발사 후 회오리. 날아가는 동안 반복한다.")]
    [SerializeField] private Sprite[] _flyProjectileFrames = System.Array.Empty<Sprite>();
    [Tooltip("추적 빔 그림. 쏘는 동안 반복한다. 비어 있으면 단색 직선을 쓴다.")]
    [SerializeField] private Sprite[] _beamFrames = System.Array.Empty<Sprite>();
    [Tooltip("빔이 나가는 입. 그림 중심 기준이고 보스 크기를 곱하기 전 값이다. 순서: 오른쪽, 오른쪽 위, 위, 왼쪽 위, 왼쪽, 왼쪽 아래, 아래, 오른쪽 아래.")]
    [SerializeField] private Vector2[] _beamMouthOffsets = System.Array.Empty<Vector2>();
    [Tooltip("수면가루가 퍼지는 동안 재생하는 그림. 비어 있으면 단색 원을 쓴다.")]
    [SerializeField] private Sprite[] _poolBurstFrames = System.Array.Empty<Sprite>();
    [Tooltip("수면가루가 범위까지 퍼진 뒤 반복하는 그림.")]
    [SerializeField] private Sprite[] _poolLingerFrames = System.Array.Empty<Sprite>();
    [Tooltip("후딘 숟가락. 차지 중과 날아가는 동안 반복한다.")]
    [SerializeField] private Sprite[] _spoonFrames = System.Array.Empty<Sprite>();

    public BossSkillKind Kind => _kind;

    public Sprite ProjectileSprite => _projectileSprite;

    public Sprite[] ChargeProjectileFrames => _chargeProjectileFrames ?? System.Array.Empty<Sprite>();

    public Sprite[] FlyProjectileFrames => _flyProjectileFrames ?? System.Array.Empty<Sprite>();

    public Sprite[] BeamFrames => _beamFrames ?? System.Array.Empty<Sprite>();

    public Vector2[] BeamMouthOffsets => _beamMouthOffsets ?? System.Array.Empty<Vector2>();

    public Sprite[] PoolBurstFrames => _poolBurstFrames ?? System.Array.Empty<Sprite>();

    public Sprite[] PoolLingerFrames => _poolLingerFrames ?? System.Array.Empty<Sprite>();

    public Sprite[] SpoonFrames => _spoonFrames ?? System.Array.Empty<Sprite>();
}
