using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementManager : MonoBehaviour
{
    [Header("Placement Settings")]
    [Tooltip("배치 가능한 캐릭터 프리팹 (1성, 2성, 3성 등 다양하게 배열로 관리 가능)")]
    public GameObject[] characterPrefabs;

    [Tooltip("현재 선택된 캐릭터 인덱스")]
    public int currentCharacterIndex = 0;

    [Tooltip("카메라")]
    public Camera mainCamera;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        // 마우스 왼쪽 클릭 시 캐릭터 배치 시도
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            TryPlaceCharacter();
        }

        // 예시: 숫자 키로 캐릭터 변경 (1 ~ 4)
        if (Input.GetKeyDown(KeyCode.Alpha1)) currentCharacterIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentCharacterIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) currentCharacterIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) currentCharacterIndex = 3;
    }

    /// <summary>
    /// UI 영역 위에 마우스가 있는지 여부 확인
    /// </summary>
    /// <returns></returns>
    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// 현재 마우스 위치의 타일을 확인 후, 배치 가능하면 캐릭터 생성
    /// </summary>
    private void TryPlaceCharacter()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Tile tile = hit.collider.GetComponent<Tile>();
            if (tile != null)
            {
                if (tile.CanPlaceCharacter())
                {
                    // 캐릭터 생성
                    GameObject characterObj = Instantiate(characterPrefabs[currentCharacterIndex], tile.transform.position, Quaternion.identity);
                    Character character = characterObj.GetComponent<Character>();
                    if (character != null)
                    {
                        // 캐릭터에 현재 타일 정보 등록
                        character.currentTile = tile;
                    }

                    // 타일 점유 상태로 변경
                    tile.isOccupied = true;
                }
                else
                {
                    Debug.Log("이 타일에 캐릭터를 배치할 수 없습니다.");
                }
            }
        }
    }
}
