using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class SaveData
    {
        // 메타데이터
        public int slotIndex;
        public bool isAutoSave;
        public long saveTime; // DateTime.Ticks
        public string gameVersion;
        
        // 플레이어 정보
        public string playerName;
        public string guildName;
        public int guildLevel;
        public float totalPlayTime;
        
        // 자원
        public int gold;
        public int wood;
        public int stone;
        public int manaStone;
        public int reputation;
        
        // 게임 진행 상태
        public int currentChapter;
        public int currentDay; // 현재 날짜
        public int currentSeason; // 현재 시즌 (0=봄, 1=여름, 2=가을, 3=겨울)
        public float dayProgress; // 하루 진행도 (0.0 ~ 1.0)
        public List<string> completedQuests = new List<string>();
        public List<string> unlockedBuildings = new List<string>();
        public Dictionary<string, int> statistics = new Dictionary<string, int>();
        
        // 모험가 데이터
        public List<AdventurerSaveData> adventurers = new List<AdventurerSaveData>();
        
        // 건물 데이터
        public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
        
        // 연구 데이터
        public List<ResearchSaveData> researches = new List<ResearchSaveData>();
        
        // 영토 데이터
        public List<TerritorySaveData> territories = new List<TerritorySaveData>();
        
        // UI/설정
        public string screenshotPath;
        public GameSettings gameSettings;
    }
    
    [System.Serializable]
    public class AdventurerSaveData
    {
        public string id;
        public string name;
        public int level;
        public int experience;
        public string jobClass;
        public string rarity;
        public int awakenLevel;
        public List<string> equipmentIds = new List<string>();
        public List<string> skillIds = new List<string>();
        public Dictionary<string, int> stats = new Dictionary<string, int>();
    }
    
    [System.Serializable]
    public class BuildingSaveData
    {
        public string buildingType;
        public int level;
        public int x;
        public int y;
        public bool isConstructing;
        public float constructionTime;
        public List<string> assignedAdventurers = new List<string>();
    }
    
    [System.Serializable]
    public class ResearchSaveData
    {
        public string researchId;
        public int level;
        public bool isResearching;
        public float researchTime;
    }
    
    [System.Serializable]
    public class TerritorySaveData
    {
        public string territoryId;
        public string ownerGuild;
        public int defenseLevel;
        public List<string> defenders = new List<string>();
        public float lastAttackTime;
    }
    
    [System.Serializable]
    public class GameSettings
    {
        public float masterVolume = 1f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 1f;
        public int graphicsQuality = 2;
        public string language = "Korean";
        public bool fullscreen = true;
        public float gameSpeed = 1f;
        public bool autoBattle = true;
    }
}