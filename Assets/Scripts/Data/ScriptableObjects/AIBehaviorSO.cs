using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace TileConquest.Data
{
    /// <summary>
    /// AI 행동 패턴을 정의하는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "AIBehavior", menuName = "TileConquest/AI Behavior", order = 4)]
    public class AIBehaviorSO : ScriptableObject
    {
        [Header("AI 정보")]
        public string behaviorName;
        public AIDifficulty difficulty;
        
        [Header("캐릭터 선택 전략")]
        [Tooltip("캐릭터 선택 시 우선순위")]
        public CharacterSelectionStrategy selectionStrategy;
        
        [Tooltip("선호하는 직업 조합")]
        public List<JobClass> preferredJobClasses = new List<JobClass>();
        
        [Tooltip("직업별 선택 비중")]
        public List<JobWeight> jobWeights = new List<JobWeight>();
        
        [Header("배치 전략")]
        [Tooltip("배치 우선순위 전략")]
        public PlacementStrategy placementStrategy;
        
        [Tooltip("타일 선호도 (A:B 비율)")]
        [Range(0f, 1f)]
        public float tileAPreference = 0.5f;
        
        [Tooltip("위치 평가 가중치")]
        public PositionWeights positionWeights;
        
        [Header("전술적 행동")]
        [Tooltip("공격적 성향 (0 = 방어적, 1 = 공격적)")]
        [Range(0f, 1f)]
        public float aggressiveness = 0.5f;
        
        [Tooltip("집중 공격 성향")]
        public bool focusFire = true;
        
        [Tooltip("카운터 플레이 활성화")]
        public bool counterPlay = true;
        
        [Header("의사결정 매개변수")]
        [Tooltip("미래 예측 턴 수")]
        [Range(0, 3)]
        public int lookAheadTurns = 1;
        
        [Tooltip("랜덤성 (0 = 완전 결정적, 1 = 완전 랜덤)")]
        [Range(0f, 0.5f)]
        public float randomness = 0.1f;
        
        /// <summary>
        /// AI가 선택할 캐릭터 목록 생성
        /// </summary>
        public List<CharacterTileDataSO> SelectCharacters(List<CharacterTileDataSO> availableCharacters, int maxCount)
        {
            List<CharacterTileDataSO> selected = new List<CharacterTileDataSO>();
            
            switch (selectionStrategy)
            {
                case CharacterSelectionStrategy.Balanced:
                    selected = SelectBalancedTeam(availableCharacters, maxCount);
                    break;
                    
                case CharacterSelectionStrategy.Aggressive:
                    selected = SelectAggressiveTeam(availableCharacters, maxCount);
                    break;
                    
                case CharacterSelectionStrategy.Defensive:
                    selected = SelectDefensiveTeam(availableCharacters, maxCount);
                    break;
                    
                case CharacterSelectionStrategy.Synergy:
                    selected = SelectSynergyTeam(availableCharacters, maxCount);
                    break;
                    
                case CharacterSelectionStrategy.Counter:
                    selected = SelectCounterTeam(availableCharacters, maxCount);
                    break;
                    
                case CharacterSelectionStrategy.Random:
                    selected = availableCharacters.OrderBy(x => Random.value).Take(maxCount).ToList();
                    break;
            }
            
            return selected;
        }
        
        /// <summary>
        /// 최적의 배치 위치 계산
        /// </summary>
        public Vector2Int CalculateBestPosition(TileBoard board, CharacterTileDataSO character, bool isTopBoard)
        {
            List<Vector2Int> availablePositions = board.GetEmptyPositions();
            if (availablePositions.Count == 0) return new Vector2Int(-1, -1);
            
            float bestScore = float.MinValue;
            Vector2Int bestPosition = availablePositions[0];
            
            foreach (var pos in availablePositions)
            {
                float score = EvaluatePosition(board, pos, character, isTopBoard);
                
                // 랜덤성 추가
                score += Random.Range(-randomness, randomness) * 100f;
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = pos;
                }
            }
            
            return bestPosition;
        }
        
        /// <summary>
        /// 위치 평가
        /// </summary>
        private float EvaluatePosition(TileBoard board, Vector2Int position, CharacterTileDataSO character, bool isTopBoard)
        {
            float score = 0f;
            
            // 공격 가능한 적 수
            int attackableEnemies = CountAttackableEnemies(board, position, character);
            score += attackableEnemies * positionWeights.attackWeight;
            
            // 방어 위치 평가
            int protectedAllies = CountProtectedAllies(board, position);
            score += protectedAllies * positionWeights.defenseWeight;
            
            // 영역 제어
            int controlledTiles = CountControlledTiles(board, position, character);
            score += controlledTiles * positionWeights.controlWeight;
            
            // 시너지 효과
            int adjacentAllies = CountAdjacentAllies(board, position);
            score += adjacentAllies * positionWeights.synergyWeight;
            
            // 위치별 보너스
            score += GetPositionBonus(position, board.Width, board.Height, character.aiPreferredPosition);
            
            return score;
        }
        
        // 균형잡힌 팀 선택
        private List<CharacterTileDataSO> SelectBalancedTeam(List<CharacterTileDataSO> available, int maxCount)
        {
            var result = new List<CharacterTileDataSO>();
            var jobCounts = new Dictionary<JobClass, int>();
            
            // 각 직업별로 균등하게 선택
            while (result.Count < maxCount && available.Count > result.Count)
            {
                var leastRepresentedJob = GetLeastRepresentedJob(jobCounts);
                var candidate = available
                    .Where(c => c.baseCharacterData.jobClass == leastRepresentedJob && !result.Contains(c))
                    .FirstOrDefault();
                    
                if (candidate == null)
                {
                    candidate = available.Where(c => !result.Contains(c)).FirstOrDefault();
                }
                
                if (candidate != null)
                {
                    result.Add(candidate);
                    if (!jobCounts.ContainsKey(candidate.baseCharacterData.jobClass))
                        jobCounts[candidate.baseCharacterData.jobClass] = 0;
                    jobCounts[candidate.baseCharacterData.jobClass]++;
                }
            }
            
            return result;
        }
        
        // 공격적인 팀 선택
        private List<CharacterTileDataSO> SelectAggressiveTeam(List<CharacterTileDataSO> available, int maxCount)
        {
            return available
                .OrderByDescending(c => c.baseCharacterData.baseAttack * c.attackMultiplier)
                .ThenByDescending(c => c.attackRange?.rangeSize ?? 0)
                .Take(maxCount)
                .ToList();
        }
        
        // 방어적인 팀 선택
        private List<CharacterTileDataSO> SelectDefensiveTeam(List<CharacterTileDataSO> available, int maxCount)
        {
            return available
                .OrderByDescending(c => c.baseCharacterData.baseHP + c.baseCharacterData.baseDefense)
                .Take(maxCount)
                .ToList();
        }
        
        // 시너지 중심 팀 선택
        private List<CharacterTileDataSO> SelectSynergyTeam(List<CharacterTileDataSO> available, int maxCount)
        {
            var result = new List<CharacterTileDataSO>();
            
            // 시너지가 있는 직업 그룹 우선 선택
            var synergyGroups = available
                .Where(c => c.hasJobSynergy)
                .GroupBy(c => c.baseCharacterData.jobClass)
                .OrderByDescending(g => g.Count())
                .ToList();
                
            foreach (var group in synergyGroups)
            {
                result.AddRange(group.Take(maxCount - result.Count));
                if (result.Count >= maxCount) break;
            }
            
            // 나머지 채우기
            if (result.Count < maxCount)
            {
                result.AddRange(available.Where(c => !result.Contains(c)).Take(maxCount - result.Count));
            }
            
            return result.Take(maxCount).ToList();
        }
        
        // 카운터 팀 선택 (플레이어의 선택에 대응)
        private List<CharacterTileDataSO> SelectCounterTeam(List<CharacterTileDataSO> available, int maxCount)
        {
            // 실제 구현에서는 플레이어의 선택을 분석하여 카운터 선택
            // 여기서는 간단히 방어적 선택으로 대체
            return SelectDefensiveTeam(available, maxCount);
        }
        
        // 헬퍼 메서드들
        private JobClass GetLeastRepresentedJob(Dictionary<JobClass, int> jobCounts)
        {
            var allJobs = System.Enum.GetValues(typeof(JobClass)).Cast<JobClass>();
            return allJobs.OrderBy(j => jobCounts.ContainsKey(j) ? jobCounts[j] : 0).First();
        }
        
        private int CountAttackableEnemies(TileBoard board, Vector2Int position, CharacterTileDataSO character)
        {
            // 실제 구현 필요
            return 0;
        }
        
        private int CountProtectedAllies(TileBoard board, Vector2Int position)
        {
            // 실제 구현 필요
            return 0;
        }
        
        private int CountControlledTiles(TileBoard board, Vector2Int position, CharacterTileDataSO character)
        {
            var attackPositions = character.GetAttackablePositions(position);
            return attackPositions.Count(pos => board.IsInBounds(pos));
        }
        
        private int CountAdjacentAllies(TileBoard board, Vector2Int position)
        {
            // 실제 구현 필요
            return 0;
        }
        
        private float GetPositionBonus(Vector2Int position, int width, int height, PreferredPosition preference)
        {
            switch (preference)
            {
                case PreferredPosition.Center:
                    float distFromCenter = Vector2.Distance(position, new Vector2(width / 2f, height / 2f));
                    return 10f - distFromCenter;
                    
                case PreferredPosition.Edge:
                    if (position.x == 0 || position.x == width - 1 || position.y == 0 || position.y == height - 1)
                        return 10f;
                    return 0f;
                    
                case PreferredPosition.Corner:
                    if ((position.x == 0 || position.x == width - 1) && (position.y == 0 || position.y == height - 1))
                        return 10f;
                    return 0f;
                    
                case PreferredPosition.Front:
                    return position.y * 2f;
                    
                case PreferredPosition.Back:
                    return (height - position.y) * 2f;
                    
                default:
                    return 0f;
            }
        }
    }
    
    /// <summary>
    /// 캐릭터 선택 전략
    /// </summary>
    public enum CharacterSelectionStrategy
    {
        Balanced,       // 균형잡힌 선택
        Aggressive,     // 공격 위주
        Defensive,      // 방어 위주
        Synergy,        // 시너지 중심
        Counter,        // 플레이어 카운터
        Random          // 랜덤
    }
    
    /// <summary>
    /// 배치 전략
    /// </summary>
    public enum PlacementStrategy
    {
        Spread,         // 분산 배치
        Concentrated,   // 집중 배치
        Frontline,      // 전선 구축
        Flanking,       // 측면 공략
        Adaptive        // 적응형
    }
    
    /// <summary>
    /// 직업별 가중치
    /// </summary>
    [System.Serializable]
    public class JobWeight
    {
        public JobClass jobClass;
        [Range(0f, 2f)]
        public float weight = 1f;
    }
    
    /// <summary>
    /// 위치 평가 가중치
    /// </summary>
    [System.Serializable]
    public class PositionWeights
    {
        [Range(0f, 100f)]
        public float attackWeight = 40f;
        
        [Range(0f, 100f)]
        public float defenseWeight = 20f;
        
        [Range(0f, 100f)]
        public float controlWeight = 25f;
        
        [Range(0f, 100f)]
        public float synergyWeight = 15f;
    }
    
    /// <summary>
    /// 타일 보드 (임시 클래스 - 실제 구현 시 교체)
    /// </summary>
    public class TileBoard
    {
        public int Width { get; set; }
        public int Height { get; set; }
        
        public List<Vector2Int> GetEmptyPositions()
        {
            // 실제 구현 필요
            return new List<Vector2Int>();
        }
        
        public bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
        }
    }
}