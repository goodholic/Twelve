using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Data;
using GuildMaster.Battle;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 캐릭터 관리 시스템
    /// </summary>
    public class CharacterManager : MonoBehaviour
    {
        private static CharacterManager _instance;
        public static CharacterManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CharacterManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CharacterManager");
                        _instance = go.AddComponent<CharacterManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Character Database")]
        [SerializeField] private CharacterDatabase characterDatabase;
        
        private Dictionary<string, CharacterData> characterDataDict = new Dictionary<string, CharacterData>();
        private List<Battle.Unit> ownedCharacters = new List<Battle.Unit>();
        private List<Battle.Unit> availableCharacters = new List<Battle.Unit>();
        
        // Events
        public event Action<Battle.Unit> OnCharacterUnlocked;
        public event Action<Battle.Unit> OnCharacterLevelUp;
        public event Action<Battle.Unit> OnCharacterPromoted;
        public event Action<Battle.Unit> OnCharacterRemoved;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeCharacterManager();
        }

        void InitializeCharacterManager()
        {
            if (characterDatabase != null)
            {
                foreach (var characterData in characterDatabase.characters)
                {
                    characterDataDict[characterData.id] = characterData;
                }
            }
        }

        public CharacterData GetCharacterData(string characterId)
        {
            characterDataDict.TryGetValue(characterId, out CharacterData data);
            return data;
        }

        public List<CharacterData> GetAllCharacterData()
        {
            return characterDataDict.Values.ToList();
        }

        public Battle.Unit CreateCharacter(string characterId, int level = 1)
        {
            var characterData = GetCharacterData(characterId);
            if (characterData == null)
            {
                Debug.LogError($"Character data not found for ID: {characterId}");
                return null;
            }

            var unit = new Battle.Unit(characterData.name, level, ConvertJobClass(characterData.jobClass), ConvertRarity(characterData.rarity))
            {
                unitId = characterId,
                maxHP = characterData.baseHP,
                currentHP = characterData.baseHP,
                maxMP = characterData.baseMP,
                currentMP = characterData.baseMP,
                attackPower = characterData.baseAttack,
                defense = characterData.baseDefense,
                magicPower = characterData.baseMagicPower,
                speed = characterData.baseSpeed,
                accuracy = characterData.accuracy,
                evasion = characterData.evasion
            };

            // 스킬 추가
            if (!string.IsNullOrEmpty(characterData.skill1Id))
                unit.skillIds.Add(int.Parse(characterData.skill1Id));
            if (!string.IsNullOrEmpty(characterData.skill2Id))
                unit.skillIds.Add(int.Parse(characterData.skill2Id));
            if (!string.IsNullOrEmpty(characterData.skill3Id))
                unit.skillIds.Add(int.Parse(characterData.skill3Id));

            return unit;
        }

        public bool UnlockCharacter(string characterId)
        {
            var characterData = GetCharacterData(characterId);
            if (characterData == null)
                return false;

            var unit = CreateCharacter(characterId);
            if (unit == null)
                return false;

            if (!ownedCharacters.Any(c => c.unitId == characterId))
            {
                ownedCharacters.Add(unit);
                OnCharacterUnlocked?.Invoke(unit);
                return true;
            }

            return false;
        }

        public bool LevelUpCharacter(string characterId)
        {
            var character = ownedCharacters.FirstOrDefault(c => c.unitId == characterId);
            if (character == null)
                return false;

            character.level++;
            
            // 스탯 증가
            var characterData = GetCharacterData(characterId);
            if (characterData != null)
            {
                character.maxHP += characterData.baseHP / 10;
                character.currentHP = character.maxHP;
                character.maxMP += characterData.baseMP / 10;
                character.currentMP = character.maxMP;
                character.attackPower += characterData.baseAttack / 10;
                character.defense += characterData.baseDefense / 10;
                character.magicPower += characterData.baseMagicPower / 10;
                character.speed += characterData.baseSpeed / 10;
            }

            OnCharacterLevelUp?.Invoke(character);
            return true;
        }

        public bool PromoteCharacter(string characterId)
        {
            var character = ownedCharacters.FirstOrDefault(c => c.unitId == characterId);
            if (character == null)
                return false;

            // 레어도 상승 (최대 Legendary까지)
            if (character.rarity < Battle.Rarity.Legendary)
            {
                character.rarity = (Battle.Rarity)((int)character.rarity + 1);
                OnCharacterPromoted?.Invoke(character);
                return true;
            }

            return false;
        }

        public bool RemoveCharacter(string characterId)
        {
            var character = ownedCharacters.FirstOrDefault(c => c.unitId == characterId);
            if (character == null)
                return false;

            ownedCharacters.Remove(character);
            OnCharacterRemoved?.Invoke(character);
            return true;
        }

        public List<Battle.Unit> GetOwnedCharacters()
        {
            return new List<Battle.Unit>(ownedCharacters);
        }

        public List<Battle.Unit> GetAvailableCharacters()
        {
            return new List<Battle.Unit>(availableCharacters);
        }

        public List<Battle.Unit> GetCharactersByJobClass(Battle.JobClass jobClass)
        {
            return ownedCharacters.Where(c => c.jobClass == jobClass).ToList();
        }

        public List<Battle.Unit> GetCharactersByRarity(Battle.Rarity rarity)
        {
            return ownedCharacters.Where(c => c.rarity == rarity).ToList();
        }

        public Battle.Unit GetCharacterById(string characterId)
        {
            return ownedCharacters.FirstOrDefault(c => c.unitId == characterId);
        }

        public bool HasCharacter(string characterId)
        {
            return ownedCharacters.Any(c => c.unitId == characterId);
        }

        public int GetCharacterCount()
        {
            return ownedCharacters.Count;
        }

        public int GetCharacterCountByJobClass(Battle.JobClass jobClass)
        {
            return ownedCharacters.Count(c => c.jobClass == jobClass);
        }

        public int GetCharacterCountByRarity(Battle.Rarity rarity)
        {
            return ownedCharacters.Count(c => c.rarity == rarity);
        }

        private Battle.JobClass ConvertJobClass(Data.JobClass dataJobClass)
        {
            return (Battle.JobClass)dataJobClass;
        }

        private Battle.Rarity ConvertRarity(CharacterRarity dataRarity)
        {
            return (Battle.Rarity)dataRarity;
        }

        public string GetCharacterReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== Character Manager Report ===");
            report.AppendLine($"Total Owned Characters: {ownedCharacters.Count}");
            report.AppendLine($"Total Available Characters: {availableCharacters.Count}");
            
            foreach (var rarity in Enum.GetValues(typeof(Battle.Rarity)))
            {
                var count = GetCharacterCountByRarity((Battle.Rarity)rarity);
                if (count > 0)
                {
                    report.AppendLine($"{rarity}: {count}");
                }
            }
            
            foreach (var jobClass in Enum.GetValues(typeof(Battle.JobClass)))
            {
                var count = GetCharacterCountByJobClass((Battle.JobClass)jobClass);
                if (count > 0)
                {
                    report.AppendLine($"{jobClass}: {count}");
                }
            }
            
            return report.ToString();
        }
    }
} 