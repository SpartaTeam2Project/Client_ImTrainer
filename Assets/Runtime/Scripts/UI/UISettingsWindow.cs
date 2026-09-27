using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 마스터, BGM, SE 볼륨을 고르고 슬라이더로 조절한다.
/// </summary>
public class UISettingsWindow : MonoBehaviour
{
    private const string MENU_MOVE_SOUND = "cursor";
    private const string OPEN_SOUND = "open";
    private const string CLOSE_SOUND = "close";
    private const float VOLUME_STEP = 0.05f;
    private const int PERCENT_SCALE = 100;
    private const int MASTER_INDEX = 0;
    private const int MUSIC_INDEX = 1;
    private const int SOUND_INDEX = 2;

    [SerializeField] private VolumeRow _masterRow;
    [SerializeField] private VolumeRow _musicRow;
    [SerializeField] private VolumeRow _soundRow;
    [SerializeField] private Button _backButton;

    private VolumeRow[] _rows;
    private int _selectedIndex;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        _rows = new[] { _masterRow, _musicRow, _soundRow };
        DisableSliderNavigation();
        if (_backButton != null)
        {
            _backButton.onClick.AddListener(Close);
        }
    }

    private void OnDestroy()
    {
        if (_backButton != null)
        {
            _backButton.onClick.RemoveListener(Close);
        }
    }

    private void OnEnable()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("설정 창에 필요한 참조가 없습니다.");
        }

        SubscribeControls();
        LoadSavedVolumes();
        _selectedIndex = MASTER_INDEX;
        ApplySelection();
    }

    private void OnDisable()
    {
        UnsubscribeControls();
    }

    private void Update()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<InputManager>(out var inputManager))
        {
            return;
        }

        var move = inputManager.ConsumeMenuMove();
        if (move.y != 0)
        {
            MoveSelection(move.y);
        }
        else if (move.x != 0)
        {
            AdjustSelectedVolume(move.x);
        }

        if (inputManager.ConsumePausePressed())
        {
            Close();
        }
    }

    /// <summary>
    /// 설정 창을 열고 저장된 볼륨을 슬라이더에 맞춘다.
    /// </summary>
    public void Open()
    {
        if (gameObject.activeSelf)
        {
            return;
        }

        PlaySound(OPEN_SOUND);
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 설정 창을 닫는다.
    /// </summary>
    public void Close()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        PlaySound(CLOSE_SOUND);
        gameObject.SetActive(false);
    }

    private bool HasRequiredReferences()
    {
        if (_backButton == null || _rows == null)
        {
            return false;
        }

        for (var i = 0; i < _rows.Length; i++)
        {
            if (_rows[i] == null || !_rows[i].HasReferences())
            {
                return false;
            }
        }

        return true;
    }

    private void DisableSliderNavigation()
    {
        for (var i = 0; i < _rows.Length; i++)
        {
            if (_rows[i] == null || _rows[i].Slider == null)
            {
                continue;
            }

            var navigation = _rows[i].Slider.navigation;
            navigation.mode = Navigation.Mode.None;
            _rows[i].Slider.navigation = navigation;
        }
    }

    private void SubscribeControls()
    {
        SubscribeSlider(_masterRow, HandleMasterVolumeChanged);
        SubscribeSlider(_musicRow, HandleMusicVolumeChanged);
        SubscribeSlider(_soundRow, HandleSoundVolumeChanged);
    }

    private void UnsubscribeControls()
    {
        UnsubscribeSlider(_masterRow, HandleMasterVolumeChanged);
        UnsubscribeSlider(_musicRow, HandleMusicVolumeChanged);
        UnsubscribeSlider(_soundRow, HandleSoundVolumeChanged);
    }

    private static void SubscribeSlider(VolumeRow row, UnityEngine.Events.UnityAction<float> handler)
    {
        if (row == null || row.Slider == null)
        {
            return;
        }

        row.Slider.onValueChanged.AddListener(handler);
    }

    private static void UnsubscribeSlider(VolumeRow row, UnityEngine.Events.UnityAction<float> handler)
    {
        if (row == null || row.Slider == null)
        {
            return;
        }

        row.Slider.onValueChanged.RemoveListener(handler);
    }

    private void LoadSavedVolumes()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        SetSliderVolume(_masterRow, audioManager.MasterVolume);
        SetSliderVolume(_musicRow, audioManager.MusicVolume);
        SetSliderVolume(_soundRow, audioManager.SoundVolume);
    }

    private static void SetSliderVolume(VolumeRow row, float volume)
    {
        if (row == null || row.Slider == null)
        {
            return;
        }

        row.Slider.SetValueWithoutNotify(volume);
        row.SetPercentText(volume);
    }

    private void MoveSelection(int direction)
    {
        var previous = _selectedIndex;
        if (direction > 0)
        {
            _selectedIndex = Mathf.Max(MASTER_INDEX, _selectedIndex - 1);
        }
        else
        {
            _selectedIndex = Mathf.Min(_rows.Length - 1, _selectedIndex + 1);
        }

        if (_selectedIndex == previous)
        {
            return;
        }

        ApplySelection();
        PlayMoveSound();
    }

    private void AdjustSelectedVolume(int direction)
    {
        var row = _rows[_selectedIndex];
        if (row == null || row.Slider == null)
        {
            return;
        }

        var steps = Mathf.Round((row.Slider.value + direction * VOLUME_STEP) / VOLUME_STEP);
        row.Slider.value = Mathf.Clamp01(steps * VOLUME_STEP);
    }

    private void ApplySelection()
    {
        for (var i = 0; i < _rows.Length; i++)
        {
            if (_rows[i] == null)
            {
                continue;
            }

            _rows[i].SetSelected(i == _selectedIndex);
        }
    }

    private void HandleMasterVolumeChanged(float value)
    {
        ApplyVolume(MASTER_INDEX, value);
    }

    private void HandleMusicVolumeChanged(float value)
    {
        ApplyVolume(MUSIC_INDEX, value);
    }

    private void HandleSoundVolumeChanged(float value)
    {
        ApplyVolume(SOUND_INDEX, value);
    }

    private void ApplyVolume(int rowIndex, float value)
    {
        if (_rows[rowIndex] != null)
        {
            _rows[rowIndex].SetPercentText(value);
        }

        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        switch (rowIndex)
        {
            case MASTER_INDEX:
                audioManager.MasterVolume = value;
                break;
            case MUSIC_INDEX:
                audioManager.MusicVolume = value;
                break;
            case SOUND_INDEX:
                audioManager.SoundVolume = value;
                break;
        }
    }

    private void PlayMoveSound()
    {
        PlaySound(MENU_MOVE_SOUND);
    }

    private static void PlaySound(string soundName)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(soundName);
    }

    [System.Serializable]
    private sealed class VolumeRow
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private GameObject _subtitle;
        [SerializeField] private GameObject _select;
        [SerializeField] private TMP_Text _valueText;

        public Slider Slider => _slider;

        public bool HasReferences()
        {
            return _slider != null && _subtitle != null && _select != null && _valueText != null;
        }

        public void SetSelected(bool selected)
        {
            if (_subtitle != null)
            {
                _subtitle.SetActive(!selected);
            }

            if (_select != null)
            {
                _select.SetActive(selected);
            }
        }

        public void SetPercentText(float volume)
        {
            if (_valueText == null)
            {
                return;
            }

            _valueText.text = Mathf.RoundToInt(volume * PERCENT_SCALE).ToString();
        }
    }
}
