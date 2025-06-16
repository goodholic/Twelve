using UnityEngine;
using System.Collections.Generic;
// using Fusion; // 임시로 주석처리
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
    public bool isGameEnded = false;
    public bool isGameStarted = false;
    public bool isGamePaused = false;
    public bool isGameOver = false;
    public bool isVictory = false;  // 승리 상태 추가
    
    [Header("게임 오버")]
    public GameObject gameOverUI;
    public GameObject victoryUI;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        // 웨이브 스포너 초기화
        if (waveSpawner != null)
        {
            waveSpawner.Initialize();
        }
        
        // 배치 매니저 초기화
        if (placementManager != null)
        {
            // placementManager 초기화 로직
        }
        
        // 결과 UI 숨기기
        if (resultUIPanel != null)
        {
            resultUIPanel.SetActive(false);
        }
        
        Debug.Log("[GameManager] 게임 시작!");
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
        // TODO: 보상 UI 표시 및 캐릭터 선택 로직
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
}