using UnityEngine;
using UnityEngine.UI;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 2D 타일(칸).
/// 자식 오브젝트("Placable", "Walkable", "Occupied")의 존재 여부로 상태를 판단.
/// Button을 통해 OnClickPlacableTile() 호출 -> 캐릭터 배치 로직.
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

    // [추가] 타일이 몇 번째 인덱스(또는 행/열)에 위치하는지 저장
    [HideInInspector] public int row;
    [HideInInspector] public int column;
    [HideInInspector] public int tileIndex;

    /// <summary>
    /// 자식 중 이름이 "Placable"인 오브젝트가 있으면 true
    /// </summary>
    public bool IsPlacable()
    {
        return (transform.Find("Placable") != null);
    }

    /// <summary>
    /// 자식 중 이름이 "Occupied"인 오브젝트가 있으면 true
    /// </summary>
    public bool IsOccupied()
    {
        return (transform.Find("Occupied") != null);
    }

    /// <summary>
    /// 자식 중 이름이 "Walkable"인 오브젝트가 있으면 true
    /// </summary>
    public bool IsWalkable()
    {
        return (transform.Find("Walkable") != null);
    }

    /// <summary>
    /// 배치 가능 조건: Walkable=false && Placable=true && Occupied=false
    /// </summary>
    public bool CanPlaceCharacter()
    {
        return (!IsWalkable() && IsPlacable() && !IsOccupied());
    }

    private void Start()
    {
        // 1) Tile Image 설정
        if (tileImage == null)
        {
            tileImage = GetComponent<Image>();
        }
        if (tileImage != null)
        {
            tileImage.color = defaultColor;
            tileImage.raycastTarget = true; // UI 클릭 이벤트 인식
        }

        // 2) Button 설정
        if (tileButton != null)
        {
            if (tileButton.targetGraphic == null && tileImage != null)
            {
                tileButton.targetGraphic = tileImage;
            }
            tileButton.onClick.RemoveAllListeners();
            tileButton.onClick.AddListener(OnClickPlacableTile);
        }

        // 3) 런타임 시 비주얼 갱신
        if (Application.isPlaying)
        {
            UpdateTileVisual_Runtime();
        }
    }

    /// <summary>
    /// Tile에 붙은 Button 클릭 시 호출되는 메서드
    /// </summary>
    public void OnClickPlacableTile()
    {
        // [추가] 몇 번째 타일(또는 row/column)인지 로그로 확인
        Debug.Log($"Tile 클릭됨: {name} (Index: {tileIndex}, Row={row}, Col={column})");

        if (CanPlaceCharacter())
        {
            var mgr = PlacementManager.Instance;
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
            // 에디터에서 맵 상태("Placable", "Walkable", "Occupied") 확인용 디버그
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
    /// 자식("Placable"/"Walkable"/"Occupied") 상태에 따라 어떤 비주얼 프리팹을 붙일지 결정
    /// </summary>
    private GameObject SelectVisualPrefab()
    {
        bool p = IsPlacable();
        bool w = IsWalkable();
        bool o = IsOccupied();

        // Walkable 전용
        if (w && !p && !o) return walkablePrefab;

        // Placable 전용
        if (!w && p && !o) return placablePrefab;

        // Occupied 전용
        if (!w && !p && o) return occupiedPrefab;

        // 그 외 (아무 자식도 없는 경우나, 여러개가 동시에 있는 경우)
        return null;
    }

    public void RefreshTileVisual()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) UpdateTileVisual_Runtime();
        else UpdateTileVisual_Editor();
#else
        UpdateTileVisual_Runtime();
#endif
    }

    public void HighlightTile()
    {
        if (tileImage != null)
        {
            tileImage.color = (CanPlaceCharacter()) ? highlightColor : blockedColor;
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

#if UNITY_EDITOR
/// <summary>
/// (추가) Tile용 CustomEditor
/// 에디터에서 "Placable / Walkable / Occupied" 상태를 Inspector에서 간단히 확인
/// </summary>
[CustomEditor(typeof(Tile))]
public class TileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 Inspector
        base.OnInspectorGUI();

        // 상태 확인
        Tile tile = (Tile)target;
        bool p = tile.IsPlacable();
        bool w = tile.IsWalkable();
        bool o = tile.IsOccupied();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("=== Tile 상태 (자식 오브젝트) ===", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Placable : {p}");
        EditorGUILayout.LabelField($"Walkable : {w}");
        EditorGUILayout.LabelField($"Occupied : {o}");

        // 필요하면 실시간 갱신 버튼 추가 가능
        // if (GUILayout.Button("Refresh Visual")) { tile.RefreshInEditor(); }
    }
}
#endif
