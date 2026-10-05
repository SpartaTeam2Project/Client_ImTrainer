using System;
using System.Collections;
using System.Transactions;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient
{
    private const string BASE_URL = "http://43.203.49.129:8080";

    public IEnumerator GetMasterData(string accessToken,Action<ApiResult>onComplete)
    {
        using UnityWebRequest request = UnityWebRequest.Get(BASE_URL + "/master-data");
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);
        yield return request.SendWebRequest();

        ApiResult result = new ApiResult(request.responseCode, request.downloadHandler.text);

        onComplete?.Invoke(result);
    }
    public IEnumerator PostRequestToken(string refreshToken, Action<ApiResult> onComplete)
    {
        var requestBody = new RefreshTokenRequest(refreshToken);

        string json = JsonUtility.ToJson(requestBody);

        using UnityWebRequest request = UnityWebRequest.Post(BASE_URL + "/auth/refresh",json,"application/json");

        yield return request.SendWebRequest();

        ApiResult result = new ApiResult(request.responseCode,request.downloadHandler.text);
        onComplete?.Invoke(result);
    }
}

public class ApiResult
{
    public long StatusCode;
    public string Body;
    public ApiResult(long  statusCode, string body)
    {
        StatusCode = statusCode;
        Body = body;
    }
}
