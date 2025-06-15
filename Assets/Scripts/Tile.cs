// ===== Tile.cs =====
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
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

    // ★★★ 수정: 타일 위의 캐릭터들 리스트로 관리 (최대 3개까지)
    private List<Character> occupyingCharacters = new List<Character>();
    private const int MAX_CHARACTERS_PER_TILE = 3;

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
            if (tileButton.targetGraphic == null)
            {
                tileButton.targetGraphic = tileImage;
            }
            tileButton.onClick.AddListener(OnClickPlacableTile);
        }
    }

    // ========================== Tile Type Checking ==========================
    public bool IsPlacable()
    {
        string n = gameObject.name.ToLower();
        bool inName = n.Contains("placable") && !n.Contains("placable2");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("placable") && !childName.Contains("placable2"))
            {
                return true;
            }
        }
        return inName;
    }

    public bool IsPlacable2()
    {
        string n = gameObject.name.ToLower();
        bool inName = n.Contains("placable2");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("placable2"))
            {
                return true;
            }
        }
        return inName;
    }

    public bool IsPlaceTile()
    {
        string n = gameObject.name.ToLower();
        bool inName = n.Contains("placetile");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("placetile"))
            {
                return true;
            }
        }
        return inName;
    }

    public bool IsPlaced2()
    {
        string n = gameObject.name.ToLower();
        bool inName = n.Contains("placed2");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("placed2"))
            {
                return true;
            }
        }
        return inName;
    }

    public bool IsWalkable()
    {
        string n = gameObject.name.ToLower();
        bool inName = n.Contains("walkable") && !n.Contains("walkable2") &&
                     !n.Contains("walkableleft") && !n.Contains("walkablecenter") && !n.Contains("walkableright");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("walkable") && !childName.Contains("walkable2") &&
                !childName.Contains("walkableleft") && !childName.Contains("walkablecenter") && !childName.Contains("walkableright"))
            {
                return true;
            }
        }
        return inName;
    }

    public bool IsWalkable2()
    {
        string n = gameObject.name.ToLower();
        bool inName = n.Contains("walkable2") && !n.Contains("walkable2left") && 
                     !n.Contains("walkable2center") && !n.Contains("walkable2right");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("walkable2") && !childName.Contains("walkable2left") &&
                !childName.Contains("walkable2center") && !childName.Contains("walkable2right"))
            {
                return true;
            }
        }
        return inName;
    }

    public bool IsWalkableLeft()
    {
        string n = gameObject.name.ToLower();
        bool inName = n.Contains("walkableleft");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("walkableleft"))
            {
                return true;
            }
        }
        return inName;
    }

    public bool IsWalkableCenter()
    {
        string n = gameObject.name.ToLower();
        bool inName = n.Contains("walkablecenter");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("walkablecenter"))
            {
                return true;
            }
        }
        return inName;
    }

    public bool IsWalkableRight()
    {
        string n = gameObject.name.ToLower();
        bool inName = n.Contains("walkableright");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("walkableright"))
            {
                return true;
            }
        }
        return inName;
    }

    public bool IsWalkable2Left()
    {
        string n = gameObject.name.ToLower();
        bool inName = n.Contains("walkable2left");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("walkable2left"))
            {
                return true;
            }
        }
        return inName;
    }

    public bool IsWalkable2Center()
    {
        string n = gameObject.name.ToLower();
        bool inName = n.Contains("walkable2center");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("walkable2center"))
            {
                return true;
            }
        }
        return inName;
    }

    public bool IsWalkable2Right()
    {
        string n = gameObject.name.ToLower();
        bool inName = n.Contains("walkable2right");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName.Contains("walkable2right"))
            {
                return true;
            }
        }
        return inName;
    }

    /// <summary>
    /// ★★★ 수정: 이 타일에 캐릭터를 배치할 수 있는지 확인
    /// </summary>
    public bool CanPlaceCharacter(Character characterToPlace = null)
    {
        // Placable 타일이거나 빈 PlaceTile만 배치 가능
        bool isPlacableType = IsPlacable() || IsPlacable2();
        bool isEmptyPlacedType = (IsPlaceTile() || IsPlaced2());

        if (!isPlacableType && !isEmptyPlacedType) return false;

        // 빈 타일이면 배치 가능
        if (occupyingCharacters.Count == 0) return true;

        // 캐릭터가 있는 경우
        if (occupyingCharacters.Count >= MAX_CHARACTERS_PER_TILE) return false;

        // 배치하려는 캐릭터가 있고, 이미 있는 캐릭터와 같은 종류인지 확인
        if (characterToPlace != null && occupyingCharacters.Count > 0)
        {
            Character firstChar = occupyingCharacters[0];
            // 같은 이름과 같은 별 등급인지 확인
            return firstChar.characterName == characterToPlace.characterName && 
                   firstChar.star == characterToPlace.star;
        }

        return false;
    }

    /// <summary>
    /// 타일 클릭 시 로직 (게임 기획서: 원 버튼 소환)
    /// </summary>
    public void OnClickPlacableTile()
    {
        Debug.Log($"[Tile] 클릭됨: {name} (Index={tileIndex}, row={row}, col={column}, Route={belongingRoute})");
        
        // 타일 상태 디버그 정보
        Debug.Log($"[Tile] 타일 상태 - CanPlace:{CanPlaceCharacter()}, CharacterCount:{occupyingCharacters.Count}");

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
            Debug.Log($"[Tile] 배치 불가 상태. (이미 캐릭터가 {occupyingCharacters.Count}개 있거나 소환 불가능한 타일)");
        }
    }

    /// <summary>
    /// ★★★ 수정: 타일에 캐릭터 추가
    /// </summary>
    public bool AddOccupyingCharacter(Character character)
    {
        if (character == null) return false;

        // 최대 개수 체크
        if (occupyingCharacters.Count >= MAX_CHARACTERS_PER_TILE) return false;

        // 같은 캐릭터 체크
        if (occupyingCharacters.Count > 0)
        {
            Character firstChar = occupyingCharacters[0];
            if (firstChar.characterName != character.characterName || firstChar.star != character.star)
            {
                Debug.Log($"[Tile] 다른 종류의 캐릭터는 같은 타일에 배치할 수 없습니다.");
                return false;
            }
        }

        occupyingCharacters.Add(character);
        UpdateCharacterSizes();
        UpdateTileColor();
        return true;
    }

    /// <summary>
    /// ★★★ 수정: 타일에서 특정 캐릭터 제거
    /// </summary>
    public void RemoveOccupyingCharacter(Character character)
    {
        if (character != null && occupyingCharacters.Contains(character))
        {
            occupyingCharacters.Remove(character);
            UpdateCharacterSizes();
            UpdateTileColor();
        }
    }

    /// <summary>
    /// ★★★ 추가: 타일의 모든 캐릭터 제거
    /// </summary>
    public void RemoveAllOccupyingCharacters()
    {
        occupyingCharacters.Clear();
        UpdateTileColor();
    }

    /// <summary>
    /// ★★★ 수정: 타일에 있는 캐릭터들 반환
    /// </summary>
    public List<Character> GetOccupyingCharacters()
    {
        return new List<Character>(occupyingCharacters);
    }

    /// <summary>
    /// ★★★ 추가: 타일에 있는 첫 번째 캐릭터 반환 (기존 코드 호환성)
    /// </summary>
    public Character GetOccupyingCharacter()
    {
        return occupyingCharacters.Count > 0 ? occupyingCharacters[0] : null;
    }

    /// <summary>
    /// ★★★ 추가: 캐릭터 크기 업데이트 (3명이 들어간 것처럼 보이도록)
    /// </summary>
    private void UpdateCharacterSizes()
    {
        if (occupyingCharacters.Count == 0) return;

        float scaleFactor = 1.0f;
        Vector2[] positions = null;

        switch (occupyingCharacters.Count)
        {
            case 1:
                scaleFactor = 1.0f;
                positions = new Vector2[] { Vector2.zero };
                break;
            case 2:
                scaleFactor = 0.8f;
                positions = new Vector2[] { 
                    new Vector2(-20f, 0), 
                    new Vector2(20f, 0) 
                };
                break;
            case 3:
                scaleFactor = 0.65f;
                positions = new Vector2[] { 
                    new Vector2(-25f, 10f), 
                    new Vector2(25f, 10f), 
                    new Vector2(0, -20f) 
                };
                break;
        }

        // 각 캐릭터의 크기와 위치 조정
        for (int i = 0; i < occupyingCharacters.Count; i++)
        {
            if (occupyingCharacters[i] != null)
            {
                RectTransform charRect = occupyingCharacters[i].GetComponent<RectTransform>();
                if (charRect != null)
                {
                    // 크기 조정
                    charRect.localScale = Vector3.one * scaleFactor;
                    
                    // 위치 조정 (타일 내에서 분산 배치)
                    if (positions != null && i < positions.Length)
                    {
                        // 타일의 RectTransform 가져오기
                        RectTransform tileRect = GetComponent<RectTransform>();
                        if (tileRect != null)
                        {
                            // 타일의 월드 좌표를 캐릭터의 부모 좌표계로 변환
                            RectTransform charParent = charRect.parent as RectTransform;
                            if (charParent != null)
                            {
                                Vector2 localPos = charParent.InverseTransformPoint(tileRect.transform.position);
                                charRect.anchoredPosition = localPos + positions[i];
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 타일 색상 업데이트
    /// </summary>
    private void UpdateTileColor()
    {
        if (tileImage == null) return;

        if (occupyingCharacters.Count > 0)
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

    // ========================== State Changes ==========================
    public void SetPlacable()
    {
        string n = gameObject.name.ToLower();
        
        if (n.Contains("walkable2left") || n.Contains("walkable2center") || n.Contains("walkable2right") || n.Contains("walkable2"))
        {
            Debug.Log($"[Tile] {name}은 walkable2 타일이므로 placable로 변경 불가");
            return;
        }
        
        // 자식 오브젝트 정리
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (!IsAssetObject(child))
            {
                DestroyImmediate(child, false);
            }
        }
        
        // placable 프리팹 생성
        if (placablePrefab != null)
        {
            currentVisual = (GameObject)PrefabUtility.InstantiatePrefab(placablePrefab, transform);
            SetupVisualObject(currentVisual);
        }
        
        UpdateTileColor();
    }

    public void SetPlacable2()
    {
        // 자식 오브젝트 정리
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (!IsAssetObject(child))
            {
                DestroyImmediate(child, false);
            }
        }
        
        // placable2 프리팹 생성
        if (placable2Prefab != null)
        {
            currentVisual = (GameObject)PrefabUtility.InstantiatePrefab(placable2Prefab, transform);
            SetupVisualObject(currentVisual);
        }
        
        UpdateTileColor();
    }

    // ========================== Visual Management ==========================
    private void SetupVisualObject(GameObject obj)
    {
        if (obj == null) return;
        
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }

    private bool IsAssetObject(GameObject obj)
    {
        if (obj == null) return false;
        
#if UNITY_EDITOR
        return EditorUtility.IsPersistent(obj) || 
               PrefabUtility.IsPartOfPrefabAsset(obj) || 
               PrefabUtility.IsPartOfPrefabInstance(obj);
#else
        return false;
#endif
    }

    private void UpdateTileVisual_Runtime()
    {
        if (currentVisual != null)
        {
            Destroy(currentVisual);
            currentVisual = null;
        }

        GameObject prefabToUse = SelectVisualPrefab();
        if (prefabToUse != null)
        {
            currentVisual = Instantiate(prefabToUse, transform);
            SetupVisualObject(currentVisual);
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

    public void UnhighlightTile()
    {
        UpdateTileColor();
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
                rt.sizeDelta = Vector2.zero;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.localScale = Vector3.one;
            }
            currentVisual = newObj;
        }
    }
#endif
}