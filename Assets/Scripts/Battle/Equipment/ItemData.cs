using UnityEngine;
using System;
using GuildMaster.Battle;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class ItemData
    {
        [Header("기본 정보")]
        public string itemId;
        public string itemName;
        public string description;
        
        // 호환성을 위한 속성들
        public string id { get => itemId; set => itemId = value; }
        public string name { get => itemName; set => itemName = value; }
        public string nameKey = ""; // 로컬라이제이션 키
        public string descriptionKey = ""; // 로컬라이제이션 키
        public float[] effectValues = new float[0]; // 효과 값들 배열
        public string iconPath = ""; // 아이콘 경로
        public Sprite iconSprite { get => itemIcon; set => itemIcon = value; } // 호환성
        
        [Header("타입 및 등급")]
        public ItemType itemType;
        public Rarity rarity = Rarity.Common;
        public ItemCategory category = ItemCategory.Consumable;
        
        [Header("스택")]
        public bool stackable = true;
        public int maxStack = 99;
        public int currentStack = 1;
        
        [Header("가치")]
        public int value = 100;
        public int sellPrice = 50;
        public int buyPrice = 100;
        
        [Header("장비 관련")]
        public bool isEquipment = false;
        public EquipmentType equipmentType = EquipmentType.Weapon;
        public EquipmentSlot equipmentSlot = EquipmentSlot.None;
        
        [Header("장비 스탯")]
        public int attack = 0;
        public int defense = 0;
        public int magicPower = 0;
        public int speed = 0;
        public int hp = 0;
        public int mp = 0;
        public float critRate = 0f;
        public float critDamage = 0f;
        
        [Header("소모품 관련")]
        public bool isConsumable = false;
        public ConsumableType consumableType = ConsumableType.Potion;
        public int healAmount = 0;
        public int manaAmount = 0;
        public float buffAmount = 0f;
        public float buffDuration = 0f;
        
        [Header("요구사항")]
        public int requiredLevel = 1;
        public JobClass requiredClass = JobClass.Warrior;
        public bool classRestricted = false;
        
        [Header("특수 효과")]
        public bool hasSpecialEffect = false;
        public string specialEffectDescription = "";
        public SpecialEffectType specialEffectType = SpecialEffectType.None;
        public float specialEffectValue = 0f;
        
        [Header("비주얼")]
        public Sprite itemIcon;
        public GameObject itemModel;
        public GameObject equipEffect;
        
        [Header("기타")]
        public bool questItem = false;
        public bool tradeable = true;
        public bool destroyable = true;
        public string loreText = "";
        
        public ItemData()
        {
            itemId = System.Guid.NewGuid().ToString();
            itemName = "Unknown Item";
            description = "";
        }
        
        public ItemData(string id, string name, ItemType type, Rarity itemRarity)
        {
            itemId = id;
            itemName = name;
            itemType = type;
            rarity = itemRarity;
        }
        
        public bool CanUse(int userLevel, JobClass userClass)
        {
            if (userLevel < requiredLevel)
                return false;
                
            if (classRestricted && userClass != requiredClass)
                return false;
                
            return true;
        }
        
        public bool CanEquip(int userLevel, JobClass userClass)
        {
            if (!isEquipment)
                return false;
                
            return CanUse(userLevel, userClass);
        }
        
        public int GetTotalValue()
        {
            return value * currentStack;
        }
        
        public int GetTotalSellPrice()
        {
            return sellPrice * currentStack;
        }
        
        public string GetItemInfo()
        {
            string info = $"<b>{itemName}</b>\n";
            info += $"등급: {GetRarityName()}\n";
            info += $"타입: {GetTypeName()}\n";
            
            if (!string.IsNullOrEmpty(description))
                info += $"\n{description}\n";
            
            if (isEquipment)
            {
                info += "\n<b>장비 스탯:</b>\n";
                if (attack > 0) info += $"공격력: +{attack}\n";
                if (defense > 0) info += $"방어력: +{defense}\n";
                if (magicPower > 0) info += $"마법력: +{magicPower}\n";
                if (speed > 0) info += $"속도: +{speed}\n";
                if (hp > 0) info += $"HP: +{hp}\n";
                if (mp > 0) info += $"MP: +{mp}\n";
                if (critRate > 0) info += $"치명타율: +{critRate * 100:F1}%\n";
                if (critDamage > 0) info += $"치명타 데미지: +{critDamage * 100:F1}%\n";
            }
            
            if (isConsumable)
            {
                info += "\n<b>소모품 효과:</b>\n";
                if (healAmount > 0) info += $"HP 회복: {healAmount}\n";
                if (manaAmount > 0) info += $"MP 회복: {manaAmount}\n";
                if (buffAmount > 0) info += $"버프 효과: +{buffAmount} ({buffDuration}초)\n";
            }
            
            if (requiredLevel > 1)
                info += $"\n요구 레벨: {requiredLevel}\n";
                
            if (classRestricted)
                info += $"요구 직업: {requiredClass}\n";
                
            info += $"\n가치: {value} 골드";
            
            if (stackable && maxStack > 1)
                info += $"\n스택: {currentStack}/{maxStack}";
                
            return info;
        }
        
        private string GetRarityName()
        {
            return rarity switch
            {
                Rarity.Common => "일반",
                Rarity.Uncommon => "고급",
                Rarity.Rare => "희귀",
                Rarity.Epic => "영웅",
                Rarity.Legendary => "전설",
                _ => "알 수 없음"
            };
        }
        
        private string GetTypeName()
        {
            return itemType switch
            {
                ItemType.Weapon => "무기",
                ItemType.Armor => "방어구",
                ItemType.Accessory => "장신구",
                ItemType.Consumable => "소모품",
                ItemType.Material => "재료",
                ItemType.Quest => "퀘스트",
                ItemType.Misc => "기타",
                _ => "알 수 없음"
            };
        }
    }
    
    public enum ItemType
    {
        Weapon,
        Armor,
        Accessory,
        Consumable,
        Material,
        Quest,
        Misc
    }
    
    public enum ItemCategory
    {
        Equipment,
        Consumable,
        Material,
        Quest,
        Misc
    }
    
    public enum EquipmentType
    {
        Weapon,
        Armor,
        Accessory,
        Special
    }
    
    public enum EquipmentSlot
    {
        None,
        MainHand,
        OffHand,
        Head,
        Chest,
        Legs,
        Feet,
        Ring,
        Necklace,
        Earring
    }
    
    public enum ConsumableType
    {
        Potion,
        Food,
        Scroll,
        Elixir,
        Buff,
        Special
    }
    
    public enum SpecialEffectType
    {
        None,
        LifeSteal,
        ManaLeech,
        CritBonus,
        DamageReflect,
        StatusResistance,
        ExpBonus,
        GoldBonus,
        Regeneration,
        ElementalDamage
    }
} 