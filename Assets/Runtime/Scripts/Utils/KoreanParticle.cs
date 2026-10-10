/// <summary>
/// 이름 뒤에 붙일 한국어 조사를 받침에 맞춰 고른다.
/// </summary>
public static class KoreanParticle
{
    /// <summary>
    /// 이름 끝 글자에 받침이 있으면 "을", 없거나 한글이 아니면 "를".
    /// </summary>
    public static string Object(string name)
    {
        return HasFinalConsonant(name) ? "을" : "를";
    }

    /// <summary>
    /// 이름 끝 글자에 받침이 있으면 "은", 없거나 한글이 아니면 "는".
    /// </summary>
    public static string Topic(string name)
    {
        return HasFinalConsonant(name) ? "은" : "는";
    }

    private static bool HasFinalConsonant(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        var last = name[name.Length - 1];
        if (last < '가' || last > '힣')
        {
            return false;
        }

        return (last - '가') % 28 != 0;
    }
}
