using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 캐릭터 배치 및 관리를 담당하는 매니저
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
    
    /// <summary>
    /// 캐릭터를 타일에 소환
    /// </summary>
    public Character SummonCharacterOnTile(CharacterData data, Tile tile, bool forceEnemyArea2 = false)
    {
        if (data == null || tile == null)
        {
            Debug.LogError("[PlacementManager] CharacterData 또는 Tile이 null입니다!");
            return null;
        }
        
        // 50마리 제한 체크
        if (!CanSummonCharacter(forceEnemyArea2))
        {
            int currentCount = GetCharacterCount(forceEnemyArea2);
            int maxCount = forceEnemyArea2 ? maxOpponentCharacters : maxPlayerCharacters;
            Debug.LogWarning($"[PlacementManager] 캐릭터 수 제한 도달! 현재: {currentCount}/{maxCount}");
            return null;
        }
        
        // 타일에 캐릭터가 배치 가능한지 확인
        if (!tile.CanPlaceCharacter())
        {
            List<Character> occupyingChars = tile.GetOccupyingCharacters();
            
            // 같은 캐릭터가 이미 있는 경우 3개까지만 허용
            if (occupyingChars.Count > 0)
            {
                Character first = occupyingChars[0];
                if (first.characterName == data.characterName && first.star == data.star)
                {
                    if (occupyingChars.Count >= 3)
                    {
                        Debug.LogWarning($"[PlacementManager] {tile.name}에 이미 3개의 {data.characterName}이 있습니다!");
                        return null;
                    }
                }
                else
                {
                    Debug.LogWarning($"[PlacementManager] {tile.name}에 다른 종류의 캐릭터가 있습니다!");
                    return null;
                }
            }
        }
        
        // 캐릭터 생성
        Transform parent = forceEnemyArea2 ? opponentCharacterParent : characterParent;
        GameObject charObj = Instantiate(data.spawnPrefab, tile.transform.position, Quaternion.identity, parent);
        
        Character character = charObj.GetComponent<Character>();
        if (character == null)
        {
            Debug.LogError($"[PlacementManager] {data.characterName}의 프리팹에 Character 컴포넌트가 없습니다!");
            Destroy(charObj);
            return null;
        }
        
        // 캐릭터 데이터 설정
        character.characterData = data;
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
        
        // ★★★ 웨이포인트 설정 - 타일 위치에 따라 라우트 결정
        SetCharacterRoute(character, tile, forceEnemyArea2);
        
        // 타일에서 자동 합성 체크
        CheckAndMergeOnTile(tile);
        
        Debug.Log($"[PlacementManager] {data.characterName}을(를) {tile.name}에 소환 완료!");
        
        return character;
    }
    
    /// <summary>
    /// ★★★ 캐릭터의 라우트를 설정하는 메서드
    /// </summary>
    private void SetCharacterRoute(Character character, Tile tile, bool isRegion2)
    {
        RouteManager routeManager = RouteManager.Instance;
        if (routeManager == null)
        {
            Debug.LogWarning("[PlacementManager] RouteManager를 찾을 수 없습니다!");
            return;
        }
        
        // 웨이브 스포너 찾기
        WaveSpawner waveSpawner = isRegion2 ? null : FindFirstObjectByType<WaveSpawner>();
        WaveSpawnerRegion2 waveSpawnerRegion2 = isRegion2 ? FindFirstObjectByType<WaveSpawnerRegion2>() : null;
        
        if (!isRegion2 && waveSpawner == null)
        {
            Debug.LogWarning("[PlacementManager] WaveSpawner를 찾을 수 없습니다!");
            return;
        }
        else if (isRegion2 && waveSpawnerRegion2 == null)
        {
            Debug.LogWarning("[PlacementManager] WaveSpawnerRegion2를 찾을 수 없습니다!");
            return;
        }
        
        // 타일 위치에 따라 라우트 결정
        RouteType selectedRoute;
        if (isRegion2)
        {
            selectedRoute = routeManager.DetermineRouteFromTile(tile, waveSpawnerRegion2);
        }
        else
        {
            selectedRoute = routeManager.DetermineRouteFromTile(tile, waveSpawner);
        }
        
        Debug.Log($"[PlacementManager] {character.characterName}의 라우트: {selectedRoute} (타일: {tile.name})");
        
        // 웨이포인트 가져오기
        Transform[] waypoints = null;
        
        if (isRegion2)
        {
            waypoints = routeManager.GetWaypointsForRoute(waveSpawnerRegion2, selectedRoute);
        }
        else
        {
            waypoints = routeManager.GetWaypointsForRoute(waveSpawner, selectedRoute);
        }
        
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"[PlacementManager] {selectedRoute} 루트의 웨이포인트를 찾을 수 없습니다!");
            return;
        }
        
        // CharacterMovement 컴포넌트 찾기
        CharacterMovement movement = character.GetComponent<CharacterMovement>();
        if (movement == null)
        {
            Debug.LogWarning($"[PlacementManager] {character.characterName}에 CharacterMovement 컴포넌트가 없습니다!");
            return;
        }
        
        // 웨이포인트 설정
        movement.SetWaypoints(waypoints, 0);
        character.selectedRoute = (int)selectedRoute;
        
        // 캐릭터 스탯 초기화
        CharacterStats stats = character.GetComponent<CharacterStats>();
        if (stats != null)
        {
            movement.Initialize(character, stats);
        }
        
        // 이동 시작
        movement.StartMoving();
        
        Debug.Log($"[PlacementManager] {character.characterName}에게 {waypoints.Length}개의 웨이포인트 설정 완료");
    }
    
    /// <summary>
    /// 캐릭터 소환 가능 여부 확인
    /// </summary>
    public bool CanSummonCharacter(bool isOpponent)
    {
        int currentCount = GetCharacterCount(isOpponent);
        int maxCount = isOpponent ? maxOpponentCharacters : maxPlayerCharacters;
        return currentCount < maxCount;
    }
    
    /// <summary>
    /// 현재 캐릭터 수 확인
    /// </summary>
    public int GetCharacterCount(bool isOpponent)
    {
        return isOpponent ? opponentCharacters.Count : playerCharacters.Count;
    }
    
    /// <summary>
    /// 캐릭터를 특정 인덱스로 타일에 소환
    /// </summary>
    public bool SummonCharacterByIndex(int characterIndex, Tile tile, bool forceEnemyArea2 = false)
    {
        if (CoreDataManager.Instance == null || CoreDataManager.Instance.characterDatabase == null)
        {
            Debug.LogError("[PlacementManager] CoreDataManager 또는 characterDatabase가 null입니다!");
            return false;
        }
        
        CharacterData[] characters = CoreDataManager.Instance.characterDatabase.currentRegisteredCharacters;
        if (characterIndex < 0 || characterIndex >= characters.Length)
        {
            Debug.LogError($"[PlacementManager] 잘못된 캐릭터 인덱스: {characterIndex}");
            return false;
        }
        
        CharacterData data = characters[characterIndex];
        if (data == null)
        {
            Debug.LogError($"[PlacementManager] 캐릭터 데이터[{characterIndex}]가 null입니다!");
            return false;
        }
        
        Character newChar = SummonCharacterOnTile(data, tile, forceEnemyArea2);
        return newChar != null;
    }
    
    /// <summary>
    /// 캐릭터를 타일에서 제거할 때 참조 정리
    /// </summary>
    public void ClearCharacterTileReference(Character character)
    {
        if (character == null) return;
        
        if (character.currentTile != null)
        {
            character.currentTile.RemoveOccupyingCharacter(character);
            character.currentTile = null;
        }
        
        // 리스트에서 제거
        playerCharacters.Remove(character);
        opponentCharacters.Remove(character);
    }
    
    /// <summary>
    /// 캐릭터가 타일에서 제거되었을 때 호출
    /// </summary>
    public void OnCharacterRemovedFromTile(Tile tile)
    {
        if (tile == null) return;
        
        // 타일 상태 업데이트는 TileManager에서 처리
        if (tileManager != null)
        {
            tileManager.OnCharacterRemovedFromTile(tile);
        }
    }
    
    /// <summary>
    /// ★★★ 타일의 자동 합성 체크
    /// </summary>
    private void CheckAndMergeOnTile(Tile tile)
    {
        if (tile == null || autoMergeManager == null) return;
        
        List<Character> characters = tile.GetOccupyingCharacters();
        
        // 같은 종류의 캐릭터가 3개 있는지 확인
        if (characters.Count >= 3)
        {
            Character first = characters[0];
            
            // 모두 같은 종류인지 확인
            bool allSame = true;
            foreach (var c in characters)
            {
                if (c.characterName != first.characterName || c.star != first.star)
                {
                    allSame = false;
                    break;
                }
            }
            
            if (allSame && first.star < CharacterStar.ThreeStar) // 3성은 합성 불가
            {
                Debug.Log($"[PlacementManager] {tile.name}에서 자동 합성 조건 충족! {first.characterName} {first.star}성 3개");
                
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
    /// 캐릭터 생성 (월드 좌표)
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
        
        Transform parent = isOpponent ? opponentBulletParent : bulletParent;
        GameObject bulletObj = Instantiate(bulletPrefab, position, Quaternion.identity, parent);
        
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet == null)
        {
            bullet = bulletObj.AddComponent<Bullet>();
        }
        
        // 총알 초기화 (Bullet 클래스의 실제 메서드에 맞게 수정)
        if (target is MonoBehaviour targetMono)
        {
            bullet.Initialize(owner.attackPower, 10f, targetMono.gameObject, owner.areaIndex, false);
        }
        
        return bullet;
    }
    
    /// <summary>
    /// 타일에서 캐릭터 제거
    /// </summary>
    public void RemoveCharacterFromTile(Character character, Tile tile)
    {
        if (character == null || tile == null) return;
        
        tile.RemoveOccupyingCharacter(character);
        
        // 타일 상태 업데이트
        if (tile.GetOccupyingCharacters().Count == 0)
        {
            if (tile.IsPlaceTile())
            {
                tile.SetTileType(Tile.TileType.Placeable);
            }
            else if (tile.IsPlaced2())
            {
                tile.SetTileType(Tile.TileType.Placeable2);
            }
        }
        
        Debug.Log($"[PlacementManager] {character.characterName}을(를) {tile.name}에서 제거");
    }
    
    /// <summary>
    /// 특정 타일로 캐릭터 이동
    /// </summary>
    public void MoveCharacterToTile(Character character, Tile newTile)
    {
        if (character == null || newTile == null) return;
        
        // 기존 타일에서 제거
        if (character.currentTile != null)
        {
            RemoveCharacterFromTile(character, character.currentTile);
        }
        
        // 새 타일에 추가
        if (newTile.CanPlaceCharacter(character))
        {
            newTile.AddOccupyingCharacter(character);
            character.currentTile = newTile;
            character.SetPositionToTile(newTile);
            
            Debug.Log($"[PlacementManager] {character.characterName}을(를) {newTile.name}으로 이동");
            
            // 이동 후 합성 체크
            CheckAndMergeOnTile(newTile);
        }
        else
        {
            Debug.LogWarning($"[PlacementManager] {newTile.name}에 {character.characterName}을(를) 배치할 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 제거 모드 토글
    /// </summary>
    public void ToggleRemoveMode()
    {
        removeMode = !removeMode;
        Debug.Log($"[PlacementManager] 제거 모드: {(removeMode ? "활성화" : "비활성화")}");
    }
    
    /// <summary>
    /// 제거 모드 상태 가져오기
    /// </summary>
    public bool IsRemoveMode()
    {
        return removeMode;
    }
    
    /// <summary>
    /// 캐릭터 제거
    /// </summary>
    public void RemoveCharacter(Character character)
    {
        if (character == null) return;
        
        // 타일에서 제거
        ClearCharacterTileReference(character);
        
        // 오브젝트 파괴
        Destroy(character.gameObject);
        
        Debug.Log($"[PlacementManager] {character.characterName} 제거 완료");
    }
    
    /// <summary>
    /// 모든 캐릭터 가져오기
    /// </summary>
    public List<Character> GetAllCharacters(bool includeOpponents = true)
    {
        List<Character> allCharacters = new List<Character>(playerCharacters);
        
        if (includeOpponents)
        {
            allCharacters.AddRange(opponentCharacters);
        }
        
        return allCharacters;
    }
    
    /// <summary>
    /// 특정 지역의 캐릭터만 가져오기
    /// </summary>
    public List<Character> GetCharactersInArea(int areaIndex)
    {
        List<Character> areaCharacters = new List<Character>();
        
        foreach (var character in playerCharacters)
        {
            if (character.areaIndex == areaIndex)
            {
                areaCharacters.Add(character);
            }
        }
        
        foreach (var character in opponentCharacters)
        {
            if (character.areaIndex == areaIndex)
            {
                areaCharacters.Add(character);
            }
        }
        
        return areaCharacters;
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
        Debug.Log("[PlacementManager] 자동 배치 실행");
        
        // 자동 배치 로직 구현
        // 예: 현재 선택된 캐릭터를 적절한 빈 타일에 자동으로 배치
        
        // 빈 배치 가능한 타일 찾기
        Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
        List<Tile> availableTiles = new List<Tile>();
        
        foreach (var tile in allTiles)
        {
            if (tile != null && !tile.isRegion2 && tile.CanPlaceCharacter())
            {
                if (tile.IsPlaceableType())
                {
                    availableTiles.Add(tile);
                }
            }
        }
        
        if (availableTiles.Count > 0)
        {
            Debug.Log($"[PlacementManager] 자동 배치 가능한 타일 {availableTiles.Count}개 발견");
            // 실제 자동 배치 로직은 필요에 따라 CharacterSelectUI와 연동하여 구현
        }
        else
        {
            Debug.LogWarning("[PlacementManager] 자동 배치 가능한 타일이 없습니다");
        }
    }
}