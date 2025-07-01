using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GuildMaster.Battle;
using GuildMaster.Core;
using GuildMaster.Systems;

namespace GuildMaster.UI
{
    /// <summary>
    /// 2:2 부대 전투 UI 관리자
    /// 각 부대의 3x3 그리드를 표시하고 전투 상황을 시각화
    /// </summary>
    public class BattleUIManager : MonoBehaviour
    {
        private static BattleUIManager _instance;
        public static BattleUIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<BattleUIManager>();
                }
                return _instance;
            }
        }

        [Header("UI 컨테이너")]
        [SerializeField] private GameObject battleUIContainer;
        [SerializeField] private Transform playerSquadsContainer;
        [SerializeField] private Transform enemySquadsContainer;

        [Header("부대 UI 프리팹")]
        [SerializeField] private GameObject squadUIPrefab;
        [SerializeField] private GameObject unitSlotPrefab;

        [Header("전투 정보 UI")]
        [SerializeField] private TextMeshProUGUI turnCountText;
        [SerializeField] private TextMeshProUGUI battleTimerText;
        [SerializeField] private Slider playerHealthBar;
        [SerializeField] private Slider enemyHealthBar;
        [SerializeField] private TextMeshProUGUI playerHealthText;
        [SerializeField] private TextMeshProUGUI enemyHealthText;

        [Header("부대 정보")]
        [SerializeField] private TextMeshProUGUI[] playerSquadNames = new TextMeshProUGUI[2];
        [SerializeField] private TextMeshProUGUI[] enemySquadNames = new TextMeshProUGUI[2];
        [SerializeField] private Image[] playerSquadTurnIndicators = new Image[2];
        [SerializeField] private Image[] enemySquadTurnIndicators = new Image[2];

        [Header("전투 결과 UI")]
        [SerializeField] private GameObject battleResultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultDetailsText;
        [SerializeField] private Button resultConfirmButton;

        [Header("효과 설정")]
        [SerializeField] private Color activeSquadColor = Color.yellow;
        [SerializeField] private Color inactiveSquadColor = Color.gray;
        [SerializeField] private float damageNumberDuration = 1.5f;
        [SerializeField] private GameObject damageNumberPrefab;

        // 부대 UI 컴포넌트
        private List<SquadUIComponent> playerSquadUIs = new List<SquadUIComponent>();
        private List<SquadUIComponent> enemySquadUIs = new List<SquadUIComponent>();

        // 전투 시간 추적
        private float battleStartTime;
        private Coroutine battleTimerCoroutine;

        void Awake()
        {
            _instance = this;
        }

        void Start()
        {
            // 전투 시스템 이벤트 구독
            if (SquadBattleSystem.Instance != null)
            {
                SquadBattleSystem.Instance.OnBattleStart += OnBattleStart;
                SquadBattleSystem.Instance.OnBattleEnd += OnBattleEnd;
                SquadBattleSystem.Instance.OnSquadTurnStart += OnSquadTurnStart;
                SquadBattleSystem.Instance.OnUnitDamaged += OnUnitDamaged;
                SquadBattleSystem.Instance.OnUnitDefeated += OnUnitDefeated;
            }

            // 초기 UI 숨기기
            if (battleUIContainer != null)
                battleUIContainer.SetActive(false);
        }

        /// <summary>
        /// 전투 시작 시 UI 초기화
        /// </summary>
        private void OnBattleStart(SquadFormation playerFormation, SquadFormation enemyFormation)
        {
            // UI 컨테이너 활성화
            if (battleUIContainer != null)
                battleUIContainer.SetActive(true);

            // 기존 UI 정리
            ClearSquadUIs();

            // 플레이어 부대 UI 생성 (2부대)
            for (int i = 0; i < 2; i++)
            {
                if (i < playerFormation.Squads.Count)
                {
                    var squadUI = CreateSquadUI(playerFormation.Squads[i], playerSquadsContainer, true);
                    playerSquadUIs.Add(squadUI);
                    
                    if (i < playerSquadNames.Length && playerSquadNames[i] != null)
                        playerSquadNames[i].text = playerFormation.Squads[i].Name;
                }
            }

            // 적 부대 UI 생성 (2부대)
            for (int i = 0; i < 2; i++)
            {
                if (i < enemyFormation.Squads.Count)
                {
                    var squadUI = CreateSquadUI(enemyFormation.Squads[i], enemySquadsContainer, false);
                    enemySquadUIs.Add(squadUI);
                    
                    if (i < enemySquadNames.Length && enemySquadNames[i] != null)
                        enemySquadNames[i].text = enemyFormation.Squads[i].Name;
                }
            }

            // 체력바 초기화
            UpdateHealthBars();

            // 전투 타이머 시작
            battleStartTime = Time.time;
            if (battleTimerCoroutine != null)
                StopCoroutine(battleTimerCoroutine);
            battleTimerCoroutine = StartCoroutine(UpdateBattleTimer());
        }

        /// <summary>
        /// 부대 UI 컴포넌트 생성
        /// </summary>
        private SquadUIComponent CreateSquadUI(Squad squad, Transform container, bool isPlayerSquad)
        {
            if (squadUIPrefab == null || container == null)
                return null;

            GameObject squadUIObj = Instantiate(squadUIPrefab, container);
            SquadUIComponent squadUI = squadUIObj.GetComponent<SquadUIComponent>();
            
            if (squadUI == null)
                squadUI = squadUIObj.AddComponent<SquadUIComponent>();

            squadUI.Initialize(squad, unitSlotPrefab, isPlayerSquad);
            return squadUI;
        }

        /// <summary>
        /// 부대 턴 시작 시 UI 업데이트
        /// </summary>
        private void OnSquadTurnStart(int squadIndex, bool isPlayer)
        {
            // 모든 부대 턴 인디케이터 비활성화
            foreach (var indicator in playerSquadTurnIndicators)
            {
                if (indicator != null)
                    indicator.color = inactiveSquadColor;
            }
            foreach (var indicator in enemySquadTurnIndicators)
            {
                if (indicator != null)
                    indicator.color = inactiveSquadColor;
            }

            // 현재 턴 부대 인디케이터 활성화
            if (isPlayer && squadIndex < playerSquadTurnIndicators.Length)
            {
                if (playerSquadTurnIndicators[squadIndex] != null)
                    playerSquadTurnIndicators[squadIndex].color = activeSquadColor;
            }
            else if (!isPlayer && squadIndex < enemySquadTurnIndicators.Length)
            {
                if (enemySquadTurnIndicators[squadIndex] != null)
                    enemySquadTurnIndicators[squadIndex].color = activeSquadColor;
            }

            // 턴 카운트 업데이트
            if (turnCountText != null)
                turnCountText.text = $"턴 {GetCurrentTurn()}";
        }

        /// <summary>
        /// 유닛 피해 시 UI 업데이트
        /// </summary>
        private void OnUnitDamaged(Unit unit, float damage)
        {
            // 데미지 숫자 표시
            ShowDamageNumber(unit, damage);

            // 체력바 업데이트
            UpdateHealthBars();

            // 유닛 UI 업데이트
            UpdateUnitUI(unit);
        }

        /// <summary>
        /// 유닛 사망 시 UI 업데이트
        /// </summary>
        private void OnUnitDefeated(Unit unit)
        {
            // 유닛 UI 비활성화
            DisableUnitUI(unit);

            // 체력바 업데이트
            UpdateHealthBars();
        }

        /// <summary>
        /// 전투 종료 시 결과 표시
        /// </summary>
        private void OnBattleEnd(BattleResult result)
        {
            // 전투 타이머 중지
            if (battleTimerCoroutine != null)
            {
                StopCoroutine(battleTimerCoroutine);
                battleTimerCoroutine = null;
            }

            // 결과 패널 표시
            ShowBattleResult(result);
        }

        /// <summary>
        /// 데미지 숫자 표시
        /// </summary>
        private void ShowDamageNumber(Unit unit, float damage)
        {
            if (damageNumberPrefab == null) return;

            // 유닛의 UI 위치 찾기
            UnitUISlot unitSlot = FindUnitUISlot(unit);
            if (unitSlot == null) return;

            // 데미지 숫자 생성
            GameObject damageObj = Instantiate(damageNumberPrefab, unitSlot.transform.position, Quaternion.identity);
            damageObj.transform.SetParent(battleUIContainer.transform, true);

            // 데미지 텍스트 설정
            TextMeshProUGUI damageText = damageObj.GetComponentInChildren<TextMeshProUGUI>();
            if (damageText != null)
            {
                damageText.text = $"-{Mathf.RoundToInt(damage)}";
                damageText.color = damage > 50 ? Color.red : Color.yellow;
            }

            // 애니메이션 및 제거
            StartCoroutine(AnimateDamageNumber(damageObj));
        }

        /// <summary>
        /// 데미지 숫자 애니메이션
        /// </summary>
        private IEnumerator AnimateDamageNumber(GameObject damageObj)
        {
            if (damageObj == null) yield break;

            float elapsed = 0f;
            Vector3 startPos = damageObj.transform.position;
            Vector3 endPos = startPos + Vector3.up * 50f;

            while (elapsed < damageNumberDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / damageNumberDuration;

                // 위로 이동 및 페이드 아웃
                damageObj.transform.position = Vector3.Lerp(startPos, endPos, t);
                
                CanvasGroup canvasGroup = damageObj.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = damageObj.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 1f - t;

                yield return null;
            }

            Destroy(damageObj);
        }

        /// <summary>
        /// 체력바 업데이트
        /// </summary>
        private void UpdateHealthBars()
        {
            float playerTotalHealth = 0f;
            float playerMaxHealth = 0f;
            float enemyTotalHealth = 0f;
            float enemyMaxHealth = 0f;

            // 플레이어 부대 체력 계산
            foreach (var squadUI in playerSquadUIs)
            {
                if (squadUI != null && squadUI.Squad != null)
                {
                    playerTotalHealth += squadUI.Squad.TotalHealth;
                    playerMaxHealth += squadUI.Squad.TotalMaxHealth;
                }
            }

            // 적 부대 체력 계산
            foreach (var squadUI in enemySquadUIs)
            {
                if (squadUI != null && squadUI.Squad != null)
                {
                    enemyTotalHealth += squadUI.Squad.TotalHealth;
                    enemyMaxHealth += squadUI.Squad.TotalMaxHealth;
                }
            }

            // UI 업데이트
            if (playerHealthBar != null)
                playerHealthBar.value = playerMaxHealth > 0 ? playerTotalHealth / playerMaxHealth : 0f;
            if (enemyHealthBar != null)
                enemyHealthBar.value = enemyMaxHealth > 0 ? enemyTotalHealth / enemyMaxHealth : 0f;

            if (playerHealthText != null)
                playerHealthText.text = $"{Mathf.RoundToInt(playerTotalHealth)} / {Mathf.RoundToInt(playerMaxHealth)}";
            if (enemyHealthText != null)
                enemyHealthText.text = $"{Mathf.RoundToInt(enemyTotalHealth)} / {Mathf.RoundToInt(enemyMaxHealth)}";
        }

        /// <summary>
        /// 전투 타이머 업데이트
        /// </summary>
        private IEnumerator UpdateBattleTimer()
        {
            while (true)
            {
                float elapsed = Time.time - battleStartTime;
                int minutes = Mathf.FloorToInt(elapsed / 60f);
                int seconds = Mathf.FloorToInt(elapsed % 60f);

                if (battleTimerText != null)
                    battleTimerText.text = $"{minutes:00}:{seconds:00}";

                yield return new WaitForSeconds(1f);
            }
        }

        /// <summary>
        /// 전투 결과 표시
        /// </summary>
        private void ShowBattleResult(BattleResult result)
        {
            if (battleResultPanel == null) return;

            battleResultPanel.SetActive(true);

            // 결과 제목
            if (resultTitleText != null)
            {
                resultTitleText.text = result.IsVictory ? "승리!" : "패배";
                resultTitleText.color = result.IsVictory ? Color.yellow : Color.red;
            }

            // 결과 상세
            if (resultDetailsText != null)
            {
                float battleTime = Time.time - battleStartTime;
                int minutes = Mathf.FloorToInt(battleTime / 60f);
                int seconds = Mathf.FloorToInt(battleTime % 60f);

                resultDetailsText.text = $"전투 시간: {minutes:00}:{seconds:00}\n" +
                                       $"총 턴 수: {result.TotalTurns}\n" +
                                       $"남은 유닛: {result.RemainingUnits}\n" +
                                       $"처치한 적: {result.EnemiesDefeated}";
            }

            // 확인 버튼
            if (resultConfirmButton != null)
            {
                resultConfirmButton.onClick.RemoveAllListeners();
                resultConfirmButton.onClick.AddListener(() => {
                    battleResultPanel.SetActive(false);
                    battleUIContainer.SetActive(false);
                });
            }
        }

        /// <summary>
        /// 유닛 UI 업데이트
        /// </summary>
        private void UpdateUnitUI(Unit unit)
        {
            UnitUISlot unitSlot = FindUnitUISlot(unit);
            if (unitSlot != null)
                unitSlot.UpdateUI();
        }

        /// <summary>
        /// 유닛 UI 비활성화
        /// </summary>
        private void DisableUnitUI(Unit unit)
        {
            UnitUISlot unitSlot = FindUnitUISlot(unit);
            if (unitSlot != null)
                unitSlot.SetDefeated();
        }

        /// <summary>
        /// 유닛의 UI 슬롯 찾기
        /// </summary>
        private UnitUISlot FindUnitUISlot(Unit unit)
        {
            // 플레이어 부대에서 검색
            foreach (var squadUI in playerSquadUIs)
            {
                var slot = squadUI.GetUnitSlot(unit);
                if (slot != null) return slot;
            }

            // 적 부대에서 검색
            foreach (var squadUI in enemySquadUIs)
            {
                var slot = squadUI.GetUnitSlot(unit);
                if (slot != null) return slot;
            }

            return null;
        }

        /// <summary>
        /// 현재 턴 수 가져오기
        /// </summary>
        private int GetCurrentTurn()
        {
            // SquadBattleSystem에서 턴 정보를 가져올 수 있다면 사용
            return 1; // 임시 구현
        }

        /// <summary>
        /// 부대 UI 정리
        /// </summary>
        private void ClearSquadUIs()
        {
            foreach (var squadUI in playerSquadUIs)
            {
                if (squadUI != null && squadUI.gameObject != null)
                    Destroy(squadUI.gameObject);
            }
            playerSquadUIs.Clear();

            foreach (var squadUI in enemySquadUIs)
            {
                if (squadUI != null && squadUI.gameObject != null)
                    Destroy(squadUI.gameObject);
            }
            enemySquadUIs.Clear();
        }

        void OnDestroy()
        {
            // 이벤트 구독 해제
            if (SquadBattleSystem.Instance != null)
            {
                SquadBattleSystem.Instance.OnBattleStart -= OnBattleStart;
                SquadBattleSystem.Instance.OnBattleEnd -= OnBattleEnd;
                SquadBattleSystem.Instance.OnSquadTurnStart -= OnSquadTurnStart;
                SquadBattleSystem.Instance.OnUnitDamaged -= OnUnitDamaged;
                SquadBattleSystem.Instance.OnUnitDefeated -= OnUnitDefeated;
            }
        }
    }

    /// <summary>
    /// 전투 결과 데이터
    /// </summary>
    [System.Serializable]
    public class BattleResult
    {
        public bool IsVictory;
        public int TotalTurns;
        public int RemainingUnits;
        public int EnemiesDefeated;
        public float BattleDuration;
    }
}