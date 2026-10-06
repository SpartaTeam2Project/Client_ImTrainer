using UnityEngine;

public static class TokenStorage
{
    private const string ACCESS_TOKEN_KEY = "access_token";
    private const string REFRESH_TOKEN_KEY = "refresh_token";

    public static bool TryLoad(out string accessToken, out string refreshToken)
    {
        accessToken = PlayerPrefs.GetString(ACCESS_TOKEN_KEY);
        refreshToken = PlayerPrefs.GetString(REFRESH_TOKEN_KEY);
        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.Log("[TokenStorage]accessToken이 없습니다.");
            return false;
        }
        if (string.IsNullOrEmpty(refreshToken)) {
            Debug.Log("[TokenStorage]refreshToken이 없습니다.");
            return false;
        }
        return true;
    }

    public static void Save(string accessToken, string refreshToken)
    {
        PlayerPrefs.SetString(ACCESS_TOKEN_KEY, accessToken);
        PlayerPrefs.SetString (REFRESH_TOKEN_KEY, refreshToken);
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(ACCESS_TOKEN_KEY);
        PlayerPrefs.DeleteKey(REFRESH_TOKEN_KEY);
        PlayerPrefs.Save();
    }
}
