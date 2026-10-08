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
            var skill = DrawSkill(party, i);
            if (skill == BossSkillKind.RisingVolley)
            {
                DrawProjectile(party, i);
            }

            if (skill == BossSkillKind.FanVolley)
            {
                DrawFanProjectileFrames(party, i);
            }

            DrawStatsForSkill(stats.GetArrayElementAtIndex(i), skill);
            EditorGUILayout.Space();
        }

        if (bossObject != null)
        {
            bossObject.ApplyModifiedProperties();
        }
    }

    private static BossSkillKind DrawSkill(SerializedProperty party, int index)
    {
        if (party == null || index < 0 || index >= party.arraySize)
        {
            return BossSkillKind.None;
        }

        var member = party.GetArrayElementAtIndex(index);
        var skill = member.FindPropertyRelative("_skill");
        if (skill == null)
        {
            return BossSkillKind.None;
        }

        EditorGUILayout.PropertyField(skill, new GUIContent("스킬"));
        var data = skill.objectReferenceValue as BossSkillData;
        return data != null ? data.Kind : BossSkillKind.None;
    }

    private static void DrawProjectile(SerializedProperty party, int index)
    {
        var sprite = party.GetArrayElementAtIndex(index).FindPropertyRelative("_projectileSprite");
        if (sprite == null)
        {
            return;
        }

        EditorGUILayout.PropertyField(sprite, new GUIContent("탄 그림"));
    }

    private static void DrawFanProjectileFrames(SerializedProperty party, int index)
    {
        var member = party.GetArrayElementAtIndex(index);
        var chargeFrames = member.FindPropertyRelative("_chargeProjectileFrames");
        var flyFrames = member.FindPropertyRelative("_flyProjectileFrames");
        if (chargeFrames != null)
        {
            EditorGUILayout.PropertyField(chargeFrames, new GUIContent("차지 탄"), true);
        }

        if (flyFrames != null)
        {
            EditorGUILayout.PropertyField(flyFrames, new GUIContent("발사 탄"), true);
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
