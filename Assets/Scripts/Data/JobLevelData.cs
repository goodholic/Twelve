using UnityEngine;
using System.Collections.Generic;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class JobLevelData
    {
        public string jobClassName;
        public int level;
        public int requiredExp;
        public float hpBonus;
        public float attackBonus;
        public float defenseBonus;
        public float speedBonus;
        public List<string> unlockedSkills = new List<string>();
        public string description;
    }
}