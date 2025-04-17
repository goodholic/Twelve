#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(oxCharacter))]
public class oxCharacterAttackPatternEditor : Editor
{
    private const int GRID_SIZE = 5;

    public override void OnInspectorGUI()
    {
        // 기존 기본 Inspector 먼저 표시
        base.OnInspectorGUI();

        // oxCharacter로 캐스팅
        oxCharacter oxChar = (oxCharacter)target;
        var attackPattern = oxChar.AttackPattern;
        if (attackPattern == null)
        {
            EditorGUILayout.HelpBox("CharacterAttackPattern이 비어있습니다!", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("공격 패턴 (5x5)", EditorStyles.boldLabel);

        for (int r = 0; r < GRID_SIZE; r++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < GRID_SIZE; c++)
            {
                bool isCenter = (r == 2 && c == 2);
                if (isCenter)
                {
                    // 중앙(자기 자신 위치)는 항상 true이므로 편집 불가
                    GUI.enabled = false;
                    EditorGUILayout.Toggle(true, GUILayout.Width(20));
                    GUI.enabled = true;
                }
                else
                {
                    bool oldVal = attackPattern.IsAttackable(r, c);
                    bool newVal = EditorGUILayout.Toggle(oldVal, GUILayout.Width(20));
                    if (newVal != oldVal)
                    {
                        Undo.RecordObject(oxChar, "Attack Pattern Change");
                        attackPattern.SetAttackable(r, c, newVal);
                        EditorUtility.SetDirty(oxChar);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(oxChar);
        }
    }
}
#endif
