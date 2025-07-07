using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace GuildMaster.UI
{
    /// <summary>
    /// UI 관리자 - 모든 UI 패널과 창을 관리
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UIManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("UIManager");
                        _instance = go.AddComponent<UIManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("UI Panels")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject gameplayUI;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject settingsPanel;

        // UI 패널들의 딕셔너리
        private Dictionary<string, GameObject> uiPanels = new Dictionary<string, GameObject>();
        private Stack<GameObject> panelStack = new Stack<GameObject>();

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeUI();
        }

        void InitializeUI()
        {
            // UI 패널들을 딕셔너리에 등록
            if (mainMenuPanel != null) uiPanels["MainMenu"] = mainMenuPanel;
            if (gameplayUI != null) uiPanels["Gameplay"] = gameplayUI;
            if (inventoryPanel != null) uiPanels["Inventory"] = inventoryPanel;
            if (settingsPanel != null) uiPanels["Settings"] = settingsPanel;
            if (loadingScreen != null) uiPanels["Loading"] = loadingScreen;

            // 시작 시 모든 패널 비활성화
            HideAllPanels();
        }

        public void ShowPanel(string panelName)
        {
            if (uiPanels.ContainsKey(panelName))
            {
                var panel = uiPanels[panelName];
                panel.SetActive(true);
                panelStack.Push(panel);
            }
        }

        public void HidePanel(string panelName)
        {
            if (uiPanels.ContainsKey(panelName))
            {
                uiPanels[panelName].SetActive(false);
            }
        }

        public void HideCurrentPanel()
        {
            if (panelStack.Count > 0)
            {
                var currentPanel = panelStack.Pop();
                currentPanel.SetActive(false);
            }
        }

        public void HideAllPanels()
        {
            foreach (var panel in uiPanels.Values)
            {
                panel.SetActive(false);
            }
            panelStack.Clear();
        }

        public bool IsPanelActive(string panelName)
        {
            if (uiPanels.ContainsKey(panelName))
            {
                return uiPanels[panelName].activeInHierarchy;
            }
            return false;
        }

        public void RegisterPanel(string panelName, GameObject panel)
        {
            uiPanels[panelName] = panel;
        }

        public void ShowLoadingScreen()
        {
            ShowPanel("Loading");
        }

        public void HideLoadingScreen()
        {
            HidePanel("Loading");
        }

        public void SetUIScale(float scale)
        {
            if (mainCanvas != null)
            {
                CanvasScaler canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
                if (canvasScaler != null)
                {
                    canvasScaler.scaleFactor = scale;
                }
            }
        }
    }
} 