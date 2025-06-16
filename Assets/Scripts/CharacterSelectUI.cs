using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 캐릭터 선택 UI - 4+1 카드 시스템
/// 게임 기획서: Hand 4장 + Next 1장 -> Reserve
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    [Header("캐릭터 덱 (로비에서 가져옴)")]
    public CharacterData[] deckFromLobby = new CharacterData[10];
    
    // 카드 시스템: Hand(4) + Next(1) + Reserve(5)
    private int[] handIndices = new int[4] { 0, 1, 2, 3 };  // Hand에 있는 덱 인덱스들
    private int nextIndex = 4;                                // Next 카드의 덱 인덱스
    private List<int> reserveIndices = new List<int> { 5, 6, 7, 8, 9 }; // Reserve의 덱 인덱스들
    
    [Header("UI - 4장 카드 버튼")]
    public SelectButton[] selectButtons = new SelectButton[4];
    
    [Header("Next Unit UI")]
    public Image nextUnitImage;
    public TextMeshProUGUI nextUnitCost;
    
    [Header("주사위 버튼 (자동 합성)")]
    public Button diceButton;
    
    [Header("UI 패널")]
    public GameObject selectButtonParent;
    
    [Header("매니저 참조")]
    private PlacementManager placementManager;
    private CharacterInventoryManager characterInventory;
    
    // 카드 선택 상태
    private bool hasPendingCard = false;
    private int pendingCardIndex = -1;
    
    [System.Serializable]
    public class SelectButton
    {
        public Button button;
        public Image iconImage;
        public TextMeshProUGUI costText;
        public bool isEmpty = false;
        public int characterIndex = -1;
    }
    
    private void Start()
    {
        StartCoroutine(InitializeAfterDelay());
    }
    
    private IEnumerator InitializeAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        
        // CoreDataManager 체크
        if (CoreDataManager.Instance == null)
        {
            Debug.LogError("[CharacterSelectUI] CoreDataManager.Instance가 null입니다!");
            yield break;
        }
        
        // 1) 1~9 캐릭터
        if (GameManager.Instance != null &&
            GameManager.Instance.currentRegisteredCharacters != null &&
            GameManager.Instance.currentRegisteredCharacters.Length >= 9)
        {
            for (int i = 0; i < 9; i++)
            {
                deckFromLobby[i] = GameManager.Instance.currentRegisteredCharacters[i];
            }
            Debug.Log("[CharacterSelectUI] GameManager.currentRegisteredCharacters에서 9개 캐릭터 로드 완료");
        }
        else
        {
            Debug.LogError("[CharacterSelectUI] 1~9 캐릭터를 가져오지 못했습니다!");
            
            // 기본 캐릭터로 채우기 (테스트용)
            var coreData = CoreDataManager.Instance;
            if (coreData != null && coreData.characterDatabase != null && 
                coreData.characterDatabase.currentRegisteredCharacters != null)
            {
                for (int i = 0; i < 9 && i < coreData.characterDatabase.currentRegisteredCharacters.Length; i++)
                {
                    deckFromLobby[i] = coreData.characterDatabase.currentRegisteredCharacters[i];
                }
                Debug.Log("[CharacterSelectUI] CoreDataManager에서 캐릭터 데이터를 가져왔습니다.");
            }
        }

        // PlacementManager 찾기
        if (placementManager == null)
        {
            placementManager = PlacementManager.Instance;
            if (placementManager == null)
            {
                placementManager = FindFirstObjectByType<PlacementManager>();
            }
            
            if (placementManager == null)
            {
                Debug.LogError("[CharacterSelectUI] PlacementManager를 찾을 수 없습니다!");
            }
        }

        // 인벤토리 매니저 찾기
        if (characterInventory == null)
        {
            characterInventory = FindFirstObjectByType<CharacterInventoryManager>();
            if (characterInventory == null)
            {
                Debug.LogWarning("[CharacterSelectUI] CharacterInventoryManager를 찾을 수 없습니다.");
            }
        }
        
        // 초기 UI 설정
        RefreshSelectButtonsUI();
        RefreshNextUnitUI();
        
        // 주사위 버튼 설정
        if (diceButton != null)
        {
            diceButton.onClick.RemoveAllListeners();
            diceButton.onClick.AddListener(OnClickDiceButton);
        }
    }
    
    /// <summary>
    /// [신규] 드래그로 카드 사용 시 호출
    /// </summary>
    public void OnDragUseCard(int usedCharIndex)
    {
        Debug.Log($"[CharacterSelectUI] 드래그로 카드 사용: 인덱스 {usedCharIndex}");
        
        // Hand에서 사용한 카드의 슬롯 찾기
        int handSlot = -1;
        for (int i = 0; i < handIndices.Length; i++)
        {
            if (handIndices[i] == usedCharIndex)
            {
                handSlot = i;
                break;
            }
        }
        
        if (handSlot >= 0)
        {
            // SelectButton을 통해 카드 사용 처리
            if (selectButtons[handSlot] != null)
            {
                OnUseCard(selectButtons[handSlot]);
            }
        }
        else
        {
            Debug.LogWarning($"[CharacterSelectUI] 사용한 카드 인덱스 {usedCharIndex}를 Hand에서 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 카드 사용 시 처리
    /// </summary>
    private void OnUseCard(SelectButton usedButton)
    {
        if (usedButton == null || usedButton.isEmpty) return;
        
        int usedCharIndex = usedButton.characterIndex;
        
        // Hand에서 사용한 카드의 슬롯 찾기
        int handSlot = -1;
        for (int i = 0; i < selectButtons.Length; i++)
        {
            if (selectButtons[i] == usedButton)
            {
                handSlot = i;
                break;
            }
        }
        
        if (handSlot < 0)
        {
            Debug.LogError("[CharacterSelectUI] 사용한 버튼을 selectButtons에서 찾을 수 없습니다!");
            return;
        }
        
        // 카드 순환
        RotateCards(handSlot, usedCharIndex);
    }
    
    /// <summary>
    /// 4장 카드 버튼 UI 갱신
    /// </summary>
    private void RefreshSelectButtonsUI()
    {
        for (int i = 0; i < selectButtons.Length && i < handIndices.Length; i++)
        {
            SelectButton sb = selectButtons[i];
            if (sb == null) continue;
            
            int idx = handIndices[i];
            
            // 유효하지 않은 인덱스면 빈 카드로 표시
            if (idx < 0 || idx >= deckFromLobby.Length || deckFromLobby[idx] == null)
            {
                // 빈 카드
                sb.isEmpty = true;
                sb.characterIndex = -1;
                if (sb.iconImage != null)
                {
                    sb.iconImage.gameObject.SetActive(false);
                }
                if (sb.costText != null)
                {
                    sb.costText.gameObject.SetActive(false);
                }
                // 버튼은 활성 상태 유지 (빈 상태도 클릭 가능)
                sb.button.gameObject.SetActive(true);

                // 버튼 클릭 이벤트
                sb.button.onClick.RemoveAllListeners();
                int buttonIndex = i;
                sb.button.onClick.AddListener(() => OnClickCardButton(-1));
            }
            else
            {
                // 정상 카드
                CharacterData charData = deckFromLobby[idx];
                sb.isEmpty = false;
                sb.characterIndex = idx;

                // 아이콘 표시
                if (sb.iconImage != null && charData.buttonIcon != null)
                {
                    sb.iconImage.gameObject.SetActive(true);
                    sb.iconImage.sprite = charData.buttonIcon;
                }

                // 코스트 표시
                if (sb.costText != null)
                {
                    sb.costText.gameObject.SetActive(true);
                    sb.costText.text = charData.cost.ToString();
                }

                // 버튼 활성화
                sb.button.gameObject.SetActive(true);

                // 버튼 클릭 이벤트
                sb.button.onClick.RemoveAllListeners();
                int capturedIndex = idx;
                sb.button.onClick.AddListener(() => OnClickCardButton(capturedIndex));
            }

            // 드래그 소환용 컴포넌트
            DraggableSummonButtonUI dragComp = sb.button.GetComponent<DraggableSummonButtonUI>();
            if (dragComp == null)
            {
                dragComp = sb.button.gameObject.AddComponent<DraggableSummonButtonUI>();
            }

            dragComp.SetSummonData(sb.characterIndex);
            dragComp.parentSelectUI = this;
        }
    }

    /// <summary>
    /// Next Unit UI 갱신
    /// </summary>
    private void RefreshNextUnitUI()
    {
        if (nextIndex < 0 || nextIndex >= deckFromLobby.Length || deckFromLobby[nextIndex] == null)
        {
            // Next가 없는 경우
            if (nextUnitImage != null)
            {
                nextUnitImage.gameObject.SetActive(false);
            }
            if (nextUnitCost != null)
            {
                nextUnitCost.gameObject.SetActive(false);
            }
        }
        else
        {
            CharacterData nextData = deckFromLobby[nextIndex];
            if (nextUnitImage != null && nextData.buttonIcon != null)
            {
                nextUnitImage.gameObject.SetActive(true);
                nextUnitImage.sprite = nextData.buttonIcon;
            }
            if (nextUnitCost != null)
            {
                nextUnitCost.gameObject.SetActive(true);
                nextUnitCost.text = nextData.cost.ToString();
            }
        }
    }

    /// <summary>
    /// [신규] 카드 버튼 클릭 시 로직
    /// "이미 선택중인 카드가 있으면 무시 / 없으면 선택 대기"
    /// </summary>
    private void OnClickCardButton(int clickedIndex)
    {
        // CoreDataManager 확인
        if (CoreDataManager.Instance == null)
        {
            Debug.LogError("[CharacterSelectUI] CoreDataManager가 없습니다!");
            return;
        }

        // ▼▼ [수정] 이미 선택된 카드와 같은 카드를 클릭하면 유지, 다른 카드면 변경 ▼▼
        if (hasPendingCard && pendingCardIndex != clickedIndex)
        {
            Debug.Log($"[CharacterSelectUI] 기존 선택({pendingCardIndex}) -> 새로운 선택({clickedIndex})으로 변경");
        }

        hasPendingCard = true;
        pendingCardIndex = clickedIndex;

        // CoreDataManager에도 인덱스 설정
        CoreDataManager.Instance.currentCharacterIndex = clickedIndex;

        // PlacementManager에 "이 캐릭터 인덱스 선택" 알림
        if (placementManager != null)
        {
            placementManager.OnClickSelectUnit(clickedIndex);
            
            // ▼▼ [추가] 자동 배치 호출 (placed/placable 꽉 차면 walkable로) ▼▼
            placementManager.OnClickAutoPlace();
        }
        else
        {
            Debug.LogError("[CharacterSelectUI] PlacementManager가 null입니다!");
        }
    }
    
    /// <summary>
    /// 카드 순환 처리
    /// </summary>
    private void RotateCards(int handSlot, int usedCharIndex)
    {
        if (handSlot < 0 || handSlot >= handIndices.Length)
        {
            Debug.LogError($"[CharacterSelectUI] 잘못된 handSlot: {handSlot}");
            return;
        }

        // 1) Hand[handSlot] <- Next
        handIndices[handSlot] = nextIndex;

        // 2) Next <- Reserve의 첫 번째
        if (reserveIndices.Count > 0)
        {
            nextIndex = reserveIndices[0];
            reserveIndices.RemoveAt(0);
        }
        else
        {
            nextIndex = -1; // Reserve가 비어있으면 Next도 빔
        }

        // 3) Reserve <- 사용한 카드
        reserveIndices.Add(usedCharIndex);

        // UI 갱신
        RefreshSelectButtonsUI();
        RefreshNextUnitUI();

        Debug.Log($"[CharacterSelectUI] 카드 순환 완료. Hand: [{string.Join(",", handIndices)}], Next: {nextIndex}, Reserve: [{string.Join(",", reserveIndices)}]");
    }

    /// <summary>
    /// [추가] 주사위 버튼 클릭 시 자동 합성
    /// </summary>
    private void OnClickDiceButton()
    {
        Debug.Log("[CharacterSelectUI] 주사위 버튼 클릭 - 자동 합성 시도");

        // AutoMergeManager를 통해 자동 합성 실행
        AutoMergeManager autoMergeManager = FindFirstObjectByType<AutoMergeManager>();
        if (autoMergeManager != null)
        {
            autoMergeManager.OnDiceButtonClick(1); // 지역1 자동 합성
        }
        else
        {
            Debug.LogWarning("[CharacterSelectUI] AutoMergeManager를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// [테스트용] 버튼으로 Hand의 4장을 무작위 지역1 타일에 자동 배치
    /// </summary>
    public void OnClickUse4CardsAtRandom()
    {
        if (placementManager == null)
        {
            Debug.LogError("[CharacterSelectUI] PlacementManager가 null입니다!");
            return;
        }

        // 지역1의 모든 placable/placeTile 타일 찾기
        List<Tile> region1Tiles = new List<Tile>();
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);

        foreach (var tile in allTiles)
        {
            if (tile == null) continue;
            if (tile.isRegion2) continue; // 지역2는 제외

            if (tile.IsPlacable() || tile.IsPlaceTile())
            {
                region1Tiles.Add(tile);
            }
        }

        if (region1Tiles.Count == 0)
        {
            Debug.LogWarning("[CharacterSelectUI] 지역1에 배치 가능한 타일이 없습니다!");
            return;
        }

        // Hand의 4장 중 비어있지 않은 카드들만 배치
        List<int> validHandIndices = new List<int>();
        for (int i = 0; i < handIndices.Length; i++)
        {
            if (handIndices[i] >= 0 && handIndices[i] < deckFromLobby.Length && deckFromLobby[handIndices[i]] != null)
            {
                validHandIndices.Add(handIndices[i]);
            }
        }

        if (validHandIndices.Count == 0)
        {
            Debug.LogWarning("[CharacterSelectUI] Hand에 유효한 카드가 없습니다!");
            return;
        }

        // 실제 배치
        int placedCount = 0;
        System.Random rng = new System.Random();
        
        foreach (int charIndex in validHandIndices)
        {
            // 빈 타일 찾기
            List<Tile> emptyTiles = new List<Tile>();
            foreach (var tile in region1Tiles)
            {
                if (tile.CanPlaceCharacter())
                {
                    emptyTiles.Add(tile);
                }
            }

            if (emptyTiles.Count == 0)
            {
                Debug.Log($"[CharacterSelectUI] 더 이상 배치 가능한 타일이 없습니다. {placedCount}개 배치 완료.");
                break;
            }

            // 랜덤 타일 선택
            Tile randomTile = emptyTiles[rng.Next(emptyTiles.Count)];
            
            // 배치 시도
            bool success = placementManager.SummonCharacterOnTile(charIndex, randomTile);
            if (success)
            {
                placedCount++;
                // 사용된 카드 처리
                for (int i = 0; i < selectButtons.Length; i++)
                {
                    if (selectButtons[i].characterIndex == charIndex)
                    {
                        OnUseCard(selectButtons[i]);
                        break;
                    }
                }
            }
        }

        Debug.Log($"[CharacterSelectUI] 자동 배치 완료: {placedCount}개 캐릭터 배치됨");
    }
    
    /// <summary>
    /// SummonCharacterAtTile 메서드 수정
    /// </summary>
    private void SummonCharacterAtTile(int characterIndex, Tile tile)
    {
        if (characterIndex < 0 || characterIndex >= deckFromLobby.Length)
        {
            Debug.LogError($"[CharacterSelectUI] 잘못된 캐릭터 인덱스: {characterIndex}");
            return;
        }
        
        CharacterData charData = deckFromLobby[characterIndex];
        if (charData == null)
        {
            Debug.LogError($"[CharacterSelectUI] 캐릭터 데이터[{characterIndex}]가 null입니다!");
            return;
        }
        
        // PlacementManager를 통해 소환
        if (PlacementManager.Instance != null)
        {
            Character newChar = PlacementManager.Instance.SummonCharacterOnTile(charData, tile, false);
            if (newChar != null)
            {
                Debug.Log($"[CharacterSelectUI] {charData.characterName}을(를) {tile.name}에 소환 성공!");
            }
        }
    }
}