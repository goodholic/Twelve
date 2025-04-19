// Assets\Scripts\CharacterSelectUI.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// (수정된 버전)
/// - 총 10개 캐릭터 중 인덱스 0~8만 사용(9개)
/// - 4개는 소환 버튼(Hand)에 표시
/// - 1개는 '다음에 나올 유닛'(NextUnit)
/// - 나머지 4개는 reserve(대기)
/// 
/// 유저가 4개 중 하나를 소환하면:
///   1) 그 버튼이 nextUnit으로 교체
///   2) 'reserve(4개) + 방금 사용(1개)' = 5개를 섞어서 새 nextUnit 선택(첫 번째)
///   3) 나머지 4개가 새 reserve
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    [Header("References")]
    public CharacterDatabase characterDatabase;      // 캐릭터 DB (최소 10개, 마지막(인덱스9)은 주인공)
    public PlacementManager placementManager;

    [System.Serializable]
    public class SelectButton
    {
        public Button button;             // 실제 클릭 버튼
        public Image iconImage;           // 캐릭터 아이콘
        public TextMeshProUGUI costText;  // 코스트 표시
        public int characterIndex;        // CharacterDB에서 어떤 인덱스를 쓰는지
        public bool isEmpty;              // 지금 버튼이 비어있는 슬롯인지 여부
    }

    [Header("Select Buttons (4개)")]
    public SelectButton[] selectButtons = new SelectButton[4];

    [Header("Next Unit (다음에 나올 유닛 표시용 이미지)")]
    public Image nextUnitImage;
    public TextMeshProUGUI nextUnitCost;

    // ===========================
    // 내부 로직용 상태
    // ===========================
    // "주인공"을 제외하고 0..8(9개)만 사용
    private List<int> allIndices = new List<int>();  // 0..8 (총 9개)

    // 현재 손(버튼 4개)에 배치된 캐릭터 인덱스
    private int[] handIndices = new int[4];

    // 다음에 나올 캐릭터 인덱스
    private int nextIndex = -1;

    // 대기(reserve) 4개
    private List<int> reserveIndices = new List<int>(4);

    private void Start()
    {
        // 1) DB 검사
        if (characterDatabase == null || characterDatabase.characters == null)
        {
            Debug.LogError("CharacterSelectUI: characterDatabase가 없거나 characters 배열이 비어있음.");
            return;
        }
        // 캐릭터가 최소 10개 이상은 있어야 (인덱스0~8 = 9마리 + 10번째=주인공) 
        if (characterDatabase.characters.Length < 10)
        {
            Debug.LogError("CharacterSelectUI: DB에 최소 10개 캐릭터가 필요합니다. (마지막은 주인공)");
            return;
        }

        if (placementManager == null)
        {
            Debug.LogError("CharacterSelectUI: placementManager가 null입니다.");
            return;
        }

        // 2) allIndices = [0..8] (9개)
        allIndices.Clear();
        for (int i = 0; i < 9; i++)  // 9개만 사용
        {
            allIndices.Add(i);
        }
        ShuffleList(allIndices);

        // 3) 처음 4개 -> Hand
        for (int i = 0; i < 4; i++)
        {
            handIndices[i] = allIndices[i];
        }

        // 4) 5번째 -> nextIndex
        nextIndex = allIndices[4];

        // 5) 6~9번째(인덱스 5..8) -> reserveIndices (4개)
        reserveIndices.Clear();
        for (int i = 5; i < 9; i++)
        {
            reserveIndices.Add(allIndices[i]);
        }

        // UI 갱신
        UpdateHandButtons();
        UpdateNextUnitUI();
    }

    /// <summary>
    /// 4개 버튼에 handIndices를 적용하여 아이콘, 코스트 등을 세팅
    /// </summary>
    private void UpdateHandButtons()
    {
        for (int i = 0; i < selectButtons.Length; i++)
        {
            SelectButton sb = selectButtons[i];
            sb.characterIndex = handIndices[i];
            sb.isEmpty = false;

            UpdateOneButtonUI(sb);
        }
    }

    /// <summary>
    /// nextIndex를 nextUnitImage 등에 표시
    /// </summary>
    private void UpdateNextUnitUI()
    {
        // nextIndex 유효성
        if (nextIndex < 0 || nextIndex >= characterDatabase.characters.Length)
        {
            if (nextUnitImage != null) nextUnitImage.sprite = null;
            if (nextUnitCost != null) nextUnitCost.text = "-";
            return;
        }

        CharacterData data = characterDatabase.characters[nextIndex];
        if (data == null)
        {
            if (nextUnitImage != null) nextUnitImage.sprite = null;
            if (nextUnitCost != null) nextUnitCost.text = "-";
            return;
        }

        // 아이콘
        if (nextUnitImage != null && data.buttonIcon != null)
        {
            nextUnitImage.sprite = data.buttonIcon.sprite;
        }
        // 코스트
        if (nextUnitCost != null)
        {
            nextUnitCost.text = data.cost.ToString();
        }
    }

    /// <summary>
    /// 특정 버튼(SelectButton sb)의 아이콘/코스트/UI 세팅
    /// </summary>
    private void UpdateOneButtonUI(SelectButton sb)
    {
        if (sb.button == null) return;

        sb.button.onClick.RemoveAllListeners();

        // 인덱스 유효성
        if (sb.characterIndex < 0 || sb.characterIndex >= characterDatabase.characters.Length)
        {
            // 비어있음
            sb.isEmpty = true;
            if (sb.iconImage != null) sb.iconImage.sprite = null;
            if (sb.costText != null)  sb.costText.text = "-";
            return;
        }

        sb.isEmpty = false;
        CharacterData data = characterDatabase.characters[sb.characterIndex];
        if (data != null)
        {
            // 아이콘
            if (sb.iconImage != null && data.buttonIcon != null)
                sb.iconImage.sprite = data.buttonIcon.sprite;

            // 코스트
            if (sb.costText != null)
                sb.costText.text = data.cost.ToString();

            // 클릭 리스너: 소환 선택 시 이 버튼을 사용 처리
            int indexCopy = sb.characterIndex;
            sb.button.onClick.AddListener(() =>
            {
                // 1) PlacementManager에 알림 (소환할 캐릭터 인덱스)
                placementManager.OnClickSelectUnit(indexCopy);

                // 2) 카드 사용 처리
                OnUseCard(sb);
            });
        }
        else
        {
            // null 캐릭터
            sb.isEmpty = true;
            if (sb.iconImage != null) sb.iconImage.sprite = null;
            if (sb.costText != null)  sb.costText.text = "-";
        }
    }

    /// <summary>
    /// 유저가 4개 중 하나를 소환(사용)했을 때 로테이션 처리
    /// </summary>
    private void OnUseCard(SelectButton usedButton)
    {
        // 1) 사용된 캐릭터 인덱스
        int usedIndex = usedButton.characterIndex;

        // 2) 그 버튼 칸을 nextIndex로 교체
        usedButton.characterIndex = nextIndex;
        usedButton.isEmpty = false;
        UpdateOneButtonUI(usedButton);

        // 3) reserve(4개) + 방금 사용(1개) => 총 5개
        List<int> fiveList = new List<int>(reserveIndices);
        fiveList.Add(usedIndex);

        // 4) 섞어서 첫 번째를 새 nextIndex
        ShuffleList(fiveList);
        nextIndex = fiveList[0];

        // 5) 나머지 4개 => reserve
        reserveIndices.Clear();
        for (int i = 1; i < 5; i++)
        {
            reserveIndices.Add(fiveList[i]);
        }

        // nextUnit UI 갱신
        UpdateNextUnitUI();
    }

    /// <summary>
    /// 리스트 랜덤 셔플
    /// </summary>
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
