using UnityEngine;
using GuildMaster.Data;

namespace GuildMaster.Battle
{
    public class UnitBehaviour : MonoBehaviour
    {
        [System.NonSerialized]
        public UnitStatus unit;
        
        public void Initialize(UnitStatus unitData)
        {
            unit = unitData;
        }
        
        public void Initialize(string name, int level, JobClass jobClass, Rarity rarity)
        {
            unit = new UnitStatus(name, level, jobClass);
            unit.rarity = rarity;
        }
    }
}