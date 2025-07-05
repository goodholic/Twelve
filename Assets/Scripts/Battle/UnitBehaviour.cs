using UnityEngine;
using GuildMaster.Data;

namespace GuildMaster.Battle
{
    public class UnitBehaviour : MonoBehaviour
    {
        public Unit unit;
        
        public void Initialize(Unit unitData)
        {
            unit = unitData;
        }
        
        public void Initialize(string name, int level, JobClass jobClass, Rarity rarity)
        {
            unit = new Unit(name, level, jobClass);
            unit.rarity = rarity;
        }
    }
}