using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.NPC;
using GuildMaster.Core;

namespace GuildMaster.UI
{
    /// <summary>
    /// NPC 상인 거래 UI
    /// 상인 목록, 아이템 구매, 특별 거래 관리
    /// </summary>
    public class MerchantTradingUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject tradingPanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        
        [Header("Merchant List")]
        [SerializeField] private GameObject merchantListPanel;
        [SerializeField] private Transform merchantListContainer;
        [SerializeField] private GameObject merchantEntryPrefab;
        [SerializeField] private TextMeshProUGUI noMerchantsText;
        
        [Header("Shop Interface")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private TextMeshProUGUI shopTitleText;
        [SerializeField] private TextMeshProUGUI merchantDescriptionText;
        [SerializeField] private Image merchantPortrait;
        [SerializeField] private Transform itemGridContainer;
        [SerializeField] private GameObject shopItemPrefab;
        [SerializeField] private Button backToListButton;
        
        [Header("Item Details")]
        [SerializeField] private GameObject itemDetailsPanel;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemTypeText;
        [SerializeField] private TextMeshProUGUI stockText;
        [SerializeField] private Transform costContainer;
        [SerializeField] private GameObject costEntryPrefab;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private TextMeshProUGUI purchaseButtonText;
        
        [Header("Resource Display")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI woodText;
        [SerializeField] private TextMeshProUGUI stoneText;
        [SerializeField] private TextMeshProUGUI manaStoneText;
        [SerializeField] private TextMeshProUGUI reputationText;
        
        [Header("Purchase Confirmation")]
        [SerializeField] private GameObject confirmationPopup;
        [SerializeField] private TextMeshProUGUI confirmationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        
        [Header("Special Effects")]
        [SerializeField] private GameObject merchantArrivalNotification;
        [SerializeField] private TextMeshProUGUI arrivalNotificationText;
        [SerializeField] private ParticleSystem purchaseSuccessEffect;
        [SerializeField] private ParticleSystem specialMerchantEffect;
        
        [Header("Timer Display")]
        [SerializeField] private GameObject timerPanel;
        [SerializeField] private TextMeshProUGUI merchantTimerText;
        [SerializeField] private Image timerFillImage;
        
        [Header("Filter Options")]
        [SerializeField] private Toggle showAllToggle;
        [SerializeField] private Toggle showResourcesToggle;
        [SerializeField] private Toggle showEquipmentToggle;
        [SerializeField] private Toggle showSpecialToggle;
        
        // System references
        private MerchantManager merchantManager;
        private ResourceManager resourceManager;
        private GuildManager guildManager;
        
        // UI State
        private NPCMerchant currentMerchant;
        private TradeItem selectedItem;
        private List<GameObject> merchantEntries = new List<GameObject>();
        private List<GameObject> shopItems = new List<GameObject>();
        private TradeItemType? currentFilter = null;
        private Coroutine timerCoroutine;
        
        [System.Serializable]
        public class MerchantEntry
        {
            public GameObject entryObject;
            public NPCMerchant merchant;
            public Button selectButton;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI typeText;
            public TextMeshProUGUI timerText;
            public Image typeIcon;
        }
        
        [System.Serializable]
        public class ShopItemUI
        {
            public GameObject itemObject;
            public TradeItem tradeItem;
            public Button selectButton;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI priceText;
            public TextMeshProUGUI stockText;
            public Image itemIcon;
            public Image affordableIndicator;
        }
        
        void Start()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogError("GameManager not found!");
                enabled = false;
                return;
            }
            
            merchantManager = FindObjectOfType<MerchantManager>();
            resourceManager = gameManager.ResourceManager;
            guildManager = gameManager.GuildManager;
            
            if (merchantManager == null)
            {
                Debug.LogError("MerchantManager not found!");
                enabled = false;
                return;
            }
            
            SetupUI();
            SubscribeToEvents();
            
            // 초기 UI 업데이트
            UpdateResourceDisplay();
            RefreshMerchantList();
            
            timerCoroutine = StartCoroutine(UpdateTimersCoroutine());
        }
        
        void SetupUI()
        {
            // 메인 버튼
            if (openButton != null)
                openButton.onClick.AddListener(() => ShowPanel(true));
            if (closeButton != null)
                closeButton.onClick.AddListener(() => ShowPanel(false));
            
            // 상점 네비게이션
            if (backToListButton != null)
                backToListButton.onClick.AddListener(ShowMerchantList);
            
            // 구매 버튼
            if (purchaseButton != null)
                purchaseButton.onClick.AddListener(OnPurchaseClicked);
            
            // 확인 팝업
            if (confirmButton != null)
                confirmButton.onClick.AddListener(ConfirmPurchase);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => confirmationPopup.SetActive(false));
            
            // 필터 토글
            SetupFilterToggles();
        }
        
        void SetupFilterToggles()
        {
            if (showAllToggle != null)
                showAllToggle.onValueChanged.AddListener((value) => { if (value) SetFilter(null); });
            if (showResourcesToggle != null)
                showResourcesToggle.onValueChanged.AddListener((value) => { if (value) SetFilter(TradeItemType.Resource); });
            if (showEquipmentToggle != null)
                showEquipmentToggle.onValueChanged.AddListener((value) => { if (value) SetFilter(TradeItemType.Equipment); });
            if (showSpecialToggle != null)
                showSpecialToggle.onValueChanged.AddListener((value) => { if (value) SetFilter(TradeItemType.SkillBook); });
        }
        
        void SubscribeToEvents()
        {
            if (merchantManager != null)
            {
                merchantManager.OnMerchantArrived += OnMerchantArrived;
                merchantManager.OnMerchantDeparted += OnMerchantDeparted;
                merchantManager.OnItemPurchased += OnItemPurchased;
            }
            
            if (resourceManager != null)
            {
                resourceManager.OnResourcesChanged += OnResourcesChanged;
            }
        }
        
        void OnDestroy()
        {
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
            }
            
            // 이벤트 구독 해제
            if (merchantManager != null)
            {
                merchantManager.OnMerchantArrived -= OnMerchantArrived;
                merchantManager.OnMerchantDeparted -= OnMerchantDeparted;
                merchantManager.OnItemPurchased -= OnItemPurchased;
            }
            
            if (resourceManager != null)
            {
                resourceManager.OnResourcesChanged -= OnResourcesChanged;
            }
        }
        
        void ShowPanel(bool show)
        {
            if (tradingPanel != null)
            {
                tradingPanel.SetActive(show);
                
                if (show)
                {
                    RefreshMerchantList();
                    UpdateResourceDisplay();
                }
            }
        }
        
        void RefreshMerchantList()
        {
            // 기존 엔트리 제거
            foreach (var entry in merchantEntries)
            {
                Destroy(entry);
            }
            merchantEntries.Clear();
            
            // 활성 상인 목록 가져오기
            var activeMerchants = merchantManager.GetActiveMerchants();
            
            if (activeMerchants.Count == 0)
            {
                if (noMerchantsText != null)
                {
                    noMerchantsText.gameObject.SetActive(true);
                    noMerchantsText.text = "현재 방문 중인 상인이 없습니다.";
                }
            }
            else
            {
                if (noMerchantsText != null)
                    noMerchantsText.gameObject.SetActive(false);
                
                // 상인 엔트리 생성
                foreach (var merchant in activeMerchants)
                {
                    CreateMerchantEntry(merchant);
                }
            }
        }
        
        void CreateMerchantEntry(NPCMerchant merchant)
        {
            if (merchantEntryPrefab == null || merchantListContainer == null) return;
            
            var entryObj = Instantiate(merchantEntryPrefab, merchantListContainer);
            merchantEntries.Add(entryObj);
            
            var entry = new MerchantEntry
            {
                entryObject = entryObj,
                merchant = merchant
            };
            
            // UI 요소 찾기
            entry.nameText = entryObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            entry.typeText = entryObj.transform.Find("TypeText")?.GetComponent<TextMeshProUGUI>();
            entry.timerText = entryObj.transform.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
            entry.typeIcon = entryObj.transform.Find("TypeIcon")?.GetComponent<Image>();
            entry.selectButton = entryObj.GetComponent<Button>();
            
            // 정보 설정
            if (entry.nameText != null)
                entry.nameText.text = merchant.Name;
            
            if (entry.typeText != null)
                entry.typeText.text = GetMerchantTypeText(merchant.Type);
            
            // 타입별 색상
            if (entry.typeIcon != null)
            {
                entry.typeIcon.color = GetMerchantTypeColor(merchant.Type);
            }
            
            // 선택 버튼
            if (entry.selectButton != null)
            {
                entry.selectButton.onClick.AddListener(() => OpenMerchantShop(merchant));
            }
            
            // 특별 상인 효과
            if (merchant.Type == MerchantType.Rare || merchant.Type == MerchantType.Special)
            {
                if (specialMerchantEffect != null)
                {
                    var effect = Instantiate(specialMerchantEffect, entryObj.transform);
                    effect.transform.localPosition = Vector3.zero;
                    effect.Play();
                }
            }
        }
        
        string GetMerchantTypeText(MerchantType type)
        {
            return type switch
            {
                MerchantType.Regular => "일반 상인",
                MerchantType.Rare => "희귀 상인",
                MerchantType.GuildExclusive => "길드 전용",
                MerchantType.Special => "특별 상인",
                _ => "상인"
            };
        }
        
        Color GetMerchantTypeColor(MerchantType type)
        {
            return type switch
            {
                MerchantType.Regular => Color.white,
                MerchantType.Rare => new Color(0.5f, 0.8f, 1f), // Blue
                MerchantType.GuildExclusive => new Color(1f, 0.8f, 0.3f), // Gold
                MerchantType.Special => new Color(1f, 0.3f, 0.8f), // Purple
                _ => Color.white
            };
        }
        
        void OpenMerchantShop(NPCMerchant merchant)
        {
            currentMerchant = merchant;
            
            if (merchantListPanel != null)
                merchantListPanel.SetActive(false);
            
            if (shopPanel != null)
            {
                shopPanel.SetActive(true);
                
                // 상점 정보 설정
                if (shopTitleText != null)
                    shopTitleText.text = merchant.Name;
                
                if (merchantDescriptionText != null)
                {
                    string description = merchant.Type switch
                    {
                        MerchantType.Rare => "희귀한 아이템을 판매하는 특별한 상인입니다.",
                        MerchantType.GuildExclusive => "길드 전용 특별 상품을 판매합니다.",
                        MerchantType.Special => "한정 기간 특별 상품을 판매합니다!",
                        _ => "다양한 상품을 판매하는 상인입니다."
                    };
                    merchantDescriptionText.text = description;
                }
                
                // 아이템 목록 표시
                DisplayShopItems(merchant.Inventory);
            }
        }
        
        void DisplayShopItems(List<TradeItem> inventory)
        {
            // 기존 아이템 제거
            foreach (var item in shopItems)
            {
                Destroy(item);
            }
            shopItems.Clear();
            
            // 필터링된 아이템 목록
            var filteredItems = currentFilter.HasValue 
                ? inventory.Where(i => i.Type == currentFilter.Value).ToList()
                : inventory;
            
            // 아이템 UI 생성
            foreach (var item in filteredItems)
            {
                CreateShopItemUI(item);
            }
        }
        
        void CreateShopItemUI(TradeItem item)
        {
            if (shopItemPrefab == null || itemGridContainer == null) return;
            
            var itemObj = Instantiate(shopItemPrefab, itemGridContainer);
            shopItems.Add(itemObj);
            
            var shopItem = new ShopItemUI
            {
                itemObject = itemObj,
                tradeItem = item
            };
            
            // UI 요소 찾기
            shopItem.nameText = itemObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            shopItem.priceText = itemObj.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();
            shopItem.stockText = itemObj.transform.Find("StockText")?.GetComponent<TextMeshProUGUI>();
            shopItem.itemIcon = itemObj.transform.Find("ItemIcon")?.GetComponent<Image>();
            shopItem.affordableIndicator = itemObj.transform.Find("AffordableIcon")?.GetComponent<Image>();
            shopItem.selectButton = itemObj.GetComponent<Button>();
            
            // 정보 설정
            if (shopItem.nameText != null)
                shopItem.nameText.text = item.Name;
            
            if (shopItem.priceText != null)
                shopItem.priceText.text = GetPriceText(item);
            
            if (shopItem.stockText != null)
            {
                shopItem.stockText.text = $"재고: {item.Stock}";
                if (item.Stock == 0)
                {
                    shopItem.stockText.color = Color.red;
                }
            }
            
            // 구매 가능 여부 표시
            UpdateItemAffordability(shopItem);
            
            // 선택 버튼
            if (shopItem.selectButton != null)
            {
                shopItem.selectButton.onClick.AddListener(() => SelectItem(item));
                shopItem.selectButton.interactable = item.Stock > 0;
            }
        }
        
        string GetPriceText(TradeItem item)
        {
            List<string> costs = new List<string>();
            
            if (item.GoldCost > 0)
                costs.Add($"{item.GoldCost} 골드");
            if (item.WoodCost > 0)
                costs.Add($"{item.WoodCost} 목재");
            if (item.StoneCost > 0)
                costs.Add($"{item.StoneCost} 석재");
            if (item.ManaStoneCost > 0)
                costs.Add($"{item.ManaStoneCost} 마나석");
            
            return string.Join(", ", costs);
        }
        
        void UpdateItemAffordability(ShopItemUI shopItem)
        {
            if (shopItem.affordableIndicator == null) return;
            
            var guildData = guildManager.GetGuildData();
            bool canAfford = shopItem.tradeItem.CanPurchase(resourceManager, guildData.GuildReputation);
            
            shopItem.affordableIndicator.color = canAfford ? Color.green : Color.red;
            
            if (shopItem.priceText != null)
                shopItem.priceText.color = canAfford ? Color.white : new Color(1f, 0.7f, 0.7f);
        }
        
        void SelectItem(TradeItem item)
        {
            selectedItem = item;
            
            if (itemDetailsPanel != null)
            {
                itemDetailsPanel.SetActive(true);
                
                // 아이템 정보 표시
                if (itemNameText != null)
                    itemNameText.text = item.Name;
                
                if (itemDescriptionText != null)
                    itemDescriptionText.text = item.Description;
                
                if (itemTypeText != null)
                    itemTypeText.text = $"타입: {GetItemTypeText(item.Type)}";
                
                if (stockText != null)
                {
                    stockText.text = $"재고: {item.Stock}/{item.MaxStock}";
                }
                
                // 비용 표시
                DisplayItemCosts(item);
                
                // 구매 버튼 업데이트
                UpdatePurchaseButton();
            }
        }
        
        string GetItemTypeText(TradeItemType type)
        {
            return type switch
            {
                TradeItemType.Resource => "자원",
                TradeItemType.Equipment => "장비",
                TradeItemType.Consumable => "소모품",
                TradeItemType.Blueprint => "설계도",
                TradeItemType.SkillBook => "스킬북",
                TradeItemType.Adventurer => "모험가",
                _ => "기타"
            };
        }
        
        void DisplayItemCosts(TradeItem item)
        {
            // 기존 비용 표시 제거
            foreach (Transform child in costContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 골드
            if (item.GoldCost > 0)
                CreateCostEntry("골드", item.GoldCost, resourceManager.GetGold() >= item.GoldCost);
            
            // 목재
            if (item.WoodCost > 0)
                CreateCostEntry("목재", item.WoodCost, resourceManager.GetWood() >= item.WoodCost);
            
            // 석재
            if (item.StoneCost > 0)
                CreateCostEntry("석재", item.StoneCost, resourceManager.GetStone() >= item.StoneCost);
            
            // 마나석
            if (item.ManaStoneCost > 0)
                CreateCostEntry("마나석", item.ManaStoneCost, resourceManager.GetManaStone() >= item.ManaStoneCost);
            
            // 명성 요구사항
            if (item.ReputationRequired > 0)
            {
                var guildData = guildManager.GetGuildData();
                CreateCostEntry("명성", item.ReputationRequired, guildData.GuildReputation >= item.ReputationRequired);
            }
        }
        
        void CreateCostEntry(string resourceName, int amount, bool hasEnough)
        {
            if (costEntryPrefab != null && costContainer != null)
            {
                var entry = Instantiate(costEntryPrefab, costContainer);
                var text = entry.GetComponentInChildren<TextMeshProUGUI>();
                
                if (text != null)
                {
                    text.text = $"{resourceName}: {amount}";
                    text.color = hasEnough ? Color.white : Color.red;
                }
            }
        }
        
        void UpdatePurchaseButton()
        {
            if (purchaseButton == null || selectedItem == null) return;
            
            var guildData = guildManager.GetGuildData();
            bool canPurchase = selectedItem.CanPurchase(resourceManager, guildData.GuildReputation);
            
            purchaseButton.interactable = canPurchase;
            
            if (purchaseButtonText != null)
            {
                if (!canPurchase)
                {
                    if (selectedItem.Stock <= 0)
                        purchaseButtonText.text = "품절";
                    else if (guildData.GuildReputation < selectedItem.ReputationRequired)
                        purchaseButtonText.text = "명성 부족";
                    else
                        purchaseButtonText.text = "자원 부족";
                }
                else
                {
                    purchaseButtonText.text = "구매하기";
                }
            }
        }
        
        void OnPurchaseClicked()
        {
            if (selectedItem == null || currentMerchant == null) return;
            
            // 확인 팝업 표시
            if (confirmationPopup != null)
            {
                confirmationPopup.SetActive(true);
                
                if (confirmationText != null)
                {
                    confirmationText.text = $"{selectedItem.Name}을(를) 구매하시겠습니까?\n\n{GetPriceText(selectedItem)}";
                }
            }
        }
        
        void ConfirmPurchase()
        {
            if (selectedItem == null || currentMerchant == null) return;
            
            bool success = merchantManager.PurchaseItem(currentMerchant, selectedItem);
            
            if (success)
            {
                // 성공 효과
                if (purchaseSuccessEffect != null)
                {
                    purchaseSuccessEffect.Play();
                }
                
                // UI 업데이트
                UpdateResourceDisplay();
                RefreshCurrentShop();
                UpdatePurchaseButton();
                
                // 팝업 닫기
                if (confirmationPopup != null)
                    confirmationPopup.SetActive(false);
            }
            else
            {
                Debug.LogWarning("Purchase failed!");
            }
        }
        
        void RefreshCurrentShop()
        {
            if (currentMerchant != null)
            {
                DisplayShopItems(currentMerchant.Inventory);
            }
        }
        
        void ShowMerchantList()
        {
            if (shopPanel != null)
                shopPanel.SetActive(false);
            
            if (merchantListPanel != null)
            {
                merchantListPanel.SetActive(true);
                RefreshMerchantList();
            }
        }
        
        void SetFilter(TradeItemType? filter)
        {
            currentFilter = filter;
            
            if (currentMerchant != null)
            {
                DisplayShopItems(currentMerchant.Inventory);
            }
        }
        
        void UpdateResourceDisplay()
        {
            if (goldText != null)
                goldText.text = $"골드: {resourceManager.GetGold()}";
            
            if (woodText != null)
                woodText.text = $"목재: {resourceManager.GetWood()}";
            
            if (stoneText != null)
                stoneText.text = $"석재: {resourceManager.GetStone()}";
            
            if (manaStoneText != null)
                manaStoneText.text = $"마나석: {resourceManager.GetManaStone()}";
            
            if (reputationText != null)
            {
                var guildData = guildManager.GetGuildData();
                reputationText.text = $"명성: {guildData.GuildReputation}";
            }
        }
        
        IEnumerator UpdateTimersCoroutine()
        {
            while (true)
            {
                UpdateMerchantTimers();
                yield return new WaitForSeconds(1f);
            }
        }
        
        void UpdateMerchantTimers()
        {
            var activeMerchants = merchantManager.GetActiveMerchants();
            
            foreach (var entry in merchantEntries)
            {
                var merchantEntry = entry.GetComponent<MerchantEntry>();
                if (merchantEntry == null) continue;
                
                var merchant = activeMerchants.FirstOrDefault(m => m.MerchantId == merchantEntry.merchant.MerchantId);
                if (merchant != null)
                {
                    var timeRemaining = merchant.VisitDuration * 3600f - 
                                      (float)(System.DateTime.Now - merchant.LastVisitTime).TotalSeconds;
                    
                    if (timeRemaining > 0)
                    {
                        int hours = Mathf.FloorToInt(timeRemaining / 3600f);
                        int minutes = Mathf.FloorToInt((timeRemaining % 3600f) / 60f);
                        
                        var timerText = entry.transform.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
                        if (timerText != null)
                        {
                            timerText.text = $"{hours:00}:{minutes:00}";
                            
                            if (timeRemaining < 600f) // 10분 미만
                            {
                                timerText.color = Color.red;
                            }
                            else if (timeRemaining < 1800f) // 30분 미만
                            {
                                timerText.color = Color.yellow;
                            }
                        }
                    }
                }
            }
            
            // 현재 상점의 타이머 업데이트
            if (currentMerchant != null && timerPanel != null)
            {
                var timeRemaining = currentMerchant.VisitDuration * 3600f - 
                                  (float)(System.DateTime.Now - currentMerchant.LastVisitTime).TotalSeconds;
                
                if (timeRemaining > 0)
                {
                    timerPanel.SetActive(true);
                    
                    if (merchantTimerText != null)
                    {
                        int hours = Mathf.FloorToInt(timeRemaining / 3600f);
                        int minutes = Mathf.FloorToInt((timeRemaining % 3600f) / 60f);
                        merchantTimerText.text = $"남은 시간: {hours:00}:{minutes:00}";
                    }
                    
                    if (timerFillImage != null)
                    {
                        timerFillImage.fillAmount = timeRemaining / (currentMerchant.VisitDuration * 3600f);
                    }
                }
                else
                {
                    timerPanel.SetActive(false);
                }
            }
        }
        
        // 이벤트 핸들러
        void OnMerchantArrived(NPCMerchant merchant)
        {
            // 도착 알림
            ShowArrivalNotification(merchant);
            
            // 목록 갱신
            if (merchantListPanel != null && merchantListPanel.activeInHierarchy)
            {
                RefreshMerchantList();
            }
        }
        
        void ShowArrivalNotification(NPCMerchant merchant)
        {
            if (merchantArrivalNotification != null)
            {
                merchantArrivalNotification.SetActive(true);
                
                if (arrivalNotificationText != null)
                {
                    string message = merchant.Type switch
                    {
                        MerchantType.Rare => $"희귀 상인 '{merchant.Name}'이(가) 도착했습니다!",
                        MerchantType.Special => $"특별 상인 '{merchant.Name}'이(가) 한정 상품을 가지고 왔습니다!",
                        MerchantType.GuildExclusive => $"길드 전용 상인 '{merchant.Name}'이(가) 방문했습니다!",
                        _ => $"상인 '{merchant.Name}'이(가) 도착했습니다."
                    };
                    
                    arrivalNotificationText.text = message;
                }
                
                // 3초 후 숨기기
                StartCoroutine(HideNotificationAfterDelay(3f));
            }
        }
        
        IEnumerator HideNotificationAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (merchantArrivalNotification != null)
            {
                merchantArrivalNotification.SetActive(false);
            }
        }
        
        void OnMerchantDeparted(NPCMerchant merchant)
        {
            // 현재 상점이 떠난 상인인 경우
            if (currentMerchant != null && currentMerchant.MerchantId == merchant.MerchantId)
            {
                ShowMerchantList();
            }
            
            // 목록 갱신
            if (merchantListPanel != null && merchantListPanel.activeInHierarchy)
            {
                RefreshMerchantList();
            }
        }
        
        void OnItemPurchased(NPCMerchant merchant, TradeItem item)
        {
            // 구매 완료 메시지
            Debug.Log($"Purchased {item.Name} from {merchant.Name}");
            
            // 특별 아이템 구매 시 추가 효과
            if (item.Type == TradeItemType.Adventurer)
            {
                ShowNotification($"새로운 모험가가 길드에 합류했습니다!");
            }
            else if (item.Type == TradeItemType.SkillBook)
            {
                ShowNotification($"새로운 스킬을 습득할 수 있게 되었습니다!");
            }
        }
        
        void OnResourcesChanged(ResourceManager.Resources resources)
        {
            UpdateResourceDisplay();
            
            // 현재 상점이 열려있으면 구매 가능 여부 업데이트
            if (shopPanel != null && shopPanel.activeInHierarchy)
            {
                foreach (var shopItem in shopItems)
                {
                    var shopItemUI = shopItem.GetComponent<ShopItemUI>();
                    if (shopItemUI != null)
                    {
                        UpdateItemAffordability(shopItemUI);
                    }
                }
                
                UpdatePurchaseButton();
            }
        }
        
        void ShowNotification(string message)
        {
            Debug.Log($"Trading Notification: {message}");
            // TODO: 실제 알림 UI 구현
        }
    }
}