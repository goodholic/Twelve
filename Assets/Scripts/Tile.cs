using UnityEngine;
using UnityEngine.UI;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 2D 타일(칸).
/// 내부에는 "Walkable"/"Walkable2"/"Placable"/"Picable2"/"Occupied"/"Occupied2" 자식 오브젝트가 붙어,
/// 어떤 종류(1P/2P 경로, 1P/2P 배치 등)인지 구분.
/// Button을 통해 OnClickPlacableTile() -> PlacementManager.PlaceCharacterOnTile(this) 호출.
/// </summary>
[RequireComponent(typeof(Image), typeof(BoxCollider2D))]
public class Tile : MonoBehaviour
{
    [Header("Tile Color Settings (시각 효과)")]
    [SerializeField] private Image tileImage;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color blockedColor = Color.red;

    [Header("Tile Prefabs (시각용)")]
    public GameObject walkablePrefab;
    public GameObject walkable2Prefab;
    public GameObject placablePrefab;
    [Tooltip("picable2 전용 프리팹(원한다면 연결)")]
    public GameObject picable2Prefab;
    public GameObject occupiedPrefab;
    [Tooltip("occupied2 전용 프리팹(원한다면 연결)")]
    public GameObject occupied2Prefab;

    [SerializeField] private GameObject currentVisual;

    [Header("Optional: Button for OnClick")]
    public Button tileButton;

    // 디버그용
    [HideInInspector] public int row;
    [HideInInspector] public int column;
    [HideInInspector] public int tileIndex;

    // ------------- 상태 체크 함수들 -------------
    public bool IsPlacable()
    {
        // "Placable" 자식 존재 여부
        return (transform.Find("Placable") != null);
    }

    public bool IsPicable2()
    {
        // "Picable2" 자식 존재 여부
        return (transform.Find("Picable2") != null);
    }

    public bool IsOccupied()
    {
        // "Occupied" 자식 존재 여부
        return (transform.Find("Occupied") != null);
    }

    public bool IsOccupied2()
    {
        // "Occupied2" 자식 존재 여부
        return (transform.Find("Occupied2") != null);
    }

    public bool IsWalkable()
    {
        // "Walkable" 자식 존재 여부
        return (transform.Find("Walkable") != null);
    }

    public bool IsWalkable2()
    {
        // "Walkable2" 자식 존재 여부
        return (transform.Find("Walkable2") != null);
    }

    /// <summary>
    /// "배치가 가능한가?"를 단순 판별.
    /// 지금은 Walkable / Walkable2 / Placable / Picable2 / Occupied / Occupied2 중
    /// 하나라도 있으면 true로 처리 (실제 로직은 자유롭게 커스터마이징).
    /// </summary>
    public bool CanPlaceCharacter()
    {
        bool hasAnyType =
            IsWalkable() || IsWalkable2() ||
            IsPlacable() || IsPicable2() ||
            IsOccupied() || IsOccupied2();

        return hasAnyType;
    }

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

        // 런타임 시점 비주얼 갱신
        if (Application.isPlaying)
        {
            UpdateTileVisual_Runtime();
        }
    }

    public void OnClickPlacableTile()
    {
        Debug.Log($"[Tile] 클릭됨: {name} (Index={tileIndex}, row={row}, col={column})");

        if (CanPlaceCharacter())
        {
            var mgr = PlacementManager.Instance;
            if (mgr != null)
            {
                mgr.PlaceCharacterOnTile(this);
            }
            else
            {
                Debug.LogWarning("[Tile] PlacementManager.Instance가 null입니다.");
            }
        }
        else
        {
            Debug.Log($"[Tile] 배치 불가 상태. (IsWalkable={IsWalkable()}, IsWalkable2={IsWalkable2()}, IsPlacable={IsPlacable()}, IsPicable2={IsPicable2()}, IsOccupied={IsOccupied()}, IsOccupied2={IsOccupied2()})");
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

        // 어떤 프리팹을 붙일지 결정
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

    /// <summary>
    /// 자식오브젝트("Walkable","Walkable2","Placable","Picable2","Occupied","Occupied2") 중
    /// 무엇이 있느냐에 따라 다른 프리팹(이미지)로 바꾸는 로직 (시각용).
    /// </summary>
    private GameObject SelectVisualPrefab()
    {
        bool w1 = IsWalkable();
        bool w2 = IsWalkable2();
        bool p1 = IsPlacable();
        bool p2 = IsPicable2();
        bool o1 = IsOccupied();
        bool o2 = IsOccupied2();

        // 우선순위 예: Walkable2 > Walkable > Picable2 > Placable > Occupied2 > Occupied
        if (w2 && walkable2Prefab != null) return walkable2Prefab;
        if (w1 && walkablePrefab != null)  return walkablePrefab;
        if (p2 && picable2Prefab != null)  return picable2Prefab;
        if (p1 && placablePrefab != null)  return placablePrefab;
        if (o2 && occupied2Prefab != null) return occupied2Prefab;
        if (o1 && occupiedPrefab != null)  return occupiedPrefab;

        // 아무것도 없으면 null
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

    /// <summary>
    /// (마우스오버 등) 임시 하이라이트
    /// </summary>
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
            tileImage.color = defaultColor;
        }
    }
}
