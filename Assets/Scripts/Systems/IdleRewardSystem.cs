using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Core;
using GuildMaster.Data;
using GuildMaster.Battle;
using ResourceType = GuildMaster.Core.ResourceType;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 방치 보상 시스템
    /// 오프라인 동안 자동으로 축적되는 보상 관리
    /// </summary>
    public class IdleRewardSystem : MonoBehaviour
    {
        private static IdleRewardSystem _instance;
        public static IdleRewardSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<IdleRewardSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("IdleRewardSystem");
                        _instance = go.AddComponent<IdleRewardSystem>();
                    }
                }
                return _instance;
            }
        }

        [Header("Idle Settings")]
        [SerializeField] private float maxIdleHours = 24f; // 최대 방치 시간
        [SerializeField] private float baseIdleMultiplier = 0.5f; // 기본 방치 효율 (50%)
        [SerializeField] private bool autoClaimEnabled = true;
        [SerializeField] private float autoClaimInterval = 300f; // 5분마다 자동 수령

        [Header("Reward Rates")]
        [SerializeField] private float goldPerMinute = 10f;
        [SerializeField] private float expPerMinute = 5f;
        [SerializeField] private float itemDropChance = 0.01f; // 분당 1% 확률

        // 방치 데이터
        private DateTime lastClaimTime;
        private DateTime lastActiveTime;
        private IdleRewardData accumulatedRewards;
        private float currentIdleMultiplier;
        private List<IdleBuff> activeIdleBuffs;

        // 진행 상황
        private Dictionary<string, IdleProgress> idleProgress;
        private List<IdleActivity> activeActivities;

        // 이벤트
        public event Action<IdleRewardData> OnIdleRewardsClaimed;
        public event Action<float> OnIdleTimeUpdated;
        public event Action<IdleBuff> OnIdleBuffApplied;
        public event Action<IdleActivity> OnActivityCompleted;

        [System.Serializable]
        public class IdleRewardData
        {
            public float idleTime; // 방치 시간 (초)
            public Dictionary<ResourceType, int> resources;
            public int totalExperience;
            public List<string> itemIds;
            public Dictionary<string, int> activityProgress;
            public DateTime collectionTime;

            public IdleRewardData()
            {
                resources = new Dictionary<ResourceType, int>();
                itemIds = new List<string>();
                activityProgress = new Dictionary<string, int>();
                collectionTime = DateTime.Now;
            }
        }

        [System.Serializable]
        public class IdleBuff
        {
            public string buffId;
            public string buffName;
            public IdleBuffType buffType;
            public float multiplier;
            public float duration;
            public DateTime startTime;
            public string source;

            public bool IsExpired => DateTime.Now > startTime.AddSeconds(duration);
            public float RemainingTime => (float)(startTime.AddSeconds(duration) - DateTime.Now).TotalSeconds;
        }

        public enum IdleBuffType
        {
            ResourceMultiplier,     // 자원 획득량 증가
            ExperienceMultiplier,   // 경험치 획득량 증가
            ItemDropRate,          // 아이템 드랍률 증가
            OfflineCapacity,       // 최대 방치 시간 증가
            AutoBattle             // 자동 전투 효율 증가
        }

        [System.Serializable]
        public class IdleActivity
        {
            public string activityId;
            public string activityName;
            public ActivityType type;
            public int progress;
            public int maxProgress;
            public DateTime startTime;
            public float progressPerMinute;
            public IdleRewardData rewards;

            public bool IsCompleted => progress >= maxProgress;
            public float CompletionPercent => (float)progress / maxProgress;
        }

        public enum ActivityType
        {
            AutoExploration,    // 자동 탐험
            ResourceGathering,  // 자원 수집
            Training,          // 훈련
            Research,          // 연구
            Building,          // 건설
            Recruitment        // 모집
        }

        [System.Serializable]
        public class IdleProgress
        {
            public string progressId;
            public float totalIdleTime;
            public int totalResourcesGained;
            public int totalExpGained;
            public int totalItemsFound;
            public DateTime lastUpdateTime;
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
            LoadIdleData();
            CalculateOfflineRewards();
            
            if (autoClaimEnabled)
            {
                StartCoroutine(AutoClaimCoroutine());
            }
            
            StartCoroutine(UpdateIdleProgress());
        }

        void InitializeSystem()
        {
            lastClaimTime = DateTime.Now;
            lastActiveTime = DateTime.Now;
            accumulatedRewards = new IdleRewardData();
            currentIdleMultiplier = baseIdleMultiplier;
            activeIdleBuffs = new List<IdleBuff>();
            idleProgress = new Dictionary<string, IdleProgress>();
            activeActivities = new List<IdleActivity>();
        }

        /// <summary>
        /// 오프라인 보상 계산
        /// </summary>
        void CalculateOfflineRewards()
        {
            DateTime now = DateTime.Now;
            float offlineSeconds = (float)(now - lastActiveTime).TotalSeconds;
            
            if (offlineSeconds > 60f) // 1분 이상 오프라인
            {
                // 최대 방치 시간 제한
                float maxSeconds = maxIdleHours * 3600f;
                offlineSeconds = Mathf.Min(offlineSeconds, maxSeconds);

                // 보상 계산
                var rewards = CalculateIdleRewards(offlineSeconds);
                
                // 보상 축적
                AccumulateRewards(rewards);
                
                Debug.Log($"오프라인 시간: {offlineSeconds / 3600f:F1}시간, 보상 준비됨");
            }
        }

        /// <summary>
        /// 방치 보상 계산
        /// </summary>
        IdleRewardData CalculateIdleRewards(float seconds)
        {
            var rewards = new IdleRewardData
            {
                idleTime = seconds
            };

            // 효율 계산
            float efficiency = CalculateIdleEfficiency();

            // 자원 계산 (ResourceProductionSystem 미구현으로 임시 주석)
            /*
            var resourceProduction = ResourceProductionSystem.Instance?.GetProductionStatistics();
            if (resourceProduction != null)
            {
                foreach (var rate in resourceProduction.CurrentRates)
                {
                    float amount = rate.Value * (seconds / 3600f) * efficiency;
                    rewards.resources[rate.Key] = Mathf.FloorToInt(amount);
                }
            }
            else
            {
                // 기본 자원 생산
                rewards.resources[ResourceType.Gold] = Mathf.FloorToInt(goldPerMinute * (seconds / 60f) * efficiency);
            }
            */
            
            // 임시 기본 자원 생산
            rewards.resources[ResourceType.Gold] = Mathf.FloorToInt(goldPerMinute * (seconds / 60f) * efficiency);

            // 경험치 계산
            rewards.totalExperience = Mathf.FloorToInt(expPerMinute * (seconds / 60f) * efficiency);

            // 아이템 드랍 계산
            int itemRolls = Mathf.FloorToInt(seconds / 60f);
            for (int i = 0; i < itemRolls; i++)
            {
                if (UnityEngine.Random.value < itemDropChance * efficiency)
                {
                    rewards.itemIds.Add(GenerateRandomItem());
                }
            }

            // 활동 진행도 계산
            foreach (var activity in activeActivities)
            {
                if (!activity.IsCompleted)
                {
                    int progress = Mathf.FloorToInt(activity.progressPerMinute * (seconds / 60f) * efficiency);
                    rewards.activityProgress[activity.activityId] = progress;
                }
            }

            return rewards;
        }

        /// <summary>
        /// 방치 효율 계산
        /// </summary>
        float CalculateIdleEfficiency()
        {
            float efficiency = currentIdleMultiplier;

            // 버프 적용
            foreach (var buff in activeIdleBuffs)
            {
                if (!buff.IsExpired && buff.buffType == IdleBuffType.ResourceMultiplier)
                {
                    efficiency *= buff.multiplier;
                }
            }

            // 건물 보너스
            var guildManager = GameManager.Instance?.GuildManager;
            if (guildManager != null)
            {
                // 특정 건물이 있으면 효율 증가
                if (System.Enum.TryParse<GuildMaster.Guild.GuildManager.BuildingType>("Shop", out var buildingType))
                {
                    efficiency *= 1f + (guildManager.GetBuildingLevel(buildingType) * 0.1f);
                }
            }

            return Mathf.Clamp(efficiency, 0.1f, 5f);
        }

        /// <summary>
        /// 보상 축적
        /// </summary>
        void AccumulateRewards(IdleRewardData newRewards)
        {
            accumulatedRewards.idleTime += newRewards.idleTime;
            accumulatedRewards.totalExperience += newRewards.totalExperience;

            foreach (var resource in newRewards.resources)
            {
                if (!accumulatedRewards.resources.ContainsKey(resource.Key))
                    accumulatedRewards.resources[resource.Key] = 0;
                
                accumulatedRewards.resources[resource.Key] += resource.Value;
            }

            accumulatedRewards.itemIds.AddRange(newRewards.itemIds);

            foreach (var progress in newRewards.activityProgress)
            {
                UpdateActivityProgress(progress.Key, progress.Value);
            }
        }

        /// <summary>
        /// 방치 보상 수령
        /// </summary>
        public IdleRewardData ClaimIdleRewards()
        {
            if (!HasIdleRewards())
                return null;

            var claimedRewards = new IdleRewardData
            {
                idleTime = accumulatedRewards.idleTime,
                resources = new Dictionary<ResourceType, int>(accumulatedRewards.resources),
                totalExperience = accumulatedRewards.totalExperience,
                itemIds = new List<string>(accumulatedRewards.itemIds),
                activityProgress = new Dictionary<string, int>(accumulatedRewards.activityProgress)
            };

            // 자원 지급
            var resourceManager = ResourceManager.Instance;
            foreach (var resource in claimedRewards.resources)
            {
                resourceManager?.AddResource(resource.Key, resource.Value, "Idle Reward");
            }

            // 경험치 지급
            if (claimedRewards.totalExperience > 0)
            {
                var guildManager = GameManager.Instance?.GuildManager;
                if (guildManager != null)
                {
                    // 길드 경험치 추가
                    guildManager.AddGuildExperience(claimedRewards.totalExperience);
                }
            }

            // 아이템 지급 (인벤토리 시스템 연동 필요)
            foreach (var itemId in claimedRewards.itemIds)
            {
                Debug.Log($"아이템 획득: {itemId}");
            }

            // 통계 업데이트
            UpdateIdleStatistics(claimedRewards);

            // 초기화
            accumulatedRewards = new IdleRewardData();
            lastClaimTime = DateTime.Now;

            // 이벤트 발생
            OnIdleRewardsClaimed?.Invoke(claimedRewards);

            SaveIdleData();

            return claimedRewards;
        }

        /// <summary>
        /// 자동 수령 코루틴
        /// </summary>
        IEnumerator AutoClaimCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(autoClaimInterval);

                if (HasIdleRewards())
                {
                    ClaimIdleRewards();
                }
            }
        }

        /// <summary>
        /// 방치 진행도 업데이트
        /// </summary>
        IEnumerator UpdateIdleProgress()
        {
            while (true)
            {
                yield return new WaitForSeconds(60f); // 1분마다 업데이트

                float deltaTime = 60f;
                
                // 온라인 중에도 방치 보상 축적 (감소된 효율)
                float onlineEfficiency = 0.3f; // 온라인 중에는 30% 효율
                var rewards = CalculateIdleRewards(deltaTime * onlineEfficiency);
                AccumulateRewards(rewards);

                // 활동 업데이트
                UpdateActiveActivities(deltaTime);

                // 만료된 버프 제거
                activeIdleBuffs.RemoveAll(b => b.IsExpired);

                // 진행 시간 업데이트
                OnIdleTimeUpdated?.Invoke(accumulatedRewards.idleTime);
            }
        }

        /// <summary>
        /// 활동 진행도 업데이트
        /// </summary>
        void UpdateActivityProgress(string activityId, int progress)
        {
            var activity = activeActivities.FirstOrDefault(a => a.activityId == activityId);
            if (activity != null)
            {
                activity.progress = Mathf.Min(activity.progress + progress, activity.maxProgress);
                
                if (activity.IsCompleted)
                {
                    CompleteActivity(activity);
                }
            }
        }

        /// <summary>
        /// 활성 활동 업데이트
        /// </summary>
        void UpdateActiveActivities(float deltaTime)
        {
            foreach (var activity in activeActivities.ToList())
            {
                if (!activity.IsCompleted)
                {
                    float progress = activity.progressPerMinute * (deltaTime / 60f);
                    activity.progress = Mathf.Min(activity.progress + Mathf.FloorToInt(progress), activity.maxProgress);

                    if (activity.IsCompleted)
                    {
                        CompleteActivity(activity);
                    }
                }
            }
        }

        /// <summary>
        /// 활동 완료
        /// </summary>
        void CompleteActivity(IdleActivity activity)
        {
            // 보상 지급
            if (activity.rewards != null)
            {
                AccumulateRewards(activity.rewards);
            }

            OnActivityCompleted?.Invoke(activity);
            
            // 반복 활동인 경우 재시작
            if (IsRepeatableActivity(activity.type))
            {
                activity.progress = 0;
                activity.startTime = DateTime.Now;
            }
            else
            {
                activeActivities.Remove(activity);
            }
        }

        bool IsRepeatableActivity(ActivityType type)
        {
            return type == ActivityType.ResourceGathering || 
                   type == ActivityType.Training ||
                   type == ActivityType.AutoExploration;
        }

        /// <summary>
        /// 방치 버프 추가
        /// </summary>
        public void AddIdleBuff(string name, IdleBuffType type, float multiplier, float duration, string source = "Unknown")
        {
            var buff = new IdleBuff
            {
                buffId = Guid.NewGuid().ToString(),
                buffName = name,
                buffType = type,
                multiplier = multiplier,
                duration = duration,
                startTime = DateTime.Now,
                source = source
            };

            activeIdleBuffs.Add(buff);
            OnIdleBuffApplied?.Invoke(buff);

            // 최대 방치 시간 증가 버프
            if (type == IdleBuffType.OfflineCapacity)
            {
                maxIdleHours *= multiplier;
            }
        }

        /// <summary>
        /// 새 활동 시작
        /// </summary>
        public void StartIdleActivity(ActivityType type, string name, int maxProgress, float progressPerMinute)
        {
            var activity = new IdleActivity
            {
                activityId = Guid.NewGuid().ToString(),
                activityName = name,
                type = type,
                progress = 0,
                maxProgress = maxProgress,
                startTime = DateTime.Now,
                progressPerMinute = progressPerMinute
            };

            // 활동별 보상 설정
            activity.rewards = GenerateActivityRewards(type, maxProgress);

            activeActivities.Add(activity);
        }

        IdleRewardData GenerateActivityRewards(ActivityType type, int maxProgress)
        {
            var rewards = new IdleRewardData();

            switch (type)
            {
                case ActivityType.AutoExploration:
                    rewards.resources[ResourceType.Gold] = maxProgress * 10;
                    rewards.totalExperience = maxProgress * 5;
                    break;
                    
                case ActivityType.ResourceGathering:
                    rewards.resources[ResourceType.Wood] = maxProgress * 5;
                    rewards.resources[ResourceType.Stone] = maxProgress * 3;
                    break;
                    
                case ActivityType.Training:
                    rewards.totalExperience = maxProgress * 20;
                    break;
                    
                case ActivityType.Research:
                    rewards.resources[ResourceType.ManaStone] = maxProgress / 10;
                    break;
            }

            return rewards;
        }

        /// <summary>
        /// 아이템 생성
        /// </summary>
        string GenerateRandomItem()
        {
            // 임시 아이템 ID 생성
            string[] itemTypes = { "weapon", "armor", "accessory", "consumable", "material" };
            string[] rarities = { "common", "uncommon", "rare", "epic" };
            
            string type = itemTypes[UnityEngine.Random.Range(0, itemTypes.Length)];
            string rarity = rarities[UnityEngine.Random.Range(0, rarities.Length)];
            
            return $"{type}_{rarity}_{UnityEngine.Random.Range(1, 100)}";
        }

        /// <summary>
        /// 통계 업데이트
        /// </summary>
        void UpdateIdleStatistics(IdleRewardData rewards)
        {
            string statisticsId = "global";
            
            if (!idleProgress.ContainsKey(statisticsId))
            {
                idleProgress[statisticsId] = new IdleProgress
                {
                    progressId = statisticsId
                };
            }

            var progress = idleProgress[statisticsId];
            progress.totalIdleTime += rewards.idleTime;
            progress.totalExpGained += rewards.totalExperience;
            progress.totalItemsFound += rewards.itemIds.Count;
            progress.totalResourcesGained += rewards.resources.Values.Sum();
            progress.lastUpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 데이터 저장
        /// </summary>
        void SaveIdleData()
        {
            PlayerPrefs.SetString("IdleSystem_LastActiveTime", lastActiveTime.ToBinary().ToString());
            PlayerPrefs.SetString("IdleSystem_LastClaimTime", lastClaimTime.ToBinary().ToString());
            PlayerPrefs.SetFloat("IdleSystem_AccumulatedTime", accumulatedRewards.idleTime);
            
            // 축적된 보상 저장
            foreach (var resource in accumulatedRewards.resources)
            {
                PlayerPrefs.SetInt($"IdleSystem_Resource_{resource.Key}", resource.Value);
            }
            
            PlayerPrefs.SetInt("IdleSystem_AccumulatedExp", accumulatedRewards.totalExperience);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 데이터 로드
        /// </summary>
        void LoadIdleData()
        {
            if (PlayerPrefs.HasKey("IdleSystem_LastActiveTime"))
            {
                lastActiveTime = DateTime.FromBinary(long.Parse(PlayerPrefs.GetString("IdleSystem_LastActiveTime")));
                lastClaimTime = DateTime.FromBinary(long.Parse(PlayerPrefs.GetString("IdleSystem_LastClaimTime")));
                accumulatedRewards.idleTime = PlayerPrefs.GetFloat("IdleSystem_AccumulatedTime");
                
                // 자원 로드
                foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                {
                    string key = $"IdleSystem_Resource_{type}";
                    if (PlayerPrefs.HasKey(key))
                    {
                        accumulatedRewards.resources[type] = PlayerPrefs.GetInt(key);
                    }
                }
                
                accumulatedRewards.totalExperience = PlayerPrefs.GetInt("IdleSystem_AccumulatedExp");
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                lastActiveTime = DateTime.Now;
                SaveIdleData();
            }
            else
            {
                CalculateOfflineRewards();
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                lastActiveTime = DateTime.Now;
                SaveIdleData();
            }
            else
            {
                CalculateOfflineRewards();
            }
        }

        void OnDestroy()
        {
            SaveIdleData();
        }

        // Public 접근자
        public bool HasIdleRewards()
        {
            return accumulatedRewards.idleTime > 0 || 
                   accumulatedRewards.resources.Any(r => r.Value > 0) ||
                   accumulatedRewards.totalExperience > 0 ||
                   accumulatedRewards.itemIds.Count > 0;
        }

        public IdleRewardData GetAccumulatedRewards()
        {
            return accumulatedRewards;
        }

        public float GetIdleTime()
        {
            return accumulatedRewards.idleTime;
        }

        public List<IdleActivity> GetActiveActivities()
        {
            return activeActivities;
        }

        public List<IdleBuff> GetActiveBuffs()
        {
            return activeIdleBuffs.Where(b => !b.IsExpired).ToList();
        }

        public float GetMaxIdleHours()
        {
            return maxIdleHours;
        }

        public void SetAutoClaimEnabled(bool enabled)
        {
            autoClaimEnabled = enabled;
        }

        public IdleProgress GetIdleStatistics()
        {
            return idleProgress.ContainsKey("global") ? idleProgress["global"] : new IdleProgress();
        }
    }
}