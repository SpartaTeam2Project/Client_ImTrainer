using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시간 클리어 후 판 결과를 보여 주고 계속 버튼으로 메뉴에 돌아간다.
/// 결과 패널은 StageResult 폴더의 패널이 그린다. 여기서는 열고 닫기와 패널 호출만 한다.
/// </summary>
public class StageCompleteScreen : MonoBehaviour
{
    private const string TITLE = "도전 성공";
    private const float FADE_DURATION = 0.3f;
    private const string STAGE_COMPLETE_SOUND = "Stage Complete";
    private const string BUTTON_CLICK_SOUND = "select";

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private StageCompleteLines _lines;
    [SerializeField] private Button _button;
    [SerializeField] private ScreenBackdrop _backdrop;

    [Header("판 결과")]
    [SerializeField] private TextMeshProUGUI _stageNameText;
    [SerializeField] private TextMeshProUGUI _lineText;
    [SerializeField] private GameObject _resultLayout;
    [SerializeField] private StagePlayerStatPanel _playerStatPanel;
    [SerializeField] private StagePartyPanel _partyPanel;
    [SerializeField] private StageDataPanel _dataPanel;
    [SerializeField] private StageDamagePanel _damagePanel;

    private Tweener _alphaTween;
    private Coroutine _showRoutine;
    private StageResult _result;

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
        _showRoutine = null;
        if (_backdrop != null)
        {
            _backdrop.Release();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 화면을 켜고 알파를 올린다. 판 종료 중에는 시간이 멈춰 있으므로 트윈은 스케일을 무시한다.
    /// result가 없으면 결과 패널을 숨기고 제목과 버튼만 보인다.
    /// </summary>
    public void Show(StageResult result)
    {
        KillTween();
        StopShowRoutine();
        _result = result;
        ApplyTitle();
        gameObject.SetActive(true);
        BindResult(result);
        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        _showRoutine = StartCoroutine(FadeInAfterCapture());
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
        StopShowRoutine();
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

    // 알파 0인 화면 아래의 게임 화면을 배경으로 담은 뒤에 알파를 올린다.
    private IEnumerator FadeInAfterCapture()
    {
        if (_backdrop != null)
        {
            yield return new WaitForEndOfFrame();
            _backdrop.Capture();
        }

        _showRoutine = null;
        _alphaTween = _canvasGroup.DOFade(1f, FADE_DURATION).SetUpdate(true);
    }

    private void StopShowRoutine()
    {
        if (_showRoutine == null)
        {
            return;
        }

        StopCoroutine(_showRoutine);
        _showRoutine = null;
    }

    private void ApplyTitle()
    {
        // 결과 패널이 없던 화면은 제목 자리에 대사를 그대로 띄운다.
        var lineTarget = _lineText != null ? _lineText : _titleText;
        if (_lineText != null && _titleText != null)
        {
            _titleText.text = TITLE;
        }

        if (lineTarget == null)
        {
            return;
        }

        if (_lines == null || !_lines.TryPick(out var line))
        {
            Debug.LogError("클리어 대사가 없습니다.");
            return;
        }

        lineTarget.text = line;
    }

    private void BindResult(StageResult result)
    {
        if (_resultLayout != null)
        {
            _resultLayout.SetActive(result != null);
        }

        if (result == null)
        {
            return;
        }

        if (_stageNameText != null)
        {
            _stageNameText.text = result.StageName;
        }

        if (_playerStatPanel != null)
        {
            _playerStatPanel.Bind(result);
        }

        if (_partyPanel != null)
        {
            _partyPanel.Bind(result);
        }

        if (_dataPanel != null)
        {
            _dataPanel.Bind(result);
        }

        if (_damagePanel != null)
        {
            _damagePanel.Bind(result);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 마지막으로 보여 준 결과를 서버 요청 JSON으로 복사한다. 서버 계약 확인용이다.
    /// </summary>
    [ContextMenu("Copy Result JSON")]
    private void CopyResultJson()
    {
        if (_result == null)
        {
            Debug.LogWarning("아직 보여 준 판 결과가 없습니다.");
            return;
        }

        Managers.Instance.TryGetManager<ItemManager>(out var itemManager);
        var json = JsonUtility.ToJson(StageResultMapper.ToRequest(_result, itemManager), true);
        GUIUtility.systemCopyBuffer = json;
        Debug.Log(json);
    }
#endif

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
