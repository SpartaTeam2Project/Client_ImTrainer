using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class SplashController : MonoBehaviour
{
    [SerializeField] private GameObject _splashRoot;
    [SerializeField] private GameObject _titleRoot;

    private readonly ApiClient _apiClient = new ApiClient();

    private const string BASE_URL = "http://43.203.49.129:8080";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!TokenStorage.TryLoad(out string accessToken, out string refreshToken))
        {
            ShowTitle();
            return;
        }

        Debug.Log("[SplashController] 저장된 토큰을 찾았습니다.");

        RequestMasterData(accessToken, true);
    }

    private void ShowTitle()
    {
        _splashRoot.SetActive(false);
        _titleRoot.SetActive(true);
    }

    private void HandleMasterDataResponse(ApiResult result,bool canRefresh)
    {
        if (result.StatusCode == 200)
        {
            MasterDataResponse response =JsonUtility.FromJson<MasterDataResponse>(result.Body);

            if (response == null)
            {
                Debug.LogError(
                    "[SplashController] MasterData 응답을 읽지 못했습니다."
                );
                return;
            }

            Debug.Log(
                $"[SplashController] MasterData 로드 성공. version={response.version}"
            );

            return;
        }

        if (result.StatusCode == 401)
        {
            Debug.Log(
                $"[SplashController] 401 Unauthorized: {result.Body}"
            );
            if (!canRefresh)
            {
                Debug.LogError(
                "[SplashController] 재발급 후에도 인증에 실패했습니다."
            );

                TokenStorage.Clear();
                ShowTitle();
                return;
            }
            HandleUnauthorized();
            return;
        }

        Debug.LogError(
            $"[SplashController] MasterData 요청 실패. StatusCode: {result.StatusCode}"
        );
    }
    private void HandleUnauthorized()
    {
        if (!TokenStorage.TryLoad(
                out string oldAccessToken,
                out string oldRefreshToken))
        {
            ShowTitle();
            return;
        }

        StartCoroutine(_apiClient.PostRequestToken(oldRefreshToken, HandleRefreshResponse));
    }

    private void HandleRefreshResponse(ApiResult result)
    {
        if (result.StatusCode == 200)
        {
            RefreshTokenResponse response =
                JsonUtility.FromJson<RefreshTokenResponse>(
                    result.Body
                );

            if (response == null ||string.IsNullOrEmpty(response.accessToken) ||string.IsNullOrEmpty(response.refreshToken))
            {
                Debug.LogError("[SplashController] Refresh 응답을 읽지 못했습니다.");
                return;
            }

            TokenStorage.Save(
                response.accessToken,
                response.refreshToken
            );

            Debug.Log("[SplashController] Token 재발급 성공.");

            // 재발급된 accessToken으로 원래 요청을 1회 재시도한다.
            RequestMasterData(response.accessToken, false);

            return;
        }

        if (result.StatusCode == 401)
        {
            Debug.Log($"[SplashController] Token 재발급 실패: {result.Body}");

            TokenStorage.Clear();
            ShowTitle();

            return;
        }

        Debug.LogError($"[SplashController] Refresh 요청 실패. StatusCode: {result.StatusCode}");
    }

    private void RequestMasterData(string accessToken, bool canRefresh)
    {
        StartCoroutine(
            _apiClient.GetMasterData(
                accessToken,
                result => HandleMasterDataResponse(result, canRefresh)
            )
        );
    }
}
