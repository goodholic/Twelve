using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

namespace GuildMaster.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Audio Settings")]
        public Slider masterVolumeSlider;
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;
        public AudioMixer audioMixer;
        
        [Header("Graphics Settings")]
        public TMP_Dropdown qualityDropdown;
        public TMP_Dropdown resolutionDropdown;
        public Toggle fullscreenToggle;
        public Toggle vsyncToggle;
        
        [Header("Gameplay Settings")]
        public Toggle autoSaveToggle;
        public Slider uiScaleSlider;
        public TMP_Dropdown languageDropdown;
        
        [Header("Controls")]
        public Button resetButton;
        public Button applyButton;
        public Button cancelButton;
        
        private void Start()
        {
            InitializeSettings();
            LoadSettings();
        }
        
        private void InitializeSettings()
        {
            // 오디오 슬라이더 이벤트
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
                
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
                
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            
            // 그래픽 설정 이벤트
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(SetQuality);
                
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(SetResolution);
                
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
                
            if (vsyncToggle != null)
                vsyncToggle.onValueChanged.AddListener(SetVSync);
            
            // 게임플레이 설정 이벤트
            if (autoSaveToggle != null)
                autoSaveToggle.onValueChanged.AddListener(SetAutoSave);
                
            if (uiScaleSlider != null)
                uiScaleSlider.onValueChanged.AddListener(SetUIScale);
                
            if (languageDropdown != null)
                languageDropdown.onValueChanged.AddListener(SetLanguage);
            
            // 버튼 이벤트
            if (resetButton != null)
                resetButton.onClick.AddListener(ResetToDefaults);
                
            if (applyButton != null)
                applyButton.onClick.AddListener(ApplySettings);
                
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelSettings);
                
            SetupDropdowns();
        }
        
        private void SetupDropdowns()
        {
            // 품질 설정 드롭다운
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "Low", "Medium", "High", "Ultra"
                });
            }
            
            // 해상도 드롭다운
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                var resolutions = new System.Collections.Generic.List<string>
                {
                    "1920x1080", "1680x1050", "1600x900", "1366x768", "1280x720"
                };
                resolutionDropdown.AddOptions(resolutions);
            }
            
            // 언어 드롭다운
            if (languageDropdown != null)
            {
                languageDropdown.ClearOptions();
                languageDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "한국어", "English", "日本語", "中文"
                });
            }
        }
        
        public void SetMasterVolume(float volume)
        {
            if (audioMixer != null)
                audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
        }
        
        public void SetMusicVolume(float volume)
        {
            if (audioMixer != null)
                audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
        }
        
        public void SetSFXVolume(float volume)
        {
            if (audioMixer != null)
                audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
        }
        
        public void SetQuality(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
        }
        
        public void SetResolution(int resolutionIndex)
        {
            switch (resolutionIndex)
            {
                case 0: Screen.SetResolution(1920, 1080, Screen.fullScreen); break;
                case 1: Screen.SetResolution(1680, 1050, Screen.fullScreen); break;
                case 2: Screen.SetResolution(1600, 900, Screen.fullScreen); break;
                case 3: Screen.SetResolution(1366, 768, Screen.fullScreen); break;
                case 4: Screen.SetResolution(1280, 720, Screen.fullScreen); break;
            }
        }
        
        public void SetFullscreen(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }
        
        public void SetVSync(bool enabled)
        {
            QualitySettings.vSyncCount = enabled ? 1 : 0;
        }
        
        public void SetAutoSave(bool enabled)
        {
            PlayerPrefs.SetInt("AutoSave", enabled ? 1 : 0);
        }
        
        public void SetUIScale(float scale)
        {
            // UI 스케일 적용
            if (UIManager.Instance != null)
                UIManager.Instance.SetUIScale(scale);
        }
        
        public void SetLanguage(int languageIndex)
        {
            string[] languages = { "ko", "en", "ja", "zh" };
            if (languageIndex < languages.Length)
            {
                PlayerPrefs.SetString("Language", languages[languageIndex]);
            }
        }
        
        public void ResetToDefaults()
        {
            // 기본값으로 리셋
            if (masterVolumeSlider != null) masterVolumeSlider.value = 0.8f;
            if (musicVolumeSlider != null) musicVolumeSlider.value = 0.7f;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = 0.8f;
            if (qualityDropdown != null) qualityDropdown.value = 2;
            if (fullscreenToggle != null) fullscreenToggle.isOn = true;
            if (vsyncToggle != null) vsyncToggle.isOn = true;
            if (autoSaveToggle != null) autoSaveToggle.isOn = true;
            if (uiScaleSlider != null) uiScaleSlider.value = 1.0f;
            if (languageDropdown != null) languageDropdown.value = 0;
        }
        
        public void ApplySettings()
        {
            SaveSettings();
        }
        
        public void CancelSettings()
        {
            LoadSettings();
        }
        
        private void SaveSettings()
        {
            if (masterVolumeSlider != null)
                PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
            if (musicVolumeSlider != null)
                PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
            if (sfxVolumeSlider != null)
                PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
            if (qualityDropdown != null)
                PlayerPrefs.SetInt("QualityLevel", qualityDropdown.value);
            if (fullscreenToggle != null)
                PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
            if (vsyncToggle != null)
                PlayerPrefs.SetInt("VSync", vsyncToggle.isOn ? 1 : 0);
            if (uiScaleSlider != null)
                PlayerPrefs.SetFloat("UIScale", uiScaleSlider.value);
            if (languageDropdown != null)
                PlayerPrefs.SetInt("Language", languageDropdown.value);
                
            PlayerPrefs.Save();
        }
        
        private void LoadSettings()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
            if (qualityDropdown != null)
                qualityDropdown.value = PlayerPrefs.GetInt("QualityLevel", 2);
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            if (vsyncToggle != null)
                vsyncToggle.isOn = PlayerPrefs.GetInt("VSync", 1) == 1;
            if (uiScaleSlider != null)
                uiScaleSlider.value = PlayerPrefs.GetFloat("UIScale", 1.0f);
            if (languageDropdown != null)
                languageDropdown.value = PlayerPrefs.GetInt("Language", 0);
        }
    }
} 