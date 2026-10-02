using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 VS 화면의 배치. 위치와 크기는 이 프리팹에서 고친다.
/// </summary>
public class BossTrainerVersusView : MonoBehaviour
{
    [SerializeField] private RectTransform _band;
    [SerializeField] private Image _firstBar;
    [SerializeField] private Image _secondBar;
    [SerializeField] private Image _player;
    [SerializeField] private Image _rival;
    [SerializeField] private Image[] _slashes;
    [SerializeField] private Image _versus;

    /// <summary>
    /// 빨간 바가 흐르는 영역.
    /// </summary>
    public RectTransform Band => _band;

    /// <summary>
    /// 오른쪽으로 흐르는 첫 빨간 바.
    /// </summary>
    public Image FirstBar => _firstBar;

    /// <summary>
    /// 첫 바 뒤를 이어 흐르는 빨간 바.
    /// </summary>
    public Image SecondBar => _secondBar;

    /// <summary>
    /// 왼쪽 플레이어 등 사진.
    /// </summary>
    public Image Player => _player;

    /// <summary>
    /// 오른쪽 보스 트레이너.
    /// </summary>
    public Image Rival => _rival;

    /// <summary>
    /// 캐릭터 뒤에 두는 슬래시 네 칸. 같은 순서로 화염 프레임을 갈아 끼운다.
    /// </summary>
    public Image[] Slashes => _slashes;

    /// <summary>
    /// 가운데 VS 글자.
    /// </summary>
    public Image Versus => _versus;

    /// <summary>
    /// 재생에 필요한 그림 칸이 다 연결됐는지 확인한다.
    /// </summary>
    public bool IsReady()
    {
        return _band != null
            && _firstBar != null
            && _secondBar != null
            && _player != null
            && _rival != null
            && _slashes != null
            && _slashes.Length > 0
            && _versus != null;
    }
}
