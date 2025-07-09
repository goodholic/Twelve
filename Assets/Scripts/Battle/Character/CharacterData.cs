using UnityEngine;
using System;
using GuildMaster.Battle;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class CharacterData
    {
        [Header("기본 정보")]
        public string characterName;
        public int characterIndex;
        public string race = "Human";
        public int star = 1;
        
        [Header("전투 스탯")]
        public int attackPower = 10;
        public int attackSpeed = 1;
        public int health = 100;
        public int cost = 1;
        public int level = 1;
        
        [Header("경험치")]
        public int currentExp = 0;
        public int expToNextLevel = 100;
        
        [Header("추가 스탯")]
        public int maxHP = 100;
        public float moveSpeed = 5f;
        public float attackRange = 1.5f;
        
        [Header("타입 정보")]
        public string rangeType = "Melee";
        public bool isAreaAttack = false;
        public bool isBuffSupport = false;
        public bool isFreeSlotOnly = false;
        
        [Header("레어도")]
        public int initialStar = 1;
        
        [Header("리소스")]
        public Sprite buttonIcon;
        public Sprite frontSprite;
        public Sprite backSprite;
        public GameObject spawnPrefab;
        public GameObject motionPrefab;
        
        [Header("범위 공격")]
        public float areaAttackRadius = 0f;
        
        // Battle.CharacterData와의 호환성을 위한 추가 프로퍼티
        public string id { get; set; }
        public string characterID { get; set; }
        public JobClass jobClass { get; set; }
        public int starLevel => star;
        
        // 생성자
        public CharacterData()
        {
            id = System.Guid.NewGuid().ToString();
            characterID = id;
        }
        
        // 복사 생성자
        public CharacterData(CharacterData other)
        {
            if (other == null) return;
            
            characterName = other.characterName;
            characterIndex = other.characterIndex;
            race = other.race;
            star = other.star;
            attackPower = other.attackPower;
            attackSpeed = other.attackSpeed;
            health = other.health;
            cost = other.cost;
            level = other.level;
            currentExp = other.currentExp;
            expToNextLevel = other.expToNextLevel;
            maxHP = other.maxHP;
            moveSpeed = other.moveSpeed;
            attackRange = other.attackRange;
            rangeType = other.rangeType;
            isAreaAttack = other.isAreaAttack;
            isBuffSupport = other.isBuffSupport;
            isFreeSlotOnly = other.isFreeSlotOnly;
            initialStar = other.initialStar;
            buttonIcon = other.buttonIcon;
            frontSprite = other.frontSprite;
            backSprite = other.backSprite;
            spawnPrefab = other.spawnPrefab;
            motionPrefab = other.motionPrefab;
            areaAttackRadius = other.areaAttackRadius;
            id = other.id;
            characterID = other.characterID;
            jobClass = other.jobClass;
        }
        
        // Battle.CharacterData로 변환
        public GuildMaster.Battle.CharacterData ToBattleCharacterData()
        {
            var battleData = new GuildMaster.Battle.CharacterData();
            battleData.id = this.id;
            battleData.characterID = this.characterID;
            battleData.name = this.characterName;
            battleData.characterName = this.characterName;
            battleData.jobClass = this.jobClass;
            battleData.level = this.level;
            battleData.star = this.star;
            battleData.baseHP = this.health;
            battleData.baseAttack = this.attackPower;
            battleData.cost = this.cost;
            battleData.buttonIcon = this.buttonIcon;
            battleData.frontSprite = this.frontSprite;
            battleData.backSprite = this.backSprite;
            battleData.spawnPrefab = this.spawnPrefab;
            battleData.motionPrefab = this.motionPrefab;
            battleData.moveSpeed = this.moveSpeed;
            battleData.areaAttackRadius = this.areaAttackRadius;
            battleData.isAreaAttack = this.isAreaAttack;
            battleData.isBuffSupport = this.isBuffSupport;
            battleData.rangeType = this.rangeType;
            battleData.isFreeSlotOnly = this.isFreeSlotOnly;
            battleData.race = this.race;
            battleData.experience = this.currentExp;
            
            return battleData;
        }
        
        // Battle.CharacterData에서 가져오기
        public static CharacterData FromBattleCharacterData(GuildMaster.Battle.CharacterData battleData)
        {
            if (battleData == null) return null;
            
            var data = new CharacterData();
            data.id = battleData.id;
            data.characterID = battleData.characterID;
            data.characterName = battleData.characterName;
            data.jobClass = battleData.jobClass;
            data.level = battleData.level;
            data.star = battleData.star;
            data.health = battleData.baseHP;
            data.attackPower = battleData.baseAttack;
            data.cost = battleData.cost;
            data.buttonIcon = battleData.buttonIcon;
            data.frontSprite = battleData.frontSprite;
            data.backSprite = battleData.backSprite;
            data.spawnPrefab = battleData.spawnPrefab;
            data.motionPrefab = battleData.motionPrefab;
            data.moveSpeed = battleData.moveSpeed;
            data.areaAttackRadius = battleData.areaAttackRadius;
            data.isAreaAttack = battleData.isAreaAttack;
            data.isBuffSupport = battleData.isBuffSupport;
            data.rangeType = battleData.rangeType;
            data.isFreeSlotOnly = battleData.isFreeSlotOnly;
            data.race = battleData.race;
            data.currentExp = battleData.experience;
            
            return data;
        }
    }
}