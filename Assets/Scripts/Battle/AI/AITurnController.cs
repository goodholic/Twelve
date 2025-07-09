using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace GuildMaster.Battle
{
    /// <summary>
    /// AI의 턴 제어와 전략적 결정을 담당하는 컨트롤러
    /// </summary>
    public class AITurnController : MonoBehaviour
    {
        [Header("AI 설정")]
        [SerializeField] private float thinkingTime = 1f; // AI 사고 시간
        [SerializeField] private float moveAnimationTime = 0.5f; // 이동 애니메이션 시간
        [SerializeField] private AIDifficulty difficulty = AIDifficulty.Normal;
        
        [Header("AI 전략 가중치")]
        [SerializeField] private float aggressiveWeight = 1f;    // 공격적 전략
        [SerializeField] private float defensiveWeight = 0.5f;   // 방어적 전략
        [SerializeField] private float balancedWeight = 0.8f;    // 균형 전략
        
        // AI가 관리하는 캐릭터들
        private List<CharacterUnit> aiCharacters = new List<CharacterUnit>();
        private Queue<CharacterUnit> characterQueue = new Queue<CharacterUnit>();
        
        // 현재 턴 상태
        private bool isAITurn = false;
        private Coroutine aiTurnCoroutine;
        
        // 이벤트
        public System.Action<CharacterUnit, Tile> OnAIMove;
        public System.Action OnAITurnStart;
        public System.Action OnAITurnEnd;
        
        /// <summary>
        /// AI 난이도
        /// </summary>
        public enum AIDifficulty
        {
            Easy,       // 쉬움 - 랜덤한 결정
            Normal,     // 보통 - 기본적인 전략
            Hard,       // 어려움 - 최적화된 전략
            Expert      // 전문가 - 예측과 카운터 전략
        }
        
        void Start()
        {
            // AI 캐릭터 초기화는 BattleManager에서 처리
        }
        
        /// <summary>
        /// AI 캐릭터 등록
        /// </summary>
        public void RegisterAICharacter(CharacterUnit character)
        {
            if (character != null && character.Team == Tile.Team.Enemy)
            {
                aiCharacters.Add(character);
            }
        }
        
        /// <summary>
        /// AI 턴 시작
        /// </summary>
        public void StartAITurn(List<CharacterUnit> charactersToPlace)
        {
            if (isAITurn) return;
            
            isAITurn = true;
            characterQueue.Clear();
            
            // 배치할 캐릭터들을 큐에 추가
            foreach (var character in charactersToPlace)
            {
                if (character != null && !character.IsPlaced)
                {
                    characterQueue.Enqueue(character);
                }
            }
            
            OnAITurnStart?.Invoke();
            
            if (aiTurnCoroutine != null)
            {
                StopCoroutine(aiTurnCoroutine);
            }
            
            aiTurnCoroutine = StartCoroutine(ProcessAITurn());
        }
        
        /// <summary>
        /// AI 턴 처리
        /// </summary>
        IEnumerator ProcessAITurn()
        {
            yield return new WaitForSeconds(thinkingTime * 0.5f);
            
            while (characterQueue.Count > 0)
            {
                CharacterUnit character = characterQueue.Dequeue();
                
                if (character == null || character.IsPlaced) continue;
                
                // AI 사고 시간
                yield return new WaitForSeconds(thinkingTime);
                
                // 최적의 타일 선택
                Tile bestTile = SelectBestTile(character);
                
                if (bestTile != null)
                {
                    // 캐릭터 배치
                    bool placed = TileGridManager.Instance.PlaceCharacter(character, bestTile);
                    
                    if (placed)
                    {
                        OnAIMove?.Invoke(character, bestTile);
                        
                        // 배치 애니메이션 대기
                        yield return new WaitForSeconds(moveAnimationTime);
                    }
                }
            }
            
            yield return new WaitForSeconds(thinkingTime * 0.5f);
            
            EndAITurn();
        }
        
        /// <summary>
        /// 최적의 타일 선택
        /// </summary>
        Tile SelectBestTile(CharacterUnit character)
        {
            List<Tile> availableTiles = GetAvailableTiles();
            
            if (availableTiles.Count == 0) return null;
            
            Tile bestTile = null;
            float bestScore = float.MinValue;
            
            foreach (Tile tile in availableTiles)
            {
                float score = EvaluateTile(character, tile);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTile = tile;
                }
            }
            
            // 난이도에 따른 변동성 추가
            if (difficulty == AIDifficulty.Easy && Random.value < 0.3f)
            {
                // 30% 확률로 랜덤 선택
                return availableTiles[Random.Range(0, availableTiles.Count)];
            }
            
            return bestTile;
        }
        
        /// <summary>
        /// 사용 가능한 타일 목록 가져오기
        /// </summary>
        List<Tile> GetAvailableTiles()
        {
            List<Tile> availableTiles = new List<Tile>();
            
            // A 타일 확인
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Tile tile = TileGridManager.Instance.GetTile(Tile.TileType.A, x, y);
                    if (tile != null && tile.CanPlaceUnit())
                    {
                        availableTiles.Add(tile);
                    }
                }
            }
            
            // B 타일 확인
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Tile tile = TileGridManager.Instance.GetTile(Tile.TileType.B, x, y);
                    if (tile != null && tile.CanPlaceUnit())
                    {
                        availableTiles.Add(tile);
                    }
                }
            }
            
            return availableTiles;
        }
        
        /// <summary>
        /// 타일 평가
        /// </summary>
        float EvaluateTile(CharacterUnit character, Tile tile)
        {
            float score = 0f;
            
            // 기본 점수 (타일 타입에 따라)
            score += tile.tileType == Tile.TileType.A ? 50f : 50f; // A와 B 동등하게 평가
            
            // 현재 타일의 아군/적군 비율 확인
            int allyCount = TileGridManager.Instance.GetCharacterCount(tile.tileType, Tile.Team.Ally);
            int enemyCount = TileGridManager.Instance.GetCharacterCount(tile.tileType, Tile.Team.Enemy);
            
            // 전략적 점수 계산
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    // 단순히 빈 곳 선호
                    score += Random.Range(0f, 100f);
                    break;
                    
                case AIDifficulty.Normal:
                    score += CalculateNormalStrategy(character, tile, allyCount, enemyCount);
                    break;
                    
                case AIDifficulty.Hard:
                    score += CalculateHardStrategy(character, tile, allyCount, enemyCount);
                    break;
                    
                case AIDifficulty.Expert:
                    score += CalculateExpertStrategy(character, tile, allyCount, enemyCount);
                    break;
            }
            
            // 공격 범위 점수
            score += EvaluateAttackRange(character, tile);
            
            // 시너지 점수
            score += EvaluateSynergy(character, tile);
            
            return score;
        }
        
        /// <summary>
        /// 보통 난이도 전략
        /// </summary>
        float CalculateNormalStrategy(CharacterUnit character, Tile tile, int allyCount, int enemyCount)
        {
            float score = 0f;
            
            // 균형잡힌 배치 선호
            if (enemyCount <= allyCount)
            {
                score += 30f * balancedWeight; // 열세인 타일에 배치
            }
            
            // 이미 우세한 타일 강화
            if (enemyCount > allyCount)
            {
                score += 20f * aggressiveWeight;
            }
            
            return score;
        }
        
        /// <summary>
        /// 어려움 난이도 전략
        /// </summary>
        float CalculateHardStrategy(CharacterUnit character, Tile tile, int allyCount, int enemyCount)
        {
            float score = 0f;
            
            // 승리에 필요한 타일 우선
            GameResult currentResult = TileGridManager.Instance.CheckVictoryCondition();
            
            if (currentResult == GameResult.Defeat)
            {
                // 패배 중이면 열세인 타일 우선
                if (enemyCount < allyCount)
                {
                    score += 50f * defensiveWeight;
                }
            }
            else if (currentResult == GameResult.Draw)
            {
                // 무승부면 한쪽을 확실히 점령
                if (enemyCount >= allyCount - 1)
                {
                    score += 40f * aggressiveWeight;
                }
            }
            
            // 캐릭터 역할에 따른 배치
            switch (character.jobClass)
            {
                case JobClass.Knight:
                case JobClass.Warrior:
                    // 탱커는 전방 배치
                    if (tile.x < 3) score += 20f;
                    break;
                    
                case JobClass.Ranger:
                case JobClass.Mage:
                    // 원거리 딜러는 후방 배치
                    if (tile.x >= 3) score += 20f;
                    break;
            }
            
            return score;
        }
        
        /// <summary>
        /// 전문가 난이도 전략
        /// </summary>
        float CalculateExpertStrategy(CharacterUnit character, Tile tile, int allyCount, int enemyCount)
        {
            float score = CalculateHardStrategy(character, tile, allyCount, enemyCount);
            
            // 다음 턴 예측
            int remainingAIUnits = characterQueue.Count;
            int potentialAllyUnits = CountPotentialAllyUnits();
            
            // 상대가 다음에 배치할 수 있는 유닛 수를 고려
            if (remainingAIUnits > potentialAllyUnits)
            {
                // AI가 유리하면 공격적으로
                score += 30f * aggressiveWeight;
            }
            else
            {
                // 불리하면 방어적으로
                score += 30f * defensiveWeight;
            }
            
            // 카운터 전략
            List<CharacterUnit> nearbyEnemies = GetNearbyEnemies(tile);
            foreach (var enemy in nearbyEnemies)
            {
                if (IsCounterTo(character.jobClass, enemy.jobClass))
                {
                    score += 25f;
                }
            }
            
            return score;
        }
        
        /// <summary>
        /// 공격 범위 평가
        /// </summary>
        float EvaluateAttackRange(CharacterUnit character, Tile tile)
        {
            float score = 0f;
            
            // 임시로 배치하여 공격 범위 확인
            character.PlaceOnTile(tile);
            List<Tile> attackRange = TileGridManager.Instance.GetAttackRangeTiles(character);
            character.CurrentTile = null; // 원복
            
            foreach (Tile rangeTile in attackRange)
            {
                if (rangeTile.isOccupied)
                {
                    if (rangeTile.occupiedTeam == Tile.Team.Ally)
                    {
                        score += 10f; // 적을 공격할 수 있는 위치
                    }
                }
            }
            
            return score;
        }
        
        /// <summary>
        /// 시너지 평가
        /// </summary>
        float EvaluateSynergy(CharacterUnit character, Tile tile)
        {
            float score = 0f;
            
            // 같은 직업군 근처 배치 시 시너지
            List<CharacterUnit> nearbyAllies = GetNearbyAllies(tile);
            
            foreach (var ally in nearbyAllies)
            {
                if (ally.jobClass == character.jobClass)
                {
                    score += 15f; // 같은 직업 시너지
                }
                
                // 보완 관계 시너지
                if (IsComplementary(character.jobClass, ally.jobClass))
                {
                    score += 10f;
                }
            }
            
            return score;
        }
        
        /// <summary>
        /// 직업 상성 확인
        /// </summary>
        bool IsCounterTo(JobClass attacker, JobClass defender)
        {
            // 간단한 상성 관계
            switch (attacker)
            {
                case JobClass.Assassin:
                    return defender == JobClass.Mage || defender == JobClass.Priest;
                case JobClass.Warrior:
                    return defender == JobClass.Assassin;
                case JobClass.Knight:
                    return defender == JobClass.Warrior;
                case JobClass.Mage:
                    return defender == JobClass.Knight;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// 직업 보완 관계
        /// </summary>
        bool IsComplementary(JobClass job1, JobClass job2)
        {
            // 탱커 + 힐러
            if ((job1 == JobClass.Knight && job2 == JobClass.Priest) ||
                (job1 == JobClass.Priest && job2 == JobClass.Knight))
                return true;
                
            // 근거리 + 원거리
            if ((IsRanged(job1) && !IsRanged(job2)) ||
                (!IsRanged(job1) && IsRanged(job2)))
                return true;
                
            return false;
        }
        
        /// <summary>
        /// 원거리 직업 확인
        /// </summary>
        bool IsRanged(JobClass job)
        {
            return job == JobClass.Ranger || job == JobClass.Mage || job == JobClass.Sage;
        }
        
        /// <summary>
        /// 주변 적 유닛 가져오기
        /// </summary>
        List<CharacterUnit> GetNearbyEnemies(Tile centerTile)
        {
            List<CharacterUnit> enemies = new List<CharacterUnit>();
            
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    Tile tile = TileGridManager.Instance.GetTile(centerTile.tileType, 
                        centerTile.x + dx, centerTile.y + dy);
                        
                    if (tile != null && tile.isOccupied && tile.occupiedTeam == Tile.Team.Ally)
                    {
                        CharacterUnit unit = tile.occupiedUnit?.GetComponent<CharacterUnit>();
                        if (unit != null)
                        {
                            enemies.Add(unit);
                        }
                    }
                }
            }
            
            return enemies;
        }
        
        /// <summary>
        /// 주변 아군 유닛 가져오기
        /// </summary>
        List<CharacterUnit> GetNearbyAllies(Tile centerTile)
        {
            List<CharacterUnit> allies = new List<CharacterUnit>();
            
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    Tile tile = TileGridManager.Instance.GetTile(centerTile.tileType, 
                        centerTile.x + dx, centerTile.y + dy);
                        
                    if (tile != null && tile.isOccupied && tile.occupiedTeam == Tile.Team.Enemy)
                    {
                        CharacterUnit unit = tile.occupiedUnit?.GetComponent<CharacterUnit>();
                        if (unit != null)
                        {
                            allies.Add(unit);
                        }
                    }
                }
            }
            
            return allies;
        }
        
        /// <summary>
        /// 잠재적 아군 유닛 수 계산
        /// </summary>
        int CountPotentialAllyUnits()
        {
            // 실제 구현에서는 플레이어가 배치 가능한 유닛 수를 계산
            // 여기서는 간단히 추정값 반환
            return 5;
        }
        
        /// <summary>
        /// AI 턴 종료
        /// </summary>
        void EndAITurn()
        {
            isAITurn = false;
            OnAITurnEnd?.Invoke();
        }
        
        /// <summary>
        /// AI 난이도 설정
        /// </summary>
        public void SetDifficulty(AIDifficulty newDifficulty)
        {
            difficulty = newDifficulty;
            
            // 난이도에 따른 파라미터 조정
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    thinkingTime = 0.5f;
                    aggressiveWeight = 0.5f;
                    defensiveWeight = 0.5f;
                    balancedWeight = 1f;
                    break;
                    
                case AIDifficulty.Normal:
                    thinkingTime = 1f;
                    aggressiveWeight = 1f;
                    defensiveWeight = 0.5f;
                    balancedWeight = 0.8f;
                    break;
                    
                case AIDifficulty.Hard:
                    thinkingTime = 1.5f;
                    aggressiveWeight = 1.2f;
                    defensiveWeight = 0.8f;
                    balancedWeight = 1f;
                    break;
                    
                case AIDifficulty.Expert:
                    thinkingTime = 2f;
                    aggressiveWeight = 1.5f;
                    defensiveWeight = 1f;
                    balancedWeight = 1.2f;
                    break;
            }
        }
    }
}