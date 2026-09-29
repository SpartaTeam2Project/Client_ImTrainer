using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 인트로에서 고를 스타터 한 마리의 그림과 초상.
/// </summary>
[Serializable]
public class IntroStarterOption
{
    [SerializeField] private MonsterVisualData _visual;
    [SerializeField] private Sprite _portrait;

    public MonsterVisualData Visual => _visual;

    public Sprite Portrait => _portrait;
}

/// <summary>
/// 처음 입장한 계정에 대사, 성별, 스타터 선택을 보여 준다.
/// </summary>
[DefaultExecutionOrder(100)]
public class UIIntroScene : MonoBehaviour
{
    private const string PLAYER_NAME_TOKEN = "[플레이어이름]";
    private const string MENU_MOVE_SOUND = "cursor";
    private const string BUTTON_CLICK_SOUND = "select";
    private const string INTRO_MUSIC_NAME = "Intro";
    private const string TITLE_MUSIC_NAME = "TitleScene";
    private const string GENDER_PROMPT = "너는 남자니, 아니면 여자니?";
    private const string STARTER_PROMPT = "같이 떠날 포켓몬을 한 마리 골라 줘.";
    private const string DEFAULT_LINE = "안녕! [플레이어이름], 기다리가 해서 미안하구나";
    private const int STARTER_COUNT = 3;
    private const int GENDER_COUNT = 2;
    private const float CHARACTERS_PER_SECOND = 28f;
    private const float MIN_TYPE_DURATION = 0.15f;
    private const float OAK_FADE_DURATION = 0.6f;

    private static readonly Color SELECTED_COLOR = Color.white;
    private static readonly Color IDLE_COLOR = new Color(0.65f, 0.65f, 0.65f, 1f);

    private enum IntroPhase
    {
        Dialogue,
        Gender,
        Starter
    }

    [SerializeField] private TMP_Text _dialogueText;
    [SerializeField] private Image _oakImage;
    [SerializeField] private RectTransform _choiceRoot;
    [SerializeField] private GameObject _genderRoot;
    [SerializeField] private GameObject _boyRow;
    [SerializeField] private GameObject _girlRow;
    [SerializeField] private GameObject _boySelect;
    [SerializeField] private GameObject _girlSelect;
    [SerializeField] private GameObject[] _starterObjects = new GameObject[STARTER_COUNT];
    [SerializeField] private string[] _lines = { DEFAULT_LINE };
    [SerializeField] private IntroStarterOption[] _starters = new IntroStarterOption[STARTER_COUNT];

    private IntroPhase _phase;
    private int _lineIndex;
    private int _choiceIndex;
    private int _ignoreClickFrame;
    private bool _opening;
    private TrainerGender _selectedGender = TrainerGender.Boy;
    private Button[] _starterButtons;
    private Image[] _starterImages;
    private Tweener _typeTween;
    private Tweener _oakFade;
    private bool _isTyping;
    private bool _oakFading;

    public bool IsOpen => gameObject.activeSelf;

    #region Unity Methods

    private void Awake()
    {
        BindStarters();
        BuildChoices();
        if (!_opening)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        KillTypeTween();
        KillOakFade();
    }

    private void Update()
    {
        if (_phase == IntroPhase.Dialogue)
        {
            AdvanceDialogueFromInput();
            return;
        }

        MoveChoiceFromInput();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 대사를 처음부터 보여 준다.
    /// </summary>
    public void Open()
    {
        BindStarters();
        _ignoreClickFrame = Time.frameCount;
        _phase = IntroPhase.Dialogue;
        _lineIndex = 0;
        _choiceIndex = 0;
        _selectedGender = TrainerGender.Boy;
        _opening = true;
        gameObject.SetActive(true);
        _opening = false;
        ShowOak();
        ShowDialogueLine();
        SetChoiceVisible(false, false);
        PlayMusic(INTRO_MUSIC_NAME);
    }

    /// <summary>
    /// 인트로를 닫고 타이틀에 남긴다.
    /// </summary>
    public void Close()
    {
        PlayMusic(TITLE_MUSIC_NAME);
        gameObject.SetActive(false);
    }

    #endregion

    #region Private Methods

    private void BindStarters()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AccountManager>(out var accountManager))
        {
            return;
        }

        var visuals = new MonsterVisualData[STARTER_COUNT];
        for (var i = 0; i < STARTER_COUNT; i++)
        {
            var option = GetOption(i);
            visuals[i] = option != null ? option.Visual : null;
        }

        accountManager.BindStarterVisuals(visuals);
    }

    private void BuildChoices()
    {
        if (_dialogueText == null)
        {
            Debug.LogError("인트로 대사가 없습니다.");
            return;
        }

        _starterButtons = new Button[STARTER_COUNT];
        _starterImages = new Image[STARTER_COUNT];
        BindGenderRow(_boyRow, _boySelect, TrainerGender.Boy);
        BindGenderRow(_girlRow, _girlSelect, TrainerGender.Girl);
        BindStarterObjects();
        SetChoiceVisible(false, false);
        ApplyGenderSelect();
    }

    private void BindGenderRow(GameObject row, GameObject select, TrainerGender gender)
    {
        if (row == null || select == null)
        {
            Debug.LogError("성별 선택에 필요한 오브젝트가 없습니다.");
            return;
        }

        var selectImage = select.GetComponent<Image>();
        if (selectImage != null)
        {
            selectImage.raycastTarget = false;
        }

        var button = row.GetComponent<Button>();
        if (button == null)
        {
            button = row.AddComponent<Button>();
        }

        button.targetGraphic = row.GetComponent<Graphic>();
        button.transition = Selectable.Transition.None;
        var captured = gender;
        button.onClick.AddListener(() => ChooseGender(captured));
    }

    private void BindStarterObjects()
    {
        if (_starterObjects == null || _starterObjects.Length < STARTER_COUNT)
        {
            Debug.LogError("스타터 오브젝트가 없습니다.");
            return;
        }

        for (var i = 0; i < STARTER_COUNT; i++)
        {
            BindStarterObject(i);
        }
    }

    private void BindStarterObject(int index)
    {
        var starter = _starterObjects[index];
        if (starter == null)
        {
            Debug.LogError("스타터 오브젝트가 비어 있습니다.");
            return;
        }

        var image = starter.GetComponent<Image>();
        var button = starter.GetComponent<Button>();
        if (button == null)
        {
            button = starter.AddComponent<Button>();
        }

        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        var captured = index;
        button.onClick.AddListener(() => ChooseStarter(captured));
        _starterButtons[index] = button;
        _starterImages[index] = image;
    }

    private void AdvanceDialogueFromInput()
    {
        if (Time.frameCount == _ignoreClickFrame)
        {
            return;
        }

        if (_oakFading)
        {
            return;
        }

        var clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        var submitted = TryConsumeSubmit();
        if (!clicked && !submitted)
        {
            return;
        }

        if (_isTyping)
        {
            FinishTyping();
            return;
        }

        AdvanceDialogue();
    }

    private void AdvanceDialogue()
    {
        _lineIndex++;
        if (_lineIndex < Lines().Length)
        {
            ShowDialogueLine();
            return;
        }

        FadeOutOak();
    }

    private void BeginGender()
    {
        _oakFading = false;
        _phase = IntroPhase.Gender;
        _choiceIndex = 0;
        SetDialogue(GENDER_PROMPT);
        SetChoiceVisible(true, false);
        ApplyGenderSelect();
    }

    private void BeginStarter()
    {
        _phase = IntroPhase.Starter;
        _choiceIndex = 0;
        SetDialogue(STARTER_PROMPT);
        SetChoiceVisible(false, true);
        ApplyHighlight(_starterImages, _choiceIndex);
    }

    private void MoveChoiceFromInput()
    {
        if (!TryGetInput(out var inputManager))
        {
            return;
        }

        var move = inputManager.ConsumeMenuMove();
        if (_phase == IntroPhase.Gender)
        {
            if (move.y != 0)
            {
                MoveGender(-move.y);
            }
        }
        else if (move.x != 0)
        {
            ShiftChoice(move.x);
        }

        if (inputManager.ConsumeMenuSubmit())
        {
            ConfirmHighlighted();
        }
    }

    private void MoveGender(int direction)
    {
        var next = Mathf.Clamp(_choiceIndex + direction, 0, GENDER_COUNT - 1);
        if (next == _choiceIndex)
        {
            return;
        }

        _choiceIndex = next;
        ApplyGenderSelect();
        PlaySound(MENU_MOVE_SOUND);
    }

    private void ShiftChoice(int direction)
    {
        var previous = _choiceIndex;
        _choiceIndex = (_choiceIndex + direction + STARTER_COUNT) % STARTER_COUNT;
        if (_choiceIndex == previous)
        {
            return;
        }

        ApplyHighlight(_starterImages, _choiceIndex);
        PlaySound(MENU_MOVE_SOUND);
    }

    private void ConfirmHighlighted()
    {
        if (_phase == IntroPhase.Gender)
        {
            ChooseGender((TrainerGender)_choiceIndex);
            return;
        }

        ChooseStarter(_choiceIndex);
    }

    private void ChooseGender(TrainerGender gender)
    {
        _selectedGender = gender;
        _choiceIndex = (int)gender;
        PlaySound(BUTTON_CLICK_SOUND);
        BeginStarter();
    }

    private void ChooseStarter(int index)
    {
        var option = GetOption(index);
        var visual = option != null ? option.Visual : null;
        if (visual == null)
        {
            Debug.LogError("스타터 그림이 연결되지 않았습니다. 인트로 인스펙터의 스타터 칸을 채우세요.");
            return;
        }

        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AccountManager>(out var accountManager))
        {
            Debug.LogError("AccountManager를 찾을 수 없습니다.");
            return;
        }

        _choiceIndex = index;
        PlaySound(BUTTON_CLICK_SOUND);
        accountManager.CompleteIntro(_selectedGender, visual);
        Close();
    }

    private void ShowOak()
    {
        KillOakFade();
        if (_oakImage == null)
        {
            return;
        }

        var color = _oakImage.color;
        color.a = 1f;
        _oakImage.color = color;
    }

    private void FadeOutOak()
    {
        if (_oakImage == null)
        {
            BeginGender();
            return;
        }

        KillOakFade();
        _oakFading = true;
        _oakFade = _oakImage.DOFade(0f, OAK_FADE_DURATION).SetUpdate(true).OnComplete(BeginGender);
    }

    private void KillOakFade()
    {
        if (_oakFade != null && _oakFade.IsActive())
        {
            _oakFade.Kill();
        }

        _oakFade = null;
        _oakFading = false;
    }

    private void ShowDialogueLine()
    {
        var lines = Lines();
        if (_lineIndex < 0 || _lineIndex >= lines.Length)
        {
            return;
        }

        SetDialogue(ApplyPlayerName(lines[_lineIndex]));
    }

    private void SetDialogue(string line)
    {
        if (_dialogueText == null)
        {
            return;
        }

        KillTypeTween();
        _dialogueText.text = line ?? string.Empty;
        _dialogueText.maxVisibleCharacters = 0;
        _dialogueText.ForceMeshUpdate();
        var characterCount = _dialogueText.textInfo.characterCount;
        if (characterCount <= 0)
        {
            _dialogueText.maxVisibleCharacters = int.MaxValue;
            return;
        }

        var duration = Mathf.Max(MIN_TYPE_DURATION, characterCount / CHARACTERS_PER_SECOND);
        _isTyping = true;
        _typeTween = _dialogueText.DOMaxVisibleCharacters(characterCount, duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(FinishTypeState);
    }

    private void FinishTyping()
    {
        KillTypeTween();
        if (_dialogueText != null)
        {
            _dialogueText.maxVisibleCharacters = int.MaxValue;
        }
    }

    private void FinishTypeState()
    {
        _isTyping = false;
        _typeTween = null;
    }

    private void KillTypeTween()
    {
        if (_typeTween != null && _typeTween.IsActive())
        {
            _typeTween.Kill();
        }

        _typeTween = null;
        _isTyping = false;
    }

    private string[] Lines()
    {
        if (_lines != null && _lines.Length > 0)
        {
            return _lines;
        }

        return new[] { DEFAULT_LINE };
    }

    private IntroStarterOption GetOption(int index)
    {
        if (_starters == null || index < 0 || index >= _starters.Length)
        {
            return null;
        }

        return _starters[index];
    }

    private void ApplyGenderSelect()
    {
        if (_boySelect != null)
        {
            _boySelect.SetActive(_choiceIndex == (int)TrainerGender.Boy);
        }

        if (_girlSelect != null)
        {
            _girlSelect.SetActive(_choiceIndex == (int)TrainerGender.Girl);
        }
    }

    private void SetChoiceVisible(bool genderVisible, bool starterVisible)
    {
        if (_genderRoot != null)
        {
            _genderRoot.SetActive(genderVisible);
        }

        SetStarterObjectsActive(starterVisible);
    }

    private void SetStarterObjectsActive(bool active)
    {
        if (_starterObjects == null)
        {
            return;
        }

        for (var i = 0; i < _starterObjects.Length; i++)
        {
            if (_starterObjects[i] != null)
            {
                _starterObjects[i].SetActive(active);
            }
        }
    }

    private static void ApplyHighlight(Image[] images, int selectedIndex)
    {
        if (images == null)
        {
            return;
        }

        for (var i = 0; i < images.Length; i++)
        {
            if (images[i] == null)
            {
                continue;
            }

            images[i].color = i == selectedIndex ? SELECTED_COLOR : IDLE_COLOR;
        }
    }

    private static string ApplyPlayerName(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return string.Empty;
        }

        return line.Replace(PLAYER_NAME_TOKEN, string.Empty);
    }

    private bool TryConsumeSubmit()
    {
        if (!TryGetInput(out var inputManager))
        {
            return false;
        }

        return inputManager.ConsumeMenuSubmit();
    }

    private static bool TryGetInput(out InputManager inputManager)
    {
        inputManager = null;
        if (Managers.Instance == null)
        {
            return false;
        }

        return Managers.Instance.TryGetManager<InputManager>(out inputManager);
    }

    private static void PlaySound(string soundName)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(soundName);
    }

    private static void PlayMusic(string musicName)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlayMusic(musicName);
    }

    #endregion
}
