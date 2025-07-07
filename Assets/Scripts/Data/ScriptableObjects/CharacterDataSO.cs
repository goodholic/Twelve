using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle;
using Unit = GuildMaster.Battle.UnitStatus;

namespace GuildMaster.Data
{
    [CreateAssetMenu(fileName = "CharacterDatabaseSO", menuName = "GuildMaster/Data/Character Database SO")]
    public class CharacterDatabaseSO : ScriptableObject
    {
        [SerializeField] public List<CharacterDataSO> characters = new List<CharacterDataSO>();
        
        private Dictionary<string, CharacterDataSO> characterLookup;
        
        public void Initialize()
        {
            Debug.Log($"CharacterDatabase initialized with {characters.Count} characters");
            characterLookup = new Dictionary<string, CharacterDataSO>();
            foreach (var character in characters)
            {
                if (!characterLookup.ContainsKey(character.id))
                {
                    characterLookup.Add(character.id, character);
                }
            }
        }
        
        public CharacterDataSO GetCharacter(string id)
        {
            return characters.FirstOrDefault(c => c.id == id);
        }
        
        public CharacterDataSO GetCharacterByName(string name)
        {
            return characters.FirstOrDefault(c => c.characterName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        
        public CharacterDataSO GetRandomCharacter()
        {
            if (characters.Count == 0) return null;
            int randomIndex = UnityEngine.Random.Range(0, characters.Count);
            return characters[randomIndex];
        }
        
        public CharacterDataSO GetRandomCharacterByRarity(GuildMaster.Data.Rarity rarity)
        {
            var filtered = characters.Where(c => c.rarity == rarity).ToList();
            if (filtered.Count == 0) return null;
            int randomIndex = UnityEngine.Random.Range(0, filtered.Count);
            return filtered[randomIndex];
        }
        
        public List<CharacterDataSO> GetCharactersByClass(JobClass jobClass)
        {
            return characters.Where(c => c.jobClass == jobClass).ToList();
        }
        
        public List<CharacterDataSO> GetCharactersByRarity(GuildMaster.Data.Rarity rarity)
        {
            return characters.Where(c => c.rarity == rarity).ToList();
        }
    }

    [System.Serializable]
    public class CharacterDataSO : ScriptableObject
    {
        public string id;
        public string characterName;
        public JobClass jobClass;
        public int baseLevel;
        public Rarity rarity;
        public int baseHP;
        public int baseMP;
        public int baseAttack;
        public int baseDefense;
        public int baseMagicPower;
        public int baseSpeed;
        public float critRate;
        public float critDamage;
        public float accuracy;
        public float evasion;
        public List<string> skillIds;
        public string description;
        public Sprite portrait;
        public GameObject modelPrefab;

        public Unit CreateUnit()
        {
            var unit = new Unit(characterName, baseLevel, jobClass, rarity);
            unit.unitId = id;
            unit.maxHP = baseHP;
            unit.maxMP = baseMP;
            unit.attackPower = baseAttack;
            unit.defense = baseDefense;
            unit.magicPower = baseMagicPower;
            unit.speed = baseSpeed;
            unit.criticalRate = critRate;
            unit.accuracy = accuracy;
            
            unit.currentHP = unit.maxHP;
            unit.currentMP = unit.maxMP;
            
            return unit;
        }
    }
}