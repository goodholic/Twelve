// Assets\Scripts\Network\FirebaseUserDataManager.cs

using System.Collections.Generic;
using UnityEngine;
using Firebase.Extensions;
using Firebase.Auth;

[System.Serializable]
public class UserProfileData
{
    public string userName;
    public int level;
    public int exp;
    public int gold;
    public List<string> ownedItemIds;
    public long lastUpdatedTimestamp;
}

public class FirebaseUserDataManager : MonoBehaviour
{
    public static FirebaseUserDataManager Instance { get; private set; }

    // 실제로는: private FirebaseFirestore firestore;
    private object firestore;

    private void Awake()
    {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 실제 Firestore 초기화
        // firestore = FirebaseFirestore.DefaultInstance;
        Debug.Log("[FirebaseUserDataManager] Firestore initialization skipped. Need to add Firebase Firestore SDK.");
    }

    // ----------------------------------------
    // (1) 유저 정보 로드
    // ----------------------------------------
    public void LoadUserData(string userId, System.Action<UserProfileData> onSuccess, System.Action<string> onFail)
    {
        if (string.IsNullOrEmpty(userId))
        {
            onFail?.Invoke("Invalid userId");
            return;
        }

        // Firestore SDK가 없으므로 임시 Mock 로직
        Debug.LogWarning("[FirebaseUserDataManager] Firestore SDK missing. Using mock data.");

        // 임시: 가짜 데이터
        UserProfileData mockData = new UserProfileData()
        {
            userName = $"User_{userId.Substring(0, 5)}",
            level = 1,
            exp = 0,
            gold = 100,
            ownedItemIds = new List<string>(),
            lastUpdatedTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        // 비동기 시뮬레이션
        System.Threading.Tasks.Task.Delay(100).ContinueWith(_ => {
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                onSuccess?.Invoke(mockData);
            });
        });
    }

    // ----------------------------------------
    // (2) 유저 정보 저장(업데이트)
    // ----------------------------------------
    public void SaveUserData(string userId, UserProfileData data, System.Action onSuccess, System.Action<string> onFail)
    {
        if (string.IsNullOrEmpty(userId))
        {
            onFail?.Invoke("Invalid userId");
            return;
        }

        Debug.LogWarning("[FirebaseUserDataManager] Firestore SDK missing. Data saving skipped.");

        // 비동기 성공 시뮬레이션
        System.Threading.Tasks.Task.Delay(100).ContinueWith(_ => {
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                Debug.Log($"[FirebaseUserDataManager] Simulated save for user: {userId}");
                onSuccess?.Invoke();
            });
        });
    }
}

// 메인 스레드에서 콜백을 실행하기 위한 Dispatcher (Firestore가 없으므로 임시 사용)
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly Queue<System.Action> _executionQueue = new Queue<System.Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        return _instance;
    }

    public void Enqueue(System.Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    private void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
}
