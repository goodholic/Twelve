using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 월드 좌표 기반 캐릭터 소환 관리자
/// 기획서: 원 버튼 소환 - 미네랄 소모하여 랜덤 캐릭터, 랜덤 위치 소환
/// ★★★ 50마리 제한 추가
/// </summary>
public class SummonManager : MonoBehaviour
{
    private static SummonManager instance;
    public static SummonManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<SummonManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("SummonManager");
                    instance = go.AddComponent<SummonManager>();
                }
            }
            return instance;
        }
    }

    [Header("소환 비용")]
    public int summonCost = 30;
    
    [Header("소환 효과")]
    public GameObject summonEffectPrefab;
    
    private CharacterDatabaseObject characterDatabase;
    private PlacementManager placementManager;
    private TileManager tileManager;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        var coreData = CoreDataManager.Instance;
        if (coreData != null)
        {
            characterDatabase = coreData.allyDatabase;
        }
        
        placementManager = PlacementManager.Instance;
        tileManager = TileManager.Instance;
    }

    /// <summary>
    /// 원 버튼 랜덤 소환 (기획서: 랜덤 캐릭터, 랜덤 위치)
    /// </summary>
    public void OnClickRandomSummon()
    {
        var coreData = CoreDataManager.Instance;
        if (coreData == null)
        {
            Debug.LogError("[SummonManager] CoreDataManager가 없습니다!");
            return;
        }

        bool isHost = coreData.isHost;
        MineralBar mineralBar = isHost ? coreData.region1MineralBar : coreData.region2MineralBar;
        
        // ★★★ 50마리 제한 체크
        if (placementManager != null && !placementManager.CanSummonCharacter(!isHost))
        {
            int currentCount = placementManager.GetCharacterCount(!isHost);
            int maxCount = isHost ? 50 : 50;
            Debug.LogWarning($"[SummonManager] 캐릭터 수 제한 도달! 현재: {currentCount}/{maxCount}");
            return;
        }
        
        // 미네랄 확인
        if (mineralBar == null || mineralBar.GetMineral() < summonCost)
        {
            Debug.LogWarning($"[SummonManager] 미네랄 부족! 필요: {summonCost}, 현재: {mineralBar?.GetMineral() ?? 0}");
            return;
        }
        
        // 빈 타일 찾기
        bool targetRegion2 = !isHost;
        Tile randomTile = FindRandomEmptyTile(targetRegion2);
        
        if (randomTile == null)
        {
            Debug.LogWarning("[SummonManager] 소환 가능한 빈 타일이 없습니다!");
            return;
        }
        
        // 랜덤 캐릭터 선택
        CharacterData randomCharData = GetRandomCharacter();
        if (randomCharData == null)
        {
            Debug.LogError("[SummonManager] 랜덤 캐릭터 데이터를 가져올 수 없습니다!");
            return;
        }
        
        // 미네랄 소모
        mineralBar.UseMineral(summonCost);
        
        // 캐릭터 생성
        Character newChar = CreateCharacterOnTile(randomCharData, randomTile, targetRegion2);
        
        if (newChar != null)
        {
            PlaySummonEffect(randomTile.transform.position);
            Debug.Log($"[SummonManager] 랜덤 소환 성공: {randomCharData.characterName} at {randomTile.name}");
        }
        else
        {
            // 소환 실패 시 미네랄 환불
            mineralBar.AddMineral(summonCost);
            Debug.LogError("[SummonManager] 캐릭터 생성 실패!");
        }
    }

    /// <summary>
    /// 특정 캐릭터를 특정 타일에 소환
    /// </summary>
    public void SummonCharacterAtTile(CharacterData data, Tile tile)
    {
        var coreData = CoreDataManager.Instance;
        if (coreData == null)
        {
            Debug.LogError("[SummonManager] CoreDataManager가 없습니다!");
            return;
        }
        
        // ★★★ 50마리 제한 체크
        bool isHost = coreData.isHost;
        if (placementManager != null && !placementManager.CanSummonCharacter(!isHost))
        {
            int currentCount = placementManager.GetCharacterCount(!isHost);
            int maxCount = 50;
            Debug.LogWarning($"[SummonManager] 캐릭터 수 제한 도달! 현재: {currentCount}/{maxCount}");
            return;
        }
        
        // 미네랄 확인
        MineralBar mineralBar = isHost ? coreData.region1MineralBar : coreData.region2MineralBar;
        
        if (mineralBar == null || mineralBar.GetMineral() < summonCost)
        {
            Debug.LogWarning($"[SummonManager] 미네랄 부족! 필요: {summonCost}, 현재: {mineralBar?.GetMineral() ?? 0}");
            return;
        }
        
        // 미네랄 소모
        mineralBar.UseMineral(summonCost);
        
        // 캐릭터 생성
        Character newChar = CreateCharacterOnTile(data, tile, !isHost);
        
        if (newChar != null)
        {
            PlaySummonEffect(tile.transform.position);
            Debug.Log($"[SummonManager] [{coreData.currentCharacterIndex}] {data.characterName} 소환 성공");
        }
    }

    /// <summary>
    /// ★★★ 특정 캐릭터를 타일에 소환
    /// </summary>
    public bool SummonCharacterOnTile(int summonIndex, Tile tile, bool forceEnemyArea2 = false)
    {
        var coreData = CoreDataManager.Instance;
        if (coreData == null)
        {
            Debug.LogError("[SummonManager] CoreDataManager가 없습니다!");
            return false;
        }
        
        // ★★★ 50마리 제한 체크
        if (placementManager != null && !placementManager.CanSummonCharacter(forceEnemyArea2))
        {
            int currentCount = placementManager.GetCharacterCount(forceEnemyArea2);
            int maxCount = 50;
            Debug.LogWarning($"[SummonManager] 캐릭터 수 제한 도달! 현재: {currentCount}/{maxCount}");
            return false;
        }
        
        if (characterDatabase == null || characterDatabase.currentRegisteredCharacters == null)
        {
            Debug.LogError("[SummonManager] characterDatabase가 없습니다!");
            return false;
        }
        
        if (summonIndex < 0 || summonIndex >= characterDatabase.currentRegisteredCharacters.Length)
        {
            Debug.LogError($"[SummonManager] 잘못된 summonIndex: {summonIndex}");
            return false;
        }
        
        CharacterData data = characterDatabase.currentRegisteredCharacters[summonIndex];
        if (data == null || data.spawnPrefab == null)
        {
            Debug.LogError($"[SummonManager] [{summonIndex}] 캐릭터 데이터가 없습니다!");
            return false;
        }
        
        // 캐릭터 생성
        Character newChar = CreateCharacterOnTile(data, tile, forceEnemyArea2);
        
        if (newChar != null)
        {
            PlaySummonEffect(tile.transform.position);
            Debug.Log($"[SummonManager] [{summonIndex}] {data.characterName} 소환 성공 at {tile.name}");
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 캐릭터를 타일에 생성
    /// </summary>
    private Character CreateCharacterOnTile(CharacterData data, Tile tile, bool isOpponent)
    {
        if (data == null || tile == null || placementManager == null) return null;
        
        Vector3 position = tile.transform.position;
        Character newChar = placementManager.CreateCharacterAtPosition(data, position, isOpponent);
        
        if (newChar != null)
        {
            // 타일에 캐릭터 추가
            tile.AddOccupyingCharacter(newChar);
            newChar.currentTile = tile;
            
            // 지역별 설정
            newChar.areaIndex = isOpponent ? 2 : 1;
            
            // 웨이포인트 설정 (필요한 경우)
            SetCharacterWaypoints(newChar, tile);
        }
        
        return newChar;
    }

    /// <summary>
    /// 캐릭터에 웨이포인트 설정
    /// </summary>
    private void SetCharacterWaypoints(Character character, Tile tile)
    {
        WaypointManager waypointManager = FindFirstObjectByType<WaypointManager>();
        if (waypointManager == null) return;
        
        // 타일 타입에 따라 웨이포인트 결정
        Transform[] waypoints = null;
        
        if (tile.IsWalkableLeft() || tile.IsWalkable2Left())
        {
            waypoints = character.areaIndex == 1 
                ? ConvertToTransforms(waypointManager.region1RouteLeft_Waypoints)
                : ConvertToTransforms(waypointManager.region2RouteLeft_Waypoints);
        }
        else if (tile.IsWalkableCenter() || tile.IsWalkable2Center())
        {
            waypoints = character.areaIndex == 1 
                ? ConvertToTransforms(waypointManager.region1RouteCenter_Waypoints)
                : ConvertToTransforms(waypointManager.region2RouteCenter_Waypoints);
        }
        else if (tile.IsWalkableRight() || tile.IsWalkable2Right())
        {
            waypoints = character.areaIndex == 1 
                ? ConvertToTransforms(waypointManager.region1RouteRight_Waypoints)
                : ConvertToTransforms(waypointManager.region2RouteRight_Waypoints);
        }
        
        if (waypoints != null && waypoints.Length > 0)
        {
            character.pathWaypoints = waypoints;
            character.currentWaypointIndex = 0;
            character.maxWaypointIndex = waypoints.Length - 1;
            
            // CharacterMovement 컴포넌트에도 설정
            CharacterMovement movement = character.GetComponent<CharacterMovement>();
            if (movement != null)
            {
                movement.SetWaypoints(waypoints, 0);
                movement.StartMoving();
            }
        }
    }

    /// <summary>
    /// 자동 배치 메서드 (PlacementManager에서 호출)
    /// </summary>
    public void OnClickAutoPlace()
    {
        var coreData = CoreDataManager.Instance;
        if (coreData == null)
        {
            Debug.LogError("[SummonManager] CoreDataManager가 없습니다!");
            return;
        }
        
        if (coreData.currentCharacterIndex < 0)
        {
            Debug.LogWarning("[SummonManager] 캐릭터를 먼저 선택하세요!");
            return;
        }
        
        // ★★★ 50마리 제한 체크
        bool isHost = coreData.isHost;
        if (placementManager != null && !placementManager.CanSummonCharacter(!isHost))
        {
            int currentCount = placementManager.GetCharacterCount(!isHost);
            int maxCount = 50;
            Debug.LogWarning($"[SummonManager] 캐릭터 수 제한 도달! 현재: {currentCount}/{maxCount}");
            return;
        }
        
        CharacterData selectedChar = characterDatabase.currentRegisteredCharacters[coreData.currentCharacterIndex];
        if (selectedChar == null)
        {
            Debug.LogError($"[SummonManager] 선택된 캐릭터 데이터[{coreData.currentCharacterIndex}]가 null입니다!");
            return;
        }
        
        // 미네랄 확인
        MineralBar mineralBar = isHost ? coreData.region1MineralBar : coreData.region2MineralBar;
        if (mineralBar == null || mineralBar.GetMineral() < selectedChar.cost)
        {
            Debug.LogWarning($"[SummonManager] 미네랄 부족! 필요: {selectedChar.cost}, 현재: {mineralBar?.GetMineral() ?? 0}");
            return;
        }
        
        // 빈 타일 찾기
        bool targetRegion2 = !isHost;
        Tile randomTile = FindRandomEmptyTile(targetRegion2);
        
        if (randomTile == null)
        {
            Debug.LogWarning("[SummonManager] 소환 가능한 빈 타일이 없습니다!");
            return;
        }
        
        // 미네랄 소모
        mineralBar.UseMineral(selectedChar.cost);
        
        // 캐릭터 생성
        Character newChar = CreateCharacterOnTile(selectedChar, randomTile, targetRegion2);
        
        if (newChar != null)
        {
            PlaySummonEffect(randomTile.transform.position);
            Debug.Log($"[SummonManager] 자동 배치 성공: {selectedChar.characterName} at {randomTile.name}");
        }
        else
        {
            // 소환 실패 시 미네랄 환불
            mineralBar.AddMineral(selectedChar.cost);
            Debug.LogError("[SummonManager] 캐릭터 생성 실패!");
        }
    }

    /// <summary>
    /// 랜덤 빈 타일 찾기
    /// </summary>
    private Tile FindRandomEmptyTile(bool isRegion2)
    {
        if (tileManager == null) return null;
        
        List<Tile> availableTiles = new List<Tile>();
        List<Tile> targetTiles = isRegion2 ? tileManager.aiSummonableTiles : tileManager.playerSummonableTiles;
        
        foreach (var tile in targetTiles)
        {
            if (tile != null && tile.CanPlaceCharacter())
            {
                availableTiles.Add(tile);
            }
        }
        
        if (availableTiles.Count == 0)
        {
            Debug.LogWarning($"[SummonManager] {(isRegion2 ? "AI" : "플레이어")} 지역에 빈 타일이 없습니다!");
            return null;
        }
        
        return availableTiles[UnityEngine.Random.Range(0, availableTiles.Count)];
    }

    /// <summary>
    /// 랜덤 캐릭터 선택
    /// </summary>
    private CharacterData GetRandomCharacter()
    {
        if (characterDatabase == null || characterDatabase.currentRegisteredCharacters == null)
            return null;
            
        List<CharacterData> validCharacters = new List<CharacterData>();
        
        foreach (var character in characterDatabase.currentRegisteredCharacters)
        {
            if (character != null && character.spawnPrefab != null)
            {
                validCharacters.Add(character);
            }
        }
        
        if (validCharacters.Count == 0)
            return null;
            
        return validCharacters[UnityEngine.Random.Range(0, validCharacters.Count)];
    }

    /// <summary>
    /// GameObject 배열을 Transform 배열로 변환
    /// </summary>
    private Transform[] ConvertToTransforms(GameObject[] gameObjects)
    {
        if (gameObjects == null) return null;
        
        Transform[] transforms = new Transform[gameObjects.Length];
        for (int i = 0; i < gameObjects.Length; i++)
        {
            transforms[i] = gameObjects[i] != null ? gameObjects[i].transform : null;
        }
        return transforms;
    }

    /// <summary>
    /// 소환 효과 재생
    /// </summary>
    private void PlaySummonEffect(Vector3 position)
    {
        if (summonEffectPrefab != null)
        {
            GameObject effect = Instantiate(summonEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    /// <summary>
    /// enemyDatabase에서 캐릭터 인덱스 찾기
    /// </summary>
    public int FindCharacterIndexInEnemyDatabase(CharacterData character)
    {
        var coreData = CoreDataManager.Instance;
        if (coreData == null) return -1;
        
        if (coreData.enemyDatabase == null || coreData.enemyDatabase.currentRegisteredCharacters == null)
        {
            Debug.LogWarning("[SummonManager] enemyDatabase가 null이거나 비어있습니다.");
            return -1;
        }

        for (int i = 0; i < coreData.enemyDatabase.currentRegisteredCharacters.Length; i++)
        {
            CharacterData data = coreData.enemyDatabase.currentRegisteredCharacters[i];
            if (data != null && data.characterName == character.characterName)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// AI용 랜덤 소환
    /// </summary>
    public void AIRandomSummon(int count = 1)
    {
        // ★★★ 50마리 제한 체크
        if (placementManager != null && !placementManager.CanSummonCharacter(true))
        {
            int currentCount = placementManager.GetCharacterCount(true);
            int maxCount = 50;
            Debug.LogWarning($"[SummonManager] AI 캐릭터 수 제한 도달! 현재: {currentCount}/{maxCount}");
            return;
        }
        
        for (int i = 0; i < count; i++)
        {
            Tile randomTile = FindRandomEmptyTile(true); // 지역2
            if (randomTile == null) continue;
            
            CharacterData randomChar = GetRandomCharacter();
            if (randomChar == null) continue;
            
            Character newChar = CreateCharacterOnTile(randomChar, randomTile, true);
            if (newChar != null)
            {
                PlaySummonEffect(randomTile.transform.position);
                Debug.Log($"[SummonManager] AI가 {randomChar.characterName}을(를) {randomTile.name}에 소환");
            }
        }
    }

    /// <summary>
    /// 디버그: 모든 소환 가능한 타일 표시
    /// </summary>
    public void DebugShowSummonableTiles()
    {
        if (tileManager == null) return;
        
        Debug.Log("[SummonManager] === 플레이어 소환 가능 타일 ===");
        foreach (var tile in tileManager.playerSummonableTiles)
        {
            if (tile.CanPlaceCharacter())
            {
                Debug.Log($"  - {tile.name}: 배치 가능");
            }
        }
        
        Debug.Log("[SummonManager] === AI 소환 가능 타일 ===");
        foreach (var tile in tileManager.aiSummonableTiles)
        {
            if (tile.CanPlaceCharacter())
            {
                Debug.Log($"  - {tile.name}: 배치 가능");
            }
        }
    }

    /// <summary>
    /// 캐릭터를 특정 타일에 배치 (Tile.cs에서 호출)
    /// </summary>
    public Character PlaceCharacterOnTile(CharacterData data, Tile tile, bool isOpponent)
    {
        if (data == null || tile == null)
        {
            Debug.LogError("[SummonManager] PlaceCharacterOnTile: data 또는 tile이 null입니다!");
            return null;
        }
        
        // ★★★ 50마리 제한 체크
        if (placementManager != null && !placementManager.CanSummonCharacter(isOpponent))
        {
            int currentCount = placementManager.GetCharacterCount(isOpponent);
            int maxCount = 50;
            Debug.LogWarning($"[SummonManager] 캐릭터 수 제한 도달! 현재: {currentCount}/{maxCount}");
            return null;
        }
        
        // 캐릭터 생성
        Character newChar = CreateCharacterOnTile(data, tile, isOpponent);
        
        if (newChar != null)
        {
            PlaySummonEffect(tile.transform.position);
            Debug.Log($"[SummonManager] {data.characterName} 배치 성공 at {tile.name}");
        }
        
        return newChar;
    }
}