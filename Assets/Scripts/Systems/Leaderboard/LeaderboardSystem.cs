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
    /// 리더보드 및 랭킹 시스템
    /// 길드 랭킹, 개인 랭킹, 시즌별 랭킹 등을 관리
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

        [Header("Leaderboard Settings")]
        [SerializeField] private int maxLeaderboardEntries = 100;
        [SerializeField] private float updateInterval = 60f; // 1분마다 업데이트
        [SerializeField] private bool enableWeeklyReset = true;
        [SerializeField] private bool enableMonthlyReset = true;
        
        // 리더보드 데이터
        private Dictionary<LeaderboardType, List<LeaderboardEntry>> leaderboards;
        private Dictionary<string, PlayerRankingData> playerRankings;
        private float lastUpdateTime;
        
        // 이벤트
        public event Action<LeaderboardType> OnLeaderboardUpdated;
        public event Action<string, int, int> OnRankingChanged; // playerId, oldRank, newRank
        public event Action<LeaderboardType> OnLeaderboardReset;
        
        public enum LeaderboardType
        {
            GuildPower,          // 길드 전투력
            GuildWealth,         // 길드 재산
            GuildReputation,     // 길드 명성
            GuildLevel,          // 길드 레벨
            BattleWins,          // 전투 승리 횟수
            // DungeonProgress,     // 던전 진행도 - Dungeon 기능 제거됨
            AchievementPoints,   // 업적 점수
            WeeklyChallenge,     // 주간 챌린지
            MonthlyChallenge,    // 월간 챌린지
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
            SubscribeToEvents();
        }
        
        void SubscribeToEvents()
        {
            var eventManager = EventManager.Instance;
            if (eventManager != null)
            {
                eventManager.Subscribe(GuildMaster.Core.EventType.GuildLevelUp, OnGuildLevelUp);
                eventManager.Subscribe(GuildMaster.Core.EventType.BattleVictory, OnBattleVictory);
                // eventManager.Subscribe(GuildMaster.Core.EventType.DungeonCompleted, OnDungeonCompleted); // Dungeon 기능 제거됨
                eventManager.Subscribe(GuildMaster.Core.EventType.AchievementUnlocked, OnAchievementUnlocked);
                eventManager.Subscribe(GuildMaster.Core.EventType.ResourceChanged, OnResourceChanged);
            }
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
            
            // 최고 기록 갱신
            if (rankInfo.bestRank == 0 || rank < rankInfo.bestRank)
            {
                rankInfo.bestRank = rank;
                rankInfo.achievedDate = DateTime.Now;
            }
            
            // 총 포인트 계산
            CalculateTotalPoints(playerData);
        }
        
        void CalculateTotalPoints(PlayerRankingData playerData)
        {
            playerData.totalPoints = 0;
            
            foreach (var ranking in playerData.rankings.Values)
            {
                // 랭킹에 따른 포인트 계산 (1위: 100점, 2위: 90점, ...)
                int points = Math.Max(0, 110 - ranking.rank * 10);
                playerData.totalPoints += points;
            }
        }
        
        /// <summary>
        /// 초기 리더보드 생성 (AI 길드 데이터 사용)
        /// </summary>
        void GenerateInitialLeaderboards()
        {
            // AI 길드 생성
            var aiGuilds = GenerateAIGuildData(30);
            
            foreach (var guild in aiGuilds)
            {
                // 길드 파워
                SubmitScore(LeaderboardType.GuildPower, guild.id, guild.name, guild.power);
                
                // 길드 재산
                SubmitScore(LeaderboardType.GuildWealth, guild.id, guild.name, guild.wealth);
                
                // 길드 명성
                SubmitScore(LeaderboardType.GuildReputation, guild.id, guild.name, guild.reputation);
                
                // 길드 레벨
                SubmitScore(LeaderboardType.GuildLevel, guild.id, guild.name, guild.level);
                
                // 전투 승리
                SubmitScore(LeaderboardType.BattleWins, guild.id, guild.name, guild.battleWins);
            }
        }
        
        List<AIGuildData> GenerateAIGuildData(int count)
        {
            var guilds = new List<AIGuildData>();
            string[] prefixes = { "용맹한", "강철", "불꽃", "서리", "황금", "은빛", "붉은", "푸른", "검은", "하얀" };
            string[] suffixes = { "사자단", "독수리단", "늑대단", "용단", "기사단", "수호자", "전사단", "마법단", "십자군", "성기사단" };
            
            for (int i = 0; i < count; i++)
            {
                var guild = new AIGuildData
                {
                    id = $"ai_guild_{i}",
                    name = $"{prefixes[UnityEngine.Random.Range(0, prefixes.Length)]} {suffixes[UnityEngine.Random.Range(0, suffixes.Length)]}",
                    level = UnityEngine.Random.Range(5, 50),
                    power = UnityEngine.Random.Range(1000, 50000),
                    wealth = UnityEngine.Random.Range(10000, 1000000),
                    reputation = UnityEngine.Random.Range(100, 10000),
                    battleWins = UnityEngine.Random.Range(10, 500)
                };
                
                guilds.Add(guild);
            }
            
            return guilds;
        }
        
        class AIGuildData
        {
            public string id;
            public string name;
            public int level;
            public int power;
            public int wealth;
            public int reputation;
            public int battleWins;
        }
        
        /// <summary>
        /// 리더보드 조회
        /// </summary>
        public List<LeaderboardEntry> GetLeaderboard(LeaderboardType type, int count = 10)
        {
            if (!leaderboards.ContainsKey(type))
                return new List<LeaderboardEntry>();
            
            var leaderboard = leaderboards[type];
            return leaderboard.Take(Math.Min(count, leaderboard.Count)).ToList();
        }
        
        /// <summary>
        /// 플레이어 랭킹 조회
        /// </summary>
        public int GetPlayerRank(LeaderboardType type, string playerId)
        {
            if (!leaderboards.ContainsKey(type))
                return -1;
            
            var entry = leaderboards[type].FirstOrDefault(e => e.id == playerId);
            return entry?.rank ?? -1;
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
        
        // 이벤트 핸들러
        void OnGuildLevelUp(GameEvent gameEvent)
        {
            var guildId = GameManager.Instance?.GuildManager?.GetGuildId() ?? "player";
            var guildName = GameManager.Instance?.GuildManager?.GetGuildName() ?? "Player Guild";
            var newLevel = gameEvent.GetParameter<int>("newLevel");
            
            SubmitScore(LeaderboardType.GuildLevel, guildId, guildName, newLevel);
        }
        
        void OnBattleVictory(GameEvent gameEvent)
        {
            var guildId = GameManager.Instance?.GuildManager?.GetGuildId() ?? "player";
            var guildName = GameManager.Instance?.GuildManager?.GetGuildName() ?? "Player Guild";
            
            var currentWins = GetPlayerScore(LeaderboardType.BattleWins, guildId);
            SubmitScore(LeaderboardType.BattleWins, guildId, guildName, currentWins + 1);
        }
        
        // void OnDungeonCompleted(GameEvent gameEvent) // Dungeon 기능 제거됨
        // {
        //     var guildId = GameManager.Instance?.GuildManager?.GetGuildId() ?? "player";
        //     var guildName = GameManager.Instance?.GuildManager?.GetGuildName() ?? "Player Guild";
        //     var floor = gameEvent.GetParameter<int>("floor");
        //     
        //     var currentProgress = GetPlayerScore(LeaderboardType.DungeonProgress, guildId);
        //     if (floor > currentProgress)
        //     {
        //         SubmitScore(LeaderboardType.DungeonProgress, guildId, guildName, floor);
        //     }
        // }
        
        void OnAchievementUnlocked(GameEvent gameEvent)
        {
            var guildId = GameManager.Instance?.GuildManager?.GetGuildId() ?? "player";
            var guildName = GameManager.Instance?.GuildManager?.GetGuildName() ?? "Player Guild";
            var points = gameEvent.GetParameter<int>("points");
            
            var currentPoints = GetPlayerScore(LeaderboardType.AchievementPoints, guildId);
            SubmitScore(LeaderboardType.AchievementPoints, guildId, guildName, currentPoints + points);
        }
        
        void OnResourceChanged(GameEvent gameEvent)
        {
            // 길드 재산 업데이트
            UpdateGuildWealth();
        }
        
        void UpdateGuildWealth()
        {
            var guildId = GameManager.Instance?.GuildManager?.GetGuildId() ?? "player";
            var guildName = GameManager.Instance?.GuildManager?.GetGuildName() ?? "Player Guild";
            // ResourceManager 타입이 제거되어 주석 처리
            // var resourceManager = GameManager.Instance?.ResourceManager;
            // 
            // if (resourceManager != null)
            // {
            //     int totalWealth = resourceManager.GetGold() + 
            //                     resourceManager.GetWood() * 2 + 
            //                     resourceManager.GetStone() * 3 + 
            //                     resourceManager.GetManaStone() * 10;
            //     
            //     SubmitScore(LeaderboardType.GuildWealth, guildId, guildName, totalWealth);
            // }
            
            // 임시로 0 값 제출
            SubmitScore(LeaderboardType.GuildWealth, guildId, guildName, 0);
        }
        
        int GetPlayerScore(LeaderboardType type, string playerId)
        {
            var entry = leaderboards[type].FirstOrDefault(e => e.id == playerId);
            return entry?.score ?? 0;
        }
        
        // 저장/로드
        void SaveLeaderboardData()
        {
            // 리더보드 데이터 저장 (PlayerPrefs 또는 파일 시스템 사용)
            foreach (var kvp in leaderboards)
            {
                string json = JsonUtility.ToJson(new SerializableList<LeaderboardEntry>(kvp.Value));
                PlayerPrefs.SetString($"Leaderboard_{kvp.Key}", json);
            }
            
            PlayerPrefs.Save();
        }
        
        void LoadLeaderboardData()
        {
            foreach (LeaderboardType type in Enum.GetValues(typeof(LeaderboardType)))
            {
                string key = $"Leaderboard_{type}";
                if (PlayerPrefs.HasKey(key))
                {
                    string json = PlayerPrefs.GetString(key);
                    var data = JsonUtility.FromJson<SerializableList<LeaderboardEntry>>(json);
                    if (data != null && data.items != null)
                    {
                        leaderboards[type] = data.items;
                    }
                }
            }
        }
        
        [System.Serializable]
        class SerializableList<T>
        {
            public List<T> items;
            
            public SerializableList(List<T> list)
            {
                items = list;
            }
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveLeaderboardData();
            }
        }
        
        void OnDestroy()
        {
            SaveLeaderboardData();
            
            // 이벤트 구독 해제
            var eventManager = EventManager.Instance;
            if (eventManager != null)
            {
                eventManager.Unsubscribe(GuildMaster.Core.EventType.GuildLevelUp, OnGuildLevelUp);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.BattleVictory, OnBattleVictory);
                // eventManager.Unsubscribe(GuildMaster.Core.EventType.DungeonCompleted, OnDungeonCompleted); // Dungeon 기능 제거됨
                eventManager.Unsubscribe(GuildMaster.Core.EventType.AchievementUnlocked, OnAchievementUnlocked);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.ResourceChanged, OnResourceChanged);
            }
        }
    }
}