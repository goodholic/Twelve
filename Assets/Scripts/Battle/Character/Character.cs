using UnityEngine;
using System.Collections.Generic;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 캐릭터 레어도
    /// </summary>
    public enum CharacterRarity
    {
        Common,     // 일반
        Uncommon,   // 고급
        Rare,       // 희귀
        Epic,       // 영웅
        Legendary   // 전설
    }
    
    /// <summary>
    /// 캐릭터 데이터 클래스
    /// </summary>
    [System.Serializable]
    public class Character
    {
        [Header("Basic Info")]
        public string characterID;
        public string characterName;
        public JobClass jobClass;
        public int level = 1;
        public CharacterRarity rarity;
        
        [Header("Base Stats")]
        public float baseHP;
        public float baseMP;
        public float baseAttack;
        public float baseDefense;
        public float baseMagicPower;
        public float baseSpeed;
        public float baseCritRate;
        public float baseCritDamage;
        public float baseAccuracy;
        public float baseEvasion;
        
        [Header("Skills")]
        public List<int> skillIDs = new List<int>();
        
        [Header("Description")]
        [TextArea(3, 5)]
        public string description;
        
        /// <summary>
        /// CSV 데이터로부터 캐릭터 생성
        /// </summary>
        public static Character CreateFromCSV(Dictionary<string, string> csvData)
        {
            Character character = new Character();
            
            character.characterID = csvData["ID"];
            character.characterName = csvData["Name"];
            character.jobClass = ParseJobClass(csvData["JobClass"]);
            character.level = int.Parse(csvData["Level"]);
            character.rarity = ParseRarity(csvData["Rarity"]);
            
            character.baseHP = float.Parse(csvData["HP"]);
            character.baseMP = float.Parse(csvData["MP"]);
            character.baseAttack = float.Parse(csvData["Attack"]);
            character.baseDefense = float.Parse(csvData["Defense"]);
            character.baseMagicPower = float.Parse(csvData["MagicPower"]);
            character.baseSpeed = float.Parse(csvData["Speed"]);
            character.baseCritRate = float.Parse(csvData["CritRate"]);
            character.baseCritDamage = float.Parse(csvData["CritDamage"]);
            character.baseAccuracy = float.Parse(csvData["Accuracy"]);
            character.baseEvasion = float.Parse(csvData["Evasion"]);
            
            // 스킬 ID 파싱
            character.skillIDs.Add(int.Parse(csvData["Skill1"]));
            character.skillIDs.Add(int.Parse(csvData["Skill2"]));
            character.skillIDs.Add(int.Parse(csvData["Skill3"]));
            
            character.description = csvData["Description"];
            
            return character;
        }
        
        private static JobClass ParseJobClass(string jobClassName)
        {
            switch (jobClassName.ToLower())
            {
                case "warrior": return JobClass.Warrior;
                case "knight": return JobClass.Knight;
                case "wizard": return JobClass.Wizard;
                case "priest": return JobClass.Priest;
                case "rogue": 
                case "assassin": return JobClass.Rogue;
                case "sage": return JobClass.Sage;
                case "archer": return JobClass.Archer;
                case "gunner": return JobClass.Gunner;
                default:
                    Debug.LogWarning($"Unknown job class: {jobClassName}");
                    return JobClass.Warrior;
            }
        }
        
        private static CharacterRarity ParseRarity(string rarityName)
        {
            switch (rarityName.ToLower())
            {
                case "common": return CharacterRarity.Common;
                case "uncommon": return CharacterRarity.Uncommon;
                case "rare": return CharacterRarity.Rare;
                case "epic": return CharacterRarity.Epic;
                case "legendary": return CharacterRarity.Legendary;
                default:
                    Debug.LogWarning($"Unknown rarity: {rarityName}");
                    return CharacterRarity.Common;
            }
        }
        
        /// <summary>
        /// 최종 스탯 계산 (직업 보정 적용)
        /// </summary>
        public CharacterStats CalculateFinalStats()
        {
            var jobMultipliers = JobClassSystem.GetJobStatMultipliers(jobClass);
            
            CharacterStats finalStats = new CharacterStats
            {
                maxHP = baseHP * jobMultipliers.hpMultiplier,
                maxMP = baseMP,
                attack = baseAttack * jobMultipliers.attackMultiplier,
                defense = baseDefense * jobMultipliers.defenseMultiplier,
                magicPower = baseMagicPower * jobMultipliers.magicPowerMultiplier,
                speed = baseSpeed * jobMultipliers.speedMultiplier,
                critRate = baseCritRate + jobMultipliers.criticalRateBonus,
                critDamage = baseCritDamage,
                accuracy = baseAccuracy,
                evasion = baseEvasion
            };
            
            return finalStats;
        }
    }
    
    /// <summary>
    /// 캐릭터 스탯 구조체
    /// </summary>
    [System.Serializable]
    public struct CharacterStats
    {
        public float maxHP;
        public float maxMP;
        public float attack;
        public float defense;
        public float magicPower;
        public float speed;
        public float critRate;
        public float critDamage;
        public float accuracy;
        public float evasion;
    }
}