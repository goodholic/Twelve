using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using GuildMaster.Game;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 게임 데이터 관리자 (전투 시스템용 일부 구현)
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        private static DataManager instance;
        public static DataManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<DataManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("DataManager");
                        instance = go.AddComponent<DataManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        private Dictionary<string, Character> characterDatabase = new Dictionary<string, Character>();
        private bool isDataLoaded = false;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadGameData();
        }
        
        private void LoadGameData()
        {
            // CSV 파일에서 캐릭터 데이터 로드
            LoadCharacterData();
            isDataLoaded = true;
        }
        
        private void LoadCharacterData()
        {
            // 실제로는 CSV 파일을 읽어야 하지만, 여기서는 샘플 데이터 생성
            CreateSampleCharacters();
        }
        
        private void CreateSampleCharacters()
        {
            // 각 직업별로 샘플 캐릭터 생성
            var jobClasses = System.Enum.GetValues(typeof(JobClass)).Cast<JobClass>().ToList();
            var rarities = System.Enum.GetValues(typeof(CharacterRarity)).Cast<CharacterRarity>().ToList();
            
            int characterId = 1;
            
            foreach (var jobClass in jobClasses)
            {
                foreach (var rarity in rarities)
                {
                    Character character = new Character
                    {
                        characterID = $"char_{characterId:D3}",
                        characterName = $"{JobClassSystem.GetJobClassName(jobClass)} {characterId}",
                        jobClass = jobClass,
                        level = GetLevelByRarity(rarity),
                        rarity = rarity,
                        
                        // 레어도에 따른 기본 스탯
                        baseHP = GetBaseStatByRarity(100, rarity),
                        baseMP = GetBaseStatByRarity(50, rarity),
                        baseAttack = GetBaseStatByRarity(20, rarity),
                        baseDefense = GetBaseStatByRarity(15, rarity),
                        baseMagicPower = GetBaseStatByRarity(25, rarity),
                        baseSpeed = GetBaseStatByRarity(10, rarity),
                        baseCritRate = 0.05f + (int)rarity * 0.02f,
                        baseCritDamage = 1.5f + (int)rarity * 0.1f,
                        baseAccuracy = 0.9f + (int)rarity * 0.02f,
                        baseEvasion = 0.05f + (int)rarity * 0.02f,
                        
                        skillIDs = new List<int> { 101, 102, 103 },
                        description = $"{rarity} 등급의 {JobClassSystem.GetJobClassName(jobClass)}"
                    };
                    
                    characterDatabase[character.characterID] = character;
                    characterId++;
                    
                    // 각 직업당 2개의 캐릭터만 생성 (테스트용)
                    if (characterId % 2 == 0) break;
                }
            }
        }
        
        private int GetLevelByRarity(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.Common: return 1;
                case CharacterRarity.Uncommon: return 3;
                case CharacterRarity.Rare: return 5;
                case CharacterRarity.Epic: return 7;
                case CharacterRarity.Legendary: return 10;
                default: return 1;
            }
        }
        
        private float GetBaseStatByRarity(float baseStat, CharacterRarity rarity)
        {
            float multiplier = 1f + (int)rarity * 0.3f;
            return baseStat * multiplier;
        }
        
        /// <summary>
        /// 모든 캐릭터 가져오기
        /// </summary>
        public List<Character> GetAllCharacters()
        {
            return characterDatabase.Values.ToList();
        }
        
        /// <summary>
        /// 특정 캐릭터 가져오기
        /// </summary>
        public Character GetCharacter(string characterID)
        {
            if (characterDatabase.ContainsKey(characterID))
                return characterDatabase[characterID];
            
            Debug.LogWarning($"Character not found: {characterID}");
            return null;
        }
        
        /// <summary>
        /// 직업별 캐릭터 가져오기
        /// </summary>
        public List<Character> GetCharactersByJob(JobClass jobClass)
        {
            return characterDatabase.Values.Where(c => c.jobClass == jobClass).ToList();
        }
        
        /// <summary>
        /// 레어도별 캐릭터 가져오기
        /// </summary>
        public List<Character> GetCharactersByRarity(CharacterRarity rarity)
        {
            return characterDatabase.Values.Where(c => c.rarity == rarity).ToList();
        }
    }
}