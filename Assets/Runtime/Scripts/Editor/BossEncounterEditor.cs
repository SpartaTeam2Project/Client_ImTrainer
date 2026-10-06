using UnityEditor;
using UnityEngine;

/// <summary>
/// 보스 클립에서 파티 마리수만큼 스탯 칸을 보여 준다.
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
            EditorGUILayout.HelpBox("파티 순서대로 스탯을 조절합니다. 몬스터와 스킬은 보스 SO에 있습니다.", MessageType.Info);
            DrawPartyStats();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPartyStats()
    {
        var stats = serializedObject.FindProperty("_partyStats");
        var boss = serializedObject.FindProperty("_boss").objectReferenceValue as BossTrainerData;
        var party = boss != null ? boss.Party : null;
        for (var i = 0; i < stats.arraySize; i++)
        {
            EditorGUILayout.PropertyField(stats.GetArrayElementAtIndex(i), new GUIContent(BuildLabel(party, i)), true);
        }
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
