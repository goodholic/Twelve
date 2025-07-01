using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using GuildMaster.Battle;

namespace GuildMaster.UI
{
    using GuildMaster.Core;
    
    public class BattleUIManager : MonoBehaviour
    {
        [Header("Battle Overview")]
        [SerializeField] private GameObject battleCanvas;
        [SerializeField] private TextMeshProUGUI battleStateText;
        [SerializeField] private TextMeshProUGUI turnIndicatorText;
        [SerializeField] private Slider battleProgressBar;
        
        [Header("Squad UI Panels")]
        [SerializeField] private SquadUIPanel[] playerSquadPanels = new SquadUIPanel[4];
        [SerializeField] private SquadUIPanel[] enemySquadPanels = new SquadUIPanel[4];
        
        [Header("Battle Speed Controls")]
        [SerializeField] private Button speedx1Button;
        [SerializeField] private Button speedx2Button;
        [SerializeField] private Button speedx4Button;
        [SerializeField] private TextMeshProUGUI currentSpeedText;
        
        [Header("Battle Statistics")]
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private TextMeshProUGUI totalDamageText;
        [SerializeField] private TextMeshProUGUI totalHealingText;
        [SerializeField] private TextMeshProUGUI unitsKilledText;
        [SerializeField] private TextMeshProUGUI battleDurationText;
        
        [Header("Battle Log")]
        [SerializeField] private GameObject battleLogPanel;
        [SerializeField] private Transform battleLogContent;
        [SerializeField] private GameObject battleLogEntryPrefab;
        [SerializeField] private int maxLogEntries = 50;
        private Queue<GameObject> battleLogEntries = new Queue<GameObject>();
        
        [Header("Damage Numbers")]
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private GameObject healNumberPrefab;
        [SerializeField] private Transform damageNumberContainer;
        
        [Header("Battle Tactics")]
        [SerializeField] private Dropdown tacticDropdown;
        [SerializeField] private TextMeshProUGUI tacticDescriptionText;
        
        [Header("Victory/Defeat Screen")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultDescriptionText;
        [SerializeField] private GameObject rewardPanel;
        [SerializeField] private TextMeshProUGUI goldRewardText;
        [SerializeField] private TextMeshProUGUI expRewardText;
        
        private BattleManager battleManager;
        private float currentBattleSpeed = 1f;
        
        [System.Serializable]
        public class SquadUIPanel
        {
            public GameObject panel;
            public TextMeshProUGUI squadNameText;
            public TextMeshProUGUI squadHealthText;
            public Slider squadHealthBar;
            public GridLayoutGroup unitGrid;
            public UnitUIElement[] unitElements = new UnitUIElement[9];
            public Image squadRoleIcon;
            public TextMeshProUGUI aliveCountText;
            public GameObject activeIndicator;
        }
        
        [System.Serializable]
        public class UnitUIElement
        {
            public GameObject unitObject;
            public Image unitIcon;
            public Slider healthBar;
            public Slider manaBar;
            public TextMeshProUGUI levelText;
            public Image jobClassIcon;
            public GameObject statusEffectContainer;
            public GameObject deathOverlay;
        }
        
        void Start()
        {
            battleManager = GameManager.Instance?.BattleManager;
            if (battleManager == null)
            {
                Debug.LogError("BattleManager not found!");
                return;
            }
            
            // Subscribe to battle events
            battleManager.OnBattleStateChanged += OnBattleStateChanged;
            battleManager.OnSquadTurnStart += OnSquadTurnStart;
            battleManager.OnUnitAttack += OnUnitAttack;
            battleManager.OnUnitHeal += OnUnitHeal;
            battleManager.OnUnitDeath += OnUnitDeath;
            battleManager.OnBattleEnd += OnBattleEnd;
            
            // Setup UI
            SetupSpeedButtons();
            SetupTacticDropdown();
            
            // Hide battle UI initially
            if (battleCanvas != null)
                battleCanvas.SetActive(false);
        }
        
        void SetupSpeedButtons()
        {
            if (speedx1Button != null)
                speedx1Button.onClick.AddListener(() => SetBattleSpeed(1f));
            if (speedx2Button != null)
                speedx2Button.onClick.AddListener(() => SetBattleSpeed(2f));
            if (speedx4Button != null)
                speedx4Button.onClick.AddListener(() => SetBattleSpeed(4f));
        }
        
        void SetupTacticDropdown()
        {
            if (tacticDropdown == null) return;
            
            tacticDropdown.ClearOptions();
            var options = new List<string> { "균형", "공격적", "방어적", "측면공격", "집중공격", "전면공격" };
            tacticDropdown.AddOptions(options);
            
            tacticDropdown.onValueChanged.AddListener(OnTacticChanged);
        }
        
        void OnTacticChanged(int index)
        {
            var tactic = (BattleManager.BattleTactic)index;
            battleManager?.SetPlayerTactic(tactic);
            
            // Update description
            if (tacticDescriptionText != null)
            {
                switch (tactic)
                {
                    case BattleManager.BattleTactic.Balanced:
                        tacticDescriptionText.text = "균형잡힌 전투 방식";
                        break;
                    case BattleManager.BattleTactic.Aggressive:
                        tacticDescriptionText.text = "공격력 +20%, 방어력 -10%";
                        break;
                    case BattleManager.BattleTactic.Defensive:
                        tacticDescriptionText.text = "방어력 +30%, 공격력 -15%";
                        break;
                    case BattleManager.BattleTactic.Flanking:
                        tacticDescriptionText.text = "후방 유닛 우선 공격";
                        break;
                    case BattleManager.BattleTactic.Focus:
                        tacticDescriptionText.text = "체력이 낮은 적 우선 공격";
                        break;
                    case BattleManager.BattleTactic.Blitz:
                        tacticDescriptionText.text = "속도 +40%, 방어력 -20%";
                        break;
                }
            }
        }
        
        public void ShowBattleUI()
        {
            if (battleCanvas != null)
                battleCanvas.SetActive(true);
                
            // Initialize squad panels
            InitializeSquadPanels();
            
            // Reset battle log
            ClearBattleLog();
            
            // Hide result panel
            if (resultPanel != null)
                resultPanel.SetActive(false);
        }
        
        public void HideBattleUI()
        {
            if (battleCanvas != null)
                battleCanvas.SetActive(false);
        }
        
        void InitializeSquadPanels()
        {
            var playerSquads = battleManager.GetPlayerSquads();
            var enemySquads = battleManager.GetEnemySquads();
            
            // Initialize player squad panels
            for (int i = 0; i < playerSquadPanels.Length; i++)
            {
                if (i < playerSquads.Length && playerSquads[i] != null)
                {
                    InitializeSquadPanel(playerSquadPanels[i], playerSquads[i], true);
                }
            }
            
            // Initialize enemy squad panels
            for (int i = 0; i < enemySquadPanels.Length; i++)
            {
                if (i < enemySquads.Length && enemySquads[i] != null)
                {
                    InitializeSquadPanel(enemySquadPanels[i], enemySquads[i], false);
                }
            }
        }
        
        void InitializeSquadPanel(SquadUIPanel panel, BattleManager.BattleSquad squad, bool isPlayer)
        {
            if (panel.panel != null)
                panel.panel.SetActive(true);
                
            if (panel.squadNameText != null)
                panel.squadNameText.text = $"{(isPlayer ? "아군" : "적군")} {squad.SquadIndex + 1}분대";
                
            // Initialize unit UI elements
            int unitIndex = 0;
            for (int row = 0; row < BattleManager.SQUAD_ROWS; row++)
            {
                for (int col = 0; col < BattleManager.SQUAD_COLS; col++)
                {
                    if (unitIndex < panel.unitElements.Length)
                    {
                        var unit = squad.Units[row, col];
                        var unitElement = panel.unitElements[unitIndex];
                        
                        if (unit != null)
                        {
                            InitializeUnitElement(unitElement, unit);
                        }
                        else
                        {
                            if (unitElement.unitObject != null)
                                unitElement.unitObject.SetActive(false);
                        }
                        
                        unitIndex++;
                    }
                }
            }
            
            UpdateSquadPanel(panel, squad);
        }
        
        void InitializeUnitElement(UnitUIElement element, Unit unit)
        {
            if (element.unitObject != null)
                element.unitObject.SetActive(true);
                
            if (element.levelText != null)
                element.levelText.text = $"Lv.{unit.Level}";
                
            if (element.jobClassIcon != null)
                element.jobClassIcon.sprite = GetJobClassSprite(unit.JobClass);
                
            UpdateUnitElement(element, unit);
        }
        
        void UpdateUnitElement(UnitUIElement element, Unit unit)
        {
            if (element.healthBar != null)
            {
                element.healthBar.value = unit.GetHealthPercentage();
            }
            
            if (element.manaBar != null)
            {
                element.manaBar.value = unit.GetManaPercentage();
            }
            
            if (element.deathOverlay != null)
            {
                element.deathOverlay.SetActive(!unit.IsAlive);
            }
        }
        
        void UpdateSquadPanel(SquadUIPanel panel, BattleManager.BattleSquad squad)
        {
            if (panel.squadHealthText != null)
                panel.squadHealthText.text = $"{(int)squad.TotalHealth}/{(int)squad.TotalHealth}";
                
            if (panel.squadHealthBar != null)
                panel.squadHealthBar.value = squad.TotalHealth / squad.TotalHealth;
                
            if (panel.aliveCountText != null)
                panel.aliveCountText.text = $"생존: {squad.GetAliveUnitsCount()}/9";
        }
        
        void OnBattleStateChanged(BattleManager.BattleState state)
        {
            if (battleStateText != null)
            {
                switch (state)
                {
                    case BattleManager.BattleState.Preparation:
                        battleStateText.text = "전투 준비중...";
                        break;
                    case BattleManager.BattleState.InProgress:
                        battleStateText.text = "전투 진행중";
                        ShowBattleUI();
                        break;
                    case BattleManager.BattleState.Victory:
                        battleStateText.text = "승리!";
                        break;
                    case BattleManager.BattleState.Defeat:
                        battleStateText.text = "패배...";
                        break;
                    case BattleManager.BattleState.Draw:
                        battleStateText.text = "무승부";
                        break;
                }
            }
        }
        
        void OnSquadTurnStart(BattleManager.BattleSquad activeSquad, BattleManager.BattleSquad targetSquad)
        {
            if (turnIndicatorText != null)
            {
                string side = activeSquad.IsPlayerSquad ? "아군" : "적군";
                turnIndicatorText.text = $"{side} {activeSquad.SquadIndex + 1}분대 턴";
            }
            
            // Update active indicators
            UpdateActiveIndicators(activeSquad);
            
            // Add to battle log
            AddBattleLogEntry($"{(activeSquad.IsPlayerSquad ? "아군" : "적군")} {activeSquad.SquadIndex + 1}분대가 행동을 시작합니다.");
        }
        
        void UpdateActiveIndicators(BattleManager.BattleSquad activeSquad)
        {
            // Clear all indicators
            foreach (var panel in playerSquadPanels.Concat(enemySquadPanels))
            {
                if (panel.activeIndicator != null)
                    panel.activeIndicator.SetActive(false);
            }
            
            // Set active indicator
            var panels = activeSquad.IsPlayerSquad ? playerSquadPanels : enemySquadPanels;
            if (activeSquad.SquadIndex < panels.Length && panels[activeSquad.SquadIndex].activeIndicator != null)
            {
                panels[activeSquad.SquadIndex].activeIndicator.SetActive(true);
            }
        }
        
        void OnUnitAttack(Unit attacker, Unit target, float damage)
        {
            // Show damage number
            ShowDamageNumber(target, damage);
            
            // Update unit UI
            UpdateUnitInSquadPanel(target);
            
            // Add to battle log
            string attackerName = $"{attacker.Name} ({attacker.GetJobIcon()})";
            string targetName = $"{target.Name} ({target.GetJobIcon()})";
            AddBattleLogEntry($"{attackerName}이(가) {targetName}에게 {(int)damage}의 피해를 입혔습니다!");
            
            // Update statistics
            UpdateBattleStatistics();
        }
        
        void OnUnitHeal(Unit target, float amount)
        {
            // Show heal number
            ShowHealNumber(target, amount);
            
            // Update unit UI
            UpdateUnitInSquadPanel(target);
            
            // Add to battle log
            string targetName = $"{target.Name} ({target.GetJobIcon()})";
            AddBattleLogEntry($"{targetName}이(가) {(int)amount}의 체력을 회복했습니다!");
            
            // Update statistics
            UpdateBattleStatistics();
        }
        
        void OnUnitDeath(Unit unit)
        {
            // Update unit UI
            UpdateUnitInSquadPanel(unit);
            
            // Add to battle log
            string unitName = $"{unit.Name} ({unit.GetJobIcon()})";
            AddBattleLogEntry($"{unitName}이(가) 쓰러졌습니다!", Color.red);
            
            // Update squad panel
            UpdateSquadPanelByUnit(unit);
        }
        
        void OnBattleEnd(bool victory)
        {
            StartCoroutine(ShowBattleResult(victory));
        }
        
        IEnumerator ShowBattleResult(bool victory)
        {
            yield return new WaitForSeconds(2f);
            
            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
                
                if (resultTitleText != null)
                    resultTitleText.text = victory ? "승리!" : "패배...";
                    
                if (resultDescriptionText != null)
                {
                    var stats = battleManager.GetBattleStatistics();
                    resultDescriptionText.text = $"전투 시간: {stats.BattleDuration:F1}초\n" +
                                               $"처치한 적: {stats.UnitsKilled}\n" +
                                               $"잃은 유닛: {stats.UnitsLost}";
                }
                
                if (victory && rewardPanel != null)
                {
                    rewardPanel.SetActive(true);
                    // TODO: Display actual rewards
                }
            }
        }
        
        void UpdateUnitInSquadPanel(Unit unit)
        {
            var squads = unit.IsPlayerUnit ? battleManager.GetPlayerSquads() : battleManager.GetEnemySquads();
            var panels = unit.IsPlayerUnit ? playerSquadPanels : enemySquadPanels;
            
            if (unit.SquadIndex < panels.Length)
            {
                var panel = panels[unit.SquadIndex];
                int unitIndex = unit.Row * BattleManager.SQUAD_COLS + unit.Col;
                
                if (unitIndex < panel.unitElements.Length)
                {
                    UpdateUnitElement(panel.unitElements[unitIndex], unit);
                }
            }
        }
        
        void UpdateSquadPanelByUnit(Unit unit)
        {
            var squads = unit.IsPlayerUnit ? battleManager.GetPlayerSquads() : battleManager.GetEnemySquads();
            var panels = unit.IsPlayerUnit ? playerSquadPanels : enemySquadPanels;
            
            if (unit.SquadIndex < squads.Length && unit.SquadIndex < panels.Length)
            {
                UpdateSquadPanel(panels[unit.SquadIndex], squads[unit.SquadIndex]);
            }
        }
        
        void ShowDamageNumber(Unit target, float damage)
        {
            if (damageNumberPrefab == null || damageNumberContainer == null) return;
            
            var damageNumber = Instantiate(damageNumberPrefab, damageNumberContainer);
            var text = damageNumber.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"-{(int)damage}";
            }
            
            // Position based on unit location
            // TODO: Convert unit grid position to screen position
            
            // Animate and destroy
            StartCoroutine(AnimateDamageNumber(damageNumber));
        }
        
        void ShowHealNumber(Unit target, float amount)
        {
            if (healNumberPrefab == null || damageNumberContainer == null) return;
            
            var healNumber = Instantiate(healNumberPrefab, damageNumberContainer);
            var text = healNumber.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"+{(int)amount}";
            }
            
            // Position based on unit location
            // TODO: Convert unit grid position to screen position
            
            // Animate and destroy
            StartCoroutine(AnimateDamageNumber(healNumber));
        }
        
        IEnumerator AnimateDamageNumber(GameObject number)
        {
            var rectTransform = number.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector3 startPos = rectTransform.anchoredPosition;
                Vector3 endPos = startPos + new Vector3(0, 100, 0);
                
                float duration = 1f;
                float elapsed = 0f;
                
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    
                    rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
                    
                    var canvasGroup = number.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = 1 - t;
                    }
                    
                    yield return null;
                }
            }
            
            Destroy(number);
        }
        
        void SetBattleSpeed(float speed)
        {
            currentBattleSpeed = speed;
            battleManager?.SetBattleSpeed(speed);
            
            if (currentSpeedText != null)
                currentSpeedText.text = $"x{speed}";
                
            // Update button states
            UpdateSpeedButtonStates();
        }
        
        void UpdateSpeedButtonStates()
        {
            if (speedx1Button != null)
                speedx1Button.interactable = currentBattleSpeed != 1f;
            if (speedx2Button != null)
                speedx2Button.interactable = currentBattleSpeed != 2f;
            if (speedx4Button != null)
                speedx4Button.interactable = currentBattleSpeed != 4f;
        }
        
        void UpdateBattleStatistics()
        {
            var stats = battleManager.GetBattleStatistics();
            
            if (totalDamageText != null)
                totalDamageText.text = $"총 피해량: {stats.TotalDamageDealt}";
            if (totalHealingText != null)
                totalHealingText.text = $"총 치유량: {stats.TotalHealingDone}";
            if (unitsKilledText != null)
                unitsKilledText.text = $"처치: {stats.UnitsKilled}";
            if (battleDurationText != null)
                battleDurationText.text = $"시간: {Time.time - stats.BattleDuration:F1}초";
        }
        
        void AddBattleLogEntry(string message, Color? color = null)
        {
            if (battleLogEntryPrefab == null || battleLogContent == null) return;
            
            var entry = Instantiate(battleLogEntryPrefab, battleLogContent);
            var text = entry.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"[{Time.time:F1}] {message}";
                if (color.HasValue)
                    text.color = color.Value;
            }
            
            battleLogEntries.Enqueue(entry);
            
            // Remove old entries
            while (battleLogEntries.Count > maxLogEntries)
            {
                var oldEntry = battleLogEntries.Dequeue();
                Destroy(oldEntry);
            }
            
            // Scroll to bottom
            Canvas.ForceUpdateCanvases();
            var scrollRect = battleLogPanel?.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        void ClearBattleLog()
        {
            while (battleLogEntries.Count > 0)
            {
                var entry = battleLogEntries.Dequeue();
                Destroy(entry);
            }
        }
        
        Sprite GetJobClassSprite(JobClass jobClass)
        {
            // TODO: Load actual sprites for each job class
            // For now, return null or placeholder
            return null;
        }
        
        void OnDestroy()
        {
            // Unsubscribe from events
            if (battleManager != null)
            {
                battleManager.OnBattleStateChanged -= OnBattleStateChanged;
                battleManager.OnSquadTurnStart -= OnSquadTurnStart;
                battleManager.OnUnitAttack -= OnUnitAttack;
                battleManager.OnUnitHeal -= OnUnitHeal;
                battleManager.OnUnitDeath -= OnUnitDeath;
                battleManager.OnBattleEnd -= OnBattleEnd;
            }
        }
    }
}