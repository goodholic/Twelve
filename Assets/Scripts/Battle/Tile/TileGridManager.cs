using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 타일 그리드 매니저 - A타일(위쪽 6x3)과 B타일(아래쪽 6x3) 관리
    /// </summary>
    public class TileGridManager : MonoBehaviour
    {
        private static TileGridManager _instance;
        public static TileGridManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<TileGridManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("TileGridManager");
                        _instance = go.AddComponent<TileGridManager>();
                    }
                }
                return _instance;
            }
        }
        
        [Header("그리드 설정")]
        [SerializeField] private int gridWidth = 6;
        [SerializeField] private int gridHeight = 3;
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private float tileSpacing = 0.1f;
        [SerializeField] private float gridSeparation = 2f; // A와 B 그리드 사이 거리
        
        [Header("타일 프리팹")]
        [SerializeField] private GameObject tilePrefab;
        
        [Header("타일 그리드")]
        private Tile[,] tileGridA; // 위쪽 6x3 그리드
        private Tile[,] tileGridB; // 아래쪽 6x3 그리드
        
        [Header("타일 부모 오브젝트")]
        [SerializeField] private Transform tileParentA;
        [SerializeField] private Transform tileParentB;
        
        // 캐릭터 배치 정보
        private Dictionary<Tile, CharacterUnit> placedCharacters = new Dictionary<Tile, CharacterUnit>();
        
        // 이벤트
        public System.Action<Tile> OnTileSelected;
        public System.Action<Tile, CharacterUnit> OnCharacterPlaced;
        public System.Action<Tile> OnCharacterRemoved;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        void Start()
        {
            InitializeGrids();
        }
        
        /// <summary>
        /// 그리드 초기화
        /// </summary>
        void InitializeGrids()
        {
            // 부모 오브젝트 생성
            if (tileParentA == null)
            {
                GameObject parentA = new GameObject("Grid_A");
                parentA.transform.parent = transform;
                tileParentA = parentA.transform;
            }
            
            if (tileParentB == null)
            {
                GameObject parentB = new GameObject("Grid_B");
                parentB.transform.parent = transform;
                tileParentB = parentB.transform;
            }
            
            // 그리드 배열 초기화
            tileGridA = new Tile[gridWidth, gridHeight];
            tileGridB = new Tile[gridWidth, gridHeight];
            
            // A 그리드 생성 (위쪽)
            CreateGrid(tileGridA, tileParentA, Tile.TileType.A, Vector3.up * gridSeparation * 0.5f);
            
            // B 그리드 생성 (아래쪽)
            CreateGrid(tileGridB, tileParentB, Tile.TileType.B, Vector3.down * gridSeparation * 0.5f);
        }
        
        /// <summary>
        /// 그리드 생성
        /// </summary>
        void CreateGrid(Tile[,] grid, Transform parent, Tile.TileType tileType, Vector3 offset)
        {
            float totalWidth = gridWidth * tileSize + (gridWidth - 1) * tileSpacing;
            float totalHeight = gridHeight * tileSize + (gridHeight - 1) * tileSpacing;
            
            Vector3 startPos = new Vector3(-totalWidth * 0.5f + tileSize * 0.5f, 0, -totalHeight * 0.5f + tileSize * 0.5f);
            startPos += offset;
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 tilePos = startPos + new Vector3(
                        x * (tileSize + tileSpacing),
                        0,
                        y * (tileSize + tileSpacing)
                    );
                    
                    GameObject tileGO = tilePrefab != null ? 
                        Instantiate(tilePrefab, tilePos, Quaternion.identity, parent) : 
                        CreateDefaultTile(tilePos, parent);
                    
                    Tile tile = tileGO.GetComponent<Tile>();
                    if (tile == null)
                    {
                        tile = tileGO.AddComponent<Tile>();
                    }
                    
                    tile.Initialize(x, y, tileType);
                    grid[x, y] = tile;
                }
            }
        }
        
        /// <summary>
        /// 기본 타일 생성 (프리팹이 없을 경우)
        /// </summary>
        GameObject CreateDefaultTile(Vector3 position, Transform parent)
        {
            GameObject tileGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tileGO.transform.position = position;
            tileGO.transform.rotation = Quaternion.Euler(90, 0, 0);
            tileGO.transform.localScale = Vector3.one * tileSize * 0.9f;
            tileGO.transform.parent = parent;
            
            // 기본 머티리얼 설정
            Renderer renderer = tileGO.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = Color.white;
            }
            
            // 콜라이더 설정
            Collider collider = tileGO.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
            
            return tileGO;
        }
        
        /// <summary>
        /// 타일 가져오기
        /// </summary>
        public Tile GetTile(Tile.TileType tileType, int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
                return null;
                
            return tileType == Tile.TileType.A ? tileGridA[x, y] : tileGridB[x, y];
        }
        
        /// <summary>
        /// 특정 팀의 캐릭터 수 계산
        /// </summary>
        public int GetCharacterCount(Tile.TileType tileType, Tile.Team team)
        {
            Tile[,] grid = tileType == Tile.TileType.A ? tileGridA : tileGridB;
            int count = 0;
            
            foreach (Tile tile in grid)
            {
                if (tile.isOccupied && tile.occupiedTeam == team)
                {
                    count++;
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// 캐릭터 배치
        /// </summary>
        public bool PlaceCharacter(CharacterUnit character, Tile targetTile)
        {
            if (character == null || targetTile == null || targetTile.isOccupied)
                return false;
            
            // 기존 위치에서 제거
            RemoveCharacterFromCurrentTile(character);
            
            // 새 위치에 배치
            targetTile.PlaceUnit(character.gameObject, character.Team);
            placedCharacters[targetTile] = character;
            character.CurrentTile = targetTile;
            
            // 캐릭터 위치 업데이트
            character.transform.position = targetTile.GetWorldPosition();
            
            OnCharacterPlaced?.Invoke(targetTile, character);
            return true;
        }
        
        /// <summary>
        /// 캐릭터 제거
        /// </summary>
        public void RemoveCharacter(CharacterUnit character)
        {
            if (character == null) return;
            
            RemoveCharacterFromCurrentTile(character);
        }
        
        /// <summary>
        /// 현재 타일에서 캐릭터 제거
        /// </summary>
        void RemoveCharacterFromCurrentTile(CharacterUnit character)
        {
            var tileToRemove = placedCharacters.FirstOrDefault(x => x.Value == character).Key;
            if (tileToRemove != null)
            {
                tileToRemove.RemoveUnit();
                placedCharacters.Remove(tileToRemove);
                character.CurrentTile = null;
                OnCharacterRemoved?.Invoke(tileToRemove);
            }
        }
        
        /// <summary>
        /// 캐릭터의 공격 범위 타일 가져오기
        /// </summary>
        public List<Tile> GetAttackRangeTiles(CharacterUnit character)
        {
            if (character == null || character.CurrentTile == null)
                return new List<Tile>();
            
            List<Tile> rangeTiles = new List<Tile>();
            Tile currentTile = character.CurrentTile;
            
            // 캐릭터의 공격 패턴에 따라 범위 계산
            foreach (var rangeOffset in character.AttackPattern.GetRangeOffsets())
            {
                int targetX = currentTile.x + rangeOffset.x;
                int targetY = currentTile.y + rangeOffset.y;
                
                // 같은 타일 그룹 내에서만 공격 가능
                Tile targetTile = GetTile(currentTile.tileType, targetX, targetY);
                if (targetTile != null)
                {
                    rangeTiles.Add(targetTile);
                }
            }
            
            return rangeTiles;
        }
        
        /// <summary>
        /// 공격 범위 표시
        /// </summary>
        public void ShowAttackRange(CharacterUnit character, bool show)
        {
            List<Tile> rangeTiles = GetAttackRangeTiles(character);
            
            foreach (Tile tile in rangeTiles)
            {
                tile.ShowPlacementIndicator(show, tile.CanPlaceUnit());
            }
        }
        
        /// <summary>
        /// 모든 타일 상태 리셋
        /// </summary>
        public void ResetAllTiles()
        {
            foreach (Tile tile in tileGridA)
            {
                tile.ResetTile();
            }
            
            foreach (Tile tile in tileGridB)
            {
                tile.ResetTile();
            }
            
            placedCharacters.Clear();
        }
        
        /// <summary>
        /// 승리 조건 체크
        /// </summary>
        public GameResult CheckVictoryCondition()
        {
            int allyCountA = GetCharacterCount(Tile.TileType.A, Tile.Team.Ally);
            int enemyCountA = GetCharacterCount(Tile.TileType.A, Tile.Team.Enemy);
            int allyCountB = GetCharacterCount(Tile.TileType.B, Tile.Team.Ally);
            int enemyCountB = GetCharacterCount(Tile.TileType.B, Tile.Team.Enemy);
            
            int allyScore = 0;
            int enemyScore = 0;
            
            // A 타일 우위 계산
            if (allyCountA > enemyCountA) allyScore++;
            else if (enemyCountA > allyCountA) enemyScore++;
            
            // B 타일 우위 계산
            if (allyCountB > enemyCountB) allyScore++;
            else if (enemyCountB > allyCountB) enemyScore++;
            
            // 결과 반환
            if (allyScore == 2) return GameResult.Victory;      // 2점 - 승리
            else if (allyScore == 1) return GameResult.Draw;    // 1점 - 무승부
            else return GameResult.Defeat;                      // 0점 - 패배
        }
    }
    
    /// <summary>
    /// 게임 결과
    /// </summary>
    public enum GameResult
    {
        Victory,    // 2점 - 양쪽 타일 모두 우위
        Draw,       // 1점 - 한쪽 타일만 우위
        Defeat      // 0점 - 우위 없음
    }
}