/// <summary>
/// 투사체가 명중할 때 피해 출처를 묻는 대상. 능력 behavior가 구현한다.
/// 같은 칸 behavior가 포켓몬 교체 뒤에도 남을 수 있어서 명중 시점에 묻는다.
/// </summary>
public interface IDamageSourceProvider
{
    DamageSource CreateDamageSource();
}
