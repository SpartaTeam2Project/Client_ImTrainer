using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 씬에서 게임 시작을 요청하고, 임시 인트로 재생 체크를 계정에 넘긴다.
/// </summary>
public class UITitleScene : MonoBehaviour
{
    private const string TRAINER_REQUIRED_MESSAGE = "트레이너를 선택해야 합니다";
    private const string MONSTER_REQUIRED_MESSAGE = "최소 1마리 이상의 포켓몬을 데려가야합니다";

    [SerializeField] private Toggle _alwaysShowIntroToggle;
    [SerializeField] private Button _gameStartButton;
    [SerializeField] private UIStorageWindow _storageWindow;
    [SerializeField] private UIToast _toast;

    private void Start()
    {
        ApplyAlwaysShowIntro();
    }

    private void OnEnable()
    {
        if (_alwaysShowIntroToggle != null)
        {
            _alwaysShowIntroToggle.onValueChanged.AddListener(HandleAlwaysShowIntroChanged);
        }

        if (_gameStartButton != null)
        {
            _gameStartButton.onClick.AddListener(StartGame);
        }
    }

    private void OnDisable()
    {
        if (_alwaysShowIntroToggle != null)
        {
            _alwaysShowIntroToggle.onValueChanged.RemoveListener(HandleAlwaysShowIntroChanged);
        }

        if (_gameStartButton != null)
        {
            _gameStartButton.onClick.RemoveListener(StartGame);
        }
    }

    /// <summary>
    /// 트레이너와 포켓몬이 있으면 게임 씬으로 전환한다. 없으면 토스트만 보여 준다.
    /// </summary>
    public void StartGame()
    {
        if (!TryGetAccount(out var account))
        {
            return;
        }

        var hasTrainer = account.ResolveSelectedPlayable() != null;
        if (_storageWindow == null)
        {
            Debug.LogError("스토리지 창을 찾을 수 없습니다.");
        }

        MonsterVisualData monster = null;
        var hasMonster = _storageWindow != null && _storageWindow.TryGetFirstEntryMonster(out monster);
        if (!hasTrainer || !hasMonster)
        {
            ShowMissingSelection(hasTrainer, hasMonster);
            return;
        }

        account.SetRunMonster(monster);
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<SceneLoadManager>(out var sceneLoadManager))
        {
            Debug.LogError("SceneLoadManager를 찾을 수 없습니다.");
            return;
        }

        sceneLoadManager.LoadGameAsync().Forget();
    }

    private void ShowMissingSelection(bool hasTrainer, bool hasMonster)
    {
        if (_toast == null)
        {
            Debug.LogError("토스트를 찾을 수 없습니다.");
            return;
        }

        if (!hasTrainer)
        {
            _toast.Show(TRAINER_REQUIRED_MESSAGE);
            return;
        }

        if (!hasMonster)
        {
            _toast.Show(MONSTER_REQUIRED_MESSAGE);
        }
    }

    private void HandleAlwaysShowIntroChanged(bool alwaysShowIntro)
    {
        SetAlwaysShowIntro(alwaysShowIntro);
    }

    private void ApplyAlwaysShowIntro()
    {
        var alwaysShowIntro = _alwaysShowIntroToggle != null && _alwaysShowIntroToggle.isOn;
        SetAlwaysShowIntro(alwaysShowIntro);
    }

    private void SetAlwaysShowIntro(bool alwaysShowIntro)
    {
        if (!TryGetAccount(out var accountManager))
        {
            return;
        }

        accountManager.SetAlwaysShowIntro(alwaysShowIntro);
    }

    private static bool TryGetAccount(out AccountManager accountManager)
    {
        accountManager = null;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager(out accountManager))
        {
            Debug.LogError("AccountManager를 찾을 수 없습니다.");
            return false;
        }

        return accountManager != null;
    }
}
