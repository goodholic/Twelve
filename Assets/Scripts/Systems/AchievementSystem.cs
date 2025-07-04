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
    /// 업적 시스템 - 포괄적인 업적 및 일일 퀘스트 시스템
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {
        private static AchievementSystem _instance;
        public static AchievementSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AchievementSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AchievementSystem");
                        _instance = go.AddComponent<AchievementSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        [Header("업적 데이터")]
        [SerializeField] private List<Achievement> allAchievements = new List<Achievement>();
        [SerializeField] private List<DailyQuest> dailyQuestTemplates = new List<DailyQuest>();
        
        [Header("일일 퀘스트 설정")]
        [SerializeField] private int dailyQuestCount = 3;
        [SerializeField] private int dailyQuestResetHour = 5; // 오전 5시 리셋
        
        [Header("보상 설정")]
        [SerializeField] private int defaultGoldReward = 100;
        [SerializeField] private int defaultGemReward = 10;
        [SerializeField] private int defaultExpReward = 50;
        
        // 진행 상황 추적
        private Dictionary<string, Achievement> achievementDict;
        private Dictionary<string, int> achievementProgress;
        private HashSet<string> completedAchievements;
        private List<DailyQuest> activeDailyQuests;
        private DateTime lastDailyReset;
        
        // 이벤트
        public event Action<Achievement> OnAchievementUnlocked;
        public event Action<Achievement> OnAchievementProgress;
        public event Action<DailyQuest> OnDailyQuestCompleted;
        public event Action OnDailyQuestsReset;
        public event Action<AchievementReward> OnRewardClaimed;
        
        [System.Serializable]
        public class Achievement
        {
            public string achievementId;
            public string name;
            public string description;
            public AchievementCategory category;
            public AchievementType type;
            public int targetValue;
            public bool isHidden;
            public string prerequisiteId; // 선행 업적
            public AchievementReward reward;
            public Sprite icon;
            
            // 진행 상황
            [NonSerialized] public int currentProgress;
            [NonSerialized] public bool isCompleted;
            [NonSerialized] public bool isRewardClaimed;
            [NonSerialized] public DateTime completionTime;
        }
        
        [System.Serializable]
        public class DailyQuest
        {
            public string questId;
            public string name;
            public string description;
            public DailyQuestType type;
            public int targetValue;
            public AchievementReward reward;
            
            // 진행 상황
            [NonSerialized] public int currentProgress;
            [NonSerialized] public bool isCompleted;
            [NonSerialized] public bool isRewardClaimed;
        }
        
        [System.Serializable]
        public class AchievementReward
        {
            public int gold;
            public int gems;
            public int exp;
            public List<string> items = new List<string>();
            public string specialReward; // 특별 보상 (칭호, 스킨 등)
        }
        
        public enum AchievementCategory
        {
            Battle,         // 전투
            Collection,     // 수집
            Guild,          // 길드
            Story,          // 스토리
            Exploration,    // 탐험
            Social,         // 소셜
            Special         // 특별
        }
        
        public enum AchievementType
        {
            Counter,        // 누적 카운터
            Milestone,      // 특정 수치 달성
            Collection,     // 수집 완료
            FirstTime,      // 최초 달성
            Consecutive     // 연속 달성
        }
        
        public enum DailyQuestType
        {
            Battle,         // 전투 X회
            Victory,        // 승리 X회
            Dungeon,        // 던전 클리어
            Guild,          // 길드 활동
            Resource,       // 자원 수집
            Character,      // 캐릭터 육성
            Gacha          // 가챠
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        
        private void Initialize()
        {
            // 초기화
            achievementDict = new Dictionary<string, Achievement>();
            achievementProgress = new Dictionary<string, int>();
            completedAchievements = new HashSet<string>();
            activeDailyQuests = new List<DailyQuest>();
            
            // 업적 로드
            LoadAchievements();
            
            // 진행 상황 로드
            LoadProgress();
            
            // 일일 퀘스트 체크
            CheckDailyReset();
            
            // 이벤트 구독
            SubscribeToGameEvents();
        }
        
        private void LoadAchievements()
        {
            // 기본 업적 생성
            CreateDefaultAchievements();
            
            // 딕셔너리에 추가
            foreach (var achievement in allAchievements)
            {
                achievementDict[achievement.achievementId] = achievement;
                
                if (!achievementProgress.ContainsKey(achievement.achievementId))
                    achievementProgress[achievement.achievementId] = 0;
            }
        }
        
        private void CreateDefaultAchievements()
        {
            // 전투 업적
            AddAchievement("first_victory", "첫 승리", "첫 전투에서 승리하기", 
                AchievementCategory.Battle, AchievementType.FirstTime, 1,
                new AchievementReward { gold = 500, gems = 50 });
            
            AddAchievement("battle_10", "전투의 시작", "10회 전투 참여", 
                AchievementCategory.Battle, AchievementType.Counter, 10,
                new AchievementReward { gold = 1000, exp = 100 });
            
            AddAchievement("battle_100", "백전노장", "100회 전투 참여", 
                AchievementCategory.Battle, AchievementType.Counter, 100,
                new AchievementReward { gold = 5000, gems = 100, specialReward = "title_veteran" });
            
            AddAchievement("perfect_victory", "완벽한 승리", "피해 없이 전투 승리", 
                AchievementCategory.Battle, AchievementType.FirstTime, 1,
                new AchievementReward { gold = 2000, gems = 50 });
            
            // 수집 업적
            AddAchievement("collect_10", "수집가", "10명의 캐릭터 보유", 
                AchievementCategory.Collection, AchievementType.Milestone, 10,
                new AchievementReward { gold = 2000, gems = 100 });
            
            AddAchievement("collect_all_jobs", "직업 마스터", "모든 직업의 캐릭터 보유", 
                AchievementCategory.Collection, AchievementType.Collection, 7,
                new AchievementReward { gold = 10000, gems = 500, specialReward = "title_jobmaster" });
            
            // 길드 업적
            AddAchievement("guild_level_5", "성장하는 길드", "길드 레벨 5 달성", 
                AchievementCategory.Guild, AchievementType.Milestone, 5,
                new AchievementReward { gold = 3000, exp = 500 });
            
            AddAchievement("guild_level_10", "명문 길드", "길드 레벨 10 달성", 
                AchievementCategory.Guild, AchievementType.Milestone, 10,
                new AchievementReward { gold = 10000, gems = 300 });
            
            AddAchievement("build_all", "건축 대가", "모든 종류의 건물 건설", 
                AchievementCategory.Guild, AchievementType.Collection, 14,
                new AchievementReward { gold = 5000, gems = 200 });
            
            // 탐험 업적
            AddAchievement("dungeon_clear_10", "던전 탐험가", "던전 10회 클리어", 
                AchievementCategory.Exploration, AchievementType.Counter, 10,
                new AchievementReward { gold = 2000, exp = 300 });
            
            AddAchievement("infinite_floor_10", "무한의 도전자", "무한 던전 10층 도달", 
                AchievementCategory.Exploration, AchievementType.Milestone, 10,
                new AchievementReward { gold = 5000, gems = 200 });
            
            // 스토리 업적
            AddAchievement("story_chapter_1", "이야기의 시작", "챕터 1 완료", 
                AchievementCategory.Story, AchievementType.FirstTime, 1,
                new AchievementReward { gold = 1000, gems = 50 });
            
            AddAchievement("all_endings", "운명의 갈래", "모든 엔딩 달성", 
                AchievementCategory.Story, AchievementType.Collection, 6,
                new AchievementReward { gold = 20000, gems = 1000, specialReward = "title_fatereader" },
                true); // 히든 업적
            
            // 특별 업적
            AddAchievement("login_7days", "성실한 길드장", "7일 연속 접속", 
                AchievementCategory.Special, AchievementType.Consecutive, 7,
                new AchievementReward { gold = 3000, gems = 150 });
            
            AddAchievement("gacha_legendary", "행운의 주인공", "전설 캐릭터 획득", 
                AchievementCategory.Special, AchievementType.FirstTime, 1,
                new AchievementReward { gold = 5000, gems = 300 });
        }
        
        private void AddAchievement(string id, string name, string desc, 
            AchievementCategory category, AchievementType type, int target,
            AchievementReward reward, bool hidden = false)
        {
            allAchievements.Add(new Achievement
            {
                achievementId = id,
                name = name,
                description = desc,
                category = category,
                type = type,
                targetValue = target,
                reward = reward,
                isHidden = hidden
            });
        }
        
        private void CreateDefaultDailyQuests()
        {
            dailyQuestTemplates.Clear();
            
            // 전투 퀘스트
            dailyQuestTemplates.Add(new DailyQuest
            {
                questId = "daily_battle_3",
                name = "전투 참여",
                description = "전투 3회 참여",
                type = DailyQuestType.Battle,
                targetValue = 3,
                reward = new AchievementReward { gold = 500, exp = 100 }
            });
            
            dailyQuestTemplates.Add(new DailyQuest
            {
                questId = "daily_victory_2",
                name = "승리의 맛",
                description = "전투에서 2회 승리",
                type = DailyQuestType.Victory,
                targetValue = 2,
                reward = new AchievementReward { gold = 800, gems = 20 }
            });
            
            // 던전 퀘스트
            dailyQuestTemplates.Add(new DailyQuest
            {
                questId = "daily_dungeon_1",
                name = "던전 탐험",
                description = "던전 1회 클리어",
                type = DailyQuestType.Dungeon,
                targetValue = 1,
                reward = new AchievementReward { gold = 1000, exp = 200 }
            });
            
            // 자원 퀘스트
            dailyQuestTemplates.Add(new DailyQuest
            {
                questId = "daily_gold_5000",
                name = "황금 수집가",
                description = "5000 골드 획득",
                type = DailyQuestType.Resource,
                targetValue = 5000,
                reward = new AchievementReward { gold = 1000, gems = 30 }
            });
            
            // 캐릭터 퀘스트
            dailyQuestTemplates.Add(new DailyQuest
            {
                questId = "daily_levelup_1",
                name = "성장의 증거",
                description = "캐릭터 1명 레벨업",
                type = DailyQuestType.Character,
                targetValue = 1,
                reward = new AchievementReward { gold = 600, exp = 150 }
            });
            
            // 가챠 퀘스트
            dailyQuestTemplates.Add(new DailyQuest
            {
                questId = "daily_gacha_1",
                name = "행운을 시험하라",
                description = "가챠 1회 진행",
                type = DailyQuestType.Gacha,
                targetValue = 1,
                reward = new AchievementReward { gold = 300, gems = 10 }
            });
        }
        
        // 일일 퀘스트 관리
        public void RefreshDailyQuests()
        {
            if (dailyQuestTemplates.Count == 0)
                CreateDefaultDailyQuests();
            
            activeDailyQuests.Clear();
            
            // 랜덤하게 일일 퀘스트 선택
            var shuffled = dailyQuestTemplates.OrderBy(x => UnityEngine.Random.value).ToList();
            for (int i = 0; i < Mathf.Min(dailyQuestCount, shuffled.Count); i++)
            {
                var template = shuffled[i];
                var quest = new DailyQuest
                {
                    questId = template.questId,
                    name = template.name,
                    description = template.description,
                    type = template.type,
                    targetValue = template.targetValue,
                    reward = template.reward,
                    currentProgress = 0,
                    isCompleted = false,
                    isRewardClaimed = false
                };
                activeDailyQuests.Add(quest);
            }
            
            lastDailyReset = DateTime.Now;
            SaveDailyQuestState();
            
            OnDailyQuestsReset?.Invoke();
        }
        
        private void CheckDailyReset()
        {
            var now = DateTime.Now;
            var resetTime = now.Date.AddHours(dailyQuestResetHour);
            
            if (now.Hour < dailyQuestResetHour)
                resetTime = resetTime.AddDays(-1);
            
            // 마지막 리셋 시간 로드
            string lastResetStr = PlayerPrefs.GetString("Achievement_LastDailyReset", "");
            if (DateTime.TryParse(lastResetStr, out lastDailyReset))
            {
                if (lastDailyReset < resetTime)
                {
                    RefreshDailyQuests();
                }
                else
                {
                    LoadDailyQuestState();
                }
            }
            else
            {
                RefreshDailyQuests();
            }
        }
        
        // 진행 상황 업데이트
        public void UpdateProgress(string achievementId, int amount = 1)
        {
            if (!achievementDict.ContainsKey(achievementId))
                return;
            
            var achievement = achievementDict[achievementId];
            if (achievement.isCompleted)
                return;
            
            // 선행 업적 체크
            if (!string.IsNullOrEmpty(achievement.prerequisiteId))
            {
                if (!IsAchievementCompleted(achievement.prerequisiteId))
                    return;
            }
            
            // 진행도 업데이트
            achievementProgress[achievementId] += amount;
            achievement.currentProgress = achievementProgress[achievementId];
            
            OnAchievementProgress?.Invoke(achievement);
            
            // 완료 체크
            if (achievement.currentProgress >= achievement.targetValue)
            {
                CompleteAchievement(achievement);
            }
            
            SaveProgress();
        }
        
        public void UpdateDailyProgress(string type, int amount = 1)
        {
            var questType = Enum.Parse<DailyQuestType>(type, true);
            
            foreach (var quest in activeDailyQuests)
            {
                if (quest.type == questType && !quest.isCompleted)
                {
                    quest.currentProgress = Mathf.Min(quest.currentProgress + amount, quest.targetValue);
                    
                    if (quest.currentProgress >= quest.targetValue)
                    {
                        CompleteDailyQuest(quest);
                    }
                }
            }
            
            SaveDailyQuestState();
        }
        
        private void CompleteAchievement(Achievement achievement)
        {
            achievement.isCompleted = true;
            achievement.completionTime = DateTime.Now;
            completedAchievements.Add(achievement.achievementId);
            
            OnAchievementUnlocked?.Invoke(achievement);
            
            // 자동 보상 지급 (설정에 따라)
            if (achievement.reward != null)
            {
                ClaimReward(achievement.achievementId);
            }
        }
        
        private void CompleteDailyQuest(DailyQuest quest)
        {
            quest.isCompleted = true;
            OnDailyQuestCompleted?.Invoke(quest);
            
            // 자동 보상 지급
            if (quest.reward != null)
            {
                ClaimDailyReward(quest.questId);
            }
        }
        
        // 보상 수령
        public void ClaimReward(string achievementId)
        {
            if (!achievementDict.ContainsKey(achievementId))
                return;
            
            var achievement = achievementDict[achievementId];
            if (!achievement.isCompleted || achievement.isRewardClaimed)
                return;
            
            achievement.isRewardClaimed = true;
            ApplyReward(achievement.reward);
            
            SaveProgress();
        }
        
        public void ClaimDailyReward(string questId)
        {
            var quest = activeDailyQuests.Find(q => q.questId == questId);
            if (quest == null || !quest.isCompleted || quest.isRewardClaimed)
                return;
            
            quest.isRewardClaimed = true;
            ApplyReward(quest.reward);
            
            SaveDailyQuestState();
        }
        
        private void ApplyReward(AchievementReward reward)
        {
            if (reward == null) return;
            
            var gameManager = GameManager.Instance;
            if (gameManager == null) return;
            
            // 골드 지급
            if (reward.gold > 0 && gameManager.ResourceManager != null)
            {
                gameManager.ResourceManager.AddGold(reward.gold);
            }
            
            // 보석 지급 (구현 필요)
            if (reward.gems > 0)
            {
                // TODO: 보석 시스템 구현
                Debug.Log($"보석 {reward.gems}개 획득!");
            }
            
            // 경험치 지급 (구현 필요)
            if (reward.exp > 0)
            {
                // TODO: 경험치 시스템 구현
                Debug.Log($"경험치 {reward.exp} 획득!");
            }
            
            // 아이템 지급
            foreach (var itemId in reward.items)
            {
                // TODO: 아이템 시스템 구현
                Debug.Log($"아이템 {itemId} 획득!");
            }
            
            // 특별 보상 (칭호 등)
            if (!string.IsNullOrEmpty(reward.specialReward))
            {
                PlayerPrefs.SetInt($"SpecialReward_{reward.specialReward}", 1);
                Debug.Log($"특별 보상 {reward.specialReward} 획득!");
            }
            
            OnRewardClaimed?.Invoke(reward);
        }
        
        // 이벤트 구독
        private void SubscribeToGameEvents()
        {
            // 전투 이벤트
            if (GameManager.Instance?.BattleManager != null)
            {
                // BattleManager 이벤트 구독
            }
            
            // 길드 이벤트
            if (GameManager.Instance?.GuildManager != null)
            {
                // GuildManager 이벤트 구독
            }
        }
        
        // 업적 조회
        public List<Achievement> GetAchievementsByCategory(AchievementCategory category)
        {
            return allAchievements
                .Where(a => a.category == category && (!a.isHidden || a.isCompleted))
                .OrderBy(a => a.name)
                .ToList();
        }
        
        public List<Achievement> GetCompletedAchievements()
        {
            return allAchievements.Where(a => a.isCompleted).ToList();
        }
        
        public List<DailyQuest> GetActiveDailyQuests()
        {
            return new List<DailyQuest>(activeDailyQuests);
        }
        
        public float GetCategoryProgress(AchievementCategory category)
        {
            var categoryAchievements = allAchievements.Where(a => a.category == category).ToList();
            if (categoryAchievements.Count == 0) return 0;
            
            int completed = categoryAchievements.Count(a => a.isCompleted);
            return (float)completed / categoryAchievements.Count;
        }
        
        public bool IsAchievementCompleted(string achievementId)
        {
            return completedAchievements.Contains(achievementId);
        }
        
        // 특별 업적 체크
        public void CheckAchievements()
        {
            // 수집 업적 체크
            CheckCollectionAchievements();
            
            // 길드 업적 체크
            CheckGuildAchievements();
            
            // 기타 특별 조건 체크
            CheckSpecialAchievements();
        }
        
        private void CheckCollectionAchievements()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null) return;
            
            // 캐릭터 수 체크
            int characterCount = 0;
            HashSet<string> collectedJobs = new HashSet<string>();
            
            foreach (var character in gameManager.currentRegisteredCharacters)
            {
                if (character != null)
                {
                    characterCount++;
                    collectedJobs.Add(character.jobClass.ToString());
                }
            }
            
            UpdateProgress("collect_10", characterCount);
            UpdateProgress("collect_all_jobs", collectedJobs.Count);
        }
        
        private void CheckGuildAchievements()
        {
            var guildManager = GameManager.Instance?.GuildManager;
            if (guildManager == null) return;
            
            // 길드 레벨 체크
            UpdateProgress("guild_level_5", guildManager.guildLevel);
            UpdateProgress("guild_level_10", guildManager.guildLevel);
        }
        
        private void CheckSpecialAchievements()
        {
            // 연속 접속 체크
            CheckConsecutiveLogin();
        }
        
        private void CheckConsecutiveLogin()
        {
            string lastLoginStr = PlayerPrefs.GetString("Achievement_LastLogin", "");
            DateTime lastLogin;
            int consecutiveDays = PlayerPrefs.GetInt("Achievement_ConsecutiveDays", 0);
            
            if (DateTime.TryParse(lastLoginStr, out lastLogin))
            {
                var daysSinceLastLogin = (DateTime.Now.Date - lastLogin.Date).Days;
                
                if (daysSinceLastLogin == 1)
                {
                    consecutiveDays++;
                }
                else if (daysSinceLastLogin > 1)
                {
                    consecutiveDays = 1;
                }
            }
            else
            {
                consecutiveDays = 1;
            }
            
            PlayerPrefs.SetString("Achievement_LastLogin", DateTime.Now.ToString());
            PlayerPrefs.SetInt("Achievement_ConsecutiveDays", consecutiveDays);
            
            UpdateProgress("login_7days", consecutiveDays);
        }
        
        // 저장/로드
        private void SaveProgress()
        {
            foreach (var kvp in achievementProgress)
            {
                PlayerPrefs.SetInt($"Achievement_Progress_{kvp.Key}", kvp.Value);
            }
            
            foreach (var id in completedAchievements)
            {
                PlayerPrefs.SetInt($"Achievement_Completed_{id}", 1);
            }
            
            PlayerPrefs.Save();
        }
        
        private void LoadProgress()
        {
            foreach (var achievement in allAchievements)
            {
                string id = achievement.achievementId;
                
                // 진행도 로드
                achievement.currentProgress = PlayerPrefs.GetInt($"Achievement_Progress_{id}", 0);
                achievementProgress[id] = achievement.currentProgress;
                
                // 완료 상태 로드
                if (PlayerPrefs.GetInt($"Achievement_Completed_{id}", 0) == 1)
                {
                    achievement.isCompleted = true;
                    completedAchievements.Add(id);
                }
                
                // 보상 수령 상태 로드
                achievement.isRewardClaimed = PlayerPrefs.GetInt($"Achievement_Reward_{id}", 0) == 1;
            }
        }
        
        private void SaveDailyQuestState()
        {
            PlayerPrefs.SetString("Achievement_LastDailyReset", lastDailyReset.ToString());
            
            for (int i = 0; i < activeDailyQuests.Count; i++)
            {
                var quest = activeDailyQuests[i];
                PlayerPrefs.SetString($"DailyQuest_{i}_Id", quest.questId);
                PlayerPrefs.SetInt($"DailyQuest_{i}_Progress", quest.currentProgress);
                PlayerPrefs.SetInt($"DailyQuest_{i}_Completed", quest.isCompleted ? 1 : 0);
                PlayerPrefs.SetInt($"DailyQuest_{i}_Claimed", quest.isRewardClaimed ? 1 : 0);
            }
            
            PlayerPrefs.SetInt("DailyQuest_Count", activeDailyQuests.Count);
            PlayerPrefs.Save();
        }
        
        private void LoadDailyQuestState()
        {
            activeDailyQuests.Clear();
            
            int count = PlayerPrefs.GetInt("DailyQuest_Count", 0);
            for (int i = 0; i < count; i++)
            {
                string questId = PlayerPrefs.GetString($"DailyQuest_{i}_Id", "");
                var template = dailyQuestTemplates.Find(t => t.questId == questId);
                
                if (template != null)
                {
                    var quest = new DailyQuest
                    {
                        questId = template.questId,
                        name = template.name,
                        description = template.description,
                        type = template.type,
                        targetValue = template.targetValue,
                        reward = template.reward,
                        currentProgress = PlayerPrefs.GetInt($"DailyQuest_{i}_Progress", 0),
                        isCompleted = PlayerPrefs.GetInt($"DailyQuest_{i}_Completed", 0) == 1,
                        isRewardClaimed = PlayerPrefs.GetInt($"DailyQuest_{i}_Claimed", 0) == 1
                    };
                    activeDailyQuests.Add(quest);
                }
            }
        }
        
        // 업적 언락
        public void UnlockAchievement(string achievementId)
        {
            UpdateProgress(achievementId, int.MaxValue);
        }
    }
}