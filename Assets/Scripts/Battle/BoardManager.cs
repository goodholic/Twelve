using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace TwelveGame.Battle
{
    /// <summary>
    /// Twelve Game의 보드 관리 시스템
    /// 게임 보드의 타일, 캐릭터 배치, 시각화를 담당합니다
    /// </summary>
    public class BoardManager : MonoBehaviour
{
    [Header("보드 설정")]
    public GameObject tilePrefab;
    public float tileSize = 1.0f;
    public float boardSpacing = 2.0f; // A와 B 보드 사이 간격

    [Header("타일 색상")]
    public Color normalTileColor = Color.white;
    public Color hoverTileColor = Color.yellow;
    public Color validPlacementColor = Color.green;
    public Color invalidPlacementColor = Color.red;
    public Color attackPreviewColor = new Color(1f, 0.5f, 0f, 0.5f); // 주황색 반투명

    // 보드 타일 배열
    private TileObject[,,] tiles = new TileObject[2, GameManager.BOARD_WIDTH, GameManager.BOARD_HEIGHT];
    
    // 현재 hover 중인 타일
    private TileObject currentHoverTile;
    
    // 공격 범위 미리보기 타일들
    private List<TileObject> attackPreviewTiles = new List<TileObject>();

    void Start()
    {
        CreateBoards();
    }

    void CreateBoards()
    {
        // 보드 A (위쪽) 생성
        Vector3 boardAStartPos = new Vector3(-GameManager.BOARD_WIDTH * tileSize / 2f, boardSpacing, 0);
        CreateBoard(0, boardAStartPos, "Board A");

        // 보드 B (아래쪽) 생성
        Vector3 boardBStartPos = new Vector3(-GameManager.BOARD_WIDTH * tileSize / 2f, -boardSpacing - GameManager.BOARD_HEIGHT * tileSize, 0);
        CreateBoard(1, boardBStartPos, "Board B");
    }

    void CreateBoard(int boardIndex, Vector3 startPos, string boardName)
    {
        GameObject boardParent = new GameObject(boardName);
        boardParent.transform.parent = transform;

        for (int x = 0; x < GameManager.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < GameManager.BOARD_HEIGHT; y++)
            {
                Vector3 tilePos = startPos + new Vector3(x * tileSize, y * tileSize, 0);
                GameObject tileObj = Instantiate(tilePrefab, tilePos, Quaternion.identity, boardParent.transform);
                tileObj.name = $"Tile_{x}_{y}";

                TileObject tile = tileObj.GetComponent<TileObject>();
                if (tile == null)
                    tile = tileObj.AddComponent<TileObject>();

                tile.Initialize(boardIndex, x, y, this);
                tiles[boardIndex, x, y] = tile;
            }
        }
    }

    public void OnTileHover(TileObject tile)
    {
        if (currentHoverTile != null && currentHoverTile != tile)
        {
            currentHoverTile.SetColor(normalTileColor);
        }

        currentHoverTile = tile;
        
        // 캐릭터가 선택되어 있으면 배치 가능 여부 표시
        if (GameManager.Instance.selectedCharacter != null)
        {
            bool canPlace = GameManager.Instance.CanPlaceCharacter(tile.boardIndex, tile.x, tile.y);
            tile.SetColor(canPlace ? validPlacementColor : invalidPlacementColor);

            // 공격 범위 미리보기
            ShowAttackPreview(tile);
        }
        else
        {
            tile.SetColor(hoverTileColor);
        }
    }

    public void OnTileExit(TileObject tile)
    {
        if (currentHoverTile == tile)
        {
            tile.SetColor(normalTileColor);
            currentHoverTile = null;
            
            // 공격 범위 미리보기 제거
            ClearAttackPreview();
        }
    }

    public void OnTileClick(TileObject tile)
    {
        GameManager.Instance.OnTileClick(tile.boardIndex, tile.x, tile.y);
    }

    void ShowAttackPreview(TileObject centerTile)
    {
        ClearAttackPreview();

        if (GameManager.Instance.selectedCharacter == null) return;

        List<Vector2Int> attackPositions = GameManager.Instance.selectedCharacter.GetAttackPositions();
        
        foreach (Vector2Int offset in attackPositions)
        {
            int targetX = centerTile.x + offset.x;
            int targetY = centerTile.y + offset.y;

            // 건너편 보드 공격인 경우
            if (AttackPatternManager.IsCrossBoardAttack(GameManager.Instance.selectedCharacter.attackPattern))
            {
                int targetBoard = 1 - centerTile.boardIndex; // 반대편 보드
                if (IsValidTilePosition(targetBoard, targetX, targetY))
                {
                    TileObject targetTile = tiles[targetBoard, targetX, targetY];
                    targetTile.ShowAttackPreview(attackPreviewColor);
                    attackPreviewTiles.Add(targetTile);
                }
            }
            else
            {
                // 같은 보드 공격
                if (IsValidTilePosition(centerTile.boardIndex, targetX, targetY))
                {
                    TileObject targetTile = tiles[centerTile.boardIndex, targetX, targetY];
                    targetTile.ShowAttackPreview(attackPreviewColor);
                    attackPreviewTiles.Add(targetTile);
                }
            }
        }
    }

    void ClearAttackPreview()
    {
        foreach (TileObject tile in attackPreviewTiles)
        {
            tile.HideAttackPreview();
        }
        attackPreviewTiles.Clear();
    }

    bool IsValidTilePosition(int boardIndex, int x, int y)
    {
        return boardIndex >= 0 && boardIndex < 2 &&
               x >= 0 && x < GameManager.BOARD_WIDTH &&
               y >= 0 && y < GameManager.BOARD_HEIGHT;
    }

    public void RefreshBoard()
    {
        // 보드 상태에 따라 타일 업데이트
        for (int b = 0; b < 2; b++)
        {
            for (int x = 0; x < GameManager.BOARD_WIDTH; x++)
            {
                for (int y = 0; y < GameManager.BOARD_HEIGHT; y++)
                {
                    Character character = GameManager.Instance.boardState[b, x, y];
                    if (character != null)
                    {
                        tiles[b, x, y].SetCharacter(character);
                    }
                    else
                    {
                        tiles[b, x, y].ClearCharacter();
                    }
                }
            }
        }
    }
}

// 타일 오브젝트 컴포넌트
public class TileObject : MonoBehaviour
{
    public int boardIndex;
    public int x;
    public int y;
    
    private BoardManager boardManager;
    private SpriteRenderer spriteRenderer;
    private GameObject characterVisual;
    private GameObject attackPreviewOverlay;
    
    public void Initialize(int boardIdx, int xPos, int yPos, BoardManager manager)
    {
        boardIndex = boardIdx;
        x = xPos;
        y = yPos;
        boardManager = manager;
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            
        // 기본 타일 스프라이트 설정 (나중에 실제 스프라이트로 교체)
        spriteRenderer.sprite = CreateDefaultTileSprite();
        
        // 콜라이더 추가
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
            gameObject.AddComponent<BoxCollider2D>();
    }
    
    void OnMouseEnter()
    {
        boardManager.OnTileHover(this);
    }
    
    void OnMouseExit()
    {
        boardManager.OnTileExit(this);
    }
    
    void OnMouseDown()
    {
        boardManager.OnTileClick(this);
    }
    
    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = color;
    }
    
    public void SetCharacter(Character character)
    {
        ClearCharacter();
        
        // 캐릭터 비주얼 생성
        characterVisual = new GameObject("Character");
        characterVisual.transform.parent = transform;
        characterVisual.transform.localPosition = Vector3.zero;
        
        SpriteRenderer charSprite = characterVisual.AddComponent<SpriteRenderer>();
        charSprite.sprite = character.characterData.characterIcon;
        charSprite.sortingOrder = 1;
        
        // 팀 표시
        TextMeshPro teamText = characterVisual.AddComponent<TextMeshPro>();
        teamText.text = character.team == GameManager.Team.X ? "X" : "O";
        teamText.fontSize = 2;
        teamText.alignment = TextAlignmentOptions.Center;
        teamText.sortingOrder = 2;
    }
    
    public void ClearCharacter()
    {
        if (characterVisual != null)
        {
            Destroy(characterVisual);
            characterVisual = null;
        }
    }
    
    public void ShowAttackPreview(Color color)
    {
        if (attackPreviewOverlay == null)
        {
            attackPreviewOverlay = new GameObject("AttackPreview");
            attackPreviewOverlay.transform.parent = transform;
            attackPreviewOverlay.transform.localPosition = new Vector3(0, 0, -0.1f);
            
            SpriteRenderer overlay = attackPreviewOverlay.AddComponent<SpriteRenderer>();
            overlay.sprite = CreateDefaultTileSprite();
            overlay.sortingOrder = 3;
        }
        
        attackPreviewOverlay.GetComponent<SpriteRenderer>().color = color;
        attackPreviewOverlay.SetActive(true);
    }
    
    public void HideAttackPreview()
    {
        if (attackPreviewOverlay != null)
            attackPreviewOverlay.SetActive(false);
    }
    
    // 임시 타일 스프라이트 생성
    Sprite CreateDefaultTileSprite()
    {
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
    }
}
}