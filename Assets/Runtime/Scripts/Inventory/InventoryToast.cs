using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 인벤토리 창 안내 문구를 잠깐 띄웠다가 서서히 감춘다. 떠 있는 중에 새 문구가 오면 바로 바꾸고 시간을 다시 센다.
/// 창이 열려 있는 동안 시간이 멈춰 있어서 시간 정지와 상관없이 돈다.
/// </summary>
public class InventoryToast : MonoBehaviour
{
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private TMP_Text _text;
    [SerializeField] private float _holdDuration = 1.5f;
    [SerializeField] private float _fadeDuration = 0.4f;

    private Sequence _sequence;

    private void Awake()
    {
        if (_group != null)
        {
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        Hide();
    }

    public void Show(string message)
    {
        if (string.IsNullOrEmpty(message) || _group == null)
        {
            return;
        }

        Stop();
        if (_text != null)
        {
            _text.text = message;
        }

        _group.alpha = 1f;
        _sequence = DOTween.Sequence()
            .AppendInterval(_holdDuration)
            .Append(_group.DOFade(0f, _fadeDuration))
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    /// <summary>
    /// 떠 있는 문구를 바로 감춘다. 창을 닫을 때 부른다.
    /// </summary>
    public void Hide()
    {
        Stop();
        if (_group != null)
        {
            _group.alpha = 0f;
        }
    }

    private void Stop()
    {
        if (_sequence != null)
        {
            _sequence.Kill();
            _sequence = null;
        }
    }
}
