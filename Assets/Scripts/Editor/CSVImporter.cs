using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using GuildMaster.Data;
using GuildMaster.Guild;

namespace GuildMaster.Editor
{
    /// <summary>
    /// CSV 파일을 ScriptableObject로 변환하는 에디터 도구
    /// </summary>
    public class CSVImporter : EditorWindow
    {
        private string csvFolderPath = "Assets/CSV/";
        private string soOutputPath = "Assets/ScriptableObjects/";
        
        private bool importCharacters = true;
        private bool importDialogues = true;
        private bool importBuildings = true;
        private bool importSkills = true;
        
        private Vector2 scrollPosition;
        
        [MenuItem("GuildMaster/CSV Importer")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(CSVImporter), false, "CSV Importer");
        }
        
        void OnGUI()
        {
            GUILayout.Label("CSV to ScriptableObject Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Paths
            EditorGUILayout.LabelField("Paths", EditorStyles.boldLabel);
            csvFolderPath = EditorGUILayout.TextField("CSV Folder:", csvFolderPath);
            soOutputPath = EditorGUILayout.TextField("Output Folder:", soOutputPath);
            
            EditorGUILayout.Space();
            
            // Import options
            EditorGUILayout.LabelField("Import Options", EditorStyles.boldLabel);
            importCharacters = EditorGUILayout.Toggle("Import Story Characters", importCharacters);
            importDialogues = EditorGUILayout.Toggle("Import Story Dialogues", importDialogues);
            importBuildings = EditorGUILayout.Toggle("Import Building Data", importBuildings);
            importSkills = EditorGUILayout.Toggle("Import Skill Data", importSkills);
            
            EditorGUILayout.Space();
            
            // Import buttons
            if (GUILayout.Button("Import All"))
            {
                ImportAll();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Import Characters Only"))
            {
                ImportCharacters();
            }
            
            if (GUILayout.Button("Import Dialogues Only"))
            {
                ImportDialogues();
            }
            
            if (GUILayout.Button("Import Buildings Only"))
            {
                ImportBuildings();
            }
            
            if (GUILayout.Button("Import Skills Only"))
            {
                ImportSkills();
            }
            
            EditorGUILayout.Space();
            
            // Status
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.LabelField("Import Status", EditorStyles.boldLabel);
            
            if (File.Exists(Path.Combine(csvFolderPath, "story_characters.csv")))
            {
                EditorGUILayout.LabelField("✓ story_characters.csv found");
            }
            else
            {
                EditorGUILayout.LabelField("✗ story_characters.csv not found", EditorStyles.miniLabel);
            }
            
            if (File.Exists(Path.Combine(csvFolderPath, "story_dialogues.csv")))
            {
                EditorGUILayout.LabelField("✓ story_dialogues.csv found");
            }
            else
            {
                EditorGUILayout.LabelField("✗ story_dialogues.csv not found", EditorStyles.miniLabel);
            }
            
            if (File.Exists(Path.Combine(csvFolderPath, "building_data.csv")))
            {
                EditorGUILayout.LabelField("✓ building_data.csv found");
            }
            else
            {
                EditorGUILayout.LabelField("✗ building_data.csv not found", EditorStyles.miniLabel);
            }
            
            if (File.Exists(Path.Combine(csvFolderPath, "skill_data.csv")))
            {
                EditorGUILayout.LabelField("✓ skill_data.csv found");
            }
            else
            {
                EditorGUILayout.LabelField("✗ skill_data.csv not found", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        void ImportAll()
        {
            if (importCharacters) ImportCharacters();
            if (importDialogues) ImportDialogues();
            if (importBuildings) ImportBuildings();
            if (importSkills) ImportSkills();
            
            AssetDatabase.Refresh();
            Debug.Log("All CSV imports completed!");
        }
        
        void ImportCharacters()
        {
            string csvPath = Path.Combine(csvFolderPath, "story_characters.csv");
            
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"CSV file not found: {csvPath}");
                return;
            }
            
            string[] lines = File.ReadAllLines(csvPath);
            
            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var character = ScriptableObject.CreateInstance<StoryCharacterSO>();
                character.InitializeFromCSV(lines[i]);
                
                string outputPath = Path.Combine(soOutputPath, "Characters", $"{character.characterId}.asset");
                
                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                
                AssetDatabase.CreateAsset(character, outputPath);
                Debug.Log($"Created character: {character.characterName} at {outputPath}");
            }
            
            Debug.Log($"Imported {lines.Length - 1} characters from {csvPath}");
        }
        
        void ImportDialogues()
        {
            string csvPath = Path.Combine(csvFolderPath, "story_dialogues.csv");
            
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"CSV file not found: {csvPath}");
                return;
            }
            
            string[] lines = File.ReadAllLines(csvPath);
            
            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var dialogue = ScriptableObject.CreateInstance<StoryDialogueSO>();
                dialogue.InitializeFromCSV(lines[i]);
                
                string chapterFolder = dialogue.chapterId.Replace("chapter_", "Chapter");
                string outputPath = Path.Combine(soOutputPath, "Dialogues", chapterFolder, $"{dialogue.dialogueId}.asset");
                
                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                
                AssetDatabase.CreateAsset(dialogue, outputPath);
                Debug.Log($"Created dialogue: {dialogue.dialogueId} at {outputPath}");
            }
            
            Debug.Log($"Imported {lines.Length - 1} dialogues from {csvPath}");
        }
        
        void ImportBuildings()
        {
            string csvPath = Path.Combine(csvFolderPath, "building_data.csv");
            
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"CSV file not found: {csvPath}");
                return;
            }
            
            string[] lines = File.ReadAllLines(csvPath);
            
            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                // Parse CSV data
                var csvData = BuildingDataCSV.FromCSVLine(lines[i]);
                
                // Create BuildingDataSO and set properties
                var building = ScriptableObject.CreateInstance<BuildingDataSO>();
                building.buildingName = csvData.buildingName;
                building.description = csvData.description;
                building.buildingType = csvData.buildingType;
                building.constructionTime = csvData.constructionTime;
                building.maxWorkers = csvData.maxWorkers;
                building.requiredGuildLevel = csvData.requiredLevel;
                building.size = new Vector2Int(csvData.width, csvData.height);
                
                // Set construction costs
                building.constructionCost[Core.ResourceType.Gold] = csvData.baseCostGold;
                if (csvData.baseCostWood > 0)
                    building.constructionCost[Core.ResourceType.Wood] = csvData.baseCostWood;
                if (csvData.baseCostStone > 0)
                    building.constructionCost[Core.ResourceType.Stone] = csvData.baseCostStone;
                
                // Set production rates if applicable
                if (csvData.canProduce && csvData.productionAmount > 0)
                {
                    // Convert ResourceType to Core.ResourceType
                    if (csvData.productionType == ResourceType.Gold)
                        building.baseProductionRates[Core.ResourceType.Gold] = csvData.productionAmount;
                    else if (csvData.productionType == ResourceType.Wood)
                        building.baseProductionRates[Core.ResourceType.Wood] = csvData.productionAmount;
                    else if (csvData.productionType == ResourceType.Stone)
                        building.baseProductionRates[Core.ResourceType.Stone] = csvData.productionAmount;
                    
                    building.productionInterval = csvData.productionTime;
                }
                
                // Set storage capacity if applicable
                if (csvData.maxStorage > 0)
                {
                    building.baseStorageCapacity[Core.ResourceType.Gold] = csvData.maxStorage;
                }
                
                string outputPath = Path.Combine(soOutputPath, "Buildings", $"{csvData.buildingId}.asset");
                
                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                
                AssetDatabase.CreateAsset(building, outputPath);
                Debug.Log($"Created building: {building.buildingName} at {outputPath}");
            }
            
            Debug.Log($"Imported {lines.Length - 1} buildings from {csvPath}");
        }
        
        void ImportSkills()
        {
            string csvPath = Path.Combine(csvFolderPath, "skill_data.csv");
            
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"CSV file not found: {csvPath}");
                return;
            }
            
            string[] lines = File.ReadAllLines(csvPath);
            
            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var skill = ScriptableObject.CreateInstance<SkillDataSO>();
                skill.InitializeFromCSV(lines[i]);
                
                string jobFolder = skill.jobClass.ToString();
                string outputPath = Path.Combine(soOutputPath, "Skills", jobFolder, $"{skill.skillId}.asset");
                
                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                
                AssetDatabase.CreateAsset(skill, outputPath);
                Debug.Log($"Created skill: {skill.skillName} at {outputPath}");
            }
            
            Debug.Log($"Imported {lines.Length - 1} skills from {csvPath}");
        }
        
        [MenuItem("GuildMaster/Quick Import Characters")]
        public static void QuickImportCharacters()
        {
            var importer = new CSVImporter();
            importer.csvFolderPath = "Assets/CSV/";
            importer.soOutputPath = "Assets/ScriptableObjects/";
            importer.ImportCharacters();
            AssetDatabase.Refresh();
        }
        
        [MenuItem("GuildMaster/Quick Import Dialogues")]
        public static void QuickImportDialogues()
        {
            var importer = new CSVImporter();
            importer.csvFolderPath = "Assets/CSV/";
            importer.soOutputPath = "Assets/ScriptableObjects/";
            importer.ImportDialogues();
            AssetDatabase.Refresh();
        }
        
        [MenuItem("GuildMaster/Quick Import Buildings")]
        public static void QuickImportBuildings()
        {
            var importer = new CSVImporter();
            importer.csvFolderPath = "Assets/CSV/";
            importer.soOutputPath = "Assets/ScriptableObjects/";
            importer.ImportBuildings();
            AssetDatabase.Refresh();
        }
        
        [MenuItem("GuildMaster/Quick Import Skills")]
        public static void QuickImportSkills()
        {
            var importer = new CSVImporter();
            importer.csvFolderPath = "Assets/CSV/";
            importer.soOutputPath = "Assets/ScriptableObjects/";
            importer.ImportSkills();
            AssetDatabase.Refresh();
        }
    }
}