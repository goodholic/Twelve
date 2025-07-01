using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Core;  // ResourceType, Building, BattleManager 등을 위해

namespace GuildMaster.Battle
{
    public class AIGuildGenerator : MonoBehaviour
    {
        // AI Guild Difficulty Levels
        public enum Difficulty
        {
            Novice,     // Level 1-5
            Bronze,     // Level 5-10
            Silver,     // Level 10-15
            Gold,       // Level 15-20
            Platinum,   // Level 20-25
            Diamond,    // Level 25-30
            Legendary   // Level 30+
        }
        
        // AI Guild Templates
        [System.Serializable]
        public class AIGuildTemplate
        {
            public string Name;
            public Difficulty Difficulty;
            public int MinLevel;
            public int MaxLevel;
            public SquadTemplate[] SquadTemplates;
        }
        
        [System.Serializable]
        public class SquadTemplate
        {
            public string Name;
            public SquadRole Role;
            public JobClass[] PreferredJobs;
            public Squad.Formation Formation;
        }
        
        // Predefined Guild Templates
        private static readonly AIGuildTemplate[] GuildTemplates = new AIGuildTemplate[]
        {
            // Novice Guilds
            new AIGuildTemplate
            {
                Name = "Rookie Raiders",
                Difficulty = Difficulty.Novice,
                MinLevel = 1,
                MaxLevel = 5,
                SquadTemplates = new SquadTemplate[]
                {
                    new SquadTemplate 
                    { 
                        Name = "Rookie Squad A", 
                        Role = SquadRole.Vanguard,
                        PreferredJobs = new[] { JobClass.Warrior, JobClass.Warrior, JobClass.Knight },
                        Formation = Squad.Formation.Defensive
                    },
                    new SquadTemplate 
                    { 
                        Name = "Rookie Squad B", 
                        Role = SquadRole.Assault,
                        PreferredJobs = new[] { JobClass.Warrior, JobClass.Ranger, JobClass.Assassin },
                        Formation = Squad.Formation.Standard
                    },
                    new SquadTemplate 
                    { 
                        Name = "Rookie Squad C", 
                        Role = SquadRole.Support,
                        PreferredJobs = new[] { JobClass.Priest, JobClass.Ranger, JobClass.Warrior },
                        Formation = Squad.Formation.Healing
                    },
                    new SquadTemplate 
                    { 
                        Name = "Rookie Squad D", 
                        Role = SquadRole.Reserve,
                        PreferredJobs = new[] { JobClass.Mage, JobClass.Ranger, JobClass.Warrior },
                        Formation = Squad.Formation.Ranged
                    }
                }
            },
            
            // Bronze Guilds
            new AIGuildTemplate
            {
                Name = "Iron Wolves",
                Difficulty = Difficulty.Bronze,
                MinLevel = 5,
                MaxLevel = 10,
                SquadTemplates = new SquadTemplate[]
                {
                    new SquadTemplate 
                    { 
                        Name = "Wolf Pack Alpha", 
                        Role = SquadRole.Vanguard,
                        PreferredJobs = new[] { JobClass.Knight, JobClass.Knight, JobClass.Warrior, JobClass.Warrior },
                        Formation = Squad.Formation.Defensive
                    },
                    new SquadTemplate 
                    { 
                        Name = "Wolf Pack Beta", 
                        Role = SquadRole.Assault,
                        PreferredJobs = new[] { JobClass.Assassin, JobClass.Assassin, JobClass.Warrior, JobClass.Ranger },
                        Formation = Squad.Formation.Offensive
                    },
                    new SquadTemplate 
                    { 
                        Name = "Wolf Pack Support", 
                        Role = SquadRole.Support,
                        PreferredJobs = new[] { JobClass.Priest, JobClass.Priest, JobClass.Sage, JobClass.Knight },
                        Formation = Squad.Formation.Healing
                    },
                    new SquadTemplate 
                    { 
                        Name = "Wolf Pack Reserve", 
                        Role = SquadRole.Reserve,
                        PreferredJobs = new[] { JobClass.Mage, JobClass.Mage, JobClass.Ranger, JobClass.Sage },
                        Formation = Squad.Formation.Ranged
                    }
                }
            },
            
            // Silver Guilds
            new AIGuildTemplate
            {
                Name = "Silver Eagles",
                Difficulty = Difficulty.Silver,
                MinLevel = 10,
                MaxLevel = 15,
                SquadTemplates = new SquadTemplate[]
                {
                    new SquadTemplate 
                    { 
                        Name = "Eagle Guard", 
                        Role = SquadRole.Vanguard,
                        PreferredJobs = new[] { JobClass.Knight, JobClass.Knight, JobClass.Knight, JobClass.Warrior, JobClass.Warrior },
                        Formation = Squad.Formation.Defensive
                    },
                    new SquadTemplate 
                    { 
                        Name = "Eagle Strike", 
                        Role = SquadRole.Assault,
                        PreferredJobs = new[] { JobClass.Ranger, JobClass.Ranger, JobClass.Ranger, JobClass.Assassin, JobClass.Assassin },
                        Formation = Squad.Formation.Ranged
                    },
                    new SquadTemplate 
                    { 
                        Name = "Eagle Support", 
                        Role = SquadRole.Support,
                        PreferredJobs = new[] { JobClass.Priest, JobClass.Sage, JobClass.Sage, JobClass.Priest, JobClass.Knight },
                        Formation = Squad.Formation.Healing
                    },
                    new SquadTemplate 
                    { 
                        Name = "Eagle Magic", 
                        Role = SquadRole.Reserve,
                        PreferredJobs = new[] { JobClass.Mage, JobClass.Mage, JobClass.Mage, JobClass.Sage, JobClass.Sage },
                        Formation = Squad.Formation.Ranged
                    }
                }
            }
        };
        
        // Name pools for variety
        private static readonly string[] GuildPrefixes = { "Iron", "Steel", "Shadow", "Storm", "Fire", "Ice", "Lightning", "Dragon", "Phoenix", "Crystal" };
        private static readonly string[] GuildSuffixes = { "Legion", "Brigade", "Company", "Order", "Brotherhood", "Alliance", "Vanguard", "Guard", "Knights", "Crusaders" };
        
        private static readonly string[] AdventurerFirstNames = { "Alex", "Blake", "Casey", "Drew", "Ellis", "Finn", "Gray", "Harper", "Iris", "Jay", "Kai", "Luna", "Max", "Nova", "Oak", "Phoenix", "Quinn", "River", "Sage", "Sky" };
        private static readonly string[] AdventurerTitles = { "the Bold", "the Swift", "the Wise", "the Strong", "the Cunning", "the Brave", "the Mighty", "the Silent", "the Fierce", "the Noble" };
        
        // Generate AI Guild
        public static List<Squad> GenerateAIGuild(Difficulty difficulty, int playerGuildLevel)
        {
            List<Squad> aiSquads = new List<Squad>();
            
            // Select appropriate template
            AIGuildTemplate template = GetTemplateForDifficulty(difficulty);
            
            // Generate squads based on template
            for (int i = 0; i < Core.BattleManager.SQUADS_PER_SIDE; i++)
            {
                Squad squad = GenerateSquad(template.SquadTemplates[i], i, playerGuildLevel);
                aiSquads.Add(squad);
            }
            
            return aiSquads;
        }
        
        static AIGuildTemplate GetTemplateForDifficulty(Difficulty difficulty)
        {
            var templates = GuildTemplates.Where(t => t.Difficulty == difficulty).ToList();
            
            if (templates.Count == 0)
            {
                // Fallback to novice if no template found
                templates = GuildTemplates.Where(t => t.Difficulty == Difficulty.Novice).ToList();
            }
            
            return templates[Random.Range(0, templates.Count)];
        }
        
        static Squad GenerateSquad(SquadTemplate template, int squadIndex, int playerLevel)
        {
            Squad squad = new Squad(template.Name, squadIndex, false);
            squad.Role = template.Role;
            squad.CurrentFormation = template.Formation;
            
            // Calculate AI unit levels based on player level and difficulty
            int minLevel = Mathf.Max(1, playerLevel - 2);
            int maxLevel = playerLevel + 2;
            
            // Generate units based on template
            int unitsToGenerate = Mathf.Min(9, template.PreferredJobs.Length * 2);
            
            for (int i = 0; i < unitsToGenerate; i++)
            {
                JobClass jobClass = template.PreferredJobs[i % template.PreferredJobs.Length];
                Unit unit = GenerateAIUnit(jobClass, Random.Range(minLevel, maxLevel + 1));
                squad.AddUnit(unit);
            }
            
            return squad;
        }
        
        static Unit GenerateAIUnit(JobClass jobClass, int level)
        {
            string name = GenerateAdventurerName();
            Unit unit = new Unit(name, level, jobClass);
            
            // Apply AI stat bonuses based on level
            float statMultiplier = 1f + (level * 0.05f); // 5% stat increase per level
            
            unit.maxHP *= statMultiplier;
            unit.currentHP = unit.maxHP;
            unit.attackPower *= statMultiplier;
            unit.defense *= statMultiplier;
            unit.magicPower *= statMultiplier;
            unit.speed *= statMultiplier;
            
            // Add some randomness to make units unique
            unit.maxHP *= Random.Range(0.9f, 1.1f);
            unit.attackPower *= Random.Range(0.9f, 1.1f);
            unit.defense *= Random.Range(0.9f, 1.1f);
            unit.magicPower *= Random.Range(0.9f, 1.1f);
            unit.speed *= Random.Range(0.9f, 1.1f);
            
            return unit;
        }
        
        static string GenerateAdventurerName()
        {
            string firstName = AdventurerFirstNames[Random.Range(0, AdventurerFirstNames.Length)];
            
            if (Random.value < 0.3f) // 30% chance for a title
            {
                string title = AdventurerTitles[Random.Range(0, AdventurerTitles.Length)];
                return $"{firstName} {title}";
            }
            
            return firstName;
        }
        
        public static string GenerateGuildName()
        {
            string prefix = GuildPrefixes[Random.Range(0, GuildPrefixes.Length)];
            string suffix = GuildSuffixes[Random.Range(0, GuildSuffixes.Length)];
            return $"{prefix} {suffix}";
        }
        
        // Get recommended difficulty based on player level
        public static Difficulty GetRecommendedDifficulty(int playerLevel)
        {
            if (playerLevel < 5) return Difficulty.Novice;
            if (playerLevel < 10) return Difficulty.Bronze;
            if (playerLevel < 15) return Difficulty.Silver;
            if (playerLevel < 20) return Difficulty.Gold;
            if (playerLevel < 25) return Difficulty.Platinum;
            if (playerLevel < 30) return Difficulty.Diamond;
            return Difficulty.Legendary;
        }
        
        // Get rewards based on difficulty
        public static void GetBattleRewards(Difficulty difficulty, out int gold, out int exp, out int reputation)
        {
            switch (difficulty)
            {
                case Difficulty.Novice:
                    gold = Random.Range(50, 100);
                    exp = Random.Range(100, 200);
                    reputation = 10;
                    break;
                    
                case Difficulty.Bronze:
                    gold = Random.Range(100, 200);
                    exp = Random.Range(200, 400);
                    reputation = 20;
                    break;
                    
                case Difficulty.Silver:
                    gold = Random.Range(200, 400);
                    exp = Random.Range(400, 800);
                    reputation = 40;
                    break;
                    
                case Difficulty.Gold:
                    gold = Random.Range(400, 800);
                    exp = Random.Range(800, 1600);
                    reputation = 80;
                    break;
                    
                case Difficulty.Platinum:
                    gold = Random.Range(800, 1600);
                    exp = Random.Range(1600, 3200);
                    reputation = 160;
                    break;
                    
                case Difficulty.Diamond:
                    gold = Random.Range(1600, 3200);
                    exp = Random.Range(3200, 6400);
                    reputation = 320;
                    break;
                    
                case Difficulty.Legendary:
                    gold = Random.Range(3200, 6400);
                    exp = Random.Range(6400, 12800);
                    reputation = 640;
                    break;
                    
                default:
                    gold = 50;
                    exp = 100;
                    reputation = 10;
                    break;
            }
        }
    }
}