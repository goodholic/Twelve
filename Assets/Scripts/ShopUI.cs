using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 상점 UI를 관리하는 클래스
/// 다이아-골드 교환 및 현금-다이아 구매 UI를 제어합니다.
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("UI 탭")]
    [SerializeField] private GameObject diamondToGoldTab;   // 다이아→골드 교환 탭
    [SerializeField] private GameObject cashToDiamondTab;   // 현금→다이아 구매 탭

    [Header("다이아-골드 교환 버튼")]
    [SerializeField] private Button exchange15DiaButton;    // 15다이아 → 100골드
    [SerializeField] private Button exchange55DiaButton;    // 55다이아 → 500골드
    [SerializeField] private Button exchange150DiaButton;   // 150다이아 → 1500골드
    [SerializeField] private Button exchange350DiaButton;   // 350다이아 → 4000골드
    [SerializeField] private Button exchange1000DiaButton;  // 1000다이아 → 12000골드

    [Header("현금-다이아 구매 버튼")]
    [SerializeField] private Button purchase1200WonButton;  // 1200원 → 15다이아
    [SerializeField] private Button purchase6500WonButton;  // 6500원 → 55다이아
    [SerializeField] private Button purchase13000WonButton; // 13000원 → 150다이아
    [SerializeField] private Button purchase27000WonButton; // 27000원 → 350다이아
    [SerializeField] private Button purchase69000WonButton; // 69000원 → 1000다이아

    [Header("상점 텍스트 정보")]
    [SerializeField] private TextMeshProUGUI[] diamondCostTexts;   // 다이아 소모량 표시 텍스트 배열
    [SerializeField] private TextMeshProUGUI[] goldRewardTexts;    // 골드 획득량 표시 텍스트 배열
    [SerializeField] private TextMeshProUGUI[] wonCostTexts;       // 원화 가격 표시 텍스트 배열
    [SerializeField] private TextMeshProUGUI[] diamondRewardTexts; // 다이아 획득량 표시 텍스트 배열

    // ShopManager 참조
    private ShopManager shopManager;

    private void Awake()
    {
        // ShopManager 찾기
        shopManager = FindFirstObjectByType<ShopManager>();
        if (shopManager == null)
        {
            Debug.LogError("[ShopUI] ShopManager를 찾을 수 없습니다.");
        }
    }

    private void Start()
    {
        // 버튼에 이벤트 리스너 설정
        SetupButtonListeners();
        
        // 초기 상태 설정
        ShowDiamondToGoldTab();
        
        // 가격 정보 초기화
        InitPriceTexts();
    }

    /// <summary>
    /// 모든 버튼에 리스너 설정
    /// </summary>
    private void SetupButtonListeners()
    {
        // 다이아-골드 교환 버튼 설정
        if (exchange15DiaButton) exchange15DiaButton.onClick.AddListener(OnExchange15DiaButtonClick);
        if (exchange55DiaButton) exchange55DiaButton.onClick.AddListener(OnExchange55DiaButtonClick);
        if (exchange150DiaButton) exchange150DiaButton.onClick.AddListener(OnExchange150DiaButtonClick);
        if (exchange350DiaButton) exchange350DiaButton.onClick.AddListener(OnExchange350DiaButtonClick);
        if (exchange1000DiaButton) exchange1000DiaButton.onClick.AddListener(OnExchange1000DiaButtonClick);

        // 현금-다이아 구매 버튼 설정
        if (purchase1200WonButton) purchase1200WonButton.onClick.AddListener(OnPurchase1200WonButtonClick);
        if (purchase6500WonButton) purchase6500WonButton.onClick.AddListener(OnPurchase6500WonButtonClick);
        if (purchase13000WonButton) purchase13000WonButton.onClick.AddListener(OnPurchase13000WonButtonClick);
        if (purchase27000WonButton) purchase27000WonButton.onClick.AddListener(OnPurchase27000WonButtonClick);
        if (purchase69000WonButton) purchase69000WonButton.onClick.AddListener(OnPurchase69000WonButtonClick);
    }

    /// <summary>
    /// 가격 정보 텍스트 초기화
    /// </summary>
    private void InitPriceTexts()
    {
        // 다이아-골드 교환 정보
        int[] diamondCosts = { 15, 55, 150, 350, 1000 };
        int[] goldRewards = { 100, 500, 1500, 4000, 12000 };

        // 현금-다이아 구매 정보
        int[] wonCosts = { 1200, 6500, 13000, 27000, 69000 };
        int[] diamondRewards = { 15, 55, 150, 350, 1000 };

        // 다이아 소모량 텍스트 설정
        for (int i = 0; i < diamondCostTexts.Length && i < diamondCosts.Length; i++)
        {
            if (diamondCostTexts[i] != null)
            {
                diamondCostTexts[i].text = $"{diamondCosts[i]} 다이아";
            }
        }

        // 골드 획득량 텍스트 설정
        for (int i = 0; i < goldRewardTexts.Length && i < goldRewards.Length; i++)
        {
            if (goldRewardTexts[i] != null)
            {
                goldRewardTexts[i].text = $"{goldRewards[i]} 골드";
            }
        }

        // 원화 가격 텍스트 설정
        for (int i = 0; i < wonCostTexts.Length && i < wonCosts.Length; i++)
        {
            if (wonCostTexts[i] != null)
            {
                wonCostTexts[i].text = $"{wonCosts[i]}원";
            }
        }

        // 다이아 획득량 텍스트 설정
        for (int i = 0; i < diamondRewardTexts.Length && i < diamondRewards.Length; i++)
        {
            if (diamondRewardTexts[i] != null)
            {
                diamondRewardTexts[i].text = $"{diamondRewards[i]} 다이아";
            }
        }
    }

    #region 탭 전환 함수

    /// <summary>
    /// 다이아-골드 교환 탭 표시
    /// </summary>
    public void ShowDiamondToGoldTab()
    {
        if (diamondToGoldTab) diamondToGoldTab.SetActive(true);
        if (cashToDiamondTab) cashToDiamondTab.SetActive(false);
    }

    /// <summary>
    /// 현금-다이아 구매 탭 표시
    /// </summary>
    public void ShowCashToDiamondTab()
    {
        if (diamondToGoldTab) diamondToGoldTab.SetActive(false);
        if (cashToDiamondTab) cashToDiamondTab.SetActive(true);
    }

    #endregion

    #region 다이아-골드 교환 버튼 이벤트

    private void OnExchange15DiaButtonClick()
    {
        if (shopManager != null)
        {
            shopManager.Exchange15DiamondsTo100Gold();
        }
    }

    private void OnExchange55DiaButtonClick()
    {
        if (shopManager != null)
        {
            shopManager.Exchange55DiamondsTo500Gold();
        }
    }

    private void OnExchange150DiaButtonClick()
    {
        if (shopManager != null)
        {
            shopManager.Exchange150DiamondsTo1500Gold();
        }
    }

    private void OnExchange350DiaButtonClick()
    {
        if (shopManager != null)
        {
            shopManager.Exchange350DiamondsTo4000Gold();
        }
    }

    private void OnExchange1000DiaButtonClick()
    {
        if (shopManager != null)
        {
            shopManager.Exchange1000DiamondsTo12000Gold();
        }
    }

    #endregion

    #region 현금-다이아 구매 버튼 이벤트

    private void OnPurchase1200WonButtonClick()
    {
        if (shopManager != null)
        {
            shopManager.Purchase1200WonFor15Diamonds();
        }
    }

    private void OnPurchase6500WonButtonClick()
    {
        if (shopManager != null)
        {
            shopManager.Purchase6500WonFor55Diamonds();
        }
    }

    private void OnPurchase13000WonButtonClick()
    {
        if (shopManager != null)
        {
            shopManager.Purchase13000WonFor150Diamonds();
        }
    }

    private void OnPurchase27000WonButtonClick()
    {
        if (shopManager != null)
        {
            shopManager.Purchase27000WonFor350Diamonds();
        }
    }

    private void OnPurchase69000WonButtonClick()
    {
        if (shopManager != null)
        {
            shopManager.Purchase69000WonFor1000Diamonds();
        }
    }

    #endregion

    /// <summary>
    /// 상점 UI 닫기
    /// </summary>
    public void CloseShop()
    {
        gameObject.SetActive(false);
    }
} 