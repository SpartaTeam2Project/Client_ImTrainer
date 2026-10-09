using UnityEditor;
using UnityEngine;

/// <summary>
/// 보스 클립에서 파티 마리수만큼 스탯 칸을 보여 준다.
/// 스킬은 보스 SO 파티와 같은 값이고, 고른 스킬에 쓰는 수치만 연다.
/// </summary>
[CustomEditor(typeof(BossEncounter))]
public class BossEncounterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var encounter = (BossEncounter)target;
        if (encounter.SyncPartyStats())
        {
            EditorUtility.SetDirty(encounter);
        }

        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "_fixedBoss", "_candidates", "_partyStats");

        if (encounter.PartySlotCount <= 0)
        {
            EditorGUILayout.HelpBox("보스 SO 파티에 몬스터를 넣으면 마리마다 스탯 칸이 생깁니다.", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_fixedBoss"), new GUIContent("고정 보스"), true);
        }
        else
        {
            EditorGUILayout.HelpBox("스킬은 보스 SO 파티와 같이 바뀝니다. 수치는 고른 스킬에 맞게 나옵니다.", MessageType.Info);
            DrawPartyStats();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPartyStats()
    {
        var stats = serializedObject.FindProperty("_partyStats");
        var boss = serializedObject.FindProperty("_boss").objectReferenceValue as BossTrainerData;
        SerializedObject bossObject = null;
        SerializedProperty party = null;
        if (boss != null)
        {
            bossObject = new SerializedObject(boss);
            party = bossObject.FindProperty("_party");
        }

        var members = boss != null ? boss.Party : null;
        for (var i = 0; i < stats.arraySize; i++)
        {
            EditorGUILayout.LabelField(BuildLabel(members, i), EditorStyles.boldLabel);
            var skillData = DrawSkill(party, i);
            var skill = skillData != null ? skillData.Kind : BossSkillKind.None;
            if (skillData != null)
            {
                var skillObject = new SerializedObject(skillData);
                skillObject.Update();
                DrawSkillSprites(skillObject, skill);
                skillObject.ApplyModifiedProperties();
            }

            DrawStatsForSkill(stats.GetArrayElementAtIndex(i), skill);
            EditorGUILayout.Space();
        }

        if (bossObject != null)
        {
            bossObject.ApplyModifiedProperties();
        }
    }

    private static BossSkillData DrawSkill(SerializedProperty party, int index)
    {
        if (party == null || index < 0 || index >= party.arraySize)
        {
            return null;
        }

        var member = party.GetArrayElementAtIndex(index);
        var skill = member.FindPropertyRelative("_skill");
        if (skill == null)
        {
            return null;
        }

        EditorGUILayout.PropertyField(skill, new GUIContent("스킬"));
        return skill.objectReferenceValue as BossSkillData;
    }

    /// <summary>
    /// 스킬 SO에 든 탄 그림만 고른 기술에 맞게 연다.
    /// </summary>
    public static void DrawSkillSprites(SerializedObject skillObject, BossSkillKind skill)
    {
        if (skill == BossSkillKind.RisingVolley)
        {
            DrawSkillProperty(skillObject, "_projectileSprite", "탄 그림");
        }

        if (skill == BossSkillKind.FanVolley)
        {
            DrawSkillProperty(skillObject, "_chargeProjectileFrames", "차지 탄");
            DrawSkillProperty(skillObject, "_flyProjectileFrames", "발사 탄");
        }

        if (skill == BossSkillKind.TrackingBeam)
        {
            DrawSkillProperty(skillObject, "_beamFrames", "빔 그림");
            DrawMouthOffsets(skillObject.FindProperty("_beamMouthOffsets"));
        }

        if (skill == BossSkillKind.SleepPowder)
        {
            DrawSkillProperty(skillObject, "_poolBurstFrames", "퍼지는 그림");
            DrawSkillProperty(skillObject, "_poolLingerFrames", "유지 그림");
        }

        if (skill == BossSkillKind.SpoonRing)
        {
            DrawSkillProperty(skillObject, "_spoonFrames", "숟가락 그림");
        }
    }

    private static void DrawSkillProperty(SerializedObject skillObject, string name, string label)
    {
        var property = skillObject.FindProperty(name);
        if (property == null)
        {
            return;
        }

        EditorGUILayout.PropertyField(property, new GUIContent(label), true);
    }

    private static void DrawMouthOffsets(SerializedProperty mouths)
    {
        if (mouths == null)
        {
            return;
        }

        if (mouths.arraySize != BossPartyMember.MOUTH_SECTOR_COUNT)
        {
            mouths.arraySize = BossPartyMember.MOUTH_SECTOR_COUNT;
            for (var i = 0; i < BossPartyMember.MOUTH_SECTOR_COUNT; i++)
            {
                mouths.GetArrayElementAtIndex(i).vector2Value = BossPartyMember.MouthOffset(null, i);
            }
        }

        var labels = new[]
        {
            "입 오른쪽", "입 오른쪽 위", "입 위", "입 왼쪽 위",
            "입 왼쪽", "입 왼쪽 아래", "입 아래", "입 오른쪽 아래",
        };
        for (var i = 0; i < labels.Length; i++)
        {
            EditorGUILayout.PropertyField(mouths.GetArrayElementAtIndex(i), new GUIContent(labels[i]));
        }
    }

    private static void DrawStatsForSkill(SerializedProperty stats, BossSkillKind skill)
    {
        DrawRelative(stats, "_maxHealth");
        DrawRelative(stats, "_contactDamage", "접촉 피해");
        DrawRelative(stats, "_skillDamage", "스킬 피해");
        DrawRelative(stats, "_moveSpeed");
        DrawRelative(stats, "_scale");

        if (skill == BossSkillKind.ChargeDash)
        {
            DrawRelative(stats, "_skillCooldown", "스킬 쿨타임");
            DrawRelative(stats, "_chargeSeconds", "차지 시간");
            DrawRelative(stats, "_dashSpeed", "돌진 속도");
            DrawRelative(stats, "_dashRecoverSeconds", "돌진 후 대기");
            return;
        }

        if (skill == BossSkillKind.RisingVolley)
        {
            DrawRelative(stats, "_skillCooldown", "스킬 쿨타임");
            DrawRelative(stats, "_projectileSpeed", "탄 속도");
            DrawRelative(stats, "_attackDistance", "탄 크기");
            return;
        }

        if (skill == BossSkillKind.Slam)
        {
            DrawRelative(stats, "_skillCooldown", "스킬 쿨타임");
            DrawRelative(stats, "_slamChargeSeconds", "차지 시간");
            DrawRelative(stats, "_skillRange", "스킬 범위");
            DrawRelative(stats, "_slamRecoverSeconds", "착지 후 대기");
            return;
        }

        if (skill == BossSkillKind.Lunge || skill == BossSkillKind.NidokingLunge)
        {
            DrawRelative(stats, "_skillCooldown", "스킬 쿨타임");
            DrawRelative(stats, "_lungeRange", "스킬 돌진범위");
            DrawRelative(stats, "_lungeSpeed", "스킬 돌진 속도");
            DrawRelative(stats, "_lungeRecoverSeconds", "스킬 후 대기");
            return;
        }

        if (skill == BossSkillKind.CircleVolley)
        {
            DrawRelative(stats, "_skillCooldown", "스킬 쿨타임");
            DrawRelative(stats, "_circleMoveSpeed", "스킬 이동속도");
            DrawRelative(stats, "_circleRange", "스킬 범위");
            DrawRelative(stats, "_circleGapSeconds", "스킬 공격간 딜레이");
            return;
        }

        if (skill == BossSkillKind.FanVolley)
        {
            DrawRelative(stats, "_skillCooldown", "스킬 쿨타임");
            DrawRelative(stats, "_fanCastRange", "스킬 시전 범위");
            DrawRelative(stats, "_chargeSeconds", "차지 시간");
            DrawRelative(stats, "_projectileSpeed", "투사체 날아가는 속도");
            DrawRelative(stats, "_fanCount", "투사체 수");
            DrawRelative(stats, "_fanDistance", "투사체 날아가는 거리");
            DrawRelative(stats, "_attackDistance", "탄 크기");
            return;
        }

        if (skill == BossSkillKind.TrackingBeam)
        {
            DrawRelative(stats, "_skillCooldown", "스킬 쿨타임");
            DrawRelative(stats, "_beamCastRange", "시전 범위");
            DrawRelative(stats, "_beamWidth", "스킬 너비");
            DrawRelative(stats, "_beamLength", "스킬 길이");
            DrawRelative(stats, "_chargeSeconds", "차지 시간");
            DrawRelative(stats, "_beamSeconds", "빔 쏘는 시간");
            DrawRelative(stats, "_beamHitInterval", "빔 다단 히트 시간");
            DrawRelative(stats, "_beamTurnSpeed", "방향 전환 속도");
            return;
        }

        if (skill == BossSkillKind.SleepPowder)
        {
            DrawRelative(stats, "_skillCooldown", "스킬 쿨타임");
            DrawRelative(stats, "_poolRange", "스킬 범위");
            DrawRelative(stats, "_poolSpreadSpeed", "퍼지는 시간");
            DrawRelative(stats, "_poolHitsPerSecond", "초당 히트 수");
            DrawRelative(stats, "_poolSlowPercent", "이동속도 감소 퍼센트");
            DrawRelative(stats, "_poolCastSeconds", "시전 시간");
            DrawRelative(stats, "_poolSeconds", "장판 존재하는 시간");
            return;
        }

        if (skill == BossSkillKind.SpoonRing)
        {
            DrawRelative(stats, "_skillCooldown", "스킬 쿨타임");
            DrawRelative(stats, "_spoonCount", "투사체 개수");
            DrawRelative(stats, "_spoonChargeSeconds", "차지 시간");
            DrawRelative(stats, "_spoonSpeed", "날아가는 속도");
            DrawRelative(stats, "_spoonFlySeconds", "날아가는 시간");
            return;
        }

        DrawRelative(stats, "_attackKind");
        DrawRelative(stats, "_attackRange");
        DrawRelative(stats, "_attackInterval");
        DrawRelative(stats, "_projectileSpeed");
        DrawRelative(stats, "_hitRadius");
        DrawRelative(stats, "_attackDistance");
    }

    private static void DrawRelative(SerializedProperty parent, string name, string label = null)
    {
        var property = parent.FindPropertyRelative(name);
        if (property == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(label))
        {
            EditorGUILayout.PropertyField(property, true);
            return;
        }

        EditorGUILayout.PropertyField(property, new GUIContent(label), true);
    }

    private static string BuildLabel(BossPartyMember[] party, int index)
    {
        var number = index + 1;
        if (party == null || index >= party.Length || party[index] == null || party[index].Monster == null)
        {
            return "몬스터 " + number;
        }

        var monsterName = party[index].Monster.MonsterName;
        if (string.IsNullOrEmpty(monsterName))
        {
            return "몬스터 " + number;
        }

        return number + ". " + monsterName;
    }
}

/// <summary>
/// 스킬 SO에서 그 기술이 쓰는 탄 그림만 보여 준다.
/// </summary>
[CustomEditor(typeof(BossSkillData))]
public class BossSkillEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_kind"));
        BossEncounterEditor.DrawSkillSprites(serializedObject, ((BossSkillData)target).Kind);
        serializedObject.ApplyModifiedProperties();
    }
}
