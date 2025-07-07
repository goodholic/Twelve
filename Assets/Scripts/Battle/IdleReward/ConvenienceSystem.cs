using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unit = GuildMaster.Battle.UnitStatus;

namespace GuildMaster.Systems
{
    public class ConvenienceSystem : MonoBehaviour
    {
        private static ConvenienceSystem _instance;
        public static ConvenienceSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ConvenienceSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ConvenienceSystem");
                        _instance = go.AddComponent<ConvenienceSystem>();
                    }
                }
                return _instance;
            }
        }
        
        // 일괄 수령 가능한 보상 타입
        public enum RewardType
        {
            Quest,
            Achievement,
            Mail,
            Battle,
            Research,
            Territory,
            SeasonPass,
            All
        }
        
        // 보상 정보
        [System.Serializable]
        public class PendingReward
        {
            public string id;
            public RewardType type;
            public string description;
            public Dictionary<string, int> rewards;
            public DateTime claimableTime;
            public bool isClaimable;
            
            public PendingReward()
            {
                rewards = new Dictionary<string, int>();
                claimableTime = DateTime.Now;
                isClaimable = true;
            }
        }
        
        // 빠른 강화 설정
        [System.Serializable]
        public class QuickEnhanceSettings
        {
            public bool autoSelectMaterials = true;
            public bool preserveHighRarity = true;
            public bool useOnlyDuplicates = false;
            public int targetLevel = -1; // -1은 최대 레벨까지
        }
        
        private Dictionary<string, PendingReward> pendingRewards;
        private QuickEnhanceSettings enhanceSettings;
        
        // 이벤트
        public event Action<List<PendingReward>> OnRewardsCollected;
        public event Action<int> OnQuickEnhanceCompleted;
        public event Action<string> OnBulkActionCompleted;
        
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
            pendingRewards = new Dictionary<string, PendingReward>();
            enhanceSettings = new QuickEnhanceSettings();
            
            // 주기적으로 보상 확인
            StartCoroutine(CheckPendingRewards());
        }
        
        // 일괄 수령
        public List<PendingReward> CollectAllRewards(RewardType type = RewardType.All)
        {
            List<PendingReward> collectedRewards = new List<PendingReward>();
            List<string> toRemove = new List<string>();
            
            foreach (var kvp in pendingRewards)
            {
                var reward = kvp.Value;
                
                if (type == RewardType.All || reward.type == type)
                {
                    if (reward.isClaimable && DateTime.Now >= reward.claimableTime)
                    {
                        // 보상 지급
                        ApplyReward(reward);
                        collectedRewards.Add(reward);
                        toRemove.Add(kvp.Key);
                    }
                }
            }
            
            // 수령한 보상 제거
            foreach (string id in toRemove)
            {
                pendingRewards.Remove(id);
            }
            
            if (collectedRewards.Count > 0)
            {
                OnRewardsCollected?.Invoke(collectedRewards);
                Debug.Log($"Collected {collectedRewards.Count} rewards!");
            }
            
            return collectedRewards;
        }
        
        // 보상 적용
        void ApplyReward(PendingReward reward)
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return;
            
            foreach (var item in reward.rewards)
            {
                switch (item.Key)
                {
                    case "gold":
                        // TODO: Gold 처리 구현 필요
                        Debug.Log($"Claiming gold: {item.Value}");
                        break;
                    case "wood":
                        // TODO: Wood 처리 구현 필요
                        Debug.Log($"Claiming wood: {item.Value}");
                        break;
                    case "stone":
                        // TODO: Stone 처리 구현 필요
                        Debug.Log($"Claiming stone: {item.Value}");
                        break;
                    case "manastone":
                        // TODO: Manastone 처리 구현 필요
                        Debug.Log($"Claiming manastone: {item.Value}");
                        break;
                    case "reputation":
                        // TODO: Reputation 처리 구현 필요
                        Debug.Log($"Claiming reputation: {item.Value}");
                        break;
                    case "exp":
                        // TODO: 경험치 처리
                        break;
                    default:
                        Debug.Log($"Received {item.Value}x {item.Key}");
                        break;
                }
            }
        }
        
        // 보상 추가
        public void AddPendingReward(string id, RewardType type, string description, Dictionary<string, int> rewards)
        {
            if (!pendingRewards.ContainsKey(id))
            {
                var reward = new PendingReward
                {
                    id = id,
                    type = type,
                    description = description,
                    rewards = rewards
                };
                
                pendingRewards[id] = reward;
            }
        }
        
        // 빠른 강화
        public bool QuickEnhance(Unit unit, int targetLevel = -1)
        {
            if (unit == null) return false;
            
            int finalTargetLevel = targetLevel > 0 ? targetLevel : 100; // 최대 레벨
            int enhanceCount = 0;
            
            // 자동으로 재료 선택하여 강화
            while (unit.level < finalTargetLevel)
            {
                // 강화 재료 자동 선택 (구현에 따라 수정 필요)
                if (!PerformEnhancement(unit))
                    break;
                    
                enhanceCount++;
            }
            
            if (enhanceCount > 0)
            {
                OnQuickEnhanceCompleted?.Invoke(enhanceCount);
                return true;
            }
            
            return false;
        }
        
        bool PerformEnhancement(Unit unit)
        {
            // 경험치 추가 (임시 구현)
            int expNeeded = unit.experienceToNextLevel - unit.experience;
            unit.AddExperience(expNeeded);
            return true;
        }
        
        // 일괄 판매
        public int BulkSell(Func<Unit, bool> filter)
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return 0;
            
            var guildManager = gameManager.GuildManager;
            if (guildManager == null) return 0;
            
            var unitsToSell = guildManager.GetAdventurers().Where(filter).ToList();
            int sellCount = 0;
            int totalGold = 0;
            
            foreach (var unit in unitsToSell)
            {
                // 판매 가격 계산
                int sellPrice = CalculateSellPrice(unit);
                totalGold += sellPrice;
                
                // 유닛 제거
                guildManager.RemoveAdventurer(unit);
                sellCount++;
            }
            
            if (totalGold > 0)
            {
                // TODO: Gold 처리 구현 필요
                Debug.Log($"Selling units for {totalGold} gold");
            }
            
            if (sellCount > 0)
            {
                OnBulkActionCompleted?.Invoke($"Sold {sellCount} units for {totalGold} gold");
            }
            
            return sellCount;
        }
        
        int CalculateSellPrice(Unit unit)
        {
            int basePrice = 100;
            basePrice += unit.level * 50;
            basePrice *= (int)unit.rarity + 1;
            return basePrice;
        }
        
        // 추천 편성
        public List<Battle.Squad> GetRecommendedFormation(string contentType = "general")
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager?.GuildManager == null) return new List<Battle.Squad>();
            
            var allUnits = gameManager.GuildManager.GetAdventurers();
            List<Battle.Squad> recommendedSquads = new List<Battle.Squad>();
            
            // 전투력 기준 정렬
            var sortedUnits = allUnits.OrderByDescending(u => u.GetCombatPower()).ToList();
            
            // 4개 부대 구성
            for (int squadIndex = 0; squadIndex < 4; squadIndex++)
            {
                var squad = new Battle.Squad($"추천 부대 {squadIndex + 1}", squadIndex, true);
                
                // 균형잡힌 구성: 탱커 2, 딜러 4, 힐러 1, 서포트 2
                var composition = GetIdealComposition(contentType);
                
                foreach (var roleRequirement in composition)
                {
                    var unit = FindBestUnitForRole(sortedUnits, roleRequirement.Key, roleRequirement.Value);
                    if (unit != null)
                    {
                        squad.AddUnit(unit);
                        sortedUnits.Remove(unit);
                    }
                }
                
                recommendedSquads.Add(squad);
            }
            
            return recommendedSquads;
        }
        
        Dictionary<Battle.JobClass, int> GetIdealComposition(string contentType)
        {
            var composition = new Dictionary<Battle.JobClass, int>();
            
            switch (contentType)
            {
                case "boss":
                    // 보스전: 탱커 1, 딜러 5, 힐러 2, 서포트 1
                    composition[Battle.JobClass.Knight] = 1;
                    composition[Battle.JobClass.Warrior] = 2;
                    composition[Battle.JobClass.Ranger] = 2;
                    composition[Battle.JobClass.Assassin] = 1;
                    composition[Battle.JobClass.Priest] = 2;
                    composition[Battle.JobClass.Sage] = 1;
                    break;
                    
                case "pvp":
                    // PvP: 탱커 2, 딜러 4, 힐러 1, 서포트 2
                    composition[Battle.JobClass.Knight] = 2;
                    composition[Battle.JobClass.Warrior] = 1;
                    composition[Battle.JobClass.Mage] = 2;
                    composition[Battle.JobClass.Assassin] = 1;
                    composition[Battle.JobClass.Priest] = 1;
                    composition[Battle.JobClass.Ranger] = 1;
                    composition[Battle.JobClass.Sage] = 1;
                    break;
                    
                default: // general
                    // 일반: 균형잡힌 구성
                    composition[Battle.JobClass.Knight] = 2;
                    composition[Battle.JobClass.Warrior] = 2;
                    composition[Battle.JobClass.Mage] = 1;
                    composition[Battle.JobClass.Priest] = 1;
                    composition[Battle.JobClass.Ranger] = 1;
                    composition[Battle.JobClass.Assassin] = 1;
                    composition[Battle.JobClass.Sage] = 1;
                    break;
            }
            
            return composition;
        }
        
        Unit FindBestUnitForRole(List<Unit> availableUnits, Battle.JobClass jobClass, int count)
        {
            return availableUnits.FirstOrDefault(u => u.jobClass == jobClass);
        }
        
        // 보상 확인 코루틴
        IEnumerator CheckPendingRewards()
        {
            while (true)
            {
                yield return new WaitForSeconds(60f); // 1분마다 확인
                
                CheckQuestRewards();
                CheckAchievementRewards();
                CheckBattleRewards();
            }
        }
        
        void CheckQuestRewards()
        {
            // 일일 퀘스트 보상 확인
            var dailyContent = DailyContentManager.Instance;
            if (dailyContent != null)
            {
                var completedQuests = dailyContent.GetCompletedDailyQuests();
                foreach (var quest in completedQuests)
                {
                    if (!quest.isRewarded)
                    {
                        var rewards = new Dictionary<string, int>
                        {
                            { "gold", quest.goldReward },
                            { "exp", quest.expReward }
                        };
                        
                        AddPendingReward($"quest_{quest.questId}", RewardType.Quest, quest.questName, rewards);
                    }
                }
            }
        }
        
        void CheckAchievementRewards()
        {
            // 업적 보상 확인 (구현 필요)
        }
        
        void CheckBattleRewards()
        {
            // 전투 보상 확인 (구현 필요)
        }
        
        // 대기 중인 보상 개수
        public int GetPendingRewardCount(RewardType type = RewardType.All)
        {
            if (type == RewardType.All)
                return pendingRewards.Count;
                
            return pendingRewards.Count(r => r.Value.type == type);
        }
        
        // 빠른 일일 루틴
        public void CompleteAllDailies()
        {
            // 일일 퀘스트 자동 완료
            var dailyContent = DailyContentManager.Instance;
            dailyContent?.CompleteAllDailyQuests();
            
            // 일일 던전 소탕
            var autoBattle = AutoBattleSystem.Instance;
            autoBattle?.SweepAllDailies();
            
            // 모든 보상 일괄 수령
            CollectAllRewards();
            
            OnBulkActionCompleted?.Invoke("All daily routines completed!");
        }
    }
}