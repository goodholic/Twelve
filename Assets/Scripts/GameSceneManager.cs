using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    [Header("게임씬의 9개 캐릭터 슬롯(이미지/텍스트)")]
    [SerializeField] private Image[] slotImages9;          // 9칸 아이콘
    [SerializeField] private TextMeshProUGUI[] slotTexts9; // 9칸 텍스트(레벨, 이름 등)

    // *** 기존 추가: 나가기 버튼 (로비씬으로 이동)
    [Header("게임씬 UI: 나가기 버튼")]
    [SerializeField] private Button exitButton;

    // *** 기존 추가: 게임 진행 시간을 표시할 TMP 텍스트
    [Header("게임 진행 시간 텍스트")]
    [SerializeField] private TextMeshProUGUI gameTimeText;

    // ============================================
    // (추가) Hero Panel 레퍼런스 (주인공 전용 패널)
    // ============================================
    [Header("Hero Panel (주인공 캐릭터 전용)")]
    [SerializeField] private GameObject heroPanel;

    // *** 추가됨: 아이템 인벤토리 패널(항상 켜고 싶음)
    [Header("아이템 인벤토리 패널 (게임씬)")]
    [SerializeField] private GameObject itemInventoryPanel;

    // 내부에서 덱을 저장할 배열 (9칸)
    private CharacterData[] deckFromLobby = new CharacterData[9];

    // (추가) 주인공 캐릭터 데이터
    private CharacterData heroCharacter = null;

    // *** 기존 추가: 게임 시간(초) 누적
    private float elapsedTime = 0f;

    // ----------------------------------------------------
    // (추가) OnEnable 시점에도 무조건 아이템 패널 켜기
    // ----------------------------------------------------
    private void OnEnable()
    {
        if (itemInventoryPanel != null)
        {
            itemInventoryPanel.SetActive(true);
        }
    }

    private void Start()
    {
        // 1) GameManager로부터 9개 캐릭터 + 주인공 캐릭터 가져오기
        var gm = GameManager.Instance;
        deckFromLobby = gm.GetDeckForGame();
        heroCharacter = gm.GetHeroCharacter();

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

        // *** 추가: "주인공"(10번째 덱) 캐릭터가 있으면 자동으로 소환(배치)하고
        //           Hero Panel의 자식 오브젝트로 들어가도록 처리
        if (heroCharacter != null && heroCharacter.spawnPrefab != null)
        {
            // 주인공 프리팹 인스턴스 생성
            GameObject heroObj = Instantiate(heroCharacter.spawnPrefab);

            // Hero Panel 내부에 두기
            if (heroPanel != null)
            {
                heroObj.transform.SetParent(heroPanel.transform, false);

                // 만약 Hero Panel이 UI 패널(=RectTransform)이라면 앵커 좌표로 배치
                RectTransform heroRect = heroObj.GetComponent<RectTransform>();
                if (heroRect != null)
                {
                    // 임의 위치(0,0) 정도로 놓고, 로컬 회전 초기화
                    heroRect.anchoredPosition = Vector2.zero;
                    heroRect.localRotation = Quaternion.identity;
                }
                else
                {
                    // 3D 오브젝트일 경우 월드 좌표로 (0,0,0)에 배치
                    heroObj.transform.localPosition = Vector3.zero;
                    heroObj.transform.localRotation = Quaternion.identity;
                }
            }
            else
            {
                // Hero Panel이 없으면 그냥 월드 좌표(0,0,0)에 놓기
                heroObj.transform.position = new Vector3(0f, 0f, 0f);
                heroObj.transform.localRotation = Quaternion.identity;
                Debug.LogWarning("[GameSceneManager] HeroPanel이 null이라 주인공을 월드에 배치했습니다!");
            }

            // 자동 이동 스크립트(HeroAutoMover) 추가
            heroObj.AddComponent<HeroAutoMover>();

            // Character 컴포넌트가 있다면, 주인공 캐릭터 능력치 설정
            Character heroComp = heroObj.GetComponent<Character>();
            if (heroComp != null)
            {
                heroComp.attackPower = heroCharacter.attackPower;
                // rangeType에 따라 공격 사거리 설정
                switch (heroCharacter.rangeType)
                {
                    case RangeType.Melee:
                        heroComp.attackRange = 1.2f;
                        break;
                    case RangeType.Ranged:
                        heroComp.attackRange = 2.5f;
                        break;
                    case RangeType.LongRange:
                        heroComp.attackRange = 4.0f;
                        break;
                }
            }
            Debug.Log($"[GameSceneManager] 주인공 '{heroCharacter.characterName}' 자동 소환( heroPanel 자식 ) 완료!");
        }

        // *** 기존 추가: Exit 버튼 초기화
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnClickExitGame);
        }

        // *** 기존 추가: 게임 시간 텍스트 초기화
        if (gameTimeText != null)
        {
            gameTimeText.text = "Time: 0.0s";
        }

        // *** 추가됨: 게임씬에서도 아이템 인벤토리 패널을 켜기
        if (itemInventoryPanel != null)
        {
            itemInventoryPanel.SetActive(true);
        }
    }

    private void Update()
    {
        // *** 기존 추가: 매 프레임마다 경과 시간 증가 + UI 갱신
        elapsedTime += Time.deltaTime;

        if (gameTimeText != null)
        {
            // 소수점 한두 자리 정도만 표시
            gameTimeText.text = $"Time: {elapsedTime:F1}s";
        }
    }

    // *** 기존 추가: 나가기 버튼 → 로비씬으로 이동
    private void OnClickExitGame()
    {
        Debug.Log("[GameSceneManager] 나가기 버튼 클릭 -> 로비씬으로 이동");
        SceneManager.LoadScene("LobbyScene"); // ← "LobbyScene" 이름에 맞춰 수정하세요.
    }
}
