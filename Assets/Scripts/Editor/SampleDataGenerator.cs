using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using GuildMaster.Data;
using System.IO;
using System.Text;

namespace GuildMaster.Editor
{
    /// <summary>
    /// Sample data generation tools as mentioned in CLAUDE.md
    /// </summary>
    public class SampleDataGenerator
    {
        [MenuItem("Tools/Generate Sample Data")]
        public static void GenerateSampleCharacterData()
        {
            if (EditorUtility.DisplayDialog("Generate Sample Data", 
                "This will generate sample character data. Continue?", "Yes", "No"))
            {
                Debug.Log("Generating sample character data...");
                
                var characters = new List<CharacterData>();
                
                // Generate warriors
                characters.Add(CreateSampleCharacter("sample_warrior_1", "Iron Warrior", JobClass.Warrior, 1, CharacterRarity.Common));
                characters.Add(CreateSampleCharacter("sample_warrior_2", "Steel Warrior", JobClass.Warrior, 5, CharacterRarity.Uncommon));
                characters.Add(CreateSampleCharacter("sample_warrior_3", "Legendary Warrior", JobClass.Warrior, 10, CharacterRarity.Rare));
                
                // Generate mages
                characters.Add(CreateSampleCharacter("sample_mage_1", "Apprentice Mage", JobClass.Mage, 1, CharacterRarity.Common));
                characters.Add(CreateSampleCharacter("sample_mage_2", "Fire Mage", JobClass.Mage, 5, CharacterRarity.Uncommon));
                characters.Add(CreateSampleCharacter("sample_mage_3", "Archmage", JobClass.Mage, 10, CharacterRarity.Epic));
                
                // Generate archers
                characters.Add(CreateSampleCharacter("sample_archer_1", "Scout", JobClass.Archer, 1, CharacterRarity.Common));
                characters.Add(CreateSampleCharacter("sample_archer_2", "Marksman", JobClass.Archer, 5, CharacterRarity.Uncommon));
                characters.Add(CreateSampleCharacter("sample_archer_3", "Master Archer", JobClass.Archer, 10, CharacterRarity.Rare));
                
                // Save to database
                string dbPath = "Assets/Prefabs/Data/SampleCharacterDatabase.asset";
                var database = ScriptableObject.CreateInstance<CharacterDatabase>();
                database.characters = characters;
                
                // Create directory if needed
                string dir = Path.GetDirectoryName(dbPath);
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                
                AssetDatabase.CreateAsset(database, dbPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log($"Generated {characters.Count} sample characters at {dbPath}");
                EditorUtility.DisplayDialog("Complete", "Sample data generation complete!", "OK");
            }
        }
        
        [MenuItem("Tools/Generate Ally Sample Data")]
        public static void GenerateAllySampleData()
        {
            if (EditorUtility.DisplayDialog("Generate Ally Sample Data", 
                "This will generate sample ally character data. Continue?", "Yes", "No"))
            {
                Debug.Log("Generating ally sample data...");
                
                var allies = new List<CharacterData>();
                
                // Generate diverse ally characters
                allies.Add(CreateSampleCharacter("ally_tank_1", "Shield Guardian", JobClass.Paladin, 5, CharacterRarity.Uncommon));
                allies.Add(CreateSampleCharacter("ally_healer_1", "Holy Priest", JobClass.Priest, 5, CharacterRarity.Uncommon));
                allies.Add(CreateSampleCharacter("ally_dps_1", "Shadow Assassin", JobClass.Rogue, 5, CharacterRarity.Uncommon));
                allies.Add(CreateSampleCharacter("ally_tank_2", "Iron Defender", JobClass.Paladin, 10, CharacterRarity.Rare));
                allies.Add(CreateSampleCharacter("ally_healer_2", "Divine Oracle", JobClass.Priest, 10, CharacterRarity.Rare));
                allies.Add(CreateSampleCharacter("ally_dps_2", "Blade Master", JobClass.Rogue, 10, CharacterRarity.Rare));
                
                // Generate CSV
                var sb = new StringBuilder();
                sb.AppendLine("ID,Name,JobClass,Level,Rarity,HP,MP,Attack,Defense,MagicPower,Speed,CritRate,CritDamage,Accuracy,Evasion,Skill1,Skill2,Skill3,Description");
                
                foreach (var ally in allies)
                {
                    sb.AppendLine($"{ally.id},{ally.name},{ally.jobClass},{ally.level}," +
                        $"{ally.rarity},{ally.baseHP},{ally.baseMP},{ally.baseAttack}," +
                        $"{ally.baseDefense},{ally.baseMagicPower},{ally.baseSpeed}," +
                        $"{ally.critRate},{ally.critDamage},{ally.accuracy},{ally.evasion}," +
                        $"{ally.skill1Id},{ally.skill2Id},{ally.skill3Id},\"{ally.description}\"");
                }
                
                // Save CSV
                string csvPath = "Assets/CSV/ally_sample_data.csv";
                File.WriteAllText(csvPath, sb.ToString());
                
                AssetDatabase.Refresh();
                
                Debug.Log($"Generated {allies.Count} ally sample characters at {csvPath}");
                EditorUtility.DisplayDialog("Complete", "Ally sample data generation complete!", "OK");
            }
        }
        
        private static CharacterData CreateSampleCharacter(string id, string name, JobClass jobClass, int level, CharacterRarity rarity)
        {
            var character = new CharacterData
            {
                id = id,
                name = name,
                jobClass = jobClass,
                level = level,
                rarity = rarity,
                description = $"Sample {jobClass} character at level {level}"
            };
            
            // Base stats by class
            switch (jobClass)
            {
                case JobClass.Warrior:
                    character.baseHP = 150;
                    character.baseAttack = 15;
                    character.baseDefense = 12;
                    character.baseMagicPower = 5;
                    character.baseSpeed = 8;
                    break;
                case JobClass.Mage:
                    character.baseHP = 80;
                    character.baseAttack = 5;
                    character.baseDefense = 5;
                    character.baseMagicPower = 20;
                    character.baseSpeed = 10;
                    break;
                case JobClass.Archer:
                    character.baseHP = 100;
                    character.baseAttack = 12;
                    character.baseDefense = 8;
                    character.baseMagicPower = 8;
                    character.baseSpeed = 15;
                    break;
                case JobClass.Priest:
                    character.baseHP = 90;
                    character.baseAttack = 5;
                    character.baseDefense = 8;
                    character.baseMagicPower = 15;
                    character.baseSpeed = 10;
                    break;
                case JobClass.Rogue:
                    character.baseHP = 90;
                    character.baseAttack = 18;
                    character.baseDefense = 6;
                    character.baseMagicPower = 8;
                    character.baseSpeed = 18;
                    break;
                case JobClass.Paladin:
                    character.baseHP = 180;
                    character.baseAttack = 12;
                    character.baseDefense = 18;
                    character.baseMagicPower = 10;
                    character.baseSpeed = 6;
                    break;
            }
            
            // Apply level scaling
            float levelMultiplier = 1f + (level - 1) * 0.1f;
            character.baseHP = Mathf.RoundToInt(character.baseHP * levelMultiplier);
            character.baseMP = Mathf.RoundToInt(50 * levelMultiplier);
            character.baseAttack = Mathf.RoundToInt(character.baseAttack * levelMultiplier);
            character.baseDefense = Mathf.RoundToInt(character.baseDefense * levelMultiplier);
            character.baseMagicPower = Mathf.RoundToInt(character.baseMagicPower * levelMultiplier);
            character.baseSpeed = Mathf.RoundToInt(character.baseSpeed * levelMultiplier);
            
            // Apply rarity scaling
            float rarityMultiplier = rarity switch
            {
                CharacterRarity.Common => 1f,
                CharacterRarity.Uncommon => 1.2f,
                CharacterRarity.Rare => 1.5f,
                CharacterRarity.Epic => 2f,
                CharacterRarity.Legendary => 3f,
                _ => 1f
            };
            
            character.baseHP = Mathf.RoundToInt(character.baseHP * rarityMultiplier);
            character.baseMP = Mathf.RoundToInt(character.baseMP * rarityMultiplier);
            character.baseAttack = Mathf.RoundToInt(character.baseAttack * rarityMultiplier);
            character.baseDefense = Mathf.RoundToInt(character.baseDefense * rarityMultiplier);
            character.baseMagicPower = Mathf.RoundToInt(character.baseMagicPower * rarityMultiplier);
            character.baseSpeed = Mathf.RoundToInt(character.baseSpeed * rarityMultiplier);
            
            // Set combat stats
            character.critRate = 0.1f + (int)rarity * 0.05f;
            character.critDamage = 1.5f + (int)rarity * 0.1f;
            character.accuracy = 0.9f + (int)rarity * 0.02f;
            character.evasion = 0.05f + (int)rarity * 0.02f;
            
            // Set skills
            character.skill1Id = "101";
            character.skill2Id = "102";
            character.skill3Id = "103";
            
            return character;
        }
    }
}