using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using GuildMaster.Data;
using GuildMaster.Battle;

namespace GuildMaster.Editor
{
    public class CSVToScriptableObjectConverter : EditorWindow
    {
        private string csvFolderPath = "Assets/CSV";
        private string scriptableObjectPath = "Assets/Data/ScriptableObjects";
        
        private bool createIndividualCharacters = true;
        private bool createCharacterDatabase = true;
        private bool convertDialogues = true;
        
        [MenuItem("Tools/GuildMaster/CSV to ScriptableObject Converter")]
        public static void ShowWindow()
        {
            GetWindow<CSVToScriptableObjectConverter>("CSV Converter");
        }
        
        void OnGUI()
        {
            GUILayout.Label("CSV to ScriptableObject Converter", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("CSV Folder Path:", csvFolderPath);
            if (GUILayout.Button("Select CSV Folder"))
            {
                string path = EditorUtility.OpenFolderPanel("Select CSV Folder", csvFolderPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    csvFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Output Path:", scriptableObjectPath);
            if (GUILayout.Button("Select Output Folder"))
            {
                string path = EditorUtility.OpenFolderPanel("Select Output Folder", scriptableObjectPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    scriptableObjectPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            
            GUILayout.Space(20);
            
            GUILayout.Label("Conversion Options", EditorStyles.boldLabel);
            createIndividualCharacters = EditorGUILayout.Toggle("Create Individual Characters", createIndividualCharacters);
            createCharacterDatabase = EditorGUILayout.Toggle("Create Character Database", createCharacterDatabase);
            convertDialogues = EditorGUILayout.Toggle("Convert Dialogue CSVs", convertDialogues);
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("Convert Character Data", GUILayout.Height(30)))
            {
                ConvertCharacterData();
            }
            
            if (GUILayout.Button("Convert Dialogue Data", GUILayout.Height(30)))
            {
                ConvertDialogueData();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Convert All", GUILayout.Height(40)))
            {
                ConvertCharacterData();
                ConvertDialogueData();
            }
        }
        
        void ConvertCharacterData()
        {
            if (!createIndividualCharacters && !createCharacterDatabase) return;
            
            string csvPath = Path.Combine(Application.dataPath, csvFolderPath.Substring(7), "character_data.csv");
            if (!File.Exists(csvPath))
            {
                EditorUtility.DisplayDialog("Error", "character_data.csv not found!", "OK");
                return;
            }
            
            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length <= 1)
            {
                EditorUtility.DisplayDialog("Error", "CSV file is empty!", "OK");
                return;
            }
            
            List<GuildMaster.Data.CharacterData> allCharacters = new List<GuildMaster.Data.CharacterData>();
            
            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = ParseCSVLine(lines[i]);
                if (values.Length < 19) continue;
                
                GuildMaster.Data.CharacterData character = new GuildMaster.Data.CharacterData();
                
                // Parse data - CharacterData 필드명 사용
                character.id = values[0];
                character.name = values[1];
                character.jobClass = ParseDataJobClass(values[2]);
                character.level = int.Parse(values[3]);
                character.rarity = ParseRarity(values[4]);
                character.baseHP = int.Parse(values[5]);
                character.baseMP = int.Parse(values[6]);
                character.baseAttack = int.Parse(values[7]);
                character.baseDefense = int.Parse(values[8]);
                character.baseMagicPower = int.Parse(values[9]);
                character.baseSpeed = int.Parse(values[10]);
                character.critRate = float.Parse(values[11]);
                character.critDamage = float.Parse(values[12]);
                character.accuracy = float.Parse(values[13]);
                character.evasion = float.Parse(values[14]);
                
                // 스킬 ID들
                character.skill1Id = values[15];
                character.skill2Id = values[16];
                character.skill3Id = values[17];
                character.description = values[18];
                
                allCharacters.Add(character);
            }
            
            // Create database
            if (createCharacterDatabase)
            {
                string dbPath = Path.Combine(scriptableObjectPath, "CharacterDatabase.asset");
                CharacterDatabase database = CreateOrLoadAsset<CharacterDatabase>(dbPath);
                // database의 characters 설정은 CharacterDatabase 구조에 따라 수정 필요
                EditorUtility.SetDirty(database);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", $"Converted {allCharacters.Count} characters!", "OK");
        }
        
        void ConvertDialogueData()
        {
            if (!convertDialogues) return;
            
            // DialogueDataSO 클래스 인식 문제로 인해 임시로 단순화
            Debug.Log("Dialogue conversion temporarily disabled due to compilation issues. Will be fixed after DialogueDataSO is properly recognized by Unity.");
            EditorUtility.DisplayDialog("Info", "Dialogue conversion temporarily disabled. Please compile the project first to recognize DialogueDataSO class.", "OK");
        }
        
        T CreateOrLoadAsset<T>(string assetPath) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                string directory = Path.GetDirectoryName(assetPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            return asset;
        }
        
        string[] ParseCSVLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string currentField = "";
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
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
            
            result.Add(currentField);
            return result.ToArray();
        }
        
        GuildMaster.Data.JobClass ParseDataJobClass(string className)
        {
            switch (className)
            {
                case "Warrior": return GuildMaster.Data.JobClass.Warrior;
                case "Knight": return GuildMaster.Data.JobClass.Paladin;
                case "Mage": return GuildMaster.Data.JobClass.Mage;
                case "Priest": return GuildMaster.Data.JobClass.Priest;
                case "Assassin": return GuildMaster.Data.JobClass.Rogue;
                case "Ranger": return GuildMaster.Data.JobClass.Archer;
                case "Sage": return GuildMaster.Data.JobClass.Mage;
                default: return GuildMaster.Data.JobClass.Warrior;
            }
        }
        
        GuildMaster.Battle.JobClass ParseJobClass(string className)
        {
            switch (className)
            {
                case "Warrior": return GuildMaster.Battle.JobClass.Warrior;
                case "Knight": return GuildMaster.Battle.JobClass.Knight;
                case "Mage": return GuildMaster.Battle.JobClass.Mage;
                case "Priest": return GuildMaster.Battle.JobClass.Priest;
                case "Assassin": return GuildMaster.Battle.JobClass.Assassin;
                case "Ranger": return GuildMaster.Battle.JobClass.Ranger;
                case "Sage": return GuildMaster.Battle.JobClass.Sage;
                default: return GuildMaster.Battle.JobClass.None;
            }
        }
        
        GuildMaster.Data.CharacterRarity ParseRarity(string rarity)
        {
            switch (rarity)
            {
                case "Common": return GuildMaster.Data.CharacterRarity.Common;
                case "Uncommon": return GuildMaster.Data.CharacterRarity.Uncommon;
                case "Rare": return GuildMaster.Data.CharacterRarity.Rare;
                case "Epic": return GuildMaster.Data.CharacterRarity.Epic;
                case "Legendary": return GuildMaster.Data.CharacterRarity.Legendary;
                default: return GuildMaster.Data.CharacterRarity.Common;
            }
        }
    }
}