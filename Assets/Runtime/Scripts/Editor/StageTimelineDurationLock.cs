using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// 고정 길이 타임라인에서 정한 시각 뒤로는 클립을 두지 않는다.
/// </summary>
[InitializeOnLoad]
public static class StageTimelineDurationLock
{
    private const double FRAME_SECONDS = 1d / 60d;
    private const string UNDO_NAME = "타임라인 길이 잠금";

    private static EntityId _appliedAssetId;
    private static double _appliedLimit = -1d;

    static StageTimelineDurationLock()
    {
        EditorApplication.update += ApplyWhenIdle;
    }

    /// <summary>
    /// 클립을 고정 길이 안으로 되돌린다. 길이 모드가 아니면 그대로 둔다.
    /// </summary>
    public static void ClampClip(TimelineClip clip)
    {
        if (!TryGetLimit(clip, out var limit))
        {
            return;
        }

        if (clip.start >= limit)
        {
            var duration = System.Math.Max(FRAME_SECONDS, clip.duration);
            clip.start = System.Math.Max(0d, limit - duration);
        }

        if (clip.end > limit + FRAME_SECONDS)
        {
            clip.duration = System.Math.Max(FRAME_SECONDS, limit - clip.start);
        }
    }

    private static void ApplyWhenIdle()
    {
        if (GUIUtility.hotControl != 0)
        {
            return;
        }

        var timeline = TimelineEditor.inspectedAsset;
        if (timeline == null && Selection.activeObject is TimelineAsset selected)
        {
            timeline = selected;
        }

        if (timeline == null || timeline.durationMode != TimelineAsset.DurationMode.FixedLength)
        {
            return;
        }

        var limit = timeline.fixedDuration;
        if (timeline.GetEntityId() == _appliedAssetId && System.Math.Abs(limit - _appliedLimit) < FRAME_SECONDS)
        {
            return;
        }

        LockTracks(timeline, limit);
        _appliedAssetId = timeline.GetEntityId();
        _appliedLimit = limit;
    }

    private static void LockTracks(TimelineAsset timeline, double limit)
    {
        if (limit <= 0d)
        {
            return;
        }

        var clips = new List<TimelineClip>();
        foreach (var track in timeline.GetOutputTracks())
        {
            if (track == null)
            {
                continue;
            }

            clips.AddRange(track.GetClips());
        }

        var remove = new List<TimelineClip>();
        var trimmed = false;
        for (var i = 0; i < clips.Count; i++)
        {
            var clip = clips[i];
            if (clip.start >= limit)
            {
                remove.Add(clip);
                continue;
            }

            if (clip.end <= limit + FRAME_SECONDS)
            {
                continue;
            }

            var track = clip.GetParentTrack();
            if (!trimmed)
            {
                Undo.RegisterCompleteObjectUndo(timeline, UNDO_NAME);
                trimmed = true;
            }

            if (track != null)
            {
                Undo.RegisterCompleteObjectUndo(track, UNDO_NAME);
            }

            clip.duration = System.Math.Max(FRAME_SECONDS, limit - clip.start);
        }

        for (var i = 0; i < remove.Count; i++)
        {
            timeline.DeleteClip(remove[i]);
        }

        if (!trimmed && remove.Count == 0)
        {
            return;
        }

        EditorUtility.SetDirty(timeline);
        TimelineEditor.Refresh(RefreshReason.ContentsModified);
    }

    private static bool TryGetLimit(TimelineClip clip, out double limit)
    {
        limit = 0d;
        var timeline = clip != null ? clip.GetParentTrack()?.timelineAsset : null;
        if (timeline == null || timeline.durationMode != TimelineAsset.DurationMode.FixedLength)
        {
            return false;
        }

        limit = timeline.fixedDuration;
        return limit > 0d;
    }
}

/// <summary>
/// 클립을 늘리거나 옮길 때 고정 길이를 넘지 못하게 한다.
/// </summary>
[CustomTimelineEditor(typeof(PlayableAsset))]
public class StageTimelineClipLock : ClipEditor
{
    /// <summary>
    /// 에디터가 클립을 바꾼 뒤 고정 길이 안으로 맞춘다.
    /// </summary>
    public override void OnClipChanged(TimelineClip clip)
    {
        StageTimelineDurationLock.ClampClip(clip);
    }
}
