using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using pjy.Data;

namespace pjy.Data.Editor
{
    /// <summary>
    /// 캐릭터 CSV 데이터베이스 에디터
    /// - CSV ↔ ScriptableObject 양방향 변환
    /// - 캐릭터 데이터 관리 GUI
    /// - 파일 시스템과 연동
    /// </summary>
    public class CharacterCSVEditor : EditorWindow
    {
        private CharacterCSVDatabase database;
        private CharacterSkillDatabase skillDatabase;
        private Vector2 scrollPosition;
        private string csvFilePath = "";
        private string exportPath = "Assets/Prefabs/Data/Characters/";
        
        [MenuItem("Tools/pjy/Character CSV Editor")]
        public static void ShowWindow()
        {
            CharacterCSVEditor window = GetWindow<CharacterCSVEditor>("Character CSV Editor");
            window.minSize = new Vector2(1000, 700);
            window.Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Character CSV Database Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            DrawDatabaseSelection();
            EditorGUILayout.Space();
            
            if (database != null)
            {
                DrawFileOperations();
                EditorGUILayout.Space();
                
                DrawCharacterList();
                EditorGUILayout.Space();
                
                DrawUtilityButtons();
            }
        }
        
        /// <summary>
        /// 데이터베이스 선택 GUI
        /// </summary>
        private void DrawDatabaseSelection()
        {
            EditorGUILayout.LabelField("데이터베이스 설정", EditorStyles.boldLabel);
            
            database = (CharacterCSVDatabase)EditorGUILayout.ObjectField(
                "캐릭터 CSV DB", database, typeof(CharacterCSVDatabase), false);
                
            skillDatabase = (CharacterSkillDatabase)EditorGUILayout.ObjectField(
                "스킬 DB", skillDatabase, typeof(CharacterSkillDatabase), false);
            
            if (database == null)
            {
                EditorGUILayout.HelpBox("CharacterCSVDatabase를 선택하거나 생성하세요.", MessageType.Info);
                
                if (GUILayout.Button("새 캐릭터 CSV DB 생성"))
                {
                    CreateNewDatabase();
                }
            }
            
            if (database != null && skillDatabase != null)
            {
                if (GUILayout.Button("스킬 데이터베이스 연결"))
                {
                    database.SetSkillDatabase(skillDatabase);
                    EditorUtility.SetDirty(database);
                }
            }
        }
        
        /// <summary>
        /// 파일 조작 GUI
        /// </summary>
        private void DrawFileOperations()
        {
            EditorGUILayout.LabelField("파일 작업", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            csvFilePath = EditorGUILayout.TextField("CSV 파일 경로", csvFilePath);
            if (GUILayout.Button("찾기", GUILayout.Width(50)))
            {
                string selectedPath = EditorUtility.OpenFilePanel("CSV 파일 선택", Application.dataPath, "csv");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    csvFilePath = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("CSV 파일에서 로드"))
            {
                LoadFromCSVFile();
            }
            
            if (GUILayout.Button("CSV 파일로 저장"))
            {
                SaveToCSVFile();
            }
            
            if (GUILayout.Button("Unity 내부 CSV 로드"))
            {
                database.LoadFromCSV();
            }
            
            if (GUILayout.Button("Unity 내부 CSV 내보내기"))
            {
                database.ExportToCSV();
                EditorUtility.SetDirty(database);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 캐릭터 리스트 GUI
        /// </summary>
        private void DrawCharacterList()
        {
            EditorGUILayout.LabelField("캐릭터 목록", EditorStyles.boldLabel);
            
            List<CharacterCSVEntry> characters = database.GetAllCharacters();
            
            if (characters.Count == 0)
            {
                EditorGUILayout.HelpBox("등록된 캐릭터가 없습니다. CSV를 로드하거나 샘플 데이터를 생성하세요.", MessageType.Info);
                return;
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            for (int i = 0; i < characters.Count; i++)
            {
                DrawCharacterEntry(characters[i], i);
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 개별 캐릭터 엔트리 GUI
        /// </summary>
        private void DrawCharacterEntry(CharacterCSVEntry character, int index)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            character.characterName = EditorGUILayout.TextField("이름", character.characterName);
            
            if (GUILayout.Button("삭제", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("캐릭터 삭제", 
                    $"{character.characterName}을(를) 삭제하시겠습니까?", "삭제", "취소"))
                {
                    database.RemoveCharacter(character.id);
                    EditorUtility.SetDirty(database);
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 기본 정보
            EditorGUILayout.BeginHorizontal();
            character.id = EditorGUILayout.IntField("ID", character.id);
            character.race = (CharacterRace)EditorGUILayout.EnumPopup("종족", character.race);
            character.star = (CharacterStar)EditorGUILayout.EnumPopup("별급", character.star);
            character.level = EditorGUILayout.IntField("레벨", character.level);
            EditorGUILayout.EndHorizontal();
            
            // 전투 스탯
            EditorGUILayout.LabelField("전투 스탯", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            character.attackPower = EditorGUILayout.FloatField("공격력", character.attackPower);
            character.attackRange = EditorGUILayout.FloatField("사거리", character.attackRange);
            character.attackSpeed = EditorGUILayout.FloatField("공격속도", character.attackSpeed);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            character.maxHP = EditorGUILayout.FloatField("최대 HP", character.maxHP);
            character.moveSpeed = EditorGUILayout.FloatField("이동속도", character.moveSpeed);
            character.cost = EditorGUILayout.IntField("비용", character.cost);
            EditorGUILayout.EndHorizontal();
            
            // 공격 설정
            EditorGUILayout.LabelField("공격 설정", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            character.rangeType = (RangeType)EditorGUILayout.EnumPopup("공격 타입", character.rangeType);
            character.attackTargetType = (AttackTargetType)EditorGUILayout.EnumPopup("공격 대상", character.attackTargetType);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            character.attackShapeType = (AttackShapeType)EditorGUILayout.EnumPopup("공격 형태", character.attackShapeType);
            character.isAreaAttack = EditorGUILayout.Toggle("범위 공격", character.isAreaAttack);
            if (character.isAreaAttack)
            {
                character.areaAttackRadius = EditorGUILayout.FloatField("범위", character.areaAttackRadius);
            }
            EditorGUILayout.EndHorizontal();
            
            // 스킬 설정
            EditorGUILayout.LabelField("스킬 & 행동", EditorStyles.boldLabel);
            DrawStringArray("스킬 ID", ref character.skillIds);
            DrawStringArray("행동 컴포넌트", ref character.behaviorComponents);
            
            // 리소스 경로
            EditorGUILayout.LabelField("리소스", EditorStyles.boldLabel);
            character.spriteResourcePath = EditorGUILayout.TextField("스프라이트 경로", character.spriteResourcePath);
            character.prefabResourcePath = EditorGUILayout.TextField("프리팹 경로", character.prefabResourcePath);
            
            // 설명
            character.description = EditorGUILayout.TextArea(character.description, GUILayout.Height(40));
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 문자열 배열 편집 GUI
        /// </summary>
        private void DrawStringArray(string label, ref string[] array)
        {
            EditorGUILayout.LabelField(label);
            EditorGUI.indentLevel++;
            
            if (array == null) array = new string[0];
            
            int newSize = EditorGUILayout.IntField("개수", array.Length);
            if (newSize != array.Length)
            {
                System.Array.Resize(ref array, newSize);
            }
            
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = EditorGUILayout.TextField($"[{i}]", array[i]);
            }
            
            EditorGUI.indentLevel--;
        }
        
        /// <summary>
        /// 유틸리티 버튼들
        /// </summary>
        private void DrawUtilityButtons()
        {
            EditorGUILayout.LabelField("유틸리티", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("샘플 데이터 생성"))
            {
                if (EditorUtility.DisplayDialog("샘플 데이터 생성", 
                    "기존 데이터를 모두 삭제하고 샘플 데이터를 생성하시겠습니까?", "생성", "취소"))
                {
                    database.GenerateSampleData();
                    EditorUtility.SetDirty(database);
                }
            }
            
            if (GUILayout.Button("ScriptableObject 생성"))
            {
                GenerateCharacterDataAssets();
            }
            
            if (GUILayout.Button("새 캐릭터 추가"))
            {
                AddNewCharacter();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            exportPath = EditorGUILayout.TextField("내보내기 경로", exportPath);
            if (GUILayout.Button("폴더 선택", GUILayout.Width(80)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("내보내기 폴더 선택", exportPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    exportPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 새 데이터베이스 생성
        /// </summary>
        private void CreateNewDatabase()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "새 캐릭터 CSV DB 생성", "CharacterCSVDatabase", "asset", "데이터베이스를 저장할 위치를 선택하세요.");
                
            if (!string.IsNullOrEmpty(path))
            {
                CharacterCSVDatabase newDB = CreateInstance<CharacterCSVDatabase>();
                AssetDatabase.CreateAsset(newDB, path);
                AssetDatabase.SaveAssets();
                database = newDB;
            }
        }
        
        /// <summary>
        /// CSV 파일에서 로드
        /// </summary>
        private void LoadFromCSVFile()
        {
            if (string.IsNullOrEmpty(csvFilePath) || !File.Exists(csvFilePath))
            {
                EditorUtility.DisplayDialog("오류", "유효한 CSV 파일 경로를 지정하세요.", "확인");
                return;
            }
            
            try
            {
                string csvContent = File.ReadAllText(csvFilePath);
                // Unity 내부 CSV 데이터 필드에 직접 설정 (리플렉션 사용)
                System.Reflection.FieldInfo csvDataField = typeof(CharacterCSVDatabase).GetField("csvData", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                csvDataField.SetValue(database, csvContent);
                
                database.LoadFromCSV();
                EditorUtility.SetDirty(database);
                
                EditorUtility.DisplayDialog("성공", "CSV 파일을 성공적으로 로드했습니다.", "확인");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("오류", $"CSV 파일 로드 중 오류 발생:\n{e.Message}", "확인");
            }
        }
        
        /// <summary>
        /// CSV 파일로 저장
        /// </summary>
        private void SaveToCSVFile()
        {
            if (string.IsNullOrEmpty(csvFilePath))
            {
                csvFilePath = EditorUtility.SaveFilePanel("CSV 파일 저장", Application.dataPath, "CharacterData", "csv");
                if (string.IsNullOrEmpty(csvFilePath)) return;
            }
            
            try
            {
                database.ExportToCSV();
                
                // Unity 내부 CSV 데이터 가져오기 (리플렉션 사용)
                System.Reflection.FieldInfo csvDataField = typeof(CharacterCSVDatabase).GetField("csvData", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                string csvContent = (string)csvDataField.GetValue(database);
                
                File.WriteAllText(csvFilePath, csvContent);
                EditorUtility.DisplayDialog("성공", "CSV 파일을 성공적으로 저장했습니다.", "확인");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("오류", $"CSV 파일 저장 중 오류 발생:\n{e.Message}", "확인");
            }
        }
        
        /// <summary>
        /// CharacterData ScriptableObject 에셋 생성
        /// </summary>
        private void GenerateCharacterDataAssets()
        {
            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }
            
            List<CharacterData> characterDataList = database.GenerateCharacterData();
            
            foreach (CharacterData data in characterDataList)
            {
                string assetPath = $"{exportPath}/{data.characterName}.asset";
                AssetDatabase.CreateAsset(data, assetPath);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("성공", 
                $"{characterDataList.Count}개의 CharacterData 에셋을 생성했습니다.\n경로: {exportPath}", "확인");
        }
        
        /// <summary>
        /// 새 캐릭터 추가
        /// </summary>
        private void AddNewCharacter()
        {
            CharacterCSVEntry newCharacter = new CharacterCSVEntry();
            newCharacter.id = GetNextCharacterId();
            newCharacter.characterName = "새 캐릭터";
            newCharacter.race = CharacterRace.Human;
            newCharacter.star = CharacterStar.OneStar;
            newCharacter.level = 1;
            newCharacter.attackPower = 50f;
            newCharacter.attackRange = 3f;
            newCharacter.attackSpeed = 1.5f;
            newCharacter.maxHP = 100f;
            newCharacter.moveSpeed = 2f;
            newCharacter.cost = 10;
            newCharacter.rangeType = RangeType.Melee;
            newCharacter.attackTargetType = AttackTargetType.Monster;
            newCharacter.attackShapeType = AttackShapeType.Single;
            newCharacter.skillIds = new string[] { "basic_attack" };
            newCharacter.behaviorComponents = new string[] { "MeleeAttackBehavior" };
            newCharacter.description = "새로 추가된 캐릭터입니다.";
            
            database.AddCharacter(newCharacter);
            EditorUtility.SetDirty(database);
        }
        
        /// <summary>
        /// 다음 캐릭터 ID 가져오기
        /// </summary>
        private int GetNextCharacterId()
        {
            List<CharacterCSVEntry> characters = database.GetAllCharacters();
            int maxId = 0;
            
            foreach (var character in characters)
            {
                if (character.id > maxId)
                    maxId = character.id;
            }
            
            return maxId + 1;
        }
    }
}