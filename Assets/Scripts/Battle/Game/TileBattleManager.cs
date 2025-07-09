using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Data;

namespace TacticalTileGame.Battle
{
    /// <summary>
    /// 타일 기반 전투 시스템 매니저
    /// </summary>
    public class TileBattleManager : MonoBehaviour
    {
        [Header("타일 설정")]
        [SerializeField] private int tileWidth = 6;
        [SerializeField] private int tileHeight = 3;
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private Transform tileParentA; // 위쪽 타일 그룹
        [SerializeField] private Transform tileParentB; // 아래쪽 타일 그룹
        
        [Header("전투 상태")]
        private TileGrid gridA;
        private TileGrid gridB;
        private List<CharacterUnit> playerUnits = new List<CharacterUnit>();
        private List<CharacterUnit> enemyUnits = new List<CharacterUnit>();
        private int currentTurn = 0;
        private bool isPlayerTurn = true;
        
        [Header("선택된 유닛")]
        private CharacterUnit selectedUnit;
        private List<Vector2Int> highlightedTiles = new List<Vector2Int>();
        
        private void Start()
        {
            InitializeBattleGrid();
        }
        
        /// <summary>
        /// 전투 그리드 초기화
        /// </summary>
        private void InitializeBattleGrid()
        {
            gridA = new TileGrid(tileWidth, tileHeight, true);
            gridB = new TileGrid(tileWidth, tileHeight, false);
            
            // 타일 생성
            CreateTileVisuals(gridA, tileParentA);
            CreateTileVisuals(gridB, tileParentB);
        }
        
        /// <summary>
        /// 타일 비주얼 생성
        /// </summary>
        private void CreateTileVisuals(TileGrid grid, Transform parent)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    GameObject tileObj = Instantiate(tilePrefab, parent);
                    Vector3 position = new Vector3(x * 1.1f, 0, y * 1.1f);
                    tileObj.transform.localPosition = position;
                    
                    TileVisual tileVisual = tileObj.GetComponent<TileVisual>();
                    if (tileVisual != null)
                    {
                        tileVisual.Initialize(grid, new Vector2Int(x, y));
                        tileVisual.OnTileClicked += OnTileClicked;
                    }
                }
            }
        }
        
        /// <summary>
        /// 캐릭터 배치
        /// </summary>
        public void PlaceCharacter(string characterId, Vector2Int position, bool isUpperGrid, bool isPlayer)
        {
            var characterData = dataManager.GetCharacter(characterId);
            if (characterData == null) return;
            
            TileGrid targetGrid = isUpperGrid ? gridA : gridB;
            if (!targetGrid.IsValidPosition(position)) return;
            if (targetGrid.GetUnit(position) != null) return; // 이미 유닛이 있음
            
            // 캐릭터 유닛 생성
            CharacterUnit unit = new CharacterUnit(characterData, position, isUpperGrid, isPlayer);
            targetGrid.PlaceUnit(position, unit);
            
            if (isPlayer)
                playerUnits.Add(unit);
            else
                enemyUnits.Add(unit);
            
            Debug.Log($"{characterData.characterName} placed at {position} on {(isUpperGrid ? "Grid A" : "Grid B")}");
        }
        
        /// <summary>
        /// 타일 클릭 이벤트
        /// </summary>
        private void OnTileClicked(TileGrid grid, Vector2Int position)
        {
            if (!isPlayerTurn) return;
            
            // 유닛 선택
            CharacterUnit clickedUnit = grid.GetUnit(position);
            if (clickedUnit != null && clickedUnit.IsPlayer)
            {
                SelectUnit(clickedUnit);
            }
            // 빈 타일에 유닛 배치
            else if (selectedUnit != null && clickedUnit == null)
            {
                // 이동이나 공격 처리
                if (highlightedTiles.Contains(position))
                {
                    PerformAction(selectedUnit, grid, position);
                }
            }
        }
        
        /// <summary>
        /// 유닛 선택
        /// </summary>
        private void SelectUnit(CharacterUnit unit)
        {
            selectedUnit = unit;
            ClearHighlights();
            
            // 공격 가능한 타일 하이라이트
            var attackableTiles = unit.GetAttackableTiles();
            foreach (var tile in attackableTiles)
            {
                HighlightTile(unit.IsUpperGrid ? gridA : gridB, tile, Color.red);
                highlightedTiles.Add(tile);
            }
        }
        
        /// <summary>
        /// 액션 수행
        /// </summary>
        private void PerformAction(CharacterUnit unit, TileGrid targetGrid, Vector2Int targetPosition)
        {
            // 공격 범위 내에 적이 있는지 확인
            var attackableTiles = unit.GetAttackableTiles();
            
            foreach (var tile in attackableTiles)
            {
                CharacterUnit target = targetGrid.GetUnit(tile);
                if (target != null && target.IsPlayer != unit.IsPlayer)
                {
                    // 공격 실행
                    PerformAttack(unit, target);
                    break;
                }
            }
            
            ClearHighlights();
            selectedUnit = null;
            EndTurn();
        }
        
        /// <summary>
        /// 공격 실행
        /// </summary>
        private void PerformAttack(CharacterUnit attacker, CharacterUnit target)
        {
            int damage = CalculateDamage(attacker, target);
            target.TakeDamage(damage);
            
            Debug.Log($"{attacker.Data.characterName} attacks {target.Data.characterName} for {damage} damage!");
            
            // 타겟이 죽었다면 그리드에서 제거
            if (target.IsDead)
            {
                TileGrid targetGrid = target.IsUpperGrid ? gridA : gridB;
                targetGrid.RemoveUnit(target.Position);
                
                if (target.IsPlayer)
                    playerUnits.Remove(target);
                else
                    enemyUnits.Remove(target);
            }
            
            // 승리 조건 체크
            CheckWinCondition();
        }
        
        /// <summary>
        /// 데미지 계산
        /// </summary>
        private int CalculateDamage(CharacterUnit attacker, CharacterUnit target)
        {
            int baseDamage = attacker.Data.baseAttack;
            int defense = target.Data.baseDefense;
            
            // 크리티컬 확률
            if (Random.value < attacker.Data.critRate)
            {
                baseDamage = Mathf.RoundToInt(baseDamage * 1.5f);
                Debug.Log("Critical Hit!");
            }
            
            int finalDamage = Mathf.Max(1, baseDamage - defense);
            return finalDamage;
        }
        
        /// <summary>
        /// 승리 조건 체크
        /// </summary>
        private void CheckWinCondition()
        {
            // A 타일과 B 타일에서 각각 유닛 수 계산
            int playerUnitsOnA = 0, enemyUnitsOnA = 0;
            int playerUnitsOnB = 0, enemyUnitsOnB = 0;
            
            foreach (var unit in playerUnits)
            {
                if (unit.IsUpperGrid) playerUnitsOnA++;
                else playerUnitsOnB++;
            }
            
            foreach (var unit in enemyUnits)
            {
                if (unit.IsUpperGrid) enemyUnitsOnA++;
                else enemyUnitsOnB++;
            }
            
            // 점수 계산
            int playerScore = 0;
            int enemyScore = 0;
            
            if (playerUnitsOnA > enemyUnitsOnA) playerScore++;
            else if (enemyUnitsOnA > playerUnitsOnA) enemyScore++;
            
            if (playerUnitsOnB > enemyUnitsOnB) playerScore++;
            else if (enemyUnitsOnB > playerUnitsOnB) enemyScore++;
            
            // 게임 종료 판정
            if (playerUnits.Count == 0 || enemyUnits.Count == 0 || currentTurn >= 20)
            {
                if (playerScore > enemyScore)
                    Debug.Log("Player Victory! Score: " + playerScore);
                else if (enemyScore > playerScore)
                    Debug.Log("Enemy Victory! Score: " + enemyScore);
                else
                    Debug.Log("Draw! Score: " + playerScore);
            }
        }
        
        /// <summary>
        /// 턴 종료
        /// </summary>
        private void EndTurn()
        {
            isPlayerTurn = !isPlayerTurn;
            currentTurn++;
            
            if (!isPlayerTurn)
            {
                // AI 턴 실행
                Invoke("ExecuteAITurn", 1f);
            }
        }
        
        /// <summary>
        /// AI 턴 실행
        /// </summary>
        private void ExecuteAITurn()
        {
            // 간단한 AI 로직: 랜덤 유닛으로 가장 가까운 적 공격
            if (enemyUnits.Count > 0)
            {
                CharacterUnit aiUnit = enemyUnits[Random.Range(0, enemyUnits.Count)];
                CharacterUnit target = FindClosestEnemy(aiUnit);
                
                if (target != null)
                {
                    PerformAttack(aiUnit, target);
                }
            }
            
            EndTurn();
        }
        
        /// <summary>
        /// 가장 가까운 적 찾기
        /// </summary>
        private CharacterUnit FindClosestEnemy(CharacterUnit unit)
        {
            CharacterUnit closest = null;
            float minDistance = float.MaxValue;
            
            foreach (var enemy in playerUnits)
            {
                float distance = Vector2Int.Distance(unit.Position, enemy.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = enemy;
                }
            }
            
            return closest;
        }
        
        /// <summary>
        /// 하이라이트 클리어
        /// </summary>
        private void ClearHighlights()
        {
            // 타일 하이라이트 제거 로직
            highlightedTiles.Clear();
        }
        
        /// <summary>
        /// 타일 하이라이트
        /// </summary>
        private void HighlightTile(TileGrid grid, Vector2Int position, Color color)
        {
            // 타일 하이라이트 표시 로직
        }
    }
    
    /// <summary>
    /// 타일 그리드
    /// </summary>
    public class TileGrid
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool IsUpperGrid { get; private set; }
        
        private CharacterUnit[,] units;
        
        public TileGrid(int width, int height, bool isUpper)
        {
            Width = width;
            Height = height;
            IsUpperGrid = isUpper;
            units = new CharacterUnit[width, height];
        }
        
        public bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
        }
        
        public CharacterUnit GetUnit(Vector2Int pos)
        {
            if (!IsValidPosition(pos)) return null;
            return units[pos.x, pos.y];
        }
        
        public void PlaceUnit(Vector2Int pos, CharacterUnit unit)
        {
            if (IsValidPosition(pos))
                units[pos.x, pos.y] = unit;
        }
        
        public void RemoveUnit(Vector2Int pos)
        {
            if (IsValidPosition(pos))
                units[pos.x, pos.y] = null;
        }
    }
    
    /// <summary>
    /// 캐릭터 유닛
    /// </summary>
    public class CharacterUnit
    {
        public CharacterDataSO Data { get; private set; }
        public Vector2Int Position { get; set; }
        public bool IsUpperGrid { get; private set; }
        public bool IsPlayer { get; private set; }
        public int CurrentHP { get; private set; }
        public bool IsDead => CurrentHP <= 0;
        
        public CharacterUnit(CharacterDataSO data, Vector2Int position, bool isUpper, bool isPlayer)
        {
            Data = data;
            Position = position;
            IsUpperGrid = isUpper;
            IsPlayer = isPlayer;
            CurrentHP = data.baseHP;
        }
        
        public List<Vector2Int> GetAttackableTiles()
        {
            return Data.GetAttackableTiles(Position);
        }
        
        public void TakeDamage(int damage)
        {
            CurrentHP -= damage;
            if (CurrentHP < 0) CurrentHP = 0;
        }
    }
    
    /// <summary>
    /// 타일 비주얼 컴포넌트
    /// </summary>
    public class TileVisual : MonoBehaviour
    {
        public delegate void TileClickedHandler(TileGrid grid, Vector2Int position);
        public event TileClickedHandler OnTileClicked;
        
        private TileGrid grid;
        private Vector2Int position;
        
        public void Initialize(TileGrid grid, Vector2Int position)
        {
            this.grid = grid;
            this.position = position;
        }
        
        private void OnMouseDown()
        {
            OnTileClicked?.Invoke(grid, position);
        }
    }
}