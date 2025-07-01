using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Core;
using GuildMaster.Battle;
using GuildMaster.Data;
using GuildMaster.UI;

namespace GuildMaster.Systems
{
    /// <summary>
    /// AI 길드 대항전 시스템
    /// 정기적인 길드전, 토너먼트, 랭킹 시스템 관리
    /// </summary>
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
                    }
                }
                return _instance;
            }
        }
        
        [Header("Guild Battle Settings")]
        [SerializeField] private float dailyBattleResetTime = 5f; // 오전 5시
        [SerializeField] private int maxDailyBattles = 5;
        [SerializeField] private float battleCooldown = 300f; // 5분
        
        [Header("Tournament Settings")]
        [SerializeField] private int tournamentSize = 8;
        [SerializeField] private float tournamentInterval = 604800f; // 7일
        [SerializeField] private int tournamentEntryFee = 1000;
        
        [Header("Ranking Settings")]
        [SerializeField] private int rankingUpdateInterval = 3600; // 1시간
        [SerializeField] private int topRankingCount = 100;
        
        // 대항전 데이터
        private List<AIGuildData> aiGuilds = new List<AIGuildData>();
        private Dictionary<string, GuildBattleRecord> battleRecords = new Dictionary<string, GuildBattleRecord>();
        private List<GuildRanking> guildRankings = new List<GuildRanking>();
        
        // 현재 상태
        private int dailyBattlesCompleted = 0;
        private float lastBattleTime = 0f;
        private DateTime lastDailyReset;
        private bool isInTournament = false;
        private TournamentData currentTournament;
        
        // 매칭 시스템
        private Queue<BattleRequest> battleQueue = new Queue<BattleRequest>();
        private Coroutine matchmakingCoroutine;
        
        // 이벤트
        public event Action<GuildBattleResult> OnBattleCompleted;
        public event Action<TournamentResult> OnTournamentCompleted;
        public event Action<List<GuildRanking>> OnRankingUpdated;
        public event Action<int> OnDailyBattlesReset;
        
        [System.Serializable]
        public class AIGuildData
        {
            public string guildId;
            public string guildName;
            public int guildLevel;
            public int rating;
            public AIGuildGenerator.Difficulty difficulty;
            public GuildStats stats;
            public List<Squad> squads;
            public DateTime lastUpdated;
            
            public AIGuildData(string name, int level, AIGuildGenerator.Difficulty diff)
            {
                guildId = Guid.NewGuid().ToString();
                guildName = name;
                guildLevel = level;
                difficulty = diff;
                rating = 1000 + (level * 50);
                stats = new GuildStats();
                lastUpdated = DateTime.Now;
            }
        }
        
        [System.Serializable]
        public class GuildBattleRecord
        {
            public string recordId;
            public string playerGuildId;
            public string aiGuildId;
            public DateTime battleTime;
            public bool victory;
            public int damageDealt;
            public int damageTaken;
            public int unitsLost;
            public int enemyUnitsKilled;
            public float battleDuration;
            public BattleRewards rewards;
        }
        
        [System.Serializable]
        public class GuildRanking
        {
            public int rank;
            public string guildId;
            public string guildName;
            public int rating;
            public int wins;
            public int losses;
            public float winRate;
            public int totalBattles;
            public DateTime lastBattle;
        }
        
        [System.Serializable]
        public class TournamentData
        {
            public string tournamentId;
            public string tournamentName;
            public DateTime startTime;
            public TournamentPhase currentPhase;
            public List<TournamentMatch> matches;
            public List<string> participants;
            public Dictionary<string, int> scores;
            public TournamentRewards rewards;
        }
        
        [System.Serializable]
        public class TournamentMatch
        {
            public string matchId;
            public string guild1Id;
            public string guild2Id;
            public string winnerId;
            public TournamentPhase phase;
            public DateTime scheduledTime;
            public bool isCompleted;
        }
        
        public enum TournamentPhase
        {
            Registration,
            RoundOf8,
            SemiFinal,
            Final,
            Completed
        }
        
        [System.Serializable]
        public class BattleRequest
        {
            public string requestId;
            public BattleType battleType;
            public AIGuildGenerator.Difficulty requestedDifficulty;
            public DateTime requestTime;
            public Action<GuildBattleResult> callback;
        }
        
        public enum BattleType
        {
            Daily,
            Ranked,
            Tournament,
            Friendly,
            Special
        }
        
        [System.Serializable]
        public class GuildBattleResult
        {
            public string battleId;
            public BattleType battleType;
            public bool victory;
            public int ratingChange;
            public BattleRewards rewards;
            public BattleStatistics statistics;
            public string replayData;
        }
        
        [System.Serializable]
        public class BattleRewards
        {
            public int gold;
            public int experience;
            public int reputation;
            public List<string> itemIds;
            public Dictionary<ResourceType, int> resources;
        }
        
        [System.Serializable]
        public class BattleStatistics
        {
            public int totalDamageDealt;
            public int totalDamageTaken;
            public int totalHealing;
            public int criticalHits;
            public int skillsUsed;
            public float averageUnitHealth;
            public string mvpUnitId;
            public Dictionary<string, int> unitKills;
        }
        
        [System.Serializable]
        public class TournamentResult
        {
            public string tournamentId;
            public int finalRank;
            public TournamentRewards rewards;
            public List<TournamentMatch> matchHistory;
        }
        
        [System.Serializable]
        public class TournamentRewards
        {
            public Dictionary<int, BattleRewards> rankRewards;
            public string specialRewardItemId;
            public string tournamentTitle;
        }
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeSystem();
        }
        
        void Start()
        {
            GenerateAIGuilds();
            StartCoroutine(DailyResetCoroutine());
            StartCoroutine(RankingUpdateCoroutine());
            matchmakingCoroutine = StartCoroutine(MatchmakingCoroutine());
        }
        
        void InitializeSystem()
        {
            lastDailyReset = DateTime.Today.AddHours(dailyBattleResetTime);
            if (DateTime.Now > lastDailyReset)
            {
                lastDailyReset = lastDailyReset.AddDays(1);
            }
        }
        
        /// <summary>
        /// AI 길드 생성
        /// </summary>
        void GenerateAIGuilds()
        {
            // 각 난이도별로 길드 생성
            foreach (AIGuildGenerator.Difficulty difficulty in Enum.GetValues(typeof(AIGuildGenerator.Difficulty)))
            {
                int guildCount = GetGuildCountForDifficulty(difficulty);
                
                for (int i = 0; i < guildCount; i++)
                {
                    string guildName = AIGuildGenerator.GenerateGuildName();
                    int level = GetLevelForDifficulty(difficulty) + UnityEngine.Random.Range(-2, 3);
                    
                    var aiGuild = new AIGuildData(guildName, level, difficulty);
                    aiGuild.squads = AIGuildGenerator.GenerateAIGuild(difficulty, level);
                    
                    // 길드 스탯 계산
                    CalculateGuildStats(aiGuild);
                    
                    aiGuilds.Add(aiGuild);
                }
            }
            
            // 초기 랭킹 생성
            UpdateRankings();
        }
        
        int GetGuildCountForDifficulty(AIGuildGenerator.Difficulty difficulty)
        {
            return difficulty switch
            {
                AIGuildGenerator.Difficulty.Novice => 20,
                AIGuildGenerator.Difficulty.Bronze => 15,
                AIGuildGenerator.Difficulty.Silver => 10,
                AIGuildGenerator.Difficulty.Gold => 8,
                AIGuildGenerator.Difficulty.Platinum => 6,
                AIGuildGenerator.Difficulty.Diamond => 4,
                AIGuildGenerator.Difficulty.Legendary => 2,
                _ => 10
            };
        }
        
        int GetLevelForDifficulty(AIGuildGenerator.Difficulty difficulty)
        {
            return difficulty switch
            {
                AIGuildGenerator.Difficulty.Novice => 3,
                AIGuildGenerator.Difficulty.Bronze => 8,
                AIGuildGenerator.Difficulty.Silver => 13,
                AIGuildGenerator.Difficulty.Gold => 18,
                AIGuildGenerator.Difficulty.Platinum => 23,
                AIGuildGenerator.Difficulty.Diamond => 28,
                AIGuildGenerator.Difficulty.Legendary => 33,
                _ => 5
            };
        }
        
        void CalculateGuildStats(AIGuildData guild)
        {
            guild.stats = new GuildStats();
            
            foreach (var squad in guild.squads)
            {
                guild.stats.totalMembers += squad.units.Count;
                guild.stats.averageLevel += squad.units.Sum(u => u.level);
                guild.stats.totalPower += squad.units.Sum(u => u.GetCombatPower());
            }
            
            if (guild.stats.totalMembers > 0)
            {
                guild.stats.averageLevel /= guild.stats.totalMembers;
            }
        }
        
        /// <summary>
        /// 일일 대항전 신청
        /// </summary>
        public void RequestDailyBattle(AIGuildGenerator.Difficulty difficulty = AIGuildGenerator.Difficulty.Bronze)
        {
            if (!CanStartBattle())
            {
                Debug.LogWarning("Cannot start battle: cooldown or daily limit reached");
                return;
            }
            
            var request = new BattleRequest
            {
                requestId = Guid.NewGuid().ToString(),
                battleType = BattleType.Daily,
                requestedDifficulty = difficulty,
                requestTime = DateTime.Now
            };
            
            battleQueue.Enqueue(request);
        }
        
        /// <summary>
        /// 랭킹전 신청
        /// </summary>
        public void RequestRankedBattle()
        {
            if (!CanStartBattle())
            {
                Debug.LogWarning("Cannot start ranked battle");
                return;
            }
            
            // 플레이어 레이팅 기반 매칭
            int playerRating = GetPlayerRating();
            var difficulty = GetDifficultyForRating(playerRating);
            
            var request = new BattleRequest
            {
                requestId = Guid.NewGuid().ToString(),
                battleType = BattleType.Ranked,
                requestedDifficulty = difficulty,
                requestTime = DateTime.Now
            };
            
            battleQueue.Enqueue(request);
        }
        
        bool CanStartBattle()
        {
            if (dailyBattlesCompleted >= maxDailyBattles)
                return false;
                
            if (Time.time - lastBattleTime < battleCooldown)
                return false;
                
            var battleManager = FindObjectOfType<BattleManager>();
            if (battleManager != null && battleManager.CurrentBattleState == BattleManager.BattleState.Preparation)
                return false;
                
            return true;
        }
        
        /// <summary>
        /// 매치메이킹 코루틴
        /// </summary>
        IEnumerator MatchmakingCoroutine()
        {
            while (true)
            {
                if (battleQueue.Count > 0) // && !BattleManager.Instance.IsBattleActive) // Instance 패턴이 없으므로 주석 처리
                {
                    var request = battleQueue.Dequeue();
                    yield return StartCoroutine(ProcessBattleRequest(request));
                }
                
                yield return new WaitForSeconds(1f);
            }
        }
        
        IEnumerator ProcessBattleRequest(BattleRequest request)
        {
            // 적절한 AI 길드 찾기
            var opponent = FindOpponent(request);
            if (opponent == null)
            {
                Debug.LogError("No suitable opponent found");
                yield break;
            }
            
            // 전투 준비
            PrepareBattle(opponent);
            
            // 전투 시작
            var battleManager = FindObjectOfType<BattleManager>();
            if (battleManager != null)
            {
                // BattleManager.Instance.StartGuildBattle(opponent.squads); // StartGuildBattle 메서드가 없으므로 주석 처리
            
                // 전투 종료 대기
                // while (BattleManager.Instance.IsBattleActive) // Instance 패턴이 없으므로 주석 처리
                // {
                //     yield return null;
                // }
                yield return new WaitForSeconds(1f); // 임시 대기
            }
            
            // 전투 결과 처리
            ProcessBattleResult(request, opponent);
        }
        
        AIGuildData FindOpponent(BattleRequest request)
        {
            List<AIGuildData> candidates;
            
            if (request.battleType == BattleType.Ranked)
            {
                // 레이팅 기반 매칭
                int playerRating = GetPlayerRating();
                candidates = aiGuilds.Where(g => 
                    Mathf.Abs(g.rating - playerRating) < 200
                ).ToList();
            }
            else
            {
                // 난이도 기반 매칭
                candidates = aiGuilds.Where(g => 
                    g.difficulty == request.requestedDifficulty
                ).ToList();
            }
            
            if (candidates.Count == 0)
            {
                // 폴백: 아무 길드나 선택
                candidates = aiGuilds;
            }
            
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }
        
        void PrepareBattle(AIGuildData opponent)
        {
            // AI 길드 스쿼드 갱신
            opponent.squads = AIGuildGenerator.GenerateAIGuild(
                opponent.difficulty, 
                opponent.guildLevel
            );
            
            // 전투 시작 시간 기록
            lastBattleTime = Time.time;
        }
        
        void ProcessBattleResult(BattleRequest request, AIGuildData opponent)
        {
            // var battleResult = BattleManager.Instance.GetLastBattleResult(); // Instance 패턴이 없으므로 임시 처리
            bool victory = UnityEngine.Random.value > 0.5f; // 임시로 랜덤 승패
            
            // 전투 기록 생성
            var record = new GuildBattleRecord
            {
                recordId = Guid.NewGuid().ToString(),
                playerGuildId = "player",
                aiGuildId = opponent.guildId,
                battleTime = DateTime.Now,
                victory = victory,
                damageDealt = UnityEngine.Random.Range(1000, 5000), // 임시 값
                damageTaken = UnityEngine.Random.Range(800, 4000), // 임시 값
                unitsLost = UnityEngine.Random.Range(0, 5), // 임시 값
                enemyUnitsKilled = UnityEngine.Random.Range(0, 8), // 임시 값
                battleDuration = UnityEngine.Random.Range(30f, 180f) // 임시 값
            };
            
            // 보상 계산
            var rewards = CalculateBattleRewards(request.battleType, opponent.difficulty, victory);
            record.rewards = rewards;
            
            // 레이팅 변경 (랭킹전인 경우)
            int ratingChange = 0;
            if (request.battleType == BattleType.Ranked)
            {
                ratingChange = CalculateRatingChange(GetPlayerRating(), opponent.rating, victory);
                UpdatePlayerRating(GetPlayerRating() + ratingChange);
                opponent.rating -= ratingChange;
            }
            
            // 전투 기록 저장
            string recordKey = $"{record.playerGuildId}_{record.battleTime.Ticks}";
            battleRecords[recordKey] = record;
            
            // 일일 전투 카운트 증가
            if (request.battleType == BattleType.Daily)
            {
                dailyBattlesCompleted++;
            }
            
            // 보상 지급
            ApplyBattleRewards(rewards);
            
            // 결과 생성
            var result = new GuildBattleResult
            {
                battleId = record.recordId,
                battleType = request.battleType,
                victory = victory,
                ratingChange = ratingChange,
                rewards = rewards,
                statistics = GenerateBattleStatistics(record)
            };
            
            // 이벤트 발생
            OnBattleCompleted?.Invoke(result);
            
            // 랭킹 업데이트
            if (request.battleType == BattleType.Ranked)
            {
                UpdateRankings();
            }
        }
        
        BattleRewards CalculateBattleRewards(BattleType battleType, AIGuildGenerator.Difficulty difficulty, bool victory)
        {
            var rewards = new BattleRewards
            {
                resources = new Dictionary<ResourceType, int>()
            };
            
            // 기본 보상
            AIGuildGenerator.GetBattleRewards(difficulty, out int gold, out int exp, out int reputation);
            
            // 전투 타입별 보너스
            float multiplier = battleType switch
            {
                BattleType.Daily => 1f,
                BattleType.Ranked => 1.5f,
                BattleType.Tournament => 2f,
                BattleType.Special => 3f,
                _ => 1f
            };
            
            // 승리/패배 보정
            if (!victory)
            {
                multiplier *= 0.3f; // 패배 시 30%만 획득
            }
            
            rewards.gold = Mathf.RoundToInt(gold * multiplier);
            rewards.experience = Mathf.RoundToInt(exp * multiplier);
            rewards.reputation = Mathf.RoundToInt(reputation * multiplier);
            
            // 추가 자원 보상
            rewards.resources[ResourceType.Gold] = rewards.gold;
            
            if (UnityEngine.Random.value < 0.3f) // 30% 확률로 추가 자원
            {
                rewards.resources[ResourceType.Wood] = UnityEngine.Random.Range(50, 200);
                rewards.resources[ResourceType.Stone] = UnityEngine.Random.Range(30, 150);
            }
            
            if (UnityEngine.Random.value < 0.1f && victory) // 10% 확률로 마나스톤 (승리 시만)
            {
                rewards.resources[ResourceType.ManaStone] = UnityEngine.Random.Range(5, 20);
            }
            
            // 아이템 보상 (추후 구현)
            rewards.itemIds = new List<string>();
            
            return rewards;
        }
        
        void ApplyBattleRewards(BattleRewards rewards)
        {
            var resourceManager = ResourceManager.Instance;
            
            foreach (var resource in rewards.resources)
            {
                resourceManager.AddResource(resource.Key, resource.Value, "Guild Battle Reward");
            }
            
            // 경험치와 명성은 GuildManager에서 처리
            if (GameManager.Instance?.GuildManager != null)
            {
                // GameManager.Instance.GuildManager.AddExperience(rewards.experience);
                // GameManager.Instance.GuildManager.AddReputation(rewards.reputation);
            }
        }
        
        int CalculateRatingChange(int playerRating, int opponentRating, bool victory)
        {
            // ELO 레이팅 시스템
            float expectedScore = 1f / (1f + Mathf.Pow(10f, (opponentRating - playerRating) / 400f));
            float actualScore = victory ? 1f : 0f;
            
            int kFactor = 32; // K-factor
            int change = Mathf.RoundToInt(kFactor * (actualScore - expectedScore));
            
            return Mathf.Clamp(change, -50, 50); // 최대 ±50
        }
        
        BattleStatistics GenerateBattleStatistics(GuildBattleRecord record)
        {
            var stats = new BattleStatistics
            {
                totalDamageDealt = record.damageDealt,
                totalDamageTaken = record.damageTaken,
                totalHealing = 0, // 임시 값
                criticalHits = 0, // 임시 값
                skillsUsed = 0, // 임시 값
                unitKills = new Dictionary<string, int>()
            };
            
            // MVP 계산 등 추가 통계
            
            return stats;
        }
        
        /// <summary>
        /// 토너먼트 시작
        /// </summary>
        public void StartTournament(string tournamentName)
        {
            if (isInTournament)
            {
                Debug.LogWarning("Tournament already in progress");
                return;
            }
            
            currentTournament = new TournamentData
            {
                tournamentId = Guid.NewGuid().ToString(),
                tournamentName = tournamentName,
                startTime = DateTime.Now,
                currentPhase = TournamentPhase.Registration,
                matches = new List<TournamentMatch>(),
                participants = new List<string>(),
                scores = new Dictionary<string, int>(),
                rewards = GenerateTournamentRewards()
            };
            
            isInTournament = true;
            
            // 참가자 등록 (플레이어 + AI 길드들)
            RegisterTournamentParticipants();
            
            // 대진표 생성
            GenerateTournamentBracket();
            
            StartCoroutine(RunTournament());
        }
        
        void RegisterTournamentParticipants()
        {
            // 플레이어 등록
            currentTournament.participants.Add("player");
            currentTournament.scores["player"] = 0;
            
            // AI 길드 선택 (레이팅 상위)
            var topGuilds = aiGuilds.OrderByDescending(g => g.rating)
                                   .Take(tournamentSize - 1)
                                   .ToList();
            
            foreach (var guild in topGuilds)
            {
                currentTournament.participants.Add(guild.guildId);
                currentTournament.scores[guild.guildId] = 0;
            }
        }
        
        void GenerateTournamentBracket()
        {
            // 8강전 대진표 생성
            var participants = new List<string>(currentTournament.participants);
            
            // 시드 배정 (레이팅 순)
            participants = participants.OrderByDescending(p => 
                p == "player" ? GetPlayerRating() : 
                aiGuilds.FirstOrDefault(g => g.guildId == p)?.rating ?? 0
            ).ToList();
            
            // 1-8, 2-7, 3-6, 4-5 매칭
            for (int i = 0; i < 4; i++)
            {
                var match = new TournamentMatch
                {
                    matchId = Guid.NewGuid().ToString(),
                    guild1Id = participants[i],
                    guild2Id = participants[7 - i],
                    phase = TournamentPhase.RoundOf8,
                    scheduledTime = DateTime.Now.AddMinutes(i * 5),
                    isCompleted = false
                };
                
                currentTournament.matches.Add(match);
            }
        }
        
        IEnumerator RunTournament()
        {
            // 각 페이즈별 진행
            yield return StartCoroutine(RunTournamentPhase(TournamentPhase.RoundOf8));
            yield return StartCoroutine(RunTournamentPhase(TournamentPhase.SemiFinal));
            yield return StartCoroutine(RunTournamentPhase(TournamentPhase.Final));
            
            // 토너먼트 종료
            CompleteTournament();
        }
        
        IEnumerator RunTournamentPhase(TournamentPhase phase)
        {
            currentTournament.currentPhase = phase;
            
            var phaseMatches = currentTournament.matches.Where(m => m.phase == phase && !m.isCompleted).ToList();
            
            foreach (var match in phaseMatches)
            {
                yield return StartCoroutine(RunTournamentMatch(match));
                yield return new WaitForSeconds(5f); // 경기 간 대기
            }
            
            // 다음 페이즈 대진표 생성
            if (phase != TournamentPhase.Final)
            {
                GenerateNextPhaseMatches(phase);
            }
        }
        
        IEnumerator RunTournamentMatch(TournamentMatch match)
        {
            bool isPlayerMatch = match.guild1Id == "player" || match.guild2Id == "player";
            
            if (isPlayerMatch)
            {
                // 플레이어 경기
                string opponentId = match.guild1Id == "player" ? match.guild2Id : match.guild1Id;
                var opponent = aiGuilds.FirstOrDefault(g => g.guildId == opponentId);
                
                if (opponent != null)
                {
                    PrepareBattle(opponent);
                    // BattleManager.Instance.StartGuildBattle(opponent.squads); // StartGuildBattle 메서드가 없으므로 주석 처리
                    
                    // 전투 종료 대기
                    // while (BattleManager.Instance.IsBattleActive) // Instance 패턴이 없으므로 주석 처리
                    // {
                    //     yield return null;
                    // }
                    yield return new WaitForSeconds(1f); // 임시 대기
                    
                    // var result = BattleManager.Instance.GetLastBattleResult(); // Instance 패턴이 없으므로 주석 처리
                }
            }
            else
            {
                // AI vs AI 시뮬레이션
                yield return StartCoroutine(SimulateAIMatch(match));
            }
            
            match.isCompleted = true;
            currentTournament.scores[match.winnerId]++;
        }
        
        IEnumerator SimulateAIMatch(TournamentMatch match)
        {
            // AI 대 AI 전투 시뮬레이션
            var guild1 = aiGuilds.FirstOrDefault(g => g.guildId == match.guild1Id);
            var guild2 = aiGuilds.FirstOrDefault(g => g.guildId == match.guild2Id);
            
            if (guild1 != null && guild2 != null)
            {
                // 간단한 파워 비교 시뮬레이션
                float guild1Power = guild1.stats.totalPower * UnityEngine.Random.Range(0.8f, 1.2f);
                float guild2Power = guild2.stats.totalPower * UnityEngine.Random.Range(0.8f, 1.2f);
                
                match.winnerId = guild1Power > guild2Power ? guild1.guildId : guild2.guildId;
            }
            
            yield return new WaitForSeconds(2f); // 시뮬레이션 시간
        }
        
        void GenerateNextPhaseMatches(TournamentPhase currentPhase)
        {
            var winners = currentTournament.matches
                .Where(m => m.phase == currentPhase && m.isCompleted)
                .Select(m => m.winnerId)
                .ToList();
            
            TournamentPhase nextPhase = currentPhase switch
            {
                TournamentPhase.RoundOf8 => TournamentPhase.SemiFinal,
                TournamentPhase.SemiFinal => TournamentPhase.Final,
                _ => TournamentPhase.Completed
            };
            
            for (int i = 0; i < winners.Count; i += 2)
            {
                if (i + 1 < winners.Count)
                {
                    var match = new TournamentMatch
                    {
                        matchId = Guid.NewGuid().ToString(),
                        guild1Id = winners[i],
                        guild2Id = winners[i + 1],
                        phase = nextPhase,
                        scheduledTime = DateTime.Now,
                        isCompleted = false
                    };
                    
                    currentTournament.matches.Add(match);
                }
            }
        }
        
        void CompleteTournament()
        {
            currentTournament.currentPhase = TournamentPhase.Completed;
            
            // 최종 순위 계산
            var rankings = currentTournament.scores
                .OrderByDescending(s => s.Value)
                .Select((s, i) => new { GuildId = s.Key, Rank = i + 1 })
                .ToList();
            
            // 플레이어 순위 찾기
            int playerRank = rankings.FirstOrDefault(r => r.GuildId == "player")?.Rank ?? 8;
            
            // 토너먼트 결과 생성
            var result = new TournamentResult
            {
                tournamentId = currentTournament.tournamentId,
                finalRank = playerRank,
                rewards = currentTournament.rewards,
                matchHistory = currentTournament.matches.Where(m => 
                    m.guild1Id == "player" || m.guild2Id == "player"
                ).ToList()
            };
            
            // 보상 지급
            ApplyTournamentRewards(playerRank);
            
            // 이벤트 발생
            OnTournamentCompleted?.Invoke(result);
            
            isInTournament = false;
            currentTournament = null;
        }
        
        TournamentRewards GenerateTournamentRewards()
        {
            var rewards = new TournamentRewards
            {
                rankRewards = new Dictionary<int, BattleRewards>()
            };
            
            // 순위별 보상 설정
            for (int rank = 1; rank <= 8; rank++)
            {
                var reward = new BattleRewards
                {
                    gold = 10000 / rank,
                    experience = 5000 / rank,
                    reputation = 1000 / rank,
                    resources = new Dictionary<ResourceType, int>
                    {
                        { ResourceType.Gold, 10000 / rank },
                        { ResourceType.ManaStone, 100 / rank }
                    }
                };
                
                rewards.rankRewards[rank] = reward;
            }
            
            rewards.specialRewardItemId = "tournament_trophy";
            rewards.tournamentTitle = "Champion";
            
            return rewards;
        }
        
        void ApplyTournamentRewards(int rank)
        {
            if (currentTournament?.rewards?.rankRewards?.ContainsKey(rank) == true)
            {
                ApplyBattleRewards(currentTournament.rewards.rankRewards[rank]);
            }
        }
        
        /// <summary>
        /// 일일 리셋 코루틴
        /// </summary>
        IEnumerator DailyResetCoroutine()
        {
            while (true)
            {
                if (DateTime.Now > lastDailyReset)
                {
                    PerformDailyReset();
                    lastDailyReset = lastDailyReset.AddDays(1);
                }
                
                yield return new WaitForSeconds(60f); // 1분마다 체크
            }
        }
        
        void PerformDailyReset()
        {
            dailyBattlesCompleted = 0;
            OnDailyBattlesReset?.Invoke(maxDailyBattles);
            
            // AI 길드 갱신
            foreach (var guild in aiGuilds)
            {
                // 레벨 조정
                if (UnityEngine.Random.value < 0.3f) // 30% 확률로 레벨 변동
                {
                    guild.guildLevel += UnityEngine.Random.Range(-1, 2);
                    guild.guildLevel = Mathf.Clamp(guild.guildLevel, 1, 50);
                }
                
                // 스쿼드 재생성
                guild.squads = AIGuildGenerator.GenerateAIGuild(guild.difficulty, guild.guildLevel);
                CalculateGuildStats(guild);
                
                guild.lastUpdated = DateTime.Now;
            }
        }
        
        /// <summary>
        /// 랭킹 업데이트 코루틴
        /// </summary>
        IEnumerator RankingUpdateCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(rankingUpdateInterval);
                UpdateRankings();
            }
        }
        
        void UpdateRankings()
        {
            guildRankings.Clear();
            
            // 플레이어 랭킹 추가
            var playerRanking = CalculateGuildRanking("player", "Player Guild", GetPlayerRating());
            guildRankings.Add(playerRanking);
            
            // AI 길드 랭킹 추가
            foreach (var guild in aiGuilds)
            {
                var ranking = CalculateGuildRanking(guild.guildId, guild.guildName, guild.rating);
                guildRankings.Add(ranking);
            }
            
            // 레이팅 순으로 정렬
            guildRankings = guildRankings.OrderByDescending(r => r.rating).ToList();
            
            // 순위 부여
            for (int i = 0; i < guildRankings.Count; i++)
            {
                guildRankings[i].rank = i + 1;
            }
            
            // 상위 랭킹만 유지
            if (guildRankings.Count > topRankingCount)
            {
                guildRankings = guildRankings.Take(topRankingCount).ToList();
            }
            
            OnRankingUpdated?.Invoke(guildRankings);
        }
        
        GuildRanking CalculateGuildRanking(string guildId, string guildName, int rating)
        {
            var records = battleRecords.Values.Where(r => 
                r.playerGuildId == guildId || r.aiGuildId == guildId
            ).ToList();
            
            int wins = records.Count(r => 
                (r.playerGuildId == guildId && r.victory) || 
                (r.aiGuildId == guildId && !r.victory)
            );
            
            int losses = records.Count(r => 
                (r.playerGuildId == guildId && !r.victory) || 
                (r.aiGuildId == guildId && r.victory)
            );
            
            return new GuildRanking
            {
                guildId = guildId,
                guildName = guildName,
                rating = rating,
                wins = wins,
                losses = losses,
                winRate = records.Count > 0 ? (float)wins / records.Count : 0f,
                totalBattles = records.Count,
                lastBattle = records.Any() ? records.Max(r => r.battleTime) : DateTime.MinValue
            };
        }
        
        // 헬퍼 메서드들
        int GetPlayerRating()
        {
            // PlayerPrefs나 SaveManager에서 플레이어 레이팅 로드
            return PlayerPrefs.GetInt("PlayerGuildRating", 1000);
        }
        
        void UpdatePlayerRating(int newRating)
        {
            PlayerPrefs.SetInt("PlayerGuildRating", newRating);
            PlayerPrefs.Save();
        }
        
        AIGuildGenerator.Difficulty GetDifficultyForRating(int rating)
        {
            if (rating < 800) return AIGuildGenerator.Difficulty.Novice;
            if (rating < 1000) return AIGuildGenerator.Difficulty.Bronze;
            if (rating < 1200) return AIGuildGenerator.Difficulty.Silver;
            if (rating < 1500) return AIGuildGenerator.Difficulty.Gold;
            if (rating < 1800) return AIGuildGenerator.Difficulty.Platinum;
            if (rating < 2100) return AIGuildGenerator.Difficulty.Diamond;
            return AIGuildGenerator.Difficulty.Legendary;
        }
        
        // Public 접근자
        public List<GuildRanking> GetTopRankings(int count = 10)
        {
            return guildRankings.Take(count).ToList();
        }
        
        public int GetPlayerRank()
        {
            var playerRanking = guildRankings.FirstOrDefault(r => r.guildId == "player");
            return playerRanking?.rank ?? -1;
        }
        
        public int GetDailyBattlesRemaining()
        {
            return maxDailyBattles - dailyBattlesCompleted;
        }
        
        public float GetBattleCooldownRemaining()
        {
            return Mathf.Max(0, battleCooldown - (Time.time - lastBattleTime));
        }
        
        public bool IsInTournament()
        {
            return isInTournament;
        }
        
        public TournamentData GetCurrentTournament()
        {
            return currentTournament;
        }
        
        public List<GuildBattleRecord> GetBattleHistory(int count = 10)
        {
            return battleRecords.Values
                .OrderByDescending(r => r.battleTime)
                .Take(count)
                .ToList();
        }
        
        [System.Serializable]
        public class GuildStats
        {
            public int totalMembers;
            public float averageLevel;
            public float totalPower;
            
            public void CalculateFromSquads(List<Squad> squads)
            {
                totalMembers = 0;
                averageLevel = 0;
                totalPower = 0;
                
                foreach (var squad in squads)
                {
                    foreach (var unit in squad.units)
                    {
                        totalMembers++;
                        averageLevel += unit.level;
                        totalPower += unit.GetCombatPower();
                    }
                }
                
                if (totalMembers > 0)
                {
                    averageLevel /= totalMembers;
                }
            }
        }
    }
}