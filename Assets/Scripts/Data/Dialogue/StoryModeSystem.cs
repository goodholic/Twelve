using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Battle;
using GuildMaster.Core;
using GuildMaster.Data;
using JobClass = GuildMaster.Battle.JobClass;
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
        
        // 전투 결과 관련 클래스 정의
        [System.Serializable]
        public class BattleResult
        {
            public BattleResultType resultType;
            public BattleStatistics statistics;
            
            public bool isVictory => resultType == BattleResultType.Victory;
            public int unitsLost => statistics?.PlayerUnitsLost ?? 0;
            public int totalTurns => statistics?.TotalTurns ?? 0;
        }
        
        public enum BattleResultType
        {
            Victory,
            Defeat,
            Draw,
            Retreat
        }
        
        [System.Serializable]
        public class BattleStatistics
        {
            public int TotalTurns;
            public int PlayerUnitsLost;
            public int EnemyUnitsLost;
            public int DamageDealt;
            public int DamageTaken;
            public int SkillsUsed;
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
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeStoryMode();
        }
        
        void InitializeStoryMode()
        {
            storyChapters = new Dictionary<int, List<StoryStage>>();
            
            // 스토리 데이터 로드
            LoadStoryData();
            
            // 첫 스테이지 해금
            if (storyChapters.ContainsKey(1) && storyChapters[1].Count > 0)
            {
                storyChapters[1][0].isUnlocked = true;
            }
        }
        
        void LoadStoryData()
        {
            // 챕터별 스테이지 생성 (예시)
            CreateChapter1Stages();
            CreateChapter2Stages();
            CreateChapter3Stages();
            CreateChapter4Stages();
            CreateChapter5Stages();
        }
        
        void CreateChapter1Stages()
        {
            // 챕터 1: 시작의 길드
            var chapter1 = new List<StoryStage>();
            
            // 스테이지 1-1
            var stage1_1 = new StoryStage
            {
                stageId = "stage_1_1",
                stageName = "길드의 첫 의뢰",
                chapterNumber = 1,
                stageNumber = 1,
                description = "신생 길드의 첫 번째 의뢰. 고블린 소탕작전!",
                recommendedLevel = 1,
                difficultyMultiplier = 1.0f,
                clearCondition = ClearCondition.DefeatAllEnemies,
                preBattleDialogue = "길드장님! 첫 의뢰가 들어왔습니다. 마을 근처의 고블린들을 처치해달라고 하네요.",
                postBattleDialogue = "훌륭합니다! 첫 의뢰를 성공적으로 완수했습니다!"
            };
            
            // 적 구성
            stage1_1.enemySquads = new List<EnemySquadData>
            {
                new EnemySquadData
                {
                    squadName = "고블린 정찰대",
                    formation = SquadFormation.Standard,
                    tactic = BattleTactic.Balanced,
                    units = new List<EnemyUnitData>
                    {
                        new EnemyUnitData
                        {
                            unitId = "goblin_warrior",
                            unitName = "고블린 전사",
                            jobClass = JobClass.Knight,
                            level = 1,
                            statMultiplier = 0.8f,
                            position = new Vector2Int(0, 1)
                        },
                        new EnemyUnitData
                        {
                            unitId = "goblin_archer",
                            unitName = "고블린 궁수",
                            jobClass = JobClass.Archer,
                            level = 1,
                            statMultiplier = 0.7f,
                            position = new Vector2Int(2, 0)
                        }
                    }
                }
            };
            
            // 보상 설정
            stage1_1.firstClearReward = new StageReward
            {
                gold = 100,
                exp = 50,
                gems = 5,
                items = new List<ItemReward>
                {
                    new ItemReward { itemId = "potion_small", quantity = 3, dropRate = 1.0f }
                }
            };
            
            stage1_1.repeatReward = new StageReward
            {
                gold = 30,
                exp = 15,
                gems = 0,
                items = new List<ItemReward>
                {
                    new ItemReward { itemId = "potion_small", quantity = 1, dropRate = 0.3f }
                }
            };
            
            chapter1.Add(stage1_1);
            
            // 나머지 스테이지들 생성
            for (int i = 2; i <= 10; i++)
            {
                chapter1.Add(CreateStandardStage(1, i, 
                    $"스테이지 1-{i}", 
                    "신생 길드의 성장", 
                    i, 
                    1.0f + (i * 0.05f)));
            }
            
            storyChapters[1] = chapter1;
        }
        
        StoryStage CreateStandardStage(int chapter, int stage, string name, string desc, int level, float difficulty)
        {
            var stageData = new StoryStage
            {
                stageId = $"stage_{chapter}_{stage}",
                stageName = name,
                chapterNumber = chapter,
                stageNumber = stage,
                description = desc,
                recommendedLevel = level,
                difficultyMultiplier = difficulty,
                clearCondition = ClearCondition.DefeatAllEnemies
            };
            
            // 기본 적 구성
            stageData.enemySquads = GenerateEnemySquads(chapter, stage, level, difficulty);
            
            // 보상 설정
            stageData.firstClearReward = new StageReward
            {
                gold = 100 * chapter * stage,
                exp = 50 * chapter * stage,
                gems = chapter * 5,
                items = GenerateRewardItems(chapter, stage)
            };
            
            stageData.repeatReward = new StageReward
            {
                gold = 30 * chapter * stage,
                exp = 15 * chapter * stage,
                gems = 0,
                items = GenerateRewardItems(chapter, stage)
            };
            
            return stageData;
        }
        
        List<EnemySquadData> GenerateEnemySquads(int chapter, int stage, int level, float difficulty)
        {
            var squads = new List<EnemySquadData>();
            
            // 챕터와 스테이지에 따라 적 수 증가
            int squadCount = Mathf.Min(1 + (chapter - 1) / 2, 3);
            int unitsPerSquad = Mathf.Min(2 + stage / 3, 6);
            
            for (int i = 0; i < squadCount; i++)
            {
                var squad = new EnemySquadData
                {
                    squadName = $"적 부대 {i + 1}",
                    formation = (SquadFormation)(i % 5),
                    tactic = (BattleTactic)(stage % 5),
                    units = new List<EnemyUnitData>()
                };
                
                for (int j = 0; j < unitsPerSquad; j++)
                {
                    squad.units.Add(new EnemyUnitData
                    {
                        unitId = $"enemy_ch{chapter}_unit{j}",
                        unitName = GetEnemyName(chapter, j),
                        jobClass = (JobClass)(j % 12),
                        level = level,
                        statMultiplier = difficulty,
                        position = new Vector2Int(j % 3, j / 3)
                    });
                }
                
                squads.Add(squad);
            }
            
            return squads;
        }
        
        string GetEnemyName(int chapter, int unitIndex)
        {
            string[] chapterEnemies = chapter switch
            {
                1 => new[] { "고블린", "늑대", "도적" },
                2 => new[] { "오크", "트롤", "다크엘프" },
                3 => new[] { "언데드", "스켈레톤", "좀비" },
                4 => new[] { "용족", "리자드맨", "와이번" },
                5 => new[] { "마족", "데몬", "서큐버스" },
                _ => new[] { "몬스터" }
            };
            
            string enemyType = chapterEnemies[unitIndex % chapterEnemies.Length];
            string[] roles = { "전사", "궁수", "마법사", "암살자", "성직자" };
            
            return $"{enemyType} {roles[unitIndex % roles.Length]}";
        }
        
        List<ItemReward> GenerateRewardItems(int chapter, int stage)
        {
            var items = new List<ItemReward>();
            
            // 기본 아이템
            items.Add(new ItemReward
            {
                itemId = "potion_small",
                quantity = stage,
                dropRate = 0.5f
            });
            
            // 챕터별 특수 아이템
            if (stage % 5 == 0) // 5스테이지마다
            {
                items.Add(new ItemReward
                {
                    itemId = $"equipment_tier{chapter}",
                    quantity = 1,
                    dropRate = 0.3f
                });
            }
            
            return items;
        }
        
        // 스테이지 시작
        public void StartStage(int chapter, int stage)
        {
            if (!storyChapters.ContainsKey(chapter))
            {
                Debug.LogError($"챕터 {chapter}를 찾을 수 없습니다.");
                return;
            }
            
            var stageData = storyChapters[chapter].Find(s => s.stageNumber == stage);
            if (stageData == null)
            {
                Debug.LogError($"스테이지 {chapter}-{stage}를 찾을 수 없습니다.");
                return;
            }
            
            if (!stageData.isUnlocked)
            {
                Debug.LogWarning($"스테이지 {chapter}-{stage}는 아직 잠겨있습니다.");
                return;
            }
            
            currentStage = stageData;
            
            // 스토리 대화 (전투 전)
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
            // Squad 시스템이 제거되었으므로 직접 전투 처리
            Debug.Log($"전투 시작: {stage.stageName}");
            
            // 임시로 전투 결과를 시뮬레이션
            StartCoroutine(SimulateBattle(stage));
        }
        
        IEnumerator SimulateBattle(StoryStage stage)
        {
            // 전투 시뮬레이션 (임시)
            yield return new WaitForSeconds(2f);
            
            // 임시 전투 결과 생성
            var result = new BattleResult
            {
                resultType = UnityEngine.Random.value > 0.3f ? BattleResultType.Victory : BattleResultType.Defeat,
                statistics = new BattleStatistics
                {
                    TotalTurns = UnityEngine.Random.Range(5, 20),
                    PlayerUnitsLost = UnityEngine.Random.Range(0, 3),
                    EnemyUnitsLost = UnityEngine.Random.Range(2, 6)
                }
            };
            
            OnBattleComplete(result);
        }
        
        void OnBattleComplete(BattleResult result)
        {
            if (currentStage == null) return;
            
            // 전투 결과 처리
            if (result.isVictory)
            {
                HandleStageVictory(currentStage, result);
            }
            else
            {
                HandleStageDefeat(currentStage, result);
            }
        }
        
        void HandleStageVictory(StoryStage stage, BattleResult result)
        {
            // 첫 클리어
            if (!stage.isCleared)
            {
                stage.isCleared = true;
                stage.clearCount = 1;
            }
            else
            {
                stage.clearCount++;
            }
            
            // 별점 계산
            int starRating = CalculateStarRating(stage, result);
            if (starRating > stage.bestStarRating)
            {
                stage.bestStarRating = starRating;
                OnStarRatingAchieved?.Invoke(stage, starRating);
            }
            
            // 보상 지급
            var reward = stage.clearCount == 1 ? stage.firstClearReward : stage.repeatReward;
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
        
        void HandleStageDefeat(StoryStage stage, BattleResult result)
        {
            Debug.Log($"스테이지 {stage.stageName} 실패!");
            OnStageCompleted?.Invoke(stage, result);
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
            
            // 아이템 보상
            foreach (var item in reward.items)
            {
                if (UnityEngine.Random.value <= item.dropRate)
                {
                    // TODO: 인벤토리 시스템과 연동
                    Debug.Log($"아이템 획득: {item.itemId} x{item.quantity}");
                }
            }
            
            // 특별 보상
            if (!string.IsNullOrEmpty(reward.specialReward))
            {
                ProcessSpecialReward(reward.specialReward);
            }
        }
        
        void ProcessSpecialReward(string rewardId)
        {
            // 특별 보상 처리 (캐릭터 해금 등)
            Debug.Log($"특별 보상: {rewardId}");
        }
        
        // 자동 반복 시스템
        public void SetAutoRepeat(StoryStage stage, bool enable)
        {
            if (enable && stage.isCleared)
            {
                autoRepeatStage = stage;
                isAutoRepeating = true;
                StartCoroutine(AutoRepeatCoroutine());
            }
            else
            {
                isAutoRepeating = false;
                autoRepeatStage = null;
                StopAllCoroutines();
            }
        }
        
        IEnumerator AutoRepeatCoroutine()
        {
            while (isAutoRepeating && autoRepeatStage != null)
            {
                yield return new WaitForSeconds(autoRepeatInterval);
                
                if (isAutoRepeating && autoRepeatStage != null)
                {
                    StartStage(autoRepeatStage.chapterNumber, autoRepeatStage.stageNumber);
                    autoRepeatCount++;
                }
            }
        }
        
        // 저장/로드
        public void SaveProgress()
        {
            // TODO: 진행 상황 저장
            PlayerPrefs.SetInt("StoryMode_HighestStage", highestUnlockedStage);
            PlayerPrefs.SetInt("StoryMode_CurrentChapter", currentChapter);
            
            // 각 스테이지 진행 상황 저장
            foreach (var chapter in storyChapters)
            {
                foreach (var stage in chapter.Value)
                {
                    string key = $"Stage_{stage.stageId}";
                    PlayerPrefs.SetInt($"{key}_Cleared", stage.isCleared ? 1 : 0);
                    PlayerPrefs.SetInt($"{key}_Stars", stage.bestStarRating);
                    PlayerPrefs.SetInt($"{key}_ClearCount", stage.clearCount);
                }
            }
            
            PlayerPrefs.Save();
        }
        
        public void LoadProgress()
        {
            highestUnlockedStage = PlayerPrefs.GetInt("StoryMode_HighestStage", 1);
            currentChapter = PlayerPrefs.GetInt("StoryMode_CurrentChapter", 1);
            
            // 각 스테이지 진행 상황 로드
            foreach (var chapter in storyChapters)
            {
                foreach (var stage in chapter.Value)
                {
                    string key = $"Stage_{stage.stageId}";
                    stage.isCleared = PlayerPrefs.GetInt($"{key}_Cleared", 0) == 1;
                    stage.bestStarRating = PlayerPrefs.GetInt($"{key}_Stars", 0);
                    stage.clearCount = PlayerPrefs.GetInt($"{key}_ClearCount", 0);
                    
                    // 해금 상태 복원
                    int stageNumber = stage.chapterNumber * 100 + stage.stageNumber;
                    stage.isUnlocked = stageNumber <= highestUnlockedStage;
                }
            }
        }
        
        void CreateChapter2Stages()
        {
            // 챕터 2: 성장하는 길드
            var chapter2 = new List<StoryStage>();
            
            for (int i = 1; i <= 15; i++)
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