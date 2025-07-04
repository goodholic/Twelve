using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using GuildMaster.Core;
using GuildMaster.Guild;
using GuildMaster.Data;
using CoreResourceType = GuildMaster.Core.ResourceType;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 자원 생산 시스템
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
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Production Settings")]
        [SerializeField] private float productionInterval = 1f; // 생산 간격 (초)
        [SerializeField] private float globalProductionMultiplier = 1f;
        
        private Dictionary<CoreResourceType, float> productionRates = new Dictionary<CoreResourceType, float>();
        private Dictionary<CoreResourceType, float> productionMultipliers = new Dictionary<CoreResourceType, float>();
        private float lastProductionTime;
        
        // Events
        public event Action<CoreResourceType, int> OnResourceProduced;
        public event Action<Dictionary<CoreResourceType, float>> OnProductionRatesChanged;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeProductionSystem();
        }

        void Start()
        {
            StartCoroutine(ProductionCoroutine());
        }

        void InitializeProductionSystem()
        {
            // 초기 생산률 설정
            foreach (CoreResourceType type in Enum.GetValues(typeof(CoreResourceType)))
            {
                if (type != CoreResourceType.None)
                {
                    productionRates[type] = 0f;
                    productionMultipliers[type] = 1f;
                }
            }
            
            lastProductionTime = Time.time;
        }

        IEnumerator ProductionCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(productionInterval);
                
                ProduceResources();
            }
        }

        void ProduceResources()
        {
            float deltaTime = Time.time - lastProductionTime;
            
            foreach (var kvp in productionRates)
            {
                CoreResourceType type = kvp.Key;
                float rate = kvp.Value;
                
                if (rate > 0f)
                {
                    float multiplier = productionMultipliers[type] * globalProductionMultiplier;
                    int amount = Mathf.RoundToInt(rate * deltaTime * multiplier);
                    
                    if (amount > 0)
                    {
                        ResourceManager.Instance?.AddResource(type, amount, "Production");
                        OnResourceProduced?.Invoke(type, amount);
                    }
                }
            }
            
            lastProductionTime = Time.time;
        }

        public void SetProductionRate(CoreResourceType type, float rate)
        {
            productionRates[type] = Mathf.Max(0f, rate);
            OnProductionRatesChanged?.Invoke(new Dictionary<CoreResourceType, float>(productionRates));
        }

        public void SetProductionMultiplier(CoreResourceType type, float multiplier)
        {
            productionMultipliers[type] = Mathf.Max(0f, multiplier);
        }

        public void SetGlobalProductionMultiplier(float multiplier)
        {
            globalProductionMultiplier = Mathf.Max(0f, multiplier);
        }

        public float GetProductionRate(CoreResourceType type)
        {
            return productionRates.ContainsKey(type) ? productionRates[type] : 0f;
        }

        public float GetProductionMultiplier(CoreResourceType type)
        {
            return productionMultipliers.ContainsKey(type) ? productionMultipliers[type] : 1f;
        }

        public Dictionary<CoreResourceType, float> GetAllProductionRates()
        {
            return new Dictionary<CoreResourceType, float>(productionRates);
        }

        public void UpdateProductionFromBuildings()
        {
            var guildManager = GameManager.Instance?.GuildManager;
            if (guildManager != null)
            {
                var buildings = guildManager.GetBuildingsByType(BuildingType.Farm);
                float foodProduction = buildings.Count * 10f; // 농장당 10/시간
                SetProductionRate(CoreResourceType.Food, foodProduction / 3600f); // 초당 변환
                
                buildings = guildManager.GetBuildingsByType(BuildingType.Mine);
                float stoneProduction = buildings.Count * 8f; // 광산당 8/시간
                SetProductionRate(CoreResourceType.Stone, stoneProduction / 3600f);
                
                buildings = guildManager.GetBuildingsByType(BuildingType.Lumbermill);
                float woodProduction = buildings.Count * 12f; // 제재소당 12/시간
                SetProductionRate(CoreResourceType.Wood, woodProduction / 3600f);
            }
        }

        public string GetProductionReport()
        {
            string report = "=== 생산 현황 ===\n";
            
            foreach (var kvp in productionRates)
            {
                if (kvp.Value > 0f)
                {
                    float hourlyRate = kvp.Value * 3600f;
                    float multiplier = productionMultipliers[kvp.Key] * globalProductionMultiplier;
                    float actualRate = hourlyRate * multiplier;
                    
                    report += $"{kvp.Key}: {actualRate:F1}/시간 (기본: {hourlyRate:F1}, 배율: {multiplier:F2}x)\n";
                }
            }
            
            return report;
        }
    }
} 