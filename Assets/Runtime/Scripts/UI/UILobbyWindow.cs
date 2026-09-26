using UnityEngine;
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

    private LobbyButton _selectedButton = LobbyButton.None;
    private LobbyButton _accountButtonBeforeSettings = LobbyButton.Login;

    private void OnEnable()
    {
        if (_loginButton == null || _registerButton == null || _settingsButton == null || _settingsSelect == null)
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
        if (_selectedButton != previous && Managers.Instance != null && Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            audioManager.PlaySound(MENU_MOVE_SOUND);
        }
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
        }

        if (_registerButton != null)
        {
            _registerButton.onClick.AddListener(PlayButtonClick);
        }

        if (_settingsButton != null)
        {
            _settingsButton.onClick.AddListener(PlayButtonClick);
        }
    }

    private void UnsubscribeButtonClicks()
    {
        if (_loginButton != null)
        {
            _loginButton.onClick.RemoveListener(PlayButtonClick);
        }

        if (_registerButton != null)
        {
            _registerButton.onClick.RemoveListener(PlayButtonClick);
        }

        if (_settingsButton != null)
        {
            _settingsButton.onClick.RemoveListener(PlayButtonClick);
        }
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
