using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ButtonLogout : MonoBehaviour
{
    [SerializeField] private Button logoutButton;
    [SerializeField] private UILobbyWindow lobbyWindow;
    private const string BUTTON_CLICK_SOUND = "select";

    private readonly ApiClient _apiClient = new ApiClient();

    private void OnEnable()
    {
        logoutButton.onClick.AddListener(Logout);
    }
    private void OnDisable()
    {
        logoutButton.onClick.RemoveListener(Logout);
    }
    private void Logout()
    {
        PlaySelect();
        if (!TokenStorage.TryLoad(out string accessToken, out string refreshToken))
        {
            HandleLogoutSuccess();
            return;
        }

        StartCoroutine(_apiClient.PostLogout(accessToken, refreshToken, result => HandleLogoutResponse(result, refreshToken, true)));
    }
    private void HandleLogoutResponse(ApiResult result,string refreshToken,bool canRefresh)
    {
        if (result.RequestResult ==
            UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError("서버에 연결할 수 없습니다.");
            return;
        }

        if (result.StatusCode == 204)
        {
            TokenStorage.Clear();
            HandleLogoutSuccess();
            return;
        }

        if (result.StatusCode == 401 && canRefresh)
        {
            StartCoroutine(_apiClient.PostRequestToken(refreshToken,HandleLogoutRefreshResponse));
            return;
        }

        ErrorResponse error = ParseError(result.Body);

        Debug.LogError( $"로그아웃 실패: {result.StatusCode} {error?.message}");
    }
    private ErrorResponse ParseError(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return null;
        }
        return JsonUtility.FromJson<ErrorResponse>(body);
    }
    private void HandleLogoutRefreshResponse(ApiResult result)
    {
        if (result.StatusCode != 200)
        {
            TokenStorage.Clear();
            HandleLogoutSuccess();
            return;
        }

        RefreshTokenResponse response =
            JsonUtility.FromJson<RefreshTokenResponse>(result.Body);

        if (response == null ||string.IsNullOrEmpty(response.accessToken) ||string.IsNullOrEmpty(response.refreshToken))
        {
            TokenStorage.Clear();
            HandleLogoutSuccess();
            return;
        }

        TokenStorage.Save(response.accessToken,response.refreshToken);

        StartCoroutine(
            _apiClient.PostLogout
            (
            response.accessToken, response.refreshToken,
            logoutResult => HandleLogoutResponse(logoutResult,response.refreshToken,false)
            )
        );
    }

    private void HandleLogoutSuccess()
    {
        TokenStorage.Clear();

        lobbyWindow.ReturnToLobby();
    }
    private static void PlaySelect()
    {
        if (Managers.Instance == null ||
            !Managers.Instance.TryGetManager<AudioManager>(
                out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(BUTTON_CLICK_SOUND);
    }
}
