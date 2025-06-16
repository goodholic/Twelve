using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 월드 좌표 기반 캐릭터 배치 관리자
/// 게임 기획서: 타일 기반 소환 시스템
/// ★★★ 50마리 제한 추가
/// ★★★ 같은 캐릭터 3개 자동 합성 추가
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
    
    [Header("캐릭터 소환 제한")]
    [Tooltip("플레이어가 소환할 수 있는 최대 캐릭터 수")]
    public int maxPlayerCharacters = 50;
    [Tooltip("AI가 소환할 수 있는 최대 캐릭터 수")]
    public int maxOpponentCharacters = 50;
    
    [Header("캐릭터/총알 부모 오브젝트")]
    public Transform characterParent;
    public Transform bulletParent;
    public Transform bulletPanel; // 추가: bulletPanel 참조
    
    [Header("상대방 부모 오브젝트")]
    public Transform opponentCharacterParent;
    public Transform opponentCharacterPanel; // 추가
    public Transform opponentBulletParent;
    public Transform opponentBulletPanel; // 추가
    public Transform opponentOurMonsterPanel; // 추가
    public Transform ourMonsterPanel; // 추가
    
    [Header("프리팹")]
    public GameObject characterPrefab;
    public GameObject bulletPrefab;
    
    [Header("제거 모드")]
    public bool removeMode = false;
    
    private TileManager tileManager;
    private AutoMergeManager autoMergeManager;
    
    // 캐릭터 리스트
    private List<Character> playerCharacters = new List<Character>();
    private List<Character> opponentCharacters = new List<Character>();
    
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
        tileManager = TileManager.Instance;
        autoMergeManager = AutoMergeManager.Instance;
        SetupParentObjects();
    }
    
    /// <summary>
    /// 부모 오브젝트 설정
    /// </summary>
    private void SetupParentObjects()
    {
        if (characterParent == null)
            characterParent = new GameObject("CharacterParent").transform;
        if (bulletParent == null)
            bulletParent = new GameObject("BulletParent").transform;
        if (opponentCharacterParent == null)
            opponentCharacterParent = new GameObject("OpponentCharacterParent").transform;
        if (opponentBulletParent == null)
            opponentBulletParent = new GameObject("OpponentBulletParent").transform;
    }
    
    /// <summary>
    /// 캐릭터 수 확인
    /// </summary>
    public int GetCharacterCount(bool isOpponent)
    {
        // null 캐릭터 제거
        if (isOpponent)
        {
            opponentCharacters.RemoveAll(c => c == null);
            return opponentCharacters.Count;
        }
        else
        {
            playerCharacters.RemoveAll(c => c == null);
            return playerCharacters.Count;
        }
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
    /// 캐릭터를 타일에 소환
    /// </summary>
    public Character SummonCharacterOnTile(CharacterData data, Tile tile, bool isOpponent = false)
    {
        if (data == null || tile == null)
        {
            Debug.LogError("[PlacementManager] CharacterData 또는 Tile이 null입니다!");
            return null;
        }
        
        // ★★★ 50마리 제한 체크
        if (!CanSummonCharacter(isOpponent))
        {
            int currentCount = GetCharacterCount(isOpponent);
            int maxCount = isOpponent ? maxOpponentCharacters : maxPlayerCharacters;
            Debug.LogWarning($"[PlacementManager] 캐릭터 수 제한 도달! 현재: {currentCount}/{maxCount}");
            return null;
        }
        
        // 타일 배치 가능 여부 확인
        if (!tile.CanPlaceCharacter())
        {
            Debug.LogWarning($"[PlacementManager] {tile.name}에 캐릭터를 배치할 수 없습니다!");
            return null;
        }
        
        // 캐릭터 생성
        Vector3 spawnPosition = tile.transform.position;
        Character newCharacter = CreateCharacterAtPosition(data, spawnPosition, isOpponent);
        
        if (newCharacter != null)
        {
            // 타일에 캐릭터 추가
            tile.AddOccupyingCharacter(newCharacter);
            newCharacter.currentTile = tile;
            
            // 리스트에 추가
            if (isOpponent)
                opponentCharacters.Add(newCharacter);
            else
                playerCharacters.Add(newCharacter);
            
            Debug.Log($"[PlacementManager] {data.characterName}을(를) {tile.name}에 소환 성공! (현재 {(isOpponent ? "AI" : "플레이어")} 캐릭터: {GetCharacterCount(isOpponent)}/{(isOpponent ? maxOpponentCharacters : maxPlayerCharacters)})");
            
            // 자동 합성 체크
            CheckAndMergeOnTile(tile);
        }
        
        return newCharacter;
    }
    
    /// <summary>
    /// 인덱스로 캐릭터 소환 (Region2AIManager에서 호출)
    /// </summary>
    public bool SummonCharacterOnTile(int characterIndex, Tile tile, bool forceEnemyArea2 = false)
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
        Debug.Log($"[PlacementManager] 제거 모드: {(removeMode ? "ON" : "OFF")}");
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
            RemoveCharacterFromTile(character, character.currentTile);
        }
        
        // 리스트에서 제거
        playerCharacters.Remove(character);
        opponentCharacters.Remove(character);
        
        // 오브젝트 파괴
        Destroy(character.gameObject);
        
        Debug.Log($"[PlacementManager] {character.characterName} 제거 완료");
    }
    
    /// <summary>
    /// 유닛 선택 버튼 클릭 처리
    /// </summary>
    public void OnClickSelectUnit()
    {
        Debug.Log("[PlacementManager] 유닛 선택 모드 활성화");
        // 유닛 선택 모드 로직 구현
    }
    
    /// <summary>
    /// 자동 배치 버튼 클릭 처리
    /// </summary>
    public void OnClickAutoPlace()
    {
        Debug.Log("[PlacementManager] 자동 배치 실행");
        // 자동 배치 로직 구현
    }
}