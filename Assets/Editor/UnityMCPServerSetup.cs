#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Unity MCP 서버를 자동으로 설정하고 관리하는 에디터 스크립트
/// </summary>
[InitializeOnLoad]
public class UnityMCPServerSetup
{
    private const string MCP_SERVER_OBJECT_NAME = "_UnityMCPServer";
    
    static UnityMCPServerSetup()
    {
        // 에디터가 시작될 때와 플레이 모드 변경 시 MCP 서버 확인
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        // 씬 열기 이벤트는 제거 (에디터 모드에서 문제 발생)
        // EditorSceneManager.sceneOpened += OnSceneOpened;
        
        // 초기 씬 체크는 하지 않음
        // CheckMCPServerInCurrentScene();
    }
    
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // 플레이 모드 진입 시 MCP 서버 확인
            EnsureMCPServerExists();
        }
    }
    
    private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        // 플레이 모드에서만 MCP 서버 확인
        if (Application.isPlaying)
        {
            EnsureMCPServerExists();
        }
    }
    
    private static void CheckMCPServerInCurrentScene()
    {
        // 플레이 모드에서만 실행
        if (Application.isPlaying)
        {
            EnsureMCPServerExists();
        }
    }
    
    /// <summary>
    /// MCP 서버가 씬에 존재하는지 확인하고, 없으면 생성
    /// </summary>
    public static void EnsureMCPServerExists()
    {
        // 플레이 모드가 아닐 때는 실행하지 않음
        if (!Application.isPlaying) return;
        
        // 이미 존재하는지 확인
        UnityMCPServer existingServer = Object.FindFirstObjectByType<UnityMCPServer>();
        
        if (existingServer == null)
        {
            // MCP 서버 오브젝트 생성
            GameObject mcpServerObj = GameObject.Find(MCP_SERVER_OBJECT_NAME);
            
            if (mcpServerObj == null)
            {
                mcpServerObj = new GameObject(MCP_SERVER_OBJECT_NAME);
                mcpServerObj.AddComponent<UnityMCPServer>();
                
                // 플레이 모드에서만 DontDestroyOnLoad 설정
                if (Application.isPlaying)
                {
                    Object.DontDestroyOnLoad(mcpServerObj);
                }
                
                Debug.Log("✅ Unity MCP 서버가 자동으로 생성되었습니다!");
            }
        }
        else
        {
            Debug.Log("🔍 Unity MCP 서버가 이미 존재합니다.");
        }
    }
    
    /// <summary>
    /// 메뉴에서 수동으로 MCP 서버 생성
    /// </summary>
    [MenuItem("Unity MCP/서버 생성", false, 1)]
    public static void CreateMCPServer()
    {
        if (Application.isPlaying)
        {
            EnsureMCPServerExists();
        }
        else
        {
            Debug.LogWarning("⚠️ MCP 서버는 플레이 모드에서만 생성할 수 있습니다.");
        }
    }
    
    /// <summary>
    /// 메뉴에서 MCP 서버 제거
    /// </summary>
    [MenuItem("Unity MCP/서버 제거", false, 2)]
    public static void RemoveMCPServer()
    {
        UnityMCPServer[] servers = Object.FindObjectsByType<UnityMCPServer>(FindObjectsSortMode.None);
        
        foreach (var server in servers)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(server.gameObject);
            }
            else
            {
                Object.DestroyImmediate(server.gameObject);
            }
        }
        
        Debug.Log($"🗑️ {servers.Length}개의 Unity MCP 서버가 제거되었습니다.");
        
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
    
    /// <summary>
    /// MCP 서버 상태 확인
    /// </summary>
    [MenuItem("Unity MCP/서버 상태 확인", false, 3)]
    public static void CheckMCPServerStatus()
    {
        UnityMCPServer[] servers = Object.FindObjectsByType<UnityMCPServer>(FindObjectsSortMode.None);
        
        if (servers.Length == 0)
        {
            Debug.Log("❌ Unity MCP 서버를 찾을 수 없습니다.");
        }
        else if (servers.Length == 1)
        {
            Debug.Log("✅ Unity MCP 서버가 정상적으로 실행 중입니다.");
        }
        else
        {
            Debug.LogWarning($"⚠️ 중복된 Unity MCP 서버가 발견되었습니다. ({servers.Length}개)");
        }
    }
    
    /// <summary>
    /// MCP 설정 파일 열기
    /// </summary>
    [MenuItem("Unity MCP/설정 파일 열기", false, 11)]
    public static void OpenMCPConfig()
    {
        string configPath = System.IO.Path.Combine(Application.dataPath, "MCP", "README.md");
        
        if (System.IO.File.Exists(configPath))
        {
            Application.OpenURL("file://" + configPath);
        }
        else
        {
            Debug.LogError("❌ MCP 설정 파일을 찾을 수 없습니다: " + configPath);
        }
    }
    
    /// <summary>
    /// Unity MCP 서버 로그 테스트
    /// </summary>
    [MenuItem("Unity MCP/로그 테스트", false, 21)]
    public static void TestMCPLogs()
    {
        Debug.Log("🔵 MCP 로그 테스트 - 정보 메시지");
        Debug.LogWarning("🟡 MCP 로그 테스트 - 경고 메시지");
        Debug.LogError("🔴 MCP 로그 테스트 - 오류 메시지");
        
        Debug.Log("✅ MCP 로그 테스트 완료! 터미널의 Problems 탭에서 확인하세요.");
    }
}
#endif 