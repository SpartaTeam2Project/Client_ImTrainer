using System;
using UnityEngine;

[Serializable]
public class RefreshTokenRequest
{
    public string refreshToken;

    public RefreshTokenRequest(string refreshToken)
    {
        this.refreshToken = refreshToken;
    }
}
[Serializable]
public class RefreshTokenResponse
{
    public string accessToken;
    public string refreshToken;
}