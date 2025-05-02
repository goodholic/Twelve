using UnityEngine;
using UnityEngine.EventSystems;
using Fusion;  // <<★ 추가: NetworkRunner 확인을 위해 필요

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
    public RectTransform characterPanel;  // 반드시 Inspector에서 할당 필요

    [Tooltip("총알이 들어갈 Panel")]
    public RectTransform bulletPanel;

    [Tooltip("아군 몬스터가 들어갈 Panel")]
    public RectTransform ourMonsterPanel;

    // [추가] VFX가 생성될 Panel
    [Tooltip("VFX 프리팹이 생성될 부모 Panel (없으면 월드에 생성)")]
    public RectTransform vfxPanel;

    // [추가] 미네랄바 레퍼런스 (없으면 씬에서 Find로 탐색)
    public MineralBar mineralBar;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (mineralBar == null)
        {
            mineralBar = FindAnyObjectByType<MineralBar>();
        }

        if (vfxPanel == null)
        {
            GameObject panelObj = GameObject.Find("VFX Panel");
            if (panelObj != null)
            {
                vfxPanel = panelObj.GetComponent<RectTransform>();
            }
        }
        Bullet.SetVfxPanel(vfxPanel);

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            Debug.LogWarning("<color=red>씬에 EventSystem이 없습니다! UI 클릭/드래그가 불가!</color>");
        }

        if (characterPanel == null)
        {
            Debug.LogWarning("[PlacementManager] characterPanel(캐릭터가 들어갈 Panel)이 null입니다.");
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
        if (characterDatabase == null 
            || characterDatabase.currentRegisteredCharacters == null 
            || characterDatabase.currentRegisteredCharacters.Length == 0)
        {
            Debug.LogWarning("[PlacementManager] characterDatabase가 비어있어 배치 불가");
            return;
        }

        if (currentCharacterIndex < 0 
            || currentCharacterIndex >= characterDatabase.currentRegisteredCharacters.Length)
        {
            Debug.LogWarning($"[PlacementManager] 잘못된 인덱스({currentCharacterIndex}) => 배치 불가");
            return;
        }

        CharacterData data = characterDatabase.currentRegisteredCharacters[currentCharacterIndex];
        if (data == null || data.spawnPrefab == null)
        {
            Debug.LogWarning($"[PlacementManager] [{currentCharacterIndex}]번 캐릭터 spawnPrefab이 null => 배치 불가");
            return;
        }

        // 미네랄 체크
        if (mineralBar != null)
        {
            if (!mineralBar.TrySpend(data.cost))
            {
                Debug.Log($"[PlacementManager] 미네랄 부족! (필요: {data.cost})");
                return;
            }
        }

        if (tile == null)
        {
            Debug.LogWarning("[PlacementManager] tile이 null => 배치 불가");
            return;
        }

        if (!tile.CanPlaceCharacter())
        {
            Debug.LogWarning($"[PlacementManager] {tile.name} => 배치 불가(조건 안 맞음)");
            return;
        }

        // (A) 만약 Walkable / Walkable2 => "아군 몬스터"로 소환
        if (tile.IsWalkable() || tile.IsWalkable2())
        {
            Debug.Log($"[PlacementManager] '{tile.name}' 클릭됨 -> Walkable(또는 Walkable2). 아군 몬스터 소환 시도");
            WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();

            if (spawner != null 
                && spawner.pathWaypoints != null 
                && spawner.pathWaypoints.Length > 0 
                && ourMonsterPanel != null)
            {
                GameObject prefabToSpawn = data.spawnPrefab;
                Character charCompCheck = prefabToSpawn.GetComponent<Character>();
                if (charCompCheck == null)
                {
                    Debug.LogError($"[PlacementManager] '{prefabToSpawn.name}' 캐릭터 프리팹에 Character 스크립트 없음!");
                    return;
                }

                Vector3 spawnPos = spawner.pathWaypoints[0].position;
                GameObject allyObj = Instantiate(prefabToSpawn, ourMonsterPanel);
                if (allyObj != null)
                {
                    RectTransform allyRect = allyObj.GetComponent<RectTransform>();
                    if (allyRect != null)
                    {
                        Vector2 localPos = ourMonsterPanel.InverseTransformPoint(spawnPos);
                        allyRect.anchoredPosition = localPos;
                        allyRect.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        allyObj.transform.position = spawnPos;
                        allyObj.transform.localRotation = Quaternion.identity;
                    }

                    Character allyCharacter = allyObj.GetComponent<Character>();
                    allyCharacter.isAlly = true;
                    allyCharacter.pathWaypoints = spawner.pathWaypoints;
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

                    // ===========================================
                    // === 변경된 핵심 로직 (Host=1, Client=2) ===
                    // ===========================================
                    var runner = FindFirstObjectByType<NetworkRunner>();
                    if (runner != null && runner.GameMode == GameMode.Client)
                    {
                        allyCharacter.areaIndex = 2;  // 클라=2
                    }
                    else
                    {
                        allyCharacter.areaIndex = 1;  // 호스트(또는 싱글)=1
                    }

                    Debug.Log($"[PlacementManager] [{data.characterName}] 아군 몬스터 소환 완료! (cost={data.cost})");
                    currentCharacterIndex = -1;
                }
                else
                {
                    Debug.LogError("[PlacementManager] Instantiate 실패!");
                }
            }
            else
            {
                Debug.LogWarning("[PlacementManager] WaveSpawner 또는 ourMonsterPanel 설정 불가 -> 아군 소환 실패");
            }
        }
        // (B) Placable / Picable2 => 기존처럼 characterPanel에 배치
        else if (tile.IsPlacable() || tile.IsPicable2())
        {
            Debug.Log($"[PlacementManager] '{tile.name}' 클릭됨 -> Placable/Picable2 처리");

            GameObject charObj = Instantiate(data.spawnPrefab, characterPanel);
            if (charObj != null)
            {
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
                    charObj.transform.position = tile.transform.position;
                    charObj.transform.localRotation = Quaternion.identity;
                }

                Character characterComp = charObj.GetComponent<Character>();
                if (characterComp != null)
                {
                    characterComp.currentTile = tile;
                    characterComp.attackPower = data.attackPower;
                    characterComp.isHero = (currentCharacterIndex == 9);
                    characterComp.isAlly = false;
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

                    if (bulletPanel != null)
                    {
                        characterComp.SetBulletPanel(bulletPanel);
                    }

                    // ===========================================
                    // === 변경된 핵심 로직 (Host=1, Client=2) ===
                    // ===========================================
                    var runner = FindFirstObjectByType<NetworkRunner>();
                    if (runner != null && runner.GameMode == GameMode.Client)
                    {
                        characterComp.areaIndex = 2;  // 클라=2
                    }
                    else
                    {
                        characterComp.areaIndex = 1;  // 호스트(또는 싱글)=1
                    }
                }

                CreateOccupiedChild(tile);

                Debug.Log($"[PlacementManager] [{data.characterName}] 배치 완료 (cost={data.cost})");
                currentCharacterIndex = -1;
            }
        }
    }

    /// <summary>
    /// (드래그 소환) 버튼 → 타일 직접 드롭 시 호출
    /// </summary>
    public void SummonCharacterOnTile(int summonIndex, Tile tile)
    {
        Debug.Log($"[PlacementManager] SummonCharacterOnTile: 인덱스={summonIndex}, 타일={tile.name}");

        if (characterDatabase == null
            || characterDatabase.currentRegisteredCharacters == null
            || characterDatabase.currentRegisteredCharacters.Length == 0)
        {
            Debug.LogWarning("[PlacementManager] DB가 없음 => 소환 불가");
            return;
        }
        if (summonIndex < 0 || summonIndex >= characterDatabase.currentRegisteredCharacters.Length)
        {
            Debug.LogWarning($"[PlacementManager] 잘못된 소환 인덱스({summonIndex}) => 실패");
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

        // 미네랄 소모
        if (mineralBar != null)
        {
            if (!mineralBar.TrySpend(data.cost))
            {
                Debug.Log($"[PlacementManager] 미네랄 부족! (필요: {data.cost})");
                return;
            }
        }

        if (tile == null)
        {
            Debug.LogWarning("[PlacementManager] tile이 null => 소환 불가");
            return;
        }

        if (!tile.CanPlaceCharacter() || tile.IsOccupied() || tile.IsOccupied2())
        {
            Debug.LogWarning($"[PlacementManager] 드롭된 타일 '{tile.name}' -> 이미 점유 중이거나 배치 불가");
            return;
        }

        // (A) Walkable / Walkable2 => 아군 몬스터
        if (tile.IsWalkable() || tile.IsWalkable2())
        {
            WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
            if (spawner != null
                && spawner.pathWaypoints != null
                && spawner.pathWaypoints.Length > 0
                && ourMonsterPanel != null)
            {
                GameObject prefabToSpawn = data.spawnPrefab;
                Character cc = prefabToSpawn.GetComponent<Character>();
                if (cc == null)
                {
                    Debug.LogError($"[PlacementManager] '{prefabToSpawn.name}' 프리팹에 Character가 없음 => 실패");
                    return;
                }

                Vector3 spawnPos = spawner.pathWaypoints[0].position;
                GameObject allyMonsterObj = Instantiate(prefabToSpawn, ourMonsterPanel);
                if (allyMonsterObj != null)
                {
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

                    // ===========================================
                    // === 변경된 핵심 로직 (Host=1, Client=2) ===
                    // ===========================================
                    var runner = FindFirstObjectByType<NetworkRunner>();
                    if (runner != null && runner.GameMode == GameMode.Client)
                    {
                        allyChar.areaIndex = 2;  // 클라=2
                    }
                    else
                    {
                        allyChar.areaIndex = 1;  // 호스트(또는 싱글)=1
                    }

                    Debug.Log($"[PlacementManager] 드래그로 [{data.characterName}] 아군 몬스터 소환 (cost={data.cost})");
                }
            }
            else
            {
                Debug.LogWarning("[PlacementManager] Walkable(2) => 소환 실패: WaveSpawner/Panel 미설정");
            }
        }
        // (B) Placable / Picable2 => 기존 배치 로직
        else if (tile.IsPlacable() || tile.IsPicable2())
        {
            Debug.Log($"[PlacementManager] (드래그) Placable/Picable2 => 일반 배치 시도");

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

                    // ===========================================
                    // === 변경된 핵심 로직 (Host=1, Client=2) ===
                    // ===========================================
                    var runner = FindFirstObjectByType<NetworkRunner>();
                    if (runner != null && runner.GameMode == GameMode.Client)
                    {
                        cComp.areaIndex = 2;  // 클라=2
                    }
                    else
                    {
                        cComp.areaIndex = 1;  // 호스트(또는 싱글)=1
                    }
                }

                CreateOccupiedChild(tile);

                Debug.Log($"[PlacementManager] [{data.characterName}] 드래그 배치 완료(Placable/Picable2)");
            }
        }
    }

    /// <summary>
    /// (드래그-드롭) 이미 배치된 캐릭터를 새 타일로 이동 or 합성 시도
    /// </summary>
    public void OnDropCharacter(Character movingChar, Tile newTile)
    {
        if (movingChar == null || newTile == null) return;

        Tile oldTile = movingChar.currentTile;
        if (oldTile != null)
        {
            RemoveOccupiedChild(oldTile);
        }

        // 빈 칸이면 "이동", 아니면 "합성" 시도
        if (newTile.CanPlaceCharacter() && !newTile.IsOccupied() && !newTile.IsOccupied2())
        {
            if (CheckAnyCharacterHasCurrentTile(newTile))
            {
                Debug.LogWarning($"[PlacementManager] {newTile.name} 이미 다른 캐릭터가 currentTile로 사용 => 이동 취소");
                CreateOccupiedChild(oldTile);
                MoveCharacterToTile(movingChar, oldTile);
                return;
            }

            // 새 Tile이 Walkable(2) 이면 => 아군 몬스터로 이동
            if (newTile.IsWalkable() || newTile.IsWalkable2())
            {
                WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
                if (spawner != null && spawner.pathWaypoints.Length > 0)
                {
                    Vector3 spawnPos = spawner.pathWaypoints[0].position;
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
                        movingChar.transform.SetParent(null);
                        movingChar.transform.position = spawnPos;
                        movingChar.transform.localRotation = Quaternion.identity;
                    }

                    movingChar.isAlly = true;
                    movingChar.currentTile = null;
                    movingChar.currentWaypointIndex = 0;

                    // ===========================================
                    // === 변경된 핵심 로직 (Host=1, Client=2) ===
                    // ===========================================
                    var runner = FindFirstObjectByType<NetworkRunner>();
                    if (runner != null && runner.GameMode == GameMode.Client)
                    {
                        movingChar.areaIndex = 2;
                    }
                    else
                    {
                        movingChar.areaIndex = 1;
                    }
                }
            }
            else
            {
                // Placable / Picable2
                MoveCharacterToTile(movingChar, newTile);
            }

            CreateOccupiedChild(newTile);
            Debug.Log("[PlacementManager] 캐릭터가 새 타일로 이동 완료");
            currentCharacterIndex = -1;
        }
        else
        {
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
            Vector2 localPos = characterPanel.InverseTransformPoint(tileRect.transform.position);
            charRect.SetParent(characterPanel, false);
            charRect.anchoredPosition = localPos;
            charRect.localRotation = Quaternion.identity;
        }
        else
        {
            character.transform.position = tile.transform.position;
            character.transform.localRotation = Quaternion.identity;
            character.transform.SetParent(null);
        }
        character.currentTile = tile;

        // ===========================================
        // === 변경된 핵심 로직 (Host=1, Client=2) ===
        // ===========================================
        // (Placable/Picable2로 이동 시)
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null && runner.GameMode == GameMode.Client)
        {
            character.areaIndex = 2;
        }
        else
        {
            character.areaIndex = 1;
        }
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
                // 별이 같고 이름이 같으면 합성(별 업)
                if (otherChar.star == movingChar.star && otherChar.name == movingChar.name)
                {
                    if (otherChar.star == CharacterStar.OneStar)
                        otherChar.star = CharacterStar.TwoStar;
                    else if (otherChar.star == CharacterStar.TwoStar)
                        otherChar.star = CharacterStar.ThreeStar;
                    else
                        Debug.Log("[PlacementManager] 이미 3성이므로 합성 불가");

                    UpgradeStats(otherChar);
                    Destroy(movingChar.gameObject);

                    Debug.Log("[PlacementManager] 합성 성공 -> 별 상승");
                    return true;
                }
                else
                {
                    Debug.Log("[PlacementManager] 캐릭터 이름 또는 별이 달라 합성 실패");
                }
            }
        }
        return false;
    }

    private void UpgradeStats(Character ch)
    {
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
        Transform exist2 = tile.transform.Find("Occupied2");

        if (exist == null && exist2 == null)
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
        Transform exist2 = tile.transform.Find("Occupied2");
        if (exist2 != null)
        {
            Destroy(exist2.gameObject);
        }
    }
}
