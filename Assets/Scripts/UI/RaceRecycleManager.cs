using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Core;
using GuildMaster.Battle;

namespace GuildMaster.UI
{
    /// <summary>
    /// 캐릭터 재활용 시스템 관리자
    /// </summary>
    public class RaceRecycleManager : MonoBehaviour
    {
        private static RaceRecycleManager _instance;
        public static RaceRecycleManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<RaceRecycleManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("RaceRecycleManager");
                        _instance = go.AddComponent<RaceRecycleManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Recycle Settings")]
        public int minRecycleLevel = 5;
        public float recycleEfficiency = 0.7f; // 70% of resources returned
        public int maxRecyclePerDay = 10;
        public float cooldownTime = 300f; // 5 minutes

        [Header("Recycle Rewards")]
        public Dictionary<string, RecycleReward> recycleRewards = new Dictionary<string, RecycleReward>();
        public List<RecycleBonus> levelBonuses = new List<RecycleBonus>();

        // Current state
        private Dictionary<string, DateTime> lastRecycleTime = new Dictionary<string, DateTime>();
        private Dictionary<string, int> dailyRecycleCount = new Dictionary<string, int>();
        private DateTime lastResetDate = DateTime.Now.Date;

        [System.Serializable]
        public class RecycleReward
        {
            public ResourceType resourceType;
            public int baseAmount;
            public float levelMultiplier;
            public float rarityMultiplier;
            public bool isGuaranteed;
            public float dropChance;
        }

        [System.Serializable]
        public class RecycleBonus
        {
            public int requiredLevel;
            public Dictionary<ResourceType, int> bonusResources;
            public float experienceBonus;
            public string description;

            public RecycleBonus()
            {
                bonusResources = new Dictionary<ResourceType, int>();
            }
        }

        [System.Serializable]
        public class RecycleResult
        {
            public bool success;
            public Dictionary<ResourceType, int> rewards;
            public int experienceGained;
            public string message;
            public List<string> bonusItems;

            public RecycleResult()
            {
                rewards = new Dictionary<ResourceType, int>();
                bonusItems = new List<string>();
            }
        }

        // Events
        public event Action<GuildMaster.Battle.Unit, RecycleResult> OnCharacterRecycled;
        public event Action<string> OnRecycleFailed;
        public event Action<int> OnDailyLimitReached;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeRecycleSystem();
        }

        void InitializeRecycleSystem()
        {
            // 기본 재활용 보상 설정
            SetupDefaultRecycleRewards();
            
            // 레벨 보너스 설정
            SetupLevelBonuses();
        }

        void SetupDefaultRecycleRewards()
        {
            // 기본 자원 보상
            recycleRewards["experience"] = new RecycleReward
            {
                resourceType = ResourceType.Experience,
                baseAmount = 50,
                levelMultiplier = 1.5f,
                rarityMultiplier = 2f,
                isGuaranteed = true,
                dropChance = 1f
            };

            recycleRewards["gold"] = new RecycleReward
            {
                resourceType = ResourceType.Gold,
                baseAmount = 100,
                levelMultiplier = 2f,
                rarityMultiplier = 3f,
                isGuaranteed = true,
                dropChance = 1f
            };

            recycleRewards["soul_essence"] = new RecycleReward
            {
                resourceType = ResourceType.Mana, // Soul essence를 Mana로 대체
                baseAmount = 10,
                levelMultiplier = 1f,
                rarityMultiplier = 5f,
                isGuaranteed = false,
                dropChance = 0.3f
            };
        }

        void SetupLevelBonuses()
        {
            // 레벨 10 보너스
            var bonus10 = new RecycleBonus
            {
                requiredLevel = 10,
                experienceBonus = 0.2f,
                description = "10레벨 보너스: 20% 추가 경험치"
            };
            bonus10.bonusResources[ResourceType.Gold] = 50;
            levelBonuses.Add(bonus10);

            // 레벨 25 보너스
            var bonus25 = new RecycleBonus
            {
                requiredLevel = 25,
                experienceBonus = 0.5f,
                description = "25레벨 보너스: 50% 추가 경험치"
            };
            bonus25.bonusResources[ResourceType.Gold] = 200;
            bonus25.bonusResources[ResourceType.Mana] = 25;
            levelBonuses.Add(bonus25);
        }

        public bool CanRecycleCharacter(GuildMaster.Battle.Unit character)
        {
            // 기본 조건 확인
            if (character == null)
                return false;

            if (character.level < minRecycleLevel)
                return false;

            // 일일 제한 확인
            CheckDailyReset();
            string characterId = character.unitId;
            
            if (dailyRecycleCount.ContainsKey(characterId) && 
                dailyRecycleCount[characterId] >= maxRecyclePerDay)
                return false;

            // 쿨다운 확인
            if (lastRecycleTime.ContainsKey(characterId))
            {
                TimeSpan timeSinceLastRecycle = DateTime.Now - lastRecycleTime[characterId];
                if (timeSinceLastRecycle.TotalSeconds < cooldownTime)
                    return false;
            }

            return true;
        }

        public RecycleResult RecycleCharacter(GuildMaster.Battle.Unit character)
        {
            var result = new RecycleResult();

            if (!CanRecycleCharacter(character))
            {
                result.success = false;
                result.message = "캐릭터를 재활용할 수 없습니다.";
                OnRecycleFailed?.Invoke(result.message);
                return result;
            }

            // 재활용 처리
            result.success = true;
            result.rewards = CalculateRecycleRewards(character);
            result.experienceGained = CalculateExperienceReward(character);

            // 보너스 아이템 계산
            result.bonusItems = CalculateBonusItems(character);

            // 자원 지급
            if (ResourceManager.Instance != null)
            {
                foreach (var reward in result.rewards)
                {
                    ResourceManager.Instance.AddResource(reward.Key, reward.Value);
                }
            }

            // 상태 업데이트
            UpdateRecycleState(character.unitId);

            // 캐릭터 제거
            RemoveCharacterFromGame(character);

            result.message = $"{character.unitName}을(를) 성공적으로 재활용했습니다!";
            OnCharacterRecycled?.Invoke(character, result);

            return result;
        }

        Dictionary<ResourceType, int> CalculateRecycleRewards(GuildMaster.Battle.Unit character)
        {
            var rewards = new Dictionary<ResourceType, int>();

            foreach (var rewardConfig in recycleRewards.Values)
            {
                if (!rewardConfig.isGuaranteed && UnityEngine.Random.value > rewardConfig.dropChance)
                    continue;

                int amount = Mathf.RoundToInt(
                    rewardConfig.baseAmount *
                    Mathf.Pow(character.level, rewardConfig.levelMultiplier / 10f) *
                    GetRarityMultiplier(character) *
                    recycleEfficiency
                );

                if (rewards.ContainsKey(rewardConfig.resourceType))
                    rewards[rewardConfig.resourceType] += amount;
                else
                    rewards[rewardConfig.resourceType] = amount;
            }

            // 레벨 보너스 적용
            ApplyLevelBonuses(character.level, rewards);

            return rewards;
        }

        int CalculateExperienceReward(GuildMaster.Battle.Unit character)
        {
            int baseExp = character.level * 10;
            float multiplier = 1f;

            // 레벨 보너스 적용
            foreach (var bonus in levelBonuses)
            {
                if (character.level >= bonus.requiredLevel)
                {
                    multiplier += bonus.experienceBonus;
                }
            }

            return Mathf.RoundToInt(baseExp * multiplier * recycleEfficiency);
        }

        List<string> CalculateBonusItems(GuildMaster.Battle.Unit character)
        {
            var bonusItems = new List<string>();

            // 레벨에 따른 특별 아이템
            if (character.level >= 50)
            {
                bonusItems.Add("고급 영혼 결정");
            }
            else if (character.level >= 25)
            {
                bonusItems.Add("영혼 조각");
            }

            // 희귀도에 따른 보너스
            if (character.rarity >= GuildMaster.Data.Rarity.Epic)
            {
                bonusItems.Add("전설의 유산");
            }

            return bonusItems;
        }

        float GetRarityMultiplier(GuildMaster.Battle.Unit character)
        {
            return character.rarity switch
            {
                GuildMaster.Data.Rarity.Common => 1f,
                GuildMaster.Data.Rarity.Uncommon => 1.5f,
                GuildMaster.Data.Rarity.Rare => 2f,
                GuildMaster.Data.Rarity.Epic => 3f,
                GuildMaster.Data.Rarity.Legendary => 5f,
                _ => 1f
            };
        }

        void ApplyLevelBonuses(int characterLevel, Dictionary<ResourceType, int> rewards)
        {
            foreach (var bonus in levelBonuses)
            {
                if (characterLevel >= bonus.requiredLevel)
                {
                    foreach (var bonusResource in bonus.bonusResources)
                    {
                        if (rewards.ContainsKey(bonusResource.Key))
                            rewards[bonusResource.Key] += bonusResource.Value;
                        else
                            rewards[bonusResource.Key] = bonusResource.Value;
                    }
                }
            }
        }

        void UpdateRecycleState(string characterId)
        {
            lastRecycleTime[characterId] = DateTime.Now;
            
            if (dailyRecycleCount.ContainsKey(characterId))
                dailyRecycleCount[characterId]++;
            else
                dailyRecycleCount[characterId] = 1;
        }

        void RemoveCharacterFromGame(GuildMaster.Battle.Unit character)
        {
            // 게임에서 캐릭터 제거
            if (character.gameObject != null)
            {
                Destroy(character.gameObject);
            }
        }

        void CheckDailyReset()
        {
            if (DateTime.Now.Date > lastResetDate)
            {
                dailyRecycleCount.Clear();
                lastResetDate = DateTime.Now.Date;
            }
        }

        public int GetRemainingRecycles(string characterId)
        {
            CheckDailyReset();
            
            if (dailyRecycleCount.ContainsKey(characterId))
                return Mathf.Max(0, maxRecyclePerDay - dailyRecycleCount[characterId]);
            
            return maxRecyclePerDay;
        }

        public float GetRecycleCooldown(string characterId)
        {
            if (!lastRecycleTime.ContainsKey(characterId))
                return 0f;

            TimeSpan timeSinceLastRecycle = DateTime.Now - lastRecycleTime[characterId];
            return Mathf.Max(0f, cooldownTime - (float)timeSinceLastRecycle.TotalSeconds);
        }

        public List<GuildMaster.Battle.Unit> GetRecyclableCharacters()
        {
            var guildManager = GuildMaster.Guild.GuildManager.Instance;
            if (guildManager == null) return new List<GuildMaster.Battle.Unit>();
            
            return guildManager.guildMembers.Where(unit => CanRecycleCharacter(unit)).ToList();
        }
    }
} 