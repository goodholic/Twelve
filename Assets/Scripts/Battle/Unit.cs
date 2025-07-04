using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle; // StatusEffect를 위해 추가

namespace GuildMaster.Battle
{
    public enum JobClass
    {
        None,
        Warrior,    // 전사: 높은 HP와 물리 공격력
        Knight,     // 기사: 최고의 방어력과 아군 보호 능력
        Mage,       // 마법사: 강력한 마법 공격력과 광역 스킬 (Wizard -> Mage로 변경)
        Priest,     // 성직자: 힐링과 부활 마법의 전문가
        Assassin,   // 도적: 빠른 속도와 크리티컬 특화 (Rogue -> Assassin으로 변경)
        Ranger,     // 궁수: 원거리 물리 공격의 달인 (Archer -> Ranger로 변경)
        Sage,       // 현자: 마법과 물리를 아우르는 만능형
        Archer,     // 호환성을 위한 별칭 (Ranger와 동일)
        All,        // 모든 직업 (스킬 호환성용)
        Rogue,      // Assassin의 별칭
        Bard,       // 음유시인 (새로운 직업)
        Blacksmith, // 대장장이 (시설 관련 직업)
        Merchant    // 상인 (시설 관련 직업)
    }
    
    // Class enum을 JobClass와 호환되도록 alias 생성
    public enum Class
    {
        None = JobClass.None,
        Warrior = JobClass.Warrior,
        Knight = JobClass.Knight,
        Wizard = JobClass.Mage,
        Priest = JobClass.Priest,
        Rogue = JobClass.Assassin,
        Archer = JobClass.Ranger,
        Sage = JobClass.Sage
    }
    
    public enum Rarity
    {
        Common,     // 일반
        Uncommon,   // 고급
        Rare,       // 희귀
        Epic,       // 영웅
        Legendary   // 전설
    }

    [System.Serializable]
    public class Unit
    {
        // Basic Info
        public string unitId;
        public string unitName;
        public int level;
        public JobClass jobClass;  // unitClass를 jobClass로 변경
        public JobClass JobClass => jobClass;  // 호환성을 위한 속성
        public Class unitClass => (Class)jobClass;  // 기존 호환성 유지
        public Rarity rarity;
        public bool isPlayerUnit;
        public int experience;
        public int experienceToNextLevel;
        
        // Job Mastery
        public float jobMastery; // 0-100
        public int awakeningLevel; // 0-5 각성 레벨

        // Position in Battle
        public int currentSquad;
        public Vector2Int gridPosition;
        public float formationBuff = 1f;

        // Base Stats
        public float maxHP;
        public float currentHP;
        public float maxMP;
        public float currentMP;
        public float attackPower;
        public float defense;
        public float magicPower;
        public float magicResistance; // 마법 저항력 추가
        public float speed;
        public float criticalRate;
        public float criticalDamage;
        public float accuracy;
        public float evasion;
        
        // 추가 속성들
        public float MaxHealth => maxHP;
        public float CurrentHealth => currentHP;
        public float MaxMana => maxMP;
        public float CurrentMana => currentMP;
        public float Attack => attackPower;
        public float MagicPower => magicPower;
        public float Defense => defense;
        public int Level { get; set; }
        public string UnitId => unitId;
        public string Name => unitName;
        public bool IsPlayerUnit => isPlayerUnit;
        public bool IsAlive => isAlive;
        public float Speed => speed;
        public float CriticalRate => criticalRate;
        public float Accuracy => accuracy;
        public int Experience => experience;
        public int SquadIndex => currentSquad;
        public int Row => gridPosition.x;
        public int Col => gridPosition.y;
        
        // Shield system
        public float currentShield;
        public float maxShield;

        // Skills
        public List<int> skillIds = new List<int>();
        public List<StatusEffect> activeStatusEffects = new List<StatusEffect>();

        // State
        public bool isAlive => currentHP > 0;
        
        // Combat Stats (Calculated)
        public float effectiveAttackPower => CalculateAttackPower();
        public float effectiveMagicPower => CalculateMagicAttackPower();
        public float effectiveDefense => CalculatePhysicalDefense();
        public float effectiveMagicDefense => CalculateMagicalDefense();
        public float effectiveHealingPower => CalculateHealingPower();
        
        // Job-specific abilities
        public class JobAbility
        {
            public string name;
            public string description;
            public float chance; // Chance to trigger
            public float value;   // Effect value
        }
        
        private List<JobAbility> jobAbilities = new List<JobAbility>();

        // Events
        public event Action<Unit, float> OnDamageTaken;
        public event Action<Unit, float> OnHealed;
        public event Action<Unit> OnDeath;

        // Battle-related properties
        public int awakenLevel => awakeningLevel;
        public Vector2Int squadPosition;
        public Transform transform; // GameObject의 transform 참조
        public GameObject gameObject; // GameObject 참조 추가
        
        // 호환성을 위한 별칭들 (읽기 전용)
        public float attack => attackPower; // 호환성을 위한 별칭
        
        // 수정 가능한 속성들 (BattleSimulationSystem 호환성)
        public float AttackModifiable
        {
            get => attackPower;
            set => attackPower = value;
        }
        
        // 추가 호환성 속성들
        public float critRate => criticalRate;
        public float critDamage => criticalDamage;
        
        // Battle methods
        public void ResetBattleState()
        {
            currentHP = maxHP;
            currentMP = maxMP;
            activeStatusEffects.Clear();
        }
        
        public bool GetCriticalHit()
        {
            return UnityEngine.Random.value < criticalRate;
        }
        
        public List<Skill> GetAvailableSkills()
        {
            // 간단한 구현 - 실제로는 스킬 시스템과 연동 필요
            return new List<Skill>();
        }
        
        public string GetJobIcon()
        {
            switch (jobClass)
            {
                case JobClass.Warrior: return "icon_warrior";
                case JobClass.Knight: return "icon_knight";
                case JobClass.Mage: return "icon_mage";
                case JobClass.Priest: return "icon_priest";
                case JobClass.Assassin: return "icon_assassin";
                case JobClass.Ranger: return "icon_ranger";
                case JobClass.Sage: return "icon_sage";
                default: return "icon_default";
            }
        }

        public Unit(string name, int level, JobClass jobClass, Rarity rank = Rarity.Common)
        {
            unitId = Guid.NewGuid().ToString();
            unitName = name;
            this.level = level;
            this.jobClass = jobClass;
            rarity = rank;
            experience = 0;
            experienceToNextLevel = CalculateExpToNextLevel();
            jobMastery = 0f;
            awakeningLevel = 0;
            
            InitializeStats();
            InitializeJobAbilities();
            InitializeSkills();
        }
        
        // 위치 설정 메서드 추가
        public void SetPosition(int squadIndex, int row, int col)
        {
            currentSquad = squadIndex;
            gridPosition = new Vector2Int(row, col);
        }
        
        int CalculateExpToNextLevel()
        {
            return 100 * level * (int)Mathf.Pow(1.2f, level - 1);
        }

        void InitializeStats()
        {
            // Apply rank multiplier
            float rankMultiplier = GetRankMultiplier();
            
            // Base stats by job class
            switch (jobClass)
            {
                case JobClass.Warrior:
                    maxHP = (100 + (level * 20)) * rankMultiplier;
                    maxMP = (50 + (level * 5)) * rankMultiplier;
                    attackPower = (15 + (level * 3)) * rankMultiplier;
                    defense = (10 + (level * 2)) * rankMultiplier;
                    magicPower = (5 + (level * 0.5f)) * rankMultiplier;
                    speed = (8 + (level * 1)) * rankMultiplier;
                    criticalRate = 0.15f + (rarity == Rarity.Legendary ? 0.1f : 0f);
                    criticalDamage = 1.5f + (awakeningLevel * 0.1f);
                    accuracy = 0.9f;
                    evasion = 0.05f;
                    break;
                    
                case JobClass.Knight:
                    maxHP = (120 + (level * 25)) * rankMultiplier;
                    maxMP = (60 + (level * 6)) * rankMultiplier;
                    attackPower = (12 + (level * 2)) * rankMultiplier;
                    defense = (15 + (level * 3)) * rankMultiplier;
                    magicPower = (8 + (level * 1)) * rankMultiplier;
                    speed = (6 + (level * 0.8f)) * rankMultiplier;
                    criticalRate = 0.1f;
                    criticalDamage = 1.4f + (awakeningLevel * 0.08f);
                    accuracy = 0.85f;
                    evasion = 0.03f + (jobMastery * 0.001f);
                    break;
                    
                case JobClass.Mage:
                    maxHP = (60 + (level * 10)) * rankMultiplier;
                    maxMP = (100 + (level * 15)) * rankMultiplier;
                    attackPower = (5 + (level * 0.5f)) * rankMultiplier;
                    defense = (5 + (level * 1)) * rankMultiplier;
                    magicPower = (20 + (level * 4)) * rankMultiplier;
                    speed = (10 + (level * 1.2f)) * rankMultiplier;
                    criticalRate = 0.2f + (jobMastery * 0.002f);
                    criticalDamage = 1.8f + (awakeningLevel * 0.12f);
                    accuracy = 0.95f;
                    evasion = 0.08f;
                    break;
                    
                case JobClass.Priest:
                    maxHP = (70 + (level * 12)) * rankMultiplier;
                    maxMP = (80 + (level * 12)) * rankMultiplier;
                    attackPower = (8 + (level * 1)) * rankMultiplier;
                    defense = (8 + (level * 1.5f)) * rankMultiplier;
                    magicPower = (15 + (level * 3)) * rankMultiplier;
                    speed = (9 + (level * 1)) * rankMultiplier;
                    criticalRate = 0.05f;
                    criticalDamage = 1.3f;
                    accuracy = 0.9f;
                    evasion = 0.06f;
                    break;
                    
                case JobClass.Assassin:
                    maxHP = (80 + (level * 15)) * rankMultiplier;
                    maxMP = (60 + (level * 8)) * rankMultiplier;
                    attackPower = (18 + (level * 3.5f)) * rankMultiplier;
                    defense = (7 + (level * 1.2f)) * rankMultiplier;
                    magicPower = (5 + (level * 0.5f)) * rankMultiplier;
                    speed = (15 + (level * 2)) * rankMultiplier;
                    criticalRate = 0.35f + (jobMastery * 0.003f);
                    criticalDamage = 2.0f + (awakeningLevel * 0.15f);
                    accuracy = 0.95f;
                    evasion = 0.15f + (jobMastery * 0.002f);
                    break;
                    
                case JobClass.Ranger:
                    maxHP = (85 + (level * 16)) * rankMultiplier;
                    maxMP = (70 + (level * 9)) * rankMultiplier;
                    attackPower = (16 + (level * 3.2f)) * rankMultiplier;
                    defense = (8 + (level * 1.5f)) * rankMultiplier;
                    magicPower = (5 + (level * 0.5f)) * rankMultiplier;
                    speed = (12 + (level * 1.5f)) * rankMultiplier;
                    criticalRate = 0.25f + (jobMastery * 0.0025f);
                    criticalDamage = 1.7f + (awakeningLevel * 0.1f);
                    accuracy = 0.98f;
                    evasion = 0.1f;
                    break;
                    
                case JobClass.Sage:
                    maxHP = (90 + (level * 18)) * rankMultiplier;
                    maxMP = (120 + (level * 18)) * rankMultiplier;
                    attackPower = (12 + (level * 2)) * rankMultiplier;
                    defense = (10 + (level * 2)) * rankMultiplier;
                    magicPower = (18 + (level * 3.5f)) * rankMultiplier;
                    speed = (11 + (level * 1.3f)) * rankMultiplier;
                    criticalRate = 0.15f + (jobMastery * 0.0015f);
                    criticalDamage = 1.6f + (awakeningLevel * 0.1f);
                    accuracy = 0.92f;
                    evasion = 0.07f;
                    break;
            }
            
            currentHP = maxHP;
            currentMP = maxMP;
            currentShield = 0;
            maxShield = maxHP * 0.3f;
        }
        
        float GetRankMultiplier()
        {
            return rarity switch
            {
                Rarity.Common => 1f,
                Rarity.Uncommon => 1.15f,
                Rarity.Rare => 1.3f,
                Rarity.Epic => 1.5f,
                Rarity.Legendary => 1.8f,
                _ => 1f
            };
        }

        void InitializeJobAbilities()
        {
            jobAbilities.Clear();
            
            switch (jobClass)
            {
                case JobClass.Warrior:
                    jobAbilities.Add(new JobAbility
                    {
                        name = "Berserker's Rage",
                        description = "Damage increases as HP decreases",
                        chance = 1f,
                        value = 0.5f
                    });
                    jobAbilities.Add(new JobAbility
                    {
                        name = "Counterattack",
                        description = "Chance to counter when attacked",
                        chance = 0.2f,
                        value = 0.5f
                    });
                    break;
                    
                case JobClass.Knight:
                    jobAbilities.Add(new JobAbility
                    {
                        name = "Guardian's Shield",
                        description = "Damage reduction when HP is low",
                        chance = 1f,
                        value = 0.3f
                    });
                    jobAbilities.Add(new JobAbility
                    {
                        name = "Cover",
                        description = "Chance to protect nearby allies",
                        chance = 0.3f,
                        value = 1f
                    });
                    break;
                    
                case JobClass.Mage:
                    jobAbilities.Add(new JobAbility
                    {
                        name = "Elemental Mastery",
                        description = "Chance for double magic damage",
                        chance = 0.15f,
                        value = 2f
                    });
                    jobAbilities.Add(new JobAbility
                    {
                        name = "Mana Surge",
                        description = "MP regeneration on spell cast",
                        chance = 0.3f,
                        value = 0.1f
                    });
                    break;
                    
                case JobClass.Priest:
                    jobAbilities.Add(new JobAbility
                    {
                        name = "Divine Grace",
                        description = "Healing spells have increased effect",
                        chance = 1f,
                        value = 0.3f
                    });
                    jobAbilities.Add(new JobAbility
                    {
                        name = "Sanctuary",
                        description = "Chance to grant shield when healing",
                        chance = 0.25f,
                        value = 0.5f
                    });
                    break;
                    
                case JobClass.Assassin:
                    jobAbilities.Add(new JobAbility
                    {
                        name = "Shadow Strike",
                        description = "Critical attacks ignore defense",
                        chance = 1f,
                        value = 1f
                    });
                    jobAbilities.Add(new JobAbility
                    {
                        name = "Evasion Master",
                        description = "Dodge grants speed boost",
                        chance = 1f,
                        value = 0.2f
                    });
                    break;
                    
                case JobClass.Ranger:
                    jobAbilities.Add(new JobAbility
                    {
                        name = "Precision",
                        description = "Attacks cannot miss",
                        chance = 1f,
                        value = 1f
                    });
                    jobAbilities.Add(new JobAbility
                    {
                        name = "Double Shot",
                        description = "Chance to attack twice",
                        chance = 0.2f,
                        value = 1f
                    });
                    break;
                    
                case JobClass.Sage:
                    jobAbilities.Add(new JobAbility
                    {
                        name = "Wisdom",
                        description = "All abilities have reduced cooldown",
                        chance = 1f,
                        value = 0.2f
                    });
                    jobAbilities.Add(new JobAbility
                    {
                        name = "Enlightenment",
                        description = "Chance to not consume MP",
                        chance = 0.3f,
                        value = 1f
                    });
                    break;
            }
        }

        void InitializeSkills()
        {
            // Add basic skills based on job class - simplified without SkillSystem dependency
            skillIds.Clear();
            
            switch (jobClass)
            {
                case JobClass.Warrior:
                    skillIds.Add(1); // Basic Slash
                    skillIds.Add(2); // Power Strike
                    if (level >= 5) skillIds.Add(3); // Whirlwind
                    break;
                    
                case JobClass.Knight:
                    skillIds.Add(11); // Shield Bash
                    skillIds.Add(12); // Taunt
                    if (level >= 5) skillIds.Add(13); // Guardian
                    break;
                    
                case JobClass.Mage:
                    skillIds.Add(21); // Magic Missile
                    skillIds.Add(22); // Fireball
                    if (level >= 5) skillIds.Add(23); // Lightning Bolt
                    break;
                    
                case JobClass.Priest:
                    skillIds.Add(31); // Heal
                    skillIds.Add(32); // Blessing
                    if (level >= 5) skillIds.Add(33); // Divine Light
                    break;
                    
                case JobClass.Assassin:
                    skillIds.Add(41); // Sneak Attack
                    skillIds.Add(42); // Poison Blade
                    if (level >= 5) skillIds.Add(43); // Shadow Strike
                    break;
                    
                case JobClass.Ranger:
                    skillIds.Add(51); // Aimed Shot
                    skillIds.Add(52); // Multi Shot
                    if (level >= 5) skillIds.Add(53); // Eagle Eye
                    break;
                    
                case JobClass.Sage:
                    skillIds.Add(61); // Wisdom
                    skillIds.Add(62); // Elemental Mastery
                    if (level >= 5) skillIds.Add(63); // Time Stop
                    break;
            }
        }

        public float GetAttackDamage()
        {
            float baseDamage;
            
            // Magic classes use magic power
            if (unitClass == Class.Wizard || unitClass == Class.Priest || unitClass == Class.Sage)
            {
                baseDamage = magicPower;
            }
            else
            {
                baseDamage = attackPower;
            }
            
            // Critical hit check
            if (UnityEngine.Random.value < criticalRate)
            {
                baseDamage *= criticalDamage;
                Debug.Log($"{unitName} scored a critical hit!");
            }
            
            // Apply formation buff
            baseDamage *= formationBuff;
            
            // Apply status effects
            foreach (var effect in activeStatusEffects)
            {
                if (effect.Type == StatusEffectType.Regeneration)
                {
                    baseDamage *= (1f + effect.Value);
                }
                else if (effect.Type == StatusEffectType.Poison || effect.Type == StatusEffectType.Burn)
                {
                    baseDamage *= (1f - effect.Value);
                }
            }
            
            // Job specific damage modifiers
            if (jobClass == JobClass.Warrior && currentHP < maxHP * 0.5f)
            {
                baseDamage *= 1.5f; // Berserker's Rage
            }
            
            return baseDamage;
        }

        public void TakeDamage(float damage, bool isMagical = false)
        {
            if (!isAlive) return;
            
            // Check evasion
            if (UnityEngine.Random.value < evasion)
            {
                Debug.Log($"{unitName} evaded the attack!");
                return;
            }
            
            // Calculate defense
            float effectiveDef = isMagical ? effectiveMagicDefense : effectiveDefense;
            float mitigatedDamage = damage * (100f / (100f + effectiveDef));
            
            // Apply to shield first
            if (currentShield > 0)
            {
                float shieldDamage = Mathf.Min(currentShield, mitigatedDamage);
                currentShield -= shieldDamage;
                mitigatedDamage -= shieldDamage;
            }
            
            // Apply remaining damage to HP
            currentHP -= mitigatedDamage;
            currentHP = Mathf.Max(0, currentHP);
            
            OnDamageTaken?.Invoke(this, mitigatedDamage);
            
            if (!isAlive)
            {
                OnDeath?.Invoke(this);
            }
        }

        public void Heal(float amount)
        {
            if (!isAlive) return;
            
            float actualHeal = amount;
            
            // Apply healing power modifier
            if (jobClass == JobClass.Priest)
            {
                actualHeal *= 1.3f; // Divine Grace
            }
            
            float previousHP = currentHP;
            currentHP = Mathf.Min(currentHP + actualHeal, maxHP);
            float healedAmount = currentHP - previousHP;
            
            OnHealed?.Invoke(this, healedAmount);
            
            // Chance to grant shield (Priest ability)
            if (jobClass == JobClass.Priest && UnityEngine.Random.value < 0.25f)
            {
                AddShield(healedAmount * 0.5f);
            }
        }

        public void AddShield(float amount)
        {
            currentShield = Mathf.Min(currentShield + amount, maxShield);
        }

        public void RestoreMana(float amount)
        {
            currentMP = Mathf.Min(currentMP + amount, maxMP);
        }

        public void AddExperience(int exp)
        {
            experience += exp;
            
            while (experience >= experienceToNextLevel)
            {
                LevelUp();
            }
        }

        void LevelUp()
        {
            experience -= experienceToNextLevel;
            level++;
            experienceToNextLevel = CalculateExpToNextLevel();
            
            // Reinitialize stats with new level
            InitializeStats();
            
            Debug.Log($"{unitName} leveled up to {level}!");
        }

        public void IncreaseJobMastery(float amount)
        {
            jobMastery = Mathf.Min(jobMastery + amount, 100f);
        }

        public void Awaken()
        {
            if (awakeningLevel < 5)
            {
                awakeningLevel++;
                InitializeStats(); // Recalculate stats with new awakening level
                Debug.Log($"{unitName} awakened to level {awakeningLevel}!");
            }
        }

        public int GetCombatPower()
        {
            float power = maxHP * 0.1f + attackPower * 2f + magicPower * 2f + 
                         defense * 1.5f + speed * 1.2f;
            power *= GetRankMultiplier();
            power *= (1f + awakeningLevel * 0.1f);
            return Mathf.RoundToInt(power);
        }

        // Status Effect Management
        public void AddStatusEffect(StatusEffect effect)
        {
            // Check if immune
            if (HasStatusEffect(StatusEffectType.Invulnerable))
                return;
                
            activeStatusEffects.Add(effect);
        }

        public void RemoveAllDebuffs()
        {
            activeStatusEffects.RemoveAll(e => 
                e.Type == StatusEffectType.Stun ||
                e.Type == StatusEffectType.Silence ||
                e.Type == StatusEffectType.Burn ||
                e.Type == StatusEffectType.Poison ||
                e.Type == StatusEffectType.Freeze ||
                e.Type == StatusEffectType.Slow ||
                e.Type == StatusEffectType.Bleed ||
                e.Type == StatusEffectType.Blind
            );
        }

        public bool HasStatusEffect(StatusEffectType type)
        {
            return activeStatusEffects.Any(effect => effect.Type == type);
        }

        public void UpdateStatusEffects()
        {
            for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeStatusEffects[i];
                
                // Apply effect
                switch (effect.Type)
                {
                    case StatusEffectType.Burn:
                    case StatusEffectType.Poison:
                    case StatusEffectType.Bleed:
                        TakeDamage(effect.Value, true);
                        break;
                        
                    case StatusEffectType.Regeneration:
                        Heal(effect.Value);
                        break;
                }
                
                // Update duration
                effect.RemainingDuration -= 1f;
                if (effect.RemainingDuration <= 0)
                {
                    activeStatusEffects.RemoveAt(i);
                }
            }
        }

        // Calculated stats
        float CalculateAttackPower()
        {
            return attackPower * formationBuff;
        }

        float CalculateMagicAttackPower()
        {
            return magicPower * formationBuff;
        }

        float CalculatePhysicalDefense()
        {
            float def = defense;
            if (jobClass == JobClass.Knight && currentHP < maxHP * 0.3f)
            {
                def *= 1.3f; // Guardian's Shield
            }
            return def * formationBuff;
        }

        float CalculateMagicalDefense()
        {
            return defense * 0.8f * formationBuff;
        }

        float CalculateHealingPower()
        {
            float healing = magicPower;
            if (jobClass == JobClass.Priest)
            {
                healing *= 1.3f; // Divine Grace
            }
            return healing * formationBuff;
        }

        // 추가 메서드들
        public float GetHealthPercentage()
        {
            return maxHP > 0 ? currentHP / maxHP : 0f;
        }
        
        public float GetManaPercentage()
        {
            return maxMP > 0 ? currentMP / maxMP : 0f;
        }
        
        public float GetHealPower()
        {
            return effectiveHealingPower;
        }
    }
}