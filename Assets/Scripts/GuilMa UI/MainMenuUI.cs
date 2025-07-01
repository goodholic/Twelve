using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GuildMaster.Core;
using GuildMaster.Systems;
using GuildMaster.Data;

namespace GuildMaster.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI versionText;
        
        [Header("Main Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
        
        [Header("New Game Panel")]
        [SerializeField] private GameObject newGamePanel;
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private TMP_InputField guildNameInput;
        [SerializeField] private Toggle[] difficultyToggles;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button backFromNewGameButton;
        
        [Header("Load Game Panel")]
        [SerializeField] private GameObject loadGamePanel;
        [SerializeField] private Transform saveSlotContainer;
        [SerializeField] private GameObject saveSlotPrefab;
        [SerializeField] private Button backFromLoadButton;
        
        [Header("Settings Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Dropdown languageDropdown;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Button applySettingsButton;
        [SerializeField] private Button backFromSettingsButton;
        
        [Header("Credits Panel")]
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private ScrollRect creditsScroll;
        [SerializeField] private Button backFromCreditsButton;
        
        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmationDialog;
        [SerializeField] private TextMeshProUGUI confirmationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        
        [Header("Background")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private ParticleSystem backgroundParticles;
        
        [Header("Animations")]
        [SerializeField] private float panelFadeDuration = 0.3f;
        [SerializeField] private AnimationCurve fadeInCurve;
        [SerializeField] private AnimationCurve fadeOutCurve;
        
        private GameInitializer gameInitializer;
        private GuildMaster.Core.SaveData[] availableSaves;
        private int selectedDifficulty = 1; // 0: Easy, 1: Normal, 2: Hard
        
        void Start()
        {
            gameInitializer = FindObjectOfType<GameInitializer>();
            
            SetupUI();
            SetupButtons();
            CheckSaveData();
            
            // 배경 음악 재생
            SoundSystem.Instance?.PlayMusic("main_menu");
            
            // 버전 표시
            if (versionText != null)
            {
                versionText.text = $"v{Application.version}";
            }
            
            // 초기 패널 설정
            ShowMainPanel();
        }
        
        void SetupUI()
        {
            // 언어 드롭다운 설정
            if (languageDropdown != null)
            {
                languageDropdown.ClearOptions();
                var languages = LocalizationSystem.Instance.GetSupportedLanguages();
                foreach (var lang in languages)
                {
                    languageDropdown.options.Add(new TMP_Dropdown.OptionData(lang.nativeName));
                }
                
                var currentLang = LocalizationSystem.Instance.GetCurrentLanguage();
                languageDropdown.value = languages.FindIndex(l => l.language == currentLang);
            }
            
            // 그래픽 품질 드롭다운 설정
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new System.Collections.Generic.List<string> { "낮음", "중간", "높음", "최고" });
                qualityDropdown.value = QualitySettings.GetQualityLevel();
            }
            
            // 볼륨 슬라이더 설정
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = SoundSystem.Instance?.GetMasterVolume() ?? 1f;
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = SoundSystem.Instance?.GetMusicVolume() ?? 0.7f;
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
            
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = SoundSystem.Instance?.GetSFXVolume() ?? 1f;
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
            
            // 전체화면 토글 설정
            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
            }
        }
        
        void SetupButtons()
        {
            // 메인 버튼들
            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGameClicked);
                
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
                
            if (loadGameButton != null)
                loadGameButton.onClick.AddListener(OnLoadGameClicked);
                
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
                
            if (creditsButton != null)
                creditsButton.onClick.AddListener(OnCreditsClicked);
                
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
            
            // 새 게임 패널 버튼들
            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGameClicked);
                
            if (backFromNewGameButton != null)
                backFromNewGameButton.onClick.AddListener(() => ShowMainPanel());
            
            // 기타 패널 뒤로가기 버튼들
            if (backFromLoadButton != null)
                backFromLoadButton.onClick.AddListener(() => ShowMainPanel());
                
            if (backFromSettingsButton != null)
                backFromSettingsButton.onClick.AddListener(() => ShowMainPanel());
                
            if (backFromCreditsButton != null)
                backFromCreditsButton.onClick.AddListener(() => ShowMainPanel());
            
            // 설정 적용 버튼
            if (applySettingsButton != null)
                applySettingsButton.onClick.AddListener(ApplySettings);
            
            // 난이도 토글 설정
            if (difficultyToggles != null)
            {
                for (int i = 0; i < difficultyToggles.Length; i++)
                {
                    int difficulty = i;
                    difficultyToggles[i].onValueChanged.AddListener((isOn) =>
                    {
                        if (isOn) selectedDifficulty = difficulty;
                    });
                }
            }
        }
        
        void CheckSaveData()
        {
            var saveManager = SaveManager.Instance;
            if (saveManager != null)
            {
                availableSaves = saveManager.GetAllSaveData();
                
                // Continue 버튼 활성화 여부
                if (continueButton != null)
                {
                    bool hasRecentSave = availableSaves != null && availableSaves.Length > 0;
                    continueButton.interactable = hasRecentSave;
                    
                    if (hasRecentSave)
                    {
                        var recentSave = GetMostRecentSave();
                        var continueText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
                        if (continueText != null)
                        {
                            continueText.text = $"이어하기\n<size=14>Lv.{recentSave.guildLevel} - {recentSave.guildName}</size>";
                        }
                    }
                }
                
                // Load 버튼 활성화 여부
                if (loadGameButton != null)
                {
                    loadGameButton.interactable = availableSaves != null && availableSaves.Length > 0;
                }
            }
        }
        
        GuildMaster.Core.SaveData GetMostRecentSave()
        {
            if (availableSaves == null || availableSaves.Length == 0)
                return null;
            
            GuildMaster.Core.SaveData mostRecent = availableSaves[0];
            foreach (var save in availableSaves)
            {
                if (save.saveTime > mostRecent.saveTime)
                {
                    mostRecent = save;
                }
            }
            
            return mostRecent;
        }
        
        void ShowMainPanel()
        {
            StartCoroutine(TransitionToPanel(mainPanel));
        }
        
        void OnNewGameClicked()
        {
            PlayButtonSound();
            StartCoroutine(TransitionToPanel(newGamePanel));
            
            // 기본값 설정
            if (playerNameInput != null)
                playerNameInput.text = "플레이어";
                
            if (guildNameInput != null)
                guildNameInput.text = "아르테니아";
                
            if (difficultyToggles != null && difficultyToggles.Length > 1)
                difficultyToggles[1].isOn = true; // Normal 난이도
        }
        
        void OnContinueClicked()
        {
            PlayButtonSound();
            var recentSave = GetMostRecentSave();
            if (recentSave != null)
            {
                ShowConfirmationDialog(
                    "게임을 이어하시겠습니까?",
                    () => LoadSaveGame(recentSave)
                );
            }
        }
        
        void OnLoadGameClicked()
        {
            PlayButtonSound();
            StartCoroutine(TransitionToPanel(loadGamePanel));
            PopulateSaveSlots();
        }
        
        void OnSettingsClicked()
        {
            PlayButtonSound();
            StartCoroutine(TransitionToPanel(settingsPanel));
        }
        
        void OnCreditsClicked()
        {
            PlayButtonSound();
            StartCoroutine(TransitionToPanel(creditsPanel));
            
            // 크레딧 스크롤 시작
            if (creditsScroll != null)
            {
                StartCoroutine(AutoScrollCredits());
            }
        }
        
        void OnQuitClicked()
        {
            PlayButtonSound();
            ShowConfirmationDialog(
                "게임을 종료하시겠습니까?",
                () =>
                {
                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #else
                        Application.Quit();
                    #endif
                }
            );
        }
        
        void OnStartGameClicked()
        {
            PlayButtonSound();
            
            string playerName = playerNameInput?.text ?? "플레이어";
            string guildName = guildNameInput?.text ?? "아르테니아";
            
            if (string.IsNullOrWhiteSpace(playerName) || string.IsNullOrWhiteSpace(guildName))
            {
                ShowNotification("이름을 입력해주세요!");
                return;
            }
            
            // 게임 데이터 설정
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.SetString("GuildName", guildName);
            PlayerPrefs.SetInt("Difficulty", selectedDifficulty);
            PlayerPrefs.Save();
            
            // 새 게임 시작
            if (gameInitializer != null)
            {
                gameInitializer.StartNewGame();
            }
        }
        
        void PopulateSaveSlots()
        {
            // 기존 슬롯 제거
            foreach (Transform child in saveSlotContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 세이브 슬롯 생성
            if (availableSaves != null && saveSlotPrefab != null)
            {
                foreach (var save in availableSaves)
                {
                    GameObject slotObj = Instantiate(saveSlotPrefab, saveSlotContainer);
                    SaveSlotUI slotUI = slotObj.GetComponent<SaveSlotUI>();
                    
                    if (slotUI != null)
                    {
                        slotUI.SetupSlot(save, () => LoadSaveGame(save));
                    }
                }
            }
            
            // 빈 슬롯 추가
            for (int i = availableSaves?.Length ?? 0; i < 3; i++)
            {
                GameObject slotObj = Instantiate(saveSlotPrefab, saveSlotContainer);
                SaveSlotUI slotUI = slotObj.GetComponent<SaveSlotUI>();
                
                if (slotUI != null)
                {
                    slotUI.SetupEmptySlot(i);
                }
            }
        }
        
        void LoadSaveGame(GuildMaster.Core.SaveData saveData)
        {
            if (gameInitializer != null)
            {
                PlayerPrefs.SetInt("LoadSlot", saveData.slotIndex);
                gameInitializer.ContinueGame();
            }
        }
        
        void ApplySettings()
        {
            PlayButtonSound();
            
            // 언어 설정
            if (languageDropdown != null)
            {
                var languages = LocalizationSystem.Instance.GetSupportedLanguages();
                if (languageDropdown.value < languages.Count)
                {
                    LocalizationSystem.Instance.SetLanguage(languages[languageDropdown.value].language);
                }
            }
            
            // 그래픽 품질 설정
            if (qualityDropdown != null)
            {
                QualitySettings.SetQualityLevel(qualityDropdown.value);
                PlayerPrefs.SetInt("GraphicsQuality", qualityDropdown.value);
            }
            
            // 전체화면 설정
            if (fullscreenToggle != null)
            {
                Screen.fullScreen = fullscreenToggle.isOn;
                PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
            }
            
            PlayerPrefs.Save();
            
            ShowNotification("설정이 적용되었습니다!");
        }
        
        void OnMasterVolumeChanged(float value)
        {
            SoundSystem.Instance?.SetMasterVolume(value);
        }
        
        void OnMusicVolumeChanged(float value)
        {
            SoundSystem.Instance?.SetMusicVolume(value);
        }
        
        void OnSFXVolumeChanged(float value)
        {
            SoundSystem.Instance?.SetSFXVolume(value);
        }
        
        IEnumerator TransitionToPanel(GameObject targetPanel)
        {
            // 모든 패널 페이드 아웃
            GameObject[] allPanels = { mainPanel, newGamePanel, loadGamePanel, settingsPanel, creditsPanel };
            
            foreach (var panel in allPanels)
            {
                if (panel != null && panel.activeSelf && panel != targetPanel)
                {
                    yield return FadePanel(panel, false);
                }
            }
            
            // 타겟 패널 페이드 인
            if (targetPanel != null)
            {
                targetPanel.SetActive(true);
                yield return FadePanel(targetPanel, true);
            }
        }
        
        IEnumerator FadePanel(GameObject panel, bool fadeIn)
        {
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }
            
            float startAlpha = fadeIn ? 0f : 1f;
            float endAlpha = fadeIn ? 1f : 0f;
            float elapsed = 0f;
            
            canvasGroup.alpha = startAlpha;
            
            while (elapsed < panelFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / panelFadeDuration;
                float curveValue = fadeIn ? fadeInCurve.Evaluate(t) : fadeOutCurve.Evaluate(t);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);
                yield return null;
            }
            
            canvasGroup.alpha = endAlpha;
            
            if (!fadeIn)
            {
                panel.SetActive(false);
            }
        }
        
        IEnumerator AutoScrollCredits()
        {
            yield return new WaitForSeconds(1f);
            
            float scrollSpeed = 30f; // pixels per second
            RectTransform content = creditsScroll.content;
            
            while (creditsPanel.activeSelf)
            {
                float newY = content.anchoredPosition.y + scrollSpeed * Time.deltaTime;
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, newY);
                
                // 끝에 도달하면 처음으로
                if (newY > content.rect.height - creditsScroll.GetComponent<RectTransform>().rect.height)
                {
                    content.anchoredPosition = Vector2.zero;
                    yield return new WaitForSeconds(2f);
                }
                
                yield return null;
            }
        }
        
        void ShowConfirmationDialog(string message, Action onConfirm)
        {
            if (confirmationDialog != null)
            {
                confirmationDialog.SetActive(true);
                
                if (confirmationText != null)
                    confirmationText.text = message;
                
                if (confirmButton != null)
                {
                    confirmButton.onClick.RemoveAllListeners();
                    confirmButton.onClick.AddListener(() =>
                    {
                        PlayButtonSound();
                        confirmationDialog.SetActive(false);
                        onConfirm?.Invoke();
                    });
                }
                
                if (cancelButton != null)
                {
                    cancelButton.onClick.RemoveAllListeners();
                    cancelButton.onClick.AddListener(() =>
                    {
                        PlayButtonSound();
                        confirmationDialog.SetActive(false);
                    });
                }
            }
        }
        
        void ShowNotification(string message)
        {
            // TODO: 알림 UI 구현
            Debug.Log($"Notification: {message}");
        }
        
        void PlayButtonSound()
        {
            SoundSystem.Instance?.PlaySound("ui_click");
        }
    }
}