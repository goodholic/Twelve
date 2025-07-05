using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Battle;
using GuildMaster.Data;

namespace GuildMaster.Systems
{
    public class AdventurerGrowthSystem : MonoBehaviour
    {
        private static AdventurerGrowthSystem _instance;
        public static AdventurerGrowthSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AdventurerGrowthSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AdventurerGrowthSystem");
                        _instance = go.AddComponent<AdventurerGrowthSystem>();
                    }
                }
                return _instance;
            }
        }
        
        // 성장 타입
        public enum GrowthType
        {
            AttackFocus,    // 공격 특화
            DefenseFocus,   // 방어 특화
            SpeedFocus,     // 속도 특화
            MagicFocus,     // 마법 특화
            Balanced        // 균형형
        }
        
        // 각성 시스템
        [System.Serializable]
        public class AwakeningData
        {
            public int level;               // 각성 레벨 (0-5)
            public float statMultiplier;    // 스탯 배율
            public string specialAbility;   // 특수 능력
            public int materialsRequired;   // 필요 재료 수
            public int goldCost;           // 골드 비용
        }
        
        // 직업 승급
        [System.Serializable]
        public class JobPromotion
        {
            public JobClass baseJob;        // 기본 직업
            public JobClass advancedJob;    // 상위 직업
            public int requiredLevel;       // 필요 레벨
            public float jobMasteryRequired; // 필요 숙련도
            public string promotionMaterial; // 승급 재료
        }
        
        // 특성 시스템
        [System.Serializable]
        public class TraitData
        {
            public string traitId;
            public string traitName;
            public string description;
            public GrowthType growthType;
            public Dictionary<string, float> statBonuses;
            public List<string> unlockedSkills;
            
            public TraitData()
            {
                statBonuses = new Dictionary<string, float>();
                unlockedSkills = new List<string>();
            }
        }
        
        private Dictionary<int, AwakeningData> awakeningTable;
        private List<JobPromotion> jobPromotions;
        private Dictionary<string, TraitData> availableTraits;
        private Dictionary<string, GrowthType> unitGrowthTypes;
        
        // 이벤트
        public event Action<Unit, int> OnUnitAwakened;
        public event Action<Unit, JobClass> OnJobPromoted;
        public event Action<Unit, TraitData> OnTraitSelected;
        public event Action<Unit, int> OnLevelUp;
        
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
            awakeningTable = new Dictionary<int, AwakeningData>();
            jobPromotions = new List<JobPromotion>();
            availableTraits = new Dictionary<string, TraitData>();
            unitGrowthTypes = new Dictionary<string, GrowthType>();
            
            SetupAwakeningTable();
            SetupJobPromotions();
            SetupTraits();
        }
        
        void SetupAwakeningTable()
        {
            // 각성 레벨별 데이터
            awakeningTable[1] = new AwakeningData
            {
                level = 1,
                statMultiplier = 1.1f,
                specialAbility = "basic_awakening",
                materialsRequired = 10,
                goldCost = 5000
            };
            
            awakeningTable[2] = new AwakeningData
            {
                level = 2,
                statMultiplier = 1.2f,
                specialAbility = "enhanced_skills",
                materialsRequired = 20,
                goldCost = 10000
            };
            
            awakeningTable[3] = new AwakeningData
            {
                level = 3,
                statMultiplier = 1.35f,
                specialAbility = "aura_unlock",
                materialsRequired = 50,
                goldCost = 25000
            };
            
            awakeningTable[4] = new AwakeningData
            {
                level = 4,
                statMultiplier = 1.5f,
                specialAbility = "ultimate_form",
                materialsRequired = 100,
                goldCost = 50000
            };
            
            awakeningTable[5] = new AwakeningData
            {
                level = 5,
                statMultiplier = 1.75f,
                specialAbility = "transcendent",
                materialsRequired = 200,
                goldCost = 100000
            };
        }
        
        void SetupJobPromotions()
        {
            // 전사 → 버서커/팔라딘
            jobPromotions.Add(new JobPromotion
            {
                baseJob = JobClass.Warrior,
                advancedJob = JobClass.Knight, // 팔라딘으로 전직
                requiredLevel = 50,
                jobMasteryRequired = 80f,
                promotionMaterial = "paladin_emblem"
            });
            
            // 마법사 → 대마법사/원소술사
            jobPromotions.Add(new JobPromotion
            {
                baseJob = JobClass.Mage,
                advancedJob = JobClass.Sage, // 대마법사로 전직
                requiredLevel = 50,
                jobMasteryRequired = 80f,
                promotionMaterial = "archmage_tome"
            });
            
            // 추가 전직 경로...
        }
        
        void SetupTraits()
        {
            // 공격 특화 특성
            var attackTrait = new TraitData
            {
                traitId = "berserker_path",
                traitName = "광전사의 길",
                description = "공격력과 크리티컬에 특화된 성장",
                growthType = GrowthType.AttackFocus
            };
            attackTrait.statBonuses["attack"] = 0.2f;
            attackTrait.statBonuses["critRate"] = 0.1f;
            attackTrait.statBonuses["defense"] = -0.1f;
            attackTrait.unlockedSkills.Add("berserk_rage");
            availableTraits["berserker_path"] = attackTrait;
            
            // 방어 특화 특성
            var defenseTrait = new TraitData
            {
                traitId = "guardian_path",
                traitName = "수호자의 길",
                description = "방어력과 생명력에 특화된 성장",
                growthType = GrowthType.DefenseFocus
            };
            defenseTrait.statBonuses["defense"] = 0.3f;
            defenseTrait.statBonuses["hp"] = 0.2f;
            defenseTrait.statBonuses["speed"] = -0.1f;
            defenseTrait.unlockedSkills.Add("iron_will");
            availableTraits["guardian_path"] = defenseTrait;
            
            // 속도 특화 특성
            var speedTrait = new TraitData
            {
                traitId = "assassin_path",
                traitName = "암살자의 길",
                description = "속도와 회피에 특화된 성장",
                growthType = GrowthType.SpeedFocus
            };
            speedTrait.statBonuses["speed"] = 0.3f;
            speedTrait.statBonuses["evasion"] = 0.15f;
            speedTrait.statBonuses["hp"] = -0.15f;
            speedTrait.unlockedSkills.Add("shadow_step");
            availableTraits["assassin_path"] = speedTrait;
        }
        
        // 레벨업 처리
        public void LevelUpUnit(Unit unit, int targetLevel)
        {
            if (unit == null || targetLevel <= unit.level) return;
            
            int levelsGained = targetLevel - unit.level;
            
            // 성장 타입에 따른 스탯 증가
            GrowthType growthType = GetUnitGrowthType(unit);
            
            for (int i = 0; i < levelsGained; i++)
            {
                ApplyLevelUpStats(unit, growthType);
                unit.level++;
                
                OnLevelUp?.Invoke(unit, unit.level);
            }
            
            // HP/MP 회복
            unit.currentHP = unit.maxHP;
            unit.currentMP = unit.maxMP;
        }
        
        void ApplyLevelUpStats(Unit unit, GrowthType growthType)
        {
            // 기본 성장치
            float hpGrowth = 10f;
            float mpGrowth = 5f;
            float attackGrowth = 2f;
            float defenseGrowth = 1f;
            float magicGrowth = 2f;
            float speedGrowth = 0.5f;
            
            // 성장 타입별 보정
            switch (growthType)
            {
                case GrowthType.AttackFocus:
                    attackGrowth *= 1.5f;
                    hpGrowth *= 0.8f;
                    break;
                    
                case GrowthType.DefenseFocus:
                    defenseGrowth *= 1.5f;
                    hpGrowth *= 1.2f;
                    speedGrowth *= 0.8f;
                    break;
                    
                case GrowthType.SpeedFocus:
                    speedGrowth *= 1.5f;
                    attackGrowth *= 1.2f;
                    defenseGrowth *= 0.8f;
                    break;
                    
                case GrowthType.MagicFocus:
                    magicGrowth *= 1.5f;
                    mpGrowth *= 1.3f;
                    hpGrowth *= 0.8f;
                    break;
            }
            
            // 스탯 적용
            unit.maxHP += hpGrowth;
            unit.maxMP += mpGrowth;
            unit.attackPower += attackGrowth;
            unit.defense += defenseGrowth;
            unit.magicPower += magicGrowth;
            unit.speed += speedGrowth;
        }
        
        // 각성
        public bool AwakeUnit(Unit unit)
        {
            if (unit == null || unit.awakeningLevel >= 5) return false;
            
            int nextLevel = unit.awakeningLevel + 1;
            if (!awakeningTable.ContainsKey(nextLevel)) return false;
            
            var awakeningData = awakeningTable[nextLevel];
            
            // 비용 확인
            var gameManager = Core.GameManager.Instance;
            if (gameManager?.ResourceManager == null) return false;
            
            if (gameManager.ResourceManager.GetGold() < awakeningData.goldCost)
                return false;
            
            // 비용 차감
            gameManager.ResourceManager.AddGold(-awakeningData.goldCost);
            
            // 각성 적용
            unit.Awaken();
            
            // 스탯 배율 적용
            unit.maxHP *= awakeningData.statMultiplier;
            unit.attackPower *= awakeningData.statMultiplier;
            unit.defense *= awakeningData.statMultiplier;
            unit.magicPower *= awakeningData.statMultiplier;
            
            OnUnitAwakened?.Invoke(unit, nextLevel);
            
            return true;
        }
        
        // 직업 승급
        public bool PromoteJob(Unit unit)
        {
            if (unit == null) return false;
            
            var promotion = jobPromotions.FirstOrDefault(p => 
                p.baseJob == unit.jobClass && 
                unit.level >= p.requiredLevel &&
                unit.jobMastery >= p.jobMasteryRequired);
                
            if (promotion == null) return false;
            
            // 직업 변경
            unit.jobClass = promotion.advancedJob;
            
            // 보너스 스탯
            unit.maxHP *= 1.2f;
            unit.attackPower *= 1.2f;
            unit.defense *= 1.2f;
            unit.magicPower *= 1.2f;
            
            OnJobPromoted?.Invoke(unit, promotion.advancedJob);
            
            return true;
        }
        
        // 특성 선택
        public bool SelectTrait(Unit unit, string traitId)
        {
            if (unit == null || !availableTraits.ContainsKey(traitId))
                return false;
                
            var trait = availableTraits[traitId];
            
            // 성장 타입 설정
            unitGrowthTypes[unit.unitId] = trait.growthType;
            
            // 스탯 보너스 적용
            foreach (var bonus in trait.statBonuses)
            {
                switch (bonus.Key)
                {
                    case "attack":
                        unit.attackPower *= (1f + bonus.Value);
                        break;
                    case "defense":
                        unit.defense *= (1f + bonus.Value);
                        break;
                    case "hp":
                        unit.maxHP *= (1f + bonus.Value);
                        break;
                    case "speed":
                        unit.speed *= (1f + bonus.Value);
                        break;
                    case "critRate":
                        unit.criticalRate += bonus.Value;
                        break;
                    case "evasion":
                        unit.evasion += bonus.Value;
                        break;
                }
            }
            
            // 스킬 해금
            foreach (string skillId in trait.unlockedSkills)
            {
                // TODO: 스킬 시스템과 연동
                Debug.Log($"Unlocked skill: {skillId}");
            }
            
            OnTraitSelected?.Invoke(unit, trait);
            
            return true;
        }
        
        // 합성 (같은 유닛 사용)
        public Unit SynthesizeUnits(Unit baseUnit, List<Unit> materials)
        {
            if (baseUnit == null || materials == null || materials.Count == 0)
                return null;
                
            // 같은 이름의 유닛만 합성 가능
            var validMaterials = materials.Where(m => 
                m.unitName == baseUnit.unitName && 
                m.unitId != baseUnit.unitId).ToList();
                
            if (validMaterials.Count == 0) return null;
            
            // 합성 효과
            foreach (var material in validMaterials)
            {
                // 경험치 전수
                baseUnit.AddExperience(material.experience / 2);
                
                // 숙련도 증가
                baseUnit.IncreaseJobMastery(10f);
                
                // 등급별 보너스
                if (material.rarity >= Rarity.Rare)
                {
                    baseUnit.maxHP += 10;
                    baseUnit.attackPower += 2;
                    baseUnit.defense += 1;
                }
                
                // 재료 유닛 제거
                var gameManager = Core.GameManager.Instance;
                gameManager?.GuildManager?.RemoveAdventurer(material);
            }
            
            return baseUnit;
        }
        
        // 성장 타입 조회
        public GrowthType GetUnitGrowthType(Unit unit)
        {
            if (unitGrowthTypes.ContainsKey(unit.unitId))
                return unitGrowthTypes[unit.unitId];
                
            // 기본 성장 타입 (직업별)
            switch (unit.jobClass)
            {
                case JobClass.Warrior:
                case JobClass.Knight:
                    return GrowthType.DefenseFocus;
                    
                case JobClass.Mage:
                case JobClass.Priest:
                    return GrowthType.MagicFocus;
                    
                case JobClass.Assassin:
                case JobClass.Ranger:
                    return GrowthType.SpeedFocus;
                    
                default:
                    return GrowthType.Balanced;
            }
        }
        
        // 특성 목록 조회
        public List<TraitData> GetAvailableTraits(JobClass jobClass)
        {
            // 직업별 사용 가능한 특성 필터링
            return availableTraits.Values.ToList();
        }
        
        // 승급 가능 여부
        public bool CanPromote(Unit unit)
        {
            return jobPromotions.Any(p => 
                p.baseJob == unit.jobClass && 
                unit.level >= p.requiredLevel &&
                unit.jobMastery >= p.jobMasteryRequired);
        }
        
        // 각성 비용 조회
        public AwakeningData GetAwakeningData(int level)
        {
            return awakeningTable.ContainsKey(level) ? awakeningTable[level] : null;
        }
    }
}