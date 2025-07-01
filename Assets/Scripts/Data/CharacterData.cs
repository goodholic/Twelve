using UnityEngine;
using GuildMaster.Data;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class CharacterData
    {
        public string id;
        public string name;
        public JobClass jobClass;
        public int level;
        public CharacterRarity rarity;
        public int baseHP;
        public int baseMP;
        public int baseAttack;
        public int baseDefense;
        public int baseMagicPower;
        public int baseSpeed;
        public float critRate;
        public float critDamage;
        public float accuracy;
        public float evasion;
        public string skill1Id;
        public string skill2Id;
        public string skill3Id;
        public string description;
        public Sprite iconSprite;
        public GameObject modelPrefab;
        
        // 호환성을 위한 추가 필드들
        public string characterName
        {
            get => name;
            set => name = value;
        }
        
        public string characterID
        {
            get => id;
            set => id = value;
        }
        
        public int starLevel { get; set; } = 1;
        public CharacterRace race { get; set; }
        public int star
        {
            get => starLevel;
            set => starLevel = value;
        }
        
        public float attackPower
        {
            get => baseAttack;
            set => baseAttack = (int)value;
        }
        
        public float baseHealth
        {
            get => baseHP;
            set => baseHP = (int)value;
        }
        
        public float health
        {
            get => baseHP;
            set => baseHP = (int)value;
        }
        
        public float maxHP
        {
            get => baseHP;
            set => baseHP = (int)value;
        }
        
        public float maxHealth
        {
            get => baseHP;
            set => baseHP = (int)value;
        }
        
        public float attackRange { get; set; } = 1.5f;
        public float range
        {
            get => attackRange;
            set => attackRange = value;
        }
        
        public float attackSpeed { get; set; } = 1f;
        
        // 추가 필드들
        public int attackTargetType { get; set; } = 2;
        public int attackShapeType { get; set; } = 0;
        public int rangeType { get; set; } = 1;
        public int tribe { get; set; } = 0;
        public bool isAreaAttack { get; set; } = false;
        public float areaAttackRadius { get; set; } = 1.5f;
        public bool isBuffSupport { get; set; } = false;
        public int cost { get; set; } = 1;
        public float moveSpeed { get; set; } = 1f;
        public int currentExp { get; set; } = 0;
        public int expToNextLevel { get; set; } = 100;
        public bool isFreeSlotOnly { get; set; } = false;
        public Sprite characterSprite { get; set; }
        public Sprite frontSprite { get; set; }
        public Sprite backSprite { get; set; }
        public GameObject spawnPrefab { get; set; }
        public GameObject motionPrefab { get; set; }
        public string prefabName { get; set; }
        public Sprite buttonIcon { get; set; }
        public int initialStar { get; set; } = 1;
        public int characterIndex { get; set; } = -1;
        
        public CharacterRarity Rarity => rarity;
    }
} 