using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 4개 버튼 중 하나 클릭 -> currentCharacterIndex 저장.
/// Tile 클릭 -> PlaceCharacterOnTile() -> 해당 Tile 자식에 캐릭터 Instantiate + "Occupied" 자식 오브젝트 생성.
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
            Debug.LogWarning("<color=red>씬에 EventSystem이 없습니다! UI 버튼 클릭 불가!</color>");
        }
    }

    private void Update()
    {
        // 선택) 키보드로 1,2,3,4 누르면 인덱스 바꾸기
        if (Input.GetKeyDown(KeyCode.Alpha1)) currentCharacterIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentCharacterIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) currentCharacterIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) currentCharacterIndex = 3;
    }

    public void OnClickSelectUnit(int index)
    {
        currentCharacterIndex = index;
        Debug.Log($"[PlacementManager] 선택된 유닛 인덱스: {currentCharacterIndex}");
    }

    public void PlaceCharacterOnTile(Tile tile)
    {
        // 1) DB 검사
        if (characterDatabase == null || characterDatabase.characters == null || characterDatabase.characters.Length == 0)
        {
            Debug.LogWarning("PlacementManager: characterDatabase가 비어있음.");
            return;
        }
        // 2) 인덱스 검사
        if (currentCharacterIndex < 0 || currentCharacterIndex >= characterDatabase.characters.Length)
        {
            Debug.LogWarning($"PlacementManager: 잘못된 인덱스({currentCharacterIndex})");
            return;
        }

        // 3) 캐릭터 데이터
        CharacterData data = characterDatabase.characters[currentCharacterIndex];
        if (data == null || data.spawnPrefab == null)
        {
            Debug.LogWarning($"PlacementManager: {currentCharacterIndex}번 캐릭터 데이터/프리팹이 null.");
            return;
        }

        // 4) Tile 검사 + 배치 가능 여부
        if (tile == null)
        {
            Debug.Log("PlacementManager: tile이 null");
            return;
        }
        if (!tile.CanPlaceCharacter())
        {
            Debug.Log($"PlacementManager: {tile.name}에 배치 불가 (Placable=false, Occupied=true 등)");
            return;
        }

        // 5) 캐릭터 Instantiate
        GameObject charObj = Instantiate(data.spawnPrefab, tile.transform);

        // UI인지 확인
        RectTransform tileRect = tile.GetComponent<RectTransform>();
        RectTransform charRect = charObj.GetComponent<RectTransform>();
        if (tileRect != null && charRect != null)
        {
            // UI
            charRect.anchoredPosition = Vector2.zero;
            charRect.localRotation = Quaternion.identity;
        }
        else
        {
            // 일반 Transform
            charObj.transform.localPosition = Vector3.zero;
        }

        // 6) Character 스크립트 세팅
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
        }

        // 7) **자식에 "Occupied" 오브젝트 생성** => Occupied 상태
        CreateOccupiedChild(tile);

        Debug.Log($"[PlacementManager] [{data.characterName}] 캐릭터 소환 완료! (cost={data.cost})");
    }

    /// <summary>
    /// Tile의 자식으로 "Occupied"라는 이름의 빈 오브젝트를 만든다.
    /// => Tile.cs에서 transform.Find("Occupied") != null 이면 Occupied=true 로 인식
    /// </summary>
    private void CreateOccupiedChild(Tile tile)
    {
        // 혹시 이미 "Occupied"가 있으면 중복 생성하지 않음
        Transform exist = tile.transform.Find("Occupied");
        if (exist != null)
        {
            return;
        }

        GameObject occupiedObj = new GameObject("Occupied");
        occupiedObj.transform.SetParent(tile.transform, false);
        occupiedObj.transform.localPosition = Vector3.zero;

        // 필요하다면 여기서 Sprite나 Image를 붙여 시각화 할 수도 있음
        // e.g. occupiedObj.AddComponent<Image>() ...
    }
}
