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
        public List<string> unlockedCharacters;
        public List<SquadSaveData> squads;
        public StoryProgressData storyProgress;
        
        [Header("캐릭터")]
        public List<AdventurerSaveData> adventurers;
        
        [Header("건물")]
        public List<BuildingSaveData> buildings;
        
        public SaveData()
        {
            completedQuests = new List<string>();
            unlockedBuildings = new List<string>();
            unlockedCharacters = new List<string>();
            adventurers = new List<AdventurerSaveData>();
            buildings = new List<BuildingSaveData>();
            squads = new List<SquadSaveData>();
            storyProgress = new StoryProgressData();
            saveTime = DateTime.Now;
            gameVersion = Application.version;
        }
    }
    
    [System.Serializable]
    public class AdventurerSaveData
    {
        public string id;
        public string unitId;
        public string characterId;
        public string name;
        public int level;
        public int experience;
        public float currentHP;
        public float currentMP;
        public Battle.JobClass jobClass;
        public Data.Rarity rarity;
        public int awakenLevel;
        public List<string> skillIds;
        public List<string> equipmentIds;
        public Dictionary<string, object> customData;
        
        public AdventurerSaveData()
        {
            skillIds = new List<string>();
            equipmentIds = new List<string>();
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
    
    [System.Serializable]
    public class SquadSaveData
    {
        public string squadId;
        public string squadName;
        public List<string> memberIds;
        
        public SquadSaveData()
        {
            memberIds = new List<string>();
        }
    }
    
    [System.Serializable]
    public class StoryProgressData
    {
        public int currentChapter;
        public int currentStage;
        public List<string> completedStages;
        
        public StoryProgressData()
        {
            completedStages = new List<string>();
        }
    }
} 