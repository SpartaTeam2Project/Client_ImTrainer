using System;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UILoginWindow : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    [Header("System Message Box")]
    [SerializeField] private GameObject _systemMessageRoot;
    [SerializeField] private TMP_Text _systemMessage;
    [SerializeField] private Button _okButton;

    private readonly ApiClient _apiClient = new ApiClient();

    private Action _onLoginSuccess;

    public bool IsOpen => gameObject.activeSelf;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        _cancelButton.onClick.AddListener(Close);
        _confirmButton.onClick.AddListener(ConfirmLogin);
        _okButton.onClick.AddListener(CloseSystemMessage);
    }
    private void OnDisable()
    {
        _cancelButton.onClick.RemoveListener(Close);
        _confirmButton.onClick.RemoveListener(ConfirmLogin);
        _okButton.onClick.RemoveListener(CloseSystemMessage);
    }
    
    public void Open(Action onLoginSuccess)
    {
        _onLoginSuccess = onLoginSuccess;
        _nameInput.text = "";
        _passwordInput.text = "";

        _systemMessageRoot.SetActive(false);
        gameObject.SetActive(true);
    }

    public void Close()
    {
        _nameInput.text = "";
        _passwordInput.text = "";

        gameObject.SetActive(false);
    }
    private void CloseSystemMessage()
    {
        _systemMessageRoot.SetActive(false);
    }

    public void ConfirmLogin()
    {
        StartCoroutine(_apiClient.PostLogin(_nameInput.text, _passwordInput.text, HandleLoginResponse));
    }

    private void HandleLoginResponse(ApiResult result)
    {
        if (result.RequestResult == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError("서버에 연결할 수 없습니다.");
            ShowMessage("서버에 연결할 수 없습니다.\n잠시 후 다시 시도해주세요");
            return;
        }

        if (result.StatusCode == 200)
        {
            LoginResponse response=JsonUtility.FromJson<LoginResponse>(result.Body);
            
            if (response == null || string.IsNullOrEmpty(response.accessToken) || string.IsNullOrEmpty(response.refreshToken))
            {
                Debug.LogError("로그인 응답의 Token이 올바르지 않습니다.");
                ShowMessage("로그인 응답의 Token이 올바르지 않습니다.");
                return;
            }
            TokenStorage.Save(response.accessToken, response.refreshToken);
            StartCoroutine(_apiClient.GetMasterData(response.accessToken,HandleMasterDataResponse));
            return;
        }
        if(result.StatusCode == 400 || result.StatusCode == 401)
        {
            ErrorResponse error = ParseError(result.Body);

            Debug.LogError($"[UILoginWindow] 로그인 실패: {error?.message}");
            ShowMessage(error?.message ?? "아이디 또는 비밀번호를 확인해주세요.");
            return;
        }

        ErrorResponse unknownError = ParseError(result.Body);
        Debug.LogError($"[UILoginWindow] 로그인 요청 실패.: {result.StatusCode}: {unknownError?.message}");
        ShowMessage(unknownError?.message ?? "로그인 처리 중 오류가 발생했습니다.");
        return;
    }
    private void HandleMasterDataResponse(ApiResult result)
    {
        if (result.RequestResult == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError("[UILoginWindow] 서버 연결에 실패했습니다.");
            ShowMessage("서버에 연결할 수 없습니다.\n잠시 후 다시 시도해주세요");

            return;
        }

        if (result.StatusCode == 200)
        {

            MasterDataResponse response = JsonUtility.FromJson<MasterDataResponse>(result.Body);

            if (response == null)
            {
                Debug.LogError("[UILoginWindow] MasterData 응답을 읽지 못했습니다.");
                ShowMessage("MasterData 응답을 읽지 못했습니다.");
                return;
            }

            Debug.Log($"[UILoginWindow] MasterData 로드 성공. version={response.version}");

            Close();

            _onLoginSuccess?.Invoke();

            return;
        }
        ErrorResponse error = ParseError(result.Body);

        Debug.LogError($"[UILoginWindow] MasterData 요청 실패: {error?.message}");
        ShowMessage(error?.message ?? "MasterData를 불러오지 못했습니다.");
    }
    private ErrorResponse ParseError(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return null;
        }
        return JsonUtility.FromJson<ErrorResponse>(body);
    }
    private void ShowMessage(string message)
    {
        _systemMessage.SetText(message);
        _systemMessageRoot.SetActive(true);
    }
}
