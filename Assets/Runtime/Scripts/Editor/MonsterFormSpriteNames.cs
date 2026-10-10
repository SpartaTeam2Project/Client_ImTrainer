using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 몬스터 SO의 도감 번호와 폼 접미사로 원본 스프라이트 이름 후보를 만든다.
/// 아이콘은 0001, 초상화는 1처럼 번호 표기만 달라서 표기는 호출하는 쪽이 정한다.
/// </summary>
public static class MonsterFormSpriteNames
{
    // 기본 이름 스프라이트가 없고 폼 이름만 있는 종
    private static readonly Dictionary<int, string> DEFAULT_FORM_SUFFIX = new Dictionary<int, string>
    {
        { 201, "-a" },
        { 412, "-plant" },
        { 413, "-plant" },
        { 421, "-overcast" },
        { 422, "-west" },
        { 423, "-west" },
        { 487, "-altered" },
        { 492, "-land" },
        { 493, "-normal" },
    };

    // 폼 에셋 접미사별로 앞에서부터 찾는 스프라이트 접미사
    private static readonly Dictionary<string, string[]> FORM_SUFFIXES = new Dictionary<string, string[]>
    {
        { "Mega", new[] { "-mega", "-mega-y", "-mega-x" } },
        { "VMAX", new[] { "-gigantamax" } },
    };

    // X/Y처럼 갈래가 있는 폼 중 이 게임이 쓰는 쪽. 기본 순서보다 먼저 찾는다.
    private static readonly Dictionary<(int Dex, string Form), string> CHOSEN_FORM_SUFFIX = new Dictionary<(int Dex, string Form), string>
    {
        { (6, "Mega"), "-mega-x" },
    };

    /// <summary>
    /// 앞에서부터 찾을 이름 후보. form은 SO 이름의 _ 뒤 접미사이고 기본형이면 null이다. 모르는 폼이면 비어 있다.
    /// </summary>
    public static IEnumerable<string> Candidates(int dex, string form, string dexFormat)
    {
        var number = dex.ToString(dexFormat);
        if (form == null)
        {
            yield return number;
            if (DEFAULT_FORM_SUFFIX.TryGetValue(dex, out var suffix))
            {
                yield return number + suffix;
            }

            yield break;
        }

        if (CHOSEN_FORM_SUFFIX.TryGetValue((dex, form), out var chosen))
        {
            yield return number + chosen;
        }

        if (FORM_SUFFIXES.TryGetValue(form, out var suffixes))
        {
            foreach (var name in suffixes.Select(s => number + s))
            {
                yield return name;
            }
        }
    }
}
