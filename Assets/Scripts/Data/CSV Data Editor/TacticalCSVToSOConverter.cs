using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using TacticalTileGame.Data;

namespace TacticalTileGame.Editor
{
    /// <summary>
    /// CSV 파일을 ScriptableObject로 변환하는 에디터 도구
    /// </summary>
    public class TacticalCSVToSOConverter : EditorWindow
    {
        private string csvFolderPath = "Assets/CSV/";
        private string soOutputPath = "Assets/ScriptableObjects/";
        
        private bool convertCharacters = true;
        private bool convertSkills = true;
        private bool convertDialogues = true;
        
        [MenuItem("TacticalTileGame/CSV Data Converter")]
        public static void ShowWindow()
        {
            GetWindow<TacticalCSVToSOConverter>("CSV to SO Converter");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("CSV to ScriptableObject Converter", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            // 경로 설정
            GUILayout.Label("Paths", EditorStyles.boldLabel);
            csvFolderPath = EditorGUILayout.TextField("CSV Folder Path", csvFolderPath);
            soOutputPath = EditorGUILayout.TextField("SO Output Path", soOutputPath);
            GUILayout.Space(10);
            
            // 변환 옵션
            GUILayout.Label("Conversion Options", EditorStyles.boldLabel);
            convertCharacters = EditorGUILayout.Toggle("Convert Characters", convertCharacters);
            convertSkills = EditorGUILayout.Toggle("Convert Skills", convertSkills);
            convertDialogues = EditorGUILayout.Toggle("Convert Dialogues", convertDialogues);
            GUILayout.Space(20);
            
            // 변환 버튼
            if (GUILayout.Button("Convert CSV to ScriptableObjects", GUILayout.Height(30)))
            {
                ConvertCSVFiles();
            }
            
            GUILayout.Space(10);
            
            // 개별 생성 버튼
            GUILayout.Label("Individual Creation", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Sample Character CSV"))
            {
                CreateSampleCharacterCSV();
            }
            if (GUILayout.Button("Create Sample Skill CSV"))
            {
                CreateSampleSkillCSV();
            }
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("Create Sample Dialogue CSV"))
            {
                CreateSampleDialogueCSV();
            }
        }
        
        private void ConvertCSVFiles()
        {
            // 출력 폴더 생성
            if (!Directory.Exists(soOutputPath))
            {
                Directory.CreateDirectory(soOutputPath);
            }
            
            // 각 타입별 변환
            if (convertCharacters)
            {
                ConvertCharacterCSV();
            }
            
            if (convertSkills)
            {
                ConvertSkillCSV();
            }
            
            if (convertDialogues)
            {
                ConvertDialogueCSV();
            }
            
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "CSV files have been converted to ScriptableObjects!", "OK");
        }
        
        private void ConvertCharacterCSV()
        {
            string csvPath = Path.Combine(csvFolderPath, "characters.csv");
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"Character CSV file not found: {csvPath}");
                return;
            }
            
            string outputFolder = Path.Combine(soOutputPath, "Characters");
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
            
            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length < 2) return; // 헤더 + 최소 1개 데이터
            
            string[] headers = ParseCSVLine(lines[0]);
            
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                string[] values = ParseCSVLine(lines[i]);
                if (values.Length != headers.Length) continue;
                
                // CSV 데이터를 Dictionary로 변환
                Dictionary<string, string> csvData = new Dictionary<string, string>();
                for (int j = 0; j < headers.Length; j++)
                {
                    csvData[headers[j]] = values[j];
                }
                
                // ScriptableObject 생성
                TacticalCharacterDataSO characterData = ScriptableObject.CreateInstance<TacticalCharacterDataSO>();
                characterData.InitializeFromCSV(csvData);
                
                // 파일로 저장
                string assetPath = Path.Combine(outputFolder, $"{characterData.characterId}.asset");
                AssetDatabase.CreateAsset(characterData, assetPath);
            }
            
            Debug.Log($"Converted {lines.Length - 1} character(s) to ScriptableObjects");
        }
        
        private void ConvertSkillCSV()
        {
            string csvPath = Path.Combine(csvFolderPath, "skills.csv");
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"Skill CSV file not found: {csvPath}");
                return;
            }
            
            string outputFolder = Path.Combine(soOutputPath, "Skills");
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
            
            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length < 2) return;
            
            string[] headers = ParseCSVLine(lines[0]);
            
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                string[] values = ParseCSVLine(lines[i]);
                if (values.Length != headers.Length) continue;
                
                Dictionary<string, string> csvData = new Dictionary<string, string>();
                for (int j = 0; j < headers.Length; j++)
                {
                    csvData[headers[j]] = values[j];
                }
                
                TacticalSkillDataSO skillData = ScriptableObject.CreateInstance<TacticalSkillDataSO>();
                skillData.InitializeFromCSV(csvData);
                
                string assetPath = Path.Combine(outputFolder, $"{skillData.skillId}.asset");
                AssetDatabase.CreateAsset(skillData, assetPath);
            }
            
            Debug.Log($"Converted {lines.Length - 1} skill(s) to ScriptableObjects");
        }
        
        private void ConvertDialogueCSV()
        {
            string csvPath = Path.Combine(csvFolderPath, "dialogues.csv");
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"Dialogue CSV file not found: {csvPath}");
                return;
            }
            
            string outputFolder = Path.Combine(soOutputPath, "Dialogues");
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
            
            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length < 2) return;
            
            string[] headers = ParseCSVLine(lines[0]);
            
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                string[] values = ParseCSVLine(lines[i]);
                if (values.Length != headers.Length) continue;
                
                Dictionary<string, string> csvData = new Dictionary<string, string>();
                for (int j = 0; j < headers.Length; j++)
                {
                    csvData[headers[j]] = values[j];
                }
                
                StoryDialogueDataSO dialogueData = ScriptableObject.CreateInstance<StoryDialogueDataSO>();
                dialogueData.InitializeFromCSV(csvData);
                
                string assetPath = Path.Combine(outputFolder, $"{dialogueData.dialogueId}.asset");
                AssetDatabase.CreateAsset(dialogueData, assetPath);
            }
            
            Debug.Log($"Converted {lines.Length - 1} dialogue(s) to ScriptableObjects");
        }
        
        /// <summary>
        /// CSV 라인 파싱 (쉼표와 따옴표 처리)
        /// </summary>
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
        
        /// <summary>
        /// 샘플 캐릭터 CSV 생성
        /// </summary>
        private void CreateSampleCharacterCSV()
        {
            if (!Directory.Exists(csvFolderPath))
            {
                Directory.CreateDirectory(csvFolderPath);
            }
            
            string csvPath = Path.Combine(csvFolderPath, "characters.csv");
            
            List<string> lines = new List<string>
            {
                "id,name,description,class,rarity,hp,attack,defense,magic,speed,critRate,attackPattern,skills",
                "char_001,검투사,\"근접 전투의 달인\",Warrior,Common,120,15,10,0,5,0.15,\"0,1;0,-1;1,0;-1,0\",\"skill_001,skill_002\"",
                "char_002,대마법사,\"원소 마법의 대가\",Wizard,Rare,80,5,5,20,3,0.1,\"0,1;0,2;0,-1;0,-2;1,0;2,0;-1,0;-2,0\",\"skill_003,skill_004\"",
                "char_003,성기사,\"신성한 수호자\",Knight,Epic,150,12,20,5,4,0.05,\"1,1;1,-1;-1,1;-1,-1\",\"skill_005,skill_006\"",
                "char_004,암살자,\"그림자의 춤꾼\",Rogue,Rare,90,18,8,0,8,0.25,\"2,1;2,-1;-2,1;-2,-1;1,2;1,-2;-1,2;-1,-2\",\"skill_007,skill_008\""
            };
            
            File.WriteAllLines(csvPath, lines);
            AssetDatabase.Refresh();
            
            Debug.Log($"Sample character CSV created at: {csvPath}");
        }
        
        /// <summary>
        /// 샘플 스킬 CSV 생성
        /// </summary>
        private void CreateSampleSkillCSV()
        {
            if (!Directory.Exists(csvFolderPath))
            {
                Directory.CreateDirectory(csvFolderPath);
            }
            
            string csvPath = Path.Combine(csvFolderPath, "skills.csv");
            
            List<string> lines = new List<string>
            {
                "id,name,description,type,targetType,damage,healing,manaCost,cooldown,skillPattern,areaEffect,requiredLevel,requiredClass,animation,sound",
                "skill_001,회전베기,\"주변 적들을 베어낸다\",Damage,Enemy,30,0,10,3,\"1,0;-1,0;0,1;0,-1;1,1;1,-1;-1,1;-1,-1\",true,1,Warrior,spin_attack,sword_swing",
                "skill_002,방패강타,\"전방의 적을 기절시킨다\",Damage,Enemy,20,0,5,5,\"0,1;0,2\",false,5,Warrior,shield_bash,shield_hit",
                "skill_003,화염구,\"불타는 구체를 발사한다\",Damage,Enemy,40,0,15,4,\"0,1;0,2;0,3\",false,1,Wizard,fireball,fire_cast",
                "skill_004,얼음폭풍,\"광범위한 얼음 폭풍을 일으킨다\",Damage,Enemy,25,0,25,8,\"1,1;1,0;1,-1;0,1;0,-1;-1,1;-1,0;-1,-1;2,0;-2,0;0,2;0,-2\",true,10,Wizard,ice_storm,ice_cast"
            };
            
            File.WriteAllLines(csvPath, lines);
            AssetDatabase.Refresh();
            
            Debug.Log($"Sample skill CSV created at: {csvPath}");
        }
        
        /// <summary>
        /// 샘플 대화 CSV 생성
        /// </summary>
        private void CreateSampleDialogueCSV()
        {
            if (!Directory.Exists(csvFolderPath))
            {
                Directory.CreateDirectory(csvFolderPath);
            }
            
            string csvPath = Path.Combine(csvFolderPath, "dialogues.csv");
            
            List<string> lines = new List<string>
            {
                "id,chapter,scene,speaker,dialogue,emotion,nextId,choices,conditions,voice,bgm,sfx",
                "dlg_001,chapter_1,intro,나레이터,\"오래전부터 전해져 내려오는 전설이 있었다...\",Normal,dlg_002,,,narrator_01,bgm_mystical,",
                "dlg_002,chapter_1,intro,주인공,\"드디어 이곳에 도착했군. 전설의 타일 전장이라...\",Determined,dlg_003,,,hero_01,bgm_mystical,footstep",
                "dlg_003,chapter_1,intro,수수께끼의 노인,\"젊은이여, 이곳에 온 목적이 무엇인가?\",Thinking,dlg_004,\"진실을 찾으러 왔습니다:dlg_004a|힘을 얻으러 왔습니다:dlg_004b\",,oldman_01,bgm_mystical,",
                "dlg_004a,chapter_1,intro,수수께끼의 노인,\"진실... 좋은 대답이다. 하지만 그 길은 험난할 것이야.\",Normal,dlg_005,,,oldman_02,bgm_mystical,",
                "dlg_004b,chapter_1,intro,수수께끼의 노인,\"힘... 많은 이들이 그것을 추구했지. 하지만 진정한 힘은 다른 곳에 있다네.\",Sad,dlg_005,,,oldman_03,bgm_mystical,"
            };
            
            File.WriteAllLines(csvPath, lines);
            AssetDatabase.Refresh();
            
            Debug.Log($"Sample dialogue CSV created at: {csvPath}");
        }
    }
}