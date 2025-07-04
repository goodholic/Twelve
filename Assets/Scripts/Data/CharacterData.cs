using UnityEngine;
using System;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class CharacterData
    {
        [Header("기본 정보")]
        public string id;
        public string name;
        public string nameKey; // 로컬라이제이션 키
        public string characterName;
        public string characterID;
        public int characterIndex = 0; // CSV 인덱스
        public JobClass jobClass;
        public int level = 1;
        public CharacterRarity rarity = CharacterRarity.Common;
        public CharacterRace race = CharacterRace.Human;
        public int star = 1;
        public int initialStar = 1;
        
        [Header("기본 스탯")]
        public int baseHP = 100;
        public int baseHp = 100; // CSV 호환성
        public int baseMP = 50;
        public int baseAttack = 10;
        public int baseDefense = 5;
        public int baseMagicPower = 5;
        public int baseSpeed = 10;
        public float baseCriticalRate = 0.1f;
        
        [Header("전투 스탯")]
        public float critRate = 0.1f;
        public float critDamage = 1.5f;
        public float accuracy = 0.95f;
        public float evasion = 0.05f;
        public float moveSpeed = 1.0f;
        public float areaAttackRadius = 0f;
        public bool isAreaAttack = false;
        public bool isBuffSupport = false;
        public string rangeType = "Melee";
        
        [Header("경제")]
        public int cost = 100; // 구매/소환 비용
        public bool isFreeSlotOnly = false;
        
        [Header("스킬")]
        public string skill1Id = "";
        public string skill2Id = "";
        public string skill3Id = "";
        public string[] skillIds = new string[0]; // CSV 호환성
        
        [Header("UI 스프라이트")]
        public string description = "";
        public Sprite iconSprite;
        public Sprite buttonIcon; // UI 버튼용 아이콘
        public Sprite frontSprite; // 전면 스프라이트
        public Sprite backSprite; // 후면 스프라이트
        public Sprite characterSprite; // 캐릭터 스프라이트
        public string iconPath = ""; // 아이콘 경로
        
        [Header("프리팹")]
        public GameObject modelPrefab;
        public GameObject spawnPrefab; // 스폰용 프리팹
        public GameObject motionPrefab; // 모션용 프리팹
        
        [Header("경험치 및 성장")]
        public int experience = 0;
        public int currentExp = 0; // experience와 동일하지만 호환성용
        public int requiredExperience = 100;
        public int maxLevel = 100;
        public float growthRate = 1.0f;
        public float growthRateHp = 1.0f;
        public float growthRateAttack = 1.0f;
        
        // 호환성을 위한 속성들 (setter 포함)
        public int attackPower 
        { 
            get => totalAttack; 
            set => baseAttack = value - (level * 2); 
        }
        
        public int attackSpeed 
        { 
            get => totalSpeed; 
            set => baseSpeed = value - (level * 1); 
        }
        
        public int health 
        { 
            get => currentHP; 
            set => baseHP = value - (level * 10); 
        }
        
        public int starLevel 
        { 
            get => star; 
            set => star = value; 
        }
        
        public int expToNextLevel 
        { 
            get => requiredExperience - experience; 
            set => requiredExperience = experience + value; 
        }
        
        public float attackRange 
        { 
            get => rangeType == "Ranged" ? 5.0f : 1.5f; 
            set => rangeType = value > 3.0f ? "Ranged" : "Melee"; 
        }
        
        // 계산된 스탯
        public int currentHP => baseHP + (level * 10);
        public int currentMP => baseMP + (level * 5);
        public int totalAttack => baseAttack + (level * 2);
        public int totalDefense => baseDefense + (level * 1);
        public int totalMagicPower => baseMagicPower + (level * 2);
        public int totalSpeed => baseSpeed + (level * 1);
        
        // 최대 스탯 (setter 포함)
        public int maxHP { get => currentHP; set => baseHP = value - (level * 10); }
        public int maxMP { get => currentMP; set => baseMP = value - (level * 5); }
        
        public CharacterData()
        {
            // 기본값 설정
            id = System.Guid.NewGuid().ToString();
            name = "Unknown Character";
            nameKey = "character_unknown";
            characterName = name;
            characterID = id;
            currentExp = experience;
        }
        
        public CharacterData(string charId, string charName, JobClass job, int charLevel, CharacterRarity charRarity)
        {
            id = charId;
            name = charName;
            nameKey = $"character_{charName.ToLower()}";
            characterName = charName;
            characterID = charId;
            jobClass = job;
            level = charLevel;
            rarity = charRarity;
            currentExp = experience;
            
            // 직업별 기본 스탯 설정
            SetBaseStatsByClass(job);
        }
        
        private void SetBaseStatsByClass(JobClass job)
        {
            switch (job)
            {
                case JobClass.Warrior:
                    baseHP = 150;
                    baseHp = 150;
                    baseAttack = 15;
                    baseDefense = 12;
                    baseMagicPower = 5;
                    baseSpeed = 8;
                    rangeType = "Melee";
                    cost = 150;
                    break;
                case JobClass.Mage:
                    baseHP = 80;
                    baseHp = 80;
                    baseAttack = 5;
                    baseDefense = 5;
                    baseMagicPower = 20;
                    baseSpeed = 10;
                    rangeType = "Ranged";
                    isAreaAttack = true;
                    areaAttackRadius = 2.0f;
                    cost = 200;
                    break;
                case JobClass.Archer:
                    baseHP = 100;
                    baseHp = 100;
                    baseAttack = 12;
                    baseDefense = 8;
                    baseMagicPower = 8;
                    baseSpeed = 15;
                    rangeType = "Ranged";
                    cost = 120;
                    break;
                case JobClass.Priest:
                    baseHP = 90;
                    baseHp = 90;
                    baseAttack = 5;
                    baseDefense = 8;
                    baseMagicPower = 15;
                    baseSpeed = 10;
                    rangeType = "Ranged";
                    isBuffSupport = true;
                    cost = 180;
                    break;
                case JobClass.Rogue:
                    baseHP = 90;
                    baseHp = 90;
                    baseAttack = 18;
                    baseDefense = 6;
                    baseMagicPower = 8;
                    baseSpeed = 18;
                    rangeType = "Melee";
                    cost = 130;
                    break;
                case JobClass.Paladin:
                    baseHP = 140;
                    baseHp = 140;
                    baseAttack = 12;
                    baseDefense = 15;
                    baseMagicPower = 10;
                    baseSpeed = 7;
                    rangeType = "Melee";
                    cost = 250;
                    break;
            }
        }
        
        public void AddExperience(int exp)
        {
            experience += exp;
            currentExp = experience;
            CheckLevelUp();
        }
        
        private void CheckLevelUp()
        {
            while (experience >= requiredExperience && level < maxLevel)
            {
                experience -= requiredExperience;
                level++;
                requiredExperience = Mathf.RoundToInt(requiredExperience * 1.2f);
            }
            currentExp = experience;
        }
        
        public CharacterData Clone()
        {
            return new CharacterData
            {
                id = this.id + "_clone",
                name = this.name,
                nameKey = this.nameKey,
                characterName = this.characterName,
                characterID = this.characterID + "_clone",
                characterIndex = this.characterIndex,
                jobClass = this.jobClass,
                level = this.level,
                rarity = this.rarity,
                race = this.race,
                star = this.star,
                initialStar = this.initialStar,
                baseHP = this.baseHP,
                baseHp = this.baseHp,
                baseMP = this.baseMP,
                baseAttack = this.baseAttack,
                baseDefense = this.baseDefense,
                baseMagicPower = this.baseMagicPower,
                baseSpeed = this.baseSpeed,
                baseCriticalRate = this.baseCriticalRate,
                critRate = this.critRate,
                critDamage = this.critDamage,
                accuracy = this.accuracy,
                evasion = this.evasion,
                moveSpeed = this.moveSpeed,
                areaAttackRadius = this.areaAttackRadius,
                isAreaAttack = this.isAreaAttack,
                isBuffSupport = this.isBuffSupport,
                rangeType = this.rangeType,
                cost = this.cost,
                isFreeSlotOnly = this.isFreeSlotOnly,
                skill1Id = this.skill1Id,
                skill2Id = this.skill2Id,
                skill3Id = this.skill3Id,
                skillIds = this.skillIds,
                description = this.description,
                iconSprite = this.iconSprite,
                buttonIcon = this.buttonIcon,
                frontSprite = this.frontSprite,
                backSprite = this.backSprite,
                characterSprite = this.characterSprite,
                iconPath = this.iconPath,
                modelPrefab = this.modelPrefab,
                spawnPrefab = this.spawnPrefab,
                motionPrefab = this.motionPrefab,
                experience = this.experience,
                currentExp = this.currentExp,
                requiredExperience = this.requiredExperience,
                maxLevel = this.maxLevel,
                growthRate = this.growthRate,
                growthRateHp = this.growthRateHp,
                growthRateAttack = this.growthRateAttack
            };
        }
    }
} 