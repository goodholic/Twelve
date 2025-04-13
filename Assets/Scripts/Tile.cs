// Assets\Scripts\Tile.cs

using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;  // PrefabUtility, EditorApplication 사용
#endif

/// <summary>
/// 맵 타일을 나타내는 스크립트.
/// 에디터 모드에서 OnValidate 시점에 DestroyImmediate를 호출하지 않고,
/// 런타임에서만 Destroy/Instantiate 되도록 작성.
/// </summary>
public class Tile : MonoBehaviour
{
    [Header("Tile Settings")]
    [Tooltip("몬스터가 지나갈 수 있는 타일이면 true, 아니면 false")]
    public bool isWalkable = false;

    [Tooltip("플레이어가 캐릭터를 배치할 수 있는 타일이면 true, 아니면 false")]
    public bool isPlacable = true;

    [Tooltip("현재 타일에 캐릭터가 배치되어 있는지 여부(전투 중 점유 상태)")]
    public bool isOccupied = false;

    [Header("Tile Color Settings")]
    [SerializeField] private Renderer tileRenderer;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color blockedColor = Color.red;

    [Header("Tile Prefabs (Walkable / Placable / Occupied)")]
    public GameObject walkablePrefab;
    public GameObject placablePrefab;
    public GameObject occupiedPrefab;

    [Tooltip("현재 표시 중인 타일 비주얼(동적으로 생성)")]
    [SerializeField] private GameObject currentVisual;

    private void Start()
    {
        // 초기 색상 세팅
        if (tileRenderer == null)
        {
            tileRenderer = GetComponent<Renderer>();
        }
        if (tileRenderer != null)
        {
            tileRenderer.material.color = defaultColor;
        }

        // 플레이 모드일 때만 자동으로 비주얼 갱신
        if (Application.isPlaying)
        {
            UpdateTileVisual_Runtime();
        }
    }

    /// <summary>
    /// (OnValidate 대신) 에디터에서 수동 Refresh할 때 사용
    /// </summary>
    public void RefreshInEditor()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UpdateTileVisual_Runtime();
        }
        else
        {
            UpdateTileVisual_Editor();
        }
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 모드(미플레이)에서 DestroyImmediate / InstantiatePrefab
    /// </summary>
    private void UpdateTileVisual_Editor()
    {
        if (currentVisual != null)
        {
            // 혹시 currentVisual이 에셋이면 제거 불가이므로 체크
            if (!IsAssetObject(currentVisual))
            {
                DestroyImmediate(currentVisual, false);
            }
            currentVisual = null;
        }

        GameObject prefabToUse = SelectPrefabBasedOnState();
        if (prefabToUse != null)
        {
            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToUse, transform);
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;

            // 혹시 프리팹 내부에 Tile.cs가 있으면 제거(자식 타일 무한증식 방지)
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

    /// <summary>
    /// 런타임(플레이 중) Destroy + Instantiate
    /// </summary>
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

        GameObject prefabToUse = SelectPrefabBasedOnState();
        if (prefabToUse != null)
        {
            GameObject newObj = Instantiate(prefabToUse, transform);
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;
            RemoveTileScriptsAtRuntime(newObj);

            currentVisual = newObj;
        }
    }

#if UNITY_EDITOR
    private bool IsPrefabAsset(GameObject obj)
    {
        return PrefabUtility.IsPartOfPrefabAsset(obj) || AssetDatabase.Contains(obj);
    }
#else
    private bool IsPrefabAsset(GameObject obj) { return false; }
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
    /// 타일 상태(isWalkable/isPlacable/isOccupied)에 따라 사용할 프리팹 결정
    /// </summary>
    private GameObject SelectPrefabBasedOnState()
    {
        if (isWalkable && !isPlacable && !isOccupied)
        {
            return walkablePrefab;
        }
        else if (!isWalkable && isPlacable && !isOccupied)
        {
            return placablePrefab;
        }
        else if (!isWalkable && !isPlacable && isOccupied)
        {
            return occupiedPrefab;
        }
        return null;
    }

    public void RefreshTileVisual()
    {
        if (Application.isPlaying)
        {
            UpdateTileVisual_Runtime();
        }
#if UNITY_EDITOR
        else
        {
            UpdateTileVisual_Editor();
        }
#endif
    }

    // ------------------- 기존 기능 -------------------
    public void HighlightTile()
    {
        if (tileRenderer != null)
        {
            tileRenderer.material.color = (isPlacable && !isOccupied) ? highlightColor : blockedColor;
        }
    }

    public void ResetHighlight()
    {
        if (tileRenderer != null)
        {
            tileRenderer.material.color = defaultColor;
        }
    }

    public bool CanPlaceCharacter()
    {
        if (isWalkable) return false;
        return isPlacable && !isOccupied;
    }
}
