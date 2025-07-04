using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace GuildMaster.UI
{
    public class BattleUIManager : MonoBehaviour
    {
        [Header("Battle UI Elements")]
        public GameObject battlePanel;
        public TextMeshProUGUI battleStatusText;
        public Slider healthBar;
        public Slider manaBar;
        public Button pauseButton;
        public Button autoButton;
        
        [Header("Unit UI")]
        public Transform unitPanelParent;
        public GameObject unitPanelPrefab;
        
        [Header("Skill UI")]
        public Transform skillButtonParent;
        public GameObject skillButtonPrefab;
        
        private List<GameObject> unitPanels = new List<GameObject>();
        private List<Button> skillButtons = new List<Button>();
        
        private static BattleUIManager _instance;
        public static BattleUIManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<BattleUIManager>();
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            InitializeUI();
        }
        
        private void InitializeUI()
        {
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseClicked);
                
            if (autoButton != null)
                autoButton.onClick.AddListener(OnAutoClicked);
        }
        
        public void UpdateBattleStatus(string status)
        {
            if (battleStatusText != null)
                battleStatusText.text = status;
        }
        
        public void UpdateHealthBar(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.value = current / max;
            }
        }
        
        public void UpdateManaBar(float current, float max)
        {
            if (manaBar != null)
            {
                manaBar.value = current / max;
            }
        }
        
        public void ShowBattleUI()
        {
            if (battlePanel != null)
                battlePanel.SetActive(true);
        }
        
        public void HideBattleUI()
        {
            if (battlePanel != null)
                battlePanel.SetActive(false);
        }
        
        public void CreateUnitPanel(string unitName, Sprite unitIcon)
        {
            if (unitPanelPrefab != null && unitPanelParent != null)
            {
                GameObject unitPanel = Instantiate(unitPanelPrefab, unitPanelParent);
                unitPanels.Add(unitPanel);
                
                var nameText = unitPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText != null)
                    nameText.text = unitName;
                    
                var iconImage = unitPanel.GetComponentInChildren<Image>();
                if (iconImage != null && unitIcon != null)
                    iconImage.sprite = unitIcon;
            }
        }
        
        public void ClearUnitPanels()
        {
            foreach (var panel in unitPanels)
            {
                if (panel != null)
                    Destroy(panel);
            }
            unitPanels.Clear();
        }
        
        private void OnPauseClicked()
        {
            Time.timeScale = Time.timeScale == 0 ? 1 : 0;
        }
        
        private void OnAutoClicked()
        {
            // 자동 전투 토글
        }
    }
} 