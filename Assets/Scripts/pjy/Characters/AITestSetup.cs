using UnityEngine;

/// <summary>
/// AI 시스템 테스트를 위한 설정 스크립트
/// GameScene에 추가하여 플레이어와 AI를 자동으로 설정합니다.
/// </summary>
public class AITestSetup : MonoBehaviour
{
    [Header("테스트 설정")]
    public bool enableAutoSetup = true;
    public AIBehavior.AIDifficulty aiDifficulty = AIBehavior.AIDifficulty.Normal;
    
    [Header("초기 자원")]
    public int humanPlayerStartMinerals = 100;
    public int aiPlayerStartMinerals = 100;
    
    private void Start()
    {
        if (!enableAutoSetup) return;
        
        // GameManager가 플레이어를 초기화할 때까지 잠시 대기
        Invoke(nameof(SetupTestEnvironment), 0.5f);
    }
    
    private void SetupTestEnvironment()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[AITestSetup] GameManager를 찾을 수 없습니다!");
            return;
        }
        
        // 인간 플레이어 설정
        if (GameManager.Instance.humanPlayer != null)
        {
            GameManager.Instance.humanPlayer.AddMinerals(humanPlayerStartMinerals);
            Debug.Log($"[AITestSetup] 인간 플레이어에게 {humanPlayerStartMinerals} 미네랄 지급");
        }
        
        // AI 플레이어 설정
        if (GameManager.Instance.aiPlayer != null)
        {
            GameManager.Instance.aiPlayer.AddMinerals(aiPlayerStartMinerals);
            GameManager.Instance.aiPlayer.SetAIDifficulty(aiDifficulty);
            Debug.Log($"[AITestSetup] AI 플레이어에게 {aiPlayerStartMinerals} 미네랄 지급, 난이도: {aiDifficulty}");
        }
        
        Debug.Log("[AITestSetup] 테스트 환경 설정 완료!");
        LogPlayerStatus();
    }
    
    private void Update()
    {
        // F1 키로 플레이어 상태 확인
        if (Input.GetKeyDown(KeyCode.F1))
        {
            LogPlayerStatus();
        }
        
        // F2 키로 AI 난이도 변경
        if (Input.GetKeyDown(KeyCode.F2))
        {
            CycleAIDifficulty();
        }
        
        // F3 키로 플레이어에게 미네랄 추가
        if (Input.GetKeyDown(KeyCode.F3))
        {
            AddMineralsToAllPlayers(50);
        }
    }
    
    private void LogPlayerStatus()
    {
        Debug.Log("=== 플레이어 상태 ===");
        
        var players = GameManager.Instance.GetAllPlayers();
        foreach (var player in players)
        {
            Debug.Log($"플레이어: {player.PlayerName} (ID: {player.PlayerID})");
            Debug.Log($"  - AI: {player.IsAI}");
            Debug.Log($"  - 미네랄: {player.CurrentMinerals}");
            Debug.Log($"  - 캐릭터 수: {player.CharacterCount}/50");
            
            if (player.IsAI)
            {
                var aiBehavior = player.GetComponent<AIBehavior>();
                if (aiBehavior != null)
                {
                    Debug.Log($"  - AI 난이도: {aiDifficulty}");
                }
            }
        }
    }
    
    private void CycleAIDifficulty()
    {
        switch (aiDifficulty)
        {
            case AIBehavior.AIDifficulty.Easy:
                aiDifficulty = AIBehavior.AIDifficulty.Normal;
                break;
            case AIBehavior.AIDifficulty.Normal:
                aiDifficulty = AIBehavior.AIDifficulty.Hard;
                break;
            case AIBehavior.AIDifficulty.Hard:
                aiDifficulty = AIBehavior.AIDifficulty.Expert;
                break;
            case AIBehavior.AIDifficulty.Expert:
                aiDifficulty = AIBehavior.AIDifficulty.Easy;
                break;
        }
        
        GameManager.Instance.SetAllAIDifficulty(aiDifficulty);
        Debug.Log($"[AITestSetup] AI 난이도 변경: {aiDifficulty}");
    }
    
    private void AddMineralsToAllPlayers(int amount)
    {
        var players = GameManager.Instance.GetAllPlayers();
        foreach (var player in players)
        {
            player.AddMinerals(amount);
        }
        Debug.Log($"[AITestSetup] 모든 플레이어에게 {amount} 미네랄 추가");
    }
}