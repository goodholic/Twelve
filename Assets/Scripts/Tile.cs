// ===== Tile.cs =====
using UnityEngine;
using UnityEngine.UI;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Image), typeof(BoxCollider2D))]
public class Tile : MonoBehaviour
{
    [Header("Region Select")]
    [Tooltip("지역1인지 지역2인지 여부를 체크할 수 있습니다. true면 지역2, false면 지역1로 간주")]
    public bool isRegion2 = false;

    [Header("Tile Color Settings")]
    [SerializeField] private Image tileImage;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color blockedColor = Color.red;
    [SerializeField] private Color summonableColor = Color.green;  // 소환 가능 타일 색상

    [Header("Tile Prefabs (시각용)")]
    public GameObject walkablePrefab;
    public GameObject walkable2Prefab;
    public GameObject walkableLeftPrefab;      // 왼쪽 라인
    public GameObject walkableCenterPrefab;    // 중앙 라인
    public GameObject walkableRightPrefab;     // 오른쪽 라인
    public GameObject walkable2LeftPrefab;
    public GameObject walkable2CenterPrefab;
    public GameObject walkable2RightPrefab;
    public GameObject placablePrefab;
    public GameObject placable2Prefab;
    public GameObject placeTilePrefab;
    public GameObject placed2Prefab;

    [SerializeField] private GameObject currentVisual;

    [Header("Optional: Button for OnClick")]
    public Button tileButton;

    [Header("게임 기획서 - 3라인 시스템")]
    [Tooltip("이 타일이 속한 라인 (Left/Center/Right)")]
    public RouteType belongingRoute = RouteType.Center;

    [HideInInspector] public int row;
    [HideInInspector] public int column;
    [HideInInspector] public int tileIndex;

    // 타일 위의 캐릭터 참조 (게임 기획서: 타일 기반 소환)
    private Character occupyingCharacter;

    private void Start()
    {
        if (tileImage == null)
        {
            tileImage = GetComponent<Image>();
        }
        if (tileImage != null)
        {
            tileImage.color = defaultColor;
            tileImage.raycastTarget = true;
        }

        if (tileButton != null)
        {
            if (tileButton.targetGraphic == null && tileImage != null)
            {
                tileButton.targetGraphic = tileImage;
            }
            tileButton.onClick.RemoveAllListeners();
            tileButton.onClick.AddListener(OnClickPlacableTile);
        }

        if (Application.isPlaying)
        {
            UpdateTileVisual_Runtime();
        }
        
        // 타일 타입에 따른 라인 자동 설정
        UpdateBelongingRoute();
    }

    /// <summary>
    /// 타일 타입에 따라 속한 라인을 자동으로 설정
    /// </summary>
    private void UpdateBelongingRoute()
    {
        if (IsWalkableLeft() || IsWalkable2Left())
        {
            belongingRoute = RouteType.Left;
        }
        else if (IsWalkableCenter() || IsWalkable2Center())
        {
            belongingRoute = RouteType.Center;
        }
        else if (IsWalkableRight() || IsWalkable2Right())
        {
            belongingRoute = RouteType.Right;
        }
    }

    /// <summary>
    /// 이 타일이 'Placable' 형태인지 체크 (소환 가능 타일)
    /// </summary>
    public bool IsPlacable()
    {
        return (transform.Find("Placable") != null);
    }

    /// <summary>
    /// 이 타일이 'Placable2' 형태인지 체크
    /// </summary>
    public bool IsPlacable2()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("placable2"))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 이 타일이 'PlaceTile' 형태인지 (캐릭터가 배치된 타일)
    /// </summary>
    public bool IsPlaceTile()
    {
        // 자기 자신의 이름이 PlacedTile인 경우
        if (gameObject.name.ToLower().Contains("placedtile") || gameObject.name.ToLower().Contains("placetile"))
        {
            return true;
        }
        
        // 자식 중에 PlaceTile이 있는 경우
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("placetile") && !childName.Contains("placed2"))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 이 타일이 'Placed2' 형태인지
    /// </summary>
    public bool IsPlaced2()
    {
        // 자기 자신의 이름이 Placed2인 경우
        if (gameObject.name.ToLower().Contains("placed2"))
        {
            return true;
        }
        
        // 자식 중에 Placed2가 있는 경우
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("placed2"))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 이 타일이 'Walkable' 형태인지 (몬스터 이동 경로)
    /// </summary>
    public bool IsWalkable()
    {
        return (transform.Find("Walkable") != null);
    }

    /// <summary>
    /// 이 타일이 'Walkable2' 형태인지
    /// </summary>
    public bool IsWalkable2()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("walkable2"))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 이 타일이 'WalkableLeft' 형태인지 (왼쪽 라인)
    /// </summary>
    public bool IsWalkableLeft()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("walkableleft") && !childName.Contains("walkable2"))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 이 타일이 'WalkableCenter' 형태인지 (중앙 라인)
    /// </summary>
    public bool IsWalkableCenter()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("walkablecenter") && !childName.Contains("walkable2"))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 이 타일이 'WalkableRight' 형태인지 (오른쪽 라인)
    /// </summary>
    public bool IsWalkableRight()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("walkableright") && !childName.Contains("walkable2"))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 이 타일이 'Walkable2Left' 형태인지
    /// </summary>
    public bool IsWalkable2Left()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("walkable2left"))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 이 타일이 'Walkable2Center' 형태인지
    /// </summary>
    public bool IsWalkable2Center()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("walkable2center"))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 이 타일이 'Walkable2Right' 형태인지
    /// </summary>
    public bool IsWalkable2Right()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("walkable2right"))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 이 타일이 캐릭터 배치 가능한 상태인지 (게임 기획서: 타일 기반 소환)
    /// </summary>
    public bool CanPlaceCharacter()
    {
        // 이미 캐릭터가 있으면 배치 불가
        if (occupyingCharacter != null) return false;

        // Placable 타일이거나 빈 PlaceTile만 배치 가능
        bool isPlacableType = IsPlacable() || IsPlacable2();
        bool isEmptyPlacedType = (IsPlaceTile() || IsPlaced2()) && occupyingCharacter == null;

        return isPlacableType || isEmptyPlacedType;
    }

    /// <summary>
    /// 타일 클릭 시 로직 (게임 기획서: 원 버튼 소환)
    /// </summary>
    public void OnClickPlacableTile()
    {
        Debug.Log($"[Tile] 클릭됨: {name} (Index={tileIndex}, row={row}, col={column}, Route={belongingRoute})");
        
        // 타일 상태 디버그 정보
        Debug.Log($"[Tile] 타일 상태 - CanPlace:{CanPlaceCharacter()}, HasCharacter:{occupyingCharacter != null}");

        // 추가: 만약 removeMode가 true라면 제거 로직
        if (PlacementManager.Instance != null && PlacementManager.Instance.removeMode)
        {
            PlacementManager.Instance.RemoveCharacterOnTile(this);
            return;
        }

        // 소환 가능한 타일인지 확인
        if (CanPlaceCharacter())
        {
            var mgr = PlacementManager.Instance;
            if (mgr != null)
            {
                // 랜덤 캐릭터 소환 시도
                mgr.PlaceCharacterOnTile(this);
            }
            else
            {
                Debug.LogWarning("[Tile] PlacementManager.Instance가 null");
            }
        }
        else
        {
            Debug.Log($"[Tile] 배치 불가 상태. (이미 캐릭터가 있거나 소환 불가능한 타일)");
        }
    }

    /// <summary>
    /// 타일에 캐릭터 설정
    /// </summary>
    public void SetOccupyingCharacter(Character character)
    {
        occupyingCharacter = character;
        UpdateTileColor();
    }

    /// <summary>
    /// 타일의 캐릭터 제거
    /// </summary>
    public void RemoveOccupyingCharacter()
    {
        occupyingCharacter = null;
        UpdateTileColor();
    }

    /// <summary>
    /// 타일에 있는 캐릭터 반환
    /// </summary>
    public Character GetOccupyingCharacter()
    {
        return occupyingCharacter;
    }

    /// <summary>
    /// 타일 색상 업데이트
    /// </summary>
    private void UpdateTileColor()
    {
        if (tileImage == null) return;

        if (occupyingCharacter != null)
        {
            // 캐릭터가 있는 타일
            tileImage.color = blockedColor;
        }
        else if (CanPlaceCharacter())
        {
            // 소환 가능한 타일
            tileImage.color = summonableColor;
        }
        else
        {
            // 일반 타일
            tileImage.color = defaultColor;
        }
    }

#if UNITY_EDITOR
    public void RefreshInEditor()
    {
        if (Application.isPlaying)
        {
            UpdateTileVisual_Runtime();
        }
        else
        {
            UpdateTileVisual_Editor();
        }
    }

    private void UpdateTileVisual_Editor()
    {
        if (currentVisual != null)
        {
            if (!IsAssetObject(currentVisual))
            {
                DestroyImmediate(currentVisual, false);
            }
            currentVisual = null;
        }

        GameObject prefabToUse = SelectVisualPrefab();
        if (prefabToUse != null)
        {
            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToUse, transform);
            if (newObj.TryGetComponent<RectTransform>(out RectTransform rt))
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localRotation = Quaternion.identity;
            }
            RemoveTileScriptsInEditor(newObj);
            currentVisual = newObj;
        }
    }

    private bool IsAssetObject(UnityEngine.Object obj)
    {
        return PrefabUtility.IsPartOfPrefabAsset(obj) || AssetDatabase.Contains(obj);
    }

    private void RemoveTileScriptsInEditor(GameObject root)
    {
        Tile[] tiles = root.GetComponentsInChildren<Tile>(true);
        foreach (Tile t in tiles)
        {
            if (t != this)
            {
                DestroyImmediate(t, false);
            }
        }
    }
#endif

    private void UpdateTileVisual_Runtime()
    {
        if (currentVisual != null)
        {
#if UNITY_EDITOR
            if (!IsPrefabAsset(currentVisual))
            {
                Destroy(currentVisual);
            }
#else
            Destroy(currentVisual);
#endif
            currentVisual = null;
        }

        GameObject prefabToUse = SelectVisualPrefab();
        if (prefabToUse != null)
        {
            GameObject newObj = Instantiate(prefabToUse, transform);
            if (newObj.TryGetComponent<RectTransform>(out RectTransform rt))
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localRotation = Quaternion.identity;
            }
            RemoveTileScriptsAtRuntime(newObj);
            currentVisual = newObj;
        }
    }

#if UNITY_EDITOR
    private bool IsPrefabAsset(GameObject obj)
    {
        return PrefabUtility.IsPartOfPrefabAsset(obj) || AssetDatabase.Contains(obj);
    }
#endif

    private void RemoveTileScriptsAtRuntime(GameObject root)
    {
        Tile[] tiles = root.GetComponentsInChildren<Tile>(true);
        foreach (Tile t in tiles)
        {
            if (t != this)
            {
                Destroy(t);
            }
        }
    }

    private GameObject SelectVisualPrefab()
    {
        bool w1 = IsWalkable();
        bool w2 = IsWalkable2();
        bool wLeft = IsWalkableLeft();
        bool wCenter = IsWalkableCenter();
        bool wRight = IsWalkableRight();
        bool w2Left = IsWalkable2Left();
        bool w2Center = IsWalkable2Center();
        bool w2Right = IsWalkable2Right();
        bool p1 = IsPlacable();
        bool p2 = IsPlacable2();
        bool pTile = IsPlaceTile();
        bool p2Tile = IsPlaced2();

        // Walkable2 타입들 우선 체크 (3라인 시스템)
        if (w2Left && walkable2LeftPrefab != null) return walkable2LeftPrefab;
        if (w2Center && walkable2CenterPrefab != null) return walkable2CenterPrefab;
        if (w2Right && walkable2RightPrefab != null) return walkable2RightPrefab;
        if (w2 && walkable2Prefab != null) return walkable2Prefab;
        
        // Walkable 타입들 체크 (3라인 시스템)
        if (wLeft && walkableLeftPrefab != null) return walkableLeftPrefab;
        if (wCenter && walkableCenterPrefab != null) return walkableCenterPrefab;
        if (wRight && walkableRightPrefab != null) return walkableRightPrefab;
        if (w1 && walkablePrefab  != null) return walkablePrefab;
        
        // 기타 타입들
        if (p2 && placable2Prefab != null) return placable2Prefab;
        if (p1 && placablePrefab  != null) return placablePrefab;
        if (p2Tile && placed2Prefab != null) return placed2Prefab;
        if (pTile && placeTilePrefab  != null) return placeTilePrefab;

        return null;
    }

    public void RefreshTileVisual()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
            UpdateTileVisual_Runtime();
        else
            UpdateTileVisual_Editor();
#else
        UpdateTileVisual_Runtime();
#endif
    }

    public void HighlightTile()
    {
        if (tileImage != null)
        {
            tileImage.color = CanPlaceCharacter() ? highlightColor : blockedColor;
        }
    }

    public void ResetHighlight()
    {
        if (tileImage != null)
        {
            UpdateTileColor();
        }
    }

    /// <summary>
    /// 타일을 Placable 상태로 변경
    /// </summary>
    public void SetPlacable()
    {
        // 기존 자식 제거
        ClearCurrentVisual();
        
        // Placable 프리팹 생성
        if (placablePrefab != null)
        {
            currentVisual = Instantiate(placablePrefab, transform);
            RemoveTileScriptsAtRuntime(currentVisual);
        }
        
        Debug.Log($"[Tile] {name}을 Placable 상태로 변경");
    }
    
    /// <summary>
    /// 타일을 Placable2 상태로 변경
    /// </summary>
    public void SetPlacable2()
    {
        // 기존 자식 제거
        ClearCurrentVisual();
        
        // Placable2 프리팹 생성
        if (placable2Prefab != null)
        {
            currentVisual = Instantiate(placable2Prefab, transform);
            RemoveTileScriptsAtRuntime(currentVisual);
        }
        
        Debug.Log($"[Tile] {name}을 Placable2 상태로 변경");
    }
    
    /// <summary>
    /// 현재 비주얼을 제거
    /// </summary>
    private void ClearCurrentVisual()
    {
        if (currentVisual != null)
        {
            if (Application.isPlaying)
            {
                Destroy(currentVisual);
            }
            else
            {
                DestroyImmediate(currentVisual);
            }
            currentVisual = null;
        }
        
        // 모든 자식 프리팹 제거 (Placable, PlaceTile 등)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.gameObject != currentVisual)
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}