using System;

[Serializable]
public class SignupRequest
{
    public string username;
    public string password;

    public SignupRequest(string username, string password)
    {
        this.username = username;
        this.password = password;
    }
}
