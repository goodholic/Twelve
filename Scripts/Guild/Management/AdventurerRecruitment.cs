using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle; // Unit, JobClass를 위해 추가

namespace GuildMaster.Guild
{
    public enum RecruitmentMethod
    {
        GuildPost,      // 길드 모집
        ElitePost,      // 고급 모집
        QuestReward,    // 퀘스트 보상
        EventReward,    // 이벤트 보상
        NPCTrade,       // NPC 거래
        StoryProgress,  // 스토리 진행
        Special         // 특별 영입
    }
    
    public enum AdventurerRarity
    {
        Common = 1,     // 일반 (흰색)
        Uncommon = 2,   // 고급 (녹색)
        Rare = 3,       // 희귀 (파란색)
        Epic = 4,       // 영웅 (보라색)
        Legendary = 5   // 전설 (주황색)
    }
    
    [System.Serializable]
    public class AdventurerTemplate
    {
        public string TemplateId { get; set; }
        public string BaseName { get; set; }
        public JobClass JobClass { get; set; }
        public AdventurerRarity Rarity { get; set; }
        public int BaseLevel { get; set; }
        
        // Stat modifiers
        public float HealthModifier { get; set; }
        public float AttackModifier { get; set; }
        public float DefenseModifier { get; set; }
        public float MagicModifier { get; set; }
        public float SpeedModifier { get; set; }
        
        // Special traits
        public List<string> Traits { get; set; }
        public List<string> Skills { get; set; }
        
        public AdventurerTemplate()
        {
            Traits = new List<string>();
            Skills = new List<string>();
            HealthModifier = 1f;
            AttackModifier = 1f;
            DefenseModifier = 1f;
            MagicModifier = 1f;
            SpeedModifier = 1f;
        }
    }
    
    [System.Serializable]
    public class AdventurerData : Unit
    {
        public string AdventurerId { get; set; }
        public AdventurerRarity Rarity { get; set; }
        public int Experience { get; set; }
        public int ExperienceToNextLevel { get; set; }
        public int StarLevel { get; set; } // 별 등급 (승급)
        public int Potential { get; set; } // 성장 잠재력
        
        // Training stats
        public int TrainingLevel { get; set; }
        public float TrainingBonus { get; set; }
        public DateTime LastTrainingTime { get; set; }
        
        // Equipment
        public string WeaponId { get; set; }
        public string ArmorId { get; set; }
        public string AccessoryId { get; set; }
        
        // Traits and Skills
        public List<string> Traits { get; set; }
        public List<string> LearnedSkills { get; set; }
        public int SkillPoints { get; set; }
        
        // Loyalty and Morale
        public float Loyalty { get; set; } // 0-100
        public float Morale { get; set; } // 0-100
        
        // Stats tracking
        public int BattlesParticipated { get; set; }
        public int BattlesWon { get; set; }
        public int TotalDamageDealt { get; set; }
        public int TotalHealingDone { get; set; }
        
        public AdventurerData(string name, int level, JobClass jobClass, AdventurerRarity rarity) 
            : base(name, level, jobClass)
        {
            AdventurerId = Guid.NewGuid().ToString();
            Rarity = rarity;
            StarLevel = 1;
            Potential = UnityEngine.Random.Range(80, 101); // 80-100
            Loyalty = 50f;
            Morale = 75f;
            
            Traits = new List<string>();
            LearnedSkills = new List<string>();
            
            ApplyRarityBonuses();
            CalculateExperienceRequirement();
        }
        
        void ApplyRarityBonuses()
        {
            float rarityMultiplier = 1f + ((int)Rarity - 1) * 0.1f; // 10% per rarity level
            
            maxHP *= rarityMultiplier;
            currentHP = maxHP;
            attackPower *= rarityMultiplier;
            defense *= rarityMultiplier;
            magicPower *= rarityMultiplier;
            speed *= rarityMultiplier;
            
            // Higher rarity = better crit and accuracy
            criticalRate += (int)Rarity * 0.02f;
            accuracy += (int)Rarity * 0.01f;
        }
        
        void CalculateExperienceRequirement()
        {
            ExperienceToNextLevel = Level * 100 * (1 + StarLevel);
        }
        
        public void AddExperience(int amount)
        {
            // Apply training ground bonus
            var guildManager = Core.GameManager.Instance?.GuildManager;
            if (guildManager != null)
            {
                float expBonus = BuildingEffects.GetCombatBonus(guildManager, JobClass, "experience");
                amount = (int)(amount * (1f + expBonus));
            }
            
            Experience += amount;
            
            while (Experience >= ExperienceToNextLevel)
            {
                LevelUp();
            }
        }
        
        void LevelUp()
        {
            Experience -= ExperienceToNextLevel;
            level++;
            
            // Stat growth based on potential
            float growthRate = Potential / 100f;
            
            // Base stat increases
            float healthGrowth = 20f * growthRate;
            float attackGrowth = 3f * growthRate;
            float defenseGrowth = 2f * growthRate;
            float magicGrowth = 3f * growthRate;
            float speedGrowth = 1.5f * growthRate;
            
            // Job-specific growth bonuses
            switch (JobClass)
            {
                case JobClass.Warrior:
                    healthGrowth *= 1.2f;
                    attackGrowth *= 1.2f;
                    break;
                case JobClass.Knight:
                    healthGrowth *= 1.3f;
                    defenseGrowth *= 1.3f;
                    break;
                case JobClass.Mage:
                    magicGrowth *= 1.5f;
                    break;
                case JobClass.Priest:
                    magicGrowth *= 1.3f;
                    healthGrowth *= 1.1f;
                    break;
                case JobClass.Assassin:
                    attackGrowth *= 1.3f;
                    speedGrowth *= 1.5f;
                    break;
                case JobClass.Ranger:
                    attackGrowth *= 1.2f;
                    speedGrowth *= 1.3f;
                    break;
                case JobClass.Sage:
                    magicGrowth *= 1.2f;
                    defenseGrowth *= 1.1f;
                    break;
            }
            
            // Apply growth
            maxHP += healthGrowth;
            currentHP = maxHP; // Full heal on level up
            attackPower += attackGrowth;
            defense += defenseGrowth;
            magicPower += magicGrowth;
            speed += speedGrowth;
            
            // Gain skill point every 5 levels
            if (Level % 5 == 0)
            {
                SkillPoints++;
            }
            
            CalculateExperienceRequirement();
            
            // Increase loyalty on level up
            ModifyLoyalty(5f);
        }
        
        public void UpgradeStar(List<AdventurerData> materials)
        {
            // Requires same job class adventurers as materials
            int requiredMaterials = StarLevel + 1;
            
            if (materials.Count < requiredMaterials) return;
            if (materials.Any(m => m.JobClass != JobClass)) return;
            if (StarLevel >= 5) return; // Max 5 stars
            
            StarLevel++;
            
            // Stat bonus for star upgrade
            float starBonus = 0.1f; // 10% per star
            maxHP *= (1f + starBonus);
            currentHP = maxHP;
            attackPower *= (1f + starBonus);
            defense *= (1f + starBonus);
            magicPower *= (1f + starBonus);
            speed *= (1f + starBonus);
            
            // Increase potential
            Potential = Mathf.Min(100, Potential + 5);
            
            CalculateExperienceRequirement();
        }
        
        public void Train(float hours)
        {
            if (DateTime.Now.Subtract(LastTrainingTime).TotalHours < 1)
                return; // Can only train once per hour
            
            LastTrainingTime = DateTime.Now;
            TrainingLevel++;
            
            // Training provides temporary stat bonuses
            TrainingBonus = TrainingLevel * 0.01f; // 1% per training level
            
            // Gain small amount of experience
            AddExperience((int)(Level * 10 * hours));
            
            // Increase morale from training
            ModifyMorale(5f);
        }
        
        public void ModifyLoyalty(float amount)
        {
            Loyalty = Mathf.Clamp(Loyalty + amount, 0f, 100f);
        }
        
        public void ModifyMorale(float amount)
        {
            Morale = Mathf.Clamp(Morale + amount, 0f, 100f);
        }
        
        public float GetCombatEffectiveness()
        {
            // Combat effectiveness based on loyalty and morale
            float loyaltyBonus = Loyalty / 100f; // 0-100% bonus
            float moraleBonus = Morale / 100f; // 0-100% bonus
            
            return 1f + (loyaltyBonus * 0.2f) + (moraleBonus * 0.1f) + TrainingBonus;
        }
        
        public new float GetAttackDamage()
        {
            float baseDamage = base.GetAttackDamage();
            return baseDamage * GetCombatEffectiveness();
        }
        
        public new float GetHealPower()
        {
            float baseHeal = base.GetHealPower();
            return baseHeal * GetCombatEffectiveness();
        }
    }
    
    public class RecruitmentManager : MonoBehaviour
    {
        // Templates
        private Dictionary<string, AdventurerTemplate> adventurerTemplates;
        private List<AdventurerData> recruitmentPool;
        
        // Recruitment costs
        private const int BASIC_RECRUITMENT_COST = 100;
        private const int ELITE_RECRUITMENT_COST = 500;
        
        // Events
        public event Action<AdventurerData> OnAdventurerRecruited;
        public event Action<List<AdventurerData>> OnRecruitmentPoolRefreshed;
        
        // Name pools
        private string[] firstNames = { 
            "알렉스", "블레이크", "케이시", "드류", "엘리스", 
            "핀", "그레이", "하퍼", "제이드", "카이",
            "리아", "맥스", "노아", "올리비아", "퀸",
            "라이언", "소피아", "타일러", "우나", "빅터"
        };
        
        private string[] titles = { 
            "용감한", "신속한", "현명한", "강인한", "교활한", 
            "용맹한", "강력한", "침묵의", "불굴의", "전설의",
            "숙련된", "명예로운", "신비한", "정의로운", "은밀한"
        };
        
        void Awake()
        {
            adventurerTemplates = new Dictionary<string, AdventurerTemplate>();
            recruitmentPool = new List<AdventurerData>();
            
            GenerateTemplates();
            RefreshRecruitmentPool();
        }
        
        void GenerateTemplates()
        {
            // Generate templates for each job class and rarity
            foreach (JobClass jobClass in Enum.GetValues(typeof(JobClass)))
            {
                foreach (AdventurerRarity rarity in Enum.GetValues(typeof(AdventurerRarity)))
                {
                    string templateId = $"template_{jobClass}_{rarity}";
                    var template = new AdventurerTemplate
                    {
                        TemplateId = templateId,
                        BaseName = GetJobClassName(jobClass),
                        JobClass = jobClass,
                        Rarity = rarity,
                        BaseLevel = 1
                    };
                    
                    // Set rarity-based modifiers
                    float rarityMod = 1f + ((int)rarity - 1) * 0.15f;
                    template.HealthModifier = rarityMod;
                    template.AttackModifier = rarityMod;
                    template.DefenseModifier = rarityMod;
                    template.MagicModifier = rarityMod;
                    template.SpeedModifier = rarityMod;
                    
                    // Add traits based on rarity
                    AddRarityTraits(template);
                    
                    adventurerTemplates[templateId] = template;
                }
            }
        }
        
        void AddRarityTraits(AdventurerTemplate template)
        {
            switch (template.Rarity)
            {
                case AdventurerRarity.Uncommon:
                    template.Traits.Add("veteran"); // 경험 획득 +10%
                    break;
                case AdventurerRarity.Rare:
                    template.Traits.Add("veteran");
                    template.Traits.Add("talented"); // 스킬 효과 +15%
                    break;
                case AdventurerRarity.Epic:
                    template.Traits.Add("veteran");
                    template.Traits.Add("talented");
                    template.Traits.Add("resilient"); // 받는 피해 -10%
                    break;
                case AdventurerRarity.Legendary:
                    template.Traits.Add("veteran");
                    template.Traits.Add("talented");
                    template.Traits.Add("resilient");
                    template.Traits.Add("heroic"); // 모든 스탯 +20%
                    break;
            }
        }
        
        string GetJobClassName(JobClass jobClass)
        {
            switch (jobClass)
            {
                case JobClass.Warrior: return "전사";
                case JobClass.Knight: return "기사";
                case JobClass.Mage: return "마법사";
                case JobClass.Priest: return "성직자";
                case JobClass.Assassin: return "도적";
                case JobClass.Ranger: return "궁수";
                case JobClass.Sage: return "현자";
                default: return "모험가";
            }
        }
        
        public void RefreshRecruitmentPool()
        {
            recruitmentPool.Clear();
            
            var guildManager = Core.GameManager.Instance?.GuildManager;
            if (guildManager == null) return;
            
            int guildLevel = guildManager.GetGuildData().GuildLevel;
            
            // Generate 5-10 adventurers
            int poolSize = UnityEngine.Random.Range(5, 11);
            
            for (int i = 0; i < poolSize; i++)
            {
                var adventurer = GenerateRandomAdventurer(guildLevel);
                recruitmentPool.Add(adventurer);
            }
            
            OnRecruitmentPoolRefreshed?.Invoke(recruitmentPool);
        }
        
        AdventurerData GenerateRandomAdventurer(int guildLevel)
        {
            // Determine rarity based on guild level
            AdventurerRarity rarity = DetermineRarity(guildLevel);
            
            // Random job class
            JobClass jobClass = (JobClass)UnityEngine.Random.Range(0, 7);
            
            // Generate name
            string firstName = firstNames[UnityEngine.Random.Range(0, firstNames.Length)];
            string title = UnityEngine.Random.Range(0f, 1f) < 0.3f ? 
                titles[UnityEngine.Random.Range(0, titles.Length)] + " " : "";
            string name = title + firstName;
            
            // Level range based on guild level
            int minLevel = Mathf.Max(1, guildLevel - 5);
            int maxLevel = guildLevel + 2;
            int level = UnityEngine.Random.Range(minLevel, maxLevel + 1);
            
            var adventurer = new AdventurerData(name, level, jobClass, rarity);
            
            // Apply template modifiers
            string templateId = $"template_{jobClass}_{rarity}";
            if (adventurerTemplates.ContainsKey(templateId))
            {
                var template = adventurerTemplates[templateId];
                adventurer.Traits = new List<string>(template.Traits);
            }
            
            return adventurer;
        }
        
        AdventurerRarity DetermineRarity(int guildLevel)
        {
            float roll = UnityEngine.Random.value;
            
            // Rarity chances increase with guild level
            float legendaryChance = 0.01f * (guildLevel / 10f);
            float epicChance = 0.05f * (guildLevel / 10f);
            float rareChance = 0.15f * (guildLevel / 10f);
            float uncommonChance = 0.3f;
            
            if (roll < legendaryChance) return AdventurerRarity.Legendary;
            if (roll < legendaryChance + epicChance) return AdventurerRarity.Epic;
            if (roll < legendaryChance + epicChance + rareChance) return AdventurerRarity.Rare;
            if (roll < legendaryChance + epicChance + rareChance + uncommonChance) return AdventurerRarity.Uncommon;
            
            return AdventurerRarity.Common;
        }
        
        public bool RecruitAdventurer(AdventurerData adventurer, RecruitmentMethod method)
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return false;
            
            var resourceManager = gameManager.ResourceManager;
            var guildManager = gameManager.GuildManager;
            
            // Check if guild has space
            var guildData = guildManager.GetGuildData();
            if (guildData.Adventurers.Count >= guildData.MaxAdventurers)
            {
                Debug.LogWarning("Guild is full! Build more dormitories.");
                return false;
            }
            
            // Check recruitment cost
            int cost = GetRecruitmentCost(adventurer, method);
            if (!resourceManager.CanAfford(cost, 0, 0, 0))
            {
                Debug.LogWarning("Not enough gold for recruitment!");
                return false;
            }
            
            // Pay cost
            resourceManager.SpendResources(cost, 0, 0, 0);
            
            // Add to guild
            guildManager.RecruitAdventurer(adventurer);
            
            // Remove from pool
            recruitmentPool.Remove(adventurer);
            
            OnAdventurerRecruited?.Invoke(adventurer);
            
            return true;
        }
        
        int GetRecruitmentCost(AdventurerData adventurer, RecruitmentMethod method)
        {
            int baseCost = method == RecruitmentMethod.ElitePost ? ELITE_RECRUITMENT_COST : BASIC_RECRUITMENT_COST;
            
            // Cost increases with level and rarity
            baseCost += adventurer.Level * 50;
            baseCost *= (int)adventurer.Rarity;
            
            return baseCost;
        }
        
        public AdventurerData CreateSpecialAdventurer(string name, JobClass jobClass, int level, AdventurerRarity rarity)
        {
            var adventurer = new AdventurerData(name, level, jobClass, rarity);
            
            // Special adventurers have higher potential
            adventurer.Potential = 100;
            adventurer.Loyalty = 75f;
            
            return adventurer;
        }
        
        public List<AdventurerData> GetRecruitmentPool()
        {
            return new List<AdventurerData>(recruitmentPool);
        }
        
        public void SimulateGacha(int pulls, bool isElite = false)
        {
            List<AdventurerData> results = new List<AdventurerData>();
            
            for (int i = 0; i < pulls; i++)
            {
                int guildLevel = Core.GameManager.Instance?.GuildManager?.GetGuildData().GuildLevel ?? 1;
                
                // Elite gacha has better rates
                if (isElite)
                {
                    guildLevel += 5;
                }
                
                var adventurer = GenerateRandomAdventurer(guildLevel);
                results.Add(adventurer);
            }
            
            // Add to recruitment pool
            recruitmentPool.AddRange(results);
            OnRecruitmentPoolRefreshed?.Invoke(recruitmentPool);
        }
    }
}