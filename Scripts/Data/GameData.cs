using System;
using System.Collections.Generic;
using UnityEngine;
using GuildMaster.Battle;

namespace GuildMaster.Data
{
    // 캐릭터 데이터
    [System.Serializable]
    public class CharacterData
    {
        public string id;
        public string name;
        public JobClass jobClass;
        public int level;
        public Rarity rarity;
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
    }
    
    // 스킬 데이터
    [System.Serializable]
    public class SkillData
    {
        public string id;
        public string name;
        public string description;
        public SkillType skillType;
        public TargetType targetType;
        public float damageMultiplier;
        public float healAmount;
        public BuffType buffType;
        public float buffAmount;
        public int buffDuration;
        public int manaCost;
        public float cooldown;
        public int range;
        public int areaOfEffect;
        public GameObject effectPrefab;
        public Sprite iconSprite;
    }
    
    // 아이템 데이터
    [System.Serializable]
    public class ItemData
    {
        public string id;
        public string name;
        public string description;
        public ItemType itemType;
        public Rarity rarity;
        public int value;
        public bool stackable;
        public int maxStack;
        public Dictionary<string, float> stats;
        public Sprite iconSprite;
    }
    
    // 건물 데이터
    [System.Serializable]
    public class BuildingData
    {
        public string id;
        public string name;
        public string buildingName;
        public string description;
        public Core.GuildManager.BuildingType buildingType;
        public BuildingCategory category;
        public int maxLevel;
        public int baseGoldCost;
        public int baseWoodCost;
        public int baseStoneCost;
        public int baseManaCost;
        public float baseConstructionTime;
        public int requiredGuildLevel;
        public int sizeX;
        public int sizeY;
        public GameObject modelPrefab;
        public GameObject buildingPrefab;
        public Sprite iconSprite;
        
        public Dictionary<string, int> GetRefundResources(int level = 1)
        {
            var refund = new Dictionary<string, int>();
            refund["gold"] = baseGoldCost * level / 2;
            refund["wood"] = baseWoodCost * level / 2;
            refund["stone"] = baseStoneCost * level / 2;
            refund["mana"] = baseManaCost * level / 2;
            return refund;
        }
    }
    
    // 퀘스트 데이터
    [System.Serializable]
    public class QuestData
    {
        public string id;
        public string name;
        public string description;
        public QuestType questType;
        public List<QuestObjective> objectives;
        public int goldReward;
        public int expReward;
        public List<string> itemRewards;
        public string prerequisiteQuest;
        public int requiredLevel;
    }
    
    [System.Serializable]
    public class QuestObjective
    {
        public ObjectiveType type;
        public string target;
        public int requiredAmount;
        public int currentAmount;
        public bool isCompleted;
    }
    
    // 대화 데이터
    [System.Serializable]
    public class DialogueData
    {
        public string id;
        public string dialogueName;
        public string speaker;
        public string characterName;
        public string content;
        public List<DialogueChoice> choices;
        public string nextDialogueId;
        public string nextId;
        public string portraitId;
        public string backgroundId;
        public string background;
        public string bgm;
        public string sfx;
        public string expression;
        public string effect;
        public float duration;
        
        public void Initialize()
        {
            // 초기화 로직
        }

        public DialogueData GetDialogue(string dialogueId)
        {
            // 특정 대화 데이터 반환
            if (id == dialogueId)
                return this;
            return null;
        }
    }
    
    [System.Serializable]
    public class DialogueChoice
    {
        public string text;
        public string nextDialogueId;
        public string requirementType;
        public int requirementValue;
    }
    
    // Enum 정의
    public enum SkillType
    {
        Attack,
        Defense,
        Heal,
        Buff,
        Debuff,
        Summon,
        Special,
        Active,
        Passive,
        Ultimate
    }
    
    public enum TargetType
    {
        Self,
        Ally,
        Enemy,
        AllAllies,
        AllEnemies,
        Area,
        Random
    }
    
    public enum BuffType
    {
        None,
        AttackUp,
        DefenseUp,
        SpeedUp,
        CritRateUp,
        Regeneration,
        Shield,
        Immunity
    }
    
    public enum ItemType
    {
        Consumable,
        Equipment,
        Material,
        Quest,
        Currency,
        Special
    }
    
    public enum QuestType
    {
        Main,
        Side,
        Daily,
        Weekly,
        Achievement,
        Hidden
    }
    
    public enum ObjectiveType
    {
        Kill,
        Collect,
        Build,
        Upgrade,
        Reach,
        Complete,
        Talk,
        Explore
    }

    // 건물 카테고리 enum 추가
    public enum BuildingCategory
    {
        Core,
        Production,
        Training,
        Research,
        Defense,
        Decoration
    }

    // 건물 타입 enum (Data 네임스페이스용)
    public enum BuildingType
    {
        GuildHall,
        Barracks,
        ArcheryRange,
        MagesTower,
        Temple,
        Workshop,
        Storage,
        Wall,
        Gate,
        Garden
    }
}