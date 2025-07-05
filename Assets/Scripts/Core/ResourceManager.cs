using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GuildMaster.Core
{
    public enum ResourceType
    {
        Gold,
        Wood,
        Stone,
        ManaStone,
        Food,
        Energy,
        Experience,
        Mana
    }
    
    public class ResourceManager : MonoBehaviour
    {
        private static ResourceManager _instance;
        public static ResourceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ResourceManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ResourceManager");
                        _instance = go.AddComponent<ResourceManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Resource Types
        [System.Serializable]
        public class Resources
        {
            public int Gold { get; set; }
            public int Wood { get; set; }
            public int Stone { get; set; }
            public int ManaStone { get; set; }
            public int Reputation { get; set; }
            
            public int GetResource(ResourceType type)
            {
                switch (type)
                {
                    case ResourceType.Gold: return Gold;
                    case ResourceType.Wood: return Wood;
                    case ResourceType.Stone: return Stone;
                    case ResourceType.ManaStone: return ManaStone;
                    case ResourceType.Food: return 0; // Not tracked in this system
                    case ResourceType.Energy: return 0; // Not tracked in this system
                    case ResourceType.Experience: return 0; // Not tracked in this system
                    case ResourceType.Mana: return ManaStone; // Mana is same as ManaStone
                    default: return 0;
                }
            }
            
            public void SetResource(ResourceType type, int value)
            {
                switch (type)
                {
                    case ResourceType.Gold: Gold = value; break;
                    case ResourceType.Wood: Wood = value; break;
                    case ResourceType.Stone: Stone = value; break;
                    case ResourceType.ManaStone: ManaStone = value; break;
                }
            }
        }

        // Storage Limits
        [System.Serializable]
        public class ResourceLimits
        {
            public int MaxGold { get; set; } = 10000;
            public int MaxWood { get; set; } = 1000;
            public int MaxStone { get; set; } = 1000;
            public int MaxManaStone { get; set; } = 500;
            
            public int GetLimit(ResourceType type)
            {
                switch (type)
                {
                    case ResourceType.Gold: return MaxGold;
                    case ResourceType.Wood: return MaxWood;
                    case ResourceType.Stone: return MaxStone;
                    case ResourceType.ManaStone: return MaxManaStone;
                    default: return 0;
                }
            }
            
            public void SetLimit(ResourceType type, int value)
            {
                switch (type)
                {
                    case ResourceType.Gold: MaxGold = value; break;
                    case ResourceType.Wood: MaxWood = value; break;
                    case ResourceType.Stone: MaxStone = value; break;
                    case ResourceType.ManaStone: MaxManaStone = value; break;
                }
            }
        }

        // Current Resources
        private Resources currentResources;
        private ResourceLimits resourceLimits;

        // Production Rates (per minute)
        private float goldProductionRate = 0f;
        private float woodProductionRate = 0f;
        private float stoneProductionRate = 0f;
        private float manaStoneProductionRate = 0f;
        
        // Resource Income Modifiers
        private float globalProductionMultiplier = 1f;
        private Dictionary<ResourceType, float> resourceMultipliers = new Dictionary<ResourceType, float>();
        
        // Auto-production
        private float lastProductionTime;
        private bool isAutoProductionEnabled = true;
        
        // Resource History
        private class ResourceTransaction
        {
            public float Timestamp { get; set; }
            public ResourceType Type { get; set; }
            public int Amount { get; set; }
            public string Source { get; set; }
        }
        private Queue<ResourceTransaction> transactionHistory = new Queue<ResourceTransaction>();
        private const int MAX_TRANSACTION_HISTORY = 100;

        // Events
        public event Action<Resources> OnResourcesChanged;
        public event Action<int> OnGoldChanged;
        public event Action<int> OnWoodChanged;
        public event Action<int> OnStoneChanged;
        public event Action<int> OnManaStoneChanged;
        public event Action<int> OnReputationChanged;

        void Awake()
        {
            currentResources = new Resources();
            resourceLimits = new ResourceLimits();
            
            // Initialize multipliers
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                resourceMultipliers[type] = 1f;
            }
            
            lastProductionTime = Time.time;
        }

        public IEnumerator Initialize()
        {
            // Set starting resources
            currentResources.Gold = 500;
            currentResources.Wood = 100;
            currentResources.Stone = 50;
            currentResources.ManaStone = 20;
            currentResources.Reputation = 0;

            OnResourcesChanged?.Invoke(currentResources);
            
            // Start auto-production coroutine
            StartCoroutine(AutoProductionCoroutine());
            
            yield return null;
        }
        
        IEnumerator AutoProductionCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f); // Check every second
                
                if (isAutoProductionEnabled)
                {
                    float deltaTime = Time.time - lastProductionTime;
                    if (deltaTime >= 60f) // Production every minute
                    {
                        ProduceResources();
                        lastProductionTime = Time.time;
                    }
                }
            }
        }
        
        void ProduceResources()
        {
            // Calculate actual production with modifiers
            int goldProduction = Mathf.RoundToInt(goldProductionRate * globalProductionMultiplier * resourceMultipliers[ResourceType.Gold]);
            int woodProduction = Mathf.RoundToInt(woodProductionRate * globalProductionMultiplier * resourceMultipliers[ResourceType.Wood]);
            int stoneProduction = Mathf.RoundToInt(stoneProductionRate * globalProductionMultiplier * resourceMultipliers[ResourceType.Stone]);
            int manaStoneProduction = Mathf.RoundToInt(manaStoneProductionRate * globalProductionMultiplier * resourceMultipliers[ResourceType.ManaStone]);
            
            // Add resources
            if (goldProduction > 0) AddResource(ResourceType.Gold, goldProduction, "Auto Production");
            if (woodProduction > 0) AddResource(ResourceType.Wood, woodProduction, "Auto Production");
            if (stoneProduction > 0) AddResource(ResourceType.Stone, stoneProduction, "Auto Production");
            if (manaStoneProduction > 0) AddResource(ResourceType.ManaStone, manaStoneProduction, "Auto Production");
        }

        // Resource Management
        public bool CanAfford(int gold, int wood, int stone, int manaStone)
        {
            return currentResources.Gold >= gold &&
                   currentResources.Wood >= wood &&
                   currentResources.Stone >= stone &&
                   currentResources.ManaStone >= manaStone;
        }

        public void SpendResources(int gold, int wood, int stone, int manaStone)
        {
            if (!CanAfford(gold, wood, stone, manaStone))
            {
                Debug.LogWarning("Insufficient resources!");
                return;
            }

            currentResources.Gold -= gold;
            currentResources.Wood -= wood;
            currentResources.Stone -= stone;
            currentResources.ManaStone -= manaStone;

            // Record transactions
            if (gold > 0) RecordTransaction(ResourceType.Gold, -gold, "Spend");
            if (wood > 0) RecordTransaction(ResourceType.Wood, -wood, "Spend");
            if (stone > 0) RecordTransaction(ResourceType.Stone, -stone, "Spend");
            if (manaStone > 0) RecordTransaction(ResourceType.ManaStone, -manaStone, "Spend");

            OnResourcesChanged?.Invoke(currentResources);
        }
        
        public bool SpendGold(int amount)
        {
            if (currentResources.Gold >= amount)
            {
                currentResources.Gold -= amount;
                RecordTransaction(ResourceType.Gold, -amount, "Spend");
                OnResourcesChanged?.Invoke(currentResources);
                OnGoldChanged?.Invoke(currentResources.Gold);
                return true;
            }
            return false;
        }

        // Add Resources
        public void AddGold(int amount)
        {
            AddResource(ResourceType.Gold, amount, "Manual Addition");
        }
        
        public void AddGems(int amount)
        {
            AddResource(ResourceType.ManaStone, amount, "Gems Addition");
        }
        
        public void AddFood(int amount)
        {
            // Food is not tracked in current system, but method exists for compatibility
            Debug.Log($"AddFood called with amount: {amount} (not tracked in current system)");
        }
        
        public void AddWood(int amount)
        {
            AddResource(ResourceType.Wood, amount, "Manual Addition");
        }
        
        public void AddStone(int amount)
        {
            AddResource(ResourceType.Stone, amount, "Manual Addition");
        }
        
        public void AddManaStone(int amount)
        {
            AddResource(ResourceType.ManaStone, amount, "Manual Addition");
        }

        public void AddResources(Dictionary<string, int> resources)
        {
            foreach (var resource in resources)
            {
                switch (resource.Key.ToLower())
                {
                    case "gold":
                        AddResource(ResourceType.Gold, resource.Value, "Refund");
                        break;
                    case "wood":
                        AddResource(ResourceType.Wood, resource.Value, "Refund");
                        break;
                    case "stone":
                        AddResource(ResourceType.Stone, resource.Value, "Refund");
                        break;
                    case "mana":
                    case "manastone":
                        AddResource(ResourceType.ManaStone, resource.Value, "Refund");
                        break;
                }
            }
        }

        public void AddResource(ResourceType type, int amount, string source = "Unknown")
        {
            int previousAmount = currentResources.GetResource(type);
            int limit = resourceLimits.GetLimit(type);
            int newAmount = Mathf.Clamp(previousAmount + amount, 0, limit);
            
            currentResources.SetResource(type, newAmount);
            
            if (newAmount != previousAmount)
            {
                // Record transaction
                RecordTransaction(type, newAmount - previousAmount, source);
                
                // Fire events
                switch (type)
                {
                    case ResourceType.Gold:
                        OnGoldChanged?.Invoke(newAmount);
                        break;
                    case ResourceType.Wood:
                        OnWoodChanged?.Invoke(newAmount);
                        break;
                    case ResourceType.Stone:
                        OnStoneChanged?.Invoke(newAmount);
                        break;
                    case ResourceType.ManaStone:
                        OnManaStoneChanged?.Invoke(newAmount);
                        break;
                }
                
                OnResourcesChanged?.Invoke(currentResources);
            }
        }


        public void AddReputation(int amount)
        {
            currentResources.Reputation += amount;
            OnReputationChanged?.Invoke(currentResources.Reputation);
            OnResourcesChanged?.Invoke(currentResources);
        }

        // Production Rate Management
        public void SetGoldProductionRate(float rate)
        {
            goldProductionRate = rate;
        }

        public void SetWoodProductionRate(float rate)
        {
            woodProductionRate = rate;
        }

        public void SetStoneProductionRate(float rate)
        {
            stoneProductionRate = rate;
        }

        public void SetManaStoneProductionRate(float rate)
        {
            manaStoneProductionRate = rate;
        }

        // Increase Storage Limits
        public void IncreaseGoldLimit(int amount)
        {
            resourceLimits.MaxGold += amount;
        }

        public void IncreaseWoodLimit(int amount)
        {
            resourceLimits.MaxWood += amount;
        }

        public void IncreaseStoneLimit(int amount)
        {
            resourceLimits.MaxStone += amount;
        }

        public void IncreaseManaStoneLimit(int amount)
        {
            resourceLimits.MaxManaStone += amount;
        }

        // Trading
        public bool TradeResources(int goldCost, int woodCost, int stoneCost, int manaStoneCost,
                                  int goldGain, int woodGain, int stoneGain, int manaStoneGain)
        {
            // Check if player can afford the trade
            if (!CanAfford(goldCost, woodCost, stoneCost, manaStoneCost))
            {
                return false;
            }

            // Check if gains would exceed limits
            if ((currentResources.Gold - goldCost + goldGain) > resourceLimits.MaxGold ||
                (currentResources.Wood - woodCost + woodGain) > resourceLimits.MaxWood ||
                (currentResources.Stone - stoneCost + stoneGain) > resourceLimits.MaxStone ||
                (currentResources.ManaStone - manaStoneCost + manaStoneGain) > resourceLimits.MaxManaStone)
            {
                Debug.LogWarning("Trade would exceed storage limits!");
                return false;
            }

            // Execute trade
            SpendResources(goldCost, woodCost, stoneCost, manaStoneCost);
            
            if (goldGain > 0) AddGold(goldGain);
            if (woodGain > 0) AddWood(woodGain);
            if (stoneGain > 0) AddStone(stoneGain);
            if (manaStoneGain > 0) AddManaStone(manaStoneGain);

            return true;
        }

        // Getters
        public Resources GetResources() => currentResources;
        public ResourceLimits GetResourceLimits() => resourceLimits;
        public int GetGold() => currentResources.Gold;
        public int GetWood() => currentResources.Wood;
        public int GetStone() => currentResources.Stone;
        public int GetManaStone() => currentResources.ManaStone;
        public int GetReputation() => currentResources.Reputation;
        
        // Get resource by type
        public int GetResource(ResourceType type)
        {
            return currentResources.GetResource(type);
        }

        // Production Rates
        public float GetGoldProductionRate() => goldProductionRate;
        public float GetWoodProductionRate() => woodProductionRate;
        public float GetStoneProductionRate() => stoneProductionRate;
        public float GetManaStoneProductionRate() => manaStoneProductionRate;

        // Resource Modifiers
        public void SetGlobalProductionMultiplier(float multiplier)
        {
            globalProductionMultiplier = Mathf.Max(0f, multiplier);
        }
        
        public void SetResourceMultiplier(ResourceType type, float multiplier)
        {
            resourceMultipliers[type] = Mathf.Max(0f, multiplier);
        }
        
        public float GetResourceMultiplier(ResourceType type)
        {
            return resourceMultipliers.ContainsKey(type) ? resourceMultipliers[type] : 1f;
        }
        
        // Production Management
        public void UpdateProductionRates()
        {
            // Get production from buildings
            var guildManager = GameManager.Instance?.GuildManager;
            if (guildManager != null)
            {
                var production = guildManager.GetResourceProduction();
                
                goldProductionRate = production.ContainsKey(ResourceType.Gold) ? production[ResourceType.Gold] : 0f;
                woodProductionRate = production.ContainsKey(ResourceType.Wood) ? production[ResourceType.Wood] : 0f;
                stoneProductionRate = production.ContainsKey(ResourceType.Stone) ? production[ResourceType.Stone] : 0f;
                manaStoneProductionRate = production.ContainsKey(ResourceType.ManaStone) ? production[ResourceType.ManaStone] : 0f;
            }
        }
        
        // Transaction History
        void RecordTransaction(ResourceType type, int amount, string source)
        {
            var transaction = new ResourceTransaction
            {
                Timestamp = Time.time,
                Type = type,
                Amount = amount,
                Source = source
            };
            
            transactionHistory.Enqueue(transaction);
            
            // Keep history size limited
            while (transactionHistory.Count > MAX_TRANSACTION_HISTORY)
            {
                transactionHistory.Dequeue();
            }
        }
        
        public Dictionary<ResourceType, int> GetRecentProduction(float timeWindow = 300f) // Last 5 minutes
        {
            var production = new Dictionary<ResourceType, int>();
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                production[type] = 0;
            }
            
            float cutoffTime = Time.time - timeWindow;
            foreach (var transaction in transactionHistory)
            {
                if (transaction.Timestamp >= cutoffTime && transaction.Amount > 0)
                {
                    production[transaction.Type] += transaction.Amount;
                }
            }
            
            return production;
        }
        
        // Market Value System
        public float GetResourceMarketValue(ResourceType type)
        {
            // Base values
            float baseValue = type switch
            {
                ResourceType.Gold => 1f,
                ResourceType.Wood => 2f,
                ResourceType.Stone => 3f,
                ResourceType.ManaStone => 10f,
                _ => 1f
            };
            
            // Adjust based on scarcity
            float currentAmount = currentResources.GetResource(type);
            float maxAmount = resourceLimits.GetLimit(type);
            float scarcityMultiplier = 1f + (1f - currentAmount / maxAmount) * 0.5f;
            
            return baseValue * scarcityMultiplier;
        }
        
        // Exchange System
        public bool ExchangeResources(ResourceType from, int fromAmount, ResourceType to)
        {
            if (currentResources.GetResource(from) < fromAmount)
                return false;
            
            float fromValue = GetResourceMarketValue(from) * fromAmount;
            float toValue = GetResourceMarketValue(to);
            int toAmount = Mathf.FloorToInt(fromValue / toValue * 0.9f); // 10% exchange fee
            
            if (toAmount <= 0)
                return false;
            
            AddResource(from, -fromAmount, "Exchange");
            AddResource(to, toAmount, "Exchange");
            
            return true;
        }
        
        // Warehouse System
        public float GetStorageUtilization()
        {
            float totalCurrent = currentResources.Gold + currentResources.Wood + 
                               currentResources.Stone + currentResources.ManaStone;
            float totalMax = resourceLimits.MaxGold + resourceLimits.MaxWood + 
                            resourceLimits.MaxStone + resourceLimits.MaxManaStone;
            
            return totalMax > 0 ? totalCurrent / totalMax : 0f;
        }
        
        public void OptimizeStorage()
        {
            // Automatically adjust storage limits based on usage patterns
            var recentProduction = GetRecentProduction();
            
            foreach (var kvp in recentProduction)
            {
                var type = kvp.Key;
                var production = kvp.Value;
                
                // If we're producing a lot of a resource, increase its storage
                if (production > resourceLimits.GetLimit(type) * 0.8f)
                {
                    int newLimit = Mathf.RoundToInt(resourceLimits.GetLimit(type) * 1.5f);
                    resourceLimits.SetLimit(type, newLimit);
                }
            }
        }
        
        // Debug Methods
        public void AddDebugResources()
        {
            AddResource(ResourceType.Gold, 1000, "Debug");
            AddResource(ResourceType.Wood, 500, "Debug");
            AddResource(ResourceType.Stone, 500, "Debug");
            AddResource(ResourceType.ManaStone, 200, "Debug");
            Debug.Log("Debug resources added!");
        }
        
        public void SetAutoProduction(bool enabled)
        {
            isAutoProductionEnabled = enabled;
        }
        
        public string GetResourceReport()
        {
            string report = "=== 자원 현황 ===\n";
            report += $"골드: {currentResources.Gold}/{resourceLimits.MaxGold}\n";
            report += $"목재: {currentResources.Wood}/{resourceLimits.MaxWood}\n";
            report += $"석재: {currentResources.Stone}/{resourceLimits.MaxStone}\n";
            report += $"마나스톤: {currentResources.ManaStone}/{resourceLimits.MaxManaStone}\n";
            report += $"\n=== 생산량 (분당) ===\n";
            report += $"골드: +{goldProductionRate * globalProductionMultiplier * resourceMultipliers[ResourceType.Gold]:F1}\n";
            report += $"목재: +{woodProductionRate * globalProductionMultiplier * resourceMultipliers[ResourceType.Wood]:F1}\n";
            report += $"석재: +{stoneProductionRate * globalProductionMultiplier * resourceMultipliers[ResourceType.Stone]:F1}\n";
            report += $"마나스톤: +{manaStoneProductionRate * globalProductionMultiplier * resourceMultipliers[ResourceType.ManaStone]:F1}\n";
            report += $"\n창고 사용률: {GetStorageUtilization():P0}";
            
            return report;
        }
    }
}