using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle;
using TacticalTileGame.Data;
// using Unit = GuildMaster.Battle.Unit; // Not needed - Unit is already in the namespace

namespace GuildMaster.Data
{
    /// <summary>
    /// 통합 캐릭터 데이터베이스 - 모든 캐릭터 데이터의 중앙 관리소
    /// UI, 전술게임, CSV 시스템이 모두 이를 참조합니다
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDatabaseSO", menuName = "GuildMaster/Data/Character Database SO")]
    public class CharacterDatabaseSO : ScriptableObject
    {
            [SerializeField] public List<CharacterDataSO> characters = new List<CharacterDataSO>();
    [SerializeField] public List<CharacterData> tacticalCharacters = new List<CharacterData>(); // TacticalCharacterDataSO → CharacterData 통합
        
        private Dictionary<string, CharacterDataSO> characterLookup;
        private Dictionary<string, CharacterData> tacticalLookup; // TacticalCharacterDataSO → CharacterData 통합
        
        public void Initialize()
        {
            Debug.Log($"통합 CharacterDatabase initialized - Characters: {characters.Count}, Tactical: {tacticalCharacters.Count}");
            
            // CharacterDataSO 룩업 테이블 생성
            characterLookup = new Dictionary<string, CharacterDataSO>();
            foreach (var character in characters)
            {
                if (!string.IsNullOrEmpty(character.id) && !characterLookup.ContainsKey(character.id))
                {
                    characterLookup.Add(character.id, character);
                }
            }
            
                    // CharacterData 룩업 테이블 생성 (통합된 전술 캐릭터)
        tacticalLookup = new Dictionary<string, CharacterData>();
        foreach (var tactical in tacticalCharacters)
        {
            if (!string.IsNullOrEmpty(tactical.characterId) && !tacticalLookup.ContainsKey(tactical.characterId))
            {
                tacticalLookup.Add(tactical.characterId, tactical);
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
        
        // === 전술 캐릭터 관련 메서드들 ===
        public CharacterData GetTacticalCharacter(string id)
        {
            if (tacticalLookup != null && tacticalLookup.TryGetValue(id, out CharacterData tactical))
                return tactical;
            return tacticalCharacters.FirstOrDefault(c => c.characterId == id);
        }
        
        public List<CharacterData> GetTacticalCharactersByClass(TacticalTileGame.Data.CharacterClass characterClass)
        {
            return tacticalCharacters.Where(c => c.jobClass == (JobClass)characterClass).ToList();
        }
        
        public List<CharacterData> GetTacticalCharactersByRarity(TacticalTileGame.Data.CharacterRarity rarity)
        {
            return tacticalCharacters.Where(c => c.tacticalRarity == rarity).ToList();
        }
        
        public List<CharacterData> GetAllTacticalCharacters()
        {
            return new List<CharacterData>(tacticalCharacters);
        }
        
        // === CSV 데이터와의 호환성을 위한 메서드들 ===
        public CSVCharacter GetCSVCharacter(string id)
        {
            var charData = GetCharacter(id);
            if (charData != null)
            {
                return CharacterConverter.FromCharacterDataSO(charData);
            }
            return null;
        }
        
        public List<CSVCharacter> GetAllCSVCharacters()
        {
            return characters.Select(c => CharacterConverter.FromCharacterDataSO(c)).ToList();
        }
    }

    [System.Serializable]
    public class CharacterDataSO : ScriptableObject
    {
        public string id;
        public string characterName;
        public JobClass jobClass;
        public int baseLevel;
        public int level = 1; // CSV Editor에서 사용하는 level 속성
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

        // NOTE: CreateUnit method removed as Unit class does not exist.
        // Use CharacterUnit component instead when creating units in battle.
    }
}