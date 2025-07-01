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
        private Dictionary<int, Queue<GuildMaster.Battle.Unit>> characterPool;
        private int poolSize = 10;
        
        // 생성된 유닛 추적
        private List<GuildMaster.Battle.Unit> activeUnits;
        
        // 이벤트
        public event Action<GuildMaster.Battle.Unit> OnUnitCreated;
        public event Action<GuildMaster.Battle.Unit> OnUnitReturned;
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
            characterPool = new Dictionary<int, Queue<GuildMaster.Battle.Unit>>();
            activeUnits = new List<GuildMaster.Battle.Unit>();
            
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
        public GuildMaster.Battle.Unit CreateUnit(string characterId, int level = 1)
        {
            GuildMaster.Data.CharacterData characterData = DataManager.Instance.GetCharacterData(characterId);
            if (characterData == null)
            {
                Debug.LogError($"Character with ID '{characterId}' not found!");
                return null;
            }

            return CreateUnitFromCharacterData(characterData, level);
        }
        
        public GuildMaster.Battle.Unit CreateUnitFromCharacterData(GuildMaster.Data.CharacterData characterData, int level = 1)
        {
            if (characterData == null) return null;

            GuildMaster.Battle.Unit unit = DataManager.Instance.CreateUnitFromData(characterData.id, level);
            return unit;
        }
        
        public GuildMaster.Data.CharacterData GetCharacterByName(string name)
        {
            var characters = DataManager.Instance.GetAllCharacters();
            return characters.FirstOrDefault(c => c.name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        
        public GuildMaster.Data.CharacterData GetRandomCharacter()
        {
            var characters = DataManager.Instance.GetAllCharacters();
            if (characters.Count == 0) return null;
            
            int randomIndex = UnityEngine.Random.Range(0, characters.Count);
            return characters[randomIndex];
        }
        
        public GuildMaster.Data.CharacterData GetRandomCharacterByRarity(GuildMaster.Battle.Rarity rarity)
        {
            var characters = DataManager.Instance.GetCharactersByRarity((GuildMaster.Data.CharacterRarity)rarity);
            if (characters.Count == 0) return null;
            
            int randomIndex = UnityEngine.Random.Range(0, characters.Count);
            return characters[randomIndex];
        }
        
        public GuildMaster.Battle.Unit RecruitRandomCharacter()
        {
            GuildMaster.Data.CharacterData characterData = GetRandomCharacter();
            if (characterData == null) return null;

            return CreateUnitFromCharacterData(characterData);
        }
        
        // 이름으로 유닛 생성
        public GuildMaster.Battle.Unit CreateUnitByName(string characterName)
        {
            GuildMaster.Data.CharacterData data = GetCharacterByName(characterName);
            if (data == null)
            {
                Debug.LogError($"Character with name '{characterName}' not found!");
                return null;
            }
            
            return CreateUnitFromCharacterData(data);
        }
        
        // 랜덤 유닛 생성
        public GuildMaster.Battle.Unit CreateRandomUnit()
        {
            GuildMaster.Data.CharacterData data = GetRandomCharacter();
            if (data == null) return null;
            return CreateUnitFromCharacterData(data);
        }
        
        public GuildMaster.Battle.Unit CreateRandomUnitByRarity(GuildMaster.Battle.Rarity rarity)
        {
            GuildMaster.Data.CharacterData data = GetRandomCharacterByRarity(rarity);
            if (data == null) return null;
            return CreateUnitFromCharacterData(data);
        }
        
        public GuildMaster.Battle.Unit CreateRandomUnitByClass(GuildMaster.Data.JobClass jobClass)
        {
            var candidates = GetCharactersByClass(jobClass);
            if (candidates.Count == 0) return null;
            
            GuildMaster.Data.CharacterData data = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            return CreateUnitFromCharacterData(data);
        }
        
        // 유닛을 풀로 반환
        public void ReturnUnit(GuildMaster.Battle.Unit unit)
        {
            if (unit == null || !activeUnits.Contains(unit)) return;
            
            activeUnits.Remove(unit);
            
            // 캐릭터 ID 찾기
            string characterId = GetCharacterIdFromUnit(unit);
            if (string.IsNullOrEmpty(characterId)) return;
            
            // 풀에 추가
            if (!characterPool.ContainsKey(int.Parse(characterId)))
            {
                characterPool[int.Parse(characterId)] = new Queue<GuildMaster.Battle.Unit>();
            }
            
            if (characterPool[int.Parse(characterId)].Count < poolSize)
            {
                characterPool[int.Parse(characterId)].Enqueue(unit);
                OnUnitReturned?.Invoke(unit);
            }
        }
        
        // 유닛에서 캐릭터 ID 찾기 (string 타입으로 수정)
        string GetCharacterIdFromUnit(GuildMaster.Battle.Unit unit)
        {
            // Unit의 unitId는 int 타입이므로 문자열로 변환
            return unit.unitId.ToString();
        }
        
        // 가챠 시뮬레이션에서 string ID 사용
        public List<GuildMaster.Battle.Unit> PerformGacha(int count, float[] rarityWeights = null)
        {
            var results = new List<GuildMaster.Battle.Unit>();
            
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
        
        GuildMaster.Battle.Rarity GetRandomRarity(float[] weights)
        {
            float random = UnityEngine.Random.value;
            float cumulative = 0f;
            
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (random <= cumulative)
                {
                    return (GuildMaster.Battle.Rarity)i;
                }
            }
            
            return GuildMaster.Battle.Rarity.Common;
        }
        
        // 팀 빌딩 헬퍼
        public List<GuildMaster.Battle.Unit> CreateBalancedTeam(int teamSize, int averageLevel = 1)
        {
            List<GuildMaster.Battle.Unit> team = new List<GuildMaster.Battle.Unit>();
            
            // 균형잡힌 팀 구성: 탱커 1-2, 딜러 2-3, 힐러 1, 서포트 0-1
            GuildMaster.Data.JobClass[] teamComposition = new GuildMaster.Data.JobClass[]
            {
                GuildMaster.Data.JobClass.Paladin,     // 탱커
                GuildMaster.Data.JobClass.Warrior,    // 근거리 딜러
                GuildMaster.Data.JobClass.Archer,     // 원거리 딜러
                GuildMaster.Data.JobClass.Priest,     // 힐러
                GuildMaster.Data.JobClass.Mage,       // 마법 딜러
                GuildMaster.Data.JobClass.Rogue    // 암살자
            };
            
            // 팀 사이즈에 맞게 조정
            for (int i = 0; i < teamSize && i < teamComposition.Length; i++)
            {
                GuildMaster.Battle.Unit unit = CreateRandomUnitByClass(teamComposition[i]);
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
        public GuildMaster.Battle.Unit CreateStoryCharacter(string characterName, int overrideLevel = -1)
        {
            GuildMaster.Battle.Unit unit = CreateUnitByName(characterName);
            if (unit != null && overrideLevel > 0)
            {
                LevelUpUnit(unit);
            }
            return unit;
        }
        
        public GuildMaster.Battle.Unit CreateBossUnit(string characterId, float statMultiplier = 2f)
        {
            var unit = CreateUnit(characterId);
            if (unit != null)
            {
                // 보스 스탯 강화
                unit.maxHP = (int)(unit.maxHP * statMultiplier);
                unit.currentHP = unit.maxHP;
                unit.attackPower *= statMultiplier;
                unit.defense *= statMultiplier;
                unit.magicPower *= statMultiplier;
            }
            return unit;
        }
        
        // 조회 메서드들
        public GuildMaster.Data.CharacterData GetCharacterData(string id)
        {
            return DataManager.Instance.GetCharacterData(id);
        }
        
        public List<GuildMaster.Data.CharacterData> GetAllCharacters()
        {
            return DataManager.Instance.GetAllCharacters();
        }
        
        public List<GuildMaster.Data.CharacterData> GetCharactersByClass(GuildMaster.Data.JobClass jobClass)
        {
            return DataManager.Instance.GetCharactersByJobClass(jobClass);
        }
        
        public List<GuildMaster.Data.CharacterData> GetCharactersByRarity(GuildMaster.Data.CharacterRarity rarity)
        {
            return DataManager.Instance.GetCharactersByRarity(rarity);
        }
        
        public List<GuildMaster.Data.CharacterData> GetCommonCharacters()
        {
            return DataManager.Instance.GetCharactersByRarity(GuildMaster.Data.CharacterRarity.Common);
        }
        
        public List<GuildMaster.Battle.Unit> GetActiveUnits()
        {
            return new List<GuildMaster.Battle.Unit>(activeUnits);
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
        public GuildMaster.Battle.Unit CreateAdventurer(string characterId, int level = 1)
        {
            return CreateUnit(characterId, level);
        }

        private void LevelUpUnit(GuildMaster.Battle.Unit unit)
        {
            // 레벨업 로직
        }

        // Method group 오류를 해결하기 위해 기존 메서드들을 수정
        public List<GuildMaster.Data.CharacterData> GetRecommendedCharacters(GuildMaster.Data.JobClass preferredClass, int maxResults = 3)
        {
            var candidates = GetCharactersByClass(preferredClass);
            return candidates.Take(maxResults).ToList();
        }

        public GuildMaster.Battle.Unit RecruitRandomAdventurer(GuildMaster.Data.JobClass jobClass, GuildMaster.Battle.Rarity rarity)
        {
            var candidates = GetCharactersByClass(jobClass)
                .Where(c => (GuildMaster.Battle.Rarity)c.rarity == rarity)
                .ToList();
            
            if (candidates.Count == 0) return null;
            
            var selectedData = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            return CreateUnitFromCharacterData(selectedData);
        }

        public GuildMaster.Battle.Unit CreateRandomAdventurer()
        {
            return CreateRandomUnit();
        }

        public List<GuildMaster.Battle.Unit> GetAdventurersByClass(GuildMaster.Data.JobClass jobClass)
        {
            return activeUnits.Where(u => (GuildMaster.Data.JobClass)u.JobClass == jobClass).ToList();
        }
        
        public GuildMaster.Battle.Unit CreateCharacterByIndex(int index)
        {
            var allCharacters = GetAllCharacters();
            if (index < 0 || index >= allCharacters.Count) return null;
            
            return CreateUnitFromCharacterData(allCharacters[index]);
        }


    }
}