using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 월드 좌표 기반 캐릭터 배치 관리자
/// 캐릭터 배치, 합성, 제거 등 전반적인 관리
/// ★★★ 수정: 같은 캐릭터끼리는 한 타일에 최대 3개까지 배치 가능
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

    [Header("Character Database")]
    public CharacterDatabase characterDatabase;

    [Header("World Parents")]
    public Transform characterParent;
    public Transform bulletParent;
    public Transform monsterParent;
    public Transform opponentCharacterParent;
    public Transform opponentBulletParent;
    public Transform opponentMonsterParent;

    [Header("Camera")]
    public Camera mainCamera;

    [Header("제거 모드")]
    public bool removeMode = false;

    [Header("Prefabs")]
    public GameObject characterPrefab;
    public GameObject bulletPrefab;

    private SummonManager summonManager;
    private TileManager tileManager;
    private MergeManager mergeManager;
    
    // 현재 선택된 캐릭터 인덱스 추적
    private int currentCharacterIndex = -1;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // 카메라 설정
        SetupCamera();
    }

    private void Start()
    {
        // CoreDataManager에서 참조 가져오기
        var coreData = CoreDataManager.Instance;
        if (coreData != null)
        {
            if (characterDatabase == null) characterDatabase = coreData.characterDatabase;
            if (mainCamera == null) mainCamera = coreData.mainCamera;
        }

        // 월드 공간 부모 오브젝트 생성
        CreateWorldParents();

        summonManager = SummonManager.Instance;
        tileManager = TileManager.Instance;
        mergeManager = MergeManager.Instance;
    }

    /// <summary>
    /// 카메라 설정
    /// </summary>
    private void SetupCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 10f; // 화면 크기에 맞게 조정
            mainCamera.transform.position = new Vector3(0, 0, -10);
            mainCamera.transform.rotation = Quaternion.identity;
            
            Debug.Log("[PlacementManager] 카메라 설정 완료: Orthographic, Size: 10");
        }
    }

    /// <summary>
    /// 월드 공간 부모 오브젝트 생성
    /// </summary>
    private void CreateWorldParents()
    {
        if (characterParent == null)
        {
            GameObject obj = new GameObject("CharacterParent");
            characterParent = obj.transform;
        }

        if (bulletParent == null)
        {
            GameObject obj = new GameObject("BulletParent");
            bulletParent = obj.transform;
        }

        if (monsterParent == null)
        {
            GameObject obj = new GameObject("MonsterParent");
            monsterParent = obj.transform;
        }

        if (opponentCharacterParent == null)
        {
            GameObject obj = new GameObject("OpponentCharacterParent");
            opponentCharacterParent = obj.transform;
        }

        if (opponentBulletParent == null)
        {
            GameObject obj = new GameObject("OpponentBulletParent");
            opponentBulletParent = obj.transform;
        }

        if (opponentMonsterParent == null)
        {
            GameObject obj = new GameObject("OpponentMonsterParent");
            opponentMonsterParent = obj.transform;
        }
    }

    /// <summary>
    /// 타일 클릭 시 캐릭터 배치
    /// </summary>
    public void PlaceCharacterOnTile(Tile tile)
    {
        if (summonManager != null)
        {
            summonManager.PlaceCharacterOnTile(tile);
        }
    }

    /// <summary>
    /// ★★★ 수정: 캐릭터를 드롭했을 때 처리
    /// </summary>
    public void OnDropCharacter(Character character, Tile newTile)
    {
        if (character == null || newTile == null) return;

        // 기존 타일에서 제거
        if (character.currentTile != null && character.currentTile != newTile)
        {
            character.currentTile.RemoveOccupyingCharacter(character);
        }

        // 새 타일에 배치 가능한지 확인
        if (!newTile.CanPlaceCharacter(character))
        {
            Debug.LogWarning($"[PlacementManager] {newTile.name}에 {character.characterName}을(를) 배치할 수 없습니다.");
            
            // 원래 타일로 복귀
            if (character.currentTile != null)
            {
                character.SetPositionToTile(character.currentTile);
                character.currentTile.AddOccupyingCharacter(character);
            }
            return;
        }

        // 새 타일에 추가
        newTile.AddOccupyingCharacter(character);
        character.currentTile = newTile;
        character.SetPositionToTile(newTile);

        Debug.Log($"[PlacementManager] {character.characterName}을(를) {newTile.name}으로 이동 완료");

        // 합성 체크
        CheckAndMergeOnTile(newTile);
    }

    /// <summary>
    /// ★★★ 타일에서 합성 가능한지 확인하고 합성 실행
    /// </summary>
    private void CheckAndMergeOnTile(Tile tile)
    {
        if (tile == null || mergeManager == null) return;

        List<Character> tileCharacters = tile.GetOccupyingCharacters();
        
        // 같은 종류의 캐릭터가 3개 이상인지 확인
        var characterGroups = tileCharacters
            .Where(c => c != null)
            .GroupBy(c => new { c.characterName, c.star })
            .Where(g => g.Count() >= 3);

        foreach (var group in characterGroups)
        {
            List<Character> mergeTargets = group.Take(3).ToList();
            
            // 3성은 합성 불가
            if (mergeTargets[0].star == CharacterStar.ThreeStar)
            {
                Debug.Log("[PlacementManager] 3성 캐릭터는 더 이상 합성할 수 없습니다.");
                continue;
            }

            Debug.Log($"[PlacementManager] {mergeTargets[0].characterName} {mergeTargets[0].star} 3개 합성 시작!");

            // 합성 실행
            bool success = mergeManager.TryMergeCharacters(mergeTargets, tile);
            
            if (success)
            {
                Debug.Log("[PlacementManager] 합성 성공!");
                
                // 합성 후 다시 체크 (연쇄 합성 가능)
                CheckAndMergeOnTile(tile);
                break;
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

        // 캐릭터 데이터 설정
        character.characterName = data.characterName;
        character.characterIndex = data.characterIndex;
        character.race = data.race;
        character.star = data.star;
        character.attackPower = data.attackPower;
        character.attackRange = data.attackRange;
        character.attackSpeed = data.attackSpeed;
        character.currentHP = data.health;
        character.maxHP = data.health;
        character.level = data.level;
        character.characterSprite = data.characterSprite;
        character.frontSprite = data.frontSprite;
        character.backSprite = data.backSprite;
        character.areaIndex = isOpponent ? 2 : 1;

        // 스프라이트 설정
        SpriteRenderer spriteRenderer = character.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null && data.characterSprite != null)
        {
            spriteRenderer.sprite = data.characterSprite;
        }

        // DraggableCharacter 컴포넌트 추가 (플레이어 캐릭터만)
        if (!isOpponent && character.GetComponent<DraggableCharacter>() == null)
        {
            character.gameObject.AddComponent<DraggableCharacter>();
        }

        return character;
    }

    /// <summary>
    /// 총알 생성 (월드 좌표)
    /// </summary>
    public Bullet CreateBullet(Vector3 position, IDamageable target, Character owner, bool isOpponent = false)
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

        bullet.target = target;
        bullet.owner = owner;
        bullet.damage = owner.attackPower;

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
            Debug.LogWarning($"[PlacementManager] {newTile.name}에 {character.characterName}을(를) 배치할 수 없습니다.");
        }
    }

    public void ClearCharacterTileReference(Character character)
    {
        if (tileManager != null)
        {
            tileManager.ClearCharacterTileReference(character);
        }
    }

    public void OnCharacterRemovedFromTile(Tile tile)
    {
        if (tileManager != null)
        {
            tileManager.OnCharacterRemovedFromTile(tile);
        }
    }

    // 현재 선택된 캐릭터 인덱스 관리
    public void SetCurrentCharacterIndex(int index)
    {
        currentCharacterIndex = index;
        CoreDataManager.Instance.currentCharacterIndex = index;
    }

    public int GetCurrentCharacterIndex()
    {
        return currentCharacterIndex;
    }

    /// <summary>
    /// 캐릭터 선택 (CharacterSelectUI에서 호출)
    /// </summary>
    public void OnClickSelectUnit(int index)
    {
        currentCharacterIndex = index;
        if (summonManager != null)
        {
            summonManager.OnClickSelectUnit(index);
        }
    }

    /// <summary>
    /// 자동 배치 메서드
    /// </summary>
    public void OnClickAutoPlace()
    {
        if (summonManager != null)
        {
            summonManager.OnClickAutoPlace();
        }
        else
        {
            Debug.LogError("[PlacementManager] SummonManager가 null입니다!");
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
    /// 월드 좌표에서 타일 찾기
    /// </summary>
    public Tile GetTileAtWorldPosition(Vector3 worldPos)
    {
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, LayerMask.GetMask("Tile"));
        
        if (hit.collider != null)
        {
            return hit.collider.GetComponent<Tile>();
        }
        
        return null;
    }

    /// <summary>
    /// 마우스 위치의 타일 찾기
    /// </summary>
    public Tile GetTileAtMousePosition()
    {
        if (mainCamera == null) return null;
        
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        return GetTileAtWorldPosition(mouseWorldPos);
    }
}