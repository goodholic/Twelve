using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 캐릭터 배치 & 아군 몬스터 소환 담당
/// 한 타일에 여러 개 배치 가능(Occupied 제거).
/// Walkable 타일이면 '아군 몬스터'가 생성되어 적 몬스터 경로를 따라감.
/// 
/// 이번 수정사항:
///  - 아군 몬스터는 "OurMonsterPanel" (Canvas 자식)에 생성되도록 변경.
/// </summary>
public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

    [Header("Placement Settings")]
    public CharacterDatabase characterDatabase;

    [Tooltip("현재 선택된 캐릭터 인덱스")]
    public int currentCharacterIndex = 0;

    [Tooltip("2D 카메라 (Orthographic)")]
    public Camera mainCamera;

    [Header("UI Panels")]
    public RectTransform tilePanel;
    public RectTransform characterPanel;
    public RectTransform bulletPanel;

    [Header("WaveSpawner 참조 (아군 몬스터 소환 시 waypoint 필요)")]
    public WaveSpawner waveSpawner;

    // === 추가: 아군 몬스터 생성 시, "OurMonsterPanel"에 배치하도록 함
    [Header("아군 몬스터 부모 패널(캔버스)")]
    [Tooltip("씬의 Canvas 하위에 있는 'OurMonster Panel'을 연결해주세요.")]
    public RectTransform ourMonsterPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            Debug.LogWarning("<color=red>EventSystem이 씬에 없습니다! UI 클릭/드래그 불가!</color>");
        }
        if (characterPanel == null)
        {
            Debug.LogWarning("PlacementManager: characterPanel이 연결되지 않음!");
        }
        if (waveSpawner == null)
        {
            Debug.LogWarning("PlacementManager: waveSpawner가 연결되지 않음! (Walkable 타일 아군 몬스터 소환 시 경로가 필요)");
        }
        // 아군 몬스터용 패널 체크
        if (ourMonsterPanel == null)
        {
            Debug.LogWarning("PlacementManager: ourMonsterPanel(아군 몬스터용)이 연결되지 않음!");
        }
    }

    private void Update()
    {
        // 키보드 숫자 입력으로 캐릭터 선택(테스트용)
        if (Input.GetKeyDown(KeyCode.Alpha1)) currentCharacterIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentCharacterIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) currentCharacterIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) currentCharacterIndex = 3;
    }

    /// <summary>
    /// UI 버튼 클릭으로 캐릭터 인덱스 선택
    /// </summary>
    public void OnClickSelectUnit(int index)
    {
        currentCharacterIndex = index;
        Debug.Log($"[PlacementManager] 선택된 유닛 인덱스: {currentCharacterIndex}");
    }

    /// <summary>
    /// (클릭 방식) 타일 위에 캐릭터를 배치.
    /// Walkable이면 아군 몬스터, Placable이면 고정형 터렛 배치.
    /// </summary>
    public void PlaceCharacterOnTile(Tile tile)
    {
        if (characterDatabase == null || characterDatabase.currentRegisteredCharacters == null)
        {
            Debug.LogWarning("PlacementManager: characterDatabase가 비어있음.");
            return;
        }
        if (currentCharacterIndex < 0 || currentCharacterIndex >= characterDatabase.currentRegisteredCharacters.Length)
        {
            Debug.LogWarning($"PlacementManager: 잘못된 인덱스({currentCharacterIndex})");
            return;
        }
        if (tile == null) return;

        CharacterData data = characterDatabase.currentRegisteredCharacters[currentCharacterIndex];
        if (data == null)
        {
            Debug.LogWarning($"PlacementManager: [{currentCharacterIndex}]번 캐릭터 데이터가 null");
            return;
        }

        // Walkable이면 아군 몬스터, Placable이면 터렛(캐릭터)로 소환
        if (tile.IsWalkable())
        {
            SpawnAllyMonster(data, tile);
        }
        else if (tile.IsPlacable())
        {
            SpawnTurretCharacter(data, tile);
        }
        else
        {
            Debug.Log($"[PlacementManager] Tile {tile.name}이 walkable/placable이 아님. 소환 취소");
        }

        // 일회성 소환 후 인덱스 해제
        currentCharacterIndex = -1;
    }

    /// <summary>
    /// (드래그 방식) 버튼 → 타일 위로 드롭
    /// Walkable이면 아군 몬스터, Placable이면 고정형 터렛
    /// </summary>
    public void SummonCharacterOnTile(int summonIndex, Tile tile)
    {
        if (characterDatabase == null || characterDatabase.currentRegisteredCharacters == null)
        {
            Debug.LogWarning("[PlacementManager] characterDatabase가 비어있어 소환 불가!");
            return;
        }
        if (summonIndex < 0 || summonIndex >= characterDatabase.currentRegisteredCharacters.Length)
        {
            Debug.LogWarning($"[PlacementManager] 잘못된 summonIndex={summonIndex} => 소환 불가");
            return;
        }
        if (tile == null) return;

        CharacterData data = characterDatabase.currentRegisteredCharacters[summonIndex];
        if (data == null)
        {
            Debug.LogWarning($"[PlacementManager] [{summonIndex}]번 캐릭터 데이터가 null => 소환 불가");
            return;
        }

        if (tile.IsWalkable())
        {
            SpawnAllyMonster(data, tile);
        }
        else if (tile.IsPlacable())
        {
            SpawnTurretCharacter(data, tile);
        }
        else
        {
            Debug.Log($"[PlacementManager] Tile {tile.name}이 walkable/placable이 아님. 소환 취소");
        }
    }

    // ----------------------------------------------------------
    // (A) 일반 캐릭터(터렛) 배치
    // ----------------------------------------------------------
    private void SpawnTurretCharacter(CharacterData data, Tile tile)
    {
        if (data.spawnPrefab == null)
        {
            Debug.LogWarning($"{data.characterName} 의 spawnPrefab이 null이라 소환 불가");
            return;
        }
        if (characterPanel == null)
        {
            Debug.LogWarning("characterPanel이 null => 소환 위치 불명");
            return;
        }

        GameObject charObj = Instantiate(data.spawnPrefab, characterPanel);
        RectTransform tileRect = tile.GetComponent<RectTransform>();
        RectTransform charRect = charObj.GetComponent<RectTransform>();

        if (tileRect != null && charRect != null)
        {
            Vector2 localPos = characterPanel.InverseTransformPoint(tileRect.transform.position);
            charRect.anchoredPosition = localPos;
            charRect.localRotation = Quaternion.identity;
        }
        else
        {
            // 3D라면 worldPos로 위치
            charObj.transform.position = tile.transform.position;
            charObj.transform.localRotation = Quaternion.identity;
        }

        // Character 컴포넌트 설정
        Character characterComp = charObj.GetComponent<Character>();
        if (characterComp != null)
        {
            characterComp.currentTile = tile;
            characterComp.attackPower = data.attackPower;

            // rangeType에 따른 사거리
            switch (data.rangeType)
            {
                case RangeType.Melee:
                    characterComp.attackRange = 1.2f;
                    break;
                case RangeType.Ranged:
                    characterComp.attackRange = 2.5f;
                    break;
                case RangeType.LongRange:
                    characterComp.attackRange = 4.0f;
                    break;
            }
            // 총알 패널
            if (bulletPanel != null)
            {
                characterComp.SetBulletPanel(bulletPanel);
            }
        }

        Debug.Log($"[PlacementManager] 터렛(캐릭터) '{data.characterName}' 소환 (Tile={tile.name})");
    }

    // ----------------------------------------------------------
    // (B) 아군 몬스터 배치
    // ----------------------------------------------------------
    private void SpawnAllyMonster(CharacterData data, Tile tile)
    {
        // ※ WaveSpawner에서 prefab은 가져오되, 부모는 "ourMonsterPanel" 사용
        if (waveSpawner == null || waveSpawner.monsterPrefab == null)
        {
            Debug.LogWarning("[PlacementManager] WaveSpawner 또는 monsterPrefab이 null => 아군 몬스터 생성 불가");
            return;
        }
        if (ourMonsterPanel == null)
        {
            Debug.LogWarning("[PlacementManager] ourMonsterPanel이 null => 아군 몬스터 생성 불가");
            return;
        }

        // ourMonsterPanel 밑에 생성
        GameObject allyObj = Instantiate(waveSpawner.monsterPrefab, ourMonsterPanel);

        // UI 모드로 타일 위치 반영
        RectTransform tileRect = tile.GetComponent<RectTransform>();
        RectTransform allyRect = allyObj.GetComponent<RectTransform>();
        if (tileRect != null && allyRect != null)
        {
            Vector2 localPos = ourMonsterPanel.InverseTransformPoint(tileRect.transform.position);
            allyRect.anchoredPosition = localPos;
            allyRect.localRotation = Quaternion.identity;
        }
        else
        {
            // 3D 환경이라면 worldPos
            allyObj.transform.position = tile.transform.position;
            allyObj.transform.localRotation = Quaternion.identity;
        }

        // 몬스터 컴포넌트 설정
        Monster allyMonster = allyObj.GetComponent<Monster>();
        if (allyMonster != null)
        {
            allyMonster.isAlly = true;  // 아군
            allyMonster.pathWaypoints = waveSpawner.pathWaypoints;

            // (예시) 체력, 이동속도 등 설정 가능
            allyMonster.health = 50f; 
        }

        // 겹침 방지용 콜라이더
        var coll = allyObj.AddComponent<CircleCollider2D>();
        coll.isTrigger = false;
        coll.radius = 30f; // UI 해상도 등 맞춰 조절

        Debug.Log($"[PlacementManager] 아군 몬스터 '{data.characterName}' 소환 (Tile={tile.name}) => OurMonsterPanel 자식");
    }

    /// <summary>
    /// 캐릭터를 다른 타일로 드래그 드롭할 때 호출되는 메서드
    /// (합성 로직 등은 제거됨)
    /// </summary>
    public void OnDropCharacter(Character character, Tile targetTile)
    {
        if (character == null || targetTile == null)
        {
            Debug.LogWarning("[PlacementManager] OnDropCharacter: character 또는 targetTile이 null");
            return;
        }

        // 단순히 타일 좌표로 이동
        RectTransform charRect = character.GetComponent<RectTransform>();
        RectTransform tileRect = targetTile.GetComponent<RectTransform>();

        if (charRect != null && tileRect != null)
        {
            Vector2 localPos = charRect.parent.InverseTransformPoint(tileRect.transform.position);
            charRect.anchoredPosition = localPos;
        }
        else
        {
            character.transform.position = targetTile.transform.position;
        }

        character.currentTile = targetTile;

        Debug.Log($"[PlacementManager] 캐릭터를 {targetTile.name}으로 이동 완료");
    }
}
