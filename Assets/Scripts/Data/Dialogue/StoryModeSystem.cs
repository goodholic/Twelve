using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Battle;
using GuildMaster.Core;
using GuildMaster.Data;
using JobClass = GuildMaster.Battle.JobClass;
using Unit = GuildMaster.Battle.UnitStatus;
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
            public string resultText;
            public ChoiceEffect effect;
        }
        
        [System.Serializable]
        public class ChoiceEffect
        {
            public string effectType;
            public Dictionary<string, object> parameters;
        }
        
        public enum ClearCondition
        {
            Victory,            // 단순 승리
            NoUnitLost,        // 유닛 손실 없이 승리
            TimeLimit,         // 제한 시간 내 승리
            SpecificUnitAlive, // 특정 유닛 생존
            DefeatBoss        // 보스 처치
        }
        
        public enum SquadFormation
        {
            Standard,
            Defensive,
            Offensive,
            Spread,
            Focused
        }
        
        public enum BattleTactic
        {
            Aggressive,
            Defensive,
            Balanced,
            Hit_and_Run,
            Focus_Fire
        }
        
        // Events
        public event Action<StoryStage> OnStageStarted;
        public event Action<StoryStage> OnStageCompleted;
        public event Action<StoryStage, int> OnStarRatingAchieved;
        public event Action<StageReward> OnRewardReceived;
        
        // 챕터 및 스테이지 데이터
        [SerializeField] private List<StoryChapter> chapters = new List<StoryChapter>();
        private StoryStage currentStage;
        private int currentChapterIndex;
        private int currentStageIndex;
        
        [System.Serializable]
        public class StoryChapter
        {
            public string chapterId;
            public string chapterName;
            public string description;
            public List<StoryStage> stages;
            public bool isUnlocked;
        }
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        public IEnumerator Initialize()
        {
            Debug.Log("스토리 모드 시스템 초기화 중...");
            
            LoadStoryData();
            UnlockFirstChapter();
            
            Debug.Log("스토리 모드 시스템 초기화 완료");
            yield break;
        }
        
        void LoadStoryData()
        {
            // TODO: 실제 구현에서는 데이터베이스나 ScriptableObject에서 로드
            GenerateTestStoryData();
        }
        
        void GenerateTestStoryData()
        {
            // 테스트용 챕터 및 스테이지 생성
            for (int i = 0; i < 5; i++)
            {
                var chapter = new StoryChapter
                {
                    chapterId = $"chapter_{i + 1}",
                    chapterName = $"Chapter {i + 1}: {GetChapterName(i)}",
                    description = $"챕터 {i + 1} 설명",
                    stages = new List<StoryStage>(),
                    isUnlocked = i == 0
                };
                
                // 각 챕터에 10개의 스테이지 생성
                for (int j = 0; j < 10; j++)
                {
                    var stage = CreateTestStage(i, j);
                    chapter.stages.Add(stage);
                }
                
                chapters.Add(chapter);
            }
        }
        
        StoryStage CreateTestStage(int chapterIndex, int stageIndex)
        {
            var stage = new StoryStage
            {
                stageId = $"stage_{chapterIndex + 1}_{stageIndex + 1}",
                stageName = $"Stage {stageIndex + 1}",
                chapterNumber = chapterIndex + 1,
                stageNumber = stageIndex + 1,
                description = "스테이지 설명",
                recommendedLevel = 10 + (chapterIndex * 10) + stageIndex,
                difficultyMultiplier = 1f + (chapterIndex * 0.5f),
                clearCondition = (ClearCondition)UnityEngine.Random.Range(0, 5),
                turnLimit = UnityEngine.Random.Range(10, 30),
                unitLossLimit = UnityEngine.Random.Range(0, 3),
                isUnlocked = chapterIndex == 0 && stageIndex == 0,
                enemySquads = GenerateEnemySquads(chapterIndex, stageIndex)
            };
            
            // 보상 설정
            stage.firstClearReward = new StageReward
            {
                gold = 1000 * (chapterIndex + 1),
                exp = 500 * (chapterIndex + 1),
                gems = 10 + (chapterIndex * 5),
                items = GenerateRandomRewards()
            };
            
            stage.repeatReward = new StageReward
            {
                gold = stage.firstClearReward.gold / 2,
                exp = stage.firstClearReward.exp / 2,
                gems = 0,
                items = new List<ItemReward>()
            };
            
            return stage;
        }
        
        List<EnemySquadData> GenerateEnemySquads(int chapterIndex, int stageIndex)
        {
            var squads = new List<EnemySquadData>();
            int squadCount = UnityEngine.Random.Range(1, 4);
            
            for (int i = 0; i < squadCount; i++)
            {
                var squad = new EnemySquadData
                {
                    squadName = $"Enemy Squad {i + 1}",
                    formation = (SquadFormation)UnityEngine.Random.Range(0, 5),
                    tactic = (BattleTactic)UnityEngine.Random.Range(0, 5),
                    units = new List<EnemyUnitData>()
                };
                
                // 유닛 생성
                int unitCount = UnityEngine.Random.Range(3, 7);
                for (int j = 0; j < unitCount; j++)
                {
                    var unit = new EnemyUnitData
                    {
                        unitId = $"enemy_{i}_{j}",
                        unitName = $"Enemy {GetRandomEnemyName()}",
                        jobClass = (JobClass)UnityEngine.Random.Range(0, 10),
                        level = 5 + (chapterIndex * 10) + stageIndex,
                        statMultiplier = 1f + (chapterIndex * 0.2f),
                        skillIds = new List<string> { "1", "2", "3" },
                        position = new Vector2Int(UnityEngine.Random.Range(0, 6), UnityEngine.Random.Range(0, 3))
                    };
                    squad.units.Add(unit);
                }
                
                squads.Add(squad);
            }
            
            return squads;
        }
        
        List<ItemReward> GenerateRandomRewards()
        {
            var rewards = new List<ItemReward>();
            int rewardCount = UnityEngine.Random.Range(1, 4);
            
            for (int i = 0; i < rewardCount; i++)
            {
                rewards.Add(new ItemReward
                {
                    itemId = $"item_{UnityEngine.Random.Range(1, 100)}",
                    quantity = UnityEngine.Random.Range(1, 5),
                    dropRate = UnityEngine.Random.Range(0.1f, 1f)
                });
            }
            
            return rewards;
        }
        
        string GetChapterName(int index)
        {
            string[] names = { "The Beginning", "Rising Threat", "Dark Forest", "Mountain Pass", "Final Confrontation" };
            return index < names.Length ? names[index] : $"Chapter {index + 1}";
        }
        
        string GetRandomEnemyName()
        {
            string[] names = { "Goblin", "Orc", "Skeleton", "Bandit", "Wolf", "Spider", "Zombie", "Demon" };
            return names[UnityEngine.Random.Range(0, names.Length)];
        }
        
        void UnlockFirstChapter()
        {
            if (chapters.Count > 0)
            {
                chapters[0].isUnlocked = true;
                if (chapters[0].stages.Count > 0)
                {
                    chapters[0].stages[0].isUnlocked = true;
                }
            }
        }
        
        // Public Methods
        public List<StoryChapter> GetChapters()
        {
            return new List<StoryChapter>(chapters);
        }
        
        public StoryChapter GetChapter(int index)
        {
            if (index >= 0 && index < chapters.Count)
                return chapters[index];
            return null;
        }
        
        public StoryStage GetStage(int chapterIndex, int stageIndex)
        {
            var chapter = GetChapter(chapterIndex);
            if (chapter != null && stageIndex >= 0 && stageIndex < chapter.stages.Count)
                return chapter.stages[stageIndex];
            return null;
        }
        
        public void StartStage(int chapterIndex, int stageIndex)
        {
            var stage = GetStage(chapterIndex, stageIndex);
            if (stage == null || !stage.isUnlocked)
            {
                Debug.LogWarning("스테이지를 시작할 수 없습니다.");
                return;
            }
            
            currentStage = stage;
            currentChapterIndex = chapterIndex;
            currentStageIndex = stageIndex;
            
            StartBattle(stage);
            OnStageStarted?.Invoke(stage);
        }
        
        void StartBattle(StoryStage stage)
        {
            // 전투 시작 로직 (Squad 관련 코드 제거)
            Debug.Log($"스테이지 {stage.stageName} 전투 시작");
        }
        
        void CompleteStage(StoryStage stage)
        {
            bool isFirstClear = !stage.isCleared;
            stage.isCleared = true;
            stage.clearCount++;
            
            // 별점 계산
            int stars = CalculateStarRating(stage);
            if (stars > stage.bestStarRating)
            {
                stage.bestStarRating = stars;
                OnStarRatingAchieved?.Invoke(stage, stars);
            }
            
            // 보상 지급
            StageReward reward = isFirstClear ? stage.firstClearReward : stage.repeatReward;
            GrantReward(reward);
            
            // 다음 스테이지 해금
            UnlockNextStage();
            
            OnStageCompleted?.Invoke(stage);
        }
        
        int CalculateStarRating(StoryStage stage)
        {
            // 별점 계산 로직 (BattleResult 관련 코드 제거)
            int stars = 1; // 기본 1성
            
            // 클리어 조건에 따른 추가 별
            if (stage.clearCondition == ClearCondition.NoUnitLost)
            {
                stars++;
            }
            
            // 턴 제한 내 클리어
            if (stage.turnLimit > 0)
            {
                stars++;
            }
            
            return Mathf.Clamp(stars, 1, 3);
        }
        
        void GrantReward(StageReward reward)
        {
            if (reward == null) return;
            
            // 골드, 경험치, 젬 지급 (ResourceManager 관련 코드 제거)
            Debug.Log($"보상 지급: Gold {reward.gold}, EXP {reward.exp}, Gems {reward.gems}");
            
            // 아이템 지급
            foreach (var item in reward.items)
            {
                if (UnityEngine.Random.value <= item.dropRate)
                {
                    Debug.Log($"아이템 획득: {item.itemId} x{item.quantity}");
                }
            }
            
            OnRewardReceived?.Invoke(reward);
        }
        
        void UnlockNextStage()
        {
            // 현재 챕터의 다음 스테이지 해금
            var chapter = GetChapter(currentChapterIndex);
            if (chapter != null && currentStageIndex + 1 < chapter.stages.Count)
            {
                chapter.stages[currentStageIndex + 1].isUnlocked = true;
            }
            // 마지막 스테이지였다면 다음 챕터 해금
            else if (currentChapterIndex + 1 < chapters.Count)
            {
                chapters[currentChapterIndex + 1].isUnlocked = true;
                if (chapters[currentChapterIndex + 1].stages.Count > 0)
                {
                    chapters[currentChapterIndex + 1].stages[0].isUnlocked = true;
                }
            }
        }
        
        public float GetChapterProgress(int chapterIndex)
        {
            var chapter = GetChapter(chapterIndex);
            if (chapter == null || chapter.stages.Count == 0)
                return 0f;
            
            int clearedCount = chapter.stages.Count(s => s.isCleared);
            return (float)clearedCount / chapter.stages.Count;
        }
        
        public float GetTotalProgress()
        {
            int totalStages = 0;
            int clearedStages = 0;
            
            foreach (var chapter in chapters)
            {
                totalStages += chapter.stages.Count;
                clearedStages += chapter.stages.Count(s => s.isCleared);
            }
            
            return totalStages > 0 ? (float)clearedStages / totalStages : 0f;
        }
        
        public int GetTotalStars()
        {
            int totalStars = 0;
            foreach (var chapter in chapters)
            {
                foreach (var stage in chapter.stages)
                {
                    totalStars += stage.bestStarRating;
                }
            }
            return totalStars;
        }
    }
}