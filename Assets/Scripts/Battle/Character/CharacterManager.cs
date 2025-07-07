using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Data;
using GuildMaster.Battle;
using GuildMaster.Core;
using JobClass = GuildMaster.Battle.JobClass;
using Unit = GuildMaster.Battle.UnitStatus;
using Rarity = GuildMaster.Data.Rarity;

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
        private Dictionary<int, Queue<UnitStatus>> characterPool;
        private int poolSize = 10;
        
        // 생성된 유닛 추적
        private List<UnitStatus> activeUnits;
        
        // 이벤트
        public event Action<UnitStatus> OnUnitCreated;
        public event Action<UnitStatus> OnUnitReturned;
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
            characterPool = new Dictionary<int, Queue<UnitStatus>>();
            activeUnits = new List<UnitStatus>();
            
            // 데이터베이스 로드
            if (characterDatabase == null)
            {
                characterDatabase = Resources.Load<CharacterDatabase>("Data/CharacterDatabase");
                
                // Resources 폴더에 없으면 다른 경로에서 시도
                if (characterDatabase == null)
                {
                    // Unity 에디터에서는 AssetDatabase를 사용할 수 있지만, 
                    // 런타임에서는 Resources 폴더에 있어야 함
                    Debug.LogWarning("CharacterDatabase not found in Resources/Data/. Please move CharacterDatabase.asset to Assets/Resources/Data/ folder.");
                }
            }
            
            if (characterDatabase != null)
            {
                characterDatabase.Initialize();
                Debug.Log($"Loaded {characterDatabase.characters.Count} characters from database.");
                
                // If database is empty, create default characters
                if (characterDatabase.characters.Count == 0)
                {
                    Debug.LogWarning("Character database is empty. Creating default characters...");
                    CreateDefaultCharacters();
                }
            }
            else
            {
                Debug.LogError("Character database not found!");
            }
        }
        
        // 캐릭터 데이터로부터 유닛 생성
        public UnitStatus CreateUnit(string characterId, int level = 1)
        {
            // DataManager 타입이 제거되어 주석 처리
            // CharacterData characterData = DataManager.Instance.GetCharacterData(characterId);
            // if (characterData == null)
            // {
            //     Debug.LogError($"Character with ID '{characterId}' not found!");
            //     return null;
            // }
            //
            // return CreateUnitFromCharacterData(characterData, level);
            
            // 임시로 null 반환
            Debug.LogError($"DataManager removed - Cannot create unit with ID '{characterId}'");
            return null;
        }
        
        public UnitStatus CreateUnitFromCharacterData(CharacterData characterData, int level = 1)
        {
            if (characterData == null) return null;

            // DataManager 타입이 제거되어 주석 처리
            // Unit unit = DataManager.Instance.CreateUnitFromData(characterData.id, level);
            // return unit;
            
            // 직접 Unit 생성
            var unit = new UnitStatus(characterData.name, level, characterData.jobClass);
            unit.unitId = characterData.id;
            unit.characterId = characterData.id;
            unit.rarity = ConvertCharacterRarityToRarity(characterData.rarity);
            unit.maxHP = characterData.baseHP;
            unit.maxMP = characterData.baseMP;
            unit.attackPower = characterData.baseAttack;
            unit.defense = characterData.baseDefense;
            unit.magicPower = characterData.baseMagicPower;
            unit.speed = characterData.baseSpeed;
            unit.criticalRate = characterData.critRate;
            unit.accuracy = characterData.accuracy;
            unit.currentHP = unit.maxHP;
            unit.currentMP = unit.maxMP;
            return unit;
        }
        
        public CharacterData GetCharacterByName(string name)
        {
            // DataManager 타입이 제거되어 주석 처리
            // var characters = DataManager.Instance.GetAllCharacters();
            // return characters.FirstOrDefault(c => c.name.Equals(name, StringComparison.OrdinalIgnoreCase));
            
            // characterDatabase에서 직접 검색
            if (characterDatabase == null || characterDatabase.characters == null) return null;
            return characterDatabase.characters.FirstOrDefault(c => c.name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        
        public CharacterData GetRandomCharacter()
        {
            // DataManager 타입이 제거되어 주석 처리
            // var characters = DataManager.Instance.GetAllCharacters();
            // if (characters.Count == 0) return null;
            // 
            // int randomIndex = UnityEngine.Random.Range(0, characters.Count);
            // return characters[randomIndex];
            
            // characterDatabase에서 직접 가져오기
            if (characterDatabase == null || characterDatabase.characters == null || characterDatabase.characters.Count == 0) return null;
            int randomIndex = UnityEngine.Random.Range(0, characterDatabase.characters.Count);
            return characterDatabase.characters[randomIndex];
        }
        
        public CharacterData GetRandomCharacterByRarity(GuildMaster.Data.Rarity rarity)
        {
            // DataManager 타입이 제거되어 주석 처리
            // var characters = DataManager.Instance.GetCharactersByRarity(ConvertRarityToCharacterRarity(rarity));
            // if (characters.Count == 0) return null;
            // 
            // int randomIndex = UnityEngine.Random.Range(0, characters.Count);
            // return characters[randomIndex];
            
            // characterDatabase에서 직접 필터링
            if (characterDatabase == null || characterDatabase.characters == null) return null;
            var characterRarity = ConvertRarityToCharacterRarity(rarity);
            var characters = characterDatabase.characters.Where(c => c.rarity == characterRarity).ToList();
            if (characters.Count == 0) return null;
            int randomIndex = UnityEngine.Random.Range(0, characters.Count);
            return characters[randomIndex];
        }
        
        public UnitStatus RecruitRandomCharacter()
        {
            CharacterData characterData = GetRandomCharacter();
            if (characterData == null) return null;

            return CreateUnitFromCharacterData(characterData);
        }
        
        // 이름으로 유닛 생성
        public UnitStatus CreateUnitByName(string characterName)
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
        public UnitStatus CreateRandomUnit()
        {
            CharacterData data = GetRandomCharacter();
            if (data == null) return null;
            return CreateUnitFromCharacterData(data);
        }
        
        public UnitStatus CreateRandomUnitByRarity(GuildMaster.Data.Rarity rarity)
        {
            CharacterData data = GetRandomCharacterByRarity(rarity);
            if (data == null) return null;
            return CreateUnitFromCharacterData(data);
        }
        
        public UnitStatus CreateRandomUnitByClass(JobClass jobClass)
        {
            var candidates = GetCharactersByClass(jobClass);
            if (candidates.Count == 0) return null;
            
            CharacterData data = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            return CreateUnitFromCharacterData(data);
        }
        
        // 유닛을 풀로 반환
        public void ReturnUnit(UnitStatus unit)
        {
            if (unit == null || !activeUnits.Contains(unit)) return;
            
            activeUnits.Remove(unit);
            
            // 캐릭터 ID 찾기
            string characterId = GetCharacterIdFromUnit(unit);
            if (string.IsNullOrEmpty(characterId)) return;
            
            // 풀에 추가
            if (!characterPool.ContainsKey(int.Parse(characterId)))
            {
                characterPool[int.Parse(characterId)] = new Queue<UnitStatus>();
            }
            
            if (characterPool[int.Parse(characterId)].Count < poolSize)
            {
                characterPool[int.Parse(characterId)].Enqueue(unit);
                OnUnitReturned?.Invoke(unit);
            }
        }
        
        // 유닛에서 캐릭터 ID 찾기 (string 타입으로 수정)
        string GetCharacterIdFromUnit(UnitStatus unit)
        {
            // Unit의 unitId는 string 타입이므로 그대로 반환
            return unit.unitId;
        }
        
        // 가챠 시뮬레이션에서 string ID 사용
        public List<UnitStatus> PerformGacha(int count, float[] rarityWeights = null)
        {
            var results = new List<UnitStatus>();
            
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
        public List<UnitStatus> CreateBalancedTeam(int teamSize, int averageLevel = 1)
        {
            List<UnitStatus> team = new List<UnitStatus>();
            
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
                UnitStatus unit = CreateRandomUnitByClass(teamComposition[i]);
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
        public UnitStatus CreateStoryCharacter(string characterName, int overrideLevel = -1)
        {
            UnitStatus unit = CreateUnitByName(characterName);
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
        public UnitStatus CreateBossUnit(string characterId, float statMultiplier = 2f)
        {
            UnitStatus unit = CreateUnit(characterId);
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
            // DataManager 타입이 제거되어 주석 처리
            // return DataManager.Instance.GetCharacterData(id);
            
            // characterDatabase에서 직접 검색
            if (characterDatabase == null || characterDatabase.characters == null) return null;
            return characterDatabase.characters.FirstOrDefault(c => c.id == id);
        }
        
        public List<CharacterData> GetAllCharacters()
        {
            // DataManager 타입이 제거되어 주석 처리
            // return DataManager.Instance.GetAllCharacters();
            
            // characterDatabase에서 직접 반환
            if (characterDatabase == null || characterDatabase.characters == null) return new List<CharacterData>();
            return new List<CharacterData>(characterDatabase.characters);
        }
        
        public List<CharacterData> GetCharactersByClass(JobClass jobClass)
        {
            // DataManager 타입이 제거되어 주석 처리
            // var characters = DataManager.Instance.GetCharactersByJobClass(jobClass);
            // return characters ?? new List<CharacterData>();
            
            // characterDatabase에서 직접 필터링
            if (characterDatabase == null || characterDatabase.characters == null) return new List<CharacterData>();
            return characterDatabase.characters.Where(c => c.jobClass == jobClass).ToList();
        }
        
        public List<CharacterData> GetCharactersByRarity(GuildMaster.Data.Rarity rarity)
        {
            // DataManager 타입이 제거되어 주석 처리
            // var characters = DataManager.Instance.GetCharactersByRarity(ConvertRarityToCharacterRarity(rarity));
            // return characters ?? new List<CharacterData>();
            
            // characterDatabase에서 직접 필터링
            if (characterDatabase == null || characterDatabase.characters == null) return new List<CharacterData>();
            var characterRarity = ConvertRarityToCharacterRarity(rarity);
            return characterDatabase.characters.Where(c => c.rarity == characterRarity).ToList();
        }
        
        public List<CharacterData> GetCommonCharacters()
        {
            // DataManager 타입이 제거되어 주석 처리
            // var characters = DataManager.Instance.GetCharactersByRarity(CharacterRarity.Common);
            // return characters ?? new List<CharacterData>();
            
            // characterDatabase에서 직접 필터링
            if (characterDatabase == null || characterDatabase.characters == null) return new List<CharacterData>();
            return characterDatabase.characters.Where(c => c.rarity == CharacterRarity.Common).ToList();
        }
        
        public List<UnitStatus> GetActiveUnits()
        {
            return new List<UnitStatus>(activeUnits);
        }
        
        private void CreateDefaultCharacters()
        {
            // Create default characters if database is empty
            if (characterDatabase == null || characterDatabase.characters == null)
            {
                Debug.LogError("Cannot create default characters - database is null!");
                return;
            }
            
            // Clear existing characters
            characterDatabase.characters.Clear();
            
            // Create default warrior
            CharacterData warrior = new CharacterData
            {
                id = "char_warrior_001",
                name = "Basic Warrior",
                jobClass = JobClass.Warrior,
                rarity = CharacterRarity.Common,
                level = 1,
                baseHP = 100,
                baseMP = 50,
                baseAttack = 15,
                baseDefense = 10,
                baseMagicPower = 5,
                baseSpeed = 8,
                critRate = 0.15f,
                critDamage = 1.5f,
                accuracy = 0.9f,
                evasion = 0.05f
            };
            characterDatabase.characters.Add(warrior);
            
            // Create default mage
            CharacterData mage = new CharacterData
            {
                id = "char_mage_001",
                name = "Basic Mage",
                jobClass = JobClass.Mage,
                rarity = CharacterRarity.Common,
                level = 1,
                baseHP = 60,
                baseMP = 100,
                baseAttack = 5,
                baseDefense = 5,
                baseMagicPower = 20,
                baseSpeed = 10,
                critRate = 0.2f,
                critDamage = 1.8f,
                accuracy = 0.95f,
                evasion = 0.08f
            };
            characterDatabase.characters.Add(mage);
            
            // Create default priest
            CharacterData priest = new CharacterData
            {
                id = "char_priest_001",
                name = "Basic Priest",
                jobClass = JobClass.Priest,
                rarity = CharacterRarity.Common,
                level = 1,
                baseHP = 70,
                baseMP = 80,
                baseAttack = 8,
                baseDefense = 8,
                baseMagicPower = 15,
                baseSpeed = 9,
                critRate = 0.05f,
                critDamage = 1.3f,
                accuracy = 0.9f,
                evasion = 0.06f
            };
            characterDatabase.characters.Add(priest);
            
            // Create default ranger
            CharacterData ranger = new CharacterData
            {
                id = "char_ranger_001",
                name = "Basic Ranger",
                jobClass = JobClass.Ranger,
                rarity = CharacterRarity.Common,
                level = 1,
                baseHP = 85,
                baseMP = 70,
                baseAttack = 16,
                baseDefense = 8,
                baseMagicPower = 5,
                baseSpeed = 12,
                critRate = 0.25f,
                critDamage = 1.7f,
                accuracy = 0.98f,
                evasion = 0.1f
            };
            characterDatabase.characters.Add(ranger);
            
            Debug.Log($"Created {characterDatabase.characters.Count} default characters.");
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
        public UnitStatus CreateAdventurer(string characterId, int level = 1)
        {
            // DataManager 타입이 제거되어 주석 처리
            // var charData = DataManager.Instance.GetCharacterData(characterId);
            // if (charData == null) return null;
            //
            // Unit newUnit = DataManager.Instance.CreateUnit(charData);
            
            var charData = GetCharacterData(characterId);
            if (charData == null) return null;
            
            UnitStatus newUnit = CreateUnitFromCharacterData(charData, level);
            
            // 레벨 적용
            for (int i = 1; i < level; i++)
            {
                LevelUpUnit(newUnit);
            }
            
            return newUnit;
        }

        private void LevelUpUnit(UnitStatus unit)
        {
            unit.Level++;
            // 스탯 증가 로직
        }

        // Method group 오류를 해결하기 위해 기존 메서드들을 수정
        public List<CharacterData> GetRecommendedCharacters(JobClass preferredClass, int maxResults = 3)
        {
            // DataManager 타입이 제거되어 주석 처리
            // var charactersOfClass = DataManager.Instance.GetCharactersByJobClass(preferredClass);
            // if (charactersOfClass == null || charactersOfClass.Count == 0)
            // {
            //     return new List<CharacterData>();
            // }
            //
            // return charactersOfClass.Take(maxResults).ToList();
            
            var charactersOfClass = GetCharactersByClass(preferredClass);
            if (charactersOfClass == null || charactersOfClass.Count == 0)
            {
                return new List<CharacterData>();
            }

            return charactersOfClass.Take(maxResults).ToList();
        }

        public UnitStatus RecruitRandomAdventurer(JobClass jobClass, Rarity rarity)
        {
            // DataManager 타입이 제거되어 주석 처리
            // var availableCharacters = DataManager.Instance.GetCharactersByJobClass(jobClass);
            // var filteredByRarity = availableCharacters.Where(c => c.rarity == ConvertRarityToCharacterRarity(rarity)).ToList();
            
            var availableCharacters = GetCharactersByClass(jobClass);
            var filteredByRarity = availableCharacters.Where(c => c.rarity == ConvertRarityToCharacterRarity(rarity)).ToList();
            
            if (filteredByRarity == null || filteredByRarity.Count == 0)
                return null;

            var randomChar = filteredByRarity[UnityEngine.Random.Range(0, filteredByRarity.Count)];
            return CreateAdventurer(randomChar.id, UnityEngine.Random.Range(1, 10));
        }

        public UnitStatus CreateRandomAdventurer()
        {
            // DataManager 타입이 제거되어 주석 처리
            // var allCharacters = DataManager.Instance.GetAllCharacters();
            // if (allCharacters.Count == 0) return null;
            // 
            // var randomChar = allCharacters[UnityEngine.Random.Range(0, allCharacters.Count)];
            // int randomLevel = UnityEngine.Random.Range(1, 10);
            // 
            // return CreateAdventurer(randomChar.id, randomLevel);
            
            var allCharacters = GetAllCharacters();
            if (allCharacters.Count == 0) return null;
            
            var randomChar = allCharacters[UnityEngine.Random.Range(0, allCharacters.Count)];
            int randomLevel = UnityEngine.Random.Range(1, 10);
            
            return CreateAdventurer(randomChar.id, randomLevel);
        }

        public List<UnitStatus> GetAdventurersByClass(JobClass jobClass)
        {
            var charactersOfClass = characterDatabase.GetCharactersByClass(jobClass);
            if (charactersOfClass.Count == 0)
            {
                return new List<UnitStatus>();
            }

            // DataManager 타입이 제거되어 주석 처리
            // return charactersOfClass.Select(c => DataManager.Instance.CreateUnit(c)).ToList();
            
            return charactersOfClass.Select(c => CreateUnitFromCharacterData(c)).ToList();
        }
        
        private CharacterRarity ConvertRarityToCharacterRarity(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common:
                    return CharacterRarity.Common;
                case Rarity.Uncommon:
                    return CharacterRarity.Uncommon;
                case Rarity.Rare:
                    return CharacterRarity.Rare;
                case Rarity.Epic:
                    return CharacterRarity.Epic;
                case Rarity.Legendary:
                    return CharacterRarity.Legendary;
                case Rarity.Mythic:
                    // CharacterRarity에는 Mythic이 없으므로 Legendary로 매핑
                    return CharacterRarity.Legendary;
                default:
                    return CharacterRarity.Common;
            }
        }
        
        private Rarity ConvertCharacterRarityToRarity(CharacterRarity characterRarity)
        {
            switch (characterRarity)
            {
                case CharacterRarity.Common:
                    return Rarity.Common;
                case CharacterRarity.Uncommon:
                    return Rarity.Uncommon;
                case CharacterRarity.Rare:
                    return Rarity.Rare;
                case CharacterRarity.Epic:
                    return Rarity.Epic;
                case CharacterRarity.Legendary:
                    return Rarity.Legendary;
                default:
                    return Rarity.Common;
            }
        }
        
        // GetCharacterStats method for compatibility
        public CharacterStats GetCharacterStats(string characterId)
        {
            // DataManager 타입이 제거되어 주석 처리
            // var characterData = DataManager.Instance.GetCharacterData(characterId);
            // if (characterData == null)
            // {
            //     return new CharacterStats
            //     {
            //         hp = 100,
            //         attack = 10,
            //         defense = 5,
            //         speed = 10
            //     };
            // }
            
            var characterData = GetCharacterData(characterId);
            if (characterData == null)
            {
                return new CharacterStats
                {
                    hp = 100,
                    attack = 10,
                    defense = 5,
                    speed = 10
                };
            }
            
            return new CharacterStats
            {
                hp = characterData.baseHP,
                attack = characterData.baseAttack,
                defense = characterData.baseDefense,
                speed = characterData.baseSpeed,
                critRate = characterData.critRate,
                critDamage = characterData.critDamage
            };
        }
        
        // CreateCharacterByIndex method for compatibility
        public UnitStatus CreateCharacterByIndex(int characterIndex)
        {
            // DataManager 타입이 제거되어 주석 처리
            // var allCharacters = DataManager.Instance.GetAllCharacters();
            // if (allCharacters == null || characterIndex < 0 || characterIndex >= allCharacters.Count)
            // {
            //     Debug.LogError($"Character index {characterIndex} is out of range!");
            //     return null;
            // }
            // 
            // var characterData = allCharacters[characterIndex];
            // return CreateUnitFromCharacterData(characterData);
            
            var allCharacters = GetAllCharacters();
            if (allCharacters == null || characterIndex < 0 || characterIndex >= allCharacters.Count)
            {
                Debug.LogError($"Character index {characterIndex} is out of range!");
                return null;
            }
            
            var characterData = allCharacters[characterIndex];
            return CreateUnitFromCharacterData(characterData);
        }
        
        public class CharacterStats
        {
            public float hp;
            public float attack;
            public float defense;
            public float speed;
            public float critRate;
            public float critDamage;
        }
    }
}