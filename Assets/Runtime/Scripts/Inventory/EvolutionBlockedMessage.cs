using UnityEngine;

/// <summary>
/// 갈래 진화라 합성으로 진화하지 못할 때 상태 줄에 띄울 대사. 목록에서 하나를 무작위로 고른다.
/// </summary>
public static class EvolutionBlockedMessage
{
    // {0}은 포켓몬 이름, {1}은 은/는.
    private static readonly string[] FORMATS =
    {
        "{0}{1} 아무 반응이 없다.",
        "특별한 도구의 힘이 있어야 진화할 수 있을 것 같다.",
        "어떤 도구에 신비하게 반응하는 포켓몬인 것 같다!",
    };

    /// <summary>
    /// 대사 하나를 골라 포켓몬 이름을 넣어 돌려준다.
    /// </summary>
    public static string Get(string pokemonName)
    {
        var format = FORMATS[Random.Range(0, FORMATS.Length)];
        return string.Format(format, pokemonName, KoreanParticle.Topic(pokemonName));
    }
}
