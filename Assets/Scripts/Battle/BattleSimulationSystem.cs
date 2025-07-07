using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle;
using Unit = GuildMaster.Battle.UnitStatus;
using GuildMaster.Core;

namespace GuildMaster.Systems
{
    public class BattleSimulationSystem : MonoBehaviour
    {
        private static BattleSimulationSystem instance;
        public static BattleSimulationSystem Instance => instance;
        
        [Header("Simulation Settings")]
        [SerializeField] private int simulationRuns = 100;
        [SerializeField] private float simulationSpeed = 10f;
        [SerializeField] private bool detailedLogging = false;
        
        private BattleManager battleManager;
        private List<SimulationResult> simulationHistory;
        private SimulationResult currentSimulation;
        private bool isSimulating = false;
        
        public event Action<SimulationResult> OnSimulationComplete;
        public event Action<float> OnSimulationProgress;
        public event Action<BattleResult> OnBattleSimulated;
        
        [System.Serializable]
        public class SimulationResult
        {
            public string simulationId;
            public DateTime startTime;
            public DateTime endTime;
            public int totalBattles;
            public int victories;
            public int defeats;
            public float winRate;
            public Dictionary<string, BattleStatistics> unitStats;
            public Dictionary<JobClass, JobClassStatistics> jobStats;
            public List<BattleResult> battleResults;
            public SimulationType type;
            public string notes;
            
            public SimulationResult()
            {
                simulationId = Guid.NewGuid().ToString();
                startTime = DateTime.Now;
                unitStats = new Dictionary<string, BattleStatistics>();
                jobStats = new Dictionary<JobClass, JobClassStatistics>();
                battleResults = new List<BattleResult>();
            }
        }
        
        [System.Serializable]
        public class BattleResult
        {
            public string battleId;
            public BattleType type;
            public bool isVictory;
            public float duration;
            public int roundsPlayed;
            public List<Squad> playerSquads;
            public List<Squad> enemySquads;
            public Dictionary<string, UnitPerformance> unitPerformances;
            public List<string> keyMoments;
            public DateTime timestamp;
            
            public BattleResult()
            {
                unitPerformances = new Dictionary<string, UnitPerformance>();
                keyMoments = new List<string>();
            }
        }
        
        [System.Serializable]
        public class BattleStatistics
        {
            public string unitId;
            public string unitName;
            public int battlesParticipated;
            public int kills;
            public int deaths;
            public float totalDamageDealt;
            public float totalDamageTaken;
            public float totalHealing;
            public float averageSurvivalTime;
            public Dictionary<string, int> skillUsage;
            public float kdRatio => deaths > 0 ? (float)kills / deaths : kills;
            
            public BattleStatistics()
            {
                skillUsage = new Dictionary<string, int>();
            }
        }
        
        [System.Serializable]
        public class JobClassStatistics
        {
            public JobClass jobClass;
            public int totalUnits;
            public float averageKD;
            public float averageDamage;
            public float averageSurvival;
            public float winRateContribution;
            public Dictionary<string, float> popularSkills;
            
            public JobClassStatistics()
            {
                popularSkills = new Dictionary<string, float>();
            }
        }
        
        [System.Serializable]
        public class UnitPerformance
        {
            public string unitId;
            public bool survived;
            public int killCount;
            public float damageDealt;
            public float damageTaken;
            public float healingDone;
            public int skillsUsed;
            public float survivalTime;
            public List<string> killedUnits;
            
            public UnitPerformance()
            {
                killedUnits = new List<string>();
            }
        }
        
        public enum SimulationType
        {
            Quick,
            Detailed,
            Tournament,
            Training,
            Optimization
        }
        
        public enum BattleType
        {
            Story,
            // Dungeon, // Dungeon 기능 제거됨
            GuildWar,
            Arena,
            Training,
            Special
        }
        
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                simulationHistory = new List<SimulationResult>();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            InitializeReferences();
        }
        
        void InitializeReferences()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                battleManager = gameManager.BattleManager;
            }
        }
        
        public void StartSimulation(SimulationType type, int runs = -1)
        {
            if (isSimulating)
            {
                Debug.LogWarning("Simulation already in progress");
                return;
            }
            
            if (runs > 0)
                simulationRuns = runs;
            
            StartCoroutine(RunSimulation(type));
        }
        
        IEnumerator RunSimulation(SimulationType type)
        {
            isSimulating = true;
            currentSimulation = new SimulationResult { type = type };
            
            Debug.Log($"Starting {type} simulation with {simulationRuns} runs");
            
            for (int i = 0; i < simulationRuns; i++)
            {
                float progress = (float)i / simulationRuns;
                OnSimulationProgress?.Invoke(progress);
                
                yield return SimulateQuickBattle();
                yield return null;
            }
            
            FinalizeSimulation();
            isSimulating = false;
        }
        
        IEnumerator SimulateQuickBattle()
        {
            var playerSquads = GenerateTestSquads(4);
            var enemySquads = GenerateEnemySquads(4, UnityEngine.Random.Range(1, 8));
            
            var result = SimulateBattleOutcome(playerSquads, enemySquads, BattleType.Training);
            ProcessBattleResult(result);
            
            yield return new WaitForSeconds(0.1f / simulationSpeed);
        }
        
        BattleResult SimulateBattleOutcome(List<Squad> playerSquads, List<Squad> enemySquads, BattleType type)
        {
            var result = new BattleResult
            {
                battleId = Guid.NewGuid().ToString(),
                type = type,
                playerSquads = new List<Squad>(playerSquads),
                enemySquads = new List<Squad>(enemySquads),
                timestamp = DateTime.Now
            };
            
            // Calculate total power
            float playerPower = 0f;
            float enemyPower = 0f;
            
            foreach (var squad in playerSquads)
            {
                foreach (var unit in squad.units)
                {
                    if (unit != null)
                        playerPower += unit.GetCombatPower();
                }
            }
            
            foreach (var squad in enemySquads)
            {
                foreach (var unit in squad.units)
                {
                    if (unit != null)
                        enemyPower += unit.GetCombatPower();
                }
            }
            
            // Basic simulation based on power with some randomness
            float powerRatio = playerPower / (playerPower + enemyPower);
            float randomFactor = UnityEngine.Random.Range(0.8f, 1.2f);
            float winChance = powerRatio * randomFactor;
            
            result.isVictory = UnityEngine.Random.value < winChance;
            result.duration = UnityEngine.Random.Range(30f, 180f);
            result.roundsPlayed = Mathf.CeilToInt(result.duration / 10f);
            
            // Simulate unit performances
            SimulateUnitPerformances(result, playerSquads, true);
            SimulateUnitPerformances(result, enemySquads, false);
            
            return result;
        }
        
        void SimulateUnitPerformances(BattleResult result, List<Squad> squads, bool isPlayerSide)
        {
            foreach (var squad in squads)
            {
                foreach (var unit in squad.units)
                {
                    if (unit == null) continue;
                    
                    var performance = new UnitPerformance
                    {
                        unitId = unit.unitId,
                        survived = UnityEngine.Random.value < (result.isVictory == isPlayerSide ? 0.7f : 0.3f),
                        killCount = UnityEngine.Random.Range(0, 5),
                        damageDealt = UnityEngine.Random.Range(100f, 1000f) * unit.attack,
                        damageTaken = UnityEngine.Random.Range(50f, 500f),
                        healingDone = unit.jobClass == JobClass.Priest ? UnityEngine.Random.Range(200f, 800f) : 0f,
                        skillsUsed = UnityEngine.Random.Range(3, 15),
                        survivalTime = UnityEngine.Random.Range(30f, result.duration)
                    };
                    
                    result.unitPerformances[unit.unitId] = performance;
                }
            }
        }
        
        void ProcessBattleResult(BattleResult result)
        {
            currentSimulation.totalBattles++;
            
            if (result.isVictory)
                currentSimulation.victories++;
            else
                currentSimulation.defeats++;
            
            currentSimulation.battleResults.Add(result);
            
            // Update unit statistics
            foreach (var kvp in result.unitPerformances)
            {
                UpdateUnitStatistics(kvp.Key, kvp.Value, result);
            }
            
            OnBattleSimulated?.Invoke(result);
        }
        
        void UpdateUnitStatistics(string unitId, UnitPerformance performance, BattleResult battle)
        {
            if (!currentSimulation.unitStats.ContainsKey(unitId))
            {
                currentSimulation.unitStats[unitId] = new BattleStatistics
                {
                    unitId = unitId,
                    unitName = GetUnitName(unitId)
                };
            }
            
            var stats = currentSimulation.unitStats[unitId];
            stats.battlesParticipated++;
            stats.kills += performance.killCount;
            stats.deaths += performance.survived ? 0 : 1;
            stats.totalDamageDealt += performance.damageDealt;
            stats.totalDamageTaken += performance.damageTaken;
            stats.totalHealing += performance.healingDone;
            stats.averageSurvivalTime = (stats.averageSurvivalTime * (stats.battlesParticipated - 1) + performance.survivalTime) / stats.battlesParticipated;
        }
        
        void FinalizeSimulation()
        {
            currentSimulation.endTime = DateTime.Now;
            currentSimulation.winRate = currentSimulation.totalBattles > 0 ? (float)currentSimulation.victories / currentSimulation.totalBattles : 0f;
            
            // Calculate job class statistics
            CalculateJobClassStatistics();
            
            simulationHistory.Add(currentSimulation);
            
            OnSimulationComplete?.Invoke(currentSimulation);
            
            Debug.Log($"Simulation complete: {currentSimulation.victories}/{currentSimulation.totalBattles} victories ({currentSimulation.winRate:P0})");
        }
        
        void CalculateJobClassStatistics()
        {
            var jobGroups = currentSimulation.unitStats.Values.GroupBy(u => GetUnitJobClass(u.unitId));
            
            foreach (var group in jobGroups)
            {
                var jobStats = new JobClassStatistics
                {
                    jobClass = group.Key,
                    totalUnits = group.Count(),
                    averageKD = group.Average(u => u.kdRatio),
                    averageDamage = group.Average(u => u.totalDamageDealt / Mathf.Max(1, u.battlesParticipated)),
                    averageSurvival = group.Average(u => u.averageSurvivalTime),
                    winRateContribution = 0f
                };
                
                currentSimulation.jobStats[group.Key] = jobStats;
            }
        }
        
        List<Squad> GenerateTestSquads(int count)
        {
            var squads = new List<Squad>();
            
            for (int i = 0; i < count; i++)
            {
                var squad = new Squad($"Test Squad {i + 1}", i, true);
                
                // Add 9 random units
                for (int j = 0; j < 9; j++)
                {
                    JobClass randomClass = (JobClass)UnityEngine.Random.Range(0, 7);
                    var unit = new Unit($"Unit {j}", UnityEngine.Random.Range(10, 20), randomClass);
                    squad.AddUnit(unit);
                }
                
                squads.Add(squad);
            }
            
            return squads;
        }
        
        List<Squad> GenerateEnemySquads(int count, int difficulty)
        {
            var squads = new List<Squad>();
            
            for (int i = 0; i < count; i++)
            {
                var squad = GenerateEnemySquad(difficulty);
                squads.Add(squad);
            }
            
            return squads;
        }
        
        Squad GenerateEnemySquad(int difficulty)
        {
            var squad = new Squad($"Enemy Squad", 0, false);
            
            // Generate units based on difficulty
            for (int i = 0; i < 9; i++)
            {
                var unit = GenerateEnemyUnit(difficulty);
                squad.AddUnit(unit);
            }
            
            return squad;
        }
        
        GuildMaster.Battle.UnitStatus GenerateEnemyUnit(int difficulty)
        {
            JobClass[] availableClasses = { JobClass.Warrior, JobClass.Knight, JobClass.Mage, JobClass.Priest, JobClass.Assassin, JobClass.Ranger };
            JobClass randomClass = availableClasses[UnityEngine.Random.Range(0, availableClasses.Length)];
            int level = UnityEngine.Random.Range(8, 15 + difficulty * 3);
            
            var unit = new Unit($"Enemy {randomClass}", level, randomClass);
            
            // Scale stats based on difficulty
            unit.maxHP *= (1f + difficulty * 0.2f);
            unit.AttackModifiable *= (1f + difficulty * 0.15f);
            unit.defense *= (1f + difficulty * 0.15f);
            unit.currentHP = unit.maxHP;
            
            return unit;
        }
        
        string GetUnitName(string unitId)
        {
            return $"Unit {unitId.Substring(0, Math.Min(8, unitId.Length))}";
        }
        
        JobClass GetUnitJobClass(string unitId)
        {
            return (JobClass)UnityEngine.Random.Range(0, 7);
        }
        
        // Public methods for UI access
        public SimulationResult GetLastSimulation()
        {
            return simulationHistory.LastOrDefault();
        }
        
        public List<SimulationResult> GetSimulationHistory(int count = 10)
        {
            return simulationHistory.TakeLast(count).ToList();
        }
        
        public BattleStatistics GetUnitStatistics(string unitId)
        {
            foreach (var sim in simulationHistory)
            {
                if (sim.unitStats.ContainsKey(unitId))
                    return sim.unitStats[unitId];
            }
            return null;
        }
        
        public Dictionary<JobClass, JobClassStatistics> GetJobClassStatistics()
        {
            if (currentSimulation != null)
                return currentSimulation.jobStats;
                
            var lastSim = GetLastSimulation();
            return lastSim?.jobStats ?? new Dictionary<JobClass, JobClassStatistics>();
        }
        
        public float GetOverallWinRate()
        {
            if (simulationHistory.Count == 0) return 0f;
            
            int totalVictories = simulationHistory.Sum(s => s.victories);
            int totalBattles = simulationHistory.Sum(s => s.totalBattles);
            
            return totalBattles > 0 ? (float)totalVictories / totalBattles : 0f;
        }
        
        public void ClearHistory()
        {
            simulationHistory.Clear();
            Debug.Log("Simulation history cleared");
        }
        
        public bool IsSimulating => isSimulating;
        public float SimulationProgress => currentSimulation != null && currentSimulation.totalBattles > 0 
            ? (float)currentSimulation.totalBattles / simulationRuns : 0f;
    }
}
