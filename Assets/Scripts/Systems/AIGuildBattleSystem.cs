using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle;
using GuildMaster.Battle.AI;

namespace GuildMaster.Systems
{
    public class AIGuildBattleSystem : MonoBehaviour
    {
        private static AIGuildBattleSystem _instance;
        public static AIGuildBattleSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AIGuildBattleSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AIGuildBattleSystem");
                        _instance = go.AddComponent<AIGuildBattleSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        [Header("AI Battle Settings")]
        [SerializeField] private float battleCheckInterval = 300f; // 5분마다 체크
        [SerializeField] private int maxConcurrentBattles = 3;
        [SerializeField] private float battleDuration = 180f; // 3분
        
        private List<AIBattle> activeBattles = new List<AIBattle>();
        private List<AIGuildData> aiGuilds = new List<AIGuildData>();
        
        // 이벤트
        public event Action<AIBattle> OnBattleStarted;
        public event Action<AIBattleResult> OnBattleCompleted;
        public event Action<AIGuildData> OnAIGuildCreated;
        
        [System.Serializable]
        public class AIGuildData
        {
            public string guildId;
            public string guildName;
            public int guildLevel;
            public int memberCount;
            public int reputation;
            public Vector2 territoryPosition;
            public List<string> memberIds;
            public List<Squad> squads;
            
            public AIGuildData()
            {
                guildId = Guid.NewGuid().ToString();
                memberIds = new List<string>();
                squads = new List<Squad>();
            }
        }
        
        [System.Serializable]
        public class AIBattle
        {
            public string battleId;
            public AIGuildData attacker;
            public AIGuildData defender;
            public List<Squad> attackerSquads;
            public List<Squad> defenderSquads;
            public float startTime;
            public bool isCompleted;
            public AIBattleResult result;
            
            public AIBattle()
            {
                battleId = Guid.NewGuid().ToString();
                attackerSquads = new List<Squad>();
                defenderSquads = new List<Squad>();
                startTime = Time.time;
                isCompleted = false;
            }
        }
        
        [System.Serializable]
        public class AIBattleResult
        {
            public string winnerGuildId;
            public bool isAttackerVictory;
            public int attackerCasualties;
            public int defenderCasualties;
            public float battleDuration;
            public Dictionary<string, int> rewards;
            
            public AIBattleResult()
            {
                rewards = new Dictionary<string, int>();
            }
        }
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            Initialize();
        }
        
        void Initialize()
        {
            // AI 길드 생성
            GenerateAIGuilds();
            
            // 배틀 체크 코루틴 시작
            StartCoroutine(BattleCheckCoroutine());
        }
        
        void GenerateAIGuilds()
        {
            int guildCount = UnityEngine.Random.Range(5, 15);
            
            for (int i = 0; i < guildCount; i++)
            {
                var guild = new AIGuildData
                {
                    guildName = GenerateGuildName(),
                    guildLevel = UnityEngine.Random.Range(1, 50),
                    memberCount = UnityEngine.Random.Range(5, 20),
                    reputation = UnityEngine.Random.Range(100, 10000),
                    territoryPosition = new Vector2(
                        UnityEngine.Random.Range(-100f, 100f),
                        UnityEngine.Random.Range(-100f, 100f)
                    )
                };
                
                // 멤버 생성
                for (int j = 0; j < guild.memberCount; j++)
                {
                    guild.memberIds.Add(Guid.NewGuid().ToString());
                }
                
                // 부대 생성
                GenerateGuildSquads(guild);
                
                aiGuilds.Add(guild);
                OnAIGuildCreated?.Invoke(guild);
            }
            
            Debug.Log($"Generated {guildCount} AI guilds");
        }
        
        void GenerateGuildSquads(AIGuildData guild)
        {
            // 2개 부대 생성 (각 9명씩)
            for (int squadIndex = 0; squadIndex < 2; squadIndex++)
            {
                var squad = new Squad($"AI Squad {squadIndex}", squadIndex, false);
                
                // 9명의 유닛 생성
                for (int i = 0; i < 9; i++)
                {
                    var unit = GenerateAIUnit(guild.guildLevel);
                    squad.AddUnit(unit);
                }
                
                guild.squads.Add(squad);
            }
        }
        
        Unit GenerateAIUnit(int guildLevel)
        {
            JobClass[] classes = { JobClass.Warrior, JobClass.Knight, JobClass.Mage, JobClass.Priest, JobClass.Assassin, JobClass.Ranger };
            JobClass randomClass = classes[UnityEngine.Random.Range(0, classes.Length)];
            
            int level = Mathf.Max(1, guildLevel + UnityEngine.Random.Range(-5, 6));
            
            var unit = new Unit($"AI Unit", level, randomClass);
            
            // 길드 레벨에 따른 스탯 보정
            float statMultiplier = 1f + (guildLevel * 0.1f);
            unit.attackPower *= statMultiplier;
            unit.maxHP *= statMultiplier;
            unit.currentHP = unit.maxHP;
            unit.defense *= statMultiplier;
            unit.magicPower *= statMultiplier;
            
            return unit;
        }
        
        string GenerateGuildName()
        {
            string[] prefixes = { "Dragon", "Phoenix", "Shadow", "Golden", "Silver", "Iron", "Storm", "Fire", "Ice" };
            string[] suffixes = { "Warriors", "Knights", "Guild", "Brotherhood", "Alliance", "Order", "Legion" };
            
            string prefix = prefixes[UnityEngine.Random.Range(0, prefixes.Length)];
            string suffix = suffixes[UnityEngine.Random.Range(0, suffixes.Length)];
            
            return $"{prefix} {suffix}";
        }
        
        IEnumerator BattleCheckCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(battleCheckInterval);
                
                // 새로운 전투 시작 가능 여부 확인
                if (activeBattles.Count < maxConcurrentBattles)
                {
                    StartRandomBattle();
                }
                
                // 진행 중인 전투 완료 체크
                CheckActiveBattles();
            }
        }
        
        void StartRandomBattle()
        {
            if (aiGuilds.Count < 2) return;
            
            // 랜덤하게 두 길드 선택
            var attacker = aiGuilds[UnityEngine.Random.Range(0, aiGuilds.Count)];
            var defender = aiGuilds[UnityEngine.Random.Range(0, aiGuilds.Count)];
            
            if (attacker.guildId == defender.guildId) return;
            
            var battle = new AIBattle
            {
                attacker = attacker,
                defender = defender,
                attackerSquads = new List<Squad>(attacker.squads),
                defenderSquads = new List<Squad>(defender.squads)
            };
            
            activeBattles.Add(battle);
            OnBattleStarted?.Invoke(battle);
            
            Debug.Log($"AI Battle started: {attacker.guildName} vs {defender.guildName}");
        }
        
        void CheckActiveBattles()
        {
            for (int i = activeBattles.Count - 1; i >= 0; i--)
            {
                var battle = activeBattles[i];
                
                if (Time.time - battle.startTime >= battleDuration)
                {
                    CompleteBattle(battle);
                    activeBattles.RemoveAt(i);
                }
            }
        }
        
        void CompleteBattle(AIBattle battle)
        {
            battle.isCompleted = true;
            battle.result = CalculateBattleResult(battle);
            
            // 결과 적용
            ApplyBattleResults(battle);
            
            OnBattleCompleted?.Invoke(battle.result);
            
            Debug.Log($"AI Battle completed: {battle.attacker.guildName} vs {battle.defender.guildName} - Winner: {battle.result.winnerGuildId}");
        }
        
        AIBattleResult CalculateBattleResult(AIBattle battle)
        {
            var result = new AIBattleResult
            {
                battleDuration = Time.time - battle.startTime
            };
            
            // 전투력 계산
            float attackerPower = CalculateSquadsPower(battle.attackerSquads);
            float defenderPower = CalculateSquadsPower(battle.defenderSquads);
            
            // 승자 결정
            if (attackerPower > defenderPower)
            {
                result.winnerGuildId = battle.attacker.guildId;
                result.isAttackerVictory = true;
                result.attackerCasualties = UnityEngine.Random.Range(0, 5);
                result.defenderCasualties = UnityEngine.Random.Range(5, 15);
            }
            else
            {
                result.winnerGuildId = battle.defender.guildId;
                result.isAttackerVictory = false;
                result.attackerCasualties = UnityEngine.Random.Range(5, 15);
                result.defenderCasualties = UnityEngine.Random.Range(0, 5);
            }
            
            // 보상 설정
            result.rewards["gold"] = UnityEngine.Random.Range(100, 1000);
            result.rewards["reputation"] = UnityEngine.Random.Range(10, 100);
            
            return result;
        }
        
        float CalculateSquadsPower(List<Squad> squads)
        {
            float totalPower = 0f;
            
            foreach (var squad in squads)
            {
                foreach (var unit in squad.units)
                {
                    if (unit != null && unit.IsAlive)
                    {
                        totalPower += unit.GetCombatPower();
                    }
                }
            }
            
            return totalPower;
        }
        
        void ApplyBattleResults(AIBattle battle)
        {
            var winner = aiGuilds.Find(g => g.guildId == battle.result.winnerGuildId);
            if (winner != null)
            {
                winner.reputation += battle.result.rewards["reputation"];
            }
        }
        
        // 공개 메서드들
        public List<AIGuildData> GetAllAIGuilds()
        {
            return new List<AIGuildData>(aiGuilds);
        }
        
        public List<AIBattle> GetActiveBattles()
        {
            return new List<AIBattle>(activeBattles);
        }
        
        public void TriggerRandomChallenge()
        {
            if (activeBattles.Count < maxConcurrentBattles)
            {
                StartRandomBattle();
            }
        }
    }
} 