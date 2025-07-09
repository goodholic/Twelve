#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Data;
using TileConquest.Data;
using TMPro;

namespace TileConquest.Editor
{
    /// <summary>
    /// CSV 데이터를 ScriptableObject로 변환하는 에디터 도구
    /// </summary>
    public class TileConquestCSVConverter : EditorWindow
    {
        private string csvFolderPath = "Assets/CSV/TileConquest";
        private string soFolderPath = "Assets/Data/ScriptableObjects/TileConquest";
        
        private bool createCharacterData = true;
        private bool createAttackRangeData = true;
        private bool createStoryData = true;
        private bool createDatabase = true;
        
        [MenuItem("TileConquest/CSV to ScriptableObject Converter")]
        public static void ShowWindow()
        {
            GetWindow<TileConquestCSVConverter>("TileConquest CSV Converter");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("TileConquest CSV to ScriptableObject Converter", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            // 폴더 경로 설정
            EditorGUILayout.LabelField("Folder Paths", EditorStyles.boldLabel);
            csvFolderPath = EditorGUILayout.TextField("CSV Folder", csvFolderPath);
            soFolderPath = EditorGUILayout.TextField("SO Output Folder", soFolderPath);
            
            EditorGUILayout.Space();
            
            // 변환 옵션
            EditorGUILayout.LabelField("Conversion Options", EditorStyles.boldLabel);
            createCharacterData = EditorGUILayout.Toggle("Create Character Data", createCharacterData);
            createAttackRangeData = EditorGUILayout.Toggle("Create Attack Range Data", createAttackRangeData);
            createStoryData = EditorGUILayout.Toggle("Create Story Data", createStoryData);
            createDatabase = EditorGUILayout.Toggle("Create Database", createDatabase);
            
            EditorGUILayout.Space();
            
            // 버튼들
            if (GUILayout.Button("Convert All CSV Files", GUILayout.Height(30)))
            {
                ConvertAllCSVFiles();
            }
            
            EditorGUILayout.Space();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Convert Characters"))
            {
                ConvertCharacterCSV();
            }
            if (GUILayout.Button("Convert Attack Ranges"))
            {
                ConvertAttackRangeCSV();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Convert Stories"))
            {
                ConvertStoryCSV();
            }
            if (GUILayout.Button("Create Sample CSV"))
            {
                CreateSampleCSVFiles();
            }
            GUILayout.EndHorizontal();
        }
        
        private void ConvertAllCSVFiles()
        {
            // 폴더 생성
            if (!Directory.Exists(soFolderPath))
            {
                Directory.CreateDirectory(soFolderPath);
            }
            
            int converted = 0;
            
            if (createCharacterData)
            {
                converted += ConvertCharacterCSV();
            }
            
            if (createAttackRangeData)
            {
                converted += ConvertAttackRangeCSV();
            }
            
            if (createStoryData)
            {
                converted += ConvertStoryCSV();
            }
            
            if (createDatabase)
            {
                CreateGameDatabase();
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Conversion Complete", 
                $"Successfully converted {converted} CSV files to ScriptableObjects!", "OK");
        }
        
        private int ConvertCharacterCSV()
        {
            string csvPath = Path.Combine(csvFolderPath, "character_tile_data.csv");
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"Character CSV file not found at: {csvPath}");
                return 0;
            }
            
            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length <= 1) return 0;
            
            int created = 0;
            
            // 헤더 스킵하고 데이터 파싱
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = ParseCSVLine(lines[i]);
                if (values.Length < 8) continue;
                
                // CharacterTileDataSO 생성
                string assetPath = Path.Combine(soFolderPath, $"Character_{values[0]}.asset");
                CharacterTileDataSO character = CreateOrLoadAsset<CharacterTileDataSO>(assetPath);
                
                // 기본 캐릭터 데이터 참조 (기존 CharacterDataSO 찾기)
                string baseCharPath = $"Assets/Data/ScriptableObjects/Characters/{values[1]}.asset";
                character.baseCharacterData = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(baseCharPath);
                
                // 공격 범위 참조
                string rangePath = Path.Combine(soFolderPath, $"Range_{values[2]}.asset");
                character.attackRange = AssetDatabase.LoadAssetAtPath<AttackRangeSO>(rangePath);
                
                // 추가 속성 설정
                character.placementPriority = int.Parse(values[3]);
                character.attackMultiplier = float.Parse(values[4]);
                character.hasJobSynergy = bool.Parse(values[5]);
                character.synergyBonus = float.Parse(values[6]);
                character.aiPreferredPosition = (PreferredPosition)System.Enum.Parse(typeof(PreferredPosition), values[7]);
                
                EditorUtility.SetDirty(character);
                created++;
            }
            
            Debug.Log($"Created {created} CharacterTileData assets");
            return created;
        }
        
        private int ConvertAttackRangeCSV()
        {
            string csvPath = Path.Combine(csvFolderPath, "attack_ranges.csv");
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"Attack Range CSV file not found at: {csvPath}");
                return 0;
            }
            
            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length <= 1) return 0;
            
            int created = 0;
            
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = ParseCSVLine(lines[i]);
                if (values.Length < 5) continue;
                
                // AttackRangeSO 생성
                string assetPath = Path.Combine(soFolderPath, $"Range_{values[0]}.asset");
                AttackRangeSO range = CreateOrLoadAsset<AttackRangeSO>(assetPath);
                
                range.rangeId = values[0];
                range.rangeName = values[1];
                range.rangeType = (RangeType)System.Enum.Parse(typeof(RangeType), values[2]);
                range.rangeSize = int.Parse(values[3]);
                range.description = values[4];
                
                // 커스텀 패턴이 있다면 파싱
                if (values.Length > 5 && range.rangeType == RangeType.Custom)
                {
                    ParseCustomPattern(range, values[5]);
                }
                
                EditorUtility.SetDirty(range);
                created++;
            }
            
            Debug.Log($"Created {created} AttackRange assets");
            return created;
        }
        
        private int ConvertStoryCSV()
        {
            string csvPath = Path.Combine(csvFolderPath, "story_dialogue.csv");
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"Story CSV file not found at: {csvPath}");
                return 0;
            }
            
            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length <= 1) return 0;
            
            // 스토리별로 그룹화
            Dictionary<string, List<string[]>> storyGroups = new Dictionary<string, List<string[]>>();
            
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = ParseCSVLine(lines[i]);
                if (values.Length < 8) continue;
                
                string storyId = values[0];
                if (!storyGroups.ContainsKey(storyId))
                {
                    storyGroups[storyId] = new List<string[]>();
                }
                storyGroups[storyId].Add(values);
            }
            
            int created = 0;
            
            // 각 스토리별로 ScriptableObject 생성
            foreach (var kvp in storyGroups)
            {
                string assetPath = Path.Combine(soFolderPath, $"Story_{kvp.Key}.asset");
                StoryDialogueDataSO story = CreateOrLoadAsset<StoryDialogueDataSO>(assetPath);
                
                story.storyId = kvp.Key;
                story.dialogueNodes.Clear();
                
                // 대화 노드 생성
                foreach (var values in kvp.Value)
                {
                    DialogueNode node = new DialogueNode
                    {
                        nodeId = values[1],
                        speakerName = values[2],
                        dialogueText = values[3],
                        position = (CharacterPosition)System.Enum.Parse(typeof(CharacterPosition), values[4]),
                        expression = (CharacterExpression)System.Enum.Parse(typeof(CharacterExpression), values[5]),
                        nextNodeId = values[6],
                        isStartNode = bool.Parse(values[7])
                    };
                    
                    // 추가 데이터가 있다면 파싱
                    if (values.Length > 8)
                    {
                        // 효과, 배경, BGM 등 추가 파싱
                    }
                    
                    story.dialogueNodes.Add(node);
                }
                
                EditorUtility.SetDirty(story);
                created++;
            }
            
            Debug.Log($"Created {created} Story assets");
            return created;
        }
        
        private void CreateGameDatabase()
        {
            string dbPath = Path.Combine(soFolderPath, "TileConquestDatabase.asset");
            TileConquestDatabaseSO database = CreateOrLoadAsset<TileConquestDatabaseSO>(dbPath);
            
            // 생성된 모든 에셋 수집
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { soFolderPath });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                
                // 타입별로 분류하여 데이터베이스에 추가
                // (리플렉션을 사용하여 private 필드에 접근하거나, public 메서드 사용)
            }
            
            database.Initialize();
            EditorUtility.SetDirty(database);
            
            Debug.Log("Game database created and initialized");
        }
        
        private void CreateSampleCSVFiles()
        {
            if (!Directory.Exists(csvFolderPath))
            {
                Directory.CreateDirectory(csvFolderPath);
            }
            
            // 캐릭터 데이터 샘플
            string characterCSV = @"ID,BaseCharacterRef,AttackRangeRef,Priority,AttackMultiplier,HasSynergy,SynergyBonus,PreferredPosition
char_warrior_01,CHR001,range_cross_1,80,1.2,true,0.15,Front
char_archer_01,CHR005,range_line_3,60,1.0,true,0.1,Back
char_wizard_01,CHR003,range_circle_2,70,1.5,false,0,Center";
            
            File.WriteAllText(Path.Combine(csvFolderPath, "character_tile_data.csv"), characterCSV);
            
            // 공격 범위 샘플
            string rangeCSV = @"ID,Name,Type,Size,Description,Pattern
range_cross_1,십자 소형,Cross,1,인접한 십자 방향 공격,
range_circle_2,원형 중형,Circle,2,반경 2 칸 원형 공격,
range_line_3,직선 대형,Line,3,직선 3칸 공격,";
            
            File.WriteAllText(Path.Combine(csvFolderPath, "attack_ranges.csv"), rangeCSV);
            
            // 스토리 샘플
            string storyCSV = @"StoryID,NodeID,Speaker,Text,Position,Expression,NextNode,IsStart
tutorial,node_01,지휘관,전략적 타일 배치의 세계에 오신 것을 환영합니다!,Center,Normal,node_02,true
tutorial,node_02,지휘관,각 캐릭터는 고유한 공격 범위를 가지고 있습니다.,Center,Happy,node_03,false
tutorial,node_03,지휘관,두 개의 타일을 모두 점령하여 승리하세요!,Center,Normal,,false";
            
            File.WriteAllText(Path.Combine(csvFolderPath, "story_dialogue.csv"), storyCSV);
            
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Sample CSV Created", 
                $"Sample CSV files created in {csvFolderPath}", "OK");
        }
        
        private T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }
        
        private string[] ParseCSVLine(string csvLine)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string currentField = "";
            
            for (int i = 0; i < csvLine.Length; i++)
            {
                char c = csvLine[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField);
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }
            
            result.Add(currentField); // 마지막 필드 추가
            return result.ToArray();
        }
        
        private void ParseCustomPattern(AttackRangeSO range, string patternString)
        {
            // 패턴 문자열 파싱 (예: "010,111,010" -> 3x3 패턴)
            string[] rows = patternString.Split(';');
            int height = rows.Length;
            int width = rows[0].Length;
            
            range.rangePattern = new bool[width, height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    range.rangePattern[x, y] = rows[y][x] == '1';
                }
            }
        }
    }
}
#endif