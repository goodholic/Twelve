using UnityEngine;
using System;
using System.Collections.Generic;

namespace GuildMaster.Guild
{
    public class Building : MonoBehaviour
    {
        [Header("Building Configuration")]
        [SerializeField] private Core.GuildManager.BuildingType buildingType;
        [SerializeField] private Vector2Int gridSize = new Vector2Int(2, 2);
        [SerializeField] private int maxLevel = 10;
        
        [Header("Visual Configuration")]
        [SerializeField] private GameObject[] levelModels; // Different models per level
        [SerializeField] private Transform productionEffectPoint;
        [SerializeField] private GameObject constructionVFX;
        [SerializeField] private GameObject upgradeVFX;
        [SerializeField] private GameObject productionVFX;
        
        [Header("Audio")]
        [SerializeField] private AudioClip constructionSound;
        [SerializeField] private AudioClip upgradeSound;
        [SerializeField] private AudioClip productionSound;
        
        // Building data
        public Core.GuildManager.BuildingType Type => buildingType;
        public Vector2Int GridSize => gridSize;
        public Vector2Int GridPosition { get; set; }
        public int Level { get; private set; } = 1;
        public bool IsPlaced { get; set; }
        public bool IsConstructing { get; private set; }
        public bool IsUpgrading { get; private set; }
        public DateTime ConstructionStartTime { get; private set; }
        public DateTime LastProductionTime { get; private set; }
        
        // Production data
        private float productionTimer;
        private bool isProducing;
        
        // Animation
        private Animator animator;
        private AudioSource audioSource;
        
        // Events
        public event Action<Building> OnConstructionComplete;
        public event Action<Building> OnUpgradeComplete;
        public event Action<Building, ResourceProduction> OnResourceProduced;
        public event Action<Building> OnBuildingDestroyed;
        
        [System.Serializable]
        public class ResourceProduction
        {
            public int Gold { get; set; }
            public int Wood { get; set; }
            public int Stone { get; set; }
            public int ManaStone { get; set; }
            public int ResearchPoints { get; set; }
            public float ProductionInterval { get; set; }
        }
        
        void Awake()
        {
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            UpdateVisuals();
        }
        
        void Start()
        {
            if (IsPlaced && !IsConstructing)
            {
                StartProduction();
            }
        }
        
        void Update()
        {
            if (IsConstructing || IsUpgrading)
            {
                UpdateConstruction();
            }
            else if (isProducing && IsPlaced)
            {
                UpdateProduction();
            }
        }
        
        void UpdateConstruction()
        {
            float timeElapsed = (float)(DateTime.Now - ConstructionStartTime).TotalSeconds;
            float constructionTime = GetConstructionTime();
            
            if (timeElapsed >= constructionTime)
            {
                CompleteConstruction();
            }
        }
        
        void UpdateProduction()
        {
            productionTimer += Time.deltaTime;
            
            var production = GetResourceProduction();
            if (productionTimer >= production.ProductionInterval)
            {
                ProduceResources();
                productionTimer = 0f;
            }
        }
        
        public void StartConstruction()
        {
            IsConstructing = true;
            ConstructionStartTime = DateTime.Now;
            
            if (constructionVFX != null)
            {
                Instantiate(constructionVFX, transform.position, Quaternion.identity, transform);
            }
            
            PlaySound(constructionSound);
            
            // Hide model during construction
            UpdateVisuals();
        }
        
        void CompleteConstruction()
        {
            IsConstructing = false;
            IsPlaced = true;
            
            UpdateVisuals();
            StartProduction();
            
            OnConstructionComplete?.Invoke(this);
        }
        
        public bool CanUpgrade()
        {
            if (IsConstructing || IsUpgrading) return false;
            if (Level >= maxLevel) return false;
            
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return false;
            
            var cost = GetUpgradeCost();
            return gameManager.ResourceManager.CanAfford(cost.Gold, cost.Wood, cost.Stone, cost.ManaStone);
        }
        
        public void Upgrade()
        {
            if (!CanUpgrade()) return;
            
            var gameManager = Core.GameManager.Instance;
            var cost = GetUpgradeCost();
            
            // Pay the cost
            gameManager.ResourceManager.SpendResources(cost.Gold, cost.Wood, cost.Stone, cost.ManaStone);
            
            IsUpgrading = true;
            ConstructionStartTime = DateTime.Now;
            
            if (upgradeVFX != null)
            {
                Instantiate(upgradeVFX, transform.position, Quaternion.identity, transform);
            }
            
            PlaySound(upgradeSound);
        }
        
        void CompleteUpgrade()
        {
            IsUpgrading = false;
            Level++;
            
            UpdateVisuals();
            
            OnUpgradeComplete?.Invoke(this);
        }
        
        void StartProduction()
        {
            isProducing = true;
            productionTimer = 0f;
            LastProductionTime = DateTime.Now;
        }
        
        void ProduceResources()
        {
            var production = GetResourceProduction();
            
            if (production.Gold > 0 || production.Wood > 0 || production.Stone > 0 || production.ManaStone > 0)
            {
                var gameManager = Core.GameManager.Instance;
                if (gameManager != null)
                {
                    gameManager.ResourceManager.AddGold(production.Gold);
                    gameManager.ResourceManager.AddWood(production.Wood);
                    gameManager.ResourceManager.AddStone(production.Stone);
                    gameManager.ResourceManager.AddManaStone(production.ManaStone);
                }
                
                // Visual feedback
                if (productionVFX != null && productionEffectPoint != null)
                {
                    Instantiate(productionVFX, productionEffectPoint.position, Quaternion.identity);
                }
                
                PlaySound(productionSound);
                
                OnResourceProduced?.Invoke(this, production);
            }
            
            LastProductionTime = DateTime.Now;
        }
        
        public ResourceProduction GetResourceProduction()
        {
            var production = new ResourceProduction();
            
            switch (buildingType)
            {
                case Core.GuildManager.BuildingType.GuildHall:
                    // Guild Hall provides no direct production but boosts overall guild efficiency
                    production.ProductionInterval = float.MaxValue;
                    break;
                    
                case Core.GuildManager.BuildingType.TrainingGround:
                    // No direct resource production, improves unit training
                    production.ProductionInterval = float.MaxValue;
                    break;
                    
                case Core.GuildManager.BuildingType.ResearchLab:
                    production.ResearchPoints = 1 * Level;
                    production.ProductionInterval = 300f; // Every 5 minutes
                    break;
                    
                case Core.GuildManager.BuildingType.MageTower:
                    production.ManaStone = 1 * Level;
                    production.ProductionInterval = 180f; // Every 3 minutes
                    break;
                    
                case Core.GuildManager.BuildingType.Temple:
                    // Provides healing and resurrection bonuses
                    production.ProductionInterval = float.MaxValue;
                    break;
                    
                case Core.GuildManager.BuildingType.Dormitory:
                    // Houses adventurers, no direct production
                    production.ProductionInterval = float.MaxValue;
                    break;
                    
                case Core.GuildManager.BuildingType.Armory:
                    // Equipment storage, no direct production
                    production.ProductionInterval = float.MaxValue;
                    break;
                    
                case Core.GuildManager.BuildingType.Shop:
                    production.Gold = 10 * Level;
                    production.ProductionInterval = 180f; // Every 3 minutes
                    break;
                    
                case Core.GuildManager.BuildingType.Warehouse:
                    // Increases resource capacity
                    production.ProductionInterval = float.MaxValue;
                    break;
                    
                case Core.GuildManager.BuildingType.ScoutPost:
                    // Provides exploration bonuses
                    production.ProductionInterval = float.MaxValue;
                    break;
            }
            
            // Apply guild-wide production bonuses
            ApplyProductionBonuses(production);
            
            return production;
        }
        
        void ApplyProductionBonuses(ResourceProduction production)
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return;
            
            // Apply research bonuses
            if (gameManager.ResearchManager != null)
            {
                float resourceBonus = gameManager.ResearchManager.GetResearchBonus("resource_production");
                production.Gold = (int)(production.Gold * (1f + resourceBonus));
                production.Wood = (int)(production.Wood * (1f + resourceBonus));
                production.Stone = (int)(production.Stone * (1f + resourceBonus));
                production.ManaStone = (int)(production.ManaStone * (1f + resourceBonus));
            }
            
            // Apply territory bonuses
            if (gameManager.TerritoryManager != null)
            {
                float territoryBonus = gameManager.TerritoryManager.GetTotalTerritoryBonus(
                    "player_guild", Systems.TerritoryBonus.ResourceProduction);
                production.Gold = (int)(production.Gold * (1f + territoryBonus));
                production.Wood = (int)(production.Wood * (1f + territoryBonus));
                production.Stone = (int)(production.Stone * (1f + territoryBonus));
                production.ManaStone = (int)(production.ManaStone * (1f + territoryBonus));
            }
        }
        
        public BuildingCost GetUpgradeCost()
        {
            var cost = new BuildingCost();
            
            int nextLevel = Level + 1;
            
            switch (buildingType)
            {
                case Core.GuildManager.BuildingType.GuildHall:
                    cost.Gold = 1000 * nextLevel;
                    cost.Wood = 500 * nextLevel;
                    cost.Stone = 300 * nextLevel;
                    break;
                    
                case Core.GuildManager.BuildingType.TrainingGround:
                    cost.Gold = 500 * nextLevel;
                    cost.Wood = 200 * nextLevel;
                    break;
                    
                case Core.GuildManager.BuildingType.ResearchLab:
                    cost.Gold = 800 * nextLevel;
                    cost.Stone = 400 * nextLevel;
                    cost.ManaStone = 100 * nextLevel;
                    break;
                    
                case Core.GuildManager.BuildingType.MageTower:
                    cost.Gold = 600 * nextLevel;
                    cost.Stone = 300 * nextLevel;
                    cost.ManaStone = 200 * nextLevel;
                    break;
                    
                default:
                    cost.Gold = 400 * nextLevel;
                    cost.Wood = 200 * nextLevel;
                    cost.Stone = 100 * nextLevel;
                    break;
            }
            
            return cost;
        }
        
        public float GetConstructionTime()
        {
            float baseTime = 60f; // 1 minute base
            
            switch (buildingType)
            {
                case Core.GuildManager.BuildingType.GuildHall:
                    baseTime = 180f; // 3 minutes
                    break;
                case Core.GuildManager.BuildingType.ResearchLab:
                case Core.GuildManager.BuildingType.MageTower:
                    baseTime = 120f; // 2 minutes
                    break;
            }
            
            // Higher level buildings take longer
            baseTime *= Level;
            
            // Apply construction speed bonuses
            var gameManager = Core.GameManager.Instance;
            if (gameManager?.ResearchManager != null)
            {
                float speedBonus = gameManager.ResearchManager.GetResearchBonus("build_speed");
                baseTime /= (1f + speedBonus);
            }
            
            return baseTime;
        }
        
        void UpdateVisuals()
        {
            // Show construction state
            if (IsConstructing || IsUpgrading)
            {
                // Show construction visuals
                if (levelModels != null && levelModels.Length > 0)
                {
                    foreach (var model in levelModels)
                    {
                        if (model != null) model.SetActive(false);
                    }
                }
                return;
            }
            
            // Show appropriate level model
            if (levelModels != null && levelModels.Length > 0)
            {
                for (int i = 0; i < levelModels.Length; i++)
                {
                    if (levelModels[i] != null)
                    {
                        levelModels[i].SetActive(i == Mathf.Min(Level - 1, levelModels.Length - 1));
                    }
                }
            }
            
            // Update animator
            if (animator != null)
            {
                animator.SetInteger("Level", Level);
                animator.SetBool("IsProducing", isProducing);
            }
        }
        
        void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        public void DestroyBuilding()
        {
            OnBuildingDestroyed?.Invoke(this);
            Destroy(gameObject);
        }
        
        public float GetConstructionProgress()
        {
            if (!IsConstructing && !IsUpgrading) return 1f;
            
            float timeElapsed = (float)(DateTime.Now - ConstructionStartTime).TotalSeconds;
            float constructionTime = GetConstructionTime();
            
            return Mathf.Clamp01(timeElapsed / constructionTime);
        }
        
        public string GetStatusText()
        {
            if (IsConstructing) return "건설 중...";
            if (IsUpgrading) return "업그레이드 중...";
            if (isProducing) return "생산 중";
            return "대기";
        }
        
        [System.Serializable]
        public class BuildingCost
        {
            public int Gold { get; set; }
            public int Wood { get; set; }
            public int Stone { get; set; }
            public int ManaStone { get; set; }
        }
    }
}