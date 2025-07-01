using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Systems;

namespace GuildMaster.UI
{
    /// <summary>
    /// 스토리 모드 UI
    /// 챕터 선택, 대화 표시, 선택지 관리
    /// </summary>
    public class StoryModeUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject storyPanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        
        [Header("Chapter Selection")]
        [SerializeField] private GameObject chapterSelectionPanel;
        [SerializeField] private Transform chapterListContainer;
        [SerializeField] private GameObject chapterEntryPrefab;
        [SerializeField] private Button mainStoryTab;
        [SerializeField] private Button sideStoryTab;
        [SerializeField] private Button characterStoryTab;
        [SerializeField] private Button hiddenStoryTab;
        
        [Header("Chapter Details")]
        [SerializeField] private GameObject chapterDetailsPanel;
        [SerializeField] private TextMeshProUGUI chapterTitleText;
        [SerializeField] private TextMeshProUGUI chapterDescriptionText;
        [SerializeField] private TextMeshProUGUI chapterRequirementsText;
        [SerializeField] private Image chapterTypeIcon;
        [SerializeField] private Button startChapterButton;
        [SerializeField] private TextMeshProUGUI completionStatusText;
        
        [Header("Story Display")]
        [SerializeField] private GameObject storyDisplayPanel;
        [SerializeField] private Image speakerPortrait;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private GameObject dialogueBackground;
        [SerializeField] private float textSpeed = 0.05f;
        
        [Header("Choices")]
        [SerializeField] private GameObject choicesPanel;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private GameObject choiceButtonPrefab;
        
        [Header("Story Controls")]
        [SerializeField] private Button skipButton;
        [SerializeField] private Button autoPlayButton;
        [SerializeField] private Slider autoPlaySpeedSlider;
        [SerializeField] private TextMeshProUGUI autoPlaySpeedText;
        [SerializeField] private Button logButton;
        [SerializeField] private Button savePositionButton;
        
        [Header("Story Log")]
        [SerializeField] private GameObject storyLogPanel;
        [SerializeField] private Transform logContainer;
        [SerializeField] private GameObject logEntryPrefab;
        [SerializeField] private Button closeLogButton;
        [SerializeField] private ScrollRect logScrollRect;
        
        [Header("Endings Gallery")]
        [SerializeField] private GameObject endingsPanel;
        [SerializeField] private Transform endingsContainer;
        [SerializeField] private GameObject endingEntryPrefab;
        [SerializeField] private Button endingsGalleryButton;
        
        [Header("Visual Effects")]
        [SerializeField] private Image screenFadeImage;
        [SerializeField] private AnimationCurve fadeInCurve;
        [SerializeField] private AnimationCurve fadeOutCurve;
        [SerializeField] private ParticleSystem choiceEffect;
        [SerializeField] private ParticleSystem chapterCompleteEffect;
        
        [Header("Progress Display")]
        [SerializeField] private Slider overallProgressBar;
        [SerializeField] private TextMeshProUGUI progressPercentageText;
        [SerializeField] private TextMeshProUGUI completedChaptersText;
        [SerializeField] private TextMeshProUGUI totalChoicesText;
        
        // System references
        private StoryManager storyManager;
        
        // UI State
        private StoryChapterType currentTabType = StoryChapterType.Main;
        private StoryChapter selectedChapter;
        private List<GameObject> chapterEntries = new List<GameObject>();
        private List<GameObject> choiceButtons = new List<GameObject>();
        private List<GameObject> logEntries = new List<GameObject>();
        private Coroutine textDisplayCoroutine;
        private bool isAutoPlay = false;
        private float autoPlaySpeed = 2f;
        private bool isDisplayingText = false;
        
        // Story log
        private List<DialogueLogEntry> dialogueLog = new List<DialogueLogEntry>();
        
        [System.Serializable]
        public class DialogueLogEntry
        {
            public string speaker;
            public string text;
            public List<string> choices;
            public string chosenOption;
            public float timestamp;
        }
        
        void Start()
        {
            storyManager = FindObjectOfType<StoryManager>();
            
            if (storyManager == null)
            {
                Debug.LogError("StoryManager not found!");
                enabled = false;
                return;
            }
            
            SetupUI();
            SubscribeToEvents();
            
            // 초기 UI 업데이트
            RefreshChapterList();
            UpdateProgressDisplay();
        }
        
        void SetupUI()
        {
            // 메인 버튼
            if (openButton != null)
                openButton.onClick.AddListener(() => ShowPanel(true));
            if (closeButton != null)
                closeButton.onClick.AddListener(() => ShowPanel(false));
            
            // 탭 버튼
            if (mainStoryTab != null)
                mainStoryTab.onClick.AddListener(() => ShowTab(StoryChapterType.Main));
            if (sideStoryTab != null)
                sideStoryTab.onClick.AddListener(() => ShowTab(StoryChapterType.Side));
            if (characterStoryTab != null)
                characterStoryTab.onClick.AddListener(() => ShowTab(StoryChapterType.Character));
            if (hiddenStoryTab != null)
                hiddenStoryTab.onClick.AddListener(() => ShowTab(StoryChapterType.Hidden));
            
            // 챕터 시작
            if (startChapterButton != null)
                startChapterButton.onClick.AddListener(StartSelectedChapter);
            
            // 스토리 컨트롤
            if (skipButton != null)
                skipButton.onClick.AddListener(SkipDialogue);
            if (autoPlayButton != null)
                autoPlayButton.onClick.AddListener(ToggleAutoPlay);
            if (logButton != null)
                logButton.onClick.AddListener(() => ShowStoryLog(true));
            if (closeLogButton != null)
                closeLogButton.onClick.AddListener(() => ShowStoryLog(false));
            if (savePositionButton != null)
                savePositionButton.onClick.AddListener(SaveStoryPosition);
            if (endingsGalleryButton != null)
                endingsGalleryButton.onClick.AddListener(ShowEndingsGallery);
            
            // Auto play speed
            if (autoPlaySpeedSlider != null)
            {
                autoPlaySpeedSlider.onValueChanged.AddListener(OnAutoPlaySpeedChanged);
                autoPlaySpeedSlider.value = autoPlaySpeed;
            }
        }
        
        void SubscribeToEvents()
        {
            if (storyManager != null)
            {
                storyManager.OnChapterStarted += OnChapterStarted;
                storyManager.OnChapterCompleted += OnChapterCompleted;
                storyManager.OnNodeStarted += OnNodeStarted;
                storyManager.OnChoiceMade += OnChoiceMade;
                storyManager.OnEndingUnlocked += OnEndingUnlocked;
                storyManager.OnDialogueDisplay += OnDialogueDisplay;
            }
        }
        
        void OnDestroy()
        {
            // 이벤트 구독 해제
            if (storyManager != null)
            {
                storyManager.OnChapterStarted -= OnChapterStarted;
                storyManager.OnChapterCompleted -= OnChapterCompleted;
                storyManager.OnNodeStarted -= OnNodeStarted;
                storyManager.OnChoiceMade -= OnChoiceMade;
                storyManager.OnEndingUnlocked -= OnEndingUnlocked;
                storyManager.OnDialogueDisplay -= OnDialogueDisplay;
            }
        }
        
        void ShowPanel(bool show)
        {
            if (storyPanel != null)
            {
                storyPanel.SetActive(show);
                
                if (show)
                {
                    RefreshChapterList();
                    UpdateProgressDisplay();
                    
                    // 진행 중인 스토리가 있으면 계속 표시
                    if (storyManager.GetCurrentChapter() != null)
                    {
                        ShowStoryDisplay(true);
                    }
                    else
                    {
                        ShowChapterSelection(true);
                    }
                }
            }
        }
        
        void ShowTab(StoryChapterType tabType)
        {
            currentTabType = tabType;
            RefreshChapterList();
            UpdateTabHighlight();
        }
        
        void UpdateTabHighlight()
        {
            Color normalColor = new Color(0.8f, 0.8f, 0.8f);
            Color selectedColor = Color.white;
            
            if (mainStoryTab != null)
                mainStoryTab.image.color = currentTabType == StoryChapterType.Main ? selectedColor : normalColor;
            if (sideStoryTab != null)
                sideStoryTab.image.color = currentTabType == StoryChapterType.Side ? selectedColor : normalColor;
            if (characterStoryTab != null)
                characterStoryTab.image.color = currentTabType == StoryChapterType.Character ? selectedColor : normalColor;
            if (hiddenStoryTab != null)
                hiddenStoryTab.image.color = currentTabType == StoryChapterType.Hidden ? selectedColor : normalColor;
        }
        
        void RefreshChapterList()
        {
            // 기존 엔트리 제거
            foreach (var entry in chapterEntries)
            {
                Destroy(entry);
            }
            chapterEntries.Clear();
            
            // 모든 챕터 가져오기
            var availableChapters = storyManager.GetAvailableChapters()
                .Where(c => c.Type == currentTabType)
                .OrderBy(c => c.ChapterNumber)
                .ToList();
            
            var completedChapters = storyManager.GetCompletedChapters()
                .Where(c => c.Type == currentTabType)
                .OrderBy(c => c.ChapterNumber)
                .ToList();
            
            // 챕터 엔트리 생성
            foreach (var chapter in availableChapters)
            {
                CreateChapterEntry(chapter, false);
            }
            
            foreach (var chapter in completedChapters)
            {
                CreateChapterEntry(chapter, true);
            }
        }
        
        void CreateChapterEntry(StoryChapter chapter, bool isCompleted)
        {
            if (chapterEntryPrefab == null || chapterListContainer == null) return;
            
            var entryObj = Instantiate(chapterEntryPrefab, chapterListContainer);
            chapterEntries.Add(entryObj);
            
            // UI 요소 찾기
            var titleText = entryObj.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
            var descriptionText = entryObj.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
            var statusIcon = entryObj.transform.Find("StatusIcon")?.GetComponent<Image>();
            var selectButton = entryObj.GetComponent<Button>();
            
            // 정보 설정
            if (titleText != null)
                titleText.text = chapter.Title;
            
            if (descriptionText != null)
                descriptionText.text = chapter.Description;
            
            if (statusIcon != null)
            {
                if (isCompleted)
                {
                    statusIcon.color = GetEndingColor(chapter.AchievedEnding);
                }
                else
                {
                    statusIcon.color = Color.gray;
                }
            }
            
            // 선택 버튼
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(() => SelectChapter(chapter));
            }
        }
        
        Color GetEndingColor(StoryEnding ending)
        {
            return ending switch
            {
                StoryEnding.HeroicVictory => Color.yellow,
                StoryEnding.PeacefulResolution => Color.green,
                StoryEnding.BitterSweet => new Color(1f, 0.5f, 0f), // Orange
                StoryEnding.TragicDefeat => Color.red,
                StoryEnding.TrueEnding => new Color(1f, 0f, 1f), // Magenta
                StoryEnding.SecretEnding => new Color(0.5f, 0f, 1f), // Purple
                _ => Color.white
            };
        }
        
        void SelectChapter(StoryChapter chapter)
        {
            selectedChapter = chapter;
            ShowChapterDetails(chapter);
        }
        
        void ShowChapterDetails(StoryChapter chapter)
        {
            if (chapterDetailsPanel != null)
            {
                chapterDetailsPanel.SetActive(true);
                
                if (chapterTitleText != null)
                    chapterTitleText.text = chapter.Title;
                
                if (chapterDescriptionText != null)
                    chapterDescriptionText.text = chapter.Description;
                
                if (chapterRequirementsText != null)
                {
                    string requirements = $"필요 길드 레벨: {chapter.RequiredGuildLevel}";
                    if (chapter.RequiredChapters.Count > 0)
                    {
                        requirements += "\n선행 챕터 필요";
                    }
                    chapterRequirementsText.text = requirements;
                }
                
                if (completionStatusText != null)
                {
                    if (chapter.IsCompleted)
                    {
                        completionStatusText.text = $"완료 ({chapter.PlaythroughCount}회 플레이)";
                        if (chapter.AchievedEnding != StoryEnding.None)
                        {
                            completionStatusText.text += $"\n엔딩: {GetEndingName(chapter.AchievedEnding)}";
                        }
                    }
                    else
                    {
                        completionStatusText.text = "미완료";
                    }
                }
                
                // 시작 버튼 상태
                if (startChapterButton != null)
                {
                    startChapterButton.interactable = !chapter.IsCompleted || chapter.Type != StoryChapterType.Main;
                    var buttonText = startChapterButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = chapter.IsCompleted ? "다시 플레이" : "챕터 시작";
                    }
                }
            }
        }
        
        string GetEndingName(StoryEnding ending)
        {
            return ending switch
            {
                StoryEnding.HeroicVictory => "영웅적 승리",
                StoryEnding.PeacefulResolution => "평화로운 해결",
                StoryEnding.BitterSweet => "씁쓸한 승리",
                StoryEnding.TragicDefeat => "비극적 패배",
                StoryEnding.TrueEnding => "진 엔딩",
                StoryEnding.SecretEnding => "시크릿 엔딩",
                _ => "알 수 없음"
            };
        }
        
        void StartSelectedChapter()
        {
            if (selectedChapter == null) return;
            
            bool started = storyManager.StartChapter(selectedChapter.ChapterId);
            
            if (started)
            {
                ShowChapterSelection(false);
                ShowStoryDisplay(true);
                StartCoroutine(FadeTransition(true));
            }
            else
            {
                Debug.LogWarning("Cannot start chapter - requirements not met");
            }
        }
        
        void ShowChapterSelection(bool show)
        {
            if (chapterSelectionPanel != null)
                chapterSelectionPanel.SetActive(show);
            
            if (chapterDetailsPanel != null && show)
                chapterDetailsPanel.SetActive(false);
        }
        
        void ShowStoryDisplay(bool show)
        {
            if (storyDisplayPanel != null)
                storyDisplayPanel.SetActive(show);
        }
        
        IEnumerator FadeTransition(bool fadeIn)
        {
            if (screenFadeImage == null) yield break;
            
            screenFadeImage.gameObject.SetActive(true);
            
            float duration = 0.5f;
            float elapsed = 0f;
            
            AnimationCurve curve = fadeIn ? fadeInCurve : fadeOutCurve;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                float alpha = curve != null ? curve.Evaluate(t) : t;
                alpha = fadeIn ? 1f - alpha : alpha;
                
                screenFadeImage.color = new Color(0, 0, 0, alpha);
                
                yield return null;
            }
            
            if (!fadeIn)
                screenFadeImage.gameObject.SetActive(false);
        }
        
        void DisplayDialogue(string speaker, string text)
        {
            if (speakerNameText != null)
                speakerNameText.text = speaker;
            
            if (dialogueText != null)
            {
                if (textDisplayCoroutine != null)
                {
                    StopCoroutine(textDisplayCoroutine);
                }
                
                textDisplayCoroutine = StartCoroutine(TypewriterEffect(text));
            }
            
            // 로그에 추가
            var logEntry = new DialogueLogEntry
            {
                speaker = speaker,
                text = text,
                timestamp = Time.time
            };
            dialogueLog.Add(logEntry);
        }
        
        IEnumerator TypewriterEffect(string text)
        {
            isDisplayingText = true;
            dialogueText.text = "";
            
            foreach (char c in text)
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(textSpeed);
            }
            
            isDisplayingText = false;
        }
        
        void DisplayChoices(StoryNode node)
        {
            // 기존 선택지 제거
            foreach (var button in choiceButtons)
            {
                Destroy(button);
            }
            choiceButtons.Clear();
            
            if (node.Choices.Count == 0)
            {
                choicesPanel.SetActive(false);
                return;
            }
            
            choicesPanel.SetActive(true);
            
            // 선택지 버튼 생성
            for (int i = 0; i < node.Choices.Count; i++)
            {
                var choice = node.Choices[i];
                var buttonObj = Instantiate(choiceButtonPrefab, choicesContainer);
                choiceButtons.Add(buttonObj);
                
                var button = buttonObj.GetComponent<Button>();
                var textComp = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                
                if (textComp != null)
                {
                    textComp.text = choice.Text;
                }
                
                // 요구사항 확인
                bool canChoose = choice.CanChoose(Core.GameManager.Instance);
                button.interactable = canChoose;
                
                if (!canChoose)
                {
                    // 요구사항 표시
                    if (textComp != null)
                    {
                        textComp.text += " (조건 미충족)";
                        textComp.color = new Color(0.7f, 0.7f, 0.7f);
                    }
                }
                
                int choiceIndex = i;
                button.onClick.AddListener(() => MakeChoice(choiceIndex));
            }
            
            // 선택 효과
            if (choiceEffect != null)
            {
                choiceEffect.Play();
            }
        }
        
        void MakeChoice(int choiceIndex)
        {
            if (choicesPanel != null)
                choicesPanel.SetActive(false);
            
            storyManager.MakeChoice(choiceIndex);
        }
        
        void SkipDialogue()
        {
            if (isDisplayingText && textDisplayCoroutine != null)
            {
                StopCoroutine(textDisplayCoroutine);
                var currentNode = storyManager.GetCurrentNode();
                if (currentNode != null && dialogueText != null)
                {
                    dialogueText.text = currentNode.DialogueText;
                    isDisplayingText = false;
                }
            }
            else
            {
                storyManager.SkipCurrentDialogue();
            }
        }
        
        void ToggleAutoPlay()
        {
            isAutoPlay = !isAutoPlay;
            storyManager.SetAutoPlay(isAutoPlay, autoPlaySpeed);
            
            if (autoPlayButton != null)
            {
                var buttonText = autoPlayButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = isAutoPlay ? "자동 재생 중지" : "자동 재생";
                }
            }
        }
        
        void OnAutoPlaySpeedChanged(float value)
        {
            autoPlaySpeed = value;
            storyManager.SetAutoPlay(isAutoPlay, autoPlaySpeed);
            
            if (autoPlaySpeedText != null)
            {
                autoPlaySpeedText.text = $"속도: {value:F1}x";
            }
        }
        
        void ShowStoryLog(bool show)
        {
            if (storyLogPanel != null)
            {
                storyLogPanel.SetActive(show);
                
                if (show)
                {
                    RefreshStoryLog();
                }
            }
        }
        
        void RefreshStoryLog()
        {
            // 기존 로그 엔트리 제거
            foreach (var entry in logEntries)
            {
                Destroy(entry);
            }
            logEntries.Clear();
            
            // 로그 엔트리 생성
            foreach (var log in dialogueLog)
            {
                CreateLogEntry(log);
            }
            
            // 스크롤 맨 아래로
            if (logScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                logScrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        void CreateLogEntry(DialogueLogEntry log)
        {
            if (logEntryPrefab == null || logContainer == null) return;
            
            var entryObj = Instantiate(logEntryPrefab, logContainer);
            logEntries.Add(entryObj);
            
            var textComp = entryObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.text = $"<b>{log.speaker}</b>\n{log.text}";
                
                if (log.chosenOption != null)
                {
                    textComp.text += $"\n<color=#FFD700>→ {log.chosenOption}</color>";
                }
            }
        }
        
        void SaveStoryPosition()
        {
            // TODO: 현재 스토리 위치 저장
            Debug.Log("Story position saved");
        }
        
        void ShowEndingsGallery()
        {
            if (endingsPanel != null)
            {
                endingsPanel.SetActive(true);
                RefreshEndingsGallery();
            }
        }
        
        void RefreshEndingsGallery()
        {
            // 기존 엔딩 엔트리 제거
            foreach (Transform child in endingsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 모든 가능한 엔딩
            var allEndings = System.Enum.GetValues(typeof(StoryEnding)).Cast<StoryEnding>()
                .Where(e => e != StoryEnding.None).ToList();
            
            var unlockedEndings = storyManager.GetUnlockedEndings();
            
            foreach (var ending in allEndings)
            {
                CreateEndingEntry(ending, unlockedEndings.Contains(ending));
            }
        }
        
        void CreateEndingEntry(StoryEnding ending, bool isUnlocked)
        {
            if (endingEntryPrefab == null || endingsContainer == null) return;
            
            var entryObj = Instantiate(endingEntryPrefab, endingsContainer);
            
            var nameText = entryObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var descriptionText = entryObj.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
            var icon = entryObj.transform.Find("Icon")?.GetComponent<Image>();
            
            if (nameText != null)
            {
                nameText.text = isUnlocked ? GetEndingName(ending) : "???";
            }
            
            if (descriptionText != null)
            {
                descriptionText.text = isUnlocked ? GetEndingDescription(ending) : "아직 발견하지 못한 엔딩입니다.";
            }
            
            if (icon != null)
            {
                icon.color = isUnlocked ? GetEndingColor(ending) : Color.gray;
            }
        }
        
        string GetEndingDescription(StoryEnding ending)
        {
            return ending switch
            {
                StoryEnding.HeroicVictory => "희생을 통해 세계를 구원했습니다.",
                StoryEnding.PeacefulResolution => "평화적인 방법으로 문제를 해결했습니다.",
                StoryEnding.BitterSweet => "승리했지만 큰 대가를 치렀습니다.",
                StoryEnding.TragicDefeat => "안타깝게도 실패했습니다.",
                StoryEnding.TrueEnding => "모든 진실을 밝혀냈습니다.",
                StoryEnding.SecretEnding => "숨겨진 길을 발견했습니다.",
                _ => ""
            };
        }
        
        void UpdateProgressDisplay()
        {
            float progress = storyManager.GetStoryCompletionPercentage();
            
            if (overallProgressBar != null)
                overallProgressBar.value = progress / 100f;
            
            if (progressPercentageText != null)
                progressPercentageText.text = $"{progress:F1}%";
            
            if (completedChaptersText != null)
            {
                int completed = storyManager.GetCompletedChapterCount();
                int total = storyManager.GetAvailableChapters().Count + completed;
                completedChaptersText.text = $"완료: {completed}/{total}";
            }
            
            if (totalChoicesText != null)
            {
                // TODO: Get total choices from StoryManager
                totalChoicesText.text = "선택: 0";
            }
        }
        
        // 이벤트 핸들러
        void OnChapterStarted(StoryChapter chapter)
        {
            Debug.Log($"Chapter started: {chapter.Title}");
            dialogueLog.Clear();
        }
        
        void OnChapterCompleted(StoryChapter chapter, StoryEnding ending)
        {
            Debug.Log($"Chapter completed: {chapter.Title} with ending: {ending}");
            
            if (chapterCompleteEffect != null)
            {
                chapterCompleteEffect.Play();
            }
            
            // 챕터 완료 알림
            StartCoroutine(ShowChapterCompleteNotification(chapter, ending));
        }
        
        IEnumerator ShowChapterCompleteNotification(StoryChapter chapter, StoryEnding ending)
        {
            yield return new WaitForSeconds(2f);
            
            ShowChapterSelection(true);
            ShowStoryDisplay(false);
            RefreshChapterList();
            UpdateProgressDisplay();
        }
        
        void OnNodeStarted(StoryNode node)
        {
            if (node.Choices.Count > 0)
            {
                DisplayChoices(node);
            }
            else
            {
                if (choicesPanel != null)
                    choicesPanel.SetActive(false);
            }
        }
        
        void OnChoiceMade(StoryChoice choice)
        {
            // 로그에 선택 기록
            if (dialogueLog.Count > 0)
            {
                dialogueLog[dialogueLog.Count - 1].chosenOption = choice.Text;
            }
        }
        
        void OnEndingUnlocked(StoryEnding ending)
        {
            Debug.Log($"New ending unlocked: {ending}");
            ShowNotification($"새로운 엔딩 해금: {GetEndingName(ending)}!");
        }
        
        void OnDialogueDisplay(string speaker, string text)
        {
            DisplayDialogue(speaker, text);
        }
        
        void ShowNotification(string message)
        {
            Debug.Log($"Story Notification: {message}");
            // TODO: 실제 알림 UI 구현
        }
    }
}