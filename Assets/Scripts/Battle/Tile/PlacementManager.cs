using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using GuildMaster.Battle;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 유닛 배치를 관리하는 매니저
    /// A타일과 B타일에 각각 아군과 적군을 배치
    /// </summary>
    public class PlacementManager : MonoBehaviour
    {
        private static PlacementManager instance;
        public static PlacementManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<PlacementManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("PlacementManager");
                        instance = go.AddComponent<PlacementManager>();
                    }
                }
                return instance;
            }
        }
        
        [Header("그리드 설정")]
        public const int GRID_WIDTH = 6;   // 가로 6칸
        public const int GRID_HEIGHT = 3;  // 세로 3칸
        public const int MAX_UNITS_PER_SIDE = 9;  // 한 진영당 최대 9명
        
        [Header("타일 관리")]
        public GameObject tilePrefab;
        public Transform tileContainer;
        public float tileSize = 1f;
        public float tileSpacing = 0.1f;
        
        // 타일 그리드 - [타일타입, 진영, x, y]
        private Tile[,,,] tileGrid;
        
        // A타일과 B타일의 유닛 리스트
        private List<GameObject> tileA_PlayerUnits = new List<GameObject>();
        private List<GameObject> tileA_EnemyUnits = new List<GameObject>();
        private List<GameObject> tileB_PlayerUnits = new List<GameObject>();
        private List<GameObject> tileB_EnemyUnits = new List<GameObject>();
        
        // 적군 대기열 (다음에 나올 적들)
        private Queue<GameObject> tileA_EnemyQueue = new Queue<GameObject>();
        private Queue<GameObject> tileB_EnemyQueue = new Queue<GameObject>();
        
        [Header("배치 설정")]
        public bool isPlacementMode = false;
        public GameObject selectedUnit;
        public Tile.TileType currentTileType = Tile.TileType.A;
        public Tile.SideType currentSideType = Tile.SideType.Player;
        
        [Header("비주얼")]
        public Color validPlacementColor = new Color(0, 1, 0, 0.5f);
        public Color invalidPlacementColor = new Color(1, 0, 0, 0.5f);
        
        // 이벤트
        public System.Action<GameObject, Tile> OnUnitPlaced;
        public System.Action<GameObject, Tile> OnUnitRemoved;
        public System.Action<Tile.TileType> OnEnemyDefeated;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            
            InitializeGrid();
        }
        
        /// <summary>
        /// 그리드 초기화
        /// </summary>
        private void InitializeGrid()
        {
            // 4차원 배열: [타일타입(2), 진영(2), x(6), y(3)]
            tileGrid = new Tile[2, 2, GRID_WIDTH, GRID_HEIGHT];
            
            if (tileContainer == null)
            {
                tileContainer = new GameObject("TileContainer").transform;
                tileContainer.parent = transform;
            }
            
            CreateTileGrid();
        }
        
        /// <summary>
        /// 타일 그리드 생성
        /// </summary>
        private void CreateTileGrid()
        {
            float startX = -((GRID_WIDTH * 2 + 2) * (tileSize + tileSpacing)) / 2f;
            float startY = -(GRID_HEIGHT * (tileSize + tileSpacing)) / 2f;
            
            // A타일과 B타일 생성
            for (int tileType = 0; tileType < 2; tileType++)
            {
                float tileGroupOffsetY = tileType * (GRID_HEIGHT * (tileSize + tileSpacing) + 2f);
                
                // 아군 영역(왼쪽)과 적군 영역(오른쪽) 생성
                for (int side = 0; side < 2; side++)
                {
                    float sideOffsetX = side * ((GRID_WIDTH + 1) * (tileSize + tileSpacing));
                    
                    for (int x = 0; x < GRID_WIDTH; x++)
                    {
                        for (int y = 0; y < GRID_HEIGHT; y++)
                        {
                            Vector3 position = new Vector3(
                                startX + x * (tileSize + tileSpacing) + sideOffsetX,
                                startY + y * (tileSize + tileSpacing) + tileGroupOffsetY,
                                0
                            );
                            
                            GameObject tileGO = Instantiate(tilePrefab, position, Quaternion.identity, tileContainer);
                            Tile tile = tileGO.GetComponent<Tile>();
                            
                            if (tile == null)
                            {
                                tile = tileGO.AddComponent<Tile>();
                            }
                            
                            Tile.TileType type = (Tile.TileType)tileType;
                            Tile.SideType sideType = (Tile.SideType)side;
                            
                            tile.Initialize(x, y, type, sideType);
                            tileGrid[tileType, side, x, y] = tile;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 배치 모드 시작
        /// </summary>
        public void EnterPlacementMode(GameObject unit = null)
        {
            isPlacementMode = true;
            selectedUnit = unit;
            ShowPlacementIndicators(true);
            Debug.Log("배치 모드 활성화");
        }
        
        /// <summary>
        /// 배치 모드 종료
        /// </summary>
        public void ExitPlacementMode()
        {
            isPlacementMode = false;
            selectedUnit = null;
            ShowPlacementIndicators(false);
            Debug.Log("배치 모드 비활성화");
        }
        
        /// <summary>
        /// 배치 모드 여부
        /// </summary>
        public bool IsInPlacementMode()
        {
            return isPlacementMode;
        }
        
        /// <summary>
        /// 유닛 선택
        /// </summary>
        public void SelectUnit(GameObject unit)
        {
            selectedUnit = unit;
            EnterPlacementMode(unit);
        }
        
        /// <summary>
        /// 유닛 배치 시도
        /// </summary>
        public bool TryPlaceUnit(Tile tile)
        {
            if (!isPlacementMode || selectedUnit == null || tile == null)
                return false;
            
            // 현재 타일 타입과 진영이 맞는지 확인
            if (tile.tileType != currentTileType || tile.sideType != currentSideType)
            {
                Debug.Log($"잘못된 타일입니다. 현재 선택: {currentTileType}-{currentSideType}");
                return false;
            }
            
            // 배치 가능한지 확인
            if (!tile.CanPlaceUnit())
            {
                Debug.Log("이 타일에는 유닛을 배치할 수 없습니다.");
                return false;
            }
            
            // 유닛 수 제한 확인
            if (!CanAddUnit(tile.tileType, tile.sideType))
            {
                Debug.Log("최대 유닛 수에 도달했습니다.");
                return false;
            }
            
            // 유닛 배치
            PlaceUnit(selectedUnit, tile);
            
            // 배치 모드 종료
            ExitPlacementMode();
            
            return true;
        }
        
        /// <summary>
        /// 유닛 배치
        /// </summary>
        private void PlaceUnit(GameObject unit, Tile tile)
        {
            // 유닛 위치 설정
            unit.transform.position = tile.GetWorldPosition();
            
            // 타일 점유 설정
            tile.SetOccupied(unit);
            
            // 해당 리스트에 유닛 추가
            AddUnitToList(unit, tile.tileType, tile.sideType);
            
            // 이벤트 발생
            OnUnitPlaced?.Invoke(unit, tile);
            
            Debug.Log($"유닛 배치 완료: {tile.GetTileInfo()}");
        }
        
        /// <summary>
        /// 유닛을 해당 리스트에 추가
        /// </summary>
        private void AddUnitToList(GameObject unit, Tile.TileType tileType, Tile.SideType sideType)
        {
            if (tileType == Tile.TileType.A)
            {
                if (sideType == Tile.SideType.Player)
                    tileA_PlayerUnits.Add(unit);
                else
                    tileA_EnemyUnits.Add(unit);
            }
            else // TileType.B
            {
                if (sideType == Tile.SideType.Player)
                    tileB_PlayerUnits.Add(unit);
                else
                    tileB_EnemyUnits.Add(unit);
            }
        }
        
        /// <summary>
        /// 유닛 추가 가능 여부
        /// </summary>
        private bool CanAddUnit(Tile.TileType tileType, Tile.SideType sideType)
        {
            int currentCount = GetUnitCount(tileType, sideType);
            return currentCount < MAX_UNITS_PER_SIDE;
        }
        
        /// <summary>
        /// 현재 유닛 수 확인
        /// </summary>
        private int GetUnitCount(Tile.TileType tileType, Tile.SideType sideType)
        {
            if (tileType == Tile.TileType.A)
            {
                return sideType == Tile.SideType.Player ? 
                    tileA_PlayerUnits.Count : tileA_EnemyUnits.Count;
            }
            else
            {
                return sideType == Tile.SideType.Player ? 
                    tileB_PlayerUnits.Count : tileB_EnemyUnits.Count;
            }
        }
        
        /// <summary>
        /// 배치 가능 타일 표시
        /// </summary>
        private void ShowPlacementIndicators(bool show)
        {
            for (int tileType = 0; tileType < 2; tileType++)
            {
                for (int side = 0; side < 2; side++)
                {
                    for (int x = 0; x < GRID_WIDTH; x++)
                    {
                        for (int y = 0; y < GRID_HEIGHT; y++)
                        {
                            Tile tile = tileGrid[tileType, side, x, y];
                            if (tile != null)
                            {
                                bool canShow = show && 
                                    tile.tileType == currentTileType && 
                                    tile.sideType == currentSideType;
                                tile.ShowPlacementIndicator(canShow);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 적 유닛이 쓰러졌을 때 처리
        /// </summary>
        public void HandleEnemyDefeated(GameObject enemy, Tile.TileType tileType)
        {
            // 해당 타일의 적군 리스트에서 제거
            if (tileType == Tile.TileType.A)
            {
                tileA_EnemyUnits.Remove(enemy);
                
                // 대기열에서 다음 적 배치
                if (tileA_EnemyQueue.Count > 0)
                {
                    GameObject nextEnemy = tileA_EnemyQueue.Dequeue();
                    SpawnEnemyOnEmptyTile(nextEnemy, Tile.TileType.A);
                }
            }
            else
            {
                tileB_EnemyUnits.Remove(enemy);
                
                // 대기열에서 다음 적 배치
                if (tileB_EnemyQueue.Count > 0)
                {
                    GameObject nextEnemy = tileB_EnemyQueue.Dequeue();
                    SpawnEnemyOnEmptyTile(nextEnemy, Tile.TileType.B);
                }
            }
            
            // 이벤트 발생
            OnEnemyDefeated?.Invoke(tileType);
        }
        
        /// <summary>
        /// 빈 타일에 적 유닛 스폰
        /// </summary>
        private void SpawnEnemyOnEmptyTile(GameObject enemy, Tile.TileType tileType)
        {
            // 빈 적군 타일 찾기
            List<Tile> emptyTiles = GetEmptyTiles(tileType, Tile.SideType.Enemy);
            
            if (emptyTiles.Count > 0)
            {
                // 랜덤하게 빈 타일 선택
                Tile targetTile = emptyTiles[Random.Range(0, emptyTiles.Count)];
                PlaceUnit(enemy, targetTile);
                
                Debug.Log($"{tileType} 타일에 새로운 적 유닛 배치");
            }
            else
            {
                // 빈 타일이 없으면 다시 대기열에 추가
                if (tileType == Tile.TileType.A)
                    tileA_EnemyQueue.Enqueue(enemy);
                else
                    tileB_EnemyQueue.Enqueue(enemy);
            }
        }
        
        /// <summary>
        /// 빈 타일 목록 가져오기
        /// </summary>
        private List<Tile> GetEmptyTiles(Tile.TileType tileType, Tile.SideType sideType)
        {
            List<Tile> emptyTiles = new List<Tile>();
            int typeIndex = (int)tileType;
            int sideIndex = (int)sideType;
            
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    Tile tile = tileGrid[typeIndex, sideIndex, x, y];
                    if (tile != null && tile.CanPlaceUnit())
                    {
                        emptyTiles.Add(tile);
                    }
                }
            }
            
            return emptyTiles;
        }
        
        /// <summary>
        /// 적 유닛을 대기열에 추가
        /// </summary>
        public void AddEnemyToQueue(GameObject enemy, Tile.TileType tileType)
        {
            if (tileType == Tile.TileType.A)
            {
                tileA_EnemyQueue.Enqueue(enemy);
            }
            else
            {
                tileB_EnemyQueue.Enqueue(enemy);
            }
        }
        
        /// <summary>
        /// 특정 타일 가져오기
        /// </summary>
        public Tile GetTile(Tile.TileType tileType, Tile.SideType sideType, int x, int y)
        {
            if (x < 0 || x >= GRID_WIDTH || y < 0 || y >= GRID_HEIGHT)
                return null;
                
            return tileGrid[(int)tileType, (int)sideType, x, y];
        }
        
        /// <summary>
        /// 모든 타일 리셋
        /// </summary>
        public void ResetAllTiles()
        {
            for (int tileType = 0; tileType < 2; tileType++)
            {
                for (int side = 0; side < 2; side++)
                {
                    for (int x = 0; x < GRID_WIDTH; x++)
                    {
                        for (int y = 0; y < GRID_HEIGHT; y++)
                        {
                            Tile tile = tileGrid[tileType, side, x, y];
                            if (tile != null)
                            {
                                tile.ResetTile();
                            }
                        }
                    }
                }
            }
            
            // 유닛 리스트 초기화
            tileA_PlayerUnits.Clear();
            tileA_EnemyUnits.Clear();
            tileB_PlayerUnits.Clear();
            tileB_EnemyUnits.Clear();
            
            // 대기열 초기화
            tileA_EnemyQueue.Clear();
            tileB_EnemyQueue.Clear();
        }
        
        /// <summary>
        /// 현재 타일 설정
        /// </summary>
        public void SetCurrentTileType(Tile.TileType tileType, Tile.SideType sideType)
        {
            currentTileType = tileType;
            currentSideType = sideType;
            
            if (isPlacementMode)
            {
                ShowPlacementIndicators(true);
            }
        }
        
        /// <summary>
        /// 디버그용 타일 정보 출력
        /// </summary>
        public void DebugPrintTileInfo()
        {
            Debug.Log($"=== 타일 정보 ===");
            Debug.Log($"A타일 - 아군: {tileA_PlayerUnits.Count}명, 적군: {tileA_EnemyUnits.Count}명");
            Debug.Log($"B타일 - 아군: {tileB_PlayerUnits.Count}명, 적군: {tileB_EnemyUnits.Count}명");
            Debug.Log($"A타일 적군 대기열: {tileA_EnemyQueue.Count}명");
            Debug.Log($"B타일 적군 대기열: {tileB_EnemyQueue.Count}명");
        }
    }
}
