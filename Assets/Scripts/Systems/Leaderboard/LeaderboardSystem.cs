using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Core;
using GuildMaster.Data;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 리더보드 시스템 - 길드 순위 관리
    /// </summary>
    public class LeaderboardSystem : MonoBehaviour
    {
        private static LeaderboardSystem _instance;
        public static LeaderboardSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<LeaderboardSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("LeaderboardSystem");
                        _instance = go.AddComponent<LeaderboardSystem>();
                    }
                }
                return _instance;
            }
        }
        
        // Events
        public event Action<LeaderboardType> OnLeaderboardUpdated;
        public event Action<string, int, int> OnRankingChanged; // playerId, oldRank, newRank
        public event Action<LeaderboardType> OnLeaderboardReset;
        
        // Leaderboard types
        public enum LeaderboardType
        {
            GuildLevel,          // 길드 레벨
            TotalWealth,         // 총 재산
            BattleWins,          // 전투 승리
            QuestCompleted,      // 퀘스트 완료
            AchievementPoints,   // 업적 점수
            WeeklyChallenge,     // 주간 도전
            MonthlyChallenge,    // 월간 도전
            Season               // 시즌 랭킹
        }
        
        [System.Serializable]
        public class LeaderboardEntry
        {
            public string id;
            public string name;
            public int score;
            public int rank;
            public int previousRank;
            public DateTime lastUpdated;
            public Dictionary<string, object> metadata;
            
            public LeaderboardEntry()
            {
                metadata = new Dictionary<string, object>();
                lastUpdated = DateTime.Now;
            }
            
            public int GetRankChange()
            {
                if (previousRank == 0) return 0;
                return previousRank - rank;
            }
        }
        
        [System.Serializable]
        public class PlayerRankingData
        {
            public string playerId;
            public string playerName;
            public Dictionary<LeaderboardType, RankInfo> rankings;
            public int totalPoints;
            public DateTime joinDate;
            
            public PlayerRankingData()
            {
                rankings = new Dictionary<LeaderboardType, RankInfo>();
                joinDate = DateTime.Now;
            }
        }
        
        [System.Serializable]
        public class RankInfo
        {
            public int rank;
            public int score;
            public int bestRank;
            public DateTime achievedDate;
        }
        
        // Leaderboard data
        private Dictionary<LeaderboardType, List<LeaderboardEntry>> leaderboards;
        private Dictionary<string, PlayerRankingData> playerRankings;
        
        // Settings
        [Header("Leaderboard Settings")]
        [SerializeField] private int maxLeaderboardEntries = 100;
        [SerializeField] private float updateInterval = 60f; // 60초마다 업데이트
        [SerializeField] private bool enableWeeklyReset = true;
        [SerializeField] private bool enableMonthlyReset = true;
        [SerializeField] private int simulatedAIGuilds = 50;
        
        private float lastUpdateTime;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeLeaderboards();
        }
        
        void InitializeLeaderboards()
        {
            leaderboards = new Dictionary<LeaderboardType, List<LeaderboardEntry>>();
            playerRankings = new Dictionary<string, PlayerRankingData>();
            
            // 모든 리더보드 타입 초기화
            foreach (LeaderboardType type in Enum.GetValues(typeof(LeaderboardType)))
            {
                leaderboards[type] = new List<LeaderboardEntry>();
            }
            
            // 저장된 데이터 로드
            LoadLeaderboardData();
            
            // AI 길드 데이터로 초기 리더보드 생성
            GenerateInitialLeaderboards();
        }
        
        void Start()
        {
            StartCoroutine(UpdateLeaderboardsRoutine());
        }
        
        IEnumerator UpdateLeaderboardsRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(updateInterval);
                UpdateAllLeaderboards();
            }
        }
        
        /// <summary>
        /// 리더보드에 점수 제출
        /// </summary>
        public void SubmitScore(LeaderboardType type, string id, string name, int score, Dictionary<string, object> metadata = null)
        {
            var entry = GetOrCreateEntry(type, id);
            entry.name = name;
            entry.score = score;
            entry.lastUpdated = DateTime.Now;
            
            if (metadata != null)
            {
                entry.metadata = metadata;
            }
            
            // 즉시 해당 리더보드 업데이트
            UpdateLeaderboard(type);
            
            // 플레이어 랭킹 데이터 업데이트
            UpdatePlayerRanking(id, name, type, entry.rank, score);
        }
        
        LeaderboardEntry GetOrCreateEntry(LeaderboardType type, string id)
        {
            var leaderboard = leaderboards[type];
            var entry = leaderboard.FirstOrDefault(e => e.id == id);
            
            if (entry == null)
            {
                entry = new LeaderboardEntry
                {
                    id = id,
                    rank = leaderboard.Count + 1
                };
                leaderboard.Add(entry);
            }
            
            return entry;
        }
        
        void UpdateLeaderboard(LeaderboardType type)
        {
            var leaderboard = leaderboards[type];
            
            // 이전 랭크 저장
            foreach (var entry in leaderboard)
            {
                entry.previousRank = entry.rank;
            }
            
            // 점수 기준으로 정렬
            leaderboard.Sort((a, b) => b.score.CompareTo(a.score));
            
            // 새로운 랭크 할당
            for (int i = 0; i < leaderboard.Count; i++)
            {
                var entry = leaderboard[i];
                int oldRank = entry.rank;
                entry.rank = i + 1;
                
                // 랭킹 변경 이벤트
                if (oldRank != entry.rank)
                {
                    OnRankingChanged?.Invoke(entry.id, oldRank, entry.rank);
                }
            }
            
            // 최대 엔트리 수 제한
            if (leaderboard.Count > maxLeaderboardEntries)
            {
                leaderboard.RemoveRange(maxLeaderboardEntries, leaderboard.Count - maxLeaderboardEntries);
            }
            
            OnLeaderboardUpdated?.Invoke(type);
        }
        
        void UpdateAllLeaderboards()
        {
            foreach (LeaderboardType type in Enum.GetValues(typeof(LeaderboardType)))
            {
                UpdateLeaderboard(type);
            }
            
            lastUpdateTime = Time.time;
        }
        
        void UpdatePlayerRanking(string playerId, string playerName, LeaderboardType type, int rank, int score)
        {
            if (!playerRankings.ContainsKey(playerId))
            {
                playerRankings[playerId] = new PlayerRankingData
                {
                    playerId = playerId,
                    playerName = playerName
                };
            }
            
            var playerData = playerRankings[playerId];
            
            if (!playerData.rankings.ContainsKey(type))
            {
                playerData.rankings[type] = new RankInfo();
            }
            
            var rankInfo = playerData.rankings[type];
            rankInfo.rank = rank;
            rankInfo.score = score;
            
            if (rank < rankInfo.bestRank || rankInfo.bestRank == 0)
            {
                rankInfo.bestRank = rank;
                rankInfo.achievedDate = DateTime.Now;
            }
            
            // 총 점수 계산
            CalculateTotalPoints(playerData);
        }
        
        void CalculateTotalPoints(PlayerRankingData playerData)
        {
            playerData.totalPoints = 0;
            
            foreach (var kvp in playerData.rankings)
            {
                // 랭킹에 따른 포인트 계산 (1위: 100점, 2위: 90점, ...)
                int points = Math.Max(0, 100 - (kvp.Value.rank - 1) * 10);
                playerData.totalPoints += points;
            }
        }
        
        void GenerateInitialLeaderboards()
        {
            // AI 길드 생성
            for (int i = 0; i < simulatedAIGuilds; i++)
            {
                string guildId = $"ai_guild_{i}";
                string guildName = GenerateGuildName();
                
                // 각 리더보드에 랜덤 점수 생성
                foreach (LeaderboardType type in Enum.GetValues(typeof(LeaderboardType)))
                {
                    int score = GenerateScoreForType(type);
                    SubmitScore(type, guildId, guildName, score);
                }
            }
        }
        
        string GenerateGuildName()
        {
            string[] prefixes = { "Iron", "Golden", "Silver", "Diamond", "Crystal", "Shadow", "Light", "Dark", "Fire", "Ice" };
            string[] suffixes = { "Knights", "Warriors", "Guardians", "Legion", "Order", "Brotherhood", "Alliance", "Company", "Guild", "Force" };
            
            return $"{prefixes[UnityEngine.Random.Range(0, prefixes.Length)]} {suffixes[UnityEngine.Random.Range(0, suffixes.Length)]}";
        }
        
        int GenerateScoreForType(LeaderboardType type)
        {
            return type switch
            {
                LeaderboardType.GuildLevel => UnityEngine.Random.Range(1, 50),
                LeaderboardType.TotalWealth => UnityEngine.Random.Range(1000, 1000000),
                LeaderboardType.BattleWins => UnityEngine.Random.Range(0, 500),
                LeaderboardType.QuestCompleted => UnityEngine.Random.Range(0, 200),
                LeaderboardType.AchievementPoints => UnityEngine.Random.Range(0, 5000),
                LeaderboardType.WeeklyChallenge => UnityEngine.Random.Range(0, 1000),
                LeaderboardType.MonthlyChallenge => UnityEngine.Random.Range(0, 5000),
                LeaderboardType.Season => UnityEngine.Random.Range(0, 10000),
                _ => 0
            };
        }
        
        // Public Methods
        /// <summary>
        /// 특정 리더보드의 상위 엔트리 조회
        /// </summary>
        public List<LeaderboardEntry> GetTopEntries(LeaderboardType type, int count = 10)
        {
            if (!leaderboards.ContainsKey(type))
                return new List<LeaderboardEntry>();
            
            var leaderboard = leaderboards[type];
            return leaderboard.Take(Math.Min(count, leaderboard.Count)).ToList();
        }
        
        /// <summary>
        /// 플레이어의 특정 리더보드 점수 조회
        /// </summary>
        public int GetPlayerScore(LeaderboardType type, string playerId)
        {
            var entry = GetEntry(type, playerId);
            return entry?.score ?? 0;
        }
        
        /// <summary>
        /// 플레이어의 특정 리더보드 랭킹 조회
        /// </summary>
        public int GetPlayerRank(LeaderboardType type, string playerId)
        {
            var entry = GetEntry(type, playerId);
            return entry?.rank ?? -1;
        }
        
        LeaderboardEntry GetEntry(LeaderboardType type, string playerId)
        {
            if (!leaderboards.ContainsKey(type))
                return null;
            
            return leaderboards[type].FirstOrDefault(e => e.id == playerId);
        }
        
        /// <summary>
        /// 플레이어 주변 랭킹 조회
        /// </summary>
        public List<LeaderboardEntry> GetNearbyEntries(LeaderboardType type, string playerId, int range = 5)
        {
            var rank = GetPlayerRank(type, playerId);
            if (rank == -1)
                return new List<LeaderboardEntry>();
            
            var leaderboard = leaderboards[type];
            int startIndex = Math.Max(0, rank - range - 1);
            int endIndex = Math.Min(leaderboard.Count, rank + range);
            
            return leaderboard.GetRange(startIndex, endIndex - startIndex);
        }
        
        /// <summary>
        /// 종합 랭킹 조회
        /// </summary>
        public List<PlayerRankingData> GetOverallRanking(int count = 10)
        {
            var sortedPlayers = playerRankings.Values
                .OrderByDescending(p => p.totalPoints)
                .Take(count)
                .ToList();
            
            return sortedPlayers;
        }
        
        /// <summary>
        /// 리더보드 리셋
        /// </summary>
        public void ResetLeaderboard(LeaderboardType type)
        {
            if (leaderboards.ContainsKey(type))
            {
                leaderboards[type].Clear();
                OnLeaderboardReset?.Invoke(type);
            }
        }
        
        /// <summary>
        /// 주간 리셋
        /// </summary>
        public void WeeklyReset()
        {
            if (!enableWeeklyReset) return;
            
            ResetLeaderboard(LeaderboardType.WeeklyChallenge);
            Debug.Log("Weekly leaderboard reset completed");
        }
        
        /// <summary>
        /// 월간 리셋
        /// </summary>
        public void MonthlyReset()
        {
            if (!enableMonthlyReset) return;
            
            ResetLeaderboard(LeaderboardType.MonthlyChallenge);
            Debug.Log("Monthly leaderboard reset completed");
        }
        
        // 수동으로 점수 업데이트하는 메서드들
        public void UpdateGuildLevel(string guildId, string guildName, int level)
        {
            SubmitScore(LeaderboardType.GuildLevel, guildId, guildName, level);
        }
        
        public void UpdateBattleWins(string guildId, string guildName)
        {
            var currentWins = GetPlayerScore(LeaderboardType.BattleWins, guildId);
            SubmitScore(LeaderboardType.BattleWins, guildId, guildName, currentWins + 1);
        }
        
        public void UpdateAchievementPoints(string guildId, string guildName, int points)
        {
            var currentPoints = GetPlayerScore(LeaderboardType.AchievementPoints, guildId);
            SubmitScore(LeaderboardType.AchievementPoints, guildId, guildName, currentPoints + points);
        }
        
        public void UpdateGuildWealth(string guildId, string guildName, int totalWealth)
        {
            SubmitScore(LeaderboardType.TotalWealth, guildId, guildName, totalWealth);
        }
        
        // Save/Load methods
        void SaveLeaderboardData()
        {
            // TODO: 실제 저장 구현
        }
        
        void LoadLeaderboardData()
        {
            // TODO: 실제 로드 구현
        }
    }
}