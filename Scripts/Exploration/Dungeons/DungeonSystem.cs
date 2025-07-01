using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle;
using GuildMaster.Core;

namespace GuildMaster.Exploration
{
    public enum DungeonType
    {
        Normal,         // 일반 던전
        Elite,          // 엘리트 던전
        Ancient,        // 고대 유적
        Infinite,       // 무한 심연
        Event,          // 이벤트 던전
        Daily,          // 일일 던전
        Cursed          // 저주받은 던전
    }
    
    public enum DungeonTheme
    {
        Cave,           // 동굴
        Ruins,          // 폐허
        Forest,         // 숲
        Desert,         // 사막
        Ice,            // 얼음
        Volcano,        // 화산
        Underwater,     // 수중
        Sky,            // 하늘
        Void            // 공허
    }
    
    [System.Serializable]
    public class Dungeon
    {
        public string DungeonId { get; set; }
        public string Name { get; set; }
        public DungeonType Type { get; set; }
        public DungeonTheme Theme { get; set; }
        public int Level { get; set; }
        public int Floors { get; set; }
        public bool IsDiscovered { get; set; }
        public bool IsCleared { get; set; }
        public DateTime LastClearTime { get; set; }
        public int ClearCount { get; set; }
        
        // Requirements
        public int RequiredGuildLevel { get; set; }
        public int RequiredReputation { get; set; }
        public string RequiredItemId { get; set; }
        
        // Rewards
        public DungeonRewards BaseRewards { get; set; }
        public List<string> PossibleDrops { get; set; }
        public float RareDropChance { get; set; }
        
        // Difficulty
        public float DifficultyMultiplier { get; set; }
        public int RecommendedPower { get; set; }
        
        // Special properties
        public Dictionary<string, float> SpecialModifiers { get; set; }
        
        public Dungeon(string id, string name, DungeonType type, DungeonTheme theme, int level)
        {
            DungeonId = id;
            Name = name;
            Type = type;
            Theme = theme;
            Level = level;
            PossibleDrops = new List<string>();
            SpecialModifiers = new Dictionary<string, float>();
            
            InitializeDungeonProperties();
        }
        
        void InitializeDungeonProperties()
        {
            // Set floors based on type
            switch (Type)
            {
                case DungeonType.Normal:
                    Floors = UnityEngine.Random.Range(3, 6);
                    DifficultyMultiplier = 1f;
                    RareDropChance = 0.1f;
                    break;
                    
                case DungeonType.Elite:
                    Floors = UnityEngine.Random.Range(5, 8);
                    DifficultyMultiplier = 1.5f;
                    RareDropChance = 0.25f;
                    break;
                    
                case DungeonType.Ancient:
                    Floors = UnityEngine.Random.Range(7, 10);
                    DifficultyMultiplier = 2f;
                    RareDropChance = 0.4f;
                    break;
                    
                case DungeonType.Infinite:
                    Floors = 999; // Infinite floors
                    DifficultyMultiplier = 1f; // Increases per floor
                    RareDropChance = 0.2f;
                    break;
                    
                case DungeonType.Event:
                    Floors = 5;
                    DifficultyMultiplier = 1.2f;
                    RareDropChance = 0.5f;
                    break;
                    
                case DungeonType.Daily:
                    Floors = 1;
                    DifficultyMultiplier = 1f;
                    RareDropChance = 0.3f;
                    break;
            }
            
            // Set theme modifiers
            switch (Theme)
            {
                case DungeonTheme.Cave:
                    SpecialModifiers["physical_defense"] = 1.2f;
                    break;
                case DungeonTheme.Forest:
                    SpecialModifiers["evasion"] = 1.3f;
                    break;
                case DungeonTheme.Ice:
                    SpecialModifiers["magic_damage"] = 1.25f;
                    break;
                case DungeonTheme.Volcano:
                    SpecialModifiers["fire_damage"] = 1.5f;
                    break;
                case DungeonTheme.Void:
                    SpecialModifiers["all_stats"] = 0.8f; // Debuff
                    break;
            }
            
            // Set requirements
            RequiredGuildLevel = Mathf.Max(1, Level - 2);
            RequiredReputation = Level * 50;
            RecommendedPower = Level * 100;
            
            // Initialize rewards
            BaseRewards = new DungeonRewards
            {
                Gold = Level * 100 * (int)DifficultyMultiplier,
                Experience = Level * 200 * (int)DifficultyMultiplier,
                Wood = Level * 20,
                Stone = Level * 15,
                ManaStone = Level * 5
            };
        }
        
        public bool CanEnter(Core.GuildManager guildManager)
        {
            if (guildManager == null) return false;
            
            var guildData = guildManager.GetGuildData();
            
            if (guildData.GuildLevel < RequiredGuildLevel) return false;
            if (guildData.GuildReputation < RequiredReputation) return false;
            
            // Check daily dungeon reset
            if (Type == DungeonType.Daily && IsCleared)
            {
                if (DateTime.Now.Date == LastClearTime.Date)
                    return false; // Already cleared today
            }
            
            return true;
        }
        
        public DungeonRewards CalculateFinalRewards(int floorsCleared, bool perfectClear)
        {
            var rewards = new DungeonRewards
            {
                Gold = BaseRewards.Gold,
                Experience = BaseRewards.Experience,
                Wood = BaseRewards.Wood,
                Stone = BaseRewards.Stone,
                ManaStone = BaseRewards.ManaStone
            };
            
            // Floor completion bonus
            float floorBonus = (float)floorsCleared / Floors;
            rewards.MultiplyAll(floorBonus);
            
            // Perfect clear bonus
            if (perfectClear)
            {
                rewards.MultiplyAll(1.5f);
            }
            
            // Type-specific bonuses
            switch (Type)
            {
                case DungeonType.Elite:
                    rewards.Items.Add(GenerateEliteItem());
                    break;
                case DungeonType.Ancient:
                    rewards.ManaStone *= 3;
                    break;
                case DungeonType.Infinite:
                    rewards.MultiplyAll(1f + (floorsCleared * 0.1f));
                    break;
                case DungeonType.Event:
                    rewards.EventCurrency = floorsCleared * 100;
                    break;
            }
            
            return rewards;
        }
        
        string GenerateEliteItem()
        {
            // TODO: Implement item generation
            return "elite_equipment_" + UnityEngine.Random.Range(1, 100);
        }
    }
    
    [System.Serializable]
    public class DungeonRewards
    {
        public int Gold { get; set; }
        public int Wood { get; set; }
        public int Stone { get; set; }
        public int ManaStone { get; set; }
        public int Experience { get; set; }
        public int EventCurrency { get; set; }
        public List<string> Items { get; set; } = new List<string>();
        
        public void MultiplyAll(float multiplier)
        {
            Gold = (int)(Gold * multiplier);
            Wood = (int)(Wood * multiplier);
            Stone = (int)(Stone * multiplier);
            ManaStone = (int)(ManaStone * multiplier);
            Experience = (int)(Experience * multiplier);
            EventCurrency = (int)(EventCurrency * multiplier);
        }
    }
    
    [System.Serializable]
    public class DungeonFloor
    {
        public int FloorNumber { get; set; }
        public List<Battle.Squad> EnemySquads { get; set; }
        public bool HasBoss { get; set; }
        public bool IsCleared { get; set; }
        public DungeonRewards FloorRewards { get; set; }
        
        public DungeonFloor(int number, int dungeonLevel, float difficultyMultiplier)
        {
            FloorNumber = number;
            EnemySquads = new List<Battle.Squad>();
            
            // Boss floors
            HasBoss = (number % 5 == 0) || (number == dungeonLevel);
            
            GenerateEnemies(dungeonLevel, difficultyMultiplier);
            GenerateFloorRewards(dungeonLevel, number);
        }
        
        void GenerateEnemies(int dungeonLevel, float difficultyMultiplier)
        {
            int enemyLevel = dungeonLevel + (FloorNumber / 5);
            var difficulty = Battle.AIGuildGenerator.GetRecommendedDifficulty(enemyLevel);
            
            // Generate enemy squads
            var enemySquads = Battle.AIGuildGenerator.GenerateAIGuild(difficulty, enemyLevel);
            
            // Apply difficulty multiplier
            foreach (var squad in enemySquads)
            {
                foreach (var unit in squad.GetAllUnits())
                {
                    unit.maxHP *= difficultyMultiplier;
                    unit.currentHP = unit.maxHP;
                    unit.attackPower *= difficultyMultiplier;
                    unit.defense *= difficultyMultiplier;
                    unit.magicPower *= difficultyMultiplier;
                    
                    if (HasBoss && squad.Index == 0) // First squad has boss
                    {
                        unit.maxHP *= 2f;
                        unit.currentHP = unit.maxHP;
                        // Note: Unit class doesn't have writable name field
                        // Boss status is tracked separately
                    }
                }
            }
            
            EnemySquads = enemySquads;
        }
        
        void GenerateFloorRewards(int dungeonLevel, int floorNumber)
        {
            FloorRewards = new DungeonRewards
            {
                Gold = dungeonLevel * 20 * floorNumber,
                Experience = dungeonLevel * 50 * floorNumber
            };
            
            if (HasBoss)
            {
                FloorRewards.Gold *= 3;
                FloorRewards.Experience *= 3;
                FloorRewards.ManaStone = dungeonLevel * 5;
            }
        }
    }
    
    public class DungeonManager : MonoBehaviour
    {
        // Dungeon data
        private Dictionary<string, Dungeon> allDungeons;
        private List<Dungeon> discoveredDungeons;
        private Dungeon currentDungeon;
        private DungeonFloor currentFloor;
        private int currentFloorNumber;
        
        // Exploration state
        private List<Unit> explorationParty;
        private bool isExploring;
        private float explorationProgress;
        
        // Events
        public event Action<Dungeon> OnDungeonDiscovered;
        public event Action<Dungeon> OnDungeonEntered;
        public event Action<DungeonFloor> OnFloorStarted;
        public event Action<DungeonFloor, bool> OnFloorCompleted;
        public event Action<Dungeon, DungeonRewards> OnDungeonCompleted;
        public event Action<string> OnTreasureFound;
        
        void Awake()
        {
            allDungeons = new Dictionary<string, Dungeon>();
            discoveredDungeons = new List<Dungeon>();
            
            GenerateDungeons();
        }
        
        void GenerateDungeons()
        {
            // Generate normal dungeons for each region
            for (int level = 1; level <= 30; level += 3)
            {
                var theme = (DungeonTheme)UnityEngine.Random.Range(0, 5);
                CreateDungeon($"dungeon_normal_{level}", $"{GetThemeName(theme)} 던전 Lv.{level}", 
                    DungeonType.Normal, theme, level);
            }
            
            // Generate elite dungeons
            for (int level = 5; level <= 25; level += 5)
            {
                CreateDungeon($"dungeon_elite_{level}", $"엘리트 던전 Lv.{level}", 
                    DungeonType.Elite, DungeonTheme.Ruins, level);
            }
            
            // Generate ancient ruins
            CreateDungeon("dungeon_ancient_forest", "고대 숲의 유적", 
                DungeonType.Ancient, DungeonTheme.Forest, 10);
            CreateDungeon("dungeon_ancient_desert", "사막의 고대 신전", 
                DungeonType.Ancient, DungeonTheme.Desert, 15);
            CreateDungeon("dungeon_ancient_ice", "얼어붙은 고대 성채", 
                DungeonType.Ancient, DungeonTheme.Ice, 20);
            
            // Generate infinite dungeon
            CreateDungeon("dungeon_infinite_abyss", "무한 심연", 
                DungeonType.Infinite, DungeonTheme.Void, 1);
            
            // Generate daily dungeons
            CreateDungeon("dungeon_daily_gold", "황금 던전", 
                DungeonType.Daily, DungeonTheme.Cave, 5);
            CreateDungeon("dungeon_daily_exp", "경험의 던전", 
                DungeonType.Daily, DungeonTheme.Sky, 5);
            CreateDungeon("dungeon_daily_material", "재료 던전", 
                DungeonType.Daily, DungeonTheme.Cave, 5);
        }
        
        void CreateDungeon(string id, string name, DungeonType type, DungeonTheme theme, int level)
        {
            var dungeon = new Dungeon(id, name, type, theme, level);
            
            // Add special drops
            switch (type)
            {
                case DungeonType.Elite:
                    dungeon.PossibleDrops.Add("elite_equipment");
                    dungeon.PossibleDrops.Add("skill_book");
                    break;
                case DungeonType.Ancient:
                    dungeon.PossibleDrops.Add("ancient_artifact");
                    dungeon.PossibleDrops.Add("legendary_material");
                    break;
                case DungeonType.Daily:
                    if (name.Contains("황금"))
                        dungeon.BaseRewards.Gold *= 5;
                    else if (name.Contains("경험"))
                        dungeon.BaseRewards.Experience *= 5;
                    else if (name.Contains("재료"))
                    {
                        dungeon.BaseRewards.Wood *= 5;
                        dungeon.BaseRewards.Stone *= 5;
                    }
                    break;
            }
            
            allDungeons[id] = dungeon;
        }
        
        string GetThemeName(DungeonTheme theme)
        {
            switch (theme)
            {
                case DungeonTheme.Cave: return "동굴";
                case DungeonTheme.Ruins: return "폐허";
                case DungeonTheme.Forest: return "숲";
                case DungeonTheme.Desert: return "사막";
                case DungeonTheme.Ice: return "얼음";
                case DungeonTheme.Volcano: return "화산";
                case DungeonTheme.Underwater: return "수중";
                case DungeonTheme.Sky: return "하늘";
                case DungeonTheme.Void: return "공허";
                default: return "미지의";
            }
        }
        
        void Update()
        {
            if (isExploring && currentDungeon != null)
            {
                UpdateExploration();
            }
            
            // Check for new dungeon discoveries
            CheckDungeonDiscoveries();
        }
        
        void UpdateExploration()
        {
            explorationProgress += Time.deltaTime * GetExplorationSpeed();
            
            if (explorationProgress >= 1f)
            {
                CompleteCurrentFloor();
                explorationProgress = 0f;
            }
        }
        
        float GetExplorationSpeed()
        {
            float baseSpeed = 0.1f; // 10 seconds per floor
            
            // Scout post bonus
            var guildManager = Core.GameManager.Instance?.GuildManager;
            if (guildManager != null)
            {
                var buildings = guildManager.GetGuildData().Buildings;
                var scoutPost = buildings.FirstOrDefault(b => b.Type == Core.GuildManager.BuildingType.ScoutPost);
                if (scoutPost != null)
                {
                    baseSpeed *= 1f + (scoutPost.GetCurrentEffect() / 100f);
                }
            }
            
            return baseSpeed;
        }
        
        void CheckDungeonDiscoveries()
        {
            var guildManager = Core.GameManager.Instance?.GuildManager;
            if (guildManager == null) return;
            
            int guildLevel = guildManager.GetGuildData().GuildLevel;
            
            foreach (var dungeon in allDungeons.Values)
            {
                if (!dungeon.IsDiscovered && guildLevel >= dungeon.RequiredGuildLevel)
                {
                    // Check discovery chance
                    float discoveryChance = 0.01f; // 1% per update
                    
                    // Scout post increases discovery chance
                    var buildings = guildManager.GetGuildData().Buildings;
                    var scoutPost = buildings.FirstOrDefault(b => b.Type == Core.GuildManager.BuildingType.ScoutPost);
                    if (scoutPost != null)
                    {
                        discoveryChance *= 1f + (scoutPost.GetCurrentEffect() / 100f);
                    }
                    
                    if (UnityEngine.Random.value < discoveryChance * Time.deltaTime)
                    {
                        DiscoverDungeon(dungeon);
                    }
                }
            }
        }
        
        void DiscoverDungeon(Dungeon dungeon)
        {
            dungeon.IsDiscovered = true;
            discoveredDungeons.Add(dungeon);
            OnDungeonDiscovered?.Invoke(dungeon);
        }
        
        public bool EnterDungeon(string dungeonId, List<Unit> party)
        {
            if (!allDungeons.ContainsKey(dungeonId)) return false;
            
            var dungeon = allDungeons[dungeonId];
            
            if (!dungeon.IsDiscovered) return false;
            if (!dungeon.CanEnter(Core.GameManager.Instance?.GuildManager)) return false;
            if (party == null || party.Count == 0) return false;
            
            currentDungeon = dungeon;
            explorationParty = party;
            currentFloorNumber = 1;
            isExploring = true;
            explorationProgress = 0f;
            
            StartFloor();
            OnDungeonEntered?.Invoke(dungeon);
            
            return true;
        }
        
        void StartFloor()
        {
            if (currentDungeon == null) return;
            
            currentFloor = new DungeonFloor(currentFloorNumber, currentDungeon.Level, currentDungeon.DifficultyMultiplier);
            
            // Apply theme modifiers to enemies
            foreach (var modifier in currentDungeon.SpecialModifiers)
            {
                ApplyThemeModifier(currentFloor.EnemySquads, modifier.Key, modifier.Value);
            }
            
            OnFloorStarted?.Invoke(currentFloor);
            
            // Start battle
            StartFloorBattle();
        }
        
        void ApplyThemeModifier(List<Battle.Squad> squads, string modifier, float value)
        {
            foreach (var squad in squads)
            {
                foreach (var unit in squad.GetAllUnits())
                {
                    switch (modifier)
                    {
                        case "physical_defense":
                            unit.defense *= value;
                            break;
                        case "magic_damage":
                            unit.magicPower *= value;
                            break;
                        case "evasion":
                            unit.speed *= value;
                            break;
                        case "all_stats":
                            unit.attackPower *= value;
                            unit.defense *= value;
                            unit.magicPower *= value;
                            unit.speed *= value;
                            break;
                    }
                }
            }
        }
        
        void StartFloorBattle()
        {
            var battleManager = Core.GameManager.Instance?.BattleManager;
            if (battleManager == null) return;
            
            var enemyUnits = new List<Unit>();
            foreach (var squad in currentFloor.EnemySquads)
            {
                enemyUnits.AddRange(squad.GetAllUnits());
            }
            
            Core.GameManager.Instance.CurrentState = Core.GameManager.GameState.Battle;
            battleManager.StartBattle(explorationParty, enemyUnits);
            
            // Subscribe to battle end
            battleManager.OnBattleEnd += OnFloorBattleEnd;
        }
        
        void OnFloorBattleEnd(bool victory)
        {
            var battleManager = Core.GameManager.Instance?.BattleManager;
            if (battleManager != null)
            {
                battleManager.OnBattleEnd -= OnFloorBattleEnd;
            }
            
            if (victory)
            {
                CompleteCurrentFloor();
            }
            else
            {
                FailDungeon();
            }
        }
        
        void CompleteCurrentFloor()
        {
            if (currentFloor == null) return;
            
            currentFloor.IsCleared = true;
            
            // Apply floor rewards
            ApplyRewards(currentFloor.FloorRewards);
            
            // Check for treasure
            if (UnityEngine.Random.value < currentDungeon.RareDropChance)
            {
                string treasure = currentDungeon.PossibleDrops[UnityEngine.Random.Range(0, currentDungeon.PossibleDrops.Count)];
                OnTreasureFound?.Invoke(treasure);
            }
            
            OnFloorCompleted?.Invoke(currentFloor, true);
            
            // Check if dungeon is complete
            if (currentFloorNumber >= currentDungeon.Floors)
            {
                CompleteDungeon();
            }
            else
            {
                // Move to next floor
                currentFloorNumber++;
                StartFloor();
            }
        }
        
        void CompleteDungeon()
        {
            if (currentDungeon == null) return;
            
            currentDungeon.IsCleared = true;
            currentDungeon.LastClearTime = DateTime.Now;
            currentDungeon.ClearCount++;
            
            // Calculate final rewards
            bool perfectClear = explorationParty.All(u => u.IsAlive);
            var finalRewards = currentDungeon.CalculateFinalRewards(currentFloorNumber, perfectClear);
            
            ApplyRewards(finalRewards);
            
            OnDungeonCompleted?.Invoke(currentDungeon, finalRewards);
            
            // Reset exploration state
            currentDungeon = null;
            currentFloor = null;
            isExploring = false;
            explorationParty = null;
        }
        
        void FailDungeon()
        {
            // Give partial rewards based on floors cleared
            if (currentDungeon != null && currentFloorNumber > 1)
            {
                var partialRewards = currentDungeon.CalculateFinalRewards(currentFloorNumber - 1, false);
                partialRewards.MultiplyAll(0.5f); // 50% penalty for failure
                ApplyRewards(partialRewards);
            }
            
            // Reset exploration state
            currentDungeon = null;
            currentFloor = null;
            isExploring = false;
            explorationParty = null;
        }
        
        void ApplyRewards(DungeonRewards rewards)
        {
            var resourceManager = Core.GameManager.Instance?.ResourceManager;
            if (resourceManager != null)
            {
                resourceManager.AddGold(rewards.Gold);
                resourceManager.AddWood(rewards.Wood);
                resourceManager.AddStone(rewards.Stone);
                resourceManager.AddManaStone(rewards.ManaStone);
            }
            
            // TODO: Apply experience to party members
            // TODO: Add items to inventory
        }
        
        public List<Dungeon> GetDiscoveredDungeons()
        {
            return discoveredDungeons.Where(d => d.IsDiscovered).ToList();
        }
        
        public List<Dungeon> GetAvailableDungeons()
        {
            var guildManager = Core.GameManager.Instance?.GuildManager;
            if (guildManager == null) return new List<Dungeon>();
            
            return discoveredDungeons.Where(d => d.CanEnter(guildManager)).ToList();
        }
        
        public Dungeon GetCurrentDungeon()
        {
            return currentDungeon;
        }
        
        public float GetExplorationProgress()
        {
            return explorationProgress;
        }
        
        public bool IsExploring()
        {
            return isExploring;
        }
    }
}