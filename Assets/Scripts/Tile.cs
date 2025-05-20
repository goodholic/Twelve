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

    [Header("Tile Prefabs (시각용)")]
    public GameObject walkablePrefab;
    public GameObject walkable2Prefab;
    public GameObject placablePrefab;
    public GameObject placable2Prefab;
    public GameObject placeTilePrefab; // 원래 occupiedPrefab → placeTilePrefab
    public GameObject placed2Prefab;   // 원래 occupied2Prefab → placed2Prefab

    [SerializeField] private GameObject currentVisual;

    [Header("Optional: Button for OnClick")]
    public Button tileButton;

    [HideInInspector] public int row;
    [HideInInspector] public int column;
    [HideInInspector] public int tileIndex;

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

    public bool IsPlacable()
    {
        return (transform.Find("Placable") != null);
    }

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

    public bool IsPlaceTile()
    {
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

    public bool IsPlaced2()
    {
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

    public bool IsWalkable()
    {
        return (transform.Find("Walkable") != null);
    }

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
    /// 이 타일이 캐릭터 배치 가능한지 여부
    /// </summary>
    public bool CanPlaceCharacter()
    {
        bool hasAnyType =
            IsWalkable() || IsWalkable2() ||
            IsPlacable() || IsPlacable2() ||
            IsPlaceTile() || IsPlaced2();

        return hasAnyType;
    }

    public void OnClickPlacableTile()
    {
        Debug.Log($"[Tile] 클릭됨: {name} (Index={tileIndex}, row={row}, col={column})");

        // ================== [수정한 부분] ==================
        // removeMode가 true라면, PlacementManager.Instance.RemoveCharacterOnTile(this) 호출
        if (PlacementManager.Instance != null && PlacementManager.Instance.removeMode)
        {
            PlacementManager.Instance.RemoveCharacterOnTile(this);
            return; // 클릭 이벤트는 여기서 종료
        }
        // ==================================================

        // 기존 로직: 캐릭터 배치
        if (CanPlaceCharacter())
        {
            var mgr = PlacementManager.Instance;
            if (mgr != null)
            {
                mgr.PlaceCharacterOnTile(this);
            }
            else
            {
                Debug.LogWarning("[Tile] PlacementManager.Instance가 null");
            }
        }
        else
        {
            Debug.Log(
                $"[Tile] 배치 불가 상태. (IsWalkable={IsWalkable()}, IsWalkable2={IsWalkable2()}, " +
                $"IsPlacable={IsPlacable()}, IsPlacable2={IsPlacable2()}, " +
                $"IsPlaceTile={IsPlaceTile()}, IsPlaced2={IsPlaced2()})"
            );
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
        bool p1 = IsPlacable();
        bool p2 = IsPlacable2();
        bool pTile = IsPlaceTile();
        bool p2Tile = IsPlaced2();

        if (w2 && walkable2Prefab != null) return walkable2Prefab;
        if (w1 && walkablePrefab  != null) return walkablePrefab;
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
            tileImage.color = defaultColor;
        }
    }
}
