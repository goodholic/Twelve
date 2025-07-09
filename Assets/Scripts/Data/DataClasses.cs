using UnityEngine;
using System.Collections.Generic;
using GuildMaster.Battle;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class SkillData
    {
        public string id;
        public string name;
        public string description;
        public JobClass requiredClass;
        public int requiredLevel;
        public float damage;
        public float cooldown;
        public int manaCost;
        public string effectType;
    }

    [System.Serializable]
    public class ItemData
    {
        public string id;
        public string name;
        public string description;
        public ItemType itemType;
        public int price;
        public int sellPrice;
        public string iconPath;
        public Dictionary<string, float> stats = new Dictionary<string, float>();
    }

    [System.Serializable]
    public class QuestData
    {
        public string id;
        public string name;
        public string description;
        public int requiredLevel;
        public string questType;
        public Dictionary<string, int> requirements = new Dictionary<string, int>();
        public Dictionary<string, int> rewards = new Dictionary<string, int>();
    }

    [System.Serializable]
    public class FormationData
    {
        public string id;
        public string name;
        public List<Vector2Int> positions = new List<Vector2Int>();
        public string bonusEffect;
        public float bonusAmount;
    }

    public enum ItemType
    {
        Equipment,
        Consumable,
        Material,
        Currency,
        Special
    }
}