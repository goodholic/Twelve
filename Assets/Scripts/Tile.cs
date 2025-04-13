using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;            // PrefabUtility, EditorApplication 사용
using UnityEditor.SceneManagement;
#endif

public class Tile : MonoBehaviour
{
    [Header("Tile Settings")]
    [Tooltip("몬스터가 지나갈 수 있는 타일이면 true, 아니면 false")]
    public bool isWalkable = false;

    [Tooltip("플레이어가 캐릭터를 배치할 수 있는 타일이면 true, 아니면 false")]
    public bool isPlacable = true;

    [Tooltip("현재 타일에 캐릭터가 배치되어 있는지 여부 (전투 중 배치/점유 상태)")]
    public bool isOccupied = false;

    [Header("Visual References (기존 색상 설정)")]
    [SerializeField] private Renderer tileRenderer;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color blockedColor = Color.red;

    [Header("Prefab Switching (Walkable / Placable / Occupied)")]
    [Tooltip("Walkable 상태일 때 사용할 타일 프리팹")]
    public GameObject walkablePrefab;

    [Tooltip("Placable 상태일 때 사용할 타일 프리팹")]
    public GameObject placablePrefab;

    [Tooltip("Occupied 상태일 때 사용할 타일 프리팹")]
    public GameObject occupiedPrefab;

    [Tooltip("현재 표시 중인 타일 비주얼(동적으로 생성)")]
    [SerializeField] private GameObject currentVisual;

    private void Start()
    {
        // 초기 컬러 세팅
        if (tileRenderer == null)
        {
            tileRenderer = GetComponent<Renderer>();
        }
        if (tileRenderer != null)
        {
            tileRenderer.material.color = defaultColor;
        }

        // 플레이 모드 시작 시점에 프리팹 갱신
        SafeUpdateTileVisual();
    }

    // --------------------------------------------------
    // OnValidate: 에디터에서 Inspector 값이 바뀔 때마다 자동 호출
    // --------------------------------------------------
    private void OnValidate()
    {
        // 에디터에서만 동작 (플레이 중이 아니라면)
        if (!Application.isPlaying)
        {
            SafeUpdateTileVisual();
        }
    }

    /// <summary>
    /// 에디터(미플레이) 또는 런타임(플레이 중)에서 안전하게 비주얼을 갱신
    /// </summary>
    private void SafeUpdateTileVisual()
    {
#if UNITY_EDITOR
        // 1) 만약 이 Tile이 'Project 창 프리팹 에셋'이면 교체 작업을 스킵
        if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
        {
            return;
        }

        // 2) 에디터 모드에서 DestroyImmediate / InstantiatePrefab 수행
        //    단, 즉시 호출이 아니라 delayCall로 다음 에디터 사이클에 실행
        EditorApplication.delayCall += () =>
        {
            // 혹시 이 스크립트나 오브젝트가 이미 제거된 경우 중단
            if (this == null || gameObject == null) return;

            // 다시 확인: Prefab 에셋이면 스킵
            if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                return;
            }

            UpdateTileVisual_Editor();
        };
#else
        // 런타임(플레이 모드)일 때는 즉시 UpdateTileVisual_Runtime
        UpdateTileVisual_Runtime();
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 모드에서 DestroyImmediate + PrefabUtility.InstantiatePrefab
    /// 그리고, 만약 새로 생성된 오브젝트에 Tile.cs가 있다면 제거.
    /// </summary>
    private void UpdateTileVisual_Editor()
    {
        if (currentVisual != null)
        {
            // 만약 currentVisual이 프로젝트 에셋이면(AssetDatabase에 존재) 제거하지 않음
            if (!IsAssetObject(currentVisual))
            {
                DestroyImmediate(currentVisual, false);
            }
        }

        // 새 프리팹 결정
        GameObject prefabToUse = SelectPrefabBasedOnState();
        if (prefabToUse != null)
        {
            // PrefabUtility.InstantiatePrefab을 써서 부모 관계 유지
            // parent는 씬 객체여야 함 (지금은 PrefabAsset이 아님이 보장됨)
            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToUse, transform);
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;

            // 만약 이 프리팹에 Tile.cs가 들어있다면 제거
            RemoveTileScriptsInChildren(newObj, true);

            currentVisual = newObj;
        }
        else
        {
            currentVisual = null;
        }

        // 변경사항이 씬에 반영되도록 표시
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    /// <summary>
    /// 특정 오브젝트가 Project 에셋인지(씬 객체가 아닌지) 확인
    /// </summary>
    private bool IsAssetObject(UnityEngine.Object obj)
    {
        return PrefabUtility.IsPartOfPrefabAsset(obj) || AssetDatabase.Contains(obj);
    }

    /// <summary>
    /// 에디터 모드에서, 생성된 프리팹 안의 Tile.cs를 모두 제거한다.
    /// </summary>
    private void RemoveTileScriptsInChildren(GameObject root, bool useDestroyImmediate)
    {
        // 하위 모든 Tile 컴포넌트를 찾음
        Tile[] tiles = root.GetComponentsInChildren<Tile>(true);
        foreach (Tile tileScript in tiles)
        {
            // 자신이면 제거 X. 여기서는 root Tile이 아니라 "자식에 들어간 Tile.cs"를 제거
            // 혹은 "프리팹이 본래 Tile.cs를 갖고 있으면" 제거
            if (tileScript != this)
            {
                if (useDestroyImmediate)
                {
                    DestroyImmediate(tileScript, false);
                }
                else
                {
                    Destroy(tileScript);
                }
            }
        }
    }
#endif

    /// <summary>
    /// 런타임(플레이 중)에 Destroy() + Instantiate()로 타일 비주얼 교체
    /// 그리고 생성된 오브젝트 내 Tile.cs 제거
    /// </summary>
    private void UpdateTileVisual_Runtime()
    {
        if (currentVisual != null)
        {
            Destroy(currentVisual);
        }

        GameObject prefabToUse = SelectPrefabBasedOnState();
        if (prefabToUse != null)
        {
            GameObject newObj = Instantiate(prefabToUse, transform);
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;

            // 런타임에서도 혹시 모를 Tile.cs 제거
            RemoveTileScriptsInChildrenRuntime(newObj);

            currentVisual = newObj;
        }
        else
        {
            currentVisual = null;
        }
    }

    private void RemoveTileScriptsInChildrenRuntime(GameObject root)
    {
        // 하위 모든 Tile 컴포넌트 제거
        Tile[] tiles = root.GetComponentsInChildren<Tile>(true);
        foreach (Tile tileScript in tiles)
        {
            if (tileScript != this)
            {
                Destroy(tileScript);
            }
        }
    }

    /// <summary>
    /// 현재 isWalkable/isPlacable/isOccupied 값에 따라 사용할 프리팹 선택
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
        return null; // 어떤 상태에도 해당하지 않으면 null
    }

    /// <summary>
    /// 외부(다른 코드)에서 타일 상태를 바꾼 후, 안전하게 비주얼 갱신
    /// </summary>
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

    // ----------------- 기존 Tile 기능 -----------------
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
        // 몬스터 경로이면 배치 불가
        if (isWalkable) return false;
        // Placable & !Occupied 상태여야 가능
        return isPlacable && !isOccupied;
    }
}
