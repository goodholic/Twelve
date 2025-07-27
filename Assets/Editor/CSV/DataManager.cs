using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using GuildMaster.Data;
using TacticalTileGame.Data;

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
        
        [Header("통합 데이터베이스")]
        [SerializeField] private CharacterDatabaseSO mainDatabase;
        [SerializeField] private string mainDatabasePath = "CharacterDatabase";
        
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
            // 통합 데이터베이스에서 캐릭터 데이터 로드
            LoadMainDatabase();
            isDataLoaded = true;
        }
        
        private void LoadMainDatabase()
        {
            if (mainDatabase == null)
            {
                mainDatabase = Resources.Load<CharacterDatabaseSO>(mainDatabasePath);
                if (mainDatabase == null)
                {
                    Debug.LogError($"CharacterDatabaseSO not found at path: {mainDatabasePath}");
                    return;
                }
            }
            
            mainDatabase.Initialize();
            Debug.Log($"Main database loaded with {mainDatabase.characters.Count} characters");
        }
        
        // 구 CreateSampleCharacters 메서드 제거됨 - 통합 데이터베이스 사용
        
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
        /// 모든 캐릭터 가져오기 (통합 데이터베이스 사용)
        /// </summary>
        public List<CSVCharacter> GetAllCharacters()
        {
            if (mainDatabase == null) return new List<CSVCharacter>();
            return mainDatabase.GetAllCSVCharacters();
        }
        
        /// <summary>
        /// 특정 캐릭터 가져오기 (통합 데이터베이스 사용)
        /// </summary>
        public CSVCharacter GetCharacter(string characterID)
        {
            if (mainDatabase == null)
            {
                Debug.LogError("Main database is null");
                return null;
            }
            
            var character = mainDatabase.GetCSVCharacter(characterID);
            if (character == null)
            {
                Debug.LogWarning($"Character with ID {characterID} not found in main database");
            }
            return character;
        }
        
        /// <summary>
        /// 직업별 캐릭터 가져오기 (통합 데이터베이스 사용)
        /// </summary>
        public List<CSVCharacter> GetCharactersByJob(JobClass jobClass)
        {
            if (mainDatabase == null) return new List<CSVCharacter>();
            return mainDatabase.GetAllCSVCharacters().Where(c => c.jobClass == jobClass).ToList();
        }
        
        /// <summary>
        /// 레어도별 캐릭터 가져오기 (통합 데이터베이스 사용)
        /// </summary>
        public List<CSVCharacter> GetCharactersByRarity(CharacterRarity rarity)
        {
            if (mainDatabase == null) return new List<CSVCharacter>();
            return mainDatabase.GetAllCSVCharacters().Where(c => c.rarity == rarity).ToList();
        }
    }
}