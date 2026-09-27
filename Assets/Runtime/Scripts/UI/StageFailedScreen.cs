using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사망 후 나가기 버튼으로 메뉴에 돌아간다.
/// </summary>
public class StageFailedScreen : MonoBehaviour
{
    private const float FADE_DURATION = 0.3f;
    private const string BUTTON_CLICK_SOUND = "select";

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private StageFailedLines _lines;
    [SerializeField] private Button _reviveButton;
    [SerializeField] private Button _exitButton;

    private Tweener _alphaTween;

    #region Unity Methods

    private void Awake()
    {
        if (_canvasGroup == null)
        {
            Debug.LogError("실패 화면에 CanvasGroup이 없습니다.");
        }

        if (_titleText == null)
        {
            Debug.LogError("실패 화면에 대사 텍스트가 없습니다.");
        }

        if (_lines == null)
        {
            Debug.LogError("실패 화면에 대사 모음이 없습니다.");
        }

        if (_reviveButton == null)
        {
            Debug.LogError("실패 화면에 부활 버튼이 없습니다.");
        }
        else
        {
            _reviveButton.gameObject.SetActive(false);
        }

        if (_exitButton == null)
        {
            Debug.LogError("실패 화면에 나가기 버튼이 없습니다.");
            return;
        }

        _exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    private void OnDisable()
    {
        KillTween();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 화면을 켜고 알파를 올린다. 부활은 업그레이드가 생기기 전까지 항상 숨긴다.
    /// </summary>
    public void Show()
    {
        KillTween();
        if (_reviveButton != null)
        {
            _reviveButton.gameObject.SetActive(false);
        }

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
            Debug.LogError("실패 대사가 없습니다.");
            return;
        }

        _titleText.text = line;
    }

    private void OnExitButtonClicked()
    {
        PlayButtonClick();
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

        sceneLoadManager.LoadTitleSceneAsync().Forget();
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

    private static void PlayButtonClick()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(BUTTON_CLICK_SOUND);
    }

    #endregion
}
