using UnityEngine;

public class CoreDataManager : MonoBehaviour
{
    private static CoreDataManager instance;
    public static CoreDataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CoreDataManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CoreDataManager");
                    instance = go.AddComponent<CoreDataManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }
    
    [Header("캐릭터 데이터베이스")]
    public CharacterDatabase characterDatabase;
    
    [Header("미네랄 바 참조")]
    public MineralBar region1MineralBar;
    public MineralBar region2MineralBar;
    
    [Header("게임 상태")]
    public bool isGamePaused = false;
    public float gameSpeed = 1f;
    public bool isHost = true; // 호스트 여부
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 캐릭터 데이터베이스 초기화
        if (characterDatabase == null)
        {
            characterDatabase = ScriptableObject.CreateInstance<CharacterDatabase>();
        }
    }
} 