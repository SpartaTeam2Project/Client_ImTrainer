using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

/// <summary>
/// 짧은 안내 문구를 한 줄씩 이어서 보여 준다.
/// </summary>
public class UIToast : MonoBehaviour
{
    private const int SHOW_MILLISECONDS = 1800;
    private const string WARNING_SOUND = "warning";

    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _message;

    private readonly Queue<string> _messages = new Queue<string>();
    private string _current;
    private bool _playing;

    private void Awake()
    {
        Hide();
    }

    /// <summary>
    /// 문구를 표시한다. 같은 문구가 떠 있거나 대기 중이면 다시 넣지 않는다.
    /// </summary>
    public void Show(string message)
    {
        if (string.IsNullOrEmpty(message) || message == _current || _messages.Contains(message))
        {
            return;
        }

        _messages.Enqueue(message);
        if (_playing)
        {
            return;
        }

        PlayQueueAsync().Forget();
    }

    private async UniTaskVoid PlayQueueAsync()
    {
        _playing = true;
        var cancellationToken = this.GetCancellationTokenOnDestroy();
        while (_messages.Count > 0)
        {
            var message = _messages.Dequeue();
            _current = message;
            if (_message != null)
            {
                _message.text = message;
            }

            if (_panel != null)
            {
                _panel.SetActive(true);
            }

            PlayWarning();

            await UniTask.Delay(SHOW_MILLISECONDS, cancellationToken: cancellationToken);
        }

        _current = null;
        Hide();
        _playing = false;
    }

    private static void PlayWarning()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(WARNING_SOUND);
    }

    private void Hide()
    {
        if (_panel != null)
        {
            _panel.SetActive(false);
        }
    }
}
