using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DrawPanelManager : MonoBehaviour
{
    [Header("CharacterInventoryManager 참조")]
    [SerializeField] private CharacterInventoryManager characterInventory;

    [Header("뽑기 결과 텍스트")]
    [SerializeField] private TextMeshProUGUI drawResultText;

    [Header("뽑기 결과 이미지 (새로 추가)")]
    [SerializeField] private Image drawResultImage;

    [Header("뽑기 버튼")]
    [SerializeField] private Button drawButton;

    // ===========================
    //  (추가) 중복 클릭 방지용
    // ===========================
    private bool isDrawing = false;

    private void Awake()
    {
        if (drawButton) drawButton.onClick.AddListener(OnClickDraw);
    }

    private void OnClickDraw()
    {
        // 이미 뽑기 진행중이면 무시(연타 방지)
        if (isDrawing)
        {
            Debug.LogWarning("[DrawPanelManager] 이미 뽑기 진행 중!");
            return;
        }

        if (!characterInventory)
        {
            Debug.LogWarning("[DrawPanelManager] characterInventory가 없음");
            return;
        }

        // 뽑기 시작
        isDrawing = true;

        // 1) 한 개만 뽑기
        CharacterData newCharacter = characterInventory.DrawRandomCharacter();

        // 2) 뽑은 결과 반영
        if (newCharacter != null)
        {
            // 뽑은 캐릭터 즉시 저장
            characterInventory.SaveCharacters();

            if (drawResultText)
            {
                drawResultText.text = $"뽑기 성공: {newCharacter.characterName}";
            }
            if (drawResultImage)
            {
                drawResultImage.sprite = (newCharacter.buttonIcon != null)
                    ? newCharacter.buttonIcon.sprite
                    : null;
            }

            // 추가 코드: 다른 패널 UI 갱신
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm)
            {
                Debug.Log("[DrawPanelManager] GameManager가 존재하므로 인벤토리 갱신 등 수행");
                // gm.RefreshInventoryDisplay() 등의 메서드가 있다면 호출 가능
            }

            UpgradePanelManager upm = FindFirstObjectByType<UpgradePanelManager>();
            if (upm)
            {
                upm.RefreshUpgradeDisplay();
            }

            DeckPanelManager dpm = FindFirstObjectByType<DeckPanelManager>();
            if (dpm)
            {
                dpm.RefreshDeckDisplay();
            }
        }
        else
        {
            if (drawResultText) drawResultText.text = "뽑기 실패(풀 비어있음)";
            if (drawResultImage) drawResultImage.sprite = null;
        }

        // 3) 뽑기 종료(버튼 다시 누를 수 있게)
        isDrawing = false;
    }
}
