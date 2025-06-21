using UnityEngine;
using System.Collections.Generic;
// using Fusion; // 임시로 주석처리
// ▼ 아래 2줄은 필요하다면 제거하거나 주석처리 가능합니다.
// using UnityEngine.SceneManagement; // (씬 전환 미사용 시 주석처리 가능)
using TMPro;
using UnityEngine.UI; // ▼▼ [추가] Image 클래스 사용을 위해 추가 ▼▼

// 게임 상태 열거형 정의
public enum GameState
{
    Menu,
    Playing,
    Paused,
    GameOver,
    Victory
}

public class GameManager : MonoBehaviour
{
    // GameManager 준비 완료 이벤트
    public static System.Action OnGameManagerReady;
    
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

    // private NetworkRunner runner; // 임시로 주석처리

    // =======================================================================
    // == 기존에는 resultSceneName 필드 + 씬 이동 로직이 있었으나 제거했습니다.
    // == 대신 결과 UI를 표시하는 방식으로 변경합니다.
    // =======================================================================
    
    [Header("결과 UI 패널")]
    [Tooltip("게임 종료 시 표시할 결과 UI 패널")]
    public GameObject resultUIPanel;
    
    [Header("결과 텍스트")]
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultDescriptionText;
    
    [Header("성 체력")]
    public int region1CastleHealth = 1000;
    public int region2CastleHealth = 1000;
    
    [Header("웨이브 정보")]
    public int currentWave = 0;
    public int totalWaves = 20;
    public int completedWaves = 0;
    
    [Header("게임 상태")]
    public GameState gameState = GameState.Menu;
    public bool isGameEnded = false;
    public bool isGameStarted = false;
    public bool isGamePaused = false;
    public bool isGameOver = false;
    public bool isVictory = false;  // 승리 상태 추가
    
    [Header("게임 오버")]
    public GameObject gameOverUI;
    public GameObject victoryUI;
    
    [Header("플레이어 관리")]
    private List<PlayerController> registeredPlayers = new List<PlayerController>();
    public PlayerController humanPlayer;
    public PlayerController aiPlayer;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        // GameManager 준비 완료 이벤트 호출
        OnGameManagerReady?.Invoke();
    }

    void Start()
    {
        try
        {
            // 웨이브 스포너 초기화
            if (waveSpawner != null)
            {
                waveSpawner.Initialize();
            }
            else
            {
                Debug.LogWarning("[GameManager] WaveSpawner가 할당되지 않았습니다!");
            }
            
            // 배치 매니저 초기화
            if (placementManager != null)
            {
                // placementManager 초기화 로직
            }
            else
            {
                Debug.LogWarning("[GameManager] PlacementManager가 할당되지 않았습니다!");
            }
            
            // 결과 UI 숨기기
            if (resultUIPanel != null)
            {
                resultUIPanel.SetActive(false);
            }
            
            // 플레이어 초기화
            InitializePlayers();
            
            Debug.Log("[GameManager] 게임 시작!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameManager] 초기화 중 오류 발생: {e.Message}\n{e.StackTrace}");
        }
    }
    
    void Update()
    {
        // 미네랄 자동 생성
        UpdateMineralGeneration();
    }

    /// <summary>
    /// 지역1 성에 데미지
    /// </summary>
    public void TakeDamageToRegion1(int damage)
    {
        if (isGameEnded) return;
        
        region1CastleHealth -= damage;
        Debug.Log($"[GameManager] 지역1 성이 {damage} 데미지를 받았습니다! 남은 체력: {region1CastleHealth}");
        
        if (region1CastleHealth <= 0)
        {
            region1CastleHealth = 0;
            EndGame(false, "지역1 성이 파괴되었습니다!");
        }
    }
    
    /// <summary>
    /// 지역2 성에 데미지
    /// </summary>
    public void TakeDamageToRegion2(int damage)
    {
        if (isGameEnded) return;
        
        region2CastleHealth -= damage;
        Debug.Log($"[GameManager] 지역2 성이 {damage} 데미지를 받았습니다! 남은 체력: {region2CastleHealth}");
        
        if (region2CastleHealth <= 0)
        {
            region2CastleHealth = 0;
            EndGame(true, "지역2 성이 파괴되었습니다!");
        }
    }
    
    /// <summary>
    /// 웨이브 완료 처리
    /// </summary>
    public void OnWaveCompleted()
    {
        completedWaves++;
        Debug.Log($"[GameManager] 웨이브 {completedWaves} 완료!");
        
        // 5웨이브마다 보상
        if (completedWaves % 5 == 0)
        {
            GiveWaveReward();
        }
        
        // 모든 웨이브 완료
        if (completedWaves >= totalWaves)
        {
            EndGame(true, $"모든 웨이브({totalWaves})를 완료했습니다!");
        }
    }
    
    /// <summary>
    /// 5웨이브 보상 (게임 기획서: 랜덤 2성 캐릭터 3개 중 1개 선택)
    /// </summary>
    private void GiveWaveReward()
    {
        Debug.Log($"[GameManager] {completedWaves}웨이브 보상! 랜덤 2성 캐릭터 선택 가능");
        
        // WaveRewardUI를 통해 보상 선택 UI 표시
        if (pjy.UI.WaveRewardUI.Instance != null)
        {
            pjy.UI.WaveRewardUI.Instance.ShowWaveReward(completedWaves);
        }
        else
        {
            Debug.LogWarning("[GameManager] WaveRewardUI.Instance가 null입니다!");
            // 기본 보상 대신 골드 지급
            AddGold(100 * completedWaves / 5); // 5웨이브당 100골드씩 증가
        }
    }
    
    /// <summary>
    /// 게임 종료 처리
    /// </summary>
    private void EndGame(bool isVictory, string reason)
    {
        if (isGameEnded) return;
        
        isGameEnded = true;
        Debug.Log($"[GameManager] 게임 종료! 승리: {isVictory}, 이유: {reason}");
        
        // 웨이브 스포너 중지
        if (waveSpawner != null)
        {
            waveSpawner.StopSpawning();
        }
        
        // 결과 UI 표시
        ShowResultUI(isVictory, reason);
    }
    
    /// <summary>
    /// 결과 UI 표시
    /// </summary>
    private void ShowResultUI(bool isVictory, string reason)
    {
        if (resultUIPanel != null)
        {
            resultUIPanel.SetActive(true);
            
            if (resultTitleText != null)
            {
                resultTitleText.text = isVictory ? "승리!" : "패배";
            }
            
            if (resultDescriptionText != null)
            {
                resultDescriptionText.text = $"{reason}\n완료 웨이브: {completedWaves}/{totalWaves}";
            }
        }
    }

    /// <summary>
    /// 게임 상태 초기화
    /// </summary>
    public void ResetGameState()
    {
        isGameEnded = false;
        region1CastleHealth = 1000;
        region2CastleHealth = 1000;
        currentWave = 0;
        completedWaves = 0;
        
        if (resultUIPanel != null)
        {
            resultUIPanel.SetActive(false);
        }
        
        Debug.Log("[GameManager] 게임 상태가 초기화되었습니다.");
    }
    
    /// <summary>
    /// 현재 웨이브 가져오기
    /// </summary>
    public int GetCurrentWave()
    {
        return currentWave;
    }
    
    /// <summary>
    /// 웨이브 시작
    /// </summary>
    public void StartWave(int waveNumber)
    {
        currentWave = waveNumber;
        Debug.Log($"[GameManager] 웨이브 {currentWave} 시작!");
    }
    
    /// <summary>
    /// 게임 시작 (튜토리얼에서 호출)
    /// </summary>
    public void StartGame()
    {
        if (isGameStarted)
        {
            Debug.LogWarning("[GameManager] 게임이 이미 시작되었습니다!");
            return;
        }
        
        isGameStarted = true;
        gameState = GameState.Playing;
        isGamePaused = false;
        isGameOver = false;
        isVictory = false;
        isGameEnded = false;
        
        // 게임 시간 정상화
        Time.timeScale = 1f;
        
        // 웨이브 시작
        if (waveSpawner != null)
        {
            waveSpawner.StartNextWave();
        }
        
        Debug.Log("[GameManager] 게임이 시작되었습니다!");
    }
    
    /// <summary>
    /// 게임 일시정지/재개
    /// </summary>
    public void PauseGame(bool pause)
    {
        isGamePaused = pause;
        gameState = pause ? GameState.Paused : GameState.Playing;
        Time.timeScale = pause ? 0f : 1f;
        
        Debug.Log($"[GameManager] 게임 {(pause ? "일시정지" : "재개")}");
    }
    
    /// <summary>
    /// 디버그용: 강제 승리
    /// </summary>
    [ContextMenu("Force Victory")]
    public void ForceVictory()
    {
        EndGame(true, "디버그: 강제 승리");
    }
    
    /// <summary>
    /// 디버그용: 강제 패배
    /// </summary>
    [ContextMenu("Force Defeat")]
    public void ForceDefeat()
    {
        EndGame(false, "디버그: 강제 패배");
    }

    /// <summary>
    /// 게임 오버 설정
    /// </summary>
    public void SetGameOver(bool victory = false)
    {
        isGameOver = true;
        isVictory = victory;
        
        if (victory)
        {
            Debug.Log("[GameManager] 게임 승리!");
            if (victoryUI != null)
                victoryUI.SetActive(true);
        }
        else
        {
            Debug.Log("[GameManager] 게임 오버!");
            if (gameOverUI != null)
                gameOverUI.SetActive(true);
        }
        
        // 게임 일시정지
        Time.timeScale = 0f;
    }

    /// <summary>
    /// 골드 추가 (몬스터 처치 보상 등)
    /// </summary>
    public void AddGold(int amount)
    {
        Debug.Log($"[GameManager] 골드 {amount} 획득!");
        
        // ShopManager가 있으면 연동
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.AddGold(amount);
        }
        else
        {
            Debug.LogWarning("[GameManager] ShopManager.Instance가 null입니다!");
        }
    }
    
    #region Player Management
    
    /// <summary>
    /// 플레이어 초기화
    /// </summary>
    private void InitializePlayers()
    {
        try
        {
            // 씬에서 기존 플레이어 찾기
            PlayerController[] existingPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            
            if (existingPlayers == null || existingPlayers.Length == 0)
            {
                // 플레이어가 없으면 생성
                CreateDefaultPlayers();
            }
            else
            {
                // 기존 플레이어 등록
                foreach (var player in existingPlayers)
                {
                    if (player != null)
                    {
                        RegisterPlayer(player);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameManager] 플레이어 초기화 중 오류: {e.Message}");
            // 오류 발생 시 기본 플레이어라도 생성
            CreateDefaultPlayers();
        }
    }
    
    /// <summary>
    /// 기본 플레이어 생성 (인간 플레이어 1명, AI 플레이어 1명)
    /// </summary>
    private void CreateDefaultPlayers()
    {
        // 인간 플레이어 생성
        GameObject humanPlayerObj = new GameObject("HumanPlayer");
        humanPlayer = humanPlayerObj.AddComponent<PlayerController>();
        humanPlayer.EnableAI(false);
        RegisterPlayer(humanPlayer);
        
        // AI 플레이어 생성
        GameObject aiPlayerObj = new GameObject("AIPlayer");
        aiPlayer = aiPlayerObj.AddComponent<PlayerController>();
        aiPlayer.EnableAI(true);
        aiPlayer.SetAIDifficulty(AIBehavior.AIDifficulty.Normal);
        RegisterPlayer(aiPlayer);
        
        Debug.Log("[GameManager] 기본 플레이어 생성 완료 (인간 1명, AI 1명)");
    }
    
    /// <summary>
    /// 플레이어 등록
    /// </summary>
    public void RegisterPlayer(PlayerController player)
    {
        if (!registeredPlayers.Contains(player))
        {
            registeredPlayers.Add(player);
            Debug.Log($"[GameManager] 플레이어 '{player.PlayerName}' (ID: {player.PlayerID}, AI: {player.IsAI}) 등록됨");
            
            // 인간/AI 플레이어 자동 할당
            if (!player.IsAI && humanPlayer == null)
            {
                humanPlayer = player;
            }
            else if (player.IsAI && aiPlayer == null)
            {
                aiPlayer = player;
            }
        }
    }
    
    /// <summary>
    /// 플레이어 등록 해제
    /// </summary>
    public void UnregisterPlayer(PlayerController player)
    {
        if (registeredPlayers.Contains(player))
        {
            registeredPlayers.Remove(player);
            Debug.Log($"[GameManager] 플레이어 '{player.PlayerName}' 등록 해제됨");
            
            if (player == humanPlayer) humanPlayer = null;
            if (player == aiPlayer) aiPlayer = null;
        }
    }
    
    /// <summary>
    /// 모든 플레이어 가져오기
    /// </summary>
    public List<PlayerController> GetAllPlayers()
    {
        return new List<PlayerController>(registeredPlayers);
    }
    
    /// <summary>
    /// AI 플레이어 가져오기
    /// </summary>
    public List<PlayerController> GetAIPlayers()
    {
        return registeredPlayers.FindAll(p => p.IsAI);
    }
    
    /// <summary>
    /// 인간 플레이어 가져오기
    /// </summary>
    public List<PlayerController> GetHumanPlayers()
    {
        return registeredPlayers.FindAll(p => !p.IsAI);
    }
    
    /// <summary>
    /// ID로 플레이어 찾기
    /// </summary>
    public PlayerController GetPlayerByID(int playerID)
    {
        return registeredPlayers.Find(p => p.PlayerID == playerID);
    }
    
    /// <summary>
    /// AI 난이도 일괄 설정
    /// </summary>
    public void SetAllAIDifficulty(AIBehavior.AIDifficulty difficulty)
    {
        foreach (var player in GetAIPlayers())
        {
            player.SetAIDifficulty(difficulty);
        }
        Debug.Log($"[GameManager] 모든 AI 플레이어 난이도를 {difficulty}로 설정");
    }
    
    #endregion
    
    #region 미네랄 시스템
    
    [Header("자원 관리")]
    [SerializeField] private int playerMineral = 100;
    [SerializeField] private int mineralPerSecond = 10;
    [SerializeField] private TextMeshProUGUI mineralText;
    
    private float mineralTimer = 0f;
    
    /// <summary>
    /// 미네랄 자동 생성 업데이트
    /// </summary>
    private void UpdateMineralGeneration()
    {
        if (gameState != GameState.Playing) return;
        
        mineralTimer += Time.deltaTime;
        if (mineralTimer >= 1f)
        {
            mineralTimer -= 1f;
            AddMineral(mineralPerSecond);
        }
    }
    
    /// <summary>
    /// 현재 미네랄 반환
    /// </summary>
    public int GetPlayerMineral()
    {
        return playerMineral;
    }
    
    /// <summary>
    /// 미네랄 추가
    /// </summary>
    public void AddMineral(int amount)
    {
        playerMineral += amount;
        UpdateMineralUI();
    }
    
    /// <summary>
    /// 미네랄 사용
    /// </summary>
    public bool SpendMineral(int amount)
    {
        if (playerMineral >= amount)
        {
            playerMineral -= amount;
            UpdateMineralUI();
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 미네랄 UI 업데이트
    /// </summary>
    private void UpdateMineralUI()
    {
        if (mineralText != null)
        {
            mineralText.text = playerMineral.ToString();
        }
    }
    
    #endregion
}