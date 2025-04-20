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

    private void Awake()
    {
        // 싱글톤
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

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
        if (characterDatabase == null || characterDatabase.characters == null || characterDatabase.characters.Length == 0)
        {
            Debug.LogWarning("PlacementManager: characterDatabase가 비어있음.");
            return;
        }
        // 2) 현재 인덱스 검사
        if (currentCharacterIndex < 0 || currentCharacterIndex >= characterDatabase.characters.Length)
        {
            Debug.LogWarning($"PlacementManager: 잘못된 인덱스({currentCharacterIndex})");
            return;
        }
        // 3) characterPanel 검사
        if (characterPanel == null)
        {
            Debug.LogWarning("PlacementManager: characterPanel이 null임!");
            return;
        }

        // 캐릭터 데이터
        CharacterData data = characterDatabase.characters[currentCharacterIndex];
        if (data == null || data.spawnPrefab == null)
        {
            Debug.LogWarning($"PlacementManager: [{currentCharacterIndex}]번 캐릭터 spawnPrefab이 null");
            return;
        }

        if (tile == null)
        {
            Debug.LogWarning("PlacementManager: tile이 null");
            return;
        }

        // -----------------------------
        // "배치 가능"인지 다시 체크
        // -----------------------------
        if (!tile.CanPlaceCharacter())
        {
            Debug.Log($"PlacementManager: {tile.name} 배치 불가 (Placable=false or Occupied=true)");
            return;
        }

        // *** "마지막 순간"에도 Occupied 재확인 (동시성 문제 대비)
        if (tile.IsOccupied())
        {
            Debug.LogWarning($"[PlacementManager] {tile.name} 이미 다른 캐릭터가 들어옴 -> 배치 취소!");
            return;
        }

        // 실제 캐릭터 프리팹 생성
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
            // rangeType
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

            // 드래그 스크립트가 있다면 parentPanel 설정
            DraggableCharacterUI drag = charObj.GetComponent<DraggableCharacterUI>();
            if (drag != null)
            {
                drag.parentPanel = characterPanel;
            }
        }

        // 타일에 "Occupied" 자식 생성
        CreateOccupiedChild(tile);

        Debug.Log($"[PlacementManager] [{data.characterName}] 배치 완료!(cost={data.cost})");

        // 한 번 배치 후에는 인덱스 해제
        currentCharacterIndex = -1;
    }

    /// <summary>
    /// (드래그 소환) 버튼 → 타일 직접 드롭 시 호출
    /// </summary>
    public void SummonCharacterOnTile(int summonIndex, Tile tile)
    {
        // 캐릭터 DB 검사
        if (characterDatabase == null || characterDatabase.characters == null || characterDatabase.characters.Length == 0)
        {
            Debug.LogWarning("[PlacementManager] characterDatabase가 비어있어 소환 불가!");
            return;
        }
        if (summonIndex < 0 || summonIndex >= characterDatabase.characters.Length)
        {
            Debug.LogWarning($"[PlacementManager] 잘못된 summonIndex={summonIndex} => 소환 불가");
            return;
        }
        if (characterPanel == null)
        {
            Debug.LogWarning("[PlacementManager] characterPanel이 null => 소환 불가");
            return;
        }

        CharacterData data = characterDatabase.characters[summonIndex];
        if (data == null || data.spawnPrefab == null)
        {
            Debug.LogWarning($"[PlacementManager] [{summonIndex}]번 캐릭터 spawnPrefab이 null => 소환 불가");
            return;
        }
        if (tile == null)
        {
            Debug.LogWarning("[PlacementManager] tile이 null => 소환 불가");
            return;
        }

        // -----------------------------
        // "배치 가능"인지 다시 체크
        // -----------------------------
        if (!tile.CanPlaceCharacter())
        {
            Debug.LogWarning($"[PlacementManager] {tile.name} 배치 불가(Placable=false or Occupied=true)");
            return;
        }

        // *** 마지막 확인
        if (tile.IsOccupied())
        {
            Debug.LogWarning($"[PlacementManager] {tile.name} 이미 다른 캐릭터 있음 -> 소환 취소!");
            return;
        }

        // 캐릭터 생성
        GameObject charObj = Instantiate(data.spawnPrefab, characterPanel);

        // UI 좌표
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

        // Character 설정
        Character characterComp = charObj.GetComponent<Character>();
        if (characterComp != null)
        {
            characterComp.currentTile = tile;
            characterComp.attackPower = data.attackPower;
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
            // 드래그 스크립트
            DraggableCharacterUI drag = charObj.GetComponent<DraggableCharacterUI>();
            if (drag != null)
            {
                drag.parentPanel = characterPanel;
            }
        }

        // Occupied
        CreateOccupiedChild(tile);
        Debug.Log($"[PlacementManager] (드래그 소환) [{data.characterName}] 소환 완료!");
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

        // ----------------------------------------------
        // **원래 타일**에서 Occupied 해제
        // ----------------------------------------------
        Tile oldTile = movingChar.currentTile;
        if (oldTile != null)
        {
            RemoveOccupiedChild(oldTile);
        }

        // ----------------------------------------------
        // 새 타일이 비어있으면 "이동"
        // ----------------------------------------------
        if (newTile.CanPlaceCharacter())
        {
            // 최종 중복 체크
            if (newTile.IsOccupied())
            {
                // 이미 다른 캐릭터가 들어온 상황 -> 이동 취소
                Debug.LogWarning($"[PlacementManager] {newTile.name} 다른 캐릭터가 선점 => 이동 실패!");
                CreateOccupiedChild(oldTile); // 기존 타일 다시 Occupied로 복구
                return;
            }

            // ★ 추가됨: 새 타일에 "같은 currentTile"을 가진 캐릭터가 있는지 확인
            // (동시에 드래그가 이루어졌을 경우를 대비)
            if (CheckAnyCharacterHasCurrentTile(newTile))
            {
                // 이미 다른 캐릭터의 currentTile이 newTile이면 -> 원위치로
                Debug.LogWarning($"[PlacementManager] {newTile.name}에 이미 캐릭터가 currentTile로 지정 => 이동 취소");
                CreateOccupiedChild(oldTile);
                MoveCharacterToTile(movingChar, oldTile);
                return;
            }

            // 정상 이동
            MoveCharacterToTile(movingChar, newTile);
            CreateOccupiedChild(newTile);
            Debug.Log("[PlacementManager] 캐릭터가 새 타일로 이동 완료");
            currentCharacterIndex = -1;
        }
        else
        {
            // ----------------------------------------------
            // 새 타일에 이미 캐릭터가 있다면 "합성" 시도
            // ----------------------------------------------
            bool success = TryMergeCharacter(movingChar, newTile);
            if (!success)
            {
                // 합성 실패 => 원래 위치로 복귀
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

    /// <summary>
    /// ★ 추가됨: 씬에 존재하는 캐릭터 중, currentTile == tile 인 애가 있는지 확인
    /// </summary>
    private bool CheckAnyCharacterHasCurrentTile(Tile tile)
    {
        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var c in allChars)
        {
            if (c != null && c.currentTile == tile)
            {
                return true; // 누군가 이 tile을 이미 currentTile로 사용 중
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
            charRect.anchoredPosition = localPos;
            charRect.localRotation = Quaternion.identity;
        }
        else
        {
            character.transform.position = tile.transform.position;
            character.transform.localRotation = Quaternion.identity;
        }
        character.currentTile = tile;
    }

    /// <summary>
    /// 이미 있는 캐릭터(otherChar)와 movingChar가 "동일한 캐릭터"면 합성
    /// </summary>
    private bool TryMergeCharacter(Character movingChar, Tile newTile)
    {
        Debug.Log($"[PlacementManager] TryMergeCharacter: movingChar={movingChar.name}, tile={newTile.name}, star={movingChar.star}");

        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var otherChar in allChars)
        {
            if (otherChar == null || otherChar == movingChar) continue;
            if (otherChar.currentTile == newTile)
            {
                // 이름+별이 같아야 합성
                if (otherChar.star == movingChar.star && otherChar.name == movingChar.name)
                {
                    if (otherChar.star == CharacterStar.OneStar)
                        otherChar.star = CharacterStar.TwoStar;
                    else if (otherChar.star == CharacterStar.TwoStar)
                        otherChar.star = CharacterStar.ThreeStar;
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
        // 기존값 역산
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
