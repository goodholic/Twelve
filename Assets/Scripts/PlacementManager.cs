// Assets\Scripts\PlacementManager.cs

using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 2D에서 마우스로 타일을 클릭했을 때 캐릭터를 생성/배치
/// </summary>
public class PlacementManager : MonoBehaviour
{
    [Header("Placement Settings")]
    [Tooltip("배치 가능한 캐릭터 프리팹 배열")]
    public GameObject[] characterPrefabs;

    [Tooltip("현재 선택된 캐릭터 인덱스")]
    public int currentCharacterIndex = 0;

    [Tooltip("2D 카메라 (Orthographic)")]
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
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            TryPlaceCharacter2D();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) currentCharacterIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentCharacterIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) currentCharacterIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) currentCharacterIndex = 3;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// 2D Raycast를 이용해 Tile을 찾고, 배치 가능하면 캐릭터 생성
    /// </summary>
    private void TryPlaceCharacter2D()
    {
        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit2D = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit2D.collider != null)
        {
            Tile tile = hit2D.collider.GetComponent<Tile>();
            if (tile != null)
            {
                if (tile.CanPlaceCharacter())
                {
                    // 캐릭터 생성
                    Vector3 spawnPos = tile.transform.position; // 타일의 위치
                    GameObject characterObj = Instantiate(characterPrefabs[currentCharacterIndex], spawnPos, Quaternion.identity);

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
