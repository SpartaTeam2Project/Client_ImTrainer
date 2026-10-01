using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 로비에서 Login, Register, Settings 선택을 방향키로 바꾼다.
/// </summary>
public class UILobbyWindow : MonoBehaviour
{
    private const string MENU_MOVE_SOUND = "cursor";
    private const string BUTTON_CLICK_SOUND = "select";

    private enum LobbyButton
    {
        None,
        Login,
        Register,
        Settings,
    }

    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _registerButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Sprite _idleSprite;
    [SerializeField] private Sprite _selectedSprite;
    [SerializeField] private GameObject _settingsSelect;
    [SerializeField] private UIIntroScene _introScene;
    [SerializeField] private UIStorageWindow _storageWindow;
    [SerializeField] private UIRegisterWindow _registerWindow;

    private LobbyButton _selectedButton = LobbyButton.None;
    private LobbyButton _accountButtonBeforeSettings = LobbyButton.Login;

    private void Awake()
    {
        AddPointerEnter(_loginButton, LobbyButton.Login);
        AddPointerEnter(_registerButton, LobbyButton.Register);
        AddPointerEnter(_settingsButton, LobbyButton.Settings);
    }

    private void OnEnable()
    {
        if (_loginButton == null || _registerButton == null || _settingsButton == null || _settingsSelect == null || _registerWindow == null)
        {
            Debug.LogError("로비 버튼 선택에 필요한 참조가 없습니다.");
        }

        SubscribeButtonClicks();
        ApplySelection();
    }

    private void OnDisable()
    {
        UnsubscribeButtonClicks();
    }

    private void Update()
    {
        if (IsBlocked())
        {
            return;
        }

        if (Managers.Instance == null || !Managers.Instance.TryGetManager<InputManager>(out var inputManager))
        {
            return;
        }

        var move = inputManager.ConsumeMenuMove();
        if (move != Vector2Int.zero)
        {
            MoveSelection(move);
        }

        if (inputManager.ConsumeMenuSubmit())
        {
            SubmitSelection();
        }
    }

    private void MoveSelection(Vector2Int move)
    {
        var previous = _selectedButton;
        if (move.x > 0)
        {
            MoveRight();
        }
        else if (move.x < 0)
        {
            MoveLeft();
        }
        else if (move.y > 0)
        {
            MoveUp();
        }
        else if (move.y < 0)
        {
            MoveDown();
        }

        ApplySelection();
        if (_selectedButton != previous)
        {
            PlayMenuMove();
        }
    }

    // 다른 창이 위에 떠 있거나 화면 전환 중이면 로비 선택을 막는다.
    private bool IsBlocked()
    {
        var settingsWindow = GetSettingsWindow();
        if (settingsWindow != null && settingsWindow.IsOpen)
        {
            return true;
        }

        if (_introScene != null && _introScene.IsOpen)
        {
            return true;
        }

        if (_storageWindow != null && _storageWindow.IsOpen)
        {
            return true;
        }

        if (_registerWindow != null && _registerWindow.IsOpen)
        {
            return true;
        }

        return ScreenTransition.Instance.IsCovering;
    }

    private void AddPointerEnter(Button button, LobbyButton target)
    {
        if (button == null)
        {
            return;
        }

        var trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ => FocusButton(target));
        trigger.triggers.Add(entry);
    }

    /// <summary>
    /// 마우스가 올라온 버튼으로 선택을 옮긴다.
    /// </summary>
    private void FocusButton(LobbyButton target)
    {
        if (target == _selectedButton || IsBlocked())
        {
            return;
        }

        if (target == LobbyButton.Settings && (_selectedButton == LobbyButton.Login || _selectedButton == LobbyButton.Register))
        {
            _accountButtonBeforeSettings = _selectedButton;
        }

        _selectedButton = target;
        ApplySelection();
        PlayMenuMove();
    }

    private static void PlayMenuMove()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(MENU_MOVE_SOUND);
    }

    private void MoveRight()
    {
        if (_selectedButton == LobbyButton.None)
        {
            _selectedButton = LobbyButton.Login;
            return;
        }

        if (_selectedButton == LobbyButton.Login)
        {
            _selectedButton = LobbyButton.Register;
        }
    }

    private void MoveLeft()
    {
        if (_selectedButton == LobbyButton.None)
        {
            _selectedButton = LobbyButton.Register;
            return;
        }

        if (_selectedButton == LobbyButton.Register)
        {
            _selectedButton = LobbyButton.Login;
        }
    }

    private void MoveUp()
    {
        if (_selectedButton == LobbyButton.None)
        {
            _selectedButton = LobbyButton.Settings;
            return;
        }

        if (_selectedButton != LobbyButton.Login && _selectedButton != LobbyButton.Register)
        {
            return;
        }

        _accountButtonBeforeSettings = _selectedButton;
        _selectedButton = LobbyButton.Settings;
    }

    private void MoveDown()
    {
        if (_selectedButton != LobbyButton.Settings)
        {
            return;
        }

        _selectedButton = _accountButtonBeforeSettings;
    }

    private void ApplySelection()
    {
        SetAccountSprite(_loginButton, _selectedButton == LobbyButton.Login);
        SetAccountSprite(_registerButton, _selectedButton == LobbyButton.Register);
        if (_settingsSelect != null)
        {
            _settingsSelect.SetActive(_selectedButton == LobbyButton.Settings);
        }
    }

    private void SetAccountSprite(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        var image = button.targetGraphic as Image;
        if (image == null)
        {
            return;
        }

        image.sprite = selected ? _selectedSprite : _idleSprite;
    }

    private void SubmitSelection()
    {
        var button = GetSelectedButton();
        if (button == null)
        {
            return;
        }

        button.onClick.Invoke();
    }

    private void SubscribeButtonClicks()
    {
        if (_loginButton != null)
        {
            _loginButton.onClick.AddListener(PlayButtonClick);
            _loginButton.onClick.AddListener(EnterAccount);
        }

        if (_registerButton != null)
        {
            _registerButton.onClick.AddListener(PlayButtonClick);
            _registerButton.onClick.AddListener(OpenRegister);
        }

        if (_settingsButton != null)
        {
            _settingsButton.onClick.AddListener(PlayButtonClick);
            _settingsButton.onClick.AddListener(OpenSettings);
        }
    }

    private void UnsubscribeButtonClicks()
    {
        if (_loginButton != null)
        {
            _loginButton.onClick.RemoveListener(PlayButtonClick);
            _loginButton.onClick.RemoveListener(EnterAccount);
        }

        if (_registerButton != null)
        {
            _registerButton.onClick.RemoveListener(PlayButtonClick);
            _registerButton.onClick.RemoveListener(OpenRegister);
        }

        if (_settingsButton != null)
        {
            _settingsButton.onClick.RemoveListener(PlayButtonClick);
            _settingsButton.onClick.RemoveListener(OpenSettings);
        }
    }

    private void EnterAccount()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AccountManager>(out var accountManager))
        {
            Debug.LogError("AccountManager를 찾을 수 없습니다.");
            return;
        }

        if (!accountManager.AlwaysShowIntro)
        {
            ShowStorage();
            return;
        }

        if (_introScene == null)
        {
            Debug.LogError("인트로 화면이 없습니다.");
            return;
        }

        accountManager.ResetOwnedProfile();
        _introScene.Open(ShowStorage);
    }

    /// <summary>
    /// 화면 전환으로 화면을 덮은 뒤 스토리지 창을 열고 다시 걷어낸다.
    /// 인트로에서 이미 덮고 넘어왔으면 바로 창을 바꾼다.
    /// </summary>
    private void ShowStorage()
    {
        if (_storageWindow == null)
        {
            Debug.LogError("스토리지 창이 없습니다.");
            return;
        }

        var transition = ScreenTransition.Instance;
        if (transition.IsClosed)
        {
            SwapToStorage(transition);
            return;
        }

        if (transition.IsCovering)
        {
            return;
        }

        transition.Close().OnComplete(() => SwapToStorage(transition));
    }

    private void SwapToStorage(ScreenTransition transition)
    {
        gameObject.SetActive(false);
        _storageWindow.Open();
        transition.FadeOut();
    }

    private void OpenRegister()
    {
        if (_registerWindow == null)
        {
            Debug.LogError("회원가입 창이 없습니다.");
            return;
        }

        _registerWindow.Open();
    }

    private void OpenSettings()
    {
        var settingsWindow = GetSettingsWindow();
        if (settingsWindow == null)
        {
            Debug.LogError("설정 창이 없습니다.");
            return;
        }

        settingsWindow.Open();
    }

    private static UISettingsWindow GetSettingsWindow()
    {
        if (Managers.Instance == null)
        {
            return null;
        }

        return Managers.Instance.SettingsWindow;
    }

    private void PlayButtonClick()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(BUTTON_CLICK_SOUND);
    }

    private Button GetSelectedButton()
    {
        switch (_selectedButton)
        {
            case LobbyButton.Login:
                return _loginButton;
            case LobbyButton.Register:
                return _registerButton;
            case LobbyButton.Settings:
                return _settingsButton;
            default:
                return null;
        }
    }
}
