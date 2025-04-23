#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;

[CustomEditor(typeof(CharacterDatabase))]
public class CharacterDatabaseEditor : Editor
{
    // -------------------------
    // CSV 입력용 멀티라인 문자열
    // -------------------------
    private bool showCsvPanel = false;
    private string csvInput = 
        "Knight, 15, Melee, false, false, 10\n" +
        "Archer, 10, Ranged, false, false, 12\n" +
        "Wizard, 8, LongRange, true, false, 20";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 대상 스크립트
        CharacterDatabase db = (CharacterDatabase)target;

        // 1) 기존 캐릭터 배열 (개별 수정 가능)
        SerializedProperty charactersProp = serializedObject.FindProperty("currentRegisteredCharacters");
        EditorGUILayout.PropertyField(charactersProp, new GUIContent("Characters"), includeChildren: true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();

        // 2) CSV 방식 Bulk 추가 + 랜덤 8개 추가
        showCsvPanel = EditorGUILayout.Foldout(showCsvPanel, "Add Bulk Characters (CSV) / Random Add");
        if (showCsvPanel)
        {
            EditorGUILayout.LabelField("CSV 형태로 캐릭터 정보를 여러 줄에 걸쳐 입력", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "형식: 이름, 공격력(float), RangeType(문자열), 광역여부(true/false), 버프여부(true/false), 코스트(int)\n" +
                "줄바꿈으로 여러 캐릭터를 구분합니다.\n" +
                "RangeType 은 Melee / Ranged / LongRange 중 하나를 사용.\n" +
                "예)\nKnight, 15, Melee, false, false, 10\nArcher, 10, Ranged, false, false, 12\nWizard, 8, LongRange, true, false, 20",
                MessageType.Info
            );

            // 멀티라인 텍스트 필드
            csvInput = EditorGUILayout.TextArea(csvInput, GUILayout.MinHeight(80));

            // CSV로 추가 버튼
            if (GUILayout.Button("Add Characters from CSV"))
            {
                AddCharactersFromCsv(db, csvInput);
            }

            EditorGUILayout.Space();

            // 랜덤 8개 추가
            if (GUILayout.Button("Add 8 Random Characters"))
            {
                AddRandomCharacters(db, 8);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// CSV 문자열을 파싱하여 여러 CharacterData를 추가
    /// 형식: "이름, 공격력, RangeType, 광역여부, 버프여부, 코스트"
    /// 줄 단위로 split
    /// </summary>
    private void AddCharactersFromCsv(CharacterDatabase db, string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return;

        // 줄 단위로 분리
        string[] lines = csv.Split('\n');
        if (lines == null || lines.Length == 0) return;

        // 유효한 라인 수 계산
        int addCount = 0;
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line)) addCount++;
        }
        if (addCount == 0) return;

        // 안전하게 SerializedObject를 통해 작업
        SerializedObject serializedDB = new SerializedObject(db);
        SerializedProperty charactersProp = serializedDB.FindProperty("currentRegisteredCharacters");
        
        // 배열 크기 조정
        int oldSize = charactersProp.arraySize;
        int newSize = oldSize + addCount;
        charactersProp.arraySize = newSize;
        
        int index = oldSize;

        foreach (string line in lines)
        {
            // 공백 또는 빈 줄은 무시
            if (string.IsNullOrWhiteSpace(line)) 
                continue;

            // 쉼표 분리
            string[] tokens = line.Split(',');
            if (tokens.Length < 6)
            {
                Debug.LogWarning($"CSV 파싱 실패 (필드가 6개 미만): {line}");
                continue;
            }

            SerializedProperty charDataProp = charactersProp.GetArrayElementAtIndex(index);
            
            // 필드 파싱
            string nameStr = tokens[0].Trim();
            string atkStr = tokens[1].Trim();
            string rangeStr = tokens[2].Trim();
            string areaStr = tokens[3].Trim();
            string buffStr = tokens[4].Trim();
            string costStr = tokens[5].Trim();

            // SerializedProperty를 통해 값 설정
            charDataProp.FindPropertyRelative("characterName").stringValue = nameStr;
            
            float atkVal = 10f;
            float.TryParse(atkStr, out atkVal);
            charDataProp.FindPropertyRelative("attackPower").floatValue = atkVal;
            
            RangeType rt = RangeType.Melee;
            if (!Enum.TryParse(rangeStr, true, out rt))
            {
                // 잘못된 값이면 기본 Melee
                Debug.LogWarning($"CSV RangeType 파싱 실패: '{rangeStr}', 기본값(Melee)으로 처리");
            }
            charDataProp.FindPropertyRelative("rangeType").enumValueIndex = (int)rt;
            
            bool areaAttack = false;
            bool.TryParse(areaStr, out areaAttack);
            charDataProp.FindPropertyRelative("isAreaAttack").boolValue = areaAttack;
            
            bool buffSupport = false;
            bool.TryParse(buffStr, out buffSupport);
            charDataProp.FindPropertyRelative("isBuffSupport").boolValue = buffSupport;
            
            int costVal = 10;
            int.TryParse(costStr, out costVal);
            charDataProp.FindPropertyRelative("cost").intValue = costVal;
            
            index++;
        }

        // 변경사항 적용
        serializedDB.ApplyModifiedProperties();
        Debug.Log($"CSV로부터 {addCount}개의 캐릭터가 추가되었습니다.");
    }

    /// <summary>
    /// 8개의 랜덤 데이터를 생성하여 추가 (기존과 동일)
    /// </summary>
    private void AddRandomCharacters(CharacterDatabase db, int count)
    {
        if (count <= 0) return;

        // 안전하게 SerializedObject를 통해 작업
        SerializedObject serializedDB = new SerializedObject(db);
        SerializedProperty charactersProp = serializedDB.FindProperty("currentRegisteredCharacters");
        
        // 배열 크기 조정
        int oldSize = charactersProp.arraySize;
        int newSize = oldSize + count;
        charactersProp.arraySize = newSize;
        
        System.Random rand = new System.Random();

        for (int i = 0; i < count; i++)
        {
            int index = oldSize + i;
            SerializedProperty charDataProp = charactersProp.GetArrayElementAtIndex(index);
            
            // 무작위로 값 설정
            charDataProp.FindPropertyRelative("characterName").stringValue = $"RandomChar_{index}";
            charDataProp.FindPropertyRelative("attackPower").floatValue = rand.Next(5, 31);
            charDataProp.FindPropertyRelative("rangeType").enumValueIndex = rand.Next(0, 3);
            charDataProp.FindPropertyRelative("isAreaAttack").boolValue = (rand.Next(0, 2) == 0);
            charDataProp.FindPropertyRelative("isBuffSupport").boolValue = (rand.Next(0, 2) == 0);
            charDataProp.FindPropertyRelative("cost").intValue = rand.Next(5, 51);
        }

        // 변경사항 적용
        serializedDB.ApplyModifiedProperties();
        Debug.Log($"랜덤 캐릭터 {count}개 추가 완료!");
    }
}
#endif
