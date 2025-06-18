#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;

[CustomEditor(typeof(CharacterDatabase))]
public class CharacterDatabaseEditor : UnityEditor.Editor
{
    // -------------------------
    // CSV 입력용 멀티라인 문자열
    // -------------------------
    private bool showCsvPanel = false;
    private string csvInput = 
        "Knight, Human, 15, Melee, false, false, 10, 1.0, 1.5, 100\n" +
        "Archer, Human, 10, Ranged, false, false, 12, 1.2, 3.0, 80\n" +
        "Wizard, Elf, 8, LongRange, true, false, 20, 0.8, 4.0, 60";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 대상 스크립트
        CharacterDatabase db = (CharacterDatabase)target;

        // 1) 기존 캐릭터 배열 (개별 수정 가능)
        SerializedProperty charactersProp = serializedObject.FindProperty("currentRegisteredCharacters");
        EditorGUILayout.PropertyField(charactersProp, new GUIContent("Characters (종족별 3명 + 자유 1명)"), includeChildren: true);

        // 종족별 카운트 표시
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("종족별 캐릭터 수", EditorStyles.boldLabel);
        SerializedProperty humanCountProp = serializedObject.FindProperty("humanCount");
        SerializedProperty orcCountProp = serializedObject.FindProperty("orcCount");
        SerializedProperty elfCountProp = serializedObject.FindProperty("elfCount");
        
        GUI.enabled = false;
        EditorGUILayout.PropertyField(humanCountProp, new GUIContent("휴먼"));
        EditorGUILayout.PropertyField(orcCountProp, new GUIContent("오크"));
        EditorGUILayout.PropertyField(elfCountProp, new GUIContent("엘프"));
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();

        // 2) CSV 방식 Bulk 추가 + 랜덤 8개 추가
        showCsvPanel = EditorGUILayout.Foldout(showCsvPanel, "Add Bulk Characters (CSV) / Random Add");
        if (showCsvPanel)
        {
            EditorGUILayout.LabelField("CSV 형태로 캐릭터 정보를 여러 줄에 걸쳐 입력", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "형식: 이름, 종족(Human/Orc/Elf), 공격력(float), RangeType(문자열), 광역여부(true/false), 버프여부(true/false), 코스트(int), 공격속도(float), 공격범위(float), 최대체력(float)\n" +
                "줄바꿈으로 여러 캐릭터를 구분합니다.\n" +
                "RangeType 은 Melee / Ranged / LongRange 중 하나를 사용.\n" +
                "예)\nKnight, Human, 15, Melee, false, false, 10, 1.0, 1.5, 100\nArcher, Human, 10, Ranged, false, false, 12, 1.2, 3.0, 80\nWizard, Elf, 8, LongRange, true, false, 20, 0.8, 4.0, 60",
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

            // 게임 기획서에 맞는 샘플 데이터 추가
            if (GUILayout.Button("Add Sample Characters (휴먼 3, 오크 3, 엘프 3, 자유 1)"))
            {
                AddSampleCharacters(db);
            }

            EditorGUILayout.Space();

            // 데이터베이스 갱신 버튼
            if (GUILayout.Button("Refresh Database from GameManager"))
            {
                db.RefreshDatabase();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// CSV 문자열을 파싱하여 여러 CharacterData를 추가
    /// 형식: "이름, 종족, 공격력, RangeType, 광역여부, 버프여부, 코스트, 공격속도, 공격범위, 최대체력"
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
        int newSize = Mathf.Min(oldSize + addCount, 10); // 최대 10개로 제한
        charactersProp.arraySize = newSize;
        
        int index = oldSize;

        foreach (string line in lines)
        {
            // 공백 또는 빈 줄은 무시
            if (string.IsNullOrWhiteSpace(line)) 
                continue;

            if (index >= 10) break; // 10개 초과 방지

            // 쉼표 분리
            string[] tokens = line.Split(',');
            if (tokens.Length < 10)
            {
                Debug.LogWarning($"CSV 파싱 실패 (필드가 10개 미만): {line}");
                continue;
            }

            SerializedProperty charDataProp = charactersProp.GetArrayElementAtIndex(index);
            
            // 필드 파싱
            string nameStr = tokens[0].Trim();
            string raceStr = tokens[1].Trim();
            string atkStr = tokens[2].Trim();
            string rangeStr = tokens[3].Trim();
            string areaStr = tokens[4].Trim();
            string buffStr = tokens[5].Trim();
            string costStr = tokens[6].Trim();
            string aspdStr = tokens[7].Trim();
            string atkRangeStr = tokens[8].Trim();
            string maxHPStr = tokens[9].Trim();

            // SerializedProperty를 통해 값 설정
            charDataProp.FindPropertyRelative("characterName").stringValue = nameStr;
            
            // 종족 파싱
            CharacterRace race = CharacterRace.Human;
            if (Enum.TryParse(raceStr, true, out race))
            {
                charDataProp.FindPropertyRelative("race").enumValueIndex = (int)race;
            }
            
            float atkVal = 10f;
            float.TryParse(atkStr, out atkVal);
            charDataProp.FindPropertyRelative("attackPower").floatValue = atkVal;
            
            RangeType rt = RangeType.Melee;
            if (!Enum.TryParse(rangeStr, true, out rt))
            {
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
            
            float aspdVal = 1.0f;
            float.TryParse(aspdStr, out aspdVal);
            charDataProp.FindPropertyRelative("attackSpeed").floatValue = aspdVal;
            
            float atkRangeVal = 1.5f;
            float.TryParse(atkRangeStr, out atkRangeVal);
            charDataProp.FindPropertyRelative("attackRange").floatValue = atkRangeVal;
            
            float maxHPVal = 100f;
            float.TryParse(maxHPStr, out maxHPVal);
            charDataProp.FindPropertyRelative("maxHP").floatValue = maxHPVal;
            
            // 기본 1성으로 설정
            charDataProp.FindPropertyRelative("initialStar").enumValueIndex = (int)CharacterStar.OneStar;
            
            index++;
        }

        // 변경사항 적용
        serializedDB.ApplyModifiedProperties();
        Debug.Log($"CSV로부터 {Mathf.Min(addCount, 10 - oldSize)}개의 캐릭터가 추가되었습니다.");
    }

    /// <summary>
    /// 게임 기획서에 맞는 샘플 캐릭터 추가
    /// </summary>
    private void AddSampleCharacters(CharacterDatabase db)
    {
        // 안전하게 SerializedObject를 통해 작업
        SerializedObject serializedDB = new SerializedObject(db);
        SerializedProperty charactersProp = serializedDB.FindProperty("currentRegisteredCharacters");
        
        // 배열 크기를 10으로 설정
        charactersProp.arraySize = 10;
        
        // 샘플 데이터
        string[] sampleData = new string[]
        {
            // 휴먼 3명 (0-2)
            "Human Knight, Human, 15, Melee, false, false, 10, 1.0, 1.5, 120",
            "Human Archer, Human, 12, Ranged, false, false, 8, 1.2, 3.0, 80",
            "Human Wizard, Human, 20, LongRange, true, false, 15, 0.8, 4.0, 60",
            // 오크 3명 (3-5)
            "Orc Warrior, Orc, 18, Melee, false, false, 12, 0.9, 1.8, 150",
            "Orc Shaman, Orc, 14, Ranged, true, true, 10, 1.1, 3.5, 90",
            "Orc Berserker, Orc, 22, Melee, true, false, 18, 1.5, 2.0, 110",
            // 엘프 3명 (6-8)
            "Elf Ranger, Elf, 13, Ranged, false, false, 9, 1.4, 4.0, 70",
            "Elf Druid, Elf, 11, LongRange, true, true, 12, 1.0, 3.5, 85",
            "Elf Assassin, Elf, 16, Melee, false, false, 11, 1.8, 1.2, 65",
            // 자유 1명 (9)
            "Dragon Knight, Human, 25, Melee, true, false, 20, 1.2, 2.5, 200"
        };

        for (int i = 0; i < 10; i++)
        {
            string[] tokens = sampleData[i].Split(',');
            SerializedProperty charDataProp = charactersProp.GetArrayElementAtIndex(i);
            
            // 캐릭터 데이터 설정
            charDataProp.FindPropertyRelative("characterName").stringValue = tokens[0].Trim();
            
            CharacterRace race = CharacterRace.Human;
            Enum.TryParse(tokens[1].Trim(), true, out race);
            charDataProp.FindPropertyRelative("race").enumValueIndex = (int)race;
            
            float atkVal = 10f;
            float.TryParse(tokens[2].Trim(), out atkVal);
            charDataProp.FindPropertyRelative("attackPower").floatValue = atkVal;
            
            RangeType rt = RangeType.Melee;
            Enum.TryParse(tokens[3].Trim(), true, out rt);
            charDataProp.FindPropertyRelative("rangeType").enumValueIndex = (int)rt;
            
            bool areaAttack = false;
            bool.TryParse(tokens[4].Trim(), out areaAttack);
            charDataProp.FindPropertyRelative("isAreaAttack").boolValue = areaAttack;
            
            bool buffSupport = false;
            bool.TryParse(tokens[5].Trim(), out buffSupport);
            charDataProp.FindPropertyRelative("isBuffSupport").boolValue = buffSupport;
            
            int costVal = 10;
            int.TryParse(tokens[6].Trim(), out costVal);
            charDataProp.FindPropertyRelative("cost").intValue = costVal;
            
            float aspdVal = 1.0f;
            float.TryParse(tokens[7].Trim(), out aspdVal);
            charDataProp.FindPropertyRelative("attackSpeed").floatValue = aspdVal;
            
            float atkRangeVal = 1.5f;
            float.TryParse(tokens[8].Trim(), out atkRangeVal);
            charDataProp.FindPropertyRelative("attackRange").floatValue = atkRangeVal;
            
            float maxHPVal = 100f;
            float.TryParse(tokens[9].Trim(), out maxHPVal);
            charDataProp.FindPropertyRelative("maxHP").floatValue = maxHPVal;
            
            // 기본 1성으로 설정
            charDataProp.FindPropertyRelative("initialStar").enumValueIndex = (int)CharacterStar.OneStar;
        }

        // 변경사항 적용
        serializedDB.ApplyModifiedProperties();
        Debug.Log("게임 기획서에 맞는 샘플 캐릭터 10개 추가 완료!");
    }
}
#endif