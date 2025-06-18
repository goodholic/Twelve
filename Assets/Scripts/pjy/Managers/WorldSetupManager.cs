using UnityEngine;

/// <summary>
/// 월드 좌표 기반 게임을 위한 초기 설정 관리자
/// 카메라, Sorting Layer, Physics 등을 설정합니다.
/// </summary>
public class WorldSetupManager : MonoBehaviour
{
    [Header("카메라 설정")]
    [Tooltip("메인 카메라")]
    public Camera mainCamera;
    
    [Tooltip("카메라 Orthographic 크기")]
    public float cameraOrthographicSize = 10f;
    
    [Tooltip("카메라 위치")]
    public Vector3 cameraPosition = new Vector3(0, 0, -10);
    
    [Header("Layer 설정")]
    [Tooltip("자동으로 Layer 설정")]
    public bool autoSetupLayers = true;
    
    [Header("Physics 2D 설정")]
    [Tooltip("중력 사용 여부")]
    public bool useGravity = false;
    
    [Tooltip("중력 값")]
    public Vector2 gravity = new Vector2(0, -9.81f);

    private void Awake()
    {
        SetupCamera();
        SetupPhysics2D();
        
        if (autoSetupLayers)
        {
            SetupLayers();
        }
        
        SetupSortingLayers();
    }

    /// <summary>
    /// 카메라 설정
    /// </summary>
    private void SetupCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (mainCamera == null)
        {
            GameObject cameraObj = new GameObject("Main Camera");
            mainCamera = cameraObj.AddComponent<Camera>();
            cameraObj.tag = "MainCamera";
        }
        
        // Orthographic 설정
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = cameraOrthographicSize;
        mainCamera.transform.position = cameraPosition;
        mainCamera.transform.rotation = Quaternion.identity;
        
        // 배경색 설정
        mainCamera.backgroundColor = new Color(0.2f, 0.2f, 0.3f);
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        
        // AudioListener 추가
        if (mainCamera.GetComponent<AudioListener>() == null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
        }
        
        Debug.Log($"[WorldSetupManager] 카메라 설정 완료 - Orthographic Size: {cameraOrthographicSize}");
    }

    /// <summary>
    /// Physics 2D 설정
    /// </summary>
    private void SetupPhysics2D()
    {
        if (useGravity)
        {
            Physics2D.gravity = gravity;
        }
        else
        {
            Physics2D.gravity = Vector2.zero;
        }
        
        // Collision Matrix 설정 (필요시)
        SetupCollisionMatrix();
        
        Debug.Log($"[WorldSetupManager] Physics2D 설정 완료 - 중력: {Physics2D.gravity}");
    }

    /// <summary>
    /// Layer 설정
    /// </summary>
    private void SetupLayers()
    {
        // Layer 이름 정의
        string[] layerNames = new string[]
        {
            "Default",      // 0
            "TransparentFX", // 1
            "Ignore Raycast", // 2
            "", // 3
            "Water", // 4
            "UI", // 5
            "", // 6
            "", // 7
            "Tile", // 8
            "Character", // 9
            "Monster", // 10
            "Bullet", // 11
            "Effect", // 12
        };
        
        // 주의: Unity Editor에서 직접 Layer를 추가해야 합니다.
        // 이 코드는 런타임에서 Layer 이름을 확인하는 용도입니다.
        
        Debug.Log("[WorldSetupManager] Layer 설정 확인:");
        for (int i = 8; i <= 12; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (string.IsNullOrEmpty(layerName))
            {
                Debug.LogWarning($"  - Layer {i}: 설정되지 않음 (Unity Editor에서 '{layerNames[i]}' 추가 필요)");
            }
            else
            {
                Debug.Log($"  - Layer {i}: {layerName}");
            }
        }
    }

    /// <summary>
    /// Sorting Layer 설정 확인
    /// </summary>
    private void SetupSortingLayers()
    {
        // Sorting Layer는 Unity Editor에서 설정해야 합니다.
        // Project Settings > Tags and Layers > Sorting Layers
        
        string[] expectedSortingLayers = new string[]
        {
            "Default",
            "Background",
            "Tiles",
            "Characters",
            "Bullets",
            "Effects",
            "UI"
        };
        
        Debug.Log("[WorldSetupManager] Sorting Layer 설정 필요:");
        Debug.Log("Unity Editor에서 Edit > Project Settings > Tags and Layers > Sorting Layers에 다음을 추가하세요:");
        foreach (var layer in expectedSortingLayers)
        {
            Debug.Log($"  - {layer}");
        }
    }

    /// <summary>
    /// Collision Matrix 설정
    /// </summary>
    private void SetupCollisionMatrix()
    {
        // Layer 번호
        int tileLayer = LayerMask.NameToLayer("Tile");
        int characterLayer = LayerMask.NameToLayer("Character");
        int monsterLayer = LayerMask.NameToLayer("Monster");
        int bulletLayer = LayerMask.NameToLayer("Bullet");
        int effectLayer = LayerMask.NameToLayer("Effect");
        
        // 충돌 설정
        // 타일끼리는 충돌 안함
        if (tileLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(tileLayer, tileLayer, true);
        }
        
        // 캐릭터끼리는 충돌 안함
        if (characterLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(characterLayer, characterLayer, true);
        }
        
        // 몬스터끼리는 충돌 안함
        if (monsterLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(monsterLayer, monsterLayer, true);
        }
        
        // 총알끼리는 충돌 안함
        if (bulletLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(bulletLayer, bulletLayer, true);
        }
        
        // 이펙트는 모든 것과 충돌 안함
        if (effectLayer >= 0)
        {
            for (int i = 0; i < 32; i++)
            {
                Physics2D.IgnoreLayerCollision(effectLayer, i, true);
            }
        }
        
        // 타일과 총알은 충돌 안함
        if (tileLayer >= 0 && bulletLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(tileLayer, bulletLayer, true);
        }
        
        Debug.Log("[WorldSetupManager] Collision Matrix 설정 완료");
    }

    /// <summary>
    /// 게임 오브젝트에 적절한 Layer 설정
    /// </summary>
    public static void SetLayerRecursively(GameObject obj, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
        {
            SetLayerRecursively(obj, layer);
        }
        else
        {
            Debug.LogWarning($"[WorldSetupManager] Layer '{layerName}'를 찾을 수 없습니다!");
        }
    }
    
    public static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// SpriteRenderer의 Sorting Layer 설정
    /// </summary>
    public static void SetSortingLayer(GameObject obj, string sortingLayerName, int sortingOrder = 0)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;
        }
        else
        {
            Debug.LogWarning($"[WorldSetupManager] {obj.name}에 SpriteRenderer가 없습니다!");
        }
    }

    /// <summary>
    /// 디버그: 현재 설정 출력
    /// </summary>
    [ContextMenu("Debug Current Setup")]
    public void DebugCurrentSetup()
    {
        Debug.Log("=== 현재 월드 설정 ===");
        
        if (mainCamera != null)
        {
            Debug.Log($"카메라: Orthographic={mainCamera.orthographic}, Size={mainCamera.orthographicSize}, Pos={mainCamera.transform.position}");
        }
        else
        {
            Debug.LogWarning("메인 카메라가 없습니다!");
        }
        
        Debug.Log($"Physics2D 중력: {Physics2D.gravity}");
        
        // Layer 확인
        Debug.Log("Layer 설정:");
        string[] layersToCheck = { "Tile", "Character", "Monster", "Bullet", "Effect" };
        foreach (var layerName in layersToCheck)
        {
            int layer = LayerMask.NameToLayer(layerName);
            Debug.Log($"  - {layerName}: {(layer >= 0 ? "설정됨" : "없음")}");
        }
    }
}