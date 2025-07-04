using UnityEngine;
using System;
using System.Collections.Generic;

namespace pjy.Data
{
    /// <summary>
    /// PJY 데이터 시스템 - 게임 데이터 관리
    /// </summary>
    public static class DataManager
    {
        public static bool IsInitialized { get; private set; } = false;
        
        // 데이터 저장소
        public static Dictionary<string, object> GameData { get; private set; } = new Dictionary<string, object>();
        public static Dictionary<string, PlayerPreference> PlayerPrefs { get; private set; } = new Dictionary<string, PlayerPreference>();
        
        public static void Initialize()
        {
            if (IsInitialized) return;
            
            LoadDefaultData();
            LoadPlayerPreferences();
            
            IsInitialized = true;
            Debug.Log("PJY Data System Initialized");
        }
        
        static void LoadDefaultData()
        {
            // 기본 게임 데이터 로드
            GameData["version"] = "1.0.0";
            GameData["max_level"] = 100;
            GameData["base_experience"] = 100;
        }
        
        static void LoadPlayerPreferences()
        {
            // 플레이어 설정 로드
            PlayerPrefs["sound_volume"] = new PlayerPreference("sound_volume", 1.0f);
            PlayerPrefs["music_volume"] = new PlayerPreference("music_volume", 1.0f);
            PlayerPrefs["graphics_quality"] = new PlayerPreference("graphics_quality", 2);
        }
        
        public static T GetData<T>(string key)
        {
            if (GameData.ContainsKey(key) && GameData[key] is T)
            {
                return (T)GameData[key];
            }
            return default(T);
        }
        
        public static void SetData<T>(string key, T value)
        {
            GameData[key] = value;
        }
        
        public static T GetPlayerPref<T>(string key)
        {
            if (PlayerPrefs.ContainsKey(key))
            {
                return PlayerPrefs[key].GetValue<T>();
            }
            return default(T);
        }
        
        public static void SetPlayerPref<T>(string key, T value)
        {
            if (PlayerPrefs.ContainsKey(key))
            {
                PlayerPrefs[key].SetValue(value);
            }
            else
            {
                PlayerPrefs[key] = new PlayerPreference(key, value);
            }
        }
    }
    
    [System.Serializable]
    public class PlayerPreference
    {
        public string key;
        public object value;
        public Type valueType;
        public DateTime lastModified;
        
        public PlayerPreference(string key, object value)
        {
            this.key = key;
            this.value = value;
            this.valueType = value?.GetType();
            this.lastModified = DateTime.Now;
        }
        
        public T GetValue<T>()
        {
            if (value is T)
                return (T)value;
            return default(T);
        }
        
        public void SetValue<T>(T newValue)
        {
            value = newValue;
            valueType = typeof(T);
            lastModified = DateTime.Now;
        }
    }
    
    /// <summary>
    /// 게임 설정 데이터
    /// </summary>
    [System.Serializable]
    public class GameSettings
    {
        public float soundVolume = 1.0f;
        public float musicVolume = 1.0f;
        public int graphicsQuality = 2;
        public bool fullscreen = true;
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;
        public string language = "ko";
        
        public GameSettings()
        {
            LoadFromPlayerPrefs();
        }
        
        public void LoadFromPlayerPrefs()
        {
            soundVolume = DataManager.GetPlayerPref<float>("sound_volume");
            musicVolume = DataManager.GetPlayerPref<float>("music_volume");
            graphicsQuality = DataManager.GetPlayerPref<int>("graphics_quality");
            fullscreen = DataManager.GetPlayerPref<bool>("fullscreen");
            resolutionWidth = DataManager.GetPlayerPref<int>("resolution_width");
            resolutionHeight = DataManager.GetPlayerPref<int>("resolution_height");
            language = DataManager.GetPlayerPref<string>("language");
        }
        
        public void SaveToPlayerPrefs()
        {
            DataManager.SetPlayerPref("sound_volume", soundVolume);
            DataManager.SetPlayerPref("music_volume", musicVolume);
            DataManager.SetPlayerPref("graphics_quality", graphicsQuality);
            DataManager.SetPlayerPref("fullscreen", fullscreen);
            DataManager.SetPlayerPref("resolution_width", resolutionWidth);
            DataManager.SetPlayerPref("resolution_height", resolutionHeight);
            DataManager.SetPlayerPref("language", language);
        }
    }
    
    /// <summary>
    /// 게임 상수 데이터
    /// </summary>
    public static class GameConstants
    {
        public const int MAX_INVENTORY_SIZE = 100;
        public const int MAX_PARTY_SIZE = 6;
        public const int MAX_GUILD_MEMBERS = 50;
        public const float DAMAGE_VARIANCE = 0.1f;
        public const float CRITICAL_DAMAGE_MULTIPLIER = 2.0f;
        
        // 자원 타입별 최대값
        public static readonly Dictionary<string, int> ResourceMaxValues = new Dictionary<string, int>
        {
            { "Gold", 999999999 },
            { "Wood", 999999 },
            { "Stone", 999999 },
            { "Food", 999999 },
            { "Mana", 999999 },
            { "Experience", int.MaxValue },
            { "Energy", 1000 }
        };
        
        // 레벨별 경험치 요구량
        public static int GetExperienceRequired(int level)
        {
            return 100 * level * level + 50 * level;
        }
        
        // 레어도별 색상
        public static Color GetRarityColor(int rarity)
        {
            switch (rarity)
            {
                case 0: return Color.white;      // Common
                case 1: return Color.green;      // Uncommon
                case 2: return Color.blue;       // Rare
                case 3: return Color.magenta;    // Epic
                case 4: return Color.yellow;     // Legendary
                case 5: return Color.red;        // Mythic
                default: return Color.white;
            }
        }
    }
    
    /// <summary>
    /// 로컬라이제이션 데이터
    /// </summary>
    public static class LocalizationData
    {
        public static Dictionary<string, Dictionary<string, string>> Texts = 
            new Dictionary<string, Dictionary<string, string>>();
        
        static LocalizationData()
        {
            InitializeDefaultTexts();
        }
        
        static void InitializeDefaultTexts()
        {
            // 한국어 텍스트
            Texts["ko"] = new Dictionary<string, string>
            {
                { "ui.start_game", "게임 시작" },
                { "ui.settings", "설정" },
                { "ui.quit", "종료" },
                { "ui.inventory", "인벤토리" },
                { "ui.character", "캐릭터" },
                { "ui.guild", "길드" },
                { "ui.battle", "전투" },
                { "ui.shop", "상점" }
            };
            
            // 영어 텍스트
            Texts["en"] = new Dictionary<string, string>
            {
                { "ui.start_game", "Start Game" },
                { "ui.settings", "Settings" },
                { "ui.quit", "Quit" },
                { "ui.inventory", "Inventory" },
                { "ui.character", "Character" },
                { "ui.guild", "Guild" },
                { "ui.battle", "Battle" },
                { "ui.shop", "Shop" }
            };
        }
        
        public static string GetText(string key, string language = "ko")
        {
            if (Texts.ContainsKey(language) && Texts[language].ContainsKey(key))
            {
                return Texts[language][key];
            }
            
            // 기본 언어로 fallback
            if (language != "ko" && Texts.ContainsKey("ko") && Texts["ko"].ContainsKey(key))
            {
                return Texts["ko"][key];
            }
            
            return key; // 텍스트를 찾을 수 없으면 키 반환
        }
    }
} 