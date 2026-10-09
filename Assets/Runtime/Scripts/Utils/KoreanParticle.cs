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
        if (string.IsNullOrEmpty(name))
        {
            return "를";
        }

        var last = name[name.Length - 1];
        if (last < '가' || last > '힣')
        {
            return "를";
        }

        return (last - '가') % 28 != 0 ? "을" : "를";
    }
}
