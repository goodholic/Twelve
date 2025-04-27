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

    [Header("UI Panels")]
    public RectTransform tilePanel;
    public RectTransform characterPanel;
    public RectTransform bulletPanel;

    // ===== 추가: 미네랄 바 참조 =====
    [Header("Mineral Bar 참조 (소환 시 cost 차감용)")]
    [SerializeField] private MineralBar mineralBar;

    private void Awake()
    {
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
            Debug.LogWarning("PlacementManager: characterPanel이 null입니다.");
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
        if (characterDatabase == null ||
            characterDatabase.currentRegisteredCharacters == null ||
            characterDatabase.currentRegisteredCharacters.Length == 0)
        {
            Debug.LogWarning("PlacementManager: characterDatabase가 비어있음.");
            return;
        }
        if (currentCharacterIndex < 0 ||
            currentCharacterIndex >= characterDatabase.currentRegisteredCharacters.Length)
        {
            Debug.LogWarning($"PlacementManager: 잘못된 인덱스({currentCharacterIndex})");
            return;
        }
        if (characterPanel == null)
        {
            Debug.LogWarning("PlacementManager: characterPanel이 null임!");
            return;
        }
        if (tile == null)
        {
            Debug.LogWarning("PlacementManager: tile이 null");
            return;
        }

        // 캐릭터 데이터
        CharacterData data = characterDatabase.currentRegisteredCharacters[currentCharacterIndex];
        if (data == null || data.spawnPrefab == null)
        {
            Debug.LogWarning($"PlacementManager: [{currentCharacterIndex}]번 캐릭터 spawnPrefab이 null");
            return;
        }

        // 미네랄 체크: cost가 충분한지?
        if (mineralBar != null)
        {
            if (!mineralBar.TrySpend(data.cost))
            {
                Debug.LogWarning($"미네랄 부족 -> {data.characterName} 소환 취소");
                return;
            }
        }

        // 배치 가능 여부
        if (!tile.CanPlaceCharacter())
        {
            Debug.Log($"PlacementManager: {tile.name} 배치 불가 (Placable=false or Occupied=true)");
            return;
        }

        if (tile.IsOccupied())
        {
            Debug.LogWarning($"[PlacementManager] {tile.name} 이미 다른 캐릭터가 들어옴 -> 배치 취소!");
            return;
        }

        // 실제 캐릭터 프리팹 생성
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
                case RangeType.Melee:    characterComp.attackRange = 1.2f;    break;
                case RangeType.Ranged:   characterComp.attackRange = 2.5f;    break;
                case RangeType.LongRange:characterComp.attackRange = 4.0f;    break;
            }
            if (bulletPanel != null)
            {
                characterComp.SetBulletPanel(bulletPanel);
            }
        }

        // Occupied 표시
        CreateOccupiedChild(tile);

        Debug.Log($"[PlacementManager] [{data.characterName}] 배치 완료!(cost={data.cost})");
        currentCharacterIndex = -1;
    }

    /// <summary>
    /// (드래그 소환) 버튼 → 타일 드롭 시 호출
    /// </summary>
    public void SummonCharacterOnTile(int summonIndex, Tile tile)
    {
        if (characterDatabase == null ||
            characterDatabase.currentRegisteredCharacters == null ||
            characterDatabase.currentRegisteredCharacters.Length == 0)
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
        if (tile == null)
        {
            Debug.LogWarning("[PlacementManager] tile이 null => 소환 불가");
            return;
        }

        CharacterData data = characterDatabase.currentRegisteredCharacters[summonIndex];
        if (data == null || data.spawnPrefab == null)
        {
            Debug.LogWarning($"[PlacementManager] [{summonIndex}]번 캐릭터 spawnPrefab이 null => 소환 불가");
            return;
        }

        // 미네랄 체크
        if (mineralBar != null)
        {
            if (!mineralBar.TrySpend(data.cost))
            {
                Debug.LogWarning($"미네랄 부족 -> {data.characterName} 소환 취소");
                return;
            }
        }

        // 배치 가능 여부
        if (!tile.CanPlaceCharacter())
        {
            Debug.LogWarning($"[PlacementManager] {tile.name} 배치 불가(Placable=false or Occupied=true)");
            return;
        }
        if (tile.IsOccupied())
        {
            Debug.LogWarning($"[PlacementManager] {tile.name} 이미 다른 캐릭터 있음 -> 소환 취소!");
            return;
        }

        // 소환
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
            charObj.transform.position = tile.transform.position;
            charObj.transform.localRotation = Quaternion.identity;
        }

        Character characterComp = charObj.GetComponent<Character>();
        if (characterComp != null)
        {
            characterComp.currentTile = tile;
            characterComp.attackPower = data.attackPower;
            switch (data.rangeType)
            {
                case RangeType.Melee:    characterComp.attackRange = 1.2f;    break;
                case RangeType.Ranged:   characterComp.attackRange = 2.5f;    break;
                case RangeType.LongRange:characterComp.attackRange = 4.0f;    break;
            }
            if (bulletPanel != null)
            {
                characterComp.SetBulletPanel(bulletPanel);
            }
        }

        CreateOccupiedChild(tile);
        Debug.Log($"[PlacementManager] (드래그 소환) [{data.characterName}] 소환 완료!");
    }

    /// <summary>
    /// 이미 배치된 캐릭터를 새 타일로 이동 or 합성 시도
    /// </summary>
    public void OnDropCharacter(Character movingChar, Tile newTile)
    {
        if (movingChar == null || newTile == null) return;

        Tile oldTile = movingChar.currentTile;
        if (oldTile != null)
        {
            RemoveOccupiedChild(oldTile);
        }

        // 새 타일이 비어있으면 이동
        if (newTile.CanPlaceCharacter())
        {
            if (newTile.IsOccupied())
            {
                Debug.LogWarning($"[PlacementManager] {newTile.name} 다른 캐릭터가 선점 => 이동 실패!");
                CreateOccupiedChild(oldTile);
                return;
            }

            if (CheckAnyCharacterHasCurrentTile(newTile))
            {
                Debug.LogWarning($"[PlacementManager] {newTile.name}에 이미 캐릭터가 currentTile로 지정 => 이동 취소");
                CreateOccupiedChild(oldTile);
                MoveCharacterToTile(movingChar, oldTile);
                return;
            }

            MoveCharacterToTile(movingChar, newTile);
            CreateOccupiedChild(newTile);
            Debug.Log("[PlacementManager] 캐릭터가 새 타일로 이동 완료");
            currentCharacterIndex = -1;
        }
        else
        {
            // 합성 시도
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

    private bool TryMergeCharacter(Character movingChar, Tile newTile)
    {
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

                    // ★ 추가: 별 등급 변경 후, 실제 머티리얼(테두리/라이팅) 적용
                    otherChar.ApplyStarVisual();

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

    /// <summary>
    /// 합성 후 스탯 수치 보정
    /// </summary>
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
}
