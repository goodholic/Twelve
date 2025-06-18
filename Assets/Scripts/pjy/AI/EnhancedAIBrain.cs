using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace pjy.AI
{
    /// <summary>
    /// 향상된 AI 브레인 - AdvancedAISystem과 연동
    /// 실제 게임 행동 결정 및 실행
    /// </summary>
    public class EnhancedAIBrain : MonoBehaviour
    {
        [Header("AI 설정")]
        [SerializeField] private AILearningData learningData;
        [SerializeField] private float reactionTime = 0.5f;
        [SerializeField] private bool useLearnedBehavior = true;
        
        [Header("행동 우선순위")]
        [SerializeField] private float summonPriority = 1f;
        [SerializeField] private float mergePriority = 1f;
        [SerializeField] private float movePriority = 1f;
        [SerializeField] private float enhancePriority = 1f;
        
        [Header("전술 설정")]
        [SerializeField] private TacticalMode currentTactic = TacticalMode.Adaptive;
        [SerializeField] private float tacticalSwitchThreshold = 0.3f;
        
        // 컴포넌트 참조
        private PlayerController aiPlayer;
        private AdvancedAISystem advancedAI;
        private PlacementManager placementManager;
        private SummonManager summonManager;
        private AutoMergeManager mergeManager;
        
        // 상태 추적
        private float nextActionTime = 0f;
        private Queue<QueuedAction> actionQueue = new Queue<QueuedAction>();
        private Dictionary<int, float> characterLastActionTime = new Dictionary<int, float>();
        
        // 전술 상태
        private float currentPressure = 0f;
        private float economicAdvantage = 0f;
        private int consecutiveLosses = 0;
        
        private void Awake()
        {
            // 컴포넌트 찾기
            advancedAI = GetComponent<AdvancedAISystem>();
            if (advancedAI == null)
            {
                advancedAI = gameObject.AddComponent<AdvancedAISystem>();
            }
        }
        
        private void Start()
        {
            // 매니저 참조
            placementManager = PlacementManager.Instance;
            summonManager = SummonManager.Instance;
            mergeManager = AutoMergeManager.Instance;
            
            // AI 플레이어 찾기
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            aiPlayer = players.FirstOrDefault(p => p.IsAI);
            
            if (aiPlayer == null)
            {
                Debug.LogError("[EnhancedAIBrain] AI 플레이어를 찾을 수 없습니다!");
                enabled = false;
                return;
            }
            
            // 학습 데이터 로드
            if (learningData != null && useLearnedBehavior)
            {
                ApplyLearnedBehavior();
            }
        }
        
        private void Update()
        {
            if (Time.time < nextActionTime) return;
            
            // 행동 큐 처리
            if (actionQueue.Count > 0)
            {
                ProcessActionQueue();
            }
            else
            {
                // 새로운 행동 결정
                DecideNextAction();
            }
            
            nextActionTime = Time.time + reactionTime;
        }
        
        /// <summary>
        /// 다음 행동 결정
        /// </summary>
        private void DecideNextAction()
        {
            // 게임 상태 분석
            AnalyzeGameState();
            
            // 전술 모드 업데이트
            UpdateTacticalMode();
            
            // 우선순위별 행동 평가
            List<PossibleAction> possibleActions = EvaluateAllActions();
            
            // 최적 행동 선택
            if (possibleActions.Count > 0)
            {
                var bestAction = SelectBestAction(possibleActions);
                ExecuteAction(bestAction);
            }
        }
        
        /// <summary>
        /// 게임 상태 분석
        /// </summary>
        private void AnalyzeGameState()
        {
            // 압박도 계산
            currentPressure = CalculatePressure();
            
            // 경제적 우위 계산
            economicAdvantage = CalculateEconomicAdvantage();
            
            // 캐릭터 분포 분석
            AnalyzeCharacterDistribution();
        }
        
        /// <summary>
        /// 압박도 계산
        /// </summary>
        private float CalculatePressure()
        {
            float pressure = 0f;
            
            // 성 체력 기반 압박도
            var castleManager = CastleHealthManager.Instance;
            if (castleManager != null)
            {
                float myHealthRatio = GetAICastleHealthRatio();
                float enemyHealthRatio = GetEnemyCastleHealthRatio();
                
                pressure = (1f - myHealthRatio) - (1f - enemyHealthRatio);
            }
            
            // 캐릭터 수 기반 압박도
            int myCharCount = aiPlayer.CharacterCount;
            int enemyCharCount = GetEnemyCharacterCount();
            
            pressure += (enemyCharCount - myCharCount) * 0.02f;
            
            return Mathf.Clamp01(pressure + 0.5f);
        }
        
        /// <summary>
        /// 경제적 우위 계산
        /// </summary>
        private float CalculateEconomicAdvantage()
        {
            float myMinerals = aiPlayer.CurrentMinerals;
            float enemyMinerals = GetEnemyMinerals();
            
            // 캐릭터 가치 포함
            float myValue = myMinerals + (aiPlayer.CharacterCount * 30f);
            float enemyValue = enemyMinerals + (GetEnemyCharacterCount() * 30f);
            
            return (myValue - enemyValue) / 1000f;
        }
        
        /// <summary>
        /// 전술 모드 업데이트
        /// </summary>
        private void UpdateTacticalMode()
        {
            switch (currentTactic)
            {
                case TacticalMode.Adaptive:
                    // 상황에 따라 자동 전환
                    if (currentPressure > 0.7f)
                    {
                        currentTactic = TacticalMode.Defensive;
                    }
                    else if (economicAdvantage > 0.3f)
                    {
                        currentTactic = TacticalMode.Aggressive;
                    }
                    else if (economicAdvantage < -0.3f)
                    {
                        currentTactic = TacticalMode.Economic;
                    }
                    break;
                    
                case TacticalMode.Aggressive:
                    if (currentPressure > 0.8f || economicAdvantage < -0.5f)
                    {
                        currentTactic = TacticalMode.Defensive;
                    }
                    break;
                    
                case TacticalMode.Defensive:
                    if (currentPressure < 0.3f && economicAdvantage > 0f)
                    {
                        currentTactic = TacticalMode.Aggressive;
                    }
                    break;
                    
                case TacticalMode.Economic:
                    if (currentPressure > 0.6f)
                    {
                        currentTactic = TacticalMode.Defensive;
                    }
                    break;
            }
            
            Debug.Log($"[EnhancedAIBrain] 전술: {currentTactic} | 압박도: {currentPressure:F2} | 경제우위: {economicAdvantage:F2}");
        }
        
        /// <summary>
        /// 모든 가능한 행동 평가
        /// </summary>
        private List<PossibleAction> EvaluateAllActions()
        {
            List<PossibleAction> actions = new List<PossibleAction>();
            
            // 소환 평가
            if (CanSummon())
            {
                float summonScore = EvaluateSummonAction();
                actions.Add(new PossibleAction
                {
                    type = ActionType.Summon,
                    score = summonScore * summonPriority * GetTacticalMultiplier(ActionType.Summon),
                    description = "랜덤 캐릭터 소환"
                });
            }
            
            // 합성 평가
            var mergeableGroups = FindMergeableCharacters();
            foreach (var group in mergeableGroups)
            {
                float mergeScore = EvaluateMergeAction(group);
                actions.Add(new PossibleAction
                {
                    type = ActionType.Merge,
                    score = mergeScore * mergePriority * GetTacticalMultiplier(ActionType.Merge),
                    targetCharacters = group,
                    description = $"{group[0].characterName} 3개 합성"
                });
            }
            
            // 이동 평가
            var moveOptions = EvaluateMoveActions();
            actions.AddRange(moveOptions);
            
            // 종족 강화 평가
            var enhanceOptions = EvaluateEnhanceActions();
            actions.AddRange(enhanceOptions);
            
            return actions.OrderByDescending(a => a.score).ToList();
        }
        
        /// <summary>
        /// 전술에 따른 행동 가중치
        /// </summary>
        private float GetTacticalMultiplier(ActionType actionType)
        {
            switch (currentTactic)
            {
                case TacticalMode.Aggressive:
                    return actionType == ActionType.Summon ? 1.5f : 
                           actionType == ActionType.Move ? 1.2f : 0.8f;
                           
                case TacticalMode.Defensive:
                    return actionType == ActionType.Move ? 1.5f :
                           actionType == ActionType.Enhance ? 1.3f : 0.9f;
                           
                case TacticalMode.Economic:
                    return actionType == ActionType.Merge ? 1.5f :
                           actionType == ActionType.Enhance ? 1.2f : 0.8f;
                           
                default:
                    return 1f;
            }
        }
        
        /// <summary>
        /// 소환 행동 평가
        /// </summary>
        private float EvaluateSummonAction()
        {
            float score = 50f;
            
            // 캐릭터 부족 시 높은 점수
            int charCount = aiPlayer.CharacterCount;
            score *= (50f - charCount) / 50f;
            
            // 압박 상황에서 가중치
            score *= (1f + currentPressure);
            
            // 초반 웨이브에서 가중치
            var gameManager = GameManager.Instance;
            if (gameManager != null && gameManager.currentWave < 5)
            {
                score *= 1.5f;
            }
            
            return score;
        }
        
        /// <summary>
        /// 합성 가능한 캐릭터 찾기
        /// </summary>
        private List<List<Character>> FindMergeableCharacters()
        {
            List<List<Character>> mergeableGroups = new List<List<Character>>();
            
            var allCharacters = aiPlayer.OwnedCharacters;
            var groupedByName = allCharacters
                .Where(c => c != null && c.star < CharacterStar.ThreeStar)
                .GroupBy(c => new { c.characterName, c.star })
                .Where(g => g.Count() >= 3);
                
            foreach (var group in groupedByName)
            {
                mergeableGroups.Add(group.Take(3).ToList());
            }
            
            return mergeableGroups;
        }
        
        /// <summary>
        /// 합성 행동 평가
        /// </summary>
        private float EvaluateMergeAction(List<Character> characters)
        {
            if (characters == null || characters.Count < 3) return 0f;
            
            float score = 40f;
            
            // 높은 등급 합성일수록 높은 점수
            if (characters[0].star == CharacterStar.TwoStar)
            {
                score *= 2f;
            }
            
            // 후반 웨이브에서 가중치
            var gameManager = GameManager.Instance;
            if (gameManager != null && gameManager.currentWave > 10)
            {
                score *= 1.5f;
            }
            
            // 캐릭터가 많을 때 가중치
            if (aiPlayer.CharacterCount > 30)
            {
                score *= 1.3f;
            }
            
            return score;
        }
        
        /// <summary>
        /// 이동 행동 평가
        /// </summary>
        private List<PossibleAction> EvaluateMoveActions()
        {
            List<PossibleAction> moveActions = new List<PossibleAction>();
            
            // 전략적으로 중요한 이동만 고려
            var frontlineCharacters = GetFrontlineCharacters();
            
            foreach (var character in frontlineCharacters)
            {
                // 위험한 위치에 있는 캐릭터 우선
                if (IsInDangerousPosition(character))
                {
                    var safeTiles = FindSafeTiles(character);
                    foreach (var tile in safeTiles)
                    {
                        float moveScore = 60f * movePriority;
                        moveActions.Add(new PossibleAction
                        {
                            type = ActionType.Move,
                            score = moveScore,
                            targetCharacter = character,
                            targetTile = tile,
                            description = $"{character.characterName}을(를) 안전한 위치로 이동"
                        });
                    }
                }
            }
            
            return moveActions;
        }
        
        /// <summary>
        /// 최적 행동 선택
        /// </summary>
        private PossibleAction SelectBestAction(List<PossibleAction> actions)
        {
            // 학습된 패턴 적용
            if (learningData != null && useLearnedBehavior)
            {
                var topPatterns = learningData.GetTopPatterns(3);
                foreach (var pattern in topPatterns)
                {
                    var matchingAction = actions.FirstOrDefault(a => 
                        pattern.pattern.Contains(a.type.ToString()));
                        
                    if (matchingAction != null)
                    {
                        matchingAction.score *= (1f + pattern.effectiveness);
                    }
                }
            }
            
            return actions.OrderByDescending(a => a.score).First();
        }
        
        /// <summary>
        /// 행동 실행
        /// </summary>
        private void ExecuteAction(PossibleAction action)
        {
            Debug.Log($"[EnhancedAIBrain] 실행: {action.description} (점수: {action.score:F1})");
            
            switch (action.type)
            {
                case ActionType.Summon:
                    ExecuteSummon();
                    break;
                    
                case ActionType.Merge:
                    ExecuteMerge(action.targetCharacters);
                    break;
                    
                case ActionType.Move:
                    ExecuteMove(action.targetCharacter, action.targetTile);
                    break;
                    
                case ActionType.Enhance:
                    ExecuteEnhance(action.targetRace);
                    break;
            }
            
            // 행동 기록
            RecordAction(action);
        }
        
        /// <summary>
        /// 소환 실행
        /// </summary>
        private void ExecuteSummon()
        {
            if (summonManager != null && aiPlayer != null)
            {
                summonManager.OnClickRandomSummon();
            }
        }
        
        /// <summary>
        /// 합성 실행
        /// </summary>
        private void ExecuteMerge(List<Character> characters)
        {
            if (mergeManager != null && characters != null && characters.Count >= 3)
            {
                // 자동 합성 트리거
                mergeManager.StartAutoMerge(aiPlayer.AreaIndex);
            }
        }
        
        /// <summary>
        /// 이동 실행
        /// </summary>
        private void ExecuteMove(Character character, Tile targetTile)
        {
            if (character != null && targetTile != null)
            {
                // 캐릭터를 새 타일로 이동
                character.SetCurrentTile(targetTile);
                targetTile.AddOccupyingCharacter(character);
            }
        }
        
        /// <summary>
        /// 학습된 행동 적용
        /// </summary>
        private void ApplyLearnedBehavior()
        {
            // 학습된 최적 파라미터 적용
            var stats = learningData.GetStatsSummary();
            Debug.Log($"[EnhancedAIBrain] 학습 데이터 로드: {stats}");
            
            // 성공률 높은 전략에 따라 우선순위 조정
            // 실제 구현에서는 더 정교한 로직 필요
        }
        
        /// <summary>
        /// 행동 기록
        /// </summary>
        private void RecordAction(PossibleAction action)
        {
            // 행동 패턴 기록
            string actionPattern = $"{currentTactic}-{action.type}";
            
            // 나중에 효과성 평가를 위해 저장
            actionQueue.Enqueue(new QueuedAction
            {
                action = action,
                executionTime = Time.time,
                gameStateBeforeAction = CaptureGameState()
            });
        }
        
        /// <summary>
        /// 게임 상태 캡처
        /// </summary>
        private GameStateSnapshot CaptureGameState()
        {
            return new GameStateSnapshot
            {
                aiCharacterCount = aiPlayer.CharacterCount,
                aiMinerals = aiPlayer.CurrentMinerals,
                timestamp = Time.time
            };
        }
        
        // 유틸리티 메서드들
        private bool CanSummon()
        {
            return aiPlayer.CurrentMinerals >= 30 && 
                   aiPlayer.CharacterCount < 50;
        }
        
        private float GetAICastleHealthRatio()
        {
            // AI 성 체력 비율 계산
            return 0.8f; // 실제 구현 필요
        }
        
        private float GetEnemyCastleHealthRatio()
        {
            // 적 성 체력 비율 계산
            return 0.7f; // 실제 구현 필요
        }
        
        private int GetEnemyCharacterCount()
        {
            // 적 캐릭터 수 가져오기
            return 20; // 실제 구현 필요
        }
        
        private float GetEnemyMinerals()
        {
            // 적 미네랄 추정
            return 100f; // 실제 구현 필요
        }
        
        private void AnalyzeCharacterDistribution()
        {
            // 캐릭터 분포 분석
        }
        
        private List<Character> GetFrontlineCharacters()
        {
            // 최전방 캐릭터 가져오기
            return new List<Character>(); // 실제 구현 필요
        }
        
        private bool IsInDangerousPosition(Character character)
        {
            // 위험한 위치 판단
            return false; // 실제 구현 필요
        }
        
        private List<Tile> FindSafeTiles(Character character)
        {
            // 안전한 타일 찾기
            return new List<Tile>(); // 실제 구현 필요
        }
        
        private List<PossibleAction> EvaluateEnhanceActions()
        {
            // 종족 강화 평가
            return new List<PossibleAction>(); // 실제 구현 필요
        }
        
        private void ExecuteEnhance(RaceType race)
        {
            // 종족 강화 실행
            Debug.Log($"[EnhancedAIBrain] {race} 종족 강화");
        }
        
        private void ProcessActionQueue()
        {
            // 행동 큐 처리
            if (actionQueue.Count > 0)
            {
                var queuedAction = actionQueue.Dequeue();
                // 효과성 평가 및 학습
            }
        }
    }
    
    // 데이터 구조체들
    public enum TacticalMode
    {
        Aggressive,  // 공격적
        Defensive,   // 방어적
        Economic,    // 경제 중심
        Adaptive     // 적응형
    }
    
    public class PossibleAction
    {
        public ActionType type;
        public float score;
        public string description;
        public Character targetCharacter;
        public Tile targetTile;
        public List<Character> targetCharacters;
        public RaceType targetRace;
    }
    
    public class QueuedAction
    {
        public PossibleAction action;
        public float executionTime;
        public GameStateSnapshot gameStateBeforeAction;
    }
    
    public class GameStateSnapshot
    {
        public int aiCharacterCount;
        public float aiMinerals;
        public float timestamp;
    }
}