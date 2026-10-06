using UnityEditor;

/// <summary>
/// 동작 그림 세트를 동작별로 접어서 보여 준다. MonsterVisualData 인스펙터 안에도 이 화면이 그려진다.
/// </summary>
[CustomEditor(typeof(MonsterAnimationSet))]
public class MonsterAnimationSetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        MonsterVisualDataEditor.DrawActions(serializedObject);
        serializedObject.ApplyModifiedProperties();
    }
}
