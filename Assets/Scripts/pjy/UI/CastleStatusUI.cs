using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using pjy.Gameplay;

namespace pjy.UI
{
    /// <summary>
    /// 성 상태 UI - 향상된 성 시스템 연동
    /// 각 성의 체력, 공격 상태, 버프 효과 등을 표시
    /// </summary>
    public class CastleStatusUI : MonoBehaviour
    {
        [Header("중간성 UI")]
        [SerializeField] private GameObject leftCastleUI;
        [SerializeField] private GameObject centerCastleUI;
        [SerializeField] private GameObject rightCastleUI;
        
        [Header("최종성 UI")]
        [SerializeField] private GameObject finalCastleUI;
        
        [Header("UI 요소들")]
        [SerializeField] private Slider leftHealthSlider;
        [SerializeField] private Slider centerHealthSlider;
        [SerializeField] private Slider rightHealthSlider;
        [SerializeField] private Slider finalHealthSlider;
        
        [SerializeField] private TextMeshProUGUI leftHealthText;
        [SerializeField] private TextMeshProUGUI centerHealthText;
        [SerializeField] private TextMeshProUGUI rightHealthText;
        [SerializeField] private TextMeshProUGUI finalHealthText;
        
        [Header("공격 상태 표시")]
        [SerializeField] private Image leftAttackIndicator;
        [SerializeField] private Image centerAttackIndicator;
        [SerializeField] private Image rightAttackIndicator;
        [SerializeField] private Image finalAttackIndicator;
        
        [Header("버프 효과 표시")]
        [SerializeField] private GameObject defenseBuffIcon;
        [SerializeField] private TextMeshProUGUI defenseBuffText;
        
        [Header("색상 설정")]
        [SerializeField] private Color normalHealthColor = Color.green;
        [SerializeField] private Color warningHealthColor = Color.yellow;
        [SerializeField] private Color criticalHealthColor = Color.red;
        [SerializeField] private Color attackingColor = Color.red;
        [SerializeField] private Color idleColor = Color.gray;
        
        [Header("성 참조")]
        private List<EnhancedCastleSystem> castles = new List<EnhancedCastleSystem>();
        private CastleHealthManager castleHealthManager;
        
        private void Start()
        {
            // CastleHealthManager 찾기
            castleHealthManager = CastleHealthManager.Instance;
            
            // 모든 향상된 성 시스템 찾기
            castles.AddRange(FindObjectsByType<EnhancedCastleSystem>(FindObjectsSortMode.None));
            
            // 초기 UI 업데이트
            UpdateAllCastleUI();
        }
        
        private void Update()
        {
            UpdateAllCastleUI();
        }
        
        /// <summary>
        /// 모든 성 UI 업데이트
        /// </summary>
        private void UpdateAllCastleUI()
        {
            foreach (var castle in castles)
            {
                if (castle == null) continue;
                
                if (castle.GetCastleType() == EnhancedCastleSystem.CastleType.Final)
                {
                    UpdateFinalCastleUI(castle);
                }
                else
                {
                    UpdateMiddleCastleUI(castle);
                }
            }
            
            // 방어 버프 아이콘 업데이트
            UpdateDefenseBuffDisplay();
        }
        
        /// <summary>
        /// 중간성 UI 업데이트
        /// </summary>
        private void UpdateMiddleCastleUI(EnhancedCastleSystem castle)
        {
            RouteType route = castle.GetRouteType();
            GameObject uiObject = null;
            Slider healthSlider = null;
            TextMeshProUGUI healthText = null;
            Image attackIndicator = null;
            
            // 라우트별 UI 요소 선택
            switch (route)
            {
                case RouteType.Left:
                    uiObject = leftCastleUI;
                    healthSlider = leftHealthSlider;
                    healthText = leftHealthText;
                    attackIndicator = leftAttackIndicator;
                    break;
                case RouteType.Center:
                    uiObject = centerCastleUI;
                    healthSlider = centerHealthSlider;
                    healthText = centerHealthText;
                    attackIndicator = centerAttackIndicator;
                    break;
                case RouteType.Right:
                    uiObject = rightCastleUI;
                    healthSlider = rightHealthSlider;
                    healthText = rightHealthText;
                    attackIndicator = rightAttackIndicator;
                    break;
            }
            
            if (uiObject == null) return;
            
            // 성이 파괴되었는지 확인
            if (castle.IsDestroyed())
            {
                uiObject.SetActive(false);
                return;
            }
            
            uiObject.SetActive(true);
            
            // 체력 업데이트
            float healthPercent = castle.GetHealthPercentage();
            if (healthSlider != null)
            {
                healthSlider.value = healthPercent;
                
                // 체력별 색상
                if (healthPercent > 0.6f)
                    healthSlider.fillRect.GetComponent<Image>().color = normalHealthColor;
                else if (healthPercent > 0.3f)
                    healthSlider.fillRect.GetComponent<Image>().color = warningHealthColor;
                else
                    healthSlider.fillRect.GetComponent<Image>().color = criticalHealthColor;
            }
            
            // 체력 텍스트
            if (healthText != null && castleHealthManager != null)
            {
                int currentHealth = 0;
                switch (route)
                {
                    case RouteType.Left:
                        currentHealth = castleHealthManager.leftMidCastleHealth;
                        break;
                    case RouteType.Center:
                        currentHealth = castleHealthManager.centerMidCastleHealth;
                        break;
                    case RouteType.Right:
                        currentHealth = castleHealthManager.rightMidCastleHealth;
                        break;
                }
                healthText.text = $"{currentHealth}/500";
            }
            
            // 공격 상태 표시
            UpdateAttackIndicator(castle, attackIndicator);
        }
        
        /// <summary>
        /// 최종성 UI 업데이트
        /// </summary>
        private void UpdateFinalCastleUI(EnhancedCastleSystem castle)
        {
            if (finalCastleUI == null) return;
            
            if (castle.IsDestroyed())
            {
                finalCastleUI.SetActive(false);
                return;
            }
            
            finalCastleUI.SetActive(true);
            
            // 체력 업데이트
            float healthPercent = castle.GetHealthPercentage();
            if (finalHealthSlider != null)
            {
                finalHealthSlider.value = healthPercent;
                
                // 체력별 색상
                if (healthPercent > 0.6f)
                    finalHealthSlider.fillRect.GetComponent<Image>().color = normalHealthColor;
                else if (healthPercent > 0.3f)
                    finalHealthSlider.fillRect.GetComponent<Image>().color = warningHealthColor;
                else
                    finalHealthSlider.fillRect.GetComponent<Image>().color = criticalHealthColor;
            }
            
            // 체력 텍스트
            if (finalHealthText != null && castleHealthManager != null)
            {
                finalHealthText.text = $"{castleHealthManager.finalCastleCurrentHealth}/1000";
            }
            
            // 공격 상태 표시
            UpdateAttackIndicator(castle, finalAttackIndicator);
        }
        
        /// <summary>
        /// 공격 상태 인디케이터 업데이트
        /// </summary>
        private void UpdateAttackIndicator(EnhancedCastleSystem castle, Image indicator)
        {
            if (indicator == null) return;
            
            // 간단한 깜빡임 효과로 공격 상태 표시
            bool isAttacking = Time.frameCount % 60 < 30; // 0.5초마다 깜빡임
            
            if (isAttacking && !castle.IsDestroyed())
            {
                indicator.color = attackingColor;
            }
            else
            {
                indicator.color = idleColor;
            }
        }
        
        /// <summary>
        /// 방어 버프 표시 업데이트
        /// </summary>
        private void UpdateDefenseBuffDisplay()
        {
            bool hasDefenseBuff = false;
            int buffedCastleCount = 0;
            
            foreach (var castle in castles)
            {
                if (castle != null && !castle.IsDestroyed() && castle.GetCastleType() == EnhancedCastleSystem.CastleType.Final)
                {
                    hasDefenseBuff = true;
                    buffedCastleCount++;
                }
            }
            
            if (defenseBuffIcon != null)
            {
                defenseBuffIcon.SetActive(hasDefenseBuff);
            }
            
            if (defenseBuffText != null && hasDefenseBuff)
            {
                defenseBuffText.text = "방어 버프 활성";
            }
        }
        
        /// <summary>
        /// 특정 성 파괴 알림
        /// </summary>
        public void OnCastleDestroyed(RouteType route, bool isFinalCastle)
        {
            string message = isFinalCastle ? 
                "최종성이 파괴되었습니다!" : 
                $"{route} 중간성이 파괴되었습니다!";
                
            Debug.LogWarning($"[CastleStatusUI] {message}");
            
            // TODO: 화면에 경고 메시지 표시
        }
    }
}