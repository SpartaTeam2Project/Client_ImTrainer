using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 트레이너 성별.
/// </summary>
public enum TrainerGender
{
    Boy = 0,
    Girl = 1
}

/// <summary>
/// 계정 프로필을 로컬에 보관한다. 인트로 완료, 성별, 스타터, 세대, 선택한 플레이어블을 플레이어 식별자와 함께 둔다.
/// </summary>
public class AccountManager : BaseManager
{
    private const string PREFS_PREFIX = "account_";
    private const string INTRO_SUFFIX = "_intro";
    private const string GENDER_SUFFIX = "_gender";
    private const string STARTER_SUFFIX = "_starter";
    private const string GENERATION_SUFFIX = "_generation";
    private const string PLAYABLE_SUFFIX = "_playable";
    private const int LOCAL_PLAYER_ID_FALLBACK = 1;
    private const int INTRO_COMPLETED_VALUE = 1;
    private const int DEFAULT_GENERATION = 1;

    // TODO: 테스트가 끝나면 false로 바꾼다. 켜져 있으면 완료 여부와 관계없이 인트로를 다시 연다.
    private const bool ALWAYS_SHOW_INTRO = true;

    [SerializeField] private MonsterVisualData[] _starterVisuals = System.Array.Empty<MonsterVisualData>();
    [SerializeField] private PlayableCharacterDatabase _playableCharacters;

    private MonsterVisualData[] _boundStarters = System.Array.Empty<MonsterVisualData>();
    private bool _introCompleted;
    private TrainerGender _gender;
    private string _starterVisualName = string.Empty;
    private int _generation = DEFAULT_GENERATION;
    private string _selectedPlayableName = string.Empty;

    public bool IntroCompleted => _introCompleted;

    /// <summary>
    /// 로그인 진입 때 인트로를 열지 판단한다. 테스트 중에는 완료 후에도 연다.
    /// </summary>
    public bool ShouldShowIntro => ALWAYS_SHOW_INTRO || !_introCompleted;

    public TrainerGender Gender => _gender;

    public string StarterVisualName => _starterVisualName;

    public int Generation => _generation;

    public string SelectedPlayableName => _selectedPlayableName;

    public PlayableCharacterData[] Playables => _playableCharacters != null
        ? _playableCharacters.Characters
        : System.Array.Empty<PlayableCharacterData>();

    /// <summary>
    /// 저장된 프로필을 읽는다.
    /// </summary>
    public override UniTask InitializeAsync()
    {
        Load(ResolvePlayerId());
        return base.InitializeAsync();
    }

    /// <summary>
    /// 인트로에 연결된 스타터 그림을 전투에서 찾을 수 있게 기억한다.
    /// </summary>
    public void BindStarterVisuals(MonsterVisualData[] visuals)
    {
        if (visuals == null || !HasAnyVisual(visuals))
        {
            return;
        }

        _boundStarters = visuals;
    }

    /// <summary>
    /// 스타터 칸의 그림. 없으면 null.
    /// </summary>
    public MonsterVisualData GetStarterVisual(int index)
    {
        var visuals = ActiveStarters();
        if (index < 0 || index >= visuals.Length)
        {
            return null;
        }

        return visuals[index];
    }

    /// <summary>
    /// 프로필에 저장된 스타터 그림. 없거나 목록에 없으면 null.
    /// </summary>
    public MonsterVisualData ResolveStarterVisual()
    {
        if (string.IsNullOrEmpty(_starterVisualName))
        {
            return null;
        }

        var visuals = ActiveStarters();
        for (var i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] != null && visuals[i].name == _starterVisualName)
            {
                return visuals[i];
            }
        }

        return null;
    }

    /// <summary>
    /// 인트로 선택을 프로필에 남긴다.
    /// </summary>
    public void CompleteIntro(TrainerGender gender, MonsterVisualData starter)
    {
        if (starter == null)
        {
            Debug.LogError("스타터 그림이 없어 인트로를 끝내지 않습니다.");
            return;
        }

        _introCompleted = true;
        _gender = gender;
        _starterVisualName = starter.name;
        _generation = DEFAULT_GENERATION;
        _selectedPlayableName = FindPlayableName(_generation, gender);
        Save(ResolvePlayerId());
    }

    /// <summary>
    /// 저장된 세대와 성별이 같은 플레이어블이면 획득이다.
    /// </summary>
    public bool IsPlayableUnlocked(PlayableCharacterData data)
    {
        return data != null && data.Generation == _generation && data.Gender == _gender;
    }

    /// <summary>
    /// 획득한 플레이어블을 선택 캐릭터로 저장한다.
    /// </summary>
    public void SelectPlayable(PlayableCharacterData data)
    {
        if (!IsPlayableUnlocked(data))
        {
            return;
        }

        _selectedPlayableName = data.name;
        Save(ResolvePlayerId());
    }

    /// <summary>
    /// 선택한 플레이어블을 비운다.
    /// </summary>
    public void ClearSelectedPlayable()
    {
        if (string.IsNullOrEmpty(_selectedPlayableName))
        {
            return;
        }

        _selectedPlayableName = string.Empty;
        Save(ResolvePlayerId());
    }

    /// <summary>
    /// 저장된 선택 캐릭터. 없거나 더 이상 획득 상태가 아니면 null.
    /// </summary>
    public PlayableCharacterData ResolveSelectedPlayable()
    {
        if (string.IsNullOrEmpty(_selectedPlayableName))
        {
            return null;
        }

        var characters = Playables;
        for (var i = 0; i < characters.Length; i++)
        {
            var character = characters[i];
            if (character != null && character.name == _selectedPlayableName && IsPlayableUnlocked(character))
            {
                return character;
            }
        }

        return null;
    }

    private MonsterVisualData[] ActiveStarters()
    {
        if (HasAnyVisual(_boundStarters))
        {
            return _boundStarters;
        }

        return _starterVisuals ?? System.Array.Empty<MonsterVisualData>();
    }

    private static bool HasAnyVisual(MonsterVisualData[] visuals)
    {
        if (visuals == null)
        {
            return false;
        }

        for (var i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private void Load(int playerId)
    {
        _introCompleted = PlayerPrefs.GetInt(Key(playerId, INTRO_SUFFIX), 0) == INTRO_COMPLETED_VALUE;
        _gender = (TrainerGender)PlayerPrefs.GetInt(Key(playerId, GENDER_SUFFIX), (int)TrainerGender.Boy);
        _starterVisualName = PlayerPrefs.GetString(Key(playerId, STARTER_SUFFIX), string.Empty);
        _generation = PlayerPrefs.GetInt(Key(playerId, GENERATION_SUFFIX), DEFAULT_GENERATION);
        if (_generation < DEFAULT_GENERATION)
        {
            _generation = DEFAULT_GENERATION;
        }

        _selectedPlayableName = PlayerPrefs.GetString(Key(playerId, PLAYABLE_SUFFIX), string.Empty);
    }

    private void Save(int playerId)
    {
        PlayerPrefs.SetInt(Key(playerId, INTRO_SUFFIX), _introCompleted ? INTRO_COMPLETED_VALUE : 0);
        PlayerPrefs.SetInt(Key(playerId, GENDER_SUFFIX), (int)_gender);
        PlayerPrefs.SetString(Key(playerId, STARTER_SUFFIX), _starterVisualName ?? string.Empty);
        PlayerPrefs.SetInt(Key(playerId, GENERATION_SUFFIX), _generation);
        PlayerPrefs.SetString(Key(playerId, PLAYABLE_SUFFIX), _selectedPlayableName ?? string.Empty);
        PlayerPrefs.Save();
    }

    private string FindPlayableName(int generation, TrainerGender gender)
    {
        var characters = Playables;
        for (var i = 0; i < characters.Length; i++)
        {
            var character = characters[i];
            if (character != null && character.Generation == generation && character.Gender == gender)
            {
                return character.name;
            }
        }

        return string.Empty;
    }

    private static string Key(int playerId, string suffix)
    {
        return PREFS_PREFIX + playerId + suffix;
    }

    private int ResolvePlayerId()
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return playerManager.LocalPlayerId;
        }

        return LOCAL_PLAYER_ID_FALLBACK;
    }
}
