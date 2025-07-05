using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Data;
using GuildMaster.Battle;
using JobClass = GuildMaster.Battle.JobClass;
using Unit = GuildMaster.Battle.Unit;
using Rarity = GuildMaster.Data.Rarity;

namespace GuildMaster.Core
{
    public class DataManager : MonoBehaviour
    {
        private static DataManager _instance;
        public static DataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DataManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("DataManager");
                        _instance = go.AddComponent<DataManager>();
                    }
                }
                return _instance;
            }
        }
        
        [Header("Data Sources")]
        [SerializeField] private string dataPath = "Data/";
        [SerializeField] private bool loadFromResources = true;
        
        // 캐릭터 데이터
        private Dictionary<string, CharacterData> characterDatabase;
        private Dictionary<string, SkillData> skillDatabase;
        private Dictionary<string, ItemData> itemDatabase;
        private Dictionary<string, QuestData> questDatabase;
        private Dictionary<string, DialogueData> dialogueDatabase;
        
        // ScriptableObject 데이터
        [Header("ScriptableObject Data")]
        [SerializeField] private List<CharacterDataSO> characterDataSOs;
        [SerializeField] private List<SkillDataSO> skillDataSOs;
        [SerializeField] private List<ItemDataSO> itemDataSOs;
        [SerializeField] private List<DialogueDataSO> dialogueDataSOs;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeDatabases();
        }
        
        void InitializeDatabases()
        {
            characterDatabase = new Dictionary<string, CharacterData>();
            skillDatabase = new Dictionary<string, SkillData>();
            itemDatabase = new Dictionary<string, ItemData>();
            questDatabase = new Dictionary<string, QuestData>();
            dialogueDatabase = new Dictionary<string, DialogueData>();
        }
        
        public void LoadAllData()
        {
            LoadCharacterData();
            LoadSkillData();
            LoadItemData();
            LoadQuestData();
            LoadDialogueData();
            
            Debug.Log("All game data loaded successfully!");
        }
        
        void LoadCharacterData()
        {
            // ScriptableObject에서 로드
            if (characterDataSOs != null && characterDataSOs.Count > 0)
            {
                foreach (var characterSO in characterDataSOs)
                {
                    if (characterSO != null)
                    {
                        CharacterData data = ConvertFromScriptableObject(characterSO);
                        characterDatabase[data.id] = data;
                    }
                }
            }
            
            // CSV에서 추가 로드
            if (loadFromResources)
            {
                TextAsset csvFile = Resources.Load<TextAsset>(dataPath + "character_data");
                if (csvFile != null)
                {
                    ParseCharacterCSV(csvFile.text);
                }
            }
            
            Debug.Log($"Loaded {characterDatabase.Count} characters");
        }
        
        void ParseCharacterCSV(string csvText)
        {
            string[] lines = csvText.Split('\n');
            
            // 헤더 스킵
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                string[] values = SplitCSVLine(line);
                if (values.Length < 18) continue;
                
                try
                {
                    CharacterData character = new CharacterData
                    {
                        id = values[0],
                        name = values[1],
                        jobClass = ParseJobClass(values[2]),
                        level = int.Parse(values[3]),
                        rarity = ParseRarity(values[4]),
                        baseHP = int.Parse(values[5]),
                        baseMP = int.Parse(values[6]),
                        baseAttack = int.Parse(values[7]),
                        baseDefense = int.Parse(values[8]),
                        baseMagicPower = int.Parse(values[9]),
                        baseSpeed = int.Parse(values[10]),
                        critRate = float.Parse(values[11]),
                        critDamage = float.Parse(values[12]),
                        accuracy = float.Parse(values[13]),
                        evasion = float.Parse(values[14]),
                        skill1Id = values[15],
                        skill2Id = values[16],
                        skill3Id = values[17],
                        description = values.Length > 18 ? values[18] : ""
                    };
                    
                    characterDatabase[character.id] = character;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing character data line {i}: {e.Message}");
                }
            }
        }
        
        string[] SplitCSVLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string current = "";
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.Trim());
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            
            result.Add(current.Trim());
            return result.ToArray();
        }
        
        JobClass ParseJobClass(string jobClassStr)
        {
            if (Enum.TryParse<JobClass>(jobClassStr, out JobClass jobClass))
            {
                return jobClass;
            }
            return JobClass.None;
        }
        
        CharacterRarity ParseRarity(string rarityStr)
        {
            if (Enum.TryParse<CharacterRarity>(rarityStr, out CharacterRarity rarity))
            {
                return rarity;
            }
            return CharacterRarity.Common;
        }
        
        GuildMaster.Data.Rarity ConvertCharacterRarityToBattleRarity(CharacterRarity charRarity)
        {
            return charRarity switch
            {
                CharacterRarity.Common => GuildMaster.Data.Rarity.Common,
                CharacterRarity.Uncommon => GuildMaster.Data.Rarity.Uncommon,
                CharacterRarity.Rare => GuildMaster.Data.Rarity.Rare,
                CharacterRarity.Epic => GuildMaster.Data.Rarity.Epic,
                CharacterRarity.Legendary => GuildMaster.Data.Rarity.Legendary,
                _ => GuildMaster.Data.Rarity.Common
            };
        }
        
        CharacterData ConvertFromScriptableObject(CharacterDataSO so)
        {
            return new CharacterData
            {
                id = so.id,
                name = so.characterName,
                jobClass = so.jobClass,
                level = so.baseLevel,
                rarity = ConvertRarityToCharacterRarity(so.rarity),
                baseHP = so.baseHP,
                baseMP = so.baseMP,
                baseAttack = so.baseAttack,
                baseDefense = so.baseDefense,
                baseMagicPower = so.baseMagicPower,
                baseSpeed = so.baseSpeed,
                critRate = so.critRate,
                critDamage = so.critDamage,
                accuracy = so.accuracy,
                evasion = so.evasion,
                skill1Id = so.skillIds != null && so.skillIds.Count > 0 ? so.skillIds[0] : "",
                skill2Id = so.skillIds != null && so.skillIds.Count > 1 ? so.skillIds[1] : "",
                skill3Id = so.skillIds != null && so.skillIds.Count > 2 ? so.skillIds[2] : "",
                description = so.description,
                iconSprite = so.portrait,
                modelPrefab = so.modelPrefab
            };
        }
        
        void LoadSkillData()
        {
            // 기본 스킬 데이터 추가
            AddDefaultSkills();
            
            // ScriptableObject에서 로드
            if (skillDataSOs != null)
            {
                foreach (var skillSO in skillDataSOs)
                {
                    if (skillSO != null)
                    {
                        SkillData data = ConvertFromScriptableObject(skillSO);
                        skillDatabase[data.id] = data;
                    }
                }
            }
            
            Debug.Log($"Loaded {skillDatabase.Count} skills");
        }
        
        void AddDefaultSkills()
        {
            // 기본 공격
            skillDatabase["101"] = new SkillData
            {
                id = "101",
                name = "기본 공격",
                description = "대상에게 물리 피해를 입힙니다.",
                skillType = GuildMaster.Data.SkillType.Attack,
                targetType = TargetType.Enemy,
                damageMultiplier = 1.0f,
                manaCost = 0,
                cooldown = 0
            };
            
            // 강타
            skillDatabase["102"] = new SkillData
            {
                id = "102",
                name = "강타",
                description = "강력한 일격으로 적에게 150% 피해를 입힙니다.",
                skillType = GuildMaster.Data.SkillType.Attack,
                targetType = TargetType.Enemy,
                damageMultiplier = 1.5f,
                manaCost = 20,
                cooldown = 3
            };
            
            // 회복
            skillDatabase["201"] = new SkillData
            {
                id = "201",
                name = "치유",
                description = "아군의 HP를 회복시킵니다.",
                skillType = GuildMaster.Data.SkillType.Heal,
                targetType = TargetType.Ally,
                healAmount = 100,
                manaCost = 30,
                cooldown = 5
            };
        }
        
        SkillData ConvertFromScriptableObject(SkillDataSO so)
        {
            return new SkillData
            {
                id = so.skillId,
                name = so.skillName,
                description = so.description,
                skillType = (GuildMaster.Data.SkillType)so.skillType,
                targetType = (GuildMaster.Data.TargetType)so.targetType,
                damageMultiplier = so.damageMultiplier,
                healAmount = so.healAmount,
                buffType = (GuildMaster.Data.BuffType)so.buffType,
                buffAmount = so.buffAmount,
                buffDuration = so.buffDuration,
                manaCost = so.manaCost,
                cooldown = so.cooldown,
                range = so.range,
                areaOfEffect = so.areaOfEffect,
                effectPrefab = so.effectPrefab,
                iconSprite = so.skillIcon
            };
        }
        
        void LoadItemData()
        {
            // 기본 아이템 추가
            AddDefaultItems();
            
            // ScriptableObject에서 로드
            if (itemDataSOs != null)
            {
                foreach (var itemSO in itemDataSOs)
                {
                    if (itemSO != null)
                    {
                        ItemData data = ConvertFromScriptableObject(itemSO);
                        itemDatabase[data.id] = data;
                    }
                }
            }
            
            Debug.Log($"Loaded {itemDatabase.Count} items");
        }
        
        void AddDefaultItems()
        {
            // 체력 포션
            itemDatabase["potion_hp_small"] = new ItemData
            {
                id = "potion_hp_small",
                name = "소형 체력 포션",
                description = "HP를 50 회복합니다.",
                itemType = ItemType.Consumable,
                rarity = Data.Rarity.Common,
                value = 50,
                stackable = true,
                maxStack = 99
            };
            
            // 경험치 포션
            itemDatabase["potion_exp_small"] = new ItemData
            {
                id = "potion_exp_small",
                name = "경험치 포션",
                description = "사용 시 경험치를 100 획득합니다.",
                itemType = ItemType.Consumable,
                rarity = Data.Rarity.Uncommon,
                value = 100,
                stackable = true,
                maxStack = 99
            };
        }
        
        ItemData ConvertFromScriptableObject(ItemDataSO so)
        {
            return new ItemData
            {
                id = so.itemId,
                name = so.itemName,
                description = so.description,
                itemType = so.itemType,
                rarity = so.rarity,
                value = so.value,
                stackable = so.stackable,
                maxStack = so.maxStack,
                iconSprite = so.itemIcon
            };
        }
        
        
        void LoadQuestData()
        {
            // 기본 퀘스트 데이터
            AddDefaultQuests();
            
            Debug.Log($"Loaded {questDatabase.Count} quests");
        }
        
        void AddDefaultQuests()
        {
            // 튜토리얼 퀘스트
            questDatabase["tutorial_1"] = new QuestData
            {
                id = "tutorial_1",
                name = "첫 걸음",
                description = "튜토리얼을 완료하세요.",
                questType = QuestType.Main,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective
                    {
                        type = QuestObjectiveType.Talk,
                        targetId = "intro_complete",
                        targetAmount = 1
                    }
                },
                goldReward = 100,
                expReward = 50
            };
        }
        
        void LoadDialogueData()
        {
            // CSV에서 대화 데이터 로드
            if (loadFromResources)
            {
                TextAsset csvFile = Resources.Load<TextAsset>(dataPath + "dialogue_data");
                if (csvFile != null)
                {
                    ParseDialogueCSV(csvFile.text);
                }
            }
            
            Debug.Log($"Loaded {dialogueDatabase.Count} dialogues");
        }
        
        
        void ParseDialogueCSV(string csvText)
        {
            // 대화 데이터 파싱 로직
        }
        
        // 데이터 접근 메서드
        public CharacterData GetCharacterData(string id)
        {
            return characterDatabase.ContainsKey(id) ? characterDatabase[id] : null;
        }
        
        public SkillData GetSkillData(string id)
        {
            return skillDatabase.ContainsKey(id) ? skillDatabase[id] : null;
        }
        
        public ItemData GetItemData(string id)
        {
            return itemDatabase.ContainsKey(id) ? itemDatabase[id] : null;
        }
        
        public QuestData GetQuestData(string id)
        {
            return questDatabase.ContainsKey(id) ? questDatabase[id] : null;
        }
        
        public DialogueData GetDialogueData(string id)
        {
            return dialogueDatabase.ContainsKey(id) ? dialogueDatabase[id] : null;
        }
        
        // 리스트 반환 메서드
        public List<CharacterData> GetAllCharacters()
        {
            return characterDatabase.Values.ToList();
        }
        
        public List<CharacterData> GetCharactersByJobClass(JobClass jobClass)
        {
            return characterDatabase.Values.Where(c => c.jobClass == jobClass).ToList();
        }
        
        public List<CharacterData> GetCharactersByRarity(CharacterRarity rarity)
        {
            return characterDatabase.Values.Where(c => c.rarity == rarity).ToList();
        }
        
        public List<SkillData> GetSkillsByType(GuildMaster.Data.SkillType type)
        {
            return skillDatabase.Values.Where(s => s.skillType == type).ToList();
        }
        
        public List<ItemData> GetItemsByType(ItemType type)
        {
            return itemDatabase.Values.Where(i => i.itemType == type).ToList();
        }
        
        // 유닛 생성 헬퍼
        public Unit CreateUnitFromData(string characterId, int level = 1)
        {
            var data = GetCharacterData(characterId);
            if (data == null) return null;
            
            Unit unit = new Unit(data.name, level, data.jobClass)
            {
                rarity = ConvertCharacterRarityToBattleRarity(data.rarity)
            };
            
            // 기본 스탯 설정 (Unit 클래스의 실제 필드에 직접 할당)
            unit.maxHP = data.baseHP;
            unit.maxMP = data.baseMP;
            unit.attackPower = data.baseAttack;
            unit.defense = data.baseDefense;
            unit.magicPower = data.baseMagicPower;
            unit.speed = data.baseSpeed;
            unit.criticalRate = data.critRate;
            unit.criticalDamage = data.critDamage;
            unit.accuracy = data.accuracy;
            unit.evasion = data.evasion;
            
            // 현재 HP/MP를 최대값으로 설정
            unit.currentHP = unit.maxHP;
            unit.currentMP = unit.maxMP;
            
            // 스킬 ID 추가 (Unit 클래스의 skillIds 리스트 사용)
            if (!string.IsNullOrEmpty(data.skill1Id))
            {
                if (int.TryParse(data.skill1Id, out int skillId))
                {
                    unit.skillIds.Add(skillId);
                }
            }
            
            return unit;
        }
        
        // CharacterData에서 Unit 생성하는 메서드 (호환성용)
        public Unit CreateUnit(CharacterData data)
        {
            if (data == null) return null;
            
            Unit unit = new Unit(data.name, data.level, data.jobClass)
            {
                rarity = ConvertCharacterRarityToBattleRarity(data.rarity)
            };
            
            // 기본 스탯 설정
            unit.maxHP = data.baseHP;
            unit.maxMP = data.baseMP;
            unit.attackPower = data.baseAttack;
            unit.defense = data.baseDefense;
            unit.magicPower = data.baseMagicPower;
            unit.speed = data.baseSpeed;
            unit.criticalRate = data.critRate;
            unit.criticalDamage = data.critDamage;
            unit.accuracy = data.accuracy;
            unit.evasion = data.evasion;
            
            // 현재 HP/MP를 최대값으로 설정
            unit.currentHP = unit.maxHP;
            unit.currentMP = unit.maxMP;
            
            return unit;
        }
        
        CharacterRarity ConvertRarityToCharacterRarity(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common:
                    return CharacterRarity.Common;
                case Rarity.Uncommon:
                    return CharacterRarity.Uncommon;
                case Rarity.Rare:
                    return CharacterRarity.Rare;
                case Rarity.Epic:
                    return CharacterRarity.Epic;
                case Rarity.Legendary:
                    return CharacterRarity.Legendary;
                case Rarity.Mythic:
                    // CharacterRarity에는 Mythic이 없으므로 Legendary로 매핑
                    return CharacterRarity.Legendary;
                default:
                    return CharacterRarity.Common;
            }
        }
        
        public BuildingData GetBuildingData(string buildingId)
        {
            // 간단한 BuildingData 반환 (실제로는 데이터베이스에서 가져와야 함)
            return new BuildingData
            {
                buildingId = buildingId,
                buildingName = buildingId,
                buildingType = BuildingType.TrainingHall,
                maxLevel = 10,
                buildCost = 100,
                baseWoodCost = 50,
                baseStoneCost = 25,
                baseManaCost = 10
            };
        }
    }
}