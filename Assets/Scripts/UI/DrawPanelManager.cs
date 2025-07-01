using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Data;

public class DrawPanelManager : MonoBehaviour
{
    [Header("CharacterInventoryManager 참조")]
    [SerializeField] private CharacterInventoryManager characterInventory;

    [Header("ShopManager 참조")]
    [SerializeField] private ShopManager shopManager;

    [Header("뽑기 결과 텍스트")]
    [SerializeField] private TextMeshProUGUI drawResultText;

    [Header("뽑기 결과 이미지")]
    [SerializeField] private Image drawResultImage;

    [Header("뽑기 버튼")]
    [SerializeField] private Button drawButton;
    [SerializeField] private Button drawTenButton; // 10개 뽑기 버튼 추가

    [Header("뽑기 비용")]
    [SerializeField] private int drawCost = 10; // 1개 뽑기에 필요한 다이아 비용
    [SerializeField] private int drawTenCost = 90; // 10개 뽑기에 필요한 다이아 비용 (10% 할인)

    [Header("에러 메시지 패널")]
    [SerializeField] private GameObject errorMessagePanel;
    [SerializeField] private TextMeshProUGUI errorMessageText;

    [Header("멀티 뽑기 결과 패널")]
    [SerializeField] private GameObject multiDrawResultPanel; // 10개 뽑기 결과 패널
    [SerializeField] private Transform multiDrawResultContainer; // 10개 뽑기 결과 아이콘을 표시할 컨테이너
    [SerializeField] private GameObject drawResultIconPrefab; // 결과 아이콘 프리팹

    // 중복 클릭 방지용
    private bool isDrawing = false;

    private void Awake()
    {
        if (drawButton)
            drawButton.onClick.AddListener(OnClickDraw);
            
        if (drawTenButton)
            drawTenButton.onClick.AddListener(OnClickDrawTen);

        if (shopManager == null)
            shopManager = FindFirstObjectByType<ShopManager>();

        // 에러 메시지 패널 초기화
        if (errorMessagePanel)
            errorMessagePanel.SetActive(false);
            
        // 멀티 뽑기 결과 패널 초기화
        if (multiDrawResultPanel)
            multiDrawResultPanel.SetActive(false);
    }

    private void OnClickDraw()
    {
        // 이미 뽑기 진행중이면 무시
        if (isDrawing)
        {
            Debug.LogWarning("[DrawPanelManager] 이미 뽑기 진행 중!");
            return;
        }
        
        // 단일 뽑기 실행
        DrawSingle();
    }
    
    private void OnClickDrawTen()
    {
        // 이미 뽑기 진행중이면 무시
        if (isDrawing)
        {
            Debug.LogWarning("[DrawPanelManager] 이미 뽑기 진행 중!");
            return;
        }
        
        // 10개 뽑기 실행
        DrawMultiple(10);
    }

    private void DrawSingle()
    {
        if (characterInventory == null)
        {
            Debug.LogWarning("[DrawPanelManager] characterInventory가 없음");
            return;
        }

        // 인벤토리 가득 찼는지 확인 (200칸 모두 차있는지 확인)
        bool isInventoryFull = true;
        for (int i = 0; i < characterInventory.sharedSlotData200.Length; i++)
        {
            if (characterInventory.sharedSlotData200[i] == null)
            {
                isInventoryFull = false;
                break;
            }
        }

        if (isInventoryFull)
        {
            // 인벤토리 가득 찼을 때 에러 메시지 표시
            ShowErrorMessage("인벤토리 200칸이 모두 가득 찼습니다!");
            return;
        }

        // 다이아몬드 차감 시도
        if (shopManager != null)
        {
            if (!shopManager.TrySpendDiamonds(drawCost))
            {
                // 다이아 부족
                ShowErrorMessage($"다이아가 부족합니다! (필요: {drawCost})");
                return;
            }
        }
        else
        {
            Debug.LogWarning("[DrawPanelManager] shopManager가 없어 다이아 차감 불가");
            return;
        }

        isDrawing = true; // 뽑기 시작

        // 1) 캐릭터 한 명 뽑기
        CharacterData newChar = characterInventory.DrawRandomCharacter();

        // 2) 결과 반영
        if (newChar != null)
        {
            // 즉시 저장
            characterInventory.SaveCharacters();

            if (drawResultText != null)
            {
                drawResultText.text = $"뽑기 성공: {newChar.characterName}";
            }

            if (drawResultImage != null)
            {
                drawResultImage.gameObject.SetActive(true);
                drawResultImage.color = Color.white;
                drawResultImage.enabled = true;

                // 아이콘이 있으면 사용, 없으면 null
                drawResultImage.sprite = (newChar.buttonIcon != null)
                    ? newChar.buttonIcon
                    : null;
            }

            // 인벤토리 UI 갱신(200칸)
            DeckPanelManager deckPanel = FindFirstObjectByType<DeckPanelManager>();
            if (deckPanel != null)
            {
                deckPanel.RefreshInventoryUI();
            }
        }
        else
        {
            // 풀 비어있으면 실패
            if (drawResultText != null)
                drawResultText.text = "뽑기 실패(풀 비어있음)";

            if (drawResultImage != null)
            {
                drawResultImage.gameObject.SetActive(true);
                drawResultImage.sprite = null;
            }
        }

        isDrawing = false;
    }
    
    private void DrawMultiple(int count)
    {
        if (characterInventory == null)
        {
            Debug.LogWarning("[DrawPanelManager] characterInventory가 없음");
            return;
        }

        // 인벤토리에 빈 슬롯 수 확인 (200칸 중에서)
        int emptySlotCount = 0;
        for (int i = 0; i < characterInventory.sharedSlotData200.Length; i++)
        {
            if (characterInventory.sharedSlotData200[i] == null)
            {
                emptySlotCount++;
            }
        }

        if (emptySlotCount < count)
        {
            // 인벤토리 공간 부족
            if (emptySlotCount == 0)
            {
                ShowErrorMessage("인벤토리 200칸이 모두 가득 찼습니다!");
            }
            else
            {
                ShowErrorMessage($"인벤토리에 빈 슬롯이 부족합니다! (필요: {count}, 남은 슬롯: {emptySlotCount})");
            }
            return;
        }

        // 다이아몬드 차감 시도
        if (shopManager != null)
        {
            int cost = (count == 10) ? drawTenCost : drawCost * count;
            if (!shopManager.TrySpendDiamonds(cost))
            {
                // 다이아 부족
                ShowErrorMessage($"다이아가 부족합니다! (필요: {cost})");
                return;
            }
        }
        else
        {
            Debug.LogWarning("[DrawPanelManager] shopManager가 없어 다이아 차감 불가");
            return;
        }

        isDrawing = true; // 뽑기 시작

        // 캐릭터 여러 명 뽑기
        List<CharacterData> newCharacters = new List<CharacterData>();
        for (int i = 0; i < count; i++)
        {
            CharacterData newChar = characterInventory.DrawRandomCharacter();
            if (newChar != null)
            {
                newCharacters.Add(newChar);
            }
        }

        // 즉시 저장
        characterInventory.SaveCharacters();

        // 멀티 뽑기 결과 표시
        ShowMultiDrawResult(newCharacters);

        // 인벤토리 UI 갱신(200칸)
        DeckPanelManager deckPanel = FindFirstObjectByType<DeckPanelManager>();
        if (deckPanel != null)
        {
            deckPanel.RefreshInventoryUI();
        }

        isDrawing = false;
    }
    
    private void ShowMultiDrawResult(List<CharacterData> characters)
    {
        if (multiDrawResultPanel == null || multiDrawResultContainer == null || drawResultIconPrefab == null)
        {
            Debug.LogWarning("[DrawPanelManager] 멀티 뽑기 결과 패널 또는 컨테이너가 설정되지 않았습니다.");
            return;
        }

        // 이전 결과 정리
        foreach (Transform child in multiDrawResultContainer)
        {
            Destroy(child.gameObject);
        }

        // 새 결과 추가
        foreach (CharacterData character in characters)
        {
            GameObject iconObj = Instantiate(drawResultIconPrefab, multiDrawResultContainer);
            
            // 컴포넌트 설정
            Image iconImage = iconObj.GetComponent<Image>();
            TextMeshProUGUI nameText = iconObj.GetComponentInChildren<TextMeshProUGUI>();
            
            // 캐릭터 정보 표시
            if (iconImage != null && character.buttonIcon != null)
            {
                iconImage.sprite = character.buttonIcon;
            }
            
            if (nameText != null)
            {
                nameText.text = character.characterName;
            }
        }

        // 결과 패널 표시
        multiDrawResultPanel.SetActive(true);

        // 5초 후 자동으로 결과 패널 닫기
        StartCoroutine(HideMultiDrawResultAfterDelay(5.0f));
    }

    /// <summary>
    /// 에러 메시지를 표시하고 자동으로 3초 후 숨깁니다.
    /// </summary>
    /// <param name="message">표시할 에러 메시지</param>
    private void ShowErrorMessage(string message)
    {
        if (errorMessagePanel == null || errorMessageText == null)
        {
            Debug.LogWarning("[DrawPanelManager] 에러 메시지 패널이 설정되지 않았습니다.");
            return;
        }

        // 메시지 설정 및 패널 활성화
        errorMessageText.text = message;
        errorMessagePanel.SetActive(true);

        // 3초 후 자동으로 숨기기
        StartCoroutine(HideErrorMessageAfterDelay(3.0f));
    }

    /// <summary>
    /// 지정된 시간 후에 에러 메시지 패널을 숨깁니다.
    /// </summary>
    /// <param name="delay">지연 시간(초)</param>
    private IEnumerator HideErrorMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (errorMessagePanel != null)
            errorMessagePanel.SetActive(false);
    }
    
    /// <summary>
    /// 지정된 시간 후에 멀티 뽑기 결과 패널을 숨깁니다.
    /// </summary>
    /// <param name="delay">지연 시간(초)</param>
    private IEnumerator HideMultiDrawResultAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (multiDrawResultPanel != null)
            multiDrawResultPanel.SetActive(false);
    }
    
    /// <summary>
    /// 멀티 뽑기 결과 패널을 닫습니다. (버튼에 연결하기 위한 public 메서드)
    /// </summary>
    public void CloseMultiDrawResult()
    {
        if (multiDrawResultPanel != null)
            multiDrawResultPanel.SetActive(false);
    }
}
