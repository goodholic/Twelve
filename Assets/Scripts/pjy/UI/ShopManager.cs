using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

/// <summary>
/// 상점 시스템을 관리하는 클래스
/// 다이아몬드와 골드 간의 교환, 현금과 다이아몬드 간의 교환을 처리합니다.
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("플레이어 재화 참조")]
    [SerializeField] private int playerDiamonds = 0;  // 플레이어의 다이아 수량
    [SerializeField] private int playerGold = 0;      // 플레이어의 골드 수량

    [Header("UI 요소")]
    [SerializeField] private TextMeshProUGUI diamondText;  // 다이아 표시 텍스트
    [SerializeField] private TextMeshProUGUI goldText;     // 골드 표시 텍스트
    [SerializeField] private GameObject insufficientDiamondPanel;  // 다이아 부족 알림 패널
    [SerializeField] private GameObject purchaseCompletePanel;    // 구매 완료 알림 패널
    
    [Header("결제 관련")]
    [SerializeField] private bool useRealPayment = false;  // 실제 결제 사용 여부 (테스트용)

    // 싱글톤 패턴
    public static ShopManager Instance { get; private set; }

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 씬 전환 시에도 파괴되지 않도록 설정 (필요시 주석 해제)
        // DontDestroyOnLoad(gameObject);
        
        // UI 패널 초기 설정
        if (insufficientDiamondPanel) insufficientDiamondPanel.SetActive(false);
        if (purchaseCompletePanel) purchaseCompletePanel.SetActive(false);
    }

    private void Start()
    {
        // 초기 UI 업데이트
        UpdateUI();
    }

    /// <summary>
    /// UI 텍스트 업데이트
    /// </summary>
    private void UpdateUI()
    {
        if (diamondText) diamondText.text = playerDiamonds.ToString();
        if (goldText) goldText.text = playerGold.ToString();
    }

    #region 다이아-골드 교환 함수

    /// <summary>
    /// 15다이아를 100골드로 교환
    /// </summary>
    public void Exchange15DiamondsTo100Gold()
    {
        ExchangeDiamondsToGold(15, 100);
    }

    /// <summary>
    /// 55다이아를 500골드로 교환
    /// </summary>
    public void Exchange55DiamondsTo500Gold()
    {
        ExchangeDiamondsToGold(55, 500);
    }

    /// <summary>
    /// 150다이아를 1500골드로 교환
    /// </summary>
    public void Exchange150DiamondsTo1500Gold()
    {
        ExchangeDiamondsToGold(150, 1500);
    }

    /// <summary>
    /// 350다이아를 4000골드로 교환
    /// </summary>
    public void Exchange350DiamondsTo4000Gold()
    {
        ExchangeDiamondsToGold(350, 4000);
    }

    /// <summary>
    /// 1000다이아를 12000골드로 교환
    /// </summary>
    public void Exchange1000DiamondsTo12000Gold()
    {
        ExchangeDiamondsToGold(1000, 12000);
    }

    /// <summary>
    /// 다이아몬드를 골드로 교환하는 내부 함수
    /// </summary>
    /// <param name="diamondCost">소모할 다이아 수량</param>
    /// <param name="goldReward">획득할 골드 수량</param>
    private void ExchangeDiamondsToGold(int diamondCost, int goldReward)
    {
        // 다이아 보유량 확인
        if (playerDiamonds >= diamondCost)
        {
            // 다이아 차감
            playerDiamonds -= diamondCost;
            
            // 골드 지급
            playerGold += goldReward;
            
            // UI 업데이트
            UpdateUI();
            
            // 구매 완료 메시지 표시
            ShowPurchaseComplete($"{diamondCost} 다이아를 {goldReward} 골드로 교환했습니다.");
            
            // 플레이어 데이터 저장 (필요시)
            SavePlayerData();
            
            Debug.Log($"[ShopManager] 교환 성공: {diamondCost} 다이아 -> {goldReward} 골드, 남은 다이아: {playerDiamonds}, 현재 골드: {playerGold}");
        }
        else
        {
            // 다이아 부족 메시지 표시
            ShowInsufficientDiamonds();
            Debug.Log($"[ShopManager] 교환 실패: 다이아 부족 (필요: {diamondCost}, 보유: {playerDiamonds})");
        }
    }

    #endregion

    #region 현금-다이아 구매 함수

    /// <summary>
    /// 1200원 지불하고 15다이아 획득
    /// </summary>
    public void Purchase1200WonFor15Diamonds()
    {
        PurchaseDiamondsWithRealMoney(1200, 15);
    }

    /// <summary>
    /// 6500원 지불하고 55다이아 획득
    /// </summary>
    public void Purchase6500WonFor55Diamonds()
    {
        PurchaseDiamondsWithRealMoney(6500, 55);
    }

    /// <summary>
    /// 13000원 지불하고 150다이아 획득
    /// </summary>
    public void Purchase13000WonFor150Diamonds()
    {
        PurchaseDiamondsWithRealMoney(13000, 150);
    }

    /// <summary>
    /// 27000원 지불하고 350다이아 획득
    /// </summary>
    public void Purchase27000WonFor350Diamonds()
    {
        PurchaseDiamondsWithRealMoney(27000, 350);
    }

    /// <summary>
    /// 69000원 지불하고 1000다이아 획득
    /// </summary>
    public void Purchase69000WonFor1000Diamonds()
    {
        PurchaseDiamondsWithRealMoney(69000, 1000);
    }

    /// <summary>
    /// 실제 화폐로 다이아몬드를 구매하는 내부 함수
    /// </summary>
    /// <param name="wonCost">원화 비용</param>
    /// <param name="diamondReward">획득할 다이아 수량</param>
    private void PurchaseDiamondsWithRealMoney(int wonCost, int diamondReward)
    {
        if (useRealPayment)
        {
            // 실제 결제 시스템 연동 (IAP 등)
            // 여기에 실제 결제 로직 구현 (예: Google Play, App Store 등)
            // RealPaymentManager.Instance.ProcessPayment(wonCost, OnPaymentSuccess, OnPaymentFailed);
            Debug.Log($"[ShopManager] 실제 결제 요청: {wonCost}원 -> {diamondReward} 다이아");
        }
        else
        {
            // 테스트용 - 결제 성공으로 간주하고 다이아 지급
            ProcessSuccessfulPurchase(wonCost, diamondReward);
        }
    }

    /// <summary>
    /// 결제 성공 시 호출되는 함수 (IAP 콜백 등에서 호출)
    /// </summary>
    public void OnPaymentSuccess(int wonCost, int diamondReward)
    {
        ProcessSuccessfulPurchase(wonCost, diamondReward);
    }

    /// <summary>
    /// 결제 실패 시 호출되는 함수 (IAP 콜백 등에서 호출)
    /// </summary>
    public void OnPaymentFailed(string errorMessage)
    {
        Debug.LogError($"[ShopManager] 결제 실패: {errorMessage}");
        // 결제 실패 메시지 표시 (필요시)
    }

    /// <summary>
    /// 결제 성공 후 실제 다이아 지급 처리
    /// </summary>
    private void ProcessSuccessfulPurchase(int wonCost, int diamondReward)
    {
        // 다이아 지급
        playerDiamonds += diamondReward;
        
        // UI 업데이트
        UpdateUI();
        
        // 구매 완료 메시지 표시
        ShowPurchaseComplete($"{wonCost}원 결제로 {diamondReward} 다이아를 획득했습니다.");
        
        // 플레이어 데이터 저장 (필요시)
        SavePlayerData();
        
        Debug.Log($"[ShopManager] 구매 성공: {wonCost}원 -> {diamondReward} 다이아, 현재 다이아: {playerDiamonds}");
    }

    #endregion

    #region 유틸리티 함수

    /// <summary>
    /// 다이아 부족 알림 표시
    /// </summary>
    private void ShowInsufficientDiamonds()
    {
        if (insufficientDiamondPanel)
        {
            insufficientDiamondPanel.SetActive(true);
            // 일정 시간 후 자동으로 닫히도록 설정 (선택 사항)
            Invoke(nameof(HideInsufficientDiamonds), 2.0f);
        }
        else
        {
            Debug.LogWarning("[ShopManager] insufficientDiamondPanel이 할당되지 않았습니다.");
        }
    }

    /// <summary>
    /// 다이아 부족 알림 숨기기
    /// </summary>
    public void HideInsufficientDiamonds()
    {
        if (insufficientDiamondPanel)
        {
            insufficientDiamondPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 구매 완료 알림 표시
    /// </summary>
    private void ShowPurchaseComplete(string message)
    {
        if (purchaseCompletePanel)
        {
            // 메시지 내용 설정 (TextMeshProUGUI 컴포넌트가 있다고 가정)
            TextMeshProUGUI messageText = purchaseCompletePanel.GetComponentInChildren<TextMeshProUGUI>();
            if (messageText)
            {
                messageText.text = message;
            }
            
            purchaseCompletePanel.SetActive(true);
            // 일정 시간 후 자동으로 닫히도록 설정 (선택 사항)
            Invoke(nameof(HidePurchaseComplete), 2.0f);
        }
        else
        {
            Debug.LogWarning("[ShopManager] purchaseCompletePanel이 할당되지 않았습니다.");
        }
    }

    /// <summary>
    /// 구매 완료 알림 숨기기
    /// </summary>
    public void HidePurchaseComplete()
    {
        if (purchaseCompletePanel)
        {
            purchaseCompletePanel.SetActive(false);
        }
    }

    /// <summary>
    /// 플레이어 데이터 저장 (PlayerPrefs 또는 다른 저장 시스템 사용)
    /// </summary>
    private void SavePlayerData()
    {
        PlayerPrefs.SetInt("PlayerDiamonds", playerDiamonds);
        PlayerPrefs.SetInt("PlayerGold", playerGold);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 플레이어 데이터 로드 (PlayerPrefs 또는 다른 저장 시스템 사용)
    /// </summary>
    private void LoadPlayerData()
    {
        playerDiamonds = PlayerPrefs.GetInt("PlayerDiamonds", 0);
        playerGold = PlayerPrefs.GetInt("PlayerGold", 0);
        UpdateUI();
    }

    /// <summary>
    /// 테스트용: 다이아 추가
    /// </summary>
    public void AddDiamondsForTesting(int amount)
    {
        playerDiamonds += amount;
        UpdateUI();
        SavePlayerData();
        Debug.Log($"[ShopManager] 테스트용 다이아 {amount}개 추가, 현재 다이아: {playerDiamonds}");
    }

    /// <summary>
    /// 테스트용: 골드 추가
    /// </summary>
    public void AddGoldForTesting(int amount)
    {
        playerGold += amount;
        UpdateUI();
        SavePlayerData();
        Debug.Log($"[ShopManager] 테스트용 골드 {amount}개 추가, 현재 골드: {playerGold}");
    }
    
    /// <summary>
    /// 게임 승리 등 보상으로 골드 추가
    /// </summary>
    /// <param name="amount">추가할 골드 양</param>
    public void AddGold(int amount)
    {
        playerGold += amount;
        UpdateUI();
        SavePlayerData();
        Debug.Log($"[ShopManager] 보상으로 골드 {amount}개 추가, 현재 골드: {playerGold}");
    }

    /// <summary>
    /// 다이아 차감 시도 (뽑기, 아이템 구매 등에 사용)
    /// </summary>
    /// <param name="amount">차감할 다이아 양</param>
    /// <returns>차감 성공 여부 (true: 성공, false: 다이아 부족)</returns>
    public bool TrySpendDiamonds(int amount)
    {
        if (playerDiamonds >= amount)
        {
            playerDiamonds -= amount;
            UpdateUI();
            SavePlayerData();
            Debug.Log($"[ShopManager] 다이아 {amount}개 차감, 남은 다이아: {playerDiamonds}");
            return true;
        }
        else
        {
            ShowInsufficientDiamonds();
            Debug.Log($"[ShopManager] 다이아 부족: 필요 {amount}개, 보유 {playerDiamonds}개");
            return false;
        }
    }

    /// <summary>
    /// 골드 차감 시도 (캐릭터 업그레이드, 아이템 구매 등에 사용)
    /// </summary>
    /// <param name="amount">차감할 골드 양</param>
    /// <returns>차감 성공 여부 (true: 성공, false: 골드 부족)</returns>
    public bool TrySpendGold(int amount)
    {
        if (playerGold >= amount)
        {
            playerGold -= amount;
            UpdateUI();
            SavePlayerData();
            Debug.Log($"[ShopManager] 골드 {amount}개 차감, 남은 골드: {playerGold}");
            return true;
        }
        else
        {
            // 골드 부족 메시지 표시
            ShowInsufficientGold();
            Debug.Log($"[ShopManager] 골드 부족: 필요 {amount}개, 보유 {playerGold}개");
            return false;
        }
    }

    /// <summary>
    /// 골드 부족 알림 표시
    /// </summary>
    private void ShowInsufficientGold()
    {
        // 다이아 부족 알림 패널을 재사용 (별도 패널 생성도 가능)
        if (insufficientDiamondPanel)
        {
            // 메시지 내용을 골드 관련으로 변경
            TextMeshProUGUI messageText = insufficientDiamondPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (messageText)
            {
                messageText.text = "골드가 부족합니다!";
            }
            
            insufficientDiamondPanel.SetActive(true);
            Invoke(nameof(HideInsufficientDiamonds), 2.0f);
        }
        else
        {
            Debug.LogWarning("[ShopManager] insufficientDiamondPanel이 할당되지 않았습니다.");
        }
    }

    #endregion
}  