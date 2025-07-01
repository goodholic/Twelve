using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle; // Unit, JobClass를 위해 추가
using GuildMaster.Guild; // Building 클래스를 위해 추가

namespace GuildMaster.Core
{
    public class GuildManager : MonoBehaviour
    {
        private static GuildManager _instance;
        public static GuildManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GuildManager>();
                }
                return _instance;
            }
        }
        // Guild Data
        public class GuildData
        {
            public string GuildName { get; set; } = "Adventurer's Guild";
            public int GuildLevel { get; set; } = 1;
            public int GuildReputation { get; set; } = 0;
            public int MaxAdventurers { get; set; } = 36;
            public List<Unit> Adventurers { get; set; } = new List<Unit>();
            public List<Building> Buildings { get; set; } = new List<Building>();
        }

        // Building Types
        public enum BuildingType
        {
            GuildHall,      // 길드홀: 중심 건물
            TrainingGround, // 훈련소: 경험치 획득량 증가
            ResearchLab,    // 연구소: 새로운 직업/스킬 연구
            Dormitory,      // 숙소: 모험가 수용 인원 확장
            Armory,         // 무기고: 장비 제작 및 강화
            MageTower,      // 마법탑: 마법사 계열 스킬 강화
            Temple,         // 신전: 성직자 계열 치유 효과 증가
            Shop,           // 상점: 자동 수익 창출
            Warehouse,      // 창고: 자원 보관 용량 확장
            ScoutPost       // 정찰대: 새로운 지역/던전 발견 속도 증가
        }
        
        // Building Category
        public enum BuildingCategory
        {
            Core,           // 핵심 시설
            Production,     // 생산 시설
            Military,       // 군사 시설
            Support,        // 지원 시설
            Special         // 특수 시설
        }

        // Building Class
        [System.Serializable]
        public class Building
        {
            public BuildingType Type { get; set; }
            public int Level { get; set; }
            public Vector2Int Position { get; set; }
            public Vector2Int Size { get; set; } // 건물 크기 (1x1, 2x2 등)
            public bool IsConstructing { get; set; }
            public float ConstructionTimeRemaining { get; set; }
            public float ProductionRate { get; set; }
            public float LastProductionTime { get; set; }
            public BuildingCategory Category { get; set; }
            public List<BuildingSynergy> ActiveSynergies { get; set; } = new List<BuildingSynergy>();
            public float EfficiencyMultiplier { get; set; } = 1f;

            public Building(BuildingType type, Vector2Int position)
            {
                Type = type;
                Position = position;
                Level = 1;
                IsConstructing = false;
                ConstructionTimeRemaining = 0f;
                ProductionRate = GetBaseProductionRate();
                LastProductionTime = Time.time;
                Category = GetBuildingCategory(type);
                Size = GetBuildingSize(type);
            }
            
            static BuildingCategory GetBuildingCategory(BuildingType type)
            {
                switch (type)
                {
                    case BuildingType.GuildHall:
                        return BuildingCategory.Core;
                    case BuildingType.Shop:
                    case BuildingType.Warehouse:
                        return BuildingCategory.Production;
                    case BuildingType.TrainingGround:
                    case BuildingType.Armory:
                        return BuildingCategory.Military;
                    case BuildingType.Dormitory:
                    case BuildingType.Temple:
                        return BuildingCategory.Support;
                    case BuildingType.MageTower:
                    case BuildingType.ResearchLab:
                    case BuildingType.ScoutPost:
                        return BuildingCategory.Special;
                    default:
                        return BuildingCategory.Core;
                }
            }
            
            public static Vector2Int GetBuildingSize(BuildingType type)
            {
                switch (type)
                {
                    case BuildingType.GuildHall:
                        return new Vector2Int(4, 4);
                    case BuildingType.TrainingGround:
                        return new Vector2Int(3, 3);
                    case BuildingType.ResearchLab:
                        return new Vector2Int(2, 3);
                    case BuildingType.Dormitory:
                        return new Vector2Int(3, 2);
                    case BuildingType.Armory:
                        return new Vector2Int(2, 2);
                    case BuildingType.MageTower:
                        return new Vector2Int(2, 2);
                    case BuildingType.Temple:
                        return new Vector2Int(2, 3);
                    case BuildingType.Shop:
                        return new Vector2Int(2, 2);
                    case BuildingType.Warehouse:
                        return new Vector2Int(3, 2);
                    case BuildingType.ScoutPost:
                        return new Vector2Int(1, 1);
                    default:
                        return new Vector2Int(2, 2);
                }
            }

            float GetBaseProductionRate()
            {
                switch (Type)
                {
                    case BuildingType.Shop:
                        return 10f; // Gold per minute
                    case BuildingType.TrainingGround:
                        return 5f; // Experience per minute
                    case BuildingType.ResearchLab:
                        return 1f; // Research points per minute
                    default:
                        return 0f;
                }
            }

            public float GetProductionAmount()
            {
                return ProductionRate * Level;
            }

            public float GetCurrentEffect()
            {
                float baseEffect = 0f;
                
                switch (Type)
                {
                    case BuildingType.TrainingGround:
                        baseEffect = 10f; // 10% experience bonus per level
                        break;
                    case BuildingType.Armory:
                        baseEffect = 5f; // 5% attack/defense bonus per level
                        break;
                    case BuildingType.MageTower:
                        baseEffect = 8f; // 8% magic power bonus per level
                        break;
                    case BuildingType.Temple:
                        baseEffect = 12f; // 12% healing bonus per level
                        break;
                    case BuildingType.Warehouse:
                        baseEffect = 500f; // +500 storage capacity per level
                        break;
                    case BuildingType.Shop:
                        baseEffect = 20f; // +20% gold income per level
                        break;
                    case BuildingType.ResearchLab:
                        baseEffect = 15f; // 15% research speed per level
                        break;
                    case BuildingType.ScoutPost:
                        baseEffect = 25f; // 25% exploration speed per level
                        break;
                    case BuildingType.Dormitory:
                        baseEffect = 9f; // +9 adventurer capacity per level
                        break;
                    default:
                        baseEffect = 5f; // Default 5% bonus per level
                        break;
                }
                
                // Apply efficiency multiplier from synergies
                return baseEffect * Level * EfficiencyMultiplier;
            }

            public int GetUpgradeCost()
            {
                return Level * 100 * (int)Mathf.Pow(1.5f, Level - 1);
            }

            public float GetUpgradeTime()
            {
                return 60f * Level; // 1 minute per level
            }
        }

        // Guild Grid
        public const int GUILD_GRID_SIZE = 10;
        private Building[,] guildGrid = new Building[GUILD_GRID_SIZE, GUILD_GRID_SIZE];
        
        // Building Synergy System
        public class BuildingSynergy
        {
            public string Name { get; set; }
            public List<BuildingType> RequiredBuildings { get; set; }
            public float EfficiencyBonus { get; set; }
            public string Description { get; set; }
        }
        
        private List<BuildingSynergy> availableSynergies = new List<BuildingSynergy>
        {
            new BuildingSynergy
            {
                Name = "군사 복합체",
                RequiredBuildings = new List<BuildingType> { BuildingType.TrainingGround, BuildingType.Armory },
                EfficiencyBonus = 0.25f,
                Description = "훈련소와 무기고가 인접 시 효율 25% 증가"
            },
            new BuildingSynergy
            {
                Name = "마법 연구단지",
                RequiredBuildings = new List<BuildingType> { BuildingType.MageTower, BuildingType.ResearchLab },
                EfficiencyBonus = 0.30f,
                Description = "마법탑과 연구소가 인접 시 효율 30% 증가"
            },
            new BuildingSynergy
            {
                Name = "상업 지구",
                RequiredBuildings = new List<BuildingType> { BuildingType.Shop, BuildingType.Warehouse },
                EfficiencyBonus = 0.35f,
                Description = "상점과 창고가 인접 시 효율 35% 증가"
            },
            new BuildingSynergy
            {
                Name = "신성 구역",
                RequiredBuildings = new List<BuildingType> { BuildingType.Temple, BuildingType.GuildHall },
                EfficiencyBonus = 0.20f,
                Description = "신전과 길드홀이 인접 시 효율 20% 증가"
            }
        };

        // Current Data
        private GuildData currentGuild;
        
        // Events
        public event Action<Building> OnBuildingConstructed;
        public event Action<Building> OnBuildingUpgraded;
        public event Action<Unit> OnAdventurerRecruited;
        public event Action<int> OnGuildLevelUp;
        public event Action<int> OnReputationChanged;
        public event Action<Unit> OnAdventurerRemoved;

        // Building Queue
        private Queue<BuildingTask> buildingQueue = new Queue<BuildingTask>();
        private BuildingTask currentBuildingTask;

        public class BuildingTask
        {
            public BuildingType Type { get; set; }
            public Vector2Int Position { get; set; }
            public bool IsUpgrade { get; set; }
            public Building TargetBuilding { get; set; }
            public float TimeRequired { get; set; }
            public float TimeRemaining { get; set; }
        }

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            currentGuild = new GuildData();
        }

        public IEnumerator Initialize()
        {
            // Create initial guild hall
            Building guildHall = new Building(BuildingType.GuildHall, new Vector2Int(5, 5));
            PlaceBuilding(BuildingType.GuildHall, new Vector2Int(5, 5));
            
            // Create initial adventurers
            CreateInitialAdventurers();
            
            yield return null;
        }

        void CreateInitialAdventurers()
        {
            // Create 9 starting adventurers (1 squad)
            JobClass[] startingClasses = {
                JobClass.Warrior, JobClass.Warrior, JobClass.Knight,
                JobClass.Ranger, JobClass.Ranger, JobClass.Priest,
                JobClass.Mage, JobClass.Assassin, JobClass.Sage
            };

            for (int i = 0; i < startingClasses.Length; i++)
            {
                Unit adventurer = new Unit($"Adventurer {i + 1}", 1, startingClasses[i]);
                adventurer.isPlayerUnit = true;
                RecruitAdventurer(adventurer);
            }
        }

        void Update()
        {
            // Process building construction
            ProcessBuildingQueue();
            
            // Process building production
            ProcessBuildingProduction();
        }

        void ProcessBuildingQueue()
        {
            if (currentBuildingTask != null)
            {
                currentBuildingTask.TimeRemaining -= Time.deltaTime;
                
                if (currentBuildingTask.TimeRemaining <= 0)
                {
                    CompleteBuildingTask(currentBuildingTask);
                    currentBuildingTask = null;
                }
            }
            
            if (currentBuildingTask == null && buildingQueue.Count > 0)
            {
                currentBuildingTask = buildingQueue.Dequeue();
            }
        }

        void ProcessBuildingProduction()
        {
            foreach (var building in currentGuild.Buildings)
            {
                if (building.IsConstructing) continue;
                
                float timeSinceLastProduction = Time.time - building.LastProductionTime;
                if (timeSinceLastProduction >= 60f) // Production every minute
                {
                    ProduceResources(building);
                    building.LastProductionTime = Time.time;
                }
            }
        }

        void ProduceResources(Building building)
        {
            ResourceManager resourceManager = GameManager.Instance.ResourceManager;
            if (resourceManager == null) return;
            
            float productionAmount = building.GetProductionAmount();
            
            switch (building.Type)
            {
                case BuildingType.Shop:
                    resourceManager.AddGold((int)productionAmount);
                    break;
                case BuildingType.TrainingGround:
                    // Add experience to adventurers
                    DistributeExperience(productionAmount);
                    break;
                case BuildingType.ResearchLab:
                    // Add research points (to be implemented)
                    break;
            }
        }

        void DistributeExperience(float totalExp)
        {
            if (currentGuild.Adventurers.Count == 0) return;
            
            float expPerAdventurer = totalExp / currentGuild.Adventurers.Count;
            foreach (var adventurer in currentGuild.Adventurers)
            {
                // TODO: Implement experience system
            }
        }

        public bool CanPlaceBuilding(BuildingType type, Vector2Int position)
        {
            Vector2Int size = Building.GetBuildingSize(type);
            
            // Check if all required cells are within bounds and empty
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    int gridX = position.x + x;
                    int gridY = position.y + y;
                    
                    if (gridX < 0 || gridX >= GUILD_GRID_SIZE ||
                        gridY < 0 || gridY >= GUILD_GRID_SIZE)
                    {
                        return false;
                    }
                    
                    if (guildGrid[gridX, gridY] != null)
                    {
                        return false;
                    }
                }
            }
            
            // Check building limits
            if (!CheckBuildingLimits(type))
            {
                return false;
            }
            
            // Check if player has resources
            ResourceManager resourceManager = GameManager.Instance.ResourceManager;
            if (resourceManager == null) return false;
            
            BuildingCost cost = GetBuildingCost(type);
            return resourceManager.CanAfford(cost.Gold, cost.Wood, cost.Stone, cost.ManaStone);
        }
        
        bool CheckBuildingLimits(BuildingType type)
        {
            int currentCount = currentGuild.Buildings.Count(b => b.Type == type);
            
            switch (type)
            {
                case BuildingType.GuildHall:
                    return currentCount < 1; // Only one guild hall
                case BuildingType.MageTower:
                case BuildingType.Temple:
                    return currentCount < 2; // Max 2 of these special buildings
                case BuildingType.ResearchLab:
                    return currentCount < 3; // Max 3 research labs
                default:
                    return true; // No limit for other buildings
            }
        }

        public struct BuildingCost
        {
            public int Gold;
            public int Wood;
            public int Stone;
            public int ManaStone;
        }

        public BuildingCost GetBuildingCost(BuildingType type)
        {
            BuildingCost cost = new BuildingCost();
            
            switch (type)
            {
                case BuildingType.GuildHall:
                    cost.Gold = 0;
                    break;
                case BuildingType.TrainingGround:
                    cost.Gold = 100;
                    cost.Wood = 50;
                    break;
                case BuildingType.ResearchLab:
                    cost.Gold = 200;
                    cost.Stone = 100;
                    cost.ManaStone = 50;
                    break;
                case BuildingType.Dormitory:
                    cost.Gold = 150;
                    cost.Wood = 100;
                    cost.Stone = 50;
                    break;
                case BuildingType.Armory:
                    cost.Gold = 250;
                    cost.Stone = 150;
                    break;
                case BuildingType.MageTower:
                    cost.Gold = 300;
                    cost.Stone = 100;
                    cost.ManaStone = 100;
                    break;
                case BuildingType.Temple:
                    cost.Gold = 200;
                    cost.Stone = 150;
                    cost.ManaStone = 50;
                    break;
                case BuildingType.Shop:
                    cost.Gold = 100;
                    cost.Wood = 100;
                    break;
                case BuildingType.Warehouse:
                    cost.Gold = 150;
                    cost.Wood = 150;
                    cost.Stone = 100;
                    break;
                case BuildingType.ScoutPost:
                    cost.Gold = 100;
                    cost.Wood = 80;
                    break;
            }
            
            return cost;
        }

        public void ConstructBuilding(BuildingType type, Vector2Int position)
        {
            if (!CanPlaceBuilding(type, position)) return;
            
            // Deduct resources
            ResourceManager resourceManager = GameManager.Instance.ResourceManager;
            BuildingCost cost = GetBuildingCost(type);
            resourceManager.SpendResources(cost.Gold, cost.Wood, cost.Stone, cost.ManaStone);
            
            // Create building task
            BuildingTask task = new BuildingTask
            {
                Type = type,
                Position = position,
                IsUpgrade = false,
                TimeRequired = GetBuildingConstructionTime(type),
                TimeRemaining = GetBuildingConstructionTime(type)
            };
            
            buildingQueue.Enqueue(task);
        }

        float GetBuildingConstructionTime(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.GuildHall: return 0f;
                case BuildingType.TrainingGround: return 60f;
                case BuildingType.ResearchLab: return 120f;
                case BuildingType.Dormitory: return 90f;
                case BuildingType.Armory: return 150f;
                case BuildingType.MageTower: return 180f;
                case BuildingType.Temple: return 150f;
                case BuildingType.Shop: return 60f;
                case BuildingType.Warehouse: return 120f;
                case BuildingType.ScoutPost: return 45f;
                default: return 60f;
            }
        }

        public void PlaceBuilding(BuildingType type, Vector2Int position)
        {
            ConstructBuilding(type, position);
        }
        
        // Building 오버로드 추가
        public void PlaceBuilding(GuildMaster.Guild.Building building)
        {
            building.GridPosition = building.GridPosition;
            PlaceBuilding(building.transform.position, building);
        }
        
        void PlaceBuilding(Vector3 position, GuildMaster.Guild.Building guildBuilding)
        {
            guildBuilding.transform.position = position;
            guildBuilding.IsPlaced = true;
            currentGuild.Buildings.Add(ConvertToGuildManagerBuilding(guildBuilding));
        }
        
        private Building ConvertToGuildManagerBuilding(GuildMaster.Guild.Building guildBuilding)
        {
            return new Building(guildBuilding.Type, guildBuilding.GridPosition)
            {
                Level = guildBuilding.Level,
                IsConstructing = guildBuilding.IsConstructing
            };
        }

        void UpdateBuildingSynergies()
        {
            // Clear existing synergies
            foreach (var building in currentGuild.Buildings)
            {
                building.ActiveSynergies.Clear();
                building.EfficiencyMultiplier = 1f;
            }
            
            // Check each building for synergies
            foreach (var building in currentGuild.Buildings)
            {
                foreach (var synergy in availableSynergies)
                {
                    if (CheckSynergyConditions(building, synergy))
                    {
                        building.ActiveSynergies.Add(synergy);
                        building.EfficiencyMultiplier += synergy.EfficiencyBonus;
                    }
                }
            }
        }
        
        bool CheckSynergyConditions(Building building, BuildingSynergy synergy)
        {
            // Check if this building is part of the synergy
            if (!synergy.RequiredBuildings.Contains(building.Type))
                return false;
            
            // Get adjacent buildings
            var adjacentBuildings = GetAdjacentBuildings(building);
            
            // Check if all required buildings are adjacent
            foreach (var requiredType in synergy.RequiredBuildings)
            {
                if (requiredType != building.Type && !adjacentBuildings.Any(b => b.Type == requiredType))
                    return false;
            }
            
            return true;
        }
        
        List<Building> GetAdjacentBuildings(Building building)
        {
            List<Building> adjacent = new List<Building>();
            HashSet<Building> checkedBuildings = new HashSet<Building>();
            
            // Check all cells around the building
            for (int x = building.Position.x - 1; x <= building.Position.x + building.Size.x; x++)
            {
                for (int y = building.Position.y - 1; y <= building.Position.y + building.Size.y; y++)
                {
                    // Skip cells that are part of the current building
                    if (x >= building.Position.x && x < building.Position.x + building.Size.x &&
                        y >= building.Position.y && y < building.Position.y + building.Size.y)
                        continue;
                    
                    // Check if within grid bounds
                    if (x >= 0 && x < GUILD_GRID_SIZE && y >= 0 && y < GUILD_GRID_SIZE)
                    {
                        var adjacentBuilding = guildGrid[x, y];
                        if (adjacentBuilding != null && !checkedBuildings.Contains(adjacentBuilding))
                        {
                            adjacent.Add(adjacentBuilding);
                            checkedBuildings.Add(adjacentBuilding);
                        }
                    }
                }
            }
            
            return adjacent;
        }

        void ApplyBuildingEffects(Building building)
        {
            switch (building.Type)
            {
                case BuildingType.Dormitory:
                    currentGuild.MaxAdventurers += 9 * building.Level; // +9 per level
                    break;
                case BuildingType.GuildHall:
                    // Unlock new building types based on level
                    break;
            }
        }

        void CompleteBuildingTask(BuildingTask task)
        {
            if (task.IsUpgrade)
            {
                task.TargetBuilding.Level++;
                task.TargetBuilding.IsConstructing = false;
                ApplyBuildingEffects(task.TargetBuilding);
                OnBuildingUpgraded?.Invoke(task.TargetBuilding);
            }
            else
            {
                Building newBuilding = new Building(task.Type, task.Position);
                PlaceBuilding(task.Type, task.Position);
                OnBuildingConstructed?.Invoke(newBuilding);
            }
        }

        public void RecruitAdventurer(Unit adventurer)
        {
            if (currentGuild.Adventurers.Count >= currentGuild.MaxAdventurers)
            {
                Debug.LogWarning("Guild is full! Build more dormitories.");
                return;
            }
            
            currentGuild.Adventurers.Add(adventurer);
            OnAdventurerRecruited?.Invoke(adventurer);
        }

        public bool CanUpgradeBuilding(Building building)
        {
            if (building.IsConstructing) return false;
            
            ResourceManager resourceManager = GameManager.Instance.ResourceManager;
            if (resourceManager == null) return false;
            
            int upgradeCost = building.GetUpgradeCost();
            return resourceManager.CanAfford(upgradeCost, 0, 0, 0);
        }

        public void UpgradeBuilding(Building building)
        {
            if (!CanUpgradeBuilding(building)) return;
            
            ResourceManager resourceManager = GameManager.Instance.ResourceManager;
            int upgradeCost = building.GetUpgradeCost();
            resourceManager.SpendResources(upgradeCost, 0, 0, 0);
            
            BuildingTask task = new BuildingTask
            {
                Type = building.Type,
                Position = building.Position,
                IsUpgrade = true,
                TargetBuilding = building,
                TimeRequired = building.GetUpgradeTime(),
                TimeRemaining = building.GetUpgradeTime()
            };
            
            building.IsConstructing = true;
            buildingQueue.Enqueue(task);
        }

        public void AddReputation(int amount)
        {
            currentGuild.GuildReputation += amount;
            OnReputationChanged?.Invoke(currentGuild.GuildReputation);
            
            // Check for guild level up
            int requiredReputation = currentGuild.GuildLevel * 1000;
            if (currentGuild.GuildReputation >= requiredReputation)
            {
                currentGuild.GuildLevel++;
                OnGuildLevelUp?.Invoke(currentGuild.GuildLevel);
            }
        }

        // Getters
        public GuildData GetGuildData() => currentGuild;
        public List<Unit> GetAdventurers() => currentGuild.Adventurers;
        public List<Unit> GetAvailableAdventurers() => currentGuild.Adventurers.Where(a => a.IsAlive).ToList();
        public Building[,] GetGuildGrid() => guildGrid;
        public Queue<BuildingTask> GetBuildingQueue() => buildingQueue;
        public BuildingTask GetCurrentBuildingTask() => currentBuildingTask;
        public List<BuildingSynergy> GetAvailableSynergies() => availableSynergies;
        
        // New methods for enhanced building system
        public void OptimizeBuildingLayout()
        {
            // AI-assisted building placement optimization
            var buildingGroups = currentGuild.Buildings.GroupBy(b => b.Category).ToList();
            
            foreach (var group in buildingGroups)
            {
                // Try to group buildings of the same category together
                // This is a placeholder for more complex optimization logic
                Debug.Log($"Optimizing {group.Key} buildings: {group.Count()} buildings");
            }
            
            UpdateBuildingSynergies();
        }
        
        public float GetTotalBuildingEfficiency()
        {
            float totalEfficiency = 0f;
            int buildingCount = 0;
            
            foreach (var building in currentGuild.Buildings)
            {
                if (!building.IsConstructing)
                {
                    totalEfficiency += building.EfficiencyMultiplier;
                    buildingCount++;
                }
            }
            
            return buildingCount > 0 ? totalEfficiency / buildingCount : 1f;
        }
        
        public List<Building> GetBuildingsByType(BuildingType type)
        {
            return currentGuild.Buildings.Where(b => b.Type == type).ToList();
        }
        
        public List<Building> GetBuildingsByCategory(BuildingCategory category)
        {
            return currentGuild.Buildings.Where(b => b.Category == category).ToList();
        }
        
        public Dictionary<ResourceType, float> GetResourceProduction()
        {
            var production = new Dictionary<ResourceType, float>
            {
                { ResourceType.Gold, 0f },
                { ResourceType.Wood, 0f },
                { ResourceType.Stone, 0f },
                { ResourceType.ManaStone, 0f }
            };
            
            foreach (var building in currentGuild.Buildings)
            {
                if (building.IsConstructing) continue;
                
                switch (building.Type)
                {
                    case BuildingType.Shop:
                        production[ResourceType.Gold] += building.GetProductionAmount();
                        break;
                    // Add other resource production buildings here
                }
            }
            
            return production;
        }
        
        // Save/Load helper methods
        public void ClearAllBuildings()
        {
            currentGuild.Buildings.Clear();
            for (int x = 0; x < GUILD_GRID_SIZE; x++)
            {
                for (int y = 0; y < GUILD_GRID_SIZE; y++)
                {
                    guildGrid[x, y] = null;
                }
            }
            Debug.Log("Cleared all buildings");
        }
        
        public void ClearAllAdventurers()
        {
            currentGuild.Adventurers.Clear();
            Debug.Log("Cleared all adventurers");
        }
        
        public Building GetBuildingAt(Vector2Int position)
        {
            return currentGuild.Buildings.FirstOrDefault(b => b.Position == position);
        }
        
        public void SetGuildLevel(int level)
        {
            currentGuild.GuildLevel = level;
        }
        
        public void SetGuildReputation(int reputation)
        {
            currentGuild.GuildReputation = reputation;
        }
        
        public void AddAdventurer(GuildMaster.Battle.Unit unit)
        {
            if (currentGuild.Adventurers.Count < currentGuild.MaxAdventurers)
            {
                currentGuild.Adventurers.Add(unit);
                OnAdventurerRecruited?.Invoke(unit);
                Debug.Log($"Added adventurer: {unit.unitName}");
            }
        }

        public bool RemoveAdventurer(GuildMaster.Battle.Unit unit)
        {
            bool removed = currentGuild.Adventurers.Remove(unit);
            if (removed)
            {
                Debug.Log($"Removed adventurer: {unit.unitName}");
            }
            return removed;
        }
        
        public void IncreaseMaxAdventurers(int amount)
        {
            currentGuild.MaxAdventurers += amount;
        }
        
        // 길드 정보 접근 메서드들 추가
        public string GetGuildName()
        {
            return currentGuild.GuildName;
        }
        
        public int GetGuildLevel() => currentGuild.GuildLevel;
        
        public int GetBuildingLevel(BuildingType buildingType)
        {
            var building = GetBuildingsByType(buildingType).FirstOrDefault();
            return building?.Level ?? 0;
        }
        
        public string GetGuildId()
        {
            return $"guild_{currentGuild.GuildName.GetHashCode()}";
        }
        
        public void AddGuildExperience(int experience)
        {
            totalExperience += experience;
            
            int newLevel = CalculateGuildLevel(totalExperience);
            if (newLevel > currentGuild.GuildLevel)
            {
                currentGuild.GuildLevel = newLevel;
                ProcessGuildLevelUp();
                OnGuildLevelChanged?.Invoke(newLevel);
            }
        }

        public void UpdateGuildLevel()
        {
            // 총 경험치에 따른 레벨 계산
            int newLevel = CalculateGuildLevel(totalExperience);
            
            if (newLevel > currentGuild.GuildLevel)
            {
                int levelsGained = newLevel - currentGuild.GuildLevel;
                currentGuild.GuildLevel = newLevel;
                
                // 레벨업 보상
                for (int i = 0; i < levelsGained; i++)
                {
                    ProcessGuildLevelUp();
                }
                
                OnGuildLevelUp?.Invoke(currentGuild.GuildLevel);
            }
        }

        private int CalculateGuildLevel(int experience)
        {
            // 경험치에 따른 레벨 계산
            return Mathf.FloorToInt(experience / 1000f) + 1;
        }

        private void ProcessGuildLevelUp()
        {
            // 길드 레벨업 처리
            Debug.Log($"Guild leveled up to {currentGuild.GuildLevel}!");
        }

        public void ApplyBuildingStats(GuildMaster.Guild.BuildingStats stats)
        {
            // 건물 스탯을 길드에 적용
            Debug.Log($"Applying building stats: maxAdventurers={stats.maxAdventurers}, goldProduction={stats.goldProduction}");
        }

        // 길드 레벨 관련 메서드들
        private int totalExperience = 0;
        public event System.Action<int> OnGuildLevelChanged;
    }
}