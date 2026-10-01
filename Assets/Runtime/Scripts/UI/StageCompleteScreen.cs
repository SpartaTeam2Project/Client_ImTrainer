using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시간 클리어 후 계속 버튼으로 메뉴에 돌아간다.
/// </summary>
public class StageCompleteScreen : MonoBehaviour
{
    private const float FADE_DURATION = 0.3f;
    private const string STAGE_COMPLETE_SOUND = "Stage Complete";
    private const string BUTTON_CLICK_SOUND = "select";

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private StageCompleteLines _lines;
    [SerializeField] private Button _button;

    private Tweener _alphaTween;

    #region Unity Methods

    private void Awake()
    {
        if (_canvasGroup == null)
        {
            Debug.LogError("클리어 화면에 CanvasGroup이 없습니다.");
        }

        if (_titleText == null)
        {
            Debug.LogError("클리어 화면에 대사 텍스트가 없습니다.");
        }

        if (_lines == null)
        {
            Debug.LogError("클리어 화면에 대사 모음이 없습니다.");
        }

        if (_button == null)
        {
            Debug.LogError("클리어 화면에 계속 버튼이 없습니다.");
            return;
        }

        _button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDisable()
    {
        KillTween();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 화면을 켜고 알파를 올린다. 판 종료 중에는 시간이 멈춰 있으므로 트윈은 스케일을 무시한다.
    /// </summary>
    public void Show()
    {
        KillTween();
        ApplyTitle();
        gameObject.SetActive(true);
        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        _alphaTween = _canvasGroup.DOFade(1f, FADE_DURATION).SetUpdate(true);
        PlaySound(STAGE_COMPLETE_SOUND);
    }

    /// <summary>
    /// 알파를 내린 뒤 화면을 끈다.
    /// </summary>
    public void Hide()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        KillTween();
        if (_canvasGroup == null)
        {
            gameObject.SetActive(false);
            return;
        }

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        _alphaTween = _canvasGroup.DOFade(0f, FADE_DURATION).SetUpdate(true).OnComplete(Deactivate);
    }

    #endregion

    #region Private Methods

    private void ApplyTitle()
    {
        if (_titleText == null)
        {
            return;
        }

        if (_lines == null || !_lines.TryPick(out var line))
        {
            Debug.LogError("클리어 대사가 없습니다.");
            return;
        }

        _titleText.text = line;
    }

    private void OnButtonClicked()
    {
        PlaySound(BUTTON_CLICK_SOUND);
        ReturnToMenu();
    }

    private void ReturnToMenu()
    {
        Time.timeScale = 1f;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<SceneLoadManager>(out var sceneLoadManager))
        {
            Debug.LogError("SceneLoadManager를 찾을 수 없습니다.");
            return;
        }

        var transition = ScreenTransition.Instance;
        if (transition.IsCovering)
        {
            return;
        }

        // 화면을 덮고 기다린 뒤 타이틀로 간다. 페이드인은 SceneLoadManager가 한다.
        transition.Close().OnComplete(() => sceneLoadManager.LoadTitleSceneAsync().Forget());
    }

    private void Deactivate()
    {
        _alphaTween = null;
        gameObject.SetActive(false);
    }

    private void KillTween()
    {
        if (_alphaTween == null)
        {
            return;
        }

        _alphaTween.Kill();
        _alphaTween = null;
    }

    private static void PlaySound(string name)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(name);
    }

    #endregion
}
