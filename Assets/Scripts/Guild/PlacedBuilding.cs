using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Core;
using GuildMaster.Data;
using CoreResourceType = GuildMaster.Core.ResourceType;


namespace GuildMaster.Guild
{
    /// <summary>
    /// 배치된 건물 정보
    /// </summary>
    [System.Serializable]
    public class PlacedBuilding
    {
        public int buildingId;
        public string name;
        public BuildingType buildingType;
        public Vector3 position;
        public Vector3 rotation;
        public int level;
        public bool isActive;
        public bool isConstructed;
        public float constructionProgress;
        public DateTime constructionStartTime;
        public DateTime lastMaintenanceTime;
        
        [Header("Production")]
        public Dictionary<CoreResourceType, float> productionRates;
        public Dictionary<CoreResourceType, float> storageCapacity;
        public Dictionary<CoreResourceType, float> currentStorage;
        
        [Header("Requirements")]
        public Dictionary<CoreResourceType, int> constructionCost;
        public Dictionary<CoreResourceType, int> upgradeCost;
        public Dictionary<CoreResourceType, int> maintenanceCost;
        
        [Header("Stats")]
        public float efficiency = 1.0f;
        public float durability = 100.0f;
        public float maxDurability = 100.0f;
        public int maxWorkers = 1;
        public int currentWorkers = 0;
        
        public List<string> assignedWorkerIds;
        public BuildingDataSO buildingData;

        public PlacedBuilding()
        {
            buildingId = UnityEngine.Random.Range(1000, 9999);
            productionRates = new Dictionary<CoreResourceType, float>();
            storageCapacity = new Dictionary<CoreResourceType, float>();
            currentStorage = new Dictionary<CoreResourceType, float>();
            constructionCost = new Dictionary<CoreResourceType, int>();
            upgradeCost = new Dictionary<CoreResourceType, int>();
            maintenanceCost = new Dictionary<CoreResourceType, int>();
            assignedWorkerIds = new List<string>();
            constructionStartTime = DateTime.Now;
            lastMaintenanceTime = DateTime.Now;
            isActive = true;
            isConstructed = false;
            level = 1;
        }

        public PlacedBuilding(BuildingDataSO data, Vector3 pos)
        {
            buildingData = data;
            name = data.buildingName;
            buildingType = data.buildingType;
            position = pos;
            rotation = Vector3.zero;
            level = 1;
            
            buildingId = UnityEngine.Random.Range(1000, 9999);
            productionRates = new Dictionary<CoreResourceType, float>(data.baseProductionRates);
            storageCapacity = new Dictionary<CoreResourceType, float>(data.baseStorageCapacity);
            currentStorage = new Dictionary<CoreResourceType, float>();
            constructionCost = new Dictionary<CoreResourceType, int>(data.constructionCost);
            upgradeCost = new Dictionary<CoreResourceType, int>(data.upgradeCost);
            maintenanceCost = new Dictionary<CoreResourceType, int>(data.maintenanceCost);
            assignedWorkerIds = new List<string>();
            
            maxWorkers = data.maxWorkers;
            constructionStartTime = DateTime.Now;
            lastMaintenanceTime = DateTime.Now;
            isActive = true;
            isConstructed = false;
            efficiency = 1.0f;
            durability = 100.0f;
            maxDurability = 100.0f;
        }

        public bool CanUpgrade()
        {
            return isConstructed && level < 10; // 최대 레벨 10으로 제한
        }

        public void Upgrade()
        {
            if (CanUpgrade())
            {
                level++;
                // 레벨업 시 능력치 향상
                foreach (var key in productionRates.Keys.ToList())
                {
                    productionRates[key] *= 1.2f;
                }
                maxWorkers = Mathf.Min(maxWorkers + 1, 5); // 최대 5명까지
            }
        }

        public float GetEfficiency()
        {
            float workerEfficiency = currentWorkers > 0 ? (float)currentWorkers / maxWorkers : 0.5f;
            float durabilityEfficiency = durability / maxDurability;
            return efficiency * workerEfficiency * durabilityEfficiency;
        }

        public bool NeedsMaintenance()
        {
            return durability < 50.0f || (DateTime.Now - lastMaintenanceTime).TotalHours > 24;
        }

        public void PerformMaintenance()
        {
            durability = maxDurability;
            lastMaintenanceTime = DateTime.Now;
        }

        public bool CanAssignWorker()
        {
            return currentWorkers < maxWorkers && isConstructed && isActive;
        }

        public void AssignWorker(string workerId)
        {
            if (CanAssignWorker() && !assignedWorkerIds.Contains(workerId))
            {
                assignedWorkerIds.Add(workerId);
                currentWorkers++;
            }
        }

        public void RemoveWorker(string workerId)
        {
            if (assignedWorkerIds.Remove(workerId))
            {
                currentWorkers--;
            }
        }
    }
} 