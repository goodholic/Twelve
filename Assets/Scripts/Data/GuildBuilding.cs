using UnityEngine;
using System;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class GuildBuilding
    {
        public string id;
        public BuildingType buildingType;
        public Vector2Int position;
        public int level = 1;
        public bool isConstructed = false;
        public bool isUpgrading = false;
        public float constructionProgress = 0f;
        public float upgradeProgress = 0f;
        public float constructionStartTime;
        public float upgradeStartTime;
        
        // 건물별 특수 데이터
        public float productionProgress = 0f;
        public float lastProductionTime;
        public int storedResources = 0;
        public bool isActive = true;
        
        public GuildBuilding()
        {
            id = Guid.NewGuid().ToString();
            buildingType = BuildingType.None;
            position = Vector2Int.zero;
        }
        
        public GuildBuilding(BuildingType type, Vector2Int pos)
        {
            id = Guid.NewGuid().ToString();
            buildingType = type;
            position = pos;
        }
        
        public void StartConstruction()
        {
            isConstructed = false;
            isUpgrading = false;
            constructionProgress = 0f;
            constructionStartTime = Time.time;
        }
        
        public void StartUpgrade()
        {
            if (!isConstructed) return;
            
            isUpgrading = true;
            upgradeProgress = 0f;
            upgradeStartTime = Time.time;
        }
        
        public void UpdateConstruction(float deltaTime)
        {
            if (isConstructed && !isUpgrading) return;
            
            if (!isConstructed)
            {
                constructionProgress += deltaTime;
                if (constructionProgress >= GetConstructionTime())
                {
                    CompleteConstruction();
                }
            }
            else if (isUpgrading)
            {
                upgradeProgress += deltaTime;
                if (upgradeProgress >= GetUpgradeTime())
                {
                    CompleteUpgrade();
                }
            }
        }
        
        private void CompleteConstruction()
        {
            isConstructed = true;
            constructionProgress = GetConstructionTime();
            Debug.Log($"건물 건설 완료: {buildingType}");
        }
        
        private void CompleteUpgrade()
        {
            level++;
            isUpgrading = false;
            upgradeProgress = 0f;
            Debug.Log($"건물 업그레이드 완료: {buildingType} Lv.{level}");
        }
        
        public float GetConstructionTime()
        {
            // 기본 건설 시간 (실제로는 BuildingData에서 가져와야 함)
            return 10f;
        }
        
        public float GetUpgradeTime()
        {
            // 기본 업그레이드 시간 (실제로는 BuildingData에서 가져와야 함)
            return 5f * level;
        }
        
        public float GetConstructionProgress()
        {
            return isConstructed ? 1f : constructionProgress / GetConstructionTime();
        }
        
        public float GetUpgradeProgress()
        {
            return isUpgrading ? upgradeProgress / GetUpgradeTime() : 0f;
        }
        
        public bool IsUnderConstruction()
        {
            return !isConstructed;
        }
        
        public bool IsUnderUpgrade()
        {
            return isUpgrading;
        }
        
        public bool IsOperational()
        {
            return isConstructed && !isUpgrading && isActive;
        }
        
        public GuildMaster.Data.BuildingData GetData()
        {
            // 간단한 구현 - 실제로는 BuildingDatabase에서 가져와야 함
            return new GuildMaster.Data.BuildingData
            {
                buildingId = buildingType.ToString(),
                buildingName = buildingType.ToString(),
                buildingType = buildingType,
                level = level,
                maxLevel = 10,
                canProduce = true,
                productionType = GuildMaster.Data.ResourceType.Gold,
                productionAmount = 10
            };
        }
    }
} 