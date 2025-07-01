using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Data;
using GuildMaster.Battle;

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
        private Dictionary<string, GuildMaster.Data.CharacterData> characterDatabase;
        private Dictionary<string, SkillData> skillDatabase;
        private Dictionary<string, ItemData> itemDatabase;
        private Dictionary<string, BuildingData> buildingDatabase;
        private Dictionary<string, QuestData> questDatabase;
        private Dictionary<string, DialogueData> dialogueDatabase;
        
        // ScriptableObject 데이터
        [Header("ScriptableObject Data")]
        [SerializeField] private List<SkillDataSO> skillDataSOs;
        [SerializeField] private List<ItemDataSO> itemDataSOs;
        [SerializeField] private List<BuildingDataSO> buildingDataSOs;
        
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
            characterDatabase = new Dictionary<string, GuildMaster.Data.CharacterData>();
            skillDatabase = new Dictionary<string, SkillData>();
            itemDatabase = new Dictionary<string, ItemData>();
            buildingDatabase = new Dictionary<string, BuildingData>();
            questDatabase = new Dictionary<string, QuestData>();
            dialogueDatabase = new Dictionary<string, DialogueData>();
        }
        
        public void LoadAllData()
        {
            LoadCharacterData();
            LoadSkillData();
            LoadItemData();
            LoadBuildingData();
            LoadQuestData();
            LoadDialogueData();
            
            Debug.Log("All game data loaded successfully!");
        }
        
        void LoadCharacterData()
        {
            // CSV에서 로드
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
                    GuildMaster.Data.CharacterData character = new GuildMaster.Data.CharacterData
                    {
                        id = values[0],
                        name = values[1],
                        jobClass = ParseJobClass(values[2]),
                        level = int.Parse(values[3]),
                        rarity = (GuildMaster.Data.CharacterRarity)Enum.Parse(typeof(GuildMaster.Data.CharacterRarity), values[4], true),
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
        
        GuildMaster.Data.JobClass ParseJobClass(string jobClassStr)
        {
            if (Enum.TryParse<GuildMaster.Data.JobClass>(jobClassStr, out GuildMaster.Data.JobClass jobClass))
            {
                return jobClass;
            }
            return GuildMaster.Data.JobClass.Warrior;
        }
        
        GuildMaster.Battle.JobClass ConvertJobClass(GuildMaster.Data.JobClass dataJobClass)
        {
            return dataJobClass switch
            {
                GuildMaster.Data.JobClass.Warrior => GuildMaster.Battle.JobClass.Warrior,
                GuildMaster.Data.JobClass.Mage => GuildMaster.Battle.JobClass.Mage,
                GuildMaster.Data.JobClass.Archer => GuildMaster.Battle.JobClass.Ranger,
                GuildMaster.Data.JobClass.Priest => GuildMaster.Battle.JobClass.Priest,
                GuildMaster.Data.JobClass.Rogue => GuildMaster.Battle.JobClass.Assassin,
                GuildMaster.Data.JobClass.Paladin => GuildMaster.Battle.JobClass.Knight,
                GuildMaster.Data.JobClass.Berserker => GuildMaster.Battle.JobClass.Warrior,
                GuildMaster.Data.JobClass.Necromancer => GuildMaster.Battle.JobClass.Sage,
                _ => GuildMaster.Battle.JobClass.Warrior
            };
        }
        
        GuildMaster.Battle.Rarity ParseRarity(string rarityStr)
        {
            if (Enum.TryParse<GuildMaster.Battle.Rarity>(rarityStr, out GuildMaster.Battle.Rarity rarity))
            {
                return rarity;
            }
            return GuildMaster.Battle.Rarity.Common;
        }
        
        GuildMaster.Battle.Rarity ConvertRarity(GuildMaster.Data.CharacterRarity dataRarity)
        {
            return dataRarity switch
            {
                GuildMaster.Data.CharacterRarity.Common => GuildMaster.Battle.Rarity.Common,
                GuildMaster.Data.CharacterRarity.Uncommon => GuildMaster.Battle.Rarity.Uncommon,
                GuildMaster.Data.CharacterRarity.Rare => GuildMaster.Battle.Rarity.Rare,
                GuildMaster.Data.CharacterRarity.Epic => GuildMaster.Battle.Rarity.Epic,
                GuildMaster.Data.CharacterRarity.Legendary => GuildMaster.Battle.Rarity.Legendary,
                _ => GuildMaster.Battle.Rarity.Common
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
                rarity = GuildMaster.Battle.Rarity.Common,
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
                rarity = GuildMaster.Battle.Rarity.Uncommon,
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
        
        void LoadBuildingData()
        {
            // ScriptableObject에서 로드
            if (buildingDataSOs != null && buildingDataSOs.Count > 0)
            {
                foreach (var buildingSO in buildingDataSOs)
                {
                    if (buildingSO != null)
                    {
                        BuildingData data = ConvertFromScriptableObject(buildingSO);
                        buildingDatabase[data.id] = data;
                    }
                }
            }
            
            // CSV에서 추가 로드
            if (loadFromResources)
            {
                TextAsset csvFile = Resources.Load<TextAsset>(dataPath + "building_data");
                if (csvFile != null)
                {
                    ParseBuildingCSV(csvFile.text);
                }
            }
            
            // 기본 건물 데이터
            AddDefaultBuildings();
            
            Debug.Log($"Loaded {buildingDatabase.Count} buildings");
        }
        
        void AddDefaultBuildings()
        {
            // 길드 홀
            buildingDatabase["guild_hall"] = new BuildingData
            {
                id = "guild_hall",
                name = "길드 홀",
                description = "길드의 중심 건물입니다.",
                buildingType = (GuildMaster.Core.GuildManager.BuildingType)GuildMaster.Data.BuildingType.GuildHall,
                maxLevel = 10,
                baseGoldCost = 0,
                baseWoodCost = 0,
                baseStoneCost = 0,
                baseConstructionTime = 0,
                sizeX = 3,
                sizeY = 3
            };
            
            // 병영
            buildingDatabase["barracks"] = new BuildingData
            {
                id = "barracks",
                name = "병영",
                description = "전사와 기사를 훈련시킬 수 있습니다.",
                buildingType = (GuildMaster.Core.GuildManager.BuildingType)GuildMaster.Data.BuildingType.Barracks,
                maxLevel = 5,
                baseGoldCost = 500,
                baseWoodCost = 200,
                baseStoneCost = 100,
                baseConstructionTime = 60,
                sizeX = 2,
                sizeY = 2
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
                description = "길드 홀을 건설하세요.",
                questType = QuestType.Main,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective
                    {
                        type = ObjectiveType.Build,
                        target = "guild_hall",
                        requiredAmount = 1
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
        
        void ParseBuildingCSV(string csvText)
        {
            string[] lines = csvText.Split('\n');
            
            // 헤더 스킵
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                string[] values = SplitCSVLine(line);
                if (values.Length < 14) continue;
                
                try
                {
                    BuildingData building = new BuildingData
                    {
                        id = values[0],
                        name = values[1],
                        buildingType = (GuildMaster.Core.GuildManager.BuildingType)ParseBuildingType(values[2]),
                        category = ParseBuildingCategory(values[3]),
                        sizeX = int.Parse(values[4]),
                        sizeY = int.Parse(values[5]),
                        baseGoldCost = int.Parse(values[6]),
                        baseWoodCost = int.Parse(values[7]),
                        baseStoneCost = int.Parse(values[8]),
                        baseManaCost = int.Parse(values[9]),
                        baseConstructionTime = float.Parse(values[10]),
                        requiredGuildLevel = int.Parse(values[11]),
                        maxLevel = int.Parse(values[12]),
                        description = values[13]
                    };
                    
                    buildingDatabase[building.id] = building;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing building data line {i}: {e.Message}");
                }
            }
        }
        
        GuildMaster.Data.BuildingType ParseBuildingType(string typeStr)
        {
            if (Enum.TryParse<GuildMaster.Data.BuildingType>(typeStr, out GuildMaster.Data.BuildingType type))
            {
                return type;
            }
            return GuildMaster.Data.BuildingType.GuildHall;
        }
        
        GuildMaster.Data.BuildingCategory ParseBuildingCategory(string categoryStr)
        {
            if (Enum.TryParse<GuildMaster.Data.BuildingCategory>(categoryStr, out GuildMaster.Data.BuildingCategory category))
            {
                return category;
            }
            return GuildMaster.Data.BuildingCategory.Core;
        }
        
        BuildingData ConvertFromScriptableObject(BuildingDataSO so)
        {
            return new BuildingData
            {
                id = so.buildingId,
                name = so.buildingName,
                description = so.description,
                buildingType = (GuildMaster.Core.GuildManager.BuildingType)so.buildingType,
                category = (GuildMaster.Data.BuildingCategory)so.category,
                sizeX = so.sizeX,
                sizeY = so.sizeY,
                baseGoldCost = so.buildCost.gold,
                baseWoodCost = so.buildCost.wood,
                baseStoneCost = so.buildCost.stone,
                baseManaCost = so.buildCost.mana,
                baseConstructionTime = so.buildTime,
                requiredGuildLevel = so.requiredGuildLevel,
                maxLevel = so.maxLevel,
                iconSprite = so.buildingIcon,
                modelPrefab = so.buildingPrefab
            };
        }
        
        void ParseDialogueCSV(string csvText)
        {
            // 대화 데이터 파싱 로직
        }
        
        // 데이터 접근 메서드
        public GuildMaster.Data.CharacterData GetCharacterData(string id)
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
        
        public BuildingData GetBuildingData(string id)
        {
            return buildingDatabase.ContainsKey(id) ? buildingDatabase[id] : null;
        }
        
        public QuestData GetQuestData(string id)
        {
            return questDatabase.ContainsKey(id) ? questDatabase[id] : null;
        }
        
        public DialogueData GetDialogueData(string id)
        {
            return dialogueDatabase.ContainsKey(id) ? dialogueDatabase[id] : null;
        }
        
        // BuildingDataSO 접근 메서드 추가
        public BuildingDataSO GetBuildingDataSO(string id)
        {
            if (buildingDataSOs != null)
            {
                return buildingDataSOs.FirstOrDefault(b => b.buildingId == id);
            }
            return null;
        }
        
        public List<BuildingDataSO> GetAllBuildingData()
        {
            return buildingDataSOs ?? new List<BuildingDataSO>();
        }
        
        // 리스트 반환 메서드
        public List<GuildMaster.Data.CharacterData> GetAllCharacters()
        {
            return characterDatabase.Values.ToList();
        }
        
        public List<GuildMaster.Data.CharacterData> GetCharactersByJobClass(GuildMaster.Data.JobClass jobClass)
        {
            return characterDatabase.Values.Where(c => c.jobClass == jobClass).ToList();
        }
        
        public List<GuildMaster.Data.CharacterData> GetCharactersByRarity(GuildMaster.Data.CharacterRarity rarity)
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
        public GuildMaster.Battle.Unit CreateUnitFromData(string characterId, int level = 1)
        {
            var data = GetCharacterData(characterId);
            if (data == null) return null;
            
            // Data.JobClass를 Battle.JobClass로 변환
            GuildMaster.Battle.JobClass battleJobClass = ConvertJobClass(data.jobClass);
            
            GuildMaster.Battle.Unit unit = new GuildMaster.Battle.Unit(
                data.name, 
                level, 
                battleJobClass, 
                ConvertRarity(data.rarity)
            );
            
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
    }
}