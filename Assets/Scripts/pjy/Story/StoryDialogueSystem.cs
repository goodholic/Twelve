using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using System.Collections;

namespace pjy.Story
{
    /// <summary>
    /// 스토리 대화 시스템
    /// - 캐릭터 대화 표시
    /// - 캐릭터 이미지 표시
    /// - 대화 진행 관리
    /// - 이벤트 조건 체크
    /// </summary>
    public class StoryDialogueSystem : MonoBehaviour
    {
        private static StoryDialogueSystem instance;
        public static StoryDialogueSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<StoryDialogueSystem>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("StoryDialogueSystem");
                        instance = go.AddComponent<StoryDialogueSystem>();
                    }
                }
                return instance;
            }
        }

        [Header("대화 UI")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Image characterImage;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private float textSpeed = 0.05f;

        [Header("캐릭터 이미지 설정")]
        [SerializeField] private Transform characterImageContainer;
        [SerializeField] private Image leftCharacterImage;
        [SerializeField] private Image rightCharacterImage;
        [SerializeField] private Image centerCharacterImage;

        [Header("선택지 UI")]
        [SerializeField] private GameObject choicePanel;
        [SerializeField] private Transform choiceButtonContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("데이터")]
        [SerializeField] private StoryDatabase storyDatabase;
        [SerializeField] private CharacterImageDatabase characterImageDatabase;

        private Queue<DialogueLine> currentDialogue;
        private DialogueLine currentLine;
        private bool isTyping = false;
        private bool canProceed = false;
        private Coroutine typingCoroutine;

        private Dictionary<string, int> storyVariables = new Dictionary<string, int>();
        private System.Action onDialogueComplete;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeUI();
            LoadStoryProgress();
        }

        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            if (choicePanel != null)
                choicePanel.SetActive(false);

            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextButtonClick);

            if (skipButton != null)
                skipButton.onClick.AddListener(OnSkipButtonClick);

            // 캐릭터 이미지 초기화
            HideAllCharacterImages();
        }

        /// <summary>
        /// 스토리 시작
        /// </summary>
        public void StartStory(string storyId, System.Action onComplete = null)
        {
            StoryData story = storyDatabase.GetStory(storyId);
            if (story == null)
            {
                Debug.LogError($"[StoryDialogueSystem] 스토리를 찾을 수 없습니다: {storyId}");
                return;
            }

            // 조건 체크
            if (!CheckStoryConditions(story))
            {
                Debug.Log($"[StoryDialogueSystem] 스토리 조건을 만족하지 않습니다: {storyId}");
                return;
            }

            onDialogueComplete = onComplete;
            currentDialogue = new Queue<DialogueLine>(story.dialogueLines);
            
            ShowDialoguePanel();
            DisplayNextLine();
        }

        /// <summary>
        /// 이벤트 스토리 체크 및 실행
        /// </summary>
        public void CheckAndStartEventStory(string eventType, int eventValue)
        {
            List<StoryData> eventStories = storyDatabase.GetEventStories(eventType, eventValue);
            
            foreach (var story in eventStories)
            {
                if (CheckStoryConditions(story) && !IsStoryCompleted(story.storyId))
                {
                    StartStory(story.storyId);
                    break; // 한 번에 하나의 스토리만 실행
                }
            }
        }

        /// <summary>
        /// 스토리 조건 체크
        /// </summary>
        private bool CheckStoryConditions(StoryData story)
        {
            if (story.conditions == null || story.conditions.Count == 0)
                return true;

            foreach (var condition in story.conditions)
            {
                if (!CheckCondition(condition))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 개별 조건 체크
        /// </summary>
        private bool CheckCondition(StoryCondition condition)
        {
            switch (condition.conditionType)
            {
                case ConditionType.Stage:
                    return CheckStageCondition(condition);
                case ConditionType.Character:
                    return CheckCharacterCondition(condition);
                case ConditionType.Item:
                    return CheckItemCondition(condition);
                case ConditionType.Variable:
                    return CheckVariableCondition(condition);
                case ConditionType.Date:
                    return CheckDateCondition(condition);
                default:
                    return true;
            }
        }

        /// <summary>
        /// 스테이지 조건 체크
        /// </summary>
        private bool CheckStageCondition(StoryCondition condition)
        {
            int currentStage = PlayerPrefs.GetInt("CurrentStage", 1);
            
            switch (condition.comparisonOperator)
            {
                case ComparisonOperator.Equal:
                    return currentStage == condition.targetValue;
                case ComparisonOperator.Greater:
                    return currentStage > condition.targetValue;
                case ComparisonOperator.Less:
                    return currentStage < condition.targetValue;
                case ComparisonOperator.GreaterOrEqual:
                    return currentStage >= condition.targetValue;
                case ComparisonOperator.LessOrEqual:
                    return currentStage <= condition.targetValue;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 캐릭터 보유 조건 체크
        /// </summary>
        private bool CheckCharacterCondition(StoryCondition condition)
        {
            // CharacterInventoryManager와 연동
            // 특정 캐릭터 보유 여부 체크
            return true; // 임시 구현
        }

        /// <summary>
        /// 아이템 조건 체크
        /// </summary>
        private bool CheckItemCondition(StoryCondition condition)
        {
            // 아이템 시스템과 연동
            return true; // 임시 구현
        }

        /// <summary>
        /// 변수 조건 체크
        /// </summary>
        private bool CheckVariableCondition(StoryCondition condition)
        {
            if (!storyVariables.ContainsKey(condition.targetKey))
                return false;

            int value = storyVariables[condition.targetKey];
            
            switch (condition.comparisonOperator)
            {
                case ComparisonOperator.Equal:
                    return value == condition.targetValue;
                case ComparisonOperator.Greater:
                    return value > condition.targetValue;
                case ComparisonOperator.Less:
                    return value < condition.targetValue;
                case ComparisonOperator.GreaterOrEqual:
                    return value >= condition.targetValue;
                case ComparisonOperator.LessOrEqual:
                    return value <= condition.targetValue;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 날짜 조건 체크
        /// </summary>
        private bool CheckDateCondition(StoryCondition condition)
        {
            // 특정 날짜나 이벤트 기간 체크
            return true; // 임시 구현
        }

        /// <summary>
        /// 다음 대화 표시
        /// </summary>
        private void DisplayNextLine()
        {
            if (currentDialogue.Count == 0)
            {
                EndDialogue();
                return;
            }

            currentLine = currentDialogue.Dequeue();
            
            // 캐릭터 이름 표시
            if (characterNameText != null)
                characterNameText.text = currentLine.characterName;

            // 캐릭터 이미지 표시
            DisplayCharacterImage(currentLine);

            // 대화 텍스트 타이핑 효과
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            
            typingCoroutine = StartCoroutine(TypeText(currentLine.dialogueText));

            // 선택지가 있는 경우
            if (currentLine.choices != null && currentLine.choices.Count > 0)
            {
                canProceed = false;
                if (nextButton != null)
                    nextButton.gameObject.SetActive(false);
            }
            else
            {
                canProceed = false;
                if (nextButton != null)
                    nextButton.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 캐릭터 이미지 표시
        /// </summary>
        private void DisplayCharacterImage(DialogueLine line)
        {
            // 모든 캐릭터 이미지 숨김
            HideAllCharacterImages();

            // 캐릭터 이미지 가져오기
            Sprite characterSprite = characterImageDatabase.GetCharacterImage(line.characterName, line.emotion);
            if (characterSprite == null)
                return;

            // 위치에 따라 이미지 표시
            Image targetImage = null;
            switch (line.position)
            {
                case CharacterPosition.Left:
                    targetImage = leftCharacterImage;
                    break;
                case CharacterPosition.Right:
                    targetImage = rightCharacterImage;
                    break;
                case CharacterPosition.Center:
                    targetImage = centerCharacterImage;
                    break;
            }

            if (targetImage != null)
            {
                targetImage.sprite = characterSprite;
                targetImage.gameObject.SetActive(true);
                
                // 페이드 인 효과
                StartCoroutine(FadeInImage(targetImage));
            }

            // 메인 캐릭터 이미지에도 표시 (구버전 호환)
            if (characterImage != null)
            {
                characterImage.sprite = characterSprite;
                characterImage.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 모든 캐릭터 이미지 숨김
        /// </summary>
        private void HideAllCharacterImages()
        {
            if (leftCharacterImage != null)
                leftCharacterImage.gameObject.SetActive(false);
            if (rightCharacterImage != null)
                rightCharacterImage.gameObject.SetActive(false);
            if (centerCharacterImage != null)
                centerCharacterImage.gameObject.SetActive(false);
        }

        /// <summary>
        /// 텍스트 타이핑 효과
        /// </summary>
        private IEnumerator TypeText(string text)
        {
            isTyping = true;
            dialogueText.text = "";
            
            foreach (char letter in text.ToCharArray())
            {
                dialogueText.text += letter;
                yield return new WaitForSeconds(textSpeed);
            }
            
            isTyping = false;
            canProceed = true;

            // 선택지 표시
            if (currentLine.choices != null && currentLine.choices.Count > 0)
            {
                ShowChoices(currentLine.choices);
            }
        }

        /// <summary>
        /// 선택지 표시
        /// </summary>
        private void ShowChoices(List<DialogueChoice> choices)
        {
            if (choicePanel == null || choiceButtonPrefab == null)
                return;

            // 기존 선택지 버튼 제거
            foreach (Transform child in choiceButtonContainer)
            {
                Destroy(child.gameObject);
            }

            choicePanel.SetActive(true);

            // 선택지 버튼 생성
            foreach (var choice in choices)
            {
                GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

                if (buttonText != null)
                    buttonText.text = choice.choiceText;

                // 선택지 클릭 이벤트
                button.onClick.AddListener(() => OnChoiceSelected(choice));
            }
        }

        /// <summary>
        /// 선택지 선택
        /// </summary>
        private void OnChoiceSelected(DialogueChoice choice)
        {
            choicePanel.SetActive(false);

            // 변수 설정
            if (!string.IsNullOrEmpty(choice.setVariable))
            {
                storyVariables[choice.setVariable] = choice.setValue;
            }

            // 다음 스토리로 진행
            if (!string.IsNullOrEmpty(choice.nextStoryId))
            {
                StartStory(choice.nextStoryId, onDialogueComplete);
            }
            else
            {
                // 현재 대화 계속
                canProceed = true;
                if (nextButton != null)
                    nextButton.gameObject.SetActive(true);
                DisplayNextLine();
            }
        }

        /// <summary>
        /// 다음 버튼 클릭
        /// </summary>
        private void OnNextButtonClick()
        {
            if (isTyping)
            {
                // 타이핑 중이면 즉시 완료
                if (typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                    dialogueText.text = currentLine.dialogueText;
                    isTyping = false;
                    canProceed = true;

                    // 선택지 표시
                    if (currentLine.choices != null && currentLine.choices.Count > 0)
                    {
                        ShowChoices(currentLine.choices);
                    }
                }
            }
            else if (canProceed)
            {
                DisplayNextLine();
            }
        }

        /// <summary>
        /// 스킵 버튼 클릭
        /// </summary>
        private void OnSkipButtonClick()
        {
            // 스킵 확인 팝업 표시
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                EndDialogue();
            }
        }

        /// <summary>
        /// 대화 종료
        /// </summary>
        private void EndDialogue()
        {
            HideDialoguePanel();
            
            // 스토리 완료 저장
            if (currentLine != null && !string.IsNullOrEmpty(currentLine.storyId))
            {
                MarkStoryAsCompleted(currentLine.storyId);
            }

            // 완료 콜백 실행
            onDialogueComplete?.Invoke();
            onDialogueComplete = null;
        }

        /// <summary>
        /// 대화 패널 표시
        /// </summary>
        private void ShowDialoguePanel()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
                // 게임 일시정지
                Time.timeScale = 0f;
            }
        }

        /// <summary>
        /// 대화 패널 숨김
        /// </summary>
        private void HideDialoguePanel()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
                // 게임 재개
                Time.timeScale = 1f;
            }

            HideAllCharacterImages();
        }

        /// <summary>
        /// 이미지 페이드 인
        /// </summary>
        private IEnumerator FadeInImage(Image image)
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Color color = image.color;
            color.a = 0f;
            image.color = color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Clamp01(elapsed / duration);
                image.color = color;
                yield return null;
            }

            color.a = 1f;
            image.color = color;
        }

        /// <summary>
        /// 스토리 완료 체크
        /// </summary>
        private bool IsStoryCompleted(string storyId)
        {
            return PlayerPrefs.GetInt($"Story_Completed_{storyId}", 0) == 1;
        }

        /// <summary>
        /// 스토리 완료 표시
        /// </summary>
        private void MarkStoryAsCompleted(string storyId)
        {
            PlayerPrefs.SetInt($"Story_Completed_{storyId}", 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 스토리 진행도 저장
        /// </summary>
        private void SaveStoryProgress()
        {
            // 변수 저장
            foreach (var kvp in storyVariables)
            {
                PlayerPrefs.SetInt($"Story_Var_{kvp.Key}", kvp.Value);
            }
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 스토리 진행도 로드
        /// </summary>
        private void LoadStoryProgress()
        {
            // 저장된 변수 로드 (실제 구현 시 키 목록 관리 필요)
        }

        /// <summary>
        /// 변수 설정
        /// </summary>
        public void SetVariable(string key, int value)
        {
            storyVariables[key] = value;
            SaveStoryProgress();
        }

        /// <summary>
        /// 변수 가져오기
        /// </summary>
        public int GetVariable(string key, int defaultValue = 0)
        {
            return storyVariables.ContainsKey(key) ? storyVariables[key] : defaultValue;
        }
    }
}