using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class EnemyData
    {
        [Header("기본 정보")]
        public string enemyId;
        public string enemyName;
        public string description;
        
        // CSVDataManager 호환성 속성들
        public int id { get => int.TryParse(enemyId, out int result) ? result : 0; set => enemyId = value.ToString(); }
        public string nameKey { get => enemyName; set => enemyName = value; }
        public float hp { get => maxHP; set => maxHP = (int)value; }
        public List<int> skillIds { get => skills.Select(s => int.TryParse(s, out int id) ? id : 0).ToList(); set { skills = value.Select(id => id.ToString()).ToList(); } }
        public int expReward { get => experienceReward; set => experienceReward = value; }
        public List<int> dropItems { get => drops.Select(d => int.TryParse(d.itemId, out int id) ? id : 0).ToList(); }
        public string spritePath = ""; // 스프라이트 경로
        
        [Header("타입 및 분류")]
        public EnemyType enemyType;
        public EnemyCategory category;
        public EnemyRank rank = EnemyRank.Normal;
        public EnemyElement element = EnemyElement.None;
        
        [Header("레벨 및 등급")]
        public int level = 1;
        public int tier = 1;
        public EnemyRarity rarity = EnemyRarity.Common;
        
        [Header("기본 스탯")]
        public int maxHP = 100;
        public int maxMP = 50;
        public int attack = 10;
        public int defense = 5;
        public int magicPower = 5;
        public int speed = 10;
        
        [Header("전투 스탯")]
        public float critRate = 0.05f;
        public float critDamage = 1.5f;
        public float accuracy = 0.9f;
        public float evasion = 0.1f;
        public float blockRate = 0f;
        
        [Header("저항력")]
        public float physicalResistance = 0f;
        public float magicalResistance = 0f;
        public float fireResistance = 0f;
        public float iceResistance = 0f;
        public float lightningResistance = 0f;
        public float darkResistance = 0f;
        
        [Header("상태이상 저항")]
        public float stunResistance = 0f;
        public float poisonResistance = 0f;
        public float sleepResistance = 0f;
        public float charmResistance = 0f;
        public float fearResistance = 0f;
        
        [Header("스킬")]
        public List<string> skills = new List<string>();
        public List<EnemyAbility> abilities = new List<EnemyAbility>();
        public string ultimateSkill = "";
        
        [Header("AI 행동")]
        public EnemyAIType aiType = EnemyAIType.Aggressive;
        public float aggroRange = 5f;
        public float attackRange = 1.5f;
        public float chaseRange = 8f;
        public float patrolRange = 3f;
        
        [Header("드롭 아이템")]
        public List<EnemyDrop> drops = new List<EnemyDrop>();
        public int experienceReward = 10;
        public int goldReward = 5;
        public float dropRateMultiplier = 1f;
        
        [Header("특수 능력")]
        public bool canFly = false;
        public bool isUndead = false;
        public bool isBoss = false;
        public bool hasRegen = false;
        public float regenRate = 0f;
        public bool immuneToDebuffs = false;
        
        [Header("비주얼")]
        public GameObject enemyPrefab;
        public Sprite enemyIcon;
        public Sprite portraitSprite;
        public RuntimeAnimatorController animatorController;
        public Vector3 scale = Vector3.one;
        
        [Header("사운드")]
        public AudioClip spawnSound;
        public AudioClip attackSound;
        public AudioClip hurtSound;
        public AudioClip deathSound;
        public AudioClip idleSound;
        
        [Header("환경")]
        public List<EnemyHabitat> habitats = new List<EnemyHabitat>();
        public bool isNocturnal = false;
        public List<WeatherType> preferredWeather = new List<WeatherType>();
        
        [Header("그룹 행동")]
        public bool canSummon = false;
        public List<string> summonableEnemies = new List<string>();
        public bool isPackHunter = false;
        public int maxPackSize = 1;
        public float packBonusRange = 3f;
        
        public EnemyData()
        {
            enemyId = System.Guid.NewGuid().ToString();
            enemyName = "Unknown Enemy";
            description = "";
        }
        
        public EnemyData(string id, string name, EnemyType type, int enemyLevel)
        {
            enemyId = id;
            enemyName = name;
            enemyType = type;
            level = enemyLevel;
            
            SetBaseStatsByType(type);
        }
        
        private void SetBaseStatsByType(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Beast:
                    maxHP = 80;
                    attack = 12;
                    defense = 6;
                    speed = 15;
                    break;
                case EnemyType.Undead:
                    maxHP = 70;
                    attack = 10;
                    defense = 8;
                    magicPower = 8;
                    isUndead = true;
                    break;
                case EnemyType.Demon:
                    maxHP = 90;
                    attack = 15;
                    magicPower = 12;
                    fireResistance = 0.3f;
                    break;
                case EnemyType.Dragon:
                    maxHP = 200;
                    attack = 25;
                    defense = 20;
                    magicPower = 20;
                    isBoss = true;
                    break;
                case EnemyType.Elemental:
                    maxHP = 60;
                    attack = 8;
                    magicPower = 18;
                    defense = 4;
                    break;
                case EnemyType.Humanoid:
                    maxHP = 85;
                    attack = 11;
                    defense = 9;
                    speed = 12;
                    break;
            }
        }
        
        public int GetScaledStat(int baseStat, float levelMultiplier = 1.2f)
        {
            return Mathf.RoundToInt(baseStat * Mathf.Pow(levelMultiplier, level - 1));
        }
        
        public int GetCurrentHP()
        {
            return GetScaledStat(maxHP);
        }
        
        public int GetCurrentAttack()
        {
            return GetScaledStat(attack);
        }
        
        public int GetCurrentDefense()
        {
            return GetScaledStat(defense);
        }
        
        public float GetResistance(DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Physical => physicalResistance,
                DamageType.Magical => magicalResistance,
                DamageType.Fire => fireResistance,
                DamageType.Ice => iceResistance,
                DamageType.Lightning => lightningResistance,
                DamageType.Dark => darkResistance,
                _ => 0f
            };
        }
        
        public bool CanDrop(string itemId)
        {
            var drop = drops.Find(d => d.itemId == itemId);
            return drop != null && UnityEngine.Random.value <= drop.dropChance * dropRateMultiplier;
        }
        
        public List<string> GetDroppedItems()
        {
            List<string> droppedItems = new List<string>();
            
            foreach (var drop in drops)
            {
                if (UnityEngine.Random.value <= drop.dropChance * dropRateMultiplier)
                {
                    for (int i = 0; i < drop.quantity; i++)
                    {
                        droppedItems.Add(drop.itemId);
                    }
                }
            }
            
            return droppedItems;
        }
        
        public bool IsWeakTo(EnemyElement attackElement)
        {
            return (element, attackElement) switch
            {
                (EnemyElement.Fire, EnemyElement.Water) => true,
                (EnemyElement.Water, EnemyElement.Lightning) => true,
                (EnemyElement.Lightning, EnemyElement.Earth) => true,
                (EnemyElement.Earth, EnemyElement.Fire) => true,
                (EnemyElement.Light, EnemyElement.Dark) => true,
                (EnemyElement.Dark, EnemyElement.Light) => true,
                _ => false
            };
        }
        
        public float GetDamageMultiplier(EnemyElement attackElement)
        {
            if (IsWeakTo(attackElement))
                return 1.5f;
            else if (element == attackElement)
                return 0.5f;
            else
                return 1f;
        }
        
        public string GetEnemyInfo()
        {
            string info = $"<b>{enemyName}</b>\n";
            info += $"타입: {GetEnemyTypeName()}\n";
            info += $"등급: {GetRankName()}\n";
            info += $"레벨: {level}\n";
            
            if (!string.IsNullOrEmpty(description))
                info += $"\n{description}\n";
            
            info += $"\n<b>스탯:</b>\n";
            info += $"HP: {GetCurrentHP()}\n";
            info += $"공격력: {GetCurrentAttack()}\n";
            info += $"방어력: {GetCurrentDefense()}\n";
            info += $"속도: {speed}\n";
            
            if (skills.Count > 0)
                info += $"\n스킬: {skills.Count}개\n";
                
            info += $"\n<b>보상:</b>\n";
            info += $"경험치: {experienceReward}\n";
            info += $"골드: {goldReward}\n";
            
            if (drops.Count > 0)
                info += $"드롭 아이템: {drops.Count}종류\n";
                
            return info;
        }
        
        private string GetEnemyTypeName()
        {
            return enemyType switch
            {
                EnemyType.Beast => "야수",
                EnemyType.Undead => "언데드",
                EnemyType.Demon => "악마",
                EnemyType.Dragon => "드래곤",
                EnemyType.Elemental => "정령",
                EnemyType.Humanoid => "인간형",
                EnemyType.Construct => "구조물",
                EnemyType.Plant => "식물",
                _ => "알 수 없음"
            };
        }
        
        private string GetRankName()
        {
            return rank switch
            {
                EnemyRank.Minion => "하급",
                EnemyRank.Normal => "일반",
                EnemyRank.Elite => "정예",
                EnemyRank.Champion => "챔피언",
                EnemyRank.Boss => "보스",
                EnemyRank.WorldBoss => "월드보스",
                _ => "일반"
            };
        }
    }
    
    [System.Serializable]
    public class EnemyAbility
    {
        public string abilityId;
        public string name;
        public string description;
        public float cooldown;
        public float lastUsed;
        public EnemyAbilityType type;
        public EnemyAbilityTrigger trigger;
        public float triggerChance = 1f;
        
        public bool CanUse()
        {
            return Time.time >= lastUsed + cooldown;
        }
        
        public void Use()
        {
            lastUsed = Time.time;
        }
    }
    
    [System.Serializable]
    public class EnemyDrop
    {
        public string itemId;
        public int quantity = 1;
        public float dropChance = 0.1f;
        public bool isGuaranteed = false;
        public int minLevel = 1;
        public int maxLevel = 999;
    }
    
    [System.Serializable]
    public class EnemyHabitat
    {
        public BiomeType biome;
        public float spawnChance = 1f;
        public int minGroupSize = 1;
        public int maxGroupSize = 1;
        public float density = 1f;
    }
    
    public enum EnemyType
    {
        Beast,
        Undead,
        Demon,
        Dragon,
        Elemental,
        Humanoid,
        Construct,
        Plant
    }
    
    public enum EnemyCategory
    {
        Common,
        Rare,
        Boss,
        Event,
        Special
    }
    
    public enum EnemyRank
    {
        Minion,
        Normal,
        Elite,
        Champion,
        Boss,
        WorldBoss
    }
    
    public enum EnemyRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    public enum EnemyElement
    {
        None,
        Fire,
        Water,
        Earth,
        Air,
        Lightning,
        Ice,
        Light,
        Dark,
        Poison,
        Arcane
    }
    
    public enum EnemyAIType
    {
        Passive,
        Defensive,
        Aggressive,
        Berserker,
        Tactical,
        Coward,
        Guardian
    }
    
    public enum DamageType
    {
        Physical,
        Magical,
        Fire,
        Ice,
        Lightning,
        Dark,
        Holy,
        Poison
    }
    
    public enum EnemyAbilityType
    {
        Attack,
        Buff,
        Debuff,
        Heal,
        Summon,
        Teleport,
        Shield,
        Rage
    }
    
    public enum EnemyAbilityTrigger
    {
        OnSpawn,
        OnDeath,
        OnLowHealth,
        OnAttack,
        OnDamaged,
        Periodic,
        Manual
    }
    
    public enum BiomeType
    {
        Forest,
        Desert,
        Mountain,
        Swamp,
        Cave,
        Dungeon,
        City,
        Plains
    }
    
    public enum WeatherType
    {
        Clear,
        Rain,
        Storm,
        Fog,
        Snow,
        Heat
    }
} 