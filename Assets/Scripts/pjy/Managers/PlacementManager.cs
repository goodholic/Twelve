using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 캐릭터 배치 및 관리를 담당하는 매니저
/// Missing Script 문제를 자동으로 해결하는 기능 추가
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
            }
            return instance;
        }
    }
    
    [Header("캐릭터 관련")]
    [SerializeField] private Transform characterParent;
    [SerializeField] private Transform opponentCharacterParent;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletParent;
    [SerializeField] private Transform opponentBulletParent;
    
    [Header("UI 패널 참조")]
    public Transform bulletPanel;
    public Transform opponentBulletPanel;
    
    [Header("관리되는 캐릭터 리스트")]
    [SerializeField] private List<Character> playerCharacters = new List<Character>();
    [SerializeField] private List<Character> opponentCharacters = new List<Character>();
    
    [Header("소환 가능 캐릭터 수 제한")]
    [SerializeField] private int maxPlayerCharacters = 50;
    [SerializeField] private int maxOpponentCharacters = 50;
    
    [Header("기타 매니저")]
    private TileManager tileManager;
    private AutoMergeManager autoMergeManager;
    
    [Header("제거 모드")]
    public bool removeMode = false;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        // 매니저 컴포넌트 찾기
        tileManager = GetComponent<TileManager>();
        autoMergeManager = GetComponent<AutoMergeManager>();
    }
    
    private void OnDestroy()
    {
        // Clean up singleton reference
        if (instance == this)
        {
            instance = null;
        }
        
        // Clear lists to prevent memory leaks
        if (playerCharacters != null)
        {
            playerCharacters.Clear();
            playerCharacters = null;
        }
        
        if (opponentCharacters != null)
        {
            opponentCharacters.Clear();
            opponentCharacters = null;
        }
    }
    
    /// <summary>
    /// 플레이어를 위한 캐릭터 배치
    /// </summary>
    public bool PlaceCharacterForPlayer(Character character, Tile targetTile, PlayerController player)
    {
        if (character == null || targetTile == null || player == null)
        {
            Debug.LogError("[PlacementManager] 잘못된 매개변수!");
            return false;
        }
        
        // 타일에 배치 가능한지 확인
        if (!targetTile.CanPlaceCharacter())
        {
            List<Character> occupyingChars = targetTile.GetOccupyingCharacters();
            
            // 같은 캐릭터가 이미 있는 경우 3개까지만 허용
            if (occupyingChars.Count > 0)
            {
                Character first = occupyingChars[0];
                if (first.characterName == character.characterName && first.star == character.star)
                {
                    if (occupyingChars.Count >= 3)
                    {
                        Debug.LogWarning($"[PlacementManager] {targetTile.name}에 이미 3개의 {character.characterName}이 있습니다!");
                        return false;
                    }
                }
                else
                {
                    Debug.LogWarning($"[PlacementManager] {targetTile.name}에 다른 종류의 캐릭터가 있습니다!");
                    return false;
                }
            }
        }
        
        // 타일에 캐릭터 배치
        targetTile.SetOccupyingCharacter(character);
        character.SetCurrentTile(targetTile);
        
        // 적절한 리스트에 추가
        if (player.IsAI)
        {
            if (!opponentCharacters.Contains(character))
                opponentCharacters.Add(character);
        }
        else
        {
            if (!playerCharacters.Contains(character))
                playerCharacters.Add(character);
        }
        
        Debug.Log($"[PlacementManager] {player.PlayerName}가 {character.characterName}을 {targetTile.name}에 배치했습니다!");
        return true;
    }
    
    /// <summary>
    /// 캐릭터를 타일에 소환 (Missing Script 자동 수정 기능 포함)
    /// ★★★ 핵심 로직: 캐릭터 소환 시 모든 필요 컴포넌트를 자동으로 확인하고 추가
    /// - 50마리 제한 확인
    /// - 같은 캐릭터 3개까지 한 타일에 배치 가능
    /// - Missing Component 자동 복구 (Character, SpriteRenderer, Collider2D)
    /// - RectTransform → Transform 자동 변환 (UI 프리팹을 월드 공간용으로)
    /// </summary>
    public Character SummonCharacterOnTile(CharacterData data, Tile tile, bool forceEnemyArea2 = false)
    {
        if (data == null || tile == null)
        {
            Debug.LogError("[PlacementManager] CharacterData 또는 Tile이 null입니다!");
            return null;
        }
        
        // ★ 게임 기획서 요구사항: 각 플레이어는 최대 50마리까지만 소환 가능
        if (!CanSummonCharacter(forceEnemyArea2))
        {
            int currentCount = GetCharacterCount(forceEnemyArea2);
            int maxCount = forceEnemyArea2 ? maxOpponentCharacters : maxPlayerCharacters;
            Debug.LogWarning($"[PlacementManager] 캐릭터 수 제한 도달! 현재: {currentCount}/{maxCount}");
            return null;
        }
        
        // ★ 게임 기획서 요구사항: 같은 캐릭터끼리는 한 타일에 최대 3개까지 배치 가능
        // 다른 종류의 캐릭터는 한 타일에 함께 배치 불가
        if (!tile.CanPlaceCharacter())
        {
            List<Character> occupyingChars = tile.GetOccupyingCharacters();
            
            // 타일에 이미 캐릭터가 있는 경우
            if (occupyingChars.Count > 0)
            {
                Character first = occupyingChars[0];
                // 같은 종류(이름과 별이 모두 같음)인지 확인
                if (first.characterName == data.characterName && first.star == data.star)
                {
                    if (occupyingChars.Count >= 3)
                    {
                        Debug.LogWarning($"[PlacementManager] {tile.name}에 이미 3개의 {data.characterName}이 있습니다!");
                        return null;
                    }
                    // 3개 미만이면 배치 가능
                }
                else
                {
                    // 다른 종류의 캐릭터가 있으면 배치 불가
                    Debug.LogWarning($"[PlacementManager] {tile.name}에 다른 종류의 캐릭터가 있습니다!");
                    return null;
                }
            }
        }
        
        // 캐릭터 생성
        Transform parent = forceEnemyArea2 ? opponentCharacterParent : characterParent;
        GameObject charObj = Instantiate(data.spawnPrefab, tile.transform.position, Quaternion.identity, parent);
        
        // ★ UI 프리팹 호환성: UI용 프리팹(RectTransform)을 월드 공간에서 사용할 수 있도록 자동 변환
        // 이를 통해 UI 인벤토리 프리팹을 게임 필드에서도 재사용 가능
        RectTransform rectTrans = charObj.GetComponent<RectTransform>();
        if (rectTrans != null)
        {
            Debug.Log($"[PlacementManager] {data.characterName} 프리팹에 RectTransform이 있어 Transform으로 변환합니다.");
            Vector3 pos = charObj.transform.position;
            Vector3 scale = charObj.transform.localScale;
            
            // RectTransform 제거 (Unity가 자동으로 일반 Transform으로 대체)
            DestroyImmediate(rectTrans);
            
            // 변환 후 위치/스케일 복원
            charObj.transform.position = pos;
            charObj.transform.localScale = scale;
        }
        
        // Character 컴포넌트 확인 및 자동 추가
        Character character = charObj.GetComponent<Character>();
        if (character == null)
        {
            Debug.LogWarning($"[PlacementManager] {data.characterName}의 프리팹에 Character 컴포넌트가 없어서 추가합니다!");
            character = charObj.AddComponent<Character>();
        }
        
        // SpriteRenderer 확인 및 자동 추가
        SpriteRenderer spriteRenderer = charObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = charObj.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogWarning($"[PlacementManager] {data.characterName}의 프리팹에 SpriteRenderer가 없어서 추가합니다!");
                spriteRenderer = charObj.AddComponent<SpriteRenderer>();
                
                // 스프라이트 설정 (우선순위: characterSprite > frontSprite > buttonIcon)
                if (data.characterSprite != null)
                    spriteRenderer.sprite = data.characterSprite;
                else if (data.frontSprite != null)
                    spriteRenderer.sprite = data.frontSprite;
                else if (data.buttonIcon != null)
                    spriteRenderer.sprite = data.buttonIcon;
                
                // 렌더러 설정
                spriteRenderer.sortingLayerName = "Characters";
                spriteRenderer.sortingOrder = 10;
            }
        }
        
        // Collider2D 확인 및 자동 추가
        Collider2D collider = charObj.GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogWarning($"[PlacementManager] {data.characterName}의 프리팹에 Collider2D가 없어서 추가합니다!");
            BoxCollider2D boxCollider = charObj.AddComponent<BoxCollider2D>();
            boxCollider.size = new Vector2(0.8f, 0.8f);
            boxCollider.isTrigger = true;
        }
        
        // 캐릭터 데이터 설정
        character.characterData = data;
        character.characterName = data.characterName;
        character.cost = data.cost;
        character.star = data.star;
        character.attackPower = data.attackPower;
        character.attackSpeed = data.attackSpeed;
        character.health = data.health;
        character.maxHealth = data.maxHealth;
        character.range = data.range;
        character.attackRange = data.attackRange;
        character.tribe = data.tribe;
        character.race = data.race;
        character.attackShapeType = data.attackShapeType;
        character.level = data.level;
        
        // 타일 및 영역 설정
        character.currentTile = tile;
        character.areaIndex = forceEnemyArea2 ? 2 : 1;
        
        // 타일에 캐릭터 추가
        tile.AddOccupyingCharacter(character);
        
        // 타일 타입 업데이트
        if (tile.IsPlacable() || tile.IsPlaceTile())
        {
            tile.SetTileType(Tile.TileType.Placed);
        }
        else if (tile.IsPlacable2())
        {
            tile.SetTileType(Tile.TileType.Placed2);
        }
        
        // 리스트에 추가
        if (forceEnemyArea2)
        {
            opponentCharacters.Add(character);
        }
        else
        {
            playerCharacters.Add(character);
        }
        
        // ★ 게임 기획서 요구사항: 3라인 시스템 - 타일의 Y좌표에 따라 좌/중/우 경로 자동 할당
        SetCharacterRoute(character, tile, forceEnemyArea2);
        
        // 플레이어 캐릭터인 경우 DraggableCharacter 컴포넌트 추가
        if (!forceEnemyArea2)
        {
            DraggableCharacter draggable = charObj.GetComponent<DraggableCharacter>();
            if (draggable == null)
            {
                draggable = charObj.AddComponent<DraggableCharacter>();
            }
        }
        
        // ★ 자동 합성 체크: 같은 타일에 같은 캐릭터가 3개가 되면 자동으로 합성 시도
        CheckAndMergeOnTile(tile);
        
        Debug.Log($"[PlacementManager] {data.characterName}을(를) {tile.name}에 소환 완료! " +
                  $"(별: {character.star}, 레벨: {character.level}, 공격력: {character.attackPower})");
        
        return character;
    }
    
    /// <summary>
    /// 캐릭터 소환 가능 여부 확인
    /// </summary>
    public bool CanSummonCharacter(bool isOpponent)
    {
        int currentCount = isOpponent ? opponentCharacters.Count : playerCharacters.Count;
        int maxCount = isOpponent ? maxOpponentCharacters : maxPlayerCharacters;
        return currentCount < maxCount;
    }
    
    /// <summary>
    /// 현재 캐릭터 수 반환
    /// </summary>
    public int GetCharacterCount(bool isOpponent)
    {
        return isOpponent ? opponentCharacters.Count : playerCharacters.Count;
    }
    
    /// <summary>
    /// 캐릭터 제거
    /// </summary>
    public void RemoveCharacter(Character character)
    {
        if (character == null) return;
        
        // 타일에서 제거
        if (character.currentTile != null)
        {
            character.currentTile.RemoveOccupyingCharacter(character);
            character.currentTile = null;
        }
        
        // 리스트에서 제거
        if (character.areaIndex == 2)
        {
            if (opponentCharacters != null)
                opponentCharacters.Remove(character);
        }
        else
        {
            if (playerCharacters != null)
                playerCharacters.Remove(character);
        }
        
        // 오브젝트 제거
        if (character.gameObject != null)
            Destroy(character.gameObject);
    }
    
    /// <summary>
    /// 캐릭터 라우트 설정
    /// </summary>
    private void SetCharacterRoute(Character character, Tile tile, bool isOpponent)
    {
        // 타일 위치에 따라 라우트 결정
        Vector3 tilePos = tile.transform.position;
        
        // Y 좌표로 라인 판단 (왼쪽, 중앙, 오른쪽)
        RouteType route;
        if (tilePos.y > 1.5f)
        {
            route = RouteType.Left;
        }
        else if (tilePos.y < -1.5f)
        {
            route = RouteType.Right;
        }
        else
        {
            route = RouteType.Center;
        }
        
        // 웨이포인트 설정은 CharacterMovement 컴포넌트에서 처리
        CharacterMovement movement = character.GetComponent<CharacterMovement>();
        if (movement != null)
        {
            movement.SetRoute(route);
        }
        
        Debug.Log($"[PlacementManager] {character.characterName}의 라우트 설정: {route}");
    }
    
    /// <summary>
    /// 타일에서 자동 합성 체크
    /// </summary>
    private void CheckAndMergeOnTile(Tile tile)
    {
        if (tile == null)
        {
            Debug.LogError("[PlacementManager] CheckAndMergeOnTile called with null tile!");
            return;
        }
        
        if (autoMergeManager == null) return;
        
        List<Character> characters = tile.GetOccupyingCharacters();
        if (characters.Count >= 3)
        {
            // 같은 캐릭터, 같은 별 등급인지 확인
            Character first = characters[0];
            bool canMerge = characters.All(c => 
                c.characterName == first.characterName && 
                c.star == first.star);
            
            if (canMerge && first.star != CharacterStar.ThreeStar)
            {
                Debug.Log($"[PlacementManager] 자동 합성 가능: {first.characterName} {first.star}성 3개");
                
                // 자동 합성 실행
                List<Character> mergeTargets = new List<Character>();
                for (int i = 0; i < 3; i++)
                {
                    mergeTargets.Add(characters[i]);
                }
                
                // AutoMergeManager를 통해 합성
                autoMergeManager.MergeCharacters(mergeTargets.ToArray());
                
                // 합성 후 다시 체크 (연쇄 합성 가능)
                CheckAndMergeOnTile(tile);
            }
        }
    }
    
    /// <summary>
    /// 캐릭터 생성 (월드 좌표) - 간단한 버전
    /// </summary>
    public Character CreateCharacterAtPosition(CharacterData data, Vector3 position, bool isOpponent = false)
    {
        if (data == null || data.spawnPrefab == null)
        {
            Debug.LogError("[PlacementManager] CharacterData 또는 spawnPrefab이 null입니다!");
            return null;
        }
        
        Transform parent = isOpponent ? opponentCharacterParent : characterParent;
        GameObject charObj = Instantiate(data.spawnPrefab, position, Quaternion.identity, parent);
        
        Character character = charObj.GetComponent<Character>();
        if (character == null)
        {
            character = charObj.AddComponent<Character>();
        }
        
        // 캐릭터 초기화
        character.characterName = data.characterName;
        character.cost = data.cost;
        character.star = data.star;
        character.attackPower = data.attackPower;
        character.health = data.health;
        character.maxHealth = data.health;
        character.attackSpeed = data.attackSpeed;
        character.range = data.range;
        character.tribe = data.tribe;
        character.attackShapeType = data.attackShapeType;
        character.areaIndex = isOpponent ? 2 : 1;
        
        return character;
    }
    
    /// <summary>
    /// 총알 생성
    /// </summary>
    public Bullet CreateBullet(Vector3 position, Character owner, object target, bool isOpponent = false)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("[PlacementManager] bulletPrefab이 설정되지 않았습니다!");
            return null;
        }
        
        if (owner == null)
        {
            Debug.LogError("[PlacementManager] CreateBullet called with null owner!");
            return null;
        }
        
        if (target == null)
        {
            Debug.LogError("[PlacementManager] CreateBullet called with null target!");
            return null;
        }
        
        Transform parent = isOpponent ? opponentBulletParent : bulletParent;
        GameObject bulletObj = Instantiate(bulletPrefab, position, Quaternion.identity, parent);
        
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.SetTarget(target, owner.attackPower);
        }
        else
        {
            Debug.LogError("[PlacementManager] Bullet component not found on bulletPrefab!");
            Destroy(bulletObj);
            return null;
        }
        
        return bullet;
    }
    
    /// <summary>
    /// 특정 타일에 있는 캐릭터들 가져오기
    /// </summary>
    public List<Character> GetCharactersOnTile(Tile tile)
    {
        if (tile == null) return new List<Character>();
        return tile.GetOccupyingCharacters();
    }
    
    /// <summary>
    /// 캐릭터의 타일 참조 정리 (CharacterMovement에서 호출)
    /// </summary>
    public void ClearCharacterTileReference(Character character)
    {
        if (character != null && character.currentTile != null)
        {
            character.currentTile.RemoveOccupyingCharacter(character);
            character.currentTile = null;
        }
    }
    
    /// <summary>
    /// 캐릭터가 타일에서 제거되었을 때 호출 (Character, CharacterMovement에서 호출)
    /// </summary>
    public void OnCharacterRemovedFromTile(Tile tile)
    {
        if (tile == null) return;
        
        // 타일이 완전히 비었을 때만 상태 변경
        if (tile.GetOccupyingCharacters().Count == 0)
        {
            // 타일 상태 업데이트
            if (tile.IsPlaceTile())
            {
                tile.SetPlacable();
            }
            else if (tile.IsPlaced2())
            {
                tile.SetPlacable2();
            }
            
            tile.RefreshTileVisual();
            Debug.Log($"[PlacementManager] {tile.name} 타일에서 모든 캐릭터 제거 후 상태 업데이트");
        }
    }
    
    /// <summary>
    /// 유닛 선택 버튼 클릭 처리 (CharacterSelectUI에서 호출)
    /// </summary>
    public void OnClickSelectUnit()
    {
        Debug.Log("[PlacementManager] 유닛 선택 모드 활성화");
        // 유닛 선택 모드 로직 구현
        // 필요시 선택된 캐릭터 인덱스를 저장하거나 UI 상태를 변경
    }
    
    /// <summary>
    /// 자동 배치 버튼 클릭 처리 (CharacterSelectUI에서 호출)
    /// </summary>
    public void OnClickAutoPlace()
    {
        Debug.Log("[PlacementManager] 자동 배치 모드 활성화");
        
        // 빈 배치 가능한 타일 찾기
        List<Tile> availableTiles = new List<Tile>();
        Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
        
        foreach (Tile tile in allTiles)
        {
            if (tile != null && !tile.isRegion2 && (tile.IsPlacable() || tile.IsPlaceTile()) && tile.CanPlaceCharacter())
            {
                availableTiles.Add(tile);
            }
        }
        
        if (availableTiles.Count > 0)
        {
            Debug.Log($"[PlacementManager] 자동 배치 가능한 타일 {availableTiles.Count}개 발견");
        }
        else
        {
            Debug.LogWarning("[PlacementManager] 자동 배치 가능한 타일이 없습니다");
        }
    }

}