using UnityEngine;

/// <summary>
/// 포켓몬을 판매했을 때 상태 줄에 띄울 대사. 목록에서 하나를 무작위로 고른다.
/// </summary>
public static class SellMessage
{
    // {0}은 포켓몬 이름, {1}은 을/를.
    private static readonly string[] FORMATS =
    {
        "{0}{1} 자연으로 돌려보냈다.",
        "바이바이, {0}!",
    };

    /// <summary>
    /// 대사 하나를 골라 포켓몬 이름을 넣어 돌려준다.
    /// </summary>
    public static string Get(string pokemonName)
    {
        var format = FORMATS[Random.Range(0, FORMATS.Length)];
        return string.Format(format, pokemonName, KoreanParticle.Object(pokemonName));
    }
}
