using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

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
        if (hasPendingCard)
        {
            Debug.Log($"[CharacterSelectUI] 이미 {pendingCardIndex}번 카드를 선택 중이므로 클릭 무시");
            return;
        }

        hasPendingCard = true;
        pendingCardIndex = clickedIndex;

        // PlacementManager에 "이 캐릭터 인덱스 선택" 알림
        if (placementManager != null)
        {
            placementManager.OnClickSelectUnit(clickedIndex);
        }
        Debug.Log($"[CharacterSelectUI] 카드({clickedIndex}) 클릭 -> 배치 대기상태");
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

        hasPendingCard = false;
        pendingCardIndex = -1;
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

    // =======================================================================
    // (기존) 코드들은 생략 없이 그대로...
    // =======================================================================

    /// <summary>
    /// 원본 코드의 끝부분: 드래그 소환 후 남아있던 메서드들...
    /// </summary>

    // =======================================================================
    // == (추가) 4장 카드를 "원버튼"으로 자동 사용 + 랜덤 타일 배치 ==
    // =======================================================================
    public void OnClickUse4CardsAtRandom()
    {
        // 1) placable 또는 placeTile 중에서 "지역1(=isRegion2 == false)" 만 모아둠
        Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
        List<Tile> validTiles = new List<Tile>();
        foreach (Tile t in allTiles)
        {
            if (t != null && !t.isRegion2)
            {
                // "지역1"이고, placable 또는 placeTile이면
                if ((t.IsPlacable() || t.IsPlaceTile()))
                {
                    validTiles.Add(t);
                }
            }
        }

        if (validTiles.Count == 0)
        {
            Debug.LogWarning("[CharacterSelectUI] 지역1에 배치할 수 있는 placable/placeTile이 없습니다!");
            return;
        }

        Debug.Log($"[CharacterSelectUI] OnClickUse4CardsAtRandom() -> 지역1의 배치가능 타일 {validTiles.Count}개 중 랜덤으로 4장 소환 시도.");

        // 2) hand 4장에 대해 차례로 시도
        for (int i = 0; i < 4; i++)
        {
            SelectButton sb = selectButtons[i];
            if (sb == null || sb.isEmpty)
            {
                Debug.Log($"[CharacterSelectUI] {i}번째 selectButton이 비어있음 -> 스킵");
                continue;
            }

            // 랜덤 타일
            Tile randomTile = validTiles[Random.Range(0, validTiles.Count)];

            if (placementManager == null)
            {
                Debug.LogWarning("[CharacterSelectUI] placementManager가 null -> 소환 불가");
                return;
            }

            // 3) 실제 배치 시도 => 소환 성공하면 OnUseCard(...)로 카드 소모
            bool success = placementManager.SummonCharacterOnTile(sb.characterIndex, randomTile, false);
            if (success)
            {
                // 소환에 성공했으므로 카드 사용 처리
                OnUseCard(sb);
            }
            else
            {
                Debug.Log($"[CharacterSelectUI] 카드({sb.characterIndex}) 소환 실패 -> 남은 카드들은 중단");
                break;
            }
        }
    }
}
