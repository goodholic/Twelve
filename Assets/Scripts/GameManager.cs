using UnityEngine;
using System.Collections.Generic;
using Fusion;
// ▼ 아래 2줄은 필요하다면 제거하거나 주석처리 가능합니다.
// using UnityEngine.SceneManagement; // (씬 전환 미사용 시 주석처리 가능)
using TMPro;
using UnityEngine.UI; // ▼▼ [추가] Image 클래스 사용을 위해 추가 ▼▼

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }

    [Header("Managers")]
    public WaveSpawner waveSpawner;
    public PlacementManager placementManager;

    // ------------------ 새로 추가된 부분 ------------------
    [Header("[호스트 전용 9칸 덱]")]
    public CharacterData[] hostDeck = new CharacterData[9];

    [Header("[클라이언트 전용 9칸 덱]")]
    public CharacterData[] clientDeck = new CharacterData[9];
    // -----------------------------------------------------

    [Header("현재 등록된 캐릭터(총 10개)")]
    [Tooltip("인덱스 0~8: 일반, 인덱스 9: 히어로 (옵션)")]
    public CharacterData[] currentRegisteredCharacters = new CharacterData[10];

    private NetworkRunner runner;

    // =======================================================================
    // == 기존에는 resultSceneName 필드 + 씬 이동 로직이 있었으나 제거했습니다. ==
    // =======================================================================
    // public string resultSceneName = "ResultScene"; // (사용 안 함)

    [HideInInspector] public bool isGameOver = false;  // 게임이 끝났는지 여부
    [HideInInspector] public bool isVictory = false;   // true면 승리, false면 패배

    // =================== (추가) 결과 패널/텍스트 연결 ===================
    [Header("결과 패널(씬 전환 대신 사용)")]
    public GameObject resultPanel;          // 인스펙터에서 연결 (기본비활성)
    public TextMeshProUGUI resultPanelText; // 승/패 문구 표시용 TMP

    // =================== [추가] 지역1 성 체력 관리 ===================
    [Header("지역1 성 체력")]
    [SerializeField] private TextMeshProUGUI region1LifeText;
    public int region1Life = 10;
    
    // =================== [새로 추가] 중간성 & 최종성 ===================
    [Header("지역1 중간성 (3개 라인)")]
    public MiddleCastle region1LeftMiddleCastle;
    public MiddleCastle region1CenterMiddleCastle;
    public MiddleCastle region1RightMiddleCastle;
    
    [Header("지역2 중간성 (3개 라인)")]
    public MiddleCastle region2LeftMiddleCastle;
    public MiddleCastle region2CenterMiddleCastle;
    public MiddleCastle region2RightMiddleCastle;
    
    [Header("최종성")]
    public FinalCastle region1FinalCastle;
    public FinalCastle region2FinalCastle;

    private void Awake()
    {
        // 싱글톤 중복 방지
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        // 네트워크 런너 찾기
        runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null)
        {
            if (runner.GameMode == GameMode.Host)
            {
                // 호스트면 hostDeck을 currentRegisteredCharacters[0..8]에 복사
                for (int i = 0; i < 9; i++)
                {
                    currentRegisteredCharacters[i] = (hostDeck != null && i < hostDeck.Length)
                        ? hostDeck[i]
                        : null;
                }
                Debug.Log("[GameManager] Host => hostDeck 사용");
            }
            else if (runner.GameMode == GameMode.Client)
            {
                // 클라이언트면 clientDeck을 currentRegisteredCharacters[0..8]에 복사
                for (int i = 0; i < 9; i++)
                {
                    currentRegisteredCharacters[i] = (clientDeck != null && i < clientDeck.Length)
                        ? clientDeck[i]
                        : null;
                }
                Debug.Log("[GameManager] Client => clientDeck 사용");
            }
        }
        else
        {
            // 싱글플레이(네트워크 없음) => 호스트 덱 사용
            for (int i = 0; i < 9; i++)
            {
                currentRegisteredCharacters[i] = (hostDeck != null && i < hostDeck.Length)
                    ? hostDeck[i]
                    : null;
            }
            Debug.Log("[GameManager] 싱글플레이 => hostDeck 사용");
        }
    }

    private void Start()
    {
        if (waveSpawner == null)
        {
            waveSpawner = FindFirstObjectByType<WaveSpawner>();
        }
        if (placementManager == null)
        {
            placementManager = FindFirstObjectByType<PlacementManager>();
        }
        
        // 지역1 생명력 텍스트 초기화
        UpdateRegion1LifeText();
        
        // 중간성과 최종성 이벤트 연결
        SetupCastleEvents();
    }
    
    /// <summary>
    /// 중간성과 최종성의 파괴 이벤트 연결
    /// </summary>
    private void SetupCastleEvents()
    {
        // 지역1 중간성
        if (region1LeftMiddleCastle != null)
        {
            region1LeftMiddleCastle.OnMiddleCastleDestroyed += OnMiddleCastleDestroyed;
        }
        if (region1CenterMiddleCastle != null)
        {
            region1CenterMiddleCastle.OnMiddleCastleDestroyed += OnMiddleCastleDestroyed;
        }
        if (region1RightMiddleCastle != null)
        {
            region1RightMiddleCastle.OnMiddleCastleDestroyed += OnMiddleCastleDestroyed;
        }
        
        // 지역2 중간성
        if (region2LeftMiddleCastle != null)
        {
            region2LeftMiddleCastle.OnMiddleCastleDestroyed += OnMiddleCastleDestroyed;
        }
        if (region2CenterMiddleCastle != null)
        {
            region2CenterMiddleCastle.OnMiddleCastleDestroyed += OnMiddleCastleDestroyed;
        }
        if (region2RightMiddleCastle != null)
        {
            region2RightMiddleCastle.OnMiddleCastleDestroyed += OnMiddleCastleDestroyed;
        }
        
        // 최종성
        if (region1FinalCastle != null)
        {
            region1FinalCastle.OnFinalCastleDestroyed += OnFinalCastleDestroyed;
        }
        if (region2FinalCastle != null)
        {
            region2FinalCastle.OnFinalCastleDestroyed += OnFinalCastleDestroyed;
        }
    }
    
    /// <summary>
    /// 중간성이 파괴되었을 때 호출
    /// </summary>
    private void OnMiddleCastleDestroyed(RouteType route, int areaIndex)
    {
        Debug.Log($"[GameManager] 지역{areaIndex} {route} 중간성 파괴됨!");
        
        // 해당 라인의 캐릭터들이 최종성으로 목표 변경하도록 알림
        Character[] allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var character in allCharacters)
        {
            if (character != null && character.areaIndex == areaIndex && character.selectedRoute == route)
            {
                // 이미 CharacterCombat에서 자동으로 처리되므로 여기서는 로그만
                Debug.Log($"[GameManager] {character.characterName}의 목표가 최종성으로 변경될 예정");
            }
        }
    }
    
    /// <summary>
    /// 최종성이 파괴되었을 때 호출 (게임 종료)
    /// </summary>
    private void OnFinalCastleDestroyed(int areaIndex)
    {
        Debug.Log($"[GameManager] 지역{areaIndex} 최종성 파괴됨! 게임 종료!");
        
        // 지역1 최종성 파괴 = 플레이어 패배
        // 지역2 최종성 파괴 = 플레이어 승리
        bool victory = (areaIndex == 2);
        SetGameOver(victory);
    }

    private void Update()
    {
        // ESC로 종료
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    /// <summary>
    /// (버튼) 웨이브 시작
    /// </summary>
    public void StartWave()
    {
        if (waveSpawner != null)
        {
            waveSpawner.StartNextWave();
        }
    }

    /// <summary>
    /// 게임 오버(승/패) 처리.  
    /// 기존에는 `SceneManager.LoadScene(resultSceneName);`를 호출했으나 제거하고,  
    /// **같은 씬**에서 `resultPanel.SetActive(true)`로 대체합니다.
    /// </summary>
    public void SetGameOver(bool victory)
    {
        if (isGameOver) return; // 이미 끝났다면 중복 처리 방지
        isGameOver = true;
        isVictory = victory;

        Debug.Log($"[GameManager] GameOver!! isVictory={victory}");
        
        // ▼▼ [추가] resultPanel null 체크 디버그 로그 ▼▼
        Debug.Log($"[GameManager] resultPanel is null? {resultPanel == null}");
        if (resultPanel != null)
        {
            Debug.Log($"[GameManager] resultPanel name: {resultPanel.name}, active: {resultPanel.activeSelf}");
        }

        // 승리 시 100골드 지급
        if (victory)
        {
            // ShopManager를 통해 골드 지급
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.AddGold(100);
                Debug.Log("[GameManager] 승리 보상으로 100골드가 지급되었습니다.");
            }
            else
            {
                Debug.LogWarning("[GameManager] ShopManager.Instance가 null - 골드를 지급할 수 없습니다.");
            }
        }

        // === 씬 이동 대신, 결과 패널을 켬 ===
        if (resultPanel != null && resultPanel.gameObject != null)
        {
            Debug.Log($"[GameManager] resultPanel을 활성화합니다: {resultPanel.name}");
            resultPanel.SetActive(true);

            // 승리냐 패배냐에 따라 텍스트 변경
            if (resultPanelText != null)
            {
                resultPanelText.text = victory ? "승리!" : "패배...";
                Debug.Log($"[GameManager] 결과 텍스트 설정: {resultPanelText.text}");
            }
            else
            {
                Debug.LogWarning("[GameManager] resultPanelText가 null입니다!");
            }
        }
        // else: 혹시라도 resultPanel이 null이면 메세지만 출력
        else
        {
            Debug.LogWarning("[GameManager] resultPanel이 null 또는 파괴됨 -> Fallback 로직 실행");
            
            // ▼▼ [수정] 더 안전한 Fallback: 씬에서 Victory 패널 찾기 ▼▼
            Debug.Log("[GameManager] Fallback: 씬에서 Victory/Defeat 패널을 찾아봅니다...");
            
            // 먼저 활성화된 오브젝트에서 찾기
            GameObject[] activeObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            GameObject foundPanel = null;
            
            // 더 많은 패널 이름 패턴 추가
            string[] panelKeywords = { "Victory", "Result", "Win", "GameOver", "End", "Popup", "Panel" };
            
            foreach (GameObject obj in activeObjects)
            {
                // 패널 이름에 키워드가 포함되어 있는지 확인
                bool hasKeyword = false;
                foreach (string keyword in panelKeywords)
                {
                    if (obj.name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hasKeyword = true;
                        break;
                    }
                }
                
                if (hasKeyword)
                {
                    // Canvas나 Panel 컴포넌트가 있는지 확인
                    if (obj.GetComponent<Canvas>() != null || obj.GetComponent<CanvasGroup>() != null || 
                        (obj.GetComponent<RectTransform>() != null && obj.transform.parent != null))
                    {
                        // 프리팹이 아닌 씬의 오브젝트인지 확인
                        if (obj.scene.IsValid() && obj.scene.name != null)
                        {
                            Debug.Log($"[GameManager] 후보 패널 발견: {obj.name} (씬: {obj.scene.name}, 활성화: {obj.activeSelf})");
                            
                            // Victory나 Win이 포함된 패널을 우선적으로 선택
                            if (obj.name.IndexOf("Victory", System.StringComparison.OrdinalIgnoreCase) >= 0 || 
                                obj.name.IndexOf("Win", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                foundPanel = obj;
                                break;
                            }
                            // 아니면 Result가 포함된 패널 선택
                            else if (foundPanel == null && obj.name.IndexOf("Result", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                foundPanel = obj;
                            }
                        }
                    }
                }
            }
            
            // 패널을 못 찾았으면 Canvas를 찾아서 그 자식들 검색
            if (foundPanel == null)
            {
                Debug.Log("[GameManager] 패널을 못 찾았습니다. Canvas의 자식들을 검색합니다...");
                Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
                
                foreach (Canvas canvas in allCanvases)
                {
                    if (canvas.gameObject.scene.IsValid())
                    {
                        Debug.Log($"[GameManager] Canvas 발견: {canvas.name}");
                        
                        // Canvas의 모든 자식 검색
                        RectTransform[] children = canvas.GetComponentsInChildren<RectTransform>(true);
                        foreach (RectTransform child in children)
                        {
                            foreach (string keyword in panelKeywords)
                            {
                                if (child.name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    Debug.Log($"[GameManager] Canvas 자식에서 패널 발견: {child.name}");
                                    if (child.name.IndexOf("Victory", System.StringComparison.OrdinalIgnoreCase) >= 0 || 
                                        child.name.IndexOf("Win", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        child.name.IndexOf("Result", System.StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        foundPanel = child.gameObject;
                                        break;
                                    }
                                }
                            }
                            if (foundPanel != null) break;
                        }
                        if (foundPanel != null) break;
                    }
                }
            }
            
            if (foundPanel != null)
            {
                Debug.Log($"[GameManager] Victory 패널을 찾았습니다: {foundPanel.name}. 활성화합니다.");
                foundPanel.SetActive(true);
                
                // 부모 오브젝트들도 활성화 (Canvas가 비활성화되어 있을 수 있음)
                Transform parent = foundPanel.transform.parent;
                while (parent != null)
                {
                    if (!parent.gameObject.activeSelf)
                    {
                        Debug.Log($"[GameManager] 부모 오브젝트도 활성화: {parent.name}");
                        parent.gameObject.SetActive(true);
                    }
                    parent = parent.parent;
                }
                
                // 텍스트도 찾아서 업데이트 시도
                var textComponents = foundPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
                bool textFound = false;
                foreach (var text in textComponents)
                {
                    // VICTORY 텍스트나 결과 텍스트 찾기
                    if (text.name.IndexOf("Result", System.StringComparison.OrdinalIgnoreCase) >= 0 || 
                        text.name.IndexOf("Text", System.StringComparison.OrdinalIgnoreCase) >= 0 || 
                        text.name.IndexOf("Victory", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        text.text.IndexOf("VICTORY", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (victory)
                        {
                            // 기존 텍스트에 VICTORY가 있으면 유지, 없으면 "승리!"로 변경
                            if (text.text.IndexOf("VICTORY", System.StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                text.text = "승리!";
                            }
                        }
                        else
                        {
                            text.text = "패배...";
                        }
                        Debug.Log($"[GameManager] 텍스트 업데이트: {text.name} = {text.text}");
                        textFound = true;
                    }
                }
                
                if (!textFound)
                {
                    Debug.LogWarning("[GameManager] 결과 텍스트를 찾을 수 없었습니다.");
                }
                
                // 패널이 제대로 표시되는지 확인
                Debug.Log($"[GameManager] 패널 활성화 상태: {foundPanel.activeSelf}");
                Canvas foundCanvas = foundPanel.GetComponentInParent<Canvas>();
                if (foundCanvas != null)
                {
                    Debug.Log($"[GameManager] Canvas 활성화 상태: {foundCanvas.gameObject.activeSelf}");
                }
            }
            else
            {
                Debug.LogError("[GameManager] Victory 패널을 찾을 수 없습니다. Unity Inspector에서 GameManager의 Result Panel을 연결해주세요!");
                
                // 최후의 수단: 간단한 승리 메시지 생성
                Debug.Log("[GameManager] 최후의 수단: 간단한 승리 메시지를 생성합니다.");
                Canvas mainCanvas = FindFirstObjectByType<Canvas>();
                if (mainCanvas != null)
                {
                    GameObject victoryObj = new GameObject("VictoryMessage");
                    victoryObj.transform.SetParent(mainCanvas.transform, false);
                    
                    RectTransform rect = victoryObj.AddComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(400, 200);
                    rect.anchoredPosition = Vector2.zero;
                    
                    Image bg = victoryObj.AddComponent<Image>();
                    bg.color = new Color(0, 0, 0, 0.8f);
                    
                    GameObject textObj = new GameObject("Text");
                    textObj.transform.SetParent(victoryObj.transform, false);
                    
                    RectTransform textRect = textObj.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;
                    textRect.anchoredPosition = Vector2.zero;
                    
                    TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
                    tmpText.text = victory ? "승리!" : "패배...";
                    tmpText.fontSize = 48;
                    tmpText.alignment = TextAlignmentOptions.Center;
                    tmpText.color = victory ? Color.yellow : Color.red;
                    
                    Debug.Log("[GameManager] 간단한 승리 메시지를 생성했습니다.");
                }
            }
        }
    }

    /// <summary>
    /// 지역1 성에 데미지를 입힙니다.
    /// </summary>
    public void TakeDamageToRegion1(int amount)
    {
        region1Life -= amount;
        if (region1Life < 0)
            region1Life = 0;

        UpdateRegion1LifeText();
        Debug.Log($"[GameManager] 지역1 체력 감소: {amount} => 남은 HP={region1Life}");

        if (region1Life <= 0)
        {
            Debug.LogWarning("[GameManager] 지역1 체력=0 => 플레이어 패배!");
            SetGameOver(false); // 패배
        }
    }

    /// <summary>
    /// 지역1 생명력 텍스트 업데이트
    /// </summary>
    private void UpdateRegion1LifeText()
    {
        if (region1LifeText != null)
        {
            region1LifeText.text = $"HP: {region1Life}";
        }
    }
    
    /// <summary>
    /// 라우트에 해당하는 중간성 반환
    /// </summary>
    public MiddleCastle GetMiddleCastle(int areaIndex, RouteType route)
    {
        if (areaIndex == 1)
        {
            switch (route)
            {
                case RouteType.Left:
                    return region1LeftMiddleCastle;
                case RouteType.Center:
                    return region1CenterMiddleCastle;
                case RouteType.Right:
                    return region1RightMiddleCastle;
            }
        }
        else if (areaIndex == 2)
        {
            switch (route)
            {
                case RouteType.Left:
                    return region2LeftMiddleCastle;
                case RouteType.Center:
                    return region2CenterMiddleCastle;
                case RouteType.Right:
                    return region2RightMiddleCastle;
            }
        }
        return null;
    }
    
    /// <summary>
    /// 지역에 해당하는 최종성 반환
    /// </summary>
    public FinalCastle GetFinalCastle(int areaIndex)
    {
        if (areaIndex == 1)
            return region1FinalCastle;
        else if (areaIndex == 2)
            return region2FinalCastle;
        return null;
    }

    /// <summary>
    /// 게임 상태를 초기화합니다 (결과 화면에서 로비로 돌아갈 때 사용)
    /// </summary>
    public void ResetGameState()
    {
        isGameOver = false;
        isVictory = false;
        region1Life = 10;
        
        // 결과 패널 비활성화
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
        
        // 지역1 생명력 텍스트 업데이트
        UpdateRegion1LifeText();
        
        Debug.Log("[GameManager] 게임 상태가 초기화되었습니다.");
    }
}