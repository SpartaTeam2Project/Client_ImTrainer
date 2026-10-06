using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient
{
    private const string BASE_URL = "http://43.203.49.129:8080";

    public IEnumerator GetMasterData(string accessToken, Action<ApiResult> onComplete)
    {
        using UnityWebRequest request = UnityWebRequest.Get(BASE_URL + "/master-data");
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);
        yield return request.SendWebRequest();

        ApiResult result = new ApiResult(request.responseCode, request.downloadHandler.text, request.result);

        onComplete?.Invoke(result);
    }
    public IEnumerator PostRequestToken(string refreshToken, Action<ApiResult> onComplete)
    {
        var requestBody = new RefreshTokenRequest(refreshToken);

        string json = JsonUtility.ToJson(requestBody);

        using UnityWebRequest request = UnityWebRequest.Post(BASE_URL + "/auth/refresh", json, "application/json");

        yield return request.SendWebRequest();

        ApiResult result = new ApiResult(request.responseCode, request.downloadHandler.text,request.result);
        onComplete?.Invoke(result);
    }

    public IEnumerator PostLogin(string username, string password, Action<ApiResult> onComplete)
    {
        LoginRequest requestBody = new LoginRequest(username, password);
        string json = JsonUtility.ToJson(requestBody);

        using UnityWebRequest request = UnityWebRequest.Post(BASE_URL+"/auth/login",json,"application/json");

        yield return request.SendWebRequest();

        ApiResult result = new ApiResult(request.responseCode,request.downloadHandler.text,request.result);

        onComplete?.Invoke(result);
    }
}

public class ApiResult
{
    public long StatusCode;
    public string Body;
    public UnityWebRequest.Result RequestResult;
    public ApiResult(long statusCode, string body, UnityWebRequest.Result requestResult)
    {
        StatusCode = statusCode;
        Body = body;
        RequestResult=requestResult;
    }
}
