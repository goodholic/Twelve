using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Battle;
using GuildMaster.Data;
using JobClass = GuildMaster.Battle.JobClass;
// Unit is already available from GuildMaster.Battle namespace
using Rarity = GuildMaster.Data.Rarity;

namespace GuildMaster.Systems
{
    public class GachaSystem : MonoBehaviour
    {
        private static GachaSystem _instance;
        public static GachaSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GachaSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GachaSystem");
                        _instance = go.AddComponent<GachaSystem>();
                    }
                }
                return _instance;
            }
        }
        
        // 가챠 타입
        public enum GachaType
        {
            Normal,     // 일반 가챠
            Premium,    // 프리미엄 가챠
            Equipment,  // 장비 전용
            Limited,    // 한정 가챠
            Free        // 무료 가챠
        }
        
        // 가챠 배너
        [System.Serializable]
        public class GachaBanner
        {
            public string bannerId;
            public string bannerName;
            public GachaType type;
            public bool isActive;
            public DateTime startDate;
            public DateTime endDate;
            public int cost;                    // 단일 뽑기 비용
            public int multiCost;               // 10연차 비용
            public string currencyType;         // 사용 화폐
            public RarityRate[] rarityRates;   // 등급별 확률
            public int guaranteedCount;         // 확정 보장 카운트
            public List<int> featuredUnits;     // 픽업 유닛 ID
            public float featuredRate;          // 픽업 확률
        }
        
        // 등급별 확률
        [System.Serializable]
        public class RarityRate
        {
            public Rarity rarity;
            public float rate;
        }
        
        // 가챠 결과
        [System.Serializable]
        public class GachaResult
        {
            // public UnitStatus unit; // Removed as UnitStatus was removed
            public bool isNew;
            public bool isFeatured;
            public int duplicateTokens; // 중복 시 받는 토큰
        }
        
        // 천장 시스템
        [System.Serializable]
        public class PitySystem
        {
            public string bannerId;
            public int currentCount;
            public int pityCount = 90;      // 천장 카운트
            public int softPityStart = 75;  // 소프트 천장 시작
            public float rateIncreasePerPull = 0.05f; // 소프트 천장 확률 증가
        }
        
        private Dictionary<string, GachaBanner> activeBanners;
        private Dictionary<string, PitySystem> playerPity;
        private Dictionary<string, DateTime> lastFreeGacha;
        
        // 이벤트
        public event Action<GachaResult> OnSingleGachaComplete;
        public event Action<List<GachaResult>> OnMultiGachaComplete;
        public event Action<string, int> OnPityUpdated;
        
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
            activeBanners = new Dictionary<string, GachaBanner>();
            playerPity = new Dictionary<string, PitySystem>();
            lastFreeGacha = new Dictionary<string, DateTime>();
            
            // 기본 배너 설정
            SetupDefaultBanners();
            
            // 배너 업데이트 체크
            StartCoroutine(BannerUpdateChecker());
        }
        
        void SetupDefaultBanners()
        {
            // 일반 가챠 배너
            var normalBanner = new GachaBanner
            {
                bannerId = "normal_gacha",
                bannerName = "일반 모집",
                type = GachaType.Normal,
                isActive = true,
                cost = 300,
                multiCost = 2700,
                currencyType = "gold",
                rarityRates = new RarityRate[]
                {
                    new RarityRate { rarity = Rarity.Common, rate = 0.60f },
                    new RarityRate { rarity = Rarity.Uncommon, rate = 0.27f },
                    new RarityRate { rarity = Rarity.Rare, rate = 0.10f },
                    new RarityRate { rarity = Rarity.Epic, rate = 0.025f },
                    new RarityRate { rarity = Rarity.Legendary, rate = 0.005f }
                },
                guaranteedCount = 0
            };
            activeBanners["normal_gacha"] = normalBanner;
            
            // 프리미엄 가챠 배너
            var premiumBanner = new GachaBanner
            {
                bannerId = "premium_gacha",
                bannerName = "프리미엄 모집",
                type = GachaType.Premium,
                isActive = true,
                cost = 150,
                multiCost = 1500,
                currencyType = "gem",
                rarityRates = new RarityRate[]
                {
                    new RarityRate { rarity = Rarity.Common, rate = 0.00f },
                    new RarityRate { rarity = Rarity.Uncommon, rate = 0.00f },
                    new RarityRate { rarity = Rarity.Rare, rate = 0.80f },
                    new RarityRate { rarity = Rarity.Epic, rate = 0.17f },
                    new RarityRate { rarity = Rarity.Legendary, rate = 0.03f }
                },
                guaranteedCount = 10 // 10회마다 Epic 이상 확정
            };
            activeBanners["premium_gacha"] = premiumBanner;
            
            // 무료 가챠 배너
            var freeBanner = new GachaBanner
            {
                bannerId = "free_gacha",
                bannerName = "일일 무료 모집",
                type = GachaType.Free,
                isActive = true,
                cost = 0,
                multiCost = 0,
                currencyType = "free",
                rarityRates = new RarityRate[]
                {
                    new RarityRate { rarity = Rarity.Common, rate = 0.70f },
                    new RarityRate { rarity = Rarity.Uncommon, rate = 0.25f },
                    new RarityRate { rarity = Rarity.Rare, rate = 0.04f },
                    new RarityRate { rarity = Rarity.Epic, rate = 0.009f },
                    new RarityRate { rarity = Rarity.Legendary, rate = 0.001f }
                },
                guaranteedCount = 0
            };
            activeBanners["free_gacha"] = freeBanner;
        }
        
        // 단일 가챠
        public GachaResult PerformSingleGacha(string bannerId)
        {
            if (!activeBanners.ContainsKey(bannerId))
                return null;
                
            var banner = activeBanners[bannerId];
            
            // 무료 가챠 쿨타임 체크
            if (banner.type == GachaType.Free)
            {
                if (!CanUseFreeGacha(bannerId))
                    return null;
            }
            
            // 비용 차감
            if (!PayGachaCost(banner, 1))
                return null;
            
            // 천장 시스템 업데이트
            UpdatePity(bannerId);
            
            // 가챠 수행
            var result = PerformGacha(banner);
            
            // 무료 가챠 사용 기록
            if (banner.type == GachaType.Free)
            {
                lastFreeGacha[bannerId] = DateTime.Now;
            }
            
            OnSingleGachaComplete?.Invoke(result);
            
            return result;
        }
        
        // 10연차
        public List<GachaResult> PerformMultiGacha(string bannerId)
        {
            if (!activeBanners.ContainsKey(bannerId))
                return null;
                
            var banner = activeBanners[bannerId];
            
            // 비용 차감
            if (!PayGachaCost(banner, 10))
                return null;
            
            List<GachaResult> results = new List<GachaResult>();
            bool hasHighRarity = false;
            
            // 9회 일반 가챠
            for (int i = 0; i < 9; i++)
            {
                UpdatePity(bannerId);
                var result = PerformGacha(banner);
                results.Add(result);
                
                if (result.unit.rarity >= Rarity.Rare)
                    hasHighRarity = true;
            }
            
            // 마지막 1회는 최소 Rare 이상 보장
            UpdatePity(bannerId);
            var lastResult = PerformGacha(banner, hasHighRarity ? Rarity.Common : Rarity.Rare);
            results.Add(lastResult);
            
            OnMultiGachaComplete?.Invoke(results);
            
            return results;
        }
        
        // 가챠 수행
        GachaResult PerformGacha(GachaBanner banner, Rarity minimumRarity = Rarity.Common)
        {
            // 천장 확인
            var pity = GetOrCreatePity(banner.bannerId);
            bool isPityPull = pity.currentCount >= pity.pityCount;
            
            // 등급 결정
            Rarity rarity = isPityPull ? Rarity.Legendary : DetermineRarity(banner, pity);
            
            if (rarity < minimumRarity)
                rarity = minimumRarity;
            
            // 픽업 여부 결정
            bool isFeatured = false;
            if (banner.featuredUnits != null && banner.featuredUnits.Count > 0)
            {
                isFeatured = UnityEngine.Random.value < banner.featuredRate;
            }
            
            // 유닛 선택
            // UnitStatus unit = null; // Removed as UnitStatus was removed
            if (isFeatured && banner.featuredUnits.Count > 0)
            {
                int featuredId = banner.featuredUnits[UnityEngine.Random.Range(0, banner.featuredUnits.Count)];
                unit = CharacterManager.Instance.CreateUnit(featuredId.ToString());
            }
            else
            {
                unit = CharacterManager.Instance.CreateRandomUnitByRarity(rarity);
            }
            
            // 중복 처리
            bool isNew = !IsUnitOwned(unit);
            int duplicateTokens = isNew ? 0 : CalculateDuplicateTokens(unit);
            
            // 천장 리셋
            if (rarity >= Rarity.Legendary || isPityPull)
            {
                pity.currentCount = 0;
            }
            
            return new GachaResult
            {
                unit = unit,
                isNew = isNew,
                isFeatured = isFeatured,
                duplicateTokens = duplicateTokens
            };
        }
        
        // 등급 결정
        Rarity DetermineRarity(GachaBanner banner, PitySystem pity)
        {
            float totalRate = 0f;
            float roll = UnityEngine.Random.value;
            
            // 소프트 천장 적용
            if (pity.currentCount >= pity.softPityStart)
            {
                int softPityCount = pity.currentCount - pity.softPityStart;
                float legendaryBonus = softPityCount * pity.rateIncreasePerPull;
                
                // 전설 확률 증가
                foreach (var rate in banner.rarityRates)
                {
                    if (rate.rarity == Rarity.Legendary)
                    {
                        rate.rate += legendaryBonus;
                        break;
                    }
                }
            }
            
            // 확률에 따른 등급 결정
            foreach (var rarityRate in banner.rarityRates)
            {
                totalRate += rarityRate.rate;
                if (roll <= totalRate)
                {
                    return rarityRate.rarity;
                }
            }
            
            return Rarity.Common;
        }
        
        // 비용 지불
        bool PayGachaCost(GachaBanner banner, int count)
        {
            if (banner.type == GachaType.Free)
                return true;
                
            // ResourceManager 타입이 제거되어 주석 처리
            // var gameManager = Core.GameManager.Instance;
            // if (gameManager?.ResourceManager == null)
            //     return false;
            // 
            // int totalCost = count == 1 ? banner.cost : banner.multiCost;
            // 
            // switch (banner.currencyType)
            // {
            //     case "gold":
            //         if (gameManager.ResourceManager.GetGold() >= totalCost)
            //         {
            //             gameManager.ResourceManager.AddGold(-totalCost);
            //             return true;
            //         }
            //         break;
            //         
            //     case "gem":
            //         // TODO: 젬 시스템 구현
            //         return true;
            // }
            // 
            // return false;
            
            // 임시로 true 반환
            return true;
        }
        
        // 천장 시스템
        PitySystem GetOrCreatePity(string bannerId)
        {
            if (!playerPity.ContainsKey(bannerId))
            {
                playerPity[bannerId] = new PitySystem { bannerId = bannerId };
            }
            return playerPity[bannerId];
        }
        
        void UpdatePity(string bannerId)
        {
            var pity = GetOrCreatePity(bannerId);
            pity.currentCount++;
            OnPityUpdated?.Invoke(bannerId, pity.currentCount);
        }
        
        // 무료 가챠 사용 가능 확인
        public bool CanUseFreeGacha(string bannerId)
        {
            if (!lastFreeGacha.ContainsKey(bannerId))
                return true;
                
            var lastUse = lastFreeGacha[bannerId];
            var nextReset = lastUse.Date.AddDays(1).AddHours(5); // 다음날 오전 5시
            
            return DateTime.Now >= nextReset;
        }
        
        // 유닛 소유 확인
        // Removed IsUnitOwned method as UnitStatus was removed
        
        // 중복 토큰 계산
        // Removed CalculateDuplicateTokens method as UnitStatus was removed
        
        // 배너 업데이트 체크
        IEnumerator BannerUpdateChecker()
        {
            while (true)
            {
                yield return new WaitForSeconds(3600f); // 1시간마다 체크
                
                // 종료된 배너 비활성화
                var now = DateTime.Now;
                foreach (var banner in activeBanners.Values)
                {
                    if (banner.endDate != default && now > banner.endDate)
                    {
                        banner.isActive = false;
                    }
                }
            }
        }
        
        // 한정 배너 추가
        public void AddLimitedBanner(GachaBanner banner)
        {
            banner.type = GachaType.Limited;
            activeBanners[banner.bannerId] = banner;
        }
        
        // 배너 조회
        public List<GachaBanner> GetActiveBanners()
        {
            return activeBanners.Values.Where(b => b.isActive).ToList();
        }
        
        public GachaBanner GetBanner(string bannerId)
        {
            return activeBanners.ContainsKey(bannerId) ? activeBanners[bannerId] : null;
        }
        
        // 천장 정보 조회
        public int GetPityCount(string bannerId)
        {
            var pity = GetOrCreatePity(bannerId);
            return pity.currentCount;
        }
        
        public int GetPityRemaining(string bannerId)
        {
            var pity = GetOrCreatePity(bannerId);
            return pity.pityCount - pity.currentCount;
        }

        // Removed CreateUnitFromCharacterData method as UnitStatus was removed
    }
}