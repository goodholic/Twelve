using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 예: 4개의 버튼 (또는 그 이상) 각각 'characterIndex'를 갖고 있고,
/// 클릭 시 PlacementManager.OnClickSelectUnit(...) 호출 -> currentCharacterIndex 변경.
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    [Header("References")]
    public CharacterDatabase characterDatabase;
    public PlacementManager placementManager;

    [System.Serializable]
    public class SelectButton
    {
        public Button button;              // 실제 클릭 버튼
        public Image iconImage;            // 캐릭터 아이콘
        public TextMeshProUGUI costText;   // 코스트 표시
        public int characterIndex;         // 0,1,2,3... 어떤 캐릭터 인덱스인지
    }

    [Header("Select Buttons")]
    public SelectButton[] selectButtons;

    private void Start()
    {
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (characterDatabase == null || placementManager == null)
        {
            Debug.LogWarning("CharacterSelectUI: Database나 PlacementManager가 null입니다.");
            return;
        }

        // 각 버튼에 대해 UI 표시 및 onClick 설정
        for (int i = 0; i < selectButtons.Length; i++)
        {
            SelectButton sb = selectButtons[i];
            if (sb.button == null)
                continue;

            // 캐릭터 인덱스 유효 범위 확인
            if (sb.characterIndex >= 0 && sb.characterIndex < characterDatabase.characters.Length)
            {
                CharacterData data = characterDatabase.characters[sb.characterIndex];
                if (data != null)
                {
                    // 버튼 아이콘
                    if (sb.iconImage != null && data.buttonIcon != null)
                    {
                        sb.iconImage.sprite = data.buttonIcon;
                    }

                    // 코스트 표시
                    if (sb.costText != null)
                    {
                        sb.costText.text = data.cost.ToString();
                    }
                }
            }
            else
            {
                // 범위를 벗어나면 "-" 등으로 표시
                if (sb.costText != null)
                    sb.costText.text = "-";
            }

            // 버튼 클릭 시 -> placementManager.OnClickSelectUnit(sb.characterIndex)
            sb.button.onClick.RemoveAllListeners();
            sb.button.onClick.AddListener(() =>
            {
                placementManager.OnClickSelectUnit(sb.characterIndex);
            });
        }
    }
}
