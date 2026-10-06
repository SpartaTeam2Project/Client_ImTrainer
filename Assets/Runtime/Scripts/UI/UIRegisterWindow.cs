using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// 회원가입 창에서 닉네임과 비밀번호를 입력받고, 확인과 취소로 창을 다룬다.
/// </summary>
public class UIRegisterWindow : MonoBehaviour
{
    private const string OPEN_SOUND = "register_open";
    private const string CLOSE_SOUND = "register_close";
    private const float OPEN_DURATION = 0.25f;

    [SerializeField] private RectTransform _panel;
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private TMP_InputField _passwordConfirmInput;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    [Header("System Message Box")]
    [SerializeField] private GameObject _systemMessageRoot;
    [SerializeField] private TMP_Text _systemMessage;
    [SerializeField] private Button _okButton;

    private Tween _openTween;
    private Action _onSignupSuccess;
    private readonly ApiClient _apiClient=new ApiClient();

    public bool IsOpen => gameObject.activeSelf;

    private void OnEnable()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("회원가입 창에 필요한 참조가 없습니다.");
        }

        if (_confirmButton != null)
        {
            _confirmButton.onClick.AddListener(Confirm);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.AddListener(Close);
        }

        _okButton.onClick.AddListener(CloseSystemMessage);
    }

    private void OnDisable()
    {
        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveListener(Confirm);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.RemoveListener(Close);
        }

        _okButton.onClick.RemoveListener(CloseSystemMessage);

        KillOpenTween();
        ResetPanelScale();
    }
    private void CloseSystemMessage()
    {
        _systemMessageRoot.SetActive(false);
    }

    private void Update()
    {
        if (IsAnyFieldFocused())
        {
            HandleTabNavigation();
            return;
        }

        if (Managers.Instance == null || !Managers.Instance.TryGetManager<InputManager>(out var inputManager))
        {
            return;
        }

        if (inputManager.ConsumeMenuCancel())
        {
            Close();
        }
    }

    /// <summary>
    /// 회원가입 창을 연다.
    /// </summary>
    public void Open(Action onSignupSuccess)
    {
        if (gameObject.activeSelf)
        {
            return;
        }
        
        _onSignupSuccess = onSignupSuccess;

        ClearFields();
        PlaySound(OPEN_SOUND);
        gameObject.SetActive(true);
        PlayOpen();
    }

    /// <summary>
    /// 회원가입 창을 닫고 입력칸을 비운다.
    /// </summary>
    public void Close()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        ClearFields();
        PlaySound(CLOSE_SOUND);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 확인 버튼. 계정 생성은 아직 연결하지 않는다.
    /// </summary>
    public void Confirm()
    {
        if (_passwordInput.text != _passwordConfirmInput.text)
        {
            ShowMessage("비밀번호가 일치하지 않습니다.");
            return;
        }

        StartCoroutine(_apiClient.PostSignup(_nicknameInput.text,_passwordInput.text,HandleSignupResponse));
    }

    private void HandleSignupResponse(ApiResult result)
    {
        if (result.RequestResult == UnityWebRequest.Result.ConnectionError)
        {
            ShowMessage("서버에 연결할 수 없습니다.\n잠시 후 다시 시도해주세요.");
            return;
        }

        if (result.StatusCode == 201)
        {
            Debug.Log("[UIRegisterWindow] 회원가입 성공");

            Close();

            // 로그인 화면 열기
            _onSignupSuccess?.Invoke();
            return;
        }

        if (result.StatusCode == 400 ||
            result.StatusCode == 409)
        {
            ErrorResponse error = ParseError(result.Body);

            ShowMessage(error?.message ?? "회원가입에 실패했습니다.");

            return;
        }

        ErrorResponse unknownError = ParseError(result.Body);

        ShowMessage(unknownError?.message ??"회원가입 처리 중 오류가 발생했습니다.");
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

    private bool HasRequiredReferences()
    {
        return _panel != null
            && _nicknameInput != null
            && _passwordInput != null
            && _passwordConfirmInput != null
            && _confirmButton != null
            && _cancelButton != null;
    }

    /// <summary>
    /// 상태창처럼 패널을 가운데에서 세로로 펼친다.
    /// </summary>
    private void PlayOpen()
    {
        if (_panel == null)
        {
            return;
        }

        KillOpenTween();
        _panel.localScale = new Vector3(1f, 0f, 1f);
        _openTween = _panel.DOScale(Vector3.one, OPEN_DURATION).SetEase(Ease.OutBack).SetUpdate(true);
    }

    private void KillOpenTween()
    {
        if (_openTween == null)
        {
            return;
        }

        _openTween.Kill();
        _openTween = null;
    }

    private void ResetPanelScale()
    {
        if (_panel == null)
        {
            return;
        }

        _panel.localScale = Vector3.one;
    }

    /// <summary>
    /// 입력 중 백스페이스가 메뉴 취소와 같아서, 칸에 포커스가 있으면 창을 닫지 않는다.
    /// </summary>
    private bool IsAnyFieldFocused()
    {
        return IsFocused(_nicknameInput) || IsFocused(_passwordInput) || IsFocused(_passwordConfirmInput);
    }

    private static bool IsFocused(TMP_InputField inputField)
    {
        return inputField != null && inputField.isFocused;
    }

    /// <summary>
    /// 입력 중 Tab은 다음 칸, Shift+Tab은 이전 칸으로 포커스를 옮긴다. 끝에서는 반대쪽 끝으로 돌아간다.
    /// </summary>
    private void HandleTabNavigation()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame)
        {
            return;
        }

        TMP_InputField[] fields = { _nicknameInput, _passwordInput, _passwordConfirmInput };
        var currentIndex = Array.FindIndex(fields, IsFocused);
        if (currentIndex < 0)
        {
            return;
        }

        var step = keyboard.shiftKey.isPressed ? -1 : 1;
        var nextIndex = (currentIndex + step + fields.Length) % fields.Length;
        var next = fields[nextIndex];
        if (next == null)
        {
            return;
        }

        fields[currentIndex].DeactivateInputField();
        next.Select();
        next.ActivateInputField();
    }

    private void ClearFields()
    {
        ClearField(_nicknameInput);
        ClearField(_passwordInput);
        ClearField(_passwordConfirmInput);
    }

    private static void ClearField(TMP_InputField inputField)
    {
        if (inputField == null)
        {
            return;
        }

        inputField.text = string.Empty;
    }

    private static void PlaySound(string soundName)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(soundName);
    }
}
