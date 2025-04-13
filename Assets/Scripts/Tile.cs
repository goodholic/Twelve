// Assets\Scripts\Tile.cs

using UnityEngine;
using UnityEngine.UI; // Image 사용
using System;
#if UNITY_EDITOR
using UnityEditor;  
#endif

/// <summary>
/// 2D 타일(칸)에 해당하는 스크립트.
/// Image 컴포넌트를 이용해 색상 변경, 
/// 캐릭터 배치 가능 여부(isPlacable), 몬스터 경로 여부(isWalkable) 등.
/// </summary>
[RequireComponent(typeof(Image), typeof(BoxCollider2D))]
public class Tile : MonoBehaviour
{
    [Header("Tile Settings")]
    [Tooltip("몬스터가 지나갈 수 있는 타일이면 true, 아니면 false")]
    public bool isWalkable = false;

    [Tooltip("플레이어가 캐릭터를 배치할 수 있는 타일이면 true, 아니면 false")]
    public bool isPlacable = true;

    [Tooltip("현재 타일에 캐릭터가 배치되어 있는지 여부")]
    public bool isOccupied = false;

    [Header("Tile Color Settings")]
    [SerializeField] private Image tileImage; // 2D에서 Image로 색상 제어
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
        // Image 할당
        if (tileImage == null)
        {
            tileImage = GetComponent<Image>();
        }
        if (tileImage != null)
        {
            tileImage.color = defaultColor;
        }

        // 플레이 모드일 때만 비주얼 갱신
        if (Application.isPlaying)
        {
            UpdateTileVisual_Runtime();
        }
    }

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

        GameObject prefabToUse = SelectPrefabBasedOnState();
        if (prefabToUse != null)
        {
            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToUse, transform);
            newObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            newObj.GetComponent<RectTransform>().localRotation = Quaternion.identity;

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

        GameObject prefabToUse = SelectPrefabBasedOnState();
        if (prefabToUse != null)
        {
            GameObject newObj = Instantiate(prefabToUse, transform);
            newObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            newObj.GetComponent<RectTransform>().localRotation = Quaternion.identity;

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
        if (tileImage != null)
        {
            tileImage.color = (isPlacable && !isOccupied) ? highlightColor : blockedColor;
        }
    }

    public void ResetHighlight()
    {
        if (tileImage != null)
        {
            tileImage.color = defaultColor;
        }
    }

    public bool CanPlaceCharacter()
    {
        if (isWalkable) return false;
        return isPlacable && !isOccupied;
    }
}
