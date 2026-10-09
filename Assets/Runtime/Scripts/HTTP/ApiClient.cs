using System;
using System.Collections;
using System.Transactions;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient
{
    private const string BASE_URL = "http://43.203.49.129:8080";

    /// <summary>
    /// 판 결과 제출 경로. 서버가 정해지면 채운다(제안: "/rank/stage-results"). 비어 있으면 RankManager는 로컬에만 보관한다.
    /// </summary>
    public const string STAGE_RESULT_PATH = "";

    public static bool HasStageResultPath => !string.IsNullOrEmpty(STAGE_RESULT_PATH);

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
    public IEnumerator PostSignup(string username, string password, Action<ApiResult> onComplete)
    {
        SignupRequest requestBody = new SignupRequest(username, password);
        string json = JsonUtility.ToJson(requestBody);

        using UnityWebRequest request = UnityWebRequest.Post(BASE_URL + "/auth/signup", json, "application/json");

        yield return request.SendWebRequest();

        ApiResult result = new ApiResult(request.responseCode, request.downloadHandler.text, request.result);

        onComplete?.Invoke(result);
    }
    /// <summary>
    /// 판 결과 JSON을 리더보드 서버에 보낸다. json은 StageResultRequest를 직렬화한 문자열이다.
    /// </summary>
    public IEnumerator PostStageResult(string accessToken, string json, Action<ApiResult> onComplete)
    {
        using UnityWebRequest request = UnityWebRequest.Post(BASE_URL + STAGE_RESULT_PATH, json, "application/json");

        request.SetRequestHeader("Authorization", $"Bearer {accessToken}");

        yield return request.SendWebRequest();

        ApiResult result = new ApiResult(request.responseCode, request.downloadHandler.text, request.result);

        onComplete?.Invoke(result);
    }

    public IEnumerator PostLogout(string accessToken, string refreshToken, Action<ApiResult> onComplete)
    {
        RefreshTokenRequest requestBody = new RefreshTokenRequest(refreshToken);

        string json = JsonUtility.ToJson(requestBody);

        using UnityWebRequest request = UnityWebRequest.Post(BASE_URL + "/auth/logout", json, "application/json");

        request.SetRequestHeader("Authorization", $"Bearer {accessToken}");

        yield return request.SendWebRequest();

        ApiResult result = new ApiResult(request.responseCode,request.downloadHandler.text, request.result);

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
