using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 증강 선택창의 카드 한 장. 내용은 AugmentWindowBehavior가 채운다.
/// </summary>
public sealed class AugmentCard : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField] private CanvasGroup _canvasGroup;

    public Button Button => _button;
    public Image Icon => _icon;
    public TextMeshProUGUI Label => _label;
    public CanvasGroup CanvasGroup => _canvasGroup;
}
