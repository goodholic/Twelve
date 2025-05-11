// Assets\Scripts\GameSceneManager.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    [Header("게임씬의 9개 캐릭터 슬롯(이미지/텍스트)")]
    [SerializeField] private Image[] slotImages9;          
    [SerializeField] private TextMeshProUGUI[] slotTexts9; 

    [Header("게임씬 UI: 나가기 버튼")]
    [SerializeField] private Button exitButton;

    [Header("게임 진행 시간 텍스트")]
    [SerializeField] private TextMeshProUGUI gameTimeText;

    [Header("Hero Panel (주인공 캐릭터 전용)")]
    public GameObject heroPanel;  

    [Header("아이템 인벤토리 패널 (게임씬)")]
    [SerializeField] private GameObject itemInventoryPanel;

    private CharacterData[] deckFromLobby = new CharacterData[9]; 
    private CharacterData heroCharacter = null; 

    private float elapsedTime = 0f;

    private void OnEnable()
    {
        if (itemInventoryPanel != null)
        {
            itemInventoryPanel.SetActive(true);
        }
    }

    private void Start()
    {
        // 1) 1~9 캐릭터
        if (GameManager.Instance != null && 
            GameManager.Instance.currentRegisteredCharacters != null &&
            GameManager.Instance.currentRegisteredCharacters.Length >= 9)
        {
            for (int i = 0; i < 9; i++)
            {
                deckFromLobby[i] = GameManager.Instance.currentRegisteredCharacters[i];
            }
            
            // 2) 10번째 Hero (인덱스 9)
            if (GameManager.Instance.currentRegisteredCharacters.Length > 9)
            {
                heroCharacter = GameManager.Instance.currentRegisteredCharacters[9];
            }
            Debug.Log("[GameSceneManager] GameManager.currentRegisteredCharacters에서 캐릭터 9개 + Hero 로드 완료");
        }
        else
        {
            Debug.LogWarning("[GameSceneManager] GameManager.currentRegisteredCharacters가 없거나 초기화되지 않음");
        }

        // 3) 1~9 -> UI 슬롯
        if (slotImages9 != null && slotTexts9 != null)
        {
            for (int i = 0; i < 9; i++)
            {
                if (i >= deckFromLobby.Length) continue;
                CharacterData c = deckFromLobby[i];

                if (slotImages9.Length > i && slotTexts9.Length > i)
                {
                    if (c != null)
                    {
                        slotImages9[i].gameObject.SetActive(true);
                        slotImages9[i].sprite = (c.buttonIcon != null) ? c.buttonIcon.sprite : null;

                        slotTexts9[i].gameObject.SetActive(true);
                        slotTexts9[i].text = $"{c.characterName}\nLv.{c.level}";
                    }
                    else
                    {
                        slotImages9[i].gameObject.SetActive(false);
                        slotTexts9[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        // 4) Hero(인덱스 9) 자동 소환
        if (heroCharacter != null && heroCharacter.spawnPrefab != null)
        {
            // 프리팹 유효성 검사
            if (!IsPrefabValid(heroCharacter.spawnPrefab))
            {
                Debug.LogError($"[GameSceneManager] 히어로 프리팹 '{heroCharacter.spawnPrefab.name}'에 Missing Script 참조가 있습니다. 프리팹을 확인해주세요.");
            }
            else
            {
                GameObject heroObj = Instantiate(heroCharacter.spawnPrefab);
                if (heroObj == null)
                {
                    Debug.LogError("[GameSceneManager] 히어로 인스턴스화 실패");
                    return;
                }
                
                // UI 요소로 적절히 배치
                if (heroPanel != null)
                {
                    // 캔버스 계층 구조 확인
                    Canvas canvas = heroPanel.GetComponentInParent<Canvas>();
                    if (canvas == null)
                    {
                        Debug.LogWarning("[GameSceneManager] heroPanel의 부모에 Canvas가 없습니다.");
                        canvas = FindFirstObjectByType<Canvas>();
                        if (canvas != null)
                        {
                            heroPanel.transform.SetParent(canvas.transform, false);
                            Debug.Log("[GameSceneManager] heroPanel을 Canvas의 자식으로 이동했습니다.");
                        }
                    }
                
                    heroObj.transform.SetParent(heroPanel.transform, false);

                    // UI 위치 설정
                    RectTransform rt = heroObj.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = Vector2.zero;
                        rt.localRotation = Quaternion.identity;
                        rt.localScale = Vector3.one;
                    }
                    else
                    {
                        heroObj.transform.localPosition = Vector3.zero;
                        heroObj.transform.localRotation = Quaternion.identity;
                        heroObj.transform.localScale = Vector3.one;
                    }
                }
                else
                {
                    heroObj.transform.position = Vector3.zero;
                    heroObj.transform.localRotation = Quaternion.identity;
                    Debug.LogWarning("[GameSceneManager] heroPanel이 null -> (0,0,0)에 배치");
                }

                // Hero가 자동 이동/공격
                if (heroObj.GetComponent<HeroAutoMover>() == null)
                {
                    heroObj.AddComponent<HeroAutoMover>();
                }

                // Character 컴포넌트 설정
                Character heroComp = heroObj.GetComponent<Character>();
                if (heroComp != null)
                {
                    // ▼▼ (추가) 히어로로 강제 지정 + HP바 비활성화 ▼▼
                    heroComp.isHero = true;
                    if (heroComp.hpBarCanvas != null)
                    {
                        heroComp.hpBarCanvas.gameObject.SetActive(false);
                    }
                    // ▲▲ (추가 끝) ▲▲

                    heroComp.attackPower = heroCharacter.attackPower;
                    switch (heroCharacter.rangeType)
                    {
                        case RangeType.Melee:    heroComp.attackRange = 1.2f; break;
                        case RangeType.Ranged:   heroComp.attackRange = 2.5f; break;
                        case RangeType.LongRange:heroComp.attackRange = 4.0f; break;
                    }

                    // 총알 패널 연결
                    var pMan = PlacementManager.Instance;
                    if (pMan != null && pMan.bulletPanel != null)
                    {
                        heroComp.SetBulletPanel(pMan.bulletPanel);
                    }
                }

                Debug.Log("[GameSceneManager] Hero(인덱스9) 자동 소환 완료");
            }
        }

        // 나가기 버튼
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnClickExitGame);
        }

        // 게임 시간 초기
        if (gameTimeText != null)
        {
            gameTimeText.text = "Time: 0.0s";
        }

        // 아이템 인벤토리 패널
        if (itemInventoryPanel != null)
        {
            itemInventoryPanel.SetActive(true);
        }
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        if (gameTimeText != null)
        {
            gameTimeText.text = $"{elapsedTime:F1}s";
        }
    }

    private void OnClickExitGame()
    {
        Debug.Log("[GameSceneManager] 나가기 버튼 클릭 -> 로비씬");
        SceneManager.LoadScene("LobbyScene");
    }

    private bool IsPrefabValid(GameObject prefab)
    {
        if (prefab == null) return false;
        
        // 프리팹의 모든 컴포넌트 확인
        Component[] components = prefab.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                return false; // null 컴포넌트 발견 (Missing Script)
            }
        }
        
        // 자식 오브젝트의 컴포넌트도 확인
        foreach (Transform child in prefab.transform)
        {
            if (child == null) continue;
            
            Component[] childComponents = child.GetComponents<Component>();
            for (int i = 0; i < childComponents.Length; i++)
            {
                if (childComponents[i] == null)
                {
                    return false; // 자식 오브젝트에서 null 컴포넌트 발견
                }
            }
        }
        
        return true;
    }
}
