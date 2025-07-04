using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Data;
using GuildMaster.Battle;
using GuildMaster.Core;

namespace GuildMaster.Systems
{
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
                    }
                }
                return _instance;
            }
        }
        
        [Header("Character Database")]
        [SerializeField] private CharacterDatabase characterDatabase;
        
        // 캐릭터 풀
        private Dictionary<int, Queue<Unit>> characterPool;
        private int poolSize = 10;
        
        // 생성된 유닛 추적
        private List<Unit> activeUnits;
        
        // 이벤트
        public event Action<Unit> OnUnitCreated;
        public event Action<Unit> OnUnitReturned;
        public event Action<CharacterData> OnCharacterUnlocked;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            Initialize();
        }
        
        void Initialize()
        {
            characterPool = new Dictionary<int, Queue<Unit>>();
            activeUnits = new List<Unit>();
            
            // 데이터베이스 로드
            if (characterDatabase == null)
            {
                characterDatabase = Resources.Load<CharacterDatabase>("Data/CharacterDatabase");
            }
            
            if (characterDatabase != null)
            {
                characterDatabase.Initialize();
                Debug.Log($"Loaded {characterDatabase.characters.Count} characters from database.");
            }
            else
            {
                Debug.LogError("Character database not found!");
            }
        }
        
        // 캐릭터 데이터로부터 유닛 생성
        public Unit CreateUnit(string characterId, int level = 1)
        {
            CharacterData characterData = DataManager.Instance.GetCharacterData(characterId);
            if (characterData == null)
            {
                Debug.LogError($"Character with ID '{characterId}' not found!");
                return null;
            }

            return CreateUnitFromCharacterData(characterData, level);
        }
        
        public Unit CreateUnitFromCharacterData(CharacterData characterData, int level = 1)
        {
            if (characterData == null) return null;

            Unit unit = DataManager.Instance.CreateUnitFromData(characterData.id, level);
            return unit;
        }
        
        public CharacterData GetCharacterByName(string name)
        {
            var characters = DataManager.Instance.GetAllCharacters();
            return characters.FirstOrDefault(c => c.name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        
        public CharacterData GetRandomCharacter()
        {
            var characters = DataManager.Instance.GetAllCharacters();
            if (characters.Count == 0) return null;
            
            int randomIndex = UnityEngine.Random.Range(0, characters.Count);
            return characters[randomIndex];
        }
        
        public CharacterData GetRandomCharacterByRarity(GuildMaster.Battle.Rarity rarity)
        {
            var characters = DataManager.Instance.GetCharactersByRarity(rarity);
            if (characters.Count == 0) return null;
            
            int randomIndex = UnityEngine.Random.Range(0, characters.Count);
            return characters[randomIndex];
        }
        
        public Unit RecruitRandomCharacter()
        {
            CharacterData characterData = GetRandomCharacter();
            if (characterData == null) return null;

            return CreateUnitFromCharacterData(characterData);
        }
        
        // 이름으로 유닛 생성
        public Unit CreateUnitByName(string characterName)
        {
            CharacterData data = GetCharacterByName(characterName);
            if (data == null)
            {
                Debug.LogError($"Character with name '{characterName}' not found!");
                return null;
            }
            
            return CreateUnitFromCharacterData(data);
        }
        
        // 랜덤 유닛 생성
        public Unit CreateRandomUnit()
        {
            CharacterData data = GetRandomCharacter();
            if (data == null) return null;
            return CreateUnitFromCharacterData(data);
        }
        
        public Unit CreateRandomUnitByRarity(GuildMaster.Battle.Rarity rarity)
        {
            CharacterData data = GetRandomCharacterByRarity(rarity);
            if (data == null) return null;
            return CreateUnitFromCharacterData(data);
        }
        
        public Unit CreateRandomUnitByClass(JobClass jobClass)
        {
            var candidates = GetCharactersByClass(jobClass);
            if (candidates.Count == 0) return null;
            
            CharacterData data = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            return CreateUnitFromCharacterData(data);
        }
        
        // 유닛을 풀로 반환
        public void ReturnUnit(Unit unit)
        {
            if (unit == null || !activeUnits.Contains(unit)) return;
            
            activeUnits.Remove(unit);
            
            // 캐릭터 ID 찾기
            string characterId = GetCharacterIdFromUnit(unit);
            if (string.IsNullOrEmpty(characterId)) return;
            
            // 풀에 추가
            if (!characterPool.ContainsKey(int.Parse(characterId)))
            {
                characterPool[int.Parse(characterId)] = new Queue<Unit>();
            }
            
            if (characterPool[int.Parse(characterId)].Count < poolSize)
            {
                characterPool[int.Parse(characterId)].Enqueue(unit);
                OnUnitReturned?.Invoke(unit);
            }
        }
        
        // 유닛에서 캐릭터 ID 찾기 (string 타입으로 수정)
        string GetCharacterIdFromUnit(Unit unit)
        {
            // Unit의 unitId는 string 타입이므로 그대로 반환
            return unit.unitId;
        }
        
        // 가챠 시뮬레이션에서 string ID 사용
        public List<Unit> PerformGacha(int count, float[] rarityWeights = null)
        {
            var results = new List<Unit>();
            
            for (int i = 0; i < count; i++)
            {
                var rarity = GetRandomRarity(rarityWeights);
                var character = GetRandomCharacterByRarity(rarity);
                if (character != null)
                {
                    var unit = CreateUnitFromCharacterData(character);
                    if (unit != null)
                    {
                        results.Add(unit);
                    }
                }
            }
            
            return results;
        }
        
        Rarity GetRandomRarity(float[] weights)
        {
            float random = UnityEngine.Random.value;
            float cumulative = 0f;
            
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (random <= cumulative)
                {
                    return (Rarity)i;
                }
            }
            
            return Rarity.Common;
        }
        
        // 팀 빌딩 헬퍼
        public List<Unit> CreateBalancedTeam(int teamSize, int averageLevel = 1)
        {
            List<Unit> team = new List<Unit>();
            
            // 균형잡힌 팀 구성: 탱커 1-2, 딜러 2-3, 힐러 1, 서포트 0-1
            JobClass[] teamComposition = new JobClass[]
            {
                JobClass.Knight,     // 탱커
                JobClass.Warrior,    // 근거리 딜러
                JobClass.Ranger,     // 원거리 딜러
                JobClass.Priest,     // 힐러
                JobClass.Mage,       // 마법 딜러
                JobClass.Assassin    // 암살자
            };
            
            // 팀 사이즈에 맞게 조정
            for (int i = 0; i < teamSize && i < teamComposition.Length; i++)
            {
                Unit unit = CreateRandomUnitByClass(teamComposition[i]);
                if (unit != null)
                {
                    // 레벨 조정
                    while (unit.level < averageLevel)
                    {
                        unit.AddExperience(unit.experienceToNextLevel);
                    }
                    team.Add(unit);
                }
            }
            
            return team;
        }
        
        // 스토리 캐릭터 생성
        public Unit CreateStoryCharacter(string characterName, int overrideLevel = -1)
        {
            Unit unit = CreateUnitByName(characterName);
            if (unit != null && overrideLevel > 0)
            {
                // 스토리용 레벨 조정
                while (unit.level < overrideLevel)
                {
                    unit.AddExperience(unit.experienceToNextLevel);
                }
            }
            return unit;
        }
        
        // 보스 유닛 생성 (강화된 버전)
        public Unit CreateBossUnit(string characterId, float statMultiplier = 2f)
        {
            Unit unit = CreateUnit(characterId);
            if (unit == null) return null;
            
            // 스탯 강화
            unit.attackPower = Mathf.RoundToInt(unit.attackPower * statMultiplier);
            unit.defense = Mathf.RoundToInt(unit.defense * statMultiplier);
            unit.maxHP = Mathf.RoundToInt(unit.maxHP * statMultiplier);
            unit.maxMP = Mathf.RoundToInt(unit.maxMP * statMultiplier);
            unit.currentHP = unit.maxHP;
            unit.currentMP = unit.maxMP;
            
            return unit;
        }
        
        // 조회 메서드들
        public CharacterData GetCharacterData(string id)
        {
            return DataManager.Instance.GetCharacterData(id);
        }
        
        public List<CharacterData> GetAllCharacters()
        {
            return DataManager.Instance.GetAllCharacters();
        }
        
        public List<CharacterData> GetCharactersByClass(JobClass jobClass)
        {
            var characters = DataManager.Instance.GetCharactersByJobClass(jobClass);
            return characters ?? new List<CharacterData>();
        }
        
        public List<CharacterData> GetCharactersByRarity(GuildMaster.Battle.Rarity rarity)
        {
            var characters = DataManager.Instance.GetCharactersByRarity(rarity);
            return characters ?? new List<CharacterData>();
        }
        
        public List<CharacterData> GetCommonCharacters()
        {
            var characters = DataManager.Instance.GetCharactersByRarity(GuildMaster.Battle.Rarity.Common);
            return characters ?? new List<CharacterData>();
        }
        
        public List<Unit> GetActiveUnits()
        {
            return new List<Unit>(activeUnits);
        }
        
        public int GetPooledUnitCount(int characterId)
        {
            return characterPool.ContainsKey(characterId) ? characterPool[characterId].Count : 0;
        }
        
        // 데이터베이스 설정
        public void SetCharacterDatabase(CharacterDatabase database)
        {
            characterDatabase = database;
            if (characterDatabase != null)
            {
                characterDatabase.Initialize();
            }
        }

        // 새로운 모험가 생성 메서드들
        public Unit CreateAdventurer(string characterId, int level = 1)
        {
            CharacterDataSO charData = characterDatabase.GetCharacter(characterId);
            if (charData == null) return null;

            Unit newUnit = charData.CreateUnit();
            
            // 레벨 적용
            for (int i = 1; i < level; i++)
            {
                LevelUpUnit(newUnit);
            }
            
            return newUnit;
        }

        private void LevelUpUnit(Unit unit)
        {
            unit.Level++;
            // 스탯 증가 로직
        }

        // Method group 오류를 해결하기 위해 기존 메서드들을 수정
        public List<CharacterDataSO> GetRecommendedCharacters(JobClass preferredClass, int maxResults = 3)
        {
            var charactersOfClass = characterDatabase.GetCharactersByClass(preferredClass);
            if (charactersOfClass.Count == 0)
            {
                return new List<CharacterDataSO>();
            }

            return charactersOfClass.Take(maxResults).ToList();
        }

        public Unit RecruitRandomAdventurer(JobClass jobClass, Rarity rarity)
        {
            var availableCharacters = characterDatabase.GetCharactersByClass(jobClass);
            var filteredByRarity = availableCharacters.Where(c => c.rarity == rarity).ToList();
            
            if (filteredByRarity.Count == 0)
                return null;

            var randomChar = filteredByRarity[UnityEngine.Random.Range(0, filteredByRarity.Count)];
            return CreateAdventurer(randomChar.id, UnityEngine.Random.Range(1, 10));
        }

        public Unit CreateRandomAdventurer()
        {
            if (characterDatabase.characters.Count == 0) return null;
            
            var randomChar = characterDatabase.characters[UnityEngine.Random.Range(0, characterDatabase.characters.Count)];
            int randomLevel = UnityEngine.Random.Range(1, 10);
            
            return CreateAdventurer(randomChar.id, randomLevel);
        }

        public List<Unit> GetAdventurersByClass(JobClass jobClass)
        {
            var charactersOfClass = characterDatabase.GetCharactersByClass(jobClass);
            if (charactersOfClass.Count == 0)
            {
                return new List<Unit>();
            }

            return charactersOfClass.Select(c => c.CreateUnit()).ToList();
        }
    }
}