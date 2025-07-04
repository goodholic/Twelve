using UnityEngine;
using System;
using System.Collections; // IEnumerator를 위해 추가
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Core; // ResourceType을 위해 추가
using GuildMaster.Data;
using CoreResourceType = GuildMaster.Core.ResourceType;


namespace GuildMaster.Systems
{
    public class ResearchSystem : MonoBehaviour
    {
        private static ResearchSystem _instance;
        public static ResearchSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ResearchSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ResearchSystem");
                        _instance = go.AddComponent<ResearchSystem>();
                    }
                }
                return _instance;
            }
        }
        
        // 연구 카테고리
        public enum ResearchCategory
        {
            Combat,         // 전투 관련
            Economy,        // 경제 관련
            Building,       // 건설 관련
            Adventurer,     // 모험가 관련
            Exploration,    // 탐험 관련
            Special         // 특수 연구
        }
        
        // 연구 타입
        public enum ResearchType
        {
            // Combat
            AttackPower,
            Defense,
            CriticalRate,
            SkillDamage,
            BattleSpeed,
            
            // Economy
            GoldProduction,
            ResourceProduction,
            TradingBonus,
            BuildingCost,
            
            // Building
            ConstructionSpeed,
            BuildingEfficiency,
            MaxBuildingLevel,
            
            // Adventurer
            ExpGain,
            RecruitmentCost,
            TrainingSpeed,
            MaxAdventurers,
            
            // Exploration
            DungeonRewards,
            ExplorationSpeed,
            DropRate,
            
            // Special
            NewJobClass,
            NewBuilding,
            SpecialSkill
        }
        
        // 연구 항목
        [System.Serializable]
        public class Research
        {
            public int researchId;
            public string researchName;
            public string description;
            public ResearchCategory category;
            public ResearchType type;
            
            public int currentLevel;
            public int maxLevel;
            
            public ResearchRequirement requirement;
            public ResearchCost[] levelCosts; // 레벨별 비용
            public ResearchEffect[] levelEffects; // 레벨별 효과
            
            public bool isUnlocked;
            public bool isResearching;
            public float researchProgress;
            public float researchTime; // 연구 소요 시간 (초)
            
            public List<int> prerequisiteResearchIds; // 선행 연구
        }
        
        // 연구 요구사항
        [System.Serializable]
        public class ResearchRequirement
        {
            public int requiredGuildLevel;
            public int requiredLabLevel; // 연구소 레벨
            public Dictionary<ResearchType, int> requiredResearchLevels; // 선행 연구 레벨
            
            public ResearchRequirement()
            {
                requiredResearchLevels = new Dictionary<ResearchType, int>();
            }
        }
        
        // 연구 비용
        [System.Serializable]
        public class ResearchCost
        {
            public int goldCost;
            public int manaStoneCost;
            public Dictionary<CoreResourceType, int> resourceCosts;
            public float researchTime; // 시간 (분)
            
            public ResearchCost()
            {
                resourceCosts = new Dictionary<CoreResourceType, int>();
            }
        }
        
        // 연구 효과
        [System.Serializable]
        public class ResearchEffect
        {
            public float effectValue; // 효과 수치
            public string effectDescription; // 효과 설명
            public bool isPercentage; // 퍼센트 값인지
            
            public string GetFormattedDescription()
            {
                if (isPercentage)
                    return string.Format(effectDescription, $"{effectValue}%");
                else
                    return string.Format(effectDescription, effectValue);
            }
        }
        
        // 연구 트리 노드
        [System.Serializable]
        public class ResearchNode
        {
            public Research research;
            public Vector2 treePosition; // 트리에서의 위치
            public List<ResearchNode> children;
            public List<ResearchNode> parents;
            
            public ResearchNode()
            {
                children = new List<ResearchNode>();
                parents = new List<ResearchNode>();
            }
        }
        
        // 연구 데이터
        private Dictionary<int, Research> allResearch;
        private Dictionary<ResearchCategory, List<ResearchNode>> researchTrees;
        private List<Research> activeResearch; // 현재 진행 중인 연구들
        private int maxConcurrentResearch = 1; // 동시 연구 가능 수
        
        // 연구 효과 적용
        private Dictionary<ResearchType, float> researchBonuses;
        
        // 이벤트
        public event Action<Research> OnResearchUnlocked;
        public event Action<Research> OnResearchStarted;
        public event Action<Research> OnResearchCompleted;
        public event Action<Research> OnResearchLevelUp;
        public event Action<ResearchType, float> OnResearchBonusChanged;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            Initialize();
        }
        
        void Initialize()
        {
            allResearch = new Dictionary<int, Research>();
            researchTrees = new Dictionary<ResearchCategory, List<ResearchNode>>();
            activeResearch = new List<Research>();
            researchBonuses = new Dictionary<ResearchType, float>();
            
            // 연구 데이터 생성
            CreateResearchData();
            
            // 연구 트리 구성
            BuildResearchTrees();
            
            // 초기 연구 해금
            UnlockInitialResearch();
            
            // 연구 진행 업데이트 시작
            StartCoroutine(ResearchProgressUpdater());
        }
        
        void CreateResearchData()
        {
            // 전투 연구
            CreateCombatResearch();
            
            // 경제 연구
            CreateEconomyResearch();
            
            // 건설 연구
            CreateBuildingResearch();
            
            // 모험가 연구
            CreateAdventurerResearch();
            
            // 탐험 연구
            CreateExplorationResearch();
            
            // 특수 연구
            CreateSpecialResearch();
        }
        
        void CreateCombatResearch()
        {
            // 공격력 증가
            Research attackResearch = new Research
            {
                researchId = 101,
                researchName = "전투 훈련",
                description = "모든 모험가의 공격력을 증가시킵니다.",
                category = ResearchCategory.Combat,
                type = ResearchType.AttackPower,
                maxLevel = 10,
                requirement = new ResearchRequirement
                {
                    requiredGuildLevel = 1,
                    requiredLabLevel = 1
                }
            };
            
            attackResearch.levelCosts = new ResearchCost[10];
            attackResearch.levelEffects = new ResearchEffect[10];
            
            for (int i = 0; i < 10; i++)
            {
                attackResearch.levelCosts[i] = new ResearchCost
                {
                    goldCost = 500 * (i + 1),
                    manaStoneCost = 10 * (i + 1),
                    researchTime = 5 * (i + 1) // 분
                };
                
                attackResearch.levelEffects[i] = new ResearchEffect
                {
                    effectValue = 2 * (i + 1),
                    effectDescription = "공격력 +{0}",
                    isPercentage = true
                };
            }
            
            allResearch[attackResearch.researchId] = attackResearch;
            
            // 방어력 증가
            Research defenseResearch = new Research
            {
                researchId = 102,
                researchName = "방어 전술",
                description = "모든 모험가의 방어력을 증가시킵니다.",
                category = ResearchCategory.Combat,
                type = ResearchType.Defense,
                maxLevel = 10,
                requirement = new ResearchRequirement
                {
                    requiredGuildLevel = 2,
                    requiredLabLevel = 1
                }
            };
            
            defenseResearch.levelCosts = new ResearchCost[10];
            defenseResearch.levelEffects = new ResearchEffect[10];
            
            for (int i = 0; i < 10; i++)
            {
                defenseResearch.levelCosts[i] = new ResearchCost
                {
                    goldCost = 500 * (i + 1),
                    manaStoneCost = 10 * (i + 1),
                    researchTime = 5 * (i + 1)
                };
                
                defenseResearch.levelEffects[i] = new ResearchEffect
                {
                    effectValue = 2 * (i + 1),
                    effectDescription = "방어력 +{0}",
                    isPercentage = true
                };
            }
            
            allResearch[defenseResearch.researchId] = defenseResearch;
            
            // 크리티컬 확률
            Research criticalResearch = new Research
            {
                researchId = 103,
                researchName = "정밀 타격",
                description = "모든 모험가의 크리티컬 확률을 증가시킵니다.",
                category = ResearchCategory.Combat,
                type = ResearchType.CriticalRate,
                maxLevel = 5,
                prerequisiteResearchIds = new List<int> { 101 }, // 공격력 연구 필요
                requirement = new ResearchRequirement
                {
                    requiredGuildLevel = 5,
                    requiredLabLevel = 2
                }
            };
            
            criticalResearch.levelCosts = new ResearchCost[5];
            criticalResearch.levelEffects = new ResearchEffect[5];
            
            for (int i = 0; i < 5; i++)
            {
                criticalResearch.levelCosts[i] = new ResearchCost
                {
                    goldCost = 1000 * (i + 1),
                    manaStoneCost = 20 * (i + 1),
                    researchTime = 10 * (i + 1)
                };
                
                criticalResearch.levelEffects[i] = new ResearchEffect
                {
                    effectValue = 2 * (i + 1),
                    effectDescription = "크리티컬 확률 +{0}",
                    isPercentage = true
                };
            }
            
            allResearch[criticalResearch.researchId] = criticalResearch;
        }
        
        void CreateEconomyResearch()
        {
            // 골드 생산 증가
            Research goldResearch = new Research
            {
                researchId = 201,
                researchName = "상업 발전",
                description = "모든 골드 생산량을 증가시킵니다.",
                category = ResearchCategory.Economy,
                type = ResearchType.GoldProduction,
                maxLevel = 10,
                requirement = new ResearchRequirement
                {
                    requiredGuildLevel = 1,
                    requiredLabLevel = 1
                }
            };
            
            goldResearch.levelCosts = new ResearchCost[10];
            goldResearch.levelEffects = new ResearchEffect[10];
            
            for (int i = 0; i < 10; i++)
            {
                goldResearch.levelCosts[i] = new ResearchCost
                {
                    goldCost = 1000 * (i + 1),
                    researchTime = 10 * (i + 1)
                };
                
                goldResearch.levelEffects[i] = new ResearchEffect
                {
                    effectValue = 5 * (i + 1),
                    effectDescription = "골드 생산량 +{0}",
                    isPercentage = true
                };
            }
            
            allResearch[goldResearch.researchId] = goldResearch;
            
            // 자원 생산 증가
            Research resourceResearch = new Research
            {
                researchId = 202,
                researchName = "자원 관리",
                description = "모든 자원 생산량을 증가시킵니다.",
                category = ResearchCategory.Economy,
                type = ResearchType.ResourceProduction,
                maxLevel = 10,
                requirement = new ResearchRequirement
                {
                    requiredGuildLevel = 3,
                    requiredLabLevel = 1
                }
            };
            
            resourceResearch.levelCosts = new ResearchCost[10];
            resourceResearch.levelEffects = new ResearchEffect[10];
            
            for (int i = 0; i < 10; i++)
            {
                resourceResearch.levelCosts[i] = new ResearchCost
                {
                    goldCost = 800 * (i + 1),
                    manaStoneCost = 15 * (i + 1),
                    researchTime = 8 * (i + 1)
                };
                
                resourceResearch.levelEffects[i] = new ResearchEffect
                {
                    effectValue = 3 * (i + 1),
                    effectDescription = "자원 생산량 +{0}",
                    isPercentage = true
                };
            }
            
            allResearch[resourceResearch.researchId] = resourceResearch;
        }
        
        void CreateBuildingResearch()
        {
            // 건설 속도 증가
            Research constructionResearch = new Research
            {
                researchId = 301,
                researchName = "건축 기술",
                description = "건물 건설 속도를 증가시킵니다.",
                category = ResearchCategory.Building,
                type = ResearchType.ConstructionSpeed,
                maxLevel = 5,
                requirement = new ResearchRequirement
                {
                    requiredGuildLevel = 2,
                    requiredLabLevel = 1
                }
            };
            
            constructionResearch.levelCosts = new ResearchCost[5];
            constructionResearch.levelEffects = new ResearchEffect[5];
            
            for (int i = 0; i < 5; i++)
            {
                constructionResearch.levelCosts[i] = new ResearchCost
                {
                    goldCost = 600 * (i + 1),
                    resourceCosts = new Dictionary<CoreResourceType, int>
                    {
                        { CoreResourceType.Wood, 100 * (i + 1) },
                        { CoreResourceType.Stone, 100 * (i + 1) }
                    },
                    researchTime = 15 * (i + 1)
                };
                
                constructionResearch.levelEffects[i] = new ResearchEffect
                {
                    effectValue = 10 * (i + 1),
                    effectDescription = "건설 속도 +{0}",
                    isPercentage = true
                };
            }
            
            allResearch[constructionResearch.researchId] = constructionResearch;
            
            // 건물 효율 증가
            Research efficiencyResearch = new Research
            {
                researchId = 302,
                researchName = "건물 최적화",
                description = "모든 건물의 효율을 증가시킵니다.",
                category = ResearchCategory.Building,
                type = ResearchType.BuildingEfficiency,
                maxLevel = 10,
                prerequisiteResearchIds = new List<int> { 301 },
                requirement = new ResearchRequirement
                {
                    requiredGuildLevel = 5,
                    requiredLabLevel = 2
                }
            };
            
            efficiencyResearch.levelCosts = new ResearchCost[10];
            efficiencyResearch.levelEffects = new ResearchEffect[10];
            
            for (int i = 0; i < 10; i++)
            {
                efficiencyResearch.levelCosts[i] = new ResearchCost
                {
                    goldCost = 1200 * (i + 1),
                    manaStoneCost = 25 * (i + 1),
                    researchTime = 20 * (i + 1)
                };
                
                efficiencyResearch.levelEffects[i] = new ResearchEffect
                {
                    effectValue = 2 * (i + 1),
                    effectDescription = "건물 효율 +{0}",
                    isPercentage = true
                };
            }
            
            allResearch[efficiencyResearch.researchId] = efficiencyResearch;
        }
        
        void CreateAdventurerResearch()
        {
            // 경험치 획득량 증가
            Research expResearch = new Research
            {
                researchId = 401,
                researchName = "효율적인 훈련",
                description = "모든 모험가의 경험치 획득량을 증가시킵니다.",
                category = ResearchCategory.Adventurer,
                type = ResearchType.ExpGain,
                maxLevel = 10,
                requirement = new ResearchRequirement
                {
                    requiredGuildLevel = 1,
                    requiredLabLevel = 1
                }
            };
            
            expResearch.levelCosts = new ResearchCost[10];
            expResearch.levelEffects = new ResearchEffect[10];
            
            for (int i = 0; i < 10; i++)
            {
                expResearch.levelCosts[i] = new ResearchCost
                {
                    goldCost = 700 * (i + 1),
                    manaStoneCost = 12 * (i + 1),
                    researchTime = 12 * (i + 1)
                };
                
                expResearch.levelEffects[i] = new ResearchEffect
                {
                    effectValue = 5 * (i + 1),
                    effectDescription = "경험치 획득량 +{0}",
                    isPercentage = true
                };
            }
            
            allResearch[expResearch.researchId] = expResearch;
            
            // 최대 모험가 수 증가
            Research maxAdventurersResearch = new Research
            {
                researchId = 402,
                researchName = "길드 확장",
                description = "최대 모험가 수를 증가시킵니다.",
                category = ResearchCategory.Adventurer,
                type = ResearchType.MaxAdventurers,
                maxLevel = 5,
                requirement = new ResearchRequirement
                {
                    requiredGuildLevel = 10,
                    requiredLabLevel = 3
                }
            };
            
            maxAdventurersResearch.levelCosts = new ResearchCost[5];
            maxAdventurersResearch.levelEffects = new ResearchEffect[5];
            
            for (int i = 0; i < 5; i++)
            {
                maxAdventurersResearch.levelCosts[i] = new ResearchCost
                {
                    goldCost = 5000 * (i + 1),
                    manaStoneCost = 50 * (i + 1),
                    researchTime = 30 * (i + 1)
                };
                
                maxAdventurersResearch.levelEffects[i] = new ResearchEffect
                {
                    effectValue = 4 * (i + 1),
                    effectDescription = "최대 모험가 수 +{0}",
                    isPercentage = false
                };
            }
            
            allResearch[maxAdventurersResearch.researchId] = maxAdventurersResearch;
        }
        
        void CreateExplorationResearch()
        {
            // 던전 보상 증가
            Research dungeonRewardResearch = new Research
            {
                researchId = 501,
                researchName = "보물 사냥꾼",
                description = "던전에서 획득하는 보상을 증가시킵니다.",
                category = ResearchCategory.Exploration,
                type = ResearchType.DungeonRewards,
                maxLevel = 10,
                requirement = new ResearchRequirement
                {
                    requiredGuildLevel = 5,
                    requiredLabLevel = 2
                }
            };
            
            dungeonRewardResearch.levelCosts = new ResearchCost[10];
            dungeonRewardResearch.levelEffects = new ResearchEffect[10];
            
            for (int i = 0; i < 10; i++)
            {
                dungeonRewardResearch.levelCosts[i] = new ResearchCost
                {
                    goldCost = 1000 * (i + 1),
                    manaStoneCost = 20 * (i + 1),
                    researchTime = 15 * (i + 1)
                };
                
                dungeonRewardResearch.levelEffects[i] = new ResearchEffect
                {
                    effectValue = 5 * (i + 1),
                    effectDescription = "던전 보상 +{0}",
                    isPercentage = true
                };
            }
            
            allResearch[dungeonRewardResearch.researchId] = dungeonRewardResearch;
        }
        
        void CreateSpecialResearch()
        {
            // 새로운 직업 해금
            Research newJobResearch = new Research
            {
                researchId = 901,
                researchName = "고급 직업 연구",
                description = "새로운 직업 클래스를 해금합니다.",
                category = ResearchCategory.Special,
                type = ResearchType.NewJobClass,
                maxLevel = 1,
                requirement = new ResearchRequirement
                {
                    requiredGuildLevel = 20,
                    requiredLabLevel = 5,
                    requiredResearchLevels = new Dictionary<ResearchType, int>
                    {
                        { ResearchType.AttackPower, 5 },
                        { ResearchType.Defense, 5 }
                    }
                }
            };
            
            newJobResearch.levelCosts = new ResearchCost[1];
            newJobResearch.levelEffects = new ResearchEffect[1];
            
            newJobResearch.levelCosts[0] = new ResearchCost
            {
                goldCost = 50000,
                manaStoneCost = 500,
                researchTime = 120
            };
            
            newJobResearch.levelEffects[0] = new ResearchEffect
            {
                effectValue = 1,
                effectDescription = "새로운 직업 '성기사' 해금",
                isPercentage = false
            };
            
            allResearch[newJobResearch.researchId] = newJobResearch;
        }
        
        void BuildResearchTrees()
        {
            // 카테고리별로 연구 트리 구성
            foreach (ResearchCategory category in Enum.GetValues(typeof(ResearchCategory)))
            {
                researchTrees[category] = new List<ResearchNode>();
                
                var categoryResearch = allResearch.Values.Where(r => r.category == category).ToList();
                
                // 루트 노드 찾기 (선행 연구가 없는 것들)
                var rootResearch = categoryResearch.Where(r => r.prerequisiteResearchIds == null || 
                                                               r.prerequisiteResearchIds.Count == 0).ToList();
                
                foreach (var research in rootResearch)
                {
                    ResearchNode node = new ResearchNode
                    {
                        research = research,
                        treePosition = new Vector2(0, researchTrees[category].Count * 100)
                    };
                    
                    researchTrees[category].Add(node);
                    BuildTreeRecursive(node, category, 1);
                }
            }
        }
        
        void BuildTreeRecursive(ResearchNode parentNode, ResearchCategory category, int depth)
        {
            // 이 연구를 선행 연구로 가진 연구들 찾기
            var children = allResearch.Values.Where(r => 
                r.category == category && 
                r.prerequisiteResearchIds != null &&
                r.prerequisiteResearchIds.Contains(parentNode.research.researchId)).ToList();
            
            int childIndex = 0;
            foreach (var childResearch in children)
            {
                ResearchNode childNode = new ResearchNode
                {
                    research = childResearch,
                    treePosition = new Vector2(depth * 200, childIndex * 100)
                };
                
                childNode.parents.Add(parentNode);
                parentNode.children.Add(childNode);
                
                // 이미 트리에 없다면 추가
                if (!researchTrees[category].Any(n => n.research.researchId == childResearch.researchId))
                {
                    researchTrees[category].Add(childNode);
                }
                
                BuildTreeRecursive(childNode, category, depth + 1);
                childIndex++;
            }
        }
        
        void UnlockInitialResearch()
        {
            // 각 카테고리의 기본 연구 해금
            foreach (var tree in researchTrees.Values)
            {
                foreach (var node in tree)
                {
                    if (node.parents.Count == 0) // 루트 노드
                    {
                        node.research.isUnlocked = true;
                        OnResearchUnlocked?.Invoke(node.research);
                    }
                }
            }
        }
        
        // 연구 시작
        public bool StartResearch(int researchId)
        {
            if (!allResearch.ContainsKey(researchId))
                return false;
            
            Research research = allResearch[researchId];
            
            // 이미 연구 중인지 확인
            if (research.isResearching || activeResearch.Count >= maxConcurrentResearch)
                return false;
            
            // 요구사항 확인
            if (!CheckRequirements(research))
                return false;
            
            // 비용 확인 및 차감
            int nextLevel = research.currentLevel;
            if (nextLevel >= research.maxLevel)
                return false;
            
            ResearchCost cost = research.levelCosts[nextLevel];
            if (!PayResearchCost(cost))
                return false;
            
            // 연구 시작
            research.isResearching = true;
            research.researchProgress = 0f;
            research.researchTime = cost.researchTime * 60f; // 분을 초로 변환
            activeResearch.Add(research);
            
            OnResearchStarted?.Invoke(research);
            
            return true;
        }
        
        bool CheckRequirements(Research research)
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return false;
            
            // 길드 레벨 확인
            int guildLevel = gameManager.GuildManager != null ? 
                gameManager.GuildManager.GetGuildData().GuildLevel : 0;
            if (guildLevel < research.requirement.requiredGuildLevel)
                return false;
            
            // 연구소 레벨 확인
            int labLevel = GetLaboratoryLevel();
            if (labLevel < research.requirement.requiredLabLevel)
                return false;
            
            // 선행 연구 확인
            if (research.prerequisiteResearchIds != null)
            {
                foreach (int prereqId in research.prerequisiteResearchIds)
                {
                    if (!allResearch.ContainsKey(prereqId) || allResearch[prereqId].currentLevel == 0)
                        return false;
                }
            }
            
            // 필요 연구 레벨 확인
            foreach (var reqResearch in research.requirement.requiredResearchLevels)
            {
                float currentBonus = GetResearchBonus(reqResearch.Key);
                if (currentBonus < reqResearch.Value)
                    return false;
            }
            
            return true;
        }
        
        int GetLaboratoryLevel()
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager?.GuildManager == null) return 0;
            
            var researchLabs = gameManager.GuildManager.GetBuildingsByType(BuildingType.ResearchLab);
            return researchLabs.Count > 0 ? researchLabs[0].level : 0;
        }
        
        bool PayResearchCost(ResearchCost cost)
        {
            var resourceManager = Core.GameManager.Instance?.ResourceManager;
            if (resourceManager == null) return false;
            
            // 비용 확인
            if (resourceManager.GetGold() < cost.goldCost)
                return false;
            
            if (resourceManager.GetManaStone() < cost.manaStoneCost)
                return false;
            
            foreach (var resource in cost.resourceCosts)
            {
                int currentAmount = 0;
                switch (resource.Key)
                {
                    case CoreResourceType.Wood:
                        currentAmount = resourceManager.GetWood();
                        break;
                    case CoreResourceType.Stone:
                        currentAmount = resourceManager.GetStone();
                        break;
                }
                
                if (currentAmount < resource.Value)
                    return false;
            }
            
            // 비용 차감
            resourceManager.AddGold(-cost.goldCost);
            resourceManager.AddManaStone(-cost.manaStoneCost);
            
            foreach (var resource in cost.resourceCosts)
            {
                switch (resource.Key)
                {
                    case CoreResourceType.Wood:
                        resourceManager.AddWood(-resource.Value);
                        break;
                    case CoreResourceType.Stone:
                        resourceManager.AddStone(-resource.Value);
                        break;
                }
            }
            
            return true;
        }
        
        IEnumerator ResearchProgressUpdater()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                
                // 연구소 레벨에 따른 연구 속도 보너스
                float speedBonus = 1f + (GetLaboratoryLevel() * 0.1f);
                
                for (int i = activeResearch.Count - 1; i >= 0; i--)
                {
                    Research research = activeResearch[i];
                    
                    research.researchProgress += speedBonus;
                    
                    if (research.researchProgress >= research.researchTime)
                    {
                        CompleteResearch(research);
                        activeResearch.RemoveAt(i);
                    }
                }
            }
        }
        
        void CompleteResearch(Research research)
        {
            research.isResearching = false;
            research.currentLevel++;
            
            // 효과 적용
            ApplyResearchEffect(research);
            
            OnResearchCompleted?.Invoke(research);
            OnResearchLevelUp?.Invoke(research);
            
            // 다음 연구 해금
            UnlockNextResearch(research);
        }
        
        void ApplyResearchEffect(Research research)
        {
            int level = research.currentLevel - 1;
            if (level < 0 || level >= research.levelEffects.Length)
                return;
            
            ResearchEffect effect = research.levelEffects[level];
            
            // 연구 보너스 업데이트
            if (!researchBonuses.ContainsKey(research.type))
                researchBonuses[research.type] = 0f;
            
            researchBonuses[research.type] += effect.effectValue;
            
            OnResearchBonusChanged?.Invoke(research.type, researchBonuses[research.type]);
            
            // 특수 효과 처리
            switch (research.type)
            {
                case ResearchType.MaxAdventurers:
                    var guildManager = Core.GameManager.Instance?.GuildManager;
                    if (guildManager != null)
                    {
                        guildManager.IncreaseMaxAdventurers((int)effect.effectValue);
                    }
                    break;
                    
                case ResearchType.NewJobClass:
                    // TODO: 새 직업 해금
                    Debug.Log($"New job class unlocked: {effect.effectDescription}");
                    break;
                    
                case ResearchType.NewBuilding:
                    // TODO: 새 건물 해금
                    Debug.Log($"New building unlocked: {effect.effectDescription}");
                    break;
            }
        }
        
        void UnlockNextResearch(Research completedResearch)
        {
            // 이 연구를 선행 조건으로 하는 연구들 확인
            foreach (var research in allResearch.Values)
            {
                if (!research.isUnlocked && 
                    research.prerequisiteResearchIds != null &&
                    research.prerequisiteResearchIds.Contains(completedResearch.researchId))
                {
                    // 모든 선행 조건 확인
                    bool allPrereqMet = true;
                    foreach (int prereqId in research.prerequisiteResearchIds)
                    {
                        if (!allResearch.ContainsKey(prereqId) || allResearch[prereqId].currentLevel == 0)
                        {
                            allPrereqMet = false;
                            break;
                        }
                    }
                    
                    if (allPrereqMet && CheckRequirements(research))
                    {
                        research.isUnlocked = true;
                        OnResearchUnlocked?.Invoke(research);
                    }
                }
            }
        }
        
        // 연구 보너스 조회
        public float GetResearchBonus(ResearchType type)
        {
            return researchBonuses.ContainsKey(type) ? researchBonuses[type] : 0f;
        }
        
        // 연구 즉시 완료 (디버그/과금 아이템용)
        public void InstantCompleteResearch(int researchId)
        {
            if (!allResearch.ContainsKey(researchId))
                return;
            
            Research research = allResearch[researchId];
            if (!research.isResearching)
                return;
            
            research.researchProgress = research.researchTime;
        }
        
        // 동시 연구 슬롯 증가
        public void IncreaseResearchSlots(int amount = 1)
        {
            maxConcurrentResearch += amount;
            Debug.Log($"Research slots increased to {maxConcurrentResearch}");
        }
        
        // 조회 메서드들
        public List<Research> GetResearchByCategory(ResearchCategory category)
        {
            return allResearch.Values.Where(r => r.category == category).ToList();
        }
        
        public List<Research> GetUnlockedResearch()
        {
            return allResearch.Values.Where(r => r.isUnlocked).ToList();
        }
        
        public List<Research> GetActiveResearch()
        {
            return new List<Research>(activeResearch);
        }
        
        public Research GetResearch(int researchId)
        {
            return allResearch.ContainsKey(researchId) ? allResearch[researchId] : null;
        }
        
        public List<ResearchNode> GetResearchTree(ResearchCategory category)
        {
            return researchTrees.ContainsKey(category) ? researchTrees[category] : new List<ResearchNode>();
        }
        
        public int GetMaxConcurrentResearch()
        {
            return maxConcurrentResearch;
        }
        
        public float GetResearchProgress(int researchId)
        {
            if (!allResearch.ContainsKey(researchId))
                return 0f;
            
            Research research = allResearch[researchId];
            return research.isResearching ? research.researchProgress / research.researchTime : 0f;
        }
        
        // 총 연구 진행도
        public float GetOverallResearchProgress()
        {
            int totalLevels = 0;
            int maxPossibleLevels = 0;
            
            foreach (var research in allResearch.Values)
            {
                totalLevels += research.currentLevel;
                maxPossibleLevels += research.maxLevel;
            }
            
            return maxPossibleLevels > 0 ? (float)totalLevels / maxPossibleLevels : 0f;
        }
    }
}