using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 씬에서 게임 시작을 요청하고, 임시 인트로 재생 체크를 계정에 넘긴다.
/// </summary>
public class UITitleScene : MonoBehaviour
{
    [SerializeField] private Toggle _alwaysShowIntroToggle;

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
    }

    private void OnDisable()
    {
        if (_alwaysShowIntroToggle != null)
        {
            _alwaysShowIntroToggle.onValueChanged.RemoveListener(HandleAlwaysShowIntroChanged);
        }
    }

    /// <summary>
    /// 게임 씬으로 전환한다.
    /// </summary>
    public void StartGame()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<SceneLoadManager>(out var sceneLoadManager))
        {
            Debug.LogError("SceneLoadManager를 찾을 수 없습니다.");
            return;
        }

        sceneLoadManager.LoadGameAsync().Forget();
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
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AccountManager>(out var accountManager))
        {
            Debug.LogError("AccountManager를 찾을 수 없습니다.");
            return;
        }

        accountManager.SetAlwaysShowIntro(alwaysShowIntro);
    }
}
