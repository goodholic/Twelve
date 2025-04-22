using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DrawPanelManager : MonoBehaviour
{
    [Header("CharacterInventoryManager 참조")]
    [SerializeField] private CharacterInventoryManager characterInventory;

    [Header("뽑기 결과 텍스트")]
    [SerializeField] private TextMeshProUGUI drawResultText;

    [Header("뽑기 결과 이미지")]
    [SerializeField] private Image drawResultImage;

    [Header("뽑기 버튼")]
    [SerializeField] private Button drawButton;

    // 중복 클릭 방지용
    private bool isDrawing = false;

    private void Awake()
    {
        if (drawButton)
            drawButton.onClick.AddListener(OnClickDraw);
    }

    private void OnClickDraw()
    {
        // 이미 뽑기 진행중이면 무시
        if (isDrawing)
        {
            Debug.LogWarning("[DrawPanelManager] 이미 뽑기 진행 중!");
            return;
        }
        if (characterInventory == null)
        {
            Debug.LogWarning("[DrawPanelManager] characterInventory가 없음");
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
                    ? newChar.buttonIcon.sprite
                    : null;
            }

            // 인벤토리 UI 갱신(20칸)
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
}
