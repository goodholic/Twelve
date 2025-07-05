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
        public string saveVersion;
        
        // 플레이어 정보
        public string playerName;
        public string guildName;
        public int guildLevel;
        public float totalPlayTime;
        public float lastSessionTime;
        
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
        public List<string> unlockedCharacters = new List<string>();
        public Dictionary<string, int> statistics = new Dictionary<string, int>();
        
        // 모험가 데이터
        public List<AdventurerSaveData> adventurers = new List<AdventurerSaveData>();
        
        // 부대 편성 데이터
        public List<SquadSaveData> squads = new List<SquadSaveData>();
        
        // 스토리 진행 데이터
        public StoryProgressData storyProgress = new StoryProgressData();
        
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
    public class SquadSaveData
    {
        public int squadIndex; // 0 or 1 for Squad 1/2
        public string squadName;
        public List<string> memberIds = new List<string>(); // 최대 9명
        public List<Vector2Int> positions = new List<Vector2Int>(); // 6x3 그리드 위치
    }
    
    [System.Serializable]
    public class StoryProgressData
    {
        public int currentStage;
        public List<string> completedStages = new List<string>();
        public List<string> viewedDialogues = new List<string>();
        public Dictionary<string, int> storyChoices = new Dictionary<string, int>();
        public int currentEnding = -1; // -1 = not reached yet, 0-5 = ending index
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