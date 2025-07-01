using UnityEngine;
using System;
using System.Collections; // IEnumerator를 위해 추가
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Core; // ResourceType을 위해 추가

namespace GuildMaster.Systems
{
    public class SeasonPassSystem : MonoBehaviour
    {
        private static SeasonPassSystem _instance;
        public static SeasonPassSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SeasonPassSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SeasonPassSystem");
                        _instance = go.AddComponent<SeasonPassSystem>();
                    }
                }
                return _instance;
            }
        }
        
        // 시즌 정보
        [System.Serializable]
        public class Season
        {
            public int seasonId;
            public string seasonName;
            public string seasonTheme;
            public DateTime startDate;
            public DateTime endDate;
            public bool isActive;
            public int totalLevels = 100;
            
            public TimeSpan TimeRemaining => endDate - DateTime.Now;
            public float Progress => (float)(DateTime.Now - startDate).TotalDays / (float)(endDate - startDate).TotalDays;
            public float DaysRemaining => (float)(endDate - DateTime.Now).TotalDays;
        }
        
        // 시즌 패스 타입
        public enum PassType
        {
            Free,       // 무료 패스
            Premium,    // 프리미엄 패스
            Elite       // 엘리트 패스 (추가 혜택)
        }
        
        // 패스 보상
        [System.Serializable]
        public class PassReward
        {
            public int level;
            public PassType requiredPass;
            public RewardItem reward;
            public bool isClaimed;
            public bool isMilestone; // 주요 보상
        }
        
        // 보상 아이템
        [System.Serializable]
        public class RewardItem
        {
            public enum RewardType
            {
                Gold,
                Resource,
                Equipment,
                Unit,
                Skin,
                Building,
                ResearchBoost,
                ExperienceBoost,
                Premium,
                Special
            }
            
            public RewardType type;
            public string itemId;
            public string itemName;
            public int quantity;
            public ResourceType resourceType; // 자원 타입인 경우
            public int rarity; // 0-5 (일반-신화)
            public Sprite icon;
            public string description;
        }
        
        // 시즌 도전과제
        [System.Serializable]
        public class SeasonChallenge
        {
            public int challengeId;
            public string challengeName;
            public string description;
            public int targetValue;
            public int currentValue;
            public int experienceReward;
            public bool isCompleted;
            public bool isClaimed;
            public ChallengeType type;
            public DateTime expiryDate; // 일일/주간 도전과제용
        }
        
        public enum ChallengeType
        {
            Daily,      // 일일 도전과제
            Weekly,     // 주간 도전과제
            Seasonal    // 시즌 도전과제
        }
        
        // 플레이어 시즌 패스 정보
        [System.Serializable]
        public class PlayerSeasonPass
        {
            public int currentLevel;
            public int currentExperience;
            public int experienceToNextLevel;
            public PassType passType;
            public List<int> claimedRewards;
            public List<int> completedChallenges;
            public int totalSeasonScore;
            
            public PlayerSeasonPass()
            {
                claimedRewards = new List<int>();
                completedChallenges = new List<int>();
            }
        }
        
        // 시즌 상점
        [System.Serializable]
        public class SeasonShopItem
        {
            public int itemId;
            public RewardItem item;
            public int seasonCurrencyCost;
            public int purchaseLimit;
            public int timesPurchased;
            public bool isAvailable = true;
        }
        
        // 현재 시즌
        private Season currentSeason;
        private PlayerSeasonPass playerPass;
        
        // 보상 데이터
        private Dictionary<int, PassReward> seasonRewards;
        private List<SeasonChallenge> activeChallenges;
        private List<SeasonShopItem> seasonShop;
        
        // 시즌 화폐
        private int seasonCurrency = 0;
        
        // 경험치 테이블
        private int[] experienceTable;
        
        // 이벤트
        public event Action<Season> OnSeasonStarted;
        public event Action<Season> OnSeasonEnded;
        public event Action<int> OnLevelUp;
        public event Action<PassReward> OnRewardClaimed;
        public event Action<SeasonChallenge> OnChallengeCompleted;
        public event Action<int> OnExperienceGained;
        public event Action<int> OnSeasonCurrencyChanged;
        
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
            seasonRewards = new Dictionary<int, PassReward>();
            activeChallenges = new List<SeasonChallenge>();
            seasonShop = new List<SeasonShopItem>();
            
            // 경험치 테이블 생성
            CreateExperienceTable();
            
            // 현재 시즌 확인 또는 새 시즌 시작
            CheckCurrentSeason();
            
            // 주기적 업데이트
            StartCoroutine(SeasonUpdater());
        }
        
        void CreateExperienceTable()
        {
            experienceTable = new int[101]; // 0-100 레벨
            experienceTable[0] = 0;
            
            for (int i = 1; i <= 100; i++)
            {
                // 레벨이 올라갈수록 필요 경험치 증가
                experienceTable[i] = 100 + (i - 1) * 50 + (i / 10) * 100;
            }
        }
        
        void CheckCurrentSeason()
        {
            // 저장된 시즌 데이터 확인 (임시로 새 시즌 생성)
            StartNewSeason();
        }
        
        void StartNewSeason()
        {
            // 시즌 생성 (3개월)
            currentSeason = new Season
            {
                seasonId = DateTime.Now.Year * 10 + (DateTime.Now.Month - 1) / 3 + 1,
                seasonName = $"시즌 {DateTime.Now.Year}-{(DateTime.Now.Month - 1) / 3 + 1}",
                seasonTheme = GetSeasonTheme(),
                startDate = DateTime.Now.Date,
                endDate = DateTime.Now.Date.AddMonths(3),
                isActive = true
            };
            
            // 플레이어 패스 초기화
            playerPass = new PlayerSeasonPass
            {
                currentLevel = 1,
                currentExperience = 0,
                experienceToNextLevel = experienceTable[1],
                passType = PassType.Free
            };
            
            // 시즌 보상 생성
            GenerateSeasonRewards();
            
            // 시즌 도전과제 생성
            GenerateSeasonChallenges();
            
            // 시즌 상점 생성
            GenerateSeasonShop();
            
            OnSeasonStarted?.Invoke(currentSeason);
        }
        
        string GetSeasonTheme()
        {
            string[] themes = {
                "영웅의 귀환",
                "고대의 비밀",
                "전쟁의 서막",
                "마법의 축제",
                "용의 각성",
                "신들의 시험"
            };
            
            return themes[UnityEngine.Random.Range(0, themes.Length)];
        }
        
        void GenerateSeasonRewards()
        {
            seasonRewards.Clear();
            
            for (int level = 1; level <= 100; level++)
            {
                // 무료 보상 (모든 레벨)
                PassReward freeReward = new PassReward
                {
                    level = level,
                    requiredPass = PassType.Free,
                    reward = GenerateRewardForLevel(level, false),
                    isMilestone = (level % 10 == 0)
                };
                seasonRewards[level * 2 - 1] = freeReward;
                
                // 프리미엄 보상 (모든 레벨)
                PassReward premiumReward = new PassReward
                {
                    level = level,
                    requiredPass = PassType.Premium,
                    reward = GenerateRewardForLevel(level, true),
                    isMilestone = (level % 10 == 0)
                };
                seasonRewards[level * 2] = premiumReward;
                
                // 엘리트 보상 (10레벨마다)
                if (level % 10 == 0)
                {
                    PassReward eliteReward = new PassReward
                    {
                        level = level,
                        requiredPass = PassType.Elite,
                        reward = GenerateEliteReward(level),
                        isMilestone = true
                    };
                    seasonRewards[level * 2 + 1000] = eliteReward;
                }
            }
        }
        
        RewardItem GenerateRewardForLevel(int level, bool isPremium)
        {
            RewardItem reward = new RewardItem();
            
            if (level % 10 == 0)
            {
                // 마일스톤 보상
                if (isPremium)
                {
                    reward.type = RewardItem.RewardType.Unit;
                    reward.itemName = "전설 모험가 소환권";
                    reward.quantity = 1;
                    reward.rarity = 4;
                }
                else
                {
                    reward.type = RewardItem.RewardType.Equipment;
                    reward.itemName = "희귀 장비 상자";
                    reward.quantity = 1;
                    reward.rarity = 2;
                }
            }
            else if (level % 5 == 0)
            {
                // 중간 보상
                if (isPremium)
                {
                    reward.type = RewardItem.RewardType.Resource;
                    reward.resourceType = ResourceType.ManaStone;
                    reward.quantity = 50 + level * 5;
                    reward.itemName = "마나스톤";
                }
                else
                {
                    reward.type = RewardItem.RewardType.Gold;
                    reward.quantity = 1000 + level * 100;
                    reward.itemName = "골드";
                }
            }
            else
            {
                // 일반 보상
                if (isPremium)
                {
                    int rewardType = UnityEngine.Random.Range(0, 4);
                    switch (rewardType)
                    {
                        case 0:
                            reward.type = RewardItem.RewardType.ExperienceBoost;
                            reward.itemName = "경험치 부스터 (1시간)";
                            reward.quantity = 1;
                            break;
                        case 1:
                            reward.type = RewardItem.RewardType.ResearchBoost;
                            reward.itemName = "연구 가속권 (30분)";
                            reward.quantity = 2;
                            break;
                        case 2:
                            reward.type = RewardItem.RewardType.Resource;
                            reward.resourceType = (ResourceType)UnityEngine.Random.Range(0, 3);
                            reward.quantity = 200 + level * 10;
                            reward.itemName = $"{reward.resourceType}";
                            break;
                        default:
                            reward.type = RewardItem.RewardType.Premium;
                            reward.itemName = "시즌 코인";
                            reward.quantity = 10 + level;
                            break;
                    }
                }
                else
                {
                    reward.type = RewardItem.RewardType.Gold;
                    reward.quantity = 200 + level * 20;
                    reward.itemName = "골드";
                }
            }
            
            reward.description = $"레벨 {level} 보상";
            return reward;
        }
        
        RewardItem GenerateEliteReward(int level)
        {
            RewardItem reward = new RewardItem
            {
                type = RewardItem.RewardType.Special,
                itemName = $"엘리트 보상 Lv.{level}",
                quantity = 1,
                rarity = 5,
                description = "엘리트 패스 전용 특별 보상"
            };
            
            switch (level)
            {
                case 10:
                    reward.itemName = "전설 스킨: 황금 기사";
                    reward.type = RewardItem.RewardType.Skin;
                    break;
                case 20:
                    reward.itemName = "건물 스킨: 수정 길드홀";
                    reward.type = RewardItem.RewardType.Building;
                    break;
                case 30:
                    reward.itemName = "신화 모험가 선택권";
                    reward.type = RewardItem.RewardType.Unit;
                    break;
                case 40:
                    reward.itemName = "무한 연구 슬롯 (7일)";
                    reward.type = RewardItem.RewardType.ResearchBoost;
                    break;
                case 50:
                    reward.itemName = "시즌 한정 칭호: " + currentSeason.seasonTheme;
                    reward.type = RewardItem.RewardType.Special;
                    break;
                default:
                    reward.itemName = $"시즌 코인 x{level * 10}";
                    reward.type = RewardItem.RewardType.Premium;
                    reward.quantity = level * 10;
                    break;
            }
            
            return reward;
        }
        
        void GenerateSeasonChallenges()
        {
            activeChallenges.Clear();
            
            // 일일 도전과제 (매일 갱신)
            GenerateDailyChallenges();
            
            // 주간 도전과제 (매주 갱신)
            GenerateWeeklyChallenges();
            
            // 시즌 도전과제 (시즌 내내 유지)
            GenerateSeasonalChallenges();
        }
        
        void GenerateDailyChallenges()
        {
            DateTime tomorrow = DateTime.Now.Date.AddDays(1).AddHours(5); // 다음날 오전 5시
            
            // 전투 도전과제
            activeChallenges.Add(new SeasonChallenge
            {
                challengeId = 1001,
                challengeName = "일일 전투",
                description = "전투에서 3회 승리하기",
                type = ChallengeType.Daily,
                targetValue = 3,
                experienceReward = 50,
                expiryDate = tomorrow
            });
            
            // 자원 도전과제
            activeChallenges.Add(new SeasonChallenge
            {
                challengeId = 1002,
                challengeName = "자원 수집가",
                description = "골드 5000 수집하기",
                type = ChallengeType.Daily,
                targetValue = 5000,
                experienceReward = 50,
                expiryDate = tomorrow
            });
            
            // 건설 도전과제
            activeChallenges.Add(new SeasonChallenge
            {
                challengeId = 1003,
                challengeName = "건설의 달인",
                description = "건물 1개 건설 또는 업그레이드",
                type = ChallengeType.Daily,
                targetValue = 1,
                experienceReward = 50,
                expiryDate = tomorrow
            });
        }
        
        void GenerateWeeklyChallenges()
        {
            DateTime nextMonday = GetNextMonday();
            
            // 주간 전투 도전과제
            activeChallenges.Add(new SeasonChallenge
            {
                challengeId = 2001,
                challengeName = "주간 정복자",
                description = "AI 길드와 10회 대전하기",
                type = ChallengeType.Weekly,
                targetValue = 10,
                experienceReward = 300,
                expiryDate = nextMonday
            });
            
            // 주간 던전 도전과제
            activeChallenges.Add(new SeasonChallenge
            {
                challengeId = 2002,
                challengeName = "던전 탐험가",
                description = "던전 15층 클리어하기",
                type = ChallengeType.Weekly,
                targetValue = 15,
                experienceReward = 300,
                expiryDate = nextMonday
            });
            
            // 주간 성장 도전과제
            activeChallenges.Add(new SeasonChallenge
            {
                challengeId = 2003,
                challengeName = "성장의 주간",
                description = "모험가 5명 레벨업",
                type = ChallengeType.Weekly,
                targetValue = 5,
                experienceReward = 300,
                expiryDate = nextMonday
            });
        }
        
        void GenerateSeasonalChallenges()
        {
            // 시즌 마스터 도전과제
            activeChallenges.Add(new SeasonChallenge
            {
                challengeId = 3001,
                challengeName = "시즌 마스터",
                description = "시즌 패스 레벨 100 달성",
                type = ChallengeType.Seasonal,
                targetValue = 100,
                experienceReward = 5000,
                expiryDate = currentSeason.endDate
            });
            
            // 영토 정복자
            activeChallenges.Add(new SeasonChallenge
            {
                challengeId = 3002,
                challengeName = "영토 정복자",
                description = "영토 10개 점령하기",
                type = ChallengeType.Seasonal,
                targetValue = 10,
                experienceReward = 2000,
                expiryDate = currentSeason.endDate
            });
            
            // 길드 성장
            activeChallenges.Add(new SeasonChallenge
            {
                challengeId = 3003,
                challengeName = "위대한 길드",
                description = "길드 레벨 30 달성",
                type = ChallengeType.Seasonal,
                targetValue = 30,
                experienceReward = 3000,
                expiryDate = currentSeason.endDate
            });
        }
        
        void GenerateSeasonShop()
        {
            seasonShop.Clear();
            
            // 특별 아이템
            seasonShop.Add(new SeasonShopItem
            {
                itemId = 1,
                item = new RewardItem
                {
                    type = RewardItem.RewardType.Unit,
                    itemName = "시즌 한정 영웅",
                    quantity = 1,
                    rarity = 5,
                    description = "이번 시즌에만 구매 가능한 특별한 영웅"
                },
                seasonCurrencyCost = 1000,
                purchaseLimit = 1
            });
            
            // 스킨
            seasonShop.Add(new SeasonShopItem
            {
                itemId = 2,
                item = new RewardItem
                {
                    type = RewardItem.RewardType.Skin,
                    itemName = currentSeason.seasonTheme + " 스킨 세트",
                    quantity = 1,
                    rarity = 4,
                    description = "시즌 테마 스킨 세트"
                },
                seasonCurrencyCost = 500,
                purchaseLimit = 1
            });
            
            // 부스터 팩
            seasonShop.Add(new SeasonShopItem
            {
                itemId = 3,
                item = new RewardItem
                {
                    type = RewardItem.RewardType.ExperienceBoost,
                    itemName = "경험치 부스터 팩 (7일)",
                    quantity = 7,
                    rarity = 2,
                    description = "7일간 경험치 2배"
                },
                seasonCurrencyCost = 200,
                purchaseLimit = 3
            });
            
            // 자원 팩
            for (int i = 0; i < 4; i++)
            {
                ResourceType resourceType = (ResourceType)i;
                seasonShop.Add(new SeasonShopItem
                {
                    itemId = 10 + i,
                    item = new RewardItem
                    {
                        type = RewardItem.RewardType.Resource,
                        resourceType = resourceType,
                        itemName = $"{resourceType} 팩",
                        quantity = 1000,
                        rarity = 1,
                        description = $"{resourceType} x1000"
                    },
                    seasonCurrencyCost = 50,
                    purchaseLimit = 10
                });
            }
        }
        
        DateTime GetNextMonday()
        {
            DateTime now = DateTime.Now;
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0 && now.Hour >= 5)
            {
                daysUntilMonday = 7;
            }
            return now.Date.AddDays(daysUntilMonday).AddHours(5);
        }
        
        IEnumerator SeasonUpdater()
        {
            while (true)
            {
                yield return new WaitForSeconds(60f); // 1분마다 체크
                
                // 시즌 종료 체크
                if (currentSeason != null && DateTime.Now >= currentSeason.endDate)
                {
                    EndCurrentSeason();
                }
                
                // 도전과제 만료 체크
                CheckExpiredChallenges();
            }
        }
        
        void EndCurrentSeason()
        {
            if (currentSeason == null || !currentSeason.isActive)
                return;
            
            currentSeason.isActive = false;
            
            // 시즌 종료 보상
            GiveSeasonEndRewards();
            
            OnSeasonEnded?.Invoke(currentSeason);
            
            // 새 시즌 시작
            StartNewSeason();
        }
        
        void GiveSeasonEndRewards()
        {
            // 달성 레벨에 따른 추가 보상
            int finalLevel = playerPass.currentLevel;
            
            if (finalLevel >= 100)
            {
                // 시즌 완주 보상
                Debug.Log("Season completed! Special rewards granted.");
            }
            else if (finalLevel >= 50)
            {
                // 중간 달성 보상
                Debug.Log($"Reached level {finalLevel}. Partial rewards granted.");
            }
            
            // 시즌 점수에 따른 보상
            int seasonScore = playerPass.totalSeasonScore;
            // TODO: 시즌 점수 보상 지급
        }
        
        void CheckExpiredChallenges()
        {
            DateTime now = DateTime.Now;
            
            // 만료된 도전과제 제거 및 새로운 도전과제 생성
            var expiredDaily = activeChallenges.Where(c => 
                c.type == ChallengeType.Daily && c.expiryDate <= now).ToList();
                
            if (expiredDaily.Count > 0)
            {
                foreach (var challenge in expiredDaily)
                {
                    activeChallenges.Remove(challenge);
                }
                GenerateDailyChallenges();
            }
            
            var expiredWeekly = activeChallenges.Where(c => 
                c.type == ChallengeType.Weekly && c.expiryDate <= now).ToList();
                
            if (expiredWeekly.Count > 0)
            {
                foreach (var challenge in expiredWeekly)
                {
                    activeChallenges.Remove(challenge);
                }
                GenerateWeeklyChallenges();
            }
        }
        
        // 패스 구매
        public bool UpgradePass(PassType newPassType)
        {
            if (playerPass.passType >= newPassType)
                return false;
            
            playerPass.passType = newPassType;
            
            // 이미 달성한 레벨의 프리미엄/엘리트 보상을 받을 수 있게 함
            Debug.Log($"Pass upgraded to {newPassType}!");
            
            return true;
        }
        
        // 경험치 획득
        public void GainExperience(int amount)
        {
            if (currentSeason == null || !currentSeason.isActive)
                return;
            
            if (playerPass.currentLevel >= 100)
                return; // 최대 레벨
            
            playerPass.currentExperience += amount;
            playerPass.totalSeasonScore += amount;
            
            OnExperienceGained?.Invoke(amount);
            
            // 레벨업 체크
            while (playerPass.currentExperience >= playerPass.experienceToNextLevel && 
                   playerPass.currentLevel < 100)
            {
                playerPass.currentExperience -= playerPass.experienceToNextLevel;
                playerPass.currentLevel++;
                
                if (playerPass.currentLevel <= 100)
                {
                    playerPass.experienceToNextLevel = experienceTable[playerPass.currentLevel];
                }
                
                OnLevelUp?.Invoke(playerPass.currentLevel);
                
                // 레벨업 보상
                CheckAndGrantLevelRewards(playerPass.currentLevel);
            }
        }
        
        // 레벨 보상 확인 및 지급
        void CheckAndGrantLevelRewards(int level)
        {
            // 자동으로 받을 수 있는 보상 확인
            foreach (var reward in seasonRewards.Values)
            {
                if (reward.level == level && !reward.isClaimed)
                {
                    if (reward.requiredPass == PassType.Free ||
                        (reward.requiredPass == PassType.Premium && playerPass.passType >= PassType.Premium) ||
                        (reward.requiredPass == PassType.Elite && playerPass.passType >= PassType.Elite))
                    {
                        // 자동 지급 가능한 보상은 바로 지급
                        if (reward.reward.type == RewardItem.RewardType.Gold ||
                            reward.reward.type == RewardItem.RewardType.Resource)
                        {
                            ClaimReward(reward);
                        }
                    }
                }
            }
        }
        
        // 보상 수령
        public bool ClaimReward(int level, PassType passType)
        {
            int rewardKey = passType == PassType.Free ? level * 2 - 1 : level * 2;
            if (passType == PassType.Elite)
            {
                rewardKey = level * 2 + 1000;
            }
            
            if (!seasonRewards.ContainsKey(rewardKey))
                return false;
            
            return ClaimReward(seasonRewards[rewardKey]);
        }
        
        bool ClaimReward(PassReward reward)
        {
            if (reward.isClaimed)
                return false;
            
            if (playerPass.currentLevel < reward.level)
                return false;
            
            if (playerPass.passType < reward.requiredPass)
                return false;
            
            // 보상 지급
            ApplyReward(reward.reward);
            
            reward.isClaimed = true;
            playerPass.claimedRewards.Add(reward.level);
            
            OnRewardClaimed?.Invoke(reward);
            
            return true;
        }
        
        void ApplyReward(RewardItem reward)
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return;
            
            switch (reward.type)
            {
                case RewardItem.RewardType.Gold:
                    if (gameManager.ResourceManager != null)
                    {
                        gameManager.ResourceManager.AddGold(reward.quantity);
                    }
                    break;
                    
                case RewardItem.RewardType.Resource:
                    if (gameManager.ResourceManager != null)
                    {
                        switch (reward.resourceType)
                        {
                            case ResourceType.Wood:
                                gameManager.ResourceManager.AddWood(reward.quantity);
                                break;
                            case ResourceType.Stone:
                                gameManager.ResourceManager.AddStone(reward.quantity);
                                break;
                            case ResourceType.ManaStone:
                                gameManager.ResourceManager.AddManaStone(reward.quantity);
                                break;
                        }
                    }
                    break;
                    
                case RewardItem.RewardType.Premium:
                    // 시즌 화폐 추가
                    AddSeasonCurrency(reward.quantity);
                    break;
                    
                default:
                    // TODO: 다른 보상 타입 처리
                    Debug.Log($"Reward received: {reward.itemName} x{reward.quantity}");
                    break;
            }
        }
        
        // 도전과제 진행
        public void UpdateChallengeProgress(int challengeId, int progress)
        {
            var challenge = activeChallenges.FirstOrDefault(c => c.challengeId == challengeId);
            if (challenge == null || challenge.isCompleted)
                return;
            
            challenge.currentValue = Mathf.Min(challenge.currentValue + progress, challenge.targetValue);
            
            if (challenge.currentValue >= challenge.targetValue)
            {
                CompleteChallenge(challenge);
            }
        }
        
        public void UpdateChallengesByType(string progressType, int amount = 1)
        {
            // 타입별로 도전과제 업데이트
            switch (progressType)
            {
                case "battle_win":
                    UpdateChallengeProgress(1001, amount); // 일일 전투
                    break;
                    
                case "gold_earned":
                    UpdateChallengeProgress(1002, amount); // 자원 수집가
                    break;
                    
                case "building_action":
                    UpdateChallengeProgress(1003, amount); // 건설의 달인
                    break;
                    
                case "ai_battle":
                    UpdateChallengeProgress(2001, amount); // 주간 정복자
                    break;
                    
                case "dungeon_floor":
                    UpdateChallengeProgress(2002, amount); // 던전 탐험가
                    break;
                    
                case "unit_levelup":
                    UpdateChallengeProgress(2003, amount); // 성장의 주간
                    break;
            }
        }
        
        void CompleteChallenge(SeasonChallenge challenge)
        {
            challenge.isCompleted = true;
            OnChallengeCompleted?.Invoke(challenge);
        }
        
        // 도전과제 보상 수령
        public bool ClaimChallengeReward(int challengeId)
        {
            var challenge = activeChallenges.FirstOrDefault(c => c.challengeId == challengeId);
            if (challenge == null || !challenge.isCompleted || challenge.isClaimed)
                return false;
            
            challenge.isClaimed = true;
            playerPass.completedChallenges.Add(challengeId);
            
            // 경험치 지급
            GainExperience(challenge.experienceReward);
            
            return true;
        }
        
        // 시즌 화폐 관리
        public void AddSeasonCurrency(int amount)
        {
            seasonCurrency += amount;
            OnSeasonCurrencyChanged?.Invoke(seasonCurrency);
        }
        
        public bool SpendSeasonCurrency(int amount)
        {
            if (seasonCurrency < amount)
                return false;
            
            seasonCurrency -= amount;
            OnSeasonCurrencyChanged?.Invoke(seasonCurrency);
            return true;
        }
        
        // 시즌 상점 구매
        public bool PurchaseShopItem(int itemId)
        {
            var shopItem = seasonShop.FirstOrDefault(i => i.itemId == itemId);
            if (shopItem == null || !shopItem.isAvailable)
                return false;
            
            if (shopItem.timesPurchased >= shopItem.purchaseLimit)
                return false;
            
            if (!SpendSeasonCurrency(shopItem.seasonCurrencyCost))
                return false;
            
            // 아이템 지급
            ApplyReward(shopItem.item);
            
            shopItem.timesPurchased++;
            
            if (shopItem.timesPurchased >= shopItem.purchaseLimit)
            {
                shopItem.isAvailable = false;
            }
            
            return true;
        }
        
        // 조회 메서드들
        public Season GetCurrentSeason()
        {
            return currentSeason;
        }
        
        public PlayerSeasonPass GetPlayerPass()
        {
            return playerPass;
        }
        
        public List<PassReward> GetRewardsForLevel(int level)
        {
            return seasonRewards.Values.Where(r => r.level == level).ToList();
        }
        
        public List<PassReward> GetAllRewards()
        {
            return seasonRewards.Values.OrderBy(r => r.level).ToList();
        }
        
        public List<PassReward> GetClaimableRewards()
        {
            return seasonRewards.Values.Where(r => 
                !r.isClaimed && 
                r.level <= playerPass.currentLevel &&
                r.requiredPass <= playerPass.passType).ToList();
        }
        
        public List<SeasonChallenge> GetActiveChallenges()
        {
            return activeChallenges;
        }
        
        public List<SeasonChallenge> GetCompletedChallenges()
        {
            return activeChallenges.Where(c => c.isCompleted && !c.isClaimed).ToList();
        }
        
        public List<SeasonShopItem> GetSeasonShop()
        {
            return seasonShop.Where(i => i.isAvailable).ToList();
        }
        
        public int GetSeasonCurrency()
        {
            return seasonCurrency;
        }
        
        public float GetLevelProgress()
        {
            if (playerPass.currentLevel >= 100)
                return 1f;
            
            return (float)playerPass.currentExperience / playerPass.experienceToNextLevel;
        }
        
        public int GetTotalRewardCount()
        {
            return seasonRewards.Count;
        }
        
        public int GetClaimedRewardCount()
        {
            return seasonRewards.Values.Count(r => r.isClaimed);
        }
    }
}