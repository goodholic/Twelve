using UnityEngine;
using GuildMaster.Battle;
using Unit = GuildMaster.Battle.UnitStatus;

namespace GuildMaster.Data
{
    [CreateAssetMenu(fileName = "ItemData", menuName = "GuildMaster/Data/Item", order = 3)]
    public class ItemDataSO : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemId;
        public string itemName;
        [TextArea(2, 4)]
        public string description;
        
        [Header("Type and Rarity")]
        public ItemType itemType;
        public Rarity rarity = Rarity.Common;
        
        [Header("Stack")]
        public bool stackable = true;
        public int maxStack = 99;
        
        [Header("Value")]
        public int value = 100;
        public int sellPrice = 50;
        
        [Header("Visuals")]
        public Sprite itemIcon;
        public GameObject itemModel;
        
        [Header("Equipment Stats")]
        public bool isEquipment = false;
        public EquipmentType equipmentType;
        public EquipmentStats equipmentStats;
        
        [Header("Consumable Effects")]
        public bool isConsumable = false;
        public ConsumableEffect[] consumableEffects;
        
        [Header("Requirements")]
        public int requiredLevel = 1;
        public JobClass requiredClass = JobClass.None;
        
        [System.Serializable]
        public class EquipmentStats
        {
            public int attack;
            public int defense;
            public int magicPower;
            public int speed;
            public float critRate;
            public float critDamage;
            public int hp;
            public int mp;
        }
        
        [System.Serializable]
        public class ConsumableEffect
        {
            public ConsumableEffectType effectType;
            public float effectValue;
            public float duration;
        }
        
        public enum EquipmentType
        {
            Weapon,
            Armor,
            Accessory,
            Special
        }
        
        public enum ConsumableEffectType
        {
            HealHP,
            HealMP,
            BuffAttack,
            BuffDefense,
            BuffSpeed,
            GainExp,
            RemoveDebuff,
            Resurrect
        }
        
        // 아이템 사용 가능 여부 확인
        public bool CanUse(Unit unit)
        {
            if (unit.level < requiredLevel)
                return false;
                
            if (requiredClass != JobClass.None && unit.jobClass != requiredClass)
                return false;
                
            return true;
        }
        
        // 아이템 효과 적용
        public void ApplyEffects(Unit unit)
        {
            if (!isConsumable || consumableEffects == null)
                return;
                
            foreach (var effect in consumableEffects)
            {
                switch (effect.effectType)
                {
                    case ConsumableEffectType.HealHP:
                        unit.Heal(effect.effectValue);
                        break;
                        
                    case ConsumableEffectType.HealMP:
                        unit.currentMP = Mathf.Min(unit.currentMP + effect.effectValue, unit.maxMP);
                        break;
                        
                    case ConsumableEffectType.GainExp:
                        unit.AddExperience((int)effect.effectValue);
                        break;
                        
                    // TODO: 버프 효과 구현
                }
            }
        }
    }
}