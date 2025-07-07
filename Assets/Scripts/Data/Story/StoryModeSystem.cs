using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Battle;
using GuildMaster.Core;
using GuildMaster.Data;
using JobClass = GuildMaster.Battle.JobClass;
using Unit = GuildMaster.Battle.Unit;
using Rarity = GuildMaster.Data.Rarity;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 스토리 모드 시스템 - 스테이지 기반 진행
    /// </summary>
    public class StoryModeSystem : MonoBehaviour
    {
        private static StoryModeSystem _instance;
        public static StoryModeSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<StoryModeSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("StoryModeSystem");
                        _instance = go.AddComponent<StoryModeSystem>();
                    }
                }
                return _instance;
            }
        }
        
        [System.Serializable]
        public class StoryStage
        {
            public string stageId;
            public string stageName;
            public int chapterNumber;
            public int stageNumber;
            public string description;
            
            // 전투 정보
            public List<EnemySquadData> enemySquads;
            public int recommendedLevel;
            public float difficultyMultiplier;
            
            // 스토리 정보
            public string preBattleDialogue;
            public string postBattleDialogue;
            public List<StoryChoice> storyChoices;
            
            // 클리어 조건
            public ClearCondition clearCondition;
            public int turnLimit;
            public int unitLossLimit;
            
            // 보상
            public StageReward firstClearReward;
            public StageReward repeatReward;
            public int starRating; // 0-3 stars
            
            // 진행 상태
            public bool isUnlocked;
            public bool isCleared;
            public int clearCount;
            public int bestStarRating;
        }
        
        [System.Serializable]
        public class EnemySquadData
        {
            public string squadName;
            public List<EnemyUnitData> units;
            public SquadFormation formation;
            public BattleTactic tactic;
        }
        
        [System.Serializable]
        public class EnemyUnitData
        {
            public string unitId;
            public string unitName;
            public JobClass jobClass;
            public int level;
            public float statMultiplier;
            public List<string> skillIds;
            public Vector2Int position; // 6x3 그리드 위치
        }
        
        [System.Serializable]
        public class StageReward
        {
            public int gold;
            public int exp;
            public int gems;
            public List<ItemReward> items;
            public string specialReward; // 캐릭터 해금 등
        }
        
        [System.Serializable]
        public class ItemReward
        {
            public string itemId;
            public int quantity;
            public float dropRate;
        }
        
        [System.Serializable]
        public class StoryChoice
        {
            public string choiceId;
            public string choiceText;
            public string consequence;
            public Dictionary<string, int> effects; // 평판, 자원 등 영향
        }
        
        public enum ClearCondition
        {
            DefeatAllEnemies,   // 모든 적 처치
            SurviveTurns,       // 특정 턴 생존
            ProtectTarget,      // 특정 유닛 보호
            DefeatBoss,         // 보스만 처치
            SpecialObjective    // 특수 목표
        }
        
        public enum SquadFormation
        {
            Standard,       // 표준 진형
            Defensive,      // 방어 진형
            Offensive,      // 공격 진형
            Flanking,       // 측면 공격
            Scattered       // 분산 진형
        }
        
        public enum BattleTactic
        {
            Balanced,       // 균형
            Aggressive,     // 공격적
            Defensive,      // 방어적
            FocusFire,      // 집중 공격
            Guerrilla       // 게릴라
        }
        
        // 스토리 챕터 및 스테이지
        private Dictionary<int, List<StoryStage>> storyChapters;
        private StoryStage currentStage;
        private int currentChapter = 1;
        private int highestUnlockedStage = 1;
        
        // 방치 전투 설정
        private StoryStage autoRepeatStage;
        private bool isAutoRepeating;
        private int autoRepeatCount;
        private float autoRepeatInterval = 300f; // 5분마다
        
        // 이벤트
        public event Action<StoryStage> OnStageStarted;
        public event Action<StoryStage, BattleResult> OnStageCompleted;
        public event Action<StoryStage, int> OnStarRatingAchieved;
        public event Action<int> OnChapterCompleted;
        public event Action<StoryChoice> OnStoryChoiceMade;
        public event Action<StageReward> OnRewardReceived;
        
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
            storyChapters = new Dictionary<int, List<StoryStage>>();
            
            // 스토리 스테이지 생성
            CreateStoryStages();
            
            // 첫 스테이지 해금
            UnlockStage(1, 1);
        }
        
        void CreateStoryStages()
        {
            // 챕터 1: 길드의 시작 (10 스테이지)
            CreateChapter1Stages();
            
            // 챕터 2: 성장하는 길드 (12 스테이지)
            CreateChapter2Stages();
            
            // 챕터 3: 첫 번째 시련 (15 스테이지)
            CreateChapter3Stages();
            
            // 챕터 4: 동맹과 배신 (15 스테이지)
            CreateChapter4Stages();
            
            // 챕터 5: 대륙의 위기 (20 스테이지)
            CreateChapter5Stages();
            
            // 챕터 6: 최종 결전 (20 스테이지)
            CreateChapter6Stages();
        }
        
        void CreateChapter1Stages()
        {
            var chapter1 = new List<StoryStage>();
            
            // 스테이지 1-1: 첫 번째 의뢰
            chapter1.Add(new StoryStage
            {
                stageId = "stage_1_1",
                stageName = "첫 번째 의뢰",
                chapterNumber = 1,
                stageNumber = 1,
                description = "길드를 물려받은 첫날, 간단한 고블린 토벌 의뢰가 들어왔다.",
                recommendedLevel = 1,
                difficultyMultiplier = 0.8f,
                preBattleDialogue = "아서: 우리 길드의 첫 의뢰다. 모두 힘내자!",
                postBattleDialogue = "란슬롯: 좋은 시작이었습니다. 이제 더 큰 의뢰도 받을 수 있겠군요.",
                clearCondition = ClearCondition.DefeatAllEnemies,
                enemySquads = CreateGoblinSquads(3, 1),
                firstClearReward = new StageReward
                {
                    gold = 500,
                    exp = 100,
                    gems = 10,
                    specialReward = "elite_002" // 란슬롯 해금
                }
            });
            
            // 스테이지 1-2: 상인 호위
            chapter1.Add(new StoryStage
            {
                stageId = "stage_1_2",
                stageName = "상인 호위",
                chapterNumber = 1,
                stageNumber = 2,
                description = "길드의 평판을 높이기 위해 상인 호위 임무를 맡았다.",
                recommendedLevel = 2,
                difficultyMultiplier = 0.9f,
                clearCondition = ClearCondition.ProtectTarget,
                turnLimit = 10,
                enemySquads = CreateBanditSquads(4, 2),
                storyChoices = new List<StoryChoice>
                {
                    new StoryChoice
                    {
                        choiceId = "negotiate",
                        choiceText = "도적들과 협상을 시도한다",
                        consequence = "평판 +10, 전투 회피 가능",
                        effects = new Dictionary<string, int> { { "reputation", 10 } }
                    },
                    new StoryChoice
                    {
                        choiceId = "attack",
                        choiceText = "즉시 공격한다",
                        consequence = "전투 시작, 승리시 골드 보너스",
                        effects = new Dictionary<string, int> { { "gold", 200 } }
                    }
                }
            });
            
            // 스테이지 1-3 ~ 1-10 추가...
            for (int i = 3; i <= 10; i++)
            {
                chapter1.Add(CreateStandardStage(1, i, 
                    $"스테이지 1-{i}", 
                    "길드의 명성을 쌓아가는 여정", 
                    i, 
                    0.9f + (i * 0.05f)));
            }
            
            storyChapters[1] = chapter1;
        }
        
        void CreateChapter2Stages()
        {
            var chapter2 = new List<StoryStage>();
            
            // 챕터 2 스테이지들 생성...
            for (int i = 1; i <= 12; i++)
            {
                chapter2.Add(CreateStandardStage(2, i, 
                    $"스테이지 2-{i}", 
                    "성장하는 길드의 도전", 
                    10 + i, 
                    1.2f + (i * 0.05f)));
            }
            
            storyChapters[2] = chapter2;
        }
        
        void CreateChapter3Stages()
        {
            // 챕터 3: 첫 번째 시련
            var chapter3 = new List<StoryStage>();
            
            for (int i = 1; i <= 15; i++)
            {
                chapter3.Add(CreateStandardStage(3, i, 
                    $"스테이지 3-{i}", 
                    "진정한 시련의 시작", 
                    25 + i, 
                    1.5f + (i * 0.05f)));
            }
            
            storyChapters[3] = chapter3;
        }
        
        void CreateChapter4Stages()
        {
            // 챕터 4: 동맹과 배신
            var chapter4 = new List<StoryStage>();
            
            for (int i = 1; i <= 15; i++)
            {
                chapter4.Add(CreateStandardStage(4, i, 
                    $"스테이지 4-{i}", 
                    "복잡한 정치와 음모", 
                    40 + i, 
                    2.0f + (i * 0.05f)));
            }
            
            storyChapters[4] = chapter4;
        }
        
        void CreateChapter5Stages()
        {
            // 챕터 5: 대륙의 위기
            var chapter5 = new List<StoryStage>();
            
            for (int i = 1; i <= 20; i++)
            {
                chapter5.Add(CreateStandardStage(5, i, 
                    $"스테이지 5-{i}", 
                    "대륙을 위협하는 거대한 위기", 
                    55 + i, 
                    2.5f + (i * 0.05f)));
            }
            
            storyChapters[5] = chapter5;
        }
        
        void CreateChapter6Stages()
        {
            // 챕터 6: 최종 결전
            var chapter6 = new List<StoryStage>();
            
            for (int i = 1; i <= 20; i++)
            {
                chapter6.Add(CreateStandardStage(6, i, 
                    $"스테이지 6-{i}", 
                    "운명의 최종 결전", 
                    75 + i, 
                    3.0f + (i * 0.1f)));
            }
            
            // 최종 보스 스테이지
            chapter6[19].stageName = "운명의 대결";
            chapter6[19].description = "모든 것이 결정되는 순간";
            chapter6[19].clearCondition = ClearCondition.DefeatBoss;
            chapter6[19].firstClearReward.specialReward = "true_ending";
            
            storyChapters[6] = chapter6;
        }
        
        StoryStage CreateStandardStage(int chapter, int stage, string name, string desc, int level, float difficulty)
        {
            return new StoryStage
            {
                stageId = $"stage_{chapter}_{stage}",
                stageName = name,
                chapterNumber = chapter,
                stageNumber = stage,
                description = desc,
                recommendedLevel = level,
                difficultyMultiplier = difficulty,
                clearCondition = ClearCondition.DefeatAllEnemies,
                enemySquads = CreateStandardEnemySquads(level, difficulty),
                firstClearReward = new StageReward
                {
                    gold = 100 * level,
                    exp = 50 * level,
                    gems = stage == 10 || stage == 20 ? 50 : 10,
                    items = GenerateRandomRewards(level)
                }
            };
        }
        
        List<EnemySquadData> CreateGoblinSquads(int unitCount, int level)
        {
            var squads = new List<EnemySquadData>();
            var squad = new EnemySquadData
            {
                squadName = "고블린 부대",
                formation = SquadFormation.Scattered,
                tactic = BattleTactic.Guerrilla,
                units = new List<EnemyUnitData>()
            };
            
            for (int i = 0; i < unitCount; i++)
            {
                squad.units.Add(new EnemyUnitData
                {
                    unitId = $"goblin_{i}",
                    unitName = "고블린 전사",
                    jobClass = JobClass.Warrior,
                    level = level,
                    statMultiplier = 0.5f,
                    skillIds = new List<string> { "101" },
                    position = new Vector2Int(i % 6, i / 6)
                });
            }
            
            squads.Add(squad);
            return squads;
        }
        
        List<EnemySquadData> CreateBanditSquads(int unitCount, int level)
        {
            var squads = new List<EnemySquadData>();
            var squad = new EnemySquadData
            {
                squadName = "도적단",
                formation = SquadFormation.Offensive,
                tactic = BattleTactic.Aggressive,
                units = new List<EnemyUnitData>()
            };
            
            // 다양한 직업 구성
            var jobClasses = new JobClass[] { JobClass.Rogue, JobClass.Warrior, JobClass.Archer };
            
            for (int i = 0; i < unitCount; i++)
            {
                squad.units.Add(new EnemyUnitData
                {
                    unitId = $"bandit_{i}",
                    unitName = $"도적 {jobClasses[i % 3]}",
                    jobClass = jobClasses[i % 3],
                    level = level,
                    statMultiplier = 0.7f,
                    skillIds = new List<string> { "101", "102" },
                    position = new Vector2Int(i % 6, i / 6)
                });
            }
            
            squads.Add(squad);
            return squads;
        }
        
        List<EnemySquadData> CreateStandardEnemySquads(int level, float difficulty)
        {
            // 레벨과 난이도에 따른 적 부대 생성
            var squads = new List<EnemySquadData>();
            
            int squadCount = difficulty > 2.0f ? 2 : 1;
            int unitsPerSquad = Mathf.Min(9, 3 + (level / 10));
            
            for (int s = 0; s < squadCount; s++)
            {
                var squad = new EnemySquadData
                {
                    squadName = $"적 부대 {s + 1}",
                    formation = (SquadFormation)UnityEngine.Random.Range(0, 5),
                    tactic = (BattleTactic)UnityEngine.Random.Range(0, 5),
                    units = new List<EnemyUnitData>()
                };
                
                for (int i = 0; i < unitsPerSquad; i++)
                {
                    var jobClass = (JobClass)UnityEngine.Random.Range(0, 7);
                    squad.units.Add(new EnemyUnitData
                    {
                        unitId = $"enemy_{s}_{i}",
                        unitName = $"적 {jobClass}",
                        jobClass = jobClass,
                        level = level,
                        statMultiplier = difficulty,
                        skillIds = GenerateEnemySkills(level),
                        position = new Vector2Int(i % 6, i / 6)
                    });
                }
                
                squads.Add(squad);
            }
            
            return squads;
        }
        
        List<string> GenerateEnemySkills(int level)
        {
            var skills = new List<string> { "101" }; // 기본 공격
            
            if (level >= 10) skills.Add("102");
            if (level >= 20) skills.Add("103");
            if (level >= 30) skills.Add("201");
            if (level >= 40) skills.Add("301");
            
            return skills;
        }
        
        List<ItemReward> GenerateRandomRewards(int level)
        {
            var rewards = new List<ItemReward>();
            
            // 레벨에 따른 보상 생성
            if (UnityEngine.Random.value < 0.3f)
            {
                rewards.Add(new ItemReward
                {
                    itemId = "potion_hp_small",
                    quantity = UnityEngine.Random.Range(1, 3),
                    dropRate = 0.5f
                });
            }
            
            if (level >= 20 && UnityEngine.Random.value < 0.2f)
            {
                rewards.Add(new ItemReward
                {
                    itemId = "enhancement_stone",
                    quantity = 1,
                    dropRate = 0.2f
                });
            }
            
            return rewards;
        }
        
        /// <summary>
        /// 스테이지 시작
        /// </summary>
        public void StartStage(int chapter, int stage)
        {
            if (!storyChapters.ContainsKey(chapter))
                return;
                
            var stageData = storyChapters[chapter].Find(s => s.stageNumber == stage);
            if (stageData == null || !stageData.isUnlocked)
                return;
            
            currentStage = stageData;
            
            // 스토리 대화 표시
            if (!string.IsNullOrEmpty(stageData.preBattleDialogue))
            {
                ShowDialogue(stageData.preBattleDialogue, () => StartBattle(stageData));
            }
            else
            {
                StartBattle(stageData);
            }
            
            OnStageStarted?.Invoke(stageData);
        }
        
        void StartBattle(StoryStage stage)
        {
            // 적 부대 생성
            var enemySquads = CreateEnemySquadsFromData(stage.enemySquads);
            
            // 플레이어 부대 가져오기
            var playerSquads = CharacterManagementSystem.Instance.GetAllSquads();
            
            // 전투 시작
            SquadBattleSystem.Instance.StartSquadBattle(playerSquads, enemySquads);
            
            // 전투 종료 이벤트 구독
            SquadBattleSystem.Instance.OnBattleEnd += OnBattleComplete;
        }
        
        List<Squad> CreateEnemySquadsFromData(List<EnemySquadData> enemyData)
        {
            var squads = new List<Squad>();
            
            foreach (var data in enemyData)
            {
                var squad = new Squad(data.squadName, squads.Count, false);
                
                foreach (var unitData in data.units)
                {
                    var unit = CreateEnemyUnit(unitData);
                    squad.AddUnit(unit, unitData.position.y, unitData.position.x);
                }
                
                squads.Add(squad);
            }
            
            return squads;
        }
        
        Unit CreateEnemyUnit(EnemyUnitData data)
        {
            var unit = new Unit(data.unitName, data.level, data.jobClass);
            
            // 스탯 조정
            unit.maxHP = Mathf.RoundToInt(unit.maxHP * data.statMultiplier);
            unit.attackPower = Mathf.RoundToInt(unit.attackPower * data.statMultiplier);
            unit.defense = Mathf.RoundToInt(unit.defense * data.statMultiplier);
            unit.speed = Mathf.RoundToInt(unit.speed * data.statMultiplier);
            
            unit.currentHP = unit.maxHP;
            unit.currentMP = unit.maxMP;
            
            // 스킬 추가
            foreach (var skillId in data.skillIds)
            {
                if (int.TryParse(skillId, out int id))
                {
                    unit.skillIds.Add(id);
                }
            }
            
            return unit;
        }
        
        void OnBattleComplete(BattleResult result)
        {
            if (currentStage == null) return;
            
            // 전투 종료 이벤트 구독 해제
            SquadBattleSystem.Instance.OnBattleEnd -= OnBattleComplete;
            
            if (result.isVictory)
            {
                // 스테이지 클리어
                CompleteStage(currentStage, result);
            }
            else
            {
                // 패배 처리
                OnStageCompleted?.Invoke(currentStage, result);
            }
        }
        
        void CompleteStage(StoryStage stage, BattleResult result)
        {
            bool isFirstClear = !stage.isCleared;
            stage.isCleared = true;
            stage.clearCount++;
            
            // 별점 계산
            int stars = CalculateStarRating(stage, result);
            if (stars > stage.bestStarRating)
            {
                stage.bestStarRating = stars;
                OnStarRatingAchieved?.Invoke(stage, stars);
            }
            
            // 보상 지급
            StageReward reward = isFirstClear ? stage.firstClearReward : stage.repeatReward;
            if (reward != null)
            {
                GiveRewards(reward);
            }
            
            // 다음 스테이지 해금
            UnlockNextStage(stage);
            
            // 스토리 대화 (전투 후)
            if (!string.IsNullOrEmpty(stage.postBattleDialogue))
            {
                ShowDialogue(stage.postBattleDialogue, null);
            }
            
            OnStageCompleted?.Invoke(stage, result);
            
            // 챕터 완료 체크
            CheckChapterCompletion(stage.chapterNumber);
        }
        
        int CalculateStarRating(StoryStage stage, BattleResult result)
        {
            int stars = 1; // 기본 1성 (클리어)
            
            // 유닛 손실 없음: +1성
            if (result.unitsLost == 0)
                stars++;
            
            // 턴 제한 내 클리어: +1성
            if (stage.turnLimit > 0 && result.totalTurns <= stage.turnLimit)
                stars++;
            else if (result.totalTurns <= 10) // 일반적으로 10턴 이내
                stars++;
            
            return Mathf.Clamp(stars, 1, 3);
        }
        
        void GiveRewards(StageReward reward)
        {
            // ResourceManager 타입이 제거되어 주석 처리
            // var resourceManager = GameManager.Instance.ResourceManager;
            // 
            // // 기본 보상
            // resourceManager.AddGold(reward.gold);
            // // TODO: 경험치 시스템과 연동
            // if (reward.gems > 0)
            // {
            //     resourceManager.AddGems(reward.gems);
            // }
            
            Debug.Log($"보상 지급: 골드 {reward.gold}, 보석 {reward.gems}");
            
            // 아이템 보상
            foreach (var item in reward.items)
            {
                if (UnityEngine.Random.value <= item.dropRate)
                {
                    // TODO: 인벤토리 시스템과 연동
                    Debug.Log($"아이템 획득: {item.itemId} x{item.quantity}");
                }
            }
            
            // 특별 보상 (캐릭터 해금 등)
            if (!string.IsNullOrEmpty(reward.specialReward))
            {
                ProcessSpecialReward(reward.specialReward);
            }
            
            OnRewardReceived?.Invoke(reward);
        }
        
        void ProcessSpecialReward(string rewardId)
        {
            if (rewardId.StartsWith("elite_"))
            {
                // DataManager 타입이 제거되어 주석 처리
                // 엘리트 캐릭터 해금
                // var characterData = DataManager.Instance.GetCharacterData(rewardId);
                // if (characterData != null)
                // {
                //     var unit = DataManager.Instance.CreateUnitFromData(rewardId);
                //     CharacterManagementSystem.Instance.AddCharacter(unit);
                // }
                
                Debug.Log($"특별 보상: {rewardId} 캐릭터 해금");
            }
            else if (rewardId == "true_ending")
            {
                // 진엔딩 달성
                UnlockEnding(6);
            }
        }
        
        void UnlockNextStage(StoryStage completedStage)
        {
            int nextChapter = completedStage.chapterNumber;
            int nextStage = completedStage.stageNumber + 1;
            
            // 같은 챕터의 다음 스테이지
            if (storyChapters[nextChapter].Count > completedStage.stageNumber)
            {
                UnlockStage(nextChapter, nextStage);
            }
            // 다음 챕터의 첫 스테이지
            else if (storyChapters.ContainsKey(nextChapter + 1))
            {
                UnlockStage(nextChapter + 1, 1);
            }
        }
        
        void UnlockStage(int chapter, int stage)
        {
            if (!storyChapters.ContainsKey(chapter))
                return;
                
            var stageData = storyChapters[chapter].Find(s => s.stageNumber == stage);
            if (stageData != null)
            {
                stageData.isUnlocked = true;
                
                if (chapter * 100 + stage > highestUnlockedStage)
                {
                    highestUnlockedStage = chapter * 100 + stage;
                }
            }
        }
        
        void CheckChapterCompletion(int chapter)
        {
            if (!storyChapters.ContainsKey(chapter))
                return;
                
            bool allCleared = storyChapters[chapter].All(s => s.isCleared);
            if (allCleared)
            {
                OnChapterCompleted?.Invoke(chapter);
            }
        }
        
        void ShowDialogue(string dialogue, Action onComplete)
        {
            // TODO: UI 시스템과 연동하여 대화 표시
            Debug.Log($"대화: {dialogue}");
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// 자동 반복 전투 설정
        /// </summary>
        public void SetAutoRepeatStage(int chapter, int stage)
        {
            if (!storyChapters.ContainsKey(chapter))
                return;
                
            var stageData = storyChapters[chapter].Find(s => s.stageNumber == stage);
            if (stageData != null && stageData.isCleared)
            {
                autoRepeatStage = stageData;
                isAutoRepeating = true;
                StartCoroutine(AutoRepeatBattle());
            }
        }
        
        public void StopAutoRepeat()
        {
            isAutoRepeating = false;
            autoRepeatStage = null;
        }
        
        IEnumerator AutoRepeatBattle()
        {
            while (isAutoRepeating && autoRepeatStage != null)
            {
                yield return new WaitForSeconds(autoRepeatInterval);
                
                if (isAutoRepeating)
                {
                    // 자동 전투 시뮬레이션
                    SimulateAutoBattle(autoRepeatStage);
                    autoRepeatCount++;
                }
            }
        }
        
        void SimulateAutoBattle(StoryStage stage)
        {
            // 간단한 전투 시뮬레이션
            var playerPower = CalculatePlayerPower();
            var enemyPower = CalculateEnemyPower(stage);
            
            bool victory = playerPower > enemyPower;
            
            if (victory)
            {
                // 반복 보상 지급
                var reward = stage.repeatReward ?? new StageReward
                {
                    gold = 50 * stage.recommendedLevel,
                    exp = 25 * stage.recommendedLevel
                };
                
                GiveRewards(reward);
                stage.clearCount++;
            }
        }
        
        float CalculatePlayerPower()
        {
            float totalPower = 0;
            var characters = CharacterManagementSystem.Instance.GetAllCharacters();
            
            foreach (var character in characters)
            {
                totalPower += character.level * 10 + character.attackPower + character.defense;
            }
            
            return totalPower;
        }
        
        float CalculateEnemyPower(StoryStage stage)
        {
            return stage.recommendedLevel * 100 * stage.difficultyMultiplier;
        }
        
        void UnlockEnding(int endingIndex)
        {
            // 엔딩 해금 처리
            Debug.Log($"엔딩 {endingIndex} 해금!");
        }
        
        // 공개 API
        public List<StoryStage> GetChapterStages(int chapter)
        {
            return storyChapters.GetValueOrDefault(chapter, new List<StoryStage>());
        }
        
        public StoryStage GetStage(int chapter, int stage)
        {
            if (!storyChapters.ContainsKey(chapter))
                return null;
                
            return storyChapters[chapter].Find(s => s.stageNumber == stage);
        }
        
        public int GetHighestUnlockedStage() => highestUnlockedStage;
        
        public bool IsAutoRepeating() => isAutoRepeating;
        
        public StoryStage GetAutoRepeatStage() => autoRepeatStage;
        
        public int GetTotalStages()
        {
            int total = 0;
            foreach (var chapter in storyChapters.Values)
            {
                total += chapter.Count;
            }
            return total;
        }
        
        public int GetClearedStages()
        {
            int cleared = 0;
            foreach (var chapter in storyChapters.Values)
            {
                cleared += chapter.Count(s => s.isCleared);
            }
            return cleared;
        }
        
        public float GetProgressPercentage()
        {
            int total = GetTotalStages();
            if (total == 0) return 0;
            
            return (float)GetClearedStages() / total * 100f;
        }
    }
}