using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildMaster.TileBattle
{
    /// <summary>
    /// 타일 점령 배틀 AI 시스템
    /// 캐릭터 선택, 배치 전략, 승리 조건 달성을 위한 AI 로직
    /// </summary>
    public class TileBattleAI : MonoBehaviour
    {
        // AI 난이도
        public enum AILevel
        {
            Easy,    // 랜덤 배치
            Normal,  // 기본 전략
            Hard,    // 고급 전략
            Expert   // 최적화된 전략
        }

        // 타일 정보
        public class TileInfo
        {
            public int tileIndex; // 0 = A타일(위), 1 = B타일(아래)
            public Vector2Int position;
            public Character occupant;
            public int controlScore; // 타일 제어 점수
        }

        // 캐릭터 정보
        [System.Serializable]
        public class Character
        {
            public string id;
            public string name;
            public int attackPower;
            public List<Vector2Int> attackRange; // 공격 범위 패턴
            public bool isAlive = true;
            public int team; // 0 = 플레이어, 1 = AI
        }

        // AI 설정
        [SerializeField] private AILevel aiLevel = AILevel.Normal;
        [SerializeField] private float thinkingTime = 1.0f; // AI 고민 시간

        // 게임 상태
        private const int TILE_WIDTH = 6;
        private const int TILE_HEIGHT = 3;
        private const int MAX_CHARACTERS = 10;
        
        private TileInfo[,] tileA = new TileInfo[TILE_WIDTH, TILE_HEIGHT]; // 위쪽 타일
        private TileInfo[,] tileB = new TileInfo[TILE_WIDTH, TILE_HEIGHT]; // 아래쪽 타일
        
        private List<Character> aiCharacterPool; // AI가 선택 가능한 캐릭터 풀
        private List<Character> selectedCharacters = new List<Character>(); // AI가 선택한 캐릭터
        private List<Character> deployedCharacters = new List<Character>(); // 배치된 캐릭터

        // 공격 범위 패턴 정의
        private readonly Dictionary<string, List<Vector2Int>> attackPatterns = new Dictionary<string, List<Vector2Int>>
        {
            // 십자 패턴 (기본)
            ["cross"] = new List<Vector2Int> { 
                new Vector2Int(0, 1), new Vector2Int(0, -1), 
                new Vector2Int(1, 0), new Vector2Int(-1, 0) 
            },
            
            // 대각선 패턴
            ["diagonal"] = new List<Vector2Int> { 
                new Vector2Int(1, 1), new Vector2Int(1, -1), 
                new Vector2Int(-1, 1), new Vector2Int(-1, -1) 
            },
            
            // 주변 8칸
            ["surrounding"] = new List<Vector2Int> { 
                new Vector2Int(0, 1), new Vector2Int(0, -1), 
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(1, 1), new Vector2Int(1, -1), 
                new Vector2Int(-1, 1), new Vector2Int(-1, -1) 
            },
            
            // 직선 3칸
            ["line3"] = new List<Vector2Int> { 
                new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3) 
            },
            
            // L자 패턴
            ["lShape"] = new List<Vector2Int> { 
                new Vector2Int(0, 1), new Vector2Int(0, 2), 
                new Vector2Int(1, 0), new Vector2Int(2, 0) 
            }
        };

        void Awake()
        {
            InitializeTiles();
            InitializeCharacterPool();
        }

        /// <summary>
        /// 타일 초기화
        /// </summary>
        void InitializeTiles()
        {
            for (int x = 0; x < TILE_WIDTH; x++)
            {
                for (int y = 0; y < TILE_HEIGHT; y++)
                {
                    tileA[x, y] = new TileInfo { tileIndex = 0, position = new Vector2Int(x, y) };
                    tileB[x, y] = new TileInfo { tileIndex = 1, position = new Vector2Int(x, y) };
                }
            }
        }

        /// <summary>
        /// AI 캐릭터 풀 초기화
        /// </summary>
        void InitializeCharacterPool()
        {
            aiCharacterPool = new List<Character>();
            
            // 다양한 공격 범위를 가진 캐릭터들 생성
            aiCharacterPool.Add(new Character { 
                id = "warrior_1", name = "전사", attackPower = 10, 
                attackRange = attackPatterns["cross"] 
            });
            
            aiCharacterPool.Add(new Character { 
                id = "mage_1", name = "마법사", attackPower = 8, 
                attackRange = attackPatterns["surrounding"] 
            });
            
            aiCharacterPool.Add(new Character { 
                id = "archer_1", name = "궁수", attackPower = 7, 
                attackRange = attackPatterns["line3"] 
            });
            
            aiCharacterPool.Add(new Character { 
                id = "knight_1", name = "기사", attackPower = 9, 
                attackRange = attackPatterns["lShape"] 
            });
            
            aiCharacterPool.Add(new Character { 
                id = "assassin_1", name = "암살자", attackPower = 11, 
                attackRange = attackPatterns["diagonal"] 
            });
            
            // 추가 캐릭터들
            for (int i = 2; i <= 3; i++)
            {
                aiCharacterPool.Add(new Character { 
                    id = $"warrior_{i}", name = $"전사{i}", attackPower = 10 + i, 
                    attackRange = attackPatterns["cross"] 
                });
                
                aiCharacterPool.Add(new Character { 
                    id = $"mage_{i}", name = $"마법사{i}", attackPower = 8 + i, 
                    attackRange = attackPatterns["surrounding"] 
                });
            }
        }

        /// <summary>
        /// AI가 10개의 캐릭터를 선택
        /// </summary>
        public List<Character> SelectCharacters()
        {
            selectedCharacters.Clear();
            
            switch (aiLevel)
            {
                case AILevel.Easy:
                    SelectCharactersRandom();
                    break;
                case AILevel.Normal:
                    SelectCharactersBalanced();
                    break;
                case AILevel.Hard:
                case AILevel.Expert:
                    SelectCharactersStrategic();
                    break;
            }
            
            Debug.Log($"AI가 {selectedCharacters.Count}개의 캐릭터를 선택했습니다.");
            return selectedCharacters;
        }

        /// <summary>
        /// 랜덤 캐릭터 선택 (Easy)
        /// </summary>
        void SelectCharactersRandom()
        {
            var shuffled = aiCharacterPool.OrderBy(x => Random.value).ToList();
            selectedCharacters = shuffled.Take(MAX_CHARACTERS).ToList();
        }

        /// <summary>
        /// 균형잡힌 캐릭터 선택 (Normal)
        /// </summary>
        void SelectCharactersBalanced()
        {
            // 각 공격 패턴별로 균형있게 선택
            var groupedByPattern = aiCharacterPool.GroupBy(c => GetPatternName(c.attackRange));
            
            foreach (var group in groupedByPattern)
            {
                var charactersFromGroup = group.OrderByDescending(c => c.attackPower).Take(2).ToList();
                selectedCharacters.AddRange(charactersFromGroup);
                
                if (selectedCharacters.Count >= MAX_CHARACTERS)
                    break;
            }
            
            // 부족한 경우 추가
            if (selectedCharacters.Count < MAX_CHARACTERS)
            {
                var remaining = aiCharacterPool.Except(selectedCharacters)
                    .OrderByDescending(c => c.attackPower)
                    .Take(MAX_CHARACTERS - selectedCharacters.Count);
                selectedCharacters.AddRange(remaining);
            }
            
            selectedCharacters = selectedCharacters.Take(MAX_CHARACTERS).ToList();
        }

        /// <summary>
        /// 전략적 캐릭터 선택 (Hard/Expert)
        /// </summary>
        void SelectCharactersStrategic()
        {
            // 공격력과 범위를 고려한 전략적 선택
            var scoredCharacters = aiCharacterPool.Select(c => new {
                Character = c,
                Score = CalculateCharacterScore(c)
            }).OrderByDescending(x => x.Score);
            
            selectedCharacters = scoredCharacters
                .Take(MAX_CHARACTERS)
                .Select(x => x.Character)
                .ToList();
        }

        /// <summary>
        /// 캐릭터 점수 계산 (공격력 + 범위 효율성)
        /// </summary>
        float CalculateCharacterScore(Character character)
        {
            float attackScore = character.attackPower;
            float rangeScore = character.attackRange.Count * 2f;
            float uniqueScore = GetPatternUniqueScore(character.attackRange);
            
            return attackScore + rangeScore + uniqueScore;
        }

        /// <summary>
        /// AI의 턴 - 캐릭터 배치
        /// </summary>
        public IEnumerator<Vector2Int> MakeMove(Character characterToPlace, TileInfo[,] currentTileA, TileInfo[,] currentTileB)
        {
            // 현재 타일 상태 업데이트
            tileA = currentTileA;
            tileB = currentTileB;
            
            // AI 고민 시간
            yield return null;
            System.Threading.Thread.Sleep((int)(thinkingTime * 1000));
            
            // 최적 위치 계산
            var bestPosition = CalculateBestPosition(characterToPlace);
            
            // 캐릭터 배치
            PlaceCharacter(characterToPlace, bestPosition);
            
            yield return bestPosition;
        }

        /// <summary>
        /// 최적 배치 위치 계산
        /// </summary>
        Vector2Int CalculateBestPosition(Character character)
        {
            Vector2Int bestPosition = new Vector2Int(-1, -1);
            float bestScore = float.MinValue;
            
            // 모든 빈 타일 검사
            CheckTilePositions(tileA, 0, character, ref bestPosition, ref bestScore);
            CheckTilePositions(tileB, 1, character, ref bestPosition, ref bestScore);
            
            return bestPosition;
        }

        /// <summary>
        /// 타일별 위치 검사
        /// </summary>
        void CheckTilePositions(TileInfo[,] tile, int tileIndex, Character character, 
            ref Vector2Int bestPosition, ref float bestScore)
        {
            for (int x = 0; x < TILE_WIDTH; x++)
            {
                for (int y = 0; y < TILE_HEIGHT; y++)
                {
                    if (tile[x, y].occupant == null)
                    {
                        float score = EvaluatePosition(character, tileIndex, new Vector2Int(x, y));
                        
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestPosition = new Vector2Int(x + (tileIndex * 100), y); // tileIndex 인코딩
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 위치 평가
        /// </summary>
        float EvaluatePosition(Character character, int tileIndex, Vector2Int position)
        {
            float score = 0f;
            
            switch (aiLevel)
            {
                case AILevel.Easy:
                    score = Random.Range(0f, 100f);
                    break;
                    
                case AILevel.Normal:
                    score = EvaluatePositionBasic(character, tileIndex, position);
                    break;
                    
                case AILevel.Hard:
                case AILevel.Expert:
                    score = EvaluatePositionAdvanced(character, tileIndex, position);
                    break;
            }
            
            return score;
        }

        /// <summary>
        /// 기본 위치 평가 (Normal)
        /// </summary>
        float EvaluatePositionBasic(Character character, int tileIndex, Vector2Int position)
        {
            float score = 0f;
            var tile = (tileIndex == 0) ? tileA : tileB;
            
            // 공격 가능한 적 수
            int enemiesInRange = CountEnemiesInRange(character, tile, position);
            score += enemiesInRange * 20f;
            
            // 아군 지원
            int alliesNearby = CountAlliesNearby(tile, position);
            score += alliesNearby * 10f;
            
            // 중앙 위치 선호
            float centerBonus = 5f - Mathf.Abs(position.x - TILE_WIDTH / 2f);
            score += centerBonus * 5f;
            
            return score;
        }

        /// <summary>
        /// 고급 위치 평가 (Hard/Expert)
        /// </summary>
        float EvaluatePositionAdvanced(Character character, int tileIndex, Vector2Int position)
        {
            float score = EvaluatePositionBasic(character, tileIndex, position);
            
            // 타일 제어 점수
            float controlScore = CalculateTileControlScore(tileIndex);
            score += controlScore * 30f;
            
            // 적 공격 범위 회피
            float dangerScore = CalculateDangerScore(tileIndex, position);
            score -= dangerScore * 15f;
            
            // 전략적 위치 (코너, 엣지)
            if (IsStrategicPosition(position))
            {
                score += 25f;
            }
            
            // 상대방 다음 수 예측
            if (aiLevel == AILevel.Expert)
            {
                score += PredictOpponentMove(character, tileIndex, position);
            }
            
            return score;
        }

        /// <summary>
        /// 공격 범위 내 적 수 계산
        /// </summary>
        int CountEnemiesInRange(Character character, TileInfo[,] tile, Vector2Int position)
        {
            int count = 0;
            
            foreach (var offset in character.attackRange)
            {
                Vector2Int targetPos = position + offset;
                
                if (IsValidPosition(targetPos))
                {
                    var target = tile[targetPos.x, targetPos.y].occupant;
                    if (target != null && target.team != character.team && target.isAlive)
                    {
                        count++;
                    }
                }
            }
            
            return count;
        }

        /// <summary>
        /// 주변 아군 수 계산
        /// </summary>
        int CountAlliesNearby(TileInfo[,] tile, Vector2Int position)
        {
            int count = 0;
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    Vector2Int checkPos = position + new Vector2Int(dx, dy);
                    if (IsValidPosition(checkPos))
                    {
                        var ally = tile[checkPos.x, checkPos.y].occupant;
                        if (ally != null && ally.team == 1 && ally.isAlive)
                        {
                            count++;
                        }
                    }
                }
            }
            
            return count;
        }

        /// <summary>
        /// 타일 제어 점수 계산
        /// </summary>
        float CalculateTileControlScore(int tileIndex)
        {
            var tile = (tileIndex == 0) ? tileA : tileB;
            int aiCount = 0;
            int playerCount = 0;
            
            for (int x = 0; x < TILE_WIDTH; x++)
            {
                for (int y = 0; y < TILE_HEIGHT; y++)
                {
                    var occupant = tile[x, y].occupant;
                    if (occupant != null && occupant.isAlive)
                    {
                        if (occupant.team == 1) aiCount++;
                        else playerCount++;
                    }
                }
            }
            
            // 타일을 거의 점령했거나 뒤지고 있을 때 보너스
            float controlDiff = aiCount - playerCount;
            if (controlDiff > 0) return controlDiff * 2f;
            else if (controlDiff < -2) return -controlDiff * 3f; // 뒤지고 있으면 더 집중
            
            return 0f;
        }

        /// <summary>
        /// 위험도 계산
        /// </summary>
        float CalculateDangerScore(int tileIndex, Vector2Int position)
        {
            float dangerScore = 0f;
            var tile = (tileIndex == 0) ? tileA : tileB;
            
            // 모든 적 캐릭터의 공격 범위 확인
            for (int x = 0; x < TILE_WIDTH; x++)
            {
                for (int y = 0; y < TILE_HEIGHT; y++)
                {
                    var enemy = tile[x, y].occupant;
                    if (enemy != null && enemy.team == 0 && enemy.isAlive)
                    {
                        foreach (var offset in enemy.attackRange)
                        {
                            if (new Vector2Int(x, y) + offset == position)
                            {
                                dangerScore += enemy.attackPower;
                                break;
                            }
                        }
                    }
                }
            }
            
            return dangerScore;
        }

        /// <summary>
        /// 전략적 위치 확인
        /// </summary>
        bool IsStrategicPosition(Vector2Int position)
        {
            // 코너
            if ((position.x == 0 || position.x == TILE_WIDTH - 1) && 
                (position.y == 0 || position.y == TILE_HEIGHT - 1))
                return true;
            
            // 중앙
            if (position.x == TILE_WIDTH / 2 && position.y == TILE_HEIGHT / 2)
                return true;
            
            return false;
        }

        /// <summary>
        /// 상대방 다음 수 예측 (Expert)
        /// </summary>
        float PredictOpponentMove(Character character, int tileIndex, Vector2Int position)
        {
            // 상대방이 다음에 놓을 가능성이 높은 위치 예측
            // 간단한 휴리스틱: 상대방도 최적 위치를 선택할 것으로 가정
            
            float predictScore = 0f;
            
            // 이 위치가 상대방의 주요 타겟이 될 가능성
            if (CountAlliesNearby((tileIndex == 0) ? tileA : tileB, position) > 2)
            {
                predictScore -= 10f; // 집중 공격 대상이 될 수 있음
            }
            
            // 상대방의 캐릭터 배치 패턴 분석
            // (실제 구현시 더 복잡한 패턴 인식 가능)
            
            return predictScore;
        }

        /// <summary>
        /// 캐릭터 배치
        /// </summary>
        void PlaceCharacter(Character character, Vector2Int encodedPosition)
        {
            int tileIndex = encodedPosition.x >= 100 ? 1 : 0;
            Vector2Int actualPosition = new Vector2Int(encodedPosition.x % 100, encodedPosition.y);
            
            var tile = (tileIndex == 0) ? tileA : tileB;
            tile[actualPosition.x, actualPosition.y].occupant = character;
            character.team = 1; // AI 팀
            
            deployedCharacters.Add(character);
            
            Debug.Log($"AI가 {character.name}을(를) {(tileIndex == 0 ? "A" : "B")}타일 ({actualPosition.x}, {actualPosition.y})에 배치했습니다.");
        }

        /// <summary>
        /// 유효한 위치인지 확인
        /// </summary>
        bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < TILE_WIDTH && 
                   position.y >= 0 && position.y < TILE_HEIGHT;
        }

        /// <summary>
        /// 패턴 이름 가져오기
        /// </summary>
        string GetPatternName(List<Vector2Int> pattern)
        {
            foreach (var kvp in attackPatterns)
            {
                if (kvp.Value.SequenceEqual(pattern))
                    return kvp.Key;
            }
            return "custom";
        }

        /// <summary>
        /// 패턴 고유 점수
        /// </summary>
        float GetPatternUniqueScore(List<Vector2Int> pattern)
        {
            string patternName = GetPatternName(pattern);
            
            switch (patternName)
            {
                case "surrounding": return 15f;
                case "line3": return 12f;
                case "lShape": return 10f;
                case "cross": return 8f;
                case "diagonal": return 8f;
                default: return 5f;
            }
        }

        /// <summary>
        /// AI 상태 리셋
        /// </summary>
        public void ResetAI()
        {
            selectedCharacters.Clear();
            deployedCharacters.Clear();
            InitializeTiles();
        }

        /// <summary>
        /// 현재 게임 상태 평가 (디버그용)
        /// </summary>
        public void EvaluateGameState()
        {
            int aiScoreA = 0, playerScoreA = 0;
            int aiScoreB = 0, playerScoreB = 0;
            
            // A타일 평가
            for (int x = 0; x < TILE_WIDTH; x++)
            {
                for (int y = 0; y < TILE_HEIGHT; y++)
                {
                    var occupant = tileA[x, y].occupant;
                    if (occupant != null && occupant.isAlive)
                    {
                        if (occupant.team == 1) aiScoreA++;
                        else playerScoreA++;
                    }
                }
            }
            
            // B타일 평가
            for (int x = 0; x < TILE_WIDTH; x++)
            {
                for (int y = 0; y < TILE_HEIGHT; y++)
                {
                    var occupant = tileB[x, y].occupant;
                    if (occupant != null && occupant.isAlive)
                    {
                        if (occupant.team == 1) aiScoreB++;
                        else playerScoreB++;
                    }
                }
            }
            
            Debug.Log($"현재 상태 - A타일: AI {aiScoreA} vs Player {playerScoreA}, B타일: AI {aiScoreB} vs Player {playerScoreB}");
            
            int aiTotalScore = (aiScoreA > playerScoreA ? 1 : 0) + (aiScoreB > playerScoreB ? 1 : 0);
            int playerTotalScore = (playerScoreA > aiScoreA ? 1 : 0) + (playerScoreB > aiScoreB ? 1 : 0);
            
            Debug.Log($"점수: AI {aiTotalScore} vs Player {playerTotalScore}");
        }
    }
}