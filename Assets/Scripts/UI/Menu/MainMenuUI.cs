using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace GuildMaster.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Menu Buttons")]
        public Button startGameButton;
        public Button continueButton;
        public Button settingsButton;
        public Button quitButton;
        
        [Header("Panels")]
        public GameObject mainMenuPanel;
        public GameObject settingsPanel;
        
        private void Start()
        {
            InitializeButtons();
        }
        
        private void InitializeButtons()
        {
            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGameClicked);
                
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
                
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
                
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
        }
        
        private void OnStartGameClicked()
        {
            SceneManager.LoadScene("GameScene");
        }
        
        private void OnContinueClicked()
        {
            // 저장된 게임 로드
            SceneManager.LoadScene("GameScene");
        }
        
        private void OnSettingsClicked()
        {
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);
                
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }
        
        private void OnQuitClicked()
        {
            Application.Quit();
        }
        
        public void ReturnToMainMenu()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
                
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);
        }
    }
} 