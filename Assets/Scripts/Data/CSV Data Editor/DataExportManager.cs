using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle;
using GuildMaster.Game;
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
        private CharacterDatabaseSO characterDatabase;
        private string selectedDatabasePath = "Assets/Prefabs/Data/CharacterDatabase.asset";
        
        // New character fields
        private string newCharId = "";
        private string newCharName = "";
        private JobClass newCharClass = JobClass.Warrior;
        private int newCharLevel = 1;
        private CharacterRarity newCharRarity = CharacterRarity.Common;
        private CharacterData editingCharacter = null;
        private bool isEditMode = false;
        
        // Character stats
        private int newCharHP = 100;
        private int newCharMP = 50;
        private int newCharAttack = 10;
        private int newCharDefense = 5;
        private int newCharMagicPower = 5;
        private int newCharSpeed = 10;
        private float newCharCritRate = 0.1f;
        private float newCharCritDamage = 1.5f;
        private float newCharAccuracy = 0.95f;
        private float newCharEvasion = 0.05f;
        private string newCharSkill1 = "101";
        private string newCharSkill2 = "102";
        private string newCharSkill3 = "103";
        private string newCharDescription = "";
        
        // Bulk operations
        private float bulkStatMultiplier = 1.0f;
        private int bulkLevelIncrease = 0;
        
        // UI state
        private bool showCharacterList = true;
        private bool showCreateNew = false;
        private bool showBulkOperations = false;
        private string searchFilter = "";

        [MenuItem("Tools/GuildMaster/Data Export Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<DataExportManager>("Data Export Manager");
            window.minSize = new Vector2(800, 600);
            window.LoadDatabase();
        }

        // Keyboard shortcut implementation
        [MenuItem("Tools/GuildMaster/Quick Data Export %#e")] // Ctrl+Shift+E
        public static void QuickExport()
        {
            var window = GetWindow<DataExportManager>();
            window.QuickExportData();
        }

        void OnEnable()
        {
            LoadDatabase();
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
            EditorGUILayout.LabelField("Character Database", EditorStyles.boldLabel);
            
            searchFilter = EditorGUILayout.TextField("Search", searchFilter);
            
            if (characterDatabase == null || characterDatabase.characters == null)
            {
                EditorGUILayout.HelpBox("No character database loaded", MessageType.Warning);
                return;
            }
            
            var filteredCharacters = characterDatabase.characters
                .Where(c => string.IsNullOrEmpty(searchFilter) || 
                           c.name.ToLower().Contains(searchFilter.ToLower()))
                .ToList();
            
            EditorGUILayout.LabelField($"Characters ({filteredCharacters.Count}):", EditorStyles.boldLabel);
            
            foreach (var character in filteredCharacters)
            {
                EditorGUILayout.BeginHorizontal(GUI.skin.box);
                
                EditorGUILayout.LabelField(character.name, GUILayout.Width(200));
                EditorGUILayout.LabelField($"Lv.{character.level}", GUILayout.Width(50));
                EditorGUILayout.LabelField(character.jobClass.ToString(), GUILayout.Width(100));
                EditorGUILayout.LabelField(character.rarity.ToString(), GUILayout.Width(100));
                
                if (GUILayout.Button("Edit", GUILayout.Width(50)))
                {
                    EditCharacter(character);
                }
                
                if (GUILayout.Button("Clone", GUILayout.Width(50)))
                {
                    CloneCharacter(character);
                }
                
                if (GUILayout.Button("Delete", GUILayout.Width(50)))
                {
                    if (EditorUtility.DisplayDialog("Delete Character", 
                        $"Are you sure you want to delete {character.name}?", "Yes", "No"))
                    {
                        characterDatabase.characters.Remove(character);
                        EditorUtility.SetDirty(characterDatabase);
                        AssetDatabase.SaveAssets();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
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
            newCharSpeed = EditorGUILayout.IntField("Speed", newCharSpeed);
            
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
            newCharSkill1 = EditorGUILayout.TextField("Skill 1 ID", newCharSkill1);
            newCharSkill2 = EditorGUILayout.TextField("Skill 2 ID", newCharSkill2);
            newCharSkill3 = EditorGUILayout.TextField("Skill 3 ID", newCharSkill3);
            
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

        private void LoadDatabase()
        {
            characterDatabase = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(selectedDatabasePath);
            if (characterDatabase == null)
            {
                // Try alternate path
                characterDatabase = AssetDatabase.LoadAssetAtPath<CharacterDatabase>("Assets/Prefabs/Data/Characters/CharacterDatabase.asset");
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
            newCharSpeed = 10;
            newCharCritRate = 0.1f;
            newCharCritDamage = 1.5f;
            newCharAccuracy = 0.95f;
            newCharEvasion = 0.05f;
            newCharSkill1 = "101";
            newCharSkill2 = "102";
            newCharSkill3 = "103";
            newCharDescription = "";
        }

        private void EditCharacter(CharacterData character)
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
            newCharSpeed = character.baseSpeed;
            newCharCritRate = character.critRate;
            newCharCritDamage = character.critDamage;
            newCharAccuracy = character.accuracy;
            newCharEvasion = character.evasion;
            newCharSkill1 = character.skill1Id;
            newCharSkill2 = character.skill2Id;
            newCharSkill3 = character.skill3Id;
            newCharDescription = character.description;
        }

        private void CloneCharacter(CharacterData original)
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
            newCharSpeed = original.baseSpeed;
            newCharCritRate = original.critRate;
            newCharCritDamage = original.critDamage;
            newCharAccuracy = original.accuracy;
            newCharEvasion = original.evasion;
            newCharSkill1 = original.skill1Id;
            newCharSkill2 = original.skill2Id;
            newCharSkill3 = original.skill3Id;
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
                    newCharSpeed = 8;
                    break;
                case JobClass.Mage:
                    newCharHP = 80;
                    newCharAttack = 5;
                    newCharDefense = 5;
                    newCharMagicPower = 20;
                    newCharSpeed = 10;
                    break;
                case JobClass.Archer:
                    newCharHP = 100;
                    newCharAttack = 12;
                    newCharDefense = 8;
                    newCharMagicPower = 8;
                    newCharSpeed = 15;
                    break;
                case JobClass.Priest:
                    newCharHP = 90;
                    newCharAttack = 5;
                    newCharDefense = 8;
                    newCharMagicPower = 15;
                    newCharSpeed = 10;
                    break;
                case JobClass.Rogue:
                    newCharHP = 90;
                    newCharAttack = 18;
                    newCharDefense = 6;
                    newCharMagicPower = 8;
                    newCharSpeed = 18;
                    break;
                case JobClass.Knight:
                    newCharHP = 180;
                    newCharAttack = 12;
                    newCharDefense = 18;
                    newCharMagicPower = 10;
                    newCharSpeed = 6;
                    break;
            }
            
            // Apply level multiplier
            float levelMultiplier = 1f + (newCharLevel - 1) * 0.1f;
            newCharHP = Mathf.RoundToInt(newCharHP * levelMultiplier);
            newCharMP = Mathf.RoundToInt(50 * levelMultiplier);
            newCharAttack = Mathf.RoundToInt(newCharAttack * levelMultiplier);
            newCharDefense = Mathf.RoundToInt(newCharDefense * levelMultiplier);
            newCharMagicPower = Mathf.RoundToInt(newCharMagicPower * levelMultiplier);
            newCharSpeed = Mathf.RoundToInt(newCharSpeed * levelMultiplier);
            
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
            newCharSpeed = Mathf.RoundToInt(newCharSpeed * rarityMultiplier);
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
            if (!isEditMode && characterDatabase != null && characterDatabase.characters != null)
            {
                if (characterDatabase.characters.Any(c => c.id == newCharId))
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
            
            if (newCharSpeed <= 0)
            {
                errors.Add("Speed must be greater than 0");
            }
            
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
            var newCharacter = new CharacterData
            {
                id = newCharId,
                name = newCharName,
                jobClass = newCharClass,
                level = newCharLevel,
                rarity = newCharRarity,
                baseHP = newCharHP,
                baseMP = newCharMP,
                baseAttack = newCharAttack,
                baseDefense = newCharDefense,
                baseMagicPower = newCharMagicPower,
                baseSpeed = newCharSpeed,
                critRate = newCharCritRate,
                critDamage = newCharCritDamage,
                accuracy = newCharAccuracy,
                evasion = newCharEvasion,
                skill1Id = newCharSkill1,
                skill2Id = newCharSkill2,
                skill3Id = newCharSkill3,
                description = newCharDescription
            };
            
            if (characterDatabase.characters == null)
                characterDatabase.characters = new List<CharacterData>();
                
            characterDatabase.characters.Add(newCharacter);
            EditorUtility.SetDirty(characterDatabase);
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
                editingCharacter.name = newCharName;
                editingCharacter.jobClass = newCharClass;
                editingCharacter.level = newCharLevel;
                editingCharacter.rarity = newCharRarity;
                editingCharacter.baseHP = newCharHP;
                editingCharacter.baseMP = newCharMP;
                editingCharacter.baseAttack = newCharAttack;
                editingCharacter.baseDefense = newCharDefense;
                editingCharacter.baseMagicPower = newCharMagicPower;
                editingCharacter.baseSpeed = newCharSpeed;
                editingCharacter.critRate = newCharCritRate;
                editingCharacter.critDamage = newCharCritDamage;
                editingCharacter.accuracy = newCharAccuracy;
                editingCharacter.evasion = newCharEvasion;
                editingCharacter.skill1Id = newCharSkill1;
                editingCharacter.skill2Id = newCharSkill2;
                editingCharacter.skill3Id = newCharSkill3;
                editingCharacter.description = newCharDescription;
                
                EditorUtility.SetDirty(characterDatabase);
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
                foreach (var character in characterDatabase.characters)
                {
                    character.baseHP = Mathf.RoundToInt(character.baseHP * bulkStatMultiplier);
                    character.baseMP = Mathf.RoundToInt(character.baseMP * bulkStatMultiplier);
                    character.baseAttack = Mathf.RoundToInt(character.baseAttack * bulkStatMultiplier);
                    character.baseDefense = Mathf.RoundToInt(character.baseDefense * bulkStatMultiplier);
                    character.baseMagicPower = Mathf.RoundToInt(character.baseMagicPower * bulkStatMultiplier);
                    character.baseSpeed = Mathf.RoundToInt(character.baseSpeed * bulkStatMultiplier);
                }
                
                EditorUtility.SetDirty(characterDatabase);
                AssetDatabase.SaveAssets();
                
                EditorUtility.DisplayDialog("Success", "Stats multiplied successfully!", "OK");
            }
        }

        private void ApplyBulkLevelIncrease()
        {
            if (EditorUtility.DisplayDialog("Apply Bulk Level Increase", 
                $"This will increase all character levels by {bulkLevelIncrease}. Continue?", "Yes", "No"))
            {
                foreach (var character in characterDatabase.characters)
                {
                    character.level = Mathf.Max(1, character.level + bulkLevelIncrease);
                }
                
                EditorUtility.SetDirty(characterDatabase);
                AssetDatabase.SaveAssets();
                
                EditorUtility.DisplayDialog("Success", "Levels increased successfully!", "OK");
            }
        }

        private void ExportToCSV()
        {
            if (characterDatabase == null || characterDatabase.characters.Count == 0)
            {
                EditorUtility.DisplayDialog("No Data", "No character data to export!", "OK");
                return;
            }
            
            string path = EditorUtility.SaveFilePanel("Export Characters to CSV", "", "character_data.csv", "csv");
            if (string.IsNullOrEmpty(path)) return;
            
            ExportCharactersToCSV(characterDatabase.characters, path);
        }

        private void ExportCharactersToCSV(List<CharacterData> characters, string path)
        {
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("ID,Name,JobClass,Level,Rarity,HP,MP,Attack,Defense,MagicPower,Speed,CritRate,CritDamage,Accuracy,Evasion,Skill1,Skill2,Skill3,Description");
            
            foreach (var character in characters)
            {
                csv.AppendLine($"{character.id},{character.name},{character.jobClass},{character.level}," +
                    $"{character.rarity},{character.baseHP},{character.baseMP},{character.baseAttack}," +
                    $"{character.baseDefense},{character.baseMagicPower},{character.baseSpeed}," +
                    $"{character.critRate},{character.critDamage},{character.accuracy},{character.evasion}," +
                    $"{character.skill1Id},{character.skill2Id},{character.skill3Id},\"{character.description}\"");
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
            if (characterDatabase == null || characterDatabase.characters.Count == 0)
            {
                EditorUtility.DisplayDialog("No Data", "No character data to export!", "OK");
                return;
            }
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string path = $"Assets/CSV/character_data_export_{timestamp}.csv";
            
            ExportCharactersToCSV(characterDatabase.characters, path);
            AssetDatabase.Refresh();
            
            Debug.Log($"Quick export completed: {path}");
        }
        
        private void ExportFilteredToCSV()
        {
            if (characterDatabase == null || characterDatabase.characters.Count == 0)
            {
                EditorUtility.DisplayDialog("No Data", "No character data to export!", "OK");
                return;
            }
            
            // Apply search filter
            var filteredCharacters = characterDatabase.characters
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
            
            ExportCharactersToCSV(filteredCharacters, path);
        }
    }
}