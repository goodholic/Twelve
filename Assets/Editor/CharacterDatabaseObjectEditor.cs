#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;


/// <summary>
/// CharacterDatabaseObject(.asset) 전용 커스텀 에디터.
/// Inspector에서 CSV로 캐릭터를 추가하거나, 랜덤 생성할 수 있음.
/// 추가로, 씬 내 CharacterDatabase 또는 CharacterInventoryManager를 복사해
/// 새 ScriptableObject를 자동으로 만들 수도 있음.
/// </summary>
[CustomEditor(typeof(CharacterDatabaseObject))]
public class CharacterDatabaseObjectEditor : Editor
{
    private bool showCsvPanel = false;
    private string csvInput =
        "Knight, 15, Melee, false, false, 10\n" +
        "Archer, 10, Ranged, false, false, 12\n" +
        "Wizard, 8, LongRange, true, false, 20";

    public override void OnInspectorGUI()
    {
        // 이미 제거되었거나 disposing된 오브젝트 체크
        if (serializedObject == null || serializedObject.targetObject == null)
        {
            EditorGUILayout.HelpBox("SerializedObject가 유효하지 않습니다.", MessageType.Error);
            return;
        }

        try
        {
            serializedObject.Update();

            // 현재 ScriptableObject
            CharacterDatabaseObject dbObj = (CharacterDatabaseObject)target;

            // ▼▼ [수정추가] 만약 dbObj가 null이면 안전하게 리턴 ▼▼
            if (dbObj == null)
            {
                EditorGUILayout.HelpBox("CharacterDatabaseObject가 null입니다.", MessageType.Warning);
                return;
            }
            // ▲▲ [수정끝] ▲▲

            SerializedProperty charactersProp = serializedObject.FindProperty("characters");
            if (charactersProp == null)
            {
                EditorGUILayout.HelpBox("characters 속성을 찾을 수 없습니다.", MessageType.Warning);
                return;
            }

            // 1) 기본 배열 필드를 Inspector에 표시
            EditorGUILayout.PropertyField(charactersProp, new GUIContent("Characters"), includeChildren: true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();

            // 2) CSV 패널 (다수 추가/랜덤 생성)
            showCsvPanel = EditorGUILayout.Foldout(showCsvPanel, "Add Bulk Characters (CSV) / Random Add");
            if (showCsvPanel)
            {
                EditorGUILayout.LabelField("CSV 형태로 캐릭터 정보를 여러 줄에 걸쳐 입력", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "형식: 이름, 공격력(float), RangeType(문자열), 광역여부(true/false), 버프여부(true/false), 코스트(int)\n" +
                    "줄바꿈으로 여러 캐릭터를 구분.\n" +
                    "RangeType은 Melee / Ranged / LongRange.\n" +
                    "예)\nKnight, 15, Melee, false, false, 10\nArcher, 10, Ranged, false, false, 12\nWizard, 8, LongRange, true, false, 20",
                    MessageType.Info);

                // CSV 입력 텍스트
                csvInput = EditorGUILayout.TextArea(csvInput, GUILayout.MinHeight(80));

                // CSV로 추가 버튼
                if (GUILayout.Button("Add Characters from CSV"))
                {
                    AddCharactersFromCsv(dbObj, charactersProp, csvInput);
                }

                EditorGUILayout.Space();

                // 랜덤 8개 추가 버튼
                if (GUILayout.Button("Add 8 Random Characters"))
                {
                    AddRandomCharacters(dbObj, charactersProp, 8);
                }
            }

            // ▼▼ [수정추가] 'Save Database' 버튼 ▼▼
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();

            if (GUILayout.Button("Save Database"))
            {
                try
                {
                    // 변경사항을 적용하고 ScriptableObject를 저장
                    serializedObject.ApplyModifiedProperties();
                    
                    // 유효한 에셋인지 확인
                    string assetPath = AssetDatabase.GetAssetPath(dbObj);
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        Debug.LogError("에셋 경로를 찾을 수 없습니다. 저장할 수 없습니다.");
                        return;
                    }

                    EditorUtility.SetDirty(dbObj);       // 변경 플래그 지정
                    AssetDatabase.SaveAssets();          // 실제 파일로 저장
                    AssetDatabase.Refresh();
                    Debug.Log("CharacterDatabaseObject 저장 완료!");

                    // ============================================
                    //  (추가) "저장 시점으로 다시 되돌리는" 로직
                    // ============================================
                    // 이미 Save된 값을 다시 디스크에서 불러옴으로써,
                    // 설사 지금 Inspector에서 값이 바뀌어 있어도 "저장된 시점"으로 강제로 복원한다.
                    CharacterDatabaseObject reloaded = AssetDatabase.LoadAssetAtPath<CharacterDatabaseObject>(assetPath);
                    if (reloaded != null)
                    {
                        // characters 배열도 디스크 값으로 덮어씀
                        if (reloaded.characters != null)
                        {
                            dbObj.characters = reloaded.characters;

                            // 새롭게 SerializedObject를 갱신하여 Inspector도 다시 표시
                            EditorUtility.SetDirty(dbObj);
                            AssetDatabase.Refresh();
                            serializedObject.Update();
                            Debug.Log("저장 직후, 디스크상의 데이터로 되돌렸습니다 (Inspector 변경사항 무시).");
                        }
                        else
                        {
                            Debug.LogWarning("다시 로드된 에셋에 characters 배열이 없습니다.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("에셋을 다시 로드할 수 없습니다: " + assetPath);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"데이터베이스 저장 중 오류 발생: {e.Message}\n{e.StackTrace}");
                }
            }
            // ▲▲ [수정끝] ▲▲

            serializedObject.ApplyModifiedProperties();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CharacterDatabaseObjectEditor에서 오류 발생: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// CSV 파싱 → CharacterData 배열에 추가
    /// </summary>
    private void AddCharactersFromCsv(CharacterDatabaseObject dbObj, SerializedProperty charactersProp, string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return;

        string[] lines = csv.Split('\n');
        if (lines == null || lines.Length == 0) return;

        // 유효 라인 수
        int addCount = 0;
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line)) addCount++;
        }
        if (addCount == 0) return;

        // 배열 크기 확장
        int oldSize = charactersProp.arraySize;
        int newSize = oldSize + addCount;
        charactersProp.arraySize = newSize;

        int index = oldSize;

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // 쉼표로 필드 분리
            string[] tokens = line.Split(',');
            if (tokens.Length < 6)
            {
                Debug.LogWarning($"CSV 파싱 실패 (필드가 6개 미만): {line}");
                continue;
            }

            SerializedProperty elementProp = charactersProp.GetArrayElementAtIndex(index);

            // 필드 추출
            string nameStr  = tokens[0].Trim();
            string atkStr   = tokens[1].Trim();
            string rangeStr = tokens[2].Trim();
            string areaStr  = tokens[3].Trim();
            string buffStr  = tokens[4].Trim();
            string costStr  = tokens[5].Trim();

            // SerializedProperty에 값 할당
            elementProp.FindPropertyRelative("characterName").stringValue = nameStr;

            float atkVal;
            if (!float.TryParse(atkStr, out atkVal)) atkVal = 10f;
            elementProp.FindPropertyRelative("attackPower").floatValue = atkVal;

            RangeType rt = RangeType.Melee;
            if (!Enum.TryParse(rangeStr, true, out rt))
            {
                Debug.LogWarning($"CSV RangeType 파싱 실패: '{rangeStr}', 기본값(Melee)");
            }
            elementProp.FindPropertyRelative("rangeType").enumValueIndex = (int)rt;

            bool areaBool;
            bool.TryParse(areaStr, out areaBool);
            elementProp.FindPropertyRelative("isAreaAttack").boolValue = areaBool;

            bool buffBool;
            bool.TryParse(buffStr, out buffBool);
            elementProp.FindPropertyRelative("isBuffSupport").boolValue = buffBool;

            int costVal;
            if (!int.TryParse(costStr, out costVal)) costVal = 10;
            elementProp.FindPropertyRelative("cost").intValue = costVal;

            index++;
        }

        serializedObject.ApplyModifiedProperties();
        Debug.Log($"CSV로부터 {addCount}개의 캐릭터 추가 완료! (ScriptableObject)");
    }

    /// <summary>
    /// 8개 랜덤 CharacterData 생성
    /// </summary>
    private void AddRandomCharacters(CharacterDatabaseObject dbObj, SerializedProperty charactersProp, int count)
    {
        if (count <= 0) return;

        int oldSize = charactersProp.arraySize;
        int newSize = oldSize + count;
        charactersProp.arraySize = newSize;

        System.Random rand = new System.Random();

        for (int i = 0; i < count; i++)
        {
            int index = oldSize + i;
            SerializedProperty elementProp = charactersProp.GetArrayElementAtIndex(index);

            elementProp.FindPropertyRelative("characterName").stringValue = $"RandomChar_{index}";
            elementProp.FindPropertyRelative("attackPower").floatValue = rand.Next(5, 31);
            elementProp.FindPropertyRelative("rangeType").enumValueIndex = rand.Next(0, 3);
            elementProp.FindPropertyRelative("isAreaAttack").boolValue = (rand.Next(0, 2) == 0);
            elementProp.FindPropertyRelative("isBuffSupport").boolValue = (rand.Next(0, 2) == 0);
            elementProp.FindPropertyRelative("cost").intValue = rand.Next(5, 51);
        }

        serializedObject.ApplyModifiedProperties();
        Debug.Log($"랜덤 캐릭터 {count}개 추가 완료! (ScriptableObject)");
    }

    // ==========================================================
    // === 아래부터 추가: 자동으로 씬의 CharacterDatabase      ===
    // === 또는 InventoryManager ScriptableObject를 찾아       ===
    // === .asset 생성 (8개 캐릭터 등)                        ===
    // ==========================================================

    [MenuItem("Assets/Create/MyGame/Character Database (Auto from Scene)")]
    public static void CreateDatabaseFromExistingSceneDB()
    {
        // 1) 우선 씬에서 CharacterDatabase(MonoBehaviour) 찾기
        CharacterDatabase sceneDb = UnityEngine.Object.FindAnyObjectByType<CharacterDatabase>();
        if (sceneDb != null)
        {
            // 씬 DB가 있으면 그것을 복사
            CreateAssetFromSceneDB(sceneDb);
            return;
        }

        // 2) 씬에 CharacterDatabase가 없다면 -> CharacterInventoryManager(.characterDatabaseObject) 확인
        CharacterInventoryManager invManager = UnityEngine.Object.FindAnyObjectByType<CharacterInventoryManager>();
        if (invManager != null)
        {
            // SerializedObject를 통해 private [SerializeField] 필드에 접근
            SerializedObject serializedInvManager = new SerializedObject(invManager);
            SerializedProperty dbObjProp = serializedInvManager.FindProperty("characterDatabaseObject");

            if (dbObjProp != null && dbObjProp.objectReferenceValue != null)
            {
                // InventoryManager에 연결된 ScriptableObject를 복사
                CreateAssetFromInventoryDB((CharacterDatabaseObject)dbObjProp.objectReferenceValue);
                return;
            }
        }

        // 3) 둘 다 없으면 -> 빈 Database 생성
        Debug.LogWarning("씬에 CharacterDatabase도 없고, InventoryManager의 characterDatabaseObject도 null이므로 빈 Database를 생성합니다.");

        var newAsset = ScriptableObject.CreateInstance<CharacterDatabaseObject>();
        newAsset.name = "NewCharacterDatabase_Empty";
        newAsset.characters = new CharacterData[0];
        SaveNewAsset(newAsset, "빈 Database (자동 생성)");
    }

    /// <summary>
    /// 씬의 CharacterDatabase(MonoBehaviour).characters[] 복사
    /// </summary>
    private static void CreateAssetFromSceneDB(CharacterDatabase sceneDb)
    {
        var newAsset = ScriptableObject.CreateInstance<CharacterDatabaseObject>();
        newAsset.name = "NewCharacterDatabase_Auto";

        if (sceneDb.currentRegisteredCharacters != null && sceneDb.currentRegisteredCharacters.Length > 0)
        {
            // 깊은 복사 수행
            newAsset.characters = new CharacterData[sceneDb.currentRegisteredCharacters.Length];
            for (int i = 0; i < sceneDb.currentRegisteredCharacters.Length; i++)
            {
                if (sceneDb.currentRegisteredCharacters[i] != null)
                {
                    // 새 CharacterData 인스턴스 생성하여 값만 복사
                    newAsset.characters[i] = new CharacterData();
                    newAsset.characters[i].characterName = sceneDb.currentRegisteredCharacters[i].characterName;
                    newAsset.characters[i].attackPower = sceneDb.currentRegisteredCharacters[i].attackPower;
                    newAsset.characters[i].rangeType = sceneDb.currentRegisteredCharacters[i].rangeType;
                    newAsset.characters[i].isAreaAttack = sceneDb.currentRegisteredCharacters[i].isAreaAttack;
                    newAsset.characters[i].isBuffSupport = sceneDb.currentRegisteredCharacters[i].isBuffSupport;
                    newAsset.characters[i].cost = sceneDb.currentRegisteredCharacters[i].cost;
                    newAsset.characters[i].level = sceneDb.currentRegisteredCharacters[i].level;

                    // 다른 필드들도 필요에 따라 복사
                    // newAsset.characters[i].otherField = sceneDb.currentRegisteredCharacters[i].otherField;
                }
            }
        }
        else
        {
            newAsset.characters = new CharacterData[0];
        }

        SaveNewAsset(newAsset, "씬의 CharacterDatabase");
    }

    /// <summary>
    /// 씬의 CharacterInventoryManager가 가진 ScriptableObject에서 characters[] 복사
    /// </summary>
    private static void CreateAssetFromInventoryDB(CharacterDatabaseObject dbObj)
    {
        var newAsset = ScriptableObject.CreateInstance<CharacterDatabaseObject>();
        newAsset.name = "NewCharacterDatabase_FromInventory";

        if (dbObj != null && dbObj.characters != null && dbObj.characters.Length > 0)
        {
            // 참조 복사가 아닌 깊은 복사로 변경
            newAsset.characters = new CharacterData[dbObj.characters.Length];
            for (int i = 0; i < dbObj.characters.Length; i++)
            {
                if (dbObj.characters[i] != null)
                {
                    // 새 CharacterData 인스턴스 생성하여 값만 복사
                    newAsset.characters[i] = new CharacterData();
                    newAsset.characters[i].characterName = dbObj.characters[i].characterName;
                    newAsset.characters[i].attackPower = dbObj.characters[i].attackPower;
                    newAsset.characters[i].rangeType = dbObj.characters[i].rangeType;
                    newAsset.characters[i].isAreaAttack = dbObj.characters[i].isAreaAttack;
                    newAsset.characters[i].isBuffSupport = dbObj.characters[i].isBuffSupport;
                    newAsset.characters[i].cost = dbObj.characters[i].cost;
                    newAsset.characters[i].level = dbObj.characters[i].level;

                    // 다른 필드들도 필요에 따라 복사
                    // newAsset.characters[i].otherField = dbObj.characters[i].otherField;
                }
            }
        }
        else
        {
            newAsset.characters = new CharacterData[0];
        }

        SaveNewAsset(newAsset, "InventoryManager.characterDatabaseObject");
    }

    /// <summary>
    /// 실제 .asset 파일로 저장
    /// </summary>
    private static void SaveNewAsset(CharacterDatabaseObject newAsset, string sourceName)
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "새 CharacterDatabaseObject 생성",
            newAsset.name,
            "asset",
            $"새 ScriptableObject를 저장할 위치를 선택하세요.\n(출처: {sourceName})"
        );
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("생성이 취소되었습니다.");
            return;
        }

        AssetDatabase.CreateAsset(newAsset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newAsset;

        Debug.Log($"{sourceName}에서 {newAsset.characters.Length}개 캐릭터 정보를 복사해 생성 완료: {path}");
    }
}
#endif
