using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    private void Start()
    {
        // 1) 1~9 캐릭터 (인덱스0..8) 가져오기
        // deckFromLobby = GameManager.Instance.GetNineCharacters(); // 구 버전 메서드 제거됨
        
        // 새로운 방식: GameManager의 currentRegisteredCharacters에서 직접 가져오기
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
            sb.button.onClick.AddListener(() =>
            {
                placementManager.OnClickSelectUnit(copyIndex);
                OnUseCard(sb);
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

    public void OnUseCard(SelectButton usedButton)
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

        UpdateNextUnitUI();
    }

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
