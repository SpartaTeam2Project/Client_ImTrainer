using System;
using UnityEngine;

[Serializable]
public class LoginRequest
{
    public string username;
    public string password;

    public LoginRequest(string username, string password)
    {
        this.username = username;
        this.password = password;
    }
}

[Serializable]
public class LoginResponse
{
    public string accessToken;
    public string refreshToken;
    public int expiresIn;
}
