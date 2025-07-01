using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle; // JobClass를 위해 추가

namespace GuildMaster.NPC
{
    public enum RelationshipLevel
    {
        Hostile = -3,       // 적대적
        Unfriendly = -2,    // 비우호적
        Cold = -1,          // 냉담
        Neutral = 0,        // 중립
        Friendly = 1,       // 우호적
        Allied = 2,         // 동맹
        Brotherhood = 3     // 형제 길드
    }
    
    public enum DiplomaticAction
    {
        SendGift,           // 선물 보내기
        TechnologyExchange, // 기술 교류
        JointTraining,      // 합동 훈련
        ResourceTrade,      // 자원 거래
        MilitaryAlliance,   // 군사 동맹
        CulturalExchange,   // 문화 교류
        Sabotage,          // 방해 공작
        Raid               // 습격
    }
    
    [System.Serializable]
    public class NPCGuild
    {
        public string GuildId { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int Power { get; set; } // Military strength
        public int Wealth { get; set; } // Economic power
        public int Influence { get; set; } // Political influence
        
        // Specializations
        public JobClass[] PreferredClasses { get; set; }
        public string Specialization { get; set; } // "Military", "Trade", "Magic", "Balanced"
        
        // Territory
        public string ControlledRegion { get; set; }
        public List<string> ControlledTerritories { get; set; }
        
        // Personality traits
        public float Aggressiveness { get; set; } // 0-1
        public float Trustworthiness { get; set; } // 0-1
        public float Greed { get; set; } // 0-1
        
        public NPCGuild(string id, string name)
        {
            GuildId = id;
            Name = name;
            Level = 1;
            ControlledTerritories = new List<string>();
            
            // Random personality
            Aggressiveness = UnityEngine.Random.Range(0.2f, 0.8f);
            Trustworthiness = UnityEngine.Random.Range(0.3f, 0.9f);
            Greed = UnityEngine.Random.Range(0.2f, 0.7f);
        }
        
        public void UpdatePower()
        {
            // Calculate power based on level and territories
            Power = Level * 100 + ControlledTerritories.Count * 50;
            Wealth = Level * 200 + (int)(Greed * 500);
            Influence = Level * 50 + ControlledTerritories.Count * 30;
        }
    }
    
    [System.Serializable]
    public class GuildRelationship
    {
        public string TargetGuildId { get; set; }
        public int RelationshipPoints { get; set; } // -1000 to 1000
        public RelationshipLevel Level { get; set; }
        public DateTime LastInteraction { get; set; }
        public List<string> ActiveTreaties { get; set; }
        public Dictionary<string, DateTime> ActionHistory { get; set; }
        
        public GuildRelationship(string targetId)
        {
            TargetGuildId = targetId;
            RelationshipPoints = 0;
            Level = RelationshipLevel.Neutral;
            ActiveTreaties = new List<string>();
            ActionHistory = new Dictionary<string, DateTime>();
        }
        
        public void UpdateRelationshipLevel()
        {
            if (RelationshipPoints <= -750) Level = RelationshipLevel.Hostile;
            else if (RelationshipPoints <= -500) Level = RelationshipLevel.Unfriendly;
            else if (RelationshipPoints <= -250) Level = RelationshipLevel.Cold;
            else if (RelationshipPoints <= 250) Level = RelationshipLevel.Neutral;
            else if (RelationshipPoints <= 500) Level = RelationshipLevel.Friendly;
            else if (RelationshipPoints <= 750) Level = RelationshipLevel.Allied;
            else Level = RelationshipLevel.Brotherhood;
        }
        
        public void ModifyRelationship(int points, string action)
        {
            RelationshipPoints = Mathf.Clamp(RelationshipPoints + points, -1000, 1000);
            UpdateRelationshipLevel();
            LastInteraction = DateTime.Now;
            
            if (!ActionHistory.ContainsKey(action))
            {
                ActionHistory[action] = DateTime.Now;
            }
        }
    }
    
    public class NPCGuildManager : MonoBehaviour
    {
        // NPC Guilds
        private Dictionary<string, NPCGuild> npcGuilds;
        private Dictionary<string, GuildRelationship> relationships;
        
        // Configuration
        private const int MAX_RELATIONSHIPS = 5;
        private const float RELATIONSHIP_DECAY_RATE = 0.01f; // Points lost per day
        private const float INTERACTION_COOLDOWN = 3600f; // 1 hour
        
        // Events
        public event Action<NPCGuild, RelationshipLevel> OnRelationshipChanged;
        public event Action<NPCGuild, string> OnTreatyEstablished;
        public event Action<NPCGuild, string> OnTreatyBroken;
        public event Action<NPCGuild> OnWarDeclared;
        public event Action<NPCGuild, NPCGuild> OnAllianceFormed;
        
        void Awake()
        {
            npcGuilds = new Dictionary<string, NPCGuild>();
            relationships = new Dictionary<string, GuildRelationship>();
            
            InitializeNPCGuilds();
        }
        
        void InitializeNPCGuilds()
        {
            // Create diverse NPC guilds
            CreateNPCGuild("guild_iron_wolves", "철의 늑대단", "Military", 
                new[] { JobClass.Warrior, JobClass.Knight }, 0.7f, 0.6f, 0.4f);
                
            CreateNPCGuild("guild_silver_merchants", "은빛 상인 연합", "Trade", 
                new[] { JobClass.Assassin, JobClass.Sage }, 0.3f, 0.8f, 0.8f);
                
            CreateNPCGuild("guild_crystal_mages", "수정 마법사회", "Magic", 
                new[] { JobClass.Mage, JobClass.Sage }, 0.4f, 0.7f, 0.5f);
                
            CreateNPCGuild("guild_holy_order", "성스러운 기사단", "Balanced", 
                new[] { JobClass.Knight, JobClass.Priest }, 0.5f, 0.9f, 0.2f);
                
            CreateNPCGuild("guild_shadow_daggers", "그림자 단검단", "Military", 
                new[] { JobClass.Assassin, JobClass.Ranger }, 0.8f, 0.3f, 0.7f);
        }
        
        void CreateNPCGuild(string id, string name, string specialization, 
            JobClass[] preferredClasses, float aggression, float trust, float greed)
        {
            var guild = new NPCGuild(id, name)
            {
                Specialization = specialization,
                PreferredClasses = preferredClasses,
                Aggressiveness = aggression,
                Trustworthiness = trust,
                Greed = greed,
                Level = UnityEngine.Random.Range(1, 10)
            };
            
            guild.UpdatePower();
            npcGuilds[id] = guild;
        }
        
        void Update()
        {
            // Decay relationships over time
            foreach (var relationship in relationships.Values)
            {
                if (DateTime.Now.Subtract(relationship.LastInteraction).TotalDays > 7)
                {
                    relationship.ModifyRelationship(-1, "natural_decay");
                }
            }
        }
        
        public bool CanInteractWith(string guildId)
        {
            if (!relationships.ContainsKey(guildId)) return true;
            
            var relationship = relationships[guildId];
            return DateTime.Now.Subtract(relationship.LastInteraction).TotalSeconds > INTERACTION_COOLDOWN;
        }
        
        public bool ExecuteDiplomaticAction(string targetGuildId, DiplomaticAction action)
        {
            if (!npcGuilds.ContainsKey(targetGuildId)) return false;
            if (!CanInteractWith(targetGuildId)) return false;
            
            var targetGuild = npcGuilds[targetGuildId];
            var relationship = GetOrCreateRelationship(targetGuildId);
            
            bool success = false;
            int relationshipChange = 0;
            
            switch (action)
            {
                case DiplomaticAction.SendGift:
                    success = ExecuteSendGift(targetGuild, relationship);
                    relationshipChange = success ? 50 : 10;
                    break;
                    
                case DiplomaticAction.TechnologyExchange:
                    success = ExecuteTechnologyExchange(targetGuild, relationship);
                    relationshipChange = success ? 100 : -20;
                    break;
                    
                case DiplomaticAction.JointTraining:
                    success = ExecuteJointTraining(targetGuild, relationship);
                    relationshipChange = success ? 75 : 0;
                    break;
                    
                case DiplomaticAction.ResourceTrade:
                    success = ExecuteResourceTrade(targetGuild, relationship);
                    relationshipChange = success ? 30 : 0;
                    break;
                    
                case DiplomaticAction.MilitaryAlliance:
                    success = ExecuteMilitaryAlliance(targetGuild, relationship);
                    relationshipChange = success ? 200 : -50;
                    break;
                    
                case DiplomaticAction.CulturalExchange:
                    success = ExecuteCulturalExchange(targetGuild, relationship);
                    relationshipChange = success ? 60 : 0;
                    break;
                    
                case DiplomaticAction.Sabotage:
                    success = ExecuteSabotage(targetGuild, relationship);
                    relationshipChange = success ? -200 : -100;
                    break;
                    
                case DiplomaticAction.Raid:
                    success = ExecuteRaid(targetGuild, relationship);
                    relationshipChange = success ? -500 : -300;
                    break;
            }
            
            relationship.ModifyRelationship(relationshipChange, action.ToString());
            
            var oldLevel = relationship.Level;
            relationship.UpdateRelationshipLevel();
            
            if (oldLevel != relationship.Level)
            {
                OnRelationshipChanged?.Invoke(targetGuild, relationship.Level);
            }
            
            return success;
        }
        
        GuildRelationship GetOrCreateRelationship(string guildId)
        {
            if (!relationships.ContainsKey(guildId))
            {
                relationships[guildId] = new GuildRelationship(guildId);
            }
            return relationships[guildId];
        }
        
        bool ExecuteSendGift(NPCGuild targetGuild, GuildRelationship relationship)
        {
            var resourceManager = Core.GameManager.Instance?.ResourceManager;
            if (resourceManager == null) return false;
            
            int giftCost = 100 * targetGuild.Level;
            if (!resourceManager.CanAfford(giftCost, 0, 0, 0)) return false;
            
            resourceManager.SpendResources(giftCost, 0, 0, 0);
            
            // Success chance based on guild personality
            float successChance = 0.8f + (targetGuild.Trustworthiness * 0.2f) - (targetGuild.Greed * 0.1f);
            return UnityEngine.Random.value < successChance;
        }
        
        bool ExecuteTechnologyExchange(NPCGuild targetGuild, GuildRelationship relationship)
        {
            // Requires friendly relationship
            if (relationship.Level < RelationshipLevel.Friendly) return false;
            
            var guildManager = Core.GameManager.Instance?.GuildManager;
            if (guildManager == null) return false;
            
            // Check if we have research lab
            var buildings = guildManager.GetGuildData().Buildings;
            bool hasResearchLab = buildings.Any(b => b.Type == Core.GuildManager.BuildingType.ResearchLab && b.Level >= 2);
            
            if (!hasResearchLab) return false;
            
            // Success based on specialization match
            float successChance = 0.6f;
            if (targetGuild.Specialization == "Magic" || targetGuild.Specialization == "Balanced")
            {
                successChance += 0.3f;
            }
            
            if (UnityEngine.Random.value < successChance)
            {
                if (!relationship.ActiveTreaties.Contains("TechnologyExchange"))
                {
                    relationship.ActiveTreaties.Add("TechnologyExchange");
                    OnTreatyEstablished?.Invoke(targetGuild, "TechnologyExchange");
                }
                return true;
            }
            
            return false;
        }
        
        bool ExecuteJointTraining(NPCGuild targetGuild, GuildRelationship relationship)
        {
            // Requires neutral or better relationship
            if (relationship.Level < RelationshipLevel.Neutral) return false;
            
            var guildManager = Core.GameManager.Instance?.GuildManager;
            if (guildManager == null) return false;
            
            // Check if we have training ground
            var buildings = guildManager.GetGuildData().Buildings;
            bool hasTrainingGround = buildings.Any(b => b.Type == Core.GuildManager.BuildingType.TrainingGround);
            
            if (!hasTrainingGround) return false;
            
            // Military guilds are more likely to accept
            float successChance = 0.5f;
            if (targetGuild.Specialization == "Military")
            {
                successChance += 0.3f;
            }
            
            return UnityEngine.Random.value < successChance;
        }
        
        bool ExecuteResourceTrade(NPCGuild targetGuild, GuildRelationship relationship)
        {
            var resourceManager = Core.GameManager.Instance?.ResourceManager;
            if (resourceManager == null) return false;
            
            // Trade guilds always accept resource trades
            if (targetGuild.Specialization == "Trade")
            {
                // Execute a simple trade
                if (resourceManager.TradeResources(100, 0, 0, 0, 0, 50, 50, 0))
                {
                    return true;
                }
            }
            
            // Other guilds have requirements
            float successChance = 0.4f + (targetGuild.Greed * 0.3f);
            return UnityEngine.Random.value < successChance;
        }
        
        bool ExecuteMilitaryAlliance(NPCGuild targetGuild, GuildRelationship relationship)
        {
            // Requires allied relationship
            if (relationship.Level < RelationshipLevel.Allied) return false;
            
            // Check power balance
            var guildManager = Core.GameManager.Instance?.GuildManager;
            if (guildManager == null) return false;
            
            int playerPower = guildManager.GetGuildData().GuildLevel * 100;
            
            // Target guild must see benefit in alliance
            if (playerPower < targetGuild.Power * 0.5f) return false;
            
            float successChance = 0.6f + (targetGuild.Trustworthiness * 0.3f) - (targetGuild.Aggressiveness * 0.2f);
            
            if (UnityEngine.Random.value < successChance)
            {
                if (!relationship.ActiveTreaties.Contains("MilitaryAlliance"))
                {
                    relationship.ActiveTreaties.Add("MilitaryAlliance");
                    OnAllianceFormed?.Invoke(targetGuild, npcGuilds.Values.First());
                }
                return true;
            }
            
            return false;
        }
        
        bool ExecuteCulturalExchange(NPCGuild targetGuild, GuildRelationship relationship)
        {
            // Most guilds are open to cultural exchange
            float successChance = 0.7f + (targetGuild.Trustworthiness * 0.2f);
            
            if (UnityEngine.Random.value < successChance)
            {
                // Small reputation boost
                var guildManager = Core.GameManager.Instance?.GuildManager;
                guildManager?.AddReputation(10);
                return true;
            }
            
            return false;
        }
        
        bool ExecuteSabotage(NPCGuild targetGuild, GuildRelationship relationship)
        {
            // Sabotage severely damages relationship
            float successChance = 0.3f;
            
            // Rogue-based guilds are harder to sabotage
            if (targetGuild.PreferredClasses.Contains(JobClass.Assassin))
            {
                successChance -= 0.2f;
            }
            
            if (UnityEngine.Random.value < successChance)
            {
                // Reduce target guild power temporarily
                targetGuild.Power = (int)(targetGuild.Power * 0.8f);
                return true;
            }
            
            return false;
        }
        
        bool ExecuteRaid(NPCGuild targetGuild, GuildRelationship relationship)
        {
            // Raiding means war
            OnWarDeclared?.Invoke(targetGuild);
            
            var battleManager = Core.GameManager.Instance?.BattleManager;
            var guildManager = Core.GameManager.Instance?.GuildManager;
            
            if (battleManager == null || guildManager == null) return false;
            
            // Start a battle against the target guild
            var playerUnits = guildManager.GetAvailableAdventurers();
            var difficulty = GetDifficultyFromGuildLevel(targetGuild.Level);
            var enemySquads = Battle.AIGuildGenerator.GenerateAIGuild(difficulty, targetGuild.Level);
            
            var enemyUnits = new List<Unit>();
            foreach (var squad in enemySquads)
            {
                enemyUnits.AddRange(squad.GetAllUnits());
            }
            
            Core.GameManager.Instance.CurrentState = Core.GameManager.GameState.Battle;
            battleManager.StartBattle(playerUnits, enemyUnits);
            
            return true;
        }
        
        Battle.AIGuildGenerator.Difficulty GetDifficultyFromGuildLevel(int level)
        {
            if (level < 5) return Battle.AIGuildGenerator.Difficulty.Novice;
            if (level < 10) return Battle.AIGuildGenerator.Difficulty.Bronze;
            if (level < 15) return Battle.AIGuildGenerator.Difficulty.Silver;
            if (level < 20) return Battle.AIGuildGenerator.Difficulty.Gold;
            if (level < 25) return Battle.AIGuildGenerator.Difficulty.Platinum;
            if (level < 30) return Battle.AIGuildGenerator.Difficulty.Diamond;
            return Battle.AIGuildGenerator.Difficulty.Legendary;
        }
        
        public List<NPCGuild> GetAllNPCGuilds()
        {
            return npcGuilds.Values.ToList();
        }
        
        public NPCGuild GetNPCGuild(string guildId)
        {
            return npcGuilds.ContainsKey(guildId) ? npcGuilds[guildId] : null;
        }
        
        public GuildRelationship GetRelationship(string guildId)
        {
            return relationships.ContainsKey(guildId) ? relationships[guildId] : null;
        }
        
        public List<NPCGuild> GetAlliedGuilds()
        {
            return npcGuilds.Values.Where(g => 
            {
                var rel = GetRelationship(g.GuildId);
                return rel != null && rel.Level >= RelationshipLevel.Allied;
            }).ToList();
        }
        
        public List<NPCGuild> GetHostileGuilds()
        {
            return npcGuilds.Values.Where(g => 
            {
                var rel = GetRelationship(g.GuildId);
                return rel != null && rel.Level <= RelationshipLevel.Hostile;
            }).ToList();
        }
    }
}