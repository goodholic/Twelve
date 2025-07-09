using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuildMaster.Core
{
    /// <summary>
    /// 제네릭 오브젝트 풀 시스템
    /// 메모리 할당/해제를 줄여 성능을 최적화합니다
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T> pool = new Stack<T>();
        private readonly Func<T> createFunc;
        private readonly Action<T> resetAction;
        private readonly int maxSize;
        
        public int CountActive { get; private set; }
        public int CountInactive => pool.Count;
        public int CountAll => CountActive + CountInactive;
        
        public ObjectPool(Func<T> createFunc = null, Action<T> resetAction = null, int maxSize = 100)
        {
            this.createFunc = createFunc;
            this.resetAction = resetAction;
            this.maxSize = maxSize;
        }
        
        public T Get()
        {
            T item;
            if (pool.Count > 0)
            {
                item = pool.Pop();
            }
            else
            {
                item = createFunc?.Invoke() ?? Activator.CreateInstance<T>();
            }
            
            CountActive++;
            return item;
        }
        
        public void Release(T item)
        {
            if (item == null) return;
            
            if (CountInactive < maxSize)
            {
                resetAction?.Invoke(item);
                pool.Push(item);
                CountActive--;
            }
        }
        
        public void Clear()
        {
            pool.Clear();
            CountActive = 0;
        }
        
        public void Prewarm(int count)
        {
            for (int i = 0; i < count && pool.Count < maxSize; i++)
            {
                var item = createFunc?.Invoke() ?? Activator.CreateInstance<T>();
                pool.Push(item);
            }
        }
    }
    
    /// <summary>
    /// GameObject 전용 오브젝트 풀
    /// </summary>
    public class GameObjectPool
    {
        private readonly Stack<GameObject> pool = new Stack<GameObject>();
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly int maxSize;
        
        public int CountActive { get; private set; }
        public int CountInactive => pool.Count;
        public int CountAll => CountActive + CountInactive;
        
        public GameObjectPool(GameObject prefab, Transform parent = null, int maxSize = 100)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.maxSize = maxSize;
        }
        
        public GameObject Get()
        {
            GameObject obj;
            
            if (pool.Count > 0)
            {
                obj = pool.Pop();
                obj.SetActive(true);
            }
            else
            {
                obj = GameObject.Instantiate(prefab, parent);
            }
            
            CountActive++;
            return obj;
        }
        
        public T Get<T>() where T : Component
        {
            return Get().GetComponent<T>();
        }
        
        public void Release(GameObject obj)
        {
            if (obj == null) return;
            
            obj.SetActive(false);
            
            if (pool.Count < maxSize)
            {
                pool.Push(obj);
            }
            else
            {
                GameObject.Destroy(obj);
            }
            
            CountActive--;
        }
        
        public void Release(Component component)
        {
            if (component != null)
                Release(component.gameObject);
        }
        
        public void Clear()
        {
            while (pool.Count > 0)
            {
                var obj = pool.Pop();
                if (obj != null)
                    GameObject.Destroy(obj);
            }
            CountActive = 0;
        }
        
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = GameObject.Instantiate(prefab, parent);
                obj.SetActive(false);
                pool.Push(obj);
            }
        }
    }
    
    /// <summary>
    /// 전역 오브젝트 풀 매니저
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        private static PoolManager _instance;
        public static PoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("PoolManager");
                    _instance = go.AddComponent<PoolManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private Dictionary<string, GameObjectPool> gameObjectPools = new Dictionary<string, GameObjectPool>();
        private Dictionary<Type, object> typedPools = new Dictionary<Type, object>();
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        /// <summary>
        /// GameObject 풀 등록
        /// </summary>
        public void RegisterPool(string poolName, GameObject prefab, int prewarmCount = 0, int maxSize = 100)
        {
            if (!gameObjectPools.ContainsKey(poolName))
            {
                var pool = new GameObjectPool(prefab, transform, maxSize);
                if (prewarmCount > 0)
                    pool.Prewarm(prewarmCount);
                    
                gameObjectPools[poolName] = pool;
            }
        }
        
        /// <summary>
        /// 타입별 풀 등록
        /// </summary>
        public void RegisterPool<T>(Func<T> createFunc = null, Action<T> resetAction = null, int prewarmCount = 0, int maxSize = 100) where T : class
        {
            var type = typeof(T);
            if (!typedPools.ContainsKey(type))
            {
                var pool = new ObjectPool<T>(createFunc, resetAction, maxSize);
                if (prewarmCount > 0)
                    pool.Prewarm(prewarmCount);
                    
                typedPools[type] = pool;
            }
        }
        
        /// <summary>
        /// GameObject 가져오기
        /// </summary>
        public GameObject Get(string poolName)
        {
            if (gameObjectPools.TryGetValue(poolName, out var pool))
            {
                return pool.Get();
            }
            
            Debug.LogWarning($"Pool '{poolName}' not found!");
            return null;
        }
        
        /// <summary>
        /// 컴포넌트와 함께 GameObject 가져오기
        /// </summary>
        public T Get<T>(string poolName) where T : Component
        {
            var obj = Get(poolName);
            return obj?.GetComponent<T>();
        }
        
        /// <summary>
        /// 타입별 오브젝트 가져오기
        /// </summary>
        public T Get<T>() where T : class
        {
            var type = typeof(T);
            if (typedPools.TryGetValue(type, out var poolObj) && poolObj is ObjectPool<T> pool)
            {
                return pool.Get();
            }
            
            Debug.LogWarning($"Pool for type '{type.Name}' not found!");
            return Activator.CreateInstance<T>();
        }
        
        /// <summary>
        /// GameObject 반환
        /// </summary>
        public void Release(string poolName, GameObject obj)
        {
            if (gameObjectPools.TryGetValue(poolName, out var pool))
            {
                pool.Release(obj);
            }
            else
            {
                Destroy(obj);
            }
        }
        
        /// <summary>
        /// 컴포넌트 반환
        /// </summary>
        public void Release<T>(string poolName, T component) where T : Component
        {
            if (component != null)
                Release(poolName, component.gameObject);
        }
        
        /// <summary>
        /// 타입별 오브젝트 반환
        /// </summary>
        public void Release<T>(T item) where T : class
        {
            var type = typeof(T);
            if (typedPools.TryGetValue(type, out var poolObj) && poolObj is ObjectPool<T> pool)
            {
                pool.Release(item);
            }
        }
        
        /// <summary>
        /// 모든 풀 정리
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in gameObjectPools.Values)
            {
                pool.Clear();
            }
            
            foreach (var poolObj in typedPools.Values)
            {
                if (poolObj is ObjectPool<object> pool)
                    pool.Clear();
            }
        }
        
        /// <summary>
        /// 풀 상태 정보 가져오기
        /// </summary>
        public string GetPoolInfo()
        {
            var info = "=== Pool Manager Info ===\n";
            
            info += "GameObject Pools:\n";
            foreach (var kvp in gameObjectPools)
            {
                var pool = kvp.Value;
                info += $"  {kvp.Key}: Active={pool.CountActive}, Inactive={pool.CountInactive}\n";
            }
            
            info += "\nTyped Pools:\n";
            foreach (var kvp in typedPools)
            {
                info += $"  {kvp.Key.Name}: Pool registered\n";
            }
            
            return info;
        }
        
        void OnDestroy()
        {
            ClearAllPools();
        }
    }
} 