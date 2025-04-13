// Assets\Scripts\PlacementManager.cs

using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementManager : MonoBehaviour
{
    [Header("Placement Settings")]
    [Tooltip("배치 가능한 캐릭터 프리팹 (1성, 2성, 3성 등 다양하게 배열로 관리 가능)")]
    public GameObject[] characterPrefabs;

    [Tooltip("현재 선택된 캐릭터 인덱스")]
    public int currentCharacterIndex = 0;

    [Tooltip("메인 카메라")]
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
        // 마우스 왼쪽 클릭 시 캐릭터 배치
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            TryPlaceCharacter();
        }

        // 예시: 숫자키로 캐릭터 선택
        if (Input.GetKeyDown(KeyCode.Alpha1)) currentCharacterIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentCharacterIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) currentCharacterIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) currentCharacterIndex = 3;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

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
                    GameObject characterObj = Instantiate(
                        characterPrefabs[currentCharacterIndex],
                        tile.transform.position,
                        Quaternion.identity
                    );

                    Character character = characterObj.GetComponent<Character>();
                    if (character != null)
                    {
                        character.currentTile = tile;
                    }

                    tile.isOccupied = true;
                }
                else
                {
                    Debug.Log("이 타일에는 캐릭터 배치 불가");
                }
            }
        }
    }
}
