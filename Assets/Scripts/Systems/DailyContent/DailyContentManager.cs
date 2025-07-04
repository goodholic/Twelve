using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle; // Unit을 위해 추가
using GuildMaster.Guild; // GuildManager를 위해 추가

namespace GuildMaster.Systems
{
    public enum QuestType
    {
        Daily,
        Weekly,
        Event,
        Achievement
    }
    
    public enum QuestObjective
    {
        CompleteBattles,
        WinBattles,
        ExploreDungeons,
        RecruitAdventurers,
        UpgradeBuildings,
        CollectResources,
        ReachGuildLevel,
        CompleteStoryChapter,
        DefeatBoss,
        TradeWithNPC
    }
    
    [System.Serializable]
    public class Quest
    {
        public string QuestId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public QuestType Type { get; set; }
        public QuestObjective Objective { get; set; }
        public int TargetAmount { get; set; }
        public int CurrentProgress { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsClaimed { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        
        // Rewards
        public int GoldReward { get; set; }
        public int WoodReward { get; set; }
        public int StoneReward { get; set; }
        public int ManaStoneReward { get; set; }
        public int ReputationReward { get; set; }
        public int ExperienceReward { get; set; }
        public List<string> ItemRewards { get; set; }

        // ConvenienceSystem에서 사용하는 프로퍼티들
        public int questId => QuestId.GetHashCode();
        public string questName => Name;
        public int goldReward => GoldReward;
        public int expReward => ExperienceReward;
        public bool isRewarded => IsClaimed;
        
        public Quest(string id, string name, QuestType type, QuestObjective objective, int target)
        {
            QuestId = id;
            Name = name;
            Type = type;
            Objective = objective;
            TargetAmount = target;
            CurrentProgress = 0;
            IsCompleted = false;
            IsClaimed = false;
            ItemRewards = new List<string>();
            
            SetQuestDuration();
        }
        
        void SetQuestDuration()
        {
            StartTime = DateTime.Now;
            
            switch (Type)
            {
                case QuestType.Daily:
                    EndTime = DateTime.Today.AddDays(1);
                    break;
                case QuestType.Weekly:
                    // Next Monday at 00:00
                    int daysUntilMonday = ((int)DayOfWeek.Monday - (int)DateTime.Today.DayOfWeek + 7) % 7;
                    if (daysUntilMonday == 0) daysUntilMonday = 7;
                    EndTime = DateTime.Today.AddDays(daysUntilMonday);
                    break;
                case QuestType.Event:
                    EndTime = StartTime.AddDays(7); // Default 7 days for events
                    break;
                case QuestType.Achievement:
                    EndTime = DateTime.MaxValue; // No time limit
                    break;
            }
        }
        
        public bool IsExpired()
        {
            return DateTime.Now > EndTime;
        }
        
        public float GetRemainingTime()
        {
            if (Type == QuestType.Achievement) return float.MaxValue;
            return (float)(EndTime - DateTime.Now).TotalSeconds;
        }
        
        public void UpdateProgress(int amount)
        {
            if (IsCompleted) return;
            
            CurrentProgress = Mathf.Min(CurrentProgress + amount, TargetAmount);
            
            if (CurrentProgress >= TargetAmount)
            {
                IsCompleted = true;
            }
        }
        
        public float GetProgressPercentage()
        {
            return TargetAmount > 0 ? (float)CurrentProgress / TargetAmount : 0f;
        }
    }
    
    [System.Serializable]
    public class LoginBonus
    {
        public int Day { get; set; }
        public int Gold { get; set; }
        public int Gems { get; set; }
        public string ItemId { get; set; }
        public bool IsClaimed { get; set; }
        public bool IsMilestone { get; set; } // Special reward for 7, 14, 28 days
        
        public LoginBonus(int day, int gold, int gems = 0, string item = null, bool milestone = false)
        {
            Day = day;
            Gold = gold;
            Gems = gems;
            ItemId = item;
            IsMilestone = milestone;
            IsClaimed = false;
        }
    }
    
    public class DailyContentManager : MonoBehaviour
    {
        private static DailyContentManager _instance;
        public static DailyContentManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DailyContentManager>();
                }
                return _instance;
            }
        }
        // Quest management
        private List<Quest> activeQuests;
        private List<Quest> completedQuests;
        private Dictionary<QuestObjective, int> objectiveTracking;
        
        // Login tracking
        private int consecutiveLoginDays;
        private DateTime lastLoginDate;
        private List<LoginBonus> monthlyLoginRewards;
        private int currentLoginCycle; // Which day of the current month
        
        // Season pass
        private int seasonPassLevel;
        private int seasonPassExperience;
        private bool isPremiumPass;
        private List<SeasonPassReward> seasonPassRewards;
        
        // Events
        public event Action<Quest> OnQuestCompleted;
        public event Action<Quest> OnQuestClaimed;
        public event Action<int> OnLoginBonusClaimed;
        public event Action<int> OnSeasonPassLevelUp;
        public event Action OnDailyReset;
        public event Action OnWeeklyReset;
        
        // Configuration
        private const int MAX_DAILY_QUESTS = 5;
        private const int MAX_WEEKLY_QUESTS = 3;
        
        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }

            activeQuests = new List<Quest>();
            completedQuests = new List<Quest>();
            objectiveTracking = new Dictionary<QuestObjective, int>();
            
            InitializeLoginRewards();
            LoadProgress();
            
            // Subscribe to game events
            SubscribeToGameEvents();
        }
        
        void Start()
        {
            CheckDailyReset();
            CheckWeeklyReset();
            CheckLoginBonus();
            
            // Start checking for resets
            InvokeRepeating(nameof(CheckDailyReset), 60f, 60f); // Check every minute
        }
        
        void InitializeLoginRewards()
        {
            monthlyLoginRewards = new List<LoginBonus>();
            
            // Generate 28-day login cycle
            for (int day = 1; day <= 28; day++)
            {
                int gold = 100 * day;
                int gems = 0;
                string item = null;
                bool milestone = false;
                
                // Milestone rewards
                if (day == 7)
                {
                    gems = 50;
                    item = "rare_recruit_ticket";
                    milestone = true;
                }
                else if (day == 14)
                {
                    gems = 100;
                    item = "epic_equipment_box";
                    milestone = true;
                }
                else if (day == 28)
                {
                    gems = 200;
                    item = "legendary_recruit_ticket";
                    milestone = true;
                }
                else if (day % 5 == 0)
                {
                    gems = 20;
                }
                
                monthlyLoginRewards.Add(new LoginBonus(day, gold, gems, item, milestone));
            }
        }
        
        void SubscribeToGameEvents()
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return;
            
            // Subscribe to battle events
            if (gameManager.BattleManager != null)
            {
                gameManager.BattleManager.OnBattleEnd += OnBattleEnd;
            }
            
            // Subscribe to other events
            if (gameManager.GuildManager != null)
            {
                gameManager.GuildManager.OnAdventurerRecruited += OnAdventurerRecruited;
                gameManager.GuildManager.OnBuildingUpgraded += OnBuildingUpgraded;
            }
        }
        
        void CheckDailyReset()
        {
            DateTime now = DateTime.Now;
            DateTime lastReset = GetLastDailyReset();
            
            if (now.Date > lastReset.Date)
            {
                PerformDailyReset();
            }
        }
        
        void CheckWeeklyReset()
        {
            DateTime now = DateTime.Now;
            DateTime lastReset = GetLastWeeklyReset();
            
            // Check if it's Monday and we haven't reset this week
            if (now.DayOfWeek == DayOfWeek.Monday && (now - lastReset).TotalDays >= 7)
            {
                PerformWeeklyReset();
            }
        }
        
        void PerformDailyReset()
        {
            // Remove expired daily quests
            activeQuests.RemoveAll(q => q.Type == QuestType.Daily && !q.IsClaimed);
            
            // Generate new daily quests
            GenerateDailyQuests();
            
            // Save reset time
            PlayerPrefs.SetString("LastDailyReset", DateTime.Now.ToString());
            
            OnDailyReset?.Invoke();
        }
        
        void PerformWeeklyReset()
        {
            // Remove expired weekly quests
            activeQuests.RemoveAll(q => q.Type == QuestType.Weekly && !q.IsClaimed);
            
            // Generate new weekly quests
            GenerateWeeklyQuests();
            
            // Save reset time
            PlayerPrefs.SetString("LastWeeklyReset", DateTime.Now.ToString());
            
            OnWeeklyReset?.Invoke();
        }
        
        void GenerateDailyQuests()
        {
            var guildLevel = Core.GameManager.Instance?.GuildManager?.GetGuildData().GuildLevel ?? 1;
            
            // Create a pool of possible daily quests
            var questPool = new List<Quest>
            {
                new Quest("daily_battles", "전투의 달인", QuestType.Daily, QuestObjective.CompleteBattles, 5)
                {
                    Description = "전투를 5회 완료하세요",
                    GoldReward = 500,
                    ExperienceReward = 100
                },
                
                new Quest("daily_wins", "승리의 길", QuestType.Daily, QuestObjective.WinBattles, 3)
                {
                    Description = "전투에서 3회 승리하세요",
                    GoldReward = 800,
                    ReputationReward = 20
                },
                
                new Quest("daily_explore", "던전 탐험가", QuestType.Daily, QuestObjective.ExploreDungeons, 2)
                {
                    Description = "던전을 2개 탐험하세요",
                    GoldReward = 600,
                    ManaStoneReward = 10
                },
                
                new Quest("daily_resources", "자원 수집가", QuestType.Daily, QuestObjective.CollectResources, 1000)
                {
                    Description = "자원을 총 1000개 수집하세요",
                    GoldReward = 400,
                    WoodReward = 100,
                    StoneReward = 100
                },
                
                new Quest("daily_trade", "상인과의 거래", QuestType.Daily, QuestObjective.TradeWithNPC, 1)
                {
                    Description = "NPC 상인과 거래를 완료하세요",
                    GoldReward = 300,
                    ItemRewards = { "trade_voucher" }
                }
            };
            
            // Randomly select daily quests
            var selectedQuests = questPool.OrderBy(x => UnityEngine.Random.value).Take(MAX_DAILY_QUESTS).ToList();
            
            // Scale rewards based on guild level
            foreach (var quest in selectedQuests)
            {
                quest.GoldReward = (int)(quest.GoldReward * (1 + guildLevel * 0.1f));
                quest.ExperienceReward = (int)(quest.ExperienceReward * (1 + guildLevel * 0.1f));
                activeQuests.Add(quest);
            }
        }
        
        void GenerateWeeklyQuests()
        {
            var guildLevel = Core.GameManager.Instance?.GuildManager?.GetGuildData().GuildLevel ?? 1;
            
            var weeklyQuests = new List<Quest>
            {
                new Quest("weekly_battles", "전투 마스터", QuestType.Weekly, QuestObjective.WinBattles, 20)
                {
                    Description = "전투에서 20회 승리하세요",
                    GoldReward = 5000,
                    ReputationReward = 100,
                    ItemRewards = { "epic_equipment_box" }
                },
                
                new Quest("weekly_recruit", "길드 확장", QuestType.Weekly, QuestObjective.RecruitAdventurers, 5)
                {
                    Description = "모험가를 5명 영입하세요",
                    GoldReward = 3000,
                    ItemRewards = { "rare_recruit_ticket" }
                },
                
                new Quest("weekly_upgrade", "발전하는 길드", QuestType.Weekly, QuestObjective.UpgradeBuildings, 3)
                {
                    Description = "건물을 3회 업그레이드하세요",
                    GoldReward = 4000,
                    WoodReward = 500,
                    StoneReward = 500
                }
            };
            
            // Add all weekly quests
            foreach (var quest in weeklyQuests)
            {
                quest.GoldReward = (int)(quest.GoldReward * (1 + guildLevel * 0.1f));
                activeQuests.Add(quest);
            }
        }
        
        void CheckLoginBonus()
        {
            DateTime now = DateTime.Now;
            
            if (lastLoginDate.Date < now.Date)
            {
                // Check consecutive days
                if ((now.Date - lastLoginDate.Date).TotalDays == 1)
                {
                    consecutiveLoginDays++;
                }
                else if ((now.Date - lastLoginDate.Date).TotalDays > 1)
                {
                    consecutiveLoginDays = 1;
                }
                
                lastLoginDate = now;
                currentLoginCycle = ((consecutiveLoginDays - 1) % 28) + 1;
                
                SaveProgress();
            }
        }
        
        public void ClaimLoginBonus()
        {
            if (currentLoginCycle <= 0 || currentLoginCycle > monthlyLoginRewards.Count) return;
            
            var bonus = monthlyLoginRewards[currentLoginCycle - 1];
            if (bonus.IsClaimed) return;
            
            // Apply rewards
            var resourceManager = Core.GameManager.Instance?.ResourceManager;
            if (resourceManager != null)
            {
                resourceManager.AddGold(bonus.Gold);
                
                if (bonus.Gems > 0)
                {
                    // TODO: Add gem system
                }
                
                if (!string.IsNullOrEmpty(bonus.ItemId))
                {
                    // TODO: Add to inventory
                }
            }
            
            bonus.IsClaimed = true;
            OnLoginBonusClaimed?.Invoke(currentLoginCycle);
            
            SaveProgress();
        }
        
        public void UpdateQuestProgress(QuestObjective objective, int amount = 1)
        {
            var relevantQuests = activeQuests.Where(q => q.Objective == objective && !q.IsCompleted).ToList();
            
            foreach (var quest in relevantQuests)
            {
                quest.UpdateProgress(amount);
                
                if (quest.IsCompleted)
                {
                    OnQuestCompleted?.Invoke(quest);
                }
            }
            
            // Track for achievements
            if (!objectiveTracking.ContainsKey(objective))
                objectiveTracking[objective] = 0;
            objectiveTracking[objective] += amount;
            
            SaveProgress();
        }
        
        public bool ClaimQuestReward(string questId)
        {
            var quest = activeQuests.FirstOrDefault(q => q.QuestId == questId);
            if (quest == null || !quest.IsCompleted || quest.IsClaimed) return false;
            
            // Apply rewards
            var gameManager = Core.GameManager.Instance;
            if (gameManager != null)
            {
                var resourceManager = gameManager.ResourceManager;
                resourceManager.AddGold(quest.GoldReward);
                resourceManager.AddWood(quest.WoodReward);
                resourceManager.AddStone(quest.StoneReward);
                resourceManager.AddManaStone(quest.ManaStoneReward);
                
                if (quest.ReputationReward > 0)
                {
                    gameManager.GuildManager.AddReputation(quest.ReputationReward);
                }
                
                // TODO: Apply experience and item rewards
            }
            
            quest.IsClaimed = true;
            completedQuests.Add(quest);
            
            // Add season pass experience
            AddSeasonPassExperience(quest.Type == QuestType.Daily ? 10 : 50);
            
            OnQuestClaimed?.Invoke(quest);
            
            // Remove if it's a daily/weekly quest
            if (quest.Type == QuestType.Daily || quest.Type == QuestType.Weekly)
            {
                activeQuests.Remove(quest);
            }
            
            SaveProgress();
            return true;
        }
        
        // Event handlers
        void OnBattleEnd(bool victory)
        {
            UpdateQuestProgress(QuestObjective.CompleteBattles, 1);
            if (victory)
            {
                UpdateQuestProgress(QuestObjective.WinBattles, 1);
            }
        }
        
        void OnAdventurerRecruited(Unit adventurer)
        {
            UpdateQuestProgress(QuestObjective.RecruitAdventurers, 1);
        }
        
        void OnBuildingUpgraded(GuildManager.Building building)
        {
            UpdateQuestProgress(QuestObjective.UpgradeBuildings, 1);
        }
        
        // Season Pass
        [System.Serializable]
        public class SeasonPassReward
        {
            public int Level { get; set; }
            public string FreeReward { get; set; }
            public string PremiumReward { get; set; }
            public bool FreeClaimed { get; set; }
            public bool PremiumClaimed { get; set; }
        }
        
        void InitializeSeasonPass()
        {
            seasonPassRewards = new List<SeasonPassReward>();
            
            // Generate 100 levels of rewards
            for (int level = 1; level <= 100; level++)
            {
                var reward = new SeasonPassReward
                {
                    Level = level,
                    FreeReward = GetFreeReward(level),
                    PremiumReward = GetPremiumReward(level)
                };
                
                seasonPassRewards.Add(reward);
            }
        }
        
        string GetFreeReward(int level)
        {
            if (level % 10 == 0) return "epic_reward_box";
            if (level % 5 == 0) return "rare_reward_box";
            return "common_reward_box";
        }
        
        string GetPremiumReward(int level)
        {
            if (level % 10 == 0) return "legendary_reward_box";
            if (level % 5 == 0) return "epic_reward_box";
            return "rare_reward_box";
        }
        
        public void AddSeasonPassExperience(int amount)
        {
            seasonPassExperience += amount;
            
            int requiredExp = GetRequiredExperienceForLevel(seasonPassLevel + 1);
            while (seasonPassExperience >= requiredExp && seasonPassLevel < 100)
            {
                seasonPassExperience -= requiredExp;
                seasonPassLevel++;
                OnSeasonPassLevelUp?.Invoke(seasonPassLevel);
                requiredExp = GetRequiredExperienceForLevel(seasonPassLevel + 1);
            }
            
            SaveProgress();
        }
        
        int GetRequiredExperienceForLevel(int level)
        {
            return 100 * level; // Simple linear progression
        }
        
        public void UpgradeToPremiuPass()
        {
            isPremiumPass = true;
            SaveProgress();
        }
        
        // Save/Load
        void SaveProgress()
        {
            PlayerPrefs.SetInt("ConsecutiveLoginDays", consecutiveLoginDays);
            PlayerPrefs.SetString("LastLoginDate", lastLoginDate.ToString());
            PlayerPrefs.SetInt("CurrentLoginCycle", currentLoginCycle);
            PlayerPrefs.SetInt("SeasonPassLevel", seasonPassLevel);
            PlayerPrefs.SetInt("SeasonPassExp", seasonPassExperience);
            PlayerPrefs.SetInt("IsPremiumPass", isPremiumPass ? 1 : 0);
            
            // Save quest progress
            string questData = JsonUtility.ToJson(new Serializable<List<Quest>>(activeQuests));
            PlayerPrefs.SetString("ActiveQuests", questData);
            
            PlayerPrefs.Save();
        }
        
        void LoadProgress()
        {
            consecutiveLoginDays = PlayerPrefs.GetInt("ConsecutiveLoginDays", 0);
            
            string lastLoginString = PlayerPrefs.GetString("LastLoginDate", "");
            if (!string.IsNullOrEmpty(lastLoginString))
            {
                DateTime.TryParse(lastLoginString, out lastLoginDate);
            }
            else
            {
                lastLoginDate = DateTime.Now.AddDays(-1);
            }
            
            currentLoginCycle = PlayerPrefs.GetInt("CurrentLoginCycle", 0);
            seasonPassLevel = PlayerPrefs.GetInt("SeasonPassLevel", 1);
            seasonPassExperience = PlayerPrefs.GetInt("SeasonPassExp", 0);
            isPremiumPass = PlayerPrefs.GetInt("IsPremiumPass", 0) == 1;
            
            // Load quest progress
            string questData = PlayerPrefs.GetString("ActiveQuests", "");
            if (!string.IsNullOrEmpty(questData))
            {
                try
                {
                    var loaded = JsonUtility.FromJson<Serializable<List<Quest>>>(questData);
                    if (loaded != null && loaded.target != null)
                    {
                        activeQuests = loaded.target;
                    }
                }
                catch { }
            }
            
            InitializeSeasonPass();
        }
        
        DateTime GetLastDailyReset()
        {
            string resetString = PlayerPrefs.GetString("LastDailyReset", "");
            if (DateTime.TryParse(resetString, out DateTime resetTime))
            {
                return resetTime;
            }
            return DateTime.MinValue;
        }
        
        DateTime GetLastWeeklyReset()
        {
            string resetString = PlayerPrefs.GetString("LastWeeklyReset", "");
            if (DateTime.TryParse(resetString, out DateTime resetTime))
            {
                return resetTime;
            }
            return DateTime.MinValue;
        }
        
        // Getters
        public List<Quest> GetActiveQuests()
        {
            return activeQuests.Where(q => !q.IsExpired()).ToList();
        }
        
        public List<Quest> GetDailyQuests()
        {
            return activeQuests.Where(q => q.Type == QuestType.Daily && !q.IsExpired()).ToList();
        }
        
        public List<Quest> GetWeeklyQuests()
        {
            return activeQuests.Where(q => q.Type == QuestType.Weekly && !q.IsExpired()).ToList();
        }
        
        public int GetConsecutiveLoginDays()
        {
            return consecutiveLoginDays;
        }
        
        public LoginBonus GetTodayLoginBonus()
        {
            if (currentLoginCycle > 0 && currentLoginCycle <= monthlyLoginRewards.Count)
            {
                return monthlyLoginRewards[currentLoginCycle - 1];
            }
            return null;
        }
        
        public List<LoginBonus> GetMonthlyLoginRewards()
        {
            return monthlyLoginRewards;
        }
        
        public int GetSeasonPassLevel()
        {
            return seasonPassLevel;
        }
        
        public float GetSeasonPassProgress()
        {
            int required = GetRequiredExperienceForLevel(seasonPassLevel + 1);
            return required > 0 ? (float)seasonPassExperience / required : 0f;
        }
        
        public List<SeasonPassReward> GetSeasonPassRewards()
        {
            return seasonPassRewards;
        }
        
        public bool IsPremiumPass()
        {
            return isPremiumPass;
        }

        // 완료된 일일 퀘스트 목록 반환
        public List<Quest> GetCompletedDailyQuests()
        {
            return activeQuests.Where(q => q.Type == QuestType.Daily && q.IsCompleted && !q.IsClaimed).ToList();
        }

        // 모든 일일 퀘스트 자동 완료
        public void CompleteAllDailyQuests()
        {
            var dailyQuests = GetDailyQuests();
            foreach (var quest in dailyQuests)
            {
                if (!quest.IsCompleted)
                {
                    quest.CurrentProgress = quest.TargetAmount;
                    quest.IsCompleted = true;
                    OnQuestCompleted?.Invoke(quest);
                }
            }
        }
        
        // Utility class for serialization
        [System.Serializable]
        public class Serializable<T>
        {
            public T target;
            public Serializable(T target)
            {
                this.target = target;
            }
        }
    }
}