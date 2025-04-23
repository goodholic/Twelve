using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 예시:
/// - 총 10개 캐릭터 중 (0~8) = 9개, 마지막(9)은 주인공이라고 가정
/// - 4개는 소환 버튼(Hand), 1개는 nextUnit, 나머지 4개는 reserve
/// 
/// 유저가 4개 중 하나를 소환하면:
///   1) 그 버튼칸을 nextUnit으로 교체
///   2) 'reserve(4개) + 사용된(1개)' = 5개를 섞어 첫 번째를 새로운 nextUnit, 나머지 4개가 새로운 reserve
/// 
/// + (추가) 각 버튼에 드래그 스크립트(DraggableSummonButtonUI)도 붙여서
///    타일로 드롭 시에 PlacementManager.SummonCharacterOnTile(...) 호출
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    [Header("References")]
    public CharacterDatabase characterDatabase; // 캐릭터 DB
    public PlacementManager placementManager;   // 배치 매니저

    [System.Serializable]
    public class SelectButton
    {
        public Button button;            
        public Image iconImage;          
        public TextMeshProUGUI costText;
        public int characterIndex;       
        public bool isEmpty;             
    }

    [Header("Select Buttons (4개)")]
    public SelectButton[] selectButtons = new SelectButton[4];

    [Header("Next Unit (다음 유닛) 표시")]
    public Image nextUnitImage;
    public TextMeshProUGUI nextUnitCost;

    // 내부에서 사용할 상태
    private List<int> allIndices = new List<int>(); // 0..8 (총 9개)
    private int[] handIndices = new int[4];         
    private int nextIndex = -1;                    
    private List<int> reserveIndices = new List<int>(4);

    private void Start()
    {
        // 0) DB 유효성 체크
        if (characterDatabase == null || characterDatabase.characters == null)
        {
            Debug.LogError("[CharacterSelectUI] characterDatabase가 설정 안 됨");
            return;
        }
        if (characterDatabase.characters.Length < 10)
        {
            Debug.LogError("[CharacterSelectUI] 최소 10개 캐릭터 필요(마지막은 주인공)");
            return;
        }
        if (placementManager == null)
        {
            Debug.LogError("[CharacterSelectUI] placementManager가 null");
            return;
        }

        // 1) allIndices = 0..8 (9개)
        allIndices.Clear();
        for (int i = 0; i < 9; i++)
        {
            allIndices.Add(i);
        }
        ShuffleList(allIndices);

        // 2) 처음 4개 -> hand
        for (int i = 0; i < 4; i++)
        {
            handIndices[i] = allIndices[i];
        }

        // 3) 5번째 -> nextIndex
        nextIndex = allIndices[4];

        // 4) 6..9번째(인덱스5..8) -> reserve
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
        if (nextIndex < 0 || nextIndex >= characterDatabase.characters.Length)
        {
            if (nextUnitImage) nextUnitImage.sprite = null;
            if (nextUnitCost) nextUnitCost.text = "-";
            return;
        }

        var data = characterDatabase.characters[nextIndex];
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
        // 버튼이 null이면 무시
        if (sb.button == null) return;

        sb.button.onClick.RemoveAllListeners();

        if (sb.characterIndex < 0 || sb.characterIndex >= characterDatabase.characters.Length)
        {
            sb.isEmpty = true;
            if (sb.iconImage) sb.iconImage.sprite = null;
            if (sb.costText) sb.costText.text = "-";
            return;
        }

        var data = characterDatabase.characters[sb.characterIndex];
        if (data != null)
        {
            sb.isEmpty = false;

            // 아이콘
            if (sb.iconImage && data.buttonIcon != null)
                sb.iconImage.sprite = data.buttonIcon.sprite;

            // 코스트
            if (sb.costText)
                sb.costText.text = data.cost.ToString();

            // (1) 클릭 -> 소환(PlacementManager에 currentCharacterIndex 설정 후 배치)
            int indexCopy = sb.characterIndex;
            sb.button.onClick.AddListener(() =>
            {
                placementManager.OnClickSelectUnit(indexCopy);
                OnUseCard(sb);
            });
        }
        else
        {
            sb.isEmpty = true;
            if (sb.iconImage) sb.iconImage.sprite = null;
            if (sb.costText) sb.costText.text = "-";
        }

        // (2) 드래그 소환을 위한 컴포넌트 부착
        DraggableSummonButtonUI dragComp = sb.button.GetComponent<DraggableSummonButtonUI>();
        if (dragComp == null)
        {
            dragComp = sb.button.gameObject.AddComponent<DraggableSummonButtonUI>();
        }
        // 버튼 드래그 시 소환할 캐릭터 인덱스 설정
        dragComp.SetSummonData(sb.characterIndex);

        // *** 추가: 드래그 종료 후 OnDragUseCard를 호출할 수 있게,
        //     드래그 측에서 CharacterSelectUI를 참조 가능하도록 세팅.
        dragComp.parentSelectUI = this;
    }

    /// <summary>
    /// 4개 중 하나를 "사용"(소환)하면 nextUnit과 자리 교체 후 reserve 재구성
    /// </summary>
    public void OnUseCard(SelectButton usedButton)
    {
        int usedIndex = usedButton.characterIndex; // 기존 인덱스
        // 1) 버튼 칸을 nextIndex로 교체
        usedButton.characterIndex = nextIndex;
        usedButton.isEmpty = false;
        UpdateOneButtonUI(usedButton);

        // 2) reserve 4개 + 방금 사용된 1개 => 5개
        List<int> five = new List<int>(reserveIndices);
        five.Add(usedIndex);

        // 3) 섞어서 첫 번째를 새 nextIndex, 나머지 4개는 reserve
        ShuffleList(five);
        nextIndex = five[0];
        reserveIndices.Clear();
        for (int i = 1; i < 5; i++)
        {
            reserveIndices.Add(five[i]);
        }

        // nextUnit UI 갱신
        UpdateNextUnitUI();
    }

    /// <summary>
    /// (드래그 소환)로 카드를 사용했을 때도, 동일한 nextUnit 로직 적용
    /// </summary>
    public void OnDragUseCard(int usedIndex)
    {
        // 손패(4칸) 중 'usedIndex'와 같은 characterIndex를 가진 버튼 찾아서 OnUseCard
        for (int i = 0; i < selectButtons.Length; i++)
        {
            if (selectButtons[i].characterIndex == usedIndex)
            {
                OnUseCard(selectButtons[i]);
                break;
            }
        }
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
}