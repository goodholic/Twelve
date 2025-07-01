using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Core;
using GuildMaster.Guild;
using GuildMaster.Data;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 자원 생산 및 관리 시스템
    /// 건물별 생산량 계산, 버프 적용, 시간별 자원 생산 관리
    /// </summary>
    public class ResourceProductionSystem : MonoBehaviour
    {
        private static ResourceProductionSystem _instance;
        public static ResourceProductionSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ResourceProductionSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ResourceProductionSystem");
                        _instance = go.AddComponent<ResourceProductionSystem>();
                    }
                }
                return _instance;
            }
        }

        [Header("Production Settings")]
        [SerializeField] private float productionInterval = 60f; // 기본 생산 간격 (초)
        [SerializeField] private bool autoCollectResources = true;
        [SerializeField] private float collectionEfficiency = 1f;
        
        [Header("Production Buffs")]
        [SerializeField] private float weekendBonus = 1.5f; // 주말 보너스
        [SerializeField] private float nightBonus = 1.2f; // 야간 보너스
        [SerializeField] private float eventMultiplier = 1f; // 이벤트 배율
        
        // 생산 데이터
        private Dictionary<string, ProductionBuilding> productionBuildings = new Dictionary<string, ProductionBuilding>();
        private Dictionary<ResourceType, float> baseProductionRates = new Dictionary<ResourceType, float>();
        private Dictionary<ResourceType, float> currentProductionRates = new Dictionary<ResourceType, float>();
        
        // 생산 버프
        private List<ProductionBuff> activeBuffs = new List<ProductionBuff>();
        
        // 생산 통계
        private Dictionary<ResourceType, float> totalProduced = new Dictionary<ResourceType, float>();
        private Dictionary<ResourceType, float> hourlyProduction = new Dictionary<ResourceType, float>();
        private float lastProductionUpdate;
        
        // 이벤트
        public event Action<Dictionary<ResourceType, int>> OnResourcesProduced;
        public event Action<ProductionBuff> OnBuffApplied;
        public event Action<ProductionBuff> OnBuffExpired;
        public event Action<string, float> OnBuildingProductionChanged;
        
        [System.Serializable]
        public class ProductionBuilding
        {
            public string buildingId;
            public PlacedBuilding building;
            public Dictionary<ResourceType, float> baseProduction;
            public Dictionary<ResourceType, float> currentProduction;
            public float efficiency = 1f;
            public bool isActive = true;
            public float lastCollectionTime;
            
            public ProductionBuilding(PlacedBuilding placedBuilding)
            {
                building = placedBuilding;
                buildingId = placedBuilding.buildingId.ToString();
                baseProduction = new Dictionary<ResourceType, float>();
                currentProduction = new Dictionary<ResourceType, float>();
                lastCollectionTime = Time.time;
            }
        }
        
        [System.Serializable]
        public class ProductionBuff
        {
            public string buffId;
            public string buffName;
            public BuffType buffType;
            public ResourceType? targetResource;
            public float multiplier;
            public float duration;
            public float startTime;
            public string source;
            
            public bool IsExpired => Time.time > startTime + duration;
            public float RemainingTime => Mathf.Max(0, (startTime + duration) - Time.time);
        }
        
        public enum BuffType
        {
            Global,         // 모든 자원
            ResourceSpecific, // 특정 자원
            BuildingType,   // 특정 건물 타입
            Temporary,      // 임시 버프
            Permanent       // 영구 버프
        }
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeProductionRates();
        }
        
        void Start()
        {
            StartCoroutine(ProductionCoroutine());
            StartCoroutine(BuffUpdateCoroutine());
            
            // 건물 시스템 이벤트 구독
            if (GuildBuildingSystem.Instance != null)
            {
                GuildBuildingSystem.Instance.OnBuildingCompleted += OnBuildingCompleted;
                GuildBuildingSystem.Instance.OnBuildingUpgraded += OnBuildingUpgraded;
                GuildBuildingSystem.Instance.OnBuildingRemoved += OnBuildingRemoved;
            }
        }
        
        void InitializeProductionRates()
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                baseProductionRates[type] = 0f;
                currentProductionRates[type] = 0f;
                totalProduced[type] = 0f;
                hourlyProduction[type] = 0f;
            }
        }
        
        /// <summary>
        /// 생산 코루틴
        /// </summary>
        IEnumerator ProductionCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(productionInterval);
                
                if (autoCollectResources)
                {
                    ProduceResources();
                }
            }
        }
        
        /// <summary>
        /// 버프 업데이트 코루틴
        /// </summary>
        IEnumerator BuffUpdateCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                
                // 만료된 버프 제거
                var expiredBuffs = activeBuffs.Where(b => b.IsExpired).ToList();
                foreach (var buff in expiredBuffs)
                {
                    RemoveBuff(buff);
                }
                
                // 생산률 재계산
                if (expiredBuffs.Count > 0)
                {
                    RecalculateAllProduction();
                }
            }
        }
        
        /// <summary>
        /// 자원 생산
        /// </summary>
        void ProduceResources()
        {
            var producedResources = new Dictionary<ResourceType, int>();
            
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                producedResources[type] = 0;
            }
            
            // 각 생산 건물에서 자원 수집
            foreach (var building in productionBuildings.Values)
            {
                if (!building.isActive) continue;
                
                foreach (var production in building.currentProduction)
                {
                    float amount = production.Value * (productionInterval / 60f); // 분당 생산량을 실제 간격으로 변환
                    int intAmount = Mathf.FloorToInt(amount * collectionEfficiency);
                    
                    if (intAmount > 0)
                    {
                        producedResources[production.Key] += intAmount;
                        totalProduced[production.Key] += intAmount;
                    }
                }
                
                building.lastCollectionTime = Time.time;
            }
            
            // 자원 추가
            var resourceManager = ResourceManager.Instance;
            foreach (var resource in producedResources)
            {
                if (resource.Value > 0)
                {
                    resourceManager.AddResource(resource.Key, resource.Value, "Building Production");
                }
            }
            
            // 시간별 생산량 업데이트
            UpdateHourlyProduction();
            
            // 이벤트 발생
            OnResourcesProduced?.Invoke(producedResources);
        }
        
        /// <summary>
        /// 건물 완성 시 호출
        /// </summary>
        void OnBuildingCompleted(PlacedBuilding building)
        {
            var buildingDataSO = DataManager.Instance?.GetBuildingDataSO(building.buildingId.ToString());
            if (buildingDataSO != null && buildingDataSO.isProducer)
            {
                RegisterProductionBuilding(building, buildingDataSO);
            }
        }
        
        /// <summary>
        /// 생산 건물 등록
        /// </summary>
        void RegisterProductionBuilding(PlacedBuilding building, BuildingDataSO buildingData)
        {
            var productionBuilding = new ProductionBuilding(building);
            
            // 기본 생산량 설정
            if (buildingData.production != null)
            {
                if (buildingData.production.goldPerHour > 0)
                    productionBuilding.baseProduction[ResourceType.Gold] = buildingData.production.goldPerHour;
                if (buildingData.production.woodPerHour > 0)
                    productionBuilding.baseProduction[ResourceType.Wood] = buildingData.production.woodPerHour;
                if (buildingData.production.stonePerHour > 0)
                    productionBuilding.baseProduction[ResourceType.Stone] = buildingData.production.stonePerHour;
                if (buildingData.production.manaPerHour > 0)
                    productionBuilding.baseProduction[ResourceType.ManaStone] = buildingData.production.manaPerHour;
            }
            
            productionBuildings[building.buildingId.ToString()] = productionBuilding;
            
            // 생산률 계산
            CalculateBuildingProduction(productionBuilding);
            UpdateGlobalProduction();
        }
        
        /// <summary>
        /// 건물별 생산량 계산
        /// </summary>
        void CalculateBuildingProduction(ProductionBuilding productionBuilding)
        {
            foreach (var baseProduction in productionBuilding.baseProduction)
            {
                float production = baseProduction.Value;
                
                // 건물 레벨 보너스
                int level = productionBuilding.building.currentLevel;
                production *= (1f + (level - 1) * 0.1f); // 레벨당 10% 증가
                
                // 건물 효율성
                production *= productionBuilding.efficiency;
                
                // 버프 적용
                production = ApplyBuffsToProduction(production, baseProduction.Key, productionBuilding);
                
                // 시간대별 보너스
                production *= GetTimeBonus();
                
                productionBuilding.currentProduction[baseProduction.Key] = production;
            }
            
            OnBuildingProductionChanged?.Invoke(productionBuilding.buildingId, productionBuilding.efficiency);
        }
        
        /// <summary>
        /// 버프 적용
        /// </summary>
        float ApplyBuffsToProduction(float baseProduction, ResourceType resourceType, ProductionBuilding building)
        {
            float totalMultiplier = 1f;
            
            foreach (var buff in activeBuffs)
            {
                bool shouldApply = false;
                
                switch (buff.buffType)
                {
                    case BuffType.Global:
                        shouldApply = true;
                        break;
                    case BuffType.ResourceSpecific:
                        shouldApply = buff.targetResource == resourceType;
                        break;
                    case BuffType.BuildingType:
                        // 건물 타입별 버프 체크
                        shouldApply = CheckBuildingTypeBuff(buff, building);
                        break;
                }
                
                if (shouldApply)
                {
                    totalMultiplier *= buff.multiplier;
                }
            }
            
            return baseProduction * totalMultiplier;
        }
        
        bool CheckBuildingTypeBuff(ProductionBuff buff, ProductionBuilding building)
        {
            // 건물 타입에 따른 버프 체크 로직
            return true; // 임시 구현
        }
        
        /// <summary>
        /// 시간대 보너스
        /// </summary>
        float GetTimeBonus()
        {
            float bonus = 1f;
            
            // 주말 체크
            if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || 
                DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
            {
                bonus *= weekendBonus;
            }
            
            // 야간 체크 (오후 10시 ~ 오전 6시)
            int hour = DateTime.Now.Hour;
            if (hour >= 22 || hour < 6)
            {
                bonus *= nightBonus;
            }
            
            // 이벤트 배율
            bonus *= eventMultiplier;
            
            return bonus;
        }
        
        /// <summary>
        /// 건물 업그레이드 시
        /// </summary>
        void OnBuildingUpgraded(PlacedBuilding building)
        {
            string buildingId = building.buildingId.ToString();
            if (productionBuildings.ContainsKey(buildingId))
            {
                CalculateBuildingProduction(productionBuildings[buildingId]);
                UpdateGlobalProduction();
            }
        }
        
        /// <summary>
        /// 건물 제거 시
        /// </summary>
        void OnBuildingRemoved(PlacedBuilding building)
        {
            string buildingId = building.buildingId.ToString();
            if (productionBuildings.ContainsKey(buildingId))
            {
                productionBuildings.Remove(buildingId);
                UpdateGlobalProduction();
            }
        }
        
        /// <summary>
        /// 전체 생산량 업데이트
        /// </summary>
        void UpdateGlobalProduction()
        {
            // 생산률 초기화
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                currentProductionRates[type] = 0f;
            }
            
            // 모든 건물의 생산량 합산
            foreach (var building in productionBuildings.Values)
            {
                if (!building.isActive) continue;
                
                foreach (var production in building.currentProduction)
                {
                    currentProductionRates[production.Key] += production.Value;
                }
            }
            
            // ResourceManager에 생산률 업데이트
            var resourceManager = ResourceManager.Instance;
            resourceManager.SetGoldProductionRate(currentProductionRates[ResourceType.Gold]);
            resourceManager.SetWoodProductionRate(currentProductionRates[ResourceType.Wood]);
            resourceManager.SetStoneProductionRate(currentProductionRates[ResourceType.Stone]);
            resourceManager.SetManaStoneProductionRate(currentProductionRates[ResourceType.ManaStone]);
        }
        
        /// <summary>
        /// 버프 추가
        /// </summary>
        public void AddBuff(string buffName, BuffType type, float multiplier, float duration, 
                           ResourceType? targetResource = null, string source = "Unknown")
        {
            var buff = new ProductionBuff
            {
                buffId = Guid.NewGuid().ToString(),
                buffName = buffName,
                buffType = type,
                multiplier = multiplier,
                duration = duration,
                startTime = Time.time,
                targetResource = targetResource,
                source = source
            };
            
            activeBuffs.Add(buff);
            OnBuffApplied?.Invoke(buff);
            
            // 생산률 재계산
            RecalculateAllProduction();
        }
        
        /// <summary>
        /// 버프 제거
        /// </summary>
        void RemoveBuff(ProductionBuff buff)
        {
            activeBuffs.Remove(buff);
            OnBuffExpired?.Invoke(buff);
        }
        
        /// <summary>
        /// 모든 생산량 재계산
        /// </summary>
        void RecalculateAllProduction()
        {
            foreach (var building in productionBuildings.Values)
            {
                CalculateBuildingProduction(building);
            }
            UpdateGlobalProduction();
        }
        
        /// <summary>
        /// 시간당 생산량 업데이트
        /// </summary>
        void UpdateHourlyProduction()
        {
            float deltaTime = Time.time - lastProductionUpdate;
            if (deltaTime >= 3600f) // 1시간
            {
                foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                {
                    hourlyProduction[type] = totalProduced[type] - hourlyProduction[type];
                }
                lastProductionUpdate = Time.time;
            }
        }
        
        /// <summary>
        /// 건물 효율성 설정
        /// </summary>
        public void SetBuildingEfficiency(string buildingId, float efficiency)
        {
            if (productionBuildings.ContainsKey(buildingId))
            {
                productionBuildings[buildingId].efficiency = Mathf.Clamp01(efficiency);
                CalculateBuildingProduction(productionBuildings[buildingId]);
                UpdateGlobalProduction();
            }
        }
        
        /// <summary>
        /// 건물 활성화/비활성화
        /// </summary>
        public void SetBuildingActive(string buildingId, bool active)
        {
            if (productionBuildings.ContainsKey(buildingId))
            {
                productionBuildings[buildingId].isActive = active;
                UpdateGlobalProduction();
            }
        }
        
        /// <summary>
        /// 수동 자원 수집
        /// </summary>
        public Dictionary<ResourceType, int> CollectResourcesManually(string buildingId)
        {
            var collected = new Dictionary<ResourceType, int>();
            
            if (!productionBuildings.ContainsKey(buildingId))
                return collected;
            
            var building = productionBuildings[buildingId];
            float timeSinceLastCollection = Time.time - building.lastCollectionTime;
            
            foreach (var production in building.currentProduction)
            {
                float amount = production.Value * (timeSinceLastCollection / 3600f); // 시간당 생산량
                int intAmount = Mathf.FloorToInt(amount);
                
                if (intAmount > 0)
                {
                    collected[production.Key] = intAmount;
                    ResourceManager.Instance.AddResource(production.Key, intAmount, "Manual Collection");
                }
            }
            
            building.lastCollectionTime = Time.time;
            
            return collected;
        }
        
        /// <summary>
        /// 생산 통계 가져오기
        /// </summary>
        public ProductionStatistics GetProductionStatistics()
        {
            return new ProductionStatistics
            {
                CurrentRates = new Dictionary<ResourceType, float>(currentProductionRates),
                HourlyProduction = new Dictionary<ResourceType, float>(hourlyProduction),
                TotalProduced = new Dictionary<ResourceType, float>(totalProduced),
                ActiveBuildingCount = productionBuildings.Count(b => b.Value.isActive),
                TotalBuildingCount = productionBuildings.Count,
                ActiveBuffCount = activeBuffs.Count
            };
        }
        
        /// <summary>
        /// 생산 예측
        /// </summary>
        public Dictionary<ResourceType, int> PredictProduction(float hours)
        {
            var prediction = new Dictionary<ResourceType, int>();
            
            foreach (var rate in currentProductionRates)
            {
                prediction[rate.Key] = Mathf.FloorToInt(rate.Value * hours);
            }
            
            return prediction;
        }
        
        /// <summary>
        /// 이벤트 배율 설정
        /// </summary>
        public void SetEventMultiplier(float multiplier)
        {
            eventMultiplier = multiplier;
            RecalculateAllProduction();
        }
        
        /// <summary>
        /// 생산 보고서 생성
        /// </summary>
        public string GenerateProductionReport()
        {
            string report = "=== 자원 생산 보고서 ===\n\n";
            
            report += "현재 생산률 (시간당):\n";
            foreach (var rate in currentProductionRates)
            {
                report += $"- {rate.Key}: +{rate.Value:F1}/h\n";
            }
            
            report += $"\n활성 생산 건물: {productionBuildings.Count(b => b.Value.isActive)}/{productionBuildings.Count}\n";
            report += $"활성 버프: {activeBuffs.Count}\n";
            
            if (activeBuffs.Count > 0)
            {
                report += "\n활성 버프 목록:\n";
                foreach (var buff in activeBuffs)
                {
                    report += $"- {buff.buffName}: x{buff.multiplier:F1} ({buff.RemainingTime:F0}초 남음)\n";
                }
            }
            
            report += $"\n현재 보너스:\n";
            report += $"- 시간대 보너스: x{GetTimeBonus():F1}\n";
            report += $"- 이벤트 배율: x{eventMultiplier:F1}\n";
            
            return report;
        }
        
        [System.Serializable]
        public class ProductionStatistics
        {
            public Dictionary<ResourceType, float> CurrentRates;
            public Dictionary<ResourceType, float> HourlyProduction;
            public Dictionary<ResourceType, float> TotalProduced;
            public int ActiveBuildingCount;
            public int TotalBuildingCount;
            public int ActiveBuffCount;
        }
    }
}