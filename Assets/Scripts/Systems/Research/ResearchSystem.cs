using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuildMaster.Systems
{
    public enum ResearchCategory
    {
        Combat,         // 전투 관련
        Economy,        // 경제 관련
        Technology,     // 기술 관련
        Magic,          // 마법 관련
        Social,         // 사회 관련
        Special         // 특수 연구
    }
    
    public enum ResearchType
    {
        // Combat
        WeaponMastery,
        ArmorCrafting,
        TacticalFormation,
        CriticalStrike,
        DefensiveStance,
        
        // Economy
        ResourceEfficiency,
        TradeRoutes,
        TaxOptimization,
        ProductionBoost,
        MarketManipulation,
        
        // Technology
        BuildingEngineering,
        AdvancedTools,
        AutomationSystems,
        ResearchMethodology,
        
        // Magic
        ElementalMastery,
        HealingArts,
        ManaChanneling,
        ArcaneKnowledge,
        
        // Social
        Diplomacy,
        Leadership,
        Recruitment,
        Morale,
        
        // Special
        AncientKnowledge,
        ForbiddenArts,
        DivineBlessing
    }
    
    [System.Serializable]
    public class Research
    {
        public string ResearchId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ResearchCategory Category { get; set; }
        public ResearchType Type { get; set; }
        public int Level { get; set; }
        public int MaxLevel { get; set; }
        
        // Requirements
        public int RequiredGuildLevel { get; set; }
        public List<string> PrerequisiteResearchIds { get; set; }
        public int ResearchPointsCost { get; set; }
        public int GoldCost { get; set; }
        public int ManaStoneCost { get; set; }
        public float ResearchTime { get; set; } // In seconds
        
        // Effects
        public Dictionary<string, float> Effects { get; set; }
        public List<string> UnlockIds { get; set; } // What this research unlocks
        
        // Progress
        public bool IsUnlocked { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsResearching { get; set; }
        public float ResearchProgress { get; set; }
        public DateTime ResearchStartTime { get; set; }
        
        public Research(string name, ResearchCategory category, ResearchType type)
        {
            ResearchId = Guid.NewGuid().ToString();
            Name = name;
            Category = category;
            Type = type;
            Level = 0;
            MaxLevel = 5;
            PrerequisiteResearchIds = new List<string>();
            Effects = new Dictionary<string, float>();
            UnlockIds = new List<string>();
            
            InitializeResearchData();
        }
        
        void InitializeResearchData()
        {
            // Set base costs and requirements based on type
            switch (Category)
            {
                case ResearchCategory.Combat:
                    ResearchPointsCost = 100;
                    GoldCost = 500;
                    ResearchTime = 300f; // 5 minutes
                    break;
                    
                case ResearchCategory.Economy:
                    ResearchPointsCost = 80;
                    GoldCost = 1000;
                    ResearchTime = 600f; // 10 minutes
                    break;
                    
                case ResearchCategory.Technology:
                    ResearchPointsCost = 150;
                    GoldCost = 800;
                    ManaStoneCost = 50;
                    ResearchTime = 900f; // 15 minutes
                    break;
                    
                case ResearchCategory.Magic:
                    ResearchPointsCost = 120;
                    GoldCost = 600;
                    ManaStoneCost = 100;
                    ResearchTime = 720f; // 12 minutes
                    break;
                    
                case ResearchCategory.Social:
                    ResearchPointsCost = 60;
                    GoldCost = 400;
                    ResearchTime = 480f; // 8 minutes
                    break;
                    
                case ResearchCategory.Special:
                    ResearchPointsCost = 200;
                    GoldCost = 2000;
                    ManaStoneCost = 200;
                    ResearchTime = 1800f; // 30 minutes
                    break;
            }
            
            // Set specific effects based on type
            InitializeTypeEffects();
        }
        
        void InitializeTypeEffects()
        {
            switch (Type)
            {
                case ResearchType.WeaponMastery:
                    Effects["attack_bonus"] = 0.05f; // 5% per level
                    Description = "모든 유닛의 공격력을 증가시킵니다.";
                    break;
                    
                case ResearchType.ArmorCrafting:
                    Effects["defense_bonus"] = 0.05f;
                    Description = "모든 유닛의 방어력을 증가시킵니다.";
                    break;
                    
                case ResearchType.TacticalFormation:
                    Effects["squad_bonus"] = 0.03f;
                    UnlockIds.Add("formation_defensive");
                    UnlockIds.Add("formation_offensive");
                    Description = "새로운 전투 대형을 해금하고 부대 효율을 증가시킵니다.";
                    break;
                    
                case ResearchType.ResourceEfficiency:
                    Effects["resource_production"] = 0.1f;
                    Description = "모든 자원 생산량을 증가시킵니다.";
                    break;
                    
                case ResearchType.TradeRoutes:
                    Effects["trade_income"] = 0.15f;
                    Effects["merchant_frequency"] = 0.2f;
                    Description = "교역 수입과 상인 방문 빈도를 증가시킵니다.";
                    break;
                    
                case ResearchType.BuildingEngineering:
                    Effects["build_speed"] = 0.2f;
                    Effects["building_cost_reduction"] = 0.1f;
                    Description = "건설 속도를 증가시키고 건설 비용을 감소시킵니다.";
                    break;
                    
                case ResearchType.ElementalMastery:
                    Effects["magic_damage"] = 0.08f;
                    UnlockIds.Add("skill_meteor");
                    UnlockIds.Add("skill_blizzard");
                    Description = "마법 공격력을 증가시키고 새로운 마법을 해금합니다.";
                    break;
                    
                case ResearchType.HealingArts:
                    Effects["healing_power"] = 0.1f;
                    Effects["resurrection_chance"] = 0.05f;
                    Description = "치유 효과를 증가시키고 부활 확률을 추가합니다.";
                    break;
                    
                case ResearchType.Diplomacy:
                    Effects["reputation_gain"] = 0.15f;
                    Effects["relationship_improvement"] = 0.1f;
                    Description = "명성 획득량과 관계 개선 속도를 증가시킵니다.";
                    break;
                    
                case ResearchType.Leadership:
                    Effects["unit_morale"] = 0.1f;
                    Effects["unit_loyalty"] = 0.1f;
                    Description = "유닛의 사기와 충성도를 증가시킵니다.";
                    break;
                    
                case ResearchType.Recruitment:
                    Effects["recruitment_cost_reduction"] = 0.1f;
                    Effects["rare_unit_chance"] = 0.05f;
                    Description = "모집 비용을 감소시키고 희귀 유닛 출현 확률을 증가시킵니다.";
                    break;
                    
                case ResearchType.AncientKnowledge:
                    Effects["all_research_speed"] = 0.2f;
                    Effects["experience_gain"] = 0.15f;
                    UnlockIds.Add("hidden_dungeon_ancient");
                    Description = "모든 연구 속도와 경험치 획득량을 증가시키며, 고대 던전을 해금합니다.";
                    RequiredGuildLevel = 15;
                    break;
                    
                case ResearchType.ForbiddenArts:
                    Effects["dark_power"] = 0.25f;
                    Effects["unit_corruption"] = -0.1f; // Negative effect
                    UnlockIds.Add("class_dark_knight");
                    UnlockIds.Add("class_necromancer");
                    Description = "강력한 어둠의 힘을 얻지만 유닛이 타락할 수 있습니다.";
                    RequiredGuildLevel = 20;
                    break;
            }
        }
        
        public bool CanResearch(Core.GameManager gameManager)
        {
            if (IsCompleted || IsResearching) return false;
            if (Level >= MaxLevel) return false;
            
            // Check guild level
            if (gameManager.GuildManager.GetGuildData().GuildLevel < RequiredGuildLevel)
                return false;
            
            // Check prerequisites
            // TODO: Check if prerequisite researches are completed
            
            // Check resources
            if (!gameManager.ResourceManager.CanAfford(GoldCost, 0, 0, ManaStoneCost))
                return false;
            
            // Check research points
            // TODO: Implement research points system
            
            return true;
        }
        
        public float GetCurrentEffect(string effectKey)
        {
            if (!Effects.ContainsKey(effectKey)) return 0f;
            return Effects[effectKey] * Level;
        }
        
        public int GetNextLevelCost()
        {
            return ResearchPointsCost * (Level + 1);
        }
        
        public float GetNextLevelTime()
        {
            return ResearchTime * (1f + Level * 0.2f); // 20% longer per level
        }
    }
    
    [System.Serializable]
    public class TechnologyTree
    {
        public string TreeId { get; set; }
        public string Name { get; set; }
        public ResearchCategory Category { get; set; }
        public List<Research> Researches { get; set; }
        public Dictionary<string, List<string>> Dependencies { get; set; } // ResearchId -> Prerequisite IDs
        
        public TechnologyTree(string name, ResearchCategory category)
        {
            TreeId = Guid.NewGuid().ToString();
            Name = name;
            Category = category;
            Researches = new List<Research>();
            Dependencies = new Dictionary<string, List<string>>();
        }
        
        public void AddResearch(Research research, params string[] prerequisites)
        {
            Researches.Add(research);
            
            if (prerequisites.Length > 0)
            {
                Dependencies[research.ResearchId] = prerequisites.ToList();
                research.PrerequisiteResearchIds = prerequisites.ToList();
            }
        }
    }
    
    public class ResearchManager : MonoBehaviour
    {
        // Research data
        private Dictionary<string, Research> allResearches;
        private Dictionary<ResearchCategory, TechnologyTree> technologyTrees;
        private Queue<Research> researchQueue;
        private Research currentResearch;
        
        // Research points
        private int researchPoints;
        private float researchPointsGenerationRate;
        private float lastResearchPointUpdate;
        
        // Events
        public event Action<Research> OnResearchStarted;
        public event Action<Research> OnResearchCompleted;
        public event Action<Research> OnResearchCancelled;
        public event Action<string> OnTechnologyUnlocked;
        public event Action<int> OnResearchPointsChanged;
        
        // Configuration
        private const float RESEARCH_POINT_UPDATE_INTERVAL = 60f; // 1 minute
        private const int BASE_RESEARCH_POINT_GENERATION = 1;
        
        void Awake()
        {
            allResearches = new Dictionary<string, Research>();
            technologyTrees = new Dictionary<ResearchCategory, TechnologyTree>();
            researchQueue = new Queue<Research>();
            
            InitializeTechnologyTrees();
        }
        
        void InitializeTechnologyTrees()
        {
            // Combat Tree
            var combatTree = new TechnologyTree("전투 기술", ResearchCategory.Combat);
            
            var weaponMastery = new Research("무기 숙련", ResearchCategory.Combat, ResearchType.WeaponMastery);
            combatTree.AddResearch(weaponMastery);
            
            var armorCrafting = new Research("갑옷 제작", ResearchCategory.Combat, ResearchType.ArmorCrafting);
            combatTree.AddResearch(armorCrafting);
            
            var tacticalFormation = new Research("전술 대형", ResearchCategory.Combat, ResearchType.TacticalFormation);
            combatTree.AddResearch(tacticalFormation, weaponMastery.ResearchId, armorCrafting.ResearchId);
            
            var criticalStrike = new Research("치명타 연구", ResearchCategory.Combat, ResearchType.CriticalStrike);
            combatTree.AddResearch(criticalStrike, weaponMastery.ResearchId);
            
            var defensiveStance = new Research("방어 태세", ResearchCategory.Combat, ResearchType.DefensiveStance);
            combatTree.AddResearch(defensiveStance, armorCrafting.ResearchId);
            
            technologyTrees[ResearchCategory.Combat] = combatTree;
            
            // Economy Tree
            var economyTree = new TechnologyTree("경제 기술", ResearchCategory.Economy);
            
            var resourceEfficiency = new Research("자원 효율", ResearchCategory.Economy, ResearchType.ResourceEfficiency);
            economyTree.AddResearch(resourceEfficiency);
            
            var tradeRoutes = new Research("교역로", ResearchCategory.Economy, ResearchType.TradeRoutes);
            economyTree.AddResearch(tradeRoutes);
            
            var taxOptimization = new Research("세금 최적화", ResearchCategory.Economy, ResearchType.TaxOptimization);
            economyTree.AddResearch(taxOptimization, resourceEfficiency.ResearchId);
            
            var productionBoost = new Research("생산 증대", ResearchCategory.Economy, ResearchType.ProductionBoost);
            economyTree.AddResearch(productionBoost, resourceEfficiency.ResearchId);
            
            var marketManipulation = new Research("시장 조작", ResearchCategory.Economy, ResearchType.MarketManipulation);
            economyTree.AddResearch(marketManipulation, tradeRoutes.ResearchId, taxOptimization.ResearchId);
            
            technologyTrees[ResearchCategory.Economy] = economyTree;
            
            // Technology Tree
            var techTree = new TechnologyTree("기술 발전", ResearchCategory.Technology);
            
            var buildingEngineering = new Research("건축 공학", ResearchCategory.Technology, ResearchType.BuildingEngineering);
            techTree.AddResearch(buildingEngineering);
            
            var advancedTools = new Research("고급 도구", ResearchCategory.Technology, ResearchType.AdvancedTools);
            techTree.AddResearch(advancedTools);
            
            var automationSystems = new Research("자동화 시스템", ResearchCategory.Technology, ResearchType.AutomationSystems);
            techTree.AddResearch(automationSystems, buildingEngineering.ResearchId, advancedTools.ResearchId);
            
            var researchMethodology = new Research("연구 방법론", ResearchCategory.Technology, ResearchType.ResearchMethodology);
            techTree.AddResearch(researchMethodology, advancedTools.ResearchId);
            
            technologyTrees[ResearchCategory.Technology] = techTree;
            
            // Magic Tree
            var magicTree = new TechnologyTree("마법 연구", ResearchCategory.Magic);
            
            var elementalMastery = new Research("원소 숙달", ResearchCategory.Magic, ResearchType.ElementalMastery);
            magicTree.AddResearch(elementalMastery);
            
            var healingArts = new Research("치유술", ResearchCategory.Magic, ResearchType.HealingArts);
            magicTree.AddResearch(healingArts);
            
            var manaChanneling = new Research("마나 전달", ResearchCategory.Magic, ResearchType.ManaChanneling);
            magicTree.AddResearch(manaChanneling, elementalMastery.ResearchId, healingArts.ResearchId);
            
            var arcaneKnowledge = new Research("비전 지식", ResearchCategory.Magic, ResearchType.ArcaneKnowledge);
            arcaneKnowledge.RequiredGuildLevel = 10;
            magicTree.AddResearch(arcaneKnowledge, manaChanneling.ResearchId);
            
            technologyTrees[ResearchCategory.Magic] = magicTree;
            
            // Social Tree
            var socialTree = new TechnologyTree("사회 발전", ResearchCategory.Social);
            
            var diplomacy = new Research("외교술", ResearchCategory.Social, ResearchType.Diplomacy);
            socialTree.AddResearch(diplomacy);
            
            var leadership = new Research("지도력", ResearchCategory.Social, ResearchType.Leadership);
            socialTree.AddResearch(leadership);
            
            var recruitment = new Research("모집 기술", ResearchCategory.Social, ResearchType.Recruitment);
            socialTree.AddResearch(recruitment, leadership.ResearchId);
            
            var morale = new Research("사기 진작", ResearchCategory.Social, ResearchType.Morale);
            socialTree.AddResearch(morale, leadership.ResearchId, diplomacy.ResearchId);
            
            technologyTrees[ResearchCategory.Social] = socialTree;
            
            // Special Tree
            var specialTree = new TechnologyTree("특수 연구", ResearchCategory.Special);
            
            var ancientKnowledge = new Research("고대 지식", ResearchCategory.Special, ResearchType.AncientKnowledge);
            ancientKnowledge.RequiredGuildLevel = 15;
            specialTree.AddResearch(ancientKnowledge);
            
            var forbiddenArts = new Research("금지된 기술", ResearchCategory.Special, ResearchType.ForbiddenArts);
            forbiddenArts.RequiredGuildLevel = 20;
            specialTree.AddResearch(forbiddenArts, ancientKnowledge.ResearchId);
            
            var divineBlessing = new Research("신의 축복", ResearchCategory.Special, ResearchType.DivineBlessing);
            divineBlessing.RequiredGuildLevel = 25;
            specialTree.AddResearch(divineBlessing, ancientKnowledge.ResearchId);
            
            technologyTrees[ResearchCategory.Special] = specialTree;
            
            // Add all researches to dictionary
            foreach (var tree in technologyTrees.Values)
            {
                foreach (var research in tree.Researches)
                {
                    allResearches[research.ResearchId] = research;
                }
            }
            
            // Unlock initial researches
            UnlockInitialResearches();
        }
        
        void UnlockInitialResearches()
        {
            // Unlock researches with no prerequisites
            foreach (var research in allResearches.Values)
            {
                if (research.PrerequisiteResearchIds.Count == 0)
                {
                    research.IsUnlocked = true;
                }
            }
        }
        
        void Update()
        {
            // Update research progress
            if (currentResearch != null && currentResearch.IsResearching)
            {
                UpdateResearchProgress();
            }
            
            // Generate research points
            if (Time.time - lastResearchPointUpdate >= RESEARCH_POINT_UPDATE_INTERVAL)
            {
                GenerateResearchPoints();
                lastResearchPointUpdate = Time.time;
            }
            
            // Process research queue
            if (currentResearch == null && researchQueue.Count > 0)
            {
                StartNextResearch();
            }
        }
        
        void UpdateResearchProgress()
        {
            float elapsedTime = (float)(DateTime.Now - currentResearch.ResearchStartTime).TotalSeconds;
            currentResearch.ResearchProgress = elapsedTime / currentResearch.GetNextLevelTime();
            
            if (currentResearch.ResearchProgress >= 1f)
            {
                CompleteResearch(currentResearch);
            }
        }
        
        void GenerateResearchPoints()
        {
            int pointsGenerated = BASE_RESEARCH_POINT_GENERATION;
            
            // Apply research lab bonuses
            var guildManager = Core.GameManager.Instance?.GuildManager;
            if (guildManager != null)
            {
                var buildings = guildManager.GetGuildData().Buildings;
                var researchLabs = buildings.Where(b => b.Type == Core.GuildManager.BuildingType.ResearchLab);
                
                foreach (var lab in researchLabs)
                {
                    pointsGenerated += lab.Level;
                }
            }
            
            // Apply research speed bonuses
            float speedBonus = GetTotalResearchSpeedBonus();
            pointsGenerated = (int)(pointsGenerated * (1f + speedBonus));
            
            AddResearchPoints(pointsGenerated);
        }
        
        float GetTotalResearchSpeedBonus()
        {
            float bonus = 0f;
            
            // Check completed researches for speed bonuses
            foreach (var research in allResearches.Values.Where(r => r.IsCompleted))
            {
                bonus += research.GetCurrentEffect("all_research_speed");
            }
            
            return bonus;
        }
        
        public bool StartResearch(string researchId)
        {
            if (!allResearches.ContainsKey(researchId)) return false;
            
            var research = allResearches[researchId];
            var gameManager = Core.GameManager.Instance;
            
            if (!research.CanResearch(gameManager)) return false;
            
            // Check if prerequisites are completed
            foreach (var prereqId in research.PrerequisiteResearchIds)
            {
                if (!allResearches[prereqId].IsCompleted)
                    return false;
            }
            
            // Pay costs
            int pointsCost = research.GetNextLevelCost();
            if (researchPoints < pointsCost) return false;
            
            researchPoints -= pointsCost;
            gameManager.ResourceManager.SpendResources(research.GoldCost, 0, 0, research.ManaStoneCost);
            
            // Start research
            research.IsResearching = true;
            research.ResearchStartTime = DateTime.Now;
            research.ResearchProgress = 0f;
            
            if (currentResearch == null)
            {
                currentResearch = research;
            }
            else
            {
                researchQueue.Enqueue(research);
            }
            
            OnResearchStarted?.Invoke(research);
            OnResearchPointsChanged?.Invoke(researchPoints);
            
            return true;
        }
        
        void StartNextResearch()
        {
            if (researchQueue.Count > 0)
            {
                currentResearch = researchQueue.Dequeue();
                currentResearch.ResearchStartTime = DateTime.Now;
            }
        }
        
        void CompleteResearch(Research research)
        {
            research.Level++;
            research.IsResearching = false;
            research.ResearchProgress = 0f;
            
            if (research.Level >= research.MaxLevel)
            {
                research.IsCompleted = true;
            }
            
            // Apply effects
            ApplyResearchEffects(research);
            
            // Unlock new researches
            UnlockDependentResearches(research);
            
            // Process unlocks
            foreach (var unlockId in research.UnlockIds)
            {
                OnTechnologyUnlocked?.Invoke(unlockId);
            }
            
            OnResearchCompleted?.Invoke(research);
            
            // Start next research
            currentResearch = null;
            if (researchQueue.Count > 0)
            {
                StartNextResearch();
            }
        }
        
        void ApplyResearchEffects(Research research)
        {
            // Effects are applied dynamically when checked by other systems
            // This allows for easy effect stacking and removal
            
            // Special immediate effects
            if (research.Effects.ContainsKey("unlock_building"))
            {
                // TODO: Unlock specific buildings
            }
            
            if (research.Effects.ContainsKey("unlock_unit"))
            {
                // TODO: Unlock specific unit types
            }
        }
        
        void UnlockDependentResearches(Research completedResearch)
        {
            foreach (var research in allResearches.Values)
            {
                if (research.IsUnlocked) continue;
                
                if (research.PrerequisiteResearchIds.Contains(completedResearch.ResearchId))
                {
                    // Check if all prerequisites are met
                    bool allPrereqsMet = true;
                    foreach (var prereqId in research.PrerequisiteResearchIds)
                    {
                        if (!allResearches[prereqId].IsCompleted)
                        {
                            allPrereqsMet = false;
                            break;
                        }
                    }
                    
                    if (allPrereqsMet)
                    {
                        research.IsUnlocked = true;
                    }
                }
            }
        }
        
        public void CancelResearch(string researchId)
        {
            if (!allResearches.ContainsKey(researchId)) return;
            
            var research = allResearches[researchId];
            
            if (research == currentResearch)
            {
                currentResearch = null;
                research.IsResearching = false;
                research.ResearchProgress = 0f;
                
                // Refund partial resources
                int refundPoints = (int)(research.GetNextLevelCost() * (1f - research.ResearchProgress) * 0.5f);
                AddResearchPoints(refundPoints);
                
                OnResearchCancelled?.Invoke(research);
                
                // Start next in queue
                if (researchQueue.Count > 0)
                {
                    StartNextResearch();
                }
            }
            else
            {
                // Remove from queue
                var newQueue = new Queue<Research>();
                while (researchQueue.Count > 0)
                {
                    var queuedResearch = researchQueue.Dequeue();
                    if (queuedResearch != research)
                    {
                        newQueue.Enqueue(queuedResearch);
                    }
                }
                researchQueue = newQueue;
                
                research.IsResearching = false;
                
                // Full refund if not started
                AddResearchPoints(research.GetNextLevelCost());
                
                OnResearchCancelled?.Invoke(research);
            }
        }
        
        public void AddResearchPoints(int amount)
        {
            researchPoints += amount;
            OnResearchPointsChanged?.Invoke(researchPoints);
        }
        
        public float GetResearchBonus(string bonusKey)
        {
            float totalBonus = 0f;
            
            foreach (var research in allResearches.Values.Where(r => r.Level > 0))
            {
                totalBonus += research.GetCurrentEffect(bonusKey);
            }
            
            return totalBonus;
        }
        
        public bool IsResearchCompleted(ResearchType type)
        {
            return allResearches.Values.Any(r => r.Type == type && r.IsCompleted);
        }
        
        public bool IsTechnologyUnlocked(string unlockId)
        {
            return allResearches.Values.Any(r => r.Level > 0 && r.UnlockIds.Contains(unlockId));
        }
        
        // Getters
        public Dictionary<ResearchCategory, TechnologyTree> GetTechnologyTrees()
        {
            return new Dictionary<ResearchCategory, TechnologyTree>(technologyTrees);
        }
        
        public List<Research> GetAvailableResearches()
        {
            return allResearches.Values.Where(r => r.IsUnlocked && !r.IsCompleted).ToList();
        }
        
        public List<Research> GetCompletedResearches()
        {
            return allResearches.Values.Where(r => r.IsCompleted).ToList();
        }
        
        public Research GetCurrentResearch()
        {
            return currentResearch;
        }
        
        public Queue<Research> GetResearchQueue()
        {
            return new Queue<Research>(researchQueue);
        }
        
        public int GetResearchPoints()
        {
            return researchPoints;
        }
        
        public Research GetResearch(string researchId)
        {
            return allResearches.ContainsKey(researchId) ? allResearches[researchId] : null;
        }
    }
}