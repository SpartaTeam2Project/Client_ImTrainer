using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상단 바를 스테이지 타임라인 진행도로 채운다. 보스 조우 시각에는 마커를 둔다.
/// </summary>
public class StageProgressBar : MonoBehaviour
{
    private static readonly Vector2 MARKER_SIZE = new Vector2(100f, 100f);

    // MVP 시연 임시. 진행도 바만 이 길이(초)로 그린다. 0이면 타임라인 실제 길이를 쓴다.
    // 정상화 요청 문구: "진행도 바 정상화해줘"
    private const double MVP_DEMO_BAR_SECONDS = 40d;

    [SerializeField] private RectMask2D _fillMask;
    [SerializeField] private Sprite _markerSprite;

    private const double BOSS_MATCH_SECONDS = 2d;

    private readonly List<double> _bossTimes = new List<double>();
    private readonly List<double> _clearedBossTimes = new List<double>();
    private readonly List<double> _handledClears = new List<double>();
    private readonly List<BossMarker> _markers = new List<BossMarker>();
    private RectTransform _markerRoot;
    private StageController _stage;
    private bool _markersBuilt;

    private void OnEnable()
    {
        _stage = null;
        _markersBuilt = false;
        ApplyFill(0f);
    }

    private void OnDisable()
    {
        ClearMarkers();
        _stage = null;
        _markersBuilt = false;
    }

    private void Update()
    {
        var stage = ResolveStage();
        if (stage != _stage)
        {
            _stage = stage;
            ClearMarkers();
            _markersBuilt = false;
        }

        if (stage == null || !stage.TryGetTimelineProgress(out var time, out var duration))
        {
            ApplyFill(0f);
            return;
        }

        var barDuration = BarDuration(duration);
        if (!_markersBuilt)
        {
            _markersBuilt = BuildMarkers(stage, barDuration);
        }

        RemoveClearedMarkers(stage);
        ApplyFill(Mathf.Clamp01((float)(time / barDuration)));
    }

    private static StageController ResolveStage()
    {
        if (Managers.Instance == null)
        {
            return null;
        }

        var gameController = Managers.Instance.GetComponent<GameController>();
        return gameController != null ? gameController.ActiveStage : null;
    }

    private static double BarDuration(double timelineDuration)
    {
        return MVP_DEMO_BAR_SECONDS > 0d ? MVP_DEMO_BAR_SECONDS : timelineDuration;
    }

    private bool BuildMarkers(StageController stage, double duration)
    {
        if (_markerSprite == null || duration <= 0d)
        {
            return false;
        }

        EnsureMarkerRoot();
        stage.CopyBossMarkerTimes(_bossTimes);
        for (var i = 0; i < _bossTimes.Count; i++)
        {
            CreateMarker(_bossTimes[i], Mathf.Clamp01((float)(_bossTimes[i] / duration)));
        }

        return true;
    }

    private void RemoveClearedMarkers(StageController stage)
    {
        stage.CopyClearedBossTimes(_clearedBossTimes);
        for (var i = 0; i < _clearedBossTimes.Count; i++)
        {
            var clearedTime = _clearedBossTimes[i];
            if (ContainsTime(_handledClears, clearedTime))
            {
                continue;
            }

            _handledClears.Add(clearedTime);
            RemoveNearestMarker(clearedTime);
        }
    }

    private void RemoveNearestMarker(double clearedTime)
    {
        var nearest = -1;
        var nearestDistance = BOSS_MATCH_SECONDS;
        for (var i = 0; i < _markers.Count; i++)
        {
            var distance = System.Math.Abs(_markers[i].Time - clearedTime);
            if (distance > nearestDistance)
            {
                continue;
            }

            nearest = i;
            nearestDistance = distance;
        }

        if (nearest < 0)
        {
            return;
        }

        var rect = _markers[nearest].Rect;
        _markers.RemoveAt(nearest);
        if (rect != null)
        {
            Destroy(rect.gameObject);
        }
    }

    private static bool ContainsTime(List<double> times, double time)
    {
        for (var i = 0; i < times.Count; i++)
        {
            if (System.Math.Abs(times[i] - time) <= BOSS_MATCH_SECONDS)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureMarkerRoot()
    {
        if (_markerRoot != null)
        {
            return;
        }

        var rootObject = new GameObject("Boss Markers");
        _markerRoot = rootObject.AddComponent<RectTransform>();
        _markerRoot.SetParent(transform, false);
        _markerRoot.anchorMin = Vector2.zero;
        _markerRoot.anchorMax = Vector2.one;
        _markerRoot.offsetMin = Vector2.zero;
        _markerRoot.offsetMax = Vector2.zero;
        _markerRoot.SetAsLastSibling();
    }

    private void CreateMarker(double time, float normalizedTime)
    {
        var markerObject = new GameObject("Boss Marker");
        var rect = markerObject.AddComponent<RectTransform>();
        _markers.Add(new BossMarker(time, rect));
        rect.SetParent(_markerRoot, false);
        rect.anchorMin = new Vector2(normalizedTime, 0.5f);
        rect.anchorMax = new Vector2(normalizedTime, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = MARKER_SIZE;
        rect.anchoredPosition = Vector2.zero;

        var image = markerObject.AddComponent<Image>();
        image.sprite = _markerSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private void ClearMarkers()
    {
        if (_markerRoot == null)
        {
            return;
        }

        Destroy(_markerRoot.gameObject);
        _markerRoot = null;
        _markers.Clear();
        _handledClears.Clear();
    }

    private readonly struct BossMarker
    {
        public BossMarker(double time, RectTransform rect)
        {
            Time = time;
            Rect = rect;
        }

        public double Time { get; }

        public RectTransform Rect { get; }
    }

    private void ApplyFill(float shown)
    {
        if (_fillMask == null)
        {
            return;
        }

        var width = _fillMask.rectTransform.rect.width;
        if (width <= 0f)
        {
            return;
        }

        var padding = _fillMask.padding;
        padding.z = width * (1f - shown);
        _fillMask.padding = padding;
    }
}
