using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 8마리 캐릭터가 계속 순환되며:
/// - 4개는 소환 버튼(4개)에 표시(Hand)
/// - 1개는 '다음에 나올 유닛'(NextUnit Image)에 표시
/// - 나머지 3개는 reserve(대기)
/// 유저가 4개 중 하나를 소환하면:
///  1) 그 자리(버튼)는 NextUnit으로 교체
///  2) 방금 사용한 인덱스와 reserve(3개) -> 총4개를 섞어서 1개를 새 NextUnit으로 결정
///  3) 나머지 3개는 새 reserve로 유지
/// 이렇게 8개가 계속 로테이션되는 방식.
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    [Header("References")]
    public CharacterDatabase characterDatabase;      // 8개 캐릭터 정보가 들어있는 DB
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
    public Image nextUnitImage;           // 다음 유닛을 보여줄 UI Image
    public TextMeshProUGUI nextUnitCost;  // 다음 유닛 코스트 표시 등(선택사항)

    // ===========================
    // 내부 로직용 상태
    // ===========================
    private int[] handIndices = new int[4];   // 현재 버튼(4개)에 배치된 캐릭터 인덱스
    private int nextIndex = -1;              // 다음에 나올 캐릭터 인덱스
    private List<int> reserveIndices = new List<int>(3); // 3개 대기상태

    // 8개 캐릭터 인덱스(0~7). 계속 순환
    private List<int> allIndices = new List<int>(); // 0..7 저장

    private void Start()
    {
        if (characterDatabase == null || characterDatabase.characters == null)
        {
            Debug.LogError("CharacterSelectUI: characterDatabase가 없거나 characters 배열이 비어있음.");
            return;
        }
        if (characterDatabase.characters.Length < 8)
        {
            Debug.LogError("CharacterSelectUI: 최소 8개 캐릭터가 필요합니다.");
            return;
        }
        if (placementManager == null)
        {
            Debug.LogError("CharacterSelectUI: placementManager가 null입니다.");
            return;
        }

        // 1) 8개 인덱스를 allIndices에 넣고 섞기
        allIndices.Clear();
        for (int i = 0; i < 8; i++)
        {
            allIndices.Add(i);
        }
        ShuffleList(allIndices);

        // 2) 처음 4개 -> handIndices
        for (int i = 0; i < 4; i++)
        {
            handIndices[i] = allIndices[i];
        }

        // 3) 5번째 -> nextIndex
        nextIndex = allIndices[4];

        // 4) 6~8번째 -> reserveIndices (3개)
        reserveIndices.Clear();
        for (int i = 5; i < 8; i++)
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
            sb.isEmpty = false; // 항상 손 4칸은 비어있지 않음

            // UI 반영
            UpdateOneButtonUI(sb);
        }
    }

    /// <summary>
    /// nextIndex를 nextUnitImage 등에 표시
    /// </summary>
    private void UpdateNextUnitUI()
    {
        if (nextIndex < 0 || nextIndex >= characterDatabase.characters.Length)
        {
            // 없는 경우
            if (nextUnitImage != null) nextUnitImage.sprite = null;
            if (nextUnitCost != null) nextUnitCost.text = "-";
            return;
        }

        CharacterData data = characterDatabase.characters[nextIndex];
        if (data == null) return;

        // 이미지
        if (nextUnitImage != null && data.buttonIcon != null)
        {
            // buttonIcon이 Image이므로 sprite 속성을 가져옴
            nextUnitImage.sprite = data.buttonIcon.sprite;
        }
        // 코스트
        if (nextUnitCost != null)
        {
            nextUnitCost.text = data.cost.ToString();
        }
    }

    /// <summary>
    /// 특정 버튼(SelectButton sb)에 대해 아이콘/코스트 등을 세팅
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
            if (sb.costText != null) sb.costText.text = "-";
            return;
        }

        // 유효한 캐릭터
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

            // 클릭 리스너
            int indexCopy = sb.characterIndex;
            sb.button.onClick.AddListener(() =>
            {
                // 캐릭터 소환 선택
                placementManager.OnClickSelectUnit(indexCopy);

                // 이 버튼을 사용했다고 처리
                OnUseCard(sb);
            });
        }
        else
        {
            // 혹시 null이면 비움
            sb.isEmpty = true;
            if (sb.iconImage != null) sb.iconImage.sprite = null;
            if (sb.costText != null) sb.costText.text = "-";
        }
    }

    /// <summary>
    /// 유저가 어떤 버튼(카드)를 사용(소환)했을 때
    /// -> 그 버튼은 nextIndex로 교체
    /// -> 기존 버튼 캐릭터 인덱스는 reserve에 합류 + 기존 reserve와 합쳐 총 4개 -> 섞어서 1개를 nextIndex로 선택
    /// -> 나머지 3개는 다시 reserve
    /// </summary>
    private void OnUseCard(SelectButton usedButton)
    {
        // 1) 손에서 사용된 캐릭터 인덱스
        int usedIndex = usedButton.characterIndex;

        // 2) 그 버튼을 nextIndex로 교체
        usedButton.characterIndex = nextIndex;
        usedButton.isEmpty = false; // 어차피 nextIndex는 유효
        UpdateOneButtonUI(usedButton);

        // 3) (reserve 3개 + usedIndex) 총 4개
        List<int> fourList = new List<int>(reserveIndices);
        fourList.Add(usedIndex);

        // 4) 섞기
        ShuffleList(fourList);

        // 5) 첫 번째를 새 nextIndex로
        nextIndex = fourList[0];

        // 6) 나머지 3개는 새 reserve
        reserveIndices.Clear();
        for (int i = 1; i < 4; i++)
        {
            reserveIndices.Add(fourList[i]);
        }

        // UI 갱신
        UpdateNextUnitUI();
    }

    /// <summary>
    /// 리스트를 랜덤 셔플
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
