using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuildMaster.Systems
{
    public class AutoBattleSystem : MonoBehaviour
    {
        private static AutoBattleSystem _instance;
        public static AutoBattleSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AutoBattleSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AutoBattleSystem");
                        _instance = go.AddComponent<AutoBattleSystem>();
                    }
                }
                return _instance;
            }
        }
        
        // 자동 전투 설정
        [System.Serializable]
        public class AutoBattleSettings
        {
            public bool enabled = true;
            public bool autoTargeting = true;
            public bool autoSkillUse = true;
            public bool prioritizeWeakEnemies = true;
            public bool focusFire = true;
            public float skillUseThreshold = 0.8f; // MP 80% 이상일 때 스킬 사용
        }
        
        // 자동 소탕 정보
        [System.Serializable]
        public class AutoSweepInfo
        {
            public string dungeonId;
            public int sweepCount;
            public int ticketsRequired;
            public float timePerSweep;
            public List<RewardInfo> expectedRewards;
        }
        
        [System.Serializable]
        public class RewardInfo
        {
            public string itemId;
            public string itemName;
            public int minQuantity;
            public int maxQuantity;
            public float dropRate;
        }
        
        private AutoBattleSettings battleSettings;
        private Dictionary<string, AutoSweepInfo> sweepableContent;
        private List<string> autoSweepQueue;
        private bool isAutoSweeping;
        
        // 이벤트
        public event Action<bool> OnAutoBattleToggled;
        public event Action<string, int> OnAutoSweepStarted;
        public event Action<string, List<RewardInfo>> OnAutoSweepCompleted;
        public event Action<float> OnAutoSweepProgress;
        
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
            battleSettings = new AutoBattleSettings();
            sweepableContent = new Dictionary<string, AutoSweepInfo>();
            autoSweepQueue = new List<string>();
            
            // 소탕 가능 콘텐츠 등록
            RegisterSweepableContent();
        }
        
        void RegisterSweepableContent()
        {
            // 일일 던전들
            RegisterDailyDungeon("daily_gold", "골드 던전", 5);
            RegisterDailyDungeon("daily_exp", "경험치 던전", 5);
            RegisterDailyDungeon("daily_material", "재료 던전", 5);
            
            // 스토리 던전들 (클리어한 던전만 소탕 가능)
            for (int i = 1; i <= 10; i++)
            {
                RegisterStoryDungeon($"story_{i}", $"스토리 {i}장", 3);
            }
        }
        
        void RegisterDailyDungeon(string id, string name, int ticketCost)
        {
            var sweepInfo = new AutoSweepInfo
            {
                dungeonId = id,
                sweepCount = 0,
                ticketsRequired = ticketCost,
                timePerSweep = 0f, // 즉시 완료
                expectedRewards = new List<RewardInfo>()
            };
            
            // 보상 설정
            if (id.Contains("gold"))
            {
                sweepInfo.expectedRewards.Add(new RewardInfo
                {
                    itemId = "gold",
                    itemName = "골드",
                    minQuantity = 1000,
                    maxQuantity = 2000,
                    dropRate = 1f
                });
            }
            else if (id.Contains("exp"))
            {
                sweepInfo.expectedRewards.Add(new RewardInfo
                {
                    itemId = "exp_potion",
                    itemName = "경험치 물약",
                    minQuantity = 3,
                    maxQuantity = 5,
                    dropRate = 1f
                });
            }
            
            sweepableContent[id] = sweepInfo;
        }
        
        void RegisterStoryDungeon(string id, string name, int ticketCost)
        {
            var sweepInfo = new AutoSweepInfo
            {
                dungeonId = id,
                sweepCount = 0,
                ticketsRequired = ticketCost,
                timePerSweep = 0f,
                expectedRewards = new List<RewardInfo>
                {
                    new RewardInfo
                    {
                        itemId = "gold",
                        itemName = "골드",
                        minQuantity = 500,
                        maxQuantity = 1000,
                        dropRate = 1f
                    },
                    new RewardInfo
                    {
                        itemId = "equipment",
                        itemName = "장비",
                        minQuantity = 1,
                        maxQuantity = 1,
                        dropRate = 0.3f
                    }
                }
            };
            
            sweepableContent[id] = sweepInfo;
        }
        
        // 자동 전투 토글
        public void ToggleAutoBattle()
        {
            battleSettings.enabled = !battleSettings.enabled;
            OnAutoBattleToggled?.Invoke(battleSettings.enabled);
        }
        
        // 자동 전투 설정
        public void SetAutoBattleEnabled(bool enabled)
        {
            battleSettings.enabled = enabled;
            OnAutoBattleToggled?.Invoke(enabled);
        }
        
        // 자동 전투 설정 가져오기
        public AutoBattleSettings GetBattleSettings()
        {
            return battleSettings;
        }
        
        // 자동 소탕 시작
        public bool StartAutoSweep(string dungeonId, int count)
        {
            if (!CanAutoSweep(dungeonId))
                return false;
            
            if (!sweepableContent.ContainsKey(dungeonId))
                return false;
            
            var sweepInfo = sweepableContent[dungeonId];
            int totalTickets = sweepInfo.ticketsRequired * count;
            
            // 티켓 확인
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return false;
            
            // TODO: 실제 티켓 확인 로직
            // if (!HasEnoughTickets(totalTickets)) return false;
            
            // 소탕 시작
            StartCoroutine(ProcessAutoSweep(dungeonId, count));
            OnAutoSweepStarted?.Invoke(dungeonId, count);
            
            return true;
        }
        
        // 자동 소탕 가능 여부
        public bool CanAutoSweep(string dungeonId)
        {
            // TODO: DungeonManager 구현 필요
            /*
            var dungeonManager = FindObjectOfType<GuildMaster.Exploration.DungeonManager>();
            if (dungeonManager == null) return false;
            */
            
            // 3성 클리어 확인 (구현에 따라 수정 필요)
            return true; // 임시로 true 반환
        }
        
        // 자동 소탕 처리
        IEnumerator ProcessAutoSweep(string dungeonId, int count)
        {
            isAutoSweeping = true;
            var sweepInfo = sweepableContent[dungeonId];
            
            for (int i = 0; i < count; i++)
            {
                float progress = (float)(i + 1) / count;
                OnAutoSweepProgress?.Invoke(progress);
                
                // 즉시 완료 또는 대기
                if (sweepInfo.timePerSweep > 0)
                {
                    yield return new WaitForSeconds(sweepInfo.timePerSweep);
                }
                
                // 보상 생성
                List<RewardInfo> actualRewards = GenerateRewards(sweepInfo.expectedRewards);
                ApplyRewards(actualRewards);
                
                OnAutoSweepCompleted?.Invoke(dungeonId, actualRewards);
            }
            
            isAutoSweeping = false;
        }
        
        // 보상 생성
        List<RewardInfo> GenerateRewards(List<RewardInfo> expectedRewards)
        {
            List<RewardInfo> actualRewards = new List<RewardInfo>();
            
            foreach (var expected in expectedRewards)
            {
                if (UnityEngine.Random.value <= expected.dropRate)
                {
                    var reward = new RewardInfo
                    {
                        itemId = expected.itemId,
                        itemName = expected.itemName,
                        minQuantity = UnityEngine.Random.Range(expected.minQuantity, expected.maxQuantity + 1),
                        maxQuantity = expected.maxQuantity,
                        dropRate = expected.dropRate
                    };
                    actualRewards.Add(reward);
                }
            }
            
            return actualRewards;
        }
        
        // 보상 적용
        void ApplyRewards(List<RewardInfo> rewards)
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return;
            
            foreach (var reward in rewards)
            {
                if (reward.itemId == "gold")
                {
                    // TODO: Gold 처리 구현 필요
                    Debug.Log($"Reward: {reward.minQuantity} gold");
                }
                // TODO: 다른 보상 타입 처리
            }
        }
        
        // 일괄 소탕
        public void SweepAllDailies()
        {
            List<string> dailyDungeons = new List<string> { "daily_gold", "daily_exp", "daily_material" };
            
            foreach (string dungeonId in dailyDungeons)
            {
                if (CanAutoSweep(dungeonId))
                {
                    StartAutoSweep(dungeonId, 1);
                }
            }
        }
        
        // 자동 전투 중인지 확인
        public bool IsAutoBattleEnabled()
        {
            return battleSettings.enabled;
        }
        
        // 자동 소탕 중인지 확인
        public bool IsAutoSweeping()
        {
            return isAutoSweeping;
        }
        
        // 소탕 가능한 던전 목록
        public List<string> GetSweepableDungeons()
        {
            List<string> sweepable = new List<string>();
            
            foreach (var dungeon in sweepableContent.Keys)
            {
                if (CanAutoSweep(dungeon))
                {
                    sweepable.Add(dungeon);
                }
            }
            
            return sweepable;
        }
    }
}