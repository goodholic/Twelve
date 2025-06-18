using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 월드 좌표계에서 타일을 자동 생성하는 매니저
/// Canvas 방식에서 World Space 방식으로 전환
/// </summary>
public class WorldTileGenerator : MonoBehaviour
{
    [Header("타일 생성 설정")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Transform tileContainer;
    
    [Header("그리드 설정")]
    [SerializeField] private int gridRows = 7;
    [SerializeField] private int gridColumns = 12;
    [SerializeField] private float tileSize = 1.0f; // 타일 하나의 크기
    [SerializeField] private float tileSpacing = 0.1f; // 타일 간 간격
    
    [Header("월드 위치 설정")]
    [SerializeField] private Vector3 gridStartPosition = new Vector3(-6f, -3f, 0f); // 그리드 시작 위치
    [SerializeField] private bool centerGrid = true; // 그리드를 중앙 정렬할지 여부
    
    [Header("플레이어별 영역 설정")]
    [SerializeField] private int player1Rows = 3; // Player 1 영역 (하단 3줄)
    [SerializeField] private int player2Rows = 3; // Player 2 영역 (상단 3줄)
    [SerializeField] private int neutralRows = 1; // 중립 영역 (중간 1줄)
    
    [Header("타일 타입별 프리팹 (선택사항)")]
    [SerializeField] private GameObject walkableTilePrefab;
    [SerializeField] private GameObject placeableTilePrefab;
    [SerializeField] private GameObject blockedTilePrefab;
    
    [Header("타일 시각화 설정")]
    [SerializeField] private Color player1TileColor = new Color(0.3f, 0.5f, 1f, 0.5f); // 파란색
    [SerializeField] private Color player2TileColor = new Color(1f, 0.5f, 0.3f, 0.5f); // 빨간색
    [SerializeField] private Color neutralTileColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 회색
    
    [Header("디버그 설정")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGridGizmos = true;
    
    // 생성된 타일들을 관리하는 2D 배열
    private Tile[,] tileGrid;
    private List<Tile> allTiles = new List<Tile>();
    
    // 타일 매니저 참조
    private TileManager tileManager;
    
    private void Start()
    {
        tileManager = TileManager.Instance;
        
        if (tilePrefab == null)
        {
            Debug.LogError("[WorldTileGenerator] 타일 프리팹이 설정되지 않았습니다!");
            return;
        }
        
        GenerateTileGrid();
    }
    
    /// <summary>
    /// 타일 그리드 생성
    /// </summary>
    public void GenerateTileGrid()
    {
        // 기존 타일 제거
        ClearExistingTiles();
        
        // 타일 배열 초기화
        tileGrid = new Tile[gridRows, gridColumns];
        
        // 시작 위치 계산 (중앙 정렬 옵션)
        Vector3 startPos = CalculateStartPosition();
        
        // 타일 생성
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridColumns; col++)
            {
                CreateTile(row, col, startPos);
            }
        }
        
        // 타일 타입 설정
        ConfigureTileTypes();
        
        // 타일 매니저에 등록
        RegisterTilesToManager();
        
        Debug.Log($"[WorldTileGenerator] {gridRows}x{gridColumns} 타일 그리드 생성 완료!");
    }
    
    /// <summary>
    /// 개별 타일 생성
    /// </summary>
    private void CreateTile(int row, int col, Vector3 startPos)
    {
        // 타일 위치 계산
        float xPos = startPos.x + (col * (tileSize + tileSpacing));
        float yPos = startPos.y + (row * (tileSize + tileSpacing));
        Vector3 tilePosition = new Vector3(xPos, yPos, 0);
        
        // 타일 프리팹 선택
        GameObject prefabToUse = SelectTilePrefab(row, col);
        
        // 타일 생성
        GameObject tileObj = Instantiate(prefabToUse, tilePosition, Quaternion.identity, tileContainer);
        tileObj.name = $"Tile_{row}_{col}";
        
        // Tile 컴포넌트 설정
        Tile tile = tileObj.GetComponent<Tile>();
        if (tile == null)
        {
            tile = tileObj.AddComponent<Tile>();
        }
        
        // 타일 인덱스 설정
        tile.row = row;
        tile.column = col;
        tile.tileIndex = (row * gridColumns) + col;
        
        // 타일 스케일 설정
        tileObj.transform.localScale = Vector3.one * tileSize;
        
        // 타일 색상 설정
        SetTileColor(tile, row);
        
        // 배열에 저장
        tileGrid[row, col] = tile;
        allTiles.Add(tile);
    }
    
    /// <summary>
    /// 시작 위치 계산 (중앙 정렬 옵션 고려)
    /// </summary>
    private Vector3 CalculateStartPosition()
    {
        if (centerGrid)
        {
            float gridWidth = (gridColumns * tileSize) + ((gridColumns - 1) * tileSpacing);
            float gridHeight = (gridRows * tileSize) + ((gridRows - 1) * tileSpacing);
            
            return new Vector3(
                gridStartPosition.x - (gridWidth / 2f) + (tileSize / 2f),
                gridStartPosition.y - (gridHeight / 2f) + (tileSize / 2f),
                gridStartPosition.z
            );
        }
        
        return gridStartPosition;
    }
    
    /// <summary>
    /// 타일 프리팹 선택
    /// </summary>
    private GameObject SelectTilePrefab(int row, int col)
    {
        // 플레이어 영역에 따라 다른 프리팹 사용 가능
        if (row < player1Rows)
        {
            // Player 1 영역
            return placeableTilePrefab != null ? placeableTilePrefab : tilePrefab;
        }
        else if (row >= gridRows - player2Rows)
        {
            // Player 2 영역
            return placeableTilePrefab != null ? placeableTilePrefab : tilePrefab;
        }
        else
        {
            // 중립 영역
            return walkableTilePrefab != null ? walkableTilePrefab : tilePrefab;
        }
    }
    
    /// <summary>
    /// 타일 색상 설정
    /// </summary>
    private void SetTileColor(Tile tile, int row)
    {
        SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        
        if (row < player1Rows)
        {
            sr.color = player1TileColor;
            tile.isRegion2 = false;
        }
        else if (row >= gridRows - player2Rows)
        {
            sr.color = player2TileColor;
            tile.isRegion2 = true;
        }
        else
        {
            sr.color = neutralTileColor;
        }
    }
    
    /// <summary>
    /// 타일 타입 설정
    /// </summary>
    private void ConfigureTileTypes()
    {
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridColumns; col++)
            {
                Tile tile = tileGrid[row, col];
                if (tile == null) continue;
                
                // 플레이어 영역 설정
                if (row < player1Rows)
                {
                    // Player 1 영역 - 배치 가능
                    tile.tileType = Tile.TileType.Placeable;
                }
                else if (row >= gridRows - player2Rows)
                {
                    // Player 2 영역 - 배치 가능
                    tile.tileType = Tile.TileType.Placeable2;
                }
                else
                {
                    // 중립 영역 - 이동 가능
                    ConfigureNeutralTile(tile, col);
                }
                
                // 타일 시각 업데이트
                tile.UpdateTileVisual();
            }
        }
    }
    
    /// <summary>
    /// 중립 타일 설정 (3라인 시스템)
    /// </summary>
    private void ConfigureNeutralTile(Tile tile, int col)
    {
        // 좌측 라인 (0-3열)
        if (col < 4)
        {
            tile.tileType = Tile.TileType.WalkableLeft;
            tile.belongingRoute = RouteType.Left;
        }
        // 중앙 라인 (4-7열)
        else if (col < 8)
        {
            tile.tileType = Tile.TileType.WalkableCenter;
            tile.belongingRoute = RouteType.Center;
        }
        // 우측 라인 (8-11열)
        else
        {
            tile.tileType = Tile.TileType.WalkableRight;
            tile.belongingRoute = RouteType.Right;
        }
    }
    
    /// <summary>
    /// 타일 매니저에 타일 등록
    /// </summary>
    private void RegisterTilesToManager()
    {
        if (tileManager == null) return;
        
        // 타일 매니저의 수집 메서드 호출
        tileManager.SendMessage("CollectAllTiles", SendMessageOptions.DontRequireReceiver);
        tileManager.SendMessage("CategorizeRouteTiles", SendMessageOptions.DontRequireReceiver);
        tileManager.SendMessage("CategorizeSummonableTiles", SendMessageOptions.DontRequireReceiver);
        tileManager.SendMessage("CategorizeRegionTiles", SendMessageOptions.DontRequireReceiver);
    }
    
    /// <summary>
    /// 기존 타일 제거
    /// </summary>
    private void ClearExistingTiles()
    {
        foreach (Tile tile in allTiles)
        {
            if (tile != null && tile.gameObject != null)
            {
                DestroyImmediate(tile.gameObject);
            }
        }
        
        allTiles.Clear();
        
        // 컨테이너의 모든 자식 제거 (안전장치)
        if (tileContainer != null)
        {
            while (tileContainer.childCount > 0)
            {
                DestroyImmediate(tileContainer.GetChild(0).gameObject);
            }
        }
    }
    
    /// <summary>
    /// 특정 위치의 타일 가져오기
    /// </summary>
    public Tile GetTileAt(int row, int col)
    {
        if (row >= 0 && row < gridRows && col >= 0 && col < gridColumns)
        {
            return tileGrid[row, col];
        }
        return null;
    }
    
    /// <summary>
    /// 월드 좌표로 타일 찾기
    /// </summary>
    public Tile GetTileAtWorldPosition(Vector3 worldPos)
    {
        float minDistance = float.MaxValue;
        Tile closestTile = null;
        
        foreach (Tile tile in allTiles)
        {
            float distance = Vector3.Distance(tile.transform.position, worldPos);
            if (distance < minDistance && distance < tileSize / 2f)
            {
                minDistance = distance;
                closestTile = tile;
            }
        }
        
        return closestTile;
    }
    
    /// <summary>
    /// 디버그용 기즈모 그리기
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGridGizmos) return;
        
        // 그리드 영역 표시
        Vector3 startPos = CalculateStartPosition();
        float gridWidth = (gridColumns * tileSize) + ((gridColumns - 1) * tileSpacing);
        float gridHeight = (gridRows * tileSize) + ((gridRows - 1) * tileSpacing);
        
        // Player 1 영역 (파란색)
        Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.3f);
        float p1Height = (player1Rows * tileSize) + ((player1Rows - 1) * tileSpacing);
        Gizmos.DrawCube(
            new Vector3(startPos.x + gridWidth/2 - tileSize/2, startPos.y + p1Height/2 - tileSize/2, 0),
            new Vector3(gridWidth, p1Height, 0.1f)
        );
        
        // Player 2 영역 (빨간색)
        Gizmos.color = new Color(1f, 0.5f, 0.3f, 0.3f);
        float p2Height = (player2Rows * tileSize) + ((player2Rows - 1) * tileSpacing);
        float p2StartY = startPos.y + gridHeight - p2Height - tileSize/2;
        Gizmos.DrawCube(
            new Vector3(startPos.x + gridWidth/2 - tileSize/2, p2StartY + p2Height/2, 0),
            new Vector3(gridWidth, p2Height, 0.1f)
        );
        
        // 중립 영역 (회색)
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        float neutralHeight = (neutralRows * tileSize) + ((neutralRows - 1) * tileSpacing);
        float neutralStartY = startPos.y + p1Height;
        Gizmos.DrawCube(
            new Vector3(startPos.x + gridWidth/2 - tileSize/2, neutralStartY + neutralHeight/2, 0),
            new Vector3(gridWidth, neutralHeight, 0.1f)
        );
    }
}