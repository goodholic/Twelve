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

        // (추가) 만약 Inspector에서 characterPanel이 미할당이면 경고
        if (characterPanel == null)
        {
            Debug.LogWarning("PlacementManager: characterPanel이 Inspector에서 할당되지 않았습니다! " +
                             "캐릭터 배치 로직이 동작하지 않을 수 있습니다.");
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
    /// UI 버튼으로 캐릭터를 선택했을 때 호출
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
            Debug.LogWarning("PlacementManager: characterPanel이 null임! (Inspector에서 RectTransform 할당 필요)");
            return;
        }

        // 캐릭터 데이터
        CharacterData data = characterDatabase.characters[currentCharacterIndex];
        if (data == null || data.spawnPrefab == null)
        {
            Debug.LogWarning($"PlacementManager: [{currentCharacterIndex}]번 캐릭터 데이터/프리팹이 null.");
            return;
        }

        if (tile == null)
        {
            Debug.LogWarning("PlacementManager: tile이 null");
            return;
        }
        if (!tile.CanPlaceCharacter())
        {
            Debug.Log($"PlacementManager: {tile.name}에 배치 불가 (Placable=false, Occupied=true 등)");
            return;
        }

        // ==========================
        // 캐릭터 생성 (부모=characterPanel)
        // ==========================
        GameObject charObj = Instantiate(data.spawnPrefab, characterPanel);

        // UI 좌표계 변환
        RectTransform tileRect = tile.GetComponent<RectTransform>();
        RectTransform charRect = charObj.GetComponent<RectTransform>();
        if (tileRect != null && charRect != null)
        {
            // tilePanel이 아닌, characterPanel의 좌표로 변환
            Vector2 localPosInCharacterPanel = characterPanel.InverseTransformPoint(tileRect.transform.position);
            charRect.anchoredPosition = localPosInCharacterPanel;
            charRect.localRotation = Quaternion.identity;
        }
        else
        {
            // (3D 상황) 그냥 worldPosition
            charObj.transform.position = tile.transform.position;
            charObj.transform.localRotation = Quaternion.identity;
        }

        // Character 설정
        Character characterComp = charObj.GetComponent<Character>();
        if (characterComp != null)
        {
            // 어떤 타일에 배치되었는지 참조만 저장 (물리적으로 같은 부모는 아니다!)
            characterComp.currentTile = tile;
            characterComp.attackPower = data.attackPower;

            // rangeType에 따라 사거리 세팅
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

            // 총알 패널 설정
            if (bulletPanel != null)
            {
                characterComp.SetBulletPanel(bulletPanel);
            }

            // 드래그 스크립트가 있다면, parentPanel을 characterPanel로
            DraggableCharacterUI drag = charObj.GetComponent<DraggableCharacterUI>();
            if (drag != null)
            {
                drag.parentPanel = characterPanel;
            }
        }

        // 타일 Occupied 처리
        CreateOccupiedChild(tile);

        Debug.Log($"[PlacementManager] [{data.characterName}] 배치 완료! (cost={data.cost})");

        // 한 번 배치 후에는 인덱스 리셋
        currentCharacterIndex = -1;
    }

    /// <summary>
    /// (드래그-드롭 방식) 캐릭터를 새 Tile로 옮길 때
    /// </summary>
    public void OnDropCharacter(Character character, Tile newTile)
    {
        if (character == null || newTile == null)
        {
            return;
        }

        // 기존 타일 Occupied 해제
        if (character.currentTile != null)
        {
            RemoveOccupiedChild(character.currentTile);
        }

        // 새 타일이 비어있으면 -> 이동
        if (newTile.CanPlaceCharacter())
        {
            MoveCharacterToTile(character, newTile);
            CreateOccupiedChild(newTile);
            Debug.Log("캐릭터가 새 타일로 이동 완료");

            // 이동만 해도 이미 사용된 것으로 간주 -> currentCharacterIndex 리셋
            currentCharacterIndex = -1;
        }
        else
        {
            // 합성 시도
            bool success = TryMergeCharacter(character, newTile);
            if (!success)
            {
                // 합성 실패 -> 원래 타일로 복귀
                if (character.currentTile != null)
                {
                    MoveCharacterToTile(character, character.currentTile);
                    CreateOccupiedChild(character.currentTile);
                }
            }
            else
            {
                // 합성 성공 시에도 인덱스 리셋
                currentCharacterIndex = -1;
            }
        }
    }

    private void MoveCharacterToTile(Character character, Tile tile)
    {
        // 캐릭터의 부모(Panel)는 characterPanel이므로,
        // 타일의 position → characterPanel 좌표로 변환
        if (characterPanel == null)
        {
            Debug.LogWarning("PlacementManager: characterPanel이 null임! (Inspector에서 RectTransform 할당 필요)");
            return;
        }

        RectTransform tileRect = tile.GetComponent<RectTransform>();
        RectTransform charRect = character.GetComponent<RectTransform>();

        if (tileRect != null && charRect != null)
        {
            Vector2 localPosInCharacterPanel = characterPanel.InverseTransformPoint(tileRect.transform.position);
            charRect.anchoredPosition = localPosInCharacterPanel;
            charRect.localRotation = Quaternion.identity;
        }
        else
        {
            // 3D 상황
            character.transform.position = tile.transform.position;
            character.transform.localRotation = Quaternion.identity;
        }

        // currentTile 갱신 -> 합성 로직에서 newTile과 같아짐
        character.currentTile = tile;
    }

    /// <summary>
    /// 별이 같은 캐릭터가 이미 그 타일에 있으면 합성
    /// </summary>
    private bool TryMergeCharacter(Character movingChar, Tile newTile)
    {
        Debug.Log($"[TryMergeCharacter] 시도: movingChar={movingChar.name}, tile={newTile.name}, star={movingChar.star}");

        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var otherChar in allChars)
        {
            if (otherChar == null) continue;

            // 자기 자신 제외, 같은 타일에 있는 캐릭터?
            if (otherChar != movingChar && otherChar.currentTile == newTile)
            {
                Debug.Log($" -> {newTile.name}에 [{otherChar.name}] 존재 (star={otherChar.star})");

                // 별이 같으면 합성
                if (otherChar.star == movingChar.star)
                {
                    if (otherChar.star == CharacterStar.OneStar)
                        otherChar.star = CharacterStar.TwoStar;
                    else if (otherChar.star == CharacterStar.TwoStar)
                        otherChar.star = CharacterStar.ThreeStar;
                    else
                    {
                        Debug.Log("이미 3성이라 더 이상 합성 불가!");
                    }

                    UpgradeStats(otherChar);
                    Destroy(movingChar.gameObject);

                    Debug.Log("합성 성공! 별 한 단계 상승");
                    return true;
                }
                else
                {
                    Debug.Log("별이 달라 합성 실패!");
                }
            }
        }

        Debug.Log("합성할 캐릭터가 없어 실패");
        return false;
    }

    private void UpgradeStats(Character ch)
    {
        float baseAtk = ch.attackPower / 1.6f;
        float baseRange = ch.attackRange / 1.2f;
        float baseSpeed = ch.attackSpeed / 1.2f;

        if (ch.star == CharacterStar.OneStar)
        {
            ch.attackPower = baseAtk;
            ch.attackRange = baseRange;
            ch.attackSpeed = baseSpeed;
        }
        else if (ch.star == CharacterStar.TwoStar)
        {
            ch.attackPower = baseAtk * 1.3f;
            ch.attackRange = baseRange * 1.1f;
            ch.attackSpeed = baseSpeed * 1.1f;
        }
        else if (ch.star == CharacterStar.ThreeStar)
        {
            ch.attackPower = baseAtk * 1.6f;
            ch.attackRange = baseRange * 1.2f;
            ch.attackSpeed = baseSpeed * 1.2f;
        }
        // 필요하면 공격 쿨타임 재계산도 가능
    }

    private void CreateOccupiedChild(Tile tile)
    {
        Transform exist = tile.transform.Find("Occupied");
        if (exist != null) return;

        GameObject occupiedObj = new GameObject("Occupied");
        occupiedObj.transform.SetParent(tile.transform, false);
        occupiedObj.transform.localPosition = Vector3.zero;
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
