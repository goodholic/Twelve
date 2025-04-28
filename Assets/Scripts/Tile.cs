using UnityEngine;
using UnityEngine.UI;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 2D 타일(칸).
/// 한 타일에 여러 캐릭터 배치 가능하도록 Occupied 체크 제거.
/// Walkable이면 아군 몬스터 배치, Placable이면 터렛 배치.
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
    public GameObject placablePrefab;
    public GameObject occupiedPrefab;

    [SerializeField] private GameObject currentVisual;

    [Header("Optional: Button for OnClick")]
    public Button tileButton;

    // (row, col, tileIndex) 디버그용
    [HideInInspector] public int row;
    [HideInInspector] public int column;
    [HideInInspector] public int tileIndex;

    /// <summary>
    /// 자식 중 "Placable" 오브젝트가 있는지
    /// </summary>
    public bool IsPlacable()
    {
        return (transform.Find("Placable") != null);
    }

    /// <summary>
    /// 자식 중 "Occupied" 오브젝트가 있는지 (이제 무시 가능)
    /// </summary>
    public bool IsOccupied()
    {
        return (transform.Find("Occupied") != null);
    }

    /// <summary>
    /// 자식 중 "Walkable" 오브젝트가 있는지
    /// </summary>
    public bool IsWalkable()
    {
        return (transform.Find("Walkable") != null);
    }

    /// <summary>
    /// 한 타일에 여러 캐릭터 배치 가능하므로 'Occupied' 체크 제거
    /// </summary>
    public bool CanPlaceCharacter()
    {
        // return (!IsWalkable() && IsPlacable() && !IsOccupied());
        return (!IsWalkable() && IsPlacable());
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

        if (Application.isPlaying)
        {
            UpdateTileVisual_Runtime();
        }
    }

    public void OnClickPlacableTile()
    {
        Debug.Log($"Tile 클릭: {name} (Index={tileIndex}, row={row}, col={column})");

        if (CanPlaceCharacter())
        {
            PlacementManager mgr = PlacementManager.Instance;
            if (mgr != null)
            {
                mgr.PlaceCharacterOnTile(this);
            }
            else
            {
                Debug.LogWarning("PlacementManager.Instance가 null입니다.");
            }
        }
        else
        {
            bool p = IsPlacable();
            bool w = IsWalkable();
            bool o = IsOccupied();
            Debug.Log($"[Tile] 배치 불가: Placable={p}, Walkable={w}, Occupied={o}");
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

    public void RefreshTileVisual()
    {
        RefreshInEditor();
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
        bool p = IsPlacable();
        bool w = IsWalkable();
        bool o = IsOccupied();

        if (w && !p && !o) return walkablePrefab;
        if (!w && p && !o) return placablePrefab;
        if (o) return occupiedPrefab;
        return null;
    }

    // ★ '한 타일에 2개 자식 이상 못 넣게'라는 제한은 제거
    private void LateUpdate()
    {
        // 여기서는 '타일 자식' 그림 2개(placable/empty)만 유지하면 되니,
        // 원하는 만큼 여유를 둬도 됩니다.
        while (transform.childCount > 2)
        {
            Transform child = transform.GetChild(transform.childCount - 1);
            Destroy(child.gameObject);
        }
    }
}
