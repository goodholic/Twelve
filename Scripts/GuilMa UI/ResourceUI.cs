using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using GuildMaster.Core;
using GuildMaster.Systems;
using System.Collections;

namespace GuildMaster.UI
{
    /// <summary>
    /// 자원 UI 시스템
    /// 현재 자원, 생산률, 창고 용량 표시
    /// </summary>
    public class ResourceUI : MonoBehaviour
    {
        [Header("Resource Display")]
        [SerializeField] private GameObject resourceDisplayPrefab;
        [SerializeField] private Transform resourceContainer;
        
        [Header("Individual Resource Panels")]
        [SerializeField] private ResourceDisplay goldDisplay;
        [SerializeField] private ResourceDisplay woodDisplay;
        [SerializeField] private ResourceDisplay stoneDisplay;
        [SerializeField] private ResourceDisplay manaDisplay;
        [SerializeField] private TextMeshProUGUI reputationText;
        
        [Header("Production Info")]
        [SerializeField] private GameObject productionTooltip;
        [SerializeField] private TextMeshProUGUI productionDetailsText;
        [SerializeField] private Button productionReportButton;
        
        [Header("Storage Info")]
        [SerializeField] private Slider storageBar;
        [SerializeField] private TextMeshProUGUI storageText;
        [SerializeField] private Image storageBarFill;
        [SerializeField] private Color storageNormalColor = Color.green;
        [SerializeField] private Color storageWarningColor = Color.yellow;
        [SerializeField] private Color storageFullColor = Color.red;
        
        [Header("Animation")]
        [SerializeField] private float updateAnimationDuration = 0.5f;
        [SerializeField] private AnimationCurve updateCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        // 리소스 매니저 참조
        private ResourceManager resourceManager;
        private ResourceProductionSystem productionSystem;
        
        // UI 상태
        private Dictionary<ResourceType, ResourceDisplay> resourceDisplays = new Dictionary<ResourceType, ResourceDisplay>();
        private Dictionary<ResourceType, int> previousValues = new Dictionary<ResourceType, int>();
        private Coroutine updateCoroutine;
        
        [System.Serializable]
        public class ResourceDisplay
        {
            public ResourceType resourceType;
            public Image iconImage;
            public TextMeshProUGUI amountText;
            public TextMeshProUGUI productionText;
            public TextMeshProUGUI capacityText;
            public GameObject changeIndicator;
            public TextMeshProUGUI changeText;
            public Button detailButton;
            public Animator animator;
            
            private Coroutine animationCoroutine;
            
            public void UpdateAmount(int current, int previous, int capacity)
            {
                amountText.text = FormatNumber(current);
                
                if (capacityText != null)
                {
                    capacityText.text = $"/{FormatNumber(capacity)}";
                }
                
                // 변화량 표시
                if (current != previous && changeIndicator != null)
                {
                    ShowChange(current - previous);
                }
                
                // 용량 경고
                float fillRate = capacity > 0 ? (float)current / capacity : 0f;
                if (fillRate > 0.9f && animator != null)
                {
                    animator.SetTrigger("Warning");
                }
            }
            
            public void UpdateProduction(float rate)
            {
                if (productionText != null)
                {
                    string rateText = rate > 0 ? $"+{rate:F1}/h" : "";
                    productionText.text = rateText;
                    productionText.color = rate > 0 ? Color.green : Color.white;
                }
            }
            
            void ShowChange(int change)
            {
                if (animationCoroutine != null)
                {
                    MonoBehaviour mb = changeIndicator.GetComponent<MonoBehaviour>();
                    mb.StopCoroutine(animationCoroutine);
                }
                
                changeIndicator.SetActive(true);
                changeText.text = change > 0 ? $"+{change}" : change.ToString();
                changeText.color = change > 0 ? Color.green : Color.red;
                
                MonoBehaviour mono = changeIndicator.GetComponent<MonoBehaviour>();
                if (mono == null)
                {
                    mono = changeIndicator.AddComponent<MonoBehaviour>();
                }
                
                animationCoroutine = mono.StartCoroutine(AnimateChange());
            }
            
            IEnumerator AnimateChange()
            {
                float elapsed = 0f;
                Vector3 startPos = changeIndicator.transform.localPosition;
                Vector3 endPos = startPos + Vector3.up * 50f;
                
                while (elapsed < 1f)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / 1f;
                    
                    changeIndicator.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                    
                    Color color = changeText.color;
                    color.a = 1f - t;
                    changeText.color = color;
                    
                    yield return null;
                }
                
                changeIndicator.SetActive(false);
                changeIndicator.transform.localPosition = startPos;
            }
            
            string FormatNumber(int number)
            {
                if (number >= 1000000)
                    return $"{number / 1000000f:F1}M";
                else if (number >= 1000)
                    return $"{number / 1000f:F1}K";
                else
                    return number.ToString();
            }
        }
        
        void Start()
        {
            resourceManager = ResourceManager.Instance;
            productionSystem = ResourceProductionSystem.Instance;
            
            if (resourceManager == null)
            {
                Debug.LogError("ResourceManager not found!");
                return;
            }
            
            SetupResourceDisplays();
            SubscribeToEvents();
            
            // 초기 업데이트
            UpdateAllResources();
            
            // 주기적 업데이트
            updateCoroutine = StartCoroutine(UpdateCoroutine());
            
            // 생산 보고서 버튼
            if (productionReportButton != null)
            {
                productionReportButton.onClick.AddListener(ShowProductionReport);
            }
        }
        
        void SetupResourceDisplays()
        {
            if (goldDisplay != null)
            {
                goldDisplay.resourceType = ResourceType.Gold;
                resourceDisplays[ResourceType.Gold] = goldDisplay;
                if (goldDisplay.detailButton != null)
                    goldDisplay.detailButton.onClick.AddListener(() => ShowResourceDetails(ResourceType.Gold));
            }
            
            if (woodDisplay != null)
            {
                woodDisplay.resourceType = ResourceType.Wood;
                resourceDisplays[ResourceType.Wood] = woodDisplay;
                if (woodDisplay.detailButton != null)
                    woodDisplay.detailButton.onClick.AddListener(() => ShowResourceDetails(ResourceType.Wood));
            }
            
            if (stoneDisplay != null)
            {
                stoneDisplay.resourceType = ResourceType.Stone;
                resourceDisplays[ResourceType.Stone] = stoneDisplay;
                if (stoneDisplay.detailButton != null)
                    stoneDisplay.detailButton.onClick.AddListener(() => ShowResourceDetails(ResourceType.Stone));
            }
            
            if (manaDisplay != null)
            {
                manaDisplay.resourceType = ResourceType.ManaStone;
                resourceDisplays[ResourceType.ManaStone] = manaDisplay;
                if (manaDisplay.detailButton != null)
                    manaDisplay.detailButton.onClick.AddListener(() => ShowResourceDetails(ResourceType.ManaStone));
            }
            
            // 이전 값 초기화
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                previousValues[type] = 0;
            }
        }
        
        void SubscribeToEvents()
        {
            if (resourceManager != null)
            {
                resourceManager.OnResourcesChanged += OnResourcesChanged;
                resourceManager.OnGoldChanged += (amount) => UpdateResource(ResourceType.Gold, amount);
                resourceManager.OnWoodChanged += (amount) => UpdateResource(ResourceType.Wood, amount);
                resourceManager.OnStoneChanged += (amount) => UpdateResource(ResourceType.Stone, amount);
                resourceManager.OnManaStoneChanged += (amount) => UpdateResource(ResourceType.ManaStone, amount);
                resourceManager.OnReputationChanged += OnReputationChanged;
            }
            
            if (productionSystem != null)
            {
                productionSystem.OnResourcesProduced += OnResourcesProduced;
                productionSystem.OnBuffApplied += (buff) => UpdateProductionRates();
                productionSystem.OnBuffExpired += (buff) => UpdateProductionRates();
            }
        }
        
        void OnDestroy()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            
            // 이벤트 구독 해제
            if (resourceManager != null)
            {
                resourceManager.OnResourcesChanged -= OnResourcesChanged;
            }
            
            if (productionSystem != null)
            {
                productionSystem.OnResourcesProduced -= OnResourcesProduced;
            }
        }
        
        IEnumerator UpdateCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                
                UpdateProductionRates();
                UpdateStorageInfo();
            }
        }
        
        void UpdateAllResources()
        {
            var resources = resourceManager.GetResources();
            var limits = resourceManager.GetResourceLimits();
            
            foreach (var display in resourceDisplays)
            {
                int current = resources.GetResource(display.Key);
                int capacity = limits.GetLimit(display.Key);
                int previous = previousValues.ContainsKey(display.Key) ? previousValues[display.Key] : current;
                
                display.Value.UpdateAmount(current, previous, capacity);
                previousValues[display.Key] = current;
            }
            
            // 명성 업데이트
            if (reputationText != null)
            {
                reputationText.text = $"명성: {resources.Reputation}";
            }
            
            UpdateProductionRates();
            UpdateStorageInfo();
        }
        
        void UpdateResource(ResourceType type, int amount)
        {
            if (!resourceDisplays.ContainsKey(type)) return;
            
            var display = resourceDisplays[type];
            var limits = resourceManager.GetResourceLimits();
            int capacity = limits.GetLimit(type);
            int previous = previousValues.ContainsKey(type) ? previousValues[type] : amount;
            
            display.UpdateAmount(amount, previous, capacity);
            previousValues[type] = amount;
        }
        
        void UpdateProductionRates()
        {
            foreach (var display in resourceDisplays)
            {
                float rate = display.Key switch
                {
                    ResourceType.Gold => resourceManager.GetGoldProductionRate(),
                    ResourceType.Wood => resourceManager.GetWoodProductionRate(),
                    ResourceType.Stone => resourceManager.GetStoneProductionRate(),
                    ResourceType.ManaStone => resourceManager.GetManaStoneProductionRate(),
                    _ => 0f
                };
                
                display.Value.UpdateProduction(rate);
            }
        }
        
        void UpdateStorageInfo()
        {
            float utilization = resourceManager.GetStorageUtilization();
            
            if (storageBar != null)
            {
                storageBar.value = utilization;
                
                // 색상 변경
                if (storageBarFill != null)
                {
                    if (utilization >= 0.9f)
                        storageBarFill.color = storageFullColor;
                    else if (utilization >= 0.7f)
                        storageBarFill.color = storageWarningColor;
                    else
                        storageBarFill.color = storageNormalColor;
                }
            }
            
            if (storageText != null)
            {
                storageText.text = $"창고: {utilization:P0}";
            }
        }
        
        void OnResourcesChanged(ResourceManager.Resources resources)
        {
            UpdateAllResources();
        }
        
        void OnResourcesProduced(Dictionary<ResourceType, int> produced)
        {
            // 생산 애니메이션
            foreach (var resource in produced)
            {
                if (resource.Value > 0 && resourceDisplays.ContainsKey(resource.Key))
                {
                    var display = resourceDisplays[resource.Key];
                    if (display.animator != null)
                    {
                        display.animator.SetTrigger("Produce");
                    }
                }
            }
        }
        
        void OnReputationChanged(int reputation)
        {
            if (reputationText != null)
            {
                reputationText.text = $"명성: {reputation}";
            }
        }
        
        void ShowResourceDetails(ResourceType type)
        {
            if (productionTooltip == null) return;
            
            productionTooltip.SetActive(true);
            
            string details = $"=== {type} 상세 정보 ===\n\n";
            
            // 현재 보유량
            var resources = resourceManager.GetResources();
            var limits = resourceManager.GetResourceLimits();
            details += $"현재 보유량: {resources.GetResource(type)}/{limits.GetLimit(type)}\n";
            
            // 생산률
            float rate = type switch
            {
                ResourceType.Gold => resourceManager.GetGoldProductionRate(),
                ResourceType.Wood => resourceManager.GetWoodProductionRate(),
                ResourceType.Stone => resourceManager.GetStoneProductionRate(),
                ResourceType.ManaStone => resourceManager.GetManaStoneProductionRate(),
                _ => 0f
            };
            details += $"생산률: +{rate:F1}/시간\n\n";
            
            // 최근 생산 통계
            if (productionSystem != null)
            {
                var stats = productionSystem.GetProductionStatistics();
                if (stats.HourlyProduction.ContainsKey(type))
                {
                    details += $"지난 1시간 생산량: {stats.HourlyProduction[type]:F0}\n";
                }
                if (stats.TotalProduced.ContainsKey(type))
                {
                    details += $"총 생산량: {stats.TotalProduced[type]:F0}\n";
                }
            }
            
            // 시장 가치
            float marketValue = resourceManager.GetResourceMarketValue(type);
            details += $"\n시장 가치: {marketValue:F1} 골드\n";
            
            if (productionDetailsText != null)
            {
                productionDetailsText.text = details;
            }
        }
        
        void ShowProductionReport()
        {
            if (productionTooltip == null || productionSystem == null) return;
            
            productionTooltip.SetActive(true);
            
            string report = productionSystem.GenerateProductionReport();
            
            if (productionDetailsText != null)
            {
                productionDetailsText.text = report;
            }
        }
        
        public void ToggleProductionTooltip()
        {
            if (productionTooltip != null)
            {
                productionTooltip.SetActive(!productionTooltip.activeSelf);
            }
        }
        
        /// <summary>
        /// 자원 부족 경고
        /// </summary>
        public void ShowResourceWarning(ResourceType type, int required, int current)
        {
            string message = $"{type} 부족! 필요: {required}, 현재: {current}";
            
            // 경고 메시지 표시
            if (resourceDisplays.ContainsKey(type))
            {
                var display = resourceDisplays[type];
                if (display.animator != null)
                {
                    display.animator.SetTrigger("Insufficient");
                }
            }
            
            Debug.LogWarning(message);
        }
        
        /// <summary>
        /// 자원 교환 UI 표시
        /// </summary>
        public void ShowResourceExchange()
        {
            // 자원 교환 UI 구현
            Debug.Log("Resource exchange UI not implemented yet");
        }
    }
}