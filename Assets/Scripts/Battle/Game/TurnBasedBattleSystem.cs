using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using GuildMaster.TileBattle;

namespace GuildMaster.Battle
{
    public enum BattlePhase
    {
        CharacterSelection,
        Placement,
        Battle,
        Result
    }
    
    public enum TurnState
    {
        PlayerTurn,
        EnemyTurn,
        Processing
    }
    
    public class TurnBasedBattleSystem : MonoBehaviour
    {
        [Header("Battle Settings")]
        [SerializeField] private int maxCharactersPerPlayer = 10;
        [SerializeField] private float turnTimer = 30f;
        [SerializeField] private float aiThinkTime = 1.5f;
        
        [Header("System References")]
        [SerializeField] private TileBoardSystem tileBoardSystem;
        [SerializeField] private CharacterSelectUI characterSelectionUI;
        [SerializeField] private BattleUIManager battleUI;
        
        [Header("Current Battle State")]
        public BattlePhase currentPhase = BattlePhase.CharacterSelection;
        public TurnState currentTurn = TurnState.PlayerTurn;
        
        // 선택된 캐릭터들
        private List<Character> playerSelectedCharacters = new List<Character>();
        private List<Character> enemySelectedCharacters = new List<Character>();
        
        // 배치된 유닛들
        private List<CharacterUnit> playerUnits = new List<CharacterUnit>();
        private List<CharacterUnit> enemyUnits = new List<CharacterUnit>();
        
        // 현재 선택된 캐릭터/유닛
        private Character selectedCharacterToPlace;
        private CharacterUnit selectedUnit;
        private int currentPlacementIndex = 0;
        
        // 타이머
        private float currentTurnTime;
        
        private void Start()
        {
            InitializeBattle();
        }
        
        private void Update()
        {
            if (currentPhase == BattlePhase.Placement || currentPhase == BattlePhase.Battle)
            {
                UpdateTurnTimer();
            }
        }
        
        private void InitializeBattle()
        {
            // 캐릭터 선택 UI 표시
            if (characterSelectionUI != null)
            {
                characterSelectionUI.Show(OnCharacterSelectionComplete);
            }
            else
            {
                // 테스트용: 랜덤 캐릭터 선택
                AutoSelectCharacters();
            }
        }
        
        private void OnCharacterSelectionComplete(List<Character> selectedCharacters)
        {
            playerSelectedCharacters = selectedCharacters;
            
            // AI 캐릭터 자동 선택
            SelectAICharacters();
            
            // 배치 페이즈로 전환
            StartPlacementPhase();
        }
        
        private void AutoSelectCharacters()
        {
            // 테스트용: 사용 가능한 캐릭터 중 10개 랜덤 선택
            var allCharacters = DataManager.Instance.GetAllCharacters();
            
            // 플레이어 캐릭터 선택
            playerSelectedCharacters = allCharacters
                .OrderBy(x => Random.Range(0f, 1f))
                .Take(maxCharactersPerPlayer)
                .ToList();
            
            // AI 캐릭터 선택
            var remainingCharacters = allCharacters
                .Where(c => !playerSelectedCharacters.Contains(c))
                .ToList();
                
            enemySelectedCharacters = remainingCharacters
                .OrderBy(x => Random.Range(0f, 1f))
                .Take(maxCharactersPerPlayer)
                .ToList();
            
            StartPlacementPhase();
        }
        
        private void SelectAICharacters()
        {
            var allCharacters = DataManager.Instance.GetAllCharacters();
            var availableCharacters = allCharacters
                .Where(c => !playerSelectedCharacters.Any(p => p.characterID == c.characterID))
                .ToList();
            
            // AI는 균형잡힌 팀 구성을 시도
            Dictionary<JobClass, int> jobCounts = new Dictionary<JobClass, int>();
            
            while (enemySelectedCharacters.Count < maxCharactersPerPlayer && availableCharacters.Count > 0)
            {
                // 가장 적은 직업 우선 선택
                var leastRepresentedJob = System.Enum.GetValues(typeof(JobClass))
                    .Cast<JobClass>()
                    .OrderBy(j => jobCounts.GetValueOrDefault(j, 0))
                    .First();
                
                var candidate = availableCharacters
                    .Where(c => c.jobClass == leastRepresentedJob)
                    .OrderByDescending(c => c.rarity)
                    .FirstOrDefault();
                
                if (candidate == null)
                {
                    candidate = availableCharacters
                        .OrderByDescending(c => c.rarity)
                        .First();
                }
                
                enemySelectedCharacters.Add(candidate);
                availableCharacters.Remove(candidate);
                
                if (!jobCounts.ContainsKey(candidate.jobClass))
                    jobCounts[candidate.jobClass] = 0;
                jobCounts[candidate.jobClass]++;
            }
        }
        
        private void StartPlacementPhase()
        {
            currentPhase = BattlePhase.Placement;
            currentPlacementIndex = 0;
            currentTurn = TurnState.PlayerTurn;
            
            if (battleUI != null)
            {
                battleUI.ShowPlacementUI();
                battleUI.UpdateTurnIndicator("배치 페이즈 - 플레이어 턴");
            }
            
            StartPlayerPlacementTurn();
        }
        
        private void StartPlayerPlacementTurn()
        {
            if (currentPlacementIndex >= playerSelectedCharacters.Count)
            {
                // 모든 캐릭터 배치 완료
                StartBattlePhase();
                return;
            }
            
            currentTurn = TurnState.PlayerTurn;
            selectedCharacterToPlace = playerSelectedCharacters[currentPlacementIndex];
            currentTurnTime = turnTimer;
            
            // 배치 가능한 타일 하이라이트
            HighlightPlaceableTiles();
            
            if (battleUI != null)
            {
                battleUI.ShowCharacterToPlace(selectedCharacterToPlace);
                battleUI.UpdateTurnIndicator($"캐릭터 배치: {selectedCharacterToPlace.characterName}");
            }
        }
        
        private void StartEnemyPlacementTurn()
        {
            if (currentPlacementIndex >= enemySelectedCharacters.Count)
            {
                currentPlacementIndex = 0;
                StartPlayerPlacementTurn();
                return;
            }
            
            currentTurn = TurnState.EnemyTurn;
            StartCoroutine(AIPlaceCharacter());
        }
        
        private System.Collections.IEnumerator AIPlaceCharacter()
        {
            currentTurn = TurnState.Processing;
            
            yield return new WaitForSeconds(aiThinkTime);
            
            var characterToPlace = enemySelectedCharacters[currentPlacementIndex];
            
            // AI 배치 전략: 공격 범위를 고려한 최적 위치 선택
            Vector2Int bestPosition = FindBestPlacementPosition(characterToPlace);
            TileBoardSystem.Board selectedBoard = Random.Range(0f, 1f) > 0.5f ? 
                tileBoardSystem.boardA : tileBoardSystem.boardB;
            
            // 유닛 생성 및 배치
            CharacterUnit unit = CreateUnit(characterToPlace, false);
            if (tileBoardSystem.PlaceUnit(selectedBoard, bestPosition, unit))
            {
                enemyUnits.Add(unit);
                currentPlacementIndex++;
            }
            
            // 다음 턴
            if (currentPlacementIndex < enemySelectedCharacters.Count)
            {
                StartEnemyPlacementTurn();
            }
            else
            {
                currentPlacementIndex = 0;
                StartPlayerPlacementTurn();
            }
        }
        
        private Vector2Int FindBestPlacementPosition(Character character)
        {
            List<Vector2Int> availablePositions = new List<Vector2Int>();
            
            // 모든 빈 타일 찾기
            CheckBoardForEmptyTiles(tileBoardSystem.boardA, availablePositions);
            CheckBoardForEmptyTiles(tileBoardSystem.boardB, availablePositions);
            
            if (availablePositions.Count == 0)
                return Vector2Int.zero;
            
            // 직업별 선호 위치 계산
            Vector2Int bestPosition = availablePositions[0];
            float bestScore = 0;
            
            foreach (var pos in availablePositions)
            {
                float score = EvaluatePosition(character, pos);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = pos;
                }
            }
            
            return bestPosition;
        }
        
        private void CheckBoardForEmptyTiles(TileBoardSystem.Board board, List<Vector2Int> emptyTiles)
        {
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (!board.tiles[x, y].IsOccupied)
                    {
                        emptyTiles.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
        
        private float EvaluatePosition(Character character, Vector2Int position)
        {
            float score = 0;
            
            // 직업별 위치 선호도
            switch (character.jobClass)
            {
                case JobClass.Warrior:
                case JobClass.Knight:
                    // 전방 선호
                    score += (5 - position.x) * 2;
                    break;
                    
                case JobClass.Archer:
                case JobClass.Gunner:
                    // 후방 선호
                    score += position.x * 2;
                    break;
                    
                case JobClass.Wizard:
                case JobClass.Sage:
                    // 중앙 선호
                    score += (3 - Mathf.Abs(position.x - 2.5f)) * 2;
                    score += (1.5f - Mathf.Abs(position.y - 1)) * 1;
                    break;
                    
                case JobClass.Priest:
                    // 아군 근처 선호
                    foreach (var unit in enemyUnits)
                    {
                        float distance = Vector2Int.Distance(position, unit.currentTile);
                        if (distance <= 2)
                            score += 3;
                    }
                    break;
                    
                case JobClass.Rogue:
                    // 측면 선호
                    score += Mathf.Min(position.y, 2 - position.y) * 3;
                    break;
            }
            
            return score;
        }
        
        public void OnTileClicked(TileBoardSystem.Board board, TileBoardSystem.Tile tile)
        {
            if (currentTurn != TurnState.PlayerTurn) return;
            
            switch (currentPhase)
            {
                case BattlePhase.Placement:
                    HandlePlacementClick(board, tile);
                    break;
                    
                case BattlePhase.Battle:
                    HandleBattleClick(board, tile);
                    break;
            }
        }
        
        private void HandlePlacementClick(TileBoardSystem.Board board, TileBoardSystem.Tile tile)
        {
            if (selectedCharacterToPlace == null) return;
            if (tile.IsOccupied) return;
            
            // 유닛 생성 및 배치
            CharacterUnit unit = CreateUnit(selectedCharacterToPlace, true);
            if (tileBoardSystem.PlaceUnit(board, tile.position, unit))
            {
                playerUnits.Add(unit);
                currentPlacementIndex++;
                
                tileBoardSystem.ClearHighlights();
                
                if (currentPlacementIndex >= playerSelectedCharacters.Count)
                {
                    // 플레이어 배치 완료, AI 턴
                    currentPlacementIndex = 0;
                    StartEnemyPlacementTurn();
                }
                else
                {
                    StartPlayerPlacementTurn();
                }
            }
        }
        
        private void HandleBattleClick(TileBoardSystem.Board board, TileBoardSystem.Tile tile)
        {
            if (tile.IsOccupied && tile.occupyingUnit.Team == Tile.Team.Ally)
            {
                // 유닛 선택
                SelectUnit(tile.occupyingUnit);
            }
            else if (selectedUnit != null && !tile.IsOccupied)
            {
                // 공격 실행
                ExecuteAttack(board, tile.position);
            }
        }
        
        private CharacterUnit CreateUnit(Character character, bool isPlayer)
        {
            // 유닛 프리팹 로드 (실제로는 Resources나 Addressables 사용)
            GameObject unitPrefab = Resources.Load<GameObject>("Prefabs/Unit");
            if (unitPrefab == null)
            {
                // 테스트용 기본 유닛 생성
                unitPrefab = new GameObject("Unit");
                unitPrefab.AddComponent<SpriteRenderer>();
            }
            
            GameObject unitObj = Instantiate(unitPrefab);
            CharacterUnit unit = unitObj.GetComponent<CharacterUnit>();
            if (unit == null)
                unit = unitObj.AddComponent<CharacterUnit>();
            
            // 유닛 초기화
            // Note: Character needs to be converted to CharacterData
            // and team needs to be set based on isPlayer flag
            // This requires additional implementation to convert Character to CharacterData
            
            return unit;
        }
        
        private void HighlightPlaceableTiles()
        {
            List<Vector2Int> placeablePositions = new List<Vector2Int>();
            
            // 모든 빈 타일 찾기
            CheckBoardForEmptyTiles(tileBoardSystem.boardA, placeablePositions);
            CheckBoardForEmptyTiles(tileBoardSystem.boardB, placeablePositions);
            
            // 하이라이트
            tileBoardSystem.HighlightTiles(tileBoardSystem.boardA, 
                placeablePositions.Where(p => p.x < 6 && p.y < 3).ToList(), 
                new Color(0.5f, 1f, 0.5f, 0.5f));
            tileBoardSystem.HighlightTiles(tileBoardSystem.boardB, 
                placeablePositions.Where(p => p.x < 6 && p.y < 3).ToList(), 
                new Color(0.5f, 1f, 0.5f, 0.5f));
        }
        
        private void StartBattlePhase()
        {
            currentPhase = BattlePhase.Battle;
            currentTurn = TurnState.PlayerTurn;
            
            if (battleUI != null)
            {
                battleUI.ShowBattleUI();
                battleUI.UpdateTurnIndicator("전투 페이즈 - 플레이어 턴");
            }
        }
        
        private void SelectUnit(CharacterUnit unit)
        {
            selectedUnit = unit;
            tileBoardSystem.ClearHighlights();
            
            // 공격 범위 표시
            // TODO: CharacterUnit doesn't have currentBoard, currentTile is CurrentTile (capital C), and jobClass is private
            // Need to refactor to track board and access jobClass properly
            // tileBoardSystem.HighlightTiles(board, attackRange, new Color(1f, 0.5f, 0.5f, 0.5f));
            
            if (battleUI != null)
            {
                battleUI.ShowUnitInfo(unit);
            }
        }
        
        private void ExecuteAttack(TileBoardSystem.Board board, Vector2Int targetPosition)
        {
            // TODO: Need to fix property access for CharacterUnit
            var attackRange = new List<Vector2Int>(); // Placeholder
            
            if (!attackRange.Contains(targetPosition)) return;
            
            // 공격 범위 내 모든 적 유닛 찾기
            List<CharacterUnit> targets = new List<CharacterUnit>();
            var tile = board.tiles[targetPosition.x, targetPosition.y];
            if (tile.IsOccupied && tile.occupyingUnit.Team != selectedUnit.Team)
            {
                targets.Add(tile.occupyingUnit);
            }
            
            // 공격 실행
            foreach (var target in targets)
            {
                DamageCalculation(selectedUnit, target);
            }
            
            // 턴 종료
            EndTurn();
        }
        
        private void DamageCalculation(CharacterUnit attacker, CharacterUnit defender)
        {
            // CharacterUnit handles damage calculation internally
            attacker.Attack(defender);
            
            if (!defender.IsAlive())
            {
                HandleUnitDefeat(defender);
            }
        }
        
        private void HandleUnitDefeat(CharacterUnit unit)
        {
            // TODO: Need to track board for unit
            var board = tileBoardSystem.boardA; // Placeholder
            tileBoardSystem.RemoveUnit(board, unit);
            
            if (unit.isPlayerUnit)
                playerUnits.Remove(unit);
            else
                enemyUnits.Remove(unit);
            
            Destroy(unit.gameObject);
            
            // 승리 조건 체크
            CheckVictoryCondition();
        }
        
        private void EndTurn()
        {
            selectedUnit = null;
            tileBoardSystem.ClearHighlights();
            
            if (currentTurn == TurnState.PlayerTurn)
            {
                currentTurn = TurnState.EnemyTurn;
                StartCoroutine(ExecuteAITurn());
            }
            else
            {
                currentTurn = TurnState.PlayerTurn;
                if (battleUI != null)
                {
                    battleUI.UpdateTurnIndicator("플레이어 턴");
                }
            }
        }
        
        private System.Collections.IEnumerator ExecuteAITurn()
        {
            currentTurn = TurnState.Processing;
            
            yield return new WaitForSeconds(aiThinkTime);
            
            // AI 로직: 가장 효과적인 공격 찾기
            CharacterUnit bestAttacker = null;
            CharacterUnit bestTarget = null;
            float bestDamage = 0;
            
            foreach (var attacker in enemyUnits)
            {
                // TODO: Need to fix property access for CharacterUnit
                var board = tileBoardSystem.boardA; // Placeholder
                var attackRange = new List<Vector2Int>(); // Placeholder
                
                foreach (var pos in attackRange)
                {
                    var tile = board.tiles[pos.x, pos.y];
                    if (tile.IsOccupied && tile.occupyingUnit.Team == Tile.Team.Ally)
                    {
                        // TODO: Need proper damage calculation
                        float potentialDamage = 10; // Placeholder
                        if (potentialDamage > bestDamage)
                        {
                            bestDamage = potentialDamage;
                            bestAttacker = attacker;
                            bestTarget = tile.occupyingUnit;
                        }
                    }
                }
            }
            
            if (bestAttacker != null && bestTarget != null)
            {
                DamageCalculation(bestAttacker, bestTarget);
            }
            
            EndTurn();
        }
        
        private void UpdateTurnTimer()
        {
            if (currentTurn == TurnState.Processing) return;
            
            currentTurnTime -= Time.deltaTime;
            
            if (battleUI != null)
            {
                battleUI.UpdateTimer(currentTurnTime);
            }
            
            if (currentTurnTime <= 0)
            {
                // 시간 초과 - 턴 강제 종료
                EndTurn();
            }
        }
        
        private void CheckVictoryCondition()
        {
            int playerScoreA = tileBoardSystem.boardA.GetUnitCount(true);
            int enemyScoreA = tileBoardSystem.boardA.GetUnitCount(false);
            int playerScoreB = tileBoardSystem.boardB.GetUnitCount(true);
            int enemyScoreB = tileBoardSystem.boardB.GetUnitCount(false);
            
            bool playerWinsA = playerScoreA > enemyScoreA;
            bool playerWinsB = playerScoreB > enemyScoreB;
            
            int totalScore = 0;
            if (playerWinsA) totalScore++;
            if (playerWinsB) totalScore++;
            
            // 모든 유닛이 제거되었는지 확인
            if (playerUnits.Count == 0 || enemyUnits.Count == 0)
            {
                EndBattle(totalScore);
            }
        }
        
        private void EndBattle(int playerScore)
        {
            currentPhase = BattlePhase.Result;
            
            string result;
            if (playerScore == 2)
                result = "승리! (2점)";
            else if (playerScore == 1)
                result = "무승부 (1점)";
            else
                result = "패배 (0점)";
            
            if (battleUI != null)
            {
                battleUI.ShowBattleResult(result, playerScore);
            }
        }
    }
    
    public class AIBattleController : MonoBehaviour
    {
        [Header("Battle Settings")]
        [SerializeField] private int maxCharactersPerPlayer = 10;
        [SerializeField] private float turnTimer = 30f;
        [SerializeField] private float aiThinkTime = 1.5f;
        
        [Header("System References")]
        [SerializeField] private TileBoardSystem tileBoardSystem;
        [SerializeField] private CharacterSelectUI characterSelectionUI;
        [SerializeField] private BattleUIManager battleUI;
        
        [Header("Current Battle State")]
        public BattlePhase currentPhase = BattlePhase.CharacterSelection;
        public TurnState currentTurn = TurnState.PlayerTurn;
        
        // 선택된 캐릭터들
        private List<Character> playerSelectedCharacters = new List<Character>();
        private List<Character> enemySelectedCharacters = new List<Character>();
        
        // 배치된 유닛들
        private List<CharacterUnit> playerUnits = new List<CharacterUnit>();
        private List<CharacterUnit> enemyUnits = new List<CharacterUnit>();
        
        // 현재 선택된 캐릭터/유닛
        private Character selectedCharacterToPlace;
        private CharacterUnit selectedUnit;
        private int currentPlacementIndex = 0;
        
        // 타이머
        private float currentTurnTime;
        
        private void Start()
        {
            InitializeBattle();
        }
        
        private void Update()
        {
            if (currentPhase == BattlePhase.Placement || currentPhase == BattlePhase.Battle)
            {
                UpdateTurnTimer();
            }
        }
        
        private void InitializeBattle()
        {
            // 캐릭터 선택 UI 표시
            if (characterSelectionUI != null)
            {
                characterSelectionUI.Show(OnCharacterSelectionComplete);
            }
            else
            {
                // 테스트용: 랜덤 캐릭터 선택
                AutoSelectCharacters();
            }
        }
        
        private void OnCharacterSelectionComplete(List<Character> selectedCharacters)
        {
            playerSelectedCharacters = selectedCharacters;
            
            // AI 캐릭터 자동 선택
            SelectAICharacters();
            
            // 배치 페이즈로 전환
            StartPlacementPhase();
        }
        
        private void AutoSelectCharacters()
        {
            // 테스트용: 사용 가능한 캐릭터 중 10개 랜덤 선택
            var allCharacters = DataManager.Instance.GetAllCharacters();
            
            // 플레이어 캐릭터 선택
            playerSelectedCharacters = allCharacters
                .OrderBy(x => Random.Range(0f, 1f))
                .Take(maxCharactersPerPlayer)
                .ToList();
            
            // AI 캐릭터 선택
            var remainingCharacters = allCharacters
                .Where(c => !playerSelectedCharacters.Contains(c))
                .ToList();
                
            enemySelectedCharacters = remainingCharacters
                .OrderBy(x => Random.Range(0f, 1f))
                .Take(maxCharactersPerPlayer)
                .ToList();
            
            StartPlacementPhase();
        }
        
        private void SelectAICharacters()
        {
            var allCharacters = DataManager.Instance.GetAllCharacters();
            var availableCharacters = allCharacters
                .Where(c => !playerSelectedCharacters.Any(p => p.characterID == c.characterID))
                .ToList();
            
            // AI는 균형잡힌 팀 구성을 시도
            Dictionary<JobClass, int> jobCounts = new Dictionary<JobClass, int>();
            
            while (enemySelectedCharacters.Count < maxCharactersPerPlayer && availableCharacters.Count > 0)
            {
                // 가장 적은 직업 우선 선택
                var leastRepresentedJob = System.Enum.GetValues(typeof(JobClass))
                    .Cast<JobClass>()
                    .OrderBy(j => jobCounts.GetValueOrDefault(j, 0))
                    .First();
                
                var candidate = availableCharacters
                    .Where(c => c.jobClass == leastRepresentedJob)
                    .OrderByDescending(c => c.rarity)
                    .FirstOrDefault();
                
                if (candidate == null)
                {
                    candidate = availableCharacters
                        .OrderByDescending(c => c.rarity)
                        .First();
                }
                
                enemySelectedCharacters.Add(candidate);
                availableCharacters.Remove(candidate);
                
                if (!jobCounts.ContainsKey(candidate.jobClass))
                    jobCounts[candidate.jobClass] = 0;
                jobCounts[candidate.jobClass]++;
            }
        }
        
        private void StartPlacementPhase()
        {
            currentPhase = BattlePhase.Placement;
            currentPlacementIndex = 0;
            currentTurn = TurnState.PlayerTurn;
            
            if (battleUI != null)
            {
                battleUI.ShowPlacementUI();
                battleUI.UpdateTurnIndicator("배치 페이즈 - 플레이어 턴");
            }
            
            StartPlayerPlacementTurn();
        }
        
        private void StartPlayerPlacementTurn()
        {
            if (currentPlacementIndex >= playerSelectedCharacters.Count)
            {
                // 모든 캐릭터 배치 완료
                StartBattlePhase();
                return;
            }
            
            currentTurn = TurnState.PlayerTurn;
            selectedCharacterToPlace = playerSelectedCharacters[currentPlacementIndex];
            currentTurnTime = turnTimer;
            
            // 배치 가능한 타일 하이라이트
            HighlightPlaceableTiles();
            
            if (battleUI != null)
            {
                battleUI.ShowCharacterToPlace(selectedCharacterToPlace);
                battleUI.UpdateTurnIndicator($"캐릭터 배치: {selectedCharacterToPlace.characterName}");
            }
        }
        
        private void StartEnemyPlacementTurn()
        {
            if (currentPlacementIndex >= enemySelectedCharacters.Count)
            {
                currentPlacementIndex = 0;
                StartPlayerPlacementTurn();
                return;
            }
            
            currentTurn = TurnState.EnemyTurn;
            StartCoroutine(AIPlaceCharacter());
        }
        
        private System.Collections.IEnumerator AIPlaceCharacter()
        {
            currentTurn = TurnState.Processing;
            
            yield return new WaitForSeconds(aiThinkTime);
            
            var characterToPlace = enemySelectedCharacters[currentPlacementIndex];
            
            // AI 배치 전략: 공격 범위를 고려한 최적 위치 선택
            Vector2Int bestPosition = FindBestPlacementPosition(characterToPlace);
            TileBoardSystem.Board selectedBoard = Random.Range(0f, 1f) > 0.5f ? 
                tileBoardSystem.boardA : tileBoardSystem.boardB;
            
            // 유닛 생성 및 배치
            CharacterUnit unit = CreateUnit(characterToPlace, false);
            if (tileBoardSystem.PlaceUnit(selectedBoard, bestPosition, unit))
            {
                enemyUnits.Add(unit);
                currentPlacementIndex++;
            }
            
            // 다음 턴
            if (currentPlacementIndex < enemySelectedCharacters.Count)
            {
                StartEnemyPlacementTurn();
            }
            else
            {
                currentPlacementIndex = 0;
                StartPlayerPlacementTurn();
            }
        }
        
        private Vector2Int FindBestPlacementPosition(Character character)
        {
            List<Vector2Int> availablePositions = new List<Vector2Int>();
            
            // 모든 빈 타일 찾기
            CheckBoardForEmptyTiles(tileBoardSystem.boardA, availablePositions);
            CheckBoardForEmptyTiles(tileBoardSystem.boardB, availablePositions);
            
            if (availablePositions.Count == 0)
                return Vector2Int.zero;
            
            // 직업별 선호 위치 계산
            Vector2Int bestPosition = availablePositions[0];
            float bestScore = 0;
            
            foreach (var pos in availablePositions)
            {
                float score = EvaluatePosition(character, pos);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = pos;
                }
            }
            
            return bestPosition;
        }
        
        private void CheckBoardForEmptyTiles(TileBoardSystem.Board board, List<Vector2Int> emptyTiles)
        {
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (!board.tiles[x, y].IsOccupied)
                    {
                        emptyTiles.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
        
        private float EvaluatePosition(Character character, Vector2Int position)
        {
            float score = 0;
            
            // 직업별 위치 선호도
            switch (character.jobClass)
            {
                case JobClass.Warrior:
                case JobClass.Knight:
                    // 전방 선호
                    score += (5 - position.x) * 2;
                    break;
                    
                case JobClass.Archer:
                case JobClass.Gunner:
                    // 후방 선호
                    score += position.x * 2;
                    break;
                    
                case JobClass.Wizard:
                case JobClass.Sage:
                    // 중앙 선호
                    score += (3 - Mathf.Abs(position.x - 2.5f)) * 2;
                    score += (1.5f - Mathf.Abs(position.y - 1)) * 1;
                    break;
                    
                case JobClass.Priest:
                    // 아군 근처 선호
                    foreach (var unit in enemyUnits)
                    {
                        float distance = Vector2Int.Distance(position, unit.currentTile);
                        if (distance <= 2)
                            score += 3;
                    }
                    break;
                    
                case JobClass.Rogue:
                    // 측면 선호
                    score += Mathf.Min(position.y, 2 - position.y) * 3;
                    break;
            }
            
            return score;
        }
        
        public void OnTileClicked(TileBoardSystem.Board board, TileBoardSystem.Tile tile)
        {
            if (currentTurn != TurnState.PlayerTurn) return;
            
            switch (currentPhase)
            {
                case BattlePhase.Placement:
                    HandlePlacementClick(board, tile);
                    break;
                    
                case BattlePhase.Battle:
                    HandleBattleClick(board, tile);
                    break;
            }
        }
        
        private void HandlePlacementClick(TileBoardSystem.Board board, TileBoardSystem.Tile tile)
        {
            if (selectedCharacterToPlace == null) return;
            if (tile.IsOccupied) return;
            
            // 유닛 생성 및 배치
            CharacterUnit unit = CreateUnit(selectedCharacterToPlace, true);
            if (tileBoardSystem.PlaceUnit(board, tile.position, unit))
            {
                playerUnits.Add(unit);
                currentPlacementIndex++;
                
                tileBoardSystem.ClearHighlights();
                
                if (currentPlacementIndex >= playerSelectedCharacters.Count)
                {
                    // 플레이어 배치 완료, AI 턴
                    currentPlacementIndex = 0;
                    StartEnemyPlacementTurn();
                }
                else
                {
                    StartPlayerPlacementTurn();
                }
            }
        }
        
        private void HandleBattleClick(TileBoardSystem.Board board, TileBoardSystem.Tile tile)
        {
            if (tile.IsOccupied && tile.occupyingUnit.Team == Tile.Team.Ally)
            {
                // 유닛 선택
                SelectUnit(tile.occupyingUnit);
            }
            else if (selectedUnit != null && !tile.IsOccupied)
            {
                // 공격 실행
                ExecuteAttack(board, tile.position);
            }
        }
        
        private CharacterUnit CreateUnit(Character character, bool isPlayer)
        {
            // 유닛 프리팹 로드 (실제로는 Resources나 Addressables 사용)
            GameObject unitPrefab = Resources.Load<GameObject>("Prefabs/Unit");
            if (unitPrefab == null)
            {
                // 테스트용 기본 유닛 생성
                unitPrefab = new GameObject("Unit");
                unitPrefab.AddComponent<SpriteRenderer>();
            }
            
            GameObject unitObj = Instantiate(unitPrefab);
            CharacterUnit unit = unitObj.GetComponent<CharacterUnit>();
            if (unit == null)
                unit = unitObj.AddComponent<CharacterUnit>();
            
            // 유닛 초기화
            // Note: Character needs to be converted to CharacterData
            // and team needs to be set based on isPlayer flag
            // This requires additional implementation to convert Character to CharacterData
            
            return unit;
        }
        
        private void HighlightPlaceableTiles()
        {
            List<Vector2Int> placeablePositions = new List<Vector2Int>();
            
            // 모든 빈 타일 찾기
            CheckBoardForEmptyTiles(tileBoardSystem.boardA, placeablePositions);
            CheckBoardForEmptyTiles(tileBoardSystem.boardB, placeablePositions);
            
            // 하이라이트
            tileBoardSystem.HighlightTiles(tileBoardSystem.boardA, 
                placeablePositions.Where(p => p.x < 6 && p.y < 3).ToList(), 
                new Color(0.5f, 1f, 0.5f, 0.5f));
            tileBoardSystem.HighlightTiles(tileBoardSystem.boardB, 
                placeablePositions.Where(p => p.x < 6 && p.y < 3).ToList(), 
                new Color(0.5f, 1f, 0.5f, 0.5f));
        }
        
        private void StartBattlePhase()
        {
            currentPhase = BattlePhase.Battle;
            currentTurn = TurnState.PlayerTurn;
            
            if (battleUI != null)
            {
                battleUI.ShowBattleUI();
                battleUI.UpdateTurnIndicator("전투 페이즈 - 플레이어 턴");
            }
        }
        
        private void SelectUnit(CharacterUnit unit)
        {
            selectedUnit = unit;
            tileBoardSystem.ClearHighlights();
            
            // 공격 범위 표시
            // TODO: CharacterUnit doesn't have currentBoard, currentTile is CurrentTile (capital C), and jobClass is private
            // Need to refactor to track board and access jobClass properly
            // tileBoardSystem.HighlightTiles(board, attackRange, new Color(1f, 0.5f, 0.5f, 0.5f));
            
            if (battleUI != null)
            {
                battleUI.ShowUnitInfo(unit);
            }
        }
        
        private void ExecuteAttack(TileBoardSystem.Board board, Vector2Int targetPosition)
        {
            // TODO: Need to fix property access for CharacterUnit
            var attackRange = new List<Vector2Int>(); // Placeholder
            
            if (!attackRange.Contains(targetPosition)) return;
            
            // 공격 범위 내 모든 적 유닛 찾기
            List<CharacterUnit> targets = new List<CharacterUnit>();
            var tile = board.tiles[targetPosition.x, targetPosition.y];
            if (tile.IsOccupied && tile.occupyingUnit.Team != selectedUnit.Team)
            {
                targets.Add(tile.occupyingUnit);
            }
            
            // 공격 실행
            foreach (var target in targets)
            {
                DamageCalculation(selectedUnit, target);
            }
            
            // 턴 종료
            EndTurn();
        }
        
        private void DamageCalculation(CharacterUnit attacker, CharacterUnit defender)
        {
            // CharacterUnit handles damage calculation internally
            attacker.Attack(defender);
            
            if (!defender.IsAlive())
            {
                HandleUnitDefeat(defender);
            }
        }
        
        private void HandleUnitDefeat(CharacterUnit unit)
        {
            // TODO: Need to track board for unit
            var board = tileBoardSystem.boardA; // Placeholder
            tileBoardSystem.RemoveUnit(board, unit);
            
            if (unit.isPlayerUnit)
                playerUnits.Remove(unit);
            else
                enemyUnits.Remove(unit);
            
            Destroy(unit.gameObject);
            
            // 승리 조건 체크
            CheckVictoryCondition();
        }
        
        private void EndTurn()
        {
            selectedUnit = null;
            tileBoardSystem.ClearHighlights();
            
            if (currentTurn == TurnState.PlayerTurn)
            {
                currentTurn = TurnState.EnemyTurn;
                StartCoroutine(ExecuteAITurn());
            }
            else
            {
                currentTurn = TurnState.PlayerTurn;
                if (battleUI != null)
                {
                    battleUI.UpdateTurnIndicator("플레이어 턴");
                }
            }
        }
        
        private System.Collections.IEnumerator ExecuteAITurn()
        {
            currentTurn = TurnState.Processing;
            
            yield return new WaitForSeconds(aiThinkTime);
            
            // AI 로직: 가장 효과적인 공격 찾기
            CharacterUnit bestAttacker = null;
            CharacterUnit bestTarget = null;
            float bestDamage = 0;
            
            foreach (var attacker in enemyUnits)
            {
                // TODO: Need to fix property access for CharacterUnit
                var board = tileBoardSystem.boardA; // Placeholder
                var attackRange = new List<Vector2Int>(); // Placeholder
                
                foreach (var pos in attackRange)
                {
                    var tile = board.tiles[pos.x, pos.y];
                    if (tile.IsOccupied && tile.occupyingUnit.Team == Tile.Team.Ally)
                    {
                        // TODO: Need proper damage calculation
                        float potentialDamage = 10; // Placeholder
                        if (potentialDamage > bestDamage)
                        {
                            bestDamage = potentialDamage;
                            bestAttacker = attacker;
                            bestTarget = tile.occupyingUnit;
                        }
                    }
                }
            }
            
            if (bestAttacker != null && bestTarget != null)
            {
                DamageCalculation(bestAttacker, bestTarget);
            }
            
            EndTurn();
        }
        
        private void UpdateTurnTimer()
        {
            if (currentTurn == TurnState.Processing) return;
            
            currentTurnTime -= Time.deltaTime;
            
            if (battleUI != null)
            {
                battleUI.UpdateTimer(currentTurnTime);
            }
            
            if (currentTurnTime <= 0)
            {
                // 시간 초과 - 턴 강제 종료
                EndTurn();
            }
        }
        
        private void CheckVictoryCondition()
        {
            int playerScoreA = tileBoardSystem.boardA.GetUnitCount(true);
            int enemyScoreA = tileBoardSystem.boardA.GetUnitCount(false);
            int playerScoreB = tileBoardSystem.boardB.GetUnitCount(true);
            int enemyScoreB = tileBoardSystem.boardB.GetUnitCount(false);
            
            bool playerWinsA = playerScoreA > enemyScoreA;
            bool playerWinsB = playerScoreB > enemyScoreB;
            
            int totalScore = 0;
            if (playerWinsA) totalScore++;
            if (playerWinsB) totalScore++;
            
            // 모든 유닛이 제거되었는지 확인
            if (playerUnits.Count == 0 || enemyUnits.Count == 0)
            {
                EndBattle(totalScore);
            }
        }
        
        private void EndBattle(int playerScore)
        {
            currentPhase = BattlePhase.Result;
            
            string result;
            if (playerScore == 2)
                result = "승리! (2점)";
            else if (playerScore == 1)
                result = "무승부 (1점)";
            else
                result = "패배 (0점)";
            
            if (battleUI != null)
            {
                battleUI.ShowBattleResult(result, playerScore);
            }
        }
    }
}