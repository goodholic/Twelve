using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GuildMaster.Battle;
using Unit = GuildMaster.Battle.UnitStatus;
// using GuildMaster.Data; // Commented out - building types removed

namespace GuildMaster.Guild
{
    /// <summary>
    /// 길드 기본 정보 관리 (README 기능만 포함)
    /// </summary>
    public class GuildManager : MonoBehaviour
    {
        private static GuildManager _instance;
        public static GuildManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GuildManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GuildManager");
                        _instance = go.AddComponent<GuildManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Guild Basic Info")]
        public string guildName = "Default Guild";
        public int guildLevel = 1;
        public int guildExperience = 0;
        
        [Header("Guild Members")]
        [System.NonSerialized]
        public List<UnitStatus> guildMembers = new List<UnitStatus>();
        public int maxMembers = 30;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AddMember(UnitStatus unit)
        {
            if (guildMembers.Count < maxMembers && !guildMembers.Contains(unit))
            {
                guildMembers.Add(unit);
            }
        }

        public void RemoveMember(UnitStatus unit)
        {
            guildMembers.Remove(unit);
        }

        public void AddExperience(int amount)
        {
            guildExperience += amount;
            CheckLevelUp();
        }

        private void CheckLevelUp()
        {
            int requiredExp = GetRequiredExperience(guildLevel);
            while (guildExperience >= requiredExp)
            {
                guildExperience -= requiredExp;
                guildLevel++;
                requiredExp = GetRequiredExperience(guildLevel);
            }
        }

        private int GetRequiredExperience(int level)
        {
            return level * 1000;
        }

        // Additional methods for compatibility
        public string GetGuildName() => guildName;
        public int GetGuildLevel() => guildLevel;
        public string GetGuildId() => guildName; // Using name as ID for now
        
        public List<UnitStatus> GetAdventurers() => guildMembers;
        
        public void RemoveAdventurer(UnitStatus unit)
        {
            RemoveMember(unit);
        }
        
        public void AddReputation(int amount)
        {
            // Reputation system to be implemented
            Debug.Log($"Adding reputation: {amount}");
        }
        
        public void AddGuildExperience(int amount)
        {
            AddExperience(amount);
        }
        
        public void LevelUp()
        {
            guildLevel++;
        }
        
        // ResourceType removed - using string instead
        public Dictionary<string, float> GetResourceProduction()
        {
            // This would normally calculate production from buildings
            // For now, return empty dictionary
            return new Dictionary<string, float>();
        }
        
        public int GetBuildingLevel(string buildingType)
        {
            // Placeholder - would normally check building system
            return 1;
        }
        
        public GuildData GetGuildData()
        {
            return new GuildData
            {
                guildName = guildName,
                guildLevel = guildLevel
            };
        }
        
        public IEnumerator Initialize()
        {
            yield return null;
        }
        
        // Events for compatibility
        public event System.Action<string> OnBuildingConstructed;
        public event System.Action<UnitStatus> OnAdventurerRecruited;
        public event System.Action OnGuildLevelUp;
        public event System.Action<string> OnBuildingUpgraded;
    }
    
    [System.Serializable]
    public class GuildData
    {
        public string guildName;
        public int guildLevel;
        public int GuildLevel => guildLevel;
        // ResourceManager 타입이 제거되어 주석 처리
        // public int GuildReputation => Core.ResourceManager.Instance?.GetReputation() ?? 0;
        public int GuildReputation => 0; // 임시로 0 반환
    }
}