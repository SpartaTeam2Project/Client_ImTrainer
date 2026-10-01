using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private const string MESSAGE_SOUND = "message";
    private const string INTRO_MUSIC_NAME = "Intro";
    private const string TITLE_MUSIC_NAME = "TitleScene";
    private const string GENDER_PROMPT = "너는 남자니, 아니면 여자니?";
    private const string STARTER_PROMPT = "밖은 위험하니 이 아이들중 하나를 데려가렴.";
    private const string STARTER_CONFIRM_PROMPT = "{0}{1} 고를거니?";
    private const string DEFAULT_LINE = "안녕! [플레이어이름], 기다리가 해서 미안하구나";
    private const int STARTER_COUNT = 3;
    private const int GENDER_COUNT = 2;
    private const int ANSWER_COUNT = 2;
    private const int ANSWER_YES = 0;
    private const int ANSWER_NO = 1;
    private const float CHARACTERS_PER_SECOND = 28f;
    private const float MIN_TYPE_DURATION = 0.15f;
    private const float OAK_FADE_DURATION = 0.6f;
    private const float INTRO_STEP_DURATION = 0.4f;

    private static readonly Color SELECTED_COLOR = Color.white;
    private static readonly Color IDLE_COLOR = new Color(0.65f, 0.65f, 0.65f, 1f);

    private enum IntroPhase
    {
        Dialogue,
        Gender,
        Starter,
        Answer
    }

    [SerializeField] private TMP_Text _dialogueText;
    [SerializeField] private Image _oakImage;
    [SerializeField] private GameObject _background;
    [SerializeField] private GameObject _introBase;
    [SerializeField] private GameObject _board;
    [SerializeField] private RectTransform _choiceRoot;
    [SerializeField] private GameObject _genderRoot;
    [SerializeField] private GameObject _boyRow;
    [SerializeField] private GameObject _girlRow;
    [SerializeField] private GameObject _boySelect;
    [SerializeField] private GameObject _girlSelect;
    [SerializeField] private GameObject _answerRoot;
    [SerializeField] private GameObject _yesRow;
    [SerializeField] private GameObject _noRow;
    [SerializeField] private GameObject _yesSelect;
    [SerializeField] private GameObject _noSelect;
    [SerializeField] private GameObject[] _starterObjects = new GameObject[STARTER_COUNT];
    [SerializeField] private string[] _lines = { DEFAULT_LINE };
    [SerializeField] private IntroStarterOption[] _starters = new IntroStarterOption[STARTER_COUNT];

    private IntroPhase _phase;
    private int _lineIndex;
    private int _choiceIndex;
    private int _pendingStarter;
    private int _ignoreClickFrame;
    private bool _opening;
    private TrainerGender _selectedGender = TrainerGender.Boy;
    private Button[] _starterButtons;
    private Image[] _starterImages;
    private Tweener _typeTween;
    private Tweener _oakFade;
    private Sequence _introSequence;
    private bool _isTyping;
    private bool _oakFading;
    private bool _introPlaying;
    private Action _onClosed;

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
        KillIntroSequence();
    }

    private void Update()
    {
        if (ScreenTransition.Instance.IsCovering)
        {
            return;
        }

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
    /// 대사를 처음부터 보여 준다. 닫히면 onClosed를 호출한다.
    /// </summary>
    public void Open(Action onClosed = null)
    {
        _onClosed = onClosed;
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
        SetChoiceVisible(false, false);
        PlayMusic(INTRO_MUSIC_NAME);
        PlayIntroSequence();
    }

    /// <summary>
    /// 인트로를 닫고 타이틀에 남긴다.
    /// </summary>
    public void Close()
    {
        PlayMusic(TITLE_MUSIC_NAME);
        gameObject.SetActive(false);
        var onClosed = _onClosed;
        _onClosed = null;
        onClosed?.Invoke();
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
        BindChoiceRow(_boyRow, _boySelect, () => ChooseGender(TrainerGender.Boy), () => FocusGender((int)TrainerGender.Boy));
        BindChoiceRow(_girlRow, _girlSelect, () => ChooseGender(TrainerGender.Girl), () => FocusGender((int)TrainerGender.Girl));
        BindChoiceRow(_yesRow, _yesSelect, () => ChooseAnswer(ANSWER_YES), () => FocusAnswer(ANSWER_YES));
        BindChoiceRow(_noRow, _noSelect, () => ChooseAnswer(ANSWER_NO), () => FocusAnswer(ANSWER_NO));
        BindStarterObjects();
        SetChoiceVisible(false, false);
        ApplyGenderSelect();
    }

    private void BindChoiceRow(GameObject row, GameObject select, Action onClick, Action onEnter)
    {
        if (row == null || select == null)
        {
            Debug.LogError("선택지에 필요한 오브젝트가 없습니다.");
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
        button.onClick.AddListener(() => onClick());
        AddPointerEnter(row, onEnter);
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
        AddPointerEnter(starter, () => FocusStarter(captured));
        _starterButtons[index] = button;
        _starterImages[index] = image;
    }

    private static void AddPointerEnter(GameObject target, Action onEnter)
    {
        var trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = target.AddComponent<EventTrigger>();
        }

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ => onEnter());
        trigger.triggers.Add(entry);
    }

    private void FocusStarter(int index)
    {
        if (_phase != IntroPhase.Starter || index == _choiceIndex)
        {
            return;
        }

        _choiceIndex = index;
        ApplyHighlight(_starterImages, _choiceIndex);
        PlaySound(MENU_MOVE_SOUND);
    }

    private void AdvanceDialogueFromInput()
    {
        if (Time.frameCount == _ignoreClickFrame)
        {
            return;
        }

        if (_introPlaying || _oakFading)
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

    private void BeginStarter(int focusIndex = 0)
    {
        _phase = IntroPhase.Starter;
        _choiceIndex = focusIndex;
        SetDialogue(STARTER_PROMPT);
        SetChoiceVisible(false, true);
        ApplyHighlight(_starterImages, _choiceIndex);
    }

    private void BeginAnswer(int starterIndex, MonsterVisualData visual)
    {
        _phase = IntroPhase.Answer;
        _pendingStarter = starterIndex;
        _choiceIndex = ANSWER_YES;
        SetDialogue(string.Format(STARTER_CONFIRM_PROMPT, visual.MonsterName, ObjectParticle(visual.MonsterName)));
        SetChoiceVisible(false, true, true);
        ApplyHighlight(_starterImages, _pendingStarter);
        ApplyAnswerSelect();
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
        else if (_phase == IntroPhase.Answer)
        {
            if (move.y != 0)
            {
                FocusAnswer(Mathf.Clamp(_choiceIndex - move.y, 0, ANSWER_COUNT - 1));
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

        if (inputManager.ConsumeMenuCancel() && _phase == IntroPhase.Answer)
        {
            ChooseAnswer(ANSWER_NO);
        }
    }

    private void MoveGender(int direction)
    {
        FocusGender(Mathf.Clamp(_choiceIndex + direction, 0, GENDER_COUNT - 1));
    }

    private void FocusGender(int index)
    {
        if (_phase != IntroPhase.Gender || index == _choiceIndex)
        {
            return;
        }

        _choiceIndex = index;
        ApplyGenderSelect();
        PlaySound(MENU_MOVE_SOUND);
    }

    private void FocusAnswer(int index)
    {
        if (_phase != IntroPhase.Answer || index == _choiceIndex)
        {
            return;
        }

        _choiceIndex = index;
        ApplyAnswerSelect();
        PlaySound(MENU_MOVE_SOUND);
    }

    private void ShiftChoice(int direction)
    {
        FocusStarter((_choiceIndex + direction + STARTER_COUNT) % STARTER_COUNT);
    }

    private void ConfirmHighlighted()
    {
        if (_phase == IntroPhase.Gender)
        {
            ChooseGender((TrainerGender)_choiceIndex);
            return;
        }

        if (_phase == IntroPhase.Answer)
        {
            ChooseAnswer(_choiceIndex);
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
        if (_phase != IntroPhase.Starter)
        {
            return;
        }

        var visual = GetStarterVisual(index);
        if (visual == null)
        {
            return;
        }

        PlaySound(BUTTON_CLICK_SOUND);
        BeginAnswer(index, visual);
    }

    private void ChooseAnswer(int answer)
    {
        if (_phase != IntroPhase.Answer)
        {
            return;
        }

        if (answer == ANSWER_YES)
        {
            CompleteStarter(_pendingStarter);
            return;
        }

        PlaySound(BUTTON_CLICK_SOUND);
        BeginStarter(_pendingStarter);
    }

    private MonsterVisualData GetStarterVisual(int index)
    {
        var option = GetOption(index);
        var visual = option != null ? option.Visual : null;
        if (visual == null)
        {
            Debug.LogError("스타터 그림이 연결되지 않았습니다. 인트로 인스펙터의 스타터 칸을 채우세요.");
        }

        return visual;
    }

    private void CompleteStarter(int index)
    {
        var visual = GetStarterVisual(index);
        if (visual == null)
        {
            return;
        }

        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AccountManager>(out var accountManager))
        {
            Debug.LogError("AccountManager를 찾을 수 없습니다.");
            return;
        }

        var transition = ScreenTransition.Instance;
        if (transition.IsCovering)
        {
            return;
        }

        _choiceIndex = index;
        PlaySound(BUTTON_CLICK_SOUND);
        accountManager.CompleteIntro(_selectedGender, visual);

        // 인트로를 띄운 채로 화면을 덮고, 다 덮이면 닫는다.
        transition.Close().OnComplete(Close);
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

    private void PlayIntroSequence()
    {
        KillIntroSequence();
        if (_dialogueText != null)
        {
            _dialogueText.text = string.Empty;
        }

        _introPlaying = true;
        _introSequence = DOTween.Sequence().SetUpdate(true);
        AppendIntroFade(_background);
        AppendIntroFade(_introBase);
        AppendIntroFade(_oakImage != null ? _oakImage.gameObject : null);
        AppendIntroFade(_board);
        _introSequence.OnComplete(FinishIntroSequence);
    }

    private void AppendIntroFade(GameObject target)
    {
        if (target == null || _introSequence == null)
        {
            return;
        }

        var group = GetOrAddCanvasGroup(target);
        group.alpha = 0f;
        _introSequence.Append(group.DOFade(1f, INTRO_STEP_DURATION).SetEase(Ease.OutSine));
    }

    private void FinishIntroSequence()
    {
        _introPlaying = false;
        _introSequence = null;
        ShowDialogueLine();
    }

    private void KillIntroSequence()
    {
        if (_introSequence != null && _introSequence.IsActive())
        {
            _introSequence.Kill();
        }

        _introSequence = null;
        _introPlaying = false;
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        var group = target.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = target.AddComponent<CanvasGroup>();
        }

        return group;
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

        PlaySound(MESSAGE_SOUND);
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

    private void ApplyAnswerSelect()
    {
        if (_yesSelect != null)
        {
            _yesSelect.SetActive(_choiceIndex == ANSWER_YES);
        }

        if (_noSelect != null)
        {
            _noSelect.SetActive(_choiceIndex == ANSWER_NO);
        }
    }

    private void SetChoiceVisible(bool genderVisible, bool starterVisible, bool answerVisible = false)
    {
        if (_genderRoot != null)
        {
            _genderRoot.SetActive(genderVisible);
        }

        if (_answerRoot != null)
        {
            _answerRoot.SetActive(answerVisible);
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

    // 이름 끝 글자에 받침이 있으면 "을", 없으면 "를"을 붙인다.
    private static string ObjectParticle(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "를";
        }

        var last = name[name.Length - 1];
        if (last < '가' || last > '힣')
        {
            return "를";
        }

        return (last - '가') % 28 != 0 ? "을" : "를";
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
