using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Battle;
using GuildMaster.Core;
using GuildMaster.Data;
using JobClass = GuildMaster.Battle.JobClass;
using Unit = GuildMaster.Battle.Unit;
using Rarity = GuildMaster.Data.Rarity;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 18명의 정예 캐릭터 관리 시스템
    /// 캐릭터 정보 열람, 육성, 부대 편성 등을 관리
    /// </summary>
    public class CharacterManagementSystem : MonoBehaviour
    {
        private static CharacterManagementSystem _instance;
        public static CharacterManagementSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CharacterManagementSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CharacterManagementSystem");
                        _instance = go.AddComponent<CharacterManagementSystem>();
                    }
                }
                return _instance;
            }
        }
        
        [Header("Character Settings")]
        public const int MAX_CHARACTERS = 18;  // 정예 18명 제한
        public const int SQUAD_SIZE = 9;       // 부대당 9명
        public const int SQUAD_COUNT = 2;      // 2개 부대
        
        // 보유 캐릭터 관리
        private Dictionary<string, Unit> ownedCharacters;
        private List<Unit> characterRoster;
        
        // 부대 편성
        private Squad[] squads = new Squad[SQUAD_COUNT];
        
        // 캐릭터 성장 데이터
        private Dictionary<string, CharacterGrowthData> growthData;
        
        // 시너지 시스템
        private Dictionary<string, SynergyBonus> activeSynergies;
        
        [System.Serializable]
        public class CharacterGrowthData
        {
            public string characterId;
            public int totalExp;
            public int awakenLevel;
            public int starLevel;
            public int bondLevel;
            public List<string> unlockedSkills;
            public Dictionary<string, int> skillLevels;
            public CharacterStats bonusStats;
        }
        
        [System.Serializable]
        public class CharacterStats
        {
            public float hp;
            public float attack;
            public float defense;
            public float speed;
            public float critRate;
            public float critDamage;
        }
        
        [System.Serializable]
        public class SynergyBonus
        {
            public string synergyId;
            public string synergyName;
            public SynergyType type;
            public List<string> requiredCharacters;
            public float attackBonus;
            public float defenseBonus;
            public float speedBonus;
            public float healingBonus;
            public string specialEffect;
        }
        
        public enum SynergyType
        {
            JobSynergy,     // 같은 직업
            ElementSynergy, // 같은 속성
            StorySynergy,   // 스토리 연관
            TacticSynergy   // 전술 조합
        }
        
        // 이벤트
        public event Action<Unit> OnCharacterAdded;
        public event Action<Unit> OnCharacterLevelUp;
        public event Action<Unit> OnCharacterAwakened;
        public event Action<int, Squad> OnSquadUpdated;
        public event Action<SynergyBonus> OnSynergyActivated;
        
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
            ownedCharacters = new Dictionary<string, Unit>();
            characterRoster = new List<Unit>();
            growthData = new Dictionary<string, CharacterGrowthData>();
            activeSynergies = new Dictionary<string, SynergyBonus>();
            
            // 부대 초기화
            for (int i = 0; i < SQUAD_COUNT; i++)
            {
                squads[i] = new Squad($"부대 {i + 1}", i, true);
            }
            
            // 기본 시너지 등록
            RegisterDefaultSynergies();
        }
        
        /// <summary>
        /// 새로운 캐릭터 추가 (최대 18명)
        /// </summary>
        public bool AddCharacter(Unit character)
        {
            if (character == null || ownedCharacters.Count >= MAX_CHARACTERS)
            {
                Debug.LogWarning($"캐릭터를 추가할 수 없습니다. (현재: {ownedCharacters.Count}/{MAX_CHARACTERS})");
                return false;
            }
            
            if (ownedCharacters.ContainsKey(character.unitId))
            {
                // 중복 캐릭터는 강화 재료로 변환
                ConvertToEnhancementMaterial(character);
                return false;
            }
            
            ownedCharacters[character.unitId] = character;
            characterRoster.Add(character);
            
            // 성장 데이터 초기화
            growthData[character.unitId] = new CharacterGrowthData
            {
                characterId = character.unitId,
                totalExp = 0,
                awakenLevel = 0,
                starLevel = GetStarLevel(character.rarity),
                bondLevel = 0,
                unlockedSkills = new List<string>(),
                skillLevels = new Dictionary<string, int>(),
                bonusStats = new CharacterStats()
            };
            
            OnCharacterAdded?.Invoke(character);
            return true;
        }
        
        /// <summary>
        /// 부대에 캐릭터 배치
        /// </summary>
        public bool AssignToSquad(string characterId, int squadIndex, int row, int col)
        {
            if (squadIndex < 0 || squadIndex >= SQUAD_COUNT)
                return false;
                
            if (!ownedCharacters.TryGetValue(characterId, out Unit character))
                return false;
            
            // 다른 부대에서 제거
            for (int i = 0; i < SQUAD_COUNT; i++)
            {
                if (i != squadIndex)
                {
                    squads[i].RemoveUnit(character);
                }
            }
            
            // 새 부대에 배치
            bool success = squads[squadIndex].AddUnit(character, row, col);
            if (success)
            {
                OnSquadUpdated?.Invoke(squadIndex, squads[squadIndex]);
                UpdateSquadSynergies(squadIndex);
            }
            
            return success;
        }
        
        /// <summary>
        /// 캐릭터 레벨업
        /// </summary>
        public void LevelUpCharacter(string characterId, int expGained)
        {
            if (!ownedCharacters.TryGetValue(characterId, out Unit character))
                return;
            
            if (!growthData.TryGetValue(characterId, out CharacterGrowthData growth))
                return;
            
            growth.totalExp += expGained;
            
            // 레벨 계산 및 스탯 증가
            int oldLevel = character.level;
            character.GainExperience(expGained);
            
            if (character.level > oldLevel)
            {
                ApplyLevelUpBonuses(character, oldLevel, character.level);
                OnCharacterLevelUp?.Invoke(character);
            }
        }
        
        /// <summary>
        /// 캐릭터 각성
        /// </summary>
        public bool AwakenCharacter(string characterId, List<string> materialIds)
        {
            if (!ownedCharacters.TryGetValue(characterId, out Unit character))
                return false;
            
            if (!growthData.TryGetValue(characterId, out CharacterGrowthData growth))
                return false;
            
            // 각성 재료 검증
            if (!ValidateAwakenMaterials(character, materialIds))
                return false;
            
            // 각성 처리
            growth.awakenLevel++;
            character.awakenLevel = growth.awakenLevel;
            
            // 각성 보너스 적용
            ApplyAwakenBonuses(character, growth.awakenLevel);
            
            // 재료 소비
            ConsumeMaterials(materialIds);
            
            OnCharacterAwakened?.Invoke(character);
            return true;
        }
        
        /// <summary>
        /// 부대 시너지 업데이트
        /// </summary>
        void UpdateSquadSynergies(int squadIndex)
        {
            var squad = squads[squadIndex];
            var units = squad.GetAllUnits();
            
            // 기존 시너지 초기화
            activeSynergies.Clear();
            
            // 직업 시너지 체크
            CheckJobSynergies(units);
            
            // 스토리 시너지 체크
            CheckStorySynergies(units);
            
            // 전술 시너지 체크
            CheckTacticSynergies(units);
            
            // 시너지 보너스 적용
            ApplySynergyBonuses(units);
        }
        
        void CheckJobSynergies(List<Unit> units)
        {
            var jobCounts = new Dictionary<JobClass, int>();
            
            foreach (var unit in units)
            {
                if (!jobCounts.ContainsKey(unit.jobClass))
                    jobCounts[unit.jobClass] = 0;
                jobCounts[unit.jobClass]++;
            }
            
            // 3명 이상 같은 직업이면 시너지 발동
            foreach (var kvp in jobCounts)
            {
                if (kvp.Value >= 3)
                {
                    var synergy = new SynergyBonus
                    {
                        synergyId = $"job_{kvp.Key}",
                        synergyName = $"{kvp.Key} 전문가",
                        type = SynergyType.JobSynergy,
                        attackBonus = 0.1f * kvp.Value,
                        defenseBonus = 0.05f * kvp.Value,
                        speedBonus = 0.05f * kvp.Value
                    };
                    
                    activeSynergies[synergy.synergyId] = synergy;
                    OnSynergyActivated?.Invoke(synergy);
                }
            }
        }
        
        void CheckStorySynergies(List<Unit> units)
        {
            // 특정 캐릭터 조합 시너지
            // 예: "전설의 삼총사" - 특정 3명이 함께 있을 때
            var unitNames = units.Select(u => u.unitName).ToList();
            
            if (unitNames.Contains("아서") && unitNames.Contains("란슬롯") && unitNames.Contains("갈라하드"))
            {
                var synergy = new SynergyBonus
                {
                    synergyId = "knights_of_round",
                    synergyName = "원탁의 기사",
                    type = SynergyType.StorySynergy,
                    attackBonus = 0.25f,
                    defenseBonus = 0.25f,
                    specialEffect = "모든 기사 유닛 크리티컬 확률 +15%"
                };
                
                activeSynergies[synergy.synergyId] = synergy;
                OnSynergyActivated?.Invoke(synergy);
            }
        }
        
        void CheckTacticSynergies(List<Unit> units)
        {
            // 전술 조합 시너지
            int tanks = units.Count(u => u.jobClass == JobClass.Knight);
            int healers = units.Count(u => u.jobClass == JobClass.Priest);
            int dps = units.Count(u => u.jobClass == JobClass.Warrior || u.jobClass == JobClass.Wizard);
            
            if (tanks >= 2 && healers >= 2 && dps >= 4)
            {
                var synergy = new SynergyBonus
                {
                    synergyId = "balanced_formation",
                    synergyName = "균형잡힌 진형",
                    type = SynergyType.TacticSynergy,
                    defenseBonus = 0.2f,
                    healingBonus = 0.3f,
                    specialEffect = "받는 피해 10% 감소"
                };
                
                activeSynergies[synergy.synergyId] = synergy;
                OnSynergyActivated?.Invoke(synergy);
            }
        }
        
        void RegisterDefaultSynergies()
        {
            // 기본 시너지 정의
            // 이 부분은 데이터 파일에서 로드하도록 확장 가능
        }
        
        void ApplyLevelUpBonuses(Unit character, int oldLevel, int newLevel)
        {
            // 레벨업 보너스 적용
            int levelDiff = newLevel - oldLevel;
            character.maxHP += levelDiff * 50;
            character.attackPower += levelDiff * 5;
            character.defense += levelDiff * 3;
            character.speed += levelDiff * 2;
            
            // 현재 HP/MP 회복
            character.currentHP = character.maxHP;
            character.currentMP = character.maxMP;
        }
        
        void ApplyAwakenBonuses(Unit character, int awakenLevel)
        {
            // 각성 보너스 적용
            float multiplier = 1f + (awakenLevel * 0.1f);
            character.maxHP = Mathf.RoundToInt(character.maxHP * multiplier);
            character.attackPower = Mathf.RoundToInt(character.attackPower * multiplier);
            character.defense = Mathf.RoundToInt(character.defense * multiplier);
            character.magicPower = Mathf.RoundToInt(character.magicPower * multiplier);
            
            // 새로운 스킬 해금
            if (awakenLevel == 1)
            {
                // 첫 각성 시 궁극기 해금
                character.skillIds.Add(999); // 궁극기 ID
            }
        }
        
        void ApplySynergyBonuses(List<Unit> units)
        {
            foreach (var unit in units)
            {
                float totalAttackBonus = 0f;
                float totalDefenseBonus = 0f;
                float totalSpeedBonus = 0f;
                
                foreach (var synergy in activeSynergies.Values)
                {
                    totalAttackBonus += synergy.attackBonus;
                    totalDefenseBonus += synergy.defenseBonus;
                    totalSpeedBonus += synergy.speedBonus;
                }
                
                // 시너지 보너스 적용
                unit.attackMultiplier = 1f + totalAttackBonus;
                unit.defenseMultiplier = 1f + totalDefenseBonus;
                unit.speedMultiplier = 1f + totalSpeedBonus;
            }
        }
        
        bool ValidateAwakenMaterials(Unit character, List<string> materialIds)
        {
            // 각성 재료 검증 로직
            return materialIds.Count >= 3; // 예시: 3개 이상의 재료 필요
        }
        
        void ConvertToEnhancementMaterial(Unit duplicate)
        {
            // 중복 캐릭터를 강화 재료로 변환
            // TODO: 재료 시스템과 연동
        }
        
        void ConsumeMaterials(List<string> materialIds)
        {
            // 재료 소비 처리
            // TODO: 인벤토리 시스템과 연동
        }
        
        int GetStarLevel(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common: return 1;
                case Rarity.Uncommon: return 2;
                case Rarity.Rare: return 3;
                case Rarity.Epic: return 4;
                case Rarity.Legendary: return 5;
                default: return 1;
            }
        }
        
        // 공개 API
        public List<Unit> GetAllCharacters() => new List<Unit>(characterRoster);
        public Unit GetCharacter(string characterId) => ownedCharacters.GetValueOrDefault(characterId);
        public Squad GetSquad(int index) => (index >= 0 && index < SQUAD_COUNT) ? squads[index] : null;
        public int GetOwnedCharacterCount() => ownedCharacters.Count;
        public bool IsCharacterOwned(string characterId) => ownedCharacters.ContainsKey(characterId);
        public List<SynergyBonus> GetActiveSynergies() => new List<SynergyBonus>(activeSynergies.Values);
        
        // 통계 API
        public CharacterStatistics GetCharacterStatistics(string characterId)
        {
            if (!ownedCharacters.TryGetValue(characterId, out Unit character))
                return null;
            
            return new CharacterStatistics
            {
                CharacterId = characterId,
                TotalDamageDealt = 0, // TODO: 전투 통계 시스템과 연동
                TotalHealingDone = 0,
                BattlesParticipated = 0,
                WinRate = 0f,
                AverageDPS = character.attackPower * character.speed / 100f,
                TankingScore = character.maxHP * character.defense / 1000f,
                HealingScore = character.magicPower * (character.jobClass == JobClass.Priest ? 2f : 1f)
            };
        }
        
        // GetCharacterList for compatibility
        public List<Unit> GetCharacterList()
        {
            return GetAllCharacters();
        }
        
        // GetAllSquads for compatibility
        public List<Squad> GetAllSquads()
        {
            var allSquads = new List<Squad>();
            for (int i = 0; i < SQUAD_COUNT; i++)
            {
                if (squads[i] != null)
                {
                    allSquads.Add(squads[i]);
                }
            }
            return allSquads;
        }
        
        public class CharacterStatistics
        {
            public string CharacterId;
            public int TotalDamageDealt;
            public int TotalHealingDone;
            public int BattlesParticipated;
            public float WinRate;
            public float AverageDPS;
            public float TankingScore;
            public float HealingScore;
        }
    }
}