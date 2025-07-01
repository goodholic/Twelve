using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuildMaster.Systems
{
    public enum TerritoryType
    {
        Village,        // 마을
        City,          // 도시
        Fortress,      // 요새
        Capital,       // 수도
        Strategic,     // 전략 거점
        Resource       // 자원 지역
    }
    
    public enum TerritoryBonus
    {
        GoldProduction,     // 골드 생산 증가
        ResourceProduction, // 자원 생산 증가
        RecruitmentBonus,   // 모험가 모집 보너스
        ExperienceBonus,    // 경험치 획득 보너스
        DefenseBonus,       // 방어 보너스
        ResearchSpeed       // 연구 속도 증가
    }
    
    [System.Serializable]
    public class Territory
    {
        public string TerritoryId { get; set; }
        public string Name { get; set; }
        public TerritoryType Type { get; set; }
        public Vector2Int Position { get; set; } // Map position
        public string ControllingGuildId { get; set; }
        public DateTime CaptureTime { get; set; }
        
        // Territory properties
        public int DefenseLevel { get; set; }
        public int DevelopmentLevel { get; set; }
        public Dictionary<TerritoryBonus, float> Bonuses { get; set; }
        
        // Connected territories
        public List<string> ConnectedTerritoryIds { get; set; }
        
        // Defense forces
        public List<Battle.Squad> DefenseSquads { get; set; }
        public float DefenseStrength { get; set; }
        
        // Resources
        public int GoldGeneration { get; set; }
        public int ResourceGeneration { get; set; }
        
        public Territory(string id, string name, TerritoryType type, Vector2Int position)
        {
            TerritoryId = id;
            Name = name;
            Type = type;
            Position = position;
            DefenseLevel = 1;
            DevelopmentLevel = 1;
            Bonuses = new Dictionary<TerritoryBonus, float>();
            ConnectedTerritoryIds = new List<string>();
            DefenseSquads = new List<Battle.Squad>();
            
            InitializeTerritoryBonuses();
        }
        
        void InitializeTerritoryBonuses()
        {
            switch (Type)
            {
                case TerritoryType.Village:
                    Bonuses[TerritoryBonus.GoldProduction] = 0.05f; // 5%
                    GoldGeneration = 50;
                    break;
                    
                case TerritoryType.City:
                    Bonuses[TerritoryBonus.GoldProduction] = 0.1f; // 10%
                    Bonuses[TerritoryBonus.RecruitmentBonus] = 0.1f;
                    GoldGeneration = 100;
                    break;
                    
                case TerritoryType.Fortress:
                    Bonuses[TerritoryBonus.DefenseBonus] = 0.2f; // 20%
                    Bonuses[TerritoryBonus.ExperienceBonus] = 0.1f;
                    GoldGeneration = 75;
                    break;
                    
                case TerritoryType.Capital:
                    Bonuses[TerritoryBonus.GoldProduction] = 0.15f;
                    Bonuses[TerritoryBonus.RecruitmentBonus] = 0.15f;
                    Bonuses[TerritoryBonus.ResearchSpeed] = 0.1f;
                    GoldGeneration = 200;
                    break;
                    
                case TerritoryType.Strategic:
                    Bonuses[TerritoryBonus.DefenseBonus] = 0.15f;
                    Bonuses[TerritoryBonus.ExperienceBonus] = 0.15f;
                    GoldGeneration = 80;
                    break;
                    
                case TerritoryType.Resource:
                    Bonuses[TerritoryBonus.ResourceProduction] = 0.25f; // 25%
                    ResourceGeneration = 50;
                    GoldGeneration = 25;
                    break;
            }
        }
        
        public void UpgradeDefense()
        {
            DefenseLevel++;
            DefenseStrength = CalculateDefenseStrength();
        }
        
        public void DevelopTerritory()
        {
            DevelopmentLevel++;
            
            // Increase generation rates
            GoldGeneration = (int)(GoldGeneration * 1.2f);
            ResourceGeneration = (int)(ResourceGeneration * 1.2f);
            
            // Increase bonuses
            foreach (var bonus in Bonuses.Keys.ToList())
            {
                Bonuses[bonus] *= 1.1f;
            }
        }
        
        public float CalculateDefenseStrength()
        {
            float baseStrength = DefenseLevel * 100;
            
            // Add squad strength
            foreach (var squad in DefenseSquads)
            {
                baseStrength += squad.GetAllUnits().Sum(u => u.Level * 10);
            }
            
            // Apply defense bonus
            if (Bonuses.ContainsKey(TerritoryBonus.DefenseBonus))
            {
                baseStrength *= (1f + Bonuses[TerritoryBonus.DefenseBonus]);
            }
            
            return baseStrength;
        }
        
        public bool IsConnectedTo(string territoryId)
        {
            return ConnectedTerritoryIds.Contains(territoryId);
        }
    }
    
    [System.Serializable]
    public class TerritoryBattle
    {
        public string BattleId { get; set; }
        public string AttackingGuildId { get; set; }
        public string DefendingGuildId { get; set; }
        public string TerritoryId { get; set; }
        public DateTime BattleTime { get; set; }
        public bool IsActive { get; set; }
        public string WinnerGuildId { get; set; }
        
        // Battle participants
        public List<string> AttackingAllies { get; set; }
        public List<string> DefendingAllies { get; set; }
        
        public TerritoryBattle(string territoryId, string attacker, string defender)
        {
            BattleId = Guid.NewGuid().ToString();
            TerritoryId = territoryId;
            AttackingGuildId = attacker;
            DefendingGuildId = defender;
            BattleTime = DateTime.Now.AddHours(24); // 24 hour preparation
            IsActive = true;
            AttackingAllies = new List<string>();
            DefendingAllies = new List<string>();
        }
    }
    
    public class TerritoryManager : MonoBehaviour
    {
        // Territory data
        private Dictionary<string, Territory> allTerritories;
        private Dictionary<string, List<string>> guildTerritories; // GuildId -> Territory IDs
        private List<TerritoryBattle> activeBattles;
        private List<TerritoryBattle> battleHistory;
        
        // Map configuration
        private const int MAP_WIDTH = 10;
        private const int MAP_HEIGHT = 10;
        private Territory[,] territoryMap;
        
        // Events
        public event Action<Territory, string> OnTerritoryCaputred;
        public event Action<Territory> OnTerritoryLost;
        public event Action<TerritoryBattle> OnTerritoryBattleStarted;
        public event Action<TerritoryBattle> OnTerritoryBattleEnded;
        public event Action<string, Dictionary<TerritoryBonus, float>> OnTerritoryBonusesChanged;
        
        // Update intervals
        private float territoryUpdateInterval = 300f; // 5 minutes
        private float lastTerritoryUpdate;
        
        void Awake()
        {
            allTerritories = new Dictionary<string, Territory>();
            guildTerritories = new Dictionary<string, List<string>>();
            activeBattles = new List<TerritoryBattle>();
            battleHistory = new List<TerritoryBattle>();
            territoryMap = new Territory[MAP_WIDTH, MAP_HEIGHT];
            
            GenerateTerritoryMap();
        }
        
        void GenerateTerritoryMap()
        {
            // Create territories
            CreateTerritory("territory_central_capital", "중앙 수도", TerritoryType.Capital, new Vector2Int(5, 5));
            
            // Create cities
            CreateTerritory("territory_north_city", "북부 도시", TerritoryType.City, new Vector2Int(5, 8));
            CreateTerritory("territory_south_city", "남부 도시", TerritoryType.City, new Vector2Int(5, 2));
            CreateTerritory("territory_east_city", "동부 도시", TerritoryType.City, new Vector2Int(8, 5));
            CreateTerritory("territory_west_city", "서부 도시", TerritoryType.City, new Vector2Int(2, 5));
            
            // Create fortresses
            CreateTerritory("territory_north_fortress", "북부 요새", TerritoryType.Fortress, new Vector2Int(5, 9));
            CreateTerritory("territory_south_fortress", "남부 요새", TerritoryType.Fortress, new Vector2Int(5, 1));
            CreateTerritory("territory_east_fortress", "동부 요새", TerritoryType.Fortress, new Vector2Int(9, 5));
            CreateTerritory("territory_west_fortress", "서부 요새", TerritoryType.Fortress, new Vector2Int(1, 5));
            
            // Create strategic points
            CreateTerritory("territory_crossroads", "십자로", TerritoryType.Strategic, new Vector2Int(4, 4));
            CreateTerritory("territory_bridge", "대교", TerritoryType.Strategic, new Vector2Int(6, 6));
            
            // Create resource areas
            CreateTerritory("territory_gold_mine", "금광", TerritoryType.Resource, new Vector2Int(7, 7));
            CreateTerritory("territory_forest", "대삼림", TerritoryType.Resource, new Vector2Int(3, 7));
            CreateTerritory("territory_quarry", "채석장", TerritoryType.Resource, new Vector2Int(7, 3));
            CreateTerritory("territory_mana_spring", "마나샘", TerritoryType.Resource, new Vector2Int(3, 3));
            
            // Create villages
            for (int x = 0; x < MAP_WIDTH; x++)
            {
                for (int y = 0; y < MAP_HEIGHT; y++)
                {
                    if (territoryMap[x, y] == null && UnityEngine.Random.value < 0.3f)
                    {
                        CreateTerritory($"territory_village_{x}_{y}", $"마을 {x}-{y}", 
                            TerritoryType.Village, new Vector2Int(x, y));
                    }
                }
            }
            
            // Connect adjacent territories
            ConnectAdjacentTerritories();
            
            // Assign initial territories to NPC guilds
            AssignInitialTerritories();
        }
        
        void CreateTerritory(string id, string name, TerritoryType type, Vector2Int position)
        {
            var territory = new Territory(id, name, type, position);
            allTerritories[id] = territory;
            
            if (position.x >= 0 && position.x < MAP_WIDTH && 
                position.y >= 0 && position.y < MAP_HEIGHT)
            {
                territoryMap[position.x, position.y] = territory;
            }
        }
        
        void ConnectAdjacentTerritories()
        {
            foreach (var territory in allTerritories.Values)
            {
                var pos = territory.Position;
                
                // Check 4 adjacent positions
                Vector2Int[] adjacentPositions = {
                    new Vector2Int(pos.x + 1, pos.y),
                    new Vector2Int(pos.x - 1, pos.y),
                    new Vector2Int(pos.x, pos.y + 1),
                    new Vector2Int(pos.x, pos.y - 1)
                };
                
                foreach (var adjPos in adjacentPositions)
                {
                    if (adjPos.x >= 0 && adjPos.x < MAP_WIDTH && 
                        adjPos.y >= 0 && adjPos.y < MAP_HEIGHT)
                    {
                        var adjTerritory = territoryMap[adjPos.x, adjPos.y];
                        if (adjTerritory != null)
                        {
                            territory.ConnectedTerritoryIds.Add(adjTerritory.TerritoryId);
                        }
                    }
                }
            }
        }
        
        void AssignInitialTerritories()
        {
            var npcGuildManager = Core.GameManager.Instance?.NPCGuildManager;
            if (npcGuildManager == null) return;
            
            var npcGuilds = npcGuildManager.GetAllNPCGuilds();
            var unassignedTerritories = allTerritories.Values.ToList();
            
            // Assign capital and major cities to strongest NPCs
            foreach (var guild in npcGuilds.OrderByDescending(g => g.Power))
            {
                var capitalOrCity = unassignedTerritories
                    .FirstOrDefault(t => t.Type == TerritoryType.Capital || t.Type == TerritoryType.City);
                    
                if (capitalOrCity != null)
                {
                    AssignTerritoryToGuild(capitalOrCity.TerritoryId, guild.GuildId);
                    unassignedTerritories.Remove(capitalOrCity);
                }
            }
            
            // Randomly assign remaining territories
            foreach (var territory in unassignedTerritories.Take(unassignedTerritories.Count / 2))
            {
                if (npcGuilds.Count > 0)
                {
                    var randomGuild = npcGuilds[UnityEngine.Random.Range(0, npcGuilds.Count)];
                    AssignTerritoryToGuild(territory.TerritoryId, randomGuild.GuildId);
                }
            }
        }
        
        public void AssignTerritoryToGuild(string territoryId, string guildId)
        {
            if (!allTerritories.ContainsKey(territoryId)) return;
            
            var territory = allTerritories[territoryId];
            
            // Remove from previous owner
            if (!string.IsNullOrEmpty(territory.ControllingGuildId))
            {
                if (guildTerritories.ContainsKey(territory.ControllingGuildId))
                {
                    guildTerritories[territory.ControllingGuildId].Remove(territoryId);
                }
            }
            
            // Assign to new owner
            territory.ControllingGuildId = guildId;
            territory.CaptureTime = DateTime.Now;
            
            if (!guildTerritories.ContainsKey(guildId))
            {
                guildTerritories[guildId] = new List<string>();
            }
            guildTerritories[guildId].Add(territoryId);
            
            // Update defense strength
            territory.DefenseStrength = territory.CalculateDefenseStrength();
            
            OnTerritoryCaputred?.Invoke(territory, guildId);
            UpdateGuildBonuses(guildId);
        }
        
        void Update()
        {
            // Update territory income
            if (Time.time - lastTerritoryUpdate >= territoryUpdateInterval)
            {
                UpdateTerritoryIncome();
                CheckActiveBattles();
                lastTerritoryUpdate = Time.time;
            }
        }
        
        void UpdateTerritoryIncome()
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return;
            
            string playerGuildId = "player_guild";
            
            if (guildTerritories.ContainsKey(playerGuildId))
            {
                int totalGold = 0;
                int totalWood = 0;
                int totalStone = 0;
                int totalManaStone = 0;
                
                foreach (var territoryId in guildTerritories[playerGuildId])
                {
                    var territory = allTerritories[territoryId];
                    totalGold += territory.GoldGeneration;
                    
                    if (territory.Type == TerritoryType.Resource)
                    {
                        totalWood += territory.ResourceGeneration / 2;
                        totalStone += territory.ResourceGeneration / 2;
                        totalManaStone += territory.ResourceGeneration / 4;
                    }
                }
                
                // Apply to resources
                gameManager.ResourceManager.AddGold(totalGold);
                gameManager.ResourceManager.AddWood(totalWood);
                gameManager.ResourceManager.AddStone(totalStone);
                gameManager.ResourceManager.AddManaStone(totalManaStone);
            }
        }
        
        public bool StartTerritoryBattle(string territoryId, string attackingGuildId)
        {
            if (!allTerritories.ContainsKey(territoryId)) return false;
            
            var territory = allTerritories[territoryId];
            
            // Check if territory is already in battle
            if (activeBattles.Any(b => b.TerritoryId == territoryId && b.IsActive))
                return false;
            
            // Check if attacker can reach territory
            if (!CanAttackTerritory(attackingGuildId, territoryId))
                return false;
            
            var battle = new TerritoryBattle(territoryId, attackingGuildId, territory.ControllingGuildId);
            activeBattles.Add(battle);
            
            OnTerritoryBattleStarted?.Invoke(battle);
            
            // AI guilds may join the battle
            CheckAIAlliances(battle);
            
            return true;
        }
        
        bool CanAttackTerritory(string attackingGuildId, string targetTerritoryId)
        {
            // Must control an adjacent territory
            if (!guildTerritories.ContainsKey(attackingGuildId))
                return false;
            
            var targetTerritory = allTerritories[targetTerritoryId];
            
            foreach (var controlledTerritoryId in guildTerritories[attackingGuildId])
            {
                if (targetTerritory.IsConnectedTo(controlledTerritoryId))
                    return true;
            }
            
            return false;
        }
        
        void CheckAIAlliances(TerritoryBattle battle)
        {
            var npcGuildManager = Core.GameManager.Instance?.NPCGuildManager;
            if (npcGuildManager == null) return;
            
            var npcGuilds = npcGuildManager.GetAllNPCGuilds();
            
            foreach (var guild in npcGuilds)
            {
                // Skip if already involved
                if (guild.GuildId == battle.AttackingGuildId || 
                    guild.GuildId == battle.DefendingGuildId)
                    continue;
                
                // Check relationships
                var attackerRelation = npcGuildManager.GetRelationship(battle.AttackingGuildId);
                var defenderRelation = npcGuildManager.GetRelationship(battle.DefendingGuildId);
                
                // Join defender if allied
                if (defenderRelation != null && defenderRelation.Level >= NPC.RelationshipLevel.Allied)
                {
                    battle.DefendingAllies.Add(guild.GuildId);
                }
                // Join attacker if allied
                else if (attackerRelation != null && attackerRelation.Level >= NPC.RelationshipLevel.Allied)
                {
                    battle.AttackingAllies.Add(guild.GuildId);
                }
            }
        }
        
        void CheckActiveBattles()
        {
            var battlesToResolve = activeBattles.Where(b => b.IsActive && DateTime.Now >= b.BattleTime).ToList();
            
            foreach (var battle in battlesToResolve)
            {
                ResolveTerritoryBattle(battle);
            }
        }
        
        void ResolveTerritoryBattle(TerritoryBattle battle)
        {
            var territory = allTerritories[battle.TerritoryId];
            
            // Calculate attacking strength
            float attackingStrength = CalculateGuildStrength(battle.AttackingGuildId);
            foreach (var allyId in battle.AttackingAllies)
            {
                attackingStrength += CalculateGuildStrength(allyId) * 0.5f; // Allies contribute 50%
            }
            
            // Calculate defending strength
            float defendingStrength = territory.DefenseStrength;
            defendingStrength += CalculateGuildStrength(battle.DefendingGuildId);
            foreach (var allyId in battle.DefendingAllies)
            {
                defendingStrength += CalculateGuildStrength(allyId) * 0.5f;
            }
            
            // Apply territory defense bonus
            defendingStrength *= 1.2f; // 20% defender advantage
            
            // Determine winner
            bool attackerWins = attackingStrength > defendingStrength;
            battle.WinnerGuildId = attackerWins ? battle.AttackingGuildId : battle.DefendingGuildId;
            battle.IsActive = false;
            
            if (attackerWins)
            {
                // Transfer territory
                AssignTerritoryToGuild(battle.TerritoryId, battle.AttackingGuildId);
            }
            
            battleHistory.Add(battle);
            activeBattles.Remove(battle);
            
            OnTerritoryBattleEnded?.Invoke(battle);
        }
        
        float CalculateGuildStrength(string guildId)
        {
            if (guildId == "player_guild")
            {
                var guildManager = Core.GameManager.Instance?.GuildManager;
                if (guildManager != null)
                {
                    return guildManager.GetGuildData().GuildLevel * 100;
                }
            }
            else
            {
                var npcGuildManager = Core.GameManager.Instance?.NPCGuildManager;
                var npcGuild = npcGuildManager?.GetNPCGuild(guildId);
                if (npcGuild != null)
                {
                    return npcGuild.Power;
                }
            }
            
            return 100f;
        }
        
        void UpdateGuildBonuses(string guildId)
        {
            if (!guildTerritories.ContainsKey(guildId)) return;
            
            var totalBonuses = new Dictionary<TerritoryBonus, float>();
            
            foreach (var territoryId in guildTerritories[guildId])
            {
                var territory = allTerritories[territoryId];
                
                foreach (var bonus in territory.Bonuses)
                {
                    if (!totalBonuses.ContainsKey(bonus.Key))
                        totalBonuses[bonus.Key] = 0f;
                    
                    totalBonuses[bonus.Key] += bonus.Value;
                }
            }
            
            OnTerritoryBonusesChanged?.Invoke(guildId, totalBonuses);
            
            // Apply bonuses if it's the player guild
            if (guildId == "player_guild")
            {
                ApplyPlayerBonuses(totalBonuses);
            }
        }
        
        void ApplyPlayerBonuses(Dictionary<TerritoryBonus, float> bonuses)
        {
            // TODO: Apply bonuses to various systems
            // This would integrate with resource production, recruitment, etc.
        }
        
        public void JoinTerritoryBattle(string battleId, string guildId, bool joinAttacker)
        {
            var battle = activeBattles.FirstOrDefault(b => b.BattleId == battleId);
            if (battle == null || !battle.IsActive) return;
            
            if (joinAttacker)
            {
                if (!battle.AttackingAllies.Contains(guildId))
                    battle.AttackingAllies.Add(guildId);
            }
            else
            {
                if (!battle.DefendingAllies.Contains(guildId))
                    battle.DefendingAllies.Add(guildId);
            }
        }
        
        public bool DevelopTerritory(string territoryId)
        {
            if (!allTerritories.ContainsKey(territoryId)) return false;
            
            var territory = allTerritories[territoryId];
            var gameManager = Core.GameManager.Instance;
            
            if (gameManager == null) return false;
            
            // Check costs
            int goldCost = territory.DevelopmentLevel * 500;
            int stoneCost = territory.DevelopmentLevel * 200;
            
            if (!gameManager.ResourceManager.CanAfford(goldCost, 0, stoneCost, 0))
                return false;
            
            // Pay costs
            gameManager.ResourceManager.SpendResources(goldCost, 0, stoneCost, 0);
            
            // Develop
            territory.DevelopTerritory();
            
            return true;
        }
        
        public bool UpgradeTerritoryDefense(string territoryId)
        {
            if (!allTerritories.ContainsKey(territoryId)) return false;
            
            var territory = allTerritories[territoryId];
            var gameManager = Core.GameManager.Instance;
            
            if (gameManager == null) return false;
            
            // Check costs
            int goldCost = territory.DefenseLevel * 300;
            int woodCost = territory.DefenseLevel * 150;
            
            if (!gameManager.ResourceManager.CanAfford(goldCost, woodCost, 0, 0))
                return false;
            
            // Pay costs
            gameManager.ResourceManager.SpendResources(goldCost, woodCost, 0, 0);
            
            // Upgrade
            territory.UpgradeDefense();
            
            return true;
        }
        
        // Getters
        public Dictionary<string, Territory> GetAllTerritories()
        {
            return new Dictionary<string, Territory>(allTerritories);
        }
        
        public List<Territory> GetPlayerTerritories()
        {
            string playerGuildId = "player_guild";
            
            if (guildTerritories.ContainsKey(playerGuildId))
            {
                return guildTerritories[playerGuildId]
                    .Select(id => allTerritories[id])
                    .ToList();
            }
            
            return new List<Territory>();
        }
        
        public List<Territory> GetGuildTerritories(string guildId)
        {
            if (guildTerritories.ContainsKey(guildId))
            {
                return guildTerritories[guildId]
                    .Select(id => allTerritories[id])
                    .ToList();
            }
            
            return new List<Territory>();
        }
        
        public List<TerritoryBattle> GetActiveBattles()
        {
            return new List<TerritoryBattle>(activeBattles.Where(b => b.IsActive));
        }
        
        public Territory[,] GetTerritoryMap()
        {
            return territoryMap;
        }
        
        public Territory GetTerritory(string territoryId)
        {
            return allTerritories.ContainsKey(territoryId) ? allTerritories[territoryId] : null;
        }
        
        public float GetTotalTerritoryBonus(string guildId, TerritoryBonus bonusType)
        {
            if (!guildTerritories.ContainsKey(guildId)) return 0f;
            
            float total = 0f;
            foreach (var territoryId in guildTerritories[guildId])
            {
                var territory = allTerritories[territoryId];
                if (territory.Bonuses.ContainsKey(bonusType))
                {
                    total += territory.Bonuses[bonusType];
                }
            }
            
            return total;
        }
    }
}