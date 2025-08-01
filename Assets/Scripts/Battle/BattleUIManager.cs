using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using GuildMaster.UI;

namespace TwelveGame.Battle
{
    /// <summary>
    /// Twelve Game의 전투 시스템 UI 관리자
    /// 게임 상태, 캐릭터 선택, 점수 등의 UI를 담당합니다
    /// </summary>
    public class BattleUIManager : MonoBehaviour
{
    private static BattleUIManager instance;
    public static BattleUIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<BattleUIManager>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    [Header("캐릭터 UI")]
    public GameObject[] characterButtons = new GameObject[4];
    public Image[] buttonImages = new Image[4];
    public TextMeshProUGUI[] buttonTexts = new TextMeshProUGUI[4];
    
    [Header("Next Character UI")]
    public GameObject nextCharacterCard;
    public Image nextCharacterImage;
    public TextMeshProUGUI nextCharacterText;
    
    [Header("Game UI")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    
    [Header("팝업 UI")]
    public GameObject gameEndPanel;
    public TextMeshProUGUI gameEndText;
    public Button gameEndButton;

    void Start()
    {
        InitializeUI();
        UpdateUI();
    }
    
    void Update()
    {
        // 테스트용: T 키를 누르면 강제로 캐릭터 버튼 업데이트
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("🔧 T키 눌림 - 강제 캐릭터 버튼 업데이트");
            UpdateCharacterButtons();
        }
        
        // P키: 선택된 캐릭터와 게임 상태 확인
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("=== 🔍 현재 게임 상태 확인 ===");
            Debug.Log($"게임 상태: {GameManager.Instance.currentState}");
            Debug.Log($"현재 턴: {GameManager.Instance.currentTurn}");
            Debug.Log($"선택된 캐릭터: {(GameManager.Instance.selectedCharacter != null ? GameManager.Instance.selectedCharacter.characterName : "null")}");
            Debug.Log($"선택된 버튼 인덱스: {GameManager.Instance.selectedButtonIndex}");
            
            // 손패 상태 확인
            var currentHand = GameManager.Instance.GetCurrentHand();
            Debug.Log($"현재 손패: {currentHand.Count}장");
            for (int i = 0; i < currentHand.Count; i++)
            {
                Debug.Log($"  손패[{i}]: {currentHand[i].characterName}");
            }
        }
        
        // 테스트용: Y 키를 누르면 캐릭터 상태 출력
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log("📊 캐릭터 상태 출력:");
            
            // 캐릭터 풀 상태 체크
            Debug.Log($"X팀 풀: {GameManager.Instance.xTeamPool.Count}개");
            Debug.Log($"O팀 풀: {GameManager.Instance.oTeamPool.Count}개");
            
            // 손패 상태 체크
            Debug.Log($"X팀 손패: {GameManager.Instance.xTeamHand.Count}개");
            Debug.Log($"O팀 손패: {GameManager.Instance.oTeamHand.Count}개");
            
            // 버튼 이미지 상태 체크
            for (int i = 0; i < buttonImages.Length; i++)
            {
                if (buttonImages[i] != null)
                {
                    string spriteName = buttonImages[i].sprite != null ? buttonImages[i].sprite.name : "null";
                    Debug.Log($"버튼[{i}] 스프라이트: {spriteName}");
                }
                else
                {
                    Debug.Log($"버튼[{i}] null 상태");
                }
            }
        }
        
        // U키: 강제 테스트 스프라이트 설정
        if (Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log("🎨 강제 테스트 스프라이트 설정");
            ForceSetTestSprites();
        }
        
        // 테스트용: I 키를 누르면 캐릭터 데이터베이스 이미지 상태 체크
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("🔍 캐릭터 데이터베이스 이미지 상태 체크");
            CheckCharacterDatabaseImages();
        }
        
        // O키: 애니메이션 테스트 (캐릭터가 배치된 타일들)
        if (Input.GetKeyDown(KeyCode.O))
        {
            Debug.Log("🎬 애니메이션 테스트 - 배치된 모든 캐릭터 Attack 애니메이션 재생");
            
            // BoardManager의 모든 타일에서 캐릭터 애니메이터 찾기
            TwelveGame.Battle.CharacterAnimator[] animators = FindObjectsOfType<TwelveGame.Battle.CharacterAnimator>();
            Debug.Log($"📊 발견된 CharacterAnimator: {animators.Length}개");
            
            foreach (TwelveGame.Battle.CharacterAnimator animator in animators)
            {
                Debug.Log($"🎭 Attack 애니메이션 재생: {animator.name}");
                animator.PlayAttackAnimation();
            }
        }
    }

    void InitializeUI()
    {
        // 버튼 이벤트 연결
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int buttonIndex = i;
            Button btn = characterButtons[i].GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnCharacterButtonClick(buttonIndex));
            }
        }

        // Next Character 카드 활성화 (테스트용)
        if (nextCharacterCard != null)
        {
            nextCharacterCard.SetActive(true);
            Debug.Log("🔍 nextCharacterCard 활성화됨 (테스트용)");
        }
    }

    public void UpdateUI()
    {
        UpdateCharacterButtons();
        UpdateNextCharacterPreview();
        UpdateGameInfo();
    }

    public void UpdateCharacterButtons()
    {
        Debug.Log("🔄 UpdateCharacterButtons 호출됨");
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("❌ GameManager.Instance가 null입니다!");
            return;
        }
        
        List<CharacterData> currentHand = GameManager.Instance.GetCurrentHand();
        Debug.Log($"📋 현재 손패: {(currentHand != null ? currentHand.Count : 0)}장");
        
        // 📊 현재 손패 캐릭터들의 이미지 상태 체크
        if (currentHand != null && currentHand.Count > 0)
        {
            Debug.Log("=== 🎴 현재 손패 캐릭터 이미지 상태 ===");
            for (int j = 0; j < currentHand.Count; j++)
            {
                CharacterData char_check = currentHand[j];
                string iconStatus = char_check.characterIcon != null ? char_check.characterIcon.name : "null";
                string buttonIconStatus = char_check.buttonIcon != null ? char_check.buttonIcon.name : "null";
                Debug.Log($"손패[{j}] '{char_check.characterName}' | characterIcon: {iconStatus} | buttonIcon: {buttonIconStatus}");
            }
        }

        for (int i = 0; i < characterButtons.Length; i++)
        {
            if (characterButtons[i] != null)
            {
                bool hasCharacter = i < currentHand.Count;
                characterButtons[i].SetActive(hasCharacter);
                
                if (hasCharacter && buttonImages != null && i < buttonImages.Length && buttonImages[i] != null)
                {
                    CharacterData character = currentHand[i];
                    
                    // 🖼️ 캐릭터 데이터베이스 SO에 설정된 실제 이미지 우선 사용
                    if (character.characterIcon != null)
                    {
                        buttonImages[i].sprite = character.characterIcon;
                        Debug.Log($"✅ 캐릭터 '{character.characterName}' characterIcon 사용 완료! (스프라이트: {character.characterIcon.name})");
                    }
                    else if (character.buttonIcon != null)
                    {
                        buttonImages[i].sprite = character.buttonIcon;
                        Debug.Log($"✅ 캐릭터 '{character.characterName}' buttonIcon 사용 완료! (스프라이트: {character.buttonIcon.name})");
                    }
                    else
                    {
                        // ⚠️ 이미지가 없는 경우에만 기본 스프라이트 생성
                        Debug.LogWarning($"⚠️ 캐릭터 '{character.characterName}' 이미지가 null! 기본 스프라이트 생성됨");
                        buttonImages[i].sprite = CreateDefaultCharacterSprite(character.characterName);
                    }
                    
                    // 캐릭터 이름 설정
                    if (buttonTexts != null && i < buttonTexts.Length && buttonTexts[i] != null)
                    {
                        buttonTexts[i].text = character.characterName;
                    }
                }
                else if (buttonImages != null && i < buttonImages.Length && buttonImages[i] != null)
                {
                    // 캐릭터가 없는 버튼은 이미지를 null로 설정
                    buttonImages[i].sprite = null;
                }
            }
        }
    }
    
    /// <summary>
    /// 캐릭터 아이콘이 없을 때 사용할 기본 스프라이트 생성
    /// </summary>
    private Sprite CreateDefaultCharacterSprite(string characterName)
    {
        // 캐릭터 이름에 따라 다른 색상 사용
        Color color = GetCharacterColor(characterName);
        
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        
        // 원형 모양의 스프라이트 생성
        Vector2 center = new Vector2(32, 32);
        float radius = 28f;
        
        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance <= radius)
                {
                    if (distance > radius - 3)
                    {
                        pixels[x + y * 64] = Color.black; // 테두리
                    }
                    else
                    {
                        pixels[x + y * 64] = color; // 내부 색상
                    }
                }
                else
                {
                    pixels[x + y * 64] = Color.clear; // 투명
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
    }
    
    /// <summary>
    /// 캐릭터 이름에 따라 색상 결정
    /// </summary>
    private Color GetCharacterColor(string characterName)
    {
        int hash = characterName.GetHashCode();
        Random.InitState(hash);
        
        return new Color(
            Random.Range(0.3f, 0.9f),
            Random.Range(0.3f, 0.9f), 
            Random.Range(0.3f, 0.9f),
            1f
        );
    }

    public void UpdateNextCharacterPreview()
    {
        CharacterData nextCharacter = GameManager.Instance.GetNextCharacter();
        
        if (nextCharacterCard != null)
        {
            // 테스트용으로 항상 활성화
            nextCharacterCard.SetActive(true);
        }
        
        if (nextCharacterImage != null)
        {
            if (nextCharacter != null)
            {
                if (nextCharacter.characterIcon != null)
                {
                    nextCharacterImage.sprite = nextCharacter.characterIcon;
                }
                else
                {
                    nextCharacterImage.sprite = CreateDefaultCharacterSprite(nextCharacter.characterName);
                }
            }
            else
            {
                nextCharacterImage.sprite = null;
            }
        }

        if (nextCharacterText != null)
        {
            nextCharacterText.text = nextCharacter != null ? nextCharacter.characterName : "없음";
        }
    }

    void UpdateGameInfo()
    {
        if (turnText != null)
        {
            turnText.text = $"턴: {GameManager.Instance.currentTurn}";
        }

        if (scoreText != null)
        {
            scoreText.text = $"점수: X팀 0 - O팀 0";
        }

        if (timeText != null)
        {
            timeText.text = $"턴 수: {GameManager.Instance.currentTurnNumber}";
        }
    }

    void OnCharacterButtonClick(int buttonIndex)
    {
        GameManager.Instance.OnCharacterButtonClick(buttonIndex);
        
        // 버튼 하이라이트
        HighlightButton(buttonIndex);
        
        // 캐릭터 정보 표시
        if (GameManager.Instance.selectedCharacter != null)
        {
            ShowCharacterInfo(GameManager.Instance.selectedCharacter);
        }
    }

    public void HighlightButton(int buttonIndex)
    {
        Debug.Log($"🔆 버튼 하이라이트: {buttonIndex}번 버튼");
        
        // 모든 버튼 하이라이트 해제
        for (int i = 0; i < characterButtons.Length; i++)
        {
            if (characterButtons[i] != null)
            {
                Image buttonImage = characterButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = Color.white; // 기본 색상
                }
            }
        }

        // 선택된 버튼 하이라이트
        if (buttonIndex >= 0 && buttonIndex < characterButtons.Length && characterButtons[buttonIndex] != null)
        {
            Image selectedButtonImage = characterButtons[buttonIndex].GetComponent<Image>();
            if (selectedButtonImage != null)
            {
                selectedButtonImage.color = Color.yellow; // 선택된 색상
                Debug.Log($"✅ {buttonIndex}번 버튼이 노란색으로 하이라이트됨");
            }
        }
    }

    public void ShowCharacterInfo(CharacterData character)
    {
        if (character != null)
        {
            Debug.Log($"캐릭터 정보: {character.characterName} (HP: {character.hp}, 공격력: {character.attackPower})");
        }
    }

    public void ShowGameEndPanel(string message)
    {
        if (gameEndPanel != null)
        {
            gameEndPanel.SetActive(true);
        }

        if (gameEndText != null)
        {
            gameEndText.text = message;
        }
    }

    public void OnGameEndButtonClick()
    {
        // 게임 재시작 또는 메인 메뉴로 이동
        Debug.Log("게임 종료 버튼 클릭");
    }

    /// <summary>
    /// 게임 오버 화면을 표시합니다
    /// </summary>
    public void ShowGameOver(string winnerMessage)
    {
        Debug.Log($"🏆 게임 종료: {winnerMessage}");
        
        if (gameEndPanel != null)
        {
            gameEndPanel.SetActive(true);
        }

        if (gameEndText != null)
        {
            gameEndText.text = winnerMessage;
        }
    }
    
    /// <summary>
    /// 강제로 테스트 스프라이트를 모든 버튼에 설정 (디버그용)
    /// </summary>
    void ForceSetTestSprites()
    {
        if (buttonImages == null)
        {
            Debug.LogError("❌ buttonImages 배열이 null입니다!");
            return;
        }
        
        Color[] testColors = {
            Color.red, Color.blue, Color.green, Color.yellow
        };
        
        for (int i = 0; i < buttonImages.Length && i < 4; i++)
        {
            if (buttonImages[i] != null)
            {
                // 테스트용 단색 스프라이트 생성
                Texture2D texture = new Texture2D(64, 64);
                Color[] pixels = new Color[64 * 64];
                for (int p = 0; p < pixels.Length; p++)
                    pixels[p] = testColors[i];
                texture.SetPixels(pixels);
                texture.Apply();
                
                Sprite testSprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
                buttonImages[i].sprite = testSprite;
                
                Debug.Log($"✅ 버튼[{i}]에 {testColors[i]} 테스트 스프라이트 설정");
            }
            else
            {
                Debug.LogError($"❌ buttonImages[{i}]가 null입니다!");
            }
        }
    }
    
    /// <summary>
    /// Inspector에서 buttonImages 배열을 자동으로 찾아서 할당
    /// </summary>
    [ContextMenu("Auto Assign Button Images")]
    void AutoAssignButtonImages()
    {
        if (characterButtons == null || characterButtons.Length == 0)
        {
            Debug.LogError("❌ characterButtons 배열이 비어있습니다!");
            return;
        }
        
        buttonImages = new Image[characterButtons.Length];
        
        for (int i = 0; i < characterButtons.Length; i++)
        {
            if (characterButtons[i] != null)
            {
                Image img = characterButtons[i].GetComponent<Image>();
                if (img == null)
                {
                    // 자식에서 Image 컴포넌트 찾기
                    img = characterButtons[i].GetComponentInChildren<Image>();
                }
                
                buttonImages[i] = img;
                Debug.Log($"✅ 버튼[{i}]의 Image 컴포넌트 자동 할당: {(img != null ? "성공" : "실패")}");
            }
        }
        
        Debug.Log($"🔧 buttonImages 배열 자동 할당 완료: {buttonImages.Length}개");
    }
    
    /// <summary>
    /// 캐릭터 데이터베이스의 이미지 상태를 체크하고 표시
    /// </summary>
    void CheckCharacterDatabaseImages()
    {
        if (GameManager.Instance?.characterDatabase == null)
        {
            Debug.LogError("❌ CharacterDatabase가 null입니다!");
            return;
        }

        var allCharacters = GameManager.Instance.characterDatabase.GetAllTacticalCharacters();
        Debug.Log($"=== 🔍 캐릭터 데이터베이스 이미지 상태 체크 (총 {allCharacters.Count}개) ===");

        int withCharacterIcon = 0;
        int withButtonIcon = 0;
        int withoutIcons = 0;

        for (int i = 0; i < allCharacters.Count; i++)
        {
            CharacterData character = allCharacters[i];
            
            string characterIconStatus = character.characterIcon != null ? character.characterIcon.name : "null";
            string buttonIconStatus = character.buttonIcon != null ? character.buttonIcon.name : "null";
            
            Debug.Log($"캐릭터[{i}] '{character.characterName}' | characterIcon: {characterIconStatus} | buttonIcon: {buttonIconStatus}");
            
            if (character.characterIcon != null) withCharacterIcon++;
            if (character.buttonIcon != null) withButtonIcon++;
            if (character.characterIcon == null && character.buttonIcon == null) withoutIcons++;
        }
        
        Debug.Log($"📊 통계: characterIcon 있음: {withCharacterIcon}개 | buttonIcon 있음: {withButtonIcon}개 | 둘 다 없음: {withoutIcons}개");
        
        // 현재 손패의 캐릭터들 체크
        var currentHand = GameManager.Instance.GetCurrentHand();
        Debug.Log($"=== 🎴 현재 손패 캐릭터 이미지 상태 (총 {currentHand.Count}개) ===");
        
        for (int i = 0; i < currentHand.Count; i++)
        {
            CharacterData character = currentHand[i];
            string characterIconStatus = character.characterIcon != null ? character.characterIcon.name : "null";
            string buttonIconStatus = character.buttonIcon != null ? character.buttonIcon.name : "null";
            
            Debug.Log($"손패[{i}] '{character.characterName}' | characterIcon: {characterIconStatus} | buttonIcon: {buttonIconStatus}");
        }

        // UI 새로고침
        UpdateCharacterButtons();
    }
}
}