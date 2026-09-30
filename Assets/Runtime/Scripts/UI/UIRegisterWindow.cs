using DG.Tweening;
using TMPro;
using UnityEngine;
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

    private Tween _openTween;

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

        KillOpenTween();
        ResetPanelScale();
    }

    private void Update()
    {
        if (IsAnyFieldFocused())
        {
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
    public void Open()
    {
        if (gameObject.activeSelf)
        {
            return;
        }

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
