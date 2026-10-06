using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SplashController : MonoBehaviour
{
    [SerializeField] private GameObject _splashRoot;
    [SerializeField] private GameObject _titleRoot;
    [SerializeField] private TMP_Text announceText;
    [SerializeField] private Button okButton;

    private readonly ApiClient _apiClient = new ApiClient();


    void Start()
    {
        announceText.text = "Now Loading...";
        if (!TokenStorage.TryLoad(out string accessToken, out _))
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
        okButton.gameObject.SetActive(false);
        announceText.text = "Now Loading...";
        okButton.onClick.RemoveAllListeners();
    }

    private void HandleMasterDataResponse(ApiResult result, bool canRefresh)
    {
        if (result.RequestResult == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError("[SplashController] 서버 연결에 실패했습니다.");

            ShowError("서버에 연결할 수 없습니다.\n잠시 후 다시 시도해주세요.");

            return;
        }

        if (result.StatusCode == 200)
        {

            MasterDataResponse response = JsonUtility.FromJson<MasterDataResponse>(result.Body);

            if (response == null)
            {
                Debug.LogError("[SplashController] MasterData 응답을 읽지 못했습니다.");
                ShowError("MasterData를 불러오지 못했습니다.");
                return;
            }

            Debug.Log($"[SplashController] MasterData 로드 성공. version={response.version}");

            //storage 화면으로 이동 위치
            ShowTitle();
            return;
        }

        if (result.StatusCode == 401)
        {
            ErrorResponse error = ParseError(result.Body);
            Debug.Log($"[SplashController] 401 Unauthorized: {error?.message}");
            if (!canRefresh)
            {
                Debug.LogError("[SplashController] 재발급 후에도 인증에 실패했습니다.");
                TokenStorage.Clear();
                ShowError(error?.message ?? "인증에 실패하였습니다.");
                return;
            }
            HandleUnauthorized();
            return;
        }
        if (result.StatusCode >= 500)
        {
            ErrorResponse error = ParseError(result.Body);

            Debug.LogError($"[SplashController] 서버 오류. StatusCode: {result.StatusCode}");

            ShowError(error?.message ?? "서버에서 오류가 발생했습니다.\n잠시 후 다시 시도해주세요.");

            return;
        }
        Debug.LogError($"[SplashController] MasterData 요청 실패. StatusCode: {result.StatusCode}");
        ErrorResponse unknownError = ParseError(result.Body);
        ShowError(unknownError?.message ?? "요청 처리 중 오류가 발생했습니다.");
    }
    private void HandleUnauthorized()
    {
        if (!TokenStorage.TryLoad(out _, out string oldRefreshToken))
        {
            ShowTitle();
            return;
        }

        StartCoroutine(_apiClient.PostRequestToken(oldRefreshToken, HandleRefreshResponse));
    }

    private void HandleRefreshResponse(ApiResult result)
    {
        if (result.RequestResult == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError("[SplashController] Token 재발급 중 서버 연결에 실패했습니다.");

            ShowError("서버에 연결할 수 없습니다.\n잠시 후 다시 시도해주세요.");

            return;
        }
        if (result.StatusCode == 200)
        {
            RefreshTokenResponse response = JsonUtility.FromJson<RefreshTokenResponse>(result.Body);

            if (response == null || string.IsNullOrEmpty(response.accessToken) || string.IsNullOrEmpty(response.refreshToken))
            {
                Debug.LogError("[SplashController] Refresh 응답을 읽지 못했습니다.");
                ShowError("인증 정보를 갱신하지 못했습니다.");
                return;
            }

            TokenStorage.Save(response.accessToken, response.refreshToken);

            Debug.Log("[SplashController] Token 재발급 성공.");

            // 재발급된 accessToken으로 원래 요청을 1회 재시도한다.
            RequestMasterData(response.accessToken, false);

            return;
        }

        if (result.StatusCode == 401)
        {
            ErrorResponse error = ParseError(result.Body);
            Debug.Log($"[SplashController] Token 재발급 실패: {error?.message}");

            TokenStorage.Clear();

            ShowError(error?.message ?? "요청 처리중 오류가 발생하였습니다.");

            return;
        }
        if (result.StatusCode >= 500)
        {
            ErrorResponse error = ParseError(result.Body);

            Debug.LogError($"[SplashController] 서버 오류. StatusCode: {result.StatusCode}");

            ShowError(error?.message ?? "서버에서 오류가 발생했습니다.\n잠시 후 다시 시도해주세요.");

            return;
        }

        Debug.LogError($"[SplashController] Refresh 요청 실패. StatusCode: {result.StatusCode}");

        ErrorResponse unknownError = ParseError(result.Body);

        ShowError(unknownError?.message ?? "요청 처리 중 오류가 발생했습니다.");
    }

    private void RequestMasterData(string accessToken, bool canRefresh)
    {
        StartCoroutine(_apiClient.GetMasterData(accessToken, result => HandleMasterDataResponse(result, canRefresh)));
    }

    private ErrorResponse ParseError(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return null;
        }
        return JsonUtility.FromJson<ErrorResponse>(body);
    }

    private void ShowError(string message)
    {
        announceText.text = message;
        okButton.gameObject.SetActive(true);

        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(() => ShowTitle());
    }
}
