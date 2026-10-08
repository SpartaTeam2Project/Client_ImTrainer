using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 가방, 장착, 상점이 같이 쓰는 칸. 포켓몬 그림, 이름, 성, 개수만 그린다.
/// </summary>
public class InventoryItem : MonoBehaviour
{
    private static readonly Color EMPTY_FRAME = new Color32(100, 100, 100, 255);
    private static readonly Color EMPTY_BACKGROUND = new Color32(100, 100, 100, 255);
    private static readonly Color FILLED_FRAME = Color.white;
    private static readonly Color FILLED_BACKGROUND = Color.white;
    private const float MERGE_HINT_MAX_ALPHA = 1f;
    private const float MERGE_HINT_MIN_ALPHA = 0.2f;
    private const float MERGE_HINT_DURATION = 0.45f;
    private const float STAR_PUNCH_SCALE = 0.5f;
    private const float STAR_PUNCH_DURATION = 0.3f;
    private const int STAR_PUNCH_VIBRATO = 6;
    private const float EVOLUTION_GLOW_IN_DURATION = 0.5f;
    private const float EVOLUTION_HOLD_DURATION = 0.15f;
    private const float EVOLUTION_GLOW_OUT_DURATION = 0.5f;
    private static readonly int HIT_EFFECT_BLEND_ID = Shader.PropertyToID("_HitEffectBlend");

    [SerializeField] private Image _frame;
    [SerializeField] private Image[] _starImages = new Image[Item.STAR_MAX];
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _select;
    [SerializeField] private GameObject _state;
    [SerializeField] private TextMeshProUGUI _starText;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private Button _button;
    [SerializeField] private Material _evolutionMaterial;

    private InventorySlotDrag _drag;
    private Color _selectColor;
    private bool _selected;
    private Tween _mergeHintTween;
    private Tween _starPunchTween;
    private Tween _evolutionTween;
    private Material _evolutionInstance;
    private Sprite _evolutionTarget;

    public Button Button => _button;

    /// <summary>
    /// 끌기를 받는 컴포넌트. 프리팹에 없으면 붙여서 돌려준다.
    /// </summary>
    public InventorySlotDrag Drag
    {
        get
        {
            if (_drag == null && !TryGetComponent(out _drag))
            {
                _drag = gameObject.AddComponent<InventorySlotDrag>();
            }

            return _drag;
        }
    }

    private void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_select != null)
        {
            _selectColor = _select.color;
        }

        ApplyFont(_starText);
        ApplyFont(_countText);
        ShowEmpty();
    }

    /// <summary>
    /// 빈 칸으로 둔다. 테두리와 배경만 남긴다.
    /// </summary>
    public void ShowEmpty()
    {
        StopEvolution();
        if (_icon != null)
        {
            _icon.sprite = null;
            _icon.enabled = false;
        }

        if (_state != null)
        {
            _state.SetActive(false);
        }

        ShowStars(0);
        SetSelected(false);
        if (_frame != null)
        {
            _frame.color = EMPTY_FRAME;
        }

        if (_background != null)
        {
            _background.color = EMPTY_BACKGROUND;
        }
    }

    /// <summary>
    /// 포켓몬 칸을 채운다. 가방이면 개수도 적는다.
    /// </summary>
    public void ShowPokemon(Sprite portrait, string pokemonName, int star, int count, bool showCount, bool selected)
    {
        // 진화 연출 중 같은 결과로 다시 그리면 그림과 별은 연출에 맡긴다. 다른 포켓몬이면 연출을 끝낸다.
        var keepEvolution = _evolutionTween != null && portrait == _evolutionTarget;
        if (!keepEvolution)
        {
            StopEvolution();
        }

        if (_icon != null && !keepEvolution)
        {
            _icon.sprite = portrait;
            _icon.enabled = portrait != null;
            _icon.preserveAspect = true;
            _icon.color = Color.white;
        }

        if (_starText != null)
        {
            var label = string.IsNullOrEmpty(pokemonName) ? string.Empty : pokemonName;
            _starText.text = label + " " + star + "성";
        }

        if (_countText != null)
        {
            _countText.text = "x" + count;
        }

        if (!keepEvolution)
        {
            ShowStars(star);
        }

        SetSelected(selected);
        if (_frame != null)
        {
            _frame.color = FILLED_FRAME;
        }

        if (_background != null)
        {
            _background.color = FILLED_BACKGROUND;
        }
    }

    /// <summary>
    /// 고른 칸 표시를 켜거나 끈다.
    /// </summary>
    public void SetSelected(bool selected)
    {
        _selected = selected;
        if (_select != null && _mergeHintTween == null)
        {
            _select.enabled = selected;
        }
    }

    /// <summary>
    /// 끌고 있는 포켓몬과 합성할 수 있는 칸이면 테두리 알파를 오르내린다. 창이 열려 있으면 시간이 멈춰 있어서 시간 정지와 상관없이 돈다.
    /// </summary>
    public void SetMergeHint(bool on)
    {
        StopMergeHint();
        if (!on || _select == null)
        {
            return;
        }

        var color = _selectColor;
        color.a = MERGE_HINT_MAX_ALPHA;
        _select.color = color;
        _select.enabled = true;
        _mergeHintTween = _select.DOFade(MERGE_HINT_MIN_ALPHA, MERGE_HINT_DURATION)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void StopMergeHint()
    {
        if (_mergeHintTween != null)
        {
            _mergeHintTween.Kill();
            _mergeHintTween = null;
        }

        if (_select != null)
        {
            _select.color = _selectColor;
            _select.enabled = _selected;
        }
    }

    /// <summary>
    /// 성이 올랐다. 새로 켜진 별을 한 번 튕긴다.
    /// </summary>
    public void PlayStarUp(int star)
    {
        StopStarPunch();
        var index = star - 1;
        if (_starImages == null || index < 0 || index >= _starImages.Length || _starImages[index] == null)
        {
            return;
        }

        _starPunchTween = _starImages[index].transform
            .DOPunchScale(Vector3.one * STAR_PUNCH_SCALE, STAR_PUNCH_DURATION, STAR_PUNCH_VIBRATO)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    /// <summary>
    /// 3성이 다음 포켓몬으로 진화한다. 이전 그림이 하얗게 빛난 채 revealDelay까지 머물고,
    /// 그때 새 그림으로 바뀌며 onRevealed를 부른 뒤 빛이 빠진다.
    /// 공개 전에 다시 그려지거나 꺼지면 onRevealed를 부르지 않는다.
    /// </summary>
    public void PlayEvolution(Sprite from, Sprite to, float revealDelay, Action onRevealed)
    {
        StopEvolution();
        if (_icon == null || _evolutionMaterial == null)
        {
            return;
        }

        if (_evolutionInstance == null)
        {
            _evolutionInstance = new Material(_evolutionMaterial);
        }

        _icon.material = _evolutionInstance;
        _icon.sprite = from;
        _icon.enabled = from != null;
        _evolutionTarget = to;
        SetEvolutionBlend(0f);
        ShowStars(Item.STAR_MAX);

        var hold = Mathf.Max(EVOLUTION_HOLD_DURATION, revealDelay - EVOLUTION_GLOW_IN_DURATION);
        _evolutionTween = DOTween.Sequence()
            .Append(DOVirtual.Float(0f, 1f, EVOLUTION_GLOW_IN_DURATION, SetEvolutionBlend).SetEase(Ease.InQuad))
            .AppendInterval(hold)
            .AppendCallback(() =>
            {
                _icon.sprite = to;
                _icon.enabled = to != null;
                ShowStars(Item.STAR_MIN);
                onRevealed?.Invoke();
            })
            .Append(DOVirtual.Float(1f, 0f, EVOLUTION_GLOW_OUT_DURATION, SetEvolutionBlend).SetEase(Ease.OutQuad))
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                _evolutionTween = null;
                ResetEvolutionMaterial();
            });
    }

    private void StopEvolution()
    {
        if (_evolutionTween == null)
        {
            return;
        }

        _evolutionTween.Kill();
        _evolutionTween = null;
        ResetEvolutionMaterial();
        if (_icon != null && _evolutionTarget != null)
        {
            _icon.sprite = _evolutionTarget;
            _icon.enabled = true;
        }
    }

    private void ResetEvolutionMaterial()
    {
        SetEvolutionBlend(0f);
        if (_icon != null)
        {
            _icon.material = null;
        }
    }

    /// <summary>
    /// 마스크 안에서는 그리는 머티리얼이 따로 복사돼서 둘 다 바꾼다.
    /// </summary>
    private void SetEvolutionBlend(float blend)
    {
        if (_evolutionInstance != null)
        {
            _evolutionInstance.SetFloat(HIT_EFFECT_BLEND_ID, blend);
        }

        if (_icon != null && _icon.materialForRendering != _evolutionInstance && _icon.material == _evolutionInstance)
        {
            _icon.materialForRendering.SetFloat(HIT_EFFECT_BLEND_ID, blend);
        }
    }

    private void StopStarPunch()
    {
        if (_starPunchTween != null)
        {
            _starPunchTween.Complete();
            _starPunchTween = null;
        }
    }

    private void OnDisable()
    {
        StopMergeHint();
        StopEvolution();
        StopStarPunch();
    }

    private void OnDestroy()
    {
        if (_evolutionInstance != null)
        {
            Destroy(_evolutionInstance);
        }
    }

    private void ShowStars(int star)
    {
        ShowStars(_starImages, star);
    }

    /// <summary>
    /// 별 이미지를 성 수만큼 켠다. 정보 창도 같이 쓴다.
    /// </summary>
    public static void ShowStars(Image[] starImages, int star)
    {
        if (starImages == null)
        {
            return;
        }

        for (var i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null)
            {
                continue;
            }

            starImages[i].gameObject.SetActive(i < star);
        }
    }

    private static void ApplyFont(TextMeshProUGUI text)
    {
        if (text == null || text.font != null || TMP_Settings.defaultFontAsset == null)
        {
            return;
        }

        text.font = TMP_Settings.defaultFontAsset;
    }
}
