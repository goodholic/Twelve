// Assets\OX UI Scripts\CharacterSelectUI.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 캐릭터 선택 UI. 1~9 캐릭터 (또는 덱)에서 4장(Hand) + Next 1장 + Reserve 4장 로직을 다룸.
/// OnClickUse4CardsAtRandom()에서 '지역1'에 placable/placeTile이 꽉 찼을 경우 walkable에 배치하도록 수정함.
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    [Header("Placement Manager 참조")]
    public PlacementManager placementManager; // 캐릭터 배치(소환) 담당

    [System.Serializable]
    public class SelectButton
    {
        public Button button;
        public Image iconImage;
        public TextMeshProUGUI costText;
        public int characterIndex;  // 0~8
        public bool isEmpty;
    }

    [Header("소환 버튼(4개)")]
    public SelectButton[] selectButtons = new SelectButton[4];

    [Header("Next Unit (다음 카드)")]
    public Image nextUnitImage;
    public TextMeshProUGUI nextUnitCost;

    // =========================================
    // 1~9 캐릭터(인덱스 0..8) 저장
    // =========================================
    private CharacterData[] deckFromLobby = new CharacterData[9];

    // "handIndices"(4개), "nextIndex"(1개), "reserveIndices"(4개)
    private int[] handIndices = new int[4];
    private int nextIndex = -1;
    private List<int> reserveIndices = new List<int>(4);

    private List<int> allIndices = new List<int>();

    // =========================================
    // [추가] 클릭으로 카드 하나가 선택 중이면,
    //        그 카드가 배치될 때까지 다른 클릭을 무시하기 위한 플래그
    // =========================================
    private bool hasPendingCard = false;   // 이미 카드 하나가 선택된 상태인지?
    private int pendingCardIndex = -1;     // 현재 선택된(아직 배치 안 된) 카드 인덱스

    // =======================================================================
    // (1) 인벤토리 표시 => CharacterInventoryManager의 sharedSlotData200 활용
    // =====================================================================

    [Header("CharacterInventoryManager (인벤토리)")]
    [SerializeField] private CharacterInventoryManager characterInventory;

    private void Start()
    {
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
            return;
        }

        // ▼▼ [수정추가] CharacterInventoryManager 자동 찾기 ▼▼
        if (characterInventory == null)
        {
            characterInventory = FindFirstObjectByType<CharacterInventoryManager>();
            if (characterInventory != null)
            {
                Debug.Log("[CharacterSelectUI] CharacterInventoryManager를 자동으로 찾았습니다.");
            }
            else
            {
                Debug.LogWarning("[CharacterSelectUI] CharacterInventoryManager를 찾을 수 없습니다. Inspector에서 수동으로 연결하거나 씬에 추가해주세요.");
            }
        }

        // ▼▼ [수정추가] PlacementManager 자동 찾기 ▼▼
        if (placementManager == null)
        {
            placementManager = PlacementManager.Instance;
            if (placementManager == null)
            {
                placementManager = FindFirstObjectByType<PlacementManager>();
            }
            if (placementManager != null)
            {
                Debug.Log("[CharacterSelectUI] PlacementManager를 자동으로 찾았습니다.");
            }
            else
            {
                Debug.LogWarning("[CharacterSelectUI] PlacementManager를 찾을 수 없습니다. Inspector에서 수동으로 연결하거나 씬에 추가해주세요.");
            }
        }
        // ▲▲ [수정추가 끝] ▲▲

        // 2) 인덱스(0..8) 셔플
        allIndices.Clear();
        for (int i = 0; i < 9; i++)
        {
            allIndices.Add(i);
        }
        ShuffleList(allIndices);

        // 3) 처음 4개 -> hand
        for (int i = 0; i < 4; i++)
        {
            handIndices[i] = allIndices[i];
        }

        // 4) 5번째 -> nextIndex
        nextIndex = allIndices[4];

        // 5) 나머지 4개 -> reserve
        reserveIndices.Clear();
        for (int i = 5; i < 9; i++)
        {
            reserveIndices.Add(allIndices[i]);
        }

        // UI 갱신
        UpdateHandButtons();
        UpdateNextUnitUI();
    }

    private void UpdateHandButtons()
    {
        for (int i = 0; i < selectButtons.Length; i++)
        {
            selectButtons[i].characterIndex = handIndices[i];
            selectButtons[i].isEmpty = false;
            UpdateOneButtonUI(selectButtons[i]);
        }
    }

    private void UpdateNextUnitUI()
    {
        if (nextIndex < 0 || nextIndex >= deckFromLobby.Length)
        {
            if (nextUnitImage) nextUnitImage.sprite = null;
            if (nextUnitCost) nextUnitCost.text = "-";
            return;
        }

        CharacterData data = deckFromLobby[nextIndex];
        if (data == null)
        {
            if (nextUnitImage) nextUnitImage.sprite = null;
            if (nextUnitCost) nextUnitCost.text = "-";
            return;
        }

        if (nextUnitImage && data.buttonIcon != null)
        {
            nextUnitImage.sprite = data.buttonIcon.sprite;
        }
        if (nextUnitCost)
        {
            nextUnitCost.text = data.cost.ToString();
        }
    }

    private void UpdateOneButtonUI(SelectButton sb)
    {
        if (sb.button == null) return;
        sb.button.onClick.RemoveAllListeners();

        if (sb.characterIndex < 0 || sb.characterIndex >= deckFromLobby.Length)
        {
            sb.isEmpty = true;
            if (sb.iconImage) sb.iconImage.sprite = null;
            if (sb.costText) sb.costText.text = "-";
            return;
        }

        CharacterData data = deckFromLobby[sb.characterIndex];
        if (data != null)
        {
            sb.isEmpty = false;
            if (sb.iconImage && data.buttonIcon != null)
            {
                sb.iconImage.sprite = data.buttonIcon.sprite;
            }
            if (sb.costText)
            {
                sb.costText.text = data.cost.ToString();
            }

            int copyIndex = sb.characterIndex;
            // ===========================
            // 클릭 → 배치 대기 상태로 설정
            // ===========================
            sb.button.onClick.AddListener(() =>
            {
                OnClickCardButton(copyIndex);
            });
        }
        else
        {
            sb.isEmpty = true;
            if (sb.iconImage) sb.iconImage.sprite = null;
            if (sb.costText) sb.costText.text = "-";
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

    /// <summary>
    /// [신규] 카드 버튼 클릭 시 로직
    /// "이미 선택중인 카드가 있으면 무시 / 없으면 선택 대기"
    /// </summary>
    private void OnClickCardButton(int clickedIndex)
    {
        // ▼▼ [수정] 이미 선택된 카드와 같은 카드를 클릭하면 유지, 다른 카드면 변경 ▼▼
        if (hasPendingCard && pendingCardIndex != clickedIndex)
        {
            Debug.Log($"[CharacterSelectUI] 기존 선택({pendingCardIndex}) -> 새로운 선택({clickedIndex})으로 변경");
        }

        hasPendingCard = true;
        pendingCardIndex = clickedIndex;

        // PlacementManager에 "이 캐릭터 인덱스 선택" 알림
        if (placementManager != null)
        {
            placementManager.OnClickSelectUnit(clickedIndex);
            
            // ▼▼ [추가] 자동 배치 호출 (placed/placable 꽉 차면 walkable로) ▼▼
            placementManager.OnClickAutoPlace();
        }
        Debug.Log($"[CharacterSelectUI] 카드({clickedIndex}) 클릭 -> 자동 배치 시도");
    }

    /// <summary>
    /// (드래그 소환) 드롭 성공 시 OnDragUseCard(usedIndex) 호출
    /// </summary>
    public void OnDragUseCard(int usedIndex)
    {
        for (int i = 0; i < selectButtons.Length; i++)
        {
            if (selectButtons[i].characterIndex == usedIndex)
            {
                OnUseCard(selectButtons[i]);
                break;
            }
        }
    }

    /// <summary>
    /// [신규] "클릭으로 선택된 카드"가 실제로 배치 성공했을 때(PlacementManager) 호출되는 메서드.
    /// </summary>
    public void MarkCardAsUsed(int usedIndex)
    {
        if (!hasPendingCard || pendingCardIndex != usedIndex)
        {
            Debug.LogWarning($"[CharacterSelectUI] MarkCardAsUsed({usedIndex})가 호출됐지만 대기중인 카드와 불일치 -> 무시");
            return;
        }

        // 실제로 OnUseCard() 실행
        for (int i = 0; i < selectButtons.Length; i++)
        {
            if (selectButtons[i].characterIndex == usedIndex)
            {
                OnUseCard(selectButtons[i]);
                break;
            }
        }

        // ▼▼ [수정] 카드 사용 후에도 선택 상태 유지 (연속 배치 가능) ▼▼
        // hasPendingCard = false;
        // pendingCardIndex = -1;
        Debug.Log($"[CharacterSelectUI] 카드({usedIndex}) 사용됨. 선택 상태는 유지됩니다.");
    }

    /// <summary>
    /// 카드 사용(이미 소환됨) → 다음 카드 교체 로직
    /// </summary>
    private void OnUseCard(SelectButton usedButton)
    {
        int usedIndex = usedButton.characterIndex;

        // 1) 사용된 버튼에 nextIndex 교체
        usedButton.characterIndex = nextIndex;
        usedButton.isEmpty = false;
        UpdateOneButtonUI(usedButton);

        // 2) reserve(4) + used(1) = 5
        List<int> five = new List<int>(reserveIndices);
        five.Add(usedIndex);

        // 3) 셔플
        ShuffleList(five);
        nextIndex = five[0];
        reserveIndices.Clear();
        for (int i = 1; i < 5; i++)
        {
            reserveIndices.Add(five[i]);
        }

        // 다음 카드 UI 갱신
        UpdateNextUnitUI();
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            T tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
    }

    public void RefreshInventoryUI()
    {
        if (characterInventory == null)
        {
            // 자동으로 CharacterInventoryManager 찾기 시도
            characterInventory = FindFirstObjectByType<CharacterInventoryManager>();
            
            if (characterInventory == null)
            {
                Debug.LogWarning("[CharacterSelectUI] CharacterInventoryManager를 찾을 수 없습니다!");
                return;
            }
            else
            {
                Debug.Log("[CharacterSelectUI] CharacterInventoryManager를 자동으로 찾았습니다.");
            }
        }

        // 현재는 구현 생략 (DeckPanelManager 등에서 구현)
        // 필요 시 DeckPanelManager 같은 곳에서 sharedSlotData200을 갱신하여 사용
    }

    /// <summary>
    /// (요청) placable/placeTile이 전부 찼을 경우 => walkable로 소환
    /// 4장의 카드(Hand)에 대해 지역1의 placable/placeTile을 찾아 랜덤으로 소환.
    /// 만약 placable/placeTile이 전혀 없으면, walkable로 소환하도록 수정.
    /// </summary>
    public void OnClickUse4CardsAtRandom()
    {
        // 지역1의 모든 Tile 스캔
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        List<Tile> validTiles = new List<Tile>();

        // placable / placeTile 중 isRegion2 == false (지역1)
        foreach (Tile t in allTiles)
        {
            if (t != null && !t.isRegion2)
            {
                if ((t.IsPlacable() || t.IsPlaceTile()))
                {
                    validTiles.Add(t);
                }
            }
        }

        // [MODIFIED for #2] => 만약 placable/placeTile이 하나도 없으면 walkable로 대체
        if (validTiles.Count == 0)
        {
            Debug.LogWarning("[CharacterSelectUI] 지역1에 배치할 수 있는 placable/placeTile이 없습니다! => walkable로 대체");
            List<Tile> walkableTiles = new List<Tile>();
            foreach (Tile t in allTiles)
            {
                if (t != null && !t.isRegion2 && t.IsWalkable())
                {
                    walkableTiles.Add(t);
                }
            }
            if (walkableTiles.Count == 0)
            {
                Debug.LogWarning("[CharacterSelectUI] 지역1에 배치할 수 있는 walkable도 없습니다! -> 소환 포기");
                return;
            }
            validTiles = walkableTiles;
        }

        Debug.Log($"[CharacterSelectUI] OnClickUse4CardsAtRandom() -> 지역1의 배치가능 타일 {validTiles.Count}개 중 랜덤으로 4장 소환 시도.");

        for (int i = 0; i < 4; i++)
        {
            SelectButton sb = selectButtons[i];
            if (sb == null || sb.isEmpty)
            {
                Debug.Log($"[CharacterSelectUI] {i}번째 selectButton이 비어있음 -> 스킵");
                continue;
            }

            // 타일 무작위 선택
            Tile randomTile = validTiles[Random.Range(0, validTiles.Count)];

            if (placementManager == null)
            {
                Debug.LogWarning("[CharacterSelectUI] placementManager가 null -> 소환 불가");
                return;
            }

            // 소환 시도
            bool success = placementManager.SummonCharacterOnTile(sb.characterIndex, randomTile, false);
            if (success)
            {
                // 성공하면 카드 사용 처리
                OnUseCard(sb);
            }
            else
            {
                Debug.Log($"[CharacterSelectUI] 카드({sb.characterIndex}) 소환 실패 -> 남은 카드들은 중단");
                break;
            }
        }
    }

    /// <summary>
    /// 테스트 용: 특정 캐릭터를 덱에 직접 추가
    /// </summary>
    public void AddCharacterToDeck(int index, CharacterData data)
    {
        if (index < 0 || index >= 9) return;
        deckFromLobby[index] = data;
    }
}
