using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 6x3 타일 보드를 관리하는 시스템
    /// </summary>
    public class TileBoardSystem : MonoBehaviour
    {
        [System.Serializable]
        public class Tile
        {
            public Vector2Int position;
            public GameObject tileObject;
            public SpriteRenderer spriteRenderer;
            public Unit occupyingUnit;
            public bool isHighlighted;
            
            public Tile(Vector2Int pos, GameObject obj)
            {
                position = pos;
                tileObject = obj;
                spriteRenderer = obj.GetComponent<SpriteRenderer>();
            }
            
            public bool IsOccupied => occupyingUnit != null;
        }
        
        [System.Serializable]
        public class Board
        {
            public string boardName;
            public Transform boardTransform;
            public Tile[,] tiles = new Tile[6, 3];
            public List<Unit> units = new List<Unit>();
            
            public int GetUnitCount(bool isPlayerUnit)
            {
                int count = 0;
                foreach (var unit in units)
                {
                    if (unit != null && unit.isPlayerUnit == isPlayerUnit)
                        count++;
                }
                return count;
            }
        }
        
        [Header("Board Settings")]
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private float tileSize = 1.1f;
        [SerializeField] private float boardSpacing = 2f;
        
        [Header("Board References")]
        public Board boardA; // 위쪽 보드
        public Board boardB; // 아래쪽 보드
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalTileColor = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color highlightedTileColor = Color.yellow;
        [SerializeField] private Color validPlacementColor = new Color(0.5f, 1f, 0.5f);
        [SerializeField] private Color invalidPlacementColor = new Color(1f, 0.5f, 0.5f);
        [SerializeField] private Color attackRangeColor = new Color(1f, 0.8f, 0.3f, 0.5f);
        
        private void Awake()
        {
            InitializeBoards();
        }
        
        private void InitializeBoards()
        {
            // Board A (위쪽) 초기화
            GameObject boardAObject = new GameObject("Board A");
            boardAObject.transform.parent = transform;
            boardAObject.transform.position = new Vector3(0, boardSpacing, 0);
            boardA = new Board { boardName = "A", boardTransform = boardAObject.transform };
            CreateBoard(boardA, Vector3.zero);
            
            // Board B (아래쪽) 초기화
            GameObject boardBObject = new GameObject("Board B");
            boardBObject.transform.parent = transform;
            boardBObject.transform.position = new Vector3(0, -boardSpacing - 3 * tileSize, 0);
            boardB = new Board { boardName = "B", boardTransform = boardBObject.transform };
            CreateBoard(boardB, Vector3.zero);
        }
        
        private void CreateBoard(Board board, Vector3 offset)
        {
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Vector3 position = new Vector3(x * tileSize, y * tileSize, 0) + offset;
                    GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, board.boardTransform);
                    tileObj.name = $"Tile_{x}_{y}";
                    
                    Tile tile = new Tile(new Vector2Int(x, y), tileObj);
                    board.tiles[x, y] = tile;
                    
                    // 타일 클릭 이벤트 추가
                    TileClickHandler clickHandler = tileObj.AddComponent<TileClickHandler>();
                    clickHandler.Initialize(board, tile, this);
                }
            }
            
            // 보드 중앙 정렬
            board.boardTransform.position -= new Vector3(2.5f * tileSize, 1f * tileSize, 0);
        }
        
        public bool PlaceUnit(Board board, Vector2Int position, Unit unit)
        {
            if (!IsValidPosition(position)) return false;
            
            Tile tile = board.tiles[position.x, position.y];
            if (tile.IsOccupied) return false;
            
            tile.occupyingUnit = unit;
            board.units.Add(unit);
            
            // 유닛 위치 업데이트
            unit.transform.position = tile.tileObject.transform.position;
            unit.currentTile = position;
            unit.currentBoard = board.boardName;
            
            return true;
        }
        
        public void RemoveUnit(Board board, Unit unit)
        {
            foreach (var tile in board.tiles)
            {
                if (tile.occupyingUnit == unit)
                {
                    tile.occupyingUnit = null;
                    break;
                }
            }
            board.units.Remove(unit);
        }
        
        public List<Vector2Int> GetAttackRange(Board board, Vector2Int position, JobClass jobClass)
        {
            List<Vector2Int> attackTiles = new List<Vector2Int>();
            
            switch (jobClass)
            {
                case JobClass.Warrior: // 주변 1칸
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            Vector2Int targetPos = position + new Vector2Int(dx, dy);
                            if (IsValidPosition(targetPos))
                                attackTiles.Add(targetPos);
                        }
                    }
                    break;
                    
                case JobClass.Knight: // 전방 2칸 직선
                    for (int i = 1; i <= 2; i++)
                    {
                        Vector2Int targetPos = position + new Vector2Int(i, 0);
                        if (IsValidPosition(targetPos))
                            attackTiles.Add(targetPos);
                    }
                    break;
                    
                case JobClass.Wizard: // 3x3 광역
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            Vector2Int targetPos = position + new Vector2Int(dx, dy);
                            if (IsValidPosition(targetPos))
                                attackTiles.Add(targetPos);
                        }
                    }
                    break;
                    
                case JobClass.Priest: // 2x2 범위
                    for (int dx = 0; dx <= 1; dx++)
                    {
                        for (int dy = 0; dy <= 1; dy++)
                        {
                            Vector2Int targetPos = position + new Vector2Int(dx, dy);
                            if (IsValidPosition(targetPos))
                                attackTiles.Add(targetPos);
                        }
                    }
                    break;
                    
                case JobClass.Rogue: // 대각선
                    attackTiles.Add(position + new Vector2Int(1, 1));
                    attackTiles.Add(position + new Vector2Int(1, -1));
                    attackTiles.Add(position + new Vector2Int(-1, 1));
                    attackTiles.Add(position + new Vector2Int(-1, -1));
                    attackTiles.RemoveAll(pos => !IsValidPosition(pos));
                    break;
                    
                case JobClass.Sage: // 십자 모양
                    for (int i = -2; i <= 2; i++)
                    {
                        if (i == 0) continue;
                        Vector2Int horizontal = position + new Vector2Int(i, 0);
                        Vector2Int vertical = position + new Vector2Int(0, i);
                        if (IsValidPosition(horizontal)) attackTiles.Add(horizontal);
                        if (IsValidPosition(vertical)) attackTiles.Add(vertical);
                    }
                    break;
                    
                case JobClass.Archer: // 직선 관통 (오른쪽 전체)
                    for (int x = position.x + 1; x < 6; x++)
                    {
                        attackTiles.Add(new Vector2Int(x, position.y));
                    }
                    break;
                    
                case JobClass.Gunner: // 장거리 단일 (3칸 이상 떨어진 곳)
                    for (int x = 0; x < 6; x++)
                    {
                        for (int y = 0; y < 3; y++)
                        {
                            Vector2Int targetPos = new Vector2Int(x, y);
                            int distance = Mathf.Abs(x - position.x) + Mathf.Abs(y - position.y);
                            if (distance >= 3 && distance <= 4)
                                attackTiles.Add(targetPos);
                        }
                    }
                    break;
            }
            
            return attackTiles;
        }
        
        public void HighlightTiles(Board board, List<Vector2Int> positions, Color color)
        {
            foreach (var pos in positions)
            {
                if (IsValidPosition(pos))
                {
                    board.tiles[pos.x, pos.y].spriteRenderer.color = color;
                    board.tiles[pos.x, pos.y].isHighlighted = true;
                }
            }
        }
        
        public void ClearHighlights()
        {
            ClearBoardHighlights(boardA);
            ClearBoardHighlights(boardB);
        }
        
        private void ClearBoardHighlights(Board board)
        {
            foreach (var tile in board.tiles)
            {
                tile.spriteRenderer.color = normalTileColor;
                tile.isHighlighted = false;
            }
        }
        
        private bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < 6 && position.y >= 0 && position.y < 3;
        }
        
        public Board GetBoard(string boardName)
        {
            return boardName == "A" ? boardA : boardB;
        }
        
        // 타일 클릭 핸들러
        private class TileClickHandler : MonoBehaviour
        {
            private Board board;
            private Tile tile;
            private TileBoardSystem system;
            
            public void Initialize(Board board, Tile tile, TileBoardSystem system)
            {
                this.board = board;
                this.tile = tile;
                this.system = system;
            }
            
            private void OnMouseDown()
            {
                TurnBasedBattleSystem battleSystem = FindObjectOfType<TurnBasedBattleSystem>();
                if (battleSystem != null)
                {
                    battleSystem.OnTileClicked(board, tile);
                }
            }
        }
    }
}