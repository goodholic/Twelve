using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Battle;
using GuildMaster.Core;
using GuildMaster.Data;

namespace GuildMaster.Systems
{
    public class AchievementSystem : MonoBehaviour
    {
        private static AchievementSystem _instance;
        public static AchievementSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AchievementSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AchievementSystem");
                        _instance = go.AddComponent<AchievementSystem>();
                    }
                }
                return _instance;
            }
        }
        
        [System.Serializable]
        public class Achievement
        {
            public string achievementId;
            public string name;
            public string description;
            public AchievementCategory category;
            public AchievementTier tier;
            public int targetValue;
            public int currentValue;
            public bool isCompleted;
            public bool isClaimed;
            public DateTime completedDate;
            public AchievementReward reward;
            public string iconPath;
            public bool isHidden; // 숨겨진 업적
        }
        
        [System.Serializable]
        public class AchievementReward
        {
            public int gold;
            public int gems;
            public int reputation;
            public string specialReward; // 칭호, 스킨 등
            public string itemId;
            public int itemCount;
        }
        
        public enum AchievementCategory
        {
            Combat,         // 전투
            Collection,     // 수집
            Building,       // 건설
            Exploration,    // 탐험
            Social,         // 사교
            Economy,        // 경제
            Special         // 특별
        }
        
        public enum AchievementTier
        {
            Bronze,
            Silver,
            Gold,
            Platinum,
            Diamond
        }
        
        [System.Serializable]
        public class AchievementProgress
        {
            public string trackingId;
            public string achievementId;
            public int progress;
            public DateTime lastUpdated;
        }
        
        // 일일 퀘스트
        [System.Serializable]
        public class DailyQuest
        {
            public string questId;
            public string questName;
            public string description;
            public QuestType type;
            public int targetValue;
            public int currentValue;
            public bool isCompleted;
            public bool isClaimed;
            
            // 보상
            public int rewardGold;
            public int rewardGem;
            public int rewardExp;
            
            // 리셋 시간
            public DateTime resetTime;
        }
        
        // 퀘스트 타입
        public enum QuestType
        {
            BattleVictory,      // 전투 승리
            RecruitAdventurer,  // 모험가 영입
            BuildConstruction,  // 건물 건설
            ResourceCollection, // 자원 수집
            SkillUpgrade,       // 스킬 강화
            EquipmentEnhance,   // 장비 강화
            DungeonClear,       // 던전 클리어
            GachaUse           // 가챠 사용
        }
        
        private Dictionary<string, Achievement> achievements;
        private Dictionary<string, AchievementProgress> progressTrackers;
        private List<Achievement> completedAchievements;
        private Dictionary<string, DailyQuest> dailyQuests;
        
        // Statistics tracking
        private Dictionary<string, long> statistics;
        
        // Events
        public event Action<Achievement> OnAchievementCompleted;
        public event Action<Achievement> OnAchievementClaimed;
        public event Action<string, int> OnProgressUpdated;
        public event Action<DailyQuest> OnDailyQuestCompleted;
        public event Action<DailyQuest> OnDailyQuestClaimed;
        public event Action OnDailyQuestsReset;
        
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
            achievements = new Dictionary<string, Achievement>();
            progressTrackers = new Dictionary<string, AchievementProgress>();
            completedAchievements = new List<Achievement>();
            dailyQuests = new Dictionary<string, DailyQuest>();
            statistics = new Dictionary<string, long>();
            
            LoadAchievements();
            LoadDailyQuests();
            LoadProgress();
            
            // 일일 퀘스트 리셋 체크
            StartCoroutine(DailyQuestResetChecker());
        }
        
        void LoadAchievements()
        {
            // 전투 업적
            CreateCombatAchievements();
            
            // 수집 업적
            CreateCollectionAchievements();
            
            // 건설 업적
            CreateBuildingAchievements();
            
            // 탐험 업적
            CreateExplorationAchievements();
            
            // 경제 업적
            CreateEconomyAchievements();
            
            // 특별 업적
            CreateSpecialAchievements();
        }
        
        void CreateCombatAchievements()
        {
            // 첫 승리
            AddAchievement(new Achievement
            {
                achievementId = "combat_first_win",
                name = "첫 승리",
                description = "전투에서 첫 승리를 거두세요",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Bronze,
                targetValue = 1,
                reward = new AchievementReward { gold = 100, reputation = 10 }
            });
            
            // 연속 승리
            AddAchievement(new Achievement
            {
                achievementId = "combat_win_streak_10",
                name = "연승의 기세",
                description = "10연승을 달성하세요",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Silver,
                targetValue = 10,
                reward = new AchievementReward { gold = 500, gems = 50 }
            });
            
            // 완벽한 승리
            AddAchievement(new Achievement
            {
                achievementId = "combat_perfect_victory",
                name = "완벽한 승리",
                description = "아군 사상자 없이 전투에서 승리하세요",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Gold,
                targetValue = 1,
                reward = new AchievementReward { gold = 1000, specialReward = "title_perfect_commander" }
            });
            
            // 대규모 전투
            AddAchievement(new Achievement
            {
                achievementId = "combat_total_battles_1000",
                name = "백전노장",
                description = "총 1000회의 전투를 수행하세요",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Platinum,
                targetValue = 1000,
                reward = new AchievementReward { gold = 5000, reputation = 500, specialReward = "skin_veteran_badge" }
            });
        }
        
        void CreateCollectionAchievements()
        {
            // 모험가 수집
            AddAchievement(new Achievement
            {
                achievementId = "collect_adventurers_10",
                name = "모험가 모집",
                description = "10명의 모험가를 모집하세요",
                category = AchievementCategory.Collection,
                tier = AchievementTier.Bronze,
                targetValue = 10,
                reward = new AchievementReward { gold = 200 }
            });
            
            // 희귀 모험가
            AddAchievement(new Achievement
            {
                achievementId = "collect_rare_adventurer",
                name = "희귀한 만남",
                description = "희귀 등급 이상의 모험가를 획득하세요",
                category = AchievementCategory.Collection,
                tier = AchievementTier.Silver,
                targetValue = 1,
                reward = new AchievementReward { gems = 100 }
            });
            
            // 전설 모험가
            AddAchievement(new Achievement
            {
                achievementId = "collect_legendary_adventurer",
                name = "전설의 시작",
                description = "전설 등급 모험가를 획득하세요",
                category = AchievementCategory.Collection,
                tier = AchievementTier.Diamond,
                targetValue = 1,
                reward = new AchievementReward { gems = 500, specialReward = "title_legendary_master" }
            });
            
            // 모든 직업 수집
            AddAchievement(new Achievement
            {
                achievementId = "collect_all_classes",
                name = "다재다능",
                description = "모든 직업의 모험가를 보유하세요",
                category = AchievementCategory.Collection,
                tier = AchievementTier.Gold,
                targetValue = 7, // 7개 직업
                reward = new AchievementReward { gold = 2000, itemId = "skill_book_rare", itemCount = 1 }
            });
        }
        
        void CreateBuildingAchievements()
        {
            // 첫 건물
            AddAchievement(new Achievement
            {
                achievementId = "build_first_building",
                name = "건설의 시작",
                description = "첫 번째 건물을 건설하세요",
                category = AchievementCategory.Building,
                tier = AchievementTier.Bronze,
                targetValue = 1,
                reward = new AchievementReward { gold = 150 }
            });
            
            // 건물 업그레이드
            AddAchievement(new Achievement
            {
                achievementId = "upgrade_building_max",
                name = "최고의 시설",
                description = "건물 하나를 최대 레벨까지 업그레이드하세요",
                category = AchievementCategory.Building,
                tier = AchievementTier.Gold,
                targetValue = 1,
                reward = new AchievementReward { gold = 3000, reputation = 200 }
            });
            
            // 모든 건물 건설
            AddAchievement(new Achievement
            {
                achievementId = "build_all_types",
                name = "완벽한 길드",
                description = "모든 종류의 건물을 건설하세요",
                category = AchievementCategory.Building,
                tier = AchievementTier.Platinum,
                targetValue = 10, // 10종류 건물
                reward = new AchievementReward { gold = 5000, specialReward = "blueprint_special_building" }
            });
        }
        
        void CreateExplorationAchievements()
        {
            // 첫 던전
            AddAchievement(new Achievement
            {
                achievementId = "explore_first_dungeon",
                name = "던전 탐험가",
                description = "첫 던전을 클리어하세요",
                category = AchievementCategory.Exploration,
                tier = AchievementTier.Bronze,
                targetValue = 1,
                reward = new AchievementReward { gold = 200 }
            });
            
            // 깊은 던전
            AddAchievement(new Achievement
            {
                achievementId = "explore_deep_dungeon_50",
                name = "심연의 탐험가",
                description = "무한 던전 50층에 도달하세요",
                category = AchievementCategory.Exploration,
                tier = AchievementTier.Gold,
                targetValue = 50,
                reward = new AchievementReward { gold = 2500, itemId = "legendary_equipment_box", itemCount = 1 }
            });
            
            // 영토 정복
            AddAchievement(new Achievement
            {
                achievementId = "conquer_territories_10",
                name = "정복자",
                description = "10개의 영토를 점령하세요",
                category = AchievementCategory.Exploration,
                tier = AchievementTier.Platinum,
                targetValue = 10,
                reward = new AchievementReward { gold = 10000, reputation = 1000, specialReward = "title_conqueror" }
            });
        }
        
        void CreateEconomyAchievements()
        {
            // 백만장자
            AddAchievement(new Achievement
            {
                achievementId = "economy_gold_1m",
                name = "백만장자",
                description = "100만 골드를 보유하세요",
                category = AchievementCategory.Economy,
                tier = AchievementTier.Gold,
                targetValue = 1000000,
                reward = new AchievementReward { gems = 200, specialReward = "title_millionaire" }
            });
            
            // 거래 달인
            AddAchievement(new Achievement
            {
                achievementId = "economy_trades_100",
                name = "거래의 달인",
                description = "NPC와 100회 거래하세요",
                category = AchievementCategory.Economy,
                tier = AchievementTier.Silver,
                targetValue = 100,
                reward = new AchievementReward { gold = 1000, reputation = 100 }
            });
        }
        
        void CreateSpecialAchievements()
        {
            // 숨겨진 업적 - 특별한 날짜
            AddAchievement(new Achievement
            {
                achievementId = "special_anniversary",
                name = "기념일",
                description = "특별한 날에 접속하세요",
                category = AchievementCategory.Special,
                tier = AchievementTier.Diamond,
                targetValue = 1,
                reward = new AchievementReward { gems = 1000, specialReward = "skin_anniversary" },
                isHidden = true
            });
            
            // 행운의 가챠
            AddAchievement(new Achievement
            {
                achievementId = "special_lucky_gacha",
                name = "행운의 신",
                description = "한 번의 10연차에서 3개 이상의 전설 등급을 획득하세요",
                category = AchievementCategory.Special,
                tier = AchievementTier.Diamond,
                targetValue = 1,
                reward = new AchievementReward { gems = 500, specialReward = "title_lucky" },
                isHidden = true
            });
        }
        
        void AddAchievement(Achievement achievement)
        {
            achievements[achievement.achievementId] = achievement;
        }
        
        public void UpdateProgress(string achievementId, int progress, bool incremental = true)
        {
            if (!achievements.ContainsKey(achievementId))
            {
                Debug.LogWarning($"Achievement {achievementId} not found!");
                return;
            }
            
            var achievement = achievements[achievementId];
            if (achievement.isCompleted) return;
            
            if (incremental)
            {
                achievement.currentValue += progress;
            }
            else
            {
                achievement.currentValue = progress;
            }
            
            OnProgressUpdated?.Invoke(achievementId, achievement.currentValue);
            
            // 완료 체크
            if (achievement.currentValue >= achievement.targetValue)
            {
                CompleteAchievement(achievement);
            }
            
            SaveProgress();
        }
        
        void CompleteAchievement(Achievement achievement)
        {
            if (achievement.isCompleted) return;
            
            achievement.isCompleted = true;
            achievement.completedDate = DateTime.Now;
            completedAchievements.Add(achievement);
            
            OnAchievementCompleted?.Invoke(achievement);
            
            // 자동 보상 지급 (일부 업적)
            if (achievement.tier == AchievementTier.Bronze)
            {
                ClaimReward(achievement.achievementId);
            }
        }
        
        public bool ClaimReward(string achievementId)
        {
            if (!achievements.ContainsKey(achievementId))
                return false;
                
            var achievement = achievements[achievementId];
            if (!achievement.isCompleted || achievement.isClaimed)
                return false;
            
            achievement.isClaimed = true;
            
            // 보상 지급
            ApplyReward(achievement.reward);
            
            OnAchievementClaimed?.Invoke(achievement);
            SaveProgress();
            
            return true;
        }
        
        void ApplyReward(AchievementReward reward)
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return;
            
            if (reward.gold > 0 && gameManager.ResourceManager != null)
            {
                gameManager.ResourceManager.AddGold(reward.gold);
            }
            
            if (reward.reputation > 0 && gameManager.ResourceManager != null)
            {
                gameManager.ResourceManager.AddReputation(reward.reputation);
            }
            
            // TODO: 젬, 특별 보상, 아이템 처리
            
            if (!string.IsNullOrEmpty(reward.specialReward))
            {
                Debug.Log($"Special reward unlocked: {reward.specialReward}");
            }
        }
        
        // 통계 추적
        public void TrackStatistic(string statName, long value, bool incremental = true)
        {
            if (!statistics.ContainsKey(statName))
            {
                statistics[statName] = 0;
            }
            
            if (incremental)
            {
                statistics[statName] += value;
            }
            else
            {
                statistics[statName] = value;
            }
            
            // 관련 업적 자동 업데이트
            CheckStatisticAchievements(statName, statistics[statName]);
        }
        
        void CheckStatisticAchievements(string statName, long value)
        {
            // 통계 기반 업적 자동 체크
            switch (statName)
            {
                case "total_battles":
                    UpdateProgress("combat_total_battles_1000", (int)value, false);
                    break;
                    
                case "total_gold_earned":
                    UpdateProgress("economy_gold_1m", (int)value, false);
                    break;
                    
                case "adventurers_recruited":
                    UpdateProgress("collect_adventurers_10", (int)value, false);
                    break;
                    
                case "buildings_constructed":
                    UpdateProgress("build_first_building", (int)value, false);
                    break;
            }
        }
        
        // 업적 조회
        public List<Achievement> GetAchievementsByCategory(AchievementCategory category)
        {
            return achievements.Values.Where(a => a.category == category).ToList();
        }
        
        public List<Achievement> GetCompletedAchievements()
        {
            return completedAchievements;
        }
        
        public List<Achievement> GetClaimableAchievements()
        {
            return achievements.Values.Where(a => a.isCompleted && !a.isClaimed).ToList();
        }
        
        public Achievement GetAchievement(string achievementId)
        {
            return achievements.ContainsKey(achievementId) ? achievements[achievementId] : null;
        }
        
        public int GetTotalAchievementPoints()
        {
            int points = 0;
            foreach (var achievement in completedAchievements)
            {
                points += GetAchievementPoints(achievement.tier);
            }
            return points;
        }
        
        int GetAchievementPoints(AchievementTier tier)
        {
            switch (tier)
            {
                case AchievementTier.Bronze: return 10;
                case AchievementTier.Silver: return 25;
                case AchievementTier.Gold: return 50;
                case AchievementTier.Platinum: return 100;
                case AchievementTier.Diamond: return 200;
                default: return 0;
            }
        }
        
        public float GetCompletionPercentage()
        {
            int total = achievements.Count;
            int completed = completedAchievements.Count;
            return total > 0 ? (float)completed / total * 100f : 0f;
        }
        
        void SaveProgress()
        {
            // 업적 진행 상황 저장
            foreach (var achievement in achievements.Values)
            {
                string key = $"Achievement_{achievement.achievementId}";
                PlayerPrefs.SetInt($"{key}_Progress", achievement.currentValue);
                PlayerPrefs.SetInt($"{key}_Completed", achievement.isCompleted ? 1 : 0);
                PlayerPrefs.SetInt($"{key}_Claimed", achievement.isClaimed ? 1 : 0);
                
                if (achievement.isCompleted)
                {
                    PlayerPrefs.SetString($"{key}_Date", achievement.completedDate.ToBinary().ToString());
                }
            }
            
            // 일일 퀘스트 진행도 저장
            foreach (var quest in dailyQuests.Values)
            {
                PlayerPrefs.SetInt($"DailyQuest_{quest.questId}_Progress", quest.currentValue);
                PlayerPrefs.SetInt($"DailyQuest_{quest.questId}_Completed", quest.isCompleted ? 1 : 0);
                PlayerPrefs.SetInt($"DailyQuest_{quest.questId}_Claimed", quest.isClaimed ? 1 : 0);
                PlayerPrefs.SetString($"DailyQuest_{quest.questId}_ResetTime", quest.resetTime.ToBinary().ToString());
            }
            
            // 통계 저장
            foreach (var stat in statistics)
            {
                PlayerPrefs.SetString($"Stat_{stat.Key}", stat.Value.ToString());
            }
            
            PlayerPrefs.Save();
        }
        
        void LoadProgress()
        {
            // 업적 진행 상황 로드
            foreach (var achievement in achievements.Values)
            {
                string key = $"Achievement_{achievement.achievementId}";
                achievement.currentValue = PlayerPrefs.GetInt($"{key}_Progress", 0);
                achievement.isCompleted = PlayerPrefs.GetInt($"{key}_Completed", 0) == 1;
                achievement.isClaimed = PlayerPrefs.GetInt($"{key}_Claimed", 0) == 1;
                
                if (achievement.isCompleted)
                {
                    string dateStr = PlayerPrefs.GetString($"{key}_Date", "");
                    if (!string.IsNullOrEmpty(dateStr) && long.TryParse(dateStr, out long dateBinary))
                    {
                        achievement.completedDate = DateTime.FromBinary(dateBinary);
                    }
                }
                
                if (achievement.isCompleted && !completedAchievements.Contains(achievement))
                {
                    completedAchievements.Add(achievement);
                }
            }
            
            // 일일 퀘스트 진행도는 리셋 시간 체크 후 로드
            var now = DateTime.Now;
            foreach (var quest in dailyQuests.Values)
            {
                string resetTimeStr = PlayerPrefs.GetString($"DailyQuest_{quest.questId}_ResetTime", "");
                if (!string.IsNullOrEmpty(resetTimeStr) && long.TryParse(resetTimeStr, out long resetBinary))
                {
                    var savedResetTime = DateTime.FromBinary(resetBinary);
                    
                    // 리셋 시간이 지나지 않았으면 진행도 로드
                    if (now < savedResetTime)
                    {
                        quest.currentValue = PlayerPrefs.GetInt($"DailyQuest_{quest.questId}_Progress", 0);
                        quest.isCompleted = PlayerPrefs.GetInt($"DailyQuest_{quest.questId}_Completed", 0) == 1;
                        quest.isClaimed = PlayerPrefs.GetInt($"DailyQuest_{quest.questId}_Claimed", 0) == 1;
                        quest.resetTime = savedResetTime;
                    }
                }
            }
            
            // 통계 로드
            // TODO: 통계 로드 구현
        }
        
        // ===== 일일 퀘스트 시스템 =====
        
        void LoadDailyQuests()
        {
            // 일일 퀘스트는 매일 랜덤하게 생성
            GenerateDailyQuests();
        }
        
        void GenerateDailyQuests()
        {
            dailyQuests.Clear();
            var resetTime = GetNextResetTime();
            
            // 전투 퀘스트
            dailyQuests["daily_battle_3"] = new DailyQuest
            {
                questId = "daily_battle_3",
                questName = "일일 전투",
                description = "전투에서 3회 승리",
                type = QuestType.BattleVictory,
                targetValue = 3,
                rewardGold = 500,
                rewardExp = 100,
                resetTime = resetTime
            };
            
            // 모집 퀘스트
            dailyQuests["daily_recruit_1"] = new DailyQuest
            {
                questId = "daily_recruit_1",
                questName = "일일 모집",
                description = "모험가 1명 영입",
                type = QuestType.RecruitAdventurer,
                targetValue = 1,
                rewardGold = 300,
                rewardGem = 10,
                resetTime = resetTime
            };
            
            // 자원 수집 퀘스트
            dailyQuests["daily_resource_1000"] = new DailyQuest
            {
                questId = "daily_resource_1000",
                questName = "일일 수집",
                description = "자원 1000개 수집",
                type = QuestType.ResourceCollection,
                targetValue = 1000,
                rewardGold = 400,
                rewardExp = 80,
                resetTime = resetTime
            };
            
            // 가챠 퀘스트
            dailyQuests["daily_gacha_1"] = new DailyQuest
            {
                questId = "daily_gacha_1",
                questName = "일일 가챠",
                description = "가챠 1회 사용",
                type = QuestType.GachaUse,
                targetValue = 1,
                rewardGem = 20,
                resetTime = resetTime
            };
            
            // 랜덤 추가 퀘스트
            var randomQuests = new List<DailyQuest>
            {
                new DailyQuest
                {
                    questId = "daily_skill_upgrade",
                    questName = "스킬 강화",
                    description = "스킬 1개 강화",
                    type = QuestType.SkillUpgrade,
                    targetValue = 1,
                    rewardGold = 600,
                    rewardGem = 15,
                    resetTime = resetTime
                },
                new DailyQuest
                {
                    questId = "daily_dungeon_1",
                    questName = "던전 탐험",
                    description = "던전 1회 클리어",
                    type = QuestType.DungeonClear,
                    targetValue = 1,
                    rewardGold = 800,
                    rewardExp = 150,
                    resetTime = resetTime
                }
            };
            
            // 랜덤하게 1개 추가
            if (randomQuests.Count > 0)
            {
                var randomQuest = randomQuests[UnityEngine.Random.Range(0, randomQuests.Count)];
                dailyQuests[randomQuest.questId] = randomQuest;
            }
        }
        
        // 일일 퀘스트 진행도 업데이트
        public void UpdateDailyQuestProgress(QuestType type, int amount = 1)
        {
            foreach (var quest in dailyQuests.Values)
            {
                if (quest.type == type && !quest.isCompleted)
                {
                    quest.currentValue += amount;
                    
                    if (quest.currentValue >= quest.targetValue)
                    {
                        CompleteDailyQuest(quest);
                    }
                }
            }
            
            SaveProgress();
        }
        
        // 일일 퀘스트 완료
        void CompleteDailyQuest(DailyQuest quest)
        {
            quest.isCompleted = true;
            OnDailyQuestCompleted?.Invoke(quest);
            
            // 알림 표시
            Debug.Log($"일일 퀘스트 완료: {quest.questName}");
            
            // 효과음 재생
            SoundSystem.Instance?.PlaySound("quest_complete");
        }
        
        // 일일 퀘스트 보상 수령
        public void ClaimDailyQuest(string questId)
        {
            if (!dailyQuests.ContainsKey(questId))
                return;
                
            var quest = dailyQuests[questId];
            if (!quest.isCompleted || quest.isClaimed)
                return;
            
            quest.isClaimed = true;
            
            // 보상 지급
            GiveDailyQuestRewards(quest);
            
            OnDailyQuestClaimed?.Invoke(quest);
            SaveProgress();
        }
        
        void GiveDailyQuestRewards(DailyQuest quest)
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return;
            
            if (quest.rewardGold > 0)
                gameManager.ResourceManager.AddGold(quest.rewardGold);
                
            if (quest.rewardGem > 0)
            {
                // TODO: 젬 시스템 구현
            }
            
            if (quest.rewardExp > 0)
            {
                // TODO: 경험치 시스템 구현
            }
        }
        
        // 일일 퀘스트 리셋
        IEnumerator DailyQuestResetChecker()
        {
            while (true)
            {
                yield return new WaitForSeconds(60f); // 1분마다 체크
                
                var now = DateTime.Now;
                var shouldReset = false;
                
                foreach (var quest in dailyQuests.Values)
                {
                    if (now >= quest.resetTime)
                    {
                        shouldReset = true;
                        break;
                    }
                }
                
                if (shouldReset)
                {
                    ResetDailyQuests();
                }
            }
        }
        
        void ResetDailyQuests()
        {
            GenerateDailyQuests();
            OnDailyQuestsReset?.Invoke();
            SaveProgress();
        }
        
        DateTime GetNextResetTime()
        {
            var now = DateTime.Now;
            var tomorrow = now.AddDays(1);
            return new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 5, 0, 0); // 다음날 오전 5시
        }
        
        // 일일 퀘스트 조회
        public List<DailyQuest> GetDailyQuests()
        {
            return dailyQuests.Values.ToList();
        }
        
        public List<DailyQuest> GetCompletedDailyQuests()
        {
            return dailyQuests.Values.Where(q => q.isCompleted).ToList();
        }
        
        // 특정 조건 체크 메서드들
        public void CheckCombatAchievements(bool isPerfectVictory)
        {
            UpdateProgress("combat_first_win", 1);
            UpdateProgress("combat_total_battles_1000", 1);
            
            if (isPerfectVictory)
            {
                UpdateProgress("combat_perfect_victory", 1);
            }
            
            UpdateDailyQuestProgress(QuestType.BattleVictory, 1);
        }
        
        public void CheckBuildingAchievements(string buildingType)
        {
            UpdateProgress("build_first_building", 1);
            UpdateDailyQuestProgress(QuestType.BuildConstruction, 1);
        }
        
        public void CheckRecruitmentAchievements(Unit unit)
        {
            UpdateProgress("collect_adventurers_10", 1);
            
            if (unit.rarity == Rarity.Legendary)
            {
                UpdateProgress("collect_legendary_adventurer", 1);
            }
            
            UpdateDailyQuestProgress(QuestType.RecruitAdventurer, 1);
        }
        
        public void CheckResourceAchievements(string resourceType, int amount)
        {
            if (resourceType == "gold")
            {
                TrackStatistic("total_gold_earned", amount);
            }
            
            UpdateDailyQuestProgress(QuestType.ResourceCollection, amount);
        }
        
        public void CheckDungeonAchievements(bool isBoss)
        {
            UpdateProgress("explore_first_dungeon", 1);
            UpdateDailyQuestProgress(QuestType.DungeonClear, 1);
        }
        
        public void CheckGachaAchievements(GachaSystem.GachaResult result)
        {
            TrackStatistic("gacha_uses", 1);
        }
        
        // GameLoopManager에서 사용하는 메서드들 추가
        public void RefreshDailyQuests()
        {
            ResetDailyQuests();
        }
        
        public void CheckAchievements()
        {
            // 모든 업적 체크
            foreach (var achievement in achievements.Values)
            {
                if (!achievement.isCompleted)
                {
                    CheckAchievementCompletion(achievement);
                }
            }
        }
        
        public void UpdateDailyProgress(string progressType, int amount = 1)
        {
            // 일일 퀘스트 진행도 업데이트
            foreach (var quest in dailyQuests.Values)
            {
                if (!quest.isCompleted && quest.type.ToString() == progressType)
                {
                    quest.currentValue += amount;
                    if (quest.currentValue >= quest.targetValue)
                    {
                        CompleteDailyQuest(quest);
                    }
                }
            }
        }
        
        public void UnlockAchievement(string achievementId)
        {
            if (achievements.ContainsKey(achievementId))
            {
                var achievement = achievements[achievementId];
                if (!achievement.isCompleted)
                {
                    achievement.isCompleted = true;
                    achievement.completedDate = DateTime.Now;
                    CompleteAchievement(achievement);
                }
            }
        }
        
        private void CheckAchievementCompletion(Achievement achievement)
        {
            // 통계 기반 업적 완료 체크
            if (statistics.ContainsKey(achievement.achievementId))
            {
                if (statistics[achievement.achievementId] >= achievement.targetValue)
                {
                    CompleteAchievement(achievement);
                }
            }
        }
    }
}