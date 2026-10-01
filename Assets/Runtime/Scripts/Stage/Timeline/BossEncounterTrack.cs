using UnityEngine;
using UnityEngine.Timeline;

/// <summary>
/// 보스전 클립을 올리는 트랙. 몬스터와 마리 수는 클립이 가진다.
/// </summary>
[TrackClipType(typeof(BossEncounter))]
[TrackColor(0.75f, 0.28f, 0.22f)]
public class BossEncounterTrack : TrackAsset
{
}
