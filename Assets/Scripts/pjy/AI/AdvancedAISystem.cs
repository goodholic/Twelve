using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace pjy.AI
{
    /// <summary>
    /// 고급 AI 시스템 - 패턴 인식, 학습, 동적 난이도 조절
    /// </summary>
    public class AdvancedAISystem : MonoBehaviour
    {
        [Header("AI 설정")]
        [SerializeField] private float decisionInterval = 2f;
        [SerializeField] private bool enableLearning = true;
        [SerializeField] private bool enableDynamicDifficulty = true;
        
        [Header("난이도 설정")]
        [SerializeField, Range(0f, 1f)] private float baseDifficulty = 0.5f;
        [SerializeField, Range(0f, 1f)] private float currentDifficulty = 0.5f;
        [SerializeField] private float difficultyAdjustmentSpeed = 0.1f;
        
        [Header("학습 파라미터")]
        [SerializeField] private int memorySize = 100;
        [SerializeField] private float learningRate = 0.1f;
        [SerializeField] private float explorationRate = 0.2f;
        
        [Header("전략 가중치")]
        [SerializeField] private float aggressiveWeight = 0.5f;
        [SerializeField] private float defensiveWeight = 0.5f;
        [SerializeField] private float economicWeight = 0.5f;
        
        // AI 상태
        private PlayerController aiPlayer;
        private PlayerController opponentPlayer;
        private float nextDecisionTime = 0f;
        
        // 패턴 인식
        private List<PlayerAction> playerActionHistory = new List<PlayerAction>();
        private Dictionary<string, float> patternScores = new Dictionary<string, float>();
        private Dictionary<string, int> strategyCounters = new Dictionary<string, int>();
        
        // 학습 데이터
        private List<DecisionRecord> decisionHistory = new List<DecisionRecord>();
        private Dictionary<string, float> actionSuccessRates = new Dictionary<string, float>();
        
        // 전략 분석
        private PlayerStrategy currentOpponentStrategy = PlayerStrategy.Balanced;
        private float[] strategyConfidence = new float[System.Enum.GetValues(typeof(PlayerStrategy)).Length];
        
        // 성능 추적
        private int winsCount = 0;
        private int lossesCount = 0;
        private float averageGameDuration = 0f;
        private List<float> recentWinRates = new List<float>();
        
        private void Awake()
        {
            // 초기 전략 가중치 설정
            InitializeStrategies();
        }
        
        private void Start()
        {
            // AI 플레이어 찾기
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.IsAI)
                {
                    aiPlayer = player;
                }
                else
                {
                    opponentPlayer = player;
                }
            }
            
            if (aiPlayer == null)
            {
                Debug.LogError("[AdvancedAISystem] AI 플레이어를 찾을 수 없습니다!");
                enabled = false;
            }
        }
        
        private void Update()
        {
            if (Time.time >= nextDecisionTime)
            {
                MakeDecision();
                nextDecisionTime = Time.time + decisionInterval;
            }
            
            // 상대 행동 추적
            TrackOpponentActions();
            
            // 패턴 분석
            if (playerActionHistory.Count > 10)
            {
                AnalyzePatterns();
            }
        }
        
        /// <summary>
        /// AI 의사결정
        /// </summary>
        private void MakeDecision()
        {
            // 게임 상태 분석
            GameState currentState = AnalyzeGameState();
            
            // 상대 전략 예측
            PredictOpponentStrategy();
            
            // 최적 행동 선택
            AIAction bestAction = SelectBestAction(currentState);
            
            // 행동 실행
            ExecuteAction(bestAction);
            
            // 학습 데이터 기록
            if (enableLearning)
            {
                RecordDecision(currentState, bestAction);
            }
        }
        
        /// <summary>
        /// 게임 상태 분석
        /// </summary>
        private GameState AnalyzeGameState()
        {
            GameState state = new GameState();
            
            // 자원 상태
            state.aiMinerals = aiPlayer.CurrentMinerals;
            state.opponentMinerals = opponentPlayer != null ? opponentPlayer.CurrentMinerals : 0;
            
            // 캐릭터 수
            state.aiCharacterCount = aiPlayer.CharacterCount;
            state.opponentCharacterCount = opponentPlayer != null ? opponentPlayer.CharacterCount : 0;
            
            // 성 체력
            var castleManager = CastleHealthManager.Instance;
            if (castleManager != null)
            {
                state.aiCastleHealth = GetTotalCastleHealth(aiPlayer.PlayerID);
                state.opponentCastleHealth = GetTotalCastleHealth(opponentPlayer?.PlayerID ?? 0);
            }
            
            // 현재 웨이브
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                state.currentWave = gameManager.currentWave;
                state.totalWaves = 20;
            }
            
            // 위협도 계산
            state.threatLevel = CalculateThreatLevel();
            state.opportunityScore = CalculateOpportunityScore();
            
            return state;
        }
        
        /// <summary>
        /// 상대 전략 예측
        /// </summary>
        private void PredictOpponentStrategy()
        {
            if (playerActionHistory.Count < 5) return;
            
            // 최근 행동 분석
            var recentActions = playerActionHistory.TakeLast(20).ToList();
            
            int summonCount = recentActions.Count(a => a.actionType == ActionType.Summon);
            int mergeCount = recentActions.Count(a => a.actionType == ActionType.Merge);
            int moveCount = recentActions.Count(a => a.actionType == ActionType.Move);
            
            // 전략 신뢰도 계산
            strategyConfidence[(int)PlayerStrategy.Aggressive] = (float)summonCount / recentActions.Count;
            strategyConfidence[(int)PlayerStrategy.Defensive] = (float)moveCount / recentActions.Count;
            strategyConfidence[(int)PlayerStrategy.Economic] = (float)mergeCount / recentActions.Count;
            strategyConfidence[(int)PlayerStrategy.Balanced] = 1f - Mathf.Max(
                strategyConfidence[(int)PlayerStrategy.Aggressive],
                strategyConfidence[(int)PlayerStrategy.Defensive],
                strategyConfidence[(int)PlayerStrategy.Economic]
            );
            
            // 가장 높은 신뢰도의 전략 선택
            int maxIndex = 0;
            float maxConfidence = 0f;
            for (int i = 0; i < strategyConfidence.Length; i++)
            {
                if (strategyConfidence[i] > maxConfidence)
                {
                    maxConfidence = strategyConfidence[i];
                    maxIndex = i;
                }
            }
            
            currentOpponentStrategy = (PlayerStrategy)maxIndex;
            
            Debug.Log($"[AdvancedAISystem] 상대 전략 예측: {currentOpponentStrategy} (신뢰도: {maxConfidence:P})");
        }
        
        /// <summary>
        /// 최적 행동 선택
        /// </summary>
        private AIAction SelectBestAction(GameState state)
        {
            List<AIAction> possibleActions = GeneratePossibleActions(state);
            
            if (possibleActions.Count == 0)
            {
                return new AIAction { type = AIActionType.Wait };
            }
            
            // 탐색 vs 활용
            if (Random.value < explorationRate && enableLearning)
            {
                // 탐색: 랜덤 행동
                return possibleActions[Random.Range(0, possibleActions.Count)];
            }
            
            // 활용: 최고 점수 행동 선택
            float bestScore = float.MinValue;
            AIAction bestAction = possibleActions[0];
            
            foreach (var action in possibleActions)
            {
                float score = EvaluateAction(action, state);
                
                // 학습된 성공률 반영
                if (actionSuccessRates.ContainsKey(action.ToString()))
                {
                    score *= (1f + actionSuccessRates[action.ToString()]);
                }
                
                // 난이도 조정
                if (enableDynamicDifficulty)
                {
                    score *= currentDifficulty;
                }
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestAction = action;
                }
            }
            
            return bestAction;
        }
        
        /// <summary>
        /// 가능한 행동 생성
        /// </summary>
        private List<AIAction> GeneratePossibleActions(GameState state)
        {
            List<AIAction> actions = new List<AIAction>();
            
            // 소환 가능 확인
            if (state.aiMinerals >= 30 && state.aiCharacterCount < 50)
            {
                actions.Add(new AIAction { type = AIActionType.Summon });
            }
            
            // 합성 가능 확인
            if (CanMerge())
            {
                actions.Add(new AIAction { type = AIActionType.Merge });
            }
            
            // 강화 가능 확인
            foreach (RaceType race in System.Enum.GetValues(typeof(RaceType)))
            {
                if (CanEnhanceRace(race))
                {
                    actions.Add(new AIAction 
                    { 
                        type = AIActionType.Enhance,
                        targetRace = race
                    });
                }
            }
            
            // 이동 가능 확인
            var moveableCharacters = GetMoveableCharacters();
            foreach (var character in moveableCharacters)
            {
                var possibleMoves = GetPossibleMoves(character);
                foreach (var targetTile in possibleMoves)
                {
                    actions.Add(new AIAction
                    {
                        type = AIActionType.Move,
                        targetCharacter = character,
                        targetTile = targetTile
                    });
                }
            }
            
            // 대기
            actions.Add(new AIAction { type = AIActionType.Wait });
            
            return actions;
        }
        
        /// <summary>
        /// 행동 평가
        /// </summary>
        private float EvaluateAction(AIAction action, GameState state)
        {
            float score = 0f;
            
            switch (action.type)
            {
                case AIActionType.Summon:
                    score = EvaluateSummon(state);
                    break;
                    
                case AIActionType.Merge:
                    score = EvaluateMerge(state);
                    break;
                    
                case AIActionType.Enhance:
                    score = EvaluateEnhance(action.targetRace, state);
                    break;
                    
                case AIActionType.Move:
                    score = EvaluateMove(action.targetCharacter, action.targetTile, state);
                    break;
                    
                case AIActionType.Wait:
                    score = EvaluateWait(state);
                    break;
            }
            
            // 상대 전략에 따른 가중치 조정
            score = AdjustScoreByOpponentStrategy(score, action.type);
            
            return score;
        }
        
        /// <summary>
        /// 상대 전략에 따른 점수 조정
        /// </summary>
        private float AdjustScoreByOpponentStrategy(float baseScore, AIActionType actionType)
        {
            switch (currentOpponentStrategy)
            {
                case PlayerStrategy.Aggressive:
                    // 상대가 공격적일 때는 방어적 행동 우선
                    if (actionType == AIActionType.Move || actionType == AIActionType.Enhance)
                    {
                        baseScore *= 1.3f;
                    }
                    break;
                    
                case PlayerStrategy.Defensive:
                    // 상대가 방어적일 때는 공격적 행동 우선
                    if (actionType == AIActionType.Summon)
                    {
                        baseScore *= 1.3f;
                    }
                    break;
                    
                case PlayerStrategy.Economic:
                    // 상대가 경제 중심일 때는 압박 우선
                    if (actionType == AIActionType.Summon || actionType == AIActionType.Move)
                    {
                        baseScore *= 1.2f;
                    }
                    break;
            }
            
            return baseScore;
        }
        
        /// <summary>
        /// 소환 평가
        /// </summary>
        private float EvaluateSummon(GameState state)
        {
            float score = 50f;
            
            // 캐릭터 수 부족 시 가중치 증가
            float characterRatio = (float)state.aiCharacterCount / 50f;
            score *= (1.5f - characterRatio);
            
            // 위협도에 따른 조정
            score *= (1f + state.threatLevel * 0.5f);
            
            // 경제 상태 고려
            float economicFactor = Mathf.Min(state.aiMinerals / 100f, 2f);
            score *= economicFactor;
            
            return score * aggressiveWeight;
        }
        
        /// <summary>
        /// 합성 평가
        /// </summary>
        private float EvaluateMerge(GameState state)
        {
            float score = 40f;
            
            // 후반 웨이브일수록 합성 우선순위 증가
            float waveProgress = (float)state.currentWave / state.totalWaves;
            score *= (1f + waveProgress);
            
            // 캐릭터 수가 많을수록 합성 우선
            if (state.aiCharacterCount > 30)
            {
                score *= 1.5f;
            }
            
            return score * economicWeight;
        }
        
        /// <summary>
        /// 행동 실행
        /// </summary>
        private void ExecuteAction(AIAction action)
        {
            switch (action.type)
            {
                case AIActionType.Summon:
                    ExecuteSummon();
                    break;
                    
                case AIActionType.Merge:
                    ExecuteMerge();
                    break;
                    
                case AIActionType.Enhance:
                    ExecuteEnhance(action.targetRace);
                    break;
                    
                case AIActionType.Move:
                    ExecuteMove(action.targetCharacter, action.targetTile);
                    break;
            }
            
            Debug.Log($"[AdvancedAISystem] 실행: {action.type}");
        }
        
        /// <summary>
        /// 소환 실행
        /// </summary>
        private void ExecuteSummon()
        {
            var summonManager = SummonManager.Instance;
            if (summonManager != null && aiPlayer != null)
            {
                summonManager.OnClickRandomSummon();
            }
        }
        
        /// <summary>
        /// 합성 실행
        /// </summary>
        private void ExecuteMerge()
        {
            var autoMergeManager = AutoMergeManager.Instance;
            if (autoMergeManager != null)
            {
                // AI 플레이어의 자동 합성 트리거
                autoMergeManager.StartAutoMerge(aiPlayer.AreaIndex);
            }
        }
        
        /// <summary>
        /// 패턴 분석
        /// </summary>
        private void AnalyzePatterns()
        {
            // 3-그램 패턴 분석
            for (int i = 0; i < playerActionHistory.Count - 3; i++)
            {
                string pattern = $"{playerActionHistory[i].actionType}-{playerActionHistory[i+1].actionType}-{playerActionHistory[i+2].actionType}";
                
                if (!patternScores.ContainsKey(pattern))
                {
                    patternScores[pattern] = 0;
                }
                
                patternScores[pattern]++;
            }
            
            // 가장 빈번한 패턴 찾기
            var topPatterns = patternScores.OrderByDescending(p => p.Value).Take(5).ToList();
            
            foreach (var pattern in topPatterns)
            {
                Debug.Log($"[AdvancedAISystem] 패턴 발견: {pattern.Key} (빈도: {pattern.Value})");
            }
        }
        
        /// <summary>
        /// 동적 난이도 조절
        /// </summary>
        private void AdjustDynamicDifficulty()
        {
            if (!enableDynamicDifficulty) return;
            
            // 최근 10게임 승률 계산
            if (recentWinRates.Count >= 10)
            {
                float avgWinRate = recentWinRates.Average();
                
                // 목표 승률: 40-60%
                if (avgWinRate > 0.6f)
                {
                    // AI가 너무 강함 - 난이도 감소
                    currentDifficulty = Mathf.Max(0.3f, currentDifficulty - difficultyAdjustmentSpeed);
                }
                else if (avgWinRate < 0.4f)
                {
                    // AI가 너무 약함 - 난이도 증가
                    currentDifficulty = Mathf.Min(1f, currentDifficulty + difficultyAdjustmentSpeed);
                }
                
                Debug.Log($"[AdvancedAISystem] 난이도 조정: {currentDifficulty:F2} (승률: {avgWinRate:P})");
            }
        }
        
        /// <summary>
        /// 게임 결과 기록
        /// </summary>
        public void RecordGameResult(bool aiWon, float gameDuration)
        {
            if (aiWon)
            {
                winsCount++;
            }
            else
            {
                lossesCount++;
            }
            
            // 승률 업데이트
            float winRate = (float)winsCount / (winsCount + lossesCount);
            recentWinRates.Add(aiWon ? 1f : 0f);
            
            // 최근 10게임만 유지
            if (recentWinRates.Count > 10)
            {
                recentWinRates.RemoveAt(0);
            }
            
            // 평균 게임 시간 업데이트
            averageGameDuration = (averageGameDuration * (winsCount + lossesCount - 1) + gameDuration) / (winsCount + lossesCount);
            
            // 동적 난이도 조절
            AdjustDynamicDifficulty();
            
            Debug.Log($"[AdvancedAISystem] 게임 결과: {(aiWon ? "승리" : "패배")} | 전체 승률: {winRate:P} | 게임 시간: {gameDuration:F1}초");
        }
        
        /// <summary>
        /// 상대 행동 추적
        /// </summary>
        private void TrackOpponentActions()
        {
            // 이 메서드는 실제 구현 시 상대 플레이어의 행동을 감지하여 기록
            // 예: 소환, 합성, 이동 등의 이벤트 감지
        }
        
        // 유틸리티 메서드들
        private bool CanMerge()
        {
            // 합성 가능 여부 확인 로직
            return false; // 실제 구현 필요
        }
        
        private bool CanEnhanceRace(RaceType race)
        {
            // 종족 강화 가능 여부 확인
            return false; // 실제 구현 필요
        }
        
        private List<Character> GetMoveableCharacters()
        {
            // 이동 가능한 캐릭터 목록
            return new List<Character>(); // 실제 구현 필요
        }
        
        private List<Tile> GetPossibleMoves(Character character)
        {
            // 캐릭터의 가능한 이동 위치
            return new List<Tile>(); // 실제 구현 필요
        }
        
        private float GetTotalCastleHealth(int playerIndex)
        {
            // 플레이어의 총 성 체력
            return 1000f; // 실제 구현 필요
        }
        
        private float CalculateThreatLevel()
        {
            // 위협도 계산 (0.0 ~ 1.0)
            return 0.5f; // 실제 구현 필요
        }
        
        private float CalculateOpportunityScore()
        {
            // 기회 점수 계산
            return 0.5f; // 실제 구현 필요
        }
        
        private float EvaluateEnhance(RaceType race, GameState state)
        {
            // 종족 강화 평가
            return 30f * defensiveWeight;
        }
        
        private float EvaluateMove(Character character, Tile targetTile, GameState state)
        {
            // 이동 평가
            return 20f * defensiveWeight;
        }
        
        private float EvaluateWait(GameState state)
        {
            // 대기 평가 (자원 축적)
            return 10f;
        }
        
        private void ExecuteEnhance(RaceType race)
        {
            // 종족 강화 실행
            Debug.Log($"[AdvancedAISystem] {race} 종족 강화");
        }
        
        private void ExecuteMove(Character character, Tile targetTile)
        {
            // 캐릭터 이동 실행
            if (character != null && targetTile != null)
            {
                Debug.Log($"[AdvancedAISystem] {character.characterName} 이동");
            }
        }
        
        private void InitializeStrategies()
        {
            // 전략별 초기 카운터 설정
            strategyCounters["rush"] = 0;
            strategyCounters["turtle"] = 0;
            strategyCounters["boom"] = 0;
            strategyCounters["timing"] = 0;
        }
        
        private void RecordDecision(GameState state, AIAction action)
        {
            var record = new DecisionRecord
            {
                timestamp = Time.time,
                gameState = state,
                action = action,
                immediateReward = 0f // 나중에 계산
            };
            
            decisionHistory.Add(record);
            
            // 메모리 크기 제한
            if (decisionHistory.Count > memorySize)
            {
                decisionHistory.RemoveAt(0);
            }
        }
    }
    
    // 데이터 구조체들
    [System.Serializable]
    public struct GameState
    {
        public int aiMinerals;
        public int opponentMinerals;
        public int aiCharacterCount;
        public int opponentCharacterCount;
        public float aiCastleHealth;
        public float opponentCastleHealth;
        public int currentWave;
        public int totalWaves;
        public float threatLevel;
        public float opportunityScore;
    }
    
    [System.Serializable]
    public struct AIAction
    {
        public AIActionType type;
        public Character targetCharacter;
        public Tile targetTile;
        public RaceType targetRace;
        
        public override string ToString()
        {
            return $"{type}_{targetRace}";
        }
    }
    
    [System.Serializable]
    public class PlayerAction
    {
        public float timestamp;
        public ActionType actionType;
        public Vector2 position;
        public int characterId;
    }
    
    [System.Serializable]
    public class DecisionRecord
    {
        public float timestamp;
        public GameState gameState;
        public AIAction action;
        public float immediateReward;
        public float delayedReward;
    }
    
    public enum AIActionType
    {
        Summon,
        Merge,
        Enhance,
        Move,
        Wait
    }
    
    public enum ActionType
    {
        Summon,
        Merge,
        Move,
        Enhance,
        Sell
    }
    
    public enum PlayerStrategy
    {
        Aggressive,
        Defensive,
        Economic,
        Balanced
    }
}