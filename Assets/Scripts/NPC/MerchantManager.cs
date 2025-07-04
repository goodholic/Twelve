using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Core;
using GuildMaster.Equipment;
using GuildMaster.Battle;

namespace GuildMaster.NPC
{
    /// <summary>
    /// 상점 시스템과 상인 NPC를 관리하는 매니저 클래스
    /// </summary>
    public class MerchantManager : MonoBehaviour
    {
        [Header("Merchant Settings")]
        public List<Merchant> merchants = new List<Merchant>();
        public ShopSettings shopSettings;

        [Header("Shop Data")]
        public List<ShopItem> globalShopItems = new List<ShopItem>();
        public Dictionary<string, List<ShopItem>> merchantInventories = new Dictionary<string, List<ShopItem>>();

        private static MerchantManager _instance;
        public static MerchantManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MerchantManager>();
                }
                return _instance;
            }
        }

        [System.Serializable]
        public class Merchant
        {
            public string merchantId;
            public string merchantName;
            public MerchantType merchantType;
            public string description;
            public Sprite portrait;
            public Vector3 position;
            public string sceneLocation;
            
            [Header("Shop Configuration")]
            public List<ShopItem.ShopItemCategory> allowedCategories = new List<ShopItem.ShopItemCategory>();
            public int maxInventorySize = 20;
            public float restockInterval = 86400f; // 24시간
            public float lastRestockTime;
            
            [Header("Reputation")]
            public int reputationLevel = 0;
            public float discountPercentage = 0f;
            public List<string> exclusiveItems = new List<string>();

            [Header("Trading")]
            public bool acceptsBartering = false;
            public List<ItemExchangeRate> exchangeRates = new List<ItemExchangeRate>();
            public Currency acceptedCurrency = Currency.Gold;

            public enum MerchantType
            {
                General,
                Weapons,
                Armor,
                Accessories,
                Consumables,
                Materials,
                Rare,
                Special,
                Guild,
                Black_Market
            }

            public enum Currency
            {
                Gold,
                Silver,
                Gems,
                GuildPoints,
                Special
            }

            [System.Serializable]
            public class ItemExchangeRate
            {
                public string itemId;
                public string requiredItemId;
                public int requiredQuantity;
                public float exchangeRate;
            }
        }

        [System.Serializable]
        public class ShopItem
        {
            public string itemId;
            public string itemName;
            public ShopItemCategory category;
            public ItemType itemType;
            public Sprite icon;
            public string description;
            
            [Header("Pricing")]
            public int basePrice;
            public int currentPrice;
            public Merchant.Currency currency;
            public float priceVariation = 0.1f;
            
            [Header("Stock")]
            public int stock;
            public int maxStock;
            public bool infiniteStock;
            public float restockRate = 1f;
            
            [Header("Availability")]
            public bool isAvailable = true;
            public int requiredReputationLevel = 0;
            public List<string> requiredItems = new List<string>();
            public List<string> requiredQuests = new List<string>();
            
            [Header("Special Properties")]
            public bool isLimited = false;
            public bool isDailySpecial = false;
            public DateTime availabilityStartTime;
            public DateTime availabilityEndTime;
            public float specialDiscountPercentage = 0f;

            public enum ShopItemCategory
            {
                Weapons,
                Armor,
                Accessories,
                Consumables,
                Materials,
                Books,
                Tools,
                Decoration,
                Special,
                Quest
            }

            public enum ItemType
            {
                Equipment,
                Consumable,
                Material,
                KeyItem,
                Currency,
                Recipe,
                Book,
                Tool
            }

            public bool CanPurchase(GuildMaster.Battle.Unit buyer, int quantity = 1)
            {
                return isAvailable &&
                       stock >= quantity &&
                       buyer.level >= requiredReputationLevel &&
                       CheckRequiredItems(buyer) &&
                       CheckRequiredQuests(buyer) &&
                       CheckTimeAvailability();
            }

            private bool CheckRequiredItems(GuildMaster.Battle.Unit buyer)
            {
                // 필요한 아이템 체크 로직
                return true; // 임시로 true 반환
            }

            private bool CheckRequiredQuests(GuildMaster.Battle.Unit buyer)
            {
                // 필요한 퀘스트 체크 로직
                return true; // 임시로 true 반환
            }

            private bool CheckTimeAvailability()
            {
                if (!isLimited) return true;
                
                DateTime now = DateTime.Now;
                return now >= availabilityStartTime && now <= availabilityEndTime;
            }

            public int GetFinalPrice(Merchant merchant, GuildMaster.Battle.Unit buyer)
            {
                float finalPrice = currentPrice;
                
                // 상인 할인 적용
                finalPrice *= (1f - merchant.discountPercentage);
                
                // 특별 할인 적용
                if (specialDiscountPercentage > 0)
                {
                    finalPrice *= (1f - specialDiscountPercentage);
                }
                
                // 평판 할인 적용 (예시)
                float reputationDiscount = Mathf.Min(merchant.reputationLevel * 0.01f, 0.2f);
                finalPrice *= (1f - reputationDiscount);
                
                return Mathf.RoundToInt(finalPrice);
            }
        }

        [System.Serializable]
        public class ShopSettings
        {
            public float globalPriceMultiplier = 1f;
            public float sellPriceMultiplier = 0.5f;
            public bool enablePriceFluctuation = true;
            public float maxPriceVariation = 0.2f;
            public float restockFrequency = 24f; // 시간
            public int maxMerchantReputation = 100;
        }

        // Events
        public static event Action<Merchant, ShopItem, int> OnItemPurchased;
        public static event Action<Merchant, ShopItem, int> OnItemSold;
        public static event Action<Merchant> OnMerchantRestocked;
        public static event Action<Merchant, int> OnReputationChanged;

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                InitializeMerchants();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            LoadMerchantData();
            StartCoroutine(UpdateMerchants());
        }

        void InitializeMerchants()
        {
            if (shopSettings == null)
            {
                shopSettings = new ShopSettings();
            }

            foreach (var merchant in merchants)
            {
                if (!merchantInventories.ContainsKey(merchant.merchantId))
                {
                    merchantInventories[merchant.merchantId] = new List<ShopItem>();
                    GenerateInitialInventory(merchant);
                }
            }
        }

        System.Collections.IEnumerator UpdateMerchants()
        {
            while (true)
            {
                yield return new WaitForSeconds(3600f); // 1시간마다 체크
                
                foreach (var merchant in merchants)
                {
                    UpdateMerchantInventory(merchant);
                    CheckForRestock(merchant);
                }
            }
        }

        void UpdateMerchantInventory(Merchant merchant)
        {
            if (!merchantInventories.ContainsKey(merchant.merchantId)) return;

            var inventory = merchantInventories[merchant.merchantId];
            
            foreach (var item in inventory)
            {
                // 가격 변동 적용
                if (shopSettings.enablePriceFluctuation)
                {
                    ApplyPriceFluctuation(item);
                }
                
                // 재고 자연 회복
                if (!item.infiniteStock && item.stock < item.maxStock)
                {
                    float restockAmount = item.restockRate * Time.deltaTime;
                    if (UnityEngine.Random.value < restockAmount)
                    {
                        item.stock = Mathf.Min(item.stock + 1, item.maxStock);
                    }
                }
            }
        }

        void ApplyPriceFluctuation(ShopItem item)
        {
            float variation = UnityEngine.Random.Range(-item.priceVariation, item.priceVariation);
            int newPrice = Mathf.RoundToInt(item.basePrice * (1f + variation));
            item.currentPrice = Mathf.Max(1, newPrice);
        }

        void CheckForRestock(Merchant merchant)
        {
            if (Time.time - merchant.lastRestockTime >= merchant.restockInterval)
            {
                RestockMerchant(merchant);
                merchant.lastRestockTime = Time.time;
            }
        }

        void RestockMerchant(Merchant merchant)
        {
            if (!merchantInventories.ContainsKey(merchant.merchantId)) return;

            var inventory = merchantInventories[merchant.merchantId];
            
            // 기존 재고 보충
            foreach (var item in inventory)
            {
                if (!item.infiniteStock)
                {
                    item.stock = item.maxStock;
                }
                item.currentPrice = item.basePrice; // 가격 리셋
            }
            
            // 새로운 아이템 추가 (확률적)
            if (UnityEngine.Random.value < 0.3f) // 30% 확률
            {
                AddRandomItemToInventory(merchant);
            }
            
            OnMerchantRestocked?.Invoke(merchant);
            Debug.Log($"Merchant {merchant.merchantName} has been restocked!");
        }

        void AddRandomItemToInventory(Merchant merchant)
        {
            var availableItems = globalShopItems.Where(item => 
                merchant.allowedCategories.Contains(item.category) &&
                !merchantInventories[merchant.merchantId].Any(i => i.itemId == item.itemId)
            ).ToList();

            if (availableItems.Count > 0)
            {
                var randomItem = availableItems[UnityEngine.Random.Range(0, availableItems.Count)];
                var newItem = CreateCopyOfItem(randomItem);
                merchantInventories[merchant.merchantId].Add(newItem);
            }
        }

        ShopItem CreateCopyOfItem(ShopItem original)
        {
            return new ShopItem
            {
                itemId = original.itemId,
                itemName = original.itemName,
                category = original.category,
                itemType = original.itemType,
                icon = original.icon,
                description = original.description,
                basePrice = original.basePrice,
                currentPrice = original.currentPrice,
                currency = original.currency,
                priceVariation = original.priceVariation,
                stock = original.stock,
                maxStock = original.maxStock,
                infiniteStock = original.infiniteStock,
                restockRate = original.restockRate,
                isAvailable = original.isAvailable,
                requiredReputationLevel = original.requiredReputationLevel
            };
        }

        void GenerateInitialInventory(Merchant merchant)
        {
            var inventory = merchantInventories[merchant.merchantId];
            
            // 카테고리별로 기본 아이템들 추가
            foreach (var category in merchant.allowedCategories)
            {
                var categoryItems = globalShopItems.Where(item => item.category == category).ToList();
                
                int itemsToAdd = Mathf.Min(5, categoryItems.Count);
                for (int i = 0; i < itemsToAdd; i++)
                {
                    if (categoryItems.Count > 0)
                    {
                        var randomItem = categoryItems[UnityEngine.Random.Range(0, categoryItems.Count)];
                        var newItem = CreateCopyOfItem(randomItem);
                        inventory.Add(newItem);
                        categoryItems.Remove(randomItem);
                    }
                }
            }
        }

        public bool PurchaseItem(string merchantId, string itemId, int quantity, GuildMaster.Battle.Unit buyer)
        {
            var merchant = GetMerchant(merchantId);
            if (merchant == null) return false;

            var item = GetMerchantItem(merchantId, itemId);
            if (item == null || !item.CanPurchase(buyer, quantity)) return false;

            int totalPrice = item.GetFinalPrice(merchant, buyer) * quantity;
            
            // 구매 가능 여부 체크 (골드 등)
            if (!CanAfford(buyer, totalPrice, item.currency)) return false;

            // 결제 처리
            ProcessPayment(buyer, totalPrice, item.currency);
            
            // 재고 감소
            if (!item.infiniteStock)
            {
                item.stock -= quantity;
            }
            
            // 아이템 지급
            GiveItemToBuyer(buyer, item, quantity);
            
            // 평판 증가
            IncreaseReputation(merchant, buyer, totalPrice);
            
            OnItemPurchased?.Invoke(merchant, item, quantity);
            return true;
        }

        public bool SellItem(string merchantId, string itemId, int quantity, GuildMaster.Battle.Unit seller)
        {
            var merchant = GetMerchant(merchantId);
            if (merchant == null) return false;

            // 판매 가격 계산 (구매가의 50%)
            var baseItem = globalShopItems.Find(i => i.itemId == itemId);
            if (baseItem == null) return false;

            int sellPrice = Mathf.RoundToInt(baseItem.basePrice * shopSettings.sellPriceMultiplier * quantity);
            
            // 아이템 제거
            if (!RemoveItemFromSeller(seller, itemId, quantity)) return false;
            
            // 골드 지급
            GiveReward(seller, sellPrice, Merchant.Currency.Gold);
            
            // 상인 인벤토리에 아이템 추가 (선택적)
            AddItemToMerchantInventory(merchant, baseItem, quantity);
            
            OnItemSold?.Invoke(merchant, baseItem, quantity);
            return true;
        }

        bool CanAfford(GuildMaster.Battle.Unit buyer, int price, Merchant.Currency currency)
        {
            // 간단한 골드 체크 로직
            // 실제로는 더 복잡한 화폐 시스템이 필요
            return true; // 임시로 true 반환
        }

        void ProcessPayment(GuildMaster.Battle.Unit buyer, int price, Merchant.Currency currency)
        {
            // 결제 처리 로직
            Debug.Log($"Player paid {price} {currency}");
        }

        void GiveItemToBuyer(GuildMaster.Battle.Unit buyer, ShopItem item, int quantity)
        {
            // 아이템 지급 로직
            Debug.Log($"Player received {quantity}x {item.itemName}");
        }

        bool RemoveItemFromSeller(GuildMaster.Battle.Unit seller, string itemId, int quantity)
        {
            // 플레이어 인벤토리에서 아이템 제거 로직
            return true; // 임시로 true 반환
        }

        void GiveReward(GuildMaster.Battle.Unit seller, int amount, Merchant.Currency currency)
        {
            // 보상 지급 로직
            Debug.Log($"Player received {amount} {currency}");
        }

        void AddItemToMerchantInventory(Merchant merchant, ShopItem item, int quantity)
        {
            var inventory = merchantInventories[merchant.merchantId];
            var existingItem = inventory.Find(i => i.itemId == item.itemId);
            
            if (existingItem != null)
            {
                existingItem.stock += quantity;
            }
            else if (inventory.Count < merchant.maxInventorySize)
            {
                var newItem = CreateCopyOfItem(item);
                newItem.stock = quantity;
                inventory.Add(newItem);
            }
        }

        void IncreaseReputation(Merchant merchant, GuildMaster.Battle.Unit buyer, int purchaseValue)
        {
            int reputationGain = purchaseValue / 100; // 100골드당 1 평판
            merchant.reputationLevel += reputationGain;
            merchant.reputationLevel = Mathf.Min(merchant.reputationLevel, shopSettings.maxMerchantReputation);
            
            // 평판에 따른 할인율 업데이트
            merchant.discountPercentage = Mathf.Min(merchant.reputationLevel * 0.001f, 0.1f); // 최대 10% 할인
            
            OnReputationChanged?.Invoke(merchant, merchant.reputationLevel);
        }

        public Merchant GetMerchant(string merchantId)
        {
            return merchants.Find(m => m.merchantId == merchantId);
        }

        public ShopItem GetMerchantItem(string merchantId, string itemId)
        {
            if (!merchantInventories.ContainsKey(merchantId)) return null;
            return merchantInventories[merchantId].Find(i => i.itemId == itemId);
        }

        public List<ShopItem> GetMerchantInventory(string merchantId)
        {
            if (!merchantInventories.ContainsKey(merchantId))
                return new List<ShopItem>();
            return merchantInventories[merchantId];
        }

        public List<ShopItem> GetAvailableItems(string merchantId, GuildMaster.Battle.Unit buyer)
        {
            var inventory = GetMerchantInventory(merchantId);
            return inventory.Where(item => item.CanPurchase(buyer)).ToList();
        }

        void LoadMerchantData()
        {
            // 저장된 상인 데이터 로드
            // 실제로는 JSON이나 다른 저장 시스템 사용
        }

        public void SaveMerchantData()
        {
            // 상인 데이터 저장
            // 실제로는 JSON이나 다른 저장 시스템 사용
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveMerchantData();
            }
        }

        void OnDestroy()
        {
            SaveMerchantData();
        }
        
        /// <summary>
        /// NPC에서 호출하는 상점 열기 메서드
        /// </summary>
        public void OpenShop(string npcId)
        {
            var merchant = GetMerchant(npcId);
            if (merchant != null)
            {
                Debug.Log($"Opening shop for merchant: {merchant.merchantName}");
                // UI 매니저를 통해 상점 UI 열기
                // UIManager.Instance?.OpenMerchantShop(merchant);
            }
            else
            {
                Debug.LogWarning($"Merchant not found with ID: {npcId}");
            }
        }
    }
} 