using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

namespace TwelveGame.Battle
{
    /// <summary>
    /// Twelve Game의 보드 관리 시스템
    /// 게임 보드의 타일, 캐릭터 배치, 시각화를 담당합니다
    /// </summary>
    public class BoardManager : MonoBehaviour
    {
        private static BoardManager instance;
        public static BoardManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<BoardManager>();
                return instance;
            }
        }

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
    [Header("보드 설정")]
    [Tooltip("A타일들 (6x3=18개, 왼쪽 위에서 아래로 순서)")]
    public Transform[] aTiles = new Transform[18];
    [Tooltip("B타일들 (6x3=18개, 왼쪽 위에서 아래로 순서)")]
    public Transform[] bTiles = new Transform[18];
    
    [Header("타일 색상")]
    public Color normalTileColor = Color.white;
    public Color hoverTileColor = Color.yellow;
    public Color validPlacementColor = Color.green;
    public Color invalidPlacementColor = Color.red;
    public Color attackPreviewColor = new Color(1f, 0.5f, 0f, 0.5f); // 주황색 반투명

    // 보드 타일 배열 (이미 만들어진 타일들을 할당)
    private TileObject[,,] tiles = new TileObject[2, GameManager.BOARD_WIDTH, GameManager.BOARD_HEIGHT];
    
    // 현재 hover 중인 타일
    private TileObject currentHoverTile;
    
    // 공격 범위 미리보기 타일들
    private List<TileObject> attackPreviewTiles = new List<TileObject>();

    void Start()
    {
        AssignExistingTiles();
    }

    void AssignExistingTiles()
    {
        // A타일들 할당 (6x3=18개, 왼쪽 위에서 아래로 순서)
        AssignTilesFromArray(0, aTiles, "A");
        
        // B타일들 할당 (6x3=18개, 왼쪽 위에서 아래로 순서)
        AssignTilesFromArray(1, bTiles, "B");
    }

    void AssignTilesFromArray(int boardIndex, Transform[] tileArray, string boardPrefix)
    {
        int requiredTileCount = GameManager.BOARD_WIDTH * GameManager.BOARD_HEIGHT; // 6 * 3 = 18
        
        // 배열 크기 체크
        if (tileArray.Length != requiredTileCount)
        {
            Debug.LogWarning($"{boardPrefix}타일 배열 크기 오류: {requiredTileCount}개가 필요하지만 {tileArray.Length}개 슬롯이 있습니다.");
        }

        // 타일 할당 순서: 왼쪽 위에서 아래로 (열 우선)
        // A타일 순서: [0]=>(0,0), [1]=>(0,1), [2]=>(0,2) | [3]=>(1,0), [4]=>(1,1), [5]=>(1,2) | ... | [15]=>(5,0), [16]=>(5,1), [17]=>(5,2)
        // B타일 순서: 동일한 방식
        int tileIndex = 0;
        int assignedCount = 0;
        
        for (int x = 0; x < GameManager.BOARD_WIDTH; x++) // 열 (왼쪽에서 오른쪽)
        {
            for (int y = 0; y < GameManager.BOARD_HEIGHT; y++) // 행 (위에서 아래로)
            {
                if (tileIndex < tileArray.Length && tileArray[tileIndex] != null)
                {
                    GameObject tileObj = tileArray[tileIndex].gameObject;
                    
                    // 타일 이름 설정 (옵션)
                    tileObj.name = $"Tile_{boardPrefix}_{x}_{y}";

                    TileObject tile = tileObj.GetComponent<TileObject>();
                    if (tile == null)
                    {
                        tile = tileObj.AddComponent<TileObject>();
                        Debug.Log($"🔧 타일 {tileObj.name}에 TileObject 컴포넌트 추가");
                    }

                    tile.Initialize(boardIndex, x, y, this);
                    tiles[boardIndex, x, y] = tile;
                    
                    assignedCount++;
                }
                else
                {
                    if (tileIndex < tileArray.Length)
                    {
                        Debug.LogWarning($"{boardPrefix}타일 [{tileIndex}] (위치: {x},{y})가 null입니다. Inspector에서 할당해주세요.");
                    }
                    else
                    {
                        Debug.LogWarning($"{boardPrefix}타일 배열 인덱스 {tileIndex}가 범위를 벗어났습니다.");
                    }
                }
                
                tileIndex++;
            }
        }

        Debug.Log($"{boardPrefix}타일 할당 완료: {assignedCount}/{requiredTileCount}개 타일이 순서대로 할당되었습니다.");
        
        // 할당 순서 가이드 출력
        if (assignedCount > 0)
        {
            Debug.Log($"{boardPrefix}타일 순서: 배열[0]=(0,0) → 배열[1]=(0,1) → 배열[2]=(0,2) → 배열[3]=(1,0) → ... → 배열[17]=(5,2)");
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
        Debug.Log($"🖱️ BoardManager.OnTileClick 호출됨: 보드{tile.boardIndex}, 위치({tile.x},{tile.y})");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTileClick(tile.boardIndex, tile.x, tile.y);
        }
        else
        {
            Debug.LogError("❌ GameManager.Instance가 null입니다!");
        }
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
        boardIndex = boardIdx; // 0=A타일, 1=B타일
        x = xPos; // 열 (0~5, 왼쪽에서 오른쪽)
        y = yPos; // 행 (0~2, 위에서 아래로)
        boardManager = manager;
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            
        // 기존 스프라이트가 있으면 유지, 없으면 기본 스프라이트 설정
        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = CreateDefaultTileSprite();
            Debug.Log($"🎨 타일 {name}에 기본 스프라이트 설정");
        }
        else
        {
            Debug.Log($"✅ 타일 {name}의 기존 스프라이트 유지: {spriteRenderer.sprite.name}");
        }
        
        // 콜라이더 추가 및 설정
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
            Debug.Log($"🔳 타일 {name}에 BoxCollider2D 추가됨");
        }
        
        // 콜라이더 크기 설정 (타일 크기에 맞게)
        collider.size = Vector2.one;
        
        Debug.Log($"✅ 타일 {name} 초기화 완료 (보드{boardIndex}, 위치{x},{y}, 콜라이더: {collider != null})");
        
        // 레이어 확인
        Debug.Log($"🎯 타일 {name} 레이어: {gameObject.layer}, 태그: {gameObject.tag}");
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
        Debug.Log($"🖱️ 타일 OnMouseDown 감지됨: {name} (보드{boardIndex}, 위치{x},{y})");
        
        if (boardManager != null)
        {
            boardManager.OnTileClick(this);
        }
        else
        {
            Debug.LogError("❌ boardManager가 null입니다!");
        }
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
        if (character.characterData.characterIcon != null)
        {
            charSprite.sprite = character.characterData.characterIcon;
        }
        else
        {
            // 캐릭터 아이콘이 없을 때 팀 색상으로 기본 스프라이트 생성
            charSprite.sprite = CreateDefaultTileCharacterSprite(character.team);
        }
        charSprite.sortingOrder = 1;
        
        // 팀 표시
        TextMeshPro teamText = characterVisual.AddComponent<TextMeshPro>();
        teamText.text = character.team == GameManager.Team.X ? "X" : "O";
        teamText.fontSize = 2;
        teamText.alignment = TextAlignmentOptions.Center;
        teamText.sortingOrder = 2;
        
        // 🎭 캐릭터 애니메이터 추가
        TwelveGame.Battle.CharacterAnimator animator = characterVisual.AddComponent<TwelveGame.Battle.CharacterAnimator>();
        
        // 🚀 배치 시 Attack 애니메이션 실행
        Debug.Log($"🎬 캐릭터 '{character.characterData.characterName}' 배치 - Attack 애니메이션 시작!");
        animator.PlayAttackAnimation();
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
    
    // 타일에 표시할 캐릭터용 기본 스프라이트 생성
    Sprite CreateDefaultTileCharacterSprite(GameManager.Team team)
    {
        Color color = team == GameManager.Team.X ? new Color(0.2f, 0.6f, 1f) : new Color(1f, 0.4f, 0.2f); // 파란색 vs 주황색
        
        Texture2D texture = new Texture2D(48, 48);
        Color[] pixels = new Color[48 * 48];
        
        // 원형 모양의 스프라이트 생성
        Vector2 center = new Vector2(24, 24);
        float radius = 20f;
        
        for (int x = 0; x < 48; x++)
        {
            for (int y = 0; y < 48; y++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance <= radius)
                {
                    if (distance > radius - 2)
                    {
                        pixels[x + y * 48] = Color.black; // 테두리
                    }
                    else
                    {
                        pixels[x + y * 48] = color; // 내부 색상
                    }
                }
                else
                {
                    pixels[x + y * 48] = Color.clear; // 투명
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 48, 48), new Vector2(0.5f, 0.5f), 48);
    }
}
}