// Assets\Scripts\PlacementManager.cs

using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

    [Header("Placement Settings")]
    public CharacterDatabase characterDatabase;

    [Tooltip("현재 선택된 캐릭터 인덱스")]
    public int currentCharacterIndex = 0;

    [Tooltip("2D 카메라 (Orthographic)")]
    public Camera mainCamera;

    // ============================
    // [중요] 씬 상의 Panel 레퍼런스
    // ============================
    [Header("UI Panels")]
    [Tooltip("타일들이 있는 Panel (GridLayout 등 적용)")]
    public RectTransform tilePanel;

    [Tooltip("캐릭터들이 들어갈 Panel (GridLayout 없음)")]
    public RectTransform characterPanel;  // <-- 반드시 Inspector에 할당 필요!

    [Tooltip("총알이 들어갈 Panel")]
    public RectTransform bulletPanel;
    
    [Tooltip("아군 몬스터가 들어갈 Panel")]
    public RectTransform ourMonsterPanel;

    // [추가] VFX가 생성될 Panel
    [Tooltip("VFX 프리팹이 생성될 부모 Panel (없으면 월드에 생성)")]
    public RectTransform vfxPanel;

    // [추가] 미네랄바 레퍼런스 (없으면 씬에서 찾음)
    public MineralBar mineralBar;

    private void Awake()
    {
        // 싱글톤
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // [추가] MineralBar 자동 찾기
        if (mineralBar == null)
        {
            mineralBar = FindAnyObjectByType<MineralBar>();
        }

        // [추가] VFX Panel 자동 찾기 (이름으로 찾기 예시)
        if (vfxPanel == null)
        {
            GameObject panelObj = GameObject.Find("VFX Panel");
            if (panelObj != null)
            {
                vfxPanel = panelObj.GetComponent<RectTransform>();
            }
            if (vfxPanel == null) Debug.LogWarning("[PlacementManager] VFX Panel을 찾을 수 없습니다.");
        }

        // [추가] Bullet 클래스에 VFX Panel 참조 전달
        Bullet.SetVfxPanel(vfxPanel);

        // EventSystem 경고
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            Debug.LogWarning("<color=red>씬에 EventSystem이 없습니다! UI 버튼 클릭 불가 + 드래그 불가!</color>");
        }

        if (characterPanel == null)
        {
            Debug.LogWarning("PlacementManager: characterPanel(캐릭터가 들어갈 Panel)이 null입니다.");
        }
    }

    private void Update()
    {
        // (테스트) 숫자키로 캐릭터 선택
        if (Input.GetKeyDown(KeyCode.Alpha1)) currentCharacterIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentCharacterIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) currentCharacterIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) currentCharacterIndex = 3;
    }

    /// <summary>
    /// UI 버튼으로 캐릭터를 "선택" (클릭 배치용)
    /// </summary>
    public void OnClickSelectUnit(int index)
    {
        currentCharacterIndex = index;
        Debug.Log($"[PlacementManager] 선택된 유닛 인덱스: {currentCharacterIndex}");
    }

    /// <summary>
    /// (클릭 방식) 타일 위에 캐릭터를 배치
    /// </summary>
    public void PlaceCharacterOnTile(Tile tile)
    {
        // 1) 캐릭터 DB 검사
        if (characterDatabase == null 
            || characterDatabase.currentRegisteredCharacters == null 
            || characterDatabase.currentRegisteredCharacters.Length == 0)
        {
            Debug.LogWarning("PlacementManager: characterDatabase가 비어있음.");
            return;
        }

        // 2) 현재 인덱스 검사
        if (currentCharacterIndex < 0 
            || currentCharacterIndex >= characterDatabase.currentRegisteredCharacters.Length)
        {
            Debug.LogWarning($"PlacementManager: 잘못된 인덱스({currentCharacterIndex})");
            return;
        }

        // 캐릭터 데이터
        CharacterData data = characterDatabase.currentRegisteredCharacters[currentCharacterIndex];
        if (data == null || data.spawnPrefab == null)
        {
            Debug.LogWarning($"PlacementManager: [{currentCharacterIndex}]번 캐릭터 spawnPrefab이 null");
            return;
        }

        // [추가] 미네랄 체크 및 소모
        if (mineralBar != null)
        {
            if (!mineralBar.TrySpend(data.cost))
            {
                Debug.Log($"[PlacementManager] 미네랄 부족! (필요: {data.cost})");
                return;
            }
        }
        else
        {
            Debug.LogWarning("[PlacementManager] MineralBar를 찾을 수 없어 cost 체크 스킵.");
        }

        // tile null 체크
        if (tile == null)
        {
            Debug.LogWarning("PlacementManager: tile이 null");
            return;
        }

        // 3) 최종적으로 tile.CanPlaceCharacter() 검사
        //    (Walkable or Placable) && not Occupied
        if (!tile.CanPlaceCharacter())
        {
            Debug.Log($"PlacementManager: {tile.name} 배치 불가 (조건 불만족)");
            return;
        }

        // (A) *** Walkable인 경우 => "아군 몬스터"로 소환 ***
        if (tile.IsWalkable())
        {
            Debug.Log($"[PlacementManager] '{tile.name}' 클릭됨 -> Walkable 타일. 아군 전환 시도...");
            WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
            
            bool canSpawn = spawner != null 
                            && spawner.pathWaypoints.Length > 0 
                            && ourMonsterPanel != null 
                            && data.spawnPrefab != null;
            Debug.Log($"[PlacementManager] Walkable 조건 => 결과: {canSpawn}");

            if (canSpawn)
            {
                GameObject prefabToSpawn = data.spawnPrefab;
                Character charCompCheck = prefabToSpawn.GetComponent<Character>();
                if (charCompCheck == null)
                {
                    Debug.LogError($"[PlacementManager] 생성 실패: 선택된 캐릭터 프리팹 '{prefabToSpawn.name}'에 Character 컴포넌트가 없습니다!");
                    return;
                }

                // **핵심**: pathWaypoints[0] 위치에서 생성
                Vector3 spawnPos = spawner.pathWaypoints[0].position;
                Debug.Log($"[PlacementManager] 아군 생성 시도 (Walkable): Prefab='{prefabToSpawn.name}', Pos={spawnPos}");

                // Instantiate ourMonsterPanel 하위에 생성
                GameObject allyObj = Instantiate(prefabToSpawn, ourMonsterPanel);
                if (allyObj == null)
                {
                    Debug.LogError("[PlacementManager] Instantiate 실패 (Walkable)!");
                    return;
                }
                Debug.Log($"[PlacementManager] Instantiate 성공 (Walkable): '{allyObj.name}'");

                // RectTransform 여부에 따라 UI 좌표 세팅
                RectTransform allyRect = allyObj.GetComponent<RectTransform>();
                if (allyRect != null)
                {
                    Vector2 localPos = ourMonsterPanel.InverseTransformPoint(spawnPos);
                    allyRect.anchoredPosition = localPos;
                    allyRect.localRotation = Quaternion.identity;
                }
                else
                {
                    // 3D 상황
                    allyObj.transform.position = spawnPos;
                    allyObj.transform.localRotation = Quaternion.identity;
                }

                // Character 설정
                Character allyCharacter = allyObj.GetComponent<Character>();
                allyCharacter.isAlly = true;  
                allyCharacter.pathWaypoints = spawner.pathWaypoints; // 웨이포인트 따라 이동
                allyCharacter.currentTile = null; 
                allyCharacter.isHero = (currentCharacterIndex == 9); 
                allyCharacter.currentWaypointIndex = 0;
                allyCharacter.attackPower = data.attackPower; 
                allyCharacter.attackSpeed = data.attackSpeed; 
                allyCharacter.attackRange = data.attackRange; 
                allyCharacter.currentHP = data.maxHP;
                allyCharacter.star = data.initialStar;
                allyCharacter.enabled = true;
                allyCharacter.ApplyStarVisual();

                Debug.Log($"[PlacementManager] [{data.characterName}] 아군 몬스터로 전환 완료! (cost={data.cost}) => 웨이포인트 0 시작");

                // 배치 끝 -> currentCharacterIndex 해제
                currentCharacterIndex = -1;
                return;
            }
            else
            {
                Debug.LogWarning("[PlacementManager] Walkable이지만 spawner/waypoint 등 설정 미비로 소환 불가");
                return;
            }
        }

        // (B) *** Placable인 경우 => 기존처럼 characterPanel에 배치 ***
        if (tile.IsPlacable())
        {
            Debug.Log($"[PlacementManager] '{tile.name}' 클릭됨 -> Placable 타일. 일반 배치 시도...");

            // 실질적으로 characterPanel에 생성
            GameObject charObj = Instantiate(data.spawnPrefab, characterPanel);

            // UI 좌표계 변환
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
                // 3D 상황
                charObj.transform.position = tile.transform.position;
                charObj.transform.localRotation = Quaternion.identity;
            }

            // Character 설정
            Character characterComp = charObj.GetComponent<Character>();
            if (characterComp != null)
            {
                characterComp.currentTile = tile;
                characterComp.attackPower = data.attackPower;

                if (currentCharacterIndex == 9)
                {
                    characterComp.isHero = true;
                    Debug.Log($"[PlacementManager] 히어로 캐릭터({data.characterName}) 배치됨.");
                }
                else
                {
                    characterComp.isHero = false;
                }
                characterComp.isAlly = false; // 일반 배치는 적 캐릭터(혹은 중립)로 처리
                // rangeType에 따라 attackRange 조절
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

                // 총알 패널 연결
                if (bulletPanel != null)
                {
                    characterComp.SetBulletPanel(bulletPanel);
                }

                // 드래그 스크립트 (있으면) parentPanel 설정
                DraggableCharacterUI drag = charObj.GetComponent<DraggableCharacterUI>();
                if (drag != null)
                {
                    drag.parentPanel = characterPanel;
                }
            }

            // Occupied 표시
            CreateOccupiedChild(tile);

            Debug.Log($"[PlacementManager] [{data.characterName}] 배치 완료!(cost={data.cost})");

            // 한 번 배치 후에는 인덱스 해제
            currentCharacterIndex = -1;
        }
    }

    /// <summary>
    /// (드래그 소환) 버튼 → 타일 직접 드롭 시 호출
    /// </summary>
    public void SummonCharacterOnTile(int summonIndex, Tile tile)
    {
        Debug.Log($"[PlacementManager] SummonCharacterOnTile 호출: Index={summonIndex}, Tile={tile.name}");
        // 캐릭터 DB 검사
        if (characterDatabase == null 
            || characterDatabase.currentRegisteredCharacters == null 
            || characterDatabase.currentRegisteredCharacters.Length == 0)
        {
            Debug.LogWarning("[PlacementManager] characterDatabase가 비어있어 소환 불가!");
            return;
        }
        if (summonIndex < 0 || summonIndex >= characterDatabase.currentRegisteredCharacters.Length)
        {
            Debug.LogWarning($"[PlacementManager] 잘못된 summonIndex={summonIndex} => 소환 불가");
            return;
        }
        if (characterPanel == null)
        {
            Debug.LogWarning("[PlacementManager] characterPanel이 null => 소환 불가");
            return;
        }

        CharacterData data = characterDatabase.currentRegisteredCharacters[summonIndex];
        if (data == null || data.spawnPrefab == null)
        {
            Debug.LogWarning($"[PlacementManager] [{summonIndex}]번 캐릭터 spawnPrefab이 null => 소환 불가");
            return;
        }

        // [추가] 미네랄 체크 및 소모
        if (mineralBar != null)
        {
            if (!mineralBar.TrySpend(data.cost))
            {
                Debug.Log($"[PlacementManager] 미네랄 부족! (필요: {data.cost})");
                return;
            }
        }
        else
        {
            Debug.LogWarning("[PlacementManager] MineralBar를 찾을 수 없어 cost 체크 스킵.");
        }

        if (tile == null)
        {
            Debug.LogWarning("[PlacementManager] tile이 null => 소환 불가");
            return;
        }

        Debug.Log($"[PlacementManager] 드롭된 타일 => Walkable={tile.IsWalkable()}, Placable={tile.IsPlacable()}, Occupied={tile.IsOccupied()}");

        // 최종 확인
        if (!tile.CanPlaceCharacter() || tile.IsOccupied())
        {
            Debug.LogWarning($"[PlacementManager] 드롭된 타일 '{tile.name}'에 최종 배치 불가");
            return;
        }

        // (A) Walkable => 아군 몬스터
        if (tile.IsWalkable())
        {
            Debug.Log($"[PlacementManager] (드래그) Walkable -> 아군몬스터 소환");
            WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();

            bool canSpawn = spawner != null 
                            && spawner.pathWaypoints.Length > 0
                            && ourMonsterPanel != null
                            && data.spawnPrefab != null;
            if (canSpawn)
            {
                GameObject prefabToSpawn = data.spawnPrefab;
                Character cc = prefabToSpawn.GetComponent<Character>();
                if (cc == null)
                {
                    Debug.LogError($"[PlacementManager] 아군몬스터 소환 실패: '{prefabToSpawn.name}'에 Character 없음!");
                    return;
                }

                // **핵심**: pathWaypoints[0] 위치에서 생성
                Vector3 spawnPos = spawner.pathWaypoints[0].position;

                GameObject allyMonsterObj = Instantiate(prefabToSpawn, ourMonsterPanel);
                if (allyMonsterObj == null)
                {
                    Debug.LogWarning("[PlacementManager] Instantiate 실패(드래그 Walkable)!");
                    return;
                }
                RectTransform allyRect = allyMonsterObj.GetComponent<RectTransform>();
                if (allyRect != null)
                {
                    Vector2 localPos = ourMonsterPanel.InverseTransformPoint(spawnPos);
                    allyRect.anchoredPosition = localPos;
                    allyRect.localRotation = Quaternion.identity;
                }
                else
                {
                    allyMonsterObj.transform.position = spawnPos;
                    allyMonsterObj.transform.localRotation = Quaternion.identity;
                }

                Character allyChar = allyMonsterObj.GetComponent<Character>();
                allyChar.isAlly = true;
                allyChar.pathWaypoints = spawner.pathWaypoints;
                allyChar.currentTile = null;
                allyChar.isHero = (summonIndex == 9);
                allyChar.currentWaypointIndex = 0;
                allyChar.attackPower = data.attackPower;
                allyChar.attackSpeed = data.attackSpeed;
                allyChar.attackRange = data.attackRange;
                allyChar.currentHP = data.maxHP;
                allyChar.star = data.initialStar;
                allyChar.enabled = true;
                allyChar.ApplyStarVisual();

                Debug.Log($"[PlacementManager] 드래그로 [{data.characterName}] 아군몬스터 소환 완료 (cost={data.cost})");
                return;
            }
            else
            {
                Debug.LogWarning("[PlacementManager] (드래그) Walkable=>아군몬스터 실패: WaveSpawner 조건 부족");
                return;
            }
        }

        // (B) Placable => 기존대로
        if (tile.IsPlacable())
        {
            Debug.Log($"[PlacementManager] (드래그) Placable -> 일반 배치 시도...");

            // 캐릭터 패널에 배치
            GameObject newCharObj = Instantiate(data.spawnPrefab, characterPanel);
            if (newCharObj != null)
            {
                RectTransform tileRect = tile.GetComponent<RectTransform>();
                RectTransform charRect = newCharObj.GetComponent<RectTransform>();
                if (tileRect != null && charRect != null)
                {
                    Vector2 localPos = characterPanel.InverseTransformPoint(tileRect.transform.position);
                    charRect.anchoredPosition = localPos;
                    charRect.localRotation = Quaternion.identity;
                }
                else
                {
                    newCharObj.transform.position = tile.transform.position;
                    newCharObj.transform.localRotation = Quaternion.identity;
                }

                Character cComp = newCharObj.GetComponent<Character>();
                if (cComp != null)
                {
                    cComp.currentTile = tile;
                    cComp.attackPower = data.attackPower;
                    cComp.isHero = (summonIndex == 9);
                    cComp.isAlly = false; 
                    switch (data.rangeType)
                    {
                        case RangeType.Melee:   cComp.attackRange = 1.2f;  break;
                        case RangeType.Ranged:  cComp.attackRange = 2.5f;  break;
                        case RangeType.LongRange:cComp.attackRange = 4.0f;  break;
                    }
                    if (bulletPanel != null)
                    {
                        cComp.SetBulletPanel(bulletPanel);
                    }
                }

                // Occupied 표시
                CreateOccupiedChild(tile);

                Debug.Log($"[PlacementManager] [{data.characterName}] 드래그 배치 완료 (Placable)");
            }
        }
    }

    /// <summary>
    /// (드래그-드롭으로) 이미 배치된 캐릭터를 새 타일로 이동 or 합성 시도
    /// </summary>
    public void OnDropCharacter(Character movingChar, Tile newTile)
    {
        if (movingChar == null || newTile == null)
        {
            return;
        }

        // **원래 타일**에서 Occupied 해제
        Tile oldTile = movingChar.currentTile;
        if (oldTile != null)
        {
            RemoveOccupiedChild(oldTile);
        }

        // 새 타일이 비어있으면 "이동"
        if (newTile.CanPlaceCharacter() && !newTile.IsOccupied())
        {
            if (CheckAnyCharacterHasCurrentTile(newTile))
            {
                // 충돌 방지
                Debug.LogWarning($"[PlacementManager] {newTile.name}에 이미 다른 캐릭터 currentTile 지정 -> 이동 취소");
                CreateOccupiedChild(oldTile);
                MoveCharacterToTile(movingChar, oldTile);
                return;
            }

            // 만약 newTile이 ourMonsterPanel에 속하는 "Walkable"이라면 => pathWaypoints[0]에서 생성
            if (newTile.IsWalkable())
            {
                WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
                if (spawner != null && spawner.pathWaypoints.Length > 0)
                {
                    Vector3 spawnPos = spawner.pathWaypoints[0].position;
                    Debug.Log("[PlacementManager] 아군 몬스터로 이동 => pathWaypoints[0] 위치로 이동 시도");

                    // UI 좌표 세팅 (ourMonsterPanel 기준)
                    RectTransform moveRect = movingChar.GetComponent<RectTransform>();
                    if (moveRect != null && ourMonsterPanel != null)
                    {
                        Vector2 localPos = ourMonsterPanel.InverseTransformPoint(spawnPos);
                        moveRect.SetParent(ourMonsterPanel, false);
                        moveRect.anchoredPosition = localPos;
                        moveRect.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        // 3D 상황
                        movingChar.transform.SetParent(null);
                        movingChar.transform.position = spawnPos;
                        movingChar.transform.localRotation = Quaternion.identity;
                    }

                    // 아군 설정
                    movingChar.isAlly = true;
                    movingChar.currentTile = null;
                    movingChar.currentWaypointIndex = 0;
                }
            }
            else
            {
                // 기존 "Placable" 타일로 이동
                MoveCharacterToTile(movingChar, newTile);
            }

            CreateOccupiedChild(newTile);
            Debug.Log("[PlacementManager] 캐릭터가 새 타일로 이동 완료");
            currentCharacterIndex = -1;
        }
        else
        {
            // 새 타일에 이미 캐릭터 => 합성 시도
            bool success = TryMergeCharacter(movingChar, newTile);
            if (!success)
            {
                if (oldTile != null)
                {
                    MoveCharacterToTile(movingChar, oldTile);
                    CreateOccupiedChild(oldTile);
                }
            }
            else
            {
                currentCharacterIndex = -1;
            }
        }
    }

    private bool CheckAnyCharacterHasCurrentTile(Tile tile)
    {
        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var c in allChars)
        {
            if (c != null && c.currentTile == tile)
            {
                return true;
            }
        }
        return false;
    }

    private void MoveCharacterToTile(Character character, Tile tile)
    {
        if (characterPanel == null) return;

        RectTransform tileRect = tile.GetComponent<RectTransform>();
        RectTransform charRect = character.GetComponent<RectTransform>();
        if (tileRect != null && charRect != null)
        {
            // characterPanel 부모 기준으로 좌표 이동
            Vector2 localPos = characterPanel.InverseTransformPoint(tileRect.transform.position);
            charRect.SetParent(characterPanel, false);
            charRect.anchoredPosition = localPos;
            charRect.localRotation = Quaternion.identity;
        }
        else
        {
            character.transform.position = tile.transform.position;
            character.transform.localRotation = Quaternion.identity;
            character.transform.SetParent(null); // 등등
        }
        character.currentTile = tile;
        // 만약 여기서 아군/적 여부도 바꾸고 싶다면 추가 가능
    }

    private bool TryMergeCharacter(Character movingChar, Tile newTile)
    {
        Debug.Log($"[PlacementManager] TryMergeCharacter: movingChar={movingChar.name}, tile={newTile.name}, star={movingChar.star}");

        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var otherChar in allChars)
        {
            if (otherChar == null || otherChar == movingChar) continue;
            if (otherChar.currentTile == newTile)
            {
                if (otherChar.star == movingChar.star && otherChar.name == movingChar.name)
                {
                    if (otherChar.star == CharacterStar.OneStar) otherChar.star = CharacterStar.TwoStar;
                    else if (otherChar.star == CharacterStar.TwoStar) otherChar.star = CharacterStar.ThreeStar;
                    else
                    {
                        Debug.Log("[PlacementManager] 이미 3성이므로 더이상 합성 불가");
                    }
                    UpgradeStats(otherChar);
                    Destroy(movingChar.gameObject);
                    Debug.Log("[PlacementManager] 합성 성공 -> 별 상승");
                    return true;
                }
                else
                {
                    Debug.Log("[PlacementManager] 캐릭터 이름이나 별이 달라 합성 실패");
                }
            }
        }
        return false;
    }

    private void UpgradeStats(Character ch)
    {
        // 기존값 역산(1성 기준)
        float baseAtk = ch.attackPower / 1.6f;
        float baseRange = ch.attackRange / 1.2f;
        float baseSpeed = ch.attackSpeed / 1.2f;

        switch (ch.star)
        {
            case CharacterStar.OneStar:
                ch.attackPower = baseAtk;
                ch.attackRange = baseRange;
                ch.attackSpeed = baseSpeed;
                break;
            case CharacterStar.TwoStar:
                ch.attackPower = baseAtk * 1.3f;
                ch.attackRange = baseRange * 1.1f;
                ch.attackSpeed = baseSpeed * 1.1f;
                break;
            case CharacterStar.ThreeStar:
                ch.attackPower = baseAtk * 1.6f;
                ch.attackRange = baseRange * 1.2f;
                ch.attackSpeed = baseSpeed * 1.2f;
                break;
        }
    }

    private void CreateOccupiedChild(Tile tile)
    {
        Transform exist = tile.transform.Find("Occupied");
        if (exist == null)
        {
            GameObject occupiedObj = new GameObject("Occupied");
            occupiedObj.transform.SetParent(tile.transform, false);
            occupiedObj.transform.localPosition = Vector3.zero;
        }
    }

    private void RemoveOccupiedChild(Tile tile)
    {
        Transform exist = tile.transform.Find("Occupied");
        if (exist != null)
        {
            Destroy(exist.gameObject);
        }
    }
}
