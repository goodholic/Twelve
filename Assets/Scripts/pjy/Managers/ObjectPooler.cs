using System.Collections.Generic;
using UnityEngine;

// 풀링할 오브젝트 정보를 담는 클래스
[System.Serializable]
public class ObjectPoolItem
{
    public string tag;          // 오브젝트 식별 태그 (예: "Bullet", "ImpactVFX", "AllyMonster")
    public GameObject prefab;   // 풀링할 프리팹
    public int size;            // 초기 풀 크기
    public Transform parent;    // 생성될 때 부모 Transform (옵션)
}

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance; // 싱글톤 인스턴스

    public List<ObjectPoolItem> itemsToPool; // 인스펙터에서 설정할 풀링 아이템 리스트
    private Dictionary<string, Queue<GameObject>> poolDictionary; // 태그별 오브젝트 큐
    private Dictionary<string, int> poolSizeTracker; // 각 풀의 현재 크기 추적
    private Dictionary<string, int> poolMaxSize; // 각 풀의 최대 크기 제한

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        // DontDestroyOnLoad(gameObject); // 필요 시 씬 전환 시 유지
    }

    void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolSizeTracker = new Dictionary<string, int>();
        poolMaxSize = new Dictionary<string, int>();

        foreach (ObjectPoolItem item in itemsToPool)
        {
            if (item == null || item.prefab == null || string.IsNullOrEmpty(item.tag))
            {
                Debug.LogError("[ObjectPooler] Invalid pool item detected!");
                continue;
            }
            
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < item.size; i++)
            {
                GameObject obj = CreatePooledObject(item);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(item.tag, objectPool);
            poolSizeTracker.Add(item.tag, item.size);
            poolMaxSize.Add(item.tag, item.size * 3); // 최대 크기는 초기 크기의 3배로 제한
            
            Debug.Log($"[ObjectPooler] Pool created: {item.tag} (Size: {item.size})");
        }
    }
    
    /// <summary>
    /// 풀링된 오브젝트 생성 헬퍼 메서드
    /// </summary>
    private GameObject CreatePooledObject(ObjectPoolItem item)
    {
        try
        {
            GameObject obj = Instantiate(item.prefab);
            if (item.parent != null)
            {
                obj.transform.SetParent(item.parent, false);
            }
            obj.SetActive(false);
            obj.name = $"{item.tag}_Pooled";
            return obj;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ObjectPooler] Failed to create pooled object: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 풀에서 오브젝트를 가져옵니다. 없으면 새로 생성합니다.
    /// </summary>
    /// <param name="tag">가져올 오브젝트의 태그</param>
    /// <param name="position">스폰될 위치</param>
    /// <param name="rotation">스폰될 회전값</param>
    /// <param name="parent">스폰될 때 설정할 부모 (옵션, null이면 풀 기본값 또는 월드)</param>
    /// <returns>활성화된 게임 오브젝트</returns>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"[ObjectPooler] Pool with tag '{tag}' doesn't exist.");
            return null;
        }

        GameObject objectToSpawn = null;
        Queue<GameObject> poolQueue = poolDictionary[tag];

        // 풀에 사용 가능한 오브젝트가 있는지 확인
        while (poolQueue.Count > 0)
        {
            objectToSpawn = poolQueue.Dequeue();
            if (objectToSpawn != null) // null 체크 추가
            {
                break;
            }
        }
        
        // 사용 가능한 오브젝트가 없으면 새로 생성 (풀 확장)
        if (objectToSpawn == null)
        {
            // 최대 크기 체크
            if (poolSizeTracker[tag] >= poolMaxSize[tag])
            {
                Debug.LogWarning($"[ObjectPooler] Pool '{tag}' has reached maximum size ({poolMaxSize[tag]}). Cannot expand.");
                return null;
            }
            
            ObjectPoolItem item = itemsToPool.Find(i => i.tag == tag);
            if (item != null)
            {
                objectToSpawn = CreatePooledObject(item);
                if (objectToSpawn != null)
                {
                    poolSizeTracker[tag]++;
                    Debug.LogWarning($"[ObjectPooler] Pool '{tag}' expanded. New size: {poolSizeTracker[tag]}");
                }
                else
                {
                    return null;
                }
            }
            else
            {
                Debug.LogError($"[ObjectPooler] Cannot find prefab for tag '{tag}' to expand pool.");
                return null;
            }
        }
        
        // 부모 설정 (요청된 parent가 있으면 우선 적용, 없으면 풀 기본값 적용)
        Transform targetParent = parent ?? itemsToPool.Find(i => i.tag == tag)?.parent;
        if (targetParent != null)
        {
            objectToSpawn.transform.SetParent(targetParent, false); // 월드 좌표 유지 false 중요
             objectToSpawn.transform.position = position; // 부모 설정 후 위치/회전 다시 설정
             objectToSpawn.transform.rotation = rotation;
        }
        else
        {
             objectToSpawn.transform.position = position;
             objectToSpawn.transform.rotation = rotation;
             objectToSpawn.transform.SetParent(null); // 부모 해제 (월드)
        }


        objectToSpawn.SetActive(true); // 오브젝트 활성화

        // IPooledObject 인터페이스가 있다면 OnObjectSpawn() 호출 (선택적 확장)
        IPooledObject pooledObj = objectToSpawn.GetComponent<IPooledObject>();
        pooledObj?.OnObjectSpawn();

        return objectToSpawn;
    }

    /// <summary>
    /// 사용한 오브젝트를 풀에 반환합니다.
    /// </summary>
    /// <param name="tag">반환할 오브젝트의 태그</param>
    /// <param name="objectToReturn">반환할 게임 오브젝트</param>
    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"[ObjectPooler] Pool with tag '{tag}' doesn't exist. Destroying object.");
            Destroy(objectToReturn);
            return;
        }

        objectToReturn.SetActive(false); // 비활성화
        
        // 부모를 풀 기본값으로 되돌릴 필요가 있다면 여기서 설정
        // ObjectPoolItem item = itemsToPool.Find(i => i.tag == tag);
        // if (item?.parent != null) objectToReturn.transform.SetParent(item.parent);

        poolDictionary[tag].Enqueue(objectToReturn); // 큐에 다시 추가
    }
    
    /// <summary>
    /// 모든 풀을 정리하고 메모리를 해제합니다.
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var kvp in poolDictionary)
        {
            ClearPool(kvp.Key);
        }
    }
    
    /// <summary>
    /// 특정 태그의 풀을 정리합니다.
    /// </summary>
    public void ClearPool(string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"[ObjectPooler] Pool with tag '{tag}' doesn't exist.");
            return;
        }
        
        Queue<GameObject> pool = poolDictionary[tag];
        while (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        
        poolSizeTracker[tag] = 0;
        Debug.Log($"[ObjectPooler] Pool '{tag}' cleared.");
    }
    
    /// <summary>
    /// 풀의 현재 상태를 출력합니다.
    /// </summary>
    public void PrintPoolStatus()
    {
        Debug.Log("=== Object Pool Status ===");
        foreach (var kvp in poolDictionary)
        {
            Debug.Log($"Pool '{kvp.Key}': Available: {kvp.Value.Count}, Total Size: {poolSizeTracker[kvp.Key]}, Max: {poolMaxSize[kvp.Key]}");
        }
    }
    
    private void OnDestroy()
    {
        // 메모리 누수 방지를 위해 모든 풀 정리
        ClearAllPools();
    }
}

// 오브젝트 풀링을 사용하는 스크립트가 구현할 인터페이스 (선택 사항)
public interface IPooledObject
{
    void OnObjectSpawn(); // 풀에서 나올 때 호출될 메서드
} 