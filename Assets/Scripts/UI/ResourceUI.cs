using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using GuildMaster.Core;

namespace GuildMaster.UI
{
    public class ResourceUI : MonoBehaviour
    {
        [Header("Resource Display")]
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI woodText;
        public TextMeshProUGUI stoneText;
        public TextMeshProUGUI foodText;
        public TextMeshProUGUI energyText;
        
        [Header("Animated Display")]
        public bool enableAnimations = true;
        public float animationDuration = 0.5f;
        
        private Dictionary<ResourceType, TextMeshProUGUI> resourceTexts;
        private Dictionary<ResourceType, int> lastValues;
        
        private static ResourceUI _instance;
        public static ResourceUI Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<ResourceUI>();
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            InitializeResourceTexts();
            UpdateAllResources();
        }
        
        private void InitializeResourceTexts()
        {
            resourceTexts = new Dictionary<ResourceType, TextMeshProUGUI>();
            lastValues = new Dictionary<ResourceType, int>();
            
            if (goldText != null)
            {
                resourceTexts[ResourceType.Gold] = goldText;
                lastValues[ResourceType.Gold] = 0;
            }
            if (woodText != null)
            {
                resourceTexts[ResourceType.Wood] = woodText;
                lastValues[ResourceType.Wood] = 0;
            }
            if (stoneText != null)
            {
                resourceTexts[ResourceType.Stone] = stoneText;
                lastValues[ResourceType.Stone] = 0;
            }
            if (foodText != null)
            {
                resourceTexts[ResourceType.Food] = foodText;
                lastValues[ResourceType.Food] = 0;
            }
            if (energyText != null)
            {
                resourceTexts[ResourceType.Energy] = energyText;
                lastValues[ResourceType.Energy] = 0;
            }
        }
        
        public void UpdateResource(ResourceType resourceType, int amount)
        {
            if (resourceTexts.ContainsKey(resourceType))
            {
                var textComponent = resourceTexts[resourceType];
                if (textComponent != null)
                {
                    textComponent.text = FormatResourceAmount(amount);
                    
                    if (enableAnimations && lastValues.ContainsKey(resourceType))
                    {
                        int difference = amount - lastValues[resourceType];
                        if (difference != 0)
                        {
                            AnimateResourceChange(textComponent, difference > 0);
                        }
                    }
                    
                    lastValues[resourceType] = amount;
                }
            }
        }
        
        public void UpdateAllResources()
        {
            if (ResourceManager.Instance != null)
            {
                UpdateResource(ResourceType.Gold, ResourceManager.Instance.GetResource(ResourceType.Gold));
                UpdateResource(ResourceType.Wood, ResourceManager.Instance.GetResource(ResourceType.Wood));
                UpdateResource(ResourceType.Stone, ResourceManager.Instance.GetResource(ResourceType.Stone));
                UpdateResource(ResourceType.Food, ResourceManager.Instance.GetResource(ResourceType.Food));
                UpdateResource(ResourceType.Energy, ResourceManager.Instance.GetResource(ResourceType.Energy));
            }
        }
        
        private string FormatResourceAmount(int amount)
        {
            if (amount >= 1000000)
                return $"{amount / 1000000.0f:F1}M";
            else if (amount >= 1000)
                return $"{amount / 1000.0f:F1}K";
            else
                return amount.ToString();
        }
        
        private void AnimateResourceChange(TextMeshProUGUI textComponent, bool isIncrease)
        {
            if (textComponent == null) return;
            
            Color originalColor = textComponent.color;
            Color targetColor = isIncrease ? Color.green : Color.red;
            
            StartCoroutine(AnimateColorChange(textComponent, originalColor, targetColor, originalColor));
        }
        
        private System.Collections.IEnumerator AnimateColorChange(TextMeshProUGUI textComponent, Color from, Color to, Color final)
        {
            float elapsed = 0f;
            float halfDuration = animationDuration / 2f;
            
            // 색상 변경
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                textComponent.color = Color.Lerp(from, to, elapsed / halfDuration);
                yield return null;
            }
            
            elapsed = 0f;
            
            // 원래 색상으로 복원
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                textComponent.color = Color.Lerp(to, final, elapsed / halfDuration);
                yield return null;
            }
            
            textComponent.color = final;
        }
        
        private void Update()
        {
            // 주기적으로 리소스 업데이트
            if (Time.time % 1f < Time.deltaTime)
            {
                UpdateAllResources();
            }
        }
    }
} 