// Assets\Scripts\GameSceneManager.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임씬에서 9개의 캐릭터 슬롯에
/// 로비씬에서 등록한 캐릭터 정보(이름, 아이콘 등)를 표시하는 예시.
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    [Header("게임씬의 9개 캐릭터 슬롯(이미지/텍스트)")]
    [SerializeField] private Image[] slotImages9;          // 9칸 아이콘
    [SerializeField] private TextMeshProUGUI[] slotTexts9; // 9칸 텍스트(레벨, 이름 등)

    private CharacterData[] deckFromLobby = new CharacterData[9];

    private void Start()
    {
        // 1) GameManager로부터 9개 캐릭터 가져오기
        var gm = GameManager.Instance;
        deckFromLobby = gm.GetDeckForGame();

        // 2) UI 슬롯 9칸에 표시
        if (slotImages9 != null && slotTexts9 != null)
        {
            for (int i = 0; i < 9; i++)
            {
                CharacterData c = deckFromLobby[i];
                if (slotImages9.Length > i && slotTexts9.Length > i)
                {
                    if (c != null)
                    {
                        // 아이콘
                        slotImages9[i].gameObject.SetActive(true);
                        slotImages9[i].sprite = (c.buttonIcon != null)
                                                ? c.buttonIcon.sprite
                                                : null;

                        // 텍스트
                        slotTexts9[i].gameObject.SetActive(true);
                        slotTexts9[i].text = $"{c.characterName}\nLv.{c.level}";
                    }
                    else
                    {
                        // null이면 빈칸 처리
                        slotImages9[i].sprite = null;
                        slotImages9[i].gameObject.SetActive(false);

                        slotTexts9[i].text = "";
                        slotTexts9[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
