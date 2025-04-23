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
    // (추가) Hero Panel 레퍼런스 (주인공 전용)
    // ============================================
    [Header("Hero Panel (주인공 캐릭터 전용)")]
    public GameObject heroPanel;  // ← private → public

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
        heroCharacter = gm.GetHeroCharacter();  // ← 여기서 "10번째 캐릭터"가 넘어옴

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

        // *** 추가: HeroPanel을 1080×1920 중앙에 위치시킴 (UI 기준)
        if (heroPanel != null)
        {
            RectTransform heroPanelRect = heroPanel.GetComponent<RectTransform>();
            if (heroPanelRect != null)
            {
                // 해상도 1080×1920 기준
                heroPanelRect.sizeDelta = new Vector2(1080f, 1920f);

                // 앵커/피벗을 중앙(0.5,0.5)로 설정하면 (0,0)이 화면 중앙
                heroPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
                heroPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
                heroPanelRect.pivot    = new Vector2(0.5f, 0.5f);

                // 앵커 기준으로 중앙 배치
                heroPanelRect.anchoredPosition = Vector2.zero;
            }
        }

        // 3) 주인공(10번째) 캐릭터 있으면 -> heroPanel 자식으로 생성 + HeroAutoMover
        if (heroCharacter != null && heroCharacter.spawnPrefab != null)
        {
            // 주인공 프리팹 인스턴스
            GameObject heroObj = Instantiate(heroCharacter.spawnPrefab);

            // HeroPanel의 자식으로 붙이기
            if (heroPanel != null)
            {
                heroObj.transform.SetParent(heroPanel.transform, false);

                // UI모드라면 RectTransform 위치조정
                RectTransform heroRect = heroObj.GetComponent<RectTransform>();
                if (heroRect != null)
                {
                    heroRect.anchoredPosition = Vector2.zero;
                    heroRect.localRotation    = Quaternion.identity;
                }
                else
                {
                    // 3D 오브젝트면 (0,0,0)에 배치
                    heroObj.transform.localPosition = Vector3.zero;
                    heroObj.transform.localRotation = Quaternion.identity;
                }
            }
            else
            {
                // HeroPanel이 없으면 (0,0,0)에 그냥 배치
                heroObj.transform.position      = Vector3.zero;
                heroObj.transform.localRotation = Quaternion.identity;
                Debug.LogWarning("[GameSceneManager] HeroPanel이 null이라 주인공을 월드(0,0,0)에 배치했습니다!");
            }

            // 자동 이동 스크립트(HeroAutoMover) 추가
            heroObj.AddComponent<HeroAutoMover>();

            // Character 컴포넌트가 있다면 주인공 스탯 설정
            Character heroComp = heroObj.GetComponent<Character>();
            if (heroComp != null)
            {
                heroComp.attackPower = heroCharacter.attackPower;

                // rangeType에 따른 사거리
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

                // -----------------------------------------------------------
                // (추가) Hero도 bulletPanel에서 총알이 생성되도록 설정
                // -----------------------------------------------------------
                var pMan = PlacementManager.Instance;
                if (pMan != null && pMan.bulletPanel != null)
                {
                    heroComp.SetBulletPanel(pMan.bulletPanel);
                }
            }

            Debug.Log($"[GameSceneManager] 주인공 '{heroCharacter.characterName}' 자동 소환 완료!");
        }

        // 나가기 버튼 초기화
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnClickExitGame);
        }

        // 게임 시간 텍스트 초기화
        if (gameTimeText != null)
        {
            gameTimeText.text = "Time: 0.0s";
        }

        // 아이템 인벤토리 패널 항상 활성
        if (itemInventoryPanel != null)
        {
            itemInventoryPanel.SetActive(true);
        }
    }

    private void Update()
    {
        // 매 프레임 경과 시간 계산
        elapsedTime += Time.deltaTime;

        if (gameTimeText != null)
        {
            // 소수점 한두 자리 정도만 표시
            gameTimeText.text = $"{elapsedTime:F1}s";
        }
    }

    // 나가기 버튼 → 로비씬으로 이동
    private void OnClickExitGame()
    {
        Debug.Log("[GameSceneManager] 나가기 버튼 클릭 -> 로비씬 이동");
        SceneManager.LoadScene("LobbyScene"); // 실제 로비씬 이름으로 교체하세요.
    }
}