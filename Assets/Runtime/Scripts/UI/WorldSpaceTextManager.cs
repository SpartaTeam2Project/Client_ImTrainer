using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 월드 좌표의 피해 숫자를 오버레이 캔버스에 띄운다.
/// </summary>
public class WorldSpaceTextManager : MonoBehaviour
{
    [SerializeField] private RectTransform _canvasRect;
    [SerializeField] private GameObject _textIndicatorPrefab;
    [SerializeField] private AnimationCurve _scaleCurve = new AnimationCurve(
        new Keyframe(0f, 0.6f),
        new Keyframe(0.3f, 1f),
        new Keyframe(0.6f, 1f),
        new Keyframe(1f, 0f));
    [SerializeField] private AnimationCurve _positionCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(1f, 1f));
    [SerializeField] private float _maxScale = 1f;
    [SerializeField] private float _maxY = 20f;
    [SerializeField] private float _duration = 1f;

    private StageObjectPool<TextIndicatorBehavior> _pool;
    private readonly List<ActiveIndicator> _active = new List<ActiveIndicator>();
    private bool _subscribed;

    #region Unity Methods

    private void Awake()
    {
        if (_canvasRect == null)
        {
            _canvasRect = transform as RectTransform;
        }

        _pool = new StageObjectPool<TextIndicatorBehavior>(_textIndicatorPrefab, _canvasRect);
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (Managers.Instance != null && _subscribed && Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            eventManager.Unsubscribe<DamageTextRequested>(HandleDamageTextRequested);
            eventManager.Unsubscribe<GameStateChanged>(HandleGameStateChanged);
        }

        HideActiveIndicators();
        _subscribed = false;
    }

    private void Update()
    {
        if (_active.Count == 0)
        {
            return;
        }

        if (Managers.Instance == null || !Managers.Instance.TryGetManager<CameraManager>(out var cameraManager))
        {
            return;
        }

        var duration = _duration > 0f ? _duration : 1f;
        var inverseDuration = 1f / duration;
        var time = Time.time;
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var data = _active[i];
            if (data.Indicator == null)
            {
                _active.RemoveAt(i);
                continue;
            }

            var t = (time - data.SpawnTime) * inverseDuration;
            if (t >= 1f)
            {
                data.Indicator.gameObject.SetActive(false);
                _active.RemoveAt(i);
                continue;
            }

            if (!cameraManager.TryWorldToViewport(data.WorldPosition, out var viewport))
            {
                continue;
            }

            var scale = _scaleCurve.Evaluate(t) * _maxScale;
            var anchoredY = _positionCurve.Evaluate(t) * _maxY;
            data.Indicator.SetAnimationParameters(scale, anchoredY, viewport);
        }
    }

    private void OnDestroy()
    {
        _active.Clear();
        _pool?.DestroyAll();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 월드 좌표에 피해 숫자를 띄운다.
    /// </summary>
    public void SpawnText(Vector2 worldPosition, string text)
    {
        if (_pool == null || string.IsNullOrEmpty(text) || Managers.Instance == null)
        {
            return;
        }

        if (IsResultScreen(Managers.Instance.CurrentState))
        {
            return;
        }

        if (!Managers.Instance.TryGetManager<CameraManager>(out var cameraManager))
        {
            return;
        }

        if (!cameraManager.TryWorldToViewport(worldPosition, out var viewport))
        {
            return;
        }

        var indicator = _pool.Get();
        if (indicator == null)
        {
            return;
        }

        indicator.SetText(text);
        var scale = _scaleCurve.Evaluate(0f) * _maxScale;
        var anchoredY = _positionCurve.Evaluate(0f) * _maxY;
        indicator.SetAnimationParameters(scale, anchoredY, viewport);
        _active.Add(new ActiveIndicator
        {
            Indicator = indicator,
            SpawnTime = Time.time,
            WorldPosition = worldPosition
        });
    }

    #endregion

    #region Private Methods

    private void TrySubscribe()
    {
        if (_subscribed || Managers.Instance == null || !Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            return;
        }

        eventManager.Subscribe<DamageTextRequested>(HandleDamageTextRequested);
        eventManager.Subscribe<GameStateChanged>(HandleGameStateChanged);
        _subscribed = true;
        if (IsResultScreen(Managers.Instance.CurrentState))
        {
            HideActiveIndicators();
        }
    }

    private void HandleDamageTextRequested(DamageTextRequested requested)
    {
        SpawnText(requested.WorldPosition, requested.Text);
    }

    private void HandleGameStateChanged(GameStateChanged changed)
    {
        if (IsResultScreen(changed.Next))
        {
            HideActiveIndicators();
        }
    }

    private static bool IsResultScreen(GameState state)
    {
        return state == GameState.Victory || state == GameState.Defeat;
    }

    private void HideActiveIndicators()
    {
        for (var i = 0; i < _active.Count; i++)
        {
            var indicator = _active[i].Indicator;
            if (indicator != null)
            {
                indicator.gameObject.SetActive(false);
            }
        }

        _active.Clear();
    }

    #endregion

    private struct ActiveIndicator
    {
        public TextIndicatorBehavior Indicator;
        public float SpawnTime;
        public Vector2 WorldPosition;
    }
}
