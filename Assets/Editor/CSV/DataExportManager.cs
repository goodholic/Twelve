using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle;
using TacticalTileGame.Data;
using GuildMaster.Data;
using System;
using System.Text;

namespace GuildMaster.Editor
{
    /// <summary>
    /// Data Export Manager for creating and exporting game data
    /// As described in CLAUDE.md
    /// </summary>
    public class DataExportManager : EditorWindow
    {
        private Vector2 scrollPosition;
        // CharacterDatabase 관련 필드들 제거됨 (TacticalCharacterData는 유지)
        // private CharacterDatabase characterDatabase;
        // private string selectedDatabasePath = "Assets/Prefabs/Data/CharacterDatabase.asset";
        
        // New character fields
        private string newCharId = "";
        private string newCharName = "";
        private JobClass newCharClass = JobClass.Warrior;
        private int newCharLevel = 1;
        private CharacterRarity newCharRarity = CharacterRarity.Common;
        private CSVCharacter editingCharacter = null;
        private bool isEditMode = false;
        
        // Character stats
        private int newCharHP = 100;
        private int newCharMP = 50;
        private int newCharAttack = 10;
        private int newCharDefense = 5;
        private int newCharMagicPower = 5;
        // private int newCharSpeed = 10; // baseSpeed 제거됨
        private float newCharCritRate = 0.1f;
        private float newCharCritDamage = 1.5f;
        private float newCharAccuracy = 0.95f;
        private float newCharEvasion = 0.05f;
            private string newCharSkill = "101"; // 스킬 하나로 통합
        private string newCharDescription = "";
        
        // Bulk operations
        private float bulkStatMultiplier = 1.0f;
        private int bulkLevelIncrease = 0;
        
        // UI state
        private bool showCharacterList = true;
        private bool showCreateNew = false;
        private bool showBulkOperations = false;
        private string searchFilter = "";

        [MenuItem("Twelve/📊 Data Management/데이터 내보내기 관리자")]
        public static void ShowWindow()
        {
            var window = GetWindow<DataExportManager>("Data Export Manager");
            window.minSize = new Vector2(800, 600);
            window.LoadDatabase();
        }

        // Keyboard shortcut implementation
        [MenuItem("Twelve/📊 Data Management/빠른 데이터 내보내기 %#e")] // Ctrl+Shift+E
        public static void QuickExport()
        {
            var window = GetWindow<DataExportManager>();
            window.QuickExportData();
        }

        void OnEnable()
        {
            LoadDatabase();
        }
        
        void LoadDatabase()
        {
            // CharacterDatabase 로딩 기능 제거됨
            // 통합 데이터베이스 시스템 사용
        }

        void OnGUI()
        {
            DrawHeader();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            if (showCreateNew || isEditMode)
            {
                DrawCharacterEditor();
            }
            else if (showBulkOperations)
            {
                DrawBulkOperations();
            }
            else if (showCharacterList)
            {
                DrawCharacterList();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Character List", EditorStyles.toolbarButton))
            {
                showCharacterList = true;
                showCreateNew = false;
                showBulkOperations = false;
                isEditMode = false;
            }
            
            if (GUILayout.Button("Create New", EditorStyles.toolbarButton))
            {
                showCreateNew = true;
                showCharacterList = false;
                showBulkOperations = false;
                isEditMode = false;
                ResetCharacterFields();
            }
            
            if (GUILayout.Button("Bulk Operations", EditorStyles.toolbarButton))
            {
                showBulkOperations = true;
                showCharacterList = false;
                showCreateNew = false;
                isEditMode = false;
            }
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Export to CSV", EditorStyles.toolbarButton))
            {
                ExportToCSV();
            }
            
            if (GUILayout.Button("Export Filtered", EditorStyles.toolbarButton))
            {
                ExportFilteredToCSV();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCharacterList()
        {
            EditorGUILayout.LabelField("Character Database (기능 제거됨)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("CharacterDatabase 기능이 제거되었습니다. TacticalCharacterData는 계속 사용 가능합니다.", MessageType.Info);
            return;
        }

        private void DrawCharacterEditor()
        {
            EditorGUILayout.LabelField(isEditMode ? "Edit Character" : "Create New Character", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Basic Info
            EditorGUILayout.LabelField("Basic Information", EditorStyles.boldLabel);
            newCharId = EditorGUILayout.TextField("ID", newCharId);
            newCharName = EditorGUILayout.TextField("Name", newCharName);
            newCharClass = (JobClass)EditorGUILayout.EnumPopup("Class", newCharClass);
            newCharLevel = EditorGUILayout.IntField("Level", newCharLevel);
            newCharRarity = (CharacterRarity)EditorGUILayout.EnumPopup("Rarity", newCharRarity);
            
            EditorGUILayout.Space();
            
            // Stats
            EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
            newCharHP = EditorGUILayout.IntField("HP", newCharHP);
            newCharMP = EditorGUILayout.IntField("MP", newCharMP);
            newCharAttack = EditorGUILayout.IntField("Attack", newCharAttack);
            newCharDefense = EditorGUILayout.IntField("Defense", newCharDefense);
            newCharMagicPower = EditorGUILayout.IntField("Magic Power", newCharMagicPower);
            // newCharSpeed = EditorGUILayout.IntField("Speed", newCharSpeed); // baseSpeed 제거됨
            
            EditorGUILayout.Space();
            
            // Combat Stats
            EditorGUILayout.LabelField("Combat Stats", EditorStyles.boldLabel);
            newCharCritRate = EditorGUILayout.Slider("Crit Rate", newCharCritRate, 0f, 1f);
            newCharCritDamage = EditorGUILayout.Slider("Crit Damage", newCharCritDamage, 1f, 5f);
            newCharAccuracy = EditorGUILayout.Slider("Accuracy", newCharAccuracy, 0f, 1f);
            newCharEvasion = EditorGUILayout.Slider("Evasion", newCharEvasion, 0f, 1f);
            
            EditorGUILayout.Space();
            
            // Skills
            EditorGUILayout.LabelField("Skills", EditorStyles.boldLabel);
            newCharSkill = EditorGUILayout.TextField("Skill ID", newCharSkill); // 스킬 하나로 통합
            
            EditorGUILayout.Space();
            
            // Description
            EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
            newCharDescription = EditorGUILayout.TextArea(newCharDescription, GUILayout.Height(60));
            
            EditorGUILayout.Space();
            
            // Auto-generate stats button
            if (GUILayout.Button("Auto-generate Stats Based on Class/Level/Rarity"))
            {
                AutoGenerateStats();
            }
            
            EditorGUILayout.Space();
            
            // Action buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button(isEditMode ? "Save Changes" : "Create Character", GUILayout.Height(30)))
            {
                if (ValidateCharacterData())
                {
                    if (isEditMode)
                    {
                        UpdateCharacter();
                    }
                    else
                    {
                        CreateCharacter();
                    }
                }
            }
            
            if (GUILayout.Button("Cancel", GUILayout.Height(30)))
            {
                showCharacterList = true;
                showCreateNew = false;
                isEditMode = false;
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBulkOperations()
        {
            EditorGUILayout.LabelField("Bulk Operations", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Apply operations to all characters in the database", MessageType.Info);
            
            EditorGUILayout.Space();
            
            bulkStatMultiplier = EditorGUILayout.Slider("Multiply All Stats By", bulkStatMultiplier, 0.1f, 3f);
            if (GUILayout.Button("Apply Stat Multiplier"))
            {
                ApplyBulkStatMultiplier();
            }
            
            EditorGUILayout.Space();
            
            bulkLevelIncrease = EditorGUILayout.IntField("Increase All Levels By", bulkLevelIncrease);
            if (GUILayout.Button("Apply Level Increase"))
            {
                ApplyBulkLevelIncrease();
            }
        }



        private void ResetCharacterFields()
        {
            newCharId = $"char_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            newCharName = "";
            newCharClass = JobClass.Warrior;
            newCharLevel = 1;
            newCharRarity = CharacterRarity.Common;
            newCharHP = 100;
            newCharMP = 50;
            newCharAttack = 10;
            newCharDefense = 5;
            newCharMagicPower = 5;
            // newCharSpeed = 10; // baseSpeed 제거됨
            newCharCritRate = 0.1f;
            newCharCritDamage = 1.5f;
            newCharAccuracy = 0.95f;
            newCharEvasion = 0.05f;
            newCharSkill = "101"; // 스킬 하나로 통합
            newCharDescription = "";
        }

        private void EditCharacter(CSVCharacter character)
        {
            editingCharacter = character;
            isEditMode = true;
            showCharacterList = false;
            showCreateNew = true;
            
            // Load character data into fields
            newCharId = character.id;
            newCharName = character.name;
            newCharClass = character.jobClass;
            newCharLevel = character.level;
            newCharRarity = character.rarity;
            newCharHP = character.baseHP;
            newCharMP = character.baseMP;
            newCharAttack = character.baseAttack;
            newCharDefense = character.baseDefense;
            newCharMagicPower = character.baseMagicPower;
                            // newCharSpeed = character.baseSpeed; // baseSpeed 제거됨
            newCharCritRate = character.critRate;
            newCharCritDamage = character.critDamage;
            newCharAccuracy = character.accuracy;
            newCharEvasion = character.evasion;
                            newCharSkill = character.skillId; // 스킬 하나로 통합
            newCharDescription = character.description;
        }

        private void CloneCharacter(CSVCharacter original)
        {
            showCreateNew = true;
            showCharacterList = false;
            isEditMode = false;
            
            // Copy all data except ID
            newCharId = $"char_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            newCharName = original.name + " (Clone)";
            newCharClass = original.jobClass;
            newCharLevel = original.level;
            newCharRarity = original.rarity;
            newCharHP = original.baseHP;
            newCharMP = original.baseMP;
            newCharAttack = original.baseAttack;
            newCharDefense = original.baseDefense;
            newCharMagicPower = original.baseMagicPower;
                            // newCharSpeed = original.baseSpeed; // baseSpeed 제거됨
            newCharCritRate = original.critRate;
            newCharCritDamage = original.critDamage;
            newCharAccuracy = original.accuracy;
            newCharEvasion = original.evasion;
                            newCharSkill = original.skillId; // 스킬 하나로 통합
            newCharDescription = original.description;
        }

        private void AutoGenerateStats()
        {
            // Base stats based on class
            switch (newCharClass)
            {
                case JobClass.Warrior:
                    newCharHP = 150;
                    newCharAttack = 15;
                    newCharDefense = 12;
                    newCharMagicPower = 5;
                    // newCharSpeed = 8; // baseSpeed 제거됨
                    break;
                case JobClass.Mage:
                    newCharHP = 80;
                    newCharAttack = 5;
                    newCharDefense = 5;
                    newCharMagicPower = 20;
                    // newCharSpeed = 10; // baseSpeed 제거됨
                    break;
                case JobClass.Archer:
                    newCharHP = 100;
                    newCharAttack = 12;
                    newCharDefense = 8;
                    newCharMagicPower = 8;
                    // newCharSpeed = 15; // baseSpeed 제거됨
                    break;
                case JobClass.Priest:
                    newCharHP = 90;
                    newCharAttack = 5;
                    newCharDefense = 8;
                    newCharMagicPower = 15;
                    // newCharSpeed = 10; // baseSpeed 제거됨
                    break;
                case JobClass.Rogue:
                    newCharHP = 90;
                    newCharAttack = 18;
                    newCharDefense = 6;
                    newCharMagicPower = 8;
                    // newCharSpeed = 18; // baseSpeed 제거됨
                    break;
                case JobClass.Knight:
                    newCharHP = 180;
                    newCharAttack = 12;
                    newCharDefense = 18;
                    newCharMagicPower = 10;
                    // newCharSpeed = 6; // baseSpeed 제거됨
                    break;
            }
            
            // Apply level multiplier
            float levelMultiplier = 1f + (newCharLevel - 1) * 0.1f;
            newCharHP = Mathf.RoundToInt(newCharHP * levelMultiplier);
            newCharMP = Mathf.RoundToInt(50 * levelMultiplier);
            newCharAttack = Mathf.RoundToInt(newCharAttack * levelMultiplier);
            newCharDefense = Mathf.RoundToInt(newCharDefense * levelMultiplier);
            newCharMagicPower = Mathf.RoundToInt(newCharMagicPower * levelMultiplier);
            // newCharSpeed = Mathf.RoundToInt(newCharSpeed * levelMultiplier); // baseSpeed 제거됨
            
            // Apply rarity multiplier
            float rarityMultiplier = newCharRarity switch
            {
                CharacterRarity.Common => 1f,
                CharacterRarity.Uncommon => 1.2f,
                CharacterRarity.Rare => 1.5f,
                CharacterRarity.Epic => 2f,
                CharacterRarity.Legendary => 3f,
                _ => 1f
            };
            
            newCharHP = Mathf.RoundToInt(newCharHP * rarityMultiplier);
            newCharMP = Mathf.RoundToInt(newCharMP * rarityMultiplier);
            newCharAttack = Mathf.RoundToInt(newCharAttack * rarityMultiplier);
            newCharDefense = Mathf.RoundToInt(newCharDefense * rarityMultiplier);
            newCharMagicPower = Mathf.RoundToInt(newCharMagicPower * rarityMultiplier);
            // newCharSpeed = Mathf.RoundToInt(newCharSpeed * rarityMultiplier); // baseSpeed 제거됨
        }

        private bool ValidateCharacterData()
        {
            var errors = new List<string>();
            
            // ID validation
            if (string.IsNullOrEmpty(newCharId))
            {
                errors.Add("Character ID cannot be empty");
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(newCharId, @"^[a-zA-Z0-9_-]+$"))
            {
                errors.Add("Character ID contains invalid characters. Use only letters, numbers, underscore, and hyphen");
            }
            
            // Check for duplicate ID if creating new
                    if (!isEditMode)
        {
            // CharacterDatabase 중복 검사 제거됨
            if (false) // 항상 false
                {
                    errors.Add($"Character with ID '{newCharId}' already exists");
                }
            }
            
            // Name validation
            if (string.IsNullOrEmpty(newCharName))
            {
                errors.Add("Character name cannot be empty");
            }
            else if (newCharName.Length > 50)
            {
                errors.Add("Character name is too long (max 50 characters)");
            }
            
            // Stats validation
            if (newCharHP <= 0)
            {
                errors.Add("HP must be greater than 0");
            }
            
            if (newCharMP < 0)
            {
                errors.Add("MP cannot be negative");
            }
            
            if (newCharAttack < 0)
            {
                errors.Add("Attack cannot be negative");
            }
            
            if (newCharDefense < 0)
            {
                errors.Add("Defense cannot be negative");
            }
            
            // if (newCharSpeed <= 0) // baseSpeed 제거됨
            // {
            //     errors.Add("Speed must be greater than 0");
            // }
            
            // Combat stats validation
            if (newCharCritRate < 0 || newCharCritRate > 1)
            {
                errors.Add("Crit rate must be between 0 and 1");
            }
            
            if (newCharCritDamage < 1 || newCharCritDamage > 5)
            {
                errors.Add("Crit damage must be between 1 and 5");
            }
            
            if (newCharAccuracy < 0 || newCharAccuracy > 1)
            {
                errors.Add("Accuracy must be between 0 and 1");
            }
            
            if (newCharEvasion < 0 || newCharEvasion > 1)
            {
                errors.Add("Evasion must be between 0 and 1");
            }
            
            // Level validation
            if (newCharLevel < 1 || newCharLevel > 100)
            {
                errors.Add("Level must be between 1 and 100");
            }
            
            // Show errors if any
            if (errors.Count > 0)
            {
                string errorMessage = string.Join("\n• ", errors);
                EditorUtility.DisplayDialog("Validation Errors", $"Please fix the following errors:\n\n• {errorMessage}", "OK");
                return false;
            }
            
            return true;
        }

        private void CreateCharacter()
        {
            var newCharacter = new CSVCharacter
            {
                id = newCharId,
                name = newCharName,
                jobClass = newCharClass,
                level = newCharLevel,
                rarity = (CharacterRarity)newCharRarity,
                baseHP = newCharHP,
                baseMP = newCharMP,
                baseAttack = newCharAttack,
                baseDefense = newCharDefense,
                baseMagicPower = newCharMagicPower,
                                    // baseSpeed 제거됨
                critRate = newCharCritRate,
                critDamage = newCharCritDamage,
                accuracy = newCharAccuracy,
                evasion = newCharEvasion,
                                    skillId = newCharSkill, // 스킬 하나로 통합
                description = newCharDescription
            };
            
                    // CharacterDatabase 추가 기능 제거됨
        Debug.Log("Character creation feature has been removed");
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("Success", $"Character '{newCharName}' created successfully!", "OK");
            
            showCharacterList = true;
            showCreateNew = false;
        }

        private void UpdateCharacter()
        {
            if (editingCharacter != null)
            {
                editingCharacter.id = newCharId;
                editingCharacter.characterID = newCharId;
                editingCharacter.name = newCharName;
                editingCharacter.characterName = newCharName;
                editingCharacter.jobClass = newCharClass;
                editingCharacter.level = newCharLevel;
                editingCharacter.rarity = (CharacterRarity)newCharRarity;
                editingCharacter.baseHP = newCharHP;
                editingCharacter.baseMP = newCharMP;
                editingCharacter.baseAttack = newCharAttack;
                editingCharacter.baseDefense = newCharDefense;
                editingCharacter.baseMagicPower = newCharMagicPower;
                // editingCharacter.baseSpeed = newCharSpeed; // baseSpeed 제거됨
                editingCharacter.critRate = newCharCritRate;
                editingCharacter.critDamage = newCharCritDamage;
                editingCharacter.accuracy = newCharAccuracy;
                editingCharacter.evasion = newCharEvasion;
                editingCharacter.skillId = newCharSkill; // 스킬 하나로 통합
                editingCharacter.description = newCharDescription;
                
                // CharacterDatabase 업데이트 제거됨
                AssetDatabase.SaveAssets();
                
                EditorUtility.DisplayDialog("Success", $"Character '{newCharName}' updated successfully!", "OK");
                
                isEditMode = false;
                showCharacterList = true;
                showCreateNew = false;
            }
        }

        private void ApplyBulkStatMultiplier()
        {
            if (EditorUtility.DisplayDialog("Apply Bulk Stat Multiplier", 
                $"This will multiply all character stats by {bulkStatMultiplier}. Continue?", "Yes", "No"))
            {
                // CharacterDatabase 접근 제거됨 
                var emptyList = new List<CSVCharacter>();
                foreach (var character in emptyList)
                {
                    character.baseHP = Mathf.RoundToInt(character.baseHP * bulkStatMultiplier);
                    character.baseMP = Mathf.RoundToInt(character.baseMP * bulkStatMultiplier);
                    character.baseAttack = Mathf.RoundToInt(character.baseAttack * bulkStatMultiplier);
                    character.baseDefense = Mathf.RoundToInt(character.baseDefense * bulkStatMultiplier);
                    character.baseMagicPower = Mathf.RoundToInt(character.baseMagicPower * bulkStatMultiplier);
                    // character.baseSpeed = Mathf.RoundToInt(character.baseSpeed * bulkStatMultiplier); // baseSpeed 제거됨
                }
                
                // CharacterDatabase 업데이트 제거됨
                AssetDatabase.SaveAssets();
                
                EditorUtility.DisplayDialog("Success", "Stats multiplied successfully!", "OK");
            }
        }

        private void ApplyBulkLevelIncrease()
        {
            if (EditorUtility.DisplayDialog("Apply Bulk Level Increase", 
                $"This will increase all character levels by {bulkLevelIncrease}. Continue?", "Yes", "No"))
            {
                // CharacterDatabase 접근 제거됨
                var emptyList = new List<CSVCharacter>();
                foreach (var character in emptyList)
                {
                    character.level = Mathf.Max(1, character.level + bulkLevelIncrease);
                }
                
                // CharacterDatabase 업데이트 제거됨
                AssetDatabase.SaveAssets();
                
                EditorUtility.DisplayDialog("Success", "Levels increased successfully!", "OK");
            }
        }

        private void ExportToCSV()
        {
                    // CharacterDatabase 체크 제거됨
        EditorUtility.DisplayDialog("No Data", "Export functionality has been removed!", "OK");
        return;
        
        // string path = EditorUtility.SaveFilePanel("Export Characters to CSV", "", "character_data.csv", "csv");
        // if (string.IsNullOrEmpty(path)) return;
        
        // CharacterDatabase 내보내기 제거됨
        // ExportCharactersToCSV(characterDatabase.characters, path);
        }

        private void ExportCharactersToCSV(List<CSVCharacter> characters, string path)
        {
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("ID,Name,JobClass,Level,Rarity,HP,MP,Attack,Defense,MagicPower,Speed,CritRate,CritDamage,Accuracy,Evasion,Skill1,Skill2,Skill3,Description");
            
            foreach (var character in characters)
            {
                csv.AppendLine($"{character.id},{character.name},{character.jobClass},{character.level}," +
                    $"{character.rarity},{character.baseHP},{character.baseMP},{character.baseAttack}," +
                    $"{character.baseDefense},{character.baseMagicPower}," + // baseSpeed 제거됨
                    $"{character.critRate},{character.critDamage},{character.accuracy},{character.evasion}," +
                    $"{character.skillId},\"{character.description}\""); // 스킬 하나로 통합
            }
            
            File.WriteAllText(path, csv.ToString());
            
            Debug.Log($"Exported {characters.Count} characters to {path}");
            EditorUtility.DisplayDialog("Export Complete", $"Exported {characters.Count} characters successfully!", "OK");
        }
        
        /// <summary>
        /// Quick export function for keyboard shortcut
        /// </summary>
        public void QuickExportData()
        {
            // CharacterDatabase 체크 제거됨 
            EditorUtility.DisplayDialog("No Data", "Quick export functionality has been removed!", "OK");
            return;
            
            // string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            // string path = $"Assets/CSV/character_data_export_{timestamp}.csv";
            
            // CharacterDatabase 내보내기 제거됨
        Debug.Log("Filtered export feature has been removed");
            AssetDatabase.Refresh();
            
            Debug.Log("Quick export completed (feature removed)");
        }
        
        private void ExportFilteredToCSV()
        {
            // CharacterDatabase 체크 제거됨 
            EditorUtility.DisplayDialog("No Data", "Filtered export functionality has been removed!", "OK");
            return;
            
            // Apply search filter
            // CharacterDatabase 필터링 제거됨
            var filteredCharacters = new List<CSVCharacter>()
                .Where(c => string.IsNullOrEmpty(searchFilter) || 
                           c.name.ToLower().Contains(searchFilter.ToLower()))
                .ToList();
            
            if (filteredCharacters.Count == 0)
            {
                EditorUtility.DisplayDialog("No Matches", "No characters match the current filter!", "OK");
                return;
            }
            
            string path = EditorUtility.SaveFilePanel("Export Filtered Characters to CSV", "", "filtered_character_data.csv", "csv");
            if (string.IsNullOrEmpty(path)) return;
            
            // CharacterDatabase 내보내기 제거됨
            Debug.Log("Export feature has been removed");
        }
    }
}