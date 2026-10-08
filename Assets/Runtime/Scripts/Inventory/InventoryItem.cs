using System;
using Coffee.UIEffects;
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
    // 별을 차례로 켤 때 다음 별까지의 간격.
    private const float STAR_REVEAL_INTERVAL = 0.15f;
    private const float EVOLUTION_GLOW_IN_DURATION = 0.5f;
    private const float EVOLUTION_HOLD_DURATION = 0.15f;
    private const float EVOLUTION_GLOW_OUT_DURATION = 0.5f;

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
    [SerializeField] private Material _summonMaterial;
    [Tooltip("칸 전체를 덮는 샤이니 덮개. 꺼 둔 채로 둔다.")]
    [SerializeField] private UIEffect _shinyEffect;
    [Tooltip("판매할 때 아이콘을 지우는 디졸브 덮개. Icon의 자식이고 꺼 둔 채로 둔다.")]
    [SerializeField] private UIEffect _dissolveEffect;

    private InventorySlotDrag _drag;
    private Color _selectColor;
    private bool _selected;
    private Tween _mergeHintTween;
    private Tween _starPunchTween;
    private int _star;
    private bool _starsHidden;
    private bool _filled;
    private bool _frameEmpty;
    private Tween _evolutionTween;
    private IconHitBlend _evolutionBlend;
    private Sprite _evolutionTarget;
    private InventorySummonEffect _summon;
    private SlotShiny _shiny;
    private IconDissolve _dissolve;

    public Button Button => _button;

    /// <summary>
    /// 판매 디졸브 설정. 끌기 그림을 디졸브할 때 같은 설정을 복사해 쓴다.
    /// </summary>
    public UIEffect DissolveTemplate => _dissolveEffect;

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

    /// <summary>
    /// 상점에서 산 포켓몬이 들어올 때의 빨간 실루엣. 프리팹에 없으면 붙여서 돌려준다.
    /// </summary>
    public InventorySummonEffect Summon
    {
        get
        {
            if (_summon == null)
            {
                if (!TryGetComponent(out _summon))
                {
                    _summon = gameObject.AddComponent<InventorySummonEffect>();
                }

                _summon.Bind(this, _icon, _summonMaterial);
            }

            return _summon;
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
        // 프리팹에서 값을 보려고 켜 둔 채 저장해도 평소에는 덮개가 보이지 않게 끈다.
        if (_shinyEffect != null)
        {
            _shinyEffect.gameObject.SetActive(false);
        }

        if (_dissolveEffect != null)
        {
            _dissolveEffect.gameObject.SetActive(false);
        }

        ShowEmpty();
    }

    /// <summary>
    /// 빈 칸으로 둔다. 테두리와 배경만 남긴다.
    /// </summary>
    public void ShowEmpty()
    {
        StopEvolution();
        StopSummon();
        StopShiny();
        StopDissolve();
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
        ShowFrame(false);
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

        if (_summon != null)
        {
            _summon.NotifyShown(portrait);
        }

        if (_dissolve != null)
        {
            _dissolve.NotifyShown(portrait);
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
        ShowFrame(true);
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
    /// 성이 올랐다. 새로 켜진 별을 한 번 튕기면서 샤이니를 한 번 보여 준다.
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
        PlayShiny();
    }

    /// <summary>
    /// 빛줄기가 칸 전체를 한 번 훑고 지나간다. 구매, 성 오름, 진화 연출의 마무리로 쓴다.
    /// </summary>
    public void PlayShiny()
    {
        if (_shinyEffect == null)
        {
            return;
        }

        if (_shiny == null)
        {
            _shiny = new SlotShiny(_shinyEffect, gameObject);
        }

        _shiny.Play();
    }

    private void StopShiny()
    {
        if (_shiny != null)
        {
            _shiny.Stop();
        }
    }

    /// <summary>
    /// 판매할 포켓몬 아이콘을 디졸브로 지운다. 다 사라지면 onFinished를 부른다. 도중에 멈추면 부르지 않는다.
    /// 진화 연출은 아이콘 머티리얼을 쓰고 소환, 샤이니는 아이콘 위에 겹쳐서 먼저 멈춘다.
    /// </summary>
    public void PlayDissolve(Action onFinished)
    {
        PrepareDissolve().Play(onFinished);
    }

    /// <summary>
    /// 디졸브 없이 아이콘만 숨겨 둔다. 휴지통에 끌어다 놓아 끌기 그림이 대신 디졸브되는 동안 쓴다.
    /// </summary>
    public void HoldDissolve()
    {
        PrepareDissolve().Hold();
    }

    private IconDissolve PrepareDissolve()
    {
        StopEvolution();
        StopSummon();
        StopShiny();
        if (_dissolve == null)
        {
            _dissolve = new IconDissolve(_icon, _dissolveEffect, gameObject);
        }

        return _dissolve;
    }

    public void StopDissolve()
    {
        if (_dissolve != null)
        {
            _dissolve.Stop();
        }
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

        if (_evolutionBlend == null)
        {
            _evolutionBlend = new IconHitBlend(_icon, _evolutionMaterial);
        }

        _evolutionBlend.Apply();
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
                PlayShiny();
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
        if (_evolutionBlend != null)
        {
            _evolutionBlend.Clear();
        }
    }

    private void SetEvolutionBlend(float blend)
    {
        if (_evolutionBlend != null)
        {
            _evolutionBlend.Set(blend);
        }
    }

    /// <summary>
    /// 소환 실루엣을 멈추고 아이콘을 보이게 되돌린다. 실루엣을 쓴 적이 없으면 아무것도 하지 않는다.
    /// </summary>
    public void StopSummon()
    {
        if (_summon != null)
        {
            _summon.Stop();
        }
    }

    /// <summary>
    /// 숨긴 별을 1성부터 차례로 하나씩 켜며 튕기고, 튕기기 시작할 때 샤이니를 같이 보여 준다.
    /// 상점에서 산 포켓몬이 칸에 다 들어왔을 때 쓴다.
    /// </summary>
    public void RevealStarsInOrder()
    {
        StopStarPunch();
        _starsHidden = false;
        if (_starImages == null)
        {
            return;
        }

        ShowStars(_starImages, 0);
        var sequence = DOTween.Sequence();
        for (var i = 0; i < _starImages.Length && i < _star; i++)
        {
            var star = _starImages[i];
            if (star == null)
            {
                continue;
            }

            var index = i;
            var at = i * STAR_REVEAL_INTERVAL;
            // 도중에 칸이 비거나 다른 포켓몬이 되면 그 칸의 성 수를 따른다.
            sequence.InsertCallback(at, () => star.gameObject.SetActive(index < _star && !_starsHidden));
            sequence.Insert(at, star.transform.DOPunchScale(Vector3.one * STAR_PUNCH_SCALE, STAR_PUNCH_DURATION, STAR_PUNCH_VIBRATO));
        }

        _starPunchTween = sequence.SetUpdate(true).SetLink(gameObject);
        PlayShiny();
    }

    /// <summary>
    /// 별을 숨기거나 되돌린다. 숨긴 동안 칸을 다시 그려도 별은 꺼진 채로 남는다.
    /// </summary>
    public void SetStarsHidden(bool hidden)
    {
        _starsHidden = hidden;
        ShowStars(_star);
    }

    /// <summary>
    /// 포켓몬이 있어도 테두리와 배경을 빈 칸 색으로 둔다. 둔 동안 칸을 다시 그려도 빈 칸 색으로 남는다.
    /// </summary>
    public void SetFrameEmpty(bool empty)
    {
        _frameEmpty = empty;
        ShowFrame(_filled);
    }

    private void ShowFrame(bool filled)
    {
        _filled = filled;
        var look = filled && !_frameEmpty;
        if (_frame != null)
        {
            _frame.color = look ? FILLED_FRAME : EMPTY_FRAME;
        }

        if (_background != null)
        {
            _background.color = look ? FILLED_BACKGROUND : EMPTY_BACKGROUND;
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
        StopShiny();
        StopDissolve();
    }

    private void OnDestroy()
    {
        if (_evolutionBlend != null)
        {
            _evolutionBlend.Dispose();
        }
    }

    private void ShowStars(int star)
    {
        _star = star;
        ShowStars(_starImages, _starsHidden ? 0 : star);
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
