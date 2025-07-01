using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuildMaster.Core
{
    [System.Serializable]
    public class SaveData
    {
        [Header("세이브 정보")]
        public int slotIndex;
        public bool isAutoSave;
        public DateTime saveTime;
        public string gameVersion;
        public string screenshotPath;
        
        [Header("플레이어 정보")]
        public string playerName;
        public string guildName;
        public int guildLevel;
        public float totalPlayTime;
        
        [Header("자원")]
        public int gold;
        public int wood;
        public int stone;
        public int manaStone;
        public int reputation;
        
        [Header("진행도")]
        public int currentChapter;
        public int currentDay;
        public int currentSeason;
        public float dayProgress;
        public List<string> completedQuests;
        public List<string> unlockedBuildings;
        
        [Header("캐릭터")]
        public List<AdventurerSaveData> adventurers;
        
        [Header("건물")]
        public List<BuildingSaveData> buildings;
        
        public SaveData()
        {
            completedQuests = new List<string>();
            unlockedBuildings = new List<string>();
            adventurers = new List<AdventurerSaveData>();
            buildings = new List<BuildingSaveData>();
            saveTime = DateTime.Now;
            gameVersion = Application.version;
        }
    }
    
    [System.Serializable]
    public class AdventurerSaveData
    {
        public string unitId;
        public string characterId;
        public string name;
        public int level;
        public int experience;
        public float currentHP;
        public float currentMP;
        public List<string> skillIds;
        public Dictionary<string, object> customData;
        
        public AdventurerSaveData()
        {
            skillIds = new List<string>();
            customData = new Dictionary<string, object>();
        }
    }
    
    [System.Serializable]
    public class BuildingSaveData
    {
        public string buildingId;
        public string buildingType;
        public int level;
        public Vector3 position;
        public bool isUnlocked;
        public Dictionary<string, object> customData;
        
        public BuildingSaveData()
        {
            customData = new Dictionary<string, object>();
        }
    }
} 