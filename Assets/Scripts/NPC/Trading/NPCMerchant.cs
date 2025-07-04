using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle; // JobClass를 위해 추가

namespace GuildMaster.NPC
{
    public enum MerchantType
    {
        Regular,        // 정기 상인
        Rare,          // 희귀 상인
        GuildExclusive, // 길드 전용 상인
        Special        // 특별 거래 상인
    }
    
    public enum TradeItemType
    {
        Resource,
        Equipment,
        Consumable,
        Blueprint,
        SkillBook,
        Adventurer
    }
    
    [System.Serializable]
    public class TradeItem
    {
        public string ItemId { get; set; }
        public string Name { get; set; }
        public TradeItemType Type { get; set; }
        public string Description { get; set; }
        public int Stock { get; set; }
        public int MaxStock { get; set; }
        
        // Costs
        public int GoldCost { get; set; }
        public int WoodCost { get; set; }
        public int StoneCost { get; set; }
        public int ManaStoneCost { get; set; }
        public int ReputationRequired { get; set; }
        
        // For resource trades
        public string ResourceType { get; set; }
        public int ResourceAmount { get; set; }
        
        // For equipment/items
        public int ItemLevel { get; set; }
        public JobClass? RequiredClass { get; set; }
        
        public bool CanPurchase(Core.ResourceManager resourceManager, int currentReputation)
        {
            if (Stock <= 0) return false;
            if (currentReputation < ReputationRequired) return false;
            return resourceManager.CanAfford(GoldCost, WoodCost, StoneCost, ManaStoneCost);
        }
    }
    
    [System.Serializable]
    public class NPCMerchant
    {
        public string MerchantId { get; set; }
        public string Name { get; set; }
        public MerchantType Type { get; set; }
        public List<TradeItem> Inventory { get; set; }
        public float AppearanceChance { get; set; }
        public int MinGuildLevel { get; set; }
        public DateTime LastVisitTime { get; set; }
        public float VisitDuration { get; set; } // Hours
        
        // Special conditions
        public DayOfWeek? PreferredDay { get; set; }
        public string SpecialEventId { get; set; }
        
        public NPCMerchant(string id, string name, MerchantType type)
        {
            MerchantId = id;
            Name = name;
            Type = type;
            Inventory = new List<TradeItem>();
            AppearanceChance = GetDefaultAppearanceChance(type);
            VisitDuration = GetDefaultVisitDuration(type);
        }
        
        float GetDefaultAppearanceChance(MerchantType type)
        {
            switch (type)
            {
                case MerchantType.Regular: return 1f; // 100% chance
                case MerchantType.Rare: return 0.1f; // 10% chance
                case MerchantType.GuildExclusive: return 0.5f; // 50% chance
                case MerchantType.Special: return 0.05f; // 5% chance
                default: return 0.5f;
            }
        }
        
        float GetDefaultVisitDuration(MerchantType type)
        {
            switch (type)
            {
                case MerchantType.Regular: return 24f; // 24 hours
                case MerchantType.Rare: return 6f; // 6 hours
                case MerchantType.GuildExclusive: return 12f; // 12 hours
                case MerchantType.Special: return 3f; // 3 hours
                default: return 12f;
            }
        }
        
        public void RefreshStock()
        {
            foreach (var item in Inventory)
            {
                item.Stock = item.MaxStock;
                
                // Add some randomness to stock
                if (Type == MerchantType.Rare || Type == MerchantType.Special)
                {
                    item.Stock = UnityEngine.Random.Range(1, item.MaxStock + 1);
                }
            }
        }
        
        public bool IsAvailable()
        {
            if (LastVisitTime == DateTime.MinValue) return false;
            
            TimeSpan timeSinceVisit = DateTime.Now - LastVisitTime;
            return timeSinceVisit.TotalHours < VisitDuration;
        }
    }
    
    public class MerchantManager : MonoBehaviour
    {
        // Merchant Templates
        private List<NPCMerchant> merchantTemplates;
        private List<NPCMerchant> activeMerchants;
        
        // Events
        public event Action<NPCMerchant> OnMerchantArrived;
        public event Action<NPCMerchant> OnMerchantDeparted;
        public event Action<NPCMerchant, TradeItem> OnItemPurchased;
        
        // Check interval
        private float merchantCheckInterval = 3600f; // Check every hour
        private float lastCheckTime;
        
        void Awake()
        {
            merchantTemplates = new List<NPCMerchant>();
            activeMerchants = new List<NPCMerchant>();
            InitializeMerchantTemplates();
        }
        
        void InitializeMerchantTemplates()
        {
            // Regular Merchants
            var regularMerchant = new NPCMerchant("merchant_regular_1", "떠돌이 상인", MerchantType.Regular);
            regularMerchant.Inventory.Add(new TradeItem
            {
                ItemId = "trade_wood_for_gold",
                Name = "목재 거래",
                Type = TradeItemType.Resource,
                Description = "골드 50으로 목재 100 구매",
                GoldCost = 50,
                ResourceType = "Wood",
                ResourceAmount = 100,
                Stock = 10,
                MaxStock = 10
            });
            regularMerchant.Inventory.Add(new TradeItem
            {
                ItemId = "trade_stone_for_gold",
                Name = "석재 거래",
                Type = TradeItemType.Resource,
                Description = "골드 75로 석재 100 구매",
                GoldCost = 75,
                ResourceType = "Stone",
                ResourceAmount = 100,
                Stock = 10,
                MaxStock = 10
            });
            merchantTemplates.Add(regularMerchant);
            
            // Rare Merchant
            var rareMerchant = new NPCMerchant("merchant_rare_1", "신비한 상인", MerchantType.Rare);
            rareMerchant.MinGuildLevel = 5;
            rareMerchant.Inventory.Add(new TradeItem
            {
                ItemId = "rare_skillbook_warrior",
                Name = "전사의 비전서",
                Type = TradeItemType.SkillBook,
                Description = "전사 계열 고급 스킬 습득",
                GoldCost = 500,
                ManaStoneCost = 50,
                RequiredClass = JobClass.Warrior,
                Stock = 1,
                MaxStock = 1,
                ReputationRequired = 100
            });
            rareMerchant.Inventory.Add(new TradeItem
            {
                ItemId = "rare_blueprint_advanced_armory",
                Name = "고급 무기고 설계도",
                Type = TradeItemType.Blueprint,
                Description = "무기고 레벨 상한 증가",
                GoldCost = 1000,
                StoneCost = 500,
                Stock = 1,
                MaxStock = 1,
                ReputationRequired = 200
            });
            merchantTemplates.Add(rareMerchant);
            
            // Guild Exclusive Merchant
            var guildMerchant = new NPCMerchant("merchant_guild_1", "길드 연합 상인", MerchantType.GuildExclusive);
            guildMerchant.MinGuildLevel = 3;
            guildMerchant.Inventory.Add(new TradeItem
            {
                ItemId = "guild_adventurer_recruit",
                Name = "숙련된 모험가 추천서",
                Type = TradeItemType.Adventurer,
                Description = "레벨 5 모험가 영입",
                GoldCost = 300,
                ItemLevel = 5,
                Stock = 3,
                MaxStock = 3,
                ReputationRequired = 50
            });
            merchantTemplates.Add(guildMerchant);
            
            // Special Event Merchant
            var specialMerchant = new NPCMerchant("merchant_special_1", "축제 상인", MerchantType.Special);
            specialMerchant.SpecialEventId = "seasonal_festival";
            specialMerchant.Inventory.Add(new TradeItem
            {
                ItemId = "special_manastone_bundle",
                Name = "마나스톤 꾸러미",
                Type = TradeItemType.Resource,
                Description = "마나스톤 100개 특별 할인",
                GoldCost = 200,
                ResourceType = "ManaStone",
                ResourceAmount = 100,
                Stock = 5,
                MaxStock = 5
            });
            merchantTemplates.Add(specialMerchant);
        }
        
        void Update()
        {
            if (Time.time - lastCheckTime >= merchantCheckInterval)
            {
                CheckMerchantArrivals();
                CheckMerchantDepartures();
                lastCheckTime = Time.time;
            }
        }
        
        void CheckMerchantArrivals()
        {
            var guildManager = Core.GameManager.Instance?.GuildManager;
            if (guildManager == null) return;
            
            int guildLevel = guildManager.GetGuildData().GuildLevel;
            
            foreach (var template in merchantTemplates)
            {
                // Check if merchant is already active
                if (activeMerchants.Any(m => m.MerchantId == template.MerchantId))
                    continue;
                
                // Check guild level requirement
                if (guildLevel < template.MinGuildLevel)
                    continue;
                
                // Check special conditions
                if (template.PreferredDay.HasValue && DateTime.Now.DayOfWeek != template.PreferredDay.Value)
                    continue;
                
                // Check appearance chance
                if (UnityEngine.Random.value <= template.AppearanceChance)
                {
                    // Create a copy of the template
                    var merchant = CreateMerchantInstance(template);
                    merchant.LastVisitTime = DateTime.Now;
                    merchant.RefreshStock();
                    
                    activeMerchants.Add(merchant);
                    OnMerchantArrived?.Invoke(merchant);
                }
            }
        }
        
        void CheckMerchantDepartures()
        {
            List<NPCMerchant> departingMerchants = new List<NPCMerchant>();
            
            foreach (var merchant in activeMerchants)
            {
                if (!merchant.IsAvailable())
                {
                    departingMerchants.Add(merchant);
                }
            }
            
            foreach (var merchant in departingMerchants)
            {
                activeMerchants.Remove(merchant);
                OnMerchantDeparted?.Invoke(merchant);
            }
        }
        
        NPCMerchant CreateMerchantInstance(NPCMerchant template)
        {
            var instance = new NPCMerchant(template.MerchantId, template.Name, template.Type)
            {
                AppearanceChance = template.AppearanceChance,
                MinGuildLevel = template.MinGuildLevel,
                VisitDuration = template.VisitDuration,
                PreferredDay = template.PreferredDay,
                SpecialEventId = template.SpecialEventId
            };
            
            // Copy inventory
            foreach (var item in template.Inventory)
            {
                instance.Inventory.Add(new TradeItem
                {
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Type = item.Type,
                    Description = item.Description,
                    Stock = item.Stock,
                    MaxStock = item.MaxStock,
                    GoldCost = item.GoldCost,
                    WoodCost = item.WoodCost,
                    StoneCost = item.StoneCost,
                    ManaStoneCost = item.ManaStoneCost,
                    ReputationRequired = item.ReputationRequired,
                    ResourceType = item.ResourceType,
                    ResourceAmount = item.ResourceAmount,
                    ItemLevel = item.ItemLevel,
                    RequiredClass = item.RequiredClass
                });
            }
            
            return instance;
        }
        
        public bool PurchaseItem(NPCMerchant merchant, TradeItem item)
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return false;
            
            var resourceManager = gameManager.ResourceManager;
            var guildManager = gameManager.GuildManager;
            
            int reputation = guildManager.GetGuildData().GuildReputation;
            
            if (!item.CanPurchase(resourceManager, reputation))
                return false;
            
            // Deduct costs
            resourceManager.SpendResources(item.GoldCost, item.WoodCost, item.StoneCost, item.ManaStoneCost);
            
            // Apply item effects
            ApplyItemEffects(item);
            
            // Reduce stock
            item.Stock--;
            
            OnItemPurchased?.Invoke(merchant, item);
            
            return true;
        }
        
        void ApplyItemEffects(TradeItem item)
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return;
            
            switch (item.Type)
            {
                case TradeItemType.Resource:
                    ApplyResourceTrade(item);
                    break;
                    
                case TradeItemType.Adventurer:
                    RecruitAdventurer(item);
                    break;
                    
                case TradeItemType.SkillBook:
                    // TODO: Apply skill book to adventurers
                    break;
                    
                case TradeItemType.Blueprint:
                    // TODO: Unlock building upgrades
                    break;
                    
                case TradeItemType.Equipment:
                    // TODO: Add equipment to inventory
                    break;
            }
        }
        
        void ApplyResourceTrade(TradeItem item)
        {
            var resourceManager = Core.GameManager.Instance?.ResourceManager;
            if (resourceManager == null) return;
            
            switch (item.ResourceType)
            {
                case "Gold":
                    resourceManager.AddGold(item.ResourceAmount);
                    break;
                case "Wood":
                    resourceManager.AddWood(item.ResourceAmount);
                    break;
                case "Stone":
                    resourceManager.AddStone(item.ResourceAmount);
                    break;
                case "ManaStone":
                    resourceManager.AddManaStone(item.ResourceAmount);
                    break;
            }
        }
        
        void RecruitAdventurer(TradeItem item)
        {
            var guildManager = Core.GameManager.Instance?.GuildManager;
            if (guildManager == null) return;
            
            // Generate random adventurer with specified level
            JobClass randomClass = item.RequiredClass ?? (JobClass)UnityEngine.Random.Range(0, 7);
            string name = GenerateAdventurerName();
            
            Unit adventurer = new Unit(name, item.ItemLevel, randomClass);
            guildManager.RecruitAdventurer(adventurer);
        }
        
        string GenerateAdventurerName()
        {
            string[] firstNames = { "알렉스", "블레이크", "케이시", "드류", "엘리스", "핀", "그레이", "하퍼" };
            string[] titles = { "용감한", "신속한", "현명한", "강인한", "교활한", "용맹한", "강력한", "침묵의" };
            
            return $"{titles[UnityEngine.Random.Range(0, titles.Length)]} {firstNames[UnityEngine.Random.Range(0, firstNames.Length)]}";
        }
        
        public List<NPCMerchant> GetActiveMerchants()
        {
            return new List<NPCMerchant>(activeMerchants);
        }
        
        public void ForceSpawnMerchant(string merchantId)
        {
            var template = merchantTemplates.FirstOrDefault(m => m.MerchantId == merchantId);
            if (template != null && !activeMerchants.Any(m => m.MerchantId == merchantId))
            {
                var merchant = CreateMerchantInstance(template);
                merchant.LastVisitTime = DateTime.Now;
                merchant.RefreshStock();
                activeMerchants.Add(merchant);
                OnMerchantArrived?.Invoke(merchant);
            }
        }
    }
}