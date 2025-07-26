using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AIPlayer : MonoBehaviour
{
    [Header("AI 설정")]
    public bool isAIEnabled = false;
    public GameManager.Team aiTeam = GameManager.Team.O;
    public float thinkingTime = 1.5f;
    public AILevel difficulty = AILevel.Normal;
    
    public enum AILevel
    {
        Easy,    // 랜덤 배치
        Normal,  // 기본 전략
        Hard     // 고급 전략
    }
    
    private GameManager gameManager;
    private BoardManager boardManager;
    
    // AI 평가 가중치
    private const float EMPTY_TILE_SCORE = 10f;
    private const float ENEMY_KILL_SCORE = 30f;
    private const float CONTROL_CENTER_SCORE = 5f;
    private const float AVOID_DEATH_PENALTY = -20f;
    private const float AREA_BALANCE_SCORE = 15f;
    
    void Start()
    {
        gameManager = GameManager.Instance;
        boardManager = FindObjectOfType<BoardManager>();
    }
    
    void Update()
    {
        if (!isAIEnabled || gameManager == null) return;
        
        // AI 차례인지 확인
        if (gameManager.currentState == GameManager.GameState.Battle && 
            gameManager.currentTurn == aiTeam &&
            gameManager.selectedCharacter == null)
        {
            StartCoroutine(AITurn());
        }
    }
    
    IEnumerator AITurn()
    {
        // 생각하는 시간
        yield return new WaitForSeconds(thinkingTime);
        
        // 최적의 행동 결정
        AIAction bestAction = DecideBestAction();
        
        if (bestAction != null)
        {
            // 캐릭터 선택
            gameManager.OnCharacterButtonClick(0); // 임시로 첫 번째 버튼 사용
            yield return new WaitForSeconds(0.5f);
            
            // 타일 배치
            gameManager.OnTileClick(bestAction.boardIndex, bestAction.x, bestAction.y);
        }
    }
    
    AIAction DecideBestAction()
    {
        List<AIAction> possibleActions = new List<AIAction>();
        
        // 사용 가능한 캐릭터 목록
        List<CharacterData> availableCharacters = GetAvailableCharacters();
        if (availableCharacters.Count == 0) return null;
        
        // 모든 가능한 행동 평가
        foreach (CharacterData character in availableCharacters)
        {
            for (int board = 0; board < 2; board++)
            {
                for (int x = 0; x < GameManager.BOARD_WIDTH; x++)
                {
                    for (int y = 0; y < GameManager.BOARD_HEIGHT; y++)
                    {
                        float score = EvaluatePosition(character, board, x, y);
                        if (score > float.MinValue) // 배치 가능한 경우
                        {
                            possibleActions.Add(new AIAction
                            {
                                character = character,
                                boardIndex = board,
                                x = x,
                                y = y,
                                score = score
                            });
                        }
                    }
                }
            }
        }
        
        if (possibleActions.Count == 0) return null;
        
        // 난이도에 따른 선택
        switch (difficulty)
        {
            case AILevel.Easy:
                // 랜덤 선택
                return possibleActions[Random.Range(0, possibleActions.Count)];
                
            case AILevel.Normal:
                // 상위 3개 중 랜덤
                var topActions = possibleActions.OrderByDescending(a => a.score).Take(3).ToList();
                return topActions[Random.Range(0, topActions.Count)];
                
            case AILevel.Hard:
                // 최고 점수 선택
                return possibleActions.OrderByDescending(a => a.score).First();
        }
        
        return null;
    }
    
    float EvaluatePosition(CharacterData character, int boardIndex, int x, int y)
    {
        Character existingChar = gameManager.boardState[boardIndex, x, y];
        
        // 아군이 있으면 배치 불가
        if (existingChar != null && existingChar.team == aiTeam)
            return float.MinValue;
        
        float score = 0f;
        
        // 빈 타일 점수
        if (existingChar == null)
        {
            score += EMPTY_TILE_SCORE;
        }
        else
        {
            // 전투 시뮬레이션
            var battleResult = BattleSystem.Instance.SimulateBattle(character, existingChar);
            if (!battleResult.canPlace)
                return float.MinValue;
                
            if (!battleResult.defenderSurvives)
                score += ENEMY_KILL_SCORE;
            if (!battleResult.attackerSurvives)
                score += AVOID_DEATH_PENALTY;
        }
        
        // 중앙 지역 보너스
        if (x >= 2 && x <= 3 && y == 1)
            score += CONTROL_CENTER_SCORE;
        
        // 보드 균형 점수
        score += EvaluateBoardBalance(boardIndex);
        
        // 공격 범위 점수
        score += EvaluateAttackPotential(character, boardIndex, x, y);
        
        return score;
    }
    
    float EvaluateBoardBalance(int boardIndex)
    {
        int[] teamCounts = new int[2];
        
        for (int b = 0; b < 2; b++)
        {
            int aiCount = 0, enemyCount = 0;
            
            for (int x = 0; x < GameManager.BOARD_WIDTH; x++)
            {
                for (int y = 0; y < GameManager.BOARD_HEIGHT; y++)
                {
                    Character c = gameManager.boardState[b, x, y];
                    if (c != null)
                    {
                        if (c.team == aiTeam) aiCount++;
                        else enemyCount++;
                    }
                }
            }
            
            teamCounts[b] = aiCount - enemyCount;
        }
        
        // 균형잡힌 배치 선호
        if (teamCounts[boardIndex] < teamCounts[1 - boardIndex])
            return AREA_BALANCE_SCORE;
        
        return 0f;
    }
    
    float EvaluateAttackPotential(CharacterData character, int boardIndex, int x, int y)
    {
        float attackScore = 0f;
        List<Vector2Int> attackPositions = character.GetAttackPositions();
        
        foreach (Vector2Int offset in attackPositions)
        {
            int targetX = x + offset.x;
            int targetY = y + offset.y;
            int targetBoard = boardIndex;
            
            if (AttackPatternManager.IsCrossBoardAttack(character.attackPattern))
            {
                targetBoard = 1 - boardIndex;
                targetX = x;
                targetY = y;
            }
            
            if (IsValidPosition(targetBoard, targetX, targetY))
            {
                Character target = gameManager.boardState[targetBoard, targetX, targetY];
                if (target != null && target.team != aiTeam)
                {
                    attackScore += 10f; // 적을 공격할 수 있는 위치
                }
            }
        }
        
        return attackScore;
    }
    
    List<CharacterData> GetAvailableCharacters()
    {
        List<CharacterData> pool = aiTeam == GameManager.Team.X ? 
            gameManager.xTeamPool : gameManager.oTeamPool;
            
        Dictionary<CharacterData, int> usageCount = aiTeam == GameManager.Team.X ? 
            gameManager.xTeamUsageCount : gameManager.oTeamUsageCount;
        
        List<CharacterData> available = new List<CharacterData>();
        
        foreach (var character in pool)
        {
            int count = usageCount.ContainsKey(character) ? usageCount[character] : 0;
            if (count < gameManager.maxUsagePerCharacter)
            {
                available.Add(character);
            }
        }
        
        return available;
    }
    
    bool IsValidPosition(int boardIndex, int x, int y)
    {
        return boardIndex >= 0 && boardIndex < 2 &&
               x >= 0 && x < GameManager.BOARD_WIDTH &&
               y >= 0 && y < GameManager.BOARD_HEIGHT;
    }
    
    // AI 행동 데이터
    class AIAction
    {
        public CharacterData character;
        public int boardIndex;
        public int x;
        public int y;
        public float score;
    }
}